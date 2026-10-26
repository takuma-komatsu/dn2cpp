#!/usr/bin/env python3
"""Offline provisioning tests; installers and SDK downloads are never executed."""
import hashlib
import importlib.util
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('reference_setup', ROOT / 'gates/setup-culture-reference.py')
setup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(setup)


class ReferenceSetupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='reference test space ')
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.environment = {'RUNNER_TEMP': str(self.directory),
                            'GITHUB_ENV': str(self.directory / 'env'),
                            'GITHUB_PATH': str(self.directory / 'path'),
                            'DOTNET_ROOT_X64': 'old-global-root'}
        self.data = b'controlled offline installer fixture\n'

    def invoke(self, platform='linux', sdk=b'10.0.402\n', runtimes=b'Microsoft.NETCore.App 10.0.13 [root]\n', fail=False, valid_hash=True):
        hashes = {key: hashlib.sha256(self.data).hexdigest() if valid_hash else 'wrong'
                  for key in ('sh', 'ps1')}
        with patch.object(setup.shutil, 'which', return_value=str(self.directory / 'PowerShell Space' / 'pwsh')), patch.object(setup.sys, 'platform', platform), patch.object(setup, 'INSTALLER_HASHES', hashes), patch.object(setup.urllib.request, 'urlopen', return_value=io.BytesIO(self.data)) as download, patch.object(setup.subprocess, 'run', side_effect=subprocess.CalledProcessError(1, []) if fail else None) as run, patch.object(setup.subprocess, 'check_output', side_effect=[sdk, runtimes]):
            self.installer_run = run
            install = setup.install_reference(self.environment)
        return install, run.call_args, download.call_args

    def test_exact_sdk_only_all_hosts_and_isolated_exports(self):
        installs = []
        for platform in ('linux', 'darwin', 'win32'):
            with self.subTest(platform=platform):
                install, run, download = self.invoke(platform)
                installs.append(install)
                command = run.args[0]
                self.assertIn(setup.SDK, command)
                self.assertIn(str(install), command)
                self.assertFalse(any(arg.lower() in ('--channel', '-channel', '--runtime', '-runtime') for arg in command))
                # Windows may expand a RUNNER~1 short temp parent to runneradmin.
                self.assertEqual(command[0], str((self.directory / 'PowerShell Space' / 'pwsh').resolve()) if platform == 'win32' else 'bash')
                self.assertNotIn('-ExecutionPolicy', command)
                self.assertIn(setup.INSTALLER_COMMIT, download.args[0])
                self.assertEqual(run.kwargs['env']['DOTNET_ROOT_X64'], str(install))
                self.assertEqual(run.kwargs['env']['DOTNET_MULTILEVEL_LOOKUP'], '0')
                self.assertTrue(run.kwargs['check'])
                self.assertIn(str(install).encode(), (self.directory / 'path').read_bytes())
        self.assertEqual(len(set(installs)), 3)
        self.assertNotIn(b'\r', (self.directory / 'env').read_bytes())

    def test_windows_powershell_path_is_canonical(self):
        location = self.directory / 'PowerShell Space' / '..' / 'PowerShell Space' / 'pwsh'
        with patch.object(setup.shutil, 'which', return_value=str(location)):
            command = setup.installer_command(Path('installer.ps1'), Path('isolated root'), True)
        self.assertEqual(command[0], str(location.resolve()))
        self.assertTrue(Path(command[0]).is_absolute())

    def test_windows_requires_powershell(self):
        with patch.object(setup.shutil, 'which', return_value=None):
            with self.assertRaisesRegex(RuntimeError, 'PowerShell Core'):
                setup.installer_command(Path('installer.ps1'), Path('isolated root'), True)

    def test_checksum_mismatch_never_executes(self):
        with self.assertRaisesRegex(RuntimeError, 'checksum'):
            self.invoke(valid_hash=False)
        self.installer_run.assert_not_called()
        self.assertFalse((self.directory / 'env').exists())

    def test_installer_failure_never_exports(self):
        with self.assertRaises(subprocess.CalledProcessError):
            self.invoke(fail=True)
        self.assertFalse((self.directory / 'env').exists())

    def test_wrong_or_additional_runtime_rejected(self):
        for runtimes in (b'Microsoft.NETCore.App 10.0.14 [root]\n',
                         b'Microsoft.NETCore.App 10.0.13 [root]\nMicrosoft.NETCore.App 10.0.14 [root]\n'):
            with self.subTest(runtimes=runtimes), self.assertRaisesRegex(RuntimeError, 'Unexpected isolated'):
                self.invoke(runtimes=runtimes)
        self.assertFalse((self.directory / 'env').exists())

    def test_wrong_sdk_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'Unexpected isolated'):
            self.invoke(sdk=b'10.0.403\n')

    def test_workflows_keep_exact_setup_without_optional_icu_preflight(self):
        for platform in ('linux', 'macos', 'windows'):
            source = (ROOT / f'.github/workflows/{platform}-smoke.yml').read_text(encoding='utf-8')
            self.assertNotIn('uses: actions/setup-dotnet', source)
            self.assertIn('run: python gates/setup-culture-reference.py', source)
            self.assertNotIn('bash gates/check-culture-reference.sh', source)
            self.assertLess(source.index('actions/setup-python'), source.index('run: python gates/setup-culture-reference.py'))


if __name__ == '__main__':
    unittest.main()
