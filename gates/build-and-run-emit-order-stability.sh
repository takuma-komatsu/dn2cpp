#!/usr/bin/env bash
# Emission must produce byte-identical code, metadata and diagnostics when the
# specialization queue is drained in reverse. Strict completion makes reads of
# unrequested members fail while the queue visits each corpus in both orders.
# The publication comparator validates the ILDiet manifest and relativizes its
# output paths before comparing every file.
#
# The full profile isolates options that perturb discovery, scheduling and
# output location. A known P/Invoke gap exercises the separate --measure writer.
# The parallel-bodies gate checks that workers actually overlap.
set -euo pipefail
cd "$(dirname "$0")/.."
# shellcheck source=gates/_common.sh
. gates/_common.sh
case "${DN2CPP_EMIT_ORDER_FULL:-0}" in
    0|1) DN2CPP_EMIT_ORDER_FULL=${DN2CPP_EMIT_ORDER_FULL:-0} ;;
    *) echo 'DN2CPP_EMIT_ORDER_FULL must be 0 or 1' >&2; exit 1 ;;
esac
# Each corner owns these axes rather than inheriting a vacuous comparison.
export DN2CPP_SPEC_DRAIN=fifo DN2CPP_STRICT_COMPLETION=0
gate_python -c 'pass' >/dev/null || gate_skip "no working Python 3 interpreter for publication comparison"

compare_outputs() {
    echo "Comparing $1 and $2" >&2
    gate_python "$(native_path gates/compare-emit-output.py)" \
        "$(native_path "$1")" "$(native_path "$2")"
}
gate_python "$(native_path gates/compare-emit-output.py)" --self-test

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
echo "corlib: $corelib"

OUT=artifacts/emit-order-stability
rm -rf "$OUT"
mkdir -p "$OUT"

if [ "$DN2CPP_EMIT_ORDER_FULL" = 0 ]; then
    echo 'emit-order coverage: SharedGenerics representative combined-axis parity'
    echo 'emit-order corpus, isolated axes, unshared and measure checks: skipped (not selected); not counted as passed'
    build_proj samples/dotnet/SharedGenerics/SharedGenerics.csproj
    app="samples/dotnet/SharedGenerics/bin/$CONFIG/$TFM/SharedGenerics.dll"
    [ -f "$bcl/System.Collections.dll" ] \
        || { echo "error: System.Collections.dll not found beside $corelib" >&2; exit 1; }
    if gate_cache_check "$OUT" "emit-order-stability|representative-v1|cli:$(_gate_cli_hash)|$corelib" \
            gates/compare-emit-output.py gates/ildiet_manifest.py "$app"; then
        gate_cache_hit_msg
        exit 0
    fi
    fifo="$OUT/SharedGenerics-fifo"
    parallel_fifo="$OUT/SharedGenerics-strict-jobs2-fifo"
    lifo="$PWD/$OUT/relocated/SharedGenerics-strict-jobs2-lifo-output-directory"
    invoke_cli "$app" -r "$corelib" -r "$bcl/System.Collections.dll" --jobs 1 -o "$fifo"
    DN2CPP_STRICT_COMPLETION=1 \
        invoke_cli "$app" -r "$corelib" -r "$bcl/System.Collections.dll" --jobs 2 -o "$parallel_fifo"
    DN2CPP_STRICT_COMPLETION=1 DN2CPP_SPEC_DRAIN=lifo \
        invoke_cli "$app" -r "$corelib" -r "$bcl/System.Collections.dll" --jobs 2 -o "$(native_path "$lifo")"
    compare_outputs "$fifo" "$parallel_fifo"
    compare_outputs "$parallel_fifo" "$lifo"
    gate_cache_commit
    echo 'OK: representative output is byte-identical across the selected combined axes'
    exit 0
fi

