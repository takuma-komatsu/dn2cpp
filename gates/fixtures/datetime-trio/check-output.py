#!/usr/bin/env python3
"""Exact ordered native-policy witnesses; public oracle is a separate entry."""
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'oracle', 'lookup'], default='all')
a = p.parse_args()
expected = []
def check(label): expected.append('datetime-trio-' + label + ':True')
def lookup():
    for name in ['add', 'create', 'ctor']: check('reflection-' + name + '-lookup')
    check('reflection-lookups')
    for label in ['add-public', 'add-static', 'create-public', 'create-instance', 'add-exact', 'create-exact',
        'add-arity', 'add-value', 'create-arity', 'create-element', 'add-generic', 'create-generic',
        'ctor-exact', 'ctor-public', 'ctor-static', 'ctor-arity', 'ctor-kind', 'public-ctor-not-private',
        'method-null-types', 'ctor-null-types', 'method-null-element', 'null-type-first']:
        check('lookup-' + label)
def reflection():
    lookup()
    for delta in [1, -1]: check('reflection-add-' + str(delta))
    check('reflection-add-null-target')
    for year in [2024, 0]: check('reflection-create-' + str(year))
    for ambiguous in ['False', 'True']: check('reflection-ctor-' + ambiguous)
    check('reflection-ctor-range-wrapped')
    check('reflection-abi')
    expected.append('DateTime trio reflection end')
def oracle():
    check('public-create-table')
    check('public-neighbors')
if a.mode == 'minimal': check('minimal')
elif a.mode == 'oracle': oracle()
elif a.mode == 'lookup': lookup()
elif a.mode == 'reflection': reflection()
else:
    for route in ['direct', 'calli', 'delegate']:
        check('add-' + route + '-matrix')
        if route != 'delegate': check('add-' + route + '-alias')
        for delta in [0, -1]: check('add-' + route + '-null-out-' + str(delta))
        if route != 'delegate':
            check('add-' + route + '-null-receiver')
            check('add-' + route + '-both-null')
    for route in ['direct', 'calli', 'delegate']:
        check('create-' + route + '-matrix')
        for invalid in [0, 1]: check('create-' + route + '-null-out-' + str(invalid))
    for route in ['direct', 'calli']:
        check('ctor-' + route + '-matrix')
        for ticks in [0, -1, 3155378976000000000]: check('ctor-' + route + '-null-order-' + str(ticks))
    check('ctor-newobj-and-delegate')
    check('ctor-newobj-invalid')
    check('add-named-delegate-identity')
    check('create-named-delegate-identity')
    for mode, count in [('add', 3), ('create', 8), ('ctor', 4)]:
        for failure in range(-1, count+1): check('producer-' + mode + '-' + str(failure))
    reflection()
    oracle()
    expected.append('DateTime trio end')
    assert sum(line.endswith(':True') for line in expected) == 94
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode('utf-8').splitlines()
assert actual == expected, 'missing, failed, duplicated or reordered DateTime trio witness'
print('datetime-trio-output:' + str(sum(line.endswith(':True') for line in expected)))
