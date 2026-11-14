#!/usr/bin/env python3
"""Exercise hosted pre-merge identity, failure evidence and receipt validation offline."""
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from bash_fixture import run_bash

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = (ROOT / '.github/workflows/premerge-debug.yml').read_text(encoding='utf-8')


def step_script(name):
    section = WORKFLOW.split('      - name: ' + name + '\n', 1)[1].split('\n      - ', 1)[0]
    source = section.split('        run: |\n', 1)[1]
    return '\n'.join(line[10:] for line in source.splitlines())


class HostedPremergeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='ci premerge space ')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.repo = self.root / 'repo space'
        self.repo.mkdir()
        (self.repo / 'gates').mkdir()
        shutil.copyfile(ROOT / 'gates/ci-premerge.sh', self.repo / 'gates/ci-premerge.sh')
        shutil.copyfile(ROOT / 'gates/_python.sh', self.repo / 'gates/_python.sh')
        (self.repo / 'gates/pre-merge.sh').write_text('''#!/usr/bin/env bash
set -euo pipefail
if [ "${FIXTURE_WAIT_MONITOR:-0}" = 1 ]; then
    until grep -q '^disk ' "$DN2CPP_PREMERGE_LOGROOT/disk-periodic.log" 2>/dev/null; do
        sleep 0.01
    done
fi
printf '%s\\n' "$@" > "$DN2CPP_PREMERGE_LOGROOT/runner-args.txt"
printf '%s %s %s\\n' "$CONFIG" "$JOBS" "$CMAKE_BUILD_PARALLEL_LEVEL" > "$DN2CPP_PREMERGE_LOGROOT/runner-env.txt"
printf 'native stdout\\r\\n'
printf 'native stderr\\n' >&2
if [ "${FIXTURE_RECEIPT:-valid}" != missing ]; then
    {
        printf '%s\\n' 'dn2cpp NON-GODOT PRE-MERGE PASSED'
        printf 'configs: %s\\n' "${FIXTURE_CONFIG:-Debug}"
        printf 'commit: fixture @ %s\\n' "${FIXTURE_COMMIT:-$(git rev-parse --short HEAD)}"
    } > "$DN2CPP_PREMERGE_LOGROOT/_receipt.txt"
fi
exit "${FIXTURE_EXIT:-0}"
''', encoding='utf-8', newline='\n')
        subprocess.run(['git', 'init', '-q', str(self.repo)], check=True)
        subprocess.run(['git', '-C', str(self.repo), 'add', '.'], check=True)
        subprocess.run(['git', '-C', str(self.repo), '-c', 'user.name=Fixture',
                        '-c', 'user.email=fixture@example.invalid', '-c', 'commit.gpgsign=false',
                        'commit', '-qm', 'fixture'], check=True)
        self.head = subprocess.check_output(['git', '-C', str(self.repo), 'rev-parse', 'HEAD']).decode().strip()
        self.logs = self.root / 'evidence space'
        self.logs.mkdir()
        self.environment = dict(os.environ, DN2CPP_PREMERGE_LOGROOT=self.logs.as_posix(),
                                EXPECTED_HEAD=self.head, GITHUB_SHA=self.head,
                                GITHUB_WORKSPACE=self.repo.as_posix(), RUNNER_TEMP=self.root.as_posix(),
                                GITHUB_ENV=(self.root / 'env').as_posix(),
                                GITHUB_STEP_SUMMARY=(self.root / 'summary').as_posix(),
                                GITHUB_REF='refs/heads/fixture', GITHUB_RUN_ID='1', GITHUB_RUN_ATTEMPT='1',
                                DN2CPP_PREMERGE_DIRNAME='initialized evidence', PREMERGE_HOST='linux',
                                DN2CPP_PYTHON=Path(sys.executable).as_posix(), FIXTURE_COMMANDS='',
                                CONFIG='Debug', JOBS='2', CMAKE_BUILD_PARALLEL_LEVEL='2')

    def invoke(self, *arguments, **environment):
        script = '''if [ -n "$FIXTURE_COMMANDS" ]; then
    commands="$FIXTURE_COMMANDS"
    if command -v cygpath >/dev/null 2>&1; then commands=$(cygpath -u "$commands"); fi
    export PATH="$commands:$PATH"
fi
bash "$@"'''
        return run_bash(script, ['fixture', (self.repo / 'gates/ci-premerge.sh').as_posix(), *arguments],
                        env=dict(self.environment, **environment), timeout=20)

    def test_initialize_checks_dispatch_before_exporting_tmpdir(self):
        result = run_bash(step_script('Initialize pre-merge evidence'), ['fixture'],
                          env=self.environment, cwd=self.repo, timeout=20)
        self.assertEqual(result.returncode, 0, result.stderr)
        initialized = self.root / self.environment['DN2CPP_PREMERGE_DIRNAME']
        self.assertEqual((initialized / 'initialize.exit').read_bytes(), b'0\n')
        exports = (self.root / 'env').read_text(encoding='utf-8')
        self.assertIn('TMPDIR=', exports)
        self.assertIn(self.head, (initialized / 'dispatch.txt').read_text(encoding='utf-8'))

    def test_initialize_rejects_invalid_or_different_sha(self):
        for index, expected in enumerate(('', self.head[:7], 'F' * 40, '0' * 40, '$(touch injected)')):
            with self.subTest(expected=expected):
                env = dict(self.environment, EXPECTED_HEAD=expected,
                           DN2CPP_PREMERGE_DIRNAME=f'invalid-{index}', GITHUB_ENV=(self.root / f'env-{index}').as_posix())
                result = run_bash(step_script('Initialize pre-merge evidence'), ['fixture'],
                                  env=env, cwd=self.repo, timeout=20)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual((self.root / f'invalid-{index}/initialize.exit').read_bytes(), b'1\n')
                self.assertNotIn('TMPDIR=', (self.root / f'env-{index}').read_text(encoding='utf-8'))
        self.assertFalse((self.repo / 'injected').exists())

    def test_identity_records_the_tree_and_rejects_changed_source(self):
        result = self.invoke('identity')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.logs / 'source-identity.txt').read_text(encoding='utf-8').splitlines()[0], self.head)
        (self.repo / 'untracked').write_text('dirty', encoding='utf-8')
        result = self.invoke('identity')
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.logs / 'identity.exit').read_bytes(), b'1\n')

    def test_identity_rejects_checkout_or_event_mismatch(self):
        for variable in ('EXPECTED_HEAD', 'GITHUB_SHA'):
            with self.subTest(variable=variable):
                result = self.invoke('identity', **{variable: '0' * 40})
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(b'commit differ', (self.logs / 'identity.stderr').read_bytes())

    def test_capture_preserves_streams_and_failure_status(self):
        for status in (0, 17):
            with self.subTest(status=status):
                result = self.invoke('capture', 'sdk', 'bash', '-c',
                                     f"printf 'sdk stdout\\r\\n'; printf 'sdk stderr\\n' >&2; exit {status}")
                self.assertEqual(result.returncode, status)
                self.assertEqual((self.logs / 'sdk.exit').read_bytes(), f'{status}\n'.encode())
                self.assertEqual((self.logs / 'sdk.stdout').read_bytes(), b'sdk stdout\r\n')
                self.assertEqual((self.logs / 'sdk.stderr').read_bytes(), b'sdk stderr\n')

    def test_run_uses_strict_debug_entry_and_preserves_failure(self):
        for status in (0, 23):
            with self.subTest(status=status):
                result = self.invoke('run', FIXTURE_EXIT=str(status))
                self.assertEqual(result.returncode, status, result.stderr)
                self.assertEqual((self.logs / 'runner-args.txt').read_bytes(), b'--skip-godot\n')
                self.assertEqual((self.logs / 'runner-env.txt').read_bytes(), b'Debug 2 2\n')
                self.assertEqual((self.logs / 'premerge.exit').read_bytes(), f'{status}\n'.encode())
                self.assertEqual((self.logs / 'premerge.stdout').read_bytes(), b'native stdout\r\n')
                self.assertTrue((self.logs / 'premerge.started').is_file())
                self.assertTrue((self.logs / 'premerge.finished').is_file())

    def test_run_rechecks_identity_and_configuration(self):
        for env in ({'EXPECTED_HEAD': '0' * 40}, {'CONFIG': 'Release'}):
            with self.subTest(env=env):
                result = self.invoke('run', **env)
                self.assertNotEqual(result.returncode, 0)
                self.assertFalse((self.logs / 'runner-args.txt').exists())

    def test_early_exit_reaps_monitor_without_waiting_for_a_probe_child(self):
        commands = self.root / 'monitor commands'
        commands.mkdir()
        for name, source in {
            'df': '''#!/usr/bin/env bash
printf 'disk blocked fixture\\n'
sleep 0.2
: > "$DN2CPP_PREMERGE_LOGROOT/probe-finished"
''',
            'python3': '''#!/usr/bin/env bash
printf '%s\\n' "$$" > "$DN2CPP_PREMERGE_LOGROOT/monitor.pid"
exec "$FIXTURE_PYTHON" "$@"
''',
        }.items():
            command = commands / name
            command.write_text(source, encoding='utf-8', newline='\n')
            command.chmod(0o755)
        result = self.invoke('run', FIXTURE_COMMANDS=commands.as_posix(), DN2CPP_PYTHON='',
                             FIXTURE_WAIT_MONITOR='1', FIXTURE_PYTHON=Path(sys.executable).as_posix())
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse((self.logs / 'probe-finished').exists(), 'monitor waited for its blocked df child')
        pid = (self.logs / 'monitor.pid').read_text(encoding='utf-8').strip()
        result = run_bash('! kill -0 "$1" 2>/dev/null', ['fixture', pid], timeout=20)
        self.assertEqual(result.returncode, 0, 'monitor is still alive after pre-merge ended')

    def test_monitor_falls_back_from_an_unusable_python3_alias(self):
        commands = self.root / 'alias commands'
        commands.mkdir()
        for name, source in {
            'python3': '''#!/usr/bin/env bash
: > "$DN2CPP_PREMERGE_LOGROOT/alias-attempted"
exit 42
''',
            'python': '''#!/usr/bin/env bash
: > "$DN2CPP_PREMERGE_LOGROOT/python-selected"
exec "$FIXTURE_PYTHON" "$@"
''',
        }.items():
            command = commands / name
            command.write_text(source, encoding='utf-8', newline='\n')
            command.chmod(0o755)
        result = self.invoke('run', FIXTURE_COMMANDS=commands.as_posix(), DN2CPP_PYTHON='',
                             FIXTURE_WAIT_MONITOR='1', FIXTURE_PYTHON=Path(sys.executable).as_posix())
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((self.logs / 'alias-attempted').is_file())
        self.assertTrue((self.logs / 'python-selected').is_file())
        self.assertIn('disk ', (self.logs / 'disk-periodic.log').read_text(encoding='utf-8'))

    def test_outcome_requires_matching_successful_debug_receipt(self):
        self.assertEqual(self.invoke('run').returncode, 0)
        result = self.invoke('outcome', PREMERGE_OUTCOME='success')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('NON-GODOT PRE-MERGE PASSED', (self.root / 'summary').read_text(encoding='utf-8'))
        for env in ({'FIXTURE_CONFIG': 'Release'}, {'FIXTURE_COMMIT': '0000000'}, {'FIXTURE_EXIT': '1'}):
            with self.subTest(env=env):
                self.invoke('run', **env)
                result = self.invoke('outcome', PREMERGE_OUTCOME='success')
                self.assertNotEqual(result.returncode, 0)
                self.assertTrue((self.logs / 'receipt-validation-error.txt').is_file())
        (self.logs / '_receipt.txt').unlink()
        self.assertNotEqual(self.invoke('outcome', PREMERGE_OUTCOME='success').returncode, 0)

    def test_outcome_compares_numeric_shas_as_text(self):
        commands = self.root / 'numeric sha commands'
        commands.mkdir()
        real_git = shutil.which('git')
        self.assertIsNotNone(real_git)
        command = commands / 'git'
        command.write_text('''#!/usr/bin/env bash
if [ "$#" = 3 ] && [ "$1" = rev-parse ] && [ "$2" = --short ] && [ "$3" = HEAD ]; then
    printf '%s\\n' "$FIXTURE_SHORT_HEAD"
else
    exec "$FIXTURE_REAL_GIT" "$@"
fi
''', encoding='utf-8', newline='\n')
        command.chmod(0o755)
        for short_head, different in (('0e89710', '0000000'), ('1e00006', '1000000')):
            with self.subTest(short_head=short_head, different=different):
                environment = dict(PREMERGE_OUTCOME='success', FIXTURE_COMMANDS=commands.as_posix(),
                                   FIXTURE_REAL_GIT=Path(real_git).as_posix(), FIXTURE_SHORT_HEAD=short_head)
                self.assertEqual(self.invoke('run', FIXTURE_COMMIT=short_head).returncode, 0)
                result = self.invoke('outcome', **environment)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(self.invoke('run', FIXTURE_COMMIT=different).returncode, 0)
                result = self.invoke('outcome', **environment)
                self.assertNotEqual(result.returncode, 0)
                self.assertTrue((self.logs / 'receipt-validation-error.txt').is_file())

    def test_failed_or_skipped_step_never_reuses_a_receipt(self):
        self.assertEqual(self.invoke('run').returncode, 0)
        for outcome in ('failure', 'cancelled', 'skipped'):
            with self.subTest(outcome=outcome):
                summary = self.root / f'summary-{outcome}'
                result = self.invoke('outcome', PREMERGE_OUTCOME=outcome, GITHUB_STEP_SUMMARY=summary.as_posix())
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn('PASSED', summary.read_text(encoding='utf-8'))
                self.assertIn('not a pass', summary.read_text(encoding='utf-8'))

    def test_receipt_cannot_override_dispatched_commit(self):
        self.assertEqual(self.invoke('run').returncode, 0)
        result = self.invoke('outcome', PREMERGE_OUTCOME='success', GITHUB_SHA='0' * 40)
        self.assertNotEqual(result.returncode, 0)

    def test_initialization_failure_still_records_outcomes_without_checkout(self):
        env = dict(self.environment, INIT_OUTCOME='failure', CHECKOUT_OUTCOME='skipped',
                   IDENTITY_OUTCOME='skipped', PYTHON_OUTCOME='skipped', SDK_OUTCOME='skipped',
                   SETUP_OUTCOME='skipped', PREMERGE_OUTCOME='skipped')
        result = run_bash(step_script('Record pre-merge outcome'), ['fixture'],
                          env=env, cwd=self.root, timeout=20)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('initialize=failure', (self.logs / 'step-outcomes.txt').read_text(encoding='utf-8'))
        self.assertIn('not a pass', (self.root / 'summary').read_text(encoding='utf-8'))

    def test_all_hosts_share_manual_scope_without_smoke_opt_outs(self):
        for host in ('linux', 'macos', 'windows'):
            with self.subTest(host=host):
                source = (ROOT / f'.github/workflows/{host}-smoke.yml').read_text(encoding='utf-8')
                smoke, premerge = source.split('\n  premerge-debug:\n')
                self.assertIn('options: [suite, premerge-debug]', smoke)
                self.assertIn("if: github.event_name != 'workflow_dispatch' || inputs.mode != 'premerge-debug'", smoke)
                self.assertIn("if: github.event_name == 'workflow_dispatch' && inputs.mode == 'premerge-debug'", premerge)
                self.assertIn('uses: ./.github/workflows/premerge-debug.yml', premerge)
                self.assertIn(f'host: {host}', premerge)
        self.assertNotIn('DN2CPP_SKIP_PLATFORM_ISA_NATIVE', WORKFLOW)
        self.assertNotIn('--skip-xcode', WORKFLOW)
        self.assertIn('shell: bash', WORKFLOW)
        self.assertIn('include-hidden-files: true', WORKFLOW)


if __name__ == '__main__':
    unittest.main()
