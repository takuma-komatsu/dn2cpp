"""Compare OFF/ON macOS or Android bodies; execute .NET parity only on macOS."""
from collections import Counter
import hashlib
import json
import re
import os
from pathlib import Path
import subprocess
import sys
from tempfile import TemporaryDirectory

root = Path(sys.argv[1]).resolve()
compiler = Path(sys.argv[2]).resolve()
work = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else root / "artifacts" / "declang-native-proof"
android = os.environ.get("DN2CPP_DECLANG_ANDROID") == "1"
work.mkdir(parents=True, exist_ok=True)
env = os.environ.copy()
env["DECLANG_HOME"] = str(work / "disabled")
(work / "disabled" / ".DeClang").mkdir(parents=True, exist_ok=True)
(work / "disabled" / ".DeClang" / "config.json").write_text('{"enable_obfuscation":0}', encoding="utf-8")


def run(*args):
    result = subprocess.run([str(a) for a in args], env=env, text=True, encoding="utf-8",
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode:
        raise AssertionError(f"Command failed: {args}\n{result.stdout}")
    return result.stdout


def cmake_compiler_directory(build, cmake_version):
    cache = (build / "CMakeCache.txt").read_text(encoding="utf-8")
    for component in ("MAJOR", "MINOR", "PATCH"):
        match = re.search(rf"^CMAKE_CACHE_{component}_VERSION:INTERNAL=([0-9]+)$", cache, re.M)
        assert match, f"CMake cache lacks its {component.lower()} version: {build}"
        assert int(match.group(1)) == cmake_version[component.lower()], build
    return build / "CMakeFiles" / cmake_version["string"]


def assert_compiler_identity(build, language, actual_version, cmake_version):
    identity = cmake_compiler_directory(build, cmake_version) / f"CMake{language}Compiler.cmake"
    assert identity.is_file(), f"Current CMake compiler metadata is missing: {identity}"
    identity_text = identity.read_text(encoding="utf-8")
    expected_compiler = f'set(CMAKE_{language}_COMPILER "{compiler.as_posix()}")'
    assert expected_compiler in identity_text, f"Current CMAKE_{language}_COMPILER is not DeClang: {identity}"
    assert f'set(CMAKE_{language}_COMPILER_VERSION "{actual_version}")' in identity_text, identity


def verify_compiler_identity_selection(build, actual_version, cmake_version):
    # Old CMake metadata may survive a successful reconfigure in the same build dir.
    numeric_version = ".".join(str(cmake_version[part]) for part in ("major", "minor", "patch"))
    prerelease_version = numeric_version + "-rc1"
    if prerelease_version == cmake_version["string"]:
        prerelease_version = numeric_version + "-rc2"
    cases = ((cmake_version, "0.0.0"),
             ({**cmake_version, "string": prerelease_version}, numeric_version))
    for version, stale_version in cases:
        with TemporaryDirectory(dir=work) as temporary:
            fixture = Path(temporary)
            (fixture / "CMakeCache.txt").write_bytes((build / "CMakeCache.txt").read_bytes())
            active = fixture / "CMakeFiles" / version["string"]
            stale = fixture / "CMakeFiles" / stale_version
            assert active != stale
            active.mkdir(parents=True)
            stale.mkdir(parents=True)
            for language in ("C", "CXX"):
                name = f"CMake{language}Compiler.cmake"
                identity_text = (cmake_compiler_directory(build, cmake_version) / name).read_text(encoding="utf-8")
                expected = f'set(CMAKE_{language}_COMPILER "{compiler.as_posix()}")'
                assert expected in identity_text
                wrong = identity_text.replace(expected, f'set(CMAKE_{language}_COMPILER "/unexpected/clang")')
                (active / name).write_text(identity_text, encoding="utf-8")
                (stale / name).write_text(wrong, encoding="utf-8")
                assert_compiler_identity(fixture, language, actual_version, version)
                (active / name).write_text(wrong, encoding="utf-8")
                try:
                    assert_compiler_identity(fixture, language, actual_version, version)
                except AssertionError as error:
                    assert f"CMAKE_{language}_COMPILER" in str(error), error
                else:
                    raise AssertionError(f"Non-DeClang CMAKE_{language}_COMPILER passed identity validation")
    print("DeClang compiler identity fixture passed: stale metadata and prerelease versions selected correctly", flush=True)


configuration = os.environ.get("CONFIG", "Release")
cli = Path(os.environ.get("DN2CPP_CLI_DLL", root / f"src/Dn2Cpp.Cli/bin/{configuration}/net10.0/dn2cpp.dll"))
if not os.environ.get("DN2CPP_SKIP_BUILD"):
    run("dotnet", "build", root / "src/Dn2Cpp.Cli", "-c", configuration,
        "-m:1", "-p:BuildInParallel=false", "--disable-build-servers")
fixture = work / "fixture"
fixture.mkdir(exist_ok=True)
(fixture / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Optimize>true</Optimize></PropertyGroup></Project>', encoding="utf-8")
(fixture / "Program.cs").write_text("""
using System;
using System.Globalization;
namespace Dn2Cpp.Runtime {
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class ObfuscateAttribute : Attribute { }
}
class Counter {
    public int Value;
    [Dn2Cpp.Runtime.Obfuscate]
    public Counter(int count) {
        int state = 1;
        for (int i = 0; i < count; ++i) {
            if (i % 3 == 0) state = (state + i * 31) % 1009;
            else state = (state - i * 7) % 1009;
        }
        Value = state;
    }
}
class Program {
    [Dn2Cpp.Runtime.Obfuscate]
    static int Mix(int value) {
        int result = 0;
        for (int i = 0; i < value; ++i) {
            if (i % 3 == 0) result = (result + i * 31) % 1009;
            else result = (result - i * 7) % 1009;
        }
        return result;
    }
    [Dn2Cpp.Runtime.Obfuscate]
    static T[] Fill<T>(int count, T first, T second) where T : class {
        T[] items = new T[count];
        for (int i = 0; i < count; ++i) {
            if (i % 3 == 0) items[i] = first;
            else items[i] = second;
        }
        return items;
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static int Unselected(int value) {
        int result = 0;
        for (int i = 0; i < value; ++i) {
            if (i % 3 == 0) result = (result + i * 31) % 1009;
            else result = (result - i * 7) % 1009;
        }
        return result;
    }
    static void Main(string[] args) {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine(Mix(args.Length + 43));
        Console.WriteLine(new Counter(args.Length + 29).Value);
        object[] objects = Fill<object>(args.Length + 11, "object-first", "object-second");
        string[] strings = Fill<string>(args.Length + 17, "string-first", "string-second");
        Console.WriteLine(objects.Length);
        Console.WriteLine(objects[0]);
        Console.WriteLine(objects[1]);
        Console.WriteLine(strings.Length);
        Console.WriteLine(strings[0]);
        Console.WriteLine(strings[1]);
        Console.WriteLine(Unselected(args.Length + 43));
    }
}
""", encoding="utf-8")
run("dotnet", "build", fixture, "-c", configuration,
    "-m:1", "-p:BuildInParallel=false", "--disable-build-servers")
assembly = fixture / f"bin/{configuration}/net10.0/Proof.dll"
expected = run("dotnet", assembly)
emitted = work / "emitted"
run("dotnet", cli,
    assembly, "--auto-ref", "--obfuscate", "--no-ildiet", "-o", emitted)
manifest = json.loads((emitted / "obfuscation-targets.json").read_text(encoding="utf-8"))
# The trip counts keep native branches; Fill's array allocation requires rgctx.
targets = manifest["targets"]
assert any("::.ctor(Int32):" in t["managedMethod"] for t in targets)
assert any("::Mix(Int32):" in t["managedMethod"] for t in targets)
shared = [t for t in targets if "::Fill<" in t["managedMethod"]]
assert len(shared) == 2
assert shared[0]["implementationSymbol"] == shared[1]["implementationSymbol"]
shared_symbol = shared[0]["implementationSymbol"]
shared_text = (emitted / shared[0]["cppFile"]).read_text(encoding="utf-8")
shared_definition = next(line for line in shared_text.splitlines()
                         if line.startswith("DN2CPP_NOINLINE ") and shared_symbol + "(" in line)
assert "__rgctx" in shared_definition
for target in targets:
    target["seed"] = hashlib.sha256(("proof\n" + target["implementationSymbol"]).encode()).hexdigest()[:32]
config = work / "targets.json"
content = json.dumps({"version": 1, "targets": targets})
if not config.exists() or config.read_text(encoding="utf-8") != content:
    config.write_text(content, encoding="utf-8")
if android:
    ndk = Path(os.environ["ANDROID_NDK_ROOT"]).resolve()
    toolchain = root / "runtime/cmake/android-declang.toolchain.cmake"
    platform_args = [f"-DCMAKE_TOOLCHAIN_FILE={toolchain}", f"-DDN2CPP_DECLANG_COMPILER={compiler}",
                     f"-DANDROID_NDK={ndk}", "-DANDROID_ABI=arm64-v8a", "-DANDROID_PLATFORM=android-24",
                     "-DANDROID_STL=c++_static"]
    objdump = next(ndk.glob("toolchains/llvm/prebuilt/*/bin/llvm-objdump" + (".exe" if os.name == "nt" else "")))
else:
    sdk = run("xcrun", "--show-sdk-path").strip()
    platform_args = [f"-DCMAKE_CXX_COMPILER={compiler}", f"-DCMAKE_C_COMPILER={compiler}",
                     "-DCMAKE_C_COMPILER_ARG1=--driver-mode=gcc", f"-DCMAKE_OSX_SYSROOT={sdk}"]
for mode in ("off", "on"):
    build = work / mode
    run("cmake", "-S", root / "runtime", "-B", build, "-G", "Ninja",
        *platform_args,
        f"-DDN2CPP_APP_DIR={emitted}", "-DDN2CPP_APP_NAME=Obfuscation", "-DDN2CPP_STRIP=OFF",
        "-DDN2CPP_USE_CURL=OFF", "-DDN2CPP_USE_ZLIB=OFF", "-DDN2CPP_USE_BROTLI=OFF",
        "-DDN2CPP_USE_HIGHWAY=OFF", f"-DDN2CPP_DECLANG_CONFIG={config if mode == 'on' else ''}")
    print(f"Building DeClang native {mode}", flush=True)
    run("cmake", "--build", build, "--parallel", "4")
    if android:
        description = run(objdump, "-f", build / "Obfuscation")
        assert "elf64-littleaarch64" in description, description
        # Reconfigure without compiler overrides to catch the NDK resetting its driver.
        run("cmake", "-S", root / "runtime", "-B", build)
        cmake_version = json.loads(run("cmake", "-E", "capabilities"))["version"]
        actual_version = re.search(r"clang version ([0-9.]+)", run(compiler, "--version")).group(1)
        for language in ("C", "CXX"):
            assert_compiler_identity(build, language, actual_version, cmake_version)
        if mode == "off":
            verify_compiler_identity_selection(build, actual_version, cmake_version)
        commands = run("ninja", "-C", build, "-t", "commands")
        pch = [line for line in commands.splitlines() if "cmake_pch" in line and " -c " in line]
        assert pch and all(compiler.as_posix() in line.replace("\\", "/") for line in pch), "PCH did not use DeClang"
    else:
        assert run(build / "Obfuscation") == expected


def instructions(mode, target):
    if android:
        dump = run(objdump, "-d", "--no-show-raw-insn", work / mode / "Obfuscation")
        (work / f"{mode}-disassembly.txt").write_text(dump, encoding="utf-8")
        prefix = "_Z" + str(len(target["implementationSymbol"])) + target["implementationSymbol"]
        lines = dump.splitlines()
        start = next(i for i, line in enumerate(lines) if re.match(r"[0-9a-f]+ <" + re.escape(prefix), line))
        body = []
        for line in lines[start + 1:]:
            if not line.strip():
                break
            # Relative control-flow destinations change when preceding bodies grow.
            instruction = re.sub(r"^\s*[0-9a-f]+:\s*", "", line)
            instruction = re.sub(r"0x[0-9a-f]+ <([^>+]+)\+0x([0-9a-f]+)>", r"<\1+\2>", instruction)
            body.append(instruction)
        assert body
        return "\n".join(body)
    dump = run("otool", "-tvV", work / mode / "Obfuscation")
    prefix = "__Z" + str(len(target["implementationSymbol"])) + target["implementationSymbol"]
    lines = dump.splitlines()
    start = next(i for i, line in enumerate(lines) if line.startswith(prefix) and line.endswith(":"))
    body = []
    for line in lines[start + 1:]:
        if line.endswith(":"):
            break
        body.append(line.split("\t", 1)[-1])
    assert body
    return "\n".join(body)


implementations = {t["implementationSymbol"]: t for t in targets}
flattened = []
for result in (work / "on" / "declang" / "results").glob("*.json"):
    flattened.extend(json.loads(result.read_text(encoding="utf-8"))["flattenedSymbols"])
assert len(flattened) == len(implementations)
for target in implementations.values():
    assert sum(bool(re.fullmatch(target["symbolPattern"], symbol)) for symbol in flattened) == 1
    assert instructions("on", target) != instructions("off", target), target["managedMethod"]
if android:
    unselected = next(re.search(r"DN2CPP_NOINLINE \S+ (\w*Unselected\w*)\(", path.read_text(encoding="utf-8"))
                      for path in emitted.glob("*.cpp") if re.search(r"DN2CPP_NOINLINE \S+ (\w*Unselected\w*)\(", path.read_text(encoding="utf-8")))
    target = {"implementationSymbol": unselected.group(1)}
    assert instructions("on", target) == instructions("off", target), "unselected machine code changed"
    def machine_bytes(mode):
        dump = run(objdump, "-d", work / mode / "Obfuscation")
        (work / f"{mode}-machine-code.txt").write_text(dump, encoding="utf-8")
        prefix = "_Z" + str(len(target["implementationSymbol"])) + target["implementationSymbol"]
        match = re.search(r"[0-9a-f]+ <" + re.escape(prefix) + r"[^>]*>:\n(.*?)(?:\n\n|\Z)", dump, re.S)
        assert match
        words = re.findall(r"^\s*[0-9a-f]+:\s+([0-9a-f]{8})\s", match.group(1), re.M)
        assert words
        return words
    assert machine_bytes("on") == machine_bytes("off"), "unselected instruction bytes changed"
    for target in implementations.values():
        before, after = instructions("off", target), instructions("on", target)
        assert len(after.splitlines()) > len(before.splitlines()), "flattening did not expand selected control flow"
        if "::Mix(" in target["managedMethod"]:
            # Flattening routes distinct basic blocks back into a common state
            # dispatcher; ordinary source loops have fewer converging branches.
            def convergence(body):
                destinations = re.findall(r"\bb(?:\.[a-z]+)?\s+(<[^>]+>)", body)
                return max(Counter(destinations).values(), default=0)
            assert convergence(after) >= 3 and convergence(after) > convergence(before), "no common flattened dispatcher in final ELF"
            common = Counter(re.findall(r"\bb(?:\.[a-z]+)?\s+(<[^>]+>)", after)).most_common(1)[0][0]
            offset = int(re.search(r"\+([0-9a-f]+)>$", common).group(1), 16)
            dispatcher = after.splitlines()[offset // 4]
            state = re.match(r"ldr\s+(w\d+), (\[sp, #0x[0-9a-f]+\])", dispatcher)
            assert state, "common branch destination does not reload the dispatcher state"
            register, slot = state.groups()
            # A paired store also updates the state when its first word uses
            # the dispatcher's stack slot.
            writes = re.findall(r"\b(?:str\s+w\d+, |stp\s+w\d+, (?:w\d+|wzr), )" + re.escape(slot), after)
            comparisons = re.findall(r"\bcmp\s+" + register + r", w\d+", after)
            assert len(writes) >= 2 and len(comparisons) >= 3, "dispatcher lacks state transitions or multi-way comparisons"

    print("DeClang Android proof passed: same emitted C++, selected ELF control flow flattened, unselected body unchanged; device execution not performed")
else:
    print("DeClang macOS native proof passed: same emitted C++, .NET parity, selected machine code differs")
