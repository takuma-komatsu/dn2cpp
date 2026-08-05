#!/usr/bin/env bash
# Console-closure self-hosting harness (NOT a regression gate — the filename
# is deliberately outside the `build-and-run-*.sh` glob so run-all-gates.sh
# ignores it). The sibling `selfhost-measure.sh` measures the FULL CLI
# (`dn2cpp.dll`, Transpiler+Godot+Runtime), whose closure statically roots the
# Godot lane's `--generate-bindings` → `Dn2Cpp.Godot.BindingGenerator.Run`
# subtree (~104 gaps: JsonSerializer + Godot-enum-format Enum cascade) — code a
# *console* transpile (the normal `ConsoleBackend` path) never executes (out of
# console-self-host scope).
#
# This script instead transpiles the **console-only CLI** (`dn2cpp-console.dll`,
# which does NOT reference Dn2Cpp.Godot) in `--measure` mode, referencing only
# Transpiler + Runtime + the real CoreLib/BCL (no Godot). The result is the
# **true console-closure blocker count**: the gaps a console transpile actually
# reaches, with the Godot/BindingGenerator subtree excluded by construction.
#
# Output (same shape as selfhost-measure.sh):
#   - a per-gap TSV  (phase | namespace | method | exception | message)
#   - a grouped summary on stdout (by phase / namespace / message category)
#
# The real BCL System.Linq is referenced directly, mirroring how samples
# transpile since the transpiler closed the gap on it.
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/selfhost-console}"

echo "== 1/4 Building the console CLI =="
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
BIN="src/Dn2Cpp.Cli.Console/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp-console.dll"
[ -f "$CLI" ]  || { echo "error: console CLI not built: $CLI" >&2; exit 1; }

echo "== 2/4 Locating the real CoreLib + BCL =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
echo "bcl dir: $bcl"

# Reference set: the console CLI's own DLLs (Transpiler + Runtime — NO Godot)
# and the real BCL assemblies the reachable code actually touches. Only files
# that exist are added, so a different runtime layout degrades gracefully.
refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Runtime.dll"
add_ref "$corelib"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.RegularExpressions System.Collections.Concurrent; do
    add_ref "$bcl/$dep.dll"
done

echo "== 3/4 Transpiling dn2cpp-console itself (--measure, tree-shaken, Godot-free) =="
mkdir -p "$OUT"
dotnet exec "$CLI" "$CLI" "${refs[@]}" --measure -o "$OUT"

echo "== 4/4 Done =="
echo "per-gap report: $OUT/s0-gaps.tsv"
