#!/usr/bin/env bash
# ONE-OFF measurement harness (NOT a regression gate — filename is outside the
# `build-and-run-*.sh` glob so run-all-gates.sh ignores it). The JSON sibling of
# gates/selfhost-measure.sh: it quantifies the
# unsupported-IL / unmapped-intrinsic surface the *real* System.Text.Json
# source-gen path drags in, to drive the gap-closure work to zero.
#
# It transpiles two source-gen probes in `--measure` mode (enumerates every
# reachable gap instead of stopping at the first), emitting NO C++:
#   - JsonProbe       — a minimal source-gen JsonSerializerContext over a small
#                       List<T>-nested schema (the AOT-safe path).
#   - JsonGodotProbe  — the real GodotApi.cs extension_api.json model shape
#                       (classes/methods/properties + utility_functions +
#                       builtin_classes/members/constructors/constants/operators),
#                       i.e. the schema dn2cpp's own engine-binding load path
#                       (Dn2Cpp.Godot.GodotApi.Load) deserializes. Same converter
#                       machinery, richer graph — the metadata-init reflection
#                       surface scales with it, so this is the S-5d/S-5e oracle.
#
# Crucially it uses --auto-ref: only `-r CoreLib -r System.Text.Json` is
# passed and the transitive shared-framework closure (System.Memory,
# System.Collections, System.Text.Encodings.Web, …) is loaded automatically —
# no hand-enumerated BCL `-r` whack-a-mole.
#
# Output per probe: <out>/<probe>/s0-gaps.tsv (phase|namespace|method|exception|
# message) + a grouped stdout summary. Read gaps as CLUSTERS, not rows: one
# unmapped member usually accounts for a whole namespace's worth of rows, so the
# grouped summary — not the row count — is what says how much work is left.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/measure-real-json}"

echo "== 1/4 Building CLI + JSON probes =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
build_proj samples/dotnet/JsonProbe/JsonProbe.csproj
build_proj samples/dotnet/JsonGodotProbe/JsonGodotProbe.csproj
CLI="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: not built: $CLI" >&2; exit 1; }

echo "== 2/4 Locating the real CoreLib + BCL =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
REAL_JSON="$bcl/System.Text.Json.dll"
[ -f "$REAL_JSON" ] || { echo "error: real System.Text.Json not found: $REAL_JSON" >&2; exit 1; }
echo "corelib:    $corelib"
echo "real json:  $REAL_JSON"

measure() {  # $1 = probe project name
    local probe="$1"
    local app="samples/dotnet/$probe/bin/$CONFIG/$TFM/$probe.dll"
    local dir="$OUT/$probe"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }
    mkdir -p "$dir"
    echo
    echo "######## measure[$probe] ########"
    # --measure returns 0 normally; --auto-ref keeps Build() from aborting on an
    # external generic. Tolerate a nonzero exit so the TSV is written regardless.
    dotnet exec "$CLI" "$app" -r "$corelib" -r "$REAL_JSON" --auto-ref --measure -o "$dir" \
        || true
    local tsv="$dir/s0-gaps.tsv"
    echo "  -> $tsv ($([ -f "$tsv" ] && wc -l < "$tsv" || echo 0) rows)"
    if [ -f "$tsv" ]; then
        echo "  -- gaps by message category (top 15) --"
        head -15 <<<"$(LC_ALL=C cut -f5 "$tsv" | LC_ALL=C sed -E 's/^[^ ]+: //; s/\[chain:.*$//; s/<[0-9]+>/<N>/g; s/_[A-Za-z0-9_]+::/::/' \
            | sort | uniq -c | sort -rn)"
    fi
}

echo "== 3/4 Measure (real System.Text.Json source-gen, --auto-ref) =="
measure JsonProbe
measure JsonGodotProbe

echo
echo "== 4/4 Done =="
echo "minimal probe gaps:  $OUT/JsonProbe/s0-gaps.tsv"
echo "GodotApi-shape gaps: $OUT/JsonGodotProbe/s0-gaps.tsv"
