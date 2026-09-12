"""Reject unusable Android DeClang configurations before compiling a game."""
import os
from pathlib import Path
import shutil
import subprocess
import sys

root, compiler = map(lambda p: Path(p).resolve(), sys.argv[1:])
ndk = Path(os.environ["ANDROID_NDK_ROOT"]).resolve()
work = root / "artifacts/declang-android-checks"
work.mkdir(parents=True, exist_ok=True)
disabled_home = work / "disabled"
(disabled_home / ".DeClang").mkdir(parents=True, exist_ok=True)
(disabled_home / ".DeClang/config.json").write_text('{"enable_obfuscation":0}')
os.environ["DECLANG_HOME"] = str(disabled_home)
probe = root / "runtime/cmake/declang_probe.cmake"


def check(label, expected=None, **overrides):
    settings = dict(COMPILER=compiler, WORK_DIR=work / label, NINJA_EXE=shutil.which("ninja"),
                    ANDROID_NDK=ndk, ANDROID_ABI="arm64-v8a", ANDROID_PLATFORM="android-24",
                    ANDROID_STL="c++_static")
    settings.update(overrides)
    result = subprocess.run(["cmake", *(f"-D{k}={v}" for k, v in settings.items()), "-P", str(probe)],
                            text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (work / f"{label}.log").write_text(result.stdout)
    if expected is None:
        assert result.returncode == 0, result.stdout
    else:
        assert result.returncode != 0 and expected.lower() in result.stdout.lower(), result.stdout


check("compatible")
check("missing-compiler", "compiler", COMPILER=work / "missing-clang++")
check("unsupported-abi", "arm64", ANDROID_ABI="x86_64")
check("missing-ndk", "NDK", ANDROID_NDK=work / "missing-ndk")
check("ordinary-clang", "application log", COMPILER=next(ndk.glob("toolchains/llvm/prebuilt/*/bin/clang++")))
print("Android DeClang configuration checks passed")

# Keep mutable identity inputs in a private overlay; the installed NDK and
# compiler distribution remain untouched.
identity_work = work / "identity"
identity_work.mkdir(exist_ok=True)
overlay = identity_work / "ndk"
overlay.mkdir(exist_ok=True)
for entry in ndk.iterdir():
    destination = overlay / entry.name
    if entry.name == "source.properties":
        destination.write_bytes(entry.read_bytes())
    elif not destination.exists():
        destination.symlink_to(entry, target_is_directory=entry.is_dir())
cmake_copy = identity_work / "cmake"
shutil.copytree(root / "runtime/cmake", cmake_copy, dirs_exist_ok=True)
resource = Path(subprocess.check_output([str(compiler), "-print-resource-dir"], text=True).strip())
resource_overlay = identity_work / "resources"
resource_overlay.mkdir(exist_ok=True)
for entry in resource.iterdir():
    destination = resource_overlay / entry.name
    if not destination.exists():
        destination.symlink_to(entry, target_is_directory=entry.is_dir())
wrapper = identity_work / "clang++"
# The actual compiler still uses its own resource directory for compilation;
# the equivalent overlay lets the fingerprint probe observe a safe mutation.
wrapper.write_text("#!/usr/bin/env python3\nimport os,sys\n"
                   f"if sys.argv[1:] == ['-print-resource-dir']: print({str(resource_overlay)!r})\n"
                   f"else: os.execv({str(compiler)!r}, [{str(compiler)!r}, *sys.argv[1:]])\n")
wrapper.chmod(0o755)
probe = cmake_copy / "declang_probe.cmake"
identity_dir = work / "identity-probe"


def identity_check(cached):
    check("identity-probe", COMPILER=wrapper, ANDROID_NDK=overlay)
    log = (work / "identity-probe.log").read_text()
    assert ("DeClang compatibility probe cached" in log) == cached, log
    return (identity_dir / "identity.txt").read_text()


(identity_dir / "identity.txt").unlink(missing_ok=True)
previous = identity_check(False)
assert identity_check(True) == previous
for path in (resource_overlay / "dn2cpp-identity-marker", cmake_copy / "declang_android_link.cmake",
             overlay / "source.properties"):
    with path.open("a") as stream:
        stream.write("\n# dn2cpp identity mutation\n")
    current = identity_check(False)
    assert current != previous, path
    assert identity_check(True) == current
    previous = current

old = os.environ.get("DN2CPP_DECLANG_INCOMPATIBLE_COMPILER")
if not old:
    candidate = root / "artifacts/declang-distribution/Release-swift5.10-v1.0.0/compiler/bin/clang++"
    if candidate.is_file() and candidate.resolve() != compiler:
        old = str(candidate)
if old:
    probe = root / "runtime/cmake/declang_probe.cmake"
    check("incompatible-compiler-ndk", "failed", COMPILER=old)
print("Android DeClang identity invalidation checks passed")
