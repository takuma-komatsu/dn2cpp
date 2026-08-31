#!/usr/bin/env bash
# Parallel method-body scheduling: a jobs=2 measure run must put two proven-pure
# bodies in flight concurrently. The fixture deliberately reaches exactly two
# static scalar bodies and one non-pure Main body. One scalar body carries a raw
# LOCAL_SIG and ldloc/stloc instructions, so the exact eligible/serial census also
# holds the primitive-local classifier rather than merely observing unrelated BCL
# work. The known P/Invoke marshalling gap keeps the run in measure mode without
# adding a managed call edge. Shared generics are disabled here so the census is
# one scheduling pass: the default two-pass protocol is covered by emit-order.
source "$(dirname "$0")/_common.sh"

echo "== 1/3 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/3 Building the primitive-local scheduling fixture =="
PROJECT=PInvokeByValTStrBad
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
app="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"

OUT=artifacts/parallel-bodies
rm -rf "$OUT"
mkdir -p "$OUT"

# This gate measures transpiler behavior directly, so the CLI hash stands in for
# the implementation. The generic/heap/assert knobs can change whether the body
# fixpoint reaches the scheduler and therefore belong to the cache context.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
if gate_cache_check "$OUT" "parallel-bodies|jobs:2|sharing:off|cli:$(_gate_cli_hash)|$corelib|$tenv" \
        "$app"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/3 Proving primitive-local bodies overlap on two workers =="
measure_out="$OUT/measure"
set +e
timing=$(DN2CPP_TIME=1 \
    invoke_cli "$app" -r "$corelib" --measure --jobs 2 --no-shared-generics \
    -o "$measure_out" \
    2>&1 >/dev/null)
rc=$?
set -e
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the jobs=2 measure transpile exited $rc:" >&2
    printf '%s\n' "$timing" >&2
    exit 1
fi

worker_line=$(grep '^dn2cpp-time: parallel-bodies ' <<<"$timing" || true)
if grep -Eq '^dn2cpp-time: parallel-bodies requested 2 effective 1( |$)' \
        <<<"$worker_line"; then
    gate_skip "Environment.ProcessorCount < 2; two body workers cannot run concurrently"
fi
if ! grep -Eq '^dn2cpp-time: parallel-bodies requested 2 effective 2 peak 2 retries [0-9]+ eligible 2 serial 1$' \
        <<<"$worker_line"; then
    echo "FAIL: jobs=2 did not run exactly the two scalar fixture bodies concurrently" >&2
    printf 'worker stats: %s\n' "${worker_line:-<missing>}" >&2
    exit 1
fi
[ -s "$measure_out/s0-gaps.tsv" ] \
    || { echo "FAIL: the measure fixture produced no known P/Invoke gap row" >&2; exit 1; }

printf '%s\n' "$worker_line"
gate_cache_commit
echo "OK: two primitive scalar bodies overlapped; the raw LOCAL_SIG body was eligible"
