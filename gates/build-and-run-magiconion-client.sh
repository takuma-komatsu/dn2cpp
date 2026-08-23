#!/usr/bin/env bash
# The real MagicOnion.Client 7.10.2 NuGet package and its source generator, in
# the same shape as upstream's NativeAOT unary tests: a generated proxy invokes a
# mock CallInvoker, which captures the MessagePack DynamicArgumentTuple request
# and unmarshals a response before the caller awaits UnaryResult<int>. Both the
# wire bytes and result are exact-diffed against real .NET.
source "$(dirname "$0")/_common.sh"

project=MagicOnionClientSample
out="artifacts/magiconion-client"
measure="artifacts/magiconion-client-measure"
DLLS_EXPECTED=gates/expected/magiconion-client-dlls.sha256
MARKERS_EXPECTED=gates/expected/magiconion-client-markers.txt
[ -s "$MARKERS_EXPECTED" ] \
    || { echo "FAIL: generated-machinery marker fixture is missing or empty: $MARKERS_EXPECTED" >&2; exit 1; }

echo "== 1/7 Locating the real net10 CoreLib =="
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet MagicOnion.Client =="
nuget_packages="$(nuget_global_packages_root)"
if { [ ! -d "$nuget_packages/magiconion.client/7.10.2" ] \
        || [ ! -d "$nuget_packages/magiconion.serialization.messagepack/7.10.2" ] \
        || [ ! -d "$nuget_packages/messagepack/3.1.7" ] \
        || [ ! -d "$nuget_packages/messagepackanalyzer/3.1.7" ]; } \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "MagicOnion.Client 7.10.2 and its source-generator/MessagePack dependencies are not all in the NuGet cache and nuget.org is unreachable"
fi
build_gate_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
appbin="$(dirname "$app")"
[ -f "$app" ] || { echo "FAIL: not built: $app" >&2; exit 1; }

package_refs=()
package_dlls=()
while read -r want name; do
    [ -n "$want" ] || continue
    dll="$appbin/$name"
    [ -f "$dll" ] || { echo "FAIL: pinned assembly did not restore to $dll" >&2; exit 1; }
    got="$(shasum -a 256 "$dll" | awk '{print $1}')"
    if [ "$got" != "$want" ]; then
        echo "FAIL: $name is not the pinned build" >&2
        echo "      expected: $want  ($DLLS_EXPECTED)" >&2
        echo "      actual:   $got" >&2
        echo "      A different assembly means different IL — re-audit before re-freezing." >&2
        exit 1
    fi
    package_refs+=(-r "$dll")
    package_dlls+=("$dll")
done < "$DLLS_EXPECTED"
echo "OK: ${#package_dlls[@]} pinned assemblies verified against $DLLS_EXPECTED"

echo "== 3/7 Transpiling the real package =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" "${package_refs[@]}")
rm -rf "$out"
( export DN2CPP_MAX_INSTANTIATIONS=30000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 1024 -o "$out" )
echo "OK (bounded: <=30,000 instantiations, <=1,024 MB heap)"

if gate_cache_check "$out" \
        "magiconion-client|cli:$(_gate_cli_hash)|corelib=$corelib" \
        "$app" "$DLLS_EXPECTED" "${package_dlls[@]}" "$MARKERS_EXPECTED" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
rm -rf "$measure"
( export DN2CPP_MAX_INSTANTIATIONS=30000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 1024 \
      --measure -o "$measure" >/dev/null 2>&1 ) || true
[ -f "$measure/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($measure/s0-gaps.tsv)" >&2; exit 1; }
gaps=$(wc -l < "$measure/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: MagicOnion.Client produced $gaps gap(s):" >&2
    LC_ALL=C cut -f2,3,5 "$measure/s0-gaps.tsv" | LC_ALL=C sed 's/^/        /' >&2
    exit 1
fi
echo "0 gaps, 0 cuts"

echo "== 5/7 The generated proxy and package machinery MUST be in the tree =="
while IFS= read -r sym; do
    [ -n "$sym" ] || continue
    grep -qh "$sym" "$out/generated.h" \
        || { echo "FAIL: generated tree lacks $sym" >&2; exit 1; }
done < "$MARKERS_EXPECTED"
echo "OK ($(grep -c . "$MARKERS_EXPECTED") machinery symbols present)"

# The generated module initializer must call Register; merely retaining its body
# would not install the generated factory before MagicOnionClient.Create runs.
if ! grep -qhE '^    m_MagicOnionClientSample_MagicOnionClientGeneratedInitializer_Register_[0-9]+\(\);$' \
        "$out"/generated_*.cpp; then
    echo "FAIL: generated module initializer does not call MagicOnionClientGeneratedInitializer.Register" >&2
    exit 1
fi

# MessagePack's resolver closure legitimately carries Reflection.Emit metadata.
# The forbidden path is MagicOnion's own Reflection.Emit proxy builder, which a
# source-generated client must never reach.
if grep -qhE 'MagicOnion_Client_DynamicClient_DynamicMagicOnionClientFactoryProvider|MagicOnion_Client_DynamicClient_ServiceClientDefinition' \
        "$out/generated.h" "$out"/generated_*.cpp; then
    echo "FAIL: MagicOnion's dynamic Reflection.Emit client builder entered the tree" >&2
    exit 1
fi
echo "OK: generated factory registered; MagicOnion dynamic client builder absent"

echo "== 6/7 Compiling C++ =="
compile_console "$out" "$project"

echo "== 7/7 Running (exact diff vs real .NET) =="
gate_run_logs_init magiconion-client MagicOnion.Client
log_dir="$_GATE_RUN_LOG_DIR"
native_stderr="$log_dir/native.log"
oracle_stderr="$log_dir/oracle.log"
expected=
expected_code=
native=
native_code=

gate_run_diagnostics() {
    gate_run_diag "MagicOnion.Client native" "$native_code" "$native" "$native_stderr"
    gate_run_diag "MagicOnion.Client oracle" "$expected_code" "$expected" "$oracle_stderr"
}

set +e
expected=$(run_bounded dotnet "$app" 2>"$oracle_stderr"); expected_code=$?
native=$(run_bounded "./$out/$project" 2>"$native_stderr"); native_code=$?
set -e
assertions_failed=0
assert_output "$native" "$expected" || assertions_failed=1
assert_exit_code "$native_code" "$expected_code" || assertions_failed=1
if [ -s "$oracle_stderr" ]; then
    echo "FAIL: real .NET wrote to stderr; the diff only covers stdout:" >&2
    cat "$oracle_stderr" >&2
    assertions_failed=1
fi
if [ -s "$native_stderr" ]; then
    echo "FAIL: the native binary wrote to stderr; it must stay silent:" >&2
    cat "$native_stderr" >&2
    assertions_failed=1
fi
if [ "$assertions_failed" -ne 0 ]; then
    gate_run_diag_once
    exit 1
fi
echo "stderr: empty on both sides"
gate_cache_commit
echo "OK — the real MagicOnion.Client 7.10.2 generated proxy ran byte-identically to real .NET."
