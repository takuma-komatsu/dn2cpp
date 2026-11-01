#!/usr/bin/env python3
"""Run diagnostic-only dn2cpp measurement and compare observed gap families."""
import argparse
import csv
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import shutil
import subprocess
import sys
import time

SCHEMA = 1
CLASSIFIER = 'operation-hints-v3'
FIELDS = ('phase', 'namespace', 'method', 'exception', 'message', 'cppSymbol', 'pass')
VECTOR = re.compile(r'System\.(?:Runtime\.Intrinsics\.Vector(?:64|128|256|512)?|Numerics\.Vector(?:2|3|4)?)(?![A-Za-z0-9_])')
VECTOR_OP = re.compile(r'(System\.(?:Runtime\.Intrinsics\.Vector(?:64|128|256|512)?|Numerics\.Vector(?:2|3|4)?)(?:<[^\n]*?>)?)(?:::|\.)(?:System\.[^\s:]*?\.)?([A-Za-z_][A-Za-z_0-9]*)')
ARITHMETIC = re.compile(r'System\.(?:MathF?|Half|Single|Double|Decimal|U?Int(?:16|32|64|128)|SByte|Byte|Char|U?IntPtr|Numerics\.(?:BigInteger|Complex))(?![A-Za-z0-9])')
ARITHMETIC_OP = re.compile(r'(System\.(?:MathF?|Half|Single|Double|Decimal|U?Int(?:16|32|64|128)|SByte|Byte|U?IntPtr))(?:::|\.)([A-Za-z_][A-Za-z_0-9]*)')
WIDTH = re.compile(r'System\.(?:Runtime\.Intrinsics\.Vector(64|128|256|512)|Numerics\.Vector(2|3|4)?)(?![A-Za-z0-9])')
UNSUPPORTED_TYPES = ('Decimal', 'Int128', 'UInt128', 'Boolean', 'Char', 'Guid')

SEMANTIC_FAMILIES = {
    'vector': {
        'number-selection': ('MaxNumber', 'MinNumber', 'MaxMagnitude', 'MinMagnitude', 'MaxMagnitudeNumber', 'MinMagnitudeNumber'),
        'trigonometry': ('Sin', 'Cos', 'SinCos', 'Tan', 'Asin', 'Acos', 'Atan', 'Atan2'),
        'angle-conversion': ('DegreesToRadians', 'RadiansToDegrees'),
        'predicate': ('All', 'Any', 'None', 'AllWhereAllBitsSet', 'AnyWhereAllBitsSet', 'NoneWhereAllBitsSet'),
        'copy': ('CopyTo', 'TryCopyTo'),
        'interface-layout': ('get_Alignment',),
    },
    'arithmetic': {
        'parsing': ('Parse', 'TryParse'),
        'numeric-conversion': ('TryConvertToChecked', 'TryConvertToSaturating', 'TryConvertToTruncating',
                               'TryConvertFromChecked', 'TryConvertFromSaturating', 'TryConvertFromTruncating'),
        'endian-read': ('TryReadBigEndian', 'TryReadLittleEndian', 'ReadBigEndian', 'ReadLittleEndian'),
        'floating-point-representation': ('get_Exponent', 'get_Significand'),
        'decimal-binary-conversion': ('GetBytes', 'ToDecimal'),
        'sign': ('Sign',),
    },
}


def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False)


def identity(path):
    p = Path(path).resolve(strict=True)
    return {'path': str(p), 'name': p.name, 'sha256': digest(p)}


def input_identity(path):
    # The compiler resolves references from lexical filenames/directories. Do not
    # silently fingerprint a different load set by following an alias here.
    if Path(os.path.abspath(path)) != Path(path).resolve(strict=True):
        raise ValueError('measurement inputs and compiler paths must not use symlink aliases: ' + str(path))
    return identity(path)


