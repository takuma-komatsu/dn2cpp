#!/usr/bin/env python3
"""Inspect callable replacement bodies, including header-inlined fixed products."""
import pathlib
import re
import sys
root = pathlib.Path(sys.argv[1])
source = '\n'.join(p.read_text() for p in sorted(root.glob('generated*')) if p.suffix in ('.cpp', '.h'))
names = ('IntPtr', 'UIntPtr') if '--native-only' in sys.argv else ('Int32', 'UInt32', 'Int64', 'UInt64', 'IntPtr', 'UIntPtr')
for name in names:
    match = re.search(r'// System\.' + name + r'::BigMul \(primitive full-width multiplication\)\n.*?\n\}', source, re.S)
    assert match, name + ' replacement body'
    body = match.group()
    if name.endswith('Ptr'):
        for token in ('sizeof(intptr_t) == 4', ' >> 32', '*(intptr_t*)', 'dn2cpp_math_bigmul_'):
            assert token in body, name + ': ' + token
        assert not re.search(r'(?:Int128|UInt128)_(?:get_|BigMul)', body), 'host-width native IL retained'
        assert not re.search(r'\((?:u?int64_t)\*\)\(a2\)', body), 'wide output write'
        if name == 'UIntPtr':
            assert body.count('= (uintptr_t)(') == 2, 'restore both unsigned operands before widening'
    elif name.endswith('64'):
        assert '.f__lower = (uint64_t)' in body and '.f__upper = (uint64_t)' in body, 'named result fields'
        assert 'dn2cpp_math_bigmul_' in body, 'existing full-width helper'
print('primitive-bigmul-generated:' + '/'.join(names) + ':callable-target-width-bodies')
