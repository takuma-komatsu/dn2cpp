#!/usr/bin/env python3
"""Split aggregate sections; preserve the former focused all-route witnesses."""
import argparse
import collections
import pathlib

parser = argparse.ArgumentParser()
parser.add_argument('stdout', type=pathlib.Path)
parser.add_argument('sections', type=pathlib.Path)
parser.add_argument('--catalog', type=pathlib.Path, default=pathlib.Path(__file__).with_name('families.tsv'))
args = parser.parse_args()
lines = args.stdout.read_bytes().replace(b'\r\n', b'\n').decode('utf-8').splitlines()
rows = [line.split('\t') for line in args.catalog.read_text().splitlines() if line and not line.startswith('#')]
args.sections.mkdir(parents=True, exist_ok=False)
sections = {}
position = 0
for folder, assembly, namespace, flag in rows:
    assert lines[position] == '== family begin ' + assembly + ' ==', 'missing/reordered family ' + assembly
    position += 1
    end = lines.index('== family end ' + assembly + ' ==', position)
    sections[assembly] = lines[position:end]
    assert end > position and not any(line.endswith(':False') for line in sections[assembly]), 'missing/failed assertions ' + assembly
    (args.sections / (assembly + '.stdout')).write_text('\n'.join(sections[assembly]) + '\n')
    position = end + 1
assert lines[position:] == ['DateTime family aggregate end'], 'unaccounted aggregate output'

def require(assembly, witnesses, count=1):
    actual = collections.Counter(sections[assembly])
    for witness in witnesses:
        assert actual[witness] == count, (assembly, witness, actual[witness], count)

# Public day/month outputs are also exactly compared with their untouched CLR oracles.
require('EraName', ['== Invariant era names ==', 'era-name-pointer-range-2:True', 'era-name-delegate-range-2:True',
    'era-name-reflect-range-2:True', 'era-name-producer-order:True', 'era-name-producer-preempts-null:True', 'era-name-end:48',
    '== Invariant abbreviated era names ==', 'abbreviated-era-name-callvirt-range-2:True',
    'abbreviated-era-name-pointer-range-2:True', 'abbreviated-era-name-delegate-range-2:True',
    'abbreviated-era-name-reflect-range-2:True', 'abbreviated-era-name-producer-order:True',
    'abbreviated-era-name-producer-preempts-null:True', 'abbreviated-era-name-literal-identity:True',
    'abbreviated-era-name-return-abi:True', 'abbreviated-era-name-end:61'])
require('InternalMonth', ['Internal invariant month selector end', 'internal-month-producer-order:True', 'internal-month-producer-throw:True']
    + ['internal-month-' + route + '-names-2-True:True' for route in ['direct', 'calli', 'delegate', 'reflection']])
require('DtfICalendar', ['dtfi-calendar-' + item + ':True' for item in [
    'direct-refused', 'direct-null', 'callvirt-refused', 'callvirt-null', 'calli-refused', 'calli-null',
    'delegate-refused', 'delegate-return-abi', 'culture-calendar-neighbor', 'reflection-wrapped',
    'reflection-refused', 'reflection-null-target', 'reflection-return-abi']]
    + ['DTFI Calendar native capability end', 'dtfi-calendar-reflection-end'])
require('DtfICalendarSetter', ['dtfi-calendar-setter-' + item + ':True' for item in [
    'direct-readonly-null', 'direct-readonly-value', 'direct-mutable-null', 'direct-mutable-value', 'direct-real-calendar-type',
    'direct-null-null', 'direct-null-value', 'producer-null', 'producer-throws', 'receiver-producer-throws',
    'callvirt-mutable-value', 'calli-mutable-value', 'delegate-mutable-value', 'delegate-readonly-null', 'delegate-abi',
    'reflection-readonly-null', 'reflection-mutable-value', 'reflection-null-target', 'reflection-abi']]
    + ['dtfi-calendar-setter-direct-end', 'dtfi-calendar-setter-reflection-end', 'DTFI Calendar setter native capability end'])