def input_context(command):
    if len(command) < 4 or command[1] != 'exec' or not command[2].endswith('.dll'):
        raise ValueError('command must start with dotnet exec <compiler.dll> <input.dll>')
    unsupported = {'--project-root', '--hotupdate-base', '--hotupdate-refs', '--gdextension',
                   '--dotnet-module', '--emit-patch', '--generate-bindings', '--check-wasm-imports',
                   '--ildiet-output', '--dump-isa-surface'}
    for token in command[4:]:
        if token in unsupported:
            raise ValueError('measurement runner does not bind the external inputs/output or backend scope of ' + token)
    compiler, target = input_identity(command[2]), input_identity(command[3])
    references, policy = [], []
    i = 4
    while i < len(command):
        token = command[i]
        if token in ('-o', '--output'):
            if i + 1 >= len(command):
                raise ValueError('missing output argument')
            i += 2
        elif token == '--measure':
            i += 1
        elif token in ('-r', '--reference'):
            if i + 1 >= len(command):
                raise ValueError('missing reference argument')
            references.append(input_identity(command[i + 1]))
            i += 2
        else:
            # Other file-valued policies (for example link.xml) bind by content.
            policy.append({'file': input_identity(token)} if Path(token).is_file() else token)
            i += 1
    if not any(r['name'] == 'System.Private.CoreLib.dll' for r in references):
        raise ValueError('repeatable measurement requires an explicit System.Private.CoreLib.dll reference')
    compiler_dir = Path(compiler['path']).parent
    compiler_siblings = [identity(p) for p in sorted(compiler_dir.rglob('*.dll'))]
    compiler_configuration = [identity(p) for p in sorted(compiler_dir.rglob('*.json'))]
    companion_hosts = [input_identity(compiler_dir / 'ildiet' / name) for name in ('ILDiet', 'ILDiet.exe')
                       if (compiler_dir / 'ildiet' / name).is_file()]
    host_path = shutil.which(command[0])
    host = identity(host_path) if host_path else {'path': command[0], 'sha256': None}
    semantic_environment = {k: v for k, v in sorted(os.environ.items())
        if ((k.startswith('DN2CPP_') and k != 'DN2CPP_RESTORE_CONFIG')
            or k.startswith(('COMPlus_', 'DOTNET_SYSTEM_', 'DOTNET_Enable', 'DOTNET_Jit')))}
    default_shims = []
    for name in ('DnZlib.dll', 'DnBrotli.dll', 'DnHttp.dll'):
        candidate = compiler_dir / name
        default_shims.append(input_identity(candidate) if candidate.is_file() else {'name': name, 'sha256': None})
    # TranspileDriver also injects this support assembly into the input load set.
    runtime_path = compiler_dir / 'Dn2Cpp.Runtime.dll'
    runtime_support = input_identity(runtime_path) if runtime_path.is_file() else {'name': runtime_path.name, 'sha256': None}
    # Bind sibling DLL contents, not just explicit references: --auto-ref and default
    # shims can change the load set without changing the CLI arguments.
    dirs = {str(Path(x['path']).parent) for x in [target] + references}
    siblings = []
    for directory in sorted(dirs):
        siblings.extend(input_identity(p) for p in sorted(Path(directory).glob('*.dll')))
    def stable(x):
        if isinstance(x, dict):
            return {k: stable(v) for k, v in x.items() if k != 'path'}
        if isinstance(x, list):
            return [stable(v) for v in x]
        return x
    # Sorting by content rather than absolute directory preserves relocation parity.
    # Directory association matters: auto-ref uses the first initial input's
    # directory containing CoreLib, not an unordered union of sibling DLLs.
    sibling_contents = []
    for item in [target] + references:
        directory = Path(item['path']).parent
        sibling_contents.append(sorted((x['name'], x['sha256']) for x in siblings
                                       if Path(x['path']).parent == directory))
    comparison = {'target': stable(target), 'references': stable(references),
                  'policy': stable(policy), 'siblingDlls': sibling_contents, 'defaultShimCandidates': stable(default_shims),
                  'runtimeSupportCandidate': stable(runtime_support), 'semanticEnvironment': semantic_environment}
    return {'compiler': compiler, 'target': target, 'references': references,
            'policy': policy, 'siblingDlls': siblings,
            'compilerSiblingDlls': compiler_siblings, 'compilerConfiguration': compiler_configuration,
            'compilerCompanionExecutables': companion_hosts,
            'host': host, 'semanticEnvironment': semantic_environment, 'defaultShimCandidates': default_shims,
            'runtimeSupportCandidate': runtime_support,
            'comparisonFingerprint': hashlib.sha256(canonical(comparison).encode()).hexdigest(),
            'comparisonScope': 'target, explicit references, sibling DLL contents, default-shim and runtime-support candidates, and non-output policies; compiler implementation excluded'}


