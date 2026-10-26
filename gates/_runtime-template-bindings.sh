#!/usr/bin/env bash
# Runtime-template delegates substitute exact location identities without enabling unsafe bodies or Invoke cells.
runtime_template_binding_asserts() {
    local output="$1" line
    while IFS= read -r line; do
        gate_output witness "$output" "$line" --count 1 --policy crlf \
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
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/template-before.$axis.raw" reflection_oracle "$_CG_APP" before-runtime-template-bindings
            else
                gate_capture_ok "$out/template-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-template-bindings
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/template-before.$axis.raw.stdout" --before '== substituted runtime delegate signatures ==' --policy crlf
        fi
        runtime_template_binding_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/base-before.$axis.raw" reflection_oracle "$_CG_APP" before-runtime-base-projections
            else
                gate_capture_ok "$out/base-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-base-projections
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/base-before.$axis.raw.stdout" --before '== runtime generic base projections ==' --policy crlf
        fi
        runtime_base_projection_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}

runtime_base_projection_asserts() {
    local output="$1" line
    while IFS= read -r line; do
        gate_output witness "$output" "$line" --count 1 --policy crlf \
            || { echo "FAIL: runtime base projection witness missing or repeated: $line" >&2; return 1; }
    done <<'WITNESSES'
== runtime generic base projections ==
projection swapped => Int32/Guid/Guid
projection AOT base => True/True/True
projection generic virtual => Guid/True/Guid
projection standalone => Guid/String/True
projection chained => Guid/String/String/String
projection local arguments => String/Guid/True/True
projection interning => True/True/True
projection inherited overload => 37/11/True
projection constructor layouts => Guid/Guid/True/True
projection constructor source => String/True
projection body layouts => String/String/True/True
projection null Forward/String => Guid
projection null Again/String => Guid
projection null Forward/Guid => NullReferenceException
projection null Again/Guid => NullReferenceException
runtime generic base projections end
WITNESSES
}

