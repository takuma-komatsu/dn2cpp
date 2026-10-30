#!/usr/bin/env python3
"""Exact typed singleton initializer and getter closure, not general CultureInfo ABI proof."""
import argparse
from pathlib import Path
import re
p = argparse.ArgumentParser()
p.add_argument('output', type=Path)
p.add_argument('--mode', choices=['all', 'reflection', 'minimal', 'minimal-culture'], default='all')
a = p.parse_args()
paths = sorted(a.output.glob('generated*.cpp'))
if (a.output / 'generated.h').exists(): paths.append(a.output / 'generated.h')
text = '\n'.join(path.read_text() for path in paths)
def extract(match):
    start = end = match.end(); depth = 1
    while depth:
        assert end < len(text)
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end - 1]
heads = list(re.finditer(r'^(t_\w+)\* (dn2cpp_invariant_compare_info_\w+)\(\)\n\{', text, re.M))
assert len(heads) == 1, 'one external non-inline factory definition for actual CompareInfo type'
head = heads[0]; typ, factory = head[1], head[2]; body = extract(head)
assert re.search(r'^' + re.escape(typ) + r'\* ' + factory + r'\(\);$', text, re.M), 'shared declaration'
assert re.search(r'struct ' + typ + r' : Dn2CppObject\s*\{[^}]*Dn2CppString\* f_m_name;[^}]*Dn2CppString\* f__sortName;', text, re.S), 'real named managed String fields'
expected = [
    'static std::mutex& mutex = dn2cpp_never_destroyed<std::mutex>();',
    'static ' + typ + '** root = nullptr;',
    'std::lock_guard<std::mutex> lock(mutex);',
    'if (root != nullptr && *root != nullptr) return *root;',
    'if (root == nullptr) root = static_cast<' + typ + '**>(dn2cpp_alloc_pinned(sizeof(' + typ + '*)));',
    'Dn2CppString* empty = dn2cpp_string_literal(u"", 0);',
    'auto* value = static_cast<' + typ + '*>(dn2cpp_alloc(sizeof(' + typ + ')));',
]
normalized = [' '.join(line.split()) for line in body.splitlines() if line.strip()]
assert normalized[:len(expected)] == expected, 'lock/root-before-allocation/empty-before-object order'
remaining = normalized[len(expected):]
assert len(remaining) == 5
assert re.fullmatch(r'value->type = &\w+;', remaining[0]), 'actual precise header'
assert remaining[1:] == ['dn2cpp_gc_store_ref(&value->f_m_name, empty);', 'dn2cpp_gc_store_ref(&value->f__sortName, empty);', 'dn2cpp_gc_store_ref(root, value);', 'return value;'], 'initialize both fields before complete publication'
for forbidden in ('cctor', '_ctor', 'InitSort', 'Invariant', 'SearchValues'):
    assert forbidden not in body, 'no managed/cctor/cache recursion in initializer'
owners = ['DateTimeFormatInfo'] if a.mode == 'minimal' else ['CultureInfo'] if a.mode == 'minimal-culture' else ['DateTimeFormatInfo', 'CultureInfo']
for owner in owners:
    matches = list(re.finditer(r'^(?:inline )?' + typ + r'\* ' + owner + r'_get_CompareInfo_m\d+\([^\n]+\)\n\{', text, re.M))
    assert len(matches) == 1, 'retained actual getter wrapper: ' + owner
    value = extract(matches[0]); call = value.index(factory + '()')
    null = re.search(r'if \(\w+ == nullptr\) dn2cpp_throw_null_reference\(\);', value)
    assert null and null.end() < call, 'receiver null before cache acquisition'
    assert value.count(factory + '()') == 1
    assert not re.search(r'dn2cpp_alloc|->', value), 'wrapper delegates allocation and does not dereference headerless CultureInfo'
if a.mode == 'all':
    assert re.search(r'DN2CPP_NOINLINE int32_t Program_DropReferences_m\d+\(', text), 'lifetime installer frame must die before collection'
    assert text.count('dn2cpp_gc_collect();') >= 2
if a.mode in ('reflection', 'minimal', 'minimal-culture'):
    assert not re.search(r'Program_(Address|Pointer|VirtualAddress|Virtual)(DtfI|Culture)_m\d+', text), 'independent mode has address roots'
    assert not re.search(r'Program_(DtfIReceiver|CultureReceiver)_m\d+', text), 'independent mode has producer roots'
if a.mode == 'reflection':
    assert text.count('dn2cpp_set_culture_compare_info_row(') == 1, 'exact runtime-owned CultureInfo row registration'
    assert not re.search(r'Program_Direct(DtfI|Culture)_m\d+', text), 'reflection mode has direct roots'
if a.mode.startswith('minimal'):
    assert not re.search(r'Program_(Metadata|ReflectionRow|OverrideRoutes|Lifetime|DropReferences)_m\d+', text), 'minimal has auxiliary roots'
    other = 'Culture' if a.mode == 'minimal' else 'DtfI'
    assert not re.search(r'Program_Direct' + other + r'_m\d+', text), 'minimal roots other getter through application helper'
print('dtfi-compare-info-generated:actual-fields-typed-root-null-first')