require('DtfICalendarSetter', ['dtfi-calendar-setter-wrapped:True'], 4)
for assembly, prefix, title, kind in [
    ('FormatFlags', 'format-flags', 'Invariant format flags', 'enum'),
    ('ForceTwoDigitYears', 'force-two-digit-years', 'Invariant force-two-digit years', 'bool')]:
    require(assembly, [title + ' end'] + [prefix + '-' + item + ':True' for item in [
        'direct-null', 'calli-null', 'receiver-once', 'receiver-throws', 'delegate-return', 'reflection-return', 'reflection-null']])
    require(assembly, [prefix + '-' + route + '-' + item + ':True' for route in ['direct', 'calli', 'delegate', 'reflection']
        for item in ['zero', 'actual-' + kind, 'name']], 2)
for assembly, prefix, title in [
    ('GeneralShortTimePattern', 'general-short-time-pattern', 'Invariant general short-time pattern'),
    ('GeneralLongTimePattern', 'general-long-time-pattern', 'Invariant general long-time pattern'),
    ('DateTimeOffsetPattern', 'datetime-offset-pattern', 'Invariant datetime offset pattern')]:
    require(assembly, [title + ' end'] + [prefix + '-' + item + ':True' for item in [
        'direct-null', 'calli-null', 'rooted-after-collect', 'receiver-once', 'receiver-throws', 'delegate-return', 'reflection-return', 'reflection-null']])
    require(assembly, [prefix + '-' + route + '-' + item + ':True' for route in ['direct', 'calli', 'delegate', 'reflection']
        for item in ['content', 'actual-string', 'rooted-identity']], 2)
for route in ['direct', 'callvirt', 'calli', 'delegate', 'reflection']:
    require('AllDateTimePatterns', ['all-datetime-patterns-' + route + '-' + witness + '-' + code + ':True'
        for code in 'dDfFgGmMoOrRstTuUyY' for witness in ['ordered', 'array', 'isolated', 'gc']])
    require('AllDateTimePatterns', ['all-datetime-patterns-' + route + '-invalid-' + str(code) + ':True'
        for code in [120, 97, 122, 0, 65535, 55296, 57343]])
for route in ['direct', 'calli']:
    require('AllDateTimePatterns', ['all-datetime-patterns-' + route + '-null-constant-' + code + ':True' for code in 'oOrRsu']
        + ['all-datetime-patterns-' + route + '-null-dependent-' + code + ':True' for code in 'dDfFgGmMtTUyY']
        + ['all-datetime-patterns-' + route + '-null-invalid-' + str(code) + ':True' for code in [120, 0, 65535, 55296]])
require('AllDateTimePatterns', ['all-datetime-patterns-' + label + ':True' for label in [
    'producer-constant', 'producer-throws', 'delegate-abi', 'reflection-abi']]
    + ['all-datetime-patterns-' + route + '-null-' + code + ':True' for route in ['reflection', 'callvirt'] for code in 'dsx']
    + ['all-datetime-patterns-direct-end', 'all-datetime-patterns-reflection-end', 'Invariant all DateTime patterns end'])
require('AllDateTimePatterns', ['all-datetime-patterns-reflection-wrapped:True'], 7)
require('DateTimeTChar', ['datetime-tchar-' + route + '-' + witness + ':True'
    for route in ['direct', 'pointer'] for witness in ['old-local-char', 'old-local-byte',
    'old-nested-char', 'old-nested-byte', 'new-char', 'new-byte', 'replaced-old-char',
    'replaced-old-byte', 'empty', 'null']] + ['datetime-tchar-collection-control:True',
    'DateTime TChar span lifetime end'])
require('DateOnlyDeconstruct', ['dateonly-deconstruct-' + witness + ':True' for witness in [
    'delegate-named-identity', 'reflection-flags-static', 'reflection-flags-nonpublic',
    'reflection-wrong-arity', 'reflection-value-parameters', 'reflection-wrong-element',
    'reflection-exact-parameters', 'reflection-abi', 'reflection-null-target',
    'direct-null-receiver', 'pointer-null-receiver', 'producer-0', 'producer-1', 'producer-2']]
    + ['DateOnly Deconstruct reflection end', 'DateOnly Deconstruct alias end'])
print('datetime-family-output:all-sections-and-former-focused-witnesses')
