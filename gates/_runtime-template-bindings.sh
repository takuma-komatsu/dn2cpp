#!/usr/bin/env bash
# Runtime-template delegates substitute exact location identities without enabling unsafe bodies or Invoke cells.
runtime_template_binding_asserts() {
    local output="$1" line
    while IFS= read -r line; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: substituted delegate witness missing or repeated: $line" >&2; return 1; }
    done <<'WITNESSES'
== substituted runtime delegate signatures ==
signature substituted ref matching => bound/bound
signature substituted ref mismatch => null/null
signature substituted pointer matching => bound/bound
signature substituted function matching => bound/bound
signature substituted function mismatch => null/null
signature substituted nested matching => bound/bound
signature substituted nested mismatch => null/null
signature substituted alias => True/True/True
signature substituted pointer calls => 256/256
signature substituted function calls => 512/768
signature substituted nested calls => 1024/1024/2
signature substituted inherited => 38/10/True
signature substituted repeated => 9/True/True/0
substituted runtime delegate signatures end
WITNESSES
}

runtime_template_binding_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-runtime-template-bindings > "$out/template-before.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-template-bindings > "$out/template-before.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/template-before.$axis.raw.stdout" > "$out/template-before.$axis.stdout"
        awk '/^== substituted runtime delegate signatures ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/template-prefix.$axis.stdout"
        diff -u "$out/template-before.$axis.stdout" "$out/template-prefix.$axis.stdout"
        runtime_template_binding_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
}

