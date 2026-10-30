#!/usr/bin/env python3
"""Exact null-first rooted pattern return, with no mutable cache machinery."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
parser.add_argument('--long', action='store_true')
args = parser.parse_args()
target = 'get_GeneralLongTimePattern' if args.long else 'get_GeneralShortTimePattern'
expected = 'MM/dd/yyyy HH:mm:ss' if args.long else 'MM/dd/yyyy HH:mm'
sources = sorted(args.output.glob('generated*.cpp'))
header = args.output / 'generated.h'
if header.exists():
    sources.append(header)
text = '\n'.join(p.read_text() for p in sources)
heads = list(re.finditer(r'^(?:inline )?Dn2CppString\* DateTimeFormatInfo_' + target + r'_m\d+\([^\n]+\)\n\{', text, re.M))
assert len(heads) == 1, 'unique full managed String getter definition required'
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
    r'(?:\[\[maybe_unused\]\])?Dn2CppString\*(\w+);'
    r'\1=a0;dn2cpp_null_check\(\1\);\2=str_(\d+);return\2;'
)
match = re.fullmatch(grammar, normalized)
assert match and match[1] != match[2], 'exact unconditional receiver/null/literal-return grammar; no allocation, fields or cache writes'
index = match[3]
assert re.search(r'static const char16_t str_data_' + index + r'\[\] = u"' + re.escape(expected) + r'";', text), 'exact invariant composite pattern'
assert re.search(r'Dn2CppString\* str_' + index + r';', text), 'global rooted literal slot'
assert re.search(r'str_' + index + r' = dn2cpp_string_literal\(str_data_' + index + r', ' + str(len(expected)) + r'\);', text), 'interned literal initialization with actual length'
if args.minimal:
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|Validate|ExpectedPattern)_m\d+', text), 'minimal has auxiliary reflection/address roots'
print('general-short-time-pattern-generated:null-first-rooted-pattern')
