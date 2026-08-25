#!/usr/bin/env bash
# A real MagicOnion generated unary client over YetAnotherHttpHandler and
# GrpcChannel. A gate-only header adapter carries the generated frame while
# keeping YAHH's HTTP/2 POST bodyless.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_yetanotherhttphandler.sh"

FIXTURE=gates/fixtures/yetanotherhttphandler
PIN=gates/expected/yetanotherhttphandler-pin.txt
MAGIC_FIXTURE=gates/fixtures/magiconion-yaha
EXPECTED=gates/expected/magiconion-yaha.txt
DLLS_EXPECTED=gates/expected/magiconion-client-dlls.sha256
OUT=artifacts/magiconion-yaha
MEASURE=artifacts/magiconion-yaha-measure
YAHA_CACHE=artifacts/magiconion-yaha-cache
YAHA_MODULE=Cysharp.Net.Http.YetAnotherHttpHandler.Native

runtimes=$(dotnet --list-runtimes)
grep -q '^Microsoft.AspNetCore.App ' <<<"$runtimes" \
    || gate_skip "ASP.NET Core shared framework absent — the MagicOnion h2c oracle is Kestrel"
nuget_packages=$(nuget_global_packages_root)
if { [ ! -d "$nuget_packages/magiconion.client/7.10.2" ] \
        || [ ! -d "$nuget_packages/magiconion.serialization.messagepack/7.10.2" ] \
        || [ ! -d "$nuget_packages/messagepack/3.1.7" ] \
        || [ ! -d "$nuget_packages/messagepackanalyzer/3.1.7" ]; } \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "MagicOnion.Client 7.10.2 and its source-generator/MessagePack dependencies are not all in the NuGet cache and nuget.org is unreachable"
fi

echo "== 1/7 Acquiring and building the pinned YetAnotherHttpHandler source =="
yaha_prepare "$PIN" "$FIXTURE"

echo "== 2/7 Building both MagicOnion drivers and pinning their package closure =="
dotnet build "$MAGIC_FIXTURE/App/MagicOnionYahaGate.csproj" -c "$CONFIG" \
    -p:YahaManagedDll="$PWD/$YAHA_MANAGED" -p:YahaPipelinesDll="$PWD/$YAHA_PIPELINES" \
    --nologo -v:minimal
dotnet build "$MAGIC_FIXTURE/Server/MagicOnionYahaServer.csproj" -c "$CONFIG" --nologo -v:minimal
app="$MAGIC_FIXTURE/App/bin/$CONFIG/$TFM/MagicOnionYahaGate.dll"
app_dir=$(dirname "$app")
server="$MAGIC_FIXTURE/Server/bin/$CONFIG/$TFM/MagicOnionYahaServer.dll"
[ -f "$app" ] || { echo "FAIL: MagicOnion/YAHH client was not built" >&2; exit 1; }
[ -f "$server" ] || { echo "FAIL: MagicOnion/YAHH server was not built" >&2; exit 1; }
cp "$YAHA_NATIVE" "$app_dir/$YAHA_ORACLE_NAME"

package_refs=()
package_dlls=()
while read -r want name; do
    [ -n "$want" ] || continue
    dll="$app_dir/$name"
    [ -f "$dll" ] || { echo "FAIL: pinned MagicOnion assembly missing: $name" >&2; exit 1; }
    got=$(file_hash "$dll")
    [ "$got" = "$want" ] || {
        echo "FAIL: $name is not the pinned MagicOnion build" >&2
        echo "      expected: $want  ($DLLS_EXPECTED)" >&2
        echo "      actual:   $got" >&2
        exit 1
    }
    package_refs+=(-r "$dll")
    package_dlls+=("$dll")
done < "$DLLS_EXPECTED"
echo "OK: ${#package_dlls[@]} MagicOnion package assemblies pinned"

echo "== 3/7 Transpiling the generated MagicOnion client =="
corelib=$(resolve_net10_corelib)
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" -r "$YAHA_MANAGED" -r "$YAHA_PIPELINES" "${package_refs[@]}")
rm -rf "$OUT"
( export DN2CPP_MAX_INSTANTIATIONS=30000
  invoke_cli "$app" "${refs[@]}" --auto-ref --pinvoke-module "$YAHA_MODULE" \
      --max-heap-mb 1024 -o "$OUT" )
native_link_manifest=$(native_path "$PWD/$YAHA_NATIVE")
printf '%s\n' "$native_link_manifest" > "$OUT/native-assets.txt"
module_count=$(strip_cr_win_file "$OUT/pinvoke-libs.txt" \
    | awk -v module="$YAHA_MODULE" '$0 == module { count++ } END { print count + 0 }')
[ "$module_count" -eq 1 ] \
    || { echo "FAIL: MagicOnion/YAHH output must contain the exact native module token once" >&2; exit 1; }

echo "== 4/7 ASSERT: combined closure is gap-free and uses the generated proxy =="
rm -rf "$MEASURE"
measure_output=$( \
    export DN2CPP_MAX_INSTANTIATIONS=30000
    invoke_cli "$app" "${refs[@]}" --auto-ref --pinvoke-module "$YAHA_MODULE" \
        --max-heap-mb 1024 --measure -o "$MEASURE" 2>&1 )
printf '%s\n' "$measure_output"
[ -f "$MEASURE/s0-gaps.tsv" ] || { echo "FAIL: combined --measure produced no gap report" >&2; exit 1; }
[ "$(wc -l < "$MEASURE/s0-gaps.tsv" | tr -d ' ')" -eq 0 ] \
    || { echo "FAIL: MagicOnion/YAHH combined closure produced managed gaps" >&2; exit 1; }
