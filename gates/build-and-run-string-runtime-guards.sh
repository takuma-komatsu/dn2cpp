#!/usr/bin/env bash
# Native-only string layout and sizing guards. The probe links the real runtime:
# successful text covers ASCII, UTF-8/UTF-16 non-ASCII, a surrogate pair and null
# concat; oversized computed results must remain catchable OOMs; corrupt object
# shapes and a negative low-level allocation must stop with fixed, allocation-free
# diagnostics. The final arm makes a non-managed exception escape the exception
# message renderer and proves the host-boundary fallback prints only the original
# type plus its fixed failure note, without retrying ToString.
# Former gates: none (new runtime invariant).
source "$(dirname "$0")/_common.sh"

out=artifacts/stringruntimeguards
mkdir -p "$out"

ctx="string_runtime_guards|normal+computed-overflow+shape-fatal+allocator-fatal+boundary-native-fallback"
ctx="$ctx$(_gate_ctx_extras)"
if gate_cache_check "$out" "$ctx" \
    samples/native/stringguards/stringguards.cpp \
    samples/native/stringguards/CMakeLists.txt; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 1/3 Building the runtime =="
export_file="$(ensure_cmake_runtime)" || exit 1

echo "== 2/3 Building the string guard probe =="
probe_args=()
[ -n "${CMAKE_CXX_COMPILER:-}" ] && probe_args+=(-DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER}")
_cmake_configure always "" "$out" "$out/dn2cpp-configure.log" \
    "configuring the string guard probe" \
    "$CMAKE" -S samples/native/stringguards -B "$out" -G Ninja \
    -DDN2CPP_RUNTIME_EXPORT="$export_file" ${probe_args[@]+"${probe_args[@]}"}
_cmake_step "$out/dn2cpp-build.log" "building the string guard probe" \
    "$CMAKE" --build "$out"

echo "== 3/3 Running normal, overflow, corruption and boundary arms =="
gate_run_logs_init stringruntimeguards "string runtime guard probe"
log_dir="$_GATE_RUN_LOG_DIR"
assert_log_contains() {
    local file="$1" expected="$2"
    if ! grep -qF "$expected" "$file"; then
        echo "FAIL: $file does not contain: $expected" >&2
        sed -n '1,40p' "$file" >&2
        return 1
    fi
}
gate_capture_ok "$log_dir/normal" run_bounded "./$out/stringguards" normal
gate_capture_ok "$log_dir/overflow" run_bounded "./$out/stringguards" overflow
gate_capture_ok "$log_dir/boundary" run_bounded "./$out/stringguards" boundary

gate_assert_text "$log_dir/normal.stdout" \
    "normal strings: ascii+japanese+surrogate+null-concat ok"
gate_assert_text "$log_dir/overflow.stdout" \
    "overflow guards: concat=oom utf8-byte-count=oom"
gate_assert_text "$log_dir/boundary.stdout" "boundary fallback: one ToString call"
assert_log_contains "$log_dir/boundary.stderr" \
    "[dn2cpp] unhandled managed exception in string guard probe: Probe.BrokenException (its ToString threw)"

run_fatal_arm() {
    local mode="$1" expected="$2" code
    gate_capture "$log_dir/$mode" run_bounded "./$out/stringguards" "$mode"
    code=$(cat "$log_dir/$mode.status")
    gate_output native-abort "$log_dir/$mode" --host "$DN2CPP_OS" --empty-stdout \
        --diagnostic "dn2cpp fatal: $expected"
    if [ "$code" -eq 0 ]; then
        echo "FAIL: string guard arm $mode returned success" >&2
        return 1
    fi
    assert_log_contains "$log_dir/$mode.stderr" "dn2cpp fatal: $expected"
}

run_fatal_arm bad-type "managed string has a non-string type"
run_fatal_arm bad-length "managed string has a negative length"
run_fatal_arm bad-chars "managed string has no character buffer"
run_fatal_arm bad-allocation "managed string allocator received a negative length"

# The runtime probe invokes the same narrowing primitive used by UTF-8 counting;
# pin that wiring so the overflow arm cannot keep passing after the codec bypasses it.
if ! grep -A8 -F 'int32_t dn2cpp_utf16_to_utf8' \
        runtime/core/intrinsics/dn2cpp_string_core.cpp \
        | grep -qF 'int64_t bytes'; then
    echo "FAIL: UTF-8 byte counting is not wide" >&2
    exit 1
fi
if ! grep -A80 -F 'int32_t dn2cpp_utf16_to_utf8' \
        runtime/core/intrinsics/dn2cpp_string_core.cpp \
        | grep -qF 'dn2cpp_string_checked_length(bytes)'; then
    echo "FAIL: UTF-8 byte count does not use the checked result-length guard" >&2
    exit 1
fi

gate_cache_commit
