#!/usr/bin/env python3
"""Check the addressable body, its branch-specific receiver policy and fresh arrays."""
import argparse
import pathlib
import re

parser = argparse.ArgumentParser()
parser.add_argument('output', type=pathlib.Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
text = '\n'.join(path.read_text() for path in sorted(args.output.glob('generated*.cpp')) + [args.output / 'generated.h'])
heads = list(re.finditer(
    r'^(?:inline )?Dn2CppArrayRef\* DateTimeFormatInfo_GetAllDateTimePatterns_m\d+'
    r'\(t_System_Globalization_DateTimeFormatInfo\* (?P<receiver>\w+), int32_t (?P<format>\w+)\)\n\{', text, re.M))
assert len(heads) == 1, 'one precise String[](Char) definition required'
head = heads[0]
start = end = head.end()
depth = 1
while depth:
    assert end < len(text), 'unclosed target'
    depth += (text[end] == '{') - (text[end] == '}')
    end += 1
body = ' '.join(text[start:end - 1].split())
# These declarations carry no behavior; all executable statements below are matched.
body = re.sub(r'\[\[maybe_unused\]\] (?:uint16_t|t_System_Globalization_DateTimeFormatInfo\*|Dn2CppArrayRef\*|Dn2CppString\*) \w+; ', '', body)
opening = re.match(r'(?P<format>\w+) = \(uint16_t\)\(' + re.escape(head['format'])
    + r'\); (?P<receiver>\w+) = ' + re.escape(head['receiver']) + r'; switch \((?P=format)\) \{ ', body)
assert opening, 'raw addressable body must not unconditionally null-check the receiver'
format_name = opening['format']
receiver = opening['receiver']
remaining = body[opening.end():]

short_dates = ['MM/dd/yyyy', 'yyyy-MM-dd']
long_dates = ['dddd, dd MMMM yyyy']
short_times = ['HH:mm', 'hh:mm tt', 'H:mm', 'h:mm tt']
long_times = ['HH:mm:ss']
def product(dates, times):
    return [date + ' ' + time for date in dates for time in times]
groups = [
    ('d', True, short_dates), ('D', True, long_dates),
    ('t', True, short_times), ('T', True, long_times),
    ('yY', True, ['yyyy MMMM']), ('mM', True, ['MMMM dd']),
    ('f', True, product(long_dates, short_times)), ('FU', True, product(long_dates, long_times)),
    ('g', True, product(short_dates, short_times)), ('G', True, product(short_dates, long_times)),
    ('oO', False, ["yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK"]),
    ('rR', False, ["ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"]),
    ('s', False, ["yyyy'-'MM'-'dd'T'HH':'mm':'ss"]),
    ('u', False, ["yyyy'-'MM'-'dd HH':'mm':'ss'Z'"])
]

def literal(symbol, expected):
    index = symbol.removeprefix('str_')
    assert re.search(r'static const char16_t str_data_' + index + r'\[\] = u"' + re.escape(expected) + r'";', text), 'wrong rooted literal ' + expected
    assert re.search(r'\b' + symbol + r' = dn2cpp_string_literal\(str_data_' + index + ', ' + str(len(expected)) + r'\);', text), 'literal length/initialization'

array = None
for codes, reads_receiver, patterns in groups:
    prefix = ''.join('case ' + str(ord(code)) + ': ' for code in codes) + '{ '
    assert remaining.startswith(prefix), 'case order or aliases changed: ' + codes
    remaining = remaining[len(prefix):]
    close = remaining.index(' } ')
    arm = remaining[:close]
    remaining = remaining[close + 3:]
    if reads_receiver:
        check = 'dn2cpp_null_check(' + receiver + '); '
        assert arm.startswith(check), 'dependent arm needs receiver check: ' + codes
        arm = arm[len(check):]
    allocation = re.match(r'(?P<array>\w+) = dn2cpp_newarr_ref_t\(' + str(len(patterns)) + r', &ti_arr_String\); ', arm)
    assert allocation, 'fresh precise String[] allocation required: ' + codes
    if array is None:
        array = allocation['array']
    assert array == allocation['array'], 'common result variable'
    arm = arm[allocation.end():]
    for index, expected in enumerate(patterns):
        store = re.match(r'dn2cpp_gc_store_ref\(&' + re.escape(array) + r'->data\[' + str(index) + r'\], \(Dn2CppObject\*\)(str_\d+)\); ', arm)
        assert store, 'ordered GC store required: ' + codes
        literal(store[1], expected)
        arm = arm[store.end():]
    assert arm == 'break;', 'unexpected Calendar access/allocation/store in arm: ' + codes

default = re.fullmatch(
    r'default: \{ (?P<character>\w+) = dn2cpp_char_to_string\(\(char16_t\)' + re.escape(format_name) + r'\); '
    r'(?P<message>\w+) = dn2cpp_string_concat3\((?P<prefix>str_\d+), (?P=character), (?P<suffix>str_\d+)\); '
    r'(?:DN2CPP_WEB_GC_LIVENESS\((?P=character)\); )?'
    r'dn2cpp_throw_argument_message\((?P=message), "format"\); \} \} '
    r'(?P<result>\w+) = ' + re.escape(array) + r'; return (?P=result);', remaining)
assert default, 'UTF16 invalid-specifier error must precede any receiver check'
literal(default['prefix'], "Format specifier '")
literal(default['suffix'], "' was invalid.")
assert re.search(r'\b(?:const )?Dn2CppTypeInfo ti_arr_String\s*=', text), 'precise String[] TypeInfo definition'
if args.minimal:
    assert not re.search(r'Program_(Expected|Valid|Metadata|Reflect|Pointer|Address|DelegateRoute|RawNull)_m\d+', text), 'minimal has alternate roots'
print('all-datetime-patterns-generated:19-cases-fresh-precise-ordered-gc-stores')
