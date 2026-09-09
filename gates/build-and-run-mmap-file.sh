#!/usr/bin/env bash
# File-backed System.IO.MemoryMappedFiles subset lowered to the dn2cpp_mmap_*
# helpers, including MemoryMappedFile reference identity, atomic exchange and
# compare-exchange through fields and arrays, concurrent ownership transfer,
# and disposal through aliases and
# IDisposable, including uninitialized objects. MemoryMappedFile.CreateFromFile
# opens + fstats the file; CreateViewAccessor mmaps a (page-aligned) range; the view's
# Read*/Write* typed accessors + the generic Read/Write/ReadArray/WriteArray<T> forms
# load/store the mapped bytes; the SafeMemoryMappedViewHandle exposes the raw byte*
# (AcquirePointer) that the System.Reflection.Metadata PEReader path scans. The real
# bodies are the SafeHandle/UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade we
# don't model; the members are intercepted at the call site AND excluded from
# reachability. Named maps / cross-process / CreateNew / non-null mapName /
# CreateViewStream are carve-outs (loud NotSupportedException).
#
# The sample takes a scratch directory as args[0]; we give the native build and real
# .NET SEPARATE fresh directories and diff their output exactly — the program prints
# only computed/observed values, never addresses or paths. MemoryMappedFile lives in
# System.IO.MemoryMappedFiles (not CoreLib), so that is referenced alongside CoreLib.
# arm64 macOS is little-endian; no cross-endian assertions. CoreLib only (no Linq shim).
source "$(dirname "$0")/_common.sh"

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
gate_extra_asserts() {
    local output
    output="$(strip_cr_win "$native")"
    if ! grep -qxF 'mmap reference exchange complete' <<< "$output"; then
        echo "FAIL: MemoryMappedFile reference exchange block did not complete" >&2
        return 1
    fi
    if ! grep -qxF 'mmap uninitialized complete' <<< "$output"; then
        echo "FAIL: MemoryMappedFile uninitialized block did not complete" >&2
        return 1
    fi

    # A factory elsewhere in the image must not supply reflection's interface map.
    local uninitialized corelib bcl uninitialized_native uninitialized_expected
    uninitialized=$(mktemp -d artifacts/mmap-uninitialized.XXXXXX)
    corelib=$(locate_corelib)
    bcl=$(dirname "$corelib")
    dotnet build samples/dotnet/MmapFile/MmapFile.csproj -c "$CONFIG" \
        --nologo -v q -p:DefineConstants=MMAP_UNINITIALIZED_ONLY -o "$uninitialized/app"
    invoke_cli "$uninitialized/app/MmapFile.dll" -r "$corelib" \
        -r "$bcl/System.IO.MemoryMappedFiles.dll" -o "$uninitialized/gen"
    if grep -q 'dn2cpp_mmap_create_from_file(' "$uninitialized/gen"/generated*.cpp; then
        echo "FAIL: uninitialized-only program reached a MemoryMappedFile factory" >&2
        return 1
    fi
    compile_console "$uninitialized/gen" MmapFile
    uninitialized_native=$(run_bounded "$uninitialized/gen/MmapFile")
    uninitialized_expected=$(run_bounded dotnet "$uninitialized/app/MmapFile.dll")
    uninitialized_native=$(strip_cr_win "$uninitialized_native")
    uninitialized_expected=$(strip_cr_win "$uninitialized_expected")
    assert_output "$uninitialized_native" "$uninitialized_expected"
    grep -qxF 'mmap uninitialized complete' <<< "$uninitialized_native"
}
export DN2CPP_GATE_EXTRA_CONTEXT="uninitialized:MMAP_UNINITIALIZED_ONLY|cli:$(_gate_cli_hash)"
corelib_diff_gate MmapFile System.IO.MemoryMappedFiles
