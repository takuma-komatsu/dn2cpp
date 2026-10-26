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
    local out="$1"
    local output
    output="$out/native.stdout"
    if ! gate_output byte-witness "$output" 'mmap reference exchange complete' --count any --policy crlf; then
        echo "FAIL: MemoryMappedFile reference exchange block did not complete" >&2
        return 1
    fi
    if ! gate_output byte-witness "$output" 'mmap uninitialized complete' --count any --policy crlf; then
        echo "FAIL: MemoryMappedFile uninitialized block did not complete" >&2
        return 1
    fi
    if ! gate_output byte-witness "$output" 'mmap stream factories complete' --count any --policy crlf; then
        echo "FAIL: MemoryMappedFile stream factory block did not complete" >&2
        return 1
    fi
    local legacy_scratch legacy_output prefix
    legacy_scratch=$(mktemp -d artifacts/mmap-legacy.XXXXXX)
    gate_capture_ok "$out/assert-legacy_output-1" run_bounded dotnet "samples/dotnet/MmapFile/bin/$CONFIG/$TFM/MmapFile.dll" "$legacy_scratch" legacy
    legacy_output="$out/assert-legacy_output-1.stdout"
    rm -rf "$legacy_scratch"
    prefix="$out/legacy-prefix.stdout"
    gate_output select "$output" "$prefix" --through 'mmap uninitialized complete' --policy crlf
    gate_output compare "$prefix" "$legacy_output" || return $?

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
    gate_capture_ok "$out/assert-uninitialized_native-2" run_bounded "$uninitialized/gen/MmapFile"
    uninitialized_native="$out/assert-uninitialized_native-2.stdout"
    gate_capture_ok "$out/assert-uninitialized_expected-3" run_bounded dotnet "$uninitialized/app/MmapFile.dll"
    uninitialized_expected="$out/assert-uninitialized_expected-3.stdout"

    gate_output compare "$uninitialized_native" "$uninitialized_expected" || return $?
    gate_output byte-witness "$uninitialized_native" 'mmap uninitialized complete' --count any --policy crlf
}
gate_extra_asserts() {
    local out="$1"
    mmap_existing_asserts "$@"
    local output before_scratch before prefix
    output="$out/native.stdout"
    gate_output byte-witness "$output" '== mmap validation ==' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap args complete' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap argument messages complete' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap message created file kept=False' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap view ranges complete' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap missing paths complete' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'mmap position messages end' --count any --policy crlf || return 1
    gate_output byte-witness "$output" 'mmap validation complete' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    before_scratch=$(mktemp -d artifacts/io-before.XXXXXX)
    DN2CPP_BEFORE_IO_VALIDATION=1 gate_capture_ok "$out/assert-before-4" run_bounded dotnet "samples/dotnet/MmapFile/bin/$CONFIG/$TFM/MmapFile.dll" "$before_scratch"
    before="$out/assert-before-4.stdout"
    rm -rf "$before_scratch"

    prefix="$out/prefix-mmap-validation.stdout"

    gate_output select "$output" "$prefix" --before '== mmap validation ==' --policy crlf
    gate_output compare "$prefix" "$before"
    before_scratch=$(mktemp -d artifacts/mmap-path-before.XXXXXX)
    gate_capture_ok "$out/assert-before-5" run_bounded dotnet "$_CG_APP" "$before_scratch" before-mmap-full-path
    before="$out/assert-before-5.stdout"
    rm -rf "$before_scratch"
    prefix="$out/prefix-lexical-mapped-file-paths.stdout"
    gate_output select "$output" "$prefix" --before '-- lexical mapped file paths --' --policy crlf
    gate_output compare "$prefix" "$before" || return $?
    gate_output byte-witness "$output" '-- lexical mapped file paths --' --count any --policy crlf || return 1
    gate_output byte-witness "$output" 'mapped lexical byte=51' --count any --policy crlf || return 1
    gate_output byte-witness "$output" '-- lexical mapped file paths end --' --count any --policy crlf || return 1
    before_scratch=$(mktemp -d artifacts/mmap-disposal-before.XXXXXX)
    gate_capture_ok "$out/assert-before-6" run_bounded dotnet "$_CG_APP" "$before_scratch" before-mmap-disposal-fields
    before="$out/assert-before-6.stdout"
    rm -rf "$before_scratch"
    prefix="$out/prefix-mapped-accessor-disposal-fields.stdout"
    gate_output select "$output" "$prefix" --before '-- mapped accessor disposal fields --' --policy crlf
    gate_output compare "$prefix" "$before" || return $?
    gate_output byte-witness "$output" '-- mapped accessor disposal fields --' --count any --policy crlf || return 1
    gate_output byte-witness "$output" 'mapped accessor disposal fields end' --count any --policy crlf || return 1
    for label in 'typed read' 'generic read' 'array read'; do
        gate_output byte-witness "$output" "closed $label object=UnmanagedMemoryAccessor" --count any --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'closed flush object=MemoryMappedViewAccessor' --count any --policy crlf || return 1
    gate_output byte-witness "$output" 'closed handle read object=Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle' --count any --policy crlf || return 1
}
export DN2CPP_GATE_EXTRA_CONTEXT="uninitialized:MMAP_UNINITIALIZED_ONLY|before-mmap-full-path|before-mmap-disposal-fields|cli:$(_gate_cli_hash)"
corelib_diff_gate MmapFile System.IO.MemoryMappedFiles
