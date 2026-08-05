#!/usr/bin/env bash
# ONE-OFF measurement harness (NOT a regression gate — filename is outside the
# `build-and-run-*.sh` glob so run-all-gates.sh ignores it). Quantifies the
# unsupported-IL / unmapped-intrinsic surface that transpiling the REAL
# Grpc.Net.Client + Google.Protobuf client stack drags in, over a small
# unary-RPC reach probe (samples/dotnet/GrpcProbe) that never talks to a
# server — see that probe's Program.cs header.
#
# It transpiles the probe in `--measure` mode (enumerates every reachable gap
# instead of stopping at the first, emits no C++) against the real net10.0
# CoreLib plus every NuGet-restored, non-framework dll actually present in the
# probe's own Release bin dir (Grpc.Net.Client, Grpc.Net.Common, Grpc.Core.Api,
# Google.Protobuf, and whatever Microsoft.Extensions.* shims those pull in) —
# with --auto-ref pulling in the transitive shared-framework closure on top
# (System.Memory, System.Buffers, …) so no hand-enumerated BCL -r
# whack-a-mole.
#
# Output: <out>/s0-gaps.tsv (phase|namespace|method|exception|message) + a
# clustered stdout summary (phase, namespace, exception, normalized message —
# read gaps as CLUSTERS, not rows, same convention as measure-real-json.sh /
# selfhost-measure.sh).
set -euo pipefail
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/measure-grpc}"
PROBE=GrpcProbe

echo "== 1/5 Building CLI + $PROBE =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
build_proj "samples/dotnet/$PROBE/$PROBE.csproj"
CLI="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: not built: $CLI" >&2; exit 1; }

BIN="samples/dotnet/$PROBE/bin/$CONFIG/$TFM"
APP="$BIN/$PROBE.dll"
[ -f "$APP" ] || { echo "error: not built: $APP" >&2; exit 1; }

echo "== 2/5 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corelib: $corelib"

echo "== 3/5 Deriving -r set from $BIN (every NuGet-restored dll actually present) =="
# Everything CopyLocal in the probe's own bin dir IS the NuGet + transitive
# closure dotnet build resolved — read it from the directory rather than
# hardcoding names, so a future package bump (a new transitive dependency
# landing, or one dropping out) is picked up automatically instead of silently
# under- or over-refing.
refs=(-r "$corelib")
for dll in "$BIN"/*.dll; do
    name=$(basename "$dll" .dll)
    [ "$name" = "$PROBE" ] && continue         # the app itself — passed positionally
    refs+=(-r "$dll")
done
echo "extra refs:"
printf '  %s\n' "${refs[@]:1}"

echo "== 4/5 Measure (real Grpc.Net.Client + Google.Protobuf, --auto-ref) =="
mkdir -p "$OUT"
# --measure returns 0 normally; --auto-ref keeps Build() from aborting on an
# external generic. Tolerate a nonzero exit so the TSV is written regardless
# (mirrors gates/measure-real-json.sh / gates/dotnet-measure.sh).
dotnet exec "$CLI" "$APP" "${refs[@]}" --auto-ref --measure -o "$OUT" \
    || true
TSV="$OUT/s0-gaps.tsv"
[ -f "$TSV" ] || { echo "error: --measure produced no $TSV" >&2; exit 1; }

echo "== 5/5 Gap inventory =="
total=$(wc -l < "$TSV" | tr -d ' ')
echo "total gaps: $total"
echo
echo "-- gaps by namespace (top 15) --"
head -15 <<<"$(LC_ALL=C cut -f2 "$TSV" | LC_ALL=C sort | uniq -c | sort -rn)"
echo
echo "-- gaps by exception (top 10) --"
head -10 <<<"$(LC_ALL=C cut -f4 "$TSV" | LC_ALL=C sort | uniq -c | sort -rn)"
echo
echo "-- clustered gaps: phase | namespace | exception | normalized message (top 30) --"
# head as a here-string consumer, never a pipeline tail — the pipeline form is the
# early-exiting-consumer shape _common.sh bans (and doc-claims counts).
head -30 <<<"$(LC_ALL=C cut -f1,2,4,5 "$TSV" \
    | LC_ALL=C sed -E 's/\t[^\t]+: /\t/; s/\[chain:.*$//; s/<[0-9]+>/<N>/g; s/_[A-Za-z0-9_]+::/::/g' \
    | LC_ALL=C sort | uniq -c | sort -rn)"

echo
echo "full list: $TSV"
