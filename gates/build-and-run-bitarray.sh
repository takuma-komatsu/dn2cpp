#!/usr/bin/env bash
# Regression gate for System.Collections.BitArray's And / Or / Xor / Not against the
# real tree-shaken CoreLib. In the default (Highway) build the BitArray SIMD fast
# path's HW-accel gate folds to true, so the vectorized path runs; the result bits
# are diffed EXACT vs real .NET. The scalar-axis twin (build-and-run-bitarray-scalar.sh)
# runs the same program with the scalar emulation. See samples/dotnet/BitArrayProbe.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate BitArrayProbe System.Collections
