#!/usr/bin/env bash
# Multi-assembly pipeline: app + a separate library DLL (MiniCorlib) ->
# cross-assembly transpile -> native binary -> run.
#
# Also pins the -r half of the module-initializer policy: MiniCorlib carries a
# [ModuleInitializer] (MiniBcl.Boot) that nothing calls. A reference assembly's .cctor
# is not a root — it is pulled in by ALLOCATING its declaring type — and the `<Module>`
# pseudo-type that holds the initializer's .cctor is never allocated, so without an
# explicit cross-module root the library's initializer would silently never run. The
# app's Main asserts it did and exits non-zero if not: this gate has no stdout oracle,
# so the assertion has to live in the program.
#
# Also pins the layout closure of a canonical shared-generics owner reachable ONLY
# through a referenced-only type's base chain: MiniBcl.LayoutMid is named as a type
# token and nothing else, so it and its base LayoutBase<LayoutCtx> are emitted
# opaquely while the struct ordering still pulls the group's canonical owner
# LayoutBase<__Canon> in — and that owner gets a FULL field layout. Its field types
# pass no other closure, so they must be declared here or generated.h spells
# undeclared t_ names and the C++ compile fails on them.
source "$(dirname "$0")/_common.sh"

xasm_gate MultiAssembly MiniCorlib.dll artifacts/multiasm

echo "== canonical owner's full layout declares its field types =="
hdr=artifacts/multiasm/generated.h
if ! grep -qw "struct t_MiniBcl_LayoutBase__CnRef : Dn2CppObject" "$hdr"; then
    echo "FAIL: the repro shape no longer materializes — no full layout for" >&2
    echo "t_MiniBcl_LayoutBase__CnRef in $hdr (the assertions below assert nothing)" >&2
    exit 1
fi
for t in t_MiniBcl_LayoutBeh__CnRef t_MiniBcl_LayoutLeaf; do
    if ! grep -qw "struct $t" "$hdr"; then
        echo "FAIL: $hdr spells $t in a layout but never declares it" >&2
        exit 1
    fi
done

# The Dn2Cpp.Runtime.dll auto-reference dedupes by FILE NAME, not full
# path — so `-r` pointing at a COPY of the shim under a different path must not
# load the CLI-sibling copy on top of it. The emitted output is a function of
# the LOAD SET (the assembly registry walks every module, reached or not), so a
# double load is a latent output divergence even when tree-shaking hides it.
# Transpile-only re-run against a copied shim; the baseline transpile above
# loads app + lib + auto shim = 3 assemblies, and the explicit copy must fold
# into that same count. Deliberately outside the gate cache: a different OUT,
# no gate_cache_check — it is a cheap transpile that asserts every run.
echo "== -r on a copied Dn2Cpp.Runtime.dll dedupes by file name =="
cli_dir="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
[ -n "${DN2CPP_CLI_DLL:-}" ] && cli_dir="$(dirname "$DN2CPP_CLI_DLL")"
app="samples/dotnet/MultiAssembly/bin/$CONFIG/$TFM/MultiAssembly.dll"
lib="samples/dotnet/MultiAssembly/bin/$CONFIG/$TFM/MiniCorlib.dll"
shimcopy_dir="artifacts/multiasm-shimcopy"
rm -rf "$shimcopy_dir"
mkdir -p "$shimcopy_dir"
cp "$cli_dir/Dn2Cpp.Runtime.dll" "$shimcopy_dir/Dn2Cpp.Runtime.dll"
dedupe_out=$(invoke_cli "$app" -r "$lib" -r "$shimcopy_dir/Dn2Cpp.Runtime.dll" \
    -o "$shimcopy_dir/out")
echo "$dedupe_out"
if ! grep -q "^dn2cpp: 3 assemblies," <<<"$dedupe_out"; then
    echo "FAIL: -r $shimcopy_dir/Dn2Cpp.Runtime.dll must dedupe against the" >&2
    echo "CLI-sibling shim by file name (expected '3 assemblies' in the line above)" >&2
    exit 1
fi
