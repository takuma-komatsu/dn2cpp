"""Compare OFF/ON native bodies from the same dn2cpp emission and compiler."""
import hashlib
import json
import re
import os
from pathlib import Path
import subprocess
import sys

root = Path(sys.argv[1]).resolve()
compiler = Path(sys.argv[2]).resolve()
work = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else root / "artifacts" / "declang-native-proof"
work.mkdir(parents=True, exist_ok=True)
env = os.environ.copy()
env["DECLANG_HOME"] = str(work / "disabled")
(work / "disabled" / ".DeClang").mkdir(parents=True, exist_ok=True)
(work / "disabled" / ".DeClang" / "config.json").write_text('{"enable_obfuscation":0}')


def run(*args):
    result = subprocess.run([str(a) for a in args], env=env, text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if result.returncode:
        raise AssertionError(f"Command failed: {args}\n{result.stdout}")
    return result.stdout


configuration = os.environ.get("CONFIG", "Release")
cli = Path(os.environ.get("DN2CPP_CLI_DLL", root / f"src/Dn2Cpp.Cli/bin/{configuration}/net10.0/dn2cpp.dll"))
if not os.environ.get("DN2CPP_SKIP_BUILD"):
    run("dotnet", "build", root / "src/Dn2Cpp.Cli", "-c", configuration,
        "-m:1", "-p:BuildInParallel=false", "--disable-build-servers")
fixture = work / "fixture"
fixture.mkdir(exist_ok=True)
(fixture / "Proof.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Optimize>true</Optimize></PropertyGroup></Project>')
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
    }
}
""")
run("dotnet", "build", fixture, "-c", configuration,
    "-m:1", "-p:BuildInParallel=false", "--disable-build-servers")
assembly = fixture / f"bin/{configuration}/net10.0/Proof.dll"
expected = run("dotnet", assembly)
emitted = work / "emitted"
run("dotnet", cli,
    assembly, "--auto-ref", "--obfuscate", "--no-ildiet", "-o", emitted)
manifest = json.loads((emitted / "obfuscation-targets.json").read_text())
# The trip counts keep native branches; Fill's array allocation requires rgctx.
targets = manifest["targets"]
assert any("::.ctor(Int32):" in t["managedMethod"] for t in targets)
assert any("::Mix(Int32):" in t["managedMethod"] for t in targets)
shared = [t for t in targets if "::Fill<" in t["managedMethod"]]
assert len(shared) == 2
assert shared[0]["implementationSymbol"] == shared[1]["implementationSymbol"]
shared_symbol = shared[0]["implementationSymbol"]
shared_text = (emitted / shared[0]["cppFile"]).read_text()
shared_definition = next(line for line in shared_text.splitlines()
                         if line.startswith("DN2CPP_NOINLINE ") and shared_symbol + "(" in line)
assert "__rgctx" in shared_definition
for target in targets:
    target["seed"] = hashlib.sha256(("proof\n" + target["implementationSymbol"]).encode()).hexdigest()[:32]
config = work / "targets.json"
content = json.dumps({"version": 1, "targets": targets})
if not config.exists() or config.read_text() != content:
    config.write_text(content)
sdk = run("xcrun", "--show-sdk-path").strip()
for mode in ("off", "on"):
    build = work / mode
    run("cmake", "-S", root / "runtime", "-B", build, "-G", "Ninja",
        f"-DCMAKE_CXX_COMPILER={compiler}", f"-DCMAKE_C_COMPILER={compiler}",
        "-DCMAKE_C_COMPILER_ARG1=--driver-mode=gcc", f"-DCMAKE_OSX_SYSROOT={sdk}",
        f"-DDN2CPP_APP_DIR={emitted}", "-DDN2CPP_APP_NAME=Obfuscation", "-DDN2CPP_STRIP=OFF",
        "-DDN2CPP_USE_CURL=OFF", "-DDN2CPP_USE_ZLIB=OFF", "-DDN2CPP_USE_BROTLI=OFF",
        "-DDN2CPP_USE_HIGHWAY=OFF", f"-DDN2CPP_DECLANG_CONFIG={config if mode == 'on' else ''}")
    print(f"Building DeClang native {mode}", flush=True)
    run("cmake", "--build", build, "--parallel", "4")
    assert run(build / "Obfuscation") == expected


def instructions(mode, target):
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
    flattened.extend(json.loads(result.read_text())["flattenedSymbols"])
assert len(flattened) == len(implementations)
for target in implementations.values():
    assert sum(bool(re.fullmatch(target["symbolPattern"], symbol)) for symbol in flattened) == 1
    assert instructions("on", target) != instructions("off", target), target["managedMethod"]
print("DeClang native proof passed: same emitted C++, .NET parity, selected machine code differs")
