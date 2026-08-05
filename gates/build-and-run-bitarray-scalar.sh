#!/usr/bin/env bash
# Scalar-axis variant of build-and-run-bitarray.sh. Runs the BitArrayProbe program
# with the Highway SIMD backend opted out (-DDN2CPP_USE_HIGHWAY=OFF, set here via
# SCALAR=1). The default build now carries Highway, so the base gate covers the
# vectorized fast path (the ISimdVector static-abstract dispatch lowered to
# dn2cpp_vec_and / _or / _xor / _ones_complement); this variant keeps the scalar
# emulation (runtime/core/dn2cpp_vectors.h with the HW-accel gate folded to 0)
# green — BitArray's And / Or / Xor / Not run the scalar int loop. Bitwise results
# are bit-identical regardless of vectorization, so the output is diffed EXACT vs
# real .NET. The SCALAR axis uses separate runtime/app build dirs and a -scalar
# output dir (gates/_common.sh).
source "$(dirname "$0")/_common.sh"

export SCALAR=1
corelib_diff_gate BitArrayProbe System.Collections
