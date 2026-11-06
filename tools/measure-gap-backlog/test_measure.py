#!/usr/bin/env python3
"""Deterministic evidence contracts; no native compilation is required."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location('gap_measure', HERE / 'measure.py')
measure = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(measure)


def row(method='Program.First', message='vector helper System.Runtime.Intrinsics.Vector128::Log<1> is not supported', symbol='first', phase='compile', pass_name='emission'):
    return dict(zip(measure.FIELDS, (phase, 'Test', method, 'NotSupportedException', message, symbol, pass_name)))


def evidence(root, rows, **overrides):
    root.mkdir(parents=True, exist_ok=True)
    metadata = dict(schemaVersion='1', runStatus='completed', successfulBodyCount='3', failedBodyCount='1',
        infrastructureGapCount='0', reachFailedMethodCount='0', reachFailedSpecializationCount='0',
        recordedGapCount=str(len(rows)), discoveryStatus='no-reported-failure', bodyPassStatus='finished',
        symbolAuditStatus='executed', namedSymbols='3', firstFailurePerBody='true', unknownGapCount='null')
    metadata.update(overrides)
    (root / 's0-measure.tsv').write_text(''.join(k + '\t' + v + '\n' for k, v in metadata.items()))
    (root / 's0-gaps-detail.tsv').write_text(''.join('\t'.join(r[k] for k in measure.FIELDS) + '\n' for r in rows))


class MeasureTests(unittest.TestCase):
    def setUp(self):
        # Retain test artifacts; a failed run must remain inspectable.
        self.root = Path(tempfile.mkdtemp(prefix='dn2cpp-gap-measure-test-')).resolve()

    def test_vector_target_wins_over_caller_prefix(self):
        r = row('System.Numerics.Vector2.MaxNumber',
                'System.Numerics.Vector2.MaxNumber: vector helper System.Runtime.Intrinsics.Vector128::Log<1> is not supported')
        c = measure.classify(r)
        self.assertEqual(('vector', 'Log', 'diagnostic-target'),
                         (c['category'], c['operation'], c['classificationBasis']))

    def test_arithmetic_target_wins_over_vector_caller(self):
        r = row('System.Numerics.Vector2.Log', 'System.Numerics.Vector2.Log: System.Math::Pow is not supported')
        c = measure.classify(r)
        self.assertEqual(('arithmetic', 'Pow'), (c['category'], c['operation']))

    def test_generic_integer_interface_target_is_arithmetic(self):
        result = measure.classify(row(message='System.Numerics.IBinaryInteger_Int32::TryReadBigEndian is unsupported'))
        self.assertEqual(('arithmetic', 'TryReadBigEndian', 'diagnostic-target'),
                         (result['category'], result['operation'], result['classificationBasis']))

    def test_mangled_simd_interface_target_is_vector(self):
        result = measure.classify(row(message='System.Runtime.Intrinsics.ISimdVector_Ginst_Vector128_Int32::get_Alignment is unsupported'))
        self.assertEqual(('vector', 'get_Alignment'), (result['category'], result['operation']))

    def test_retention_chain_is_not_failed_target(self):
        result = measure.classify(row(message='System.Threading.Tasks.Task::get_InternalCurrent is unsupported [chain: System.Runtime.Intrinsics.Vector128::Log -> Program.Main]'))
        self.assertEqual(('other', 'get_InternalCurrent'), (result['category'], result['operation']))

    def test_explicit_interface_method_fallback_keeps_operation(self):
        method = 'System.Numerics.Vector<System.Decimal>.System.Runtime.Intrinsics.ISimdVector<System.Numerics.Vector<System.Decimal>,System.Decimal>.MaxNumber'
        result = measure.classify(row(method, 'generic method is unsupported'))
        self.assertEqual(('vector', 'MaxNumber', 'method-name'),
                         (result['category'], result['operation'], result['classificationBasis']))

    def test_unsupported_guard_is_only_a_hint(self):
        c = measure.classify(row('System.Runtime.Intrinsics.Vector128<System.Int128>.MaxNumber'))
        self.assertEqual('candidate-not-proven', c['guardStatus'])
        self.assertEqual(['Int128'], c['unsupportedElementHints'])
        self.assertIn('128', c['widthHints'])

    def test_actual_mangled_vector_width_and_element_hints(self):
        method = 'System.Runtime.Intrinsics.Vector128_System_UInt128.System.Runtime.Intrinsics.ISimdVector<System.Runtime.Intrinsics.Vector128<T>,T>.MaxNumber'
        c = measure.classify(row(method, 'Vector magnitude selection requires an exact floating-point generic CoreLib declaration and method specification'))
        self.assertEqual(['128'], c['widthHints'])
        self.assertEqual(['UInt128'], c['unsupportedElementHints'])
        self.assertEqual('number-selection', c['semanticFamilyHint'])
        self.assertEqual('candidate-not-proven', c['guardStatus'])

    def test_encoded_simd_owner_retains_width(self):
        owner = 'System.Runtime.Intrinsics.ISimdVector_Ginst31_XSystem_002EPrivate_002ECoreLib37_XSystem_002ERuntime_002EIntrinsics_002EVector256_00601_1_5_NByteEnd_Byte'
        c = measure.classify(row(owner + '.LoadAligned', owner + '::get_Alignment is an InternalCall/extern method with no IL body and no intrinsic mapping'))
        self.assertEqual(['256'], c['widthHints'])
        self.assertEqual('interface-no-body-or-intrinsic', c['mechanismHint'])

    def test_owner_category_respects_type_boundaries(self):
        self.assertEqual('other', measure.owner_category('System.Numerics.VectorHelper'))
        for name in ('System.IntPtr', 'System.UIntPtr', 'System.Numerics.BigInteger', 'System.Numerics.Complex'):
            self.assertEqual('arithmetic', measure.owner_category(name))

    def test_numeric_parsable_closures_are_arithmetic_but_dates_and_guids_are_not(self):
        for owner in ('System.ISpanParsable_Char', 'System.ISpanParsable_IntPtr',
                      'System.ISpanParsable_UIntPtr', 'System.IParsable<System.Int32>',
                      'System.IUtf8SpanParsable_System_Numerics_BigInteger'):
            c = measure.classify(row(message=owner + '::Parse is an InternalCall/extern method with no IL body and no intrinsic mapping'))
            self.assertEqual(('arithmetic', 'parsing', 'interface-no-body-or-intrinsic'),
                             (c['category'], c['semanticFamilyHint'], c['mechanismHint']))
        for owner in ('System.ISpanParsable_Guid', 'System.IParsable_System_DateTime', 'System.IParsable<TSelf>'):
            self.assertEqual('other', measure.owner_category(owner))

    def test_failure_identity_ignores_pass_and_retention_chain_not_closure(self):
        a = row(message='call to System.Math::Pow has no intrinsic mapping yet [chain: CallerA]')
        b = dict(a, phase='reach', **{'pass': 'planning', 'message': 'call to System.Math::Pow has no intrinsic mapping yet [chain: CallerB]'})
        self.assertEqual(measure.classify(a)['failureId'], measure.classify(b)['failureId'])
        self.assertNotEqual(measure.classify(a)['failureId'], measure.classify(dict(a, cppSymbol='another_closed_body'))['failureId'])

    def test_failure_identity_distinguishes_later_overload_and_exception(self):
        a = row(message='call to System.Runtime.Intrinsics.Vector128::Sin(System.Runtime.Intrinsics.Vector128_Single) has no intrinsic mapping yet')
        b = dict(a, message=a['message'].replace('_Single', '_Double'))
        self.assertNotEqual(measure.classify(a)['failureId'], measure.classify(b)['failureId'])
        self.assertNotEqual(measure.classify(a)['failureId'], measure.classify(dict(a, exception='InvalidOperationException'))['failureId'])

    def test_semantic_family_retains_operations_and_deduplicates_closures(self):
        rows = [row(), row(phase='reach'), row(message='vector helper System.Runtime.Intrinsics.Vector128::Sin is not supported', symbol='second')]
        rows[0]['message'] = rows[1]['message'] = 'vector helper System.Runtime.Intrinsics.Vector128::SinCos is not supported'
        for r in rows: r.update(measure.classify(r))
        group, = measure.backlog_families(rows)
        self.assertEqual('trigonometry', group['semanticFamilyHint'])
        self.assertEqual(['Sin', 'SinCos'], group['operations'])
        self.assertEqual(3, group['observations'])
        self.assertEqual(2, group['distinctReportedSymbols'])
        self.assertEqual(2, group['distinctFailureIds'])

    def test_unknown_category_is_preserved(self):
        c = measure.classify(row('Program.First', 'unsupported opcode jmp'))
        self.assertEqual(('other', '<unclassified>'), (c['category'], c['operation']))

    def test_order_stable_and_symbols_deduplicated(self):
        rows = [row(symbol='a'), row(symbol='a', phase='reach', pass_name='reachability'), row(symbol='b')]
        evidence(self.root / 'a', rows)
        evidence(self.root / 'b', list(reversed(rows)))
        a, b = measure.read_evidence(self.root / 'a'), measure.read_evidence(self.root / 'b')
        self.assertEqual(a, b)
        self.assertEqual(3, a[2][0]['observations'])
        self.assertEqual(2, a[2][0]['distinctReportedSymbols'])
        self.assertEqual(['a', 'b'], a[2][0]['symbols'])
        self.assertEqual(3, len(a[1]))

    def test_partial_and_truncated_evidence_survives(self):
        evidence(self.root, [row(phase='layout', symbol='', pass_name='planning')],
                 infrastructureGapCount='1', discoveryStatus='partial', bodyPassStatus='truncated',
                 symbolAuditStatus='skipped', namedSymbols='null')
        metadata, rows, groups = measure.read_evidence(self.root)
        self.assertEqual(('partial', 'truncated', 'skipped'), tuple(metadata[k] for k in ('discoveryStatus', 'bodyPassStatus', 'symbolAuditStatus')))
        self.assertIsNone(metadata['unknownGapCount'])
        self.assertIsNone(metadata['namedSymbols'])
        self.assertTrue(metadata['firstFailurePerBody'])
        self.assertEqual('planning', rows[0]['pass'])

    def test_invalid_evidence_does_not_become_zero(self):
        evidence(self.root, [], successfulBodyCount='-1')
        with self.assertRaises(ValueError): measure.read_evidence(self.root)
        evidence(self.root, [])
        with (self.root / 's0-measure.tsv').open('a') as f: f.write('schemaVersion\t1\n')
        with self.assertRaises(ValueError): measure.read_evidence(self.root)

    def test_record_count_mismatch_rejected(self):
        evidence(self.root, [row()], recordedGapCount='0')
        with self.assertRaises(ValueError): measure.read_evidence(self.root)

    def make_command(self, location):
        location.mkdir()
        compiler = location / 'compiler'; compiler.mkdir()
        target = location / 'target'; target.mkdir()
        refs = location / 'refs'; refs.mkdir()
        for path, data in ((compiler / 'dn2cpp.dll', b'compiler'), (target / 'app.dll', b'app'), (refs / 'System.Private.CoreLib.dll', b'bcl')):
            path.write_bytes(data)
        return ['dotnet', 'exec', str(compiler / 'dn2cpp.dll'), str(target / 'app.dll'), '-r', str(refs / 'System.Private.CoreLib.dll'), '--stripping-level', 'medium', '--auto-ref', '-o', str(location / 'old-output')]

    def test_input_context_relocation_and_compiler_changes(self):
        a = self.make_command(self.root / 'a'); b = self.make_command(self.root / 'b')
        Path(b[2]).write_bytes(b'new compiler')
        self.assertEqual(measure.input_context(a)['comparisonFingerprint'], measure.input_context(b)['comparisonFingerprint'])
        Path(b[5]).write_bytes(b'changed bcl')
        self.assertNotEqual(measure.input_context(a)['comparisonFingerprint'], measure.input_context(b)['comparisonFingerprint'])
        Path(b[5]).write_bytes(b'bcl'); b[7] = 'high'
        self.assertNotEqual(measure.input_context(a)['comparisonFingerprint'], measure.input_context(b)['comparisonFingerprint'])

    def test_sibling_directory_association_is_not_erased(self):
        command = self.make_command(self.root / 'a')
        target_dir, refs_dir = Path(command[3]).parent, Path(command[5]).parent
        (target_dir / 'Extra.dll').write_bytes(b'one')
        (refs_dir / 'Extra.dll').write_bytes(b'two')
        before = measure.input_context(command)['comparisonFingerprint']
        (target_dir / 'Extra.dll').write_bytes(b'two')
        (refs_dir / 'Extra.dll').write_bytes(b'one')
        self.assertNotEqual(before, measure.input_context(command)['comparisonFingerprint'])

    def test_default_shim_contents_and_presence_are_bound(self):
        command = self.make_command(self.root / 'a')
        compiler_dir = Path(command[2]).parent
        absent = measure.input_context(command)['comparisonFingerprint']
        shim = compiler_dir / 'DnZlib.dll'; shim.write_bytes(b'shim v1')
        present = measure.input_context(command)['comparisonFingerprint']
        self.assertNotEqual(absent, present)
        shim.write_bytes(b'shim v2')
        self.assertNotEqual(present, measure.input_context(command)['comparisonFingerprint'])
        current = measure.input_context(command)['comparisonFingerprint']
        (compiler_dir / 'Dn2Cpp.Transpiler.dll').write_bytes(b'changed implementation')
        self.assertEqual(current, measure.input_context(command)['comparisonFingerprint'])

    def test_injected_runtime_input_is_bound_not_compiler_only(self):
        a = self.make_command(self.root / 'a'); b = self.make_command(self.root / 'b')
        absent = measure.input_context(a)
        runtime = Path(a[2]).parent / 'Dn2Cpp.Runtime.dll'
        runtime.write_bytes(b'runtime v1')
        present = measure.input_context(a)
        self.assertNotEqual(absent['comparisonFingerprint'], present['comparisonFingerprint'])
        self.assertEqual('Dn2Cpp.Runtime.dll', present['runtimeSupportCandidate']['name'])
        (Path(b[2]).parent / runtime.name).write_bytes(b'runtime v1')
        self.assertEqual(present['comparisonFingerprint'], measure.input_context(b)['comparisonFingerprint'])
        runtime.write_bytes(b'runtime v2')
        changed = measure.input_context(a)
        self.assertNotEqual(present['comparisonFingerprint'], changed['comparisonFingerprint'])
        common = dict(schemaVersion=1, classifierVersion=measure.CLASSIFIER, runStatus='completed', groups=[], gaps=[])
        self.assertEqual('incompatible-inputs-or-policy',
                         measure.compare(dict(common, inputs=changed), dict(common, inputs=present))['status'])
        Path(a[2]).write_bytes(b'new compiler')
        self.assertEqual(changed['comparisonFingerprint'], measure.input_context(a)['comparisonFingerprint'])

    def test_semantic_environment_changes_comparison(self):
        command = self.make_command(self.root / 'a')
        with mock.patch.dict(os.environ, {'DN2CPP_MAX_INSTANTIATIONS': '100'}):
            before = measure.input_context(command)
        with mock.patch.dict(os.environ, {'DN2CPP_MAX_INSTANTIATIONS': '200'}):
            after = measure.input_context(command)
        self.assertEqual('100', before['semanticEnvironment']['DN2CPP_MAX_INSTANTIATIONS'])
        self.assertNotEqual(before['comparisonFingerprint'], after['comparisonFingerprint'])

    def test_runtime_configuration_is_recorded_but_not_implementation_delta(self):
        command = self.make_command(self.root / 'a')
        config = Path(command[2]).with_suffix('.runtimeconfig.json')
        config.write_text('{"runtimeOptions":{"tfm":"net10.0"}}')
        before = measure.input_context(command)
        self.assertEqual(['dn2cpp.runtimeconfig.json'], [x['name'] for x in before['compilerConfiguration']])
        config.write_text('{"runtimeOptions":{"tfm":"net11.0"}}')
        after = measure.input_context(command)
        self.assertNotEqual(before['compilerConfiguration'], after['compilerConfiguration'])
        self.assertNotEqual(before, after)  # run-time mutation guard compares full context.
        self.assertEqual(before['comparisonFingerprint'], after['comparisonFingerprint'])

    def test_explicit_corelib_is_required(self):
        command = self.make_command(self.root / 'a')
        del command[4:6]
        with self.assertRaises(ValueError): measure.input_context(command)

    def test_unbound_directory_backend_and_external_output_modes_are_rejected(self):
        command = self.make_command(self.root / 'a')
        for flag in ('--project-root', '--hotupdate-base', '--hotupdate-refs', '--gdextension',
                     '--dotnet-module', '--emit-patch', '--generate-bindings', '--check-wasm-imports',
                     '--ildiet-output', '--dump-isa-surface'):
            with self.subTest(flag=flag), self.assertRaises(ValueError):
                measure.input_context(command + [flag, str(self.root)])

    def test_companion_implementation_is_recorded_for_mutation_check(self):
        command = self.make_command(self.root / 'a')
        companion = Path(command[2]).parent / 'ildiet'; companion.mkdir()
        binary = companion / 'ILDiet.dll'; binary.write_bytes(b'old')
        before = measure.input_context(command)
        binary.write_bytes(b'new')
        after = measure.input_context(command)
        self.assertNotEqual(before, after)
        self.assertEqual(before['comparisonFingerprint'], after['comparisonFingerprint'])

    def test_companion_apphost_is_recorded_for_mutation_check(self):
        command = self.make_command(self.root / 'a')
        companion = Path(command[2]).parent / 'ildiet'; companion.mkdir()
        for name in ('ILDiet', 'ILDiet.exe'):
            with self.subTest(name=name):
                apphost = companion / name; apphost.write_bytes(b'apphost v1')
                before = measure.input_context(command)
                apphost.write_bytes(b'apphost v2')
                after = measure.input_context(command)
                self.assertNotEqual(before, after)
                self.assertEqual(before['comparisonFingerprint'], after['comparisonFingerprint'])

    def test_aliased_semantic_inputs_are_rejected(self):
        command = self.make_command(self.root / 'a')
        for index in (2, 3, 5):
            with self.subTest(index=index):
                target = Path(command[index]); alias = target.with_name('alias-' + target.name)
                alias.symlink_to(target)
                altered = list(command); altered[index] = str(alias)
                with self.assertRaisesRegex(ValueError, 'symlink'):
                    measure.input_context(altered)
        alias_directory = self.root / 'target-alias'
        alias_directory.symlink_to(Path(command[3]).parent, target_is_directory=True)
        altered = list(command); altered[3] = str(alias_directory / 'app.dll')
        with self.assertRaisesRegex(ValueError, 'symlink'):
            measure.input_context(altered)

    def test_noncompleted_metadata_is_rejected(self):
        evidence(self.root, [], runStatus='aborted')
        with self.assertRaises(ValueError): measure.read_evidence(self.root)

    def test_measure_command_keeps_policy_and_replaces_output(self):
        a = self.make_command(self.root / 'a') + ['--measure']
        cmd = measure.measurement_command(a, self.root / 'raw')
        self.assertEqual(1, cmd.count('--measure'))
        self.assertEqual(1, cmd.count('-o'))
        self.assertEqual(str(self.root / 'raw'), cmd[-1])
        self.assertIn('medium', cmd)

    def test_delta_never_claims_resolution(self):
        common = dict(schemaVersion=1, classifierVersion=measure.CLASSIFIER, runStatus='completed', inputs={'comparisonFingerprint':'same'})
        old = dict(common, groups=[{'key':'vector:Log', 'observations':3}], gaps=[{'failureId': 'old'}] * 3)
        new = dict(common, groups=[{'key':'vector:MaxNumber', 'observations':1}], gaps=[{'failureId': 'new'}])
        delta = measure.compare(new, old)
        self.assertEqual(['vector:Log'], delta['noLongerObservedFamilies'])
        self.assertIsNone(delta['provenResolvedFamilies'])
        self.assertEqual({'vector:Log':-3, 'vector:MaxNumber':1}, delta['observationDeltas'])
        self.assertEqual(['new'], delta['newlyObservedFailureIds'])
        self.assertEqual(['old'], delta['noLongerObservedFailureIds'])
        new['runStatus'] = 'aborted'
        self.assertEqual('unavailable-incomplete-run', measure.compare(new, old)['status'])
        new['runStatus'] = 'completed'; new['inputs'] = {'comparisonFingerprint':'changed'}
        self.assertEqual('incompatible-inputs-or-policy', measure.compare(new, old)['status'])

    def test_invalid_baseline_shape_and_duplicate_groups_rejected(self):
        current = dict(schemaVersion=1, classifierVersion=measure.CLASSIFIER, runStatus='completed',
                       inputs={'comparisonFingerprint': 'same'}, groups=[], gaps=[])
        with self.assertRaises(ValueError): measure.compare(current, [])
        bad = dict(current, groups=[{'key': 'vector:Sin', 'observations': 1}] * 2)
        with self.assertRaises(ValueError): measure.compare(current, bad)
        missing = dict(current); del missing['gaps']
        with self.assertRaises(ValueError): measure.compare(current, missing)
        with self.assertRaises(ValueError): measure.compare(current, dict(current, groups=[{'key': 'vector:Sin', 'observations': 1}]))

    def test_exit_zero_without_sidecar_is_aborted_not_clean(self):
        command = self.make_command(self.root / 'a')
        executable = self.root / 'noop'
        executable.write_text('#!/bin/sh\nexit 0\n'); executable.chmod(0o755)
        command[0] = str(executable)
        command_file = self.root / 'command.json'; command_file.write_text(json.dumps(command))
        out = self.root / 'run'
        args = argparse.Namespace(output=str(out), command_file=str(command_file), baseline=None, timeout=5, max_output_bytes=100000)
        self.assertEqual(2, measure.run(args))
        report = json.loads((out / 'report.json').read_text())
        self.assertEqual('aborted', report['runStatus'])
        self.assertIsNone(report['counts']); self.assertIsNone(report['groups'])
        self.assertIsNone(report['coverage']['completionPercentage'])
        self.assertEqual(0, report['process']['exitCode'])
        with self.assertRaises(FileExistsError): measure.run(args)

    def test_invalid_baseline_does_not_abort_completed_measurement(self):
        command = self.make_command(self.root / 'a')
        executable = self.root / 'noop'
        executable.write_text('#!/bin/sh\nexit 0\n'); executable.chmod(0o755)
        command[0] = str(executable)
        command_file = self.root / 'command.json'; command_file.write_text(json.dumps(command))
        baseline = self.root / 'invalid.json'; baseline.write_text('{broken json')
        args = argparse.Namespace(output=str(self.root / 'run'), command_file=str(command_file),
                                  baseline=str(baseline), timeout=5, max_output_bytes=100000)
        def fake_evidence(raw):
            evidence(raw, [row()])
            return original_read(raw)
        original_read = measure.read_evidence
        with mock.patch.object(measure, 'read_evidence', side_effect=fake_evidence):
            self.assertEqual(0, measure.run(args))
        report = json.loads((self.root / 'run/report.json').read_text())
        self.assertEqual('completed', report['runStatus'])
        self.assertEqual(1, report['counts']['observations'])
        self.assertEqual('invalid-baseline', report['delta']['status'])

    def test_failed_launch_still_writes_envelope(self):
        command = self.make_command(self.root / 'a'); command[0] = str(self.root / 'missing-executable')
        path = self.root / 'command.json'; path.write_text(json.dumps(command))
        args = argparse.Namespace(output=str(self.root / 'run'), command_file=str(path), baseline=None, timeout=5, max_output_bytes=100000)
        self.assertEqual(2, measure.run(args))
        report = json.loads((self.root / 'run/report.json').read_text())
        self.assertEqual('aborted', report['runStatus']); self.assertIsNone(report['counts'])


if __name__ == '__main__':
    unittest.main()
