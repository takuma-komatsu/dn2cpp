#!/usr/bin/env python3
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'oracle'], default='all')
a = p.parse_args()
expected = []
def check(label): expected.append('dtfi-name-arrays-' + label + ':True')
def reflection():
    for family in ['genitive', 'leap']:
        for suffix in ['data', 'identity', 'abi', 'null']: check(family + '-reflection-' + suffix)
        if family == 'genitive': check(family + '-reflection-abbreviated')
if a.mode == 'oracle':
    for label in ['full', 'short', 'clone']: check('oracle-' + label)
elif a.mode == 'minimal':
    check('minimal-shape'); check('minimal-alias')
elif a.mode == 'reflection':
    reflection()
    expected.append('DTFI name arrays reflection end')
else:
    for label in ['literal-may', 'literal-empty', 'literal-scalar', 'full-leap', 'cross-receiver', 'clone-before', 'clone-after', 'readonly', 'public-clones', 'public-clones-isolated']: check(label)
    for route in ['direct', 'callvirt', 'calli']:
        for suffix in ['full', 'abbreviated', 'identity', 'null']: check('genitive-' + route + '-' + suffix)
    for route in ['direct', 'callvirt', 'calli']:
        for suffix in ['data', 'identity', 'null']: check('leap-' + route + '-' + suffix)
    for label in ['genitive', 'leap', 'abi']: check('delegate-' + label)
    for mode in range(4): check('producer-' + str(mode))
    reflection()
    for index in [0, 12]:
        for variant in range(2):
            for suffix in ['private', 'public-clones', 'scalars', 'internal']: check(f'mutation-{index}-{variant}-{suffix}')
    check('restored'); check('gc-root')
    expected.append('DTFI name arrays end')
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode().splitlines()
assert actual == expected, 'missing, failed, reordered or duplicate name-array witness'
print('dtfi-name-arrays-output:' + str(sum(line.endswith(':True') for line in expected)))
