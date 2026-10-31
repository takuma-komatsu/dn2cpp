#!/usr/bin/env python3
"""Check pinned CLR unsupported fixed-vector outcomes without target normalization."""
import argparse
from pathlib import Path

cases = [
    ('default-init', 'result=False', ''),
    ('IsSupported', 'result=False', ''),
    ('Count', 'NotSupportedException', ''),
    ('typed', 'NotSupportedException', ''),
    ('hash', 'NotSupportedException', ''),
    ('object-null', 'result=False', ''),
    ('object-wrong', 'result=False', ''),
    ('object-other-vector', 'result=False', ''),
    ('object-same-vector', 'NotSupportedException', ''),
    ('boxed-null', 'result=False', ''),
    ('boxed-wrong', 'result=False', ''),
    ('boxed-same', 'NotSupportedException', ''),
    ('boxed-distinct-same-type', 'NotSupportedException', ''),
    ('boxed-hash', 'NotSupportedException', ''),
    ('interface', 'NotSupportedException', ''),
    ('constrained-hash', 'NotSupportedException', ''),
    ('constrained', 'NotSupportedException', ''),
    ('comparer-equal', 'NotSupportedException', ''),
    ('comparer-hash', 'NotSupportedException', ''),
    ('interface-address', 'NotSupportedException', ''),
    ('typed-address', 'NotSupportedException', ''),
    ('object-address-null', 'result=False', ''),
    ('object-address-same', 'NotSupportedException', ''),
    ('hash-address', 'NotSupportedException', ''),
    ('effects-typed', 'NotSupportedException', 'RA'),
    ('effects-typed-argument-throws', 'InvalidOperationException', 'RA'),
    ('effects-typed-receiver-throws', 'InvalidOperationException', 'R'),
    ('effects-object-null', 'result=False', 'RA'),
    ('effects-object-same', 'NotSupportedException', 'RA'),
    ('effects-object-argument-throws', 'InvalidOperationException', 'RA'),
    ('effects-hash', 'NotSupportedException', 'R'),
]

def expected_lines():
    result = ['-- fixed vector unsupported semantics --']
    for width in (64, 128, 256, 512):
        for element in ('decimal', 'int128', 'uint128', 'enum', 'struct', 'ref-containing-struct'):
            for name, outcome, effects in cases:
                if not outcome.startswith('result='):
                    outcome = 'exception=System.' + outcome
                result.append(f'fixed{width}.{element}.{name}|{outcome}|effects={effects}')
    result.append('fixed vector unsupported end')
    return result

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--full', action='store_true')
parser.add_argument('--prefix', type=Path, help='complete pre-extension output, checked byte for byte')
args = parser.parse_args()
expected = expected_lines()
with args.output.open('rb') as stream:
    if args.prefix:
        with args.prefix.open('rb') as previous:
            for line in previous:
                assert stream.readline().replace(b'\r\n', b'\n') == line.replace(b'\r\n', b'\n'), 'historical full prefix changed'
    elif args.full:
        prefix = 0
        while True:
            position = stream.tell()
            line = stream.readline().decode().rstrip('\r\n')
            assert line, 'missing fixed unsupported block'
            if line == expected[0]:
                assert prefix > 0, 'missing historical prefix'
                stream.seek(position)
                break
            prefix += 1
    for index, line in enumerate(expected):
        actual = stream.readline().decode().rstrip('\r\n')
        assert actual == line, ('fixed unsupported outcome', index, actual, line)
    assert not stream.read(1), 'unexpected trailing fixed unsupported output'
print('fixed unsupported four-width six-element CLR outcomes and ordered effects passed')