def measurement_command(command, raw):
    result = command[:4]
    i = 4
    while i < len(command):
        if command[i] in ('-o', '--output'):
            i += 2
        elif command[i] == '--measure':
            i += 1
        else:
            result.append(command[i]); i += 1
    return result + ['--measure', '-o', str(raw)]


def owner_category(owner):
    if re.match(r'System\.(?:Runtime\.Intrinsics\.(?:Vector(?:64|128|256|512)?|ISimdVector)|Numerics\.Vector(?:2|3|4)?)(?![A-Za-z0-9])', owner):
        return 'vector'
    parsable = re.fullmatch(r'System\.I(?:Parsable|SpanParsable|Utf8SpanParsable)(?:_([^<>]+)|<([^<>]+)>)', owner)
    if parsable:
        argument = (parsable.group(1) or parsable.group(2)).replace('_', '.')
        if not argument.startswith('System.'):
            argument = 'System.' + argument
        if ARITHMETIC.fullmatch(argument):
            return 'arithmetic'
    if ARITHMETIC.match(owner) or re.match(
            r'System\.Numerics\.I(?:BinaryInteger|BinaryNumber|Number|FloatingPoint|SignedNumber|UnsignedNumber|'
            r'AdditionOperators|SubtractionOperators|MultiplyOperators|DivisionOperators|ModulusOperators|'
            r'UnaryPlusOperators|UnaryNegationOperators|IncrementOperators|DecrementOperators|'
            r'BitwiseOperators|ShiftOperators|ComparisonOperators|EqualityOperators|MinMaxValue|'
            r'TrigonometricFunctions|HyperbolicFunctions|ExponentialFunctions|LogarithmicFunctions|'
            r'PowerFunctions|RootFunctions)', owner):
        return 'arithmetic'
    return 'other'


