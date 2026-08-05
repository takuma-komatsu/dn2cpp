#!/usr/bin/env bash
# Emit-order stability: the emitted C++ is a function of the INPUT ASSEMBLIES, not of
# the order in which discovery happened to walk the model.
#
# Why this needs a gate. Model discovery and emission used to be coupled by accident.
# The specialization queue's drain order decided the order Compilation.Classes grew
# in, and emission read that order in six places at once: which same-CppName twin won
# (shared-source internal generics are linked into several assemblies), what order
# declarations came out in, the numbering of the literal pool (str_N/blob_N) and of
# the rgctx slots — both handed out by first use during body compilation and then
# baked into method bodies — the content-interned metadata pools (parmpool_,
# itfpool_, genargpool_, ...), and the order static constructors run in within a
# module depth (that last one changes BEHAVIOUR, not just bytes: it is how
# System.HashCode's seed once got bucketed at zero). ClassInfo.CompareByOrder now orders
# all six by the class's identity in the input metadata instead.
#
# How the gate proves it. DN2CPP_SPEC_DRAIN=lifo drains the specialization queue
# backwards. That is a sound perturbation, not a different compilation: the reachable
# set and the specialization closure are both least fixpoints of monotone operators,
# so draining in the opposite order reaches the very same SET — only the order the
# model is built in changes. So the two runs must agree byte-for-byte, and any byte
# that moves is emission reading discovery order again.
#
# Run over samples that are dense in exactly the things that used to move: generic
# instantiations (twins, rgctx slots, shared bodies), string literals, and static
# constructors. Both with canonical shared generics on (the default) and off, because
# the canonical-owner linkage walks Classes order too.
#
# The second axis: DN2CPP_STRICT_COMPLETION=1. A specialization's members are decoded on
# demand, and the accessors on ClassInfo decode them for whoever reads them — so a reader
# that forgot to ask cannot produce a wrong answer, it can only make the model bigger than
# it needed to be. That failure is silent, and it is the failure that matters: one walk
# over every class that reads .Methods and filters the result would decode the entire
# program and give back every byte the deferral saves, with nothing to show for it. Strict
# mode makes that walk throw instead of decoding, so this gate can hold the discipline: the
# corpus must transpile clean under it, and emit the same bytes with it on and off (it
# changes nothing that reaches the output — if it did, the deferral would not be sound).
set -euo pipefail
cd "$(dirname "$0")/.."
# shellcheck source=gates/_common.sh
. gates/_common.sh

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
echo "corlib: $corelib"

OUT=artifacts/emit-order-stability
rm -rf "$OUT"
mkdir -p "$OUT"

# project | extra BCL references (the same reference set each project's own gate uses)
SAMPLES=(
    "ReflectTypes|System.Linq.Expressions System.Linq System.Collections System.Reflection.Emit System.Reflection.Emit.Lightweight System.Reflection.Emit.ILGeneration System.ComponentModel.Primitives"
    "StringCore|System.Linq"
    "ArrayCore|"
)

echo "== 2/4 Building app assemblies =="
for entry in "${SAMPLES[@]}"; do
    build_proj "samples/dotnet/${entry%%|*}/${entry%%|*}.csproj"
done

# The whole gate is transpiler BEHAVIOR — byte-identity across drain orders and
# strict-completion cleanliness; its dozen-plus transpiles ARE the work and much
# of it (a clean strict run) leaves nothing to key on — so the cache key stands
# in for the transpiler itself via _gate_cli_hash (see that helper's doc). OUT
# was just cleared, so the key's surface term is empty and stable. The
# transpiler-behavior env axis rides in the context for the same reason: an
# ambient DN2CPP_SPEC_DRAIN/STRICT_COMPLETION (this gate's own levers!) or a
# cap/assert knob changes what the runs below do, with no surface to catch it.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
if gate_cache_check "$OUT" "emit-order-stability|cli:$(_gate_cli_hash)|$corelib|$tenv" \
        "samples/dotnet/ReflectTypes/bin/$CONFIG/$TFM/ReflectTypes.dll" \
        "samples/dotnet/StringCore/bin/$CONFIG/$TFM/StringCore.dll" \
        "samples/dotnet/ArrayCore/bin/$CONFIG/$TFM/ArrayCore.dll"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/4 Transpiling each FIFO and LIFO, comparing bytes =="
