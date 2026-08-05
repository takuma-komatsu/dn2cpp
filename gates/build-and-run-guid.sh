#!/usr/bin/env bash
# Consolidated System.Guid gate, transpiled from the real CoreLib IL:
# NewGuid (RFC 4122 version/variant bits, uniqueness, round-trips — printed as
# derived properties only, never raw GUID text, so the diff vs real .NET holds
# despite real entropy), Parse/TryParse/ParseExact across D/N/B/P/X, ToString/
# interpolation/TryFormat, byte[]/span/bigEndian ctors + ToByteArray/
# TryWriteBytes (doubling as a 16-byte layout check), comparison/hashing/
# collection keys. NewGuid bottoms out in the P/Invoke
# SystemNative_GetCryptographicallySecureRandomBytes, implemented in
# runtime/core/platform/posix/dn2cpp_system_native.cpp (native-only; that PAL
# file is not part of the wasm build).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate GuidOps
