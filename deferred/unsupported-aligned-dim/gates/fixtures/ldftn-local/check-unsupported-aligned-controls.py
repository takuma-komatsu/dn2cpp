#!/usr/bin/env python3
"""Public memory controls are baseline non-regressions, not CLR parity claims."""
import argparse
import itertools
from pathlib import Path


def check(path):
    data = Path(path).read_bytes().replace(b'\r\n', b'\n')
    assert b'\r' not in data
    lines = data.decode('utf-8').splitlines()
    assert lines[0] == 'unsupported-aligned-control:start'
    assert lines[-1] == 'unsupported-aligned-control:passed'
    pairs = [(0, 'Decimal'), *itertools.product((64, 128, 256, 512), ('Char', 'Int128', 'UInt128'))]
    expected = {f'unsupported-aligned-dim:{width}:{element}:{operation}:control-{kind}:{mode}'
                for width, element in pairs for operation in ('LoadAligned', 'StoreAligned')
                for kind in ('unsafe', 'offset', 'aligned', 'nontemporal') for mode in (0, 1)}
    seen = set()
    for line in lines[1:-1]:
        fields = line.split('|'); key = fields[0]
        assert key in expected and key not in seen
        seen.add(key)
        assert fields[-2] == ('effects=VA' if ':StoreAligned:' in key else 'effects=A')
        assert fields[-1].startswith('memory=') and 0 <= int(fields[-1][7:]) <= 0xFFFFFFFF
        assert len(fields) == (4 if fields[1] == 'returned' else 6)
        if fields[1] != 'returned':
            assert fields[1].startswith('System.') and fields[2]
            assert -(1 << 31) <= int(fields[3]) < (1 << 31)
    assert seen == expected and len(seen) == 208
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output')
    parser.add_argument('--baseline')
    args = parser.parse_args()
    data = check(args.output)
    if args.baseline:
        assert data == check(args.baseline), 'public control behavior changed from the frozen baseline'
    print('unsupported-aligned-controls:observations=208:baseline-nonregression-only:pass')


if __name__ == '__main__':
    main()
