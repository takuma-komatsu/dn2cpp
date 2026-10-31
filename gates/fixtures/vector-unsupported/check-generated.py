#!/usr/bin/env python3
"""Check retained Count/guard/resource path and managed, not numeric, value bodies."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
pattern = re.compile(r'^// (?P<owner>[^\n]+)::(?P<member>[^\n]+)\n(?P<decl>[^\n;{}]+\b(?P<name>\w+)\([^\n]*\))\n\{(?P<body>.*?)^\}', re.M | re.S)
functions = {}
for path in [root / 'generated.h', *sorted(root.glob('generated*.cpp'))]:
    source = path.read_text()
    # Unsupported elements must not be smuggled into numeric equality/hash helpers.
    for args in re.findall(r'dn2cpp_vec_value_\w+<([^>]+)>', source):
        assert not any(t in args for t in ('Decimal', 'Int128', 'VectorUnsupported')), ('numeric helper on unsupported element', path, args)
    for match in pattern.finditer(source):
        functions[match['name']] = match.groupdict()
for f in functions.values():
    # Unboxed managed receivers cannot be reinterpreted as object headers.
    pointers = re.findall(r't_System_Numerics_Vector_\w+\* (\w+);', f['body'])
    for pointer in pointers:
        assert not re.search(r'dn2cpp_object_(?:equals_virtual|gethashcode)\([^;\n]*\(\(Dn2CppObject\*\)' + re.escape(pointer) + r'\)', f['body']), ('unboxed object dispatch', f['name'], pointer)
counts = [f for f in functions.values() if f['owner'].startswith('System.Numerics.Vector_') and f['member'] == 'get_Count']
assert len(counts) == 6, ('managed Count closure coverage', len(counts))
for count in counts:
    body = count['body']
    helper_calls = re.findall(r'(ThrowHelper_ThrowForUnsupportedNumericsVectorBaseType_\w+)\(\);', body)
    assert len(helper_calls) == 1, ('exact guard call', count['name'])
    helper = functions[helper_calls[0]]
    assert helper['owner'] == 'System.ThrowHelper'
    assert body.index(helper_calls[0]) < body.index('sizeof('), count['name']
    hb = helper['body']
    assert re.search(r'= 0;\s+if \(\w+ != 0\) goto ', hb), ('false IsSupported branch', helper['name'])
    assert '= 67;' in hb, ('resource operand', helper['name'])
    assert 'dn2cpp_throw_of_msg(&dn2cpp_not_supported_exception_type, "Specified type is not supported")' in hb, ('resource-bearing NotSupported sink', helper['name'])
    members = [f for f in functions.values() if f['owner'] == count['owner']]
    assert len([f for f in members if f['member'] == 'Equals']) == 2, count['owner']
    hashes = [f for f in members if f['member'] == 'GetHashCode']
    assert len(hashes) == 1, count['owner']
    for f in members:
        assert 'dn2cpp_vec_value_' not in f['body'], ('managed body cut to numeric helper', f['name'])
    # The real IL loop enters at its Count condition, not at its lane body.
    for f in [hashes[0], *[f for f in members if 'SoftwareFallback' in f['member'] or f['member'] == 'op_Equality']]:
        b = f['body']
        target = re.search(r'goto (IL_\w+);', b)
        assert target, ('missing initial loop-condition branch', f['name'])
        label = b.index(target[1] + ':;')
        assert count['name'] + '()' in b[label:], ('condition must call real Count', f['name'])
        assert target.start() < label, f['name']
print('six managed Count -> exact guard -> resource NotSupported paths retained; numeric helper boundary intact')
