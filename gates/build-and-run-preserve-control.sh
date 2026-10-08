#!/usr/bin/env bash
# Preserved reflection-created boxes retain interface dispatch after write-back.
# Managed DLL stripping and explicit preservation: unreachable metadata is removed,
# while PreserveAttribute and merged Unity-format link.xml keep selected bodies.
# A property selected through a nonexistent accessor survives without its getter.
# ILDietControl also checks Array.Initialize constructors reached through method groups
# and application constructors/types selected at run time. Lookup-only members
# retain their signatures and compiler state-machine declarations without keeping
# unreachable async/iterator body dependencies; runtime roots still promote them.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/preserve-control/Program.cs gates/fixtures/preserve-control/MetadataProbe.csproj gates/fixtures/preserve-control/accessorless-indexer/AccessorlessIndexer.csproj gates/fixtures/preserve-control/accessorless-indexer/Program.cs gates/fixtures/preserve-control/accessorless-indexer/link.xml gates/fixtures/preserve-control/accessorless-indexer/Targets/IndexerTargets.csproj gates/fixtures/preserve-control/accessorless-indexer/Targets/Indexed.cs gates/fixtures/ildiet-metadata-validation/Program.cs gates/fixtures/ildiet-metadata-validation/check.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|attribute-object-prefix:${DN2CPP_BEFORE_ATTRIBUTE_OBJECT:-}|named-reflection-prefix:${DN2CPP_BEFORE_NAMED_REFLECTION:-}|runtime-reflection-prefix:${DN2CPP_BEFORE_RUNTIME_REFLECTION:-}|metadata-lookup-prefix:${DN2CPP_BEFORE_METADATA_LOOKUP:-}|signature-construction-prefix:${DN2CPP_BEFORE_SIGNATURE_CONSTRUCTION:-}|signature-invocation-prefix:${DN2CPP_BEFORE_SIGNATURE_INVOCATION:-}|field-boxing-prefix:${DN2CPP_BEFORE_FIELD_BOXING:-}|field-context-prefix:${DN2CPP_BEFORE_FIELD_CONTEXT:-}|field-owner-prefix:${DN2CPP_BEFORE_FIELD_OWNER:-}|field-shape-prefix:${DN2CPP_BEFORE_FIELD_SHAPE:-}|construction-payload-prefix:${DN2CPP_BEFORE_CONSTRUCTION_PAYLOAD:-}|field-cycle-prefix:${DN2CPP_BEFORE_FIELD_CYCLE:-}|member-field-signature-prefix:${DN2CPP_BEFORE_MEMBER_FIELD_SIGNATURE:-}|member-construction-signature-prefix:${DN2CPP_BEFORE_MEMBER_CONSTRUCTION_SIGNATURE:-}|base-field-signature-prefix:${DN2CPP_BEFORE_BASE_FIELD_SIGNATURE:-}|base-construction-signature-prefix:${DN2CPP_BEFORE_BASE_CONSTRUCTION_SIGNATURE:-}|ctor-signature-invocation-prefix:${DN2CPP_BEFORE_CTOR_SIGNATURE_INVOCATION:-}|field-owner-signature-prefix:${DN2CPP_BEFORE_FIELD_OWNER_SIGNATURE:-}|interface-owner-signature-prefix:${DN2CPP_BEFORE_INTERFACE_OWNER_SIGNATURE:-}|ctor-field-owner-signature-prefix:${DN2CPP_BEFORE_CTOR_FIELD_OWNER_SIGNATURE:-}|reflection-depth-seeds-prefix:${DN2CPP_BEFORE_REFLECTION_DEPTH_SEEDS:-}|caller-depth-prefix:${DN2CPP_BEFORE_CALLER_DEPTH:-}|extended-depth-prefix:${DN2CPP_BEFORE_EXTENDED_DEPTH:-}|state-machine-signatures-prefix:${DN2CPP_BEFORE_STATE_MACHINE_SIGNATURES:-}"
PYTHON=$(resolve_python) || gate_skip "no working Python 3 interpreter for ILDiet validation"

PROJECT=PreserveControl
ROOT="samples/dotnet/$PROJECT"
LIBPROJECT=samples/dotnet/PreserveControlLib
ASSEMBLYPROJECT=samples/dotnet/PreserveAssemblyLib

echo "== Building app and checking both Runtime target frameworks =="
build_proj "$ROOT/$PROJECT.csproj"
build_proj samples/dotnet/ILDietControl/ILDietControl.csproj
build_proj samples/dotnet/ILDietControl/LookupOnly/LookupOnly.csproj
build_proj samples/dotnet/ILDietControl/EventBoundary/EventBoundary.csproj
build_proj samples/dotnet/ILDietControl/ConstructorOnly/ConstructorOnly.csproj
build_proj samples/dotnet/ILDietControl/InvokeOnly/InvokeOnly.csproj
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
build_gate_proj gates/fixtures/preserve-control/MetadataProbe.csproj
build_gate_proj gates/fixtures/preserve-control/accessorless-indexer/AccessorlessIndexer.csproj
build_gate_proj gates/fixtures/ildiet-roots/App/RootApp.csproj
if [ -z "${DN2CPP_SKIP_BUILD:-}" ]; then
    dotnet build src/Dn2Cpp.Runtime/Dn2Cpp.Runtime.csproj -c "$CONFIG" -f net8.0 \
        --nologo -v:minimal
fi
[ -f "src/Dn2Cpp.Runtime/bin/$CONFIG/net8.0/Dn2Cpp.Runtime.dll" ] \
    || { echo "FAIL: Dn2Cpp.Runtime net8.0 output is missing" >&2; exit 1; }
[ -f "src/Dn2Cpp.Runtime/bin/$CONFIG/net10.0/Dn2Cpp.Runtime.dll" ] \
    || { echo "FAIL: Dn2Cpp.Runtime net10.0 output is missing" >&2; exit 1; }
grep -q -- '--project-root &quot;$(MSBuildProjectDirectory)&quot;' \
    src/Dn2Cpp.Build/build/Dn2Cpp.Build.targets \
    || { echo "FAIL: Dn2Cpp.Build does not pass the consuming project root" >&2; exit 1; }

APP="$ROOT/bin/$CONFIG/$TFM/$PROJECT.dll"
LIBDLL="$LIBPROJECT/bin/$CONFIG/$TFM/PreserveControlLib.dll"
ASSEMBLYDLL="$ASSEMBLYPROJECT/bin/$CONFIG/$TFM/PreserveAssemblyLib.dll"
INPUT_HASHES=$(shasum -a 256 "$APP" "$LIBDLL" "$ASSEMBLYDLL")
PROBE="gates/fixtures/preserve-control/bin/$CONFIG/$TFM/MetadataProbe.dll"
OUT=artifacts/preserve-control
mkdir -p "$OUT"
printf '<linker />\n' > "$OUT/.ildiet-request.xml"
printf 'preexisting-result-descriptor\n' > "$OUT/.ildiet-result.xml"
PROTOCOL_HASHES=$(shasum -a 256 "$OUT/.ildiet-request.xml" "$OUT/.ildiet-result.xml")
STALE_ROOT=$(mktemp -d "$PWD/artifacts/preserve-control-stale.XXXXXX")
cleanup_stale_root() {
    case "$STALE_ROOT" in
        "$PWD"/artifacts/preserve-control-stale.*) rm -rf -- "$STALE_ROOT" ;;
        *) echo "FAIL: refusing to clean unexpected temporary root $STALE_ROOT" >&2 ;;
    esac
}
trap cleanup_stale_root EXIT
for ignored_dir in bin obj .godot .git; do
    mkdir -p "$STALE_ROOT/$ignored_dir"
    printf '<stale-invalid-descriptor' > "$STALE_ROOT/$ignored_dir/link.xml"