runtime_template_binding_diff_axes() {
    local axis out corelib bcl mode representation
    corelib=$(locate_corelib)
    bcl=$(dirname "$corelib")
    # Unshared MakeGenericType cannot create these absent AOT bodies; the ordinary
    # unshared reflection gate covers its closed controls separately. The helpers that
    # receive a runtime-made owner as a parameter reach their clone bodies only through
    # keep-runtime-templates.xml.
    local axes=(packed native uncompressed trimmed ildiet)
    if ! reflection_full_profile; then axes=(ildiet); fi
    for axis in "${axes[@]}"; do
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
        reflection_build_once samples/dotnet/ReflectInvoke/RuntimeTemplateBindingOnly.csproj "$out/app" -v minimal > "$out/build.log" 2>&1 \
            || { cat "$out/build.log" >&2; return 1; }
        grep -Eq '^[[:space:]]*template pointer slot fixtures written[[:space:]]*$' "$out/build.log"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/RuntimeTemplateBindingOnly.dll" -r "$corelib" \
            -r "$bcl/System.ComponentModel.TypeConverter.dll" -r "$bcl/System.Linq.dll" \
            -r "$bcl/System.Threading.Tasks.Parallel.dll" -r "$out/app/TrimReflectLib.dll" \
            --link-xml samples/dotnet/ReflectInvoke/keep-runtime-templates.xml \
            ${diet_flags[@]+"${diet_flags[@]}"} ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        case "$axis" in
            packed|native)
                representation=record
                [ "$axis" != native ] || representation=native
                grep -Eq "^(alignas\(2\) extern const uint8_t|extern const Dn2CppMethodInfo) md_${representation}_methtab_ReflectDelegateSubset_SignatureGuidAlias\[\]" "$out"/generated_m*.cpp
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
            gate_capture_ok "$out/$mode.clr.raw" reflection_oracle "$out/app/RuntimeTemplateBindingOnly.dll" ${args[@]+"${args[@]}"}
            gate_capture_ok "$out/$mode.native.raw" run_bounded "$out/RuntimeTemplateBindingOnly$EXE_EXT" ${args[@]+"${args[@]}"}
            if [ "$mode" = boundary ]; then
                # Invoke marshalling retains independent refusals.
                local runtime
                for runtime in clr native; do
                    gate_output omit-prefix "$out/$mode.$runtime.raw.stdout" "$out/$mode.$runtime.parity.stdout" \
                        --prefix 'signature opaque return invoke =>' --prefix 'signature opaque argument invoke =>' \
                        --prefix 'signature ordinary ref invoke =>' --prefix 'signature ordinary pointer invoke =>' \
                        --count 1 --policy crlf
                done
                gate_output compare "$out/$mode.clr.parity.stdout" "$out/$mode.native.parity.stdout"
                reflection_assert_lines "$out/$mode.native.raw.stdout" any \
                    'signature opaque return invoke => NotSupportedException' \
                    'signature opaque argument invoke => NotSupportedException' \
                    'signature ordinary ref invoke => NotSupportedException' \
                    'signature ordinary pointer invoke => NotSupportedException' \
                    'signature overload reordered base => bound'
            elif [ "$mode/$axis" = stripped/trimmed ]; then
                gate_output witness "$out/$mode.native.raw.stdout" 'template stripped binding => PlatformNotSupportedException' --count 2 --policy crlf
                gate_output witness "$out/$mode.clr.raw.stdout" 'template stripped binding => True' --count 2 --policy crlf
            else
                gate_output compare "$out/$mode.clr.raw.stdout" "$out/$mode.native.raw.stdout"
            fi
        done
        runtime_template_binding_asserts "$out/default.native.raw.stdout"
        runtime_base_projection_asserts "$out/default.native.raw.stdout"
        printf 'runtime generic base projections end\n' > "$out/end.expected"
        gate_output suffix "$out/default.native.raw.stdout" "$out/end.expected" --policy crlf
        for mode in clr native; do
            if [ "$mode" = clr ]; then
                gate_capture_ok "$out/base-before.$mode.raw" reflection_oracle "$out/app/RuntimeTemplateBindingOnly.dll" before-base-projections
            else
                gate_capture_ok "$out/base-before.$mode.raw" run_bounded "$out/RuntimeTemplateBindingOnly$EXE_EXT" before-base-projections
            fi
            gate_output prefix "$out/default.$mode.raw.stdout" "$out/base-before.$mode.raw.stdout" --before '== runtime generic base projections ==' --policy crlf
        done
        gate_capture_ok "$out/unsafe.raw" run_bounded "$out/RuntimeTemplateBindingOnly$EXE_EXT" unsafe
        local line
        reflection_assert_lines "$out/unsafe.raw.stdout" any \
            'signature unsafe indirect method => unsupported' \
            'signature unsafe indirect named => unsupported' \
            'signature unsafe value copy => unsupported' \
            'signature unsafe byref Invoke => NotSupportedException' \
            'signature unsafe return Invoke => NotSupportedException' \
            'signature unsafe pointer read => unsupported/unsupported' \
            'signature unsafe pointer clear => unsupported/unsupported' \
            'signature unsafe nested read => unsupported/unsupported' \
            'signature unsafe nested clear => unsupported/unsupported' \
            'signature unsafe indirect calls => 0'
        if grep -Eq '^// ReflectDelegateSubset.SignatureUnsafePointer.*::(Read|Clear)' "$out"/generated*.h "$out"/generated_b*.cpp; then
            echo 'FAIL: canonical pointer reference-slot body was emitted' >&2
            return 1
        fi
        gate_capture_ok "$out/unsafe.clr.raw" reflection_oracle "$out/app/RuntimeTemplateBindingOnly.dll" unsafe
        reflection_assert_lines "$out/unsafe.clr.raw.stdout" any \
            'signature unsafe pointer read => bound/bound' \
            'signature unsafe pointer clear => bound/bound' \
            'signature unsafe nested read => bound/bound' \
            'signature unsafe nested clear => bound/bound'
        reflection_expected_lines=()
        for line in 'ProjectionRepeated`2' 'ProjectionConstant`2' 'ProjectionConstructed`2'; do
            reflection_expected_lines+=("projection unsupported $line => unsupported")
        done
        reflection_assert_lines "$out/unsafe.raw.stdout" any "${reflection_expected_lines[@]}"
        reflection_expected_lines=()
        for line in 'ProjectionRepeated`2' 'ProjectionConstant`2' 'ProjectionConstructed`2'; do
            reflection_expected_lines+=("projection unsupported $line => created")
        done
        reflection_assert_lines "$out/unsafe.clr.raw.stdout" any "${reflection_expected_lines[@]}"
        echo "runtime-template binding $axis OK"
    done
}
