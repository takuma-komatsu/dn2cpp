"""Exercise Android compiler routing and result dependencies with the host NDK."""
import json
import os
from pathlib import Path
import subprocess
import sys

root = Path(sys.argv[1]).resolve()
ndk = Path(os.environ["ANDROID_NDK_ROOT"]).resolve()
host = "windows-x86_64" if os.name == "nt" else "darwin-x86_64"
suffix = ".exe" if os.name == "nt" else ""
driver = ndk / f"toolchains/llvm/prebuilt/{host}/bin/clang++{suffix}"
work = root / "artifacts/declang android host checks"
source = work / "src"
build = work / "build"
source.mkdir(parents=True, exist_ok=True)
config = work / "targets.json"
config.write_text('{"version":1,"targets":[]}', encoding="utf-8")
cpp = source / "generated.cpp"
cpp.write_text('int main() { return 0; }\n', encoding="utf-8")
helper = root / "runtime/cmake"
(source / "CMakeLists.txt").write_text(f'''cmake_minimum_required(VERSION 3.21)
project(AndroidHostRouting C CXX)
set(CMAKE_CXX_COMPILER_LAUNCHER "${{CMAKE_COMMAND}};-DCONFIG={config.as_posix()};-DAPP_DIR={source.as_posix()};-DWORK_DIR={build.as_posix()}/declang;-P;{helper.as_posix()}/declang_launcher.cmake;--")
add_executable(probe generated.cpp)
set_property(SOURCE generated.cpp APPEND PROPERTY OBJECT_DEPENDS "{config.as_posix()}")
''', encoding="utf-8")


def run(*args, expected=None):
    result = subprocess.run([str(arg) for arg in args], text=True,
                            stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if expected is None:
        assert result.returncode == 0, result.stdout
    else:
        assert result.returncode != 0 and expected in result.stdout, result.stdout
    return result.stdout


run("cmake", "-S", source, "-B", build, "-G", "Ninja",
    f"-DCMAKE_TOOLCHAIN_FILE={helper.as_posix()}/android-declang.toolchain.cmake",
    f"-DDN2CPP_DECLANG_COMPILER={driver.as_posix()}", f"-DANDROID_NDK={ndk.as_posix()}")
run("cmake", "--build", build)
result = build / "declang/results/generated.cpp.json"
assert json.loads(result.read_text(encoding="utf-8"))["flattenedSymbols"] == []
assert result.stat().st_mtime_ns == cpp.stat().st_mtime_ns
readelf = driver.parent / ("llvm-readelf" + suffix)
assert "AArch64" in run(readelf, "-h", build / "probe")
obj = next(build.rglob("generated.cpp.o"))
unchanged = obj.stat().st_mtime_ns
run("cmake", "--build", build)
assert obj.stat().st_mtime_ns == unchanged, "unchanged result caused a rebuild"
result.unlink()
run("cmake", "--build", build)
assert result.exists() and obj.stat().st_mtime_ns != unchanged
config.write_text(json.dumps({"version": 1, "targets": [{"cppFile": "generated.cpp",
    "symbolPattern": "^main$", "seed": "0123456789abcdef0123456789abcdef"}]}), encoding="utf-8")
run("cmake", "--build", build, expected="DeClang produced no application log")
assert not result.exists(), "failed application retained stale success evidence"
run("cmake", f"-DCOMPILER={driver}", f"-DANDROID_NDK={ndk}",
    f"-DWORK_DIR={work / 'compatibility'}", "-P", helper / "declang_probe.cmake",
    expected="DeClang produced no application log")
print("Android host routing, result timestamp, incremental rebuild and ordinary Clang rejection passed")
