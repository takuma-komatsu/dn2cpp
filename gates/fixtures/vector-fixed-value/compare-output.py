#!/usr/bin/env python3
"""Validate the fixed-value block and stream-compare a supplied old full prefix."""
import argparse
import subprocess
import sys
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--full', action='store_true', help='accept an independently checked historical prefix')
parser.add_argument('--prefix', '--prefix-output', dest='prefix', type=Path, help='pre-extension full VectorProbe output')
args = parser.parse_args()
block = [b'-- fixed vector value semantics --\n']
block += [f'fixed vector {width} twelve closures and all-lane IEEE oracle passed\n'.encode()
          for width in (64, 128, 256, 512)]
block += [b'fixed vector value end\n']
with args.output.open('rb') as stream:
    if args.prefix:
        with args.prefix.open('rb') as old:
            for previous in old:
                actual = stream.readline().replace(b'\r\n', b'\n')
                assert actual == previous.replace(b'\r\n', b'\n'), ('historical prefix differs', actual, previous)
    elif args.full:
        prefix_lines = 0
        while True:
            position = stream.tell()
            line = stream.readline().replace(b'\r\n', b'\n')
            assert line, 'missing fixed block'
            if line == block[0]:
                assert prefix_lines > 0, 'missing historical prefix'
                stream.seek(position)
                break
            prefix_lines += 1
    for expected in block:
        actual = stream.readline().replace(b'\r\n', b'\n')
        assert actual == expected, ('fixed value block differs', actual, expected)
    if args.full:
        assert stream.readline().replace(b'\r\n', b'\n') == b'-- fixed vector unsupported semantics --\n', 'fixed unsupported block must immediately follow numeric block'
        subprocess.run([sys.executable, str(Path(__file__).with_name('compare-managed-output.py')), str(args.output), '--full'], check=True)
    else:
        assert stream.read(1) == b'', 'unexpected trailing output'
print('fixed vector value block and supplied historical prefix passed' if args.prefix
      else 'fixed vector full output suffix passed' if args.full else 'fixed vector focused value block passed')
