#!/usr/bin/env bash
# The real R3 1.3.1 core NuGet package: multiple Subject subscriptions,
# Where/Select/Scan, resumable and terminal errors, ReactiveProperty equality,
# Timer, and Channels-backed async enumeration, exact-diffed against real .NET.
# Also a MoveNextAsync read completed and cancelled from another thread while
# pending — IValueTaskSource.OnCompleted's continuation path and AsyncOperation.TrySetCanceled.
# And a still-pending source-backed read taken SYNCHRONOUSLY, which must raise the
# source's own refusal rather than block until somebody settles it, and must leave the
# refused operation awaitable — the markers pin the two sources' throwing guards.
source "$(dirname "$0")/_common.sh"

project=R3Sample
out="artifacts/r3"
measure="artifacts/r3-measure"
R3_SHA_EXPECTED=gates/expected/r3-dll.sha256
MARKERS_EXPECTED=gates/expected/r3-markers.txt

echo "== 1/7 Locating the real net10 CoreLib =="
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet R3 =="
nuget_packages="$(nuget_global_packages_root)"
if [ ! -d "$nuget_packages/r3/1.3.1" ] \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "R3 1.3.1 is not in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
r3="samples/dotnet/$project/bin/$CONFIG/$TFM/R3.dll"
[ -f "$app" ] || { echo "FAIL: not built: $app" >&2; exit 1; }
[ -f "$r3" ] || { echo "FAIL: R3 did not restore to $r3" >&2; exit 1; }

r3_sha="$(shasum -a 256 "$r3" | awk '{print $1}')"
if [ "$r3_sha" != "$(cat "$R3_SHA_EXPECTED")" ]; then
    echo "FAIL: R3.dll is not the pinned 1.3.1 net8.0 build" >&2
    echo "      expected: $(cat "$R3_SHA_EXPECTED")  ($R3_SHA_EXPECTED)" >&2
    echo "      actual:   $r3_sha" >&2
    exit 1
fi
echo "R3.dll: $r3_sha (pinned 1.3.1)"

echo "== 3/7 Transpiling the real package =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" -r "$r3")
rm -rf "$out"
( export DN2CPP_MAX_INSTANTIATIONS=12000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 256 -o "$out" )
echo "OK (bounded: <=12,000 instantiations, <=256 MB heap)"

if gate_cache_check "$out" \
        "r3|cli:$(_gate_cli_hash)|corelib=$corelib" \
        "$app" "$r3" "$MARKERS_EXPECTED" \
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
    echo "FAIL: R3 produced $gaps gap(s):" >&2
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
gate_run_logs_init r3 R3
log_dir="$_GATE_RUN_LOG_DIR"
expected=
expected_code=
native=
native_code=

gate_run_diagnostics() {
    gate_run_diag "R3 native" "$native_code" "$native" "$log_dir/native.log"
    gate_run_diag "R3 oracle" "$expected_code" "$expected" "$log_dir/oracle.log"
}

set +e
expected=$(run_bounded dotnet "$app" 2>"$log_dir/oracle.log"); expected_code=$?
native=$(run_bounded "./$out/$project" 2>"$log_dir/native.log"); native_code=$?
set -e
assertions_failed=0
assert_output "$native" "$expected" || assertions_failed=1
assert_exit_code "$native_code" "$expected_code" || assertions_failed=1
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi
gate_cache_commit
echo "OK — the real R3 1.3.1 ran byte-identically to real .NET."
