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
# Also pins field closure for the full canonical-owner layout floated by an opaque
# referenced-only base chain. Its by-value field requires both size and alignment.
# A shared open-generic metadata row honors typeof tokens and explicit format
# selectors from every assembly declaring that full name.
# Attribute storage policies resolve external ancestry and assembly scopes without
# adding retention roots, including when ILDiet removes unreachable code.
source "$(dirname "$0")/_common.sh"

descriptor=samples/dotnet/MultiAssembly/link.xml
app="samples/dotnet/MultiAssembly/bin/$CONFIG/$TFM/MultiAssembly.dll"
lib="samples/dotnet/MultiAssembly/bin/$CONFIG/$TFM/MiniCorlib.dll"
aliaslib="samples/dotnet/MultiAssembly/bin/$CONFIG/$TFM/MultiAssemblyAlias.dll"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $aliaslib"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS $descriptor" \
    xasm_gate MultiAssembly MiniCorlib.dll artifacts/multiasm -r "$aliaslib" --link-xml "$descriptor"

# This lane exercises C++ reflection inference without explicit retention roots.
xasm_gate MultiAssembly MiniCorlib.dll artifacts/multiasm-inference -r "$aliaslib" --no-ildiet

shared_definition_layout() {
    local out="$1" format="$2" other=record
    [ "$format" = record ] && other=native
    local symbol=refl_gendef_MetadataAssemblyCollision_Subject_1
    grep -qw "md_${format}_$symbol" "$out"/generated*.cpp \
        || { echo "FAIL: $out did not emit $format shared generic metadata" >&2; return 1; }
    if grep -qw "md_${other}_$symbol" "$out"/generated*.cpp; then
        echo "FAIL: $out emitted both formats for one shared generic definition" >&2
        return 1
    fi
}

metadata_section='metadata-assembly-begin'
expected_output=$(run_bounded dotnet "$app")
assert_output "$(sed '/^metadata-policy-assembly-begin$/,$d' <<<"$expected_output")" \
    "$(cat gates/expected/multiassembly-prefix.txt)"
expected_metadata=$(sed -n '/^metadata-assembly-begin$/,/^metadata-assembly-end$/p' <<<"$expected_output")
grep -Fxq "$metadata_section" <<<"$expected_metadata"
expected_policy=$(sed -n '/^metadata-policy-assembly-begin$/,/^metadata-policy-assembly-end$/p' <<<"$expected_output")
grep -Fxq metadata-policy-assembly-begin <<<"$expected_policy"
grep -Fxq metadata-policy-assembly-end <<<"$expected_policy"
metadata_section_parity() {
    local out="$1" actual
    run_bounded "$out/MultiAssembly$EXE_EXT" > "$out/metadata-assembly.stdout"
    actual=$(sed -n '/^metadata-assembly-begin$/,/^metadata-assembly-end$/p' "$out/metadata-assembly.stdout")
    assert_output "$actual" "$expected_metadata"
    actual=$(sed -n '/^metadata-policy-assembly-begin$/,/^metadata-policy-assembly-end$/p' "$out/metadata-assembly.stdout")
    assert_output "$actual" "$expected_policy"
    assert_output "$(cat "$out/metadata-assembly.stdout")" "$expected_output"
}

policy_layout() {
    local out="$1" symbol="$2" format="$3" other=record
    [ "$format" = record ] && other=native
    grep -qw "md_${format}_refl_$symbol" "$out"/generated*.cpp \
        || { echo "FAIL: $out did not emit $format metadata for $symbol" >&2; return 1; }
    if grep -qw "md_${other}_refl_$symbol" "$out"/generated*.cpp; then
        echo "FAIL: $out emitted the wrong metadata format for $symbol" >&2
        return 1
    fi
}
for out in artifacts/multiasm artifacts/multiasm-inference; do
    shared_definition_layout "$out" native
    metadata_section_parity "$out"
    policy_layout "$out" ti_MultiAssembly_MetadataExternalPolicy native
    policy_layout "$out" ti_MultiAssembly_MetadataDerived native
    policy_layout "$out" ti_MiniBcl_MetadataMiddle native
    policy_layout "$out" ti_MetadataCompressionCollision_UnmarkedSubject record
    policy_layout "$out" ti_MetadataCompressionCollision_MarkedSubject native
    policy_layout "$out" ti_MultiAssembly_MetadataScopeUnmarked record
    policy_layout "$out" ti_MultiAssembly_MetadataScopeMarked native
    policy_layout "$out" gendef_MetadataCompressionCollision_SharedSubject_1 native
    policy_layout "$out" ti_MetadataCompressionCollision_SharedSubject_Int32 record
    policy_layout "$out" ti_MetadataCompressionCollision_SharedSubject_String native
done
if grep -Eq 'MetadataUnreachable|MustRemainUnreachable' artifacts/multiasm/generated*.cpp artifacts/multiasm/generated.h; then
    echo "FAIL: metadata storage attributes retained unused types or members" >&2
    exit 1
fi

echo "== qualified metadata formats for an assembly-shared generic definition =="
metadata_emit_layout() {
    local name="$1" format="$2"
    shift 2
    local out="artifacts/multiasm-metadata-$name"
    invoke_cli "$app" -r "$lib" -r "$aliaslib" --no-ildiet -o "$out" "$@"
    shared_definition_layout "$out" "$format"
}
metadata_emit_layout first-packed record \
    --reflection-metadata 'MiniCorlib::MetadataAssemblyCollision.Subject`1=packed'
