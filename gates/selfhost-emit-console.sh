#!/usr/bin/env bash
# Native-build self-hosting harness (NOT a regression gate — the filename is
# deliberately outside the `build-and-run-*.sh` glob so run-all-gates.sh ignores
# it, same treatment as the sibling selfhost-measure*.sh).
#
# Where `selfhost-measure-console.sh` runs the *console* CLI in --measure mode
# (which never compiles any C++ — it only checks each reachable method body's
# MethodCompiler.Compile() does not throw), this harness does the **real
# emission** (no --measure) and then proves the generated C++ is actually a valid
# translation unit: it runs
#
#     clang++ -std=c++17 -fsyntax-only -ferror-limit=0 -I runtime/core generated.cpp
#
# and asserts 0 errors. This catches codegen-correctness bugs that --measure
# cannot see (undeclared symbols, type mismatches, malformed expressions).
# -fsyntax-only (no codegen/link), so it is fast and
# needs no runtime object. generated.cpp goes to artifacts/ (gitignored).
#
# Reference set mirrors the full-CLI selfhost-emit.sh: the console CLI's own DLLs
# (Transpiler + Runtime — NO Godot) plus the real CoreLib/BCL a console transpile
# touches (including the real System.Linq), pinned to net10.0
# (resolve_net10_corelib) and closed transitively with --auto-ref. The net10 pin
# avoids the 11.0-preview CoreLib version skew (ResolveMemberRefField "Sequence
# contains no matching element"; see resolve_net10_corelib in _common.sh).
# --auto-ref turns on Compilation.LoadReferenceClosure, which pulls
# System.Security.Cryptography.dll out of the shared framework (the Transpiler
# references it) so the SHA1-based public-key-token derivation in
# CppEmitter.PublicKeyTokenOf reaches the real crypto IL instead of aborting at
# reachability with "SHA1::HashData has no intrinsic mapping yet". The
# closure stays Godot-free: the console CLI references no Godot, so nothing in the
# transitive walk pulls it in.
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/selfhost-emit-console}"

echo "== 1/4 Building the console CLI =="
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
BIN="src/Dn2Cpp.Cli.Console/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp-console.dll"
[ -f "$CLI" ]  || { echo "error: console CLI not built: $CLI" >&2; exit 1; }

echo "== 2/4 Locating the real net10.0 CoreLib + BCL =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
echo "bcl dir: $bcl"

refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); return 0; }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Runtime.dll"
add_ref "$corelib"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.RegularExpressions System.Collections.Concurrent; do
    add_ref "$bcl/$dep.dll"
done

echo "== 3/4 Emitting dn2cpp-console itself (real emission, tree-shaken, Godot-free) =="
rm -rf "$OUT"
mkdir -p "$OUT"
dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$OUT"
[ -f "$OUT/generated.cpp" ] || { echo "error: no generated.cpp produced" >&2; exit 1; }

# The self-transpiled transpiler is large, so the emitter splits it into
# generated.cpp + generated_N.cpp (all sharing generated.h); syntax-check every TU.
tus=("$OUT"/generated*.cpp)
echo "== 4/4 clang -fsyntax-only on the self-transpiled translation units (${#tus[@]} TUs) =="
errlog="$OUT/syntax-errors.txt"
set +e
clang++ -std=c++17 -fsyntax-only -ferror-limit=0 -I runtime/core -I "$OUT" "${tus[@]}" 2>"$errlog"
rc=$?
set -e
n=$(grep -c "error:" "$errlog" || true)
if [ "$rc" -ne 0 ] || [ "$n" -ne 0 ]; then
    echo "FAIL: self-transpiled translation units do not pass -fsyntax-only ($n errors):" >&2
    head -50 >&2 <<<"$(grep "error:" "$errlog")"
    exit 1
fi
echo "OK: ${#tus[@]} translation unit(s) are -fsyntax-only clean (0 errors)"