done

echo "== Transpiling with recursive project roots and the com feature =="
if ! transpile=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --project-root "$ROOT" --project-root "$ROOT/./" --project-root "$STALE_ROOT" \
    --link-xml "$OUT/.ildiet-request.xml" --link-feature com -o "$OUT" 2>&1); then
    printf '%s\n' "$transpile" >&2
    exit 1
fi
[ "$PROTOCOL_HASHES" = "$(shasum -a 256 "$OUT/.ildiet-request.xml" "$OUT/.ildiet-result.xml")" ] \
    || { echo "FAIL: protocol staging replaced a preexisting descriptor" >&2; exit 1; }
printf '%s\n' "$transpile"
grep -q "NoSuchType" <<<"$transpile" \
    || { echo "FAIL: a missing type did not produce a warning naming the target" >&2; exit 1; }
grep -q "NoSuch&Member" <<<"$transpile" \
    || { echo "FAIL: a missing member did not warn with its decoded entity" >&2; exit 1; }
[ "$(grep -c 'NoSuchType' <<<"$transpile")" -eq 1 ] \
    && [ "$(grep -c 'NoSuch&Member' <<<"$transpile")" -eq 1 ] \
    || { echo "FAIL: effective preservation repeated a missing-target warning" >&2; exit 1; }
if grep -q "NoSuchAssembly" <<<"$transpile"; then
    echo "FAIL: ignoreIfMissing did not suppress the missing-assembly warning" >&2
    exit 1
fi

for symbol in BuiltInMethod SameNameMethod DerivedMethod AssemblyAwareDerivedMethod get_PreservedProperty \
        set_PreservedProperty add_PreservedEvent remove_PreservedEvent XmlMethod \
        SignatureIntMarker get_XmlProperty add_XmlEvent remove_XmlEvent ComMethod GenericMethod \
        ConditionalUsedMarker FieldSignatureType SignaturePropertyGetMarker \
        SignatureEventAddMarker SignatureEventRemoveMarker AssemblyOnlyTarget__ctor AttributeMembers__ctor \
        PreservedField InitializedField dginvoke_PreserveControlLib_PreservedDelegate \
        methtab_PreserveControlLib_PreservedDelegate SelectedChainLeaf FieldsModePayload \
        ConditionalChainLeaf; do
    grep -q "$symbol" "$OUT"/generated* \
        || { echo "FAIL: preserved body $symbol is absent from generated C++" >&2; exit 1; }
done
for symbol in DroppedMethod NotKeptByTypeAttribute SignatureNoArgMarker set_XmlProperty \
        SreMethod ConditionalUnusedMarker SignaturePropertySetMarker \
        AssemblyMethodNotKept NonDerivedMethod ConditionalUnusedChainLeaf \
        NotSelected FieldsModeMethodDrop IgnoreIfUnreferencedMethod; do
    if grep -Fq "_${symbol}_" "$OUT"/generated*; then
        echo "FAIL: unselected body $symbol survived stripping" >&2
        exit 1
    fi
done

echo "== Backend roots resolve generic bases by assembly identity =="
ROOT_FIXTURE=gates/fixtures/ildiet-roots
root_types=$(dotnet exec "$PROBE" --root-types External.Object \
    "$ROOT_FIXTURE/App/bin/$CONFIG/$TFM/RootApp.dll" \
    "$ROOT_FIXTURE/Decoy/bin/$CONFIG/$TFM/RootDecoy.dll" \
    "$ROOT_FIXTURE/Base/bin/$CONFIG/$TFM/RootBase.dll")
grep -Fxq 'RootApp:RootApp.RegisteredScript' <<<"$root_types" \
    || { echo "FAIL: a colliding type hid an externally instantiated script" >&2; exit 1; }
if grep -Fxq 'RootApp:RootApp.OrdinaryClass' <<<"$root_types"; then
    echo "FAIL: a colliding type made an ordinary class an external root" >&2; exit 1
fi

echo "== DLL metadata is removed before the transpiler model is built =="
original_lib=$(dotnet exec "$PROBE" "$LIBDLL")
stripped_lib=$(dotnet exec "$PROBE" "$OUT/ildiet/PreserveControlLib.dll")
original_app=$(dotnet exec "$PROBE" "$APP")
stripped_app=$(dotnet exec "$PROBE" "$OUT/ildiet/$PROJECT.dll")
for row in 'method PreserveControlLib.Live::UnusedPublic' \
        'type PreserveControlLib.UnusedType' \
        'method PreserveControlLib.AttributeMembers::DroppedMethod' \
        'type PreserveControlLib.ConditionalUnused'; do
    grep -Fxq "$row" <<<"$original_lib" \
        || { echo "FAIL: original metadata fixture is missing $row" >&2; exit 1; }
    if grep -Fxq "$row" <<<"$stripped_lib"; then
        echo "FAIL: ILDiet retained unreachable metadata $row" >&2; exit 1
    fi
done
grep -Fxq 'method PreserveControlLib.AttributeMembers::BuiltInMethod' <<<"$stripped_lib" \
    || { echo "FAIL: ILDiet removed an attributed method" >&2; exit 1; }
grep -Fxq 'type PreserveControl.UnusedAppType' <<<"$original_app" \
    || { echo "FAIL: original unused-public application fixture is missing" >&2; exit 1; }
grep -Fxq 'type PreserveControl.UnusedAppType' <<<"$stripped_app" \
    || { echo "FAIL: the armed reflection route lost an application type" >&2; exit 1; }
[ "$(wc -l <<<"$stripped_lib")" -lt "$(wc -l <<<"$original_lib")" ] \
    || { echo "FAIL: ILDiet did not reduce the model input" >&2; exit 1; }

