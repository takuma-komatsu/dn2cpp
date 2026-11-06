#!/usr/bin/env python3
"""Verify inline opaque-token field semantics and the conservative body boundary."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
text = '\n'.join(p.read_text() for p in sorted(root.glob('generated*.cpp')))
header = (root / 'generated.h').read_text()
assert 't_System_Nullable_System_Runtime_InteropServices_GCHandle' in header
assert not re.search(r'// System\.Nullable[^\n]*GCHandle::Equals\b', text)
assert not re.search(r'\b\w*Equals_m\d+\(t_System_Nullable_System_Runtime_InteropServices_GCHandle\*', header)
assert not re.search(r'\b\w*GCHandle_Equals_m\d+\(', text + header)
for owner, nullable in [('Holder', False), ('NullableHolder', True)]:
    cpp = 't_GCHandleEqualitySubset_' + owner
    x = f'(({cpp}*)a0)->f_' + ('Value' if nullable else 'Handle')
    y = '(a1).f_' + ('Value' if nullable else 'Handle')
    if nullable:
        x, y = f'({x}).f_value', f'({y}).f_value'
    for method, kind in [('Equals', 'equality'), ('GetHashCode', 'hash')]:
        pattern = re.escape(f'// GCHandleEqualitySubset.{owner}::{method} (synthesized ValueType field-by-field {kind})') + r'\n[^\n]+\n\{\n(.*?)\n\}'
        bodies = re.findall(pattern, text, re.S)
        assert len(bodies) == 1, (owner, method, len(bodies))
        body = bodies[0]
        assert not any(word in body for word in ['dn2cpp_box', 'dn2cpp_alloc', 'memcmp', 'dn2cpp_gchandle_target', 'dn2cpp_gchandle_addr', 'f__handle', '.cell']), (owner, method)
        if method == 'Equals':
            expression = f'(dn2cpp_gchandle_to_intptr({x}) == dn2cpp_gchandle_to_intptr({y}) ? 1 : 0)'
        else:
            token = f'(uint64_t)dn2cpp_gchandle_to_intptr({x})'
            expression = f'(int32_t)(({token}) ^ (({token}) >> 32))'
        assert body.count(expression) == 1, (owner, method, 'token expression')
        remainder = body.replace(expression, '')
        assert ('f_value' if nullable else 'f_Handle') not in remainder, (owner, method, 'extra payload use')
        if nullable:
            assert 'f_hasValue' in remainder, (owner, method, 'presence guard')
print('gchandle-equality-generated-token-and-retention:PASS')