grep -q '0 dangling' <<<"$measure_output" \
    || { echo "FAIL: MagicOnion/YAHH combined closure produced dangling methods" >&2; exit 1; }
grep -qhE '^    m_MagicOnionYahaGate_MagicOnionClientGeneratedInitializer_Register_[0-9]+\(\);$' \
    "$OUT"/generated_*.cpp \
    || { echo "FAIL: generated MagicOnion module initializer does not register its proxy" >&2; exit 1; }
if grep -qhE 'MagicOnion_Client_DynamicClient_DynamicMagicOnionClientFactoryProvider|MagicOnion_Client_DynamicClient_ServiceClientDefinition' \
        "$OUT/generated.h" "$OUT"/generated_*.cpp; then
    echo "FAIL: MagicOnion's Reflection.Emit proxy builder entered the combined tree" >&2
    exit 1
fi

echo "== 5/7 Linking the client and staging YetAnotherHttpHandler =="
compile_console "$OUT" MagicOnionYahaGate
native_builddir=$(_cmake_app_builddir "$OUT")
native_generation=$(cat "$native_builddir/native-assets-generation.txt")
[ -f "$OUT/.dn2cpp-native/$native_generation/$(basename "$YAHA_NATIVE")" ] \
    || { echo "FAIL: combined client did not stage the YetAnotherHttpHandler native asset" >&2; exit 1; }

echo "== 6/7 Starting the deterministic h2c oracle =="
gate_run_logs_init magiconion-yetanotherhttphandler MagicOnionYaha
log_dir="$_GATE_RUN_LOG_DIR"
dotnet "$server" >"$log_dir/server.out" 2>"$log_dir/server.err" &
server_pid=$!
cleanup_server() {
    [ -z "${server_pid:-}" ] || kill "$server_pid" >/dev/null 2>&1 || true
}
gate_add_exit_hook cleanup_server
ready=$(wait_ready_line "MagicOnion/YAHH h2c server" "$server_pid" \
    "$log_dir/server.out" "$log_dir/server.err") || exit 1
read -r tag port <<<"$ready"
[ "$tag" = READY ] && [ -n "${port:-}" ] \
    || { echo "FAIL: malformed MagicOnion/YAHH server ready line: $ready" >&2; exit 1; }
url="http://127.0.0.1:$port"

echo "== 7/7 Running MagicOnion through YetAnotherHttpHandler under incremental GC =="
if [ "$DN2CPP_OS" = windows ]; then
    set +e
    oracle=$(run_bounded dotnet "$app" "$url" 2>"$log_dir/oracle.err")
    oracle_code=$?
    set -e
else
    set +e
    oracle=$(run_bounded env "$LIB_PATH_ENV=$app_dir" dotnet "$app" "$url" \
        2>"$log_dir/oracle.err")
    oracle_code=$?
    set -e
fi
set +e
native=$(run_bounded env DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    "$OUT/MagicOnionYahaGate" "$url" 2>"$log_dir/native.err")
native_code=$?
set -e
if [ "$oracle_code" -ne 0 ] || [ "$native_code" -ne 0 ]; then
    echo "FAIL: MagicOnion/YAHH oracle=$oracle_code native=$native_code" >&2
    printf '%s\n' "$oracle" | sed 's/^/oracle stdout: /' >&2
    printf '%s\n' "$native" | sed 's/^/native stdout: /' >&2
    sed 's/^/oracle stderr: /' "$log_dir/oracle.err" >&2
    sed 's/^/native stderr: /' "$log_dir/native.err" >&2
    sed 's/^/server stdout: /' "$log_dir/server.out" >&2
    sed 's/^/server stderr: /' "$log_dir/server.err" >&2
    exit 1
fi
expected=$(cat "$EXPECTED")
assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$oracle")"
assert_output "$(strip_cr_win "$native")" "$expected"
grep -qF 'magiconion first=67890' <<<"$native" \
    || { echo "FAIL: MagicOnion response did not cross the YAHH HTTP/2 transport" >&2; exit 1; }
grep -qF 'trailers=67890,2468' <<<"$native" \
    || { echo "FAIL: gRPC trailers were not consumed through the YAHH response" >&2; exit 1; }
[ "$(grep -c '^REQUEST ' "$log_dir/server.out")" -eq 4 ] \
    || { echo "FAIL: the h2c server did not receive all four requests" >&2; cat "$log_dir/server.out" >&2; exit 1; }
grep -qF 'REQUEST 4' "$log_dir/server.out" \
    || { echo "FAIL: the final native request did not enter the h2c route" >&2; cat "$log_dir/server.out" >&2; exit 1; }
[ ! -s "$log_dir/oracle.err" ] \
    || { echo "FAIL: MagicOnion/YAHH oracle wrote to stderr" >&2; cat "$log_dir/oracle.err" >&2; exit 1; }
grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/native.err" \
    || { echo "FAIL: combined native client did not report incremental GC" >&2; cat "$log_dir/native.err" >&2; exit 1; }
[ ! -s "$log_dir/server.err" ] \
    || { echo "FAIL: MagicOnion/YAHH server wrote to stderr" >&2; cat "$log_dir/server.err" >&2; exit 1; }
kill "$server_pid" >/dev/null 2>&1 || true
wait "$server_pid" 2>/dev/null || true
server_pid=
echo "OK — MagicOnion used YetAnotherHttpHandler for two h2c unary calls and consumed their trailers."
