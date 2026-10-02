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
#
# The sample takes a scratch directory as args[0] and builds a known tree in
# it. The native build and real .NET get SEPARATE fresh mktemp directories and
# their output is diffed exactly — the program prints only booleans, counts,
# and sorted root-relative paths, never the absolute scratch path (readdir
# order is filesystem-dependent, so every listing is ordinal-sorted before
# printing). CoreLib only: the whole enumeration stack lives there.
source "$(dirname "$0")/_common.sh"
unset DN2CPP_BEFORE_IO_VALIDATION

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
gate_extra_asserts() {
    local output before_scratch before prefix
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
}
corelib_diff_gate FileSystemEnumCore
