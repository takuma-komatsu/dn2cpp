#!/usr/bin/env python3
"""SDK-free optional culture selection; never invokes the real suite runner."""
import os
from pathlib import Path
import re
import shutil
import tempfile
import unittest
from bash_fixture import run_bash

ROOT = Path(__file__).resolve().parents[2]


class CultureScopeTests(unittest.TestCase):
    def shell(self, script, *args):
        return run_bash('set -euo pipefail\ncd "$1"; shift\n' + script,
                        ['culture-scope', ROOT.as_posix(), *args])

    def test_shared_gate_and_project_eligibility(self):
        for selected in ('0', '1'):
            result = self.shell('''DN2CPP_INCLUDE_CULTURE="$1"
source gates/_culture_gate_scope.sh
for gate in gates/build-and-run-{culture-info-api,default-culture,datetime,math-subset}.sh; do
    if culture_gate_selected "$gate"; then printf '%s\\n' "$gate"; fi
done
for project in CultureInfoApi DefaultCulture DateTimeSubset MathSubset; do
    if culture_project_selected "$project"; then printf '%s\\n' "$project"; fi
done
''', selected)
            self.assertEqual(result.returncode, 0, (result.stdout, result.stderr))
            self.assertEqual(b'culture-info-api' in result.stdout, selected == '1')
            self.assertEqual(b'CultureInfoApi' in result.stdout, selected == '1')
            for name in (b'default-culture', b'datetime', b'math-subset', b'DefaultCulture'):
                self.assertIn(name, result.stdout)
        result = self.shell('DN2CPP_INCLUDE_CULTURE=unknown\nsource gates/_culture_gate_scope.sh\necho BAD')
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn(b'BAD', result.stdout)

    def test_actual_premerge_dry_run_default_optin_and_scope_count(self):
        outputs = []
        for args in ([], ['--with-culture']):
            result = self.shell('DN2CPP_INCLUDE_CULTURE=1 "$BASH" gates/pre-merge.sh --dry-run --skip-godot --skip-xcode "$@"', *args)
            self.assertEqual(result.returncode, 0, (result.stdout, result.stderr))
            outputs.append(result.stdout)
        default, opted = outputs
        self.assertIn(b'DN2CPP_INCLUDE_CULTURE=0', default)
        self.assertIn(b'DN2CPP_INCLUDE_CULTURE=1', opted)
        self.assertIn(b'non-godot-except-xcode-except-culture', default)
        self.assertNotIn(b'except-culture', opted)
        self.assertIn(b'CultureInfoAPI: excluded', default)
        self.assertNotIn(b'CultureInfoAPI: excluded', opted)
        counts = [int(re.search(rb'gates:  (\d+)', value).group(1)) for value in outputs]
        self.assertEqual(counts[1], counts[0] + 1)
        self.assertIn(b'verify-culture-invariance.sh', default)
        self.assertNotIn(b'check-culture-reference.sh', default)
        self.assertIn(b'check-culture-reference.sh', opted)

    def test_invariance_entry_rejects_invalid_selection_and_empty_subjects(self):
        result = self.shell('DN2CPP_INCLUDE_CULTURE=invalid "$BASH" gates/verify-culture-invariance.sh')
        self.assertEqual(result.returncode, 2, (result.stdout, result.stderr))
        self.assertIn(b'selection must be 0 or 1', result.stderr)
        result = self.shell('DN2CPP_INCLUDE_CULTURE=0 "$BASH" gates/verify-culture-invariance.sh ""')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(b'no selected subjects', result.stderr)

    def test_optin_preflight_failure_precedes_invariance_and_builds(self):
        with tempfile.TemporaryDirectory(prefix='culture scope ') as directory:
            root = Path(directory)
            gates = root / 'gates'
            gates.mkdir()
            for name in ('pre-merge.sh', '_godot_gate_chains.sh', '_culture_gate_scope.sh'):
                shutil.copyfile(ROOT / 'gates' / name, gates / name)
            (gates / 'build-and-run-culture-info-api.sh').write_bytes(b'')
            (gates / 'build-and-run-sample.sh').write_bytes(b'')
            (gates / 'check-culture-reference.sh').write_text('echo reference >> "$DN2CPP_PREMERGE_LOGROOT/order"\nexit 73\n', encoding='utf-8')
            (gates / 'verify-culture-invariance.sh').write_text('echo invariance >> "$DN2CPP_PREMERGE_LOGROOT/order"\nexit 71\n', encoding='utf-8')
            for opted in (False, True):
                logs = root / ('opted' if opted else 'default')
                logs.mkdir()
                (logs / '_receipt.txt').write_bytes(b'old passed receipt')
                result = self.shell('DN2CPP_PREMERGE_NO_CAFFEINATE=1 DN2CPP_PREMERGE_LOGROOT="$2" "$BASH" "$1/gates/pre-merge.sh" --skip-godot --skip-xcode "${@:3}"', root.as_posix(), logs.as_posix(), *(['--with-culture'] if opted else []))
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((logs / '_receipt.txt').exists())
                self.assertEqual((logs / 'order').read_bytes(), b'reference\n' if opted else b'invariance\n')
                if opted:
                    self.assertEqual((logs / '_strict-culture.status').read_bytes(), b'73\n')
                else:
                    self.assertFalse((logs / '_strict-culture.status').exists())

    def test_ci_allowlist_does_not_inherit_culture_optin(self):
        for partition in ('core', 'arrays', 'limits'):
            result = self.shell('DN2CPP_INCLUDE_CULTURE=1 "$BASH" gates/ci-smoke.sh --profile primitives --partition "$1" --list', partition)
            self.assertEqual(result.returncode, 0, (result.stdout, result.stderr))
            self.assertNotIn(b'culture-info-api', result.stdout)
        source = (ROOT / 'gates/ci-smoke.sh').read_text(encoding='utf-8')
        self.assertIn('export DN2CPP_INCLUDE_CULTURE=0', source)

    def test_manual_gate_stays_strict_and_self_test_stays_sdk_free(self):
        source = (ROOT / 'gates/build-and-run-culture-info-api.sh').read_text(encoding='utf-8')
        preflight = 'bash "$_DN2CPP_GATE_ROOT/gates/check-culture-reference.sh"'
        self.assertEqual(source.count(preflight), 1)
        self.assertLess(source.index('if [ "${1:-}" = --self-test ]'), source.index(preflight))
        self.assertLess(source.index(preflight), source.index('DN2CPP_GATE_RUN_ARGS=before-culture-names'))
        self.assertNotIn('culture-reference-export.py', source)


if __name__ == '__main__':
    unittest.main()
