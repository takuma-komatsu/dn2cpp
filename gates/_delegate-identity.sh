#!/usr/bin/env bash
# Selected virtual method identity survives code folding and receiver metadata trimming.
gate_virtual_delegate_prefix_asserts() {
    local out="$1" axis line
    run_bounded dotnet "$_CG_APP" > "$out/virtual-delegate-full.dotnet.raw.stdout"
    run_bounded dotnet "$_CG_APP" before-virtual-delegate-identity > "$out/virtual-delegate-prefix.dotnet.raw.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/virtual-delegate-full.native.raw.stdout"
    run_bounded "$out/ReflectInvoke$EXE_EXT" before-virtual-delegate-identity > "$out/virtual-delegate-prefix.native.raw.stdout"
    for axis in dotnet native; do
        strip_cr_win_file "$out/virtual-delegate-full.$axis.raw.stdout" > "$out/virtual-delegate-full.$axis.stdout"
        strip_cr_win_file "$out/virtual-delegate-prefix.$axis.raw.stdout" > "$out/virtual-delegate-prefix.$axis.stdout"
        awk '/^== virtual delegate selected methods ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/virtual-delegate-old.$axis.stdout"
        diff -u "$out/virtual-delegate-prefix.$axis.stdout" "$out/virtual-delegate-old.$axis.stdout"
        for line in '== virtual delegate selected methods ==' 'virtual delegate selected methods end' \
            '== runtime delegate selected methods ==' 'runtime delegate selected methods end'; do
            test "$(grep -Fxc -- "$line" "$out/virtual-delegate-full.$axis.stdout")" = 1 \
                || { echo "FAIL: delegate identity block must run once: $axis/$line" >&2; return 1; }
        done
    done
    diff -u "$out/virtual-delegate-prefix.dotnet.stdout" "$out/virtual-delegate-prefix.native.stdout"
}

delegate_identity_diff_axes() {
    local axis out native line
    for axis in default trimmed unshared; do
        out="artifacts/reflect-invoke-delegate-identity-$axis"
        local build_flags=() emit_flags=()
        case "$axis" in
            trimmed) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--trim-reflection -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
            unshared) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--no-shared-generics -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/DelegateVirtualIdentityOnly.csproj \
            -c "$CONFIG" --nologo -v q ${build_flags[@]+"${build_flags[@]}"} -o "$out/app"
        run_bounded dotnet "$out/app/DelegateVirtualIdentityOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        invoke_cli "$out/app/DelegateVirtualIdentityOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${emit_flags[@]+"${emit_flags[@]}"} -o "$out"
        if [ "$axis" = trimmed ]; then
            grep -Eq '^const Dn2CppTypeInfo ti_DelegateVirtualIdentitySubset_Child = .*DN2CPP_TF_METADATA_STRIPPED' "$out"/generated*.cpp \
                || { echo 'FAIL: delegate receiver metadata was not stripped' >&2; return 1; }
        fi
        compile_console "$out" DelegateVirtualIdentityOnly
        run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        "$(resolve_python)" gates/fold-delegate-fixture.py "$out" > "$out/folding.log"
        grep -Eq '^folded delegate methods: [1-9][0-9]*; address references: [1-9][0-9]*$' "$out/folding.log"
        compile_console "$out" DelegateVirtualIdentityOnly
        run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" > "$out/folded.raw.stdout" 2> "$out/folded.stderr"
        strip_cr_win_file "$out/folded.raw.stdout" > "$out/folded.stdout"
        diff -u "$out/clr.stdout" "$out/folded.stdout"
        strip_cr_win_file "$out/folded.stderr" > "$out/folded.normalized.stderr"
        test "$(grep -Ec '^delegate folded addresses: [1-9][0-9]*$' "$out/folded.normalized.stderr")" = 1
        for native in clr native folded; do
            for line in '== virtual delegate selected methods ==' \
                'virtual identity distinct overrides: False/True/True/7/7' \
                'virtual identity base override: True/True/False/7/7' \
                'virtual identity class interface: True/True/False/7/7' \
                'virtual identity interface aliases: True/True/False/7/7' \
                'virtual identity variant interface: True/True/False/7/7' \
                'virtual identity new slots: False/True/True/7/7' \
                'virtual identity explicit methods: False/True/True/7/7' \
                'virtual identity closed owner: False/True/True/7/7' \
                'virtual identity generic virtuals: False/True/True/7/7' \
                'virtual identity generic arguments: False/True/True/7/7' \
                'virtual identity generic same: True/True/False/7/7' \
                'virtual identity closed base override: True/True/False/7/7' \
                'virtual identity Object distinct: False/True/True/7/7' \
                'virtual identity Object override: True/True/False/7/7' \
                'virtual identity boxed methods: False/True/True/7/7' \
                'virtual identity boxed same: True/True/False/7/7' \
                'virtual identity String dispatch: True/same' \
                'virtual identity String enumerators: False/True/True/CharEnumerator/CharEnumerator' \
                'virtual identity String same: True/True/False/CharEnumerator/CharEnumerator' \
                'virtual identity String interface: True/True/False/same/same' \
                'virtual identity array dispatch: True' \
                'virtual identity array enumerators: False/True/True/True/True' \
                'virtual identity array variant: False/True/True/True/True' \
                'virtual identity array same: True/True/False/True/True' \
                'virtual delegate selected methods end'; do
                test "$(grep -Fxc -- "$line" "$out/$native.stdout")" = 1 \
                    || { echo "FAIL: delegate identity witness: $axis/$native/$line" >&2; return 1; }
            done
            if [ "$axis" = default ]; then
                for line in '== runtime delegate selected methods ==' \
                    'virtual identity reflected distinct: False/True/True/7/7' \
                    'virtual identity reflected override: True/True/False/7/7' \
                    'virtual identity runtime distinct: False/True/True/7/7' \
                    'virtual identity runtime interface: True/True/False/7/7' \
                    'virtual identity runtime same: True/True/False/7/7' \
                    'runtime delegate selected methods end'; do
                    test "$(grep -Fxc -- "$line" "$out/$native.stdout")" = 1 \
                        || { echo "FAIL: runtime delegate identity witness: $axis/$native/$line" >&2; return 1; }
                done
            fi
        done
    done
}