def classify(row):
    # Retention chains describe why a caller exists, not the failed operation.
    text = row['message'].split(' [chain:', 1)[0]
    if ': ' in text:
        prefix, rest = text.split(': ', 1)
        if ' ' not in prefix:
            text = rest
    basis, owner = 'diagnostic-target', None
    explicit_target = re.search(r'(System\.[^\s:]+)::([A-Za-z_][A-Za-z_0-9]*)', text)
    vector_matches = list(VECTOR_OP.finditer(text))
    arithmetic_matches = list(ARITHMETIC_OP.finditer(text))
    if explicit_target:
        owner = explicit_target.group(1)
        category = owner_category(owner)
        operation = explicit_target.group(2)
    elif vector_matches:
        explicit = re.search(r'\.ISimdVector<.*>\.([A-Za-z_][A-Za-z_0-9]*)', text)
        operation, category = (explicit.group(1) if explicit else vector_matches[0].group(2)), 'vector'
        owner = vector_matches[0].group(1)
    elif arithmetic_matches:
        operation, category = arithmetic_matches[0].group(2), 'arithmetic'
        owner = arithmetic_matches[0].group(1)
    else:
        member = re.search(r'(?:\.|::)([A-Za-z_][A-Za-z_0-9]*)(?:<[^>]*>)?$', row['method'])
        category = owner_category(row['method'])
        operation = member.group(1) if member and category != 'other' else '<unclassified>'
        basis = 'method-name' if operation != '<unclassified>' else 'unclassified'
    # Other BCL operations stay grouped by their named target when available.
    # These names are hints, not a claim of one independent implementation task.
    all_text = re.sub(r'_([0-9A-Fa-f]{4})', lambda m: chr(int(m.group(1), 16)), row['method'] + ' ' + text)
    widths = sorted({m.group(1) or m.group(2) or 'Numerics' for m in WIDTH.finditer(all_text)})
    elements = sorted({t for t in UNSUPPORTED_TYPES
                       if re.search(r'(?<![A-Za-z0-9])' + t + r'(?![A-Za-z0-9])', all_text)})
    mechanism = 'unclassified'
    if 'InternalCall/extern method with no IL body' in text:
        mechanism = 'interface-no-body-or-intrinsic' if owner and re.match(
            r'System\.(?:Numerics\.I[A-Z]|Runtime\.Intrinsics\.ISimdVector|I(?:Parsable|SpanParsable|Utf8SpanParsable))', owner) else 'no-body-or-intrinsic'
    elif 'has no intrinsic mapping yet' in text:
        mechanism = 'missing-intrinsic-call'
    elif 'vector helper ' in text and 'is not supported' in text:
        mechanism = 'unsupported-vector-helper'
    elif 'requires an exact' in text and 'CoreLib declaration' in text:
        mechanism = 'declaration-or-type-guard'
    elif 'deliberately unmapped' in text:
        mechanism = 'deliberately-unmapped'
    family = next((name for name, operations in SEMANTIC_FAMILIES.get(category, {}).items()
                   if operation in operations), operation)
    failure = {'symbol': row['cppSymbol'] or row['method'], 'targetOwner': owner,
               'category': category, 'operation': operation, 'mechanismHint': mechanism,
               'exception': row['exception'], 'diagnostic': text}
    return {'category': category, 'operation': operation, 'classificationBasis': basis,
            'targetOwner': owner, 'semanticFamilyHint': family, 'mechanismHint': mechanism,
            'failureId': hashlib.sha256(canonical(failure).encode()).hexdigest(),
            'widthHints': widths, 'unsupportedElementHints': elements,
            'guardStatus': 'candidate-not-proven' if elements and category == 'vector' else 'unknown'}


def backlog_families(rows):
    groups = {}
    for row in rows:
        key = ':'.join((row['category'], row['semanticFamilyHint'], row['mechanismHint']))
        group = groups.setdefault(key, {'key': key, 'category': row['category'],
            'semanticFamilyHint': row['semanticFamilyHint'], 'mechanismHint': row['mechanismHint'],
            'observations': 0, 'symbols': set(), 'failureIds': set(), 'operations': set(),
            'targetOwners': set(), 'widthHints': set(), 'unsupportedElementHints': set()})
        group['observations'] += 1
        for field, value in (('symbols', row['cppSymbol']), ('failureIds', row['failureId']),
                             ('operations', row['operation']), ('targetOwners', row['targetOwner'])):
            if value: group[field].add(value)
        for field in ('widthHints', 'unsupportedElementHints'):
            group[field].update(row[field])
    result = []
    for key in sorted(groups):
        group = groups[key]
        group['distinctReportedSymbols'] = len(group['symbols'])
        group['distinctFailureIds'] = len(group['failureIds'])
        for field, value in list(group.items()):
            if isinstance(value, set): group[field] = sorted(value)
        result.append(group)
    return result


