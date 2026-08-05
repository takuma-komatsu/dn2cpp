#!/usr/bin/env bash
# Managed-backend brotli gate: the same CompressionCore program the native-path
# gate runs, but transpiled with DnBrotli (internal/DnBrotli — the pure-C#
# brotli) as an extra reference. DnBrotli's [NativeImplementation] methods
# substitute the twelve raw brotli P/Invokes of the real CoreLib's
# System.IO.Compression.Brotli at transpile time, so BrotliStream/BrotliEncoder/
# BrotliDecoder run fully managed and the transpiler's vendored native brotli
# drops out of the binary — merely referencing DnBrotli flips the backend. A
# second variant references DnBrotli AND DnZlib together, proving the two swaps
# compose: a System.IO.Compression with zero native codec symbols. Output is
# diffed exactly against real .NET, which is a valid oracle only because
# CompressionCore prints round-trip results, never self-created compressed byte
# counts (DnBrotli's raw-stream bytes happen to match native brotli v1.1.0
# today, but that is implementation-defined and this gate must not rely on it;
# a future section printing self-created compressed sizes would split this diff
# — keep them round-trip based). nm then proves the swap happened: no native
# brotli symbol may survive (the raw brotli API is re-exported unwrapped, so
# every native entry point is a _Brotli*-prefixed name-field entry; transpiled
# DnBrotli methods are _m_*/__Z*m_DnBrotli_*-mangled — no false positives).
# The DnBrotli-only variant keeps native zlib deflate as the positive control
# that the swap is surgical, not a wholesale unlinking of the native lib; the
# combined variant asserts all of DnZlib's checks too (no CompressionNative_*,
# no native deflate/inflate).
#
# Pinned to net10.0 (resolve_net10_corelib) for the same reason the
# compression-core/dnzlib gates are. Distinct OUT dirs (artifacts/*dnbrotli*)
# so this parallelizes with those gates.
source "$(dirname "$0")/_common.sh"

