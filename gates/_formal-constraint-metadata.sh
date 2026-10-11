#!/usr/bin/env bash
# Formal constraints and definition members preserve symbolic contexts and cold publication.
generic_definition_member_asserts() {
    local output="$1" line
    for line in '== generic definition members ==' \
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
        'generic definition members end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: generic definition member witness missing or repeated: $line" >&2; return 1; }
    done
}

generic_definition_member_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-generic-definition-members > "$out/definitions-before.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-generic-definition-members > "$out/definitions-before.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/definitions-before.$axis.raw.stdout" > "$out/definitions-before.$axis.stdout"
        awk '/^== generic definition members ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/definitions-prefix.$axis.stdout"
        diff -u "$out/definitions-before.$axis.stdout" "$out/definitions-prefix.$axis.stdout"
        generic_definition_member_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
}


open_signature_asserts() {
    local output="$1" line
    for line in '== open generic signature types ==' \
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
        'open generic signature types end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: open signature witness missing or repeated: $line" >&2; return 1; }
    done
}

open_signature_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-open-generic-signatures > "$out/open-before.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-open-generic-signatures > "$out/open-before.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/open-before.$axis.raw.stdout" > "$out/open-before.$axis.stdout"
        awk '/^== open generic signature types ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/open-prefix.$axis.stdout"
        diff -u "$out/open-before.$axis.stdout" "$out/open-prefix.$axis.stdout"
        open_signature_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
}

formal_constraint_asserts() {
    local output="$1" line
    for line in '== formal constraint metadata ==' \
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
        'formal constraint metadata end'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: formal constraint witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

formal_constraint_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-formal-constraint-metadata > "$out/constraints-before.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-formal-constraint-metadata > "$out/constraints-before.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/constraints-before.$axis.raw.stdout" > "$out/constraints-before.$axis.stdout"
        awk '/^== formal constraint metadata ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/constraints-prefix.$axis.stdout"
        diff -u "$out/constraints-before.$axis.stdout" "$out/constraints-prefix.$axis.stdout"
        formal_constraint_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
}