def read_evidence(raw):
    metadata = {}
    with (raw / 's0-measure.tsv').open(encoding='utf-8-sig', newline='') as f:
        for line in csv.reader(f, delimiter='\t'):
            if len(line) != 2 or line[0] in metadata:
                raise ValueError('invalid or duplicate measurement metadata field')
            metadata[line[0]] = line[1]
    if metadata.get('runStatus') != 'completed':
        raise ValueError('compiler did not provide a completed diagnostic report')
    if metadata.get('schemaVersion') != '1':
        raise ValueError('unsupported measurement evidence version')
    for key in ('successfulBodyCount', 'failedBodyCount', 'infrastructureGapCount',
                'reachFailedMethodCount', 'reachFailedSpecializationCount'):
        metadata[key] = int(metadata[key])
        if metadata[key] < 0: raise ValueError('negative measurement counter')
    metadata['recordedGapCount'] = int(metadata['recordedGapCount'])
    metadata['namedSymbols'] = None if metadata['namedSymbols'] == 'null' else int(metadata['namedSymbols'])
    if metadata['recordedGapCount'] < 0 or (metadata['namedSymbols'] is not None and metadata['namedSymbols'] < 0):
        raise ValueError('negative measurement counter')
    if metadata.get('firstFailurePerBody') != 'true' or metadata.get('unknownGapCount') != 'null':
        raise ValueError('missing measurement coverage qualification')
    metadata['firstFailurePerBody'], metadata['unknownGapCount'] = True, None
    for key, choices in {'discoveryStatus': ('partial', 'no-reported-failure'),
                         'bodyPassStatus': ('truncated', 'finished'),
                         'symbolAuditStatus': ('skipped', 'partial', 'executed')}.items():
        if metadata.get(key) not in choices: raise ValueError('invalid ' + key)
    rows = []
    with (raw / 's0-gaps-detail.tsv').open(encoding='utf-8-sig', newline='') as f:
        for values in csv.reader(f, delimiter='\t', quoting=csv.QUOTE_NONE):
            if len(values) != len(FIELDS): raise ValueError('invalid detailed gap row')
            row = dict(zip(FIELDS, values)); row.update(classify(row)); rows.append(row)
    if metadata['recordedGapCount'] != len(rows):
        raise ValueError('detailed gaps do not match recordedGapCount')
    for key in ('failedEmissionBodyCount', 'unreplayedPlanningFailureBodyCount'):
        if key in metadata:
            metadata[key] = int(metadata[key])
            if metadata[key] < 0: raise ValueError('negative measurement counter')
    # All raw rows remain in evidence; summaries separately deduplicate observations.
    rows.sort(key=lambda row: tuple(row[k] for k in FIELDS))
    groups = {}
    for row in rows:
        key = row['category'] + ':' + row['operation']
        group = groups.setdefault(key, {'key': key, 'category': row['category'],
            'operation': row['operation'], 'observations': 0, 'methods': set(),
            'symbols': set(), 'phases': set(), 'messages': set(), 'widthHints': set(),
            'unsupportedElementHints': set(), 'classificationBasis': set()})
        group['observations'] += 1
        for key2, value in (('methods', row['method']), ('symbols', row['cppSymbol']),
                            ('phases', row['phase']), ('messages', row['message']),
                            ('classificationBasis', row['classificationBasis'])):
            if value: group[key2].add(value)
        group['widthHints'].update(row['widthHints'])
        group['unsupportedElementHints'].update(row['unsupportedElementHints'])
    result = []
    for key in sorted(groups):
        group = groups[key]
        group['distinctReportedSymbols'] = len(group['symbols'])
        for field, value in list(group.items()):
            if isinstance(value, set): group[field] = sorted(value)
        result.append(group)
    return metadata, rows, result


