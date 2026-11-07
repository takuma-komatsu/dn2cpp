#!/usr/bin/env python3
"""Verify native project graph records and isolation against stale CLR intermediates."""
import os
from pathlib import Path
import subprocess
import tempfile
import sys

fork = Path(sys.argv[1]).resolve()
with tempfile.TemporaryDirectory(prefix='unreal-project-outputs-') as temporary:
    root = Path(temporary)
    (root / "NuGet.Config").write_text("<configuration><packageSources><clear /></packageSources></configuration>")
    for name in ('App', 'Dependency', 'TwinA', 'TwinB', 'Unrelated'):
        project = root / name
        project.mkdir()
        reference = '<ItemGroup><ProjectReference Include="../Dependency/Dependency.csproj" /><ProjectReference Include="../TwinA/Twin.csproj" /><ProjectReference Include="../TwinB/Twin.csproj" /></ItemGroup>' if name == 'App' else ''
        (project / (('Twin' if name.startswith('Twin') else name) + '.csproj')).write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>{name}</AssemblyName></PropertyGroup><Import Project="{fork}/UnrealSharp.Build.props" />{reference}</Project>')
        (project / 'Code.cs').write_text(f'public class {name} {{ }}')
    env = dict(os.environ, DOTNET_CLI_HOME='/tmp/dn2cpp-unreal-dotnet')
    def run(*arguments):
        subprocess.run(['dotnet', *arguments, '-m:1', '-p:BuildInParallel=false', '-p:RestoreDisableParallel=true', '--disable-build-servers'], env=env, check=True)
    app = str(root / 'App/App.csproj')
    run('build', app, '-c', 'Release')
    stale = root / 'App/obj/Release/Stale.cs'
    stale.write_text('this old generated source must never compile')
    records = root / 'records'
    records.mkdir()
    publish = root / 'publish'
    run('publish', app, '-c', 'Release', '-p:PackagingBackend=Dn2Cpp', '-p:UETargetType=Game', '-p:UEBuildConfig=Shipping',
        '-p:BaseOutputPath=bin/Dn2Cpp/Game/Shipping/arm64/',
        '-p:BaseIntermediateOutputPath=obj/Dn2Cpp/Game/Shipping/arm64/',
        f'-p:CustomAfterMicrosoftCommonTargets={fork}/Build/Scripts/Dn2Cpp.ProjectOutputs.targets',
        f'-p:Dn2CppProjectOutputRecords={records}', f'-p:PublishDir={publish}/')
    assert {p.read_text().splitlines()[0] for p in records.glob('*.txt')} == {'App.dll', 'Dependency.dll', 'TwinA.dll', 'TwinB.dll'}
    for name in ('App', 'Dependency', 'TwinA', 'TwinB'):
        assert (root / name / f'obj/Dn2Cpp/Game/Shipping/arm64/Release/net10.0/{name}.dll').exists()
        assert (publish / (name + '.dll')).exists()
    assert stale.exists()
    assert (root / 'App/bin/Release/net10.0/App.dll').exists()
print('UNREALSHARP_PROJECT_OUTPUTS_OK')