# project | extra BCL references (the same reference set each project's own gate uses)
SAMPLES=(
    "SharedGenerics|System.Collections"
    "ReflectTypes|System.Linq.Expressions System.Linq System.Collections System.Reflection.Emit System.Reflection.Emit.Lightweight System.Reflection.Emit.ILGeneration System.ComponentModel.Primitives System.Collections.Concurrent"
    "StringCore|System.Linq"
    "ArrayCore|System.Collections"
    "ArrayDispatch|System.Linq"
)

echo "== 2/4 Building app assemblies =="
inputs=("$corelib")
for entry in "${SAMPLES[@]}"; do
    proj=${entry%%|*}
    build_proj "samples/dotnet/$proj/$proj.csproj"
    inputs+=("samples/dotnet/$proj/bin/$CONFIG/$TFM/$proj.dll")
    for name in ${entry#*|}; do
        [ -f "$bcl/$name.dll" ] \
            || { echo "error: requested reference $name not found beside $corelib" >&2; exit 1; }
        inputs+=("$bcl/$name.dll")
    done
done
build_proj samples/dotnet/PInvokeByValTStrBad/PInvokeByValTStrBad.csproj
measure_app="samples/dotnet/PInvokeByValTStrBad/bin/$CONFIG/$TFM/PInvokeByValTStrBad.dll"

if gate_cache_check "$OUT" "emit-order-stability|full|drain:fifo,lifo|strict:0,1|sharing:on,off|jobs:1,2|relocation|measure-jobs:1,2|cli:$(_gate_cli_hash)|$corelib" \
        gates/compare-emit-output.py gates/ildiet_manifest.py "${inputs[@]}" "$measure_app"; then
    gate_cache_hit_msg
    exit 0
fi

compare_drain_order() {
    local prefix="$1"
    shift
    echo "Checking strict FIFO and LIFO for $prefix"
    DN2CPP_STRICT_COMPLETION=1 \
        invoke_cli "$@" --jobs 2 -o "$prefix-fifo" >/dev/null
    DN2CPP_STRICT_COMPLETION=1 DN2CPP_SPEC_DRAIN=lifo \
        invoke_cli "$@" --jobs 2 -o "$prefix-lifo" >/dev/null
    compare_outputs "$prefix-fifo" "$prefix-lifo"
}

echo "== 3/4 Comparing emitted bytes across independent axes =="
for entry in "${SAMPLES[@]}"; do
    proj=${entry%%|*}
    app="samples/dotnet/$proj/bin/$CONFIG/$TFM/$proj.dll"
    refs=(-r "$corelib")
    for name in ${entry#*|}; do
        refs+=(-r "$bcl/$name.dll")
    done
    prefix="$OUT/$proj"
    compare_drain_order "$prefix" "$app" "${refs[@]}"

    # Canonical linkage and closed-generic reflection diagnostics depend on sharing.
    case "$proj" in
        SharedGenerics|ReflectTypes)
            compare_drain_order "$prefix-unshared" "$app" "${refs[@]}" --no-shared-generics
            ;;
    esac
    # Array provenance and reflection diagnostics must survive parallel body planning.
    case "$proj" in
        ArrayCore|ReflectTypes)
            DN2CPP_STRICT_COMPLETION=1 \
                invoke_cli "$app" "${refs[@]}" --jobs 1 -o "$prefix-serial" >/dev/null
            compare_outputs "$prefix-serial" "$prefix-fifo"
            ;;
    esac
    if [ "$proj" = ReflectTypes ]; then
        for report in "$prefix-fifo" "$prefix-unshared-fifo"; do
            [ -s "$report/reflection-unrecognized.tsv" ] \
                || { echo "FAIL: missing reflection diagnostics in $report" >&2; exit 1; }
        done
        # Hold the options fixed while changing the spelling, length and depth of -o.
        relocated="$PWD/$OUT/relocated/ReflectTypes-longer-output-directory"
        DN2CPP_STRICT_COMPLETION=1 \
            invoke_cli "$app" "${refs[@]}" --jobs 1 -o "$(native_path "$relocated")" >/dev/null
        compare_outputs "$prefix-serial" "$relocated"

        invoke_cli "$app" "${refs[@]}" --jobs 2 -o "$prefix-normal" >/dev/null
        compare_outputs "$prefix-normal" "$prefix-fifo"
    fi
