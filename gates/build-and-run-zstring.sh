#!/usr/bin/env bash
# The real ZString 2.6.0 core NuGet package: zero-allocation string formatting,
# Utf16ValueStringBuilder, Utf8ValueStringBuilder, ZString.Format/Concat/Join,
# custom format strings, exact-diffed against real .NET.
source "$(dirname "$0")/_common.sh"

project=ZStringSample
out="artifacts/zstring"
measure="artifacts/zstring-measure"
ZSTRING_SHA_EXPECTED=gates/expected/zstring-dll.sha256
# One plain substring per line. A marker's numeral is the member's MethodDef row in
# the hash-pinned ZString.dll and its NameSuffix is the caller's instantiation, so
# both move only with the pin — re-derive the file when the pin changes.
MARKERS_EXPECTED=gates/expected/zstring-markers.txt

echo "== 1/7 Locating the real net10 CoreLib =="
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet ZString =="
nuget_packages="$(nuget_global_packages_root)"
if [ ! -d "$nuget_packages/zstring/2.6.0" ] \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "ZString 2.6.0 is not in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
zstring="samples/dotnet/$project/bin/$CONFIG/$TFM/ZString.dll"
[ -f "$app" ] || { echo "FAIL: not built: $app" >&2; exit 1; }
[ -f "$zstring" ] || { echo "FAIL: ZString did not restore to $zstring" >&2; exit 1; }

zstring_sha="$(shasum -a 256 "$zstring" | awk '{print $1}')"
if [ "$zstring_sha" != "$(cat "$ZSTRING_SHA_EXPECTED")" ]; then
    echo "FAIL: ZString.dll is not the pinned 2.6.0 build" >&2
    echo "      expected: $(cat "$ZSTRING_SHA_EXPECTED")  ($ZSTRING_SHA_EXPECTED)" >&2
    echo "      actual:   $zstring_sha" >&2
    exit 1
fi
echo "ZString.dll: $zstring_sha (pinned 2.6.0)"

echo "== 3/7 Transpiling the real package =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" -r "$zstring")
rm -rf "$out"
# A cap can only turn the run into an abort or back, never perturb a succeeding
# transpile's bytes (AGENTS.md). Measured 3,168 instantiations (inst 2,904 +
# minst 264), cap ~3.8x; measured peak heap 122 MB, belt ~2.1x.
( export DN2CPP_MAX_INSTANTIATIONS=12000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 256 -o "$out" )
echo "OK (bounded: <=12,000 instantiations, <=256 MB heap)"

if gate_cache_check "$out" \
        "zstring|cli:$(_gate_cli_hash)|corelib=$corelib" \
        "$app" "$zstring" "$MARKERS_EXPECTED" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
rm -rf "$measure"
( export DN2CPP_MAX_INSTANTIATIONS=12000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 256 \
      --measure -o "$measure" >/dev/null 2>&1 ) || true
[ -f "$measure/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($measure/s0-gaps.tsv)" >&2; exit 1; }
gaps=$(wc -l < "$measure/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: ZString produced $gaps gap(s):" >&2
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
# startup-cctor failure reports there and nowhere else. Eager static init runs every
# monomorphized Cysharp.Text.EnumUtil<T> cctor, and the non-enum T ones throw; each is
# recorded SILENTLY, exactly as real .NET is silent about an initializer it never runs.
# This program never touches those types — a touch would report here and re-raise.
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
echo "OK — the real ZString 2.6.0 ran byte-identically to real .NET."