def compare(report, baseline):
    if not isinstance(baseline, dict):
        raise ValueError('baseline must be a report object')
    if baseline.get('schemaVersion') != SCHEMA or baseline.get('classifierVersion') != CLASSIFIER:
        return {'status': 'incompatible-schema-or-classifier'}
    if report.get('runStatus') != 'completed' or baseline.get('runStatus') != 'completed':
        return {'status': 'unavailable-incomplete-run'}
    if not isinstance(baseline.get('inputs'), dict):
        raise ValueError('baseline input identity is malformed')
    if report['inputs']['comparisonFingerprint'] != baseline['inputs'].get('comparisonFingerprint'):
        return {'status': 'incompatible-inputs-or-policy'}
    groups = baseline.get('groups')
    if (not isinstance(groups, list) or any(not isinstance(g, dict) or not isinstance(g.get('key'), str)
            or type(g.get('observations')) is not int or g['observations'] < 0 for g in groups)
            or len({g['key'] for g in groups}) != len(groups)):
        raise ValueError('baseline groups are malformed or duplicated')
    gaps = baseline.get('gaps')
    if not isinstance(gaps, list) or any(not isinstance(g, dict) or not isinstance(g.get('failureId'), str) for g in gaps):
        raise ValueError('baseline failure identities are malformed')
    if sum(g['observations'] for g in groups) != len(gaps):
        raise ValueError('baseline group counts do not match gap observations')
    old, new = {g['key']: g for g in baseline['groups']}, {g['key']: g for g in report['groups']}
    old_ids = {g['failureId'] for g in baseline.get('gaps', [])}
    new_ids = {g['failureId'] for g in report.get('gaps', [])}
    return {'status': 'comparable-observations-only',
            'newlyObservedFamilies': sorted(new.keys() - old.keys()),
            'noLongerObservedFamilies': sorted(old.keys() - new.keys()),
            'remainingObservedFamilies': sorted(new.keys() & old.keys()),
            'observationDeltas': {k: new.get(k, {}).get('observations', 0) - old.get(k, {}).get('observations', 0)
                                  for k in sorted(old.keys() | new.keys())},
            'newlyObservedFailureIds': sorted(new_ids - old_ids),
            'noLongerObservedFailureIds': sorted(old_ids - new_ids),
            'stillObservedFailureCount': len(new_ids & old_ids),
            'provenResolvedFamilies': None,
            'warning': 'Not observed no longer does not prove resolved: discovery and first failures can change.'}


def directory_bytes(path):
    total = 0
    for p in path.rglob('*'):
        try:
            if p.is_file(): total += p.stat().st_size
        except FileNotFoundError:
            # Build/runtime temporary files can disappear between enumeration/stat.
            pass
    return total