formal_constraint_diff_axes() {
    local axis out
    for axis in packed native uncompressed trimmed unshared; do
        out="artifacts/formal-constraint-metadata-$axis"
        mkdir -p "$out"
        local flags=() build_flags=() diet_flags=(--no-ildiet)
        case "$axis" in
            packed) flags=(--reflection-metadata 'ReflectGenericMethodSubset.ConstraintOwner`1[ReflectGenericMethodSubset.ConstraintRoot]=packed' --reflection-metadata 'ReflectGenericMethodSubset.GenericMemberBase`1=packed'); diet_flags=() ;;
            native) flags=(--reflection-metadata 'ReflectGenericMethodSubset.ConstraintOwner`1[ReflectGenericMethodSubset.ConstraintRoot]=native' --reflection-metadata 'ReflectGenericMethodSubset.GenericMemberBase`1=native') ;;
            uncompressed) flags=(--no-metadata-compression) ;;
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics); build_flags=(-p:FormalConstraintsUnshared=true) ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/FormalConstraintsOnly.csproj -c "$CONFIG" --nologo -v q ${build_flags[@]+"${build_flags[@]}"} -o "$out/app"
        run_bounded dotnet "$out/app/FormalConstraintsOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/FormalConstraintsOnly.dll" -r "$(locate_corelib)" \
            -r "$out/app/TrimReflectLib.dll" \
            -r "$out/app/MultiAssemblyAlias.dll" \
            ${diet_flags[@]+"${diet_flags[@]}"} ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        case "$axis" in
            packed|native)
                local representation=record
                [ "$axis" != native ] || representation=native
                grep -Eq "^(alignas\(2\) extern const uint8_t|static const Dn2CppMethodInfo) md_${representation}_methtab_ReflectGenericMethodSubset_ConstraintOwner_ReflectGenericMethodSubset_ConstraintRoot\\[\\]" "$out"/generated_m*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppTypeReflection) md_${representation}_refl_ti_ReflectGenericMethodSubset_ConstraintOwner_ReflectGenericMethodSubset_ConstraintRoot\\[\\]" "$out"/generated_m*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppMethodInfo) md_${representation}_definition_ReflectGenericMethodSubset_GenericMemberBase_1_methods\[\]" "$out"/generated*.cpp
                ;;
        esac
        if grep -Eq '^struct ReflectGenericMethodSubset_GenericMemberBare' "$out/generated.h"; then
            echo 'FAIL: bare generic definition fabricated a class layout' >&2
            return 1
        fi
        compile_console "$out" FormalConstraintsOnly
        run_bounded "$out/FormalConstraintsOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        formal_constraint_asserts "$out/native.stdout"
        local mode
        for mode in unloaded-method-identity unloaded-self-assignability retained-formal-members formal-interface-order constraint-scope nested-constraint nested-assignability open-generic-signatures unloaded-open-signatures generic-definition-members generic-definition-relations-cold generic-definition-relations-warm; do
            run_bounded dotnet "$out/app/FormalConstraintsOnly.dll" "$mode" > "$out/$mode.clr.raw.stdout"
            strip_cr_win_file "$out/$mode.clr.raw.stdout" > "$out/$mode.clr.stdout"
            run_bounded "$out/FormalConstraintsOnly$EXE_EXT" "$mode" > "$out/$mode.native.raw.stdout"
            strip_cr_win_file "$out/$mode.native.raw.stdout" > "$out/$mode.native.stdout"
            diff -u "$out/$mode.clr.stdout" "$out/$mode.native.stdout"
            if [ "$mode" = open-generic-signatures ]; then
                open_signature_asserts "$out/$mode.native.stdout"
            elif [ "$mode" = generic-definition-members ]; then
                generic_definition_member_asserts "$out/$mode.native.stdout"
            elif [[ "$mode" = generic-definition-relations-* ]]; then
                test "$(grep -Fxc 'definition transitive relations: True/True' "$out/$mode.native.stdout")" = 1
            fi
        done
        run_bounded "$out/FormalConstraintsOnly$EXE_EXT" unloaded-relations > "$out/unloaded-relations.native.raw.stdout"
        strip_cr_win_file "$out/unloaded-relations.native.raw.stdout" > "$out/unloaded-relations.native.stdout"
        test "$(grep -Fxc 'unloaded constraint query: PlatformNotSupportedException' "$out/unloaded-relations.native.stdout")" = 2
        grep -Fxq 'unloaded constraint base: PlatformNotSupportedException' "$out/unloaded-relations.native.stdout"
        grep -Fxq 'unloaded constraint interfaces: PlatformNotSupportedException' "$out/unloaded-relations.native.stdout"
        run_bounded dotnet "$out/app/FormalConstraintsOnly.dll" scoped-constraint-targets > "$out/scoped-targets.clr.raw.stdout"
        strip_cr_win_file "$out/scoped-targets.clr.raw.stdout" > "$out/scoped-targets.clr.stdout"
        grep -Fxq 'scoped constraint targets: False/False' "$out/scoped-targets.clr.stdout"
        run_bounded "$out/FormalConstraintsOnly$EXE_EXT" unloaded-scoped-constraints > "$out/unloaded-scoped.native.raw.stdout"
        strip_cr_win_file "$out/unloaded-scoped.native.raw.stdout" > "$out/unloaded-scoped.native.stdout"
        grep -Fxq 'unloaded scoped identity: U/V' "$out/unloaded-scoped.native.stdout"
        test "$(grep -Fxc 'unloaded scoped leaf: PlatformNotSupportedException' "$out/unloaded-scoped.native.stdout")" = 2
        test "$(grep -Fxc 'unloaded scoped generic: PlatformNotSupportedException' "$out/unloaded-scoped.native.stdout")" = 2
        grep -Fxq 'unloaded scoped stable: True/True' "$out/unloaded-scoped.native.stdout"
        run_bounded "$out/FormalConstraintsOnly$EXE_EXT" stripped-formal-constraints > "$out/stripped.native.raw.stdout"
        strip_cr_win_file "$out/stripped.native.raw.stdout" > "$out/stripped.native.stdout"
        if [ "$axis" = trimmed ]; then
            test "$(grep -Fxc 'stripped constraint query: PlatformNotSupportedException' "$out/stripped.native.stdout")" = 2
            grep -Fxq 'stripped constraint base: PlatformNotSupportedException' "$out/stripped.native.stdout"
            grep -Fxq 'stripped constraint interfaces: PlatformNotSupportedException' "$out/stripped.native.stdout"
        else
            run_bounded dotnet "$out/app/FormalConstraintsOnly.dll" stripped-formal-constraints > "$out/stripped.clr.raw.stdout"
            strip_cr_win_file "$out/stripped.clr.raw.stdout" > "$out/stripped.clr.stdout"
            diff -u "$out/stripped.clr.stdout" "$out/stripped.native.stdout"
        fi
        echo "formal constraints $axis OK"
    done
}
