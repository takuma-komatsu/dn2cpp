#!/usr/bin/env bash
# Selected virtual method identity survives code folding and receiver metadata trimming.
gate_virtual_delegate_prefix_asserts() {
    local out="$1" axis line
    gate_capture "$out/virtual-delegate-full.dotnet.raw" reflection_oracle "$_CG_APP"
    gate_output status "$(cat "$out/virtual-delegate-full.dotnet.raw.status")" 0
    if reflection_full_profile; then
        gate_capture "$out/virtual-delegate-prefix.dotnet.raw" reflection_oracle "$_CG_APP" before-virtual-delegate-identity
        gate_output status "$(cat "$out/virtual-delegate-prefix.dotnet.raw.status")" 0
    fi
    gate_capture "$out/virtual-delegate-full.native.raw" reflection_native_full "$out" ReflectInvoke
    gate_output status "$(cat "$out/virtual-delegate-full.native.raw.status")" 0
    if reflection_full_profile; then
        gate_capture "$out/virtual-delegate-prefix.native.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-virtual-delegate-identity
        gate_output status "$(cat "$out/virtual-delegate-prefix.native.raw.status")" 0
    fi
    for axis in dotnet native; do
        if reflection_full_profile; then
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" \
                "$out/virtual-delegate-prefix.$axis.raw.stdout" --policy crlf \
                --before '== virtual delegate selected methods =='
        fi
        reflection_assert_lines "$out/virtual-delegate-full.$axis.raw.stdout" 1 \
            '== virtual delegate selected methods ==' 'virtual delegate selected methods end' \
            '== runtime delegate selected methods ==' 'runtime delegate selected methods end'
    done
    if reflection_full_profile; then
        gate_output compare "$out/virtual-delegate-prefix.native.raw.stdout" "$out/virtual-delegate-prefix.dotnet.raw.stdout"
    fi
}

gate_runtime_owned_delegate_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture "$out/runtime-owned-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-runtime-owned-method-groups
                gate_output status "$(cat "$out/runtime-owned-prefix.$axis.raw.status")" 0
            else
                gate_capture "$out/runtime-owned-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-owned-method-groups
                gate_output status "$(cat "$out/runtime-owned-prefix.$axis.raw.status")" 0
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" \
                "$out/runtime-owned-prefix.$axis.raw.stdout" --policy crlf \
                --before '== runtime-owned class method groups =='
        fi
        runtime_owned_delegate_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/runtime-owned-prefix.native.raw.stdout" "$out/runtime-owned-prefix.dotnet.raw.stdout"
    fi
}

runtime_owned_delegate_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== runtime-owned class method groups ==' \
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
        'runtime-owned class method groups end'
}

delegate_object_equality_only() {
    local axis out catalog
    local axes=(default trimmed)
    if ! reflection_full_profile; then axes=(default); fi
    for axis in "${axes[@]}"; do
        out="artifacts/reflect-invoke-object-equality-only-$axis"
        local emit_flags=()
        [ "$axis" != trimmed ] || emit_flags=(--trim-reflection)
        reflection_build_once samples/dotnet/ReflectInvoke/DelegateObjectEqualityOnly.csproj "$out/app" -m:1
        gate_capture "$out/clr.raw" reflection_oracle "$out/app/DelegateObjectEqualityOnly.dll"
        gate_output status "$(cat "$out/clr.raw.status")" 0
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
        gate_capture "$out/native.raw" run_bounded "$out/DelegateObjectEqualityOnly$EXE_EXT"
        gate_output status "$(cat "$out/native.raw.status")" 0
        gate_output compare "$out/native.raw.stdout" "$out/clr.raw.stdout"
        printf '%s\n' \
            '== equality without Method ==' 'Object-values=True' \
            'Object-equality=True/True/True' 'Object-remove=True/System.Object' \
            'Override-values=True' 'Override-equality=True/True/True' 'Override-remove=True/override' \
            'base-equality=False/False' 'base-remove=True/override' \
            'Object-Equals=True/True/True/True/True' 'Object-GetHashCode=True/True/True/True' \
            'equality without Method end' > "$out/expected.stdout"
        gate_output compare "$out/native.raw.stdout" "$out/expected.stdout" --policy crlf
    done
}


