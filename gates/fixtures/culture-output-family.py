#!/usr/bin/env python3
"""Exercise the actual locale-family loop without an SDK or native build."""

import hashlib
import json
from pathlib import Path
import re
import shlex
from bash_fixture import run_bash
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = (ROOT / 'gates/build-and-run-culture-info-api.sh').read_text(encoding='utf-8')
COMMON = (ROOT / 'gates/_common.sh').read_text(encoding='utf-8')
ADAPTER = re.search(r'^culture_info_assert_locale_completion\(\) \{\n.*?^\}', SOURCE, re.M | re.S)[0]
ARRAY = re.search(r'^LOCALE_CASES=\(\n.*?^\)', SOURCE, re.M | re.S)[0]
MODES = shlex.split(ARRAY.split('(', 1)[1].rsplit(')', 1)[0])
LOOP = re.search(r'^    for mode in "\$\{LOCALE_CASES\[@\]\}"; do\n.*?^    done', SOURCE, re.M | re.S)[0]
MARKERS = {
    'culture-locale-upper': 'culture uppercase extensions end',
    'culture-locale-cache-true': 'culture data cache end',
    'culture-locale-cache-false': 'culture data cache end',
    'culture-sort-strings': 'culture sort strings end',
    'culture-sort-upper-key': 'culture sort strings end',
    'culture-numbering-boundaries': 'culture numbering boundaries end',
    'culture-wrapper-alias-first': 'culture wrapper cache end',
    'culture-wrapper-base-first': 'culture wrapper cache end',
    'culture-wrapper-sort-first': 'culture wrapper cache end',
    'culture-wrapper-unicode-first': 'culture wrapper cache end',
    'culture-wrapper-private-first': 'culture wrapper cache end',
    'culture-wrapper-canonical-first': 'culture wrapper cache end',
    'culture-primary-currency': 'culture primary currency end',
    'culture-missing-cache-first': 'culture missing number format cache end',
    'culture-missing-cache-empty-first': 'culture missing number format cache end',
    'culture-missing-upper-constructor-first': 'culture missing number format uppercase cache end',
    'culture-missing-upper-cached-first': 'culture missing number format uppercase cache end',
}
PRELUDE = '''set -euo pipefail
python="$1"; root="$2"; fixture="$3"
gate_output() { "$python" "$root/gates/gate-output.py" "$@"; }
DN2CPP_OS=linux; EXE_EXT=; _CG_APP=unused
'''
PRELUDE += re.search(r'^gate_assert_text\(\) \{\n.*?^\}', COMMON, re.M | re.S).group(0) + '\n'


class CultureFamilyTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.directory = Path(self.temporary.name)
        self.assertEqual(set(MODES), set(MARKERS), 'pin each actual locale mode independently')
        self.fill(b'\n')

    def fill(self, newline):
        for mode in MODES:
            data = b'opaque\0payload\rbytes' + newline + MARKERS[mode].encode() + newline
            for side in ('native', 'oracle'):
                (self.directory / f'{mode}.{side}').write_bytes(data)

    def run_family(self):
        script = PRELUDE + ARRAY + '\n' + ADAPTER + '''
culture_info_run() {
    local result="$1" side="$2"
    printf -v "$result" '%s' "$fixture/$mode.$side"
}
run_family() {
    local out="$fixture" native expected mode
''' + LOOP + '\n}\nrun_family\n'
        before = {p.name: p.read_bytes() for p in self.directory.iterdir()}
        result = run_bash(script, [ 'culture-family-fixture',
                                 Path(sys.executable).as_posix(), ROOT.as_posix(), self.directory.as_posix()])
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.directory.iterdir()},
                         'the family must never rewrite raw captures')
        return result

    def run_full_frozen_hook(self, data):
        path = self.directory / 'full.stdout'
        path.write_bytes(data)
        adapter = re.findall(r'^gate_extra_asserts\(\) \{\n.*?^\}', SOURCE, re.M | re.S)[0]
        script = PRELUDE + '''
culture_info_run() {
    local result="$1"
    printf -v "$result" '%s' "$fixture/full.stdout"
}
''' + adapter + '\ngate_extra_asserts "$fixture"\n'
        result = run_bash(script, [
                                 (ROOT / 'gates/build-and-run-culture-info-api.sh').as_posix(),
                                 Path(sys.executable).as_posix(), ROOT.as_posix(), self.directory.as_posix()])
        self.assertEqual(path.read_bytes(), data)
        return result

    def test_full_frozen_prefix_binary_container(self):
        snapshot = (ROOT / 'gates/expected/culture-info-api.txt').read_bytes()
        for newline in (b'\n', b'\r\n'):
            with self.subTest(newline=newline):
                prefix = snapshot.replace(b'\n', newline)
                section = b'== culture name resolution ==' + newline + b'fault=bad\0locale' + newline
                marker = b'culture name resolution end' + newline
                result = self.run_full_frozen_hook(prefix + section + marker)
                self.assertEqual(result.returncode, 0, f'stdout={result.stdout!r}, stderr={result.stderr!r}')
                self.assertNotEqual(self.run_full_frozen_hook(prefix + section).returncode, 0)
                self.assertNotEqual(self.run_full_frozen_hook(b'changed' + prefix + section + marker).returncode, 0)

    def test_lf_and_crlf_binary_containers(self):
        for newline in (b'\n', b'\r\n'):
            with self.subTest(newline=newline):
                self.fill(newline)
                result = self.run_family()
                self.assertEqual(result.returncode, 0, f'stdout={result.stdout!r}, stderr={result.stderr!r}')

    def test_missing_markers_fail_each_actual_mode(self):
        for mode in MODES:
            with self.subTest(mode=mode):
                self.fill(b'\n')
                for side in ('native', 'oracle'):
                    (self.directory / f'{mode}.{side}').write_bytes(b'opaque\0payload\n')
                self.assertNotEqual(self.run_family().returncode, 0)

    def test_count_any_keeps_duplicate_marker_semantics(self):
        for mode in MODES:
            for side in ('native', 'oracle'):
                path = self.directory / f'{mode}.{side}'
                path.write_bytes(path.read_bytes() + MARKERS[mode].encode() + b'\n')
        result = self.run_family()
        self.assertEqual(result.returncode, 0, f'stdout={result.stdout!r}, stderr={result.stderr!r}')

    def test_raw_parity_still_rejects_changed_nul_and_newlines(self):
        mode = 'culture-numbering-boundaries'
        for change in ('nul', 'newline'):
            with self.subTest(change=change):
                self.fill(b'\n')
                path = self.directory / f'{mode}.native'
                data = path.read_bytes()
                path.write_bytes(data.replace(b'\0', b'X') if change == 'nul' else data + b'\n')
                self.assertNotEqual(self.run_family().returncode, 0)


def replay_audit(directory):
    """Replay captured real program bytes through both actual extra-assert adapters."""
    directory = Path(directory).resolve()
    manifest = json.loads((directory / 'manifest.json').read_text(encoding='utf-8'))
    results = [json.loads(line) for line in (directory / 'results.jsonl').read_text(encoding='utf-8').splitlines()]
    for result in results:
        path = directory / f"{result['case']:03d}-{result['side']}.stdout"
        if result['status'] != 0 or hashlib.sha256(path.read_bytes()).hexdigest() != result['sha256']:
            raise SystemExit(f'FAIL: invalid audit receipt: {path}')
    adapters = re.findall(r'^gate_extra_asserts\(\) \{\n.*?^\}', SOURCE, re.M | re.S)
    arrays = SOURCE[SOURCE.index('LOCALE_CASES=('):SOURCE.index('# The locale family compares')]
    script = PRELUDE + arrays + '\n' + ADAPTER + '''
culture_info_run() {
    local result="$1" side="$2" path
    shift 2
    if [ "$side" = oracle ]; then shift; fi
    shift
    path=$("$python" "$root/gates/fixtures/culture-output-family.py" --lookup "$fixture" "$side" "$@")
    printf -v "$result" '%s' "$path"
}
''' + adapters[0] + '\ngate_extra_asserts "$fixture"\n' + adapters[-1] + '\ngate_extra_asserts "$fixture"\n'
    result = run_bash(script, [ (ROOT / 'gates/build-and-run-culture-info-api.sh').as_posix(),
                             Path(sys.executable).as_posix(), ROOT.as_posix(), directory.as_posix()])
    if result.returncode:
        raise SystemExit(f'FAIL: actual culture adapter replay exited {result.returncode}: '
                             f'stdout={result.stdout!r}, stderr={result.stderr!r}')
    print(f"OK: both actual culture adapters replayed against audited bytes ({len(manifest['args'])} cases)")


if __name__ == '__main__':
    if len(sys.argv) > 1 and sys.argv[1] == '--lookup':
        directory, side, *args = sys.argv[2:]
        cases = json.loads((Path(directory) / 'manifest.json').read_text(encoding='utf-8'))['args']
        index = cases.index(args)
        path = Path(directory) / f"{index:03d}-{'clr' if side == 'oracle' else side}.stdout"
        sys.stdout.buffer.write(path.as_posix().encode('utf-8') + b'\n')
    elif len(sys.argv) == 3 and sys.argv[1] == '--replay-audit':
        replay_audit(sys.argv[2])
    else:
        unittest.main()
