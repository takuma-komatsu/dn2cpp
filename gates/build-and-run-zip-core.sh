#!/usr/bin/env bash
# Consolidated in-memory ZipArchive gate: Create-mode round-trips (default +
# every CompressionLevel, subdirectory names, an empty entry), full metadata
# reads over a byte-fixed embedded blob (including CompressedLength/Crc32,
# which are deterministic ONLY because the blob is frozen -- self-created
# archives never print them: real .NET links zlib-ng, the native binary
# classic zlib), Update-mode delete/append/add rewrites, archive/entry
# comments incl. 65535-byte Rune-boundary truncation, and corrupted-archive
# error paths -- transpiled once against the tree-shaken real pinned
# CoreLib + real System.IO.Compression, and diffed exactly against real .NET.
# Everything is in-memory (MemoryStream); no filesystem/ZipFile surface here.
#
# Pinned CoreLib (resolve_pinned_corelib, not locate_corelib) for the same
# reason the compression-core/JSON gates are: this host has multiple
# side-by-side shared runtimes installed and the System.IO.Compression
# native surface can shift shape across them. See bcl_diff_gate /
# resolve_pinned_corelib in _common.sh.
#
# THE --no-default-ref FLAGS ARE WHAT KEEP THIS GATE NATIVE, and they look
# removable because the round-trip diff passes without them. Referencing
# System.IO.Compression makes the transpiler inject the DnZlib shim by default
# (AssemblyLoadSet.InjectDefaultRefs), which substitutes the CompressionNative_*
# P/Invokes — including the CRC-32 this program's every entry goes through —
# with pure C#. Drop the flags and this stops being the native-zlib ZipArchive
# test with nothing going red; the managed-backend ZipCore is already covered
# by `dnzlib_diff_gate ZipCore 0` in build-and-run-compression-dnzlib.sh, which
# asserts the swap with nm rather than trusting the diff. DnBrotli is declined
# too although Zip never uses brotli: --auto-ref pulls the Brotli assembly into
# the closure, so without the flag the shim rides in for nothing.
source "$(dirname "$0")/_common.sh"

# The runtime has no AES or PBKDF2, so the same binary, run with `aes-refusal`,
# must refuse an encrypted entry with the platform-not-supported message. The CLR
# is not run with the argument: it encrypts, which is the intended difference.
assert_aes_refused() {
    local out="$1" project="$2"
    gate_capture_ok "$out/aes" run_bounded "./$out/$project$EXE_EXT" aes-refusal
    gate_output byte-witness "$out/aes.stdout" \
        'PlatformNotSupportedException:WinZip AES encryption is not supported on this platform. A dn2cpp-transpiled binary provides no AES or PBKDF2 implementation.' \
        --count any --policy crlf \
        || { echo "FAIL: WinZip AES refusal witness missing" >&2; return 1; }
}

# link.xml roots WinZipAesStream.CreateKey, a member the WinZip-AES cut row
# replaces: the rooted method must bind through the row, not revive its body
# (whose PBKDF2 import the runtime does not define), or this binary stops linking.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/ZipCore/link.xml" \
bcl_diff_gate \
    --post-assert assert_aes_refused \
    --cli-arg --no-default-ref --cli-arg DnZlib \
    --cli-arg --no-default-ref --cli-arg DnBrotli \
    --cli-arg --link-xml --cli-arg samples/dotnet/ZipCore/link.xml \
    ZipCore System.IO.Compression
