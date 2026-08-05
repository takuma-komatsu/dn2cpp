#!/usr/bin/env bash
# Reference-type isinst /
# castclass / catch-filter correctness, exercised polymorphically through a real PE
# read (the path native dn2cpp itself walks to load an input assembly).
#
# Two bugs this pins (both broke native dn2cpp on real samples):
#   (1) An un-emitted / External reference target folded an `isinst`/`castclass`/catch
#       to an identity match — `o is NotSupportedException` was always true, a typed
#       `catch (NotSupportedException)` became an unconditional catch-all, and `o is
#       FormatException` was true for a BadImageFormatException. Reference targets now
#       resolve to a unique per-type type-info (or the shared runtime handle for a
#       runtime-raised exception) and run a real dn2cpp_isinst.
#   (2) The runtime dn2cpp_exception_type and the emitted ti_System_Exception were two
#       distinct symbols, so `o is Exception` missed an emitted exception object. The
#       exception type-info is now unified: emitted exception subclasses base-chain to
#       the runtime handle, and isinst/catch/GetType reference the same symbol the
#       runtime stamps.
#
# It also covers the GCHandle.AddrOfPinnedObject fix that unblocked PE reading: SRM's
# ByteArrayMemoryProvider pins a byte[] held in an `object` field, so the pinned data
# address must be discovered from the runtime rep (&element[0]), not assumed `obj + 1`.
#
# PeReadProbe reads a real PE (HelloWorld.dll) through System.Reflection.Metadata's
# PEReader + GCHandle pinning + multi-byte unaligned reads, then runs a battery of
# reference-type isinst / typed-catch / catch-filter checks over the exception
# hierarchy, and finally counts the PE's TypeDefinitions. The transpiled native binary
# must match real .NET byte-for-byte (CoreLib + SRM via -r, tree-shaken). Self-contained
# addresses are never printed (only byte VALUES / counts), so the diff is exact.
source "$(dirname "$0")/_common.sh"

# The probe reads a TARGET assembly at run time: build it, hand the same path
# to both sides, and fold its content into the cache key (a run input the
# transpile surface cannot see — DN2CPP_GATE_EXTRA_INPUTS exists for exactly
# this).
build_proj samples/dotnet/HelloWorld/HelloWorld.csproj
target="samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"
export DN2CPP_GATE_RUN_ARGS="$target"
export DN2CPP_GATE_EXTRA_INPUTS="$target"
corelib_diff_gate PeReadProbe System.Linq System.Reflection.Metadata \
    System.Collections.Immutable System.Collections System.Collections.Concurrent \
    System.Runtime System.Memory System.Console System.Linq.Expressions \
    System.Reflection System.Runtime.InteropServices
