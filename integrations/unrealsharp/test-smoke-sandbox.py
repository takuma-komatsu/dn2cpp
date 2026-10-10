#!/usr/bin/env python3
"""Exercise signed-entitlement output selection without changing real app containers."""
import contextlib
import io
from pathlib import Path
import plistlib
import subprocess
import sys
import tempfile
from unittest.mock import patch

source = (Path(__file__).resolve().parents[2] / 'gates/run-unrealsharp-package-smoke.sh').read_text()
code = source.split("<<'PYCODE'\n", 1)[1].split('\nPYCODE', 1)[0]
with tempfile.TemporaryDirectory() as temporary:
    root = Path(temporary)
    app = root / 'Game.app'
    (app / 'Contents').mkdir(parents=True)
    (app / 'Contents/Info.plist').write_bytes(plistlib.dumps({'CFBundleIdentifier': 'org.example.Smoke'}))
    evidence = root / 'evidence'
    evidence.mkdir()
    for sandboxed in (False, True):
        output = io.StringIO()
        entitlements = plistlib.dumps({'com.apple.security.app-sandbox': sandboxed})
        with patch.object(sys, 'argv', ['probe', str(app), str(evidence)]), patch.object(subprocess, 'check_output', return_value=entitlements), patch.object(Path, 'home', return_value=root / 'home'), contextlib.redirect_stdout(output):
            exec(compile(code, 'package-smoke-entitlements', 'exec'), {})
        directory = Path(output.getvalue().strip())
        if sandboxed:
            assert directory.is_dir()
            assert directory.parent == root / 'home/Library/Containers/org.example.Smoke/Data/tmp'
            copy_body = source.split('copy_runtime_evidence() {\n', 1)[1].split('\n}\ngate_add_exit_hook', 1)[0]
            for name in ('result.txt', 'lifecycle.txt', 'run.log', 'pending.txt', 'late-callback.txt'):
                (directory / name).write_text(name)
            script = 'copy_runtime_evidence() {\n' + copy_body + '\n}\ntrap copy_runtime_evidence EXIT\nexit 19\n'
            result = subprocess.run(['bash', '-c', script], env={
                'PATH': '/usr/bin:/bin', 'runtime_work': str(directory), 'work': str(evidence),
                'pending_evidence': str(evidence / 'pending.txt'), 'late_evidence': str(evidence / 'late-callback.txt')})
            assert result.returncode == 19
            for name in ('result.txt', 'lifecycle.txt', 'run.log', 'pending.txt', 'late-callback.txt'):
                assert (evidence / name).read_text() == name
        else:
            assert directory == evidence
print('UNREALSHARP_SANDBOX_EVIDENCE_OK')
