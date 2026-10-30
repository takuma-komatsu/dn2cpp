#!/usr/bin/env python3
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'minimal-culture', 'oracle'], default='all')
a = p.parse_args()
expected = []
def check(label): expected.append('dtfi-compare-info-' + label + ':True')
def route(label):
    for suffix in ['initialized', 'identity', 'equals-hash', 'compare', 'null']: check(label + '-' + suffix)
def reflection():
    for family in ['dtfi', 'culture']:
        for suffix in ['initialized', 'identity', 'equals-hash', 'compare', 'abi', 'null']: check(family + '-reflection-' + suffix)
if a.mode == 'oracle':
    route('public'); check('public-override')
elif a.mode in ('minimal', 'minimal-culture'):
    check('minimal-culture' if a.mode == 'minimal-culture' else 'minimal-dtfi')
elif a.mode == 'reflection':
    reflection(); expected.append('DTFI CompareInfo reflection end')
else:
    for family in ['dtfi', 'culture']:
        for call in ['direct', 'callvirt', 'calli']: route(family + '-' + call)
    for label in ['delegate-dtfi', 'delegate-culture', 'delegate-abi', 'cross-getter', 'cross-receiver', 'dtfi-clone-readonly']: check(label)
    reflection()
    for family in ['dtfi', 'culture']:
        for mode in range(3): check(family + '-producer-' + str(mode))
    check('gc-initialized'); check('gc-identity'); expected.append('DTFI CompareInfo end')
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode().splitlines()
assert actual == expected, 'missing, false, reordered or duplicate CompareInfo witness'
print('dtfi-compare-info-output:' + str(sum(x.endswith(':True') for x in expected)))