compile_console artifacts/multiasm-metadata-first-packed MultiAssembly
metadata_section_parity artifacts/multiasm-metadata-first-packed
metadata_emit_layout second-native native \
    --reflection-metadata 'MultiAssemblyAlias::MetadataAssemblyCollision.Subject`1=native'
metadata_emit_layout both-native native \
    --reflection-metadata 'MiniCorlib::MetadataAssemblyCollision.Subject`1=native' \
    --reflection-metadata 'MultiAssemblyAlias::MetadataAssemblyCollision.Subject`1=native'
metadata_emit_layout policy-packed native \
    --reflection-metadata 'MultiAssemblyAlias::MetadataCompressionCollision.SharedSubject`1=packed' \
    --reflection-metadata 'MultiAssembly.MetadataExternalPolicy=packed' \
    --reflection-metadata 'MiniBcl.MetadataMiddle=packed'
policy_layout artifacts/multiasm-metadata-policy-packed gendef_MetadataCompressionCollision_SharedSubject_1 record
policy_layout artifacts/multiasm-metadata-policy-packed ti_MultiAssembly_MetadataExternalPolicy record
policy_layout artifacts/multiasm-metadata-policy-packed ti_MiniBcl_MetadataMiddle record
policy_layout artifacts/multiasm-metadata-policy-packed ti_MultiAssembly_MetadataDerived native
compile_console artifacts/multiasm-metadata-policy-packed MultiAssembly
metadata_section_parity artifacts/multiasm-metadata-policy-packed

metadata_reject_layout() {
    local name="$1" diagnostic="$2" status=0
    shift 2
    local out="artifacts/multiasm-metadata-$name"
    mkdir -p "$out"
    run_bounded invoke_cli "$app" -r "$lib" -r "$aliaslib" --no-ildiet -o "$out" "$@" \
        > "$out/diagnostic.log" 2>&1 || status=$?
    if [ "$status" -ne 2 ] || ! grep -Fq -- "$diagnostic" "$out/diagnostic.log"; then
        cat "$out/diagnostic.log" >&2
        echo "FAIL: shared generic metadata policy $name was not rejected" >&2
        return 1
    fi
}
metadata_reject_layout conflicting 'Conflicting --reflection-metadata formats for shared generic definition' \
    --reflection-metadata 'MiniCorlib::MetadataAssemblyCollision.Subject`1=native' \
    --reflection-metadata 'MultiAssemblyAlias::MetadataAssemblyCollision.Subject`1=packed'
metadata_reject_layout ambiguous 'Ambiguous --reflection-metadata selector' \
    --reflection-metadata 'MetadataAssemblyCollision.Subject`1=native'

echo "== canonical owner's full layout declares its field types =="
hdr=artifacts/multiasm/generated.h
if ! grep -qw "struct t_MiniBcl_LayoutBase__CnRef : Dn2CppObject" "$hdr"; then
    echo "FAIL: the repro shape no longer materializes — no full layout for" >&2
    echo "t_MiniBcl_LayoutBase__CnRef in $hdr (the assertions below assert nothing)" >&2
    exit 1
fi
for t in t_MiniBcl_LayoutBeh__CnRef t_MiniBcl_LayoutLeaf t_MiniBcl_LayoutAligned; do
    if ! grep -qw "struct $t" "$hdr"; then
        echo "FAIL: $hdr spells $t in a layout but never declares it" >&2
        exit 1
    fi
done
grep -qw "t_MiniBcl_LayoutAligned f__aligned" "$hdr" \
    || { echo "FAIL: canonical owner does not carry the aligned field by value" >&2; exit 1; }
grep -qw "struct alignas(8) t_MiniBcl_LayoutAligned" "$hdr" \
    || { echo "FAIL: opaque aligned field shell lost its 8-byte alignment" >&2; exit 1; }

# The Dn2Cpp.Runtime.dll auto-reference dedupes by FILE NAME, not full
# path — so `-r` pointing at a COPY of the shim under a different path must not
# load the CLI-sibling copy on top of it. The emitted output is a function of
# the LOAD SET (the assembly registry walks every module, reached or not), so a
# double load is a latent output divergence even when tree-shaking hides it.
# Transpile-only re-run against a copied shim; the baseline transpile above
# loads app + both libraries + auto shim, and the explicit copy must fold
# into that same count. Deliberately outside the gate cache: a different OUT,
# no gate_cache_check — it is a cheap transpile that asserts every run.
echo "== -r on a copied Dn2Cpp.Runtime.dll dedupes by file name =="
cli_dir="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
[ -n "${DN2CPP_CLI_DLL:-}" ] && cli_dir="$(dirname "$DN2CPP_CLI_DLL")"
shimcopy_dir="artifacts/multiasm-shimcopy"
rm -rf "$shimcopy_dir"
mkdir -p "$shimcopy_dir"
cp "$cli_dir/Dn2Cpp.Runtime.dll" "$shimcopy_dir/Dn2Cpp.Runtime.dll"
dedupe_out=$(invoke_cli "$app" -r "$lib" -r "$aliaslib" -r "$shimcopy_dir/Dn2Cpp.Runtime.dll" \
    --link-xml "$descriptor" -o "$shimcopy_dir/out")
echo "$dedupe_out"
if ! grep -q "^dn2cpp: 4 assemblies," <<<"$dedupe_out"; then
    echo "FAIL: -r $shimcopy_dir/Dn2Cpp.Runtime.dll must dedupe against the" >&2
    echo "CLI-sibling shim by file name (expected '4 assemblies' in the line above)" >&2
    exit 1
fi
