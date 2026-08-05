#!/usr/bin/env bash
# Full-CLI self-hosting fixpoint harness (NOT a regression gate — the
# filename is deliberately outside the `build-and-run-*.sh` glob so
# run-all-gates.sh ignores it, same treatment as the sibling selfhost-measure*.sh
# / selfhost-emit-console.sh).
#
# This drives the FULL CLI (`dn2cpp.dll` = Transpiler + Godot + Runtime, which
# statically roots the `--generate-bindings` STJ-source-gen subtree) all the way
# to a self-host FIXPOINT, in three escalating proofs:
#
#   (3) EMIT     — the managed CLI transpiles its own `dn2cpp.dll` to C++.
#   (4) BUILD    — that self-transpiled C++ compiles + links into a native binary
#                  via CMake (runtime/CMakeLists.txt, the sole build backend).
#   (5) FIXPOINT — the *native* binary re-transpiles `dn2cpp.dll` and the output
#                  is byte-identical to the managed emit from (3). f(f) = f: a
#                  natively-compiled dn2cpp reproduces the managed transpiler's
#                  output exactly, over its own whole source — console transpile
#                  path, GodotBackend codegen, and the binding-generation closure.
#
# Where selfhost-emit-console.sh only emits + syntax-checks the *console* CLI
# (no Godot) and build-and-run-godot-bindgen.sh proves the binding-gen subtree's
# runtime byte-match, this closes the loop on the entire transpiler.
#
# Pins net10.0 and references the real System.Text.Json with --auto-ref, exactly
# like selfhost-measure.sh / build-and-run-godot-bindgen.sh: the STJ source-gen
# JsonTypeInfo<T> graph otherwise resolves as external generics and aborts, and
# the 11.0-preview CoreLib shape skews the transpile.
#
# Dn2Cpp.Runtime.dll is deliberately NOT passed with -r: each side must find its
# own sibling copy through the auto-reference (AppContext.BaseDirectory), which is
# the path every real caller takes — the exported-game toolchain among them. Since
# a missing shim now fails the transpile outright, step 5 doubles as the regression
# oracle for the native binary resolving its own base directory.
#
# The same holds for the conditionally-referenced managed backends (DnZlib, DnBrotli,
# DnHttp), and for them it is not merely a fidelity point — the fixpoint cannot hold
# without it. The emitted output is a function of the LOAD SET, not of reachability:
# CppEmitter.EmitAssemblyRegistry lists every entry of _c.Modules without consulting
# reachability at all, so one extra loaded module that nothing calls still changes
# generated.cpp byte for byte. And one of them IS loaded here — the --auto-ref closure
# pulls System.IO.Compression in through Dn2Cpp.Transpiler -> System.Reflection.Metadata,
# without a single reachable method, which is precisely the condition DnZlib's injection
# keys on. The two sides have DIFFERENT base directories (managed: src/Dn2Cpp.Cli/bin/…,
# native: this $OUT), so absent the copies below the native side would load one module
# fewer than the managed side and step 5's diff would fail every time.
# All three are copied, though only DnZlib is injected today: leaving the other two out
# would make the symmetry depend on an EXTERNAL fact — the BCL's own reverse-reference
# graph — and it would break on the day an SDK update starts pulling System.Net.Http
# into the closure, on one side only. Three files make it structural.
#
# The real BCL System.Linq is referenced directly, mirroring how samples
# transpile since the transpiler closed the gap on it. Both stages (the managed
# emit and the native re-emit) get it through the same -r set below, so the
# fixpoint compares like against like.
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/selfhost-fullcli}"

echo "== 1/5 Building the full CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ]  || { echo "error: CLI not built: $CLI"   >&2; exit 1; }

echo "== 2/5 Locating the real net10.0 CoreLib + BCL + System.Text.Json =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
echo "bcl dir: $bcl"

