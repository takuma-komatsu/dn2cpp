#!/usr/bin/env python3
import argparse
from pathlib import Path
p = argparse.ArgumentParser()
p.add_argument('stdout', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'minimal-culture', 'oracle', 'validator', 'initializer-cultures'], default='all')
p.add_argument('--validator-output', type=Path)
p.add_argument('--culture-oracle', type=Path)
a = p.parse_args()
actual = a.stdout.read_bytes().replace(b'\r\n', b'\n').decode().splitlines()
def constructor_outcomes(records):
    names = ['', 'en-US', 'de-DE_phoneb', 'zh-CHS', 'zh-CHT', 'zh-CHS_phoneb', 'zh-CHT_phoneb', 'en/US']
    assert len(records) == len(names), 'missing or extra initializer constructor outcome'
    accepted = []
    for index, name in enumerate(names):
        prefix = f'initializer-culture:{index}|{name}|'
        success = prefix + 'accepted'
        refusal = prefix + 'rejected|System.Globalization.CultureNotFoundException|name|' + name
        allowed = [success] if index < 5 else [refusal] if index == 7 else [success, refusal]
        assert records[index] in allowed, 'initializer constructor identity, outcome or exception fields'
        accepted.append(records[index] == success)
    return accepted
if a.mode == 'initializer-cultures':
    assert not a.culture_oracle and not a.validator_output, 'constructor oracle mode has no extraction inputs'
    constructor_outcomes(actual)
    print('dtfi-compare-info-constructor-outcomes:8')
    raise SystemExit(0)
assert a.mode == 'all' or not a.culture_oracle, 'constructor oracle applies only to the complete fixture'
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
elif a.mode == 'validator':
    pass
elif a.mode == 'reflection':
    reflection(); expected.append('DTFI CompareInfo reflection end')
else:
    assert a.culture_oracle is not None, 'complete initializer checks require the independent CLR constructor oracle'
    cultures = a.culture_oracle.read_bytes().replace(b'\r\n', b'\n').decode().splitlines()
    accepted = constructor_outcomes(cultures)
    for family in ['dtfi', 'culture']:
        for call in ['direct', 'callvirt', 'calli']: route(family + '-' + call)
    for label in ['delegate-dtfi', 'delegate-culture', 'delegate-abi', 'cross-getter', 'cross-receiver', 'dtfi-clone-readonly']: check(label)
    reflection()
    for family in ['dtfi', 'culture']:
        for mode in range(3): check(family + '-producer-' + str(mode))
    check('gc-initialized'); check('gc-identity'); expected.append('DTFI CompareInfo end')
    for index in range(7):
        expected.append(cultures[index])
        if accepted[index]:
            for route in ['newobj', 'call-ctor', 'seed', 'direct', 'calli']: check('initializer-' + route + '-' + str(index))
    expected.append(cultures[7])
    check('initializer-newobj-null')
    for route in range(3):
        check('initializer-null-culture-' + str(route)); check('initializer-null-receiver-' + str(route))
    check('initializer-gc-ctor'); check('initializer-gc-initsort')
    check('initializer-subclass-type'); check('initializer-subclass-metadata'); check('initializer-subclass-newobj')
    for route in range(3): check('initializer-subclass-' + str(route))
    expected.append('DTFI CompareInfo initializers end')
validator_start = len(expected)
if a.mode in ('all', 'validator'):
    expected.append('== CultureInfo name validation ==')
    for index in range(10):
        for fail in range(2):
            for route in range(3):
                check(f'validator-{index}-{fail}-{route}')
                prefix = f'culture-name-validator:{index}:{fail}:{route}:'
                valid = 1 <= index <= 6
                result = 'exception:System.NullReferenceException|<null>|' if index == 0 else \
                    'exception:System.ArgumentException|<null>|' if not valid and fail else 'value:' + str(valid)
                assert len(actual) > len(expected), 'missing validator result'
                record = actual[len(expected)]
                if result.startswith('exception:'):
                    assert record.startswith(prefix + result) and record.endswith('|end') \
                        and len(record) > len(prefix + result + '|end'), 'validator exception type/parameter/message'
                else:
                    assert record == prefix + result + '|end', 'validator result'
                expected.append(record)
    expected.append('CultureInfo name validation end')
if a.validator_output:
    assert a.mode == 'all', 'extract validator records only from the complete native fixture'
assert actual == expected, 'missing, false, reordered or duplicate CompareInfo witness'
if a.validator_output:
    a.validator_output.write_text('\n'.join(actual[validator_start:]) + '\n')
print('dtfi-compare-info-output:' + str(sum(x.endswith(':True') for x in expected)))
