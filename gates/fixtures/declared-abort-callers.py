#!/usr/bin/env python3
"""Exercise declared abort call sites without launching a compiler or a probe."""
from pathlib import Path
import re
from bash_fixture import run_bash
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


def source(name):
    return (ROOT / 'gates' / name).read_text(encoding='utf-8')


def statement(text, prefix):
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.lstrip().startswith(prefix):
            result = [line]
            while result[-1].rstrip().endswith('\\'):
                index += 1
                result.append(lines[index])
            return '\n'.join(result)
    raise AssertionError(f'missing actual declaration: {prefix}')


COMMON_TEXT = re.search(r'^gate_assert_text\(\) \{\n.*?^\}',
                        source('_common.sh'), re.M | re.S).group(0)
UNHANDLED = source('build-and-run-unhandled-exit-subset.sh')
# Include the actual host policy setup, not a fixture copy of its branches.
WORKERS = UNHANDLED[UNHANDLED.index('first_line_policy='):]
FATAL = re.search(r'^run_fatal_arm\(\) \{\n.*?^\}',
                  source('build-and-run-string-runtime-guards.sh'), re.M | re.S).group(0)
MODES = ('pool', 'thread', 'timer', 'cts timer', 'async void', 'local continuation',
         'nested drain', 'generic exception', 'message override')


def diagnostic(mode):
    if mode == 'generic exception':
        return 'Unhandled exception. Program+GenericFailure`1[System.Int32]: escaped generic exception'
    if mode == 'message override':
        return 'Unhandled exception. Program+OverriddenMessage: overridden message'
    if mode == 'cts timer':
        return 'Unhandled exception. System.AggregateException: One or more errors occurred. (escaped cts timer)'
    return f'Unhandled exception. System.InvalidOperationException: escaped {mode}'


