#!/usr/bin/env python3
"""Require snapshot-before-write and exact per-store null/alias semantics."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
args = parser.parse_args()
text = '\n'.join(path.read_text() for path in sorted(args.output.glob('generated*.cpp')) + [args.output / 'generated.h'])
heads = list(re.finditer(r'^(?:inline )?void DateOnly_Deconstruct_m\d+\(Dn2CppDateOnly\* (?P<self>\w+), int32_t\* (?P<year>\w+), int32_t\* (?P<month>\w+), int32_t\* (?P<day>\w+)\)\n\{', text, re.M))
assert len(heads) == 1, 'one exact retained byref DateOnly Deconstruct wrapper'
head = heads[0]
start = end = head.end()
depth = 1
while depth:
    assert end < len(text), 'unclosed target'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = ' '.join(text[start:end - 1].split())
body = re.sub(r'\[\[maybe_unused\]\] (?:int32_t\*|int32_t|Dn2CppDateOnly|bool) \w+; ', '', body)
shape = (
    r'(?P<dayOut>\w+) = ' + re.escape(head['day']) + r'; '
    r'(?P<monthOut>\w+) = ' + re.escape(head['month']) + r'; '
    r'(?P<yearOut>\w+) = ' + re.escape(head['year']) + r'; '
    r'dn2cpp_null_check\(' + re.escape(head['self']) + r'\); '
    r'(?P<snapshot>\w+) = \(\*\(Dn2CppDateOnly\*\)\(' + re.escape(head['self']) + r'\)\); '
    r'(?P<year>\w+) = dn2cpp_dateonly_year\((?P=snapshot)\); '
    r'(?P<month>\w+) = dn2cpp_dateonly_month\((?P=snapshot)\); '
    r'(?P<day>\w+) = dn2cpp_dateonly_day\((?P=snapshot)\); '
    r'(?P<rollover>\w+) = (?P=month) <= 2; '
    r'dn2cpp_null_check\((?P=yearOut)\); \*(?P=yearOut) = (?P=year) - \((?P=rollover) \? 1 : 0\); '
    r'dn2cpp_null_check\((?P=monthOut)\); \*(?P=monthOut) = (?P=month) \+ \((?P=rollover) \? 12 : 0\); '
    r'dn2cpp_null_check\((?P=dayOut)\); \*(?P=dayOut) = (?P=day); '
    r'if \((?P=rollover)\) \{ \+\+\*(?P=yearOut); \*(?P=monthOut) -= 12; \}(?: return;)?'
)
assert re.fullmatch(shape, body), 'snapshot/partial writes/addressed rollover sequence changed: ' + body
print('dateonly-deconstruct-generated:snapshot-ordered-null-stores-aliased-rollover')
