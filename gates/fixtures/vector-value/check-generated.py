#!/usr/bin/env python3
"""Prove actual double tuple bodies, then add non-semantic execution counters.

Application-wrapper calli is checked as such, never as an intrinsic address thunk.
Only modified translation units are backed up; no whole-output copy is made.
"""
from pathlib import Path
import re
import sys

out = Path(sys.argv[1])
check_only = '--check-only' in sys.argv[2:]
header = out / 'generated.h'
header_original = header.read_text()
function = re.compile(r'^([^\n;{}]+\b(\w+)\([^\n]*\))\n\{.*?^\}', re.M | re.S)
# Resolve each adapter's actual target body before partitioning its ABI family.
value_targets = {}
for source_path in [header, *sorted(out.glob('generated*.cpp'))]:
    for target_match in function.finditer(source_path.read_text()):
        if target_match.group(2).startswith('Vector_1_Equals_'):
            value_targets[target_match.group(2)] = (target_match.group(1), target_match.group(0))
managed_payloads = set()
expected_managed_payloads = {
    't_System_Numerics_Vector_System_Decimal',
    't_System_Numerics_Vector_VectorProbe_VectorUnsupportedEnum',
    't_System_Numerics_Vector_VectorProbe_VectorUnsupportedStruct',
    't_System_Numerics_Vector_VectorProbe_VectorUnsupportedRefStruct',
}
seen = set()
verified = 0
routes = set()
wrappers = set()
adapters = set()
bindings = set()
for match in function.finditer(header_original):
    if 'Program_VectorValueAddressWrapper_' in match.group(2):
        assert 'dn2cpp_vec_value_equals<double, 16>' in match.group(0)
        routes.add('wrapper-address')
changes = []
for path in sorted(out.glob('generated*.cpp')):
    if path.name == 'generated_vector_value_probe.cpp':
        raise AssertionError('already instrumented')
    original = path.read_text()
    def instrument(match):
        global verified
        body, name = match.group(0), match.group(2)
        kind = None
        if 'Program_VectorValueTupleWitness_' in name:
            assert not re.search(r'vtable\[\s*-', body), 'bound vector method used a negative vtable slot'
            for token in ('&dn2cpp_object_equals_virtual', '&dn2cpp_object_gethashcode',
                          'dgidv_Object_Equals_', 'dgidv_Object_GetHashCode_'):
                assert token in body, ('object/hash method-group binding', token)
            assert body.count('dn2cpp_box(&ti_System_Numerics_Vector_Double,') >= 3
            bindings.update(re.findall(r'&dgadapn_(Vector_1_Equals_\w+)', body))
        if name.startswith('Vector_1_Equals_') and 'dn2cpp_vec_value_equals<double, 16>' in body:
            assert 'std::memcpy' in body, 'boxed receiver must be copied to aligned storage'
            wrappers.add(name)
            kind = 6
        if name.startswith('dgadapn_Vector_1_Equals_'):
            target = name[len('dgadapn_'):]
            assert target in value_targets, ('adapter target body missing', target)
            declaration, target_body = value_targets[target]
            if 'Dn2CppVectorT a0)' in match.group(1):
                assert target + '(' in body and '__target + 1' in body
                assert 'dn2cpp_vec_value_equals<double, 16>' in target_body
                adapters.add(target)
                kind = 7
            else:
                payload = re.fullmatch(r'int32_t ' + re.escape(name) + r'\(Dn2CppObject\* __target, (t_\w+) a0\)', match.group(1))
                assert payload and payload[1] in expected_managed_payloads, ('unknown vector adapter ABI', name)
                value_type = payload[1]
                assert re.fullmatch(r'(?:DN2CPP_NOINLINE )?int32_t ' + re.escape(target) + r'\(' + re.escape(value_type) + r'\* a0, ' + re.escape(value_type) + r' a1\)', declaration), ('managed target payload ABI', declaration)
                assert 'return ' + target + '((' + value_type + '*)(__target + 1), a0);' in body
                assert 'dn2cpp_vec_value_' not in body + target_body
                assert re.search(r'Vector_1_(?:op_Equality|U3CEqualsU3Eg__SoftwareFallback)\w*\(', target_body), ('managed adapter must reach real value body', target)
                managed_payloads.add(value_type)
        if 'VectorValuePair_TisDouble_' in name:
            for token in ('dn2cpp_vec_value_equals<double, 16>', 'dn2cpp_vec_value_hash<double, 16>',
                          'dn2cpp_vec_value_object_equals<double, 16>', 'dn2cpp_vec_equals_all<double, 16>',
                          'dn2cpp_vec_equals<double, 16>', 'dn2cpp_object_equals_virtual'):
                assert token in body, (name, token)
            routes.add('direct')
            kind = 2
        if 'VectorValueConstrainedEquals_' in name and 'dn2cpp_vec_value_equals<double, 16>' in body:
            routes.add('constrained-equality')
            kind = 3
        if 'VectorValueConstrainedHash_' in name and 'dn2cpp_vec_value_hash<double, 16>' in body:
            routes.add('constrained-hash')
            kind = 4
        if 'ValueTuple_2_Equals_' in name and 'dn2cpp_vec_value_equals<double, 16>' in body:
            kind = 0
        if 'ValueTuple_2_GetHashCode_' in name and 'dn2cpp_vec_value_hash<double, 16>' in body:
            kind = 1
        if kind is not None:
            seen.add(kind)
            body = body.replace('\n{', '\n{\n    dn2cpp_vector_value_witness(%d);' % kind, 1)
        body, calls = re.subn(r'(    Program_VectorValueTupleWitness_m\d+\(\);)',
                            r'\1\n    dn2cpp_vector_value_verify();', body)
        verified += calls
        return body
    changed = function.sub(instrument, original)
    def interface_counter(match):
        name, body = match.group(1), match.group(2)
        assert 'dn2cpp_vec_value_equals<double, 16>' in body
        assert '(const void*)&' + name in original
        assert 'vector_value_itfs_System_Numerics_Vector_Double' in original
        type_row = re.search(r'^const Dn2CppTypeInfo ti_System_Numerics_Vector_Double = .*$', original, re.M)
        assert type_row and all(token in type_row.group(0) for token in (
            'vector_value_itfs_System_Numerics_Vector_Double',
            '&dn2cpp_vec_value_box_hash<double, 16>', '&dn2cpp_vec_value_box_equals<double, 16>'))
        seen.add(5)
        return match.group(0).replace('{', '{ dn2cpp_vector_value_witness(5);', 1)
    changed = re.sub(r'^static int32_t (vector_value_itf_System_Numerics_Vector_Double_\w+_equals)\([^\n]*\) \{([^\n]*)\}$',
                     interface_counter, changed, flags=re.M)
    if changed != original:
        backup = path.with_suffix(path.suffix + '.vector-value-original')
        assert not backup.exists(), backup
        changes.append((path, backup, original, changed))
