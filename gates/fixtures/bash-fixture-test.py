#!/usr/bin/env python3
"""The adapter harness must enter Bash before accepting positive/negative results."""
import os
import subprocess
import unittest
from unittest.mock import patch

from bash_fixture import run_bash


class BashFixtureTests(unittest.TestCase):
    def test_explicit_outer_shell_and_raw_result(self):
        for status in (0, 1):
            with self.subTest(status=status), patch.dict(os.environ, {'DN2CPP_TEST_BASH': 'C:/Program Files/Git/bin/bash.exe'}):
                receipt = subprocess.CompletedProcess([], status, b'dn2cpp-fixture-bash-entered\nraw\0\r\r\n', b'diagnostic\r\n')
                with patch('bash_fixture.os.path.isfile', return_value=True), patch('bash_fixture.subprocess.run', return_value=receipt) as launch:
                    result = run_bash('exit 1', ['fixture', 'argument with spaces'])
                self.assertEqual(launch.call_args.args[0][0], 'C:/Program Files/Git/bin/bash.exe')
                self.assertEqual(result.returncode, status)
                self.assertEqual(result.stdout, b'raw\0\r\r\n')
                self.assertEqual(result.stderr, b'diagnostic\r\n')

    def test_launcher_failure_cannot_pass_negative_test(self):
        receipt = subprocess.CompletedProcess([], 1, b'WSL is unavailable\n', b'')
        with patch.dict(os.environ, {'DN2CPP_TEST_BASH': 'explicit-bash'}), patch('bash_fixture.os.path.isfile', return_value=True), patch('bash_fixture.subprocess.run', return_value=receipt):
            with self.assertRaisesRegex(AssertionError, 'WSL is unavailable'):
                run_bash('exit 1', ['fixture'])

    def test_windows_requires_explicit_shell(self):
        with patch.dict(os.environ, {}, clear=True), patch('bash_fixture.os.name', 'nt'):
            with self.assertRaisesRegex(AssertionError, 'DN2CPP_TEST_BASH'):
                run_bash(':', ['fixture'])

    def test_windows_executable_suffix(self):
        receipt = subprocess.CompletedProcess([], 0, b'dn2cpp-fixture-bash-entered\n', b'')
        with patch.dict(os.environ, {'DN2CPP_TEST_BASH': 'C:/Git Bash/bin/bash'}), patch('bash_fixture.os.name', 'nt'), patch('bash_fixture.os.path.isfile', side_effect=lambda p: p.endswith('.exe')), patch('bash_fixture.subprocess.run', return_value=receipt) as launch:
            run_bash(':', ['fixture'])
            self.assertEqual(launch.call_args.args[0][0], 'C:/Git Bash/bin/bash.exe')

    def test_invalid_explicit_shell_fails_before_adapter(self):
        with patch.dict(os.environ, {'DN2CPP_TEST_BASH': '/dn2cpp-nonexistent-shell-unique'}):
            with self.assertRaisesRegex(AssertionError, 'Bash'):
                run_bash('exit 1', ['fixture'])

    def test_real_outer_shell_enters_for_both_statuses(self):
        for status in (0, 1):
            argument = "spaces 'quotes' \"double\" \\ slash"
            result = run_bash(f'printf "%s" "$1"; exit {status}', ['fixture', argument])
            self.assertEqual(result.stdout, argument.encode())
            self.assertEqual(result.returncode, status)


if __name__ == '__main__':
    unittest.main()
