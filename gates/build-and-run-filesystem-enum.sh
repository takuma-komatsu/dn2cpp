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
    local output before_scratch before prefix line trailing_error
    output=$(strip_cr_win "$native")
    grep -qxF -- '-- System.IO path arguments --' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'nullBytesCreated=False' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'nulTarget=3' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'cwdMoved=False' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'cwdNulTruncated=True' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'cwdRestored=True' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF -- '-- System.IO path arguments end --' <<< "$output" || { echo "FAIL: IO validation witness missing" >&2; return 1; }
    grep -qxF 'cwd failure relative=DirectoryNotFoundException original=True' <<< "$output" || return 1
    grep -qxF 'cwd failure nul=DirectoryNotFoundException original=True' <<< "$output" || return 1
    before_scratch=$(mktemp -d artifacts/io-before.XXXXXX)
    before=$(DN2CPP_BEFORE_IO_VALIDATION=1 run_bounded dotnet "samples/dotnet/FileSystemEnumCore/bin/$CONFIG/$TFM/FileSystemEnumCore.dll" "$before_scratch") || return $?
    rm -rf "$before_scratch"
    before=$(strip_cr_win "$before")
    prefix=$(awk '$0 == "-- System.IO path arguments --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    before=$(run_bounded dotnet "$_CG_APP" "$before_scratch" before-managed-path) || return $?
    rm -rf "$before_scratch"
    prefix=$(awk '$0 == "-- managed UTF16 full paths --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$before")" || return $?
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    before=$(run_bounded dotnet "$_CG_APP" "$before_scratch" before-file-dot-components) || return $?
    rm -rf "$before_scratch"
    prefix=$(awk '$0 == "-- lexical file operation paths --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$before")" || return $?
    before_scratch=$(mktemp -d artifacts/path-before.XXXXXX)
    before=$(run_bounded dotnet "$_CG_APP" "$before_scratch" before-unicode-file-names) || return $?
    rm -rf "$before_scratch"
    prefix=$(awk '$0 == "-- Unicode file names --" { exit } { print }' <<< "$output")
    assert_output "$prefix" "$(strip_cr_win "$before")" || return $?
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
        grep -Fxq -- "$line" <<< "$output" \
            || { echo "FAIL: managed path witness missing: $line" >&2; return 1; }
    done
    grep -Eq '^missing high=FileNotFoundException units=.* D800 0027 002E$' <<< "$output" || return 1
    grep -Eq '^missing low=FileNotFoundException units=.* DFFF 0027 002E$' <<< "$output" || return 1
    for line in '-- Unicode file names --' 'text=text exists=True' 'bytes=2/7' \
        'directory exists=True' 'deleted=True/True' '-- Unicode file names end --'; do
        grep -Fxq -- "$line" <<< "$output" \
            || { echo "FAIL: Unicode file witness missing: $line" >&2; return 1; }
    done
    if [ "$DN2CPP_OS" != windows ]; then
        grep -Fxq 'deleted cwd file exists=False directory exists=False' <<< "$output" || return 1
    fi
}
corelib_diff_gate FileSystemEnumCore
