#!/usr/bin/env bash
# ONE-OFF measurement harness (NOT a regression gate — filename is outside the
# `build-and-run-*.sh` glob so run-all-gates.sh ignores it). The godot-dotnet
# sibling of gates/measure-real-json.sh: it quantifies the unsupported-IL /
# unmapped-intrinsic surface that transpiling the REAL GodotSharp assembly (built
# from the pinned godot clone by gates/setup-godot-dotnet.sh) plus a real
# Godot.NET.Sdk game project drags in, to drive the mono-module drop-in
# gap-closure slices to zero.
#
# It builds samples/godot-dotnet/DotnetSample (a real-style Godot C# project
# restored from the local nuget feed) and transpiles it in `--measure` mode
# (enumerates every reachable gap instead of stopping at the first, emits no
# C++) against the real net10.0 CoreLib + the real GodotSharp.dll, with
# --auto-ref pulling the transitive shared-framework closure.
#
# Requires the artifacts of gates/setup-godot-dotnet.sh, resolved through
# gates/_godot_dotnet.sh exactly like the six godot-dotnet gates:
# DN2CPP_GODOT_DOTNET_ROOT overrides where to look, defaulting to the setup
# script's own cache root — artifacts built there are found with no env var
# set. Opts out via gate_skip (exit 77) when absent — never "SKIP + exit 0",
# which is indistinguishable from a pass.
#
# nuget.config note: nuget.config cannot expand environment variables, and the
# MSBuild NuGet SDK resolver (which resolves `Sdk="Godot.NET.Sdk/..."` before any
# restore flag applies) discovers config by walking up from the project
# directory — so godot_dotnet_nuget_config GENERATES samples/godot-dotnet/
# DotnetSample/nuget.config (gitignored) pointing at the artifact root's feed.
# The checked-in sample therefore carries no machine-specific absolute path.
#
# Output: <out>/s0-gaps.tsv (phase|namespace|method|exception|message), a copy in
# gates/out-dotnet-measure/gaps.txt (gitignored), and a grouped stdout summary.
# Read gaps as CLUSTERS, not rows: one unmapped member usually accounts for a
# whole namespace's worth of rows, so the grouped summary — not the row count —
# is what says how much work is left.
set -euo pipefail
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

OUT="${1:-artifacts/dotnet-measure}"
GAPS_OUT_DIR=gates/out-dotnet-measure
SAMPLE_DIR="$GODOT_DOTNET_SAMPLE_DIR"
ROOT="$GODOT_DOTNET_ROOT"
GODOTSHARP="$GODOT_DOTNET_GODOTSHARP"

if ! godot_dotnet_root_ok; then
    gate_skip "godot-dotnet artifacts absent at $ROOT — run gates/setup-godot-dotnet.sh"
fi

echo "== 1/5 Generating the sample's nuget.config (local feed) =="
godot_dotnet_nuget_config "$SAMPLE_DIR"
echo "wrote $SAMPLE_DIR/nuget.config -> $ROOT/nuget"

echo "== 2/5 Building the sample game assembly (real Godot.NET.Sdk) =="
# ExportRelease is Godot.NET.Sdk's release configuration (its <Configurations>
# are Debug/ExportDebug/ExportRelease); it selects the Release GodotSharp API
# and Optimize=true. The Sdk routes output to .godot/mono/temp/bin/<config>/.
if ! dotnet build "$SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease --nologo -v q; then
    echo "error: sample build failed. Usual causes:" >&2
    echo "  - local feed incomplete: re-run gates/setup-godot-dotnet.sh" >&2
    echo "    (needs Godot.NET.Sdk/GodotSharp/Godot.SourceGenerators nupkgs in $ROOT/nuget)" >&2
    echo "  - stale SDK resolution: delete $SAMPLE_DIR/.godot/mono/temp and retry" >&2
    exit 1
fi
APP="$SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
[ -f "$APP" ] || { echo "error: not built: $APP" >&2; exit 1; }

echo "== 3/5 Building the dn2cpp CLI + locating the net10.0 CoreLib =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
CLI="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: not built: $CLI" >&2; exit 1; }
# Pin net10.0 (the JSON gates' rule): the highest installed runtime can be an
# 11.0 preview whose CoreLib shape skews the transpile spuriously.
corelib=$(resolve_net10_corelib)
echo "corelib:    $corelib"
echo "godotsharp: $GODOTSHARP"

echo "== 4/5 Measure (real GodotSharp + game assembly, --auto-ref) =="
mkdir -p "$OUT" "$GAPS_OUT_DIR"
# --measure returns 0 normally; tolerate a nonzero exit so the TSV is written
# regardless (mirrors gates/measure-real-json.sh).
dotnet exec "$CLI" "$APP" -r "$corelib" -r "$GODOTSHARP" --auto-ref --measure -o "$OUT" \
    || true
TSV="$OUT/s0-gaps.tsv"
[ -f "$TSV" ] || { echo "error: --measure produced no $TSV" >&2; exit 1; }

echo "== 5/5 Gap inventory =="
cp -f "$TSV" "$GAPS_OUT_DIR/gaps.txt"
total=$(wc -l < "$TSV" | tr -d ' ')
echo "total gaps: $total"
echo
echo "-- gaps by namespace (top 15) --"
head -15 <<<"$(LC_ALL=C cut -f2 "$TSV" | LC_ALL=C sort | uniq -c | sort -rn)"
echo
echo "-- gaps by message category (top 25) --"
head -25 <<<"$(LC_ALL=C cut -f5 "$TSV" | LC_ALL=C sed -E 's/^[^ ]+: //; s/\[chain:.*$//; s/<[0-9]+>/<N>/g; s/_[A-Za-z0-9_]+::/::/' \
    | sort | uniq -c | sort -rn)"
echo
echo "full list: $TSV"
echo "copy:      $GAPS_OUT_DIR/gaps.txt"
