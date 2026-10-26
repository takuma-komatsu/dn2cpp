#!/usr/bin/env bash
# Formal parameters preserve declaring methods, symbolic constraints and cold member contexts.
generic_definition_member_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== generic definition members ==' \
        'definition transitive relations: True/True' \
        'definition closed control: 7' \
        'definition method found: True' \
        'definition field found: True' \
        'definition property found: True' \
        'definition constructors: 2' \
        'definition method shape: True/True/True/True' \
        'definition base shape: True' \
        'definition base assignability: True/True' \
        'definition inherited found: True' \
        'definition inherited shape: True/True/True' \
        'definition bare found: True' \
        'definition generic shape: True/True/True/True' \
        'definition member types: True/True/True' \
        'definition accessors: True/True/True/True' \
        'definition special names: True/False/True' \
        'definition constants: 19/19/literal/True/True/True' \
        'definition field guards: InvalidOperationException/InvalidOperationException/InvalidOperationException/InvalidOperationException' \
        'definition invoke guards: InvalidOperationException/MemberAccessException/InvalidOperationException/InvalidOperationException' \
        'definition constructed method: True/True/True/True/True' \
        'definition constructed signature: True/True/True' \
        'definition constructed display: System.String Mixed[String](System.Collections.Generic.Dictionary`2[T,System.String[]])' \
        'definition delegate guard: ArgumentException' \
        'definition closed inherited operation: 23/29/31/True' \
        'definition private accessor: True/True' \
        'definition mixed virtual: 3/1/1' \
        'definition object inheritance: 4/True' \
        'definition inherited generic: True/True/True/True' \
        'definition filtered lookup: True/True/True/True' \
        'definition inherited selection: True/True/1' \
        'definition base method: True' \
        'definition displays: T Echo(T)/T[] Echo(T[])/T[] value' \
        'definition nested owner: 2/True/True/True/True' \
        'definition parameter member: True/True/True/True' \
        'definition default member: 1/Item' \
        'definition parallel: True' \
        'definition repeated: True' \
        'generic definition members end'
}

generic_definition_member_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/definitions-before.$axis.raw" reflection_oracle "$_CG_APP" before-generic-definition-members
            else
                gate_capture_ok "$out/definitions-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-generic-definition-members
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/definitions-before.$axis.raw.stdout" --before '== generic definition members ==' --policy crlf
        fi
        generic_definition_member_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}


open_signature_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== open generic signature types ==' \
        'open signature ArrayShape: True/True/True/True/True/True' \
        'open signature RefShape: True/True/True/True/True/True' \
        'open signature ListShape: True/True/True/True/True/True' \
        'open signature EnumerableShape: True/True/True/True/True/True' \
        'open signature FunctionShape: True/True/True/True/True/True' \
        'open signature TaskShape: True/True/True/True/True/True' \
        'open signature owner context: True/True/True' \
        'open signature arrays: True/False/1/2/True/True' \
        'open method identity: True/True/True/True/True' \
        'open method signature: True/True/True/True/True' \
        'open method nested context: True/True/True/True' \
        'open signature guards: InvalidOperationException/ArgumentException/NotSupportedException/ArgumentException' \
        'open signature validation: IndexOutOfRangeException/ArgumentNullException/ArgumentException/InvalidOperationException' \
        'open signature ref-like identity: True/True' \
        'open signature ref-like arrays: TypeLoadException/TypeLoadException/TypeLoadException' \
        'open signature shared interner: True/True' \
        'open signature constraint evidence: none/ArgumentException/none/none/none/ArgumentException' \
        'open signature array assignability: False/True/False/True/False' \
        'open signature variance unconstrained: False/False' \
        'open signature variance reference: True/True' \
        'open signature variance value: False/False' \
        'open signature variance base: True/True' \
        'open signature variance dependent: False/False' \
        'open signature foreign owner: True/True' \
        'open signature parallel: True' 'open signature repeated: True/True' \
        'open generic signature types end'
}

open_signature_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/open-before.$axis.raw" reflection_oracle "$_CG_APP" before-open-generic-signatures
            else
                gate_capture_ok "$out/open-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-open-generic-signatures
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/open-before.$axis.raw.stdout" --before '== open generic signature types ==' --policy crlf
        fi
        open_signature_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}

declaring_method_asserts() {
    local output="$1" line
    while IFS= read -r line; do
        gate_output witness "$output" "$line" --count 1 --policy crlf \
            || { echo "FAIL: declaring method witness missing or repeated: $line" >&2; return 1; }
    done <<'WITNESSES'
== formal parameter declaring methods ==
declaring method typical: True/True/True/True
declaring method contexts: True/True/True/True
declaring method positions: True/True
declaring method composition: True/True/InvalidOperationException
declaring method nested: True/True
declaring method intrinsic: True
declaring method type formals: True
declaring method ordinary guards: InvalidOperationException/InvalidOperationException/InvalidOperationException
declaring method shape guards: InvalidOperationException/InvalidOperationException/InvalidOperationException/NullReferenceException
declaring method repeated: True/True
formal parameter declaring methods end
WITNESSES
}

declaring_method_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/declaring-before.$axis.raw" reflection_oracle "$_CG_APP" before-formal-declaring-methods
            else
                gate_capture_ok "$out/declaring-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-declaring-methods
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/declaring-before.$axis.raw.stdout" --before '== formal parameter declaring methods ==' --policy crlf
        fi
        declaring_method_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}

formal_constraint_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== formal constraint metadata ==' \
        'constraint simple Unconstrained: 0/System.Object/0' \
        'constraint simple ValueConstrained: 1/System.ValueType/0' \
        'constraint simple ReferenceConstrained: 0/System.Object/0' \
        'constraint simple BaseConstrained: 1/ReflectGenericMethodSubset.DefinitionConstraintBase/0' \
        'constraint simple InterfaceConstrained: 1/System.Object/1' \
        'constraint typical owner: True/True/True/True' \
        'constraint method dependency: True/True/1' \
        'constraint composed: 2/True/True/True/True/True/True/2' \
        'constraint recursive: True/True/True' \
        'constraint repeated identity: True/True' \
        'constraint fresh arrays: True/True' \
        'constraint owner index: 2/True/True' \
        'constraint parallel identity: True/True' \
        'constraint closed identity: True/True' \
        'constraint open guards: ArgumentException/MemberAccessException/NotSupportedException' \
        'constraint nested: True/True/True/True' \
        'formal constraint metadata end'
}

formal_constraint_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/constraints-before.$axis.raw" reflection_oracle "$_CG_APP" before-formal-constraint-metadata
            else
                gate_capture_ok "$out/constraints-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-constraint-metadata
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/constraints-before.$axis.raw.stdout" --before '== formal constraint metadata ==' --policy crlf
        fi
        formal_constraint_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}

formal_constraint_diff_axes() {
    local axis out
    local axes=(packed native uncompressed trimmed unshared)
    if ! reflection_full_profile; then axes=(packed); fi
    for axis in "${axes[@]}"; do
        out="artifacts/formal-constraint-metadata-$axis"
        mkdir -p "$out"
        local flags=() build_flags=() diet_flags=(--no-ildiet)
        case "$axis" in
            packed) flags=(--reflection-metadata 'ReflectGenericMethodSubset.ConstraintOwner`1[ReflectGenericMethodSubset.ConstraintRoot]=packed' --reflection-metadata 'ReflectGenericMethodSubset.GenericMemberBase`1=packed' --reflection-metadata 'ReflectGenericMethodSubset.DeclaringMethodOwner`1=packed' --reflection-metadata 'ReflectGenericMethodSubset.DeclaringMethodOwner`1[System.Int32]=packed'); diet_flags=() ;;
            native) flags=(--reflection-metadata 'ReflectGenericMethodSubset.ConstraintOwner`1[ReflectGenericMethodSubset.ConstraintRoot]=native' --reflection-metadata 'ReflectGenericMethodSubset.GenericMemberBase`1=native' --reflection-metadata 'ReflectGenericMethodSubset.DeclaringMethodOwner`1=native' --reflection-metadata 'ReflectGenericMethodSubset.DeclaringMethodOwner`1[System.Int32]=native') ;;
            uncompressed) flags=(--no-metadata-compression) ;;
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics); build_flags=(-p:FormalConstraintsUnshared=true) ;;
        esac
        reflection_build_once samples/dotnet/ReflectInvoke/FormalConstraintsOnly.csproj "$out/app" ${build_flags[@]+"${build_flags[@]}"}
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/FormalConstraintsOnly.dll"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/FormalConstraintsOnly.dll" -r "$(locate_corelib)" \
            -r "$out/app/TrimReflectLib.dll" \
            -r "$out/app/MultiAssemblyAlias.dll" \
            ${diet_flags[@]+"${diet_flags[@]}"} ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        case "$axis" in
            packed|native)
                local representation=record
                [ "$axis" != native ] || representation=native
                grep -Eq "^(alignas\(2\) extern const uint8_t|extern const Dn2CppMethodInfo) md_${representation}_methtab_ReflectGenericMethodSubset_ConstraintOwner_ReflectGenericMethodSubset_ConstraintRoot\\[\\]" "$out"/generated_m*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppTypeReflection) md_${representation}_refl_ti_ReflectGenericMethodSubset_ConstraintOwner_ReflectGenericMethodSubset_ConstraintRoot\\[\\]" "$out"/generated_m*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppMethodInfo) md_${representation}_definition_ReflectGenericMethodSubset_GenericMemberBase_1_methods\[\]" "$out"/generated*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppMethodInfo) md_${representation}_definition_ReflectGenericMethodSubset_DeclaringMethodOwner_1_methods\[\]" "$out"/generated*.cpp
                ;;
        esac
        if grep -Eq '^struct ReflectGenericMethodSubset_GenericMemberBare' "$out/generated.h"; then
            echo 'FAIL: bare generic definition fabricated a class layout' >&2
            return 1
        fi
        compile_console "$out" FormalConstraintsOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT"
        gate_output compare "$out/clr.raw.stdout" "$out/native.raw.stdout"
        formal_constraint_asserts "$out/native.raw.stdout"
        declaring_method_asserts "$out/native.raw.stdout"
        printf 'formal parameter declaring methods end\n' > "$out/end.expected"
        gate_output suffix "$out/native.raw.stdout" "$out/end.expected" --policy crlf
        local mode
        for mode in declaring-method-cold declaring-method-reverse unloaded-method-identity unloaded-self-assignability retained-formal-members formal-interface-order constraint-scope nested-constraint nested-assignability open-generic-signatures unloaded-open-signatures generic-definition-members generic-definition-relations-cold generic-definition-relations-warm; do
            gate_capture_ok "$out/$mode.clr.raw" reflection_oracle "$out/app/FormalConstraintsOnly.dll" "$mode"
            gate_capture_ok "$out/$mode.native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT" "$mode"
            gate_output compare "$out/$mode.clr.raw.stdout" "$out/$mode.native.raw.stdout"
            if [[ "$mode" = declaring-method-* ]]; then
                declaring_method_asserts "$out/$mode.native.raw.stdout"
            elif [ "$mode" = open-generic-signatures ]; then
                open_signature_asserts "$out/$mode.native.raw.stdout"
            elif [ "$mode" = generic-definition-members ]; then
                generic_definition_member_asserts "$out/$mode.native.raw.stdout"
            elif [[ "$mode" = generic-definition-relations-* ]]; then
                gate_output witness "$out/$mode.native.raw.stdout" 'definition transitive relations: True/True' --count 1 --policy crlf
            fi
        done
        gate_capture_ok "$out/unloaded-declaring.clr.raw" reflection_oracle "$out/app/FormalConstraintsOnly.dll" unloaded-declaring-method
        gate_capture_ok "$out/unloaded-declaring.native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT" unloaded-declaring-method
        for mode in clr native; do
            gate_output witness "$out/unloaded-declaring.$mode.raw.stdout" 'unloaded declaring method before: True' --count any --policy crlf
            gate_output witness "$out/unloaded-declaring.$mode.raw.stdout" 'unloaded declaring method after: True' --count any --policy crlf
        done
        gate_output witness "$out/unloaded-declaring.clr.raw.stdout" 'unloaded declaring constraint: none' --count 2 --policy crlf
        gate_output witness "$out/unloaded-declaring.native.raw.stdout" 'unloaded declaring constraint: PlatformNotSupportedException' --count 2 --policy crlf
        gate_capture_ok "$out/unloaded-relations.native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT" unloaded-relations
        gate_output witness "$out/unloaded-relations.native.raw.stdout" 'unloaded constraint query: PlatformNotSupportedException' --count 2 --policy crlf
        gate_output witness "$out/unloaded-relations.native.raw.stdout" 'unloaded constraint base: PlatformNotSupportedException' --count any --policy crlf
        gate_output witness "$out/unloaded-relations.native.raw.stdout" 'unloaded constraint interfaces: PlatformNotSupportedException' --count any --policy crlf
        gate_capture_ok "$out/scoped-targets.clr.raw" reflection_oracle "$out/app/FormalConstraintsOnly.dll" scoped-constraint-targets
        gate_output witness "$out/scoped-targets.clr.raw.stdout" 'scoped constraint targets: False/False' --count any --policy crlf
        gate_capture_ok "$out/unloaded-scoped.native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT" unloaded-scoped-constraints
        gate_output witness "$out/unloaded-scoped.native.raw.stdout" 'unloaded scoped identity: U/V' --count any --policy crlf
        gate_output witness "$out/unloaded-scoped.native.raw.stdout" 'unloaded scoped leaf: PlatformNotSupportedException' --count 2 --policy crlf
        gate_output witness "$out/unloaded-scoped.native.raw.stdout" 'unloaded scoped generic: PlatformNotSupportedException' --count 2 --policy crlf
        gate_output witness "$out/unloaded-scoped.native.raw.stdout" 'unloaded scoped stable: True/True' --count any --policy crlf
        gate_capture_ok "$out/stripped.native.raw" run_bounded "$out/FormalConstraintsOnly$EXE_EXT" stripped-formal-constraints
        gate_output witness "$out/stripped.native.raw.stdout" 'stripped declaring method: True' --count any --policy crlf
        if [ "$axis" = trimmed ]; then
            gate_output witness "$out/stripped.native.raw.stdout" 'stripped constraint query: PlatformNotSupportedException' --count 2 --policy crlf
            gate_output witness "$out/stripped.native.raw.stdout" 'stripped constraint base: PlatformNotSupportedException' --count any --policy crlf
            gate_output witness "$out/stripped.native.raw.stdout" 'stripped constraint interfaces: PlatformNotSupportedException' --count any --policy crlf
        else
            gate_capture_ok "$out/stripped.clr.raw" reflection_oracle "$out/app/FormalConstraintsOnly.dll" stripped-formal-constraints
            gate_output compare "$out/stripped.clr.raw.stdout" "$out/stripped.native.raw.stdout"
        fi
        echo "formal constraints $axis OK"
    done
}
