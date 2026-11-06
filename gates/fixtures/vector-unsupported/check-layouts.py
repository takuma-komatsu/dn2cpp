#!/usr/bin/env python3
"""Pin managed-vector storage separation without disabling ordinary canonical layouts."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
abi = '--abi' in sys.argv[2:]
header = (root / 'generated.h').read_text()
structs = {m[1]: (m[2], m[3]) for m in re.finditer(
    r'^struct (t_\w+)(?: : ([^\n]+))?\n\{(.*?)^\};', header, re.M | re.S)}
rows = {}
for path in root.glob('generated*.cpp'):
    with path.open() as stream:
        for line in stream:
            if line.startswith('const Dn2CppTypeInfo ti_VectorProbe_VectorUnsupported'):
                name = line.split(' = ', 1)[0].split()[-1]
                rows[name] = line

def storage(family, element, canonical):
    name = f't_VectorProbe_VectorUnsupported{family}_{canonical}'
    assert name in structs, ('missing storage', name)
    metadata = f'ti_VectorProbe_VectorUnsupported{family}_{element}'
    assert metadata in rows and f'sizeof({name})' in rows[metadata], ('metadata/layout link', metadata, name)
    return name, structs[name]

managed = 'VectorProbe_VectorUnsupportedEnum'
for element, canonical in ((managed, managed), ('Int32', '_CnInt32'), ('UInt32', '_CnUInt32')):
    value = f't_System_Numerics_Vector_{managed}' if element == managed else 'Dn2CppVectorT'
    for family in ('Direct', 'Inline', 'Base'):
        name, (_, body) = storage(family, element, canonical)
        assert f'{value} f_Value;' in body, ('vector field representation', name)
    name, (_, body) = storage('Nested', element, canonical)
    assert f't_VectorProbe_VectorUnsupportedInline_{canonical} f_Inner;' in body, ('nested value representation', name)
    name, (base, _) = storage('Derived', element, canonical)
    assert base == f't_VectorProbe_VectorUnsupportedBase_{canonical}', ('base representation', name, base)

# Enum scalar and Int32 scalar still use the same canonical class layout.
for element in (managed, 'Int32'):
    _, (_, body) = storage('Scalar', element, '_CnInt32')
    assert 'int32_t f_Value;' in body
assert f't_VectorProbe_VectorUnsupportedScalar_{managed}' not in structs
assert 't_VectorProbe_VectorUnsupportedScalar_Int32' not in structs

if abi:
    for element, canonical in ((managed, managed), ('Int32', '_CnInt32'), ('UInt32', '_CnUInt32')):
        name, (_, body) = storage('AbiWrapper', element, canonical)
        value = f't_System_Numerics_Vector_{managed}' if element == managed else 'Dn2CppVectorT'
        assert f'{value} f_Value;' in body and 'int32_t f_Marker;' in body
        for method in ('Identity', 'ByRef'):
            # The retained callable ABI must use this representation, not just
            # give the containing class a separately emitted correct layout.
            declarations = re.findall(r'^.*Program_VectorUnsupportedAbi' + method + r'_\w+\([^\n]*\);$', header, re.M)
            assert any(name in d for d in declarations), ('generic callable representation', method, name)
    for element in (managed, 'Int32'):
        storage('AbiScalar', element, '_CnInt32')
print('managed vector direct/nested/base layouts separated; scalar and numeric canonical layout links retained' + ('; generic wrapper callable ABI checked' if abi else ''))
