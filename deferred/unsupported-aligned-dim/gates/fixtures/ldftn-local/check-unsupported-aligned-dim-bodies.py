#!/usr/bin/env python3
"""Check emitted retained DIMs, not public memory facades or their unsafe tails."""
import collections
import hashlib
import json
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
paths = [root / 'generated.h', *sorted(root.glob('generated*.cpp'))]
text = '\n'.join(p.read_text() for p in paths)
pattern = r'^// (System\.Runtime\.Intrinsics\.ISimdVector_[^\n]+)::(LoadAligned|StoreAligned)\n([^\n]+)\n\{\n(.*?)^\}'
bodies = re.findall(pattern, text, re.M | re.S)
assert len(bodies) == 26, 'missing or extra closed unsupported DIM bodies'
operations = collections.Counter()
widths = collections.Counter()
helpers = set()
alignments = set()
rows = []
for owner, operation, declaration, body in bodies:
    operations[operation] += 1
    assert 'void*' in declaration and '&' not in declaration, 'DIM pointer ABI changed'
    assert body.count('dn2cpp_rem_unsigned') == 1
    assert body.count('ThrowHelper_ThrowAccessViolationException_') == 1
    assert body.count('unsupported aligned vector guard returned') == 1
    assert 'dn2cpp_fail("unsupported aligned vector guard returned");' in body
    assert not re.search(r'\breturn\b|sizeof\s*\(|memcpy|memmove|memset|dn2cpp_vec_|LoadUnsafe|StoreUnsafe|get_Count|->|\(\*|null|Null|byref|ByRef|dn2cpp_throw_invalid_operation', body), 'unsafe/default/fixup path in DIM'
    assert not re.search(r'(?<![A-Za-z0-9_])\*\s*(?:\(|[at][0-9]+\b)', body), 'pointee dereference in DIM'
    alignment = re.findall(r'\b(Vector(?:64|128|256|512)?_1_[A-Za-z0-9_]+_get_Alignment_m[0-9]+)\(\)', body)
    helper = re.findall(r'\b(ThrowHelper_ThrowForUnsupported(NumericsVector|IntrinsicsVector(?:64|128|256|512))BaseType_[A-Za-z0-9_]+)\(\)', body)
    assert len(alignment) == len(helper) == 1
    symbol, family = helper[0]
    width = '0' if family == 'NumericsVector' else family.removeprefix('IntrinsicsVector')
    assert alignment[0].startswith('Vector_1_' if width == '0' else f'Vector{width}_1_')
    assert body.index(alignment[0]) < body.index('dn2cpp_rem_unsigned') < body.index('ThrowHelper_ThrowAccessViolationException_') < body.index(symbol) < body.index('dn2cpp_fail')
    widths[width] += 1
    helpers.add(symbol); alignments.add(alignment[0])
    rows.append({'owner': owner, 'operation': operation, 'helper': symbol, 'alignment': alignment[0], 'body_sha256': hashlib.sha256(body.encode()).hexdigest()})
assert operations == {'LoadAligned': 13, 'StoreAligned': 13}
assert widths == {'0': 2, '64': 6, '128': 6, '256': 6, '512': 6}
assert len(helpers) == len(alignments) == 13
for symbol in helpers:
    found = re.findall(r'^(?:inline )?void ' + re.escape(symbol) + r'\(\)\n\{\n(.*?)^\}', text, re.M | re.S)
    assert len(found) == 1, 'genuine closed ThrowHelper body missing: ' + symbol
    body = found[0]
    assert re.search(r'= 67;', body), 'wrong exception resource'
    assert 'dn2cpp_throw_of_msg(&dn2cpp_not_supported_exception_type, "Specified type is not supported")' in body
    assert not re.search(r'get_Count|SkipInit|LoadUnsafe|StoreUnsafe|dn2cpp_vec_|memcpy|sizeof\s*\(', body)
for symbol in alignments:
    found = re.findall(r'^(?:inline )?int32_t ' + re.escape(symbol) + r'\(\)\n\{\n(.*?)^\}', text, re.M | re.S)
    assert len(found) == 1, 'genuine private Alignment bridge missing: ' + symbol
    body = found[0]
    match = re.match(r'Vector(64|128|256|512)_1_', symbol)
    if match:
        assert re.search(r'= ' + str(int(match[1]) // 8) + r';', body)
    else:
        assert re.search(r'Vector_get_Alignment_m[0-9]+\(\)', body)
assert not re.search(r'^// System\.(?:Numerics|Runtime\.Intrinsics)\.Vector[^\n]+::(?:[^\n]*\.)?(?:LoadUnsafe|StoreUnsafe)\n', text, re.M), 'proof-only unsafe facade/bridge was emitted'
print(json.dumps({'dim_bodies': len(rows), 'operations': dict(operations), 'width_bodies': dict(widths),
                  'actual_helper_bodies': len(helpers), 'actual_alignment_bodies': len(alignments),
                  'source_hashes': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}, 'bodies': rows}, indent=2, sort_keys=True))
