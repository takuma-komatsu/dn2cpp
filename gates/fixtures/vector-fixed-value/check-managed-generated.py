#!/usr/bin/env python3
"""Require real fixed value/Count/guard bodies without unsupported numeric lanes."""
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
pattern = re.compile(r'^// (?P<owner>[^\n]+)::(?P<member>[^\n]+)\n(?P<decl>[^\n;{}]+\b(?P<name>\w+)\([^\n]*\))\n\{(?P<body>.*?)^\}', re.M | re.S)
functions = {}
for path in [root / 'generated.h', *sorted(root.glob('generated*.cpp'))]:
    source = path.read_text()
    for arguments in re.findall(r'dn2cpp_vec_value_\w+<([^>]+)>', source):
        assert not any(t in arguments for t in ('Decimal', 'Int128', 'VectorUnsupported')), ('unsupported numeric lane', path, arguments)
    for match in pattern.finditer(source):
        functions[match['name']] = match.groupdict()
for width in (64, 128, 256, 512):
    counts = [f for f in functions.values() if f['owner'].startswith(f'System.Runtime.Intrinsics.Vector{width}_') and f['member'] == 'get_Count']
    assert len(counts) == 6, ('fixed managed Count closures', width, len(counts))
    helper_width = 64 if width == 64 else 128
    for count in counts:
        calls = re.findall(r'(ThrowHelper_ThrowForUnsupportedIntrinsicsVector' + str(helper_width) + r'BaseType_\w+)\(\);', count['body'])
        assert len(calls) == 1, ('exact fixed helper', count['name'], calls)
        assert count['body'].index(calls[0]) < count['body'].index('sizeof('), ('guard before size', count['name'])
        helper = functions[calls[0]]
        assert helper['owner'] == 'System.ThrowHelper'
        assert re.search(r'= 0;\s+if \(\w+ != 0\) goto ', helper['body']), ('unsupported IsSupported', helper['name'])
        assert '= 67;' in helper['body'], ('unsupported resource', helper['name'])
        assert 'dn2cpp_throw_of_msg(&dn2cpp_not_supported_exception_type, "Specified type is not supported")' in helper['body'], ('managed NotSupported sink', helper['name'])
        members = [f for f in functions.values() if f['owner'] == count['owner']]
        equals = [f for f in members if f['member'] == 'Equals']
        hashes = [f for f in members if f['member'] == 'GetHashCode']
        assert len(equals) == 2 and len(hashes) == 1, ('complete managed value family', count['owner'])
        for member in [*equals, *hashes]:
            assert 'dn2cpp_vec_value_' not in member['body'], ('numeric value cut', member['name'])
        for member in [*hashes, *[f for f in members if 'SoftwareFallback' in f['member']]]:
            branch = re.search(r'goto (IL_\w+);', member['body'])
            assert branch, ('Count loop precondition branch', member['name'])
            target = member['body'].index(branch[1] + ':;')
            assert branch.start() < target and count['name'] + '()' in member['body'][target:], ('Count before first lane', member['name'])
print('four-width managed value families, exact guards and pre-lane Count paths retained')
