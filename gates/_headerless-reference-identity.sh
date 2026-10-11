#!/usr/bin/env bash
# Headerless typed payloads preserve identity across managed and MD array boundaries.
headerless_reference_prefix_asserts() {
    local out="$1" project="$2" before="$3" marker="$4" end="$5" axis
    run_bounded "$out/$project$EXE_EXT" > "$out/headerless-full.native.raw.stdout"
    strip_cr_win_file "$out/headerless-full.native.raw.stdout" > "$out/headerless-full.native.stdout"
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" "$before" > "$out/headerless-before.$axis.stdout"
        else
            run_bounded "$out/$project$EXE_EXT" "$before" > "$out/headerless-before.$axis.stdout"
        fi
        awk -v marker="$marker" '$0 == marker { exit } { print }' \
            "$out/headerless-full.native.stdout" > "$out/headerless-prefix.stdout"
        diff -u <(strip_cr_win_file "$out/headerless-before.$axis.stdout") \
            <(strip_cr_win_file "$out/headerless-prefix.stdout")
    done
    local line
    for line in "$marker" "$end"; do
        [ "$(grep -Fxc -- "$line" "$out/headerless-full.native.stdout")" = 1 ] \
            || { echo "FAIL: headerless identity block missing or repeated: $line" >&2; return 1; }
    done
}

headerless_managed_reference_asserts() {
    local out="$1" line
    headerless_reference_prefix_asserts "$out" ReflectInvoke before-headerless-reference-identity \
        '== headerless managed reference identity ==' 'headerless managed reference identity end'
    for line in 'current numbers identity=True:True:True:True:True' \
        'date provider identity=True:True:True:True:True' 'date provider relation=True' \
        'date numeric fallback=True' 'culture provider payload=1~5:True' \
        'number provider payload=1~5:True:True' 'text cloneable identity=True:True:True' \
        'assembly attribute identity=True:True:True' 'module attribute identity=True:True:True' \
        'provider shared identity=True' 'provider variant delegate=True' 'provider Invoke date=True' \
        'provider delegate date=True' 'provider Invoke invalid=ArgumentException' \
        'provider GetFormat numbers=True' 'provider GetFormat date=True' 'provider GetFormat other=True' \
        'provider GetInstance direct=True' 'provider ignored text=x' 'provider ignored datetime=2020-01-02'; do
        [ "$(grep -Fxc -- "$line" "$out/headerless-full.native.stdout")" = 1 ] \
            || { echo "FAIL: managed headerless witness missing: $line" >&2; return 1; }
    done
}

headerless_md_reference_asserts() {
    local out="$1" line
    headerless_reference_prefix_asserts "$out" ArrayCore before-headerless-md-identity \
        '== headerless multidimensional reference identity ==' 'headerless multidimensional reference identity end'
    for line in 'headerless md typed to reflective=True' 'headerless md typed to covariant=True' \
        'headerless md reflective to typed=ja-JP:True' 'headerless md byref to reflective=True' \
        'headerless md covariant to typed=ja-JP:True' 'headerless md reflective null=True:True' \
        'headerless text md kinds=True:True:True:True' 'headerless module md kinds=True:True:True' \
        'headerless numbers md identity=True:True:True' \
        'headerless md receiver once=True:1' 'headerless md null=NullReferenceException:2' \
        'headerless md copy to object=True' 'headerless md copy to typed=True:ja-JP' \
        'headerless rank1 copy to SZ=True:True' 'headerless rank1 copy to MD=True:True' \
        'generic md default reads=True:True' 'generic md null stores=True:True'; do
        [ "$(grep -Fxc -- "$line" "$out/headerless-full.native.stdout")" = 1 ] \
            || { echo "FAIL: MD headerless witness missing: $line" >&2; return 1; }
    done
}

headerless_format_provider_asserts() {
    local out="$1" line
    headerless_reference_prefix_asserts "$out" StringBuild before-managed-format-provider \
        '== managed format provider consumption ==' 'managed format provider consumption end'
    for line in 'provider literal=literal' 'provider text=<text>' 'provider array=text/x' \
        'provider span=text/x' 'provider composite=<text>' 'provider composite span=<text>' \
        'provider builder=text/x' 'provider datetime=2020-01-02' 'provider null format=format' \
        'provider numeric payload=1~5/2~5'; do
        [ "$(grep -Fxc -- "$line" "$out/headerless-full.native.stdout")" = 1 ] \
            || { echo "FAIL: managed formatting witness missing: $line" >&2; return 1; }
    done
}
