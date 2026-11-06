#!/usr/bin/env python3
"""Require a typed Boolean adapter with no byref dereference or hidden side effect."""
import argparse
from pathlib import Path
import re
p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--minimal', action='store_true')
a = p.parse_args()
sources = sorted(a.output.glob('generated*.cpp')) + [a.output / 'generated.h']
text = '\n'.join(path.read_text() for path in sources if path.exists())
heads = list(re.finditer(r'^(?:inline )?int32_t DateTimeFormatInfo_YearMonthAdjustment_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'unique actual Boolean adjustment definition required'
start = end = heads[0].end()
depth = 1
while depth:
    assert end < len(text)
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = re.sub(r'\s+', '', text[start:end-1]).replace('[[maybe_unused]]', '')
env = {f'a{i}': f'a{i}' for i in range(4)}
operations = []
for statement in body.split(';'):
    if not statement:
        continue
    if re.fullmatch(r'(?:t_System_Globalization_DateTimeFormatInfo\*|int32_t\*?|uint8_t)(\w+)', statement):
        continue
    assignment = re.fullmatch(r'(\w+)=(\w+)', statement)
    if assignment:
        lhs, rhs = assignment.groups()
        assert lhs not in ['a0', 'a1', 'a2', 'a3'], 'cannot mutate caller arguments'
        assert rhs in env or rhs == '1', 'not a simple staged argument or true literal'
        env[lhs] = env.get(rhs, rhs)
        if rhs == '1':
            operations.append('one')
        continue
    consumed = re.fullmatch(r'\(void\)\((\w+)\)', statement)
    if consumed:
        operations.append('void:' + env[consumed[1]])
        continue
    checked = re.fullmatch(r'dn2cpp_null_check\((\w+)\)', statement)
    if checked:
        operations.append('null:' + env[checked[1]])
        continue
    returned = re.fullmatch(r'return(\w+)', statement)
    if returned:
        operations.append('return:' + env[returned[1]])
        continue
    raise AssertionError('unexpected adapter instruction: ' + statement)
assert operations == ['void:a3', 'void:a2', 'void:a1', 'null:a0', 'one', 'return:1'], operations
if a.minimal:
    assert not re.search(r'Program_(Metadata|HasMetadata|HasDirect|HasPointer|Pointer|Address|Virtual|DelegateRoute|RunReflectionChecks)_m\d+\(', text), 'alternate minimal route retained'
print('year-month-adjustment-generated:consume-only-byrefs-null-first-true')

# The getter must remain actual managed IL: the FormatFlags lowering supplies
# None, then the original bit-8 AND and unsigned comparison still execute.
if not a.minimal:
    getters = list(re.finditer(r'^(?:inline )?int32_t DateTimeFormatInfo_get_HasYearMonthAdjustment_m\d+\([^\n]+\)\n\{', text, re.M))
    assert len(getters) == 1, 'unique real-IL HasYearMonthAdjustment required'
    begin = finish = getters[0].end()
    depth = 1
    while depth:
        depth += (text[finish] == '{') - (text[finish] == '}')
        finish += 1
    getter = re.sub(r'\s+', '', text[begin:finish-1]).replace('[[maybe_unused]]', '')
    getter = re.sub(r'(?:t_System_Globalization_DateTimeFormatInfo\*|int32_t)\w+;', '', getter)
    pattern = (r'(?P<receiver>\w+)=a0;(?P<checked>\w+)=(?P=receiver);dn2cpp_null_check\((?P=checked)\);'
        r'(?P<flags>\w+)=0;(?P<bit>\w+)=8;(?P<masked>\w+)=(?P=flags)&(?P=bit);'
        r'(?P<zero>\w+)=0;(?P<result>\w+)=\(\(\(uint32_t\)(?P=masked)\)>\(\(uint32_t\)(?P=zero)\)\)\?1:0;'
        r'return(?P=result);')
    assert re.fullmatch(pattern, getter), 'Has getter lost its real FormatFlags/bit-8 comparison: ' + getter
    print('year-month-adjustment-has-real-il:None-and-8-unsigned-compare')
