#!/usr/bin/env bash
# Capstone of the low-level / unsafe-IL epic: drive the REAL
# System.Reflection.Metadata raw-byte* read core (PEReader / MetadataReader /
# BlobReader's MemoryBlock byte* scanning) over a fixed input assembly and diff
# the native build exactly against real .NET. This is the empirical proof that the
# direct-conversion route for SRM works end-to-end — not a shim, not a hand-rolled
# reader: the program references the real System.Reflection.Metadata assembly and
# the transpiler resolves + compiles its byte* scanning bodies.
#
# The reader exercises both open paths into a PEReader and asserts they agree:
#   A) File.ReadAllBytes -> ImmutableArray<byte> -> new PEReader(image)
#   B) MemoryMappedFile.CreateFromFile -> CreateViewAccessor ->
#      SafeMemoryMappedViewHandle.AcquirePointer(ref byte*) -> new PEReader(p, len)
#      — i.e. the PEReader scans the raw byte* of the file-backed memory map directly.
# Through the resulting MetadataReader it enumerates the TypeDefinition and
# MethodDefinition tables (sorted full type names; method names + count), decodes
# each method signature blob via BlobReader (ReadByte + compressed-integer param
# count), reads the metadata-root version string, and reads a user string constant
# straight out of the blob heap (BlobReader.ReadUTF16). All printed facts are
# stable (counts, sorted names, the constant) — no addresses / paths / hash-order.
#
# Input is a deterministic fixture class library (SrmFixtureLib) built at gate time;
# the native binary and `dotnet` read the identical file, so their output diffs
# exactly. SRM lives outside CoreLib, so System.Reflection.Metadata +
# System.Collections.Immutable + their dependencies are referenced alongside CoreLib.
# CoreLib only (no Linq shim). arm64 macOS is little-endian; no cross-endian asserts.
source "$(dirname "$0")/_common.sh"

# The reader parses a FIXTURE assembly at run time: build it, hand the same
# path to both sides, and fold its content into the cache key (a run input the
# transpile surface cannot see — DN2CPP_GATE_EXTRA_INPUTS exists for exactly
# this).
fixture_project=SrmFixtureLib
build_proj "samples/dotnet/$fixture_project/$fixture_project.csproj"
fixture="samples/dotnet/$fixture_project/bin/$CONFIG/$TFM/$fixture_project.dll"
[ -f "$fixture" ] || { echo "error: fixture not built: $fixture" >&2; exit 1; }
export DN2CPP_GATE_RUN_ARGS="$fixture"
export DN2CPP_GATE_EXTRA_INPUTS="$fixture"
corelib_diff_gate SrmReadCore System.Reflection.Metadata \
    System.Collections.Immutable System.Memory System.Runtime System.Collections \
    System.Reflection System.IO.MemoryMappedFiles
