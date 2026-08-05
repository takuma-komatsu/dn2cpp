#!/usr/bin/env bash
# Consolidated compression-core gate: DeflateStream/GZipStream round-trips
# (basic, all four CompressionLevel values, a buffer sized to span multiple
# internal-buffer refills, concatenated gzip members, corrupted-input error
# paths, the async Stream surface — WriteAsync/ReadAsync/FlushAsync/
# CopyToAsync/DisposeAsync round-trips plus pre-canceled-token early-outs —
# ZLibStream, and the Brotli face: BrotliStream round-trips across every
# CompressionLevel plus the BrotliEncoder/BrotliDecoder one-shot struct API
# and corrupted-input error paths, against the vendored third_party/brotli),
# transpiled once against the tree-shaken real net10.0 CoreLib + real
# System.IO.Compression + real System.IO.Compression.Brotli, and diffed
# exactly against real .NET.
#
# TWO ARMS, because there are now two backends and the round-trip diff cannot
# tell them apart. Referencing System.IO.Compression makes the transpiler
# inject the DnZlib shim by default, and System.IO.Compression.Brotli makes it
# inject DnBrotli (Compilation.InjectDefaultRefs) — so this one program runs
# fully managed unless the arm declines them. Both backends pass the same
# round-trip diff, which is exactly the problem: without the split, flipping
# the default silently converted the suite's only native-codec end-to-end test
# into a second managed-codec one, green throughout.
#
#   Arm 1 (native, artifacts/compressioncore-native) opts out of BOTH shims by
#     name, so DeflateStream/GZipStream/ZLibStream run on the vendored zlib and
#     BrotliStream on the vendored brotli. THE OPT-OUT LOOKS REMOVABLE AND IS
#     NOT: delete either --no-default-ref and the diff still passes, the gate
#     still says OK, and the native codecs are simply never exercised again.
#     The nm positive control below is what makes that visible — it is the only
#     assertion in this gate that can tell the two arms apart.
#   Arm 2 (default, artifacts/compressioncore-default) passes no flag at all,
#     i.e. it is the shape a tool-installed dn2cpp gives someone who wrote
#     `new GZipStream(...)` and passed no -r: the shims are injected because the
#     BCL that needs them is in the load set. Its nm assertions are the mirror
#     image — no CompressionNative_*, no native deflate/inflate, no _Brotli* —
#     so together the two arms prove the DEFAULT is managed, the OPT-OUT is
#     native, and both produce byte-identical output to real .NET.
#
# Pinned to net10.0 (resolve_net10_corelib, not locate_corelib) for the same
# reason the JSON gates are: this host has multiple side-by-side shared
# runtimes installed and the System.IO.Compression native surface can shift
# shape across them. See net10_bcl_diff_gate / resolve_net10_corelib in
# _common.sh.
source "$(dirname "$0")/_common.sh"

# Both arms build with --keep-symbols (-DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF):
# dead-strip would drop the very codec symbols these assertions read, which
# breaks the presence checks outright and — worse — makes the absence checks
# pass vacuously.

# assert_native_codecs OUT PROJECT — arm 1's positive control. Both vendored
# codecs must actually be linked in. The greps mirror the shapes the
# compression-dnzlib / compression-dnbrotli gates assert the ABSENCE of, so the
# two directions cannot drift apart: ` <letter> _?<name>$` anchored on the name
# field, with the letter class accepting local t/u alongside T/U (the runtime
# compiles with hidden visibility, which turns a linked-in definition's nm
# classification lowercase, and this is about what got linked, not exported).
# Snapshot the table once and match from a here-string, never `nm | grep -q`:
# under `set -o pipefail` grep -q matching early SIGPIPEs nm and the pipeline
# reports failure, which would break a must-be-present check.
assert_native_codecs() {
    local out="$1" project="$2" syms
    echo "== Positive control: both native codecs are linked (nm) =="
    syms="$(dump_symbols "$out/$project")"
    if ! grep -qE " [Tt] _?deflate$" <<<"$syms"; then
        echo "FAIL: $project has no native zlib deflate — the native arm is not native any more." >&2
        echo "      Most likely --no-default-ref DnZlib was dropped from the arm below; the" >&2
        echo "      round-trip diff passes either way, so this line is the only symptom." >&2
        return 1
    fi
    if ! grep -qE " [Tt] _?BrotliEncoderCompress$" <<<"$syms"; then
        echo "FAIL: $project has no native BrotliEncoderCompress — the native arm is not native" >&2
        echo "      any more. Most likely --no-default-ref DnBrotli was dropped from the arm" >&2
        echo "      below; the round-trip diff passes either way." >&2
        return 1
    fi
    echo "OK: native zlib deflate and native BrotliEncoderCompress are both linked"
}

# assert_managed_codecs OUT PROJECT — arm 2's proof that the injected shims,
# not the vendored native libraries, are what ran. The transpiled DnZlib
# adapter class is itself named CompressionNative and transpiled DnBrotli
# methods are _m_*/__Z*m_DnBrotli_*-mangled, so anchoring on the extern-"C"
# name-field shape is what keeps those out of the match.
assert_managed_codecs() {
    local out="$1" project="$2" syms
    echo "== Asserting the DEFAULT (no -r, no flag) is the managed backend (nm) =="
    syms="$(dump_symbols "$out/$project")"
    if grep -qE " [TUtu] _CompressionNative_" <<<"$syms"; then
        echo "FAIL: $project still references CompressionNative_* — DnZlib was not injected" >&2
        return 1
    fi
    if grep -qE " [Tt] _?(deflate|inflate)$" <<<"$syms"; then
        echo "FAIL: $project still links native zlib deflate/inflate — DnZlib was not injected" >&2
        return 1
    fi
    if grep -qE " [TUtu] _Brotli" <<<"$syms"; then
        echo "FAIL: $project still links native brotli — DnBrotli was not injected" >&2
        return 1
    fi
    echo "OK: no native codec symbol survives — GZip/Deflate/ZLib/Brotli all ran managed"
}

echo "#### Arm 1/2: the NATIVE codecs, with both default shims declined ####"
net10_bcl_diff_gate \
    --out-suffix -native --keep-symbols --post-assert assert_native_codecs \
    --cli-arg --no-default-ref --cli-arg DnZlib \
    --cli-arg --no-default-ref --cli-arg DnBrotli \
    CompressionCore System.IO.Compression System.IO.Compression.Brotli

echo "#### Arm 2/2: the DEFAULT shape — no flag, both shims injected ####"
net10_bcl_diff_gate \
    --out-suffix -default --keep-symbols --post-assert assert_managed_codecs \
    CompressionCore System.IO.Compression System.IO.Compression.Brotli

echo OK
