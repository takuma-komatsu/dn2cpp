#!/usr/bin/env python3
"""Exact deterministic native-policy witnesses; this is not a private CLR oracle."""
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal'], default='all')
a = p.parse_args()
expected = []
def witness(name):
    expected.append('year-month-adjustment-' + name + ':True')
if a.mode == 'minimal':
    witness('minimal')
else:
    if a.mode == 'all':
        for route in ['direct', 'callvirt', 'calli', 'delegate']:
            for parsed in ['False', 'True']:
                for index in range(5):
                    witness(f'{route}-values-{index}-{parsed}')
                    witness(f'{route}-alias-{index}-{parsed}')
                for label in ['null-year', 'null-month', 'null-both']:
                    witness(f'{route}-{label}-{parsed}')
                if route != 'delegate':
                    witness(f'{route}-null-receiver-{parsed}')
        for label in ['delegate-abi', 'has-direct', 'has-calli', 'has-direct-null', 'has-calli-null']:
            witness(label)
        for index in range(-1, 5):
            witness('producer-' + str(index))
    for parsed in ['False', 'True']:
        for index in range(5):
            witness(f'reflection-values-{index}-{parsed}')
    for label in ['reflection-abi', 'reflection-null-target', 'has-reflection', 'has-reflection-null-target']:
        witness(label)
    expected.append('Year month adjustment reflection end')
    if a.mode == 'all':
        assert sum(line.endswith(':True') for line in expected) == 135
        expected.append('Year month adjustment end:135')
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode('utf-8').splitlines()
assert actual == expected, 'year/month policy output missing, reordered, duplicated or failed'
print('year-month-adjustment-output:' + str(sum(line.endswith(':True') for line in expected)))
