#!/usr/bin/env bash
# IEEE vector results, lane coverage, and the native/software FMA contract.
vector_floating_witnesses() {
    local output="$1" shape precision operation
    gate_output byte-witness "$output" '-- vector floating point --' \
        'floating witness divide=7FF0000000000000' \
        'floating witness min-nan=NaN' 'floating witness max-nan=NaN' \
        'floating witness clamp-nan=NaN' \
        'floating witness min-zero=8000000000000000' \
        'floating witness max-zero=0000000000000000' \
        'floating witness fma-f32=B2800000' \
        'floating witness fma-f64=BC30000000000000' \
        'vector floating point end' --count 1 --policy crlf
    for shape in v64 v128 v256 v512 vn; do
        for precision in f32 f64; do
            for operation in Min Max Clamp Fma Divide operator/ MinNative MaxNative ClampNative; do
                gate_output match-record "$output" \
                    "floating $shape $precision $operation=[0-9A-F]{16}" --count 1 --policy crlf
            done
        done
    done
}

vector_floating_prefix() {
    local out="$1" operation suffix
    DOTNET_EnableAVX=0 gate_capture_ok "$out/before-floating-point" \
        run_bounded dotnet "$_CG_APP" before-floating-point
    gate_output prefix "$out/native.stdout" "$out/before-floating-point.stdout" \
        --before '-- vector floating point --' --policy crlf
    vector_floating_witnesses "$out/native.stdout"
    for operation in min max clamp; do
        for suffix in '' _native; do
            grep -qw "dn2cpp_vec_${operation}${suffix}" "$out/generated.h" "$out"/generated*.cpp \
                || { echo "FAIL: missing vector route dn2cpp_vec_${operation}${suffix}" >&2; return 1; }
        done
    done
}

vector_fma_branches() {
    local app="$1" out=artifacts/vector-fma-branches capability arch=none
    mkdir -p "$out"
    gate_capture_ok "$out/capability" run_bounded dotnet "$app" fma-support
    gate_output match-record "$out/capability.stdout" '(avx2|arm64|none)' --count 1 --policy crlf
    gate_output view "$out/capability.stdout" "$out/capability.lf" --policy crlf
    IFS= read -r capability < "$out/capability.lf"
    case "$capability" in
        avx2|arm64) arch="$capability" ;;
        none) echo 'Native FMA target is outside this host ISA; checking the software FMA target.' ;;
    esac
    if gate_cache_check "$out" "vector-fma-branches|native:$arch|HWY_COMPILE_ONLY_EMU128|fp-contract:off" \
            gates/_vector-floating-point.sh gates/fixtures/vector-floating-point/CMakeLists.txt \
            gates/fixtures/vector-floating-point/fma.cpp "$app"; then
        gate_cache_hit_msg
        return 0
    fi
    local compiler_args=()
    [ -n "${CMAKE_CXX_COMPILER:-}" ] && compiler_args+=(-DCMAKE_CXX_COMPILER="$CMAKE_CXX_COMPILER")
    _cmake_configure always "" "$out/build" "$out/configure.log" 'configuring vector FMA branch checks' \
        "$CMAKE" -S gates/fixtures/vector-floating-point -B "$out/build" -G Ninja \
        -DCMAKE_BUILD_TYPE="$CONFIG" -DDN2CPP_VECTOR_FMA_ARCH="$arch" \
        ${compiler_args[@]+"${compiler_args[@]}"}
    _cmake_step "$out/build.log" 'building vector FMA branch checks' "$CMAKE" --build "$out/build"
    gate_capture_ok "$out/software" run_bounded "$out/build/vector-fma-software$EXE_EXT"
    gate_assert_text "$out/software.stdout" 'FMA software branch: exact'
    if [ "$arch" != none ]; then
        gate_capture_ok "$out/native" run_bounded "$out/build/vector-fma-native$EXE_EXT"
        gate_assert_text "$out/native.stdout" 'FMA native branch: exact'
    fi
    gate_cache_commit
}

vector_floating_wasm_gate() {
    local mode out _CG_CORELIB_IN DN2CPP_WASM_SIMD=
    _CG_CORELIB_IN=$(locate_corelib_cross_posix net10)
    export DN2CPP_WASM_SIMD
    for mode in default simd; do
        out="artifacts/vectorprobe-wasm-$mode"
        DN2CPP_WASM_SIMD=
        [ "$mode" = default ] || DN2CPP_WASM_SIMD=1
        _corelib_gate_core VectorProbe "$out"
        if gate_cache_check "$out" "vector-floating-wasm|mode:$mode|$_CG_CORELIB|argv:floating-point|oracle:DOTNET_EnableAVX=0" \
                "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" \
                gates/_vector-floating-point.sh samples/dotnet/VectorProbe/FloatingPoint.cs; then
            gate_cache_hit_msg
            continue
        fi
        compile_console_wasm "$out" VectorProbe
        gate_capture_ok "$out/native" run_bounded node "$out/VectorProbe.js" floating-point
        DOTNET_EnableAVX=0 gate_capture_ok "$out/oracle" run_bounded dotnet "$_CG_APP" floating-point
        gate_output compare "$out/native.stdout" "$out/oracle.stdout" --policy crlf
        vector_floating_witnesses "$out/native.stdout"
        gate_cache_commit
    done
}
