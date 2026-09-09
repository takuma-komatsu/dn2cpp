#!/usr/bin/env bash
# System.Reflection.Metadata PEReader / MetadataReader / BlobReader over byte
# arrays, raw mapped pointers, and FileStream, diffed against real .NET.
#
# The reader exercises these open paths into a PEReader:
#   A) File.ReadAllBytes -> ImmutableArray<byte> -> new PEReader(image)
#   B) MemoryMappedFile.CreateFromFile -> CreateViewAccessor ->
#      SafeMemoryMappedViewHandle.AcquirePointer(ref byte*) -> new PEReader(p, len)
#      — i.e. the PEReader scans the raw byte* of the file-backed memory map directly.
#   C) File.OpenRead -> new PEReader(stream) over the small fixture.
#   D) File.OpenRead -> new PEReader(stream) over System.Reflection.Metadata.dll;
#      its metadata exceeds SRM's memory-map threshold and reaches the FileStream
#      overload of MemoryMappedFile.CreateFromFile through real SRM IL.
# Through the resulting MetadataReader it enumerates the TypeDefinition and
# MethodDefinition tables (sorted full type names; method names + count), decodes
# each method signature blob via BlobReader (ReadByte + compressed-integer param
# count), reads the metadata-root version string, and reads a user string constant
# straight out of the blob heap (BlobReader.ReadUTF16). All printed facts are
# stable (counts, sorted names, the constant) — no addresses / paths / hash-order.
#
# Inputs are a fixture class library (SrmFixtureLib) built at gate time and the
# framework's SRM assembly; native and `dotnet` read identical files, so output diffs
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
corelib=$(locate_corelib)
mapped_fixture="$(dirname "$corelib")/System.Reflection.Metadata.dll"
[ -f "$mapped_fixture" ] || { echo "error: mapped fixture missing: $mapped_fixture" >&2; exit 1; }
printf -v DN2CPP_GATE_RUN_ARGS '%q %q' "$fixture" "$mapped_fixture"
export DN2CPP_GATE_RUN_ARGS
export DN2CPP_GATE_EXTRA_INPUTS="$fixture $mapped_fixture"

gate_extra_asserts() {
    local output
    output="$(strip_cr_win "$native")"
    case "$output" in
        *$'\nC(filestream) typeDefs='*$'\n  streamClosed=True\nD(mapped-filestream) typeDefs='*$'\n  streamClosed=True\nfile stream readers complete') ;;
        *) echo "FAIL: SRM FileStream read, mapping, and disposal blocks did not complete" >&2; return 1 ;;
    esac
}

corelib_diff_gate SrmReadCore System.Reflection.Metadata \
    System.Collections.Immutable System.Memory System.Runtime System.Collections \
    System.Reflection System.IO.MemoryMappedFiles
