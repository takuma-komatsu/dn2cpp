#!/usr/bin/env python3
"""Build and stage the closed Game IL output using the dn2cpp native backend."""
import argparse
import configparser
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

PLUGIN_ASSEMBLIES = ['UnrealSharp.Binds', 'UnrealSharp.Log', 'UnrealSharp.Core', 'UnrealSharp.Plugins', 'UnrealSharp']
PLUGIN_MANIFEST = '00UnrealSharp.LoadOrder.json'
MANAGED_FRAMEWORK = 'net10.0'


def validate_backend_config(path):
    config = configparser.ConfigParser(interpolation=None, strict=False, inline_comment_prefixes=(';',))
    if not config.read(path):
        raise SystemExit('Missing UnrealSharp project config: ' + str(path))
    if config.get('/Script/UnrealSharpCore.CSUnrealSharpSettings', 'PackagingBackend', fallback='Clr') != 'Dn2Cpp':
        raise SystemExit('Set PackagingBackend=Dn2Cpp in DefaultUnrealSharp.ini before cooking the native archive')


def validate_native_archive(root):
    if not root.exists():
        return
    for path in root.rglob('*'):
        if not path.is_file():
            continue
        name, suffix = path.name.lower(), path.suffix.lower()
        if suffix == '.dll' or (suffix in ('.dylib', '.so') and any(part in name for part in ('hostfxr', 'coreclr', 'hostpolicy', 'clrjit'))):
            raise SystemExit('Archive contains CLR binaries; use a clean native archive: ' + str(path))


def load_manifests(managed):
    records = {}
    for path in sorted(managed.glob('*.LoadOrder.json')):
        record = json.loads(path.read_text())
        order = record.get('LoadOrder')
        if not isinstance(order, list) or not all(isinstance(name, str) and name for name in order):
            raise SystemExit('Invalid Game load order: ' + path.name)
        if type(record.get('Priority')) is not int or record.get('Collectible') is not False:
            raise SystemExit('Game load order requires integer Priority and Collectible=false: ' + path.name)
        if any(name in PLUGIN_ASSEMBLIES for name in order):
            if order != PLUGIN_ASSEMBLIES or record['Priority'] != 100:
                raise SystemExit('Conflicting UnrealSharp plugin load order: ' + path.name)
            continue
        if path.name == PLUGIN_MANIFEST:
            raise SystemExit('Reserved plugin manifest name: ' + path.name)
        records[path.name] = record
    if not records or not any(record['LoadOrder'] for record in records.values()):
        raise SystemExit('No Game load-order assemblies found')
    records[PLUGIN_MANIFEST] = {'Priority': 100, 'Collectible': False, 'LoadOrder': PLUGIN_ASSEMBLIES}
    records = dict(sorted(records.items(), key=lambda pair: (-pair[1]['Priority'], pair[0])))
    if next(iter(records)) != PLUGIN_MANIFEST:
        raise SystemExit('Game load order must follow the UnrealSharp plugin manifest')
    names = set()
    ordered_names = []
    for record in records.values():
        for name in record['LoadOrder']:
            if name in names or Path(name).name != name or not (managed / (name + '.dll')).is_file():
                raise SystemExit('Duplicate or missing assembly: ' + name)
            names.add(name)
            ordered_names.append(name)
    return records, ordered_names


