"""Resolve the Bash executable used by gate subprocesses."""
import os
from pathlib import Path
import shutil


def resolve_bash():
    if os.name == "nt":
        # Git for Windows keeps these siblings; cygpath has no WSL alias that
        # can redirect a child away from the Git Bash running the parent gate.
        cygpath = shutil.which("cygpath")
        if cygpath is not None:
            git_bash = Path(cygpath).with_name("bash.exe")
            if git_bash.is_file():
                return str(git_bash)

    bash = shutil.which("bash")
    if bash is None:
        raise RuntimeError("Bash is required")
    return bash