header_changed = function.sub(instrument, header_original)
assert wrappers and wrappers == adapters == bindings, ('typed intrinsic callable chain', wrappers, adapters, bindings)
assert managed_payloads == expected_managed_payloads, ('managed callable payload coverage', managed_payloads)
assert seen == {0, 1, 2, 3, 4, 5, 6, 7}, ('actual tuple/direct/constrained helpers missing', seen)
assert verified == 1, ('tuple witness caller', verified)
assert routes == {'direct', 'constrained-equality', 'constrained-hash', 'wrapper-address'}, routes
original = header_original
assert 'dn2cpp_vec_value_box_equals<double, 16>' in original or any(
    'dn2cpp_vec_value_box_equals<double, 16>' in p.read_text() for p in out.glob('generated*.cpp')), 'boxed double hook missing'
backup = header.with_suffix('.h.vector-value-original')
assert not backup.exists(), backup
if check_only:
    print('vector value generated validation passed; no files changed')
    sys.exit(0)
for path, saved, before, after in changes:
    saved.write_text(before)
    path.write_text(after)
backup.write_text(original)
header.write_text('void dn2cpp_vector_value_witness(int);\nvoid dn2cpp_vector_value_verify();\n' + header_changed)
(out / 'generated_vector_value_probe.cpp').write_text('''#include "generated.h"
#include <cstdio>
#include <cstdlib>
static unsigned vector_value_hits[8] = {};
void dn2cpp_vector_value_witness(int kind) { ++vector_value_hits[kind]; }
void dn2cpp_vector_value_verify() {
    if (!vector_value_hits[0] || !vector_value_hits[1] || !vector_value_hits[2]
        || !vector_value_hits[3] || !vector_value_hits[4] || !vector_value_hits[5]
        || !vector_value_hits[6] || !vector_value_hits[7]) {
        std::fprintf(stderr, "vector value route witness failed tuple=%u/%u direct=%u constrained=%u/%u interface=%u callable=%u/%u\\n",
            vector_value_hits[0], vector_value_hits[1], vector_value_hits[2], vector_value_hits[3],
            vector_value_hits[4], vector_value_hits[5], vector_value_hits[6], vector_value_hits[7]);
        std::abort();
    }
    std::fprintf(stderr, "vector value actual tuple equality/hash passed hw=%d pointer=%zu\\n",
        DN2CPP_SIMD_HW_ACCEL, sizeof(void*));
}
''')
print('vector value generated actual tuple/direct/constrained helper counters installed')
