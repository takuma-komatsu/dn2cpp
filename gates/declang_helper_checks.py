"""Exercise the shipped CMake helpers against an existing native DeClang driver."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

root = Path(sys.argv[1]).resolve()
compiler = Path(sys.argv[2]).resolve()
work = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else root / "artifacts" / "declang helper checks"
shutil.rmtree(work, ignore_errors=True)
work.mkdir(parents=True)
helpers = root / "runtime" / "cmake"
env = os.environ.copy()
env["DECLANG_HOME"] = str(work / "disabled")
(work / "disabled" / ".DeClang").mkdir(parents=True)
(work / "disabled" / ".DeClang" / "config.json").write_text('{"enable_obfuscation":0}')


def run(*args, success=True, **kwargs):
    result = subprocess.run([str(a) for a in args], env=env, text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, **kwargs)
    if (result.returncode == 0) != success:
        raise AssertionError(f"Unexpected exit {result.returncode}: {args}\n{result.stdout}")
    return result.stdout


# A staged driver must reach child exports without hiding invalid overrides.
discovery = work / "compiler discovery"
discovery.mkdir()
discovery_env = env.copy()
discovery_env.pop("DN2CPP_DECLANG_COMPILER", None)


def resolve_driver(expected_code, override=None):
    child_env = discovery_env.copy()
    if override is not None:
        child_env["DN2CPP_DECLANG_COMPILER"] = str(override)
    result = subprocess.run(
        ["bash", "-c", 'set -euo pipefail; source "$1"; '
         'gate_skip() { echo "$*" >&2; exit 77; }; ensure_declang_compiler; '
         'bash -c \'printf "%s" "$DN2CPP_DECLANG_COMPILER"\'',
         "discovery", str(root / "gates" / "_declang.sh")],
        cwd=discovery, env=child_env, text=True, capture_output=True)
    assert result.returncode == expected_code, result.stderr
    return result.stdout + result.stderr


assert "set DN2CPP_DECLANG_COMPILER" in resolve_driver(77)
staged_driver = discovery / "artifacts/declang-distribution/first/compiler/bin/clang++"
staged_driver.parent.mkdir(parents=True)
staged_driver.symlink_to(compiler)
assert resolve_driver(0) == str(staged_driver)
assert resolve_driver(0, compiler) == str(compiler)
assert "not an executable file" in resolve_driver(1, discovery / "missing")
nonexecutable = discovery / "nonexecutable"
nonexecutable.write_text("compiler")
assert "not an executable file" in resolve_driver(1, nonexecutable)
assert "not an executable file" in resolve_driver(1, discovery)
second_driver = discovery / "artifacts/declang-distribution/second/compiler/bin/clang++"
second_driver.parent.mkdir(parents=True)
second_driver.symlink_to(compiler)
assert "multiple DeClang distributions" in resolve_driver(1)
assert resolve_driver(0, compiler) == str(compiler)
print("DeClang compiler discovery checks passed", flush=True)

probe = work / "probe"
probe_args = ["cmake", f"-DCOMPILER={compiler}", f"-DWORK_DIR={probe}",
              "-P", helpers / "declang_probe.cmake"]
run(*probe_args)
assert "cached" in run(*probe_args)

# A stable path is insufficient identity for a locally updated compiler.
resources = work / "compiler resources"
shutil.copytree(Path(run(compiler, "-print-resource-dir").strip()), resources)
identity_compiler = work / "identity compiler"
identity_compiler.write_text(
    "#!/usr/bin/env python3\nimport os,sys\n" +
    f"compiler={str(compiler)!r}\nresources={str(resources)!r}\n" +
    "os.execv(compiler, [compiler, '-resource-dir', resources, *sys.argv[1:]])\n")
identity_compiler.chmod(0o755)
identity_probe = work / "identity probe"
identity_args = ["cmake", f"-DCOMPILER={identity_compiler}",
                 f"-DWORK_DIR={identity_probe}", "-P", helpers / "declang_probe.cmake"]
assert "probe passed" in run(*identity_args)
identity_file = identity_probe / "identity.txt"
original_identity = identity_file.read_text()
assert "cached" in run(*identity_args)
with identity_compiler.open("a") as updated:
    updated.write("# Same executable path, changed compiler content.\n")
updated_output = run(*identity_args)
assert "probe passed" in updated_output and "cached" not in updated_output
updated_identity = identity_file.read_text()
assert updated_identity != original_identity
assert "cached" in run(*identity_args)
# Change a real resource header without changing its path or compiler bytes.
resource_header = resources / "include" / "stddef.h"
with resource_header.open("a") as updated:
    updated.write("\n/* Resource content participates in compatibility identity. */\n")
resource_output = run(*identity_args)
assert "probe passed" in resource_output and "cached" not in resource_output
assert identity_file.read_text() != updated_identity
assert "cached" in run(*identity_args)
assert "DeClang" in run("cmake", "-DCOMPILER=/usr/bin/clang++",
                       f"-DWORK_DIR={work / 'ordinary'}", "-P", helpers / "declang_probe.cmake", success=False)
run("cmake", "-DCOMPILER=/missing/declang", f"-DWORK_DIR={work / 'missing'}",
    "-P", helpers / "declang_probe.cmake", success=False)
source = work / "source with spaces"
source.mkdir()
shutil.copy(probe / "probe" / "src" / "generated.cpp", source)
shutil.copy(probe / "probe" / "src" / "smoke.c", source)
(source / "generated_other.cpp").write_text('int other(int a) { return a + 1; }\n')
(source / "generated.h").write_text('#include <string>\n')
config = work / "targets.json"
targets = json.loads((probe / "probe" / "targets.json").read_text())
config.write_text(json.dumps(targets))
(source / "CMakeLists.txt").write_text(f'''cmake_minimum_required(VERSION 3.21)
project(DeClangHelpers C CXX)
set(CMAKE_CXX_STANDARD 17)
set(DN2CPP_APP_DIR "{source}")
include("{helpers / 'dn2cpp_declang.cmake'}")
set(DN2CPP_APP_SRCS "{source}/generated.cpp" "{source}/generated_other.cpp")
if(DN2CPP_DECLANG_CONFIG)
    dn2cpp_declang_sources()
endif()
add_executable(app ${{DN2CPP_APP_SRCS}} "{source}/smoke.c")
target_compile_options(app PRIVATE -O2)
target_precompile_headers(app PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:{source}/generated.h>")
''')
sdk = run("xcrun", "--show-sdk-path").strip()
build = work / "build"
configure = ["cmake", "-S", source, "-B", build, "-G", "Ninja",
             f"-DCMAKE_CXX_COMPILER={compiler}", f"-DCMAKE_C_COMPILER={compiler}",
             "-DCMAKE_C_COMPILER_ARG1=--driver-mode=gcc", f"-DCMAKE_OSX_SYSROOT={sdk}",
             f"-DDN2CPP_DECLANG_CONFIG={config}"]
run(*configure)
run("cmake", "--build", build, "--parallel", "4")
run(build / "app")
result = build / "declang" / "results" / "generated.cpp.json"
assert json.loads(result.read_text())["flattenedSymbols"] == ["_Z8selectedi"]
assert json.loads((result.parent / "generated_other.cpp.json").read_text())["flattenedSymbols"] == []
assert "no work to do" in run("cmake", "--build", build)
result.unlink()
assert "Building CXX object" in run("cmake", "--build", build)
assert result.exists()
obj = build / "CMakeFiles" / "app.dir" / "generated.cpp.o"
original_obj = obj.read_bytes()
targets["targets"][0]["seed"] = "fedcba9876543210fedcba9876543210"
config.write_text(json.dumps(targets))
assert "Building CXX object" in run("cmake", "--build", build)
assert original_obj != obj.read_bytes()


def selected_instructions(path):
    text = run("otool", "-tvV", path)
    selected = text.split("__Z8selectedi:\n", 1)[1].split("\n__Z", 1)[0]
    return "\n".join(line.split("\t", 1)[-1] for line in selected.splitlines())


on_instructions = selected_instructions(obj)
off = work / "off"
run("cmake", "-S", source, "-B", off, "-G", "Ninja",
    f"-DCMAKE_CXX_COMPILER={compiler}", f"-DCMAKE_C_COMPILER={compiler}",
             "-DCMAKE_C_COMPILER_ARG1=--driver-mode=gcc", f"-DCMAKE_OSX_SYSROOT={sdk}")
run("cmake", "--build", off)
run(off / "app")
assert on_instructions != selected_instructions(off / "CMakeFiles" / "app.dir" / "generated.cpp.o")
unity_failure = run(*configure, "-DCMAKE_UNITY_BUILD=ON", success=False)
assert "DeClang does not support Unity builds" in unity_failure
run(*configure, "-DCMAKE_UNITY_BUILD=OFF")
run(*configure, "-DCMAKE_CXX_FLAGS=-flto", success=False)
run(*configure, "-DCMAKE_CXX_FLAGS=", "-DCMAKE_CXX_COMPILER_LAUNCHER=ccache", success=False)
run(*configure, "-DCMAKE_CXX_COMPILER_LAUNCHER=", "-DDN2CPP_RUNTIME_EXPORT=prebuilt", success=False)
run(*configure, "-DDN2CPP_RUNTIME_EXPORT=")
# A successful compiler exit cannot substitute for an intact success record.
wrapper = work / "compiler wrapper"
wrapper.write_text("#!/usr/bin/env python3\n" +
                   "import os,pathlib,subprocess,sys\n" +
                   f"result=subprocess.run([{str(compiler)!r},*sys.argv[1:]])\n" +
                   "log=pathlib.Path(os.environ['DECLANG_HOME'])/'.DeClang/log.txt'\n" +
                   "mode=os.environ['DECLANG_TEST_LOG_MODE']\n" +
                   "if mode == 'missing': log.unlink(missing_ok=True)\n" +
                   "elif mode == 'unknown': log.write_text(log.read_text()+'unknown record\\n')\n" +
                   "elif mode == 'unselected': log.write_text(log.read_text()+'[Frontend]: Successfully flattened func _Z9unrelatedi\\n')\n" +
                   "sys.exit(result.returncode)\n")
wrapper.chmod(0o755)
for mode in ("missing", "unknown", "unselected"):
    env["DECLANG_TEST_LOG_MODE"] = mode
    run("cmake", f"-DCONFIG={config}", f"-DAPP_DIR={source}",
        f"-DWORK_DIR={build / 'declang'}", "-P", helpers / "declang_launcher.cmake", "--",
        wrapper, "-std=c++17", "-O2", "-isysroot", sdk, "-MD", "-MF", work / "failure.d",
        "-c", source / "generated.cpp", "-o", work / "failure.o", success=False)
    assert not result.exists()
del env["DECLANG_TEST_LOG_MODE"]
# A selected body optimized to a single block must fail closed.
(source / "generated.cpp").write_text('__attribute__((noinline)) int selected(int a) { return a; }\nint main() { return selected(0); }\n')
assert "DeClang" in run("cmake", "--build", build, success=False)
assert not result.exists()
print("DeClang helper checks passed: real flatten, native difference, exclusions, PCH, cache and failures")
