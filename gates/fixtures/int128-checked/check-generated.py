#!/usr/bin/env python3
"""Check checked-factory bodies independently of the legacy truncating arm."""
import pathlib
import re
import sys

PATTERN = re.compile(r'\bt_System_Int128\s+Int128_CreateChecked\w*\([^;{}]*\)\s*\{')

def verify(text):
    bodies = []
    for match in PATTERN.finditer(text):
        end, depth = match.end(), 1
        while depth and end < len(text):
            depth += (text[end] == '{') - (text[end] == '}')
            end += 1
        if depth:
            raise ValueError('unterminated checked body')
        bodies.append(text[match.start():end])
    if not bodies:
        raise ValueError('no checked factory wrappers found')
    for body in bodies:
        if 'uint64_t*)&' in body or 'dn2cpp_alloc(' in body:
            raise ValueError('checked factory used alias stores or allocation')
        if 'CreateChecked_TisUIntPtr_' in body and '(uint64_t)((uintptr_t)' not in body:
            raise ValueError('UIntPtr did not restore target-width unsigned storage')
        if 'CreateChecked_TisInt128_' in body:
            if not re.search(r'\w+\s*=\s*\w+;', body) or '.f__lower' in body:
                raise ValueError('identity is not a typed copy')
        elif '.f__lower' not in body or '.f__upper' not in body or not re.search(r'uint64_t\)\w+', body):
            raise ValueError('missing named field construction')
    return len(bodies)


def self_test():
    body = ('t_System_Int128 Int128_CreateChecked_TisUIntPtr_m1(intptr_t a0) {'
            'uint64_t t1; t1=(uint64_t)((uintptr_t)(a0));'
            '(t0).f__lower=(uint64_t)t1; (t0).f__upper=(uint64_t)0; return t0;}')
    assert verify(body) == 1
    for bad in ('', body.replace('uintptr_t', 'intptr_t'), body.replace('.f__upper', '.other'),
                body.replace('return t0;', 'dn2cpp_alloc(8); return t0;'),
                body.replace('return t0;', '((uint64_t*)&t0)[0]=t1; return t0;')):
        try:
            verify(bad)
        except ValueError:
            continue
        raise AssertionError('checker accepted negative control')


self_test()
root = pathlib.Path(sys.argv[1])
text = '\n'.join(p.read_text() for p in sorted(root.glob('generated*.*')))
try:
    count = verify(text)
except ValueError as error:
    raise SystemExit(str(error)) from error
print('int128-checked-generated:typed-wrappers=' + str(count))
