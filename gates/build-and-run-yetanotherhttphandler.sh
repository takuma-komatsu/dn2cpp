#!/usr/bin/env bash
# YetAnotherHttpHandler 1.11.5's unmodified managed source and official native
# asset: exact P/Invoke token discovery, native-asset link, Rust-thread async
# callbacks, and response-body delivery under incremental GC.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_yetanotherhttphandler.sh"

PROJECT=YetAnotherHttpHandlerGate
FIXTURE=gates/fixtures/yetanotherhttphandler
PIN=gates/expected/yetanotherhttphandler-pin.txt
EXPECTED=gates/expected/yetanotherhttphandler.txt
OUT=artifacts/yetanotherhttphandler
MEASURE=artifacts/yetanotherhttphandler-measure
YAHA_CACHE=artifacts/yetanotherhttphandler-cache
YAHA_MODULE=Cysharp.Net.Http.YetAnotherHttpHandler.Native

echo "== 1/8 Acquiring and building the pinned official source =="
yaha_prepare "$PIN" "$FIXTURE"

echo "== 2/8 Building the HTTP driver and deterministic loopback server =="
dotnet build "$FIXTURE/App/$PROJECT.csproj" -c "$CONFIG" \
    -p:YahaManagedDll="$PWD/$YAHA_MANAGED" -p:YahaPipelinesDll="$PWD/$YAHA_PIPELINES" \
    --nologo -v:minimal
dotnet build "$FIXTURE/Server/YetAnotherHttpHandlerServer.csproj" -c "$CONFIG" --nologo -v:minimal
app="$FIXTURE/App/bin/$CONFIG/$TFM/$PROJECT.dll"
server="$FIXTURE/Server/bin/$CONFIG/$TFM/YetAnotherHttpHandlerServer.dll"
app_dir=$(dirname "$app")
cp "$YAHA_NATIVE" "$app_dir/$YAHA_ORACLE_NAME"

echo "== 3/8 Transpiling the HTTP driver with the exact native module token =="
corelib=$(resolve_net10_corelib)
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
refs=(-r "$corelib" -r "$YAHA_MANAGED" -r "$YAHA_PIPELINES")
rm -rf "$OUT"
invoke_cli "$app" "${refs[@]}" --auto-ref --direct-pinvoke "$YAHA_MODULE" -o "$OUT"
[ "$(strip_cr_win_file "$OUT/pinvoke-libs.txt")" = "$YAHA_MODULE" ] \
    || { echo "FAIL: pinvoke-libs.txt does not contain the exact module token" >&2; exit 1; }
[ "$(wc -l < "$OUT/pinvoke-libs.txt" | tr -d ' ')" -eq 1 ] \
    || { echo "FAIL: pinvoke-libs.txt contains extra tokens" >&2; exit 1; }
if [ "$DN2CPP_OS" = windows ]; then
    grep -qF "$YAHA_MODULE"$'\t''yaha_client_config_unix_domain_socket_path' \
        "$OUT/pinvoke-symbols.txt" \
        || { echo "FAIL: Windows delay-import manifest omitted the lazily resolved export" >&2; exit 1; }
fi
native_link_manifest=$(native_path "$PWD/$YAHA_NATIVE")
printf '%s\n' "$native_link_manifest" > "$OUT/native-assets.txt"

echo "== 4/8 ASSERT: HTTP driver has ZERO gaps and ZERO dangling methods =="
rm -rf "$MEASURE"
measure_output=$(invoke_cli "$app" "${refs[@]}" --auto-ref --direct-pinvoke "$YAHA_MODULE" --measure -o "$MEASURE" 2>&1)
printf '%s\n' "$measure_output"
[ -f "$MEASURE/s0-gaps.tsv" ] || { echo "FAIL: --measure produced no gap report" >&2; exit 1; }
[ "$(wc -l < "$MEASURE/s0-gaps.tsv" | tr -d ' ')" -eq 0 ] \
    || { echo "FAIL: YetAnotherHttpHandler produced managed gaps" >&2; exit 1; }
grep -q '0 dangling' <<<"$measure_output" \
    || { echo "FAIL: YetAnotherHttpHandler produced dangling methods" >&2; exit 1; }

echo "== 5/8 Linking the official native asset through native-assets.txt =="
compile_console "$OUT" "$PROJECT"
native_builddir=$(_cmake_app_builddir "$OUT")
native_generation=$(cat "$native_builddir/native-assets-generation.txt")
[ -f "$OUT/.dn2cpp-native/$native_generation/$(basename "$YAHA_NATIVE")" ] \
    || { echo "FAIL: compile_console did not publish the staged native asset" >&2; exit 1; }
if [ "$DN2CPP_OS" = windows ]; then
    native_imports="$native_builddir/$PROJECT.imports.txt"
    dumpbin.exe //nologo //imports "$(cygpath -w "$OUT/$PROJECT$EXE_EXT")" > "$native_imports"
    grep -qi "^    $YAHA_ORACLE_NAME$" "$native_imports" \
        || { echo "FAIL: native executable does not import the official Windows DLL" >&2; exit 1; }
    [ ! -e "$OUT/$YAHA_ORACLE_NAME" ] \
        || { echo "FAIL: Windows native asset escaped its immutable generation" >&2; exit 1; }
fi

echo "== 6/8 Starting the deterministic loopback HTTP server =="
gate_run_logs_init yetanotherhttphandler YetAnotherHttpHandler
log_dir="$_GATE_RUN_LOG_DIR"
port_file="$log_dir/port"
dotnet "$server" "$port_file" >"$log_dir/server.out" 2>"$log_dir/server.err" &
server_pid=$!
cleanup_yaha_server() {
    [ -z "${server_pid:-}" ] || kill "$server_pid" >/dev/null 2>&1 || true
}
gate_add_exit_hook cleanup_yaha_server
for _ in $(seq 1 100); do [ -s "$port_file" ] && break; sleep 0.05; done
[ -s "$port_file" ] || { echo "FAIL: loopback server did not start" >&2; exit 1; }
url="http://127.0.0.1:$(cat "$port_file")/probe"

echo "== 7/8 Running the real-.NET oracle and foreign callback path =="
if [ "$DN2CPP_OS" = windows ]; then
    oracle=$(run_bounded dotnet "$app" "$url" 2>"$log_dir/oracle.err")
else
    oracle=$(run_bounded env "$LIB_PATH_ENV=$app_dir" dotnet "$app" "$url" 2>"$log_dir/oracle.err")
fi

echo "== 8/8 Running native under incremental GC =="
native_output=$(run_bounded env DN2CPP_GC_INCREMENTAL=1 DN2CPP_GC_STATS=1 \
    "$OUT/$PROJECT" "$url" 2>"$log_dir/native.err")
wait "$server_pid"
server_pid=
expected=$(cat "$EXPECTED")
assert_output "$(strip_cr_win "$oracle")" "$expected"
assert_output "$(strip_cr_win "$native_output")" "$expected"
[ ! -s "$log_dir/oracle.err" ] || { echo "FAIL: oracle wrote to stderr" >&2; cat "$log_dir/oracle.err" >&2; exit 1; }
grep -qF '[dn2cpp] GC mode: incremental' "$log_dir/native.err" \
    || { echo "FAIL: runtime stats did not report incremental GC" >&2; cat "$log_dir/native.err" >&2; exit 1; }
[ ! -s "$log_dir/server.err" ] || { echo "FAIL: server wrote to stderr" >&2; cat "$log_dir/server.err" >&2; exit 1; }
echo "OK — YetAnotherHttpHandler $YAHA_VERSION linked and delivered its async response under incremental GC."
