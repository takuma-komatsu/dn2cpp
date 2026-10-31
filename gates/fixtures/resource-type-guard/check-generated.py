#!/usr/bin/env python3
"""The resource name guard is a real body, not a successful constant stub."""
import argparse
from pathlib import Path
import re
p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--route', default='all')
a = p.parse_args()
s = '\n'.join(x.read_text() for x in sorted(a.output.glob('generated*.cpp')))
h = (a.output / 'generated.h').read_text()
pattern = r'^(?:inline )?int32_t ResourceManager_IsDefaultType_m\d+\([^\n]*\)\n\{'
matches = list(re.finditer(pattern, s + '\n' + h, re.M))
assert len(matches) == 1, ('unique managed guard definition', len(matches))
assert not re.search(r'ResourceManager_(?:CommonAssemblyInit|GetResourceSet|InternalGetResourceSet|GetSatelliteAssembly)_m\d+\(', h), 'resource object-model closure leaked'
if a.route in ('reflection', 'invoke'):
    assert not re.search(r'Program_(?:Direct|Address)_m\d+\(', h), 'independent reflection root lost'
if a.route == 'address':
    assert not re.search(r'Program_(?:Direct|Reflected|Invoke)_m\d+\(', h), 'independent address root lost'
if a.route == 'direct':
    assert not re.search(r'Program_(?:Address|Reflected|Invoke)_m\d+\(', h), 'independent direct root lost'
print('resource-type-guard-real-il')
