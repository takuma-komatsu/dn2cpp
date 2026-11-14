#!/usr/bin/env python3
"""Managed-only integration checks for the diagnostic measurement contract."""
import argparse
import csv
import json
from pathlib import Path
import subprocess

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--cli', required=True)
p.add_argument('--corelib', required=True)
p.add_argument('--fixture', required=True)
p.add_argument('--output', required=True)
p.add_argument('--dotnet', default='dotnet')
a = p.parse_args()
out = Path(a.output).resolve(); out.mkdir(parents=True, exist_ok=False)
cli = Path(a.cli).resolve(); fixture = Path(a.fixture).resolve(); core = Path(a.corelib).resolve()
checks = 0

def check(condition, message):
    global checks
    checks += 1
    if not condition: raise AssertionError(message)


def execute(name, command):
    (out / (name + '.command.json')).write_text(json.dumps(command, indent=2) + '\n')
    with (out / (name + '.stdout')).open('w') as so, (out / (name + '.stderr')).open('w') as se:
        result = subprocess.run(command, stdout=so, stderr=se, timeout=120)
    (out / (name + '.exit')).write_text(str(result.returncode) + '\n')
    return result.returncode


base = [a.dotnet, 'exec', str(cli), str(fixture), '-r', str(core), '--no-ildiet']
normal = out / 'normal'
check(execute('normal', base + ['-o', str(normal)]) == 2, 'normal mode must fail closed')
normal_error = (out / 'normal.stderr').read_text()
check('GapFixture.First' in normal_error, 'normal mode identifies first failure')
check('GapFixture.Second' not in normal_error, 'normal mode remains fail-fast')
for workers in (1, 2):
    target = out / ('measure-' + str(workers))
    check(execute('measure-' + str(workers), base + ['--measure', '--jobs', str(workers), '-o', str(target)]) == 0,
          'measure succeeds as a diagnostic with gaps')
    lines = list(csv.reader((target / 's0-gaps-detail.tsv').open(), delimiter='\t', quoting=csv.QUOTE_NONE))
    for method in ('First', 'Second', 'TwoFailures'):
        check(sum(row[0] == 'compile' and row[2] == 'GapFixture.' + method for row in lines) == 1,
              'exactly first compile failure recorded in ' + method)
    # TwoFailures contains two distinct unsupported API calls, but one body failure.
    metadata = dict(csv.reader((target / 's0-measure.tsv').open(), delimiter='\t'))
    check(metadata['firstFailurePerBody'] == 'true', 'first-failure limit declared')
    check(metadata['discoveryStatus'] == 'partial', 'scan failures make discovery partial')
    check(metadata['unknownGapCount'] == 'null', 'unseen gaps are unknown, not zero')
    check(not list(target.glob('generated*')) and not (target / 'CMakeLists.txt').exists(), 'measure emits no usable native output')
for file in ('s0-gaps-detail.tsv', 's0-measure.tsv'):
    check((out / 'measure-1' / file).read_bytes() == (out / 'measure-2' / file).read_bytes(),
          'deterministic evidence across worker counts: ' + file)
(out / 'verification.json').write_text(json.dumps({'checks': checks, 'passed': True, 'nativeExecution': False}, indent=2) + '\n')
print(f'Measurement CLI checks: {checks}')