def run(args):
    out = Path(args.output).resolve()
    out.mkdir(parents=True, exist_ok=False)
    raw = out / 'raw'
    report = {'schemaVersion': SCHEMA, 'classifierVersion': CLASSIFIER,
              'runStatus': 'aborted', 'counts': None, 'groups': None, 'gaps': None,
              'coverage': {'remainingWorkCount': None, 'completionPercentage': None,
                  'unknownGapCount': None, 'unvisitedBodyCount': None, 'unvisitedCallSiteCount': None,
                  'scope': 'retained emission-body attempts and separately marked unreplayed planning trials; not executed control flow',
                  'limitations': ['First failure per body; later calls and dependent closures can be unvisited.',
                    'Family grouping is heuristic, not a count of independently implementable fixes.',
                    'No generated C++, native compilation, linking, backend epilogue or execution is validated.',
                    'Successful method compilation does not prove every arithmetic/Vector API is supported.']}}
    try:
        command = json.loads(Path(args.command_file).read_text())
        if not isinstance(command, list) or not all(isinstance(s, str) for s in command):
            raise ValueError('command file must be a JSON string array')
        report['inputs'] = input_context(command)
        command = measurement_command(command, raw)
        report['command'] = command
        (out / 'command.json').write_text(json.dumps(command, ensure_ascii=False, indent=2) + '\n')
        stop_reason = None
        start = time.monotonic()
        with (out / 'stdout.log').open('wb') as stdout, (out / 'stderr.log').open('wb') as stderr:
            proc = subprocess.Popen(command, stdout=stdout, stderr=stderr, start_new_session=True)
            while proc.poll() is None:
                time.sleep(.2)
                if time.monotonic() - start > args.timeout: stop_reason = 'timeout'
                if directory_bytes(out) > args.max_output_bytes: stop_reason = 'aggregate-output-budget'
                if stop_reason:
                    os.killpg(proc.pid, signal.SIGTERM)
                    try: proc.wait(timeout=10)
                    except subprocess.TimeoutExpired:
                        os.killpg(proc.pid, signal.SIGKILL); proc.wait()
                    break
            report['process'] = {'exitCode': proc.returncode, 'stopReason': stop_reason}
        after = input_context(json.loads(Path(args.command_file).read_text()))
        report['inputsUnchanged'] = after == report['inputs']
        if not report['inputsUnchanged']:
            raise ValueError('compiler or measurement inputs changed during execution')
        if proc.returncode != 0 or stop_reason:
            raise ValueError('measurement process did not finish successfully')
        if list(raw.glob('generated*')) or (raw / 'CMakeLists.txt').exists():
            raise ValueError('diagnostic run unexpectedly produced native output')
        metadata, gaps, groups = read_evidence(raw)
        report['runStatus'], report['measurement'] = 'completed', metadata
        report['gaps'], report['groups'] = gaps, groups
        report['backlogFamilies'] = backlog_families(gaps)
        report['counts'] = {'observations': len(gaps), 'operationFamilyHints': len(groups),
            'distinctReportedSymbols': len({g['cppSymbol'] for g in gaps if g['cppSymbol']}),
            'distinctFailureIds': len({g['failureId'] for g in gaps}),
            'semanticMechanismFamilyHints': len(report['backlogFamilies']),
            'vectorFamilyHints': sum(g['category'] == 'vector' for g in groups),
            'arithmeticFamilyHints': sum(g['category'] == 'arithmetic' for g in groups),
            'otherFamilyHints': sum(g['category'] == 'other' for g in groups)}
        report['categorySummary'] = []
        for category in ('arithmetic', 'vector', 'other'):
            selected = [g for g in gaps if g['category'] == category]
            report['categorySummary'].append({'category': category, 'observations': len(selected),
                'distinctReportedSymbols': len({g['cppSymbol'] for g in selected if g['cppSymbol']}),
                'failedEmissionBodyCount': len({g['cppSymbol'] or g['method'] for g in selected
                                               if g['phase'] == 'compile' and g['pass'] != 'planning'}),
                'unreplayedPlanningFailureBodyCount': len({g['cppSymbol'] or g['method'] for g in selected
                                                          if g['phase'] == 'compile' and g['pass'] == 'planning'}),
                'reachFailedMethodCount': len({g['cppSymbol'] for g in selected if g['phase'] == 'reach' and g['cppSymbol']}),
                'operationFamilyHints': sum(g['category'] == category for g in groups),
                'semanticMechanismFamilyHints': sum(g['category'] == category for g in report['backlogFamilies'])})
        if args.baseline:
            try:
                report['delta'] = compare(report, json.loads(Path(args.baseline).read_text()))
            except (OSError, ValueError, KeyError, TypeError) as ex:
                report['delta'] = {'status': 'invalid-baseline', 'error': str(ex)}
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as ex:
        report['error'] = str(ex)
        if args.baseline: report['delta'] = {'status': 'unavailable-incomplete-run'}
    (out / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2, sort_keys=True) + '\n')
    lines = ['Measurement: ' + report['runStatus'], 'Observed lower bound only; remaining total is unknown.']
    for group in report.get('backlogFamilies') or []:
        lines.append(f"{group['key']}\t{group['observations']} observations\t{group['distinctReportedSymbols']} symbols")
    if report.get('error'): lines.append('Error: ' + report['error'])
    (out / 'summary.txt').write_text('\n'.join(lines) + '\n')
    print('\n'.join(lines))
    return 0 if report['runStatus'] == 'completed' else 2


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--command-file', required=True, help='JSON argv: dotnet exec compiler.dll input.dll ...')
    p.add_argument('--output', required=True, help='New directory; never reuse an earlier report directory')
    p.add_argument('--baseline', help='Earlier report.json with the same input and policy')
    p.add_argument('--timeout', type=float, default=1800)
    p.add_argument('--max-output-bytes', type=int, default=1024**3)
    args = p.parse_args()
    if os.name != 'posix': p.error('the process-budget runner currently requires a POSIX host')
    if args.timeout <= 0 or args.max_output_bytes <= 0: p.error('budgets must be positive')
    try: return run(args)
    except FileExistsError:
        p.error('output directory already exists; choose a fresh directory')


if __name__ == '__main__':
    sys.exit(main())
