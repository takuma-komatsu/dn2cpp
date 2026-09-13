#!/usr/bin/env python3
"""Exercise the editor's option and argument code without launching native builds."""
import os
import pathlib
import re
import subprocess
import sys
from xml.sax.saxutils import escape

fork, output = map(pathlib.Path, sys.argv[1:])
source_dir = fork / 'modules/mono/editor/GodotTools/GodotTools/Export'
plugin = (source_dir / 'ExportPlugin.cs').read_text(encoding='utf-8')
exporter = (source_dir / 'Dn2CppExporter.cs').read_text(encoding='utf-8')


def method(source, signature):
    start = source.index(signature)
    # Every method ends at this class indentation, including nested lambdas.
    end = source.index('\n        }', start) + len('\n        }')
    return source[start:end].replace('public override ', 'public ')


options = method(plugin, 'public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetExportOptions(')
visibility = method(plugin, 'public override bool _GetExportOptionVisibility(')
transpile = method(exporter, 'private void Transpile(')
read_start = plugin.index('            Variant ilPrestrippingOption =')
read_end = plugin.index('            bool keepWebSymbols =', read_start)
read_options = plugin[read_start:read_end]
# Pin the handoff through BuildDropIn: the extracted methods must be the ones the
# real export invokes, with the same resolved options.
assert re.search(r'BuildDropIn\([^;]+ilPrestripping, optimizationOptions\);', plugin)
assert 'Transpile(publishOutputDir, assemblyName, ilDir, genDir, ilPrestripping, optimizationOptions);' in exporter

fixture = pathlib.Path(__file__).with_suffix('.cs').read_text(encoding='utf-8')
for marker, source in [('OPTIONS', options), ('VISIBILITY', visibility), ('TRANSPILE', transpile), ('READ_OPTIONS', read_options)]:
    fixture = fixture.replace('// INSERT_' + marker, source)
output.mkdir(parents=True, exist_ok=True)
(output / 'Program.cs').write_text(fixture, encoding='utf-8')
helper = escape(str((source_dir / 'Dn2CppOptimizationOptions.cs').resolve()), {'"': '&quot;'})
(output / 'ExportOptions.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <NuGetAudit>false</NuGetAudit><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup><Compile Include="{helper}" Link="Dn2CppOptimizationOptions.cs" /></ItemGroup>
</Project>
''', encoding='utf-8')
subprocess.run(['dotnet', 'run', '--project', str(output / 'ExportOptions.csproj'), '-c', os.environ.get('CONFIG', 'Release'), '--', str(output.resolve() / 'work')], check=True)
