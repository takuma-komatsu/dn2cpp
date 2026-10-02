#!/usr/bin/env bash
# Constrained Boolean conversions box receivers in monomorphic and shared contexts.
set -euo pipefail
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/SharedGenerics/BooleanConvertibleOnly.csproj samples/dotnet/SharedGenerics/BooleanConvertibleOnlyProgram.cs samples/dotnet/SharedGenerics/ConstrainedObjectInterfaceSubset.cs samples/dotnet/SharedGenerics/Directory.Build.props"
_corelib_gate_core() {
    local project="$1" out="$2"
    _CG_OUT="$out"
    _CG_CORELIB=$(locate_corelib)
    _CG_EXTRA_REFERENCE_INPUTS=()
    dotnet build "samples/dotnet/SharedGenerics/$project.csproj" -c "$CONFIG" \
        --nologo -v q -o "$out/app"
    _CG_APP="$out/app/$project.dll"
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "$boolean_sharing" -o "$out"
}
gate_extra_asserts() {
    local out="$1" native prefix before line canonical body
    native=$(run_bounded "$out/BooleanConvertibleOnly$EXE_EXT")
    native=$(strip_cr_win "$native")
    for line in '== constrained Boolean conversion ==' \
        'constrained boolean=False/True' \
        'constrained boolean siblings=False/True/False/True' \
        'constrained boolean reference=False/True/False/True' \
        'constrained boolean scalar-context=Int32/False/Int32/True/Shade/False/Shade/True' \
        'constrained boolean reference-context=String/False/String/True/Object/False/Object/True' \
        'constrained Boolean conversion end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: constrained Boolean witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-boolean-receivers)
    before=$(strip_cr_win "$before")
    prefix=$(sed '/^== constrained Boolean conversion ==/,$d' <<< "$native")
    assert_output "$prefix" "$before"
    if [ "$boolean_sharing" = --shared-generics ]; then
        for canonical in CnInt32 CnRef; do
            body=$(awk -v owner="// ConstrainedObjectInterfaceSubset.BooleanReceiverContext_\$$canonical::Convert" \
                'index($0, owner) == 1 { copy = 1; next } copy { print; if ($0 == "}") exit }' \
                "$out"/generated*.cpp)
            grep -Fq 'dn2cpp_box(&dn2cpp_bool_type' <<< "$body" \
                && grep -Fq 'dn2cpp_resolve_interface' <<< "$body" \
                || { echo "FAIL: $canonical Boolean receiver lost its shared boxed dispatch" >&2; exit 1; }
            if [ "$canonical" = CnInt32 ]; then
                grep -Fq '__rgctx[' <<< "$body" \
                    || { echo 'FAIL: scalar context lost its real type-info slot' >&2; exit 1; }
            fi
        done
    fi
}
for boolean_sharing in --shared-generics --no-shared-generics; do
    DN2CPP_OUT_SUFFIX="-$CONFIG-${boolean_sharing#--}"
    DN2CPP_GATE_EXTRA_CONTEXT="sharing:$boolean_sharing"
    corelib_diff_gate BooleanConvertibleOnly
done
build_proj samples/dotnet/SharedGenerics/SharedGenerics.csproj
bucket="samples/dotnet/SharedGenerics/bin/$CONFIG/$TFM/SharedGenerics.dll"
prefix_out="artifacts/boolean-convertible-prefix-$CONFIG"
mkdir -p "$prefix_out"
run_bounded dotnet "$bucket" before-boolean-receivers > "$prefix_out/before.stdout"
run_bounded dotnet "$bucket" > "$prefix_out/full.stdout"
sed '/^== constrained Boolean conversion ==/,$d' "$prefix_out/full.stdout" > "$prefix_out/prefix.stdout"
diff -u <(strip_cr_win_file "$prefix_out/before.stdout") <(strip_cr_win_file "$prefix_out/prefix.stdout")

echo 'PASS: constrained Boolean conversion'
