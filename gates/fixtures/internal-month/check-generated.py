#!/usr/bin/env python3
"""Focused emitted wrapper proof; exceptional boxing is intentionally permitted."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
target = 'InternalGetMonthName'
names = ('January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December', '')
short_names = ('Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec', '')
sources = sorted(args.output.glob('generated*.cpp'))
header = args.output / 'generated.h'
if header.exists():
    sources.append(header)
text = '\n'.join(p.read_text() for p in sources)
heads = list(re.finditer(r'^(?:inline )?Dn2CppString\* DateTimeFormatInfo_' + target + r'_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'exactly one callable internal-month wrapper definition required'
start = heads[0].end()
depth = 1
end = start
while depth:
    assert end < len(text), 'unclosed wrapper'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = text[start:end - 1]
for forbidden in ('dn2cpp_dtfi_invariant_abbrev_month_names', 'dn2cpp_dtfi_invariant_month_names', 'dn2cpp_string_from_utf8', 'dn2cpp_newarr', 'dn2cpp_string_literal'):
    assert forbidden not in body, 'successful scalar body must not call ' + forbidden
cases = re.findall(r'case (\d+): (\w+) = \((\w+) != 2 && (\w+) != 0\) \? str_(\d+) : str_(\d+); break;', body)
assert [int(c[0]) for c in cases] == list(range(1, 14)), 'thirteen exact selector cases'
assert len({(c[1], c[2], c[3]) for c in cases}) == 1, 'same result/style/abbreviation operands in every case'
for case, full, short in zip(cases, names, short_names):
    for index, name in ((case[4], short), (case[5], full)):
        assert re.search(r'static const char16_t str_data_' + index + r'\[\] = u"' + name + r'";', text), 'literal bytes: ' + name
        assert re.search(r'Dn2CppString\* str_' + index + r';', text), 'global literal slot'
        assert re.search(r'str_' + index + r' = dn2cpp_string_literal\(str_data_' + index + r', ' + str(len(name)) + r'\);', text), 'literal initialization'
assert body.index('dn2cpp_null_check(') < body.index('if ((uint32_t)'), 'receiver check must precede range'
assert 'dn2cpp_box(&dn2cpp_int32_type,' in body, 'actual value must box the original Int32'
box = re.search(r'(\w+) = dn2cpp_box\(&dn2cpp_int32_type,', body)
formatted = re.search(r'(\w+) = dn2cpp_format_int\([^;]+;', body)
assert box and formatted, 'staged box and formatted text required'
barrier = 'DN2CPP_WEB_GC_LIVENESS(' + box[1] + ');'
throw = re.search(r'dn2cpp_throw_argument_out_of_range_actual\([^;]+;', body)
assert throw and box.end() < formatted.start() < body.index(barrier) < throw.start(), 'box must remain live across formatting'
assert re.search(r',\s*' + box[1] + r',\s*' + formatted[1] + r'\);$', throw[0]), 'throw must consume staged exact box and text'

if args.minimal:
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|Validate)_m\d+', text), 'minimal app contains auxiliary test roots'

index = re.search(r'if \(\(uint32_t\)(\w+) - 1u >= 13u\)', body)
assert index, 'overflow-safe unsigned range required'
assert re.search(r'dn2cpp_box\(&dn2cpp_int32_type, &' + index[1] + r', sizeof\(' + index[1] + r'\)\)', body), 'box must use original argument, not decremented index'
assert re.search(r'dn2cpp_format_int\(' + index[1] + r', 4, nullptr\)', body), 'format original argument'

print('internal-month-generated:thirteen-rooted-literals-and-original-int32')