runtime_template_binding_diff_axes() {
    local axis out corelib bcl mode representation
    corelib=$(locate_corelib)
    bcl=$(dirname "$corelib")
    # Unshared MakeGenericType cannot create these absent AOT bodies; the ordinary
    # unshared reflection gate covers its closed controls separately.
    for axis in packed native uncompressed trimmed ildiet; do
        out="artifacts/runtime-template-bindings-$axis"
        mkdir -p "$out"
        local flags=() diet_flags=(--no-ildiet)
        case "$axis" in
            packed) flags=(--reflection-metadata 'ReflectDelegateSubset.SignatureGuidAlias=packed') ;;
            native) flags=(--reflection-metadata 'ReflectDelegateSubset.SignatureGuidAlias=native') ;;
            uncompressed) flags=(--no-metadata-compression) ;;
            trimmed) flags=(--trim-reflection) ;;
            ildiet) diet_flags=() ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/RuntimeTemplateBindingOnly.csproj -c "$CONFIG" --nologo -v minimal -o "$out/app" > "$out/build.log" 2>&1 \
            || { cat "$out/build.log" >&2; return 1; }
        grep -Eq '^[[:space:]]*template pointer slot fixtures written[[:space:]]*$' "$out/build.log"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/RuntimeTemplateBindingOnly.dll" -r "$corelib" \
            -r "$bcl/System.ComponentModel.TypeConverter.dll" -r "$bcl/System.Linq.dll" \
            -r "$bcl/System.Threading.Tasks.Parallel.dll" -r "$out/app/TrimReflectLib.dll" \
            ${diet_flags[@]+"${diet_flags[@]}"} ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        case "$axis" in
            packed|native)
                representation=record
                [ "$axis" != native ] || representation=native
                grep -Eq "^(alignas\(2\) extern const uint8_t|static const Dn2CppMethodInfo) md_${representation}_methtab_ReflectDelegateSubset_SignatureGuidAlias\[\]" "$out"/generated_m*.cpp
                ;;
        esac
        if grep -Eq '^extern const Dn2CppTypeInfo ti_ReflectDelegateSubset_SignatureAliasGeneric_(System_)?(Guid|Int32);' "$out/generated.h"; then
            echo 'FAIL: substituted alias owners were unexpectedly AOT instantiated' >&2
            return 1
        fi
        compile_console "$out" RuntimeTemplateBindingOnly
        for mode in default boundary invoke stripped; do
            local args=()
            [ "$mode" = default ] || args=("$mode")
            run_bounded dotnet "$out/app/RuntimeTemplateBindingOnly.dll" ${args[@]+"${args[@]}"} > "$out/$mode.clr.raw.stdout"
            run_bounded "$out/RuntimeTemplateBindingOnly$EXE_EXT" ${args[@]+"${args[@]}"} > "$out/$mode.native.raw.stdout"
            strip_cr_win_file "$out/$mode.clr.raw.stdout" > "$out/$mode.clr.stdout"
            strip_cr_win_file "$out/$mode.native.raw.stdout" > "$out/$mode.native.stdout"
            if [ "$mode" = boundary ]; then
                # Invoke marshalling and permuted-base cloning retain independent refusals.
                local runtime
                for runtime in clr native; do
                    sed -E '/^signature (opaque (return|argument) invoke|ordinary (ref|pointer) invoke|overload reordered base) =>/d' \
                        "$out/$mode.$runtime.stdout" > "$out/$mode.$runtime.parity.stdout"
                done
                diff -u "$out/$mode.clr.parity.stdout" "$out/$mode.native.parity.stdout"
                for line in 'signature opaque return invoke => NotSupportedException' \
                    'signature opaque argument invoke => NotSupportedException' \
                    'signature ordinary ref invoke => NotSupportedException' \
                    'signature ordinary pointer invoke => NotSupportedException' \
                    'signature overload reordered base => unsupported'; do
                    grep -Fxq "$line" "$out/$mode.native.stdout"
                done
            elif [ "$mode/$axis" = stripped/trimmed ]; then
                test "$(grep -Fxc 'template stripped binding => PlatformNotSupportedException' "$out/$mode.native.stdout")" = 2
                test "$(grep -Fxc 'template stripped binding => True' "$out/$mode.clr.stdout")" = 2
            else
                diff -u "$out/$mode.clr.stdout" "$out/$mode.native.stdout"
            fi
        done
        runtime_template_binding_asserts "$out/default.native.stdout"
        test "$(tail -n 1 "$out/default.native.stdout")" = 'substituted runtime delegate signatures end'
        run_bounded "$out/RuntimeTemplateBindingOnly$EXE_EXT" unsafe > "$out/unsafe.raw.stdout"
        strip_cr_win_file "$out/unsafe.raw.stdout" > "$out/unsafe.stdout"
        local line
        for line in 'signature unsafe indirect method => unsupported' \
            'signature unsafe indirect named => unsupported' \
            'signature unsafe value copy => unsupported' \
            'signature unsafe byref Invoke => NotSupportedException' \
            'signature unsafe return Invoke => NotSupportedException' \
            'signature unsafe pointer read => unsupported/unsupported' \
            'signature unsafe pointer clear => unsupported/unsupported' \
            'signature unsafe nested read => unsupported/unsupported' \
            'signature unsafe nested clear => unsupported/unsupported' \
            'signature unsafe indirect calls => 0'; do
            grep -Fxq "$line" "$out/unsafe.stdout"
        done
        if grep -Eq '^// ReflectDelegateSubset.SignatureUnsafePointer.*::(Read|Clear)' "$out"/generated*.h "$out"/generated_b*.cpp; then
            echo 'FAIL: canonical pointer reference-slot body was emitted' >&2
            return 1
        fi
        run_bounded dotnet "$out/app/RuntimeTemplateBindingOnly.dll" unsafe > "$out/unsafe.clr.raw.stdout"
        strip_cr_win_file "$out/unsafe.clr.raw.stdout" > "$out/unsafe.clr.stdout"
        for line in 'signature unsafe pointer read => bound/bound' \
            'signature unsafe pointer clear => bound/bound' \
            'signature unsafe nested read => bound/bound' \
            'signature unsafe nested clear => bound/bound'; do
            grep -Fxq "$line" "$out/unsafe.clr.stdout"
        done
        echo "runtime-template binding $axis OK"
    done
}
