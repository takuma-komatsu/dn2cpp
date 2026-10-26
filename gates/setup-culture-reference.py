#!/usr/bin/env python3
"""Install only the pinned SDK in a fresh CI root; never add latest LTS runtime."""
import hashlib
import os
from pathlib import Path
import subprocess
import shutil
import sys
import tempfile
import urllib.request

SDK = '10.0.402'
RUNTIME = '10.0.13'
INSTALLER_COMMIT = 'e5cf1dd2d1540ed05ac84f8eb8c5cdec2807621e'
INSTALLER_HASHES = {
    'sh': '082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e',
    'ps1': '3bb07bc8025211836c1e4f9d3f6a044e55b1fb6eec518a6c78851d04e210442b',
}


def installer_command(script, install, windows):
    if windows:
        powershell = shutil.which('pwsh')
        if not powershell:
            raise RuntimeError('PowerShell Core (pwsh) is required on the Windows runner')
        return [str(Path(powershell).resolve()), '-NoLogo', '-NoProfile', '-NonInteractive',
                '-File', str(script), '-Version', SDK, '-InstallDir', str(install), '-NoPath']
    return ['bash', str(script), '--version', SDK, '--install-dir', str(install), '--no-path']


def install_reference(environment):
    # Require CI output files before doing any network or filesystem work.
    runner_temp = Path(environment['RUNNER_TEMP']).resolve()
    env_file, path_file = Path(environment['GITHUB_ENV']), Path(environment['GITHUB_PATH'])
    windows = sys.platform == 'win32'
    extension = 'ps1' if windows else 'sh'
    work = Path(tempfile.mkdtemp(prefix='dn2cpp-culture-reference-', dir=runner_temp))
    install = work / 'dotnet'
    script = work / f'dotnet-install.{extension}'
    url = f'https://raw.githubusercontent.com/dotnet/install-scripts/{INSTALLER_COMMIT}/src/{script.name}'
    with urllib.request.urlopen(url, timeout=60) as response:
        data = response.read()
    if hashlib.sha256(data).hexdigest() != INSTALLER_HASHES[extension]:
        raise RuntimeError(f'Pinned dotnet installer checksum mismatch: {url}')
    script.write_bytes(data)
    selected = dict(environment)
    selected.update(DOTNET_ROOT=str(install), DOTNET_MULTILEVEL_LOOKUP='0')
    # Apphosts prefer architecture-specific ROOT variables over DOTNET_ROOT.
    for architecture in ('X64', 'ARM64', 'X86'):
        selected[f'DOTNET_ROOT_{architecture}'] = str(install)
    subprocess.run(installer_command(script, install, windows), env=selected, check=True)
    dotnet = install / ('dotnet.exe' if windows else 'dotnet')
    sdk = subprocess.check_output([str(dotnet), '--version'], env=selected).strip()
    runtimes = subprocess.check_output([str(dotnet), '--list-runtimes'], env=selected)
    versions = [line.split()[1] for line in runtimes.splitlines()
                if line.startswith(b'Microsoft.NETCore.App ')]
    if sdk != SDK.encode('ascii') or versions != [RUNTIME.encode('ascii')]:
        raise RuntimeError(f'Unexpected isolated SDK/runtime: sdk={sdk!r}, runtimes={runtimes!r}')
    # Existing check-culture-reference.sh subsequently verifies the actual CLR
    # process Environment.Version and ICU, not merely these installed inventories.
    exports = ['DOTNET_ROOT', 'DOTNET_MULTILEVEL_LOOKUP', 'DOTNET_ROOT_X64',
               'DOTNET_ROOT_ARM64', 'DOTNET_ROOT_X86']
    with env_file.open('a', encoding='utf-8', newline='\n') as stream:
        for key in exports:
            stream.write(f'{key}={selected[key]}\n')
    with path_file.open('a', encoding='utf-8', newline='\n') as stream:
        stream.write(str(install) + '\n')
    print(f'Installed exact SDK {SDK}, CoreLib {RUNTIME} at {install}')
    return install


if __name__ == '__main__':
    install_reference(os.environ)
