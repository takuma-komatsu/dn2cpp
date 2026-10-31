#!/usr/bin/env python3
"""Check and instrument real four-width value routes, including wide callable ABI.

Every edited generated unit is preserved beside its instrumented successor.
Instrumentation observes execution only; it never changes equality or hash results.
"""
import argparse
from pathlib import Path
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--check-only', action='store_true')
args = parser.parse_args()
out = args.output
header = out / 'generated.h'
paths = [header, *sorted(out.glob('generated*.cpp'))]
assert not (out / 'generated_fixed_vector_value_probe.cpp').exists(), 'already instrumented'
function = re.compile(r'^([^\n;{}]+\b(\w+)\([^\n]*\))\n\{.*?^\}', re.M | re.S)
widths = (64, 128, 256, 512)
seen = {w: set() for w in widths}
wrappers = {w: set() for w in widths}
adapters = {w: set() for w in widths}
bindings = {w: set() for w in widths}
changes = []
verified = 0

def helper(name, width):
    return re.compile(r'dn2cpp_vec_value_' + name + r'<double,\s*' + str(width // 8) + r'>')

def instrument(match):
    global verified
    declaration, name, body = match[1], match[2], match[0]
    for index, width in enumerate(widths):
        n = width // 8
        equal = helper('equals', width)
        hashed = helper('hash', width)
        kind = None
        if f'Program_FixedValueTupleWitness{width}_' in name:
            assert not re.search(r'vtable\[\s*-', body), 'negative bound method vtable slot'
            bindings[width].update(re.findall(r'&dgadapn_(Vector' + str(width) + r'_1_Equals_\w+)', body))
            for token in ('&dn2cpp_object_equals_virtual', '&dn2cpp_object_gethashcode'):
                assert token in body, (name, token)
        if name.startswith(f'Vector{width}_1_Equals_') and equal.search(body):
            assert 'std::memcpy' in body, 'boxed receiver must be copied'
            assert f'Dn2CppVector{width}' in declaration, ('wrapper ABI', declaration)
            wrappers[width].add(name)
            kind = 6
        if name.startswith(f'dgadapn_Vector{width}_1_Equals_'):
            assert f'Dn2CppVector{width} a0)' in declaration, ('adapter argument ABI', declaration)
            assert '__target + 1' in body, 'adapter must pass boxed payload'
            target = name[len('dgadapn_'):]
            assert target + '(' in body
            adapters[width].add(target)
            kind = 7
        if f'FixedValuePair{width}_TisDouble_' in name:
            for token in (equal, hashed, helper('object_equals', width),
                          re.compile(r'dn2cpp_vec_equals_all<double,\s*' + str(n) + '>'),
                          re.compile(r'dn2cpp_vec_equals<double,\s*' + str(n) + '>')):
                assert token.search(body), (name, token.pattern)
            kind = 2
        if 'FixedValueConstrainedEquals_' in name and equal.search(body):
            kind = 3
        if 'FixedValueConstrainedHash_' in name and hashed.search(body):
            kind = 4
        if 'ValueTuple_2_Equals_' in name and equal.search(body):
            kind = 0
        if 'ValueTuple_2_GetHashCode_' in name and hashed.search(body):
            kind = 1
        if f'Program_FixedValueArrayWitness{width}_' in name:
            assert re.search(r'Dn2CppArray\w*\* a0\)', declaration), ('array parameter ABI', declaration)
            body = body.replace('\n{', '\n{\n    dn2cpp_fixed_vector_array(a0, %d, %d);' % (index, n), 1)
            seen[width].add(8)
        if 'FixedValueMutableBox_1_' in name and ('Equals_' in name or 'GetHashCode_' in name) \
                and f'Vector{width}' in declaration:
            assert not re.search(r'->f_(?:Current|Replacement|Writes|Prefix|Marker)\b', body), (
                'boxed mutable receiver used aligned parent field dereference', name)
            for field in ('Current', 'Writes', 'Prefix', 'Marker'):
                assert re.search(r'offsetof\([^,]+, f_' + field + r'\)', body), (
                    'mutable field byte-offset/alias missing', name, field)
            assert 'std::memcpy' in body, ('mutable vector access must copy full width', name)
            kind = 9 if 'Equals_' in name else 10
        if f'FixedValueUnalignedRoundtrip_TisVector{width}_1_' in name:
            assert body.count('std::memcpy') >= 4, ('unaligned load/store copies missing', name)
            assert not re.search(r'\*\s*\(\s*\(?Dn2CppVector' + str(width) + r'\s*\*', body), (
                'unaligned vector dereference', name)
            kind = 11
        if kind is not None:
            seen[width].add(kind)
            body = body.replace('\n{', '\n{\n    dn2cpp_fixed_vector_hit(%d, %d);' % (index, kind), 1)
    body, count = re.subn(r'(    Program_FixedValueTupleWitness512_m\d+\(\);)',
                          r'\1\n    dn2cpp_fixed_vector_verify();', body)
    verified += count
    return body

for path in paths:
    original = path.read_text()
    changed = function.sub(instrument, original)
    for index, width in enumerate(widths):
        pattern = (r'^static int32_t (vector_value_itf_System_Runtime_Intrinsics_Vector' + str(width)
                   + r'_Double_\w+_equals)\([^\n]*\) \{([^\n]*)\}$')
        def interface(match, width=width, index=index):
            declaration = match[0].split('{', 1)[0]
            assert f'Dn2CppVector{width} other)' in declaration, ('interface ABI', declaration)
            assert helper('equals', width).search(match[2])
            assert 'std::memcpy' in match[2]
            assert '(const void*)&' + match[1] in original
            for name in ('box_hash', 'box_equals'):
                assert helper(name, width).search(original), ('boxed hook', width, name)
            seen[width].add(5)
            return match[0].replace('{', '{ dn2cpp_fixed_vector_hit(%d, 5);' % index, 1)
        changed = re.sub(pattern, interface, changed, flags=re.M)
    if changed != original:
        backup = path.with_suffix(path.suffix + '.fixed-vector-value-original')
        assert not backup.exists(), backup
        changes.append((path, backup, original, changed))

for width in widths:
    assert wrappers[width] and wrappers[width] == adapters[width] == bindings[width], (
        'typed binding/adapter/wrapper chain', width, wrappers[width], adapters[width], bindings[width])
    assert seen[width] == set(range(12)), ('missing actual routes', width, seen[width])
assert verified == 1, ('verify call site', verified)
if args.check_only:
    print('fixed vector generated routes and ABI checks passed; no files changed')
    raise SystemExit(0)
header_changed = False
for path, backup, original, changed in changes:
    backup.write_text(original)
    if path == header:
        changed = ('void dn2cpp_fixed_vector_hit(int, int);\n'
                   'void dn2cpp_fixed_vector_verify();\n'
                   'void dn2cpp_fixed_vector_array(Dn2CppArray*, int, int);\n' + changed)
        # The array forward declaration precedes the generated runtime include.
        changed = 'struct Dn2CppArray;\n' + changed
        header_changed = True
    path.write_text(changed)
if not header_changed:
    backup = header.with_suffix('.h.fixed-vector-value-original')
    assert not backup.exists()
    original = header.read_text()
    backup.write_text(original)
    header.write_text('struct Dn2CppArray;\nvoid dn2cpp_fixed_vector_hit(int, int);\n'
                      'void dn2cpp_fixed_vector_verify();\n'
                      'void dn2cpp_fixed_vector_array(Dn2CppArray*, int, int);\n' + original)
(out / 'generated_fixed_vector_value_probe.cpp').write_text('''#include "generated.h"
#include <cstdio>
#include <cstdlib>
#include <cstdint>
static unsigned fixed_vector_hits[4][12] = {};
static_assert(sizeof(Dn2CppVector64) == 8 && alignof(Dn2CppVector64) == 8);
static_assert(sizeof(Dn2CppVector128) == 16 && alignof(Dn2CppVector128) == 16);
static_assert(sizeof(Dn2CppVector256) == 32 && alignof(Dn2CppVector256) == 16);
static_assert(sizeof(Dn2CppVector512) == 64 && alignof(Dn2CppVector512) == 16);
void dn2cpp_fixed_vector_hit(int width, int route) { ++fixed_vector_hits[width][route]; }
void dn2cpp_fixed_vector_array(Dn2CppArray* value, int width, int bytes) {
    auto array = (Dn2CppArrayN*)value;
    const uintptr_t alignment = bytes < 16 ? bytes : 16;
    if (!array || array->length != 2 || array->elemSize != bytes
        || ((uintptr_t)array->data % alignment) != 0
        || ((uintptr_t)(array->data + bytes) % alignment) != 0) {
        std::fprintf(stderr, "fixed vector array ABI failed width=%d\\n", bytes);
        std::abort();
    }
    ++fixed_vector_hits[width][8];
}
void dn2cpp_fixed_vector_verify() {
    for (int width = 0; width < 4; ++width)
        for (int route = 0; route < 12; ++route)
            if (!fixed_vector_hits[width][route]) {
                std::fprintf(stderr, "fixed vector route missing width=%d route=%d\\n", width, route);
                std::abort();
            }
    std::fprintf(stderr, "fixed vector actual four-width tuple interface callable ABI passed hw=%d pointer=%zu\\n",
        DN2CPP_SIMD_HW_ACCEL, sizeof(void*));
}
''')
print('fixed vector actual four-width route and array ABI counters installed')
