#!/usr/bin/env python3
"""Focused emitted wrapper proof; exceptional boxing is intentionally permitted."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
parser.add_argument('--name', choices=('abbreviated', 'full'), default='abbreviated')
args = parser.parse_args()
target = 'GetDayName' if args.name == 'full' else 'GetAbbreviatedDayName'
names = ('Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday') if args.name == 'full' else ('Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat')
text = '\n'.join(p.read_text() for p in sorted(args.output.glob('generated*.cpp')))
heads = list(re.finditer(r'^Dn2CppString\* DateTimeFormatInfo_' + target + r'_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'exactly one callable abbreviated-day wrapper definition required'
start = heads[0].end()
depth = 1
end = start
while depth:
    assert end < len(text), 'unclosed wrapper'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = text[start:end - 1]
for forbidden in ('dn2cpp_dtfi_invariant_abbrev_day_names', 'dn2cpp_dtfi_invariant_day_names', 'dn2cpp_string_from_utf8', 'dn2cpp_newarr', 'dn2cpp_string_literal'):
    assert forbidden not in body, 'successful scalar body must not call ' + forbidden
cases = re.findall(r'case (\d+): (\w+) = str_(\d+); break;', body)
assert [int(c[0]) for c in cases] == list(range(7)), 'exact seven literal cases required'
assert len({c[1] for c in cases}) == 1, 'single switch result required'
for (_, _, index), name in zip(cases, names):
    assert re.search(r'static const char16_t str_data_' + index + r'\[\] = u"' + name + r'";', text), 'literal bytes missing: ' + name
    assert re.search(r'Dn2CppString\* str_' + index + r';', text), 'global literal slot missing'
    assert re.search(r'str_' + index + r' = dn2cpp_string_literal\(str_data_' + index + r', ' + str(len(name)) + r'\);', text), 'literal initialization missing'
assert body.index('dn2cpp_null_check(') < body.index('if ((uint32_t)'), 'receiver check must precede range'
assert 'dn2cpp_box(&ti_System_DayOfWeek,' in body, 'actual value must box the actual enum'
box = re.search(r'(\w+) = dn2cpp_box\(&ti_System_DayOfWeek,', body)
formatted = re.search(r'(\w+) = dn2cpp_format_int\([^;]+;', body)
assert box and formatted, 'staged box and formatted text required'
barrier = 'DN2CPP_WEB_GC_LIVENESS(' + box[1] + ');'
throw = re.search(r'dn2cpp_throw_argument_out_of_range_actual\([^;]+;', body)
assert throw and box.end() < formatted.start() < body.index(barrier) < throw.start(), 'box must remain live across formatting'
assert re.search(r',\s*' + box[1] + r',\s*' + formatted[1] + r'\);$', throw[0]), 'throw must consume staged exact box and text'

assert len(re.findall(r'^const Dn2CppTypeInfo ti_System_DayOfWeek = ', text, re.M)) == 1, 'enum type identity definition missing'
if args.minimal:
    assert 'AbbreviatedDaySubset' not in text and 'FullDaySubset' not in text, 'minimal app contaminated with full test sample'
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|Validate)_m\d+', text), 'minimal app contains auxiliary test roots'
print('abbreviated-day-generated:seven-rooted-literals-and-enum-identity')
