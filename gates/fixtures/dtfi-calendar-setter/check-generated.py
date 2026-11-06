#!/usr/bin/env python3
"""Require the exact setter boundary, not just a bag of guard tokens."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
paths = sorted(args.output.glob('generated*.cpp')) + [args.output / 'generated.h']
text = '\n'.join(path.read_text() for path in paths)
heads = list(re.finditer(
    r'^(?:inline )?void DateTimeFormatInfo_set_Calendar_m\d+'
    r'\(t_System_Globalization_DateTimeFormatInfo\* (?P<self>\w+), '
    r't_System_Globalization_Calendar\* (?P<value>\w+)\)\n\{', text, re.M))
assert len(heads) == 1, 'one exact Void(Calendar) definition required'
head = heads[0]
start = end = head.end()
depth = 1
while depth:
    assert end < len(text), 'unclosed setter'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = ' '.join(text[start:end - 1].split())
# Managed argument staging is independent of the subsequent validation order.
shape = (
    r'\[\[maybe_unused\]\] t_System_Globalization_DateTimeFormatInfo\* (?P<receiver>\w+); '
    r'(?P=receiver) = ' + re.escape(head['self']) + r'; '
    r'dn2cpp_null_check\((?P=receiver)\); '
    r'if \((?P=receiver)->dn2cpp_read_only\) dn2cpp_throw_invalid_operation_msg\("Instance is read-only\."\); '
    r'if \(\(\(Dn2CppObject\*\)' + re.escape(head['value']) + r'\) == nullptr\) dn2cpp_throw_argument_null_param\("value"\); '
    r'dn2cpp_throw_platform_not_supported\("DateTimeFormatInfo.Calendar is not supported"\);(?: return;)?'
)
assert re.fullmatch(shape, body), 'exact receiver/readonly/null-value/PNSE-only body required: ' + body
if args.minimal:
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|NativeCalendar|Cases)_m\d+', text), 'minimal fixture has auxiliary roots'
print('dtfi-calendar-setter-generated:ordered-void-refusal')
