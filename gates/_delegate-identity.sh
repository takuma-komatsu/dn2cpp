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

gate_runtime_owned_delegate_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-runtime-owned-method-groups > "$out/runtime-owned-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-owned-method-groups > "$out/runtime-owned-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/runtime-owned-prefix.$axis.raw.stdout" > "$out/runtime-owned-prefix.$axis.stdout"
        awk '/^== runtime-owned class method groups ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/runtime-owned-old.$axis.stdout"
        diff -u "$out/runtime-owned-prefix.$axis.stdout" "$out/runtime-owned-old.$axis.stdout"
        runtime_owned_delegate_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/runtime-owned-prefix.dotnet.stdout" "$out/runtime-owned-prefix.native.stdout"
}

runtime_owned_delegate_asserts() {
    local output="$1" line
    for line in '== runtime-owned class method groups ==' \
        'runtime-owned binding array class interface: True/True/False' \
        'runtime-owned binding value array class interface: True/True/False' \
        'runtime-owned binding MD array class interface: True/True/False' \
        'runtime-owned array invoke: True/True/True/True' \
        'runtime-owned binding Enum Object ToString: True/True/False' \
        'runtime-owned binding Enum Object GetHashCode: True/True/False' \
        'runtime-owned binding Enum Object Equals: True/True/False' \
        'runtime-owned binding Enum provider interface: True/True/False' \
        'runtime-owned binding Enum format interface: True/True/False' \
        'runtime-owned binding Enum TypeCode interface: True/True/False' \
        'runtime-owned binding Enum CompareTo interface: True/True/False' \
        'runtime-owned Enum invoke: Friday/Friday/True/True/False/Friday/Friday/5/5/5/Int32/0' \
        'runtime-owned binding WaitHandle Close Dispose: False/True/True' \
        'runtime-owned binding WaitHandle Dispose interface: True/True/False' \
        'runtime-owned binding WaitHandle Close same: True/True/False' \
        'runtime-owned WaitHandle invoke: True' \
        'runtime-owned null binding: ArgumentException/ArgumentException/NullReferenceException' \
        'runtime-owned class method groups end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: runtime-owned binding witness: $output/$line" >&2; return 1; }
    done
}

delegate_object_equality_only() {
    local axis out catalog
    for axis in default trimmed; do
        out="artifacts/reflect-invoke-object-equality-only-$axis"
        local emit_flags=()
        [ "$axis" != trimmed ] || emit_flags=(--trim-reflection)
        dotnet build samples/dotnet/ReflectInvoke/DelegateObjectEqualityOnly.csproj \
            -c "$CONFIG" --nologo -v q -m:1 -o "$out/app"
        run_bounded dotnet "$out/app/DelegateObjectEqualityOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        invoke_cli "$out/app/DelegateObjectEqualityOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${emit_flags[@]+"${emit_flags[@]}"} -o "$out"
        if grep -Eq 'dn2cpp_delegate_get_method\(' "$out"/generated*.cpp; then
            echo 'FAIL: equality-only delegate driver reads Delegate.Method' >&2
            return 1
        fi
        catalog=$(awk '/^static const Dn2CppDelegateMethodCatalog delegate_method_catalog\[\] = / { print }' "$out"/generated*.cpp)
        grep -Fq '{ &dn2cpp_object_type,' <<< "$catalog" \
            || { echo 'FAIL: equality-only Object binding has no declaration tokens' >&2; return 1; }
        test "$(grep -oF '{ &' <<< "$catalog" | wc -l | tr -d ' ')" = 1 \
            || { echo 'FAIL: equality-only binding retained unrelated private catalogs' >&2; return 1; }
        compile_console "$out" DelegateObjectEqualityOnly
        run_bounded "$out/DelegateObjectEqualityOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        assert_output "$(cat "$out/native.stdout")" "$(printf '%s\n' \
            '== equality without Method ==' 'Object-values=True' \
            'Object-equality=True/True/True' 'Object-remove=True/System.Object' \
            'Override-values=True' 'Override-equality=True/True/True' 'Override-remove=True/override' \
            'base-equality=False/False' 'base-remove=True/override' \
            'Object-Equals=True/True/True/True/True' 'Object-GetHashCode=True/True/True/True' \
            'equality without Method end')"
    done
}

