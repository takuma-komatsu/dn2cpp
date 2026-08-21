#!/usr/bin/env bash
# The real MessagePipe 1.8.2 core NuGet package (+ Microsoft.Extensions.DependencyInjection):
# in-memory pub/sub, async pub/sub, keyed pub/sub, buffered pub/sub, event helper,
# request/response, async request/response, request all, global filter pipelines,
# exact-diffed against real .NET.
source "$(dirname "$0")/_common.sh"

project=MessagePipeSample
out="artifacts/messagepipe"
measure="artifacts/messagepipe-measure"
MESSAGEPIPE_SHA_EXPECTED=gates/expected/messagepipe-dlls.sha256
# One plain substring per line, so no comments in the file. The bare type names say
# the library's machinery is in the tree; the `m_…_<row>__<suffix>` rows are the
# per-section evidence — a Program.cs holder keeps the broker/handler TYPES alive on
# its own, so only a method symbol carrying a caller's instantiation shape can prove
# a driver section still runs. A numeral is that member's MethodDef row in the
# hash-pinned DLL, so it moves only with the pin — re-derive the file when it changes.
MARKERS_EXPECTED=gates/expected/messagepipe-markers.txt

echo "== 1/7 Locating the real net10 CoreLib =="
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet MessagePipe =="
nuget_packages="$(nuget_global_packages_root)"
if { [ ! -d "$nuget_packages/messagepipe/1.8.2" ] \
        || [ ! -d "$nuget_packages/microsoft.extensions.dependencyinjection/10.0.1" ]; } \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "MessagePipe 1.8.2 + DependencyInjection 10.0.1 are not both in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
appbin="samples/dotnet/$project/bin/$CONFIG/$TFM"
[ -f "$app" ] || { echo "FAIL: not built: $app" >&2; exit 1; }

# Content tripwire on every restored assembly the transpile consumes: the transport
# is the network, WHAT is transpiled is pinned by hash. MessagePipe 1.8.2's highest
# lib is net6.0; the two DependencyInjection 10.0.1 libs come from lib/net10.0. This
# loop is also what keeps the -r list below honest — a DLL named here must exist in
# the sample's bin, so a package rename/regroup fails here first with its own sentence.
mp_refs=()
mp_dlls=()
while read -r want name; do
    [ -n "$want" ] || continue
    dll="$appbin/$name"
    [ -f "$dll" ] || { echo "FAIL: pinned assembly did not restore to $dll" >&2; exit 1; }
    got="$(shasum -a 256 "$dll" | awk '{print $1}')"
    if [ "$got" != "$want" ]; then
        echo "FAIL: $name is not the pinned build" >&2
        echo "      expected: $want  ($MESSAGEPIPE_SHA_EXPECTED)" >&2
        echo "      actual:   $got" >&2
        echo "      A different assembly means different IL — re-audit before re-freezing." >&2
        exit 1
    fi
    mp_refs+=(-r "$dll")
    mp_dlls+=("$dll")
done < "$MESSAGEPIPE_SHA_EXPECTED"
echo "OK: ${#mp_dlls[@]} pinned assemblies verified against $MESSAGEPIPE_SHA_EXPECTED"

echo "== 3/7 Transpiling the real package =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
CLI_DLL="${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
CLI_SHA="$(shasum -a 256 "$CLI_DLL" | awk '{print $1}')"
refs=(-r "$corelib" "${mp_refs[@]}")
rm -rf "$out"
( export DN2CPP_MAX_INSTANTIATIONS=15000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 512 -o "$out" )
echo "OK (bounded: <=15,000 instantiations, <=512 MB heap)"

if gate_cache_check "$out" \
        "messagepipe|cli=$CLI_SHA|corelib=$corelib" \
        "$app" "$MESSAGEPIPE_SHA_EXPECTED" "${mp_dlls[@]}" "$MARKERS_EXPECTED" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
rm -rf "$measure"
( export DN2CPP_MAX_INSTANTIATIONS=15000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 512 \
      --measure -o "$measure" >/dev/null 2>&1 ) || true
[ -f "$measure/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($measure/s0-gaps.tsv)" >&2; exit 1; }
gaps=$(wc -l < "$measure/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: MessagePipe produced $gaps gap(s):" >&2
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
set +e
expected=$(dotnet "$app"); expected_code=$?
native=$("./$out/$project" 2>"$native_stderr"); native_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"

# stdout matching hides everything the runtime writes to stderr — a swallowed
# startup-cctor failure reports there and nowhere else. This corpus reports none.
if [ -s "$native_stderr" ]; then
    echo "FAIL: the native binary wrote to stderr; it must stay silent:" >&2
    cat "$native_stderr" >&2
    exit 1
fi
echo "native stderr: empty"
gate_cache_commit
echo "OK — the real MessagePipe 1.8.2 ran byte-identically to real .NET."
