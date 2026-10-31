#!/usr/bin/env python3
"""Stream-check the historical prefix and deterministic value-contract block."""
import argparse
import hashlib
import subprocess
import sys
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--full', action='store_true')
args = parser.parse_args()
block = [
    b'-- vector value semantics --\n',
    b'vector value tuple equality/hash and application wrapper passed\n',
    b'vector value twelve closures and IEEE byte oracle passed\n',
    b'vector value end\n',
]
# AVX-pinned pre-extension VectorProbe oracle; also checked against native output.
# This is a prefix regression assertion, not a replacement for CLR parity.
prefix_hash = '59c9632e32345208ec4a075edf886e4ab9048264eafcb0bde6f807d5efdaf016'
digest = hashlib.sha256()
index = 0
started = False
trailing = False
with args.output.open('rb') as stream:
    for line in stream:
        line = line.replace(b'\r\n', b'\n')
        if not started and line == block[0]:
            started = True
            if args.full:
                assert digest.hexdigest() == prefix_hash, ('historical prefix changed', digest.hexdigest())
        if started and index == len(block):
            assert args.full, ("unexpected trailing focused output", line)
            if not trailing:
                assert line == b'-- fixed vector value semantics --\n', ('unexpected block after numeric values', line)
            trailing = True
            continue
        if started:
            assert index < len(block) and line == block[index], ('value block mismatch', index, line)
            index += 1
        else:
            assert args.full, ('unexpected prefix in focused run', line)
            digest.update(line)
assert started and index == len(block), ('missing value block', index)
if trailing:
    subprocess.run([sys.executable, str(Path(__file__).parent.parent / 'vector-fixed-value' / 'compare-output.py'), str(args.output), '--full'], check=True)
print('vector value output and historical prefix passed' if args.full else 'vector value focused output passed')
