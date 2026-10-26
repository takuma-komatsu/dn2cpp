#!/usr/bin/env python3
"""Regression checks for the deliberately narrow gate-output ratchet."""

import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).with_name('lint-gate-output.py').resolve()
SPEC = importlib.util.spec_from_file_location('lint_gate_output', SCRIPT)
LINT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(LINT)


class PatternTests(unittest.TestCase):
    def test_legacy_helpers_and_cr_deletion(self):
        for source in [
            'actual=$(strip_cr_win "$actual")',
            'strip_cr_win_file "$raw" > "$view"',
            r"LC_ALL=C tr -d '\r' < output",
            r"tr --delete '\015'",
            r"tr -d '\r\n'",
            r'''tr -d "\x0d"''',
            r"tr -d $'\r'",
        ]:
            with self.subTest(source=source):
                self.assertTrue(LINT.violations(source))

    def test_unrelated_text_tools_and_comments_allowed(self):
        source = '\n'.join([
            '# strip_cr_win is legacy',
            'strip_cr_win() {',
            'strip_cr_win_file () {',
            "tr -d 'abc'",
            "grep -Fx 'marker' file",
            'gate_output witness file marker --policy crlf',
            'gate_output byte-witness file marker --policy crlf',
        ])
        self.assertFalse(LINT.violations(source))

    def test_new_binary_alias_prefers_named_api(self):
        legacy = 'gate_output witness "$raw" marker --binary-container --policy crlf'
        self.assertTrue(LINT.violations(legacy))
        self.assertFalse(LINT.introduced({'gates/old.sh': legacy}, {'gates/old.sh': legacy}))
        self.assertTrue(LINT.introduced({}, {'gates/new.sh': legacy}))

    def test_ratchet_retains_existing_and_catches_duplicate(self):
        line = 'strip_cr_win_file "$raw" > "$view"\n'
        before = {'gates/old.sh': line}
        self.assertFalse(LINT.introduced(before, {'gates/old.sh': '# moved\n' + line}))
        self.assertTrue(LINT.introduced(before, {'gates/old.sh': line * 2}))
        self.assertTrue(LINT.introduced(before, {'gates/new.sh': line}))
        self.assertFalse(LINT.introduced(before, {}))


class RepositoryTests(unittest.TestCase):
    def test_working_files_revisions_and_missing_base(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            def git(*args):
                return subprocess.check_output(['git', '-C', str(root), *args], stderr=subprocess.DEVNULL)
            def lint(*args):
                return subprocess.run([sys.executable, str(SCRIPT), *args], cwd=root,
                                      capture_output=True)
            git('init')
            git('config', 'user.name', 'Gate test')
            git('config', 'user.email', 'gate-test@example.invalid')
            (root / 'gates').mkdir()
            gate = root / 'gates' / 'old.sh'
            gate.write_text('strip_cr_win "$raw"\n', encoding='utf-8')
            git('add', '.')
            git('commit', '-m', 'baseline')
            base = git('rev-parse', 'HEAD').decode().strip()
            self.assertEqual(lint('--base', base).returncode, 0)
            gate.write_text('strip_cr_win "$changed"\n', encoding='utf-8')
            self.assertEqual(lint('--base', base).returncode, 1)
            self.assertEqual(lint('--base', base, '--head', base).returncode, 0)
            gate.write_text('', encoding='utf-8')
            (root / 'gates' / 'untracked.sh').write_text("tr -d '\\r'\n", encoding='utf-8')
            self.assertEqual(lint('--base', base).returncode, 1)
            self.assertEqual(lint('--base', 'missing-policy-base').returncode, 2)


if __name__ == '__main__':
    unittest.main()