for entry in "${SAMPLES[@]}"; do
    proj=${entry%%|*}
    extras=${entry#*|}
    app="samples/dotnet/$proj/bin/$CONFIG/$TFM/$proj.dll"

    refs=(-r "$corelib")
    # A requested extra is a hard requirement: dropping an absent one transpiles
    # a different, smaller program and asserts it green, and the cache CONTEXT
    # carries the REQUESTED names so the narrowed run is replayed forever
    # (same rule as _corelib_gate_core).
    for name in $extras; do
        [ -f "$bcl/$name.dll" ] \
            || { echo "error: requested reference $name not found beside the CoreLib: $bcl/$name.dll" >&2; exit 1; }
        refs+=(-r "$bcl/$name.dll")
    done

    for sharing in on off; do
        share_flag=""
        [ "$sharing" = off ] && share_flag="--no-shared-generics"

        fifo="$OUT/$proj-$sharing-fifo"
        lifo="$OUT/$proj-$sharing-lifo"
        rm -rf "$fifo" "$lifo"

        # shellcheck disable=SC2086  # $share_flag is one flag or empty, deliberately
        invoke_cli "$app" "${refs[@]}" $share_flag -o "$fifo" >/dev/null
        # shellcheck disable=SC2086
        DN2CPP_SPEC_DRAIN=lifo invoke_cli "$app" "${refs[@]}" $share_flag -o "$lifo" >/dev/null

        if ! diff -r "$fifo" "$lifo" >/dev/null 2>&1; then
            echo "FAIL: $proj (shared generics $sharing) — the emitted C++ depends on"
            echo "      the specialization drain order. Emission is reading discovery"
            echo "      order somewhere; the diff says where:"
            head -20 <<<"$(diff -r "$fifo" "$lifo")"
            exit 1
        fi
        echo "OK: $proj (shared generics $sharing) — byte-identical across drain orders"
    done
done

echo "== 4/4 Every member read is one somebody asked for (DN2CPP_STRICT_COMPLETION=1) =="
for entry in "${SAMPLES[@]}"; do
    proj=${entry%%|*}
    extras=${entry#*|}
    app="samples/dotnet/$proj/bin/$CONFIG/$TFM/$proj.dll"

    refs=(-r "$corelib")
    # A requested extra is a hard requirement: dropping an absent one transpiles
    # a different, smaller program and asserts it green, and the cache CONTEXT
    # carries the REQUESTED names so the narrowed run is replayed forever
    # (same rule as _corelib_gate_core).
    for name in $extras; do
        [ -f "$bcl/$name.dll" ] \
            || { echo "error: requested reference $name not found beside the CoreLib: $bcl/$name.dll" >&2; exit 1; }
        refs+=(-r "$bcl/$name.dll")
    done

    for sharing in on off; do
        share_flag=""
        [ "$sharing" = off ] && share_flag="--no-shared-generics"

        strict="$OUT/$proj-$sharing-strict"
        rm -rf "$strict"

        rc=0
        # shellcheck disable=SC2086
        err=$(DN2CPP_STRICT_COMPLETION=1 invoke_cli "$app" "${refs[@]}" $share_flag -o "$strict" 2>&1 >/dev/null) || rc=$?
        if [ "$rc" -ne 0 ]; then
            echo "FAIL: $proj (shared generics $sharing) read a specialization's members without"
            echo "      asking for them. That is not a wrong answer — the accessor decodes them —"
            echo "      but if it walks every class it decodes the whole program, which is the"
            echo "      saving. Either pull it (EnsureCompleted/EnsureMembers) or skip the"
            echo "      specializations nothing reaches. The throw says which read it was:"
            head -12 <<<"$err"
            exit 1
        fi

        # Strict mode only decides what happens on an un-asked-for read; it must not change
        # what comes out. If these differ, the deferral is unsound — some emitted byte
        # depends on WHEN a specialization was decoded.
        if ! diff -r "$OUT/$proj-$sharing-fifo" "$strict" >/dev/null 2>&1; then
            echo "FAIL: $proj (shared generics $sharing) emits different bytes under strict"
            echo "      completion — an emitted byte depends on when members were decoded:"
            head -20 <<<"$(diff -r "$OUT/$proj-$sharing-fifo" "$strict")"
            exit 1
        fi
        echo "OK: $proj (shared generics $sharing) — strict-clean, and byte-identical to the normal run"
    done
done

gate_cache_commit
echo "OK: emitted C++ is a function of the input, not of discovery order — and every"
echo "    specialization whose members were decoded is one something asked for"
