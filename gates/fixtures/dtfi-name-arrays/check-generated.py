#!/usr/bin/env python3
"""Finite seven-method cache/index proof; bounds and exceptional payload remain exact."""
import argparse
from pathlib import Path
import re

p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--minimal', action='store_true')
p.add_argument('--reflection', action='store_true')
p.add_argument('--scalar', choices=['abbreviated', 'full', 'internal'])
a = p.parse_args()
sources = sorted(a.output.glob('generated*.cpp'))
if (a.output / 'generated.h').exists(): sources.append(a.output / 'generated.h')
text = '\n'.join(path.read_text() for path in sources)

def body(name, result):
    heads = list(re.finditer(r'^(?:inline )?' + re.escape(result) + r' DateTimeFormatInfo_' + name + r'_m\d+\([^\n]+\)\n\{', text, re.M))
    assert len(heads) == 1, 'unique actual wrapper: ' + name
    start = heads[0].end(); end = start; depth = 1
    while depth:
        assert end < len(text)
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end - 1]

def precise_ti(symbol):
    assert symbol != 'dn2cpp_array_ref_type', 'generic ref-array TI is insufficient'
    assert re.search(r'const Dn2CppTypeInfo ' + re.escape(symbol) + r'\s*=', text), 'precise array TI definition missing'

def scalar(mode):
    name = {'full':'GetMonthName', 'abbreviated':'GetAbbreviatedMonthName', 'internal':'InternalGetMonthName'}[mode]
    value = body(name, 'Dn2CppString*')
    index = re.search(r'if \(\(uint32_t\)(\w+) - 1u >= 13u\)', value)
    assert index, 'exact overflow-safe range 1..13'
    assert value.index('dn2cpp_null_check(') < index.start(), 'null receiver before range'
    original = index[1]
    assert re.search(r'dn2cpp_box\(&dn2cpp_int32_type, &' + original + r', sizeof\(' + original + r'\)\)', value), 'original Int32 actual value'
    box = re.search(r'(\w+) = dn2cpp_box\(&dn2cpp_int32_type,', value)
    formatted = re.search(r'(\w+) = dn2cpp_format_int\(' + original + r', 4, nullptr\);', value)
    throw = re.search(r'dn2cpp_throw_argument_out_of_range_actual\([^;]+;', value)
    assert box and formatted and throw
    barrier = 'DN2CPP_WEB_GC_LIVENESS(' + box[1] + ');'
    assert box.end() < formatted.start() < value.index(barrier) < throw.start()
    assert re.search(r',\s*' + box[1] + r',\s*' + formatted[1] + r'\);$', throw[0])
    selector = r'\((\w+) != 2 && (\w+) != 0\)' if mode == 'internal' else ('true' if mode == 'abbreviated' else 'false')
    cache = re.search(r'(\w+) = dn2cpp_dtfi_invariant_month_names\(&(\w+), ' + selector + r'\);', value)
    assert cache and throw.end() < cache.start(), 'cache only after null/range handling'
    precise_ti(cache[2])
    assert value.count('dn2cpp_dtfi_invariant_month_names(') == 1
    assert re.search(r'\(Dn2CppString\*\)' + cache[1] + r'->data\[' + original + r' - 1\]', value), 'stored slot including null and index13'
    assert not re.search(r'\bswitch\b|\bcase\b|str_\d+\s*;|dn2cpp_string_from_utf8|dn2cpp_newarr', value), 'no literal fallback or per-call name array'
    tail = value[cache.end():]
    assert re.fullmatch(r'\s*(\w+) = \(Dn2CppString\*\)' + cache[1] + r'->data\[' + original + r' - 1\];\s*return \1;\s*', tail), 'successful tail must return exact stored element'

if a.scalar:
    scalar(a.scalar)
else:
    for name, selector, clone in [('InternalGetGenitiveMonthNames', r'\w+', False), ('InternalGetLeapYearMonthNames', '0', False)]:
        value = body(name, 'Dn2CppArrayRef*')
        cache = re.search(r'(\w+) = dn2cpp_dtfi_invariant_month_names\(&(\w+), ' + selector + r' != 0\);', value)
        assert cache and value.index('dn2cpp_null_check(') < cache.start()
        precise_ti(cache[2])
        assert value.count('dn2cpp_dtfi_invariant_month_names(') == 1
        assert re.fullmatch(r'\s*(\w+) = ' + cache[1] + r';\s*return \1;\s*', value[cache.end():]), 'private method returns exact backing identity'
        assert not re.search(r'dn2cpp_array_clone|dn2cpp_newarr|\bif\b|->', value), 'no receiver field/cache emulation or copy'
    if not a.minimal and not a.reflection:
        # Public wrappers may inline into callers; their exact emitted clone calls
        # are checked by semantic freshness tests and the bounded source review.
        assert 'dn2cpp_array_clone_ref(' in text, 'public clone path not retained'
        assert re.search(r'DN2CPP_NOINLINE void Program_InstallLifetimeMarker_m\d+\(', text), 'GC marker installation must not inline into collecting caller'
        assert text.count('dn2cpp_gc_collect();') >= 2, 'explicit collection before and after allocation pressure'
if a.minimal and a.scalar:
    assert not re.search(r'Program_(Metadata|Reflect|Pointer|Address|Validate)_m\d+', text), 'scalar minimal auxiliary roots'
    if a.scalar in ('abbreviated', 'full'):
        assert 'AbbreviatedMonthSubset' not in text and 'FullMonthSubset' not in text, 'scalar minimal app contaminated by full sample'
if a.minimal:
    assert not re.search(r'Program_(Metadata|ReflectionRow|PointerGenitive|PointerLeap|AddressGenitive|AddressLeap|Mutation|Ownership)_m\d+', text), 'minimal auxiliary roots'
print('dtfi-name-arrays-generated:typed-cache-and-exact-contract')
