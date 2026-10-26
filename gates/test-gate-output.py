#!/usr/bin/env python3
"""Synthetic contract checks; no SDK or native compiler required."""

import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

HELPER = Path(__file__).with_name("gate-output.py")
SPEC = importlib.util.spec_from_file_location("gate_output", HELPER)
output = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(output)


class OutputContract(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.actual = self.directory / "actual.out"
        self.expected = self.directory / "expected.out"

    def invoke(self, *args, ok=True):
        result = subprocess.run([sys.executable, str(HELPER), *map(str, args)],
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.assertEqual(result.returncode, 0 if ok else 1, result.stderr)
        return result

    def compare(self, actual, expected, *options, ok=True):
        self.actual.write_bytes(actual)
        self.expected.write_bytes(expected)
        self.invoke("compare", self.actual, self.expected, *options, ok=ok)
        self.assertEqual(self.actual.read_bytes(), actual)
        self.assertEqual(self.expected.read_bytes(), expected)

    def test_exact_bytes(self):
        for data in (b"", b"a\n", b"a\r\n", b"a\r", b"a\n\n", bytes(range(256))):
            with self.subTest(data=data):
                self.compare(data, data)
        for actual, expected in ((b"a\r\n", b"a\n"), (b"a", b"a\n"),
                                 (b"a\0b", b"ab"), (b"a\n\n", b"a\n")):
            self.compare(actual, expected, ok=False)

    def test_crlf_only(self):
        self.compare(b"a\r\nb\nc\r\n", b"a\nb\nc\n", "--policy", "crlf")
        for actual, expected in ((b"a\rb\n", b"ab\n"), (b"a \n", b"a\n"),
                                 (b"a\n\n", b"a\n"), (b"a\0\n", b"a\0\n")):
            self.compare(actual, expected, "--policy", "crlf", ok=False)

    def test_terminal_policy(self):
        self.compare(b"a\r\n", b"a", "--policy", "crlf", "--terminal-newline", "optional")
        self.compare(b"a\n\n", b"a\n", "--terminal-newline", "optional", ok=False)
        self.compare(b"a\r", b"a", "--terminal-newline", "optional", ok=False)
        self.compare(b"a\0\n", b"a\0", "--terminal-newline", "optional", ok=False)

    def test_records(self):
        self.assertEqual(output.records(b"a\r\n\r\n", "crlf"), [b"a", b""])
        self.assertEqual(output.records(b"a\rb\n\n", "crlf"), [b"a\rb", b""])
        self.assertEqual(output.records(b""), [])
        self.assertEqual(output.records(b"\n"), [b""])
        self.assertEqual(output.records(b"a\r\n"), [b"a\r"])
        with self.assertRaises(output.OutputError):
            output.records(b"a\0")

    def test_witness_count_and_order(self):
        self.actual.write_bytes(b"header\r\na\r\nb\r\ntail\r\n")
        self.expected.write_bytes(b"a\nb\n")
        self.invoke("witness", self.actual, "a", "b", "--policy", "crlf")
        self.invoke("witness", self.actual, "--policy", "crlf", "--count", "any", "a", "b")
        self.invoke("witness", self.actual, "a", ok=False)
        self.invoke("witness", self.actual, "a", "--count", 2, "--policy", "crlf", ok=False)
        self.invoke("witness", self.actual, "missing", "--policy", "crlf", ok=False)
        self.invoke("witnesses", self.actual, self.expected, "--policy", "crlf")
        self.expected.write_bytes(b"b\na\n")
        self.invoke("witnesses", self.actual, self.expected, "--policy", "crlf", ok=False)
        self.actual.write_bytes(b"a\na\nb\n")
        self.expected.write_bytes(b"a\nb\n")
        self.invoke("witnesses", self.actual, self.expected, ok=False)

    def test_prefix_suffix_section(self):
        self.actual.write_bytes(b"header\r\nstart\r\na\r\n\r\nend\r\ntail\r\n")
        for mode, expected, options in (
                ("prefix", b"header\n", ["--before", "start"]),
                ("suffix", b"tail\n", ["--after", "end"]),
                ("section", b"a\n\n", ["--start", "start", "--end", "end"]),
                ("prefix", b"header\nstart\n", []),
                ("suffix", b"end\ntail\n", [])):
            self.expected.write_bytes(expected)
            self.invoke(mode, self.actual, self.expected, "--policy", "crlf", *options)
        self.expected.write_bytes(b"a\n")
        self.invoke("section", self.actual, self.expected, "--policy", "crlf",
                    "--start", "start", "--end", "end", ok=False)
        for actual in (b"start\na\n", b"end\na\nstart\n", b"start\na\nend\nend\n"):
            self.actual.write_bytes(actual)
            self.invoke("section", self.actual, self.expected, "--start", "start", "--end", "end", ok=False)

    def test_status(self):
        self.invoke("status", 0, 0)
        self.invoke("status", 134, 134)
        self.invoke("status", 1, 0, ok=False)
        self.invoke("status", 1, 134, ok=False)
        self.invoke("status", 1, 134, "--policy", "success-class")
        self.invoke("status", 1, 0, "--policy", "success-class", ok=False)
        self.invoke("status", 0, 1, "--policy", "success-class", ok=False)
        for code in (124, 125, 126, 127, 130, 137, 143):
            self.invoke("status", code, code, ok=False)
            self.invoke("status", code, 134, "--policy", "success-class", ok=False)
            self.invoke("status", 134, code, "--policy", "success-class", ok=False)

    def test_view_preserves_raw(self):
        raw = b"a\r\nb\r\n\n\rc\n"
        self.actual.write_bytes(raw)
        self.invoke("view", self.actual, self.expected, "--policy", "crlf")
        self.assertEqual(self.actual.read_bytes(), raw)
        self.assertEqual(self.expected.read_bytes(), b"a\nb\n\n\rc\n")
        self.invoke("view", self.actual, self.actual, "--policy", "crlf", ok=False)
        alias = self.directory / "alias.out"
        alias.hardlink_to(self.actual)
        self.invoke("view", self.actual, alias, "--policy", "crlf", ok=False)
        self.assertEqual(self.actual.read_bytes(), raw)
        self.actual.write_bytes(b"a\0\n")
        self.invoke("view", self.actual, self.expected, "--policy", "crlf", ok=False)

    def test_select(self):
        raw = b"header\r\n\r\nmarker\r\ntail\r\n\r\n"
        self.actual.write_bytes(raw)
        self.invoke("select", self.actual, self.expected, "--before", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"header\r\n\r\n")
        self.invoke("select", self.actual, self.expected, "--from", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"marker\r\ntail\r\n\r\n")
        self.assertEqual(self.actual.read_bytes(), raw)
        self.actual.write_bytes(b"header\r\r\nmarker\r\ntail\r\r\n")
        self.invoke("select", self.actual, self.expected, "--before", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"header\r\r\n")
        self.invoke("select", self.actual, self.expected, "--from", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"marker\r\ntail\r\r\n")
        self.actual.write_bytes(b"header\nmarker\ntail\r")
        self.invoke("select", self.actual, self.expected, "--from", "marker", "--policy", "exact")
        self.assertEqual(self.expected.read_bytes(), b"marker\ntail\r")
        self.invoke("select", self.actual, self.actual, "--from", "marker", "--policy", "exact", ok=False)
        for raw in (b"absent\n", b"marker\nmarker\n"):
            self.actual.write_bytes(raw)
            self.invoke("select", self.actual, self.expected, "--before", "marker", "--policy", "exact", ok=False)
        self.actual.write_bytes(b"\n")
        self.invoke("select", self.actual, self.expected, "--from", "", "--policy", "exact")
        self.assertEqual(self.expected.read_bytes(), b"\n")
        self.invoke("select", self.actual, self.expected, "--before", "", "--policy", "exact")
        self.assertEqual(self.expected.read_bytes(), b"")

    def test_binary_selection_and_bounds(self):
        self.actual.write_bytes(b"head\0\r\nmarker\r\ntail\0\r\r\n")
        self.invoke("select", self.actual, self.expected, "--before", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"head\0\r\n")
        self.invoke("byte-prefix", self.actual, self.expected)
        self.invoke("select", self.actual, self.expected, "--from", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"marker\r\ntail\0\r\r\n")
        self.invoke("byte-suffix", self.actual, self.expected)
        self.invoke("select", self.actual, self.expected, "--through", "marker", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"head\0\r\nmarker\r\n")
        self.invoke("compare", self.expected, self.expected, "--policy", "crlf", ok=False)
        self.expected.write_bytes(b"tail\0\r\n")
        self.invoke("byte-suffix", self.actual, self.expected, ok=False)
        self.expected.write_bytes(b"head\0\n")
        self.invoke("byte-prefix", self.actual, self.expected, ok=False)
        self.actual.write_bytes(b"witness\nwitness\n")
        self.invoke("witness", self.actual, "witness", "--count", "any")
        self.invoke("witness", self.actual, "absent", "--count", "any", ok=False)

    def test_exclude_section(self):
        self.actual.write_bytes(b"head\0\r\r\nstart\r\nbody\0\r\nend\r\ntail\r")
        self.invoke("select", self.actual, self.expected, "--exclude-start", "start",
                    "--exclude-end", "end", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"head\0\r\r\ntail\r")
        for source in (b"end\nstart\n", b"start\nbody\n", b"start\nstart\nend\n"):
            self.actual.write_bytes(source)
            self.invoke("select", self.actual, self.expected, "--exclude-start", "start",
                        "--exclude-end", "end", "--policy", "crlf", ok=False)
        self.invoke("select", self.actual, self.expected, "--exclude-start", "start", "--policy", "crlf", ok=False)
        self.invoke("select", self.actual, self.expected, "--from", "start", "--exclude-end", "end", "--policy", "crlf", ok=False)

    def test_omit_prefix(self):
        raw = b"head\0\r\r\nremove: a\r\nkeep\r\nremove-other: b\r\ntail\r"
        self.actual.write_bytes(raw)
        self.invoke("omit-prefix", self.actual, self.expected, "--prefix", "remove:",
                    "--prefix", "remove-other:", "--count", 1, "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"head\0\r\r\nkeep\r\ntail\r")
        self.assertEqual(self.actual.read_bytes(), raw)
        self.invoke("omit-prefix", self.actual, self.expected, "--prefix", "absent", "--policy", "crlf", ok=False)
        self.invoke("omit-prefix", self.actual, self.expected, "--prefix", "remove:", "--count", 2, "--policy", "crlf", ok=False)
        self.invoke("omit-prefix", self.actual, self.actual, "--prefix", "remove:", "--policy", "crlf", ok=False)
        self.actual.write_bytes(b"lone\r")
        self.invoke("omit-prefix", self.actual, self.expected, "--prefix", "lone\r", "--policy", "crlf")
        self.assertEqual(self.expected.read_bytes(), b"")

    def test_mixed_binary_witness(self):
        raw = b"a\0b\r\nwitness\r\nlone\r\r\n"
        self.actual.write_bytes(raw)
        self.invoke("witness", self.actual, "witness", "--policy", "crlf", ok=False)
        self.invoke("witness", self.actual, "witness", "--binary-container", "--policy", "crlf")
        self.invoke("witness", self.actual, "ab", "--binary-container", "--policy", "crlf", ok=False)
        self.invoke("witness", self.actual, "lone", "--binary-container", "--policy", "crlf", ok=False)
        self.invoke("witness", self.actual, "lone\r", "--binary-container", "--policy", "crlf")
        self.assertEqual(self.actual.read_bytes(), raw)
        with self.assertRaises(output.OutputError):
            output.assert_witness([b"a\0b"], b"a\0b", 1)

    def test_byte_witness_contract(self):
        raw = b"binary\0payload\r\nready\r\n\r\nlo\0ne\ncr\r\r\nspace \n"
        self.actual.write_bytes(raw)
        for command, options in (("byte-witness", []), ("witness", ["--binary-container"])):
            self.invoke(command, self.actual, "ready", "", "cr\r", "space ", "--policy", "crlf", *options)
            for missing in ("lone", "cr", "space", "absent"):
                self.invoke(command, self.actual, missing, "--policy", "crlf", *options, ok=False)
            self.invoke(command, self.actual, "ready", "--count", 2, "--policy", "crlf", *options, ok=False)
            self.assertEqual(self.actual.read_bytes(), raw)
        self.invoke("witness", self.actual, "ready", "--policy", "crlf", ok=False)
        self.expected.write_bytes(raw.replace(b"\0", b""))
        self.invoke("compare", self.actual, self.expected, ok=False)
        self.expected.write_bytes(raw.rstrip(b"\n"))
        self.invoke("compare", self.actual, self.expected, ok=False)
        for empty in (b"", b"ready\0\n", b"ready\nready\n"):
            self.actual.write_bytes(empty)
            self.invoke("byte-witness", self.actual, "ready", ok=False)

    def test_bounded_selection_callers(self):
        for start, end in (("== runtime delegate method identities ==", "runtime delegate method identities end"),
                           ("metadata-assembly-begin", "metadata-assembly-end"),
                           ("metadata-policy-assembly-begin", "metadata-policy-assembly-end")):
            wanted = start.encode() + b"\r\npayload\0\r\r\n" + end.encode() + b"\r\n"
            self.actual.write_bytes(b"head\r\n" + wanted + b"tail\n")
            self.invoke("select", self.actual, self.expected, "--from", start, "--through", end, "--policy", "crlf")
            self.assertEqual(self.expected.read_bytes(), wanted)
        for raw in (b"start\nbody\n", b"start\nstart\nend\n", b"start\nend\nend\n", b"end\nstart\n"):
            self.actual.write_bytes(raw)
            self.invoke("select", self.actual, self.expected, "--from", "start", "--through", "end", "--policy", "crlf", ok=False)
        self.invoke("select", self.actual, self.expected, "--before", "start", "--through", "end", "--policy", "crlf", ok=False)
        self.invoke("select", self.actual, self.expected, "--exclude-start", "start", "--exclude-end", "end", "--through", "end", "--policy", "crlf", ok=False)
        self.invoke("select", self.actual, self.expected, "--policy", "exact", ok=False)

    def test_prefix_boundary_selection(self):
        raw = b"payload\0\r\nblank\r\r\n\n"
        for ending in (b"\n", b"\r\n", b""):
            self.actual.write_bytes(raw + b"boundary: platform text" + ending)
            self.invoke("select", self.actual, self.expected, "--before-prefix", "boundary:", "--policy", "exact")
            self.assertEqual(self.expected.read_bytes(), raw)
        for data in (raw, raw + b"boundary: one\nboundary: two\n", raw + b"not-boundary: text\n"):
            self.actual.write_bytes(data)
            self.invoke("select", self.actual, self.expected, "--before-prefix", "boundary:", "--policy", "exact", ok=False)
        self.actual.write_bytes(raw + b"boundary: text\n")
        for prefix in ("", "boundary:\n"):
            self.invoke("select", self.actual, self.expected, "--before-prefix", prefix, "--policy", "exact", ok=False)
        self.invoke("select", self.actual, self.expected, "--before-prefix", "boundary:", "--through", "end", "--policy", "exact", ok=False)
        self.invoke("select", self.actual, self.actual, "--before-prefix", "boundary:", "--policy", "exact", ok=False)

    def test_first_record_raw_then_semantic(self):
        for raw in (b"record\r\n", b"record\r\r\n", b"\n", b"binary\0\n", b"unterminated\r"):
            self.actual.write_bytes(raw + (b"tail\n" if raw.endswith(b"\n") else b""))
            self.invoke("select", self.actual, self.expected, "--first-record", "--policy", "exact")
            self.assertEqual(self.expected.read_bytes(), raw)
        self.actual.write_bytes(b"record\r\r\nnext\n")
        self.invoke("select", self.actual, self.expected, "--first-record", "--policy", "exact")
        self.actual.write_bytes(b"record\n")
        self.invoke("compare", self.expected, self.actual, "--policy", "crlf", ok=False)
        self.actual.write_bytes(b"")
        self.invoke("select", self.actual, self.expected, "--first-record", "--policy", "exact", ok=False)
        self.invoke("select", self.actual, self.expected, "--first-record", "--through", "end", "--policy", "exact", ok=False)
        self.actual.write_bytes(b"record\r\ntrace\n")
        self.invoke("select", self.actual, self.expected, "--first-record", "--policy", "exact")
        self.actual.write_bytes(b"record\n")
        self.invoke("compare", self.expected, self.actual, ok=False)
        for path in (self.expected, self.actual):
            self.invoke("witness", path, "record", "--policy", "crlf")

    def test_filter_view_without_second_normalization(self):
        raw = b"keep\r\r\n\r\nremove: one\r\nremove: two\nlast\r"
        self.actual.write_bytes(raw)
        view = self.directory / "view.out"
        self.invoke("view", self.actual, view, "--policy", "crlf")
        self.invoke("omit-prefix", view, self.expected, "--prefix", "remove:", "--prefix", "absent:", "--count", "all", "--policy", "exact")
        self.assertEqual(self.expected.read_bytes(), b"keep\r\n\nlast\r")
        self.actual.write_bytes(b"keep\n\nlast\r")
        self.invoke("compare", self.expected, self.actual, ok=False)
        self.actual.write_bytes(b"binary\0\r\r\nremove: one\n\nlast")
        self.invoke("omit-prefix", self.actual, self.expected, "--prefix", "remove:", "--count", "all", "--policy", "exact")
        self.assertEqual(self.expected.read_bytes(), b"binary\0\r\r\n\nlast")

    def test_byte_record_pattern(self):
        pattern = r"value=[0-9]+:done"
        for raw in (b"value=12:done\n", b"value=12:done\r\n"):
            self.actual.write_bytes(raw)
            self.invoke("match-record", self.actual, pattern, "--policy", "crlf")
        for raw in (b"value=12:done\r\r\n", b"value=12:done \n", b"prefixvalue=12:done\n", b"value=12:done\0\n"):
            self.actual.write_bytes(raw)
            self.invoke("match-record", self.actual, pattern, "--policy", "crlf", ok=False)
        self.actual.write_bytes(b"value=12:done\nvalue=3:done\n")
        self.invoke("match-record", self.actual, pattern, "--count", 2)
        self.invoke("match-record", self.actual, pattern, "--count", "any")
        self.invoke("match-record", self.actual, pattern, ok=False)
        self.invoke("match-record", self.actual, "[", ok=False)

    def test_migrated_reflection_patterns(self):
        cases = (
            (r'separate delegate method=.+/7', 'separate delegate method=Pick/7', 'any'),
            (r'delegate folded addresses: [1-9][0-9]*', 'delegate folded addresses: 3', 1),
            (r'reflected DateTimeOffset offset=90:ticks=[0-9]+:utc=True:kind=Unspecified:minutes=90',
             'reflected DateTimeOffset offset=90:ticks=123:utc=True:kind=Unspecified:minutes=90', 1),
            (r'signature ArrayShape GetMethod return=type:.*\|parameter=False\|contains=True',
             'signature ArrayShape GetMethod return=type:U[]|parameter=False|contains=True', 'any'),
            (r'signature closure Pick GetMethod return: name=DefinitionSignatureHolder`1 full=.+ generic=True',
             'signature closure Pick GetMethod return: name=DefinitionSignatureHolder`1 full=Example generic=True', 'any'),
        )
        for pattern, record, count in cases:
            for ending in (b'\n', b'\r\n', b'\r\r\n', b' \n'):
                self.actual.write_bytes(record.encode() + ending)
                self.invoke('match-record', self.actual, pattern, '--policy', 'crlf', '--count', count,
                            ok=ending in (b'\n', b'\r\n'))

    def test_expected_abort_evidence(self):
        witness = 'registration reached'
        diagnostic = 'Unhandled exception. System.InvalidOperationException: rejected'
        def write(code=127):
            for prefix in (self.actual, self.expected):
                Path(str(prefix) + '.status').write_bytes(f'{code}\n'.encode())
                Path(str(prefix) + '.stdout').write_bytes(b'baseline\r\nregistration reached\r\n')
                Path(str(prefix) + '.stderr').write_bytes(diagnostic.encode() + b'\r\n   at frame\r\n')
        def check(ok=True, host='windows'):
            self.invoke('abort-parity', self.actual, self.expected, '--host', host,
                        '--witness', witness, '--diagnostic', diagnostic, ok=ok)
        write()
        check()
        check(False, 'linux')
        check(False, 'macos')
        self.invoke('status', 127, 127, ok=False)
        for code in (0, 1, 124, 125, 126, 130, 137, 143):
            write(code)
            check(False)
        write(134)
        check(host='linux')
        for prefix in (self.actual, self.expected):
            for suffix, bad in (('.stdout', b''), ('.stdout', b'baseline\n'),
                                ('.stdout', b'registration reached\nregistration reached\n'),
                                ('.stdout', b'baseline\nregistration reached\n'),
                                ('.stderr', b'command not found\n'), ('.stderr', b''),
                                ('.stderr', diagnostic.encode() + b'\r\r\n'),
                                ('.status', b'134\n'), ('.status', b'127junk\n')):
                write()
                Path(str(prefix) + suffix).write_bytes(bad)
                check(False)

    def test_distinct_abort_diagnostics(self):
        native = 'dn2cpp fatal: unhandled exception in finalizer: System.InvalidOperationException'
        oracle = 'Unhandled exception. System.InvalidOperationException: boom'
        for prefix, diagnostic in ((self.actual, native), (self.expected, oracle)):
            Path(str(prefix) + '.status').write_bytes(b'127\n')
            Path(str(prefix) + '.stdout').write_bytes(b'about to throw from finalizer\r\n')
            Path(str(prefix) + '.stderr').write_bytes(diagnostic.encode() + b'\r\n')
        args = ('abort-parity', self.actual, self.expected, '--host', 'windows',
                '--witness', 'about to throw from finalizer', '--diagnostic', native)
        self.invoke(*args, ok=False)
        self.invoke(*args, '--oracle-diagnostic', oracle)
        for bad in ('', native, oracle + '\r', 'command not found'):
            self.invoke(*args, '--oracle-diagnostic', bad, ok=False)
        for actual_diagnostic, expected_diagnostic in ((oracle, native), ('', oracle),
                                                       (native, ''), ('command not found', oracle)):
            Path(str(self.actual) + '.stderr').write_bytes(actual_diagnostic.encode() + b'\n')
            Path(str(self.expected) + '.stderr').write_bytes(expected_diagnostic.encode() + b'\n')
            self.invoke(*args, '--oracle-diagnostic', oracle, ok=False)

    def test_wasm_abort_evidence(self):
        native = 'dn2cpp fatal: unhandled exception in finalizer: System.InvalidOperationException'
        oracle = 'Unhandled exception. System.InvalidOperationException: boom'
        args = ('wasm-abort', self.actual, self.expected, '--host', 'windows',
                '--witness', 'about to throw from finalizer', '--diagnostic', native,
                '--oracle-diagnostic', oracle)

        def write():
            Path(str(self.actual) + '.status').write_bytes(b'1\n')
            Path(str(self.expected) + '.status').write_bytes(b'127\n')
            Path(str(self.actual) + '.stdout').write_bytes(b'about to throw from finalizer\n')
            Path(str(self.expected) + '.stdout').write_bytes(b'about to throw from finalizer\r\n')
            Path(str(self.actual) + '.stderr').write_bytes(native.encode() + b'\n')
            Path(str(self.expected) + '.stderr').write_bytes(oracle.encode() + b'\r\n   at Finalize()\r\n')

        write()
        self.invoke(*args)
        self.invoke(*args[:4], 'linux', *args[5:], ok=False)
        for suffix, bad in (('.status', b'0\n'), ('.status', b'124\n'),
                            ('.stdout', b'other output\n'), ('.stderr', b'command not found\n')):
            write()
            Path(str(self.actual) + suffix).write_bytes(bad)
            self.invoke(*args, ok=False)
        for suffix, bad in (('.status', b'1\n'), ('.status', b'124\n'),
                            ('.stdout', b'about to throw from finalizer\r\nextra\r\n'),
                            ('.stderr', b'command not found\n')):
            write()
            Path(str(self.expected) + suffix).write_bytes(bad)
            self.invoke(*args, ok=False)

    def test_native_abort_evidence(self):
        diagnostic = 'dn2cpp fatal: declared runtime fault'
        prefix = str(self.actual)
        def write(code=127, stdout=b'reached fault\r\n'):
            Path(prefix + '.status').write_bytes(f'{code}\n'.encode())
            Path(prefix + '.stdout').write_bytes(stdout)
            Path(prefix + '.stderr').write_bytes(diagnostic.encode() + b'\r\n')
        def check(ok=True, host='windows', empty=False):
            evidence = ['--empty-stdout'] if empty else ['--witness', 'reached fault']
            self.invoke('native-abort', self.actual, '--host', host, '--diagnostic', diagnostic,
                        *evidence, ok=ok)
        write()
        check()
        self.assertEqual(Path(prefix + '.stdout').read_bytes(), b'reached fault\r\n')
        self.assertEqual(Path(prefix + '.stderr').read_bytes(), diagnostic.encode() + b'\r\n')
        check(False, 'linux')
        check(False, 'macos')
        check(False, empty=True)
        for code in (0, 1, 124, 125, 126, 130, 137, 143):
            write(code)
            check(False)
            write(code, b'')
            check(False, empty=True)
        write(134)
        check(host='linux')
        write(134, b'')
        check(host='macos', empty=True)
        for suffix, bad in (('.stdout', b''), ('.stdout', b'not the witness\n'),
                            ('.stdout', b'reached fault\nreached fault\n'),
                            ('.stdout', b'reached fault\r\r\n'),
                            ('.stdout', b'reached\0 fault\n'),
                            ('.stderr', b''), ('.stderr', b'command not found\n'),
                            ('.stderr', diagnostic.encode() + b'\r\r\n'),
                            ('.status', b'127junk\n'), ('.status', b'127\r\n')):
            write()
            Path(prefix + suffix).write_bytes(bad)
            check(False)
        for bad in (b'', b'command not found\n'):
            write(stdout=b'')
            Path(prefix + '.stderr').write_bytes(bad)
            check(False, empty=True)
        write(stdout=b'\n')
        check(False, empty=True)
        for suffix in ('.stdout', '.stderr', '.status'):
            write()
            Path(prefix + suffix).unlink()
            check(False)
        write(stdout=b'')
        check(empty=True)
        check(False, host='linux', empty=True)
        check(False, host='macos', empty=True)
        for extra in (['--witness', ''], ['--empty-stdout', '--diagnostic', '']):
            self.invoke('native-abort', self.actual, '--host', 'windows', '--diagnostic', diagnostic,
                        *extra, ok=False)
        for evidence in ([], ['--witness', 'reached fault', '--empty-stdout']):
            result = subprocess.run([sys.executable, str(HELPER), 'native-abort', prefix,
                                     '--host', 'windows', '--diagnostic', diagnostic, *evidence],
                                    stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            self.assertEqual(result.returncode, 2)

    def test_abort_opt_in_does_not_change_ordinary_status(self):
        prefix = str(self.actual)
        Path(prefix + '.status').write_bytes(b'127\n')
        Path(prefix + '.stdout').write_bytes(b'')
        Path(prefix + '.stderr').write_bytes(b'dn2cpp fatal: declared runtime fault\n')
        output.run(output.parser().parse_args(['native-abort', prefix, '--host', 'windows',
                                              '--diagnostic', 'dn2cpp fatal: declared runtime fault',
                                              '--empty-stdout']))
        for policy in ('exact', 'success-class'):
            with self.assertRaises(output.OutputError):
                output.run(output.parser().parse_args(['status', '127', '127', '--policy', policy]))

    def test_missing_input(self):
        self.expected.write_bytes(b"")
        for name in ("actual.out", "missing\\quoted'output.out"):
            with self.subTest(name=name):
                actual = self.directory / name
                result = self.invoke("compare", actual, self.expected, ok=False)
                # Diagnostics quote paths, including Windows separators.
                self.assertIn(f"actual={str(actual)!r}".encode(), result.stderr)
                self.assertIn(f"expected={str(self.expected)!r}".encode(), result.stderr)


if __name__ == "__main__":
    unittest.main()