done

echo "== 4/4 --measure reports and summaries are stable across worker counts =="
measure_j1="$OUT/measure-jobs1"
measure_j2="$OUT/measure-jobs2"
rm -rf "$measure_j1" "$measure_j2"

set +e
DN2CPP_TIME=0 invoke_cli "$measure_app" -r "$corelib" --measure --jobs 1 -o "$measure_j1" \
    >"$OUT/measure-jobs1.stdout" 2>"$OUT/measure-jobs1.stderr"
measure_rc1=$?
DN2CPP_TIME=1 invoke_cli "$measure_app" -r "$corelib" --measure --jobs 2 -o "$measure_j2" \
    >"$OUT/measure-jobs2.stdout" 2>"$OUT/measure-jobs2.stderr"
measure_rc2=$?
set -e

if [ "$measure_rc1" -ne 0 ] || [ "$measure_rc2" -ne 0 ]; then
    echo "FAIL: --measure must exit 0 after recording the known gap; jobs=1 returned" \
        "$measure_rc1 and jobs=2 returned $measure_rc2" >&2
    exit 1
fi
[ -s "$measure_j1/s0-gaps.tsv" ] \
    || { echo "FAIL: the known-gap fixture produced no jobs=1 gap row" >&2; exit 1; }
[ -s "$measure_j2/s0-gaps.tsv" ] \
    || { echo "FAIL: the known-gap fixture produced no jobs=2 gap row" >&2; exit 1; }

if ! comparison=$(compare_outputs "$measure_j1" "$measure_j2" 2>&1); then
    echo "FAIL: --measure sidecars depend on the worker count:" >&2
    head -20 <<<"$comparison" >&2
    exit 1
fi

# ILDiet reports an absolute native path; the CLI also reports the requested
# relative path. Replace both literally, preserving every other diagnostic byte.
measure_path_pattern() {
    printf '%s' "$1" | sed 's/[][\\.^$*|]/\\&/g'
}
normalize_measure_output() {
    local relative="$1" absolute="$PWD/$1"
    if [ "$DN2CPP_OS" = windows ]; then
        absolute=$(cygpath -aw "$absolute")
    fi
    sed -e "s|$(measure_path_pattern "$absolute")|MEASURE_OUT|g" \
        -e "s|$(measure_path_pattern "$relative")|MEASURE_OUT|g" "$2"
}
normalize_measure_output "$measure_j1" "$OUT/measure-jobs1.stdout" \
    >"$OUT/measure-jobs1.stdout.normalized"
normalize_measure_output "$measure_j2" "$OUT/measure-jobs2.stdout" \
    >"$OUT/measure-jobs2.stdout.normalized"
normalize_measure_output "$measure_j1" "$OUT/measure-jobs1.stderr" \
    >"$OUT/measure-jobs1.stderr.normalized"
normalize_measure_output "$measure_j2" "$OUT/measure-jobs2.stderr" | sed '/^dn2cpp-time:/d' \
    >"$OUT/measure-jobs2.stderr.normalized"
cmp -s "$OUT/measure-jobs1.stdout.normalized" "$OUT/measure-jobs2.stdout.normalized" \
    || { echo "FAIL: --measure summary order depends on the worker count" >&2; exit 1; }
cmp -s "$OUT/measure-jobs1.stderr.normalized" "$OUT/measure-jobs2.stderr.normalized" \
    || { echo "FAIL: --measure diagnostic order depends on the worker count" >&2; exit 1; }
echo "OK: --measure exit, sidecars, summary and diagnostics are identical for jobs=1 and jobs=2"

gate_cache_commit
echo "OK: emit-order and measure comparisons passed"
