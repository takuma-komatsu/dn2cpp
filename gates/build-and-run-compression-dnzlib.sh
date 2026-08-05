#!/usr/bin/env bash
# Managed-backend compression gate: the same CompressionCore and ZipCore programs
# the native-path gates run, but transpiled with DnZlib (internal/DnZlib — the
# pure-C# zlib) as an extra reference. DnZlib's [NativeImplementation] methods
# substitute the real CoreLib's eight CompressionNative_* P/Invokes at transpile
# time, so DeflateStream/GZipStream/ZLibStream and ZipArchive's CRC-32 run fully
# managed while the Brotli face stays on the vendored native path — merely
# referencing DnZlib flips the backend. Output is diffed exactly against real
# .NET, which is a valid oracle only because these programs print round-trip
# results, never self-created compressed byte counts (DnZlib's encoder output is
# byte-different from both zlib-ng and classic zlib; a future section printing
# self-created compressed sizes would split this diff — keep them round-trip
# based). nm then proves the swap happened: no CompressionNative_* symbol and no
# native deflate/inflate may survive in either binary (transpiled DnZlib symbols
# are m..._DnZlib_-prefixed, so no false positives), while for CompressionCore
# BrotliEncoderCompress must remain as the positive control that the swap is
# surgical, not a wholesale unlinking of the native lib. That control needs
# --no-default-ref DnBrotli to survive — see the comment at the transpile.
#
# Pinned to net10.0 (resolve_net10_corelib) for the same reason the
# compression-core/zip gates are. Distinct OUT dirs (artifacts/*dnzlib) so this
# parallelizes with those gates.
source "$(dirname "$0")/_common.sh"

# dnzlib_diff_gate PROJECT REQUIRE_BROTLI BCL_ASSEMBLY_NAME...
#   REQUIRE_BROTLI=1 also asserts the native Brotli path survived the swap.
dnzlib_diff_gate() {
    local project="$1"; shift
    local require_brotli="$1"; shift
    local out
    out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')dnzlib"

    echo "== 1/5 Locating the real net10.0 CoreLib + $* =="
    local corelib bcl name real_asm refs=()
    corelib=$(resolve_net10_corelib)
    bcl=$(dirname "$corelib")
    echo "corlib:   $corelib"
    for name in "$@"; do
        real_asm="$bcl/$name.dll"
        [ -f "$real_asm" ] || { echo "error: real $name not found: $real_asm" >&2; return 1; }
        echo "real bcl: $real_asm"
        refs+=(-r "$real_asm")
    done

    echo "== 2/5 Building DnZlib + app assembly =="
    build_proj internal/DnZlib/src/DnZlib/DnZlib.csproj
    local dnzlib="internal/DnZlib/src/DnZlib/bin/$CONFIG/$TFM/DnZlib.dll"
    [ -f "$dnzlib" ] || { echo "error: not built: $dnzlib" >&2; return 1; }
    build_proj "samples/dotnet/$project/$project.csproj"
    local app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }

    echo "== 3/5 Transpiling app + real CoreLib + $* + DnZlib (--auto-ref, tree-shaken) =="
    # --no-default-ref DnBrotli is what keeps the REQUIRE_BROTLI positive control
    # in step 5 meaningful. Referencing System.IO.Compression.Brotli makes the
    # transpiler inject the DnBrotli shim by default (Compilation.InjectDefaultRefs),
    # which would substitute the native brotli this gate asserts SURVIVED — and the
    # round-trip diff passes on either backend, so removing this flag would delete
    # the assertion's whole meaning while the gate stayed green.
    #
    # DnZlib needs no such flag, and that is itself a live test of the precedence
    # rule: the explicit `-r $dnzlib` below wins, and InjectDefaultRefs skips its
    # DnZlib row as AlreadyLoaded. If that rule ever broke, the shim would be
    # loaded twice under one simple name and this transpile would fail.
    invoke_cli "$app" -r "$corelib" "${refs[@]}" -r "$dnzlib" \
        --no-default-ref DnBrotli --auto-ref -o "$out"

    # The --no-default-ref token above is folded into the CONTEXT: a green
    # recorded before it existed must not be replayed for a run that passes it.
    if gate_cache_check "$out" "dnzlib_diff_gate|$project|$require_brotli|$*|$corelib|nodefault:DnBrotli" \
            "$app" "$dnzlib" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/5 Compiling C++ and running (exact diff vs real .NET) =="
    # Keep the symbol table: step 5 reads it to prove the substitution happened.
    # Dead-strip is off too — it would drop the very deflate/inflate symbols whose
    # *absence* step 5 asserts, turning a real regression into a silent pass.
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

    echo "== 5/5 Asserting the zlib backend really swapped (nm) =="
    # Snapshot the symbol table once and grep the string (here-strings, no pipe):
    # under `set -o pipefail`, `nm | grep -q` reports the pipeline as failed when
    # grep -q matches early and nm dies on SIGPIPE, which would break a
    # must-be-present check.
    local syms
    syms="$(dump_symbols "$out/$project")"
    # Match only the extern "C" entry-point form (name field == _CompressionNative_*).
    # The transpiled DnZlib adapter class is itself named CompressionNative, so its
    # C++-mangled symbols (__Z..._CompressionNative_Deflate_NNN) contain the substring —
    # anchor on the ` <letter> _CompressionNative_` shape to exclude them. The letter
    # class accepts local t/u alongside T/U: the runtime compiles with hidden
    # visibility, which turns a linked-in definition's nm classification lowercase,
    # and these checks are about what got linked, not what got exported.
    if grep -qE " [TUtu] _CompressionNative_" <<<"$syms"; then
        echo "FAIL: $project still references CompressionNative_* (native zlib shim not substituted)" >&2
        return 1
    fi
    if grep -qE " [Tt] _?(deflate|inflate)$" <<<"$syms"; then
        echo "FAIL: $project still links native zlib deflate/inflate" >&2
        return 1
    fi
    # The positive control. It survives only because step 3 passes
    # --no-default-ref DnBrotli: otherwise the transpiler injects the DnBrotli
    # shim (System.IO.Compression.Brotli is in the load set) and the native
    # brotli this line looks for is gone — taking the assertion's meaning with
    # it, and silently, because the round-trip diff above passes on either
    # backend.
    if [ "$require_brotli" = 1 ] && ! grep -qE " [Tt] _?BrotliEncoderCompress$" <<<"$syms"; then
        echo "FAIL: $project lost the native Brotli path (BrotliEncoderCompress missing) — the swap must be surgical" >&2
        return 1
    fi
    gate_cache_commit
}

# CompressionCore also asserts Brotli stayed native (the substitution is
# zlib-only); ZipCore exercises the CompressionNative_Crc32 route.
dnzlib_diff_gate CompressionCore 1 System.IO.Compression System.IO.Compression.Brotli
dnzlib_diff_gate ZipCore 0 System.IO.Compression

echo OK
