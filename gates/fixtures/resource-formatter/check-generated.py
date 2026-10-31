#!/usr/bin/env python3
"""Exact formatter accessors refuse rather than silently returning a dummy object."""
import argparse
from pathlib import Path
import re
p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--route', required=True)
a = p.parse_args()
s = '\n'.join(x.read_text() for x in sorted(a.output.glob('generated*.cpp')))
h = (a.output / 'generated.h').read_text()
assert 'BinaryFormatter' in s and 'primitive' in s, 'explicit unsupported resource policy missing'
assert 'dn2cpp_throw_not_supported_msg(' in s, 'catchable policy refusal missing'
assert not re.search(r'BinaryFormatter_(?:_ctor|Deserialize)_m\d+\(', s + h), 'unsafe target closure leaked'
if a.route in ('pointer', 'address', 'reflection', 'all'):
    for name in ('CreateBinaryFormatter', 'Deserialize'):
        pattern = r'^(?:inline )?[^\n;]+ ResourceReader_' + name + r'_m\d+\([^\n]*\)\n\{'
        assert len(re.findall(pattern, s + '\n' + h, re.M)) == 1, ('unique callable wrapper', name)
if a.route == 'reflection':
    assert not re.search(r'Program_(?:Direct|Pointer|Address)(?:Create|Decode)_m\d+\(', h), 'independent reflection root lost'
print('resource-formatter-policy-shape:' + a.route)
