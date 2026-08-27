#!/usr/bin/env bash
# Consolidated enum gate: [Flags] enums, enums in string interpolation,
# Enum.ToString, value->name ToString, external-assembly enums, and the real
# System.Enum..cctor (its (RuntimeType)typeof(bool) castclass must verify
# against the shared Type header — EnumCctorRuntimeTypeSubset runs the real cctor
# via RunClassConstructor and pins the underlying-type surface). Diffed exactly
# vs real .NET.
# EnumWide64Subset is not a theme but a WIDTH: every lowering that reads an enum's
# declared member table, driven with a long/ulong-underlying enum whose constants do
# not survive the int32 model — the interpolation hole, the generic Enum.*<T>
# statics, the packed E[] materialization, the boxed ToString slot, the non-generic
# Type-driven statics, Convert.ChangeType's own boxed-enum reader, and Enum.Format.
# Its members include long.MinValue and 1UL<<63, which is also the only assert that
# the emitted enummembers_ table renders a C++ literal that COMPILES.
# Its last two blocks are a WIDTH subject the heading does not say: Enum.Parse's numeric
# token is range-checked at the UNDERLYING type, not at the int32/int64 model, so they
# span all eight underlyings — including the OverflowException-vs-Argument-
# Exception split a name-path fall-through erases — and then read a boxed enum's payload
# width back off a run-time type handle (Activator / GetUninitializedObject / ToObject),
# which is what sizes the allocation.
# Generic Enum.Parse/TryParse cover all four ReadOnlySpan<char> overloads; each
# must honor the active slice bounds before consulting the enum member table.
# Former gates: enum-flags, enum-interp, enum-tostring, enum-value-tostring,
# external-enum.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate EnumOps
