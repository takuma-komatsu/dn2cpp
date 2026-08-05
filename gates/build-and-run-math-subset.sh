#!/usr/bin/env bash
# Math bucket: one themed program consolidating the System.Math coverage —
# Clamp/Sign plus the integral Min/Max/Clamp/Abs overloads across every width
# and signedness (catches the unsigned precision loss above 2^53 that a
# double-routed lowering would miss), the trig/log completion (Cbrt,
# Asin/Acos/Atan, Sinh/Cosh/Tanh, Log2, Log(x, base)) and the rounding
# completion (Truncate, Log10, Round(value, MidpointRounding) and the
# digits overloads Round(value, digits[, mode]) with their exception paths),
# the former two asserted by equality so the output is formatting-independent,
# the inverse
# hyperbolics (Acosh/Asinh/Atanh, boolean anchors — transcendental values are
# never printed raw), the IEEE-exact members (CopySign, IEEERemainder,
# FusedMultiplyAdd, ScaleB, BitIncrement/BitDecrement, ILogB, Max/MinMagnitude)
# and BigMul across all six overloads (the 128-bit results via their halves),
# plus the Reciprocal(Sqrt)Estimate pair (tolerance relations + the exact IEEE
# special cases only — the finite results are platform-dependent estimates)
# and the decimal members (Round across all five MidpointRounding modes with
# their scale behavior, Clamp with its min > max throw), plus the systematic
# MathF sweep (the whole single-precision surface: exact prints for the
# IEEE-exact members, float-vs-double divergence traps like Round(2.675f, 2)
# and the FusedMultiplyAdd cancellations, boolean/tolerance anchors for the
# transcendentals, and the float Clamp/Round exception paths), plus the
# System.Double/System.Single static generic-math surface (the shared-map
# pass-throughs, the exp/log compositions and the *Pi family — exact prints,
# the compositions bit-match real .NET on this host — Lerp and
# MultiplyAddEstimate, Ieee754Remainder, the NaN-dropping *Number and
# platform-defined *Native min-max variants, Pi/E/Tau, and the full IsXxx
# predicate truth table over the special values), plus the *Pi trig family
# proper (SinPi/CosPi/TanPi/SinCosPi), RootN and Hypot — verbatim
# contraction-free runtime ports of the BCL's managed AOCL algorithms,
# asserted with exact prints across the reduction intervals, small-argument
# tiers, integral-boundary parities and the Hypot rescale branches, plus the
# System.Half basic surface (creation/conversion/arithmetic/comparison/
# predicates through the transpiled BCL software bit algorithms, ToString and
# Parse/TryParse through the transpiled Number.FormatFloat/TryParseFloat
# subtree, CompareTo/Equals/GetHashCode ordering, boxing, struct fields, and
# the binary16 edges: subnormals, overflow-to-infinity, round-to-nearest-even
# — bit patterns asserted exactly via BitConverter.HalfToUInt16Bits), plus the
# System.Half static generic-math surface (the whole INumber /
# IFloatingPointIeee754 / ITrigonometricFunctions / ... static member set:
# min-max/clamp families with their NaN and ±0 semantics, roots/powers,
# trig + the *Pi family, hyperbolics, exp/log compositions, rounding, the
# IEEE-exact members, Lerp/degrees, the Create*/checked-narrowing conversions
# and the IsXxx predicates — almost all (Half)MathF.Xxx((float)x) compositions
# in the BCL, asserted bit-exactly; the platform-defined estimates and *Native
# variants keep tolerance/special-case anchors — and constrained-generic
# T.Abs/T.Sin/... dispatch with T = Half through the static-abstract
# interfaces).
# One section file per area in samples/dotnet/MathSubset, driven in order by
# Program.cs.
# Driven by the real System.Private.CoreLib (passed with -r) -> cross-assembly
# resolve + tree-shake -> native binary -> run, diffed exactly against real
# .NET (the program is deterministic, so `dotnet $app` is its own oracle).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate MathSubset
