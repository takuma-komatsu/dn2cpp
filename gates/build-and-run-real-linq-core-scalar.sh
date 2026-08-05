#!/usr/bin/env bash
# Scalar-axis variant of build-and-run-real-linq-core.sh. Builds the LinqCore
# program against the REAL System.Linq.dll with the Highway SIMD backend opted out
# (-DDN2CPP_USE_HIGHWAY=OFF, set here via SCALAR=1). Inside System.Linq the
# HW-accel gate (DN2CPP_SIMD_HW_ACCEL_LINQ) is backend-selected, so this variant
# keeps the Enumerable.Sum/Min/Max/Average software fallbacks green — the checked
# per-element loops the default (Highway) gate no longer executes — plus everything
# else LinqCore reaches (SpanHelpers and the other gated BCL paths) running against
# the scalar-emulation dn2cpp_vec_* ops instead of the Highway implementation.
#
# Diffed EXACT vs real .NET: the reductions exercised here are over integer lanes
# (bit-exact regardless of vectorization) and float lanes whose magnitudes stay in
# the exact-representable range — the same oracle as the default (Highway) gate.
# The SCALAR axis uses separate runtime/app build dirs and a -scalar output dir
# (gates/_common.sh), so it runs alongside the default gate without clobbering it.
source "$(dirname "$0")/_common.sh"

export SCALAR=1
corelib_diff_gate LinqCore \
    System.Linq System.Collections.Immutable System.Collections System.Runtime
