#!/usr/bin/env bash
# Consolidated crypto-hash gate: the real net10 System.Security.Cryptography.dll
# transpiled and run over dn2cpp's portable AppleCryptoNative_* PAL
# (runtime/core/intrinsics/dn2cpp_apple_crypto_digest.cpp) — one-shot HashData
# for MD5/SHA1/SHA256/SHA384/SHA512 (with NIST/RFC known-answer asserts),
# instance ComputeHash reuse (DigestFinal re-init), TransformBlock over odd
# chunks, IncrementalHash with mid-stream GetCurrentHash (DigestCurrent),
# HashData(Stream), the five HMACs (one-shot / instance / over-block-length
# key / incremental, RFC 4231 vectors), and RandomNumberGenerator derived
# properties. Diffed exactly vs real .NET.
#
# The last section (AchievementKeyHmacSubset) is NOT about the hash cores: its
# subject is System.Convert's UNSIGNED overloads and ulong.RotateLeft/Right —
# the key-derivation arithmetic Thrive's achievements.bin integrity HMAC is
# built from. Convert.ToString(ulong) rendering the value signed makes the
# derived key wrong with a hash mismatch as the only symptom; the same slip sits
# in Convert.ToInt32/ToInt64/ToDouble/ToSingle over unsigned sources. It
# lives in this bucket because it needs System.Security.Cryptography, not
# because it is a hashing test — do not prune it by theme.
source "$(dirname "$0")/_common.sh"

net10_bcl_diff_gate CryptoHashCore System.Security.Cryptography