delegate_identity_diff_axes() {
    local axis out native line
    for axis in default opaque trimmed unshared; do
        out="artifacts/reflect-invoke-delegate-identity-$axis"
        local build_flags=() emit_flags=()
        case "$axis" in
            opaque) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(-r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
            trimmed) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--trim-reflection -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
            unshared) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--no-shared-generics -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/DelegateVirtualIdentityOnly.csproj \
            -c "$CONFIG" --nologo -v q -m:1 ${build_flags[@]+"${build_flags[@]}"} -o "$out/app"
        run_bounded dotnet "$out/app/DelegateVirtualIdentityOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        invoke_cli "$out/app/DelegateVirtualIdentityOnly.dll" -r "$(locate_corelib)" \
            -r "$out/app/ReflectReturnLib.dll" \
            --no-ildiet ${emit_flags[@]+"${emit_flags[@]}"} -o "$out"
        if [ "$axis" = trimmed ]; then
            # Selected declaring owners survive Method reads; inheriting receivers do not.
            grep -Eq '^const Dn2CppTypeInfo ti_DelegateVirtualIdentitySubset_InheritedChild = .*DN2CPP_TF_METADATA_STRIPPED' "$out"/generated*.cpp \
                || { echo 'FAIL: delegate receiver metadata was not stripped' >&2; return 1; }
            grep -Eq '^const Dn2CppTypeInfo ti_DelegateVirtualIdentitySubset_Child = ' "$out"/generated*.cpp \
                || { echo 'FAIL: selected delegate owner metadata was not emitted' >&2; return 1; }
            if grep -Eq '^const Dn2CppTypeInfo ti_DelegateVirtualIdentitySubset_Child = .*DN2CPP_TF_METADATA_STRIPPED' "$out"/generated*.cpp; then
                echo 'FAIL: selected delegate owner metadata was stripped' >&2
                return 1
            fi
            grep -Eq '^const Dn2CppTypeInfo ti_DelegateVirtualIdentitySubset_Program = .*DN2CPP_TF_METADATA_STRIPPED' "$out"/generated*.cpp \
                || { echo 'FAIL: unrelated delegate library owner metadata was not stripped' >&2; return 1; }
        fi
        if [ "$axis" = opaque ]; then
            grep -qw 'ti_ReflectReturnLib_TypeOnlyException_String' "$out"/generated*.cpp \
                || { echo 'FAIL: referenced-only exception type was not emitted' >&2; return 1; }
            if grep -qw 'ti_ReflectReturnLib_TypeOnlyException__CnRef' "$out"/generated*.cpp "$out/generated.h" \
                || grep -Eq 'TypeOnlyException_1_ToString_m[0-9]+\(' "$out"/generated*.cpp "$out/generated.h"; then
                echo 'FAIL: a type-only exception acquired canonical metadata or a reached override' >&2
                return 1
            fi
        fi
        compile_console "$out" DelegateVirtualIdentityOnly
        run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        for native in clr native; do
            if [ "$native" = clr ]; then
                run_bounded dotnet "$out/app/DelegateVirtualIdentityOnly.dll" before-type-only-exception > "$out/type-only-prefix.$native.raw.stdout"
            else
                run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" before-type-only-exception > "$out/type-only-prefix.$native.raw.stdout"
            fi
            strip_cr_win_file "$out/type-only-prefix.$native.raw.stdout" > "$out/type-only-prefix.$native.stdout"
            awk '/^== type-only generic exception identity ==$/ { exit } { print }' "$out/$native.stdout" > "$out/type-only-old.$native.stdout"
            diff -u "$out/type-only-prefix.$native.stdout" "$out/type-only-old.$native.stdout"
            for line in '== type-only generic exception identity ==' \
                'exception type=TypeOnlyException`1' \
                'type-only generic exception identity end'; do
                test "$(grep -Fxc -- "$line" "$out/$native.stdout")" = 1 \
                    || { echo "FAIL: type-only generic exception witness: $axis/$native/$line" >&2; return 1; }
            done
            grep -Eq '^separate delegate method=.+/7$' "$out/$native.stdout" \
                || { echo "FAIL: separate delegate Method read missing: $axis/$native" >&2; return 1; }
        done
        "$(resolve_python)" gates/fold-delegate-fixture.py "$out" > "$out/folding.log"
        grep -Eq '^folded delegate methods: [1-9][0-9]*; address references: [1-9][0-9]*$' "$out/folding.log"
        compile_console "$out" DelegateVirtualIdentityOnly
        run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" > "$out/folded.raw.stdout" 2> "$out/folded.stderr"
        strip_cr_win_file "$out/folded.raw.stdout" > "$out/folded.stdout"
        diff -u "$out/clr.stdout" "$out/folded.stdout"
        strip_cr_win_file "$out/folded.stderr" > "$out/folded.normalized.stderr"
        test "$(grep -Ec '^delegate folded addresses: [1-9][0-9]*$' "$out/folded.normalized.stderr")" = 1
        for native in clr native folded; do
            runtime_owned_delegate_asserts "$out/$native.stdout"
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
