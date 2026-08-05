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
# Former gates: directory-subset (Directory.CreateDirectory lowered to the
# dn2cpp_directory_create helper — recursive, idempotent, trailing separator,
# not-a-file) and file-subset (File.Exists/Delete/ReadAllText/WriteAllText/
# ReadAllBytes/WriteAllBytes lowered to the dn2cpp_file_* libc helpers: UTF-8
# with no BOM, BOM-stripping reads, catchable error paths). Both were already
# `@SCRATCH@` gates over a scratch root, which is why they land here; both are
# appended at the tail of the driver, after the enumeration sections have walked
# the fixture, and each touches only names of its own.
#
# The sample takes a scratch directory as args[0] and builds a known tree in
# it. The native build and real .NET get SEPARATE fresh mktemp directories and
# their output is diffed exactly — the program prints only booleans, counts,
# and sorted root-relative paths, never the absolute scratch path (readdir
# order is filesystem-dependent, so every listing is ordinal-sorted before
# printing). CoreLib only: the whole enumeration stack lives there.
source "$(dirname "$0")/_common.sh"

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
corelib_diff_gate FileSystemEnumCore
