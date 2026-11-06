#!/usr/bin/env python3
"""Prove the exact unconditional Calendar refusal body, including inline headers."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
paths = sorted(args.output.glob('generated*.cpp')) + [args.output / 'generated.h']
text = '\n'.join(p.read_text() for p in paths)
heads = list(re.finditer(
    r'^(?:inline )?t_System_Globalization_Calendar\* DateTimeFormatInfo_get_Calendar_m\d+'
    r'\(t_System_Globalization_DateTimeFormatInfo\* (?P<arg>\w+)\)\n\{', text, re.M))
assert len(heads) == 1, 'exact typed retained getter definition required'
start = end = heads[0].end()
depth = 1
while depth:
    assert end < len(text), 'unclosed getter'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = ' '.join(text[start:end - 1].split())
# An exact statement sequence rejects branches, payload reads, allocations and
# early success returns, rather than merely checking the order of selected tokens.
shape = (
    r'\[\[maybe_unused\]\] t_System_Globalization_DateTimeFormatInfo\* (?P<receiver>\w+); '
    r'\[\[maybe_unused\]\] t_System_Globalization_Calendar\* (?P<result>\w+); '
    r'(?P=receiver) = ' + re.escape(heads[0]['arg']) + r'; '
    r'dn2cpp_null_check\((?P=receiver)\); '
    r'dn2cpp_throw_platform_not_supported\("DateTimeFormatInfo.Calendar is not supported"\); '
    r'(?P=result) = nullptr; return (?P=result);'
)
match = re.fullmatch(shape, body)
assert match and match['receiver'] != match['result'], 'exact null-first unconditional typed refusal required'
if args.minimal:
    assert 'DtfICalendarSubset' not in text, 'minimal fixture rooted full sample'
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address)_m\d+', text), 'minimal fixture has auxiliary roots'
print('dtfi-calendar-generated:null-first-unconditional-typed-refusal')