def run(*arguments):
    subprocess.run([str(argument) for argument in arguments], check=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dn2cpp-root', required=True, type=Path)
    parser.add_argument('--managed', required=True, type=Path)
    parser.add_argument('--archive', required=True, type=Path)
    parser.add_argument('--work', required=True, type=Path)
    parser.add_argument('--configuration', required=True, choices=['Development', 'Shipping'])
    parser.add_argument('--unrealsharp-config', required=True, type=Path)
    parser.add_argument('--identity', default=os.environ.get('DN2CPP_SIGN_IDENTITY', '-'))
    args = parser.parse_args()
    validate_backend_config(args.unrealsharp_config)
    root, managed, archive, work = [p.resolve() for p in (args.dn2cpp_root, args.managed, args.archive, args.work)]
    if archive.suffix == '.app':
        projects = [path for path in (archive / 'Contents/UE').glob('*')
                    if path.is_dir() and path.name != 'Engine' and (path / 'Binaries').is_dir()]
        if len(projects) != 1:
            raise SystemExit('Select the packaged project directory inside the application')
        archive = projects[0]
    app_bundle = next((parent for parent in (archive, *archive.parents) if parent.suffix == '.app'), None)
    if managed == archive / 'Binaries/Managed' or archive / 'Binaries/Managed' in managed.parents:
        raise SystemExit('Managed input must be outside the staged archive')
    scan_root = app_bundle or archive
    validate_native_archive(scan_root)
    managed_stage = archive / 'Binaries/Managed'
    if managed_stage.exists() and any(path.suffix.lower() in ('.dll', '.dylib', '.so') for path in managed_stage.rglob('*')):
        raise SystemExit('Archive already contains CLR binaries; use a clean archive')
    entry = managed / 'UnrealSharp.Plugins.dll'
    cli = root / 'src/Dn2Cpp.Cli/bin/Release/net10.0/dn2cpp.dll'
    if not entry.is_file() or not cli.is_file():
        raise SystemExit('Build the patched Game IL and Release dn2cpp CLI before packaging')
    records, ordered_names = load_manifests(managed)
    entry = managed / (ordered_names[-1] + '.dll')
    for executable in ('dotnet', 'cmake', 'ninja', 'codesign', 'install_name_tool', 'otool', 'xcrun'):
        if shutil.which(executable) is None:
            raise SystemExit('Missing tool: ' + executable)
    work.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='native-', dir=work))
    manifest_directory = work / 'manifests'
    manifest_directory.mkdir()
    manifests = []
    for name, record in records.items():
        manifest = manifest_directory / name
        manifest.write_text(json.dumps(record, ensure_ascii=False) + '\n')
        manifests.append(manifest)
    generated = work / 'generated'
    build = work / 'native'
    command = ['dotnet', str(cli), str(entry), '--auto-ref', '--unrealsharp', '-o', str(generated)]
    for dll in sorted(managed.glob('*.dll')):
        if dll != entry:
            command.extend(['-r', str(dll)])
    for manifest in manifests:
        command.extend(['--unrealsharp-load-order', str(manifest)])
    run(*command)
    compiler = os.environ.get('CMAKE_CXX_COMPILER') or os.environ.get('CXX') or subprocess.check_output(['xcrun', '--sdk', 'macosx', '--find', 'clang++'], text=True).strip()
    sdk = os.environ.get('SDKROOT') or subprocess.check_output(['xcrun', '--sdk', 'macosx', '--show-sdk-path'], text=True).strip()
    run('cmake', '-S', root / 'runtime', '-B', build, '-G', 'Ninja',
        '-DCMAKE_BUILD_TYPE=' + ('Release' if args.configuration == 'Shipping' else 'RelWithDebInfo'), '-DCMAKE_OSX_ARCHITECTURES=arm64', '-DCMAKE_CXX_COMPILER=' + compiler, '-DCMAKE_OSX_SYSROOT=' + sdk,
        '-DDN2CPP_UNREALSHARP=ON', '-DDN2CPP_APP_NAME=UnrealSharpGame',
        '-DDN2CPP_APP_DIR=' + str(generated))
    run('cmake', '--build', build, '--target', 'UnrealSharpGame')
    native = build / 'libUnrealSharpGame.dylib'
    dependencies = subprocess.check_output(['otool', '-L', str(native)], text=True).splitlines()[2:]
    for line in dependencies:
        dependency = line.strip().split(' (', 1)[0]
        if not dependency.startswith(('/usr/lib/', '/System/Library/')):
            raise SystemExit('Unstaged native dependency: ' + dependency)
    run('install_name_tool', '-id', '@rpath/libUnrealSharpGame.dylib', native)
    run('codesign', '--force', '--sign', args.identity, native)
    run('codesign', '--verify', '--strict', native)
    destination = archive / 'Binaries/Mac'
    destination.mkdir(parents=True, exist_ok=True)
    shutil.copy2(native, destination / native.name)
    # Native CSPathsUtilities uses the same runtime-version folder as the CLR lane.
    stage = archive / 'Binaries/Managed' / MANAGED_FRAMEWORK
    stage.mkdir(parents=True, exist_ok=True)
    for old in stage.glob('*.LoadOrder.json'):
        old.unlink()
    for manifest in manifests:
        shutil.copy2(manifest, stage / manifest.name)
    (stage / 'UnrealSharpBuild.flag').touch()
    validate_native_archive(scan_root)
    if app_bundle is not None:
        run('codesign', '--force', '--sign', args.identity, '--preserve-metadata=identifier,entitlements,requirements,flags,runtime', app_bundle)
        run('codesign', '--verify', '--deep', '--strict', app_bundle)


if __name__ == '__main__':
    main()
