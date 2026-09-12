"""Exercise DeClang gate discovery without requiring a compiler installation."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile

root = Path(sys.argv[1]).resolve()
suffix = ".exe" if os.name == "nt" else ""
env = os.environ.copy()
env.pop("DN2CPP_DECLANG_COMPILER", None)
env["DN2CPP_OS"] = "windows" if os.name == "nt" else "macos"

with tempfile.TemporaryDirectory(prefix="declang discovery ") as temporary:
    work = Path(temporary).resolve()

    def executable(path):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("#!/usr/bin/env bash\nexit 0\n", encoding="utf-8")
        path.chmod(0o755)
        return path

    def resolve(expected_code, android=False, override=None):
        child_env = env.copy()
        if override is not None:
            child_env["DN2CPP_DECLANG_COMPILER"] = str(override)
        function = "ensure_declang_android_compiler" if android else "ensure_declang_compiler"
        # Exercise the Windows selection branch on POSIX without MSYS path conversion.
        path_conversion = 'cygpath() { printf "%s" "$2"; }; ' if os.name != "nt" else ""
        result = subprocess.run(
            ["bash", "-c", 'set -euo pipefail; source "$1"; '
             'gate_skip() { echo "$*" >&2; exit 77; }; ' + path_conversion + function + '; '
             'bash -c \'printf "%s" "$DN2CPP_DECLANG_COMPILER"\'',
             "discovery", str(root / "gates/_declang.sh")],
            cwd=work, env=child_env, text=True, capture_output=True)
        assert result.returncode == expected_code, result.stdout + result.stderr
        return result.stdout + result.stderr

    def selected(path, android=False, override=None):
        assert Path(resolve(0, android, override)).resolve() == path.resolve()

    assert "set DN2CPP_DECLANG_COMPILER" in resolve(77)
    assert "set DN2CPP_DECLANG_COMPILER" in resolve(77, android=True)
    desktop = executable(work / f"artifacts/declang-distribution/desktop/compiler/bin/clang++{suffix}")
    selected(desktop)
    selected(desktop, android=True)
    android = executable(work / f"artifacts/declang-android-current/build/bin/clang++{suffix}")
    selected(android if os.name != "nt" else desktop, android=True)
    selected(desktop)
    selected(desktop, android=True, override=desktop)
    assert "not an executable file" in resolve(1, android=True, override=work / "missing")
    assert "not an executable file" in resolve(1, override=work)
    second_desktop = executable(work / f"artifacts/declang-distribution/other/compiler/bin/clang++{suffix}")
    assert "multiple DeClang distributions" in resolve(1)
    if os.name != "nt":
        selected(android, android=True)
    else:
        assert "multiple DeClang distributions" in resolve(1, android=True)
    second_android = executable(work / f"artifacts/declang-android-other/build/bin/clang++{suffix}")
    expected = "multiple Android DeClang builds" if os.name != "nt" else "multiple DeClang distributions"
    assert expected in resolve(1, android=True)
    selected(android, android=True, override=android)
    selected(desktop, override=desktop)
    second_android.unlink()
    second_desktop.unlink()
    desktop.unlink()
    if os.name != "nt":
        selected(android, android=True)
    else:
        assert "set DN2CPP_DECLANG_COMPILER" in resolve(77, android=True)
    assert "set DN2CPP_DECLANG_COMPILER" in resolve(77)
    if os.name != "nt":
        android.chmod(0o644)
        assert "set DN2CPP_DECLANG_COMPILER" in resolve(77, android=True)
        assert "not an executable file" in resolve(1, android=True, override=android)

    env["DN2CPP_OS"] = "windows"
    desktop_exe = executable(work / "artifacts/declang-distribution/windows/compiler/bin/clang++.exe")
    android_exe = executable(work / "artifacts/declang-android-windows/build/bin/clang++.exe")
    selected(desktop_exe, android=True)
    selected(android_exe, android=True, override=android_exe)
    assert "not an executable file" in resolve(1, android=True, override=work / "missing.exe")
    desktop_exe.unlink()
    assert "set DN2CPP_DECLANG_COMPILER" in resolve(77, android=True)

print("DeClang desktop and Android compiler discovery checks passed", flush=True)