# dnbrotli_diff_gate PROJECT WITH_DNZLIB
#   WITH_DNZLIB=1 also builds + references DnZlib and asserts the zlib swap.
dnbrotli_diff_gate() {
    local project="$1"; shift
    local with_dnzlib="$1"; shift
    local out
    out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')dnbrotli"
    [ "$with_dnzlib" = 1 ] && out="${out}dnzlib"

    echo "== 1/5 Locating the real net10.0 CoreLib + System.IO.Compression[.Brotli] =="
    local corelib bcl name real_asm refs=()
    corelib=$(resolve_net10_corelib)
    bcl=$(dirname "$corelib")
    echo "corlib:   $corelib"
    for name in System.IO.Compression System.IO.Compression.Brotli; do
        real_asm="$bcl/$name.dll"
        [ -f "$real_asm" ] || { echo "error: real $name not found: $real_asm" >&2; return 1; }
        echo "real bcl: $real_asm"
        refs+=(-r "$real_asm")
    done

    echo "== 2/5 Building DnBrotli + app assembly =="
    build_proj internal/DnBrotli/src/DnBrotli/DnBrotli.csproj
    local dnbrotli="internal/DnBrotli/src/DnBrotli/bin/$CONFIG/$TFM/DnBrotli.dll"
    [ -f "$dnbrotli" ] || { echo "error: not built: $dnbrotli" >&2; return 1; }
    refs+=(-r "$dnbrotli")
    if [ "$with_dnzlib" = 1 ]; then
        build_proj internal/DnZlib/src/DnZlib/DnZlib.csproj
        local dnzlib="internal/DnZlib/src/DnZlib/bin/$CONFIG/$TFM/DnZlib.dll"
        [ -f "$dnzlib" ] || { echo "error: not built: $dnzlib" >&2; return 1; }
        refs+=(-r "$dnzlib")
    fi
    build_proj "samples/dotnet/$project/$project.csproj"
    local app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }

    # Variant 1 only: --no-default-ref DnZlib is what keeps its step-5 positive
    # control (native `deflate` still linked) meaningful. Referencing
    # System.IO.Compression makes the transpiler inject the DnZlib shim by
    # default (Compilation.InjectDefaultRefs), which would substitute exactly the
    # native zlib that assertion says survived — and the round-trip diff passes
    # on either backend, so dropping this flag would empty the assertion while
    # the gate stayed green. Variant 2 wants DnZlib and passes it explicitly as
    # a real -r, which wins (InjectDefaultRefs skips an AlreadyLoaded name), so
    # it needs no opt-out.
    local nodefault=()
    [ "$with_dnzlib" = 1 ] || nodefault=(--no-default-ref DnZlib)
    invoke_cli "$app" -r "$corelib" "${refs[@]}" \
        ${nodefault[@]+"${nodefault[@]}"} --auto-ref -o "$out"
    local key_files=("$app" "$dnbrotli" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json")
    [ "$with_dnzlib" = 1 ] && key_files+=("$dnzlib")
    # The opt-out is spelled into the CONTEXT rather than left implied by
    # $with_dnzlib: a green recorded before the flag existed must not be
    # replayed for a run that passes it.
    local nodefault_ctx="none"
    [ "$with_dnzlib" = 1 ] || nodefault_ctx="DnZlib"
    if gate_cache_check "$out" "dnbrotli_diff_gate|$project|$with_dnzlib|$corelib|nodefault:$nodefault_ctx" \
            "${key_files[@]}"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/5 Compiling C++ and running (exact diff vs real .NET) =="
    # Keep the symbol table: step 5 reads it to prove the substitution happened.
    # Dead-strip is off too — it would drop the very _Brotli* symbols whose
    # *absence* step 5 asserts, turning a real regression into a silent pass
    # (and, in the DnBrotli-only variant, the deflate positive control with it).
    ( export DN2CPP_EXTRA_CMAKE_ARGS="-DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF"
      compile_console "$out" "$project" )
    # Both statuses captured, not just the native one: an oracle invoked inline
    # as `$(dotnet "$app")` contributes only its stdout, so an oracle that died
    # feeds the diff a truncated string — and the native side, which exited 0
    # and printed the same prefix (in the limit, nothing at all), agrees with
    # it.
    local native_out native_code oracle_out oracle_code
    set +e
    native_out="$("./$out/$project")"; native_code=$?
    oracle_out="$(dotnet "$app")"; oracle_code=$?
    set -e
    [ "$native_code" -eq 0 ] \
        || { echo "FAIL: native binary exited $native_code, expected 0" >&2; return 1; }
    [ "$oracle_code" -eq 0 ] \
        || { echo "FAIL: real-.NET oracle \`dotnet $app\` exited $oracle_code, expected 0" >&2; return 1; }
    assert_output "$native_out" "$oracle_out"

    echo "== 5/5 Asserting the brotli backend really swapped (nm) =="
    # Snapshot the symbol table once and grep the string (here-strings, no pipe):
    # under `set -o pipefail`, `nm | grep -q` reports the pipeline as failed when
    # grep -q matches early and nm dies on SIGPIPE, which would break a
    # must-be-present check.
    local syms
    syms="$(dump_symbols "$out/$project")"
    # Native brotli entry points are the only _Brotli*-prefixed name-field
    # entries: dotnet/runtime re-exports the brotli library's own public symbols
    # unwrapped, and transpiled DnBrotli methods are _m_*/__Z*m_DnBrotli_*-mangled
    # — anchor on the ` <letter> _Brotli` shape. The letter class accepts local
    # t/u alongside T/U: the runtime compiles with hidden visibility, which turns
    # a linked-in definition's nm classification lowercase, and these checks are
    # about what got linked, not what got exported.
    if grep -qE " [TUtu] _Brotli" <<<"$syms"; then
        echo "FAIL: $project still links/references native brotli symbols (swap not substituted)" >&2
        return 1
    fi
    if [ "$with_dnzlib" = 1 ]; then
        # Combined variant: DnZlib's own assertions must hold too — a fully
        # managed System.IO.Compression, zero native codec symbols. The
        # transpiled DnZlib adapter class is itself named CompressionNative, so
        # anchor on the extern-"C" ` <letter> _CompressionNative_` shape to
        # exclude its C++-mangled symbols.
        if grep -qE " [TUtu] _CompressionNative_" <<<"$syms"; then
            echo "FAIL: $project still references CompressionNative_* (native zlib shim not substituted)" >&2
            return 1
        fi
        if grep -qE " [Tt] _?(deflate|inflate)$" <<<"$syms"; then
            echo "FAIL: $project still links native zlib deflate/inflate" >&2
            return 1
        fi
    else
        # DnBrotli-only variant: native zlib must survive as the positive
        # control that the swap is surgical, not a wholesale unlinking. It
        # survives only because step 3 passed --no-default-ref DnZlib; without
        # it the DnZlib shim is injected (System.IO.Compression is in the load
        # set), this line goes red, and "fixing" it by deleting the assertion
        # would leave a gate that can no longer tell a surgical swap from a
        # wholesale one.
        if ! grep -qE " [Tt] _?deflate$" <<<"$syms"; then
            echo "FAIL: $project lost the native zlib path (deflate missing) — the swap must be surgical" >&2
            return 1
        fi
    fi
    gate_cache_commit
}

# Variant 1: DnBrotli only — brotli managed, zlib stays native (positive control).
dnbrotli_diff_gate CompressionCore 0
# Variant 2: DnBrotli + DnZlib — fully managed System.IO.Compression.
dnbrotli_diff_gate CompressionCore 1

echo OK
