#!/usr/bin/env bash
# Actual CLR PIE entry, automatic managed BeginPlay, Blueprint call and PIE exit.
source "$(dirname "$0")/_common.sh"

[ -n "${UE_ROOT:-}" ] || gate_skip "Set UE_ROOT and run the UnrealSharp engine smoke first"
case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp PIE smoke requires macOS arm64" ;; esac
project="$PWD/artifacts/unrealsharp/Baseline"
editor="$UE_ROOT/Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor"
[ -f "$project/Content/Dn2CppSmokeMap.umap" ] || gate_skip "Run the UnrealSharp engine smoke to build the Editor modules and save its map"
[ -x "$editor" ] || gate_skip "UnrealEditor executable is unavailable"
rm -f "$project/pie-result.txt"
DN2CPP_RUN_WATCHDOG_SECS=180 run_bounded "$editor" "$project/Baseline.uproject" /Game/Dn2CppSmokeMap \
    -Unattended -NullRHI -NoSplash -NoSound -NoAutoRecompile -Dn2CppSmokePIE \
    "-Dn2CppSmokeResult=$project/pie-result.txt" "-abslog=$project/pie-run.log" > "$project/pie-stdout.log" 2>&1
assert_output "$(cat "$project/pie-result.txt")" $'pie=OK\nworld=PIE\nbegin-play=42\nblueprint-override=OK\nblueprint=45\nended=true'
echo "unrealsharp real CLR PIE smoke OK"
