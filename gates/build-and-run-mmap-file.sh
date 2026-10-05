#!/usr/bin/env bash
# File-backed System.IO.MemoryMappedFiles subset lowered to the dn2cpp_mmap_*
# helpers, including MemoryMappedFile reference identity, atomic exchange and
# compare-exchange through fields and arrays, concurrent ownership transfer,
# stream/handle factories, buffered writes, source ownership, virtual stream
# accessors, and non-aligned SafeBuffer pointers,
# and disposal through aliases and
# IDisposable, including uninitialized objects. MemoryMappedFile.CreateFromFile
# opens + fstats the file; CreateViewAccessor mmaps a (page-aligned) range; the view's
# Read*/Write* typed accessors + the generic Read/Write/ReadArray/WriteArray<T> forms
# load/store the mapped bytes; the SafeMemoryMappedViewHandle exposes the raw byte*
# (AcquirePointer) that the System.Reflection.Metadata PEReader path scans. Maps
# and accessors lower to runtime objects; SafeBuffer and SafeHandle retain their
# real managed bodies and reference-counted pointer leases.
# Named maps / cross-process / CreateNew / non-null mapName /
# CreateViewStream are unsupported. Unix named file maps raise the library
# platform refusal after the source checks. Accessor bounds, factory parameters,
# library messages, view ranges and missing paths are diffed against .NET.
# File-backed factories normalize lexical dot components before opening the file.
#
# The sample takes a scratch directory as args[0]; we give the native build and real
# .NET SEPARATE fresh directories and diff their output exactly — the program prints
# only computed/observed values, never addresses or paths. MemoryMappedFile lives in
# System.IO.MemoryMappedFiles (not CoreLib), so that is referenced alongside CoreLib.
# arm64 macOS is little-endian; no cross-endian assertions. CoreLib only (no Linq shim).
source "$(dirname "$0")/_common.sh"
unset DN2CPP_BEFORE_IO_VALIDATION DN2CPP_BEFORE_MMAP_IO_PARITY

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
mmap_existing_asserts() {
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
    if ! grep -qxF 'mmap stream factories complete' <<< "$output"; then
        echo "FAIL: MemoryMappedFile stream factory block did not complete" >&2
        return 1
    fi
    local legacy_scratch legacy_output prefix
    legacy_scratch=$(mktemp -d artifacts/mmap-legacy.XXXXXX)
    legacy_output=$(run_bounded dotnet "samples/dotnet/MmapFile/bin/$CONFIG/$TFM/MmapFile.dll" "$legacy_scratch" legacy) || return $?
    rm -rf "$legacy_scratch"
    prefix=$(awk '{ print } /^mmap uninitialized complete$/ { exit }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$legacy_output")" || return $?

    # A factory elsewhere in the image must not supply reflection's interface map.
    local uninitialized corelib bcl uninitialized_native uninitialized_expected
    uninitialized=$(mktemp -d artifacts/mmap-uninitialized.XXXXXX)
    corelib=$(locate_corelib)
    bcl=$(dirname "$corelib")
    dotnet build samples/dotnet/MmapFile/MmapFile.csproj -c "$CONFIG" \
        --nologo -v q -p:DefineConstants=MMAP_UNINITIALIZED_ONLY -o "$uninitialized/app" || return $?
    invoke_cli "$uninitialized/app/MmapFile.dll" -r "$corelib" \
        -r "$bcl/System.IO.MemoryMappedFiles.dll" -o "$uninitialized/gen" || return $?
    if grep -q 'dn2cpp_mmap_create_from_file(' "$uninitialized/gen"/generated*.cpp; then
        echo "FAIL: uninitialized-only program reached a MemoryMappedFile factory" >&2
        return 1
    fi
    compile_console "$uninitialized/gen" MmapFile || return $?
    uninitialized_native=$(run_bounded "$uninitialized/gen/MmapFile") || return $?
    uninitialized_expected=$(run_bounded dotnet "$uninitialized/app/MmapFile.dll") || return $?
    uninitialized_native=$(strip_cr_win "$uninitialized_native")
    uninitialized_expected=$(strip_cr_win "$uninitialized_expected")
    assert_output "$uninitialized_native" "$uninitialized_expected" || return $?
    grep -qxF 'mmap uninitialized complete' <<< "$uninitialized_native"
}
gate_extra_asserts() {
    mmap_existing_asserts "$@"
    local output before_scratch before prefix
    output=$(strip_cr_win "$native")
    grep -qxF '== mmap validation ==' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap args complete' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap argument messages complete' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap message created file kept=False' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap view ranges complete' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap missing paths complete' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'mmap position messages end' <<< "$output" || return 1
    grep -qxF 'mmap validation complete' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    before_scratch=$(mktemp -d artifacts/io-before.XXXXXX)
    before=$(DN2CPP_BEFORE_IO_VALIDATION=1 run_bounded dotnet "samples/dotnet/MmapFile/bin/$CONFIG/$TFM/MmapFile.dll" "$before_scratch") || return $?
    rm -rf "$before_scratch"
    before=$(strip_cr_win "$before")
    prefix=$(awk '$0 == "== mmap validation ==" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
    before_scratch=$(mktemp -d artifacts/mmap-path-before.XXXXXX)
    before=$(run_bounded dotnet "$_CG_APP" "$before_scratch" before-mmap-full-path) || return $?
    rm -rf "$before_scratch"
    prefix=$(awk '$0 == "-- lexical mapped file paths --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$before")" || return $?
    grep -Fxq -- '-- lexical mapped file paths --' <<< "$output" || return 1
    grep -Fxq 'mapped lexical byte=51' <<< "$output" || return 1
    grep -Fxq -- '-- lexical mapped file paths end --' <<< "$output" || return 1
    before_scratch=$(mktemp -d artifacts/mmap-disposal-before.XXXXXX)
    before=$(run_bounded dotnet "$_CG_APP" "$before_scratch" before-mmap-disposal-fields) || return $?
    rm -rf "$before_scratch"
    prefix=$(awk '$0 == "-- mapped accessor disposal fields --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$before")" || return $?
    grep -Fxq -- '-- mapped accessor disposal fields --' <<< "$output" || return 1
    grep -Fxq 'mapped accessor disposal fields end' <<< "$output" || return 1
    for label in 'typed read' 'generic read' 'array read'; do
        grep -Fxq "closed $label object=UnmanagedMemoryAccessor" <<< "$output" || return 1
    done
    grep -Fxq 'closed flush object=MemoryMappedViewAccessor' <<< "$output" || return 1
    grep -Fxq 'closed handle read object=Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle' <<< "$output" || return 1
}
export DN2CPP_GATE_EXTRA_CONTEXT="uninitialized:MMAP_UNINITIALIZED_ONLY|before-mmap-full-path|before-mmap-disposal-fields|cli:$(_gate_cli_hash)"
corelib_diff_gate MmapFile System.IO.MemoryMappedFiles
