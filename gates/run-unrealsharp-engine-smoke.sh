#!/usr/bin/env bash
# Real-engine CLR Blueprint, interop, lifetime and assembly smoke.
source "$(dirname "$0")/_common.sh"

: "${UE_ROOT:?Set UE_ROOT to the installed Unreal Engine}"
: "${UNREALSHARP_PLUGIN:?Set UNREALSHARP_PLUGIN to the pinned plugin checkout}"
case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp engine smoke requires macOS arm64" ;; esac
plugin=$(cd "$UNREALSHARP_PLUGIN" && pwd -P)
project="$PWD/artifacts/unrealsharp/Baseline"
mkdir -p "$project/Plugins"
cp -Rp samples/unrealsharp/EngineSmoke/. "$project/"
project_plugin="$project/Plugins/UnrealSharp"
if [ "$plugin" != "$project_plugin" ]; then
    [ ! -L "$project_plugin" ] || rm "$project_plugin"
    mkdir -p "$project_plugin"
    rsync -a --delete --exclude=.git --exclude=Binaries --exclude=Intermediate \
        --exclude=bin --exclude=obj "$plugin/" "$project_plugin/"
    plugin="$project_plugin"
fi
for template in "$project"/Script/*/*.csproj.template; do
    cp -p "$template" "${template%.template}"
done
ubt="$UE_ROOT/Engine/Binaries/DotNET/UnrealBuildTool/UnrealBuildTool.dll"
editor="$UE_ROOT/Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor"
modules=Baseline+UnrealSharpCore+UnrealSharpBinds+UnrealSharpAsync+UnrealSharpEditor+UnrealSharpCompiler+UnrealSharpAsyncBlueprint+UnrealSharpRuntimeGlue+UnrealSharpUtilities
solution="$project/Script/ManagedBaseline.slnx"
held_solution="$project/ManagedBaseline.slnx"
rm -f "$project/Script/ManagedBaseline.sln"
mv "$solution" "$held_solution"
trap 'mv "$held_solution" "$solution"' EXIT
dotnet "$ubt" BaselineEditor Mac Development "-Project=$project/Baseline.uproject" \
    -UsePrecompiled -NoEngineChanges -NoManifestChanges -NoHotReload -NoUBA \
    "-Module=$modules" -architecture=arm64 "-Log=$project/build.log"
# UHT may leave its generated glue-only solution beside the sample solution.
# Folder-based packaging must see the sample's single complete solution.
rm -f "$project/Script/ManagedBaseline.sln"
mv "$held_solution" "$solution"
trap - EXIT
dotnet build "$plugin/Managed/UnrealSharp/UnrealSharp.sln" -c Release \
    -p:PackagingBackend=Clr -p:UETargetType=Editor -p:UEBuildConfig=Development
dotnet build "$project/Script/ManagedBaseline/ManagedBaseline.csproj" -c Release \
    -p:PackagingBackend=Clr -p:UETargetType=Editor -p:UEBuildConfig=Development \
    -o "$project/Binaries/Managed/net10.0"

# Module-only UBT builds omit manifests. Use the engine identity the modules
# just linked against, writing only the project and plugin's own manifests.
python3 - "$UE_ROOT" "$project" "$plugin" <<'PY'
import json
import pathlib
import sys
engine, project, plugin = map(pathlib.Path, sys.argv[1:])
identity = json.loads((engine / 'Engine/Binaries/Mac/UnrealEditor.modules').read_text())['BuildId']
for folder in [project / 'Binaries/Mac', plugin / 'Binaries/Mac']:
    modules = {p.name[len('libUnrealEditor-'):-len('.dylib')]: p.name for p in folder.glob('libUnrealEditor-*.dylib')}
    (folder / 'UnrealEditor.modules').write_text(json.dumps({'BuildId': identity, 'Modules': modules}, indent=2) + '\n')
order = project / 'Binaries/Managed/net10.0/ManagedBaseline.LoadOrder.json'
order.write_text(json.dumps({'Priority': 0, 'Collectible': True, 'LoadOrder': ['ManagedDependency', 'ManagedBaseline']}) + '\n')
PY

rm -f "$project/result.txt" "$project/Content/BP_Dn2CppSmoke.uasset" "$project/Content/Dn2CppSmokeMap.umap"
DN2CPP_RUN_WATCHDOG_SECS=240 run_bounded "$editor" "$project/Baseline.uproject" /Engine/Maps/Entry \
    -Unattended -NullRHI -NoSplash -NoSound -NoAutoRecompile -Dn2CppSmoke \
    "-Dn2CppSmokeResult=$project/result.txt" "-abslog=$project/run.log" > "$project/stdout.log" 2>&1
assert_output "$(cat "$project/result.txt")" $'engine-smoke=OK\ncounter=45\nblueprint-override=OK\nasync=1023\nassemblies=42\nlifetime=255\ndestroyed=7\nfeatures=511\nref-out=42:True\nabi-shapes=7\nclr=present'
for marker in 'module-start' 'begin-play=42 tick=False' 'blueprint-call=45 property=45' 'engine-smoke=OK'; do
    if ! grep -Fq "$marker" "$project/run.log"; then
        echo "error: missing smoke marker $marker; inspect $project/run.log" >&2
        exit 1
    fi
done
echo "unrealsharp real-engine CLR smoke OK"
