#!/usr/bin/env python3
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('package_native', Path(__file__).with_name('package-native.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class NativePackagingTests(unittest.TestCase):
    def test_builds_game_and_stages_only_native_and_manifests(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            managed = root / 'input/custom-publish-folder'
            managed.mkdir(parents=True)
            for name in (*module.PLUGIN_ASSEMBLIES, 'Game'):
                (managed / (name + '.dll')).touch()
            (managed / 'UserCode.LoadOrder.json').write_text(json.dumps({'Priority': 0, 'Collectible': False, 'LoadOrder': ['Game']}))
            cli = root / 'src/Dn2Cpp.Cli/bin/Release/net10.0/dn2cpp.dll'
            cli.parent.mkdir(parents=True)
            cli.touch()
            config = root / 'DefaultUnrealSharp.ini'
            config.write_text('[/Script/UnrealSharpCore.CSUnrealSharpSettings]\nPackagingBackend=Dn2Cpp\n')
            calls = []
            def run(*args):
                args = [str(arg) for arg in args]
                calls.append(args)
                if args[:2] == ['cmake', '--build']:
                    output = Path(args[2]) / 'libUnrealSharpGame.dylib'
                    output.parent.mkdir(parents=True)
                    output.touch()
            arguments = ['package-native.py', '--dn2cpp-root', str(root), '--managed', str(managed),
                '--archive', str(root / 'archive'), '--work', str(root / 'work'), '--configuration', 'Development', '--unrealsharp-config', str(config)]
            with patch('sys.argv', arguments), patch.object(module, 'run', run), patch.object(module.shutil, 'which', return_value='/tool'), patch.object(module.subprocess, 'check_output', return_value='library:\n\t@rpath/libUnrealSharpGame.dylib (id)\n\t/usr/lib/libSystem.B.dylib (system)\n'):
                module.main()
            self.assertEqual(calls[0][2], str((managed / 'Game.dll').resolve()))
            self.assertIn('--auto-ref', calls[0])
            self.assertIn('--unrealsharp-load-order', calls[0])
            self.assertIn('-DCMAKE_BUILD_TYPE=RelWithDebInfo', calls[1])
            self.assertTrue((root / 'archive/Binaries/Mac/libUnrealSharpGame.dylib').exists())
            self.assertTrue((root / 'archive/Binaries/Managed/net10.0/UserCode.LoadOrder.json').exists())
            plugin_manifest = root / 'archive/Binaries/Managed/net10.0' / module.PLUGIN_MANIFEST
            self.assertEqual(json.loads(plugin_manifest.read_text()), {'Priority': 100, 'Collectible': False, 'LoadOrder': module.PLUGIN_ASSEMBLIES})
            cli_manifests = [Path(calls[0][index + 1]) for index, arg in enumerate(calls[0]) if arg == '--unrealsharp-load-order']
            self.assertEqual(cli_manifests[0].read_bytes(), plugin_manifest.read_bytes())
            self.assertFalse((managed / module.PLUGIN_MANIFEST).exists())
            self.assertFalse(list((root / 'archive').rglob('*.dll')))
            for component in ('hostfxr', 'coreclr', 'hostpolicy', 'clrjit'):
                for suffix in ('.dylib', '.so'):
                    runtime_file = root / 'archive' / ('lib' + component + suffix)
                    runtime_file.touch()
                    with self.assertRaisesRegex(SystemExit, 'CLR'):
                        module.validate_native_archive(root / 'archive')
                    runtime_file.unlink()
            contamination = root / 'archive/Binaries/Managed/net10.0/libcoreclr.dylib'
            contamination.touch()
            with patch('sys.argv', arguments), patch.object(module, 'run') as runner:
                with self.assertRaisesRegex(SystemExit, 'CLR'):
                    module.main()
                runner.assert_not_called()
            editor_output = root / 'Binaries/Managed/net10.0/Editor.dll'
            editor_output.parent.mkdir(parents=True)
            editor_output.write_bytes(b'editor-clr')
            app = root / 'RenamedGame.app'
            (app / 'Contents/UE/Game/Binaries/Mac').mkdir(parents=True)
            app_arguments = list(arguments)
            app_arguments[app_arguments.index('--archive') + 1] = str(app)
            app_arguments[app_arguments.index('--configuration') + 1] = 'Shipping'
            def app_run(*args):
                run(*args)
                if str(args[0]) == 'codesign' and str(args[-1]).endswith('.app'):
                    self.assertTrue((app / 'Contents/UE/Game/Binaries/Managed/net10.0/UnrealSharpBuild.flag').exists())
                    self.assertTrue((app / 'Contents/UE/Game/Binaries/Mac/libUnrealSharpGame.dylib').exists())
            with patch('sys.argv', app_arguments), patch.object(module, 'run', app_run), patch.object(module.shutil, 'which', return_value='/tool'), patch.object(module.subprocess, 'check_output', return_value='library:\n\t@rpath/libUnrealSharpGame.dylib (id)\n\t/usr/lib/libSystem.B.dylib (system)\n'):
                module.main()
            self.assertEqual(editor_output.read_bytes(), b'editor-clr')
            self.assertEqual(calls[-1], ['codesign', '--verify', '--deep', '--strict', str(app.resolve())])
            rejected = root / 'rejected'
            rejected_arguments = list(arguments)
            rejected_arguments[rejected_arguments.index('--archive') + 1] = str(rejected)
            with patch('sys.argv', rejected_arguments), patch.object(module, 'run', run), patch.object(module.shutil, 'which', return_value='/tool'), patch.object(module.subprocess, 'check_output', return_value='library:\n\t@rpath/libUnrealSharpGame.dylib (id)\n\t/opt/local/libdependency.dylib (custom)\n'):
                with self.assertRaisesRegex(SystemExit, 'Unstaged native dependency'):
                    module.main()
            self.assertFalse((rejected / 'Binaries').exists())

    def test_backend_and_whole_archive_validation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            config = root / 'DefaultUnrealSharp.ini'
            config.write_text('[/Script/UnrealSharpCore.CSUnrealSharpSettings]\nPackagingBackend=Clr\n')
            with self.assertRaisesRegex(SystemExit, 'before cooking'):
                module.validate_backend_config(config)
            plugin = root / 'Game.app/Contents/UE/Engine/Plugins/UnrealSharp/Binaries/Managed/net10.0/UnrealSharp.dll'
            plugin.parent.mkdir(parents=True)
            plugin.touch()
            with self.assertRaisesRegex(SystemExit, 'CLR binaries'):
                module.validate_native_archive(root / 'Game.app')
            plugin.unlink()
            module.validate_native_archive(root / 'Game.app')

    def test_manifest_order_and_conflicts(self):
        with tempfile.TemporaryDirectory() as directory:
            managed = Path(directory)
            for name in (*module.PLUGIN_ASSEMBLIES, 'Game', 'Glue'):
                (managed / (name + '.dll')).touch()
            for name, priority, assemblies in [('UserCode', 0, ['Game']), ('GlueCode', 100, ['Glue']), ('UnrealSharp', 100, module.PLUGIN_ASSEMBLIES)]:
                (managed / (name + '.LoadOrder.json')).write_text(json.dumps({'Priority': priority, 'Collectible': False, 'LoadOrder': assemblies}))
            records, names = module.load_manifests(managed)
            self.assertEqual(list(records), [module.PLUGIN_MANIFEST, 'GlueCode.LoadOrder.json', 'UserCode.LoadOrder.json'])
            self.assertEqual(names, [*module.PLUGIN_ASSEMBLIES, 'Glue', 'Game'])
            (managed / 'UnrealSharp.Core.dll').unlink()
            with self.assertRaisesRegex(SystemExit, 'missing assembly: UnrealSharp.Core'):
                module.load_manifests(managed)
            (managed / 'UnrealSharp.Core.dll').touch()
            (managed / 'UnrealSharp.LoadOrder.json').write_text(json.dumps({'Priority': 100, 'Collectible': False, 'LoadOrder': ['UnrealSharp.Core']}))
            with self.assertRaisesRegex(SystemExit, 'Conflicting'):
                module.load_manifests(managed)



if __name__ == '__main__':
    unittest.main()
