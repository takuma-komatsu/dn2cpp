#!/usr/bin/env bash
# The gate that answers "does dn2cpp support C# <N>?". One section per C# major
# version (Cs01..Cs14), each exercising the features that version introduced, in
# a single deterministic program transpiled once against the tree-shaken real
# CoreLib and diffed byte-for-byte — stdout and exit status — against real .NET.
#
# The driver (samples/dotnet/LangVersions/Program.cs) is written as C# 9 top-level
# Five feature sections folded in from their own gates are driven from the
# TAIL of that driver rather than from inside the Cs0N section they belong to:
# IteratorSubset (C# 2), NullableValueSubset (C# 2), TupleSubset (C# 7),
# DefaultInterfaceMethodSubset (C# 8), RecordSubset (C# 9). Appending is what keeps
# the output-order change at the end of the diff; the version each covers is named
# beside its call.
#
# statements on purpose: a compilation may hold at most one such file and it
# becomes the entry point, so the driver is the only place in the bucket where the
# feature can be exercised, and it doubles as the assertion that the transpiler
# finds the synthesized `<Program>$::<Main>$`.
#
# A project has ONE LangVersion, so every section is compiled by the C# 14
# compiler. What this asserts is that the FEATURE's IL shape transpiles — not that
# a historical compiler's IL shape does. The former is what a transpiler owes; the
# latter is unreachable without shipping fourteen compilers.
#
# Two language features are NOT here because they diverge from real .NET on
# purpose, and a diff gate cannot hold a deliberate divergence:
#   - C# 4 `dynamic` (the DLR CallSite) and
#   - C# 3 `Expression.Compile()`
# are statically cut and raise a catchable PlatformNotSupportedException, the
# NativeAOT posture. That the cut is loud is asserted by the freeze gate over
# samples/dotnet/ReflectTypes/DynamicCodegenSubset.cs. Cs03 covers expression-tree
# *building*, which does transpile; Cs04 covers the rest of C# 4.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate LangVersions System.Linq System.Linq.Expressions \
    System.Collections System.Runtime System.Threading