class DeclaredAbortCallers(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)

    def capture(self, prefix, stdout, stderr, code=127):
        prefix = self.directory / prefix
        prefix.parent.mkdir(parents=True, exist_ok=True)
        for suffix, data in (('.stdout', stdout), ('.stderr', stderr),
                             ('.status', f'{code}\n'.encode('ascii'))):
            Path(str(prefix) + suffix).write_bytes(data)

    def run_shell(self, body, host='windows'):
        script = '''set -euo pipefail
python="$1"; root="$2"; out="$3"; DN2CPP_OS="$4"
OUT="$out"; log_dir="$out"; project=Unused; _CG_APP=unused; EXE_EXT=
gate_output() { "$python" "$root/gates/gate-output.py" "$@"; }
gate_capture() { :; }
assert_log_contains() { grep -Fq "$2" "$1"; }
''' + COMMON_TEXT + '\n' + body + '\n'
        return run_bash(script, [ 'declared-abort-fixture',
                               Path(sys.executable).as_posix(), ROOT.as_posix(),
                               self.directory.as_posix(), host])

    def test_paired_wrapper_declarations(self):
        for name, expected in (
            ('build-and-run-unhandled-exit-subset.sh',
             ['UnhandledExitSubset', 'before throw',
              'Unhandled exception. System.InvalidOperationException: escaped Main']),
            ('build-and-run-finalizers.sh',
             ['Finalizers', 'about to throw from finalizer',
              'dn2cpp fatal: unhandled exception in finalizer: System.InvalidOperationException',
              '--oracle-diagnostic', 'Unhandled exception. System.InvalidOperationException: boom',
              '--link-xml', 'samples/dotnet/Finalizers/link.xml'])):
            body = "corelib_diff_abort_gate() { printf '%s\\0' \"$@\"; }\n"
            result = self.run_shell(body + statement(source(name), 'corelib_diff_abort_gate '))
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.split(b'\0')[:-1], [value.encode() for value in expected])

    def test_native_trace_and_shadow_declarations(self):
        declarations = (
            (UNHANDLED, 'trace', b'before throw\r\n',
             b'Unhandled exception. System.InvalidOperationException: escaped Main'),
            (source('build-and-run-shadow-stack.sh'), 'unhandled', b'',
             b'Unhandled exception. System.InvalidOperationException: thrown in Depth5'))
        for text, prefix, stdout, first in declarations:
            body = statement(text, 'gate_output native-abort ')
            for code, host, ok in ((127, 'windows', True), (134, 'linux', True),
                                   (127, 'linux', False), (124, 'windows', False),
                                   (137, 'windows', False)):
                self.capture(prefix, stdout, first + b'\r\n   at frame\r\n', code)
                result = self.run_shell(body, host)
                self.assertEqual(result.returncode == 0, ok, result.stderr)
            self.capture(prefix, stdout, b'command not found\n')
            self.assertNotEqual(self.run_shell(body).returncode, 0)
            self.capture(prefix, stdout + b'unexpected\n', first + b'\n')
            if prefix == 'unhandled':
                self.assertNotEqual(self.run_shell(body).returncode, 0)

    def test_string_fatal_actual_adapter(self):
        for mode, reason in (('bad-type', 'managed string has a non-string type'),
                             ('bad-length', 'managed string has a negative length'),
                             ('bad-chars', 'managed string has no character buffer'),
                             ('bad-allocation', 'managed string allocator received a negative length')):
            first = f'dn2cpp fatal: {reason}'.encode()
            body = FATAL + f'\nrun_fatal_arm {mode} "{reason}"'
            self.capture(mode, b'', first + b'\r\n')
            self.assertEqual(self.run_shell(body).returncode, 0)
            for stderr in (b'', b'command not found\n', first + b'\r\r\n'):
                self.capture(mode, b'', stderr)
                self.assertNotEqual(self.run_shell(body).returncode, 0)

    def seed_workers(self, native_eol=b'\r\n', oracle_eol=b'\r\n', code=127):
        for mode in MODES:
            stdout = f'before throw\r\n-- escaping worker: {mode} --\r\nthrowing {mode}\r\n'.encode()
            for side, eol in (('native', native_eol), ('oracle', oracle_eol)):
                stderr = diagnostic(mode).encode() + eol + b'   at Program.Worker()' + eol
                self.capture(f'worker-{mode}/{side}', stdout, stderr, code)

    def test_worker_first_record_policy_is_host_specific(self):
        self.seed_workers(native_eol=b'\n')
        result = self.run_shell(WORKERS)
        self.assertEqual(result.returncode, 0, (result.stdout, result.stderr))

        # Use a valid Linux abort status, so only the byte comparison can fail.
        self.seed_workers(native_eol=b'\n', oracle_eol=b'\n', code=134)
        result = self.run_shell(WORKERS, 'linux')
        self.assertEqual(result.returncode, 0, (result.stdout, result.stderr))
        self.seed_workers(native_eol=b'\n', code=134)
        result = self.run_shell(WORKERS, 'linux')
        self.assertNotEqual(result.returncode, 0, (result.stdout, result.stderr))
        self.assertIn(b'output bytes differs', result.stderr)

    def test_worker_first_record_preserves_payload_cr(self):
        for side in ('native', 'oracle', 'both'):
            with self.subTest(side=side):
                self.seed_workers()
                for target in ('native', 'oracle') if side == 'both' else (side,):
                    path = self.directory / f'worker-pool/{target}.stderr'
                    path.write_bytes(diagnostic('pool').encode()
                                     + b'\r\r\n   at Program.Worker()\r\n')
                result = self.run_shell(WORKERS)
                self.assertNotEqual(result.returncode, 0, (result.stdout, result.stderr))

    def test_actual_worker_loop_keeps_pair_and_trace_checks(self):
        self.seed_workers()
        result = self.run_shell(WORKERS)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotEqual(self.run_shell(WORKERS, 'linux').returncode, 0)
        prefix = self.directory / 'worker-pool/native'
        Path(str(prefix) + '.stderr').write_bytes(diagnostic('pool').encode() + b'\r\n')
        self.assertNotEqual(self.run_shell(WORKERS).returncode, 0)
        self.seed_workers()
        Path(str(prefix) + '.stdout').write_bytes(b'wrong\n')
        self.assertNotEqual(self.run_shell(WORKERS).returncode, 0)


if __name__ == '__main__':
    unittest.main()
