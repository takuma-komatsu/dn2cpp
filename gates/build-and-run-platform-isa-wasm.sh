#!/usr/bin/env bash
# The Wasm platform-ISA family's capability contract on the wasm32 axis, in two
# builds of the same probe.
#
# Run A, the default build, is diffed against real .NET: PackedSimd (and every
# X86/Arm row) answers false, and every family's representative instruction throws
# PlatformNotSupportedException — the oracle is real .NET under
# DOTNET_EnableHWIntrinsic=0, the one configuration where its own answer to the
# whole table is false.
#
# Run B, the DN2CPP_WASM_SIMD build (-msimd128 on the runtime and the app), answers
# true for PackedSimd and runs the generated exercise of every PackedSimd method.
# Its output is FROZEN in gates/expected/platform-isa-wasm-simd.txt rather than
# diffed live: no host .NET can be the oracle (PackedSimd.IsSupported is false
# everywhere but under a wasm JIT), and wasm is a fixed target — the same module
# computes the same bytes on every host and engine — so the freeze is
# host-independent. Two things keep a frozen file honest here: every exercise
# line that has a portable equivalent carries a `ref=` cross-check computed by the
# Vector128 layer (an independent implementation), and the file must contain no
# MISMATCH; and the witnesses below (PackedSimd=True and the native-only
# immediate contract).
# To refresh after an intentional change to the lowering or the exercise, run the
# gate, then copy `node artifacts/platformisaprobe-wasm-simd/PlatformIsaProbe.js
# Wasm.PackedSimd` over the expected file and read the diff.
#
# Run B-masked runs the SIMD module under DN2CPP_CPU_FEATURES=none: its output must
# equal Run A's oracle byte for byte with exactly one `effective=(none)` diag line
# on stderr. That proves the console pre-js forwards the node process's DN2CPP_*
# variables into the module's environment, and that the mask beats compile-time
# detection — a SIMD module can still be told to take every software fallback.
#
# Machines without the Emscripten toolchain (or node) skip; the runner reports the
# gate as SKIPPED rather than passed (gate_skip in _common.sh).
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    fi
done

export WASM=1
# An inherited mask or axis would narrow the runs and record that answer under
# the keys below.
unset DN2CPP_CPU_FEATURES DN2CPP_CPU_FEATURES_DIAG DN2CPP_WASM_SIMD

project="$PLATFORM_ISA_PROJECT"
out="$(_corelib_gate_out "$project")-wasm"
expected_simd="$(dirname "$0")/expected/platform-isa-wasm-simd.txt"
[ -f "$expected_simd" ] || { echo "error: frozen snapshot not found: $expected_simd" >&2; exit 1; }

# The wasm axis cross-targets Emscripten, so the CoreLib flavour follows the
# TARGET, not the host (docs/PORTING.md H6).
_CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
_corelib_gate_core "$project" "$out"

# Run A's oracle is also Run B-masked's expectation, so it is computed once, here,
# outside the cache decision: a cached Run A still has to hand it over.
DOTNET_EnableHWIntrinsic=0 gate_capture "$out/oracle" run_bounded dotnet "$_CG_APP" all
oracle_code=$(cat "$out/oracle.status")
gate_output view "$out/oracle.stdout" "$out/oracle.lf" --policy crlf
platform_isa_output validate "$out/oracle.lf" || exit 1
# CpuId is a host-only target witness, not a wasm capability observation.
gate_output omit-prefix "$out/oracle.lf" "$out/oracle-filtered.lf" \
    --prefix 'CpuId(0,0).target=' --count all --policy exact
oracle="$out/oracle-filtered.lf"

if gate_cache_check "$out" "platform_isa_wasm|default|$_CG_CORELIB|argv:all|oracle:DOTNET_EnableHWIntrinsic=0+foreign-target-cpuid-elided" \
        "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json"; then
    gate_cache_hit_msg
else
    echo "== 4/4 Compiling to wasm and running the contract under node (exact diff vs real .NET, all families masked) =="
    compile_console_wasm "$out" "$project"

    # Exact code equality cannot hold across runtimes (node reports an abort as 1,
    # .NET as 134): the sides must only FAIL ALIKE, both 0 or both nonzero.
    gate_capture "$out/native" run_bounded node "./$out/$project.js" all
    native_code=$(cat "$out/native.status")
    gate_output status "$native_code" "$oracle_code" --policy success-class
    gate_output view "$out/native.stdout" "$out/native.lf" --policy crlf
    platform_isa_output validate "$out/native.lf" || exit 1
    native="$out/native.lf"
    gate_output compare "$native" "$oracle"

    # The diff above would also pass if both sides printed nothing useful; these
    # say the contract block was there and answered false throughout.
    block=$(platform_isa_contract_block "$native") || exit 1
    n_true=$(grep -c '=True$' <<<"$block" || true)
    if [ "$n_true" -ne 0 ]; then
        echo "FAIL: the default wasm build answered true for $n_true row(s); SIMD is a separate build (run B)" >&2
        exit 1
    fi
    n_packed=$(grep -c '^Wasm\.PackedSimd=False$' <<<"$block" || true)
    if [ "$n_packed" -ne 1 ]; then
        echo "FAIL: expected exactly one 'Wasm.PackedSimd=False' row, found $n_packed" >&2
        exit 1
    fi
    echo "OK: every platform-ISA row false on the default wasm build, PackedSimd included"

    gate_cache_commit
