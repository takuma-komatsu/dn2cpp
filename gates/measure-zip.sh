#!/usr/bin/env bash
# ONE-OFF measurement harness (NOT a regression gate — filename is outside the
# `build-and-run-*.sh` glob so run-all-gates.sh ignores it). The Zip sibling
# of gates/measure-compression.sh: it quantifies the unsupported-IL /
# unmapped-intrinsic surface the *real* System.IO.Compression ZipArchive /
# ZipArchiveEntry / ZipFile / ZipFileExtensions path (sync + net10 async)
# drags in, ahead of any Zip production work.
#
# It transpiles samples/dotnet/ZipProbe (a throwaway five-section probe:
# in-memory create / fixed-blob read / update / ZipFile filesystem round-trip
# / async APIs) in --measure mode (enumerates every reachable gap instead of
# stopping at the first), emitting NO C++.
#
# Crucially it uses --auto-ref so the transitive shared-framework closure
# resolves automatically, and pins net10.0 via resolve_net10_corelib (NOT
# locate_corelib — this host has 8.0/10.0/11.0-preview runtimes installed
# side by side and the System.IO.Compression native ABI differs across them;
# see the resolve_net10_corelib comment in _common.sh).
#
# Output: <out>/s0-gaps.tsv (phase|namespace|method|exception|message) + a
# grouped stdout summary. Read gaps as CLUSTERS, not rows: one unmapped member
# usually accounts for a whole namespace's worth of rows, so the grouped summary
# — not the row count — is what says how much work is left.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/measure-zip}"

echo "== 1/4 Building CLI + ZipProbe =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
build_proj samples/dotnet/ZipProbe/ZipProbe.csproj
CLI="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: not built: $CLI" >&2; exit 1; }

echo "== 2/4 Locating the real net10.0 CoreLib + Zip assemblies =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
REAL_COMPRESSION="$bcl/System.IO.Compression.dll"
REAL_ZIPFILE="$bcl/System.IO.Compression.ZipFile.dll"
[ -f "$REAL_COMPRESSION" ] || { echo "error: real System.IO.Compression not found: $REAL_COMPRESSION" >&2; exit 1; }
[ -f "$REAL_ZIPFILE" ] || { echo "error: real System.IO.Compression.ZipFile not found: $REAL_ZIPFILE" >&2; exit 1; }
echo "corelib:          $corelib"
echo "real compression: $REAL_COMPRESSION"
echo "real zipfile:     $REAL_ZIPFILE"

app="samples/dotnet/ZipProbe/bin/$CONFIG/$TFM/ZipProbe.dll"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }
mkdir -p "$OUT"

echo "== 3/4 Measure (real Zip surface, --auto-ref) =="
tsv="$OUT/s0-gaps.tsv"
rm -f "$tsv"  # drop any stale report from a prior run
# --measure returns 0 normally; --auto-ref keeps Build() from aborting on an
# external generic. Tolerate a nonzero exit so the TSV summary below still
# prints whatever was written.
# --no-default-ref declines the DnZlib/DnBrotli shims the transpiler now injects
# whenever System.IO.Compression[.Brotli] is in the load set. This harness exists
# to count the gaps the REAL Zip/BCL path drags in; with DnZlib injected the
# CompressionNative_* rows (deflate and the CRC-32 every entry goes through) stop
# being reachable and the numbers would silently describe a different subject.
dotnet exec "$CLI" "$app" -r "$corelib" -r "$REAL_COMPRESSION" -r "$REAL_ZIPFILE" \
    --no-default-ref DnZlib --no-default-ref DnBrotli --auto-ref --measure -o "$OUT" \
    || true

echo "  -> $tsv ($([ -f "$tsv" ] && wc -l < "$tsv" || echo 0) rows)"
if [ -f "$tsv" ]; then
    echo "  -- gaps by message category (top 20) --"
    head -20 <<<"$(LC_ALL=C cut -f5 "$tsv" | LC_ALL=C sed -E 's/^[^ ]+: //; s/\[chain:.*$//; s/<[0-9]+>/<N>/g; s/_[A-Za-z0-9_]+::/::/' \
        | sort | uniq -c | sort -rn)"
fi

echo
echo "== 4/4 Done =="
echo "gaps: $tsv"
