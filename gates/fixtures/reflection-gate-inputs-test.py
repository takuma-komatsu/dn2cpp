#!/usr/bin/env python3
"""Pin witness multiplicity and the managed input reuse boundary."""

import contextlib
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location(
    "reflection_gate_inputs", Path(__file__).resolve().parents[1] / "reflection-gate-inputs.py")
inputs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inputs)


class ReflectionInputsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.app = self.directory / "Driver.dll"
        self.app.write_bytes(b"managed driver")
        self.reference = self.directory / "Library.dll"
        self.reference.write_bytes(b"managed reference")
        self.configuration = self.directory / "Driver.runtimeconfig.json"
        self.configuration.write_bytes(b"runtime configuration")

    def check(self, data, count, expected, prefixes=False):
        source = self.directory / "stdout"
        source.write_bytes(data)
        with contextlib.redirect_stderr(io.StringIO()):
            return inputs.check_lines(str(source), count, expected, prefixes=prefixes)

    def key(self, app=None, arguments=None):
        result = io.StringIO()
        with contextlib.redirect_stdout(result):
            inputs.oracle_key(str(app or self.app), arguments or [])
        return result.getvalue()

    def test_exact_lines_preserve_multiplicity(self):
        self.assertEqual(0, self.check(b"first\nneedle\nlast", "1", ["first", "needle", "last"]))
        self.assertEqual(1, self.check(b"needle\nneedle\n", "1", ["needle"]))
        self.assertEqual(0, self.check(b"needle\nneedle\n", "2", ["needle"]))
        self.assertEqual(0, self.check(b"needle\nneedle\n", "any", ["needle"]))

    def test_missing_and_substring_rows_fail(self):
        self.assertEqual(1, self.check(b"extra needle\nneedle extra\n", "any", ["needle"]))
        self.assertEqual(1, self.check(b"other\n", "1", ["needle"]))
        self.assertEqual(1, self.check(b"", "1", [""]))
        self.assertEqual(0, self.check(b"\n", "1", [""]))

    def test_prefixes_count_rows_without_accepting_middle_substrings(self):
        self.assertEqual(0, self.check(b"name=first\nother name=second\n", "1", ["name="], prefixes=True))
        self.assertEqual(1, self.check(b"other name=first\n", "1", ["name="], prefixes=True))
        self.assertEqual(1, self.check(b"name=first\nname=second\n", "1", ["name="], prefixes=True))

    def test_utf8_and_line_endings_match_grep(self):
        self.assertEqual(0, self.check("反射\n".encode("utf-8"), "1", ["反射"]))
        self.assertEqual(0 if os.name == "nt" else 1,
                         self.check(b"needle\r\n", "1", ["needle"]))
        self.assertEqual(1 if os.name == "nt" else 0,
                         self.check(b"needle\r\n", "1", ["needle\r"]))

    def test_payload_and_runtime_configuration_invalidate_oracle(self):
        original = self.key()
        self.reference.write_bytes(b"changed reference")
        reference = self.key()
        self.assertNotEqual(original, reference)
        self.configuration.write_bytes(b"changed runtime configuration")
        self.assertNotEqual(reference, self.key())
        self.assertNotEqual(self.key(), self.key(arguments=["prefix"]))
        self.app.write_bytes(b"rewritten IL")
        self.assertNotEqual(original, self.key())

    def test_copies_share_only_the_same_managed_payload(self):
        copied = self.directory / "copy"
        copied.mkdir()
        for source in (self.app, self.reference, self.configuration):
            (copied / source.name).write_bytes(source.read_bytes())
        self.assertEqual(self.key(), self.key(copied / self.app.name))

    def test_runtime_environment_is_part_of_the_oracle(self):
        for name in ("DN2CPP_BEFORE_LDFTN_LOCAL", "DN2CPP_REFLECTION_MEASURE",
                     "DN2CPP_STRIPPED_OVERRIDES", "DOTNET_ReadyToRun", "COMPlus_ReadyToRun"):
            with patch.dict(os.environ, {name: "first"}):
                original = self.key()
            with patch.dict(os.environ, {name: "second"}):
                self.assertNotEqual(original, self.key(), name)
        with patch.dict(os.environ, {"DN2CPP_OUT_SUFFIX": "native"}):
            native = self.key()
        with patch.dict(os.environ, {"DN2CPP_OUT_SUFFIX": "packed"}):
            self.assertEqual(native, self.key())


if __name__ == "__main__":
    unittest.main()
