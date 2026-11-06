#!/usr/bin/env python3
"""Pin the address-only structural field expressions, independently of execution."""
from pathlib import Path
import re
import sys

text = '\n'.join(p.read_text() for p in sorted(Path(sys.argv[1]).glob('generated*.cpp')))
shapes = {
    'StructuralPointerSubset.Mixed': ['Pointer'],
    'StructuralPointerSubset.TypedOuter': ['Pointer'],
    'System.Reflection.Metadata.BlobReader': ['_endPointer', '_currentPointer'],
    'System.Reflection.Internal.MemoryBlock': ['Pointer'],
}
for owner, fields in shapes.items():
    cpp = 't_' + owner.replace('.', '_')
    for method, kind in [('Equals', 'equality'), ('GetHashCode', 'hash')]:
        pattern = re.escape(f'// {owner}::{method} (synthesized ValueType field-by-field {kind})') + r'\n[^\n]+\n\{\n(.*?)\n\}'
        bodies = re.findall(pattern, text, re.S)
        assert len(bodies) == 1, (owner, method, len(bodies))
        body = bodies[0]
        assert not any(word in body for word in ['dn2cpp_box', 'memcmp', 'memcpy', 'dn2cpp_alloc']), (owner, method)
        for field in fields:
            x = f'(({cpp}*)a0)->f_{field}'
            y = f'(a1).f_{field}'
            if method == 'Equals':
                expression = f'((uintptr_t)({x}) == (uintptr_t)({y}))'
            else:
                address = f'(uint64_t)(uintptr_t)({x})'
                expression = f'(int32_t)(({address}) ^ (({address}) >> 32))'
            assert body.count(expression) == 1, (owner, method, field, 'expression')
            # No other use of this field can read its pointee or introduce a helper.
            assert 'f_' + field not in body.replace(expression, ''), (owner, method, field, 'extra use')
print('structural-pointer-generated-address-only:PASS')