compare_dlls() {
    local expected="$1" actual="$2" dll
    for dll in "$expected"/*.dll; do
        cmp "$dll" "$actual/$(basename "$dll")" \
            || { echo "FAIL: different stripped DLL: $(basename "$dll")" >&2; return 1; }
    done
}

echo "== Standalone ILDiet agrees with automatic preprocessing =="
CLI_DLL="${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
CLI_BIN=$(dirname "$CLI_DLL")
ILD_DLL="$CLI_BIN/ildiet/ILDiet.dll"
RUNTIME_DLL="$CLI_BIN/Dn2Cpp.Runtime.dll"
BCL=$(dirname "$(locate_corelib)")
echo "== Preserved indexer metadata survives removal of its accessor =="
INDEXER_FIXTURE=gates/fixtures/preserve-control/accessorless-indexer
INDEXER_APP="$INDEXER_FIXTURE/bin/$CONFIG/$TFM/AccessorlessIndexer.dll"
dotnet exec "$ILD_DLL" "$INDEXER_APP" -r "$BCL/System.Private.CoreLib.dll" \
    -r "$BCL/System.Runtime.dll" -r "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll" --link-xml "$INDEXER_FIXTURE/link.xml" \
    -o "$STALE_ROOT/accessorless-indexer"
indexer_original=$(dotnet exec "$PROBE" "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll")
indexer_diet=$(dotnet exec "$PROBE" "$STALE_ROOT/accessorless-indexer/IndexerTargets.dll")
indexer_original=$(strip_cr_win "$indexer_original")
indexer_diet=$(strip_cr_win "$indexer_diet")
grep -Fxq 'property PreserveFixture.Indexed::Item/accessors=1' <<<"$indexer_original" \
    && grep -Fxq 'property PreserveFixture.Indexed::Item/accessors=0' <<<"$indexer_diet" \
    || { echo "FAIL: a preserved property could not lose its unselected accessor" >&2; exit 1; }
if grep -Fxq 'method PreserveFixture.Indexed::get_Item' <<<"$indexer_diet"; then
    echo "FAIL: a nonexistent setter preserved the getter" >&2; exit 1
fi
STANDALONE="$STALE_ROOT/standalone 日本語 with spaces"
standalone_refs=(-r "$LIBDLL" -r "$ASSEMBLYDLL" -r "$RUNTIME_DLL")
for dll in "$OUT/ildiet/"*.dll; do
    name=$(basename "$dll")
    case "$name" in
        "$PROJECT.dll"|PreserveControlLib.dll|PreserveAssemblyLib.dll|Dn2Cpp.Runtime.dll) continue ;;
    esac
    if [ -f "$BCL/$name" ]; then
        standalone_refs+=(-r "$BCL/$name")
    elif [ -f "$CLI_BIN/$name" ]; then
        standalone_refs+=(-r "$CLI_BIN/$name")
    else
        echo "FAIL: cannot locate original reference $name" >&2; exit 1
    fi
done
dotnet exec "$ILD_DLL" "$APP" "${standalone_refs[@]}" \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" \
    --link-feature com -o "$STANDALONE"
compare_dlls "$OUT/ildiet" "$STANDALONE"

echo "== Repeated preprocessing is deterministic and validates cuts before deletion =="
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --cut PreserveControlLib.UnusedType::UnusedMethod \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" \
    --link-feature com -o "$OUT"
compare_dlls "$STANDALONE" "$OUT/ildiet"
[ "$INPUT_HASHES" = "$(shasum -a 256 "$APP" "$LIBDLL" "$ASSEMBLYDLL")" ] \
    || { echo "FAIL: ILDiet changed an input DLL" >&2; exit 1; }

echo "== Explicit stripped inputs and bypass produce the same C++ =="
MANUAL="$STALE_ROOT/manual-cpp"
invoke_cli "$OUT/ildiet/$PROJECT.dll" \
    -r "$OUT/ildiet/PreserveControlLib.dll" -r "$OUT/ildiet/PreserveAssemblyLib.dll" \
    -r "$OUT/ildiet/Dn2Cpp.Runtime.dll" -r "$OUT/ildiet/System.Private.CoreLib.dll" \
    --auto-ref --no-ildiet --trim-reflection \
    --link-xml "$OUT/ildiet/preservation.xml" --link-feature com -o "$MANUAL"
for generated in "$OUT"/generated*; do
    cmp "$generated" "$MANUAL/$(basename "$generated")" \
        || { echo "FAIL: manual preprocessing changed emitted C++" >&2; exit 1; }
done
[ ! -d "$MANUAL/ildiet" ] \
    || { echo "FAIL: --no-ildiet created an intermediate DLL directory" >&2; exit 1; }

echo "== Assembly element without children preserves the whole assembly =="
WHOLE=artifacts/preserve-control-whole
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --project-root "$ROOT" \
    --link-feature remoting -o "$WHOLE"
grep -Fq "_DroppedMethod_" "$WHOLE"/generated* \
    || { echo "FAIL: childless assembly descriptor did not preserve all methods" >&2; exit 1; }

if gate_cache_check "$OUT" "preserve-control|ildiet|com|trim-reflection|cut=PreserveControlLib.UnusedType::UnusedMethod|preserved-box-prefix:${DN2CPP_BEFORE_PRESERVED_BOXES:-}" \
        "$APP" "$LIBDLL" "$ASSEMBLYDLL" "$ROOT/link.xml" "$ROOT/Nested/link.xml" \
        gates/expected/preserve-control.txt; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    native=$("./$OUT/$PROJECT")
    assert_output "$(strip_cr_win "$native")" "$(cat gates/expected/preserve-control.txt)"
    before=$(DN2CPP_BEFORE_PRESERVED_BOXES=1 run_bounded "./$OUT/$PROJECT")
    prefix=$(awk '/^== preserved reflection boxes ==$/ { exit } { print }' <<< "$native")
    assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
    gate_cache_commit
fi

echo "== IL dispatch, initialization, layout and type-token reflection construction retain .NET behavior after stripping =="
DIET_LIB="samples/dotnet/ILDietControlLib/bin/$CONFIG/$TFM/ILDietControlLib.dll"
gate_extra_asserts() {
    local out="$1" native before prefix
    native=$(run_bounded "$out/ILDietControl$EXE_EXT") || return $?
    native=$(strip_cr_win "$native")
    before=$(DN2CPP_BEFORE_ATTRIBUTE_OBJECT=1 run_bounded "$out/ILDietControl$EXE_EXT") || return $?
    prefix=$(awk '/^== attribute construction roots ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'attribute-object=by-object' <<< "$native" \
        || { echo 'FAIL: boxed Type attribute constructor did not run' >&2; return 1; }
    grep -Fxq 'attribute construction roots end' <<< "$native" \
        || { echo 'FAIL: Type attribute construction section did not run' >&2; return 1; }
    before=$(DN2CPP_BEFORE_NAMED_REFLECTION=1 run_bounded "$out/ILDietControl$EXE_EXT") || return $?
    prefix=$(awk '/^== constant-name reflection ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'method-static=42' <<< "$native" \
        || { echo 'FAIL: a method selected only by its constant name did not run' >&2; return 1; }
    grep -Fxq 'constant-name reflection end' <<< "$native" \
        || { echo 'FAIL: constant-name reflection section did not run' >&2; return 1; }
    before=$(DN2CPP_BEFORE_RUNTIME_REFLECTION=1 run_bounded "$out/ILDietControl$EXE_EXT") || return $?
    prefix=$(awk '/^== runtime reflection preservation ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'field-type-construction=field-payload' <<< "$native" \
        || { echo 'FAIL: construction through a reflected field type did not run' >&2; return 1; }
    grep -Fxq 'assembly-types=True:True' <<< "$native" \
        || { echo 'FAIL: application types selected only by name were not listed' >&2; return 1; }
    grep -Fxq 'runtime reflection preservation end' <<< "$native" \
        || { echo 'FAIL: runtime reflection preservation section did not run' >&2; return 1; }
}
corelib_diff_gate ILDietControl -r "$DIET_LIB"
unset -f gate_extra_asserts
DIET_OUT="$_CG_OUT"
DIET_APP="$_CG_APP"
original_diet_app=$(dotnet exec "$PROBE" "$DIET_APP")
stripped_diet_app=$(dotnet exec "$PROBE" "$DIET_OUT/ildiet/ILDietControl.dll")
for row in 'method ILDietControl.Loose::.ctor' 'method ILDietControl.Box`1::.ctor' \
        'method ILDietControl.Seeded::.ctor'; do
    kept=$(grep -Fxc "$row" <<<"$stripped_diet_app" || true)
    [ "$kept" -gt 0 ] && [ "$kept" = "$(grep -Fxc "$row" <<<"$original_diet_app")" ] \
        || { echo "FAIL: ILDiet removed a type-token-selected constructor: $row" >&2; exit 1; }
done
for row in 'method ILDietControl.Named::Twice' 'method ILDietControl.Named::Describe' \
        'method ILDietControl.Named::get_Label' 'method ILDietControl.Named::set_Label'; do
    grep -Fxq "$row" <<<"$stripped_diet_app" \
        || { echo "FAIL: ILDiet removed a member a reflective invoke can select: $row" >&2; exit 1; }
done
grep -Fxq 'method ILDietControl.CalledOnly::.ctor' <<<"$original_diet_app" \
    || { echo "FAIL: original fixture is missing CalledOnly's constructor" >&2; exit 1; }
grep -Fxq 'method ILDietControl.CalledOnly::.ctor' <<<"$stripped_diet_app" \
    || { echo "FAIL: the armed constructor route lost an application constructor" >&2; exit 1; }
diet_metadata=$(dotnet exec "$PROBE" "$DIET_OUT/ildiet/ILDietControlLib.dll")
for row in 'method ILDietControlLib.Base::Foo' \
        'method ILDietControlLib.Callbacks::NativeCallback' \
        'method ILDietControlLib.Callbacks::CallbackLeaf' \
        'method ILDietControlLib.Callbacks::NativeEntry' \
        'method ILDietControlLib.Callbacks::NativeLeaf' \
        'field ILDietControlLib.Layout::_unused' \
        'method <Module>::.cctor'; do
    grep -Fxq "$row" <<<"$diet_metadata" \
        || { echo "FAIL: ILDiet lost a runtime or layout dependency: $row" >&2; exit 1; }
done
for row in 'method ILDietControlLib.Base::UnusedPrivate' \
        'type ILDietControlLib.UnusedType' \
        'type ILDietControlLib.ConstructorOnlyDependency'; do
    if grep -Fxq "$row" <<<"$diet_metadata"; then
        echo "FAIL: unused ordinary library code survived: $row" >&2; exit 1
    fi
done
[ -f "${DIET_LIB%.dll}.pdb" ] && [ ! -e "$DIET_OUT/ildiet/ILDietControlLib.pdb" ] \
    || { echo "FAIL: rewritten DLL retained stale debug symbols" >&2; exit 1; }

echo "== Lookup-only reflection retains declarations without body dependencies =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/LookupOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native before prefix original stripped original_library stripped_library row
        native=$(run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        before=$(DN2CPP_BEFORE_METADATA_LOOKUP=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== metadata-only reflection ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq 'event-accessors=True:True' <<< "$native" \
            || { echo 'FAIL: lookup-only event accessor declarations are absent' >&2; return 1; }
        grep -Fxq 'metadata-only reflection end' <<< "$native" \
            || { echo 'FAIL: metadata-only reflection section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_BOXING=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== reflected field boxing ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq '0 JPY' <<< "$native" && grep -Fxq '0 USD' <<< "$native" \
            && grep -Fxq '0 EUR' <<< "$native" \
            && grep -Fxq 'reflected field boxing end' <<< "$native" \
            || { echo 'FAIL: reflected field boxing section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_CONTEXT=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== closed field contexts ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq '0 GBP' <<< "$native" && grep -Fxq 'boxed-array-argument' <<< "$native" \
            && grep -Fxq 'closed field contexts end' <<< "$native" \
            || { echo 'FAIL: closed field context section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_OWNER=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== reference field owners ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq '0 CHF' <<< "$native" && grep -Fxq '0 CAD' <<< "$native" \
            && grep -Fxq 'reference field owners end' <<< "$native" \
            || { echo 'FAIL: reference field owner section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_SHAPE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== field type shapes ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq 'array-owner-box' <<< "$native" && grep -Fxq 'generic-argument-owner-box' <<< "$native" \
            && grep -Fxq 'field type shapes end' <<< "$native" \
            || { echo 'FAIL: field type shape section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_CYCLE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== finite field contexts ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq 'boxed-cycle-a' <<< "$native" && grep -Fxq 'boxed-cycle-b' <<< "$native" \
            && grep -Fxq 'finite field contexts end' <<< "$native" \
            || { echo 'FAIL: finite field context section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_MEMBER_FIELD_SIGNATURE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== closed member field signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq '0 NZD' <<< "$native" && grep -Fxq '0 SGD' <<< "$native" \
            && grep -Fxq 'closed member field signatures end' <<< "$native" \
            || { echo 'FAIL: closed member field signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_BASE_FIELD_SIGNATURE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== closed base field signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'base-return-money' <<< "$native" && grep -Fxq 'base-parameter-money' <<< "$native" \
            && grep -Fxq 'closed base field signatures end' <<< "$native" \
            || { echo 'FAIL: closed base field signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_FIELD_OWNER_SIGNATURE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== field owner member signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'field-owner-money' <<< "$native" \
            && grep -Fxq 'field owner member signatures end' <<< "$native" \
            || { echo 'FAIL: field owner member signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_INTERFACE_OWNER_SIGNATURE=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== interface owner member signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'interface-owner-money' <<< "$native" \
            && grep -Fxq 'interface owner member signatures end' <<< "$native" \
            || { echo 'FAIL: interface owner member signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_REFLECTION_DEPTH_SEEDS=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== reflection depth seeds ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'library-depth-second' <<< "$native" && grep -Fxq 'method-depth-second' <<< "$native" \
            && grep -Fxq 'reflection depth seeds end' <<< "$native" \
            || { echo 'FAIL: reflection depth seed section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_CALLER_DEPTH=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== caller reflection depth ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq "$caller_depth_case-caller-depth-second" <<< "$native" \
            && grep -Fxq 'caller reflection depth end' <<< "$native" \
            || { echo 'FAIL: the independent caller depth section did not run' >&2; return 1; }
        if [ "$extended_depth_case" != none ]; then
            before=$(DN2CPP_BEFORE_EXTENDED_DEPTH=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
            prefix=$(awk '/^== extended reflection depth ==$/ { exit } { print }' <<< "$native")
            assert_output "$prefix" "$(strip_cr_win "$before")"
            grep -Fxq "$extended_depth_case-depth-second" <<< "$native" \
                && grep -Fxq 'extended reflection depth end' <<< "$native" \
                || { echo 'FAIL: the independent extended depth section did not run' >&2; return 1; }
        fi
        before=$(DN2CPP_BEFORE_STATE_MACHINE_SIGNATURES=1 run_bounded "$out/LookupOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== signature-only state machines ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in '== signature-only state machines ==' 'async metadata=True' \
            'iterator metadata=True' 'async iterator metadata=True' 'async called=7' \
            'iterator called=8' 'promoted machine=9' 'signature-only state machines end'; do
            [ "$(grep -Fxc -- "$row" <<< "$native")" = 1 ] \
                || { echo "FAIL: state-machine signature block did not run once: $row" >&2; return 1; }
        done
        original=$(dotnet exec "$PROBE" "$_CG_APP") || return $?
        original=$(strip_cr_win "$original")
        stripped=$(dotnet exec "$PROBE" "$out/ildiet/LookupOnly.dll") || return $?
        stripped=$(strip_cr_win "$stripped")
        local state_original state_stripped state_bodies
        state_original=$(strip_cr_win "$original")
        state_stripped=$(strip_cr_win "$stripped")
        for row in AsyncBodyOnlyDependency IteratorBodyOnlyDependency AsyncIteratorBodyOnlyDependency; do
            grep -Fxq "type ILDietLookupOnly.$row" <<< "$state_original" \
                || { echo "FAIL: original state-machine fixture has no body dependency: $row" >&2; return 1; }
            if grep -Fxq "type ILDietLookupOnly.$row" <<< "$state_stripped"; then
                echo "FAIL: signature-only state machine retained its original body dependency: $row" >&2; return 1
            fi
        done
        for row in CalledAsyncDependency CalledIteratorDependency PromotedMachineDependency ScalarAttributeDependency; do
            grep -Fxq "method ILDietLookupOnly.$row::Read" <<< "$state_stripped" \
                || { echo "FAIL: executable or scalar Type attribute root lost its body: $row" >&2; return 1; }
        done
        state_bodies=$(dotnet exec "$PROBE" --state-machine-bodies "$out/ildiet/LookupOnly.dll") || return $?
        assert_output "$(strip_cr_win "$state_bodies")" "state-machine UncalledAsync/method-stub=True/move-next-stub=True
state-machine UncalledIterator/method-stub=True/move-next-stub=True
state-machine UncalledAsyncIterator/method-stub=True/move-next-stub=True
state-machine CalledAsync/method-stub=False/move-next-stub=False
state-machine CalledIterator/method-stub=False/move-next-stub=False
state-machine LateRegistration/method-stub=True/move-next-stub=False
state-machine CalledRegistration/method-stub=False/move-next-stub=False"
        if [ "$extended_depth_case" != none ]; then
            grep -Fxq 'type ILDietLookupOnly.ExtendedDepthBodyOnlyDependency' <<< "$original" \
                || { echo 'FAIL: the original extended depth fixture has no body dependency' >&2; return 1; }
            if grep -Fxq 'type ILDietLookupOnly.ExtendedDepthBodyOnlyDependency' <<< "$stripped"; then
                echo 'FAIL: extended depth accounting opened an unused method body' >&2; return 1
            fi
        fi
        grep -Fxq 'type ILDietLookupOnly.CallerDepthBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original caller depth fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietLookupOnly.CallerDepthBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: caller depth accounting opened an unused method body' >&2; return 1
        fi
        grep -Fxq 'type ILDietLookupOnly.DepthBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original depth fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietLookupOnly.DepthBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: reflection depth accounting opened an unused method body' >&2; return 1
        fi
        grep -Fxq 'type ILDietLookupOnly.InterfaceSignatureBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original interface owner fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietLookupOnly.InterfaceSignatureBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: an interface owner signature opened an unused method body' >&2; return 1
        fi
        grep -Fxq 'method ILDietLookupOnly.InterfaceSignatureInner`1::Make' <<< "$stripped" \
            && grep -Fxq 'method ILDietLookupOnly.InterfaceSignatureMoney::ToString' <<< "$stripped" \
            || { echo 'FAIL: interface owner signatures lost member metadata or boxed dispatch' >&2; return 1; }
        grep -Fxq 'type ILDietLookupOnly.FieldSignatureBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original field owner fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietLookupOnly.FieldSignatureBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: a field owner signature opened an unused method body' >&2; return 1
        fi
        grep -Fxq 'method ILDietLookupOnly.FieldSignatureInner`1::Make' <<< "$stripped" \
            && grep -Fxq 'method ILDietLookupOnly.FieldSignatureMoney::ToString' <<< "$stripped" \
            || { echo 'FAIL: field owner signatures lost member metadata or boxed dispatch' >&2; return 1; }
        grep -Fxq 'type ILDietLookupOnly.BodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original lookup fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietLookupOnly.BodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: lookup-only signatures kept an unreachable body dependency' >&2; return 1
        fi
        grep -Fxq 'type ILDietLookupOnly.SignatureOnlyType' <<< "$stripped" \
            || { echo 'FAIL: lookup-only signatures lost their parameter type' >&2; return 1; }
        grep -Fxq 'event ILDietLookupOnly.MetadataOnly::Tick/accessors=2' <<< "$stripped" \
            || { echo 'FAIL: lookup-only event metadata lost an accessor' >&2; return 1; }
        local event_original event_stripped
        event_original=$(dotnet exec "$PROBE" --lookup-event "$_CG_APP" ILDietLookupOnly.MetadataOnly Tick) || return $?
        event_stripped=$(dotnet exec "$PROBE" --lookup-event "$out/ildiet/LookupOnly.dll" ILDietLookupOnly.MetadataOnly Tick) || return $?
        assert_output "$(strip_cr_win "$event_stripped")" "$(strip_cr_win "$event_original")"
        original_library=$(dotnet exec "$PROBE" "$DIET_LIB") || return $?
        original_library=$(strip_cr_win "$original_library")
        stripped_library=$(dotnet exec "$PROBE" "$out/ildiet/ILDietControlLib.dll") || return $?
        stripped_library=$(strip_cr_win "$stripped_library")
        grep -Fxq 'type ILDietControlLib.UnusedBoxDependency' <<< "$original_library" \
            || { echo 'FAIL: the original box fixture has no unused body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.UnusedBoxDependency' <<< "$stripped_library"; then
            echo 'FAIL: reflected field boxing kept an unused library method body' >&2; return 1
        fi
        grep -Fxq 'method ILDietControlLib.ReflectionMoney::ToString' <<< "$stripped_library" \
            || { echo 'FAIL: reflected field boxing lost user-library dispatch' >&2; return 1; }
    }
    dotnet build "$DN2CPP_SAMPLE_PROJECT_DIR/LookupOnly.csproj" -c "$CONFIG" -p:DefineConstants= -p:BuildProjectReferences=false
    DN2CPP_SKIP_BUILD=1
    extended_depth_case=none
    caller_depth_case=class
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|caller-depth-case:$caller_depth_case"
    corelib_diff_gate LookupOnly -r "$DIET_LIB"
    (
        echo "== Copied framework event wrappers keep lookup-only bodies stripped =="
        DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-framework-events"
        corelib_diff_gate LookupOnly -r "$DIET_LIB" -r "$(dirname "$_CG_CORELIB")/System.Runtime.InteropServices.dll"
    )
    # Separate DLLs prevent either caller's depth from covering the other route.
    dotnet build "$DN2CPP_SAMPLE_PROJECT_DIR/LookupOnly.csproj" -c "$CONFIG" \
        -p:DefineConstants=METHOD_DEPTH_CALLER -p:BuildProjectReferences=false
    caller_depth_case=method
    DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-method-caller"
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|caller-depth-case:$caller_depth_case"
    DN2CPP_SKIP_BUILD=1
    corelib_diff_gate LookupOnly -r "$DIET_LIB"
    for extended_depth_case in virtual framework gvm gvm-owner inherited-virtual factory-virtual symbolic-gvm late-symbolic-gvm; do
        case "$extended_depth_case" in
            virtual) depth_constants=EXTENDED_VIRTUAL_DEPTH ;;
            inherited-virtual) depth_constants=EXTENDED_INHERITED_VIRTUAL_DEPTH ;;
            factory-virtual) depth_constants=EXTENDED_FACTORY_VIRTUAL_DEPTH ;;
            symbolic-gvm) depth_constants=EXTENDED_SYMBOLIC_GVM_DEPTH ;;
            late-symbolic-gvm) depth_constants=EXTENDED_LATE_SYMBOLIC_GVM_DEPTH ;;
            framework) depth_constants=EXTENDED_FRAMEWORK_DEPTH ;;
            gvm) depth_constants=EXTENDED_GVM_DEPTH ;;
            gvm-owner) depth_constants=EXTENDED_GVM_OWNER_DEPTH ;;
        esac
        dotnet build "$DN2CPP_SAMPLE_PROJECT_DIR/LookupOnly.csproj" -c "$CONFIG" \
            -p:DefineConstants="$depth_constants" -p:BuildProjectReferences=false
        caller_depth_case=class
        DN2CPP_OUT_SUFFIX="-$extended_depth_case-depth"
        DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|extended-depth-case:$extended_depth_case"
        corelib_diff_gate LookupOnly -r "$DIET_LIB"
    done
)

echo "== Constructor-only reflection constructs signature-selected types without opening application accessor bodies =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/ConstructorOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native before prefix original stripped app original_arming stripped_arming
        native=$(run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        before=$(DN2CPP_BEFORE_SIGNATURE_CONSTRUCTION=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== signature construction ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq 'accessor-metadata=True' <<< "$native" \
            && grep -Fxq 'signature construction end' <<< "$native" \
            || { echo 'FAIL: signature construction section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_CONSTRUCTION_PAYLOAD=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== generic construction dispatch ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq '0 CHF' <<< "$native" \
            && grep -Fxq 'generic construction dispatch end' <<< "$native" \
            || { echo 'FAIL: generic construction dispatch section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_MEMBER_CONSTRUCTION_SIGNATURE=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== closed member construction signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq '0 HKD' <<< "$native" && grep -Fxq '0 CNY' <<< "$native" \
            && grep -Fxq 'closed member construction signatures end' <<< "$native" \
            || { echo 'FAIL: closed member construction signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_BASE_CONSTRUCTION_SIGNATURE=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== closed base construction signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'base-library-return' <<< "$native" && grep -Fxq 'base-library-parameter' <<< "$native" \
            && grep -Fxq 'closed base construction signatures end' <<< "$native" \
            || { echo 'FAIL: closed base construction signature section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_CTOR_FIELD_OWNER_SIGNATURE=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== constructor field owner signatures ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'ctor-field-owner-money' <<< "$native" \
            && grep -Fxq 'constructor field owner signatures end' <<< "$native" \
            || { echo 'FAIL: constructor field owner signature section did not run' >&2; return 1; }
        original=$(dotnet exec "$PROBE" "$DIET_LIB") || return $?
        original=$(strip_cr_win "$original")
        stripped=$(dotnet exec "$PROBE" "$out/ildiet/ILDietControlLib.dll") || return $?
        stripped=$(strip_cr_win "$stripped")
        grep -Fxq 'type ILDietControlLib.ConstructorFieldBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original constructor field owner fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.ConstructorFieldBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: constructor field discovery opened an unused method body' >&2; return 1
        fi
        grep -Fxq 'method ILDietControlLib.ConstructorFieldMoney::ToString' <<< "$stripped" \
            || { echo 'FAIL: constructor field discovery lost user-library dispatch' >&2; return 1; }
        grep -Fxq 'type ILDietControlLib.AccessorOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original constructor fixture has no accessor dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.AccessorOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: the constructor route opened an unused application accessor body' >&2; return 1
        fi
        grep -Fxq 'type ILDietControlLib.ConstructionBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original construction fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.ConstructionBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: closed signature construction kept an unused method body' >&2; return 1
        fi
        grep -Fxq 'method ILDietControlLib.ConstructionMoney::ToString' <<< "$stripped" \
            || { echo 'FAIL: signature construction lost user-library dispatch' >&2; return 1; }
        grep -Fxq 'type ILDietControlLib.MemberConstructionBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original member signature fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.MemberConstructionBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: substituted member signatures opened an unused library method body' >&2; return 1
        fi
        for type in MemberConstructionMoney MemberParameterMoney; do
            grep -Fxq "method ILDietControlLib.$type::ToString" <<< "$stripped" \
                || { echo 'FAIL: substituted member construction lost user-library dispatch' >&2; return 1; }
        done
        grep -Fxq 'type ILDietControlLib.BaseConstructionBodyOnlyDependency' <<< "$original" \
            || { echo 'FAIL: the original base signature fixture has no body dependency' >&2; return 1; }
        if grep -Fxq 'type ILDietControlLib.BaseConstructionBodyOnlyDependency' <<< "$stripped"; then
            echo 'FAIL: substituted base signatures opened an unused library method body' >&2; return 1
        fi
        for type in BaseConstructionMoney BaseParameterMoney; do
            grep -Fxq "method ILDietControlLib.$type::ToString" <<< "$stripped" \
                || { echo 'FAIL: substituted base construction lost user-library dispatch' >&2; return 1; }
        done
        original_arming=$(dotnet exec "$PROBE" --check-construction-only "$_CG_APP" "$CLI_BIN/Dn2Cpp.Transpiler.dll") || return $?
        original_arming=$(strip_cr_win "$original_arming")
        stripped_arming=$(dotnet exec "$PROBE" --check-construction-only "$out/ildiet/ConstructorOnly.dll" "$CLI_BIN/Dn2Cpp.Transpiler.dll") || return $?
        stripped_arming=$(strip_cr_win "$stripped_arming")
        assert_output "$stripped_arming" "$original_arming"
        grep -Fxq 'constructor-only descriptors=clean' <<< "$stripped_arming" \
            || { echo 'FAIL: constructor fixture does not isolate its reflection route' >&2; return 1; }
        app=$(dotnet exec "$PROBE" "$out/ildiet/ConstructorOnly.dll") || return $?
        app=$(strip_cr_win "$app")
        grep -Fxq 'property ILDietConstructorOnly.AccessorOwner::Label/accessors=1' <<< "$app" \
            || { echo 'FAIL: the constructor route lost application accessor metadata' >&2; return 1; }
    }
    corelib_diff_gate ConstructorOnly -r "$DIET_LIB"
)

echo "== Invoke-only reflection runs methods on signature-selected closed types =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/InvokeOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native before prefix original stripped app
        native=$(run_bounded "$out/InvokeOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        before=$(DN2CPP_BEFORE_SIGNATURE_INVOCATION=1 run_bounded "$out/InvokeOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== signature invocation ==$/ { exit } { print }' <<< "$native")
        assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
        grep -Fxq 'signature invocation end' <<< "$native" \
            || { echo 'FAIL: signature invocation section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_CTOR_SIGNATURE_INVOCATION=1 run_bounded "$out/InvokeOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== constructor signature invocation ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'Int32' <<< "$native" && grep -Fxq 'invoke-library-money' <<< "$native" \
            && grep -Fxq 'constructor signature invocation end' <<< "$native" \
            || { echo 'FAIL: constructor signature invocation section did not run' >&2; return 1; }
        original=$(dotnet exec "$PROBE" "$DIET_LIB") || return $?
        original=$(strip_cr_win "$original")
        stripped=$(dotnet exec "$PROBE" "$out/ildiet/ILDietControlLib.dll") || return $?
        stripped=$(strip_cr_win "$stripped")
        for type in InvokeBodyOnlyDependency InvokeConstructorOnlyDependency; do
            grep -Fxq "type ILDietControlLib.$type" <<< "$original" \
                || { echo 'FAIL: the original invocation fixture has no unused dependency' >&2; return 1; }
            if grep -Fxq "type ILDietControlLib.$type" <<< "$stripped"; then
                echo 'FAIL: invocation opened an unused library or application constructor body' >&2; return 1
            fi
        done
        grep -Fxq 'method ILDietControlLib.InvokeSignatureMoney::ToString' <<< "$stripped" \
            || { echo 'FAIL: signature invocation lost user-library dispatch' >&2; return 1; }
        app=$(dotnet exec "$PROBE" "$out/ildiet/InvokeOnly.dll") || return $?
        app=$(strip_cr_win "$app")
        grep -Fxq 'method ILDietInvokeOnly.ConstructorSignatureTarget`1::Value' <<< "$app" \
            && grep -Fxq 'method ILDietInvokeOnly.LibrarySignatureTarget`1::Get' <<< "$app" \
            || { echo 'FAIL: signature invocation lost a selected method' >&2; return 1; }
    }
    corelib_diff_gate InvokeOnly -r "$DIET_LIB"
)

echo "== Reflected event subscription retains bodies; native event lookup refuses the unsupported representation =="
EVENT_APP="samples/dotnet/ILDietControl/EventBoundary/bin/$CONFIG/$TFM/EventBoundary.dll"
EVENT_DIET="$STALE_ROOT/event-boundary"
dotnet exec "$ILD_DLL" "$EVENT_APP" -r "$_CG_CORELIB" -o "$EVENT_DIET"
event_original=$(run_bounded dotnet "$EVENT_APP")
event_original=$(strip_cr_win "$event_original")
event_stripped=$(run_bounded dotnet exec --runtimeconfig "${EVENT_APP%.dll}.runtimeconfig.json" "$EVENT_DIET/EventBoundary.dll")
event_stripped=$(strip_cr_win "$event_stripped")
assert_output "$(strip_cr_win "$event_stripped")" "$(strip_cr_win "$event_original")"
grep -Fxq 'event-operations=done' <<< "$event_stripped" \
    || { echo 'FAIL: reflected event subscription section did not run' >&2; exit 1; }
for preprocessing in enabled disabled; do
    event_flags=()
    [ "$preprocessing" = disabled ] && event_flags=(--no-ildiet)
    set +e
    event_error=$(invoke_cli "$EVENT_APP" -r "$_CG_CORELIB" ${event_flags[@]+"${event_flags[@]}"} -o "$STALE_ROOT/event-native-$preprocessing" 2>&1)
    event_code=$?
    set -e
    [ "$event_code" -ne 0 ] && grep -Fq 'System.Type::GetEvent(String) has no intrinsic mapping yet' <<< "$event_error" \
        || { echo "FAIL: native event lookup crossed its unsupported boundary: $event_error" >&2; exit 1; }
done

echo "== Failed stripping preserves the last complete output and unrelated directories =="
TRANSACTION="$STALE_ROOT/transaction"
dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" -r "$RUNTIME_DLL" \
    -o "$TRANSACTION"
compare_dlls "$DIET_OUT/ildiet" "$TRANSACTION"
transaction_hashes=$(shasum -a 256 "$TRANSACTION/"*.dll "$TRANSACTION/preservation.xml")
printf '<linker><assembly' > "$STALE_ROOT/broken.xml"
OCCUPIED="$STALE_ROOT/occupied"
mkdir -p "$OCCUPIED"
printf 'unrelated-user-data' > "$OCCUPIED/keep.txt"
set +e
failed_output=$(dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" \
    -r "$RUNTIME_DLL" --link-xml "$STALE_ROOT/broken.xml" -o "$TRANSACTION" 2>&1)
failed_code=$?
occupied_output=$(dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" \
    -r "$RUNTIME_DLL" -o "$OCCUPIED" 2>&1)
occupied_code=$?
set -e
[ "$failed_code" -ne 0 ] && grep -q 'broken.xml' <<<"$failed_output" \
    || { echo "FAIL: malformed XML did not fail before publication" >&2; exit 1; }
[ "$transaction_hashes" = "$(shasum -a 256 "$TRANSACTION/"*.dll "$TRANSACTION/preservation.xml")" ] \
    || { echo "FAIL: a failed strip changed the previous complete generation" >&2; exit 1; }
[ "$occupied_code" -ne 0 ] && [ "$(cat "$OCCUPIED/keep.txt")" = unrelated-user-data ] \
    && [ ! -e "$OCCUPIED/ILDietControl.dll" ] \
    || { echo "FAIL: ILDiet modified an unrelated occupied directory: $occupied_output" >&2; exit 1; }

echo "== A missing companion stops before C++ emission =="
BROKEN="$STALE_ROOT/missing companion"
mkdir -p "$BROKEN"
cp "$CLI_BIN/"*.dll "$CLI_BIN/"*.json "$BROKEN/"
set +e
missing_err=$(dotnet exec "$BROKEN/dn2cpp.dll" "$APP" --auto-ref \
    -r "$LIBDLL" -r "$ASSEMBLYDLL" -o "$STALE_ROOT/failed-cpp" 2>&1)
missing_code=$?
set -e
[ "$missing_code" -ne 0 ] && grep -q 'ILDiet companion not found' <<<"$missing_err" \
    || { echo "FAIL: missing ILDiet did not stop clearly" >&2; exit 1; }
[ ! -f "$STALE_ROOT/failed-cpp/generated.h" ] \
    || { echo "FAIL: failed ILDiet emitted C++" >&2; exit 1; }

echo "== Invalid link feature and malformed XML fail before emission =="
set +e
feature_err=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" \
    --link-feature typo -o artifacts/preserve-feature-bad 2>&1)
feature_code=$?
malformed_err=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" \
    --project-root samples/dotnet/PreserveControlMalformed -o artifacts/preserve-malformed 2>&1)
malformed_code=$?
set -e
[ "$feature_code" -ne 0 ] && grep -q "com, sre, or remoting" <<<"$feature_err" \
    || { echo "FAIL: invalid --link-feature was not rejected clearly" >&2; exit 1; }
[ "$malformed_code" -ne 0 ] \
    && grep -q "PreserveControlMalformed/link.xml" <<<"$(tr '\\' / <<<"$malformed_err")" \
    || { echo "FAIL: malformed link.xml did not fail naming its path" >&2; exit 1; }

CONSOLE_CLI="src/Dn2Cpp.Cli.Console/bin/$CONFIG/$TFM/dn2cpp-console.dll"
CONSOLE_ILD="$STALE_ROOT/console stripped 日本語"
dotnet exec "$CONSOLE_CLI" "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" \
    --link-feature com --ildiet-output "$CONSOLE_ILD" -o "$STALE_ROOT/console-cpp"
compare_dlls "$OUT/ildiet" "$CONSOLE_ILD"

set +e
console_err=$(dotnet exec "$CONSOLE_CLI" "$APP" --link-feature typo 2>&1)
console_code=$?
set -e
[ "$console_code" -ne 0 ] && grep -q "com, sre, or remoting" <<<"$console_err" \
    || { echo "FAIL: console CLI did not validate --link-feature" >&2; exit 1; }

for fixture in PreserveControlUnknownElement PreserveControlUnknownPreserve \
        PreserveControlUnknownFeature PreserveControlBadDeclaration \
        PreserveControlIgnoredInvalid; do
    set +e
    schema_err=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" \
        --project-root "samples/dotnet/$fixture" -o "artifacts/$fixture" 2>&1)
    schema_code=$?
    set -e
    [ "$schema_code" -ne 0 ] \
        && grep -q "$fixture/link.xml" <<<"$(tr '\\' / <<<"$schema_err")" \
        || { echo "FAIL: $fixture did not hard-fail naming its descriptor" >&2; exit 1; }
done

build_proj samples/dotnet/HelloWorld/HelloWorld.csproj
output_code=0
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PYTHON gates/fixtures/preserve-control/check-ildiet-output.py "$ILD_DLL" \
    "samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll" "$_CG_CORELIB" \
    "artifacts/ildiet-output-tests-$CONFIG" || output_code=$?
if [ "$output_code" -eq 77 ]; then
    gate_skip "ILDiet output alias checks require permission to create real file and directory symlinks"
fi
[ "$output_code" -eq 0 ] || exit "$output_code"

echo "== ILDiet payload changes invalidate behavior gate caches =="
(
    probe="$(mktemp -d "$PWD/artifacts/ildiet-cache-hash.XXXXXX")"
    trap 'rm -rf "$probe"' EXIT
    mkdir -p "$probe/ildiet/runtime"
    files=(dn2cpp.dll Dn2Cpp.Transpiler.dll ildiet/ILDiet.dll ildiet/Mono.Cecil.dll
           ildiet/ILDiet.runtimeconfig.json ildiet/ILDiet.deps.json ildiet/ILDiet
           ildiet/runtime/libcoreclr)
    for file in "${files[@]}"; do printf 'original' > "$probe/$file"; done
    DN2CPP_CLI_DLL="$probe/dn2cpp.dll"
    before="$(_gate_cli_hash)"
    for file in "${files[@]}"; do
        printf 'changed' >> "$probe/$file"
        after="$(_gate_cli_hash)"
        [ "$before" != "$after" ] || {
            echo "FAIL: changing $file did not invalidate the CLI cache hash" >&2; exit 1; }
        before="$after"
    done
)

echo "== Rewritten metadata, signed identity, resources and dead assemblies =="
build_gate_proj gates/fixtures/ildiet-metadata-validation/MetadataValidation.csproj
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PYTHON gates/fixtures/ildiet-metadata-validation/check.py "$ILD_DLL" \
    "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    "$DIET_APP" "$DIET_LIB" "$_CG_CORELIB" "artifacts/ildiet-metadata-tests-$CONFIG"

echo "== Conditional engine scripts and constructor registries are rewritten in DLLs =="
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PYTHON gates/fixtures/ildiet-metadata-validation/check-engine-policy.py "$ILD_DLL" \
    "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    "$_CG_CORELIB" "artifacts/ildiet-engine-policy-$CONFIG"
script_discovery=$(dotnet exec "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    --check-script-discovery "artifacts/ildiet-script-discovery-$CONFIG")
assert_output "$(strip_cr_win "$script_discovery")" \
    "godot-script-discovery=all-scenes,autoload,relative-autoload,const-path,triple-path,resource,uid,relative,root-relative,global,import-uid,ignored-strings,binary-image,binary-script,ignored-output,unknown-resource,indirect-uid,escaped-path,missing-project
godot-script-escapes=controls,unicode-path,unicode-uid,supplementary,surrogates,continuation,raw,invalid,unterminated,zero-replacement,missing-autoload,project-fallback,diagnostics"

echo "== Suppressed signature-only initializers never become throwing startup roots =="
(
    DN2CPP_SKIP_BUILD=1
    eval "$(declare -f compile_console | sed '1s/compile_console/compile_initializer_console/')"
    compile_console() {
        # Check the existing runtime completion flags after the eager pass and
        # before Main; stdout parity alone cannot observe a swallowed failure.
        $PYTHON gates/fixtures/ildiet-metadata-validation/check.py --instrument-startup "$1" "$initializer_case"
        compile_initializer_console "$@"
    }
    gate_extra_asserts() {
        echo "native-initializer-policy=$initializer_case"
    }
    for initializer_case in signature explicit late original; do
        DN2CPP_SAMPLE_PROJECT_DIR="artifacts/ildiet-metadata-tests-$CONFIG/initializer-fixtures/$initializer_case/native"
        DN2CPP_OUT_SUFFIX="-initializer-$initializer_case"
        corelib_diff_gate InitializerFixture System.Console --no-ildiet
    done
)

echo "OK"
