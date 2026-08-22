#!/usr/bin/env bash
# The real MemoryPack 1.21.4 NuGet package (MemoryPack.Core + its source generator):
# generated object/struct/record formatters, member ordering, [MemoryPackConstructor],
# the four serialization callbacks, collection and scalar members, Utf8/Utf16 string
# encodings, union dispatch over an interface and an abstract base, version-tolerant and
# explicit layouts, the unmanaged whole-struct memcpy path, the IBufferWriter and
# ReadOnlySequence entry points and the overwrite-in-place deserialize, exact-diffed
# against real .NET.
#
# The generator's output is part of the subject: it lands in the driver assembly, so the
# transpiled IL is code no human wrote and no other gate covers.
#
# The memcpy path carries `decimal` too: MemoryPack puts the raw struct bytes on the wire,
# so the runtime decimal's field order is observable there and nowhere a caller reads the
# value as a number. MemoryPackLayoutSubset's [fixed-decimal] section pins it as hex
# against real .NET, one value per axis of the layout (a trailing-zero scale, a negative,
# scale 28, a full 96-bit mantissa, and a signed zero). decimal-as-a-number is the
# decimal-ops and boxed-decimal buckets.
source "$(dirname "$0")/_common.sh"

project=MemoryPackSample
out="artifacts/memorypack"
measure="artifacts/memorypack-measure"
MEMORYPACK_SHA_EXPECTED=gates/expected/memorypack-dll.sha256
# One plain substring per line. A marker's numeral is the member's MethodDef row in the
# hash-pinned MemoryPack.Core.dll (moves only with the pin); its NameSuffix is the
# caller's instantiation (moves with the driver's type names). Re-derive on either change.
MARKERS_EXPECTED=gates/expected/memorypack-markers.txt

echo "== 1/7 Locating the real net10 CoreLib =="
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet MemoryPack =="
nuget_packages="$(nuget_global_packages_root)"
# The generator is checked too: without it the driver builds to IL with no formatters,
# which transpiles clean and asserts nothing.
if { [ ! -d "$nuget_packages/memorypack/1.21.4" ] \
        || [ ! -d "$nuget_packages/memorypack.core/1.21.4" ] \
        || [ ! -d "$nuget_packages/memorypack.generator/1.21.4" ]; } \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "MemoryPack 1.21.4 (Core + Generator) is not in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
memorypack="samples/dotnet/$project/bin/$CONFIG/$TFM/MemoryPack.Core.dll"
[ -f "$app" ] || { echo "FAIL: not built: $app" >&2; exit 1; }
[ -f "$memorypack" ] || { echo "FAIL: MemoryPack.Core did not restore to $memorypack" >&2; exit 1; }

memorypack_sha="$(shasum -a 256 "$memorypack" | awk '{print $1}')"
if [ "$memorypack_sha" != "$(cat "$MEMORYPACK_SHA_EXPECTED")" ]; then
    # `MemoryPack` is a meta package; net8.0 is its highest lib, so that is what a
    # net10.0 app restores. MemoryPack.Core is its only runtime assembly.
    echo "FAIL: MemoryPack.Core.dll is not the pinned 1.21.4 net8.0 build" >&2
    echo "      expected: $(cat "$MEMORYPACK_SHA_EXPECTED")  ($MEMORYPACK_SHA_EXPECTED)" >&2
    echo "      actual:   $memorypack_sha" >&2
    exit 1
fi
echo "MemoryPack.Core.dll: $memorypack_sha (pinned 1.21.4)"

echo "== 3/7 Transpiling the real package =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" -r "$memorypack")
rm -rf "$out"
# A cap can only turn the run into an abort or back, never perturb a succeeding
# transpile's bytes (AGENTS.md). Measured 6,912 instantiations (inst 4,797 +
# minst 2,115), cap ~2.9x; measured peak heap 259 MB, belt ~3.0x.
( export DN2CPP_MAX_INSTANTIATIONS=20000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 768 -o "$out" )
echo "OK (bounded: <=20,000 instantiations, <=768 MB heap)"

if gate_cache_check "$out" \
        "memorypack|cli:$(_gate_cli_hash)|corelib=$corelib" \
        "$app" "$memorypack" "$MARKERS_EXPECTED" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
rm -rf "$measure"
( export DN2CPP_MAX_INSTANTIATIONS=20000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 768 \
      --measure -o "$measure" >/dev/null 2>&1 ) || true
[ -f "$measure/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($measure/s0-gaps.tsv)" >&2; exit 1; }
gaps=$(wc -l < "$measure/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: MemoryPack produced $gaps gap(s):" >&2
    LC_ALL=C cut -f2,3,5 "$measure/s0-gaps.tsv" | LC_ALL=C sed 's/^/        /' >&2
    exit 1
fi
echo "0 gaps, 0 cuts"

echo "== 5/7 The package's real machinery MUST be in the tree =="
while IFS= read -r sym; do
    [ -n "$sym" ] || continue
    grep -qh "$sym" "$out/generated.h" \
        || { echo "FAIL: generated tree lacks $sym" >&2; exit 1; }
done < "$MARKERS_EXPECTED"
echo "OK ($(grep -c . "$MARKERS_EXPECTED") machinery symbols present)"

echo "== 6/7 Compiling C++ =="
compile_console "$out" "$project"

echo "== 7/7 Running (exact diff vs real .NET) =="
native_stderr="$out/native-stderr.txt"
oracle_stderr="$out/oracle-stderr.txt"
set +e
expected=$(dotnet "$app" 2>"$oracle_stderr"); expected_code=$?
native=$("./$out/$project" 2>"$native_stderr"); native_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"

# stdout matching hides everything the runtime writes to stderr — a swallowed
# startup-cctor failure reports there and nowhere else. This corpus reports none.
# The bar is two-sided: a divergence where only real .NET reports must fail too.
if [ -s "$oracle_stderr" ]; then
    echo "FAIL: real .NET wrote to stderr; the diff only covers stdout:" >&2
    cat "$oracle_stderr" >&2
    exit 1
fi
if [ -s "$native_stderr" ]; then
    echo "FAIL: the native binary wrote to stderr; it must stay silent:" >&2
    cat "$native_stderr" >&2
    exit 1
fi
echo "stderr: empty on both sides"
gate_cache_commit
echo "OK — the real MemoryPack 1.21.4 ran byte-identically to real .NET."
