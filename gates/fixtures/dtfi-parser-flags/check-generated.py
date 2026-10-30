#!/usr/bin/env python3
"""Prove all three retained getters preserve real IL over the invariant FormatFlags callee."""
import argparse
from pathlib import Path
import re
p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--minimal', action='store_true')
a = p.parse_args()
paths = sorted(a.output.glob('generated*.cpp')) + [a.output / 'generated.h']
text = '\n'.join(path.read_text() for path in paths if path.exists())
for name, bit in [('HasSpacesInMonthNames', 4), ('HasSpacesInDayNames', 16), ('HasYearMonthAdjustment', 8)]:
    heads = list(re.finditer(r'^(?:inline )?int32_t DateTimeFormatInfo_get_' + name + r'_m\d+\([^\n]+\)\n\{', text, re.M))
    assert len(heads) == 1, 'unique Boolean real-IL definition required: ' + name
    start = end = heads[0].end()
    depth = 1
    while depth:
        assert end < len(text)
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    body = re.sub(r'\s+', '', text[start:end-1]).replace('[[maybe_unused]]', '')
    body = re.sub(r'(?:t_System_Globalization_DateTimeFormatInfo\*|int32_t)\w+;', '', body)
    grammar = (r'(?P<receiver>\w+)=a0;(?P<checked>\w+)=(?P=receiver);dn2cpp_null_check\((?P=checked)\);'
        r'(?P<flags>\w+)=0;(?P<bit>\w+)=' + str(bit) + r';(?P<masked>\w+)=(?P=flags)&(?P=bit);'
        r'(?P<zero>\w+)=0;(?P<result>\w+)=\(\(\(uint32_t\)(?P=masked)\)>\(\(uint32_t\)(?P=zero)\)\)\?1:0;'
        r'return(?P=result);')
    assert re.fullmatch(grammar, body), name + ': lost real FormatFlags/mask/cgt.un chain: ' + body
    print('dtfi-parser-flags-real-il:' + name + ':mask=' + str(bit))
if a.minimal:
    assert not re.search(r'Program_(?:Metadata|Family|ReflectionRow|(?:Pointer|Virtual|Address|Producer)(?:Month|Day|Adjustment))_m\d+\(', text), 'minimal retained alternate roots'
