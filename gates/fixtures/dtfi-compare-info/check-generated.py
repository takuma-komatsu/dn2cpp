#!/usr/bin/env python3
"""Exact singleton and same-symbol CompareInfo initializers, not general CultureInfo ABI proof."""
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
    def method_body(prefix, exact=False):
        symbol = re.escape(prefix) + ('' if exact else r'_m\d+')
        matches = list(re.finditer(r'^[^\n;{}]*\b(' + symbol + r')\([^\n;]*\)\n\{', text, re.M))
        assert len(matches) == 1, 'one emitted body, not just a declaration: ' + prefix
        return matches[0][1], extract(matches[0])

    # Bind through actual call sites so an unrelated body/declaration cannot satisfy the assertions.
    ctor_caller = method_body('Program_CallCtor')[1]
    ctor_call = re.search(r'\b(CompareInfo__ctor_m\d+)\(', ctor_caller)
    init_caller = method_body('Program_DirectInitSort')[1]
    init_call = re.search(r'\b(CompareInfo_InitSort_m\d+)\(', init_caller)
    assert ctor_call and init_call, 'fixture calls actual installed initializer symbols'
    ctor_symbol, ctor_body = method_body(ctor_call[1], exact=True)
    init_symbol, init_body = method_body(init_call[1], exact=True)
    assert ctor_call[1] == ctor_symbol and init_call[1] == init_symbol, 'direct calls bind the checked bodies'
    assert re.search(r'\b' + re.escape(ctor_symbol) + r'\(', method_body('Program_CreateCompareInfo')[1]), 'newobj invokes the same constructor body'
    pointer = method_body('Program_PointerInitSort')[1]
    assert re.search(r'&' + re.escape(init_symbol) + r'\b', pointer) and 'void (*)(' in pointer, 'calli addresses the same InitSort body'
    for label, value in [('ctor', ctor_body), ('InitSort', init_body)]:
        guard = value.find('dn2cpp_compare_info_require_native_culture(')
        assert guard >= 0 and value.count('dn2cpp_compare_info_require_native_culture(') == 1, label + ' validates the native payload once'
        assert guard < value.index('dn2cpp_culture_'), label + ' rejects managed subclasses before headerless access'
        sort = re.search(r'(\w+) = dn2cpp_culture_sort_name\(\(\(const Dn2CppNumberFormatInfo\*\)a1\)\);', value)
        assert sort and value.count('dn2cpp_culture_sort_name(') == 1, label + ' evaluates the native SortName once'
        store = re.search(r'dn2cpp_gc_store_ref\(&[^;\n]*->f__sortName, ' + re.escape(sort[1]) + r'\);', value)
        flag = re.search(r'->f__isAsciiEqualityOrdinal = true;', value)
        assert store and flag and sort.end() < store.start() < flag.start(), label + ' GC-stores the actual sort field before its ASCII flag'
        assert typ in value and 'f__sortHandle' not in value, label + ' owns managed fields and leaves sort handle unchanged'
        assert factory not in value and not re.search(r'CompareInfo_(?:_ctor|InitSort)_m\d+\(', value), 'no factory or initializer recursion'
    legacy = re.search(r'(\w+) = dn2cpp_culture_tostring\(', ctor_body)
    name_store = re.search(r'dn2cpp_gc_store_ref\(&[^;\n]*->f_m_name, (\w+)\);', ctor_body)
    assert legacy and name_store and legacy[1] == name_store[1] and legacy.end() < name_store.start() < ctor_body.index('dn2cpp_culture_sort_name('), 'constructor stores full legacy identity before SortName'
    assert 'f_m_name' not in init_body and 'dn2cpp_culture_tostring(' not in init_body, 'InitSort preserves existing m_name'
    seed = method_body('Program_SeedCompareInfo')[1]
    for field in ['m_name', '_sortName']:
        assert re.search(r'->f_' + field + r' = ', seed), 'fixture seeds actual managed reference field ' + field
    assert seed.count('dn2cpp_gc_write_barrier(') >= 2, 'ordinary field IL retains reference-store barriers'
    for reader, field in [('ReadLegacyName', 'm_name'), ('ReadSortName', '_sortName'), ('ReadAsciiOrdinal', '_isAsciiEqualityOrdinal'), ('ReadSortHandle', '_sortHandle')]:
        assert '->f_' + field in method_body('Program_' + reader)[1], 'fixture observes actual initializer field ' + field
    assert re.search(r'DN2CPP_NOINLINE [^\n]* Program_InitializerForCollection_m\d+\(', text), 'initializer culture/name frame dies before collection'
    allocation = method_body('Program_MakeInitializerCulture')[1]
    assert 'dn2cpp_get_uninitialized_object(' in allocation and not re.search(r'CultureInfo__ctor_m\d+\(', allocation), 'managed boundary fixture never invokes a headerless base constructor'
    validator_call = re.search(r'\b(CultureInfo_VerifyCultureName_m\d+)\(', method_body('Program_DirectValidateCultureName')[1])
    assert validator_call, 'direct validator uses the genuine same-symbol body'
    validator_symbol, validator_body = method_body(validator_call[1], exact=True)
    pointer = method_body('Program_PointerValidateCultureName')[1]
    assert re.search(r'&' + re.escape(validator_symbol) + r'\b', pointer) and 'int32_t (*)(' in pointer, 'validator calli addresses the checked body'
    for operation in ['dn2cpp_string_get_char(', 'dn2cpp_char_unicode_category(', 'dn2cpp_string_format1(', 'dn2cpp_throw(']:
        assert operation in validator_body, 'validator retains original Unicode and exception IL: ' + operation
    assert '->length' in validator_body and not re.search(r'->f_|dn2cpp_culture_', validator_body), 'validator is field-free String validation, not CultureInfo payload access'
    assert text.count('dn2cpp_set_culture_name_validator_row(') == 1, 'exact runtime-owned validator row registration'
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
