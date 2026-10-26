#!/usr/bin/env bash
# Consolidated filesystem-enumeration gate: the real CoreLib
# FileSystemEnumerator/FileSystemEnumerable machinery over the
# SystemNative_OpenDir/ReadDir/CloseDir PAL — Directory.EnumerateFiles /
# GetFiles / EnumerateDirectories / GetDirectories (bare, pattern, recursive
# SearchOption), the DirectoryInfo instance enumerators (EnumerateFiles /
# GetFileSystemInfos, i.e. the real DirectoryInfo/FileInfo IL), the
# DirectoryInfo returned by Directory.CreateDirectory actually USED
# (Name/Exists/FullName — the regression gate for the former null-placeholder
# return, which NRE'd only at runtime), and Directory.Delete (recursive walk +
# SystemNative_RmDir) — transpiled once against the tree-shaken real CoreLib
# and diffed exactly against real .NET.
# File, Path, Directory and Environment path arguments cover empty/NUL names,
# exception parameters, bytes-before-path order and current-directory truncation.
# Managed full paths preserve UTF-16 and precede the OS file operation.
#
# The sample takes a scratch directory as args[0] and builds a known tree in
# it. The native build and real .NET get SEPARATE fresh mktemp directories and
# their output is diffed exactly — the program prints only booleans, counts,
# and sorted root-relative paths, never the absolute scratch path (readdir
# order is filesystem-dependent, so every listing is ordinal-sorted before
# printing). CoreLib only: the whole enumeration stack lives there.
source "$(dirname "$0")/_common.sh"
unset DN2CPP_BEFORE_IO_VALIDATION
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-managed-path|before-file-dot-components|before-unicode-file-names"

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
gate_extra_asserts() {
    local out="$1"
    local output before_scratch before prefix line trailing_error
    output="$out/native.stdout"
    gate_output byte-witness "$output" '-- System.IO path arguments --' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'nullBytesCreated=False' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'nulTarget=3' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'cwdMoved=False' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'cwdNulTruncated=True' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'cwdRestored=True' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" '-- System.IO path arguments end --' --count any --policy crlf || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    gate_output byte-witness "$output" 'cwd failure relative=DirectoryNotFoundException original=True' --count any --policy crlf || return 1
    gate_output byte-witness "$output" 'cwd failure nul=DirectoryNotFoundException original=True' --count any --policy crlf || return 1
    before_scratch=$(mktemp -d artifacts/io-before.XXXXXX)
    DN2CPP_BEFORE_IO_VALIDATION=1 gate_capture_ok "$out/assert-before-1" run_bounded dotnet "samples/dotnet/FileSystemEnumCore/bin/$CONFIG/$TFM/FileSystemEnumCore.dll" "$before_scratch"
    before="$out/assert-before-1.stdout"
    rm -rf "$before_scratch"

    prefix="$out/prefix-System.IO-path-arguments.stdout"

    gate_output select "$output" "$prefix" --before '-- System.IO path arguments --' --policy crlf
    gate_output compare "$prefix" "$before"
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" "$before_scratch" before-managed-path
    before="$out/assert-before-2.stdout"
    rm -rf "$before_scratch"
    prefix="$out/prefix-managed-UTF16-full-paths.stdout"
    gate_output select "$output" "$prefix" --before '-- managed UTF16 full paths --' --policy crlf
    gate_output compare "$prefix" "$before" || return $?
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" "$before_scratch" before-file-dot-components
    before="$out/assert-before-3.stdout"
    rm -rf "$before_scratch"
    prefix="$out/prefix-lexical-file-operation-paths.stdout"
    gate_output select "$output" "$prefix" --before '-- lexical file operation paths --' --policy crlf
    gate_output compare "$prefix" "$before" || return $?
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    gate_capture_ok "$out/assert-before-4" run_bounded dotnet "$_CG_APP" "$before_scratch" before-unicode-file-names
    before="$out/assert-before-4.stdout"
    rm -rf "$before_scratch"
    prefix="$out/prefix-Unicode-file-names.stdout"
    gate_output select "$output" "$prefix" --before '-- Unicode file names --' --policy crlf
    gate_output compare "$prefix" "$before" || return $?
    trailing_error=DirectoryNotFoundException
    if [ "$DN2CPP_OS" = windows ]; then
        trailing_error=IOException
    fi
    for line in '-- managed UTF16 full paths --' \
        'lexical 0 units=006D 0069 0073 0073 0069 006E 0067 D800 absoluteSame=True' \
        'lexical 1 units=006D 0069 0073 0073 0069 006E 0067 DFFF absoluteSame=True' \
        'lexical 2 units=0063 0061 0066 00E9 002D D83D DE00 absoluteSame=True' \
        'lexical dot-root=True parent-root=True' \
        '-- managed UTF16 full paths end --' \
        '-- lexical file operation paths --' \
        'dot exists=True direct=True trailing=False' 'dot text=keep' 'dot bytes=4/107' \
        'dot written=written' 'dot bytes written=2' 'dot absolute=written' \
        "dot read trailing=$trailing_error" 'dot deleted=True' \
        'dot directory exists=True' 'dot info=7' 'dot stream=119' \
        '-- lexical file operation paths end --'; do
        gate_output byte-witness "$output" "$line" --count any --policy crlf \
            || { echo "FAIL: managed path witness missing: $line" >&2; return 1; }
    done
    gate_output view "$output" "$out/path-witnesses.lf" --policy crlf
    grep -Eq '^missing high=FileNotFoundException units=.* D800 0027 002E$' "$out/path-witnesses.lf" || return 1
    grep -Eq '^missing low=FileNotFoundException units=.* DFFF 0027 002E$' "$out/path-witnesses.lf" || return 1
    for line in '-- Unicode file names --' 'text=text exists=True' 'bytes=2/7' \
        'directory exists=True' 'deleted=True/True' '-- Unicode file names end --'; do
        gate_output byte-witness "$output" "$line" --count any --policy crlf \
            || { echo "FAIL: Unicode file witness missing: $line" >&2; return 1; }
    done
    if [ "$DN2CPP_OS" != windows ]; then
        gate_output byte-witness "$output" 'deleted cwd file exists=False directory exists=False' --count any --policy crlf || return 1
    fi
}
corelib_diff_gate FileSystemEnumCore