delegate_identity_diff_axes() {
    local axis out native line
    local axes=(default opaque trimmed unshared)
    if ! reflection_full_profile; then axes=(trimmed); fi
    for axis in "${axes[@]}"; do
        out="artifacts/reflect-invoke-delegate-identity-$axis"
        local build_flags=() emit_flags=()
        case "$axis" in
            opaque) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(-r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
            trimmed) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--trim-reflection -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
            unshared) build_flags=(-p:DelegateIdentityIlOnly=true); emit_flags=(--no-shared-generics -r "$out/app/DelegateVirtualIdentityLibrary.dll") ;;
        esac
        reflection_build_once samples/dotnet/ReflectInvoke/DelegateVirtualIdentityOnly.csproj "$out/app" -m:1 ${build_flags[@]+"${build_flags[@]}"}
        gate_capture "$out/clr.raw" reflection_oracle "$out/app/DelegateVirtualIdentityOnly.dll"
        gate_output status "$(cat "$out/clr.raw.status")" 0
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
        gate_capture "$out/native.raw" run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT"
        gate_output status "$(cat "$out/native.raw.status")" 0
        gate_output compare "$out/native.raw.stdout" "$out/clr.raw.stdout"
        for native in clr native; do
            if [ "$native" = clr ]; then
                gate_capture "$out/type-only-prefix.$native.raw" reflection_oracle "$out/app/DelegateVirtualIdentityOnly.dll" before-type-only-exception
                gate_output status "$(cat "$out/type-only-prefix.$native.raw.status")" 0
            else
                gate_capture "$out/type-only-prefix.$native.raw" run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT" before-type-only-exception
                gate_output status "$(cat "$out/type-only-prefix.$native.raw.status")" 0
            fi
            gate_output prefix "$out/$native.raw.stdout" "$out/type-only-prefix.$native.raw.stdout" \
                --policy crlf --before '== type-only generic exception identity =='
            reflection_assert_lines "$out/$native.raw.stdout" 1 \
                '== type-only generic exception identity ==' \
                'exception type=TypeOnlyException`1' \
                'type-only generic exception identity end'
            gate_output match-record "$out/$native.raw.stdout" 'separate delegate method=.+/7' --policy crlf --count any \
                || { echo "FAIL: separate delegate Method read missing: $axis/$native" >&2; return 1; }
        done
        "$(resolve_python)" gates/fold-delegate-fixture.py "$out" > "$out/folding.log"
        grep -Eq '^folded delegate methods: [1-9][0-9]*; address references: [1-9][0-9]*$' "$out/folding.log"
        compile_console "$out" DelegateVirtualIdentityOnly
        gate_capture "$out/folded.raw" run_bounded "$out/DelegateVirtualIdentityOnly$EXE_EXT"
        gate_output status "$(cat "$out/folded.raw.status")" 0
        gate_output compare "$out/folded.raw.stdout" "$out/clr.raw.stdout"
        gate_output match-record "$out/folded.raw.stderr" 'delegate folded addresses: [1-9][0-9]*' --policy crlf --count 1
        for native in clr native folded; do
            runtime_owned_delegate_asserts "$out/$native.raw.stdout"
            reflection_assert_lines "$out/$native.raw.stdout" 1 \
                '== virtual delegate selected methods ==' \
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
                'virtual delegate selected methods end'
            if [ "$axis" = default ]; then
                reflection_assert_lines "$out/$native.raw.stdout" 1 \
                    '== runtime delegate selected methods ==' \
                    'virtual identity reflected distinct: False/True/True/7/7' \
                    'virtual identity reflected override: True/True/False/7/7' \
                    'virtual identity runtime distinct: False/True/True/7/7' \
                    'virtual identity runtime interface: True/True/False/7/7' \
                    'virtual identity runtime same: True/True/False/7/7' \
                    'runtime delegate selected methods end'
            fi
        done
    done
}
