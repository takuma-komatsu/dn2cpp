#!/usr/bin/env python3
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'oracle'], default='all')
a = p.parse_args()
expected = []
def check(label): expected.append('dtfi-parser-flags-' + label + ':True')
def reflection(label):
    for suffix in ['invariant', 'clone', 'abi', 'null', 'public']: check(label + '-reflection-' + suffix)
if a.mode == 'oracle':
    check('public-month-no-spaces')
    check('public-day-no-spaces')
elif a.mode == 'minimal':
    for label in ['month', 'day', 'adjustment']: check('minimal-' + label)
else:
    for label in ['month', 'day', 'adjustment']:
        if a.mode == 'all':
            for route in ['direct', 'callvirt', 'calli']:
                for suffix in ['invariant', 'clone', 'null']: check(label + '-' + route + '-' + suffix)
            for suffix in ['invariant', 'clone', 'abi']: check(label + '-delegate-' + suffix)
            for mode in range(3): check(label + '-producer-' + str(mode))
        reflection(label)
    expected.append('DTFI parser flags end' if a.mode == 'all' else 'DTFI parser flags reflection end')
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode('utf-8').splitlines()
assert actual == expected, 'missing, failed, reordered or duplicate parser flag witness'
counts = {'all': 60, 'reflection': 15, 'minimal': 3, 'oracle': 2}
assert sum(line.endswith(':True') for line in expected) == counts[a.mode]
print('dtfi-parser-flags-output:' + str(counts[a.mode]))