fi

# ── Run B: the DN2CPP_WASM_SIMD build ────────────────────────────────────────
# A subshell: the axis variable must not leak into anything after it, and the
# gate cache keys on it (a green of one axis never replays for the other).
(
    export DN2CPP_WASM_SIMD=1
    out_simd="$out-simd"
    _corelib_gate_core "$project" "$out_simd"
    invalid_rows=$(platform_isa_invalid_immediate_rows wasm) || exit 1
    invalid_count=$(grep -c . <<<"$invalid_rows" || true)
    [ "$invalid_count" -gt 0 ] \
        || { echo "error: generated exercises list no Wasm invalid-immediate boundary" >&2; exit 1; }
    invalid_csv=$(tr '\n' ',' <<<"$invalid_rows")
    invalid_csv=${invalid_csv%,}

    ctx="platform_isa_wasm|simd|$_CG_CORELIB|argv:Wasm.PackedSimd+all"
    ctx="$ctx|expected:$(shasum -a 256 < "$expected_simd" | cut -d' ' -f1)"
    ctx="$ctx|masked:DN2CPP_CPU_FEATURES=none+DN2CPP_CPU_FEATURES_DIAG=1|oracle:DOTNET_EnableHWIntrinsic=0+foreign-target-cpuid-elided"
    ctx="$ctx|invalid-immediates:$invalid_csv|N=default+--invalid-immediates|NM=DN2CPP_CPU_FEATURES=none+--invalid-immediates"
    ctx="$ctx|assert:packedsimd-true+invalid-require-before-range+no-mismatch+mask-diag"
    if gate_cache_check "$out_simd" "$ctx" \
            "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" "$expected_simd"; then
        gate_cache_hit_msg
        exit 0
    fi

    echo "== 4/4 Compiling to wasm with -msimd128 and running PackedSimd under node (diff vs frozen snapshot) =="
    compile_console_wasm "$out_simd" "$project"

    gate_run_logs_init "platform_isa_wasm_simd" "platform-isa-wasm-simd"
    log_dir="$_GATE_RUN_LOG_DIR"
    _B_OUT=""; _B_CODE=""; _M_OUT=""; _M_CODE=""
    _N_OUT=""; _N_CODE=""; _NM_OUT=""; _NM_CODE=""
    gate_run_diagnostics() {
        gate_run_diag_files B "$_B_CODE" "$log_dir/B.stdout" "$log_dir/B.log"
        gate_run_diag_files B-masked "$_M_CODE" "$log_dir/B-masked.stdout" "$log_dir/B-masked.log"
        gate_run_diag_files N "$_N_CODE" "$log_dir/N.stdout" "$log_dir/N.log"
        gate_run_diag_files NM "$_NM_CODE" "$log_dir/NM.stdout" "$log_dir/NM.log"
    }

    gate_capture "$log_dir/B" run_bounded node "./$out_simd/$project.js" Wasm.PackedSimd
    _B_CODE=$(cat "$log_dir/B.status")
    cp "$log_dir/B.stderr" "$log_dir/B.log"
    gate_output view "$log_dir/B.stdout" "$log_dir/B.lf" --policy crlf
    platform_isa_output validate "$log_dir/B.lf" || exit 1
    _B_OUT="$log_dir/B.lf"
    echo "-- run B: argv=Wasm.PackedSimd (frozen snapshot)"
    if ! gate_output compare "$_B_OUT" "$expected_simd" || ! gate_output status "$_B_CODE" 0; then
        gate_run_diag_once
        exit 1
    fi
    require_count() {
        if [ "$3" -ne "$2" ]; then
            echo "FAIL: run $1: expected $2 $4, found $3" >&2
            gate_run_diag_once
            exit 1
        fi
    }
    n=$(grep -c '^Wasm\.PackedSimd=True$' "$_B_OUT" || true)
    require_count B 1 "$n" "'Wasm.PackedSimd=True' line (the SIMD build detects the instruction set)"
    n=$(grep -c '=ArgumentOutOfRangeException$' "$_B_OUT" || true)
    require_count B 0 "$n" "out-of-contract immediate results in oracle-parity output"
    n=$(grep -c 'ref=MISMATCH' "$_B_OUT" || true)
    require_count B 0 "$n" "'ref=MISMATCH' lines (a helper disagreeing with the portable Vector128 computation)"
    n_ref=$(grep -c ' ref=OK$' "$_B_OUT" || true)
    n_rows=$(grep -c '^[A-Za-z0-9]*([^)]*)=' "$_B_OUT" || true)
    if [ "$n_ref" -eq 0 ] || [ "$n_ref" -gt "$n_rows" ]; then
        echo "FAIL: run B: $n_ref 'ref=OK' lines over $n_rows exercise lines" >&2
        gate_run_diag_once
        exit 1
    fi
    echo "OK: PackedSimd true, every exercise line frozen, $n_ref of $n_rows lines cross-checked against Vector128"

    # B-masked. The masked SIMD module must be indistinguishable from the default
    # build on stdout; the diag line is the only trace of the mask.
    DN2CPP_CPU_FEATURES=none DN2CPP_CPU_FEATURES_DIAG=1 gate_capture "$log_dir/B-masked" run_bounded node "./$out_simd/$project.js" all
    _M_CODE=$(cat "$log_dir/B-masked.status")
    cp "$log_dir/B-masked.stderr" "$log_dir/B-masked.log"
    gate_output view "$log_dir/B-masked.stdout" "$log_dir/B-masked.lf" --policy crlf
    platform_isa_output validate "$log_dir/B-masked.lf" || exit 1
    _M_OUT="$log_dir/B-masked.lf"
    echo "-- run B-masked: argv=all ours=[DN2CPP_CPU_FEATURES=none DN2CPP_CPU_FEATURES_DIAG=1] oracle=[DOTNET_EnableHWIntrinsic=0]"
    if ! gate_output compare "$_M_OUT" "$oracle"; then
        gate_run_diag_once
        exit 1
    fi
    gate_output status "$_M_CODE" "$oracle_code" --policy success-class || { gate_run_diag_once; exit 1; }
    n=$(grep -c 'effective=(none)' "$log_dir/B-masked.log" || true)
    require_count B-masked 1 "$n" "'effective=(none)' diag line on stderr (the pre-js forwarded both variables; DN2CPP_CPU_FEATURES_DIAG=1 prints exactly one)"
    echo "OK: the masked SIMD module answers as the default build does, with one diag line"

    # The out-of-contract immediate is executed only by dn2cpp. With the SIMD
    # token true it reaches the range check; with the token masked false it must
    # fail the capability requirement first.
    gate_capture "$log_dir/N" run_bounded node "./$out_simd/$project.js" Wasm.PackedSimd \
        --invalid-immediates
    _N_CODE=$(cat "$log_dir/N.status")
    cp "$log_dir/N.stderr" "$log_dir/N.log"
    gate_output view "$log_dir/N.stdout" "$log_dir/N.lf" --policy crlf
    platform_isa_output validate "$log_dir/N.lf" || exit 1
    _N_OUT="$log_dir/N.lf"
    echo "-- run N: argv=Wasm.PackedSimd --invalid-immediates ours=[<no ISA env>]"
    gate_output status "$_N_CODE" 0 || { gate_run_diag_once; exit 1; }
    n=$(grep -c '^== invalid [A-Za-z0-9.]* ==$' "$_N_OUT" || true)
    require_count N "$invalid_count" "$n" "generated invalid-immediate sections"
    violations=$(platform_isa_invalid_immediate_violations "$_N_OUT") || exit 1
    if [ -n "$violations" ]; then
        echo "FAIL: run N invalid-immediate token/exception contract:" >&2
        printf '%s\n' "$violations" >&2
        gate_run_diag_once
        exit 1
    fi

    DN2CPP_CPU_FEATURES=none gate_capture "$log_dir/NM" run_bounded node "./$out_simd/$project.js" Wasm.PackedSimd \
                  --invalid-immediates
    _NM_CODE=$(cat "$log_dir/NM.status")
    cp "$log_dir/NM.stderr" "$log_dir/NM.log"
    gate_output view "$log_dir/NM.stdout" "$log_dir/NM.lf" --policy crlf
    platform_isa_output validate "$log_dir/NM.lf" || exit 1
    _NM_OUT="$log_dir/NM.lf"
    echo "-- run NM: argv=Wasm.PackedSimd --invalid-immediates ours=[DN2CPP_CPU_FEATURES=none]"
    gate_output status "$_NM_CODE" 0 || { gate_run_diag_once; exit 1; }
    n=$(grep -c '^== invalid [A-Za-z0-9.]* ==$' "$_NM_OUT" || true)
    require_count NM "$invalid_count" "$n" "generated invalid-immediate sections"
    n=$(grep -c '=ArgumentOutOfRangeException$' "$_NM_OUT" || true)
    require_count NM 0 "$n" "AOOE results while the PackedSimd token is false"
    n=$(grep -c '=PlatformNotSupportedException$' "$_NM_OUT" || true)
    require_count NM "$invalid_count" "$n" "PNSE results before immediate dispatch"
    violations=$(platform_isa_invalid_immediate_violations "$_NM_OUT") || exit 1
    if [ -n "$violations" ]; then
        echo "FAIL: run NM invalid-immediate token/exception contract:" >&2
        printf '%s\n' "$violations" >&2
        gate_run_diag_once
        exit 1
    fi
    echo "OK: native-only immediate checks follow token-before-range order"

    gate_cache_commit
)