# Reference set: dn2cpp's own DLLs (Transpiler + Godot), the real
# System.Text.Json (the --generate-bindings closure), and the real BCL
# assemblies (including the real System.Linq) dn2cpp's reachable code touches.
# Only files that exist are added, so a different runtime layout degrades
# gracefully. Dn2Cpp.Runtime.dll is absent on purpose — see the header.
refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); return 0; }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Godot.dll"
add_ref "$BIN/Dn2Cpp.DotnetModule.dll"
add_ref "$corelib"
add_ref "$bcl/System.Text.Json.dll"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.Encodings.Web System.Threading System.Threading.Tasks; do
    add_ref "$bcl/$dep.dll"
done

echo "== 3/5 Emitting the full CLI from the managed transpiler (--auto-ref) =="
rm -rf "$OUT"
mkdir -p "$OUT"
dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$OUT"
[ -f "$OUT/generated.cpp" ] || { echo "error: no generated.cpp produced" >&2; exit 1; }
tus=("$OUT"/generated*.cpp)
echo "emitted ${#tus[@]} translation unit(s), $(cat "${tus[@]}" | wc -l | tr -d ' ') C++ lines"

echo "== 4/5 Native build of the self-transpiled full CLI (CMake) =="
compile_console "$OUT" dn2cpp
[ -x "$OUT/dn2cpp" ] || { echo "error: native binary not produced: $OUT/dn2cpp" >&2; exit 1; }
# Lay the artifact out the way the distributed bundle is (bin/dn2cpp next to
# bin/Dn2Cpp.Runtime.dll and the managed backends), so the native binary
# auto-references them from its own directory in step 5 — the same resolution an
# installed toolchain performs. The backends are load-set inputs, so omitting one
# would not merely lose fidelity, it would make step 5 diff two different programs;
# see the header.
cp -f "$BIN/Dn2Cpp.Runtime.dll" "$OUT/Dn2Cpp.Runtime.dll"
cp -f "$BIN/DnZlib.dll"   "$OUT/DnZlib.dll"
cp -f "$BIN/DnBrotli.dll" "$OUT/DnBrotli.dll"
cp -f "$BIN/DnHttp.dll"   "$OUT/DnHttp.dll"
# Stamp the binary with the source tree it was built from, beside it. This is what
# lets dist/package-toolchain.sh reuse a prebuilt binary WITHOUT reusing a stale
# one: `[ -x ]` alone says a file is there, never that it describes today's `src/`
# and `runtime/`, and a bundle is exactly where that difference stops being visible
# (the exported game's compile fails on a header or a CLI flag, naming neither the
# binary nor its age). Written after the link and before the fixpoint proof, for the
# same reason step 5's failure is not a reason to leave the binary unpackaged: what
# the stamp describes is the artifact step 4 produced.
# Assign first: a src_tree_hash failure inside a command-substitution argument
# would not propagate through set -e, and the stamp must never be a lie.
srchash="$(src_tree_hash)"
printf '%s\n' "$srchash" > "$OUT/dn2cpp.src-hash"
echo "built native binary: $OUT/dn2cpp (src $(cat "$OUT/dn2cpp.src-hash"))"

echo "== 5/5 Fixpoint: native binary re-transpiles dn2cpp.dll, diff vs managed =="
native_out="$OUT/native-retranspile"
rm -rf "$native_out"; mkdir -p "$native_out"
"./$OUT/dn2cpp" "$CLI" "${refs[@]}" --auto-ref -o "$native_out"
[ -f "$native_out/generated.cpp" ] || { echo "error: native re-transpile produced no output" >&2; exit 1; }
fail=0
for f in generated.h "${tus[@]##*/}"; do
    if ! diff -q "$OUT/$f" "$native_out/$f" >/dev/null 2>&1; then
        echo "FAIL: native re-transpile differs from managed emit in $f" >&2
        head -20 >&2 <<<"$(diff "$OUT/$f" "$native_out/$f" 2>&1)"
        fail=1
    fi
done
[ "$fail" -eq 0 ] || exit 1
echo "OK: full CLI fixpoint — native binary reproduces the managed emit byte-for-byte (${#tus[@]} TUs)"
