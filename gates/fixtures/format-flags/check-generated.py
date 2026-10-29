#!/usr/bin/env python3
"""Actual enum identity and a null-first, side-effect-free retained getter."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
sources = sorted(args.output.glob('generated*.cpp'))
header = args.output / 'generated.h'
if header.exists():
    sources.append(header)
text = '\n'.join(p.read_text() for p in sources)
heads = list(re.finditer(r'^(?:inline )?int32_t DateTimeFormatInfo_get_FormatFlags_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'unique full enum-storage getter definition required'
start = end = heads[0].end()
depth = 1
while depth:
    assert end < len(text), 'unclosed getter'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = text[start:end - 1]
normalized = re.sub(r'\s+', '', body)
grammar = (
    r'(?:\[\[maybe_unused\]\])?t_System_Globalization_DateTimeFormatInfo\*(\w+);'
    r'(?:\[\[maybe_unused\]\])?int32_t(\w+);'
    r'\1=a0;dn2cpp_null_check\(\1\);\2=0;return\2;'
)
match = re.fullmatch(grammar, normalized)
assert match and match[1] != match[2], 'getter must unconditionally copy/check receiver then materialize/return None0; no branches or side effects'
identity = 'ti_System_Globalization_DateTimeFormatFlags'
assert len(re.findall(r'^const Dn2CppTypeInfo ' + identity + r' = ', text, re.M)) == 1, 'actual enum TypeInfo definition missing'
if args.minimal:
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|Validate)_m\d+', text), 'minimal has auxiliary boxing/reflection/address roots'
print('format-flags-generated:null-first-none-and-actual-enum-identity')
