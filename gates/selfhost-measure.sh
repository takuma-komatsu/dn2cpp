#!/usr/bin/env bash
# Self-hosting feasibility harness (NOT a regression gate — the filename is
# deliberately outside the `build-and-run-*.sh` glob so run-all-gates.sh ignores
# it). Transpiles dn2cpp itself (the CLI exe + the transpiler/Godot/runtime DLLs)
# with the real CoreLib + BCL pulled in via -r, in `--measure` mode: instead of
# stopping at the first unsupported-IL / unmapped-intrinsic hit, the CLI compiles
# every reachable method body (and scans reachability) tolerantly and reports
# *every* gap in one pass. Output:
#   - a per-gap TSV  (phase | namespace | method | exception | message)
#   - a grouped summary on stdout (by phase / namespace / message category)
# Re-run after a transpiler change to re-measure the gap surface / catch regressions.
#
# The real BCL System.Linq is referenced directly, mirroring how samples
# transpile. Read the gaps as CLUSTERS, not rows: one unmapped member usually
# accounts for a whole namespace's worth of rows, so the grouped summary — not
# the row count — is what says how much work is left.
#
# Pins net10.0 (resolve_net10_corelib) and references the real System.Text.Json,
# with --auto-ref, exactly like build-and-run-godot-bindgen.sh: the full CLI's
# closure statically roots the Godot lane's --generate-bindings subtree, which
# deserializes extension_api.json through the STJ source-gen JsonTypeInfo<T>
# graph. Without System.Text.Json in the -r set those JsonTypeInfo<T> /
# JsonConverter<T> instantiations resolve as *external* generics and abort the
# measure on the first one; without the net10 pin the 11.0-preview CoreLib shape
# skews the STJ transpile (resolve_net10_corelib in _common.sh). --auto-ref
# eagerly loads the transitive shared-framework closure so every external generic
# resolves before Build().
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/selfhost-s0}"

echo "== 1/4 Building the CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ]  || { echo "error: CLI not built: $CLI"   >&2; exit 1; }

echo "== 2/4 Locating the real net10.0 CoreLib + BCL + System.Text.Json =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
echo "bcl dir: $bcl"

# Reference set: dn2cpp's own DLLs, the real System.Text.Json (the
# --generate-bindings STJ source-gen closure), and the real BCL assemblies
# (including the real System.Linq) dn2cpp's reachable code actually touches.
# Only files that exist are added, so a different runtime layout degrades
# gracefully.
refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Godot.dll"
add_ref "$BIN/Dn2Cpp.DotnetModule.dll"
add_ref "$BIN/Dn2Cpp.Runtime.dll"
add_ref "$corelib"
add_ref "$bcl/System.Text.Json.dll"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.Encodings.Web System.Threading System.Threading.Tasks; do
    add_ref "$bcl/$dep.dll"
done

echo "== 3/4 Transpiling dn2cpp itself (--measure, tree-shaken, --auto-ref) =="
mkdir -p "$OUT"
dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref --measure -o "$OUT"

echo "== 4/4 Done =="
echo "per-gap report: $OUT/s0-gaps.tsv"
