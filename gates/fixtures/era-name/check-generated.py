#!/usr/bin/env python3
"""Exact retained era getter body and rooted invariant name."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
parser.add_argument('--abbreviated', action='store_true')
args = parser.parse_args()
target = 'GetAbbreviatedEraName' if args.abbreviated else 'GetEraName'
expected = 'AD' if args.abbreviated else 'A.D.'
sources = sorted(args.output.glob('generated*.cpp'))
header = args.output / 'generated.h'
if header.exists():
    sources.append(header)
text = '\n'.join(p.read_text() for p in sources)
heads = list(re.finditer(r'^(?:inline )?Dn2CppString\* DateTimeFormatInfo_' + target + r'_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'unique full getter definition (not declaration)'
start = end = heads[0].end()
depth = 1
while depth:
    assert end < len(text), 'unclosed getter'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = text[start:end - 1]
for token in ('dn2cpp_newarr', 'dn2cpp_string_from_utf8', 'dn2cpp_string_literal', 'get_Calendar', '->f_', 'dn2cpp_dtfi_invariant'):
    assert token not in body, 'no Calendar/backing array/name construction: ' + token
indices = set(re.findall(r'\bstr_(\d+)\b', body))
assert len(indices) == 1, 'one rooted era literal'
index = next(iter(indices))
assert re.search(r'static const char16_t str_data_' + index + r'\[\] = u"' + re.escape(expected) + r'";', text), 'exact A.D. literal'
assert re.search(r'Dn2CppString\* str_' + index + r';', text), 'global literal slot'
assert re.search(r'str_' + index + r' = dn2cpp_string_literal\(str_data_' + index + r', ' + str(len(expected)) + r'\);', text), 'exact literal length'
condition = re.search(r'if \((\w+) != 0 && \1 != 1\)', body)
assert condition, 'only invariant current era0 and era1 accepted, no subtraction overflow'
assert body.index('dn2cpp_null_check(') < condition.start(), 'null before era validation'
box = re.search(r'(\w+) = dn2cpp_box\(&dn2cpp_int32_type, &' + condition[1] + r', sizeof\(' + condition[1] + r'\)\);', body)
formatted = re.search(r'(\w+) = dn2cpp_format_int\(' + condition[1] + r', 4, nullptr\);', body)
assert box and formatted, 'box and format original era Int32'
barrier = 'DN2CPP_WEB_GC_LIVENESS(' + box[1] + ');'
throw = re.search(r'dn2cpp_throw_argument_out_of_range_actual\("Era value was not valid\.", "era", ' + box[1] + r', ' + formatted[1] + r'\);', body)
assert throw and box.end() < formatted.start() < body.index(barrier) < throw.start(), 'exact era resource and same-box liveness'
if args.minimal:
    assert not re.search(r'Program_(Metadata|Reflect|Virtual|Pointer|Address|Delegate|DelegateRoute|Validate)(Abbreviated)?_m\d+', text), 'minimal has auxiliary roots'
print('era-name-generated:rooted-current-era-and-original-int32')
