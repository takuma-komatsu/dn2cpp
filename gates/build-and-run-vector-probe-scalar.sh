#!/usr/bin/env bash
# Scalar-axis variant of build-and-run-vector-probe.sh. Builds the runtime + the
# VectorProbe sample with the Highway SIMD backend opted out
# (-DDN2CPP_USE_HIGHWAY=OFF, set here via SCALAR=1), so every dn2cpp_vec_* op runs
# the portable scalar emulation (runtime/core/dn2cpp_vectors.h) instead of the
# Highway implementation (runtime/core/dn2cpp_vectors_hwy.h) the default build now
# carries.
#
# VectorProbe exercises the vec-op surface (add/sub/mul/min/max/and/or/xor/sum over
# integer lanes + equals/greater_than masks, plus direct Vector128 calls), and
# integer SIMD is bit-exact regardless of vectorization, so the output is diffed
# EXACT vs real .NET (corelib_diff_gate) — the same oracle as the default (Highway)
# vector-probe gate.
#
# The SCALAR axis uses separate runtime/app build dirs and a -scalar output dir
# (gates/_common.sh), so this runs alongside the default gate without clobbering it.
source "$(dirname "$0")/_common.sh"

export SCALAR=1
corelib_diff_gate VectorProbe
