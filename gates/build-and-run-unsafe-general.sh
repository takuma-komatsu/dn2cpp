#!/usr/bin/env bash
# Consolidated Unsafe/MemoryMarshal arbitrary-type gate: proves the transpiler's
# System.Runtime.CompilerServices.Unsafe and System.Runtime.InteropServices.MemoryMarshal
# intrinsic interceptions are correct over arbitrary element/struct types and arbitrary
# memory backings (stackalloc, heap arrays, struct fields), not just int. Covers
# Unsafe.As / AsRef / Add / Subtract / ByteOffset / SizeOf / AreSame / NullRef /
# IsNullRef / Read / Write / ReadUnaligned / WriteUnaligned / CopyBlock / InitBlock
# / Unbox (mutate a box's payload in place through the returned ref),
# and MemoryMarshal.Cast / AsBytes / GetReference / GetArrayDataReference / Read /
# Write / CreateSpan / CreateReadOnlySpan over multi-field structs, double/long, and
# byte-backed reinterprets. Also proves [StructLayout] layout control:
# LayoutKind.Explicit unions (all-offset-0 and disjoint offsets), explicit Size,
# Pack=1/Pack=8 packing, and an explicit union nested in a packed sequential
# struct (the godot_variant shape), checked by sizeof / byte punning / AsBytes.
# Reinterpret byte order is host-endian — the gate diffs the
# transpiled binary against real .NET on the same machine, so they agree.
# Diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate UnsafeGeneral
