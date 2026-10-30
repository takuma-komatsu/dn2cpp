#!/usr/bin/env python3
"""Validate the retained routes, then add test-only execution counters."""
from pathlib import Path
import re
import sys

out = Path(sys.argv[1])
header = out / 'generated.h'
text = header.read_text()
assert re.search(r'inline Dn2CppVectorT Vector_1_op_Explicit_\w+\(Dn2CppVectorT a0\)\n\{\s*\[\[maybe_unused\]\] Dn2CppVectorT t0;\s*t0 = a0;\s*return t0;\s*\}', text)
names = ['Boolean', 'UInt16', 'UInt32', 'UInt64', 'Single', 'Double', 'Sixteen', 'Three', 'Object', 'WithReference']
elements = {1: 'uint16_t', 2: 'uint32_t', 3: 'uint64_t', 4: 'float', 5: 'double'}
seen = set()
verify_calls = 0
for path in sorted(out.glob('generated*.cpp')):
    original = path.read_text()
    def instrument(match):
        kind, body = match.group(1), match.group(0)
        if kind not in names:
            return body
        i = names.index(kind)
        assert i not in seen
        seen.add(i)
        assert 'DN2CPP_SIMD_HW_ACCEL' in body
        assert body.count('IL_019E:;') == body.count('IL_0235:;') == 1
        body = body.replace('\n{', '\n{\n    dn2cpp_vector_fill_witness(0, %d);' % i, 1)
        body = body.replace('IL_019E:;', 'IL_019E:; dn2cpp_vector_fill_witness(1, %d);' % i)
        body = body.replace('IL_0235:;', 'IL_0235:; dn2cpp_vector_fill_witness(2, %d);' % i)
        if i in elements:
            # The next statement is the new explicit operator's spilled value copy.
            expr = r'(    (t\d+) = dn2cpp_vec_create_broadcast<' + elements[i] + r', 16>\([^\n]+\);\n    t\d+ = \2;)'
            body, count = re.subn(expr, r'\1\n    dn2cpp_vector_fill_witness(3, %d);' % i, body)
            assert count == 1, (kind, count)
        return body
    changed = re.sub(r'^void SpanHelpers_Fill_Tis(\w+)_m\d+\([^\n]+\)\n\{.*?^\}', instrument, original, flags=re.M | re.S)
    changed, calls = re.subn(r'(    ExplicitReinterprets_RetainedFills_m\d+\(\);)', r'\1\n    dn2cpp_vector_fill_verify();', changed)
    verify_calls += calls
    if changed != original:
        path.with_suffix(path.suffix + '.uninstrumented').write_text(original)
        path.write_text(changed)
assert seen == set(range(10)), seen
assert verify_calls == 1, verify_calls
all_cpp = '\n'.join(p.read_text() for p in out.glob('generated*.cpp'))
assert re.search(r'= \(void\*\)&Vector_1_op_Explicit_', all_cpp)
assert len(re.findall(r'= \(void\*\*?\)\(?&?Span_1_Fill_|= \(void\*\)&Span_1_Fill_', all_cpp)) >= 10
header.with_suffix('.h.uninstrumented').write_text(text)
text, count = re.subn(r'(inline Dn2CppVectorT Vector_1_op_Explicit_\w+\(Dn2CppVectorT a0\)\n\{)', r'\1\n    dn2cpp_vector_fill_witness(4, 0);', text)
assert count == 1
header.write_text('void dn2cpp_vector_fill_witness(int, int);\nvoid dn2cpp_vector_fill_verify();\n' + text)
(out / 'generated_vector_explicit_probe.cpp').write_text('''#include "generated.h"
#include <cstdio>
#include <cstdlib>
static unsigned hits[5][10] = {};
void dn2cpp_vector_fill_witness(int group, int kind) { ++hits[group][kind]; }
void dn2cpp_vector_fill_verify() {
        for (int i = 0; i < 10; ++i) {
            bool vector_expected = DN2CPP_SIMD_HW_ACCEL && i < 7;
            if (!hits[0][i] || !hits[2][i] || (hits[1][i] != 0) != vector_expected
                || (i >= 1 && i <= 5 && (hits[3][i] != 0) != vector_expected)) {
                std::fprintf(stderr, "retained fill witness failed %d: entry=%u vector=%u scalar=%u conversion=%u hw=%d\\n",
                    i, hits[0][i], hits[1][i], hits[2][i], hits[3][i], DN2CPP_SIMD_HW_ACCEL);
                std::abort();
            }
            std::fprintf(stderr, "retained fill branch %d: vector=%u scalar=%u conversion=%u\\n", i, hits[1][i], hits[2][i], hits[3][i]);
        }
        if (hits[4][0] != 2) std::abort();
        std::fprintf(stderr, "vector explicit callable and retained fill witnesses passed hw=%d pointer=%zu\\n", DN2CPP_SIMD_HW_ACCEL, sizeof(void*));
}
''')
print('vector explicit generated: real callable wrapper; ten retained Fill/SpanHelpers bodies; vector/scalar/conversion counters installed')
