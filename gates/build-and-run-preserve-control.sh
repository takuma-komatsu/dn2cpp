#!/usr/bin/env bash
# Preserved reflection-created boxes retain interface dispatch after write-back.
# Full mode also exercises the isolated routing and preprocessing variants below.
# Managed DLL stripping and explicit preservation: unreachable metadata is removed,
# while PreserveAttribute and merged Unity-format link.xml keep selected bodies.
# An indexer selected through a nonexistent accessor fails signature validation.
# ILDietControl also checks Array.Initialize constructors reached through method groups
# and application constructors/types selected at run time. Lookup-only members
# retain their signatures and compiler state-machine declarations without keeping
# unreachable async/iterator body dependencies; runtime roots still promote them.
# Isolated drivers pin what stays uncompiled: a lookup's body, an unselected overload,
# an unrelated field's type, a preserved constructor's uncalled interface members,
# an untouched type's initializer, the attributes IsDefined or CustomAttributeData
# reads and the attribute types a typed read does not name. A reflection target
# from a parameter warns, refuses with a remedy, and matches .NET once that remedy preserves it; so does Activator
# over a library type whose constructor rows the image omits or a struct it never
# boxes, while a type declaring no such constructor still throws
# MissingMethodException. One site that binds a name per instantiation fixes every
# owner it binds. Reflection that
# selects members on a user-library base type or on a type named only by a constant
# string keeps exactly the selected library bodies through preprocessing; a lookup or
# listing that runs nothing keeps the identities and rows it selects as signature-only stubs.
# A static field read through a constant-named derived owner keeps its base's
# initializer, a conditional descriptor applies to a type used only through a
# constant name, and Type attribute arguments retain their types without a descriptor.
# Conditional field signatures activate earlier, later and closed generic owners.
# Local and captured interface arrays supply null defaults without a base type row.
# Typed loads recover external enum zero defaults; untyped defaults stay unknown.
# Initialized locals retain typed zero/null alternatives when control flow joins.
# Repeated allocations retain older arrays' defaults without widening array type identity.
# Integer array and captured-field stores truncate before signed or unsigned reads.
# Captured field reads retain zero/null defaults alongside later finite writes.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_ildiet.sh"
# The default profile keeps independent positive/negative witnesses. Full mode
# restores the cross-product for focused preservation and reachability changes.
case "${DN2CPP_PRESERVE_CONTROL_FULL:-0}" in
    0|1) ;;
    *) echo 'DN2CPP_PRESERVE_CONTROL_FULL must be 0 or 1' >&2; exit 1 ;;
esac
export DN2CPP_PRESERVE_CONTROL_FULL="${DN2CPP_PRESERVE_CONTROL_FULL:-0}"
preserve_full() { [ "$DN2CPP_PRESERVE_CONTROL_FULL" = 1 ]; }
echo "preserve-control-profile=$([ "$DN2CPP_PRESERVE_CONTROL_FULL" = 1 ] && echo full || echo representative)"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|preserve-control-full:$DN2CPP_PRESERVE_CONTROL_FULL"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $(printf '%s ' gates/fixtures/ildiet-metadata-validation/*.py)"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/preserve-control/Program.cs gates/fixtures/preserve-control/MetadataProbe.csproj gates/fixtures/preserve-control/accessorless-indexer/AccessorlessIndexer.csproj gates/fixtures/preserve-control/accessorless-indexer/Program.cs gates/fixtures/preserve-control/accessorless-indexer/link.xml gates/fixtures/preserve-control/accessorless-indexer/Targets/IndexerTargets.csproj gates/fixtures/preserve-control/accessorless-indexer/Targets/Indexed.cs gates/fixtures/ildiet-metadata-validation/Program.cs gates/fixtures/ildiet-metadata-validation/check.py samples/dotnet/ILDietControl/keep-attribute-constructed.xml samples/dotnet/ILDietControl/ConstructorOnly/keep-signature-constructed.xml samples/dotnet/ILDietControl/InvokeOnly/keep-signature-invoked.xml samples/dotnet/ILDietControl/AbsenceOnly/keep-preserved-constructor.xml samples/dotnet/ILDietControl/UnknownTargetOnly/keep-parameter-targets.xml samples/dotnet/ILDietControl/ConditionalNamedOnly/keep-conditional-named.xml gates/fixtures/ildiet-attribute-roots/AttributeRoots.csproj gates/fixtures/ildiet-attribute-roots/Program.cs gates/expected/preserve-control-unknown-target.tsv"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|reflected-event-operations:native-ildiet-and-no-ildiet|attribute-object-prefix:${DN2CPP_BEFORE_ATTRIBUTE_OBJECT:-}|named-reflection-prefix:${DN2CPP_BEFORE_NAMED_REFLECTION:-}|runtime-reflection-prefix:${DN2CPP_BEFORE_RUNTIME_REFLECTION:-}|metadata-lookup-prefix:${DN2CPP_BEFORE_METADATA_LOOKUP:-}|signature-construction-prefix:${DN2CPP_BEFORE_SIGNATURE_CONSTRUCTION:-}|signature-invocation-prefix:${DN2CPP_BEFORE_SIGNATURE_INVOCATION:-}|field-boxing-prefix:${DN2CPP_BEFORE_FIELD_BOXING:-}|field-context-prefix:${DN2CPP_BEFORE_FIELD_CONTEXT:-}|field-owner-prefix:${DN2CPP_BEFORE_FIELD_OWNER:-}|field-shape-prefix:${DN2CPP_BEFORE_FIELD_SHAPE:-}|construction-payload-prefix:${DN2CPP_BEFORE_CONSTRUCTION_PAYLOAD:-}|field-cycle-prefix:${DN2CPP_BEFORE_FIELD_CYCLE:-}|member-field-signature-prefix:${DN2CPP_BEFORE_MEMBER_FIELD_SIGNATURE:-}|member-construction-signature-prefix:${DN2CPP_BEFORE_MEMBER_CONSTRUCTION_SIGNATURE:-}|base-field-signature-prefix:${DN2CPP_BEFORE_BASE_FIELD_SIGNATURE:-}|base-construction-signature-prefix:${DN2CPP_BEFORE_BASE_CONSTRUCTION_SIGNATURE:-}|ctor-signature-invocation-prefix:${DN2CPP_BEFORE_CTOR_SIGNATURE_INVOCATION:-}|field-owner-signature-prefix:${DN2CPP_BEFORE_FIELD_OWNER_SIGNATURE:-}|interface-owner-signature-prefix:${DN2CPP_BEFORE_INTERFACE_OWNER_SIGNATURE:-}|ctor-field-owner-signature-prefix:${DN2CPP_BEFORE_CTOR_FIELD_OWNER_SIGNATURE:-}|captured-array-defaults-prefix:${DN2CPP_BEFORE_CAPTURED_ARRAY_DEFAULTS:-}|interface-array-defaults-prefix:${DN2CPP_BEFORE_INTERFACE_ARRAY_DEFAULTS:-}|external-array-defaults-prefix:${DN2CPP_BEFORE_EXTERNAL_ARRAY_DEFAULTS:-}|local-defaults-prefix:${DN2CPP_BEFORE_LOCAL_DEFAULTS:-}|reflection-depth-seeds-prefix:${DN2CPP_BEFORE_REFLECTION_DEPTH_SEEDS:-}|caller-depth-prefix:${DN2CPP_BEFORE_CALLER_DEPTH:-}|extended-depth-prefix:${DN2CPP_BEFORE_EXTENDED_DEPTH:-}|state-machine-signatures-prefix:${DN2CPP_BEFORE_STATE_MACHINE_SIGNATURES:-}|retained-absence-prefix:${DN2CPP_BEFORE_RETAINED_ABSENCE:-}|unknown-target-prefix:${DN2CPP_BEFORE_UNKNOWN_TARGET:-}|initonly-field-prefix:${DN2CPP_BEFORE_INITONLY_FIELD:-}|metadata-owners-prefix:${DN2CPP_BEFORE_METADATA_OWNERS:-}|assembly-construction-prefix:${DN2CPP_BEFORE_ASSEMBLY_CONSTRUCTION:-}|listed-owners-prefix:${DN2CPP_BEFORE_LISTED_OWNERS:-}|dispatched-target-prefix:${DN2CPP_BEFORE_DISPATCHED_TARGET:-}|written-target-prefix:${DN2CPP_BEFORE_WRITTEN_TARGET:-}|absence-values:${DN2CPP_JOINED_TARGET_NAME+set:$DN2CPP_JOINED_TARGET_NAME}:${DN2CPP_FORMED_ONLY+set:$DN2CPP_FORMED_ONLY}:${DN2CPP_CONSTRUCTED_TEMPLATE+set:$DN2CPP_CONSTRUCTED_TEMPLATE}:${DN2CPP_UNUSED_STATICS+set:$DN2CPP_UNUSED_STATICS}:${DN2CPP_USED_STATICS+set:$DN2CPP_USED_STATICS}"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|repeated-arrays-prefix:${DN2CPP_BEFORE_REPEATED_ARRAYS:-}|integer-storage-prefix:${DN2CPP_BEFORE_ARRAY_NARROWING:-}|captured-scalar-defaults-prefix:${DN2CPP_BEFORE_CAPTURED_SCALAR_DEFAULTS:-}"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/ildiet-metadata-validation/CutRequestCapture.cs gates/fixtures/ildiet-metadata-validation/cut_authority_admission.py gates/fixtures/ildiet-metadata-validation/selected_cut_admission.py gates/fixtures/ildiet-metadata-validation/cut_request_capture.py"
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
        ConditionalChainLeaf ConditionalEarlierMarker ConditionalLaterMarker ConditionalGenericMarker; do
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
stripped_lib=$(dotnet exec "$PROBE" "$(ildiet_generation "$OUT/ildiet")/PreserveControlLib.dll")
original_app=$(dotnet exec "$PROBE" "$APP")
stripped_app=$(dotnet exec "$PROBE" "$(ildiet_generation "$OUT/ildiet")/$PROJECT.dll")
for row in 'method PreserveControlLib.Live::UnusedPublic' \
        'type PreserveControlLib.UnusedType' \
        'type PreserveControlLib.ConditionalUnused'; do
    grep -Fxq "$row" <<<"$original_lib" \
        || { echo "FAIL: original metadata fixture is missing $row" >&2; exit 1; }
    if grep -Fxq "$row" <<<"$stripped_lib"; then
        echo "FAIL: ILDiet retained unreachable metadata $row" >&2; exit 1
    fi
done
grep -Fxq 'method PreserveControlLib.AttributeMembers::BuiltInMethod' <<<"$stripped_lib" \
    || { echo "FAIL: ILDiet removed an attributed method" >&2; exit 1; }
# A constant-name lookup that runs nothing keeps the row it selects without its body.
looked_up=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$OUT/ildiet")/PreserveControlLib.dll" \
    PreserveControlLib.AttributeMembers::DroppedMethod)
assert_output "$(strip_cr_win "$looked_up")" 'PreserveControlLib.AttributeMembers::DroppedMethod stub=True'
grep -Fxq 'type PreserveControl.UnusedAppType' <<<"$original_app" \
    || { echo "FAIL: original unused-public application fixture is missing" >&2; exit 1; }
if grep -Fxq 'type PreserveControl.UnusedAppType' <<<"$stripped_app"; then
    echo "FAIL: reflection on a known target retained an unrelated application type" >&2; exit 1
fi
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
echo "== Removing an indexer's only accessor fails signature validation =="
INDEXER_FIXTURE=gates/fixtures/preserve-control/accessorless-indexer
INDEXER_APP="$INDEXER_FIXTURE/bin/$CONFIG/$TFM/AccessorlessIndexer.dll"
indexer_original=$(dotnet exec "$PROBE" "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll")
indexer_original=$(strip_cr_win "$indexer_original")
grep -Fxq 'property PreserveFixture.Indexed::Item/accessors=1' <<<"$indexer_original" \
    || { echo "FAIL: the original indexer fixture lost its getter" >&2; exit 1; }
invoke_cli "$INDEXER_APP" -r "$BCL/System.Private.CoreLib.dll" -r "$BCL/System.Runtime.dll" \
    -r "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll" --no-ildiet \
    --link-xml "$INDEXER_FIXTURE/link.xml" -o "$STALE_ROOT/accessorless-original"
indexer_output="$STALE_ROOT/accessorless-indexer"
indexer_auto="$STALE_ROOT/accessorless-auto"
set +e
indexer_error=$(dotnet exec "$ILD_DLL" "$INDEXER_APP" -r "$BCL/System.Private.CoreLib.dll" \
    -r "$BCL/System.Runtime.dll" -r "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll" \
    --link-xml "$INDEXER_FIXTURE/link.xml" -o "$indexer_output" 2>&1)
indexer_code=$?
indexer_auto_error=$(invoke_cli "$INDEXER_APP" -r "$BCL/System.Private.CoreLib.dll" -r "$BCL/System.Runtime.dll" \
    -r "$INDEXER_FIXTURE/Targets/bin/$CONFIG/$TFM/IndexerTargets.dll" \
    --link-xml "$INDEXER_FIXTURE/link.xml" -o "$indexer_auto" 2>&1)
indexer_auto_code=$?
set -e
indexer_error=$(strip_cr_win "$indexer_error")
indexer_auto_error=$(strip_cr_win "$indexer_auto_error")
indexer_expected='ILDiet: error: Original array signature does not match the retained Cecil signature.'
[ "$indexer_code" -eq 1 ] && grep -Fxq "$indexer_expected" <<<"$indexer_error" \
    && [ ! -e "$indexer_output/ildiet-result.xml" ] \
    || { echo "FAIL: standalone ILDiet did not reject the lost indexer signature: $indexer_error" >&2; exit 1; }
[ "$indexer_auto_code" -eq 2 ] && grep -Fxq "$indexer_expected" <<<"$indexer_auto_error" \
    && [ ! -e "$indexer_auto/ildiet/ildiet-result.xml" ] \
    || { echo "FAIL: automatic ILDiet did not reject the lost indexer signature: $indexer_auto_error" >&2; exit 1; }
echo "accessorless-indexer=original-primary,standalone-and-integrated-refusal"
STANDALONE="$STALE_ROOT/standalone 日本語 with spaces"
standalone_refs=(-r "$LIBDLL" -r "$ASSEMBLYDLL" -r "$RUNTIME_DLL")
for dll in "$(ildiet_generation "$OUT/ildiet")/"*.dll; do
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
compare_dlls "$(ildiet_generation "$OUT/ildiet")" "$(ildiet_generation "$STANDALONE")"

echo "== Repeating the same preprocessing contract is deterministic =="
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" \
    --link-feature com -o "$OUT"
compare_dlls "$(ildiet_generation "$STANDALONE")" "$(ildiet_generation "$OUT/ildiet")"
echo "== Cuts retain original managed bodies and validate before deletion =="
CUT_OUT="$STALE_ROOT/cut-cpp"
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --cut PreserveControlLib.UnusedType::UnusedMethod \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" --link-feature com -o "$CUT_OUT"
cut_generation=$(ildiet_generation "$CUT_OUT/ildiet")
cut_metadata=$(dotnet exec "$PROBE" "$cut_generation/PreserveControlLib.dll")
cut_metadata=$(strip_cr_win "$cut_metadata")
for row in 'type PreserveControlLib.UnusedType' 'method PreserveControlLib.UnusedType::UnusedMethod'; do
    grep -Fxq "$row" <<< "$cut_metadata" \
        || { echo "FAIL: managed cut target lost metadata: $row" >&2; exit 1; }
    if grep -Fxq "$row" <<< "$stripped_lib"; then
        echo "FAIL: no-cut negative control retained the cut-only target: $row" >&2; exit 1
    fi
done
cut_body=$(dotnet exec "$PROBE" --method-body "$cut_generation/PreserveControlLib.dll" PreserveControlLib.UnusedType::UnusedMethod)
assert_output "$(strip_cr_win "$cut_body")" 'PreserveControlLib.UnusedType::UnusedMethod stub=False'
cmp "$(ildiet_generation "$OUT/ildiet")/preservation.xml" "$cut_generation/preservation.xml"
if grep -Fxq '// PreserveControlLib.UnusedType::UnusedMethod' "$CUT_OUT"/generated*.cpp; then
    echo "FAIL: native cut target body was admitted" >&2; exit 1
fi
cut_baseline="$STALE_ROOT/cut-baseline"
mkdir -p "$cut_baseline"
cp "$cut_generation/"*.dll "$cut_baseline/"
cut_generation_name=$(basename "$cut_generation")
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --cut PreserveControlLib.UnusedType::UnusedMethod \
    --link-xml "$ROOT/link.xml" --link-xml "$ROOT/Nested/link.xml" --link-feature com -o "$CUT_OUT"
cut_repeated=$(ildiet_generation "$CUT_OUT/ildiet")
[ "$(basename "$cut_repeated")" = "$cut_generation_name" ] \
    || { echo "FAIL: identical cut contract changed generation" >&2; exit 1; }
compare_dlls "$cut_baseline" "$cut_repeated"
invalid_cut_out="$STALE_ROOT/cut-invalid"
invalid_cut_code=0
invalid_cut_error=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --cut PreserveControlLib.DoesNotExist::UnusedMethod -o "$invalid_cut_out" 2>&1) || invalid_cut_code=$?
[ "$invalid_cut_code" -ne 0 ] && grep -Fq -- '--cut PreserveControlLib.DoesNotExist::UnusedMethod' <<< "$invalid_cut_error" \
    && [ ! -e "$invalid_cut_out/ildiet/ildiet-result.xml" ] && [ ! -e "$invalid_cut_out/generated.h" ] \
    || { echo "FAIL: invalid cut escaped validation before deletion: $invalid_cut_error" >&2; exit 1; }
echo "cut-contract=kept-original-body,no-cut-absence,repeat-byte-identical,invalid-selector"
[ "$INPUT_HASHES" = "$(shasum -a 256 "$APP" "$LIBDLL" "$ASSEMBLYDLL")" ] \
    || { echo "FAIL: ILDiet changed an input DLL" >&2; exit 1; }

echo "== Explicit stripped inputs and bypass produce the same C++ =="
grep -Fq '<!--dn2cpp-type-names-v1:' "$(ildiet_generation "$OUT/ildiet")/preservation.xml" \
    || { echo "FAIL: ILDiet did not publish original type-name resolution metadata" >&2; exit 1; }
MANUAL="$STALE_ROOT/manual-cpp"
invoke_cli "$(ildiet_generation "$OUT/ildiet")/$PROJECT.dll" \
    -r "$(ildiet_generation "$OUT/ildiet")/PreserveControlLib.dll" -r "$(ildiet_generation "$OUT/ildiet")/PreserveAssemblyLib.dll" \
    -r "$(ildiet_generation "$OUT/ildiet")/Dn2Cpp.Runtime.dll" -r "$(ildiet_generation "$OUT/ildiet")/System.Private.CoreLib.dll" \
    --auto-ref --no-ildiet --trim-reflection \
    --link-xml "$(ildiet_generation "$OUT/ildiet")/preservation.xml" --link-feature com -o "$MANUAL"
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

if gate_cache_check "$OUT" "preserve-control|full:$DN2CPP_PRESERVE_CONTROL_FULL|ildiet|com|trim-reflection|cut=none|preserved-box-prefix:${DN2CPP_BEFORE_PRESERVED_BOXES:-}|conditional-fields-prefix:${DN2CPP_BEFORE_CONDITIONAL_FIELDS:-}" \
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
    before=$(DN2CPP_BEFORE_CONDITIONAL_FIELDS=1 run_bounded "./$OUT/$PROJECT")
    prefix=$(awk '/^== conditional field chains ==$/ { exit } { print }' <<< "$native")
    assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
    grep -Fxq 'conditional field chains end' <<< "$native" \
        || { echo 'FAIL: conditional field chains section did not run' >&2; exit 1; }
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
# Construct(Type) activates attribute-carried types no analyzable site names; their
# constructors are preserved explicitly.
corelib_diff_gate ILDietControl -r "$DIET_LIB" --link-xml samples/dotnet/ILDietControl/keep-attribute-constructed.xml
unset -f gate_extra_asserts
DIET_OUT="$_CG_OUT"
DIET_APP="$_CG_APP"
original_diet_app=$(dotnet exec "$PROBE" "$DIET_APP")
stripped_diet_app=$(dotnet exec "$PROBE" "$(ildiet_generation "$DIET_OUT/ildiet")/ILDietControl.dll")
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
    || { echo "FAIL: application Members retention lost its constructor row" >&2; exit 1; }
called_constructor=$(dotnet exec "$PROBE" --method-body "$(ildiet_generation "$DIET_OUT/ildiet")/ILDietControl.dll" ILDietControl.CalledOnly::.ctor)
assert_output "$(strip_cr_win "$called_constructor")" 'ILDietControl.CalledOnly::.ctor stub=True'
diet_metadata=$(dotnet exec "$PROBE" "$(ildiet_generation "$DIET_OUT/ildiet")/ILDietControlLib.dll")
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
[ -f "${DIET_LIB%.dll}.pdb" ] && [ ! -e "$(ildiet_generation "$DIET_OUT/ildiet")/ILDietControlLib.pdb" ] \
    || { echo "FAIL: rewritten DLL retained stale debug symbols" >&2; exit 1; }

echo "== Type attribute arguments retain their types without a descriptor or an armed reflection route =="
# ILDietControl's descriptor and armed construction route keep every application
# type, so this fixture holds the attribute arguments' types only through the blob.
ATTRIBUTE_FIXTURE=gates/fixtures/ildiet-attribute-roots
build_gate_proj "$ATTRIBUTE_FIXTURE/AttributeRoots.csproj"
ATTRIBUTE_APP="$ATTRIBUTE_FIXTURE/bin/$CONFIG/$TFM/AttributeRoots.dll"
dotnet exec "$ILD_DLL" "$ATTRIBUTE_APP" -r "$BCL/System.Private.CoreLib.dll" -r "$BCL/System.Runtime.dll" \
    -o "$STALE_ROOT/attribute-roots"
attribute_original=$(strip_cr_win "$(dotnet exec "$PROBE" "$ATTRIBUTE_APP")")
attribute_diet=$(strip_cr_win "$(dotnet exec "$PROBE" "$(ildiet_generation "$STALE_ROOT/attribute-roots")/AttributeRoots.dll")")
for row in ByArgument ByField ByProperty ByArrayFirst ByArraySecond ByObject Unnamed; do
    grep -Fxq "type AttributeRoots.$row" <<<"$attribute_original" \
        || { echo "FAIL: the attribute-root fixture is missing $row" >&2; exit 1; }
done
for row in ByArgument ByField ByProperty ByArrayFirst ByArraySecond ByObject; do
    grep -Fxq "type AttributeRoots.$row" <<<"$attribute_diet" \
        || { echo "FAIL: ILDiet dropped a type only an attribute argument names: $row" >&2; exit 1; }
done
if grep -Fxq 'type AttributeRoots.Unnamed' <<<"$attribute_diet"; then
    echo "FAIL: the attribute-root fixture kept a type nothing names" >&2; exit 1
fi

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
        stripped=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/LookupOnly.dll") || return $?
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
        state_bodies=$(dotnet exec "$PROBE" --state-machine-bodies "$(ildiet_generation "$out/ildiet")/LookupOnly.dll") || return $?
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
        local lookup_bodies
        lookup_bodies=$(dotnet exec "$PROBE" --method-body "$(ildiet_generation "$out/ildiet")/LookupOnly.dll" \
            ILDietLookupOnly.MetadataOnly::Uncalled ILDietLookupOnly.MetadataOnly::Signature \
            ILDietLookupOnly.MetadataOnly::add_Tick ILDietLookupOnly.MetadataOnly::remove_Tick \
            ILDietLookupOnly.SignatureOnlyType::UnusedVirtual \
            'ILDietLookupOnly.MemberSignatureOwner`1::Return' 'ILDietLookupOnly.MemberSignatureOwner`1::Parameter' \
            'ILDietLookupOnly.BaseSignatureOwner`1::Return' 'ILDietLookupOnly.BaseSignatureOwner`1::Parameter' \
            'ILDietLookupOnly.InnerWallet`1::UnusedVirtual' 'ILDietLookupOnly.CyclePair`2::UnusedVirtual' \
            ILDietLookupOnly.RuntimeFieldOwnerDependency::Run) || return $?
        assert_output "$(strip_cr_win "$lookup_bodies")" 'ILDietLookupOnly.MetadataOnly::Uncalled stub=True
ILDietLookupOnly.MetadataOnly::Signature stub=True
ILDietLookupOnly.MetadataOnly::add_Tick stub=True
ILDietLookupOnly.MetadataOnly::remove_Tick stub=True
ILDietLookupOnly.SignatureOnlyType::UnusedVirtual stub=True
ILDietLookupOnly.MemberSignatureOwner`1::Return stub=True
ILDietLookupOnly.MemberSignatureOwner`1::Parameter stub=True
ILDietLookupOnly.BaseSignatureOwner`1::Return stub=True
ILDietLookupOnly.BaseSignatureOwner`1::Parameter stub=True
ILDietLookupOnly.InnerWallet`1::UnusedVirtual stub=False
ILDietLookupOnly.CyclePair`2::UnusedVirtual stub=False
ILDietLookupOnly.RuntimeFieldOwnerDependency::Run stub=False'
        grep -Fxq 'type ILDietLookupOnly.SignatureOnlyType' <<< "$stripped" \
            || { echo 'FAIL: lookup-only signatures lost their parameter type' >&2; return 1; }
        grep -Fxq 'event ILDietLookupOnly.MetadataOnly::Tick/accessors=2' <<< "$stripped" \
            || { echo 'FAIL: lookup-only event metadata lost an accessor' >&2; return 1; }
        local event_original event_stripped
        event_original=$(dotnet exec "$PROBE" --lookup-event "$_CG_APP" ILDietLookupOnly.MetadataOnly Tick) || return $?
        event_stripped=$(dotnet exec "$PROBE" --lookup-event "$(ildiet_generation "$out/ildiet")/LookupOnly.dll" ILDietLookupOnly.MetadataOnly Tick) || return $?
        assert_output "$(strip_cr_win "$event_stripped")" "$(strip_cr_win "$event_original")"
        original_library=$(dotnet exec "$PROBE" "$DIET_LIB") || return $?
        original_library=$(strip_cr_win "$original_library")
        stripped_library=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll") || return $?
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
    if preserve_full; then
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
    fi
)

echo "== Constructor-only reflection constructs signature-selected types and keeps unwritten defaults on captured and repeated arrays without opening application accessor bodies =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/ConstructorOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    export DN2CPP_DUMP_REFLECTION_SUMMARY=1
    eval "$(declare -f invoke_cli | sed '1s/invoke_cli/invoke_constructor_array_cli/')"
    invoke_cli() {
        local rc=0
        invoke_constructor_array_cli "$@" 2> "$external_array_summary" || rc=$?
        if [ "$rc" -ne 0 ]; then cat "$external_array_summary" >&2; fi
        return "$rc"
    }
    gate_extra_asserts() {
        local out="$1" native before prefix original stripped app original_arming stripped_arming type section
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
        before=$(DN2CPP_BEFORE_CAPTURED_ARRAY_DEFAULTS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== captured array defaults ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in 'conditional=default' 'unwritten=default' 'copied=default' 'published=default' \
            'initialized=exact' 'captured array defaults end'; do
            grep -Fxq -- "$row" <<< "$native" \
                || { echo "FAIL: captured array default section did not run: $row" >&2; return 1; }
        done
        before=$(DN2CPP_BEFORE_INTERFACE_ARRAY_DEFAULTS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== interface array defaults ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        before=$(DN2CPP_BEFORE_INTERFACE_ARRAY_DEFAULTS=1 run_bounded dotnet "$_CG_APP") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in 'local=null' 'captured=null' 'interface array defaults end'; do
            grep -Fxq -- "$row" <<< "$native" \
                || { echo "FAIL: interface array default section did not run: $row" >&2; return 1; }
        done
        grep -Eqw 'CapturedInitializedTarget__ctor_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h \
            || { echo 'FAIL: the initialized captured array lost its selected constructor symbol' >&2; return 1; }
        if grep -Eqw 'CapturedInitializedTarget_UnselectedCapturedConstructor_m[0-9]+' \
            "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: an initialized captured array retained an unselected constructor dependency' >&2; return 1
        fi
        before=$(DN2CPP_BEFORE_EXTERNAL_ARRAY_DEFAULTS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== external array defaults ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        before=$(DN2CPP_BEFORE_EXTERNAL_ARRAY_DEFAULTS=1 run_bounded dotnet "$_CG_APP") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in external-local=default external-captured=default external-conditional=default \
                external-copied=default external-initialized=exact external-untyped=default 'external array defaults end'; do
            grep -Fxq -- "$row" <<< "$native" \
                || { echo "FAIL: external array default section did not run: $row" >&2; return 1; }
        done
        for type in ExternalLocalTarget ExternalCapturedTarget ExternalConditionalTarget ExternalCopiedTarget ExternalUntypedTarget; do
            grep -Eqw "${type}_SelectedDefault_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: an external array default lost $type's selected constructor body" >&2; return 1; }
        done
        grep -Eqw 'ExternalInitializedTarget_SelectedArgument_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h \
            || { echo 'FAIL: an initialized external array lost its selected constructor body' >&2; return 1; }
        if grep -Eqw 'External(Local|Captured|Conditional|Copied|Initialized|Untyped)Target_UnselectedArgument_m[0-9]+' \
                "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: an external array default retained an unselected constructor body' >&2; return 1
        fi
        section=$(awk '/^reflection-summary ConstructorOnly ILDietConstructorOnly.Program::ExternalUntypedFlags / { on = 1; next }
            /^reflection-summary / { on = 0 } on' "$external_array_summary")
        grep -Fq 'p1=?ArrayDefault(ILDietControlLib.ArrayBindingFlags)' <<< "$section" \
            || { echo 'FAIL: an untyped external array default became a concrete flag' >&2; echo "$section" >&2; return 1; }
        before=$(DN2CPP_BEFORE_REPEATED_ARRAYS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== repeated array allocations ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        before=$(DN2CPP_BEFORE_REPEATED_ARRAYS=1 run_bounded dotnet "$_CG_APP") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in repeated-local=default repeated-published=default repeated-copy=default \
                repeated-type=identity acyclic-initialized=exact 'repeated array allocations end'; do
            grep -Fxq -- "$row" <<< "$native" \
                || { echo "FAIL: repeated array section did not run: $row" >&2; return 1; }
        done
        for type in RepeatedLocalTarget RepeatedPublishedTarget RepeatedCopiedTarget RepeatedIdentityTarget; do
            grep -Eqw "${type}_SelectedDefault_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: repeated allocation lost $type's selected constructor body" >&2; return 1; }
        done
        grep -Eqw 'AcyclicInitializedTarget_SelectedArgument_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h \
            || { echo 'FAIL: an acyclic initialized array lost its selected constructor body' >&2; return 1; }
        if grep -Eqw '(Repeated(Local|Published|Copied)Target|AcyclicInitializedTarget)_UnselectedArgument_m[0-9]+' \
                "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: a repeated or acyclic array retained an unselected constructor body' >&2; return 1
        fi
        [ "$external_array_mode" != no-ildiet ] || return 0
        original=$(dotnet exec "$PROBE" "$DIET_LIB") || return $?
        original=$(strip_cr_win "$original")
        stripped=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll") || return $?
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
        original_arming=$(dotnet exec "$PROBE" --check-construction-only "$_CG_APP") || return $?
        original_arming=$(strip_cr_win "$original_arming")
        stripped_arming=$(dotnet exec "$PROBE" --check-construction-only "$(ildiet_generation "$out/ildiet")/ConstructorOnly.dll") || return $?
        stripped_arming=$(strip_cr_win "$stripped_arming")
        assert_output "$stripped_arming" "$original_arming"
        grep -Fxq 'constructor-only descriptors=clean' <<< "$stripped_arming" \
            || { echo 'FAIL: constructor fixture does not isolate its reflection route' >&2; return 1; }
        app=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/ConstructorOnly.dll") || return $?
        app=$(strip_cr_win "$app")
        grep -Fxq 'property ILDietConstructorOnly.AccessorOwner::Label/accessors=1' <<< "$app" \
            || { echo 'FAIL: the constructor route lost application accessor metadata' >&2; return 1; }
    }
    # Construct(string, Type) activates signature-selected types no analyzable site names.
    external_array_context="$DN2CPP_GATE_EXTRA_CONTEXT"
    for external_array_mode in ildiet no-ildiet; do
        [ "$external_array_mode" = ildiet ] || preserve_full || continue
        DN2CPP_GATE_EXTRA_CONTEXT="$external_array_context|external-array-defaults:$external_array_mode|reflection-summary:1"
        external_array_summary="artifacts/constructor-external-array-$CONFIG-$external_array_mode-summary.txt"
        mkdir -p artifacts
        external_array_flags=()
        if [ "$external_array_mode" = no-ildiet ]; then
            DN2CPP_OUT_SUFFIX="-external-array-no-ildiet"
            external_array_flags=(--no-ildiet)
        fi
        corelib_diff_gate ConstructorOnly System.Linq -r "$DIET_LIB" \
            --link-xml "$DN2CPP_SAMPLE_PROJECT_DIR/keep-signature-constructed.xml" \
            ${external_array_flags[@]+"${external_array_flags[@]}"}
    done
)

if preserve_full; then
echo "== Initialized local reflection operands retain zero and null across control-flow joins =="
build_gate_proj gates/fixtures/ildiet-metadata-validation/MetadataValidation.csproj
(
    local_defaults_original="samples/dotnet/ILDietControl/ConstructorOnly/bin/$CONFIG/$TFM/ConstructorOnly.dll"
    DN2CPP_SAMPLE_PROJECT_DIR="artifacts/constructor-local-defaults-$CONFIG"
    local_defaults_bin="$DN2CPP_SAMPLE_PROJECT_DIR/bin/$CONFIG/$TFM"
    mkdir -p "$local_defaults_bin"
    cp "${local_defaults_original%/*}/"*.dll "${local_defaults_original%/*}/"*.json "$local_defaults_bin/"
    local_defaults_fixture=$(dotnet exec "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
        --create-local-defaults-fixture "$local_defaults_original" "$local_defaults_bin/ConstructorOnly.dll")
    assert_output "$(strip_cr_win "$local_defaults_fixture")" \
        'local-defaults-fixture=initialized-bool,joined-null-array,uninitialized-bool'
    local_defaults_prefix=$(run_bounded dotnet "$local_defaults_original")
    local_defaults_before=$(DN2CPP_BEFORE_LOCAL_DEFAULTS=1 run_bounded dotnet "$local_defaults_bin/ConstructorOnly.dll")
    assert_output "$(strip_cr_win "$local_defaults_before")" "$(strip_cr_win "$local_defaults_prefix")"
    DN2CPP_SKIP_BUILD=1
    export DN2CPP_DUMP_REFLECTION_SUMMARY=1
    eval "$(declare -f invoke_cli | sed '1s/invoke_cli/invoke_local_defaults_cli/')"
    invoke_cli() {
        local rc=0
        invoke_local_defaults_cli "$@" 2> "$local_defaults_summary" || rc=$?
        if [ "$rc" -ne 0 ]; then cat "$local_defaults_summary" >&2; fi
        return "$rc"
    }
    gate_extra_asserts() {
        local out="$1" native prefix before marker section
        native=$(run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        prefix=$(awk '/^== initialized local operands ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$local_defaults_prefix")"
        before=$(DN2CPP_BEFORE_LOCAL_DEFAULTS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for marker in case-local=default args-local=default args-local=one uninitialized-local=skipped \
                'initialized local operands end'; do
            grep -Fxq "$marker" <<< "$native" \
                || { echo "FAIL: local default witness did not run: $marker" >&2; return 1; }
        done
        for marker in InitializedCaseTarget_SelectedDefault InitializedArgumentsTarget_SelectedDefault \
                InitializedArgumentsTarget_SelectedArgument; do
            grep -Eqw "${marker}_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: initialized local lost constructor body $marker" >&2; return 1; }
        done
        if grep -Eqw 'Initialized(Case|Arguments)Target_UnselectedPair_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: initialized locals retained an unselected constructor body' >&2; return 1
        fi
        section=$(awk '/^reflection-summary ConstructorOnly ILDietConstructorOnly.Program::InitializedCaseLocal / { on = 1; next }
            /^reflection-summary / { on = 0 } on' "$local_defaults_summary")
        grep -Eq 'AssemblyGetType/TypeIdentity .* p0="ILDietConstructorOnly.InitializedCaseTarget" p1=0 p2=0$' <<< "$section" \
            || { echo 'FAIL: initialized Boolean local was not false in the summary' >&2; echo "$section" >&2; return 1; }
        section=$(awk '/^reflection-summary ConstructorOnly ILDietConstructorOnly.Program::JoinedArgumentsLocal / { on = 1; next }
            /^reflection-summary / { on = 0 } on' "$local_defaults_summary")
        grep -Eq 'ActivatorCreate/Construct .* p1=\{null \| \[[^],]*"one"[^],]*\]\}$' <<< "$section" \
            || { echo 'FAIL: joined local summary lost null or one-element arguments' >&2; echo "$section" >&2; return 1; }
        section=$(awk '/^reflection-summary ConstructorOnly ILDietConstructorOnly.Program::UninitializedCaseLocal / { on = 1; next }
            /^reflection-summary / { on = 0 } on' "$local_defaults_summary")
        grep -Fq 'p2=?InitialLocal(V_0)' <<< "$section" \
            || { echo 'FAIL: an uninitialized local became a concrete reflection operand' >&2; echo "$section" >&2; return 1; }
    }
    for local_defaults_mode in ildiet no-ildiet; do
        DN2CPP_OUT_SUFFIX="-local-defaults-$local_defaults_mode"
        DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|local-defaults:$local_defaults_mode"
        local_defaults_summary="$DN2CPP_SAMPLE_PROJECT_DIR/$local_defaults_mode-summary.txt"
        local_defaults_flags=()
        [ "$local_defaults_mode" != no-ildiet ] || local_defaults_flags=(--no-ildiet)
        corelib_diff_gate ConstructorOnly System.Linq -r "$DIET_LIB" \
            --link-xml samples/dotnet/ILDietControl/ConstructorOnly/keep-signature-constructed.xml \
            ${local_defaults_flags[@]+"${local_defaults_flags[@]}"}
    done
)

echo "== Integer storage operands and captured field defaults retain their CLI values =="
build_gate_proj gates/fixtures/ildiet-metadata-validation/MetadataValidation.csproj
(
    narrowing_original="samples/dotnet/ILDietControl/ConstructorOnly/bin/$CONFIG/$TFM/ConstructorOnly.dll"
    DN2CPP_SAMPLE_PROJECT_DIR="artifacts/constructor-array-narrowing-$CONFIG"
    narrowing_bin="$DN2CPP_SAMPLE_PROJECT_DIR/bin/$CONFIG/$TFM"
    mkdir -p "$narrowing_bin"
    cp "${narrowing_original%/*}/"*.dll "${narrowing_original%/*}/"*.json "$narrowing_bin/"
    narrowing_fixture=$(dotnet exec "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
        --create-array-narrowing-fixture "$narrowing_original" "$narrowing_bin/ConstructorOnly.dll")
    assert_output "$(strip_cr_win "$narrowing_fixture")" \
        'array-narrowing-fixture=wrap,sign-extension,zero-extension,generic,sequence,copy,captured-array,captured-scalar'
    narrowing_complete_prefix=$(run_bounded dotnet "$narrowing_bin/ConstructorOnly.dll")
    narrowing_fixture=$(dotnet exec "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
        --create-captured-scalar-defaults-fixture "$narrowing_original" "$narrowing_bin/ConstructorOnly.dll")
    assert_output "$(strip_cr_win "$narrowing_fixture")" \
        'captured-scalar-defaults-fixture=byte,int,array-reference,object-reference,generic-reference'
    narrowing_prefix=$(run_bounded dotnet "$narrowing_original")
    narrowing_before=$(DN2CPP_BEFORE_ARRAY_NARROWING=1 run_bounded dotnet "$narrowing_bin/ConstructorOnly.dll")
    assert_output "$(strip_cr_win "$narrowing_before")" "$(strip_cr_win "$narrowing_prefix")"
    DN2CPP_SKIP_BUILD=1
    export DN2CPP_DUMP_REFLECTION_SUMMARY=1
    eval "$(declare -f invoke_cli | sed '1s/invoke_cli/invoke_narrowing_cli/')"
    invoke_cli() {
        local rc=0
        invoke_narrowing_cli "$@" 2> "$narrowing_summary" || rc=$?
        if [ "$rc" -ne 0 ]; then cat "$narrowing_summary" >&2; fi
        return "$rc"
    }
    gate_extra_asserts() {
        local out="$1" native prefix before name section
        native=$(run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        prefix=$(awk '/^== integer storage operands ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$narrowing_prefix")"
        before=$(DN2CPP_BEFORE_ARRAY_NARROWING=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for name in ByteWrap WordWrap ByteSigned ByteUnsigned WordSigned WordUnsigned GenericByte \
                ByteSequence ByteCopy CapturedArray CapturedScalar; do
            grep -Fxq "integer-storage=$name" <<< "$native" \
                || { echo "FAIL: integer storage witness did not run: $name" >&2; return 1; }
            grep -Eqw "Narrowing${name}Target_SelectedBody_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: integer storage lost selected body $name" >&2; return 1; }
            section=$(awk -v name="Narrowing$name" \
                '/^reflection-summary / { on = index($0, "ILDietConstructorOnly.Program::" name " ") != 0; next } on' \
                "$narrowing_summary")
            grep -Eq 'GetMethod/[^ ]+ .* p0="Pick" p1=0 p2=\[\]$' <<< "$section" \
                || { echo "FAIL: integer storage arity is not exactly zero: $name" >&2; echo "$section" >&2; return 1; }
        done
        grep -Fxq 'integer storage operands end' <<< "$native" \
            || { echo 'FAIL: integer storage section did not finish' >&2; return 1; }
        if grep -Eqw 'Narrowing[A-Za-z]+Target_UnselectedBody_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: integer storage retained an unselected generic body' >&2; return 1
        fi
        prefix=$(awk '/^== captured scalar defaults ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$narrowing_complete_prefix")"
        before=$(DN2CPP_BEFORE_CAPTURED_SCALAR_DEFAULTS=1 run_bounded "$out/ConstructorOnly$EXE_EXT") || return $?
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for name in Byte Int ObjectReceiver GenericReceiver; do
            grep -Fxq "integer-storage=Default$name" <<< "$native" \
                || { echo "FAIL: captured scalar default witness did not run: $name" >&2; return 1; }
            grep -Eqw "NarrowingDefault${name}Target_SelectedBody_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: captured scalar default lost selected body $name" >&2; return 1; }
        done
        for name in SelectedDefault SelectedArgument; do
            grep -Eqw "ScalarDefaultArgumentsTarget_${name}_m[0-9]+" "$out"/generated*.cpp "$out"/generated.h \
                || { echo "FAIL: captured null arguments lost constructor body $name" >&2; return 1; }
        done
        if grep -Eqw 'ScalarDefaultArgumentsTarget_UnselectedPair_m[0-9]+' "$out"/generated*.cpp "$out"/generated.h; then
            echo 'FAIL: captured null arguments retained an unselected constructor body' >&2; return 1
        fi
        for marker in scalar-arguments=0 scalar-arguments=1 'captured scalar defaults end'; do
            grep -Fxq "$marker" <<< "$native" \
                || { echo "FAIL: captured scalar default section did not finish: $marker" >&2; return 1; }
        done
        for name in Byte Int; do
            section=$(awk -v name="ScalarDefault$name" \
                '/^reflection-summary / { on = index($0, "ILDietConstructorOnly.Program::" name " ") != 0; next } on' \
                "$narrowing_summary")
            grep -Eq 'GetMethod/[^ ]+ .* p0="Pick" p1=\{(0 \| 1|1 \| 0)\} p2=\[\]$' <<< "$section" \
                || { echo "FAIL: captured scalar lost default zero or finite later store: $name" >&2; echo "$section" >&2; return 1; }
        done
        section=$(awk '/^reflection-summary / { on = index($0, "ILDietConstructorOnly.Program::ScalarDefaultArguments ") != 0; next } on' \
            "$narrowing_summary")
        grep -Eq 'ActivatorCreate/Construct .* p1=\{(null \| \[null\]|\[null\] \| null)\}$' <<< "$section" \
            || { echo 'FAIL: captured array field lost null or finite array arguments' >&2; echo "$section" >&2; return 1; }
        section=$(awk '/^reflection-summary / { on = index($0, "ILDietConstructorOnly.Program::ScalarDefaultObjectReceiver ") != 0; next } on' \
            "$narrowing_summary")
        grep -Eq 'MethodInvoke/Execute .* p0=\{(null \| new\(System.Object\)|new\(System.Object\) \| null)\} p1=null$' <<< "$section" \
            || { echo 'FAIL: captured object field lost null or finite later store' >&2; echo "$section" >&2; return 1; }
        section=$(awk '/^reflection-summary / { on = index($0, "ILDietConstructorOnly.Program::ScalarDefaultGenericReceiver ") != 0; next } on' \
            "$narrowing_summary")
        grep -Eq 'MethodInvoke/Execute .* p0=\{new\(System.Object\) \| \?InstanceField\([^}]+\)\} p1=null$' <<< "$section" \
            || { echo 'FAIL: unsupported captured field default lost finite store or unknown' >&2; echo "$section" >&2; return 1; }

    }
    for narrowing_mode in ildiet no-ildiet; do
        DN2CPP_OUT_SUFFIX="-array-narrowing-$narrowing_mode"
        DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|integer-storage:$narrowing_mode"
        narrowing_summary="$DN2CPP_SAMPLE_PROJECT_DIR/$narrowing_mode-summary.txt"
        narrowing_flags=()
        [ "$narrowing_mode" != no-ildiet ] || narrowing_flags=(--no-ildiet)
        corelib_diff_gate ConstructorOnly System.Linq -r "$DIET_LIB" \
            --link-xml samples/dotnet/ILDietControl/ConstructorOnly/keep-signature-constructed.xml \
            ${narrowing_flags[@]+"${narrowing_flags[@]}"}
    done
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
        stripped=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll") || return $?
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
        app=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/InvokeOnly.dll") || return $?
        app=$(strip_cr_win "$app")
        grep -Fxq 'method ILDietInvokeOnly.ConstructorSignatureTarget`1::Value' <<< "$app" \
            && grep -Fxq 'method ILDietInvokeOnly.LibrarySignatureTarget`1::Get' <<< "$app" \
            || { echo 'FAIL: signature invocation lost a selected method' >&2; return 1; }
    }
    # Invoke(string, Type) runs a method on signature-selected types no analyzable site names.
    corelib_diff_gate InvokeOnly -r "$DIET_LIB" \
        --link-xml "$DN2CPP_SAMPLE_PROJECT_DIR/keep-signature-invoked.xml"
)

echo "== Recognized reflection on user-library base types and constant type names keeps the selected bodies and rows; Assembly.CreateInstance alone keeps the constructors it names; a type argument's assembly lists its generic definitions =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/LibraryOwnerOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    owner_summaries="$STALE_ROOT/library-owner-summaries.stderr"
    eval "$(declare -f invoke_cli | sed '1s/invoke_cli/invoke_library_owner_cli/')"
    invoke_cli() {
        local status=0
        DN2CPP_DUMP_REFLECTION_SUMMARY=1 invoke_library_owner_cli "$@" 2> "$owner_summaries" || status=$?
        grep -Ev '^(reflection-summary |  IL_)' "$owner_summaries" >&2 || true
        return "$status"
    }
    # library_body_count OUT TYPE MEMBER — "<body comments> <distinct mangled symbols>"
    # compiled for ILDietControlLib.TYPE::MEMBER.
    library_body_count() {
        local out="$1" type="$2" member="$3" comments symbols
        comments=$(cat "$out"/generated*.cpp "$out"/generated.h \
            | grep -Ec "^// ILDietControlLib\\.$type::${member}( |\$)" || true)
        symbols=$(cat "$out"/generated*.cpp "$out"/generated.h \
            | { grep -Eow "${type}_${member}_m[0-9]+" || true; } | sort -u | wc -l | tr -d ' ')
        printf '%s %s\n' "$comments" "$symbols"
    }
    gate_extra_asserts() {
        local out="$1" native stripped member before prefix stubs count
        native=$(run_bounded "$out/LibraryOwnerOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        grep -Fxq 'library base members end' <<< "$native" \
            && grep -Fxq 'constant type names end' <<< "$native" \
            || { echo 'FAIL: the library owner sections did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_METADATA_OWNERS=1 run_bounded "$out/LibraryOwnerOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== metadata-only library owners ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for member in 'identity=ILDietControlLib.MetadataIdentityOnly' 'named-method=True' 'named-property=True' \
            'listed=ListedFirst,ListedSecond,get_ListedLabel' 'base-method=True' 'library-base-method=True' \
            'metadata-only owners end'; do
            grep -Fxq -- "$member" <<< "$native" \
                || { echo "FAIL: the metadata-only owner section did not run: $member" >&2; return 1; }
        done
        before=$(DN2CPP_BEFORE_ASSEMBLY_CONSTRUCTION=1 run_bounded "$out/LibraryOwnerOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== assembly-named construction ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for member in 'application=assembly-named-application' 'library=assembly-named-library' \
            'assembly-named construction end'; do
            grep -Fxq -- "$member" <<< "$native" \
                || { echo "FAIL: the assembly-named construction section did not run: $member" >&2; return 1; }
        done
        before=$(DN2CPP_BEFORE_LISTED_OWNERS=1 run_bounded "$out/LibraryOwnerOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== listed library owners ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for member in 'listed types=AssemblyListedAlpha`1,AssemblyListedBeta`2' 'listed owners end'; do
            grep -Fxq -- "$member" <<< "$native" \
                || { echo "FAIL: the listed owner section did not run: $member" >&2; return 1; }
        done
        # Metadata-only sites keep the rows they select as signature-only stubs and
        # their owners' other members are still removed; an invoked selection keeps its body.
        stubs=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll" \
            ILDietControlLib.MetadataNamedTarget::Probe ILDietControlLib.MetadataNamedTarget::get_Label \
            ILDietControlLib.MetadataListedTarget::ListedFirst ILDietControlLib.MetadataListedTarget::ListedSecond \
            ILDietControlLib.MetadataListedTarget::get_ListedLabel ILDietControlLib.MetadataBase::BaseOnly \
            ILDietControlLib.MetadataNamedTarget::Unlooked ILDietControlLib.MetadataBase::BaseUnlooked \
            ILDietControlLib.MetadataIdentityOnly::Hidden ILDietControlLib.NamedOnlyTarget::Run \
            ILDietControlLib.MetadataLibraryBase::LibraryBaseOnly ILDietControlLib.MetadataLibraryBase::LibraryBaseUnlooked) \
            || return $?
        assert_output "$(strip_cr_win "$stubs")" "ILDietControlLib.MetadataNamedTarget::Probe stub=True
ILDietControlLib.MetadataNamedTarget::get_Label stub=True
ILDietControlLib.MetadataListedTarget::ListedFirst stub=True
ILDietControlLib.MetadataListedTarget::ListedSecond stub=True
ILDietControlLib.MetadataListedTarget::get_ListedLabel stub=True
ILDietControlLib.MetadataBase::BaseOnly stub=True
ILDietControlLib.MetadataNamedTarget::Unlooked absent
ILDietControlLib.MetadataBase::BaseUnlooked absent
ILDietControlLib.MetadataIdentityOnly::Hidden absent
ILDietControlLib.NamedOnlyTarget::Run stub=False
ILDietControlLib.MetadataLibraryBase::LibraryBaseOnly stub=True
ILDietControlLib.MetadataLibraryBase::LibraryBaseUnlooked absent"
        count=$(library_body_count "$out" NamedOnlyTarget Run)
        [ "${count% *}" -ge 1 ] && [ "${count#* }" -ge 1 ] \
            || { echo "FAIL: the invoked library body lost its positive control ($count)" >&2; return 1; }
        for member in MetadataNamedTarget::Probe MetadataNamedTarget::get_Label MetadataListedTarget::ListedFirst \
            MetadataListedTarget::ListedSecond MetadataListedTarget::get_ListedLabel MetadataBase::BaseOnly \
            MetadataLibraryBase::LibraryBaseOnly; do
            count=$(library_body_count "$out" "${member%%::*}" "${member#*::}")
            [ "$count" = '0 0' ] \
                || { echo "FAIL: a metadata-only site compiled $member ($count)" >&2; return 1; }
        done
        stripped=$(dotnet exec "$PROBE" "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll") || return $?
        stripped=$(strip_cr_win "$stripped")
        for member in AppFacingBase::BaseDescribe AppFacingBase::get_BaseLabel \
            ReflectedOwnerBase::InheritedDescribe NamedOnlyTarget::Run 'NamedOnlyBox`1::Describe'; do
            grep -Fxq "method ILDietControlLib.$member" <<< "$stripped" \
                || { echo "FAIL: preprocessing dropped the selected library member $member" >&2; return 1; }
        done
        # A name selects bodies; the owners' other members are still removed.
        for member in AppFacingBase::UnselectedAppFacing ReflectedOwnerBase::UnselectedLibraryBase \
            NamedOnlyTarget::UnselectedNamed; do
            if grep -Fxq "method ILDietControlLib.$member" <<< "$stripped"; then
                echo "FAIL: preprocessing kept the unselected library member $member" >&2; return 1
            fi
        done
    }
    corelib_diff_gate LibraryOwnerOnly -r "$DIET_LIB"
    # The summary dump names each BindingFlags bit by its value, past the unused 0x80.
    construction_summary=$(awk '/^reflection-summary LibraryOwnerOnly ILDietLibraryOwnerOnly\.Program::AssemblyConstruction / { on = 1; next }
        /^reflection-summary / { on = 0 } on' "$owner_summaries")
    grep -Eq ' AssemblyCreateInstance/Construct .* p2=Instance\|Public\|CreateInstance( |$)' <<< "$construction_summary" \
        || { echo 'FAIL: the reflection summary dump misnamed the CreateInstance binding flags' >&2; exit 1; }
)

echo "== A static field read through a constant-named derived owner runs its base's initializer through preprocessing when nothing else uses either type =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/InheritedStaticOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native member stubs
        native=$(run_bounded "$out/InheritedStaticOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        for member in 'library=42' 'application=41' 'inherited static fields end'; do
            grep -Fxq -- "$member" <<< "$native" \
                || { echo "FAIL: the inherited static field section did not run: $member" >&2; return 1; }
        done
        stubs=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll" \
            ILDietControlLib.InheritedStaticBase::.cctor) || return $?
        assert_output "$(strip_cr_win "$stubs")" "ILDietControlLib.InheritedStaticBase::.cctor stub=False"
        stubs=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$out/ildiet")/InheritedStaticOnly.dll" \
            ILDietInheritedStaticOnly.AppStaticBase::.cctor) || return $?
        assert_output "$(strip_cr_win "$stubs")" "ILDietInheritedStaticOnly.AppStaticBase::.cctor stub=False"
    }
    corelib_diff_gate InheritedStaticOnly -r "$DIET_LIB"
)

echo "== A conditional descriptor on an application or library type used only through a constant name keeps its members' bodies through preprocessing =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/ConditionalNamedOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native member stubs
        native=$(run_bounded "$out/ConditionalNamedOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        for member in 'application=Bar' 'library=Run' 'conditional constant-named owners end'; do
            grep -Fxq -- "$member" <<< "$native" \
                || { echo "FAIL: the conditional constant-named section did not run: $member" >&2; return 1; }
        done
        stubs=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$out/ildiet")/ConditionalNamedOnly.dll" \
            ILDietConditionalNamedOnly.NamedApplication::Bar ILDietConditionalNamedOnly.NamedApplication::.ctor) \
            || return $?
        assert_output "$(strip_cr_win "$stubs")" "ILDietConditionalNamedOnly.NamedApplication::Bar stub=False
ILDietConditionalNamedOnly.NamedApplication::.ctor stub=False"
        stubs=$(dotnet exec "$PROBE" --method-stubs "$(ildiet_generation "$out/ildiet")/ILDietControlLib.dll" \
            ILDietControlLib.ConditionalNamedTarget::Run) || return $?
        assert_output "$(strip_cr_win "$stubs")" "ILDietControlLib.ConditionalNamedTarget::Run stub=False"
    }
    corelib_diff_gate ConditionalNamedOnly -r "$DIET_LIB" \
        --link-xml "$DN2CPP_SAMPLE_PROJECT_DIR/keep-conditional-named.xml"
)

fi

echo "== Lookups, unselected overloads, unrelated field types, preserved constructors, untouched types, joined names' unresolved alternatives, formed generic types and attribute metadata reads compile no bodies =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/AbsenceOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local out="$1" native before prefix
        native=$(run_bounded "$out/AbsenceOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        before=$(DN2CPP_BEFORE_RETAINED_ABSENCE=1 run_bounded "$out/AbsenceOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== unrequested bodies stay absent ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        for row in 'lookup=LookedUp:String/LookedUpField:LookupFieldValue' 'overload=int:7' \
            'unused-type=UnusedStatics' 'unrequested bodies stay absent end' 'joined=known' 'joined names end' \
            'defined=True' 'data=[ILDietAbsenceOnly.DataOnlyAttribute("data")]' 'filtered=requested' \
            'attribute metadata reads end' 'formed=FormedOnly`1' 'constructed=constructed:Int32' 'formed generic types end' \
            'framework-defined=True' 'framework-data=[System.Diagnostics.DebuggerDisplayAttribute("metadata-only")]' \
            'framework-instance=Selected' 'framework attribute metadata reads end'; do
            grep -Fxq -- "$row" <<< "$native" \
                || { echo "FAIL: unrequested body absence section did not run: $row" >&2; return 1; }
        done
    }
    # absence_definitions OUT TYPE MEMBER — "<body comments> <distinct mangled symbols>"
    # compiled for ILDietAbsenceOnly.TYPE::MEMBER; MEMBER '*' counts every member.
    absence_definitions() {
        local out="$1" type="$2" member="$3" comment symbol comments symbols
        if [ "$member" = '*' ]; then
            comment='[^ ]+' symbol='[A-Za-z0-9_]+'
        else
            comment="${member//./\\.}" symbol="${member//./_}"
        fi
        comments=$(cat "$out"/generated*.cpp "$out"/generated.h \
            | grep -Ec "^// ILDietAbsenceOnly\\.$type::$comment( |\$)" || true)
        symbols=$(cat "$out"/generated*.cpp "$out"/generated.h \
            | { grep -Eow "${type}_${symbol}_m[0-9]+" || true; } | sort -u | wc -l | tr -d ' ')
        printf '%s %s\n' "$comments" "$symbols"
    }
    # Each absent body is paired with a present body or row of the same case, so an
    # emission that changed shape cannot pass the absences vacuously.
    assert_absence_case() {
        local out="$1" label="$2" absent="$3" present="$4" type member count
        for type in $present; do
            member="${type#*::}" type="${type%%::*}"
            count=$(absence_definitions "$out" "$type" "$member")
            [ "${count% *}" -ge 1 ] && [ "${count#* }" -ge 1 ] \
                || { echo "FAIL: $label lost its positive control $type::$member ($count)" >&2; exit 1; }
        done
        for type in $absent; do
            member="${type#*::}" type="${type%%::*}"
            count=$(absence_definitions "$out" "$type" "$member")
            [ "$count" = '0 0' ] \
                || { echo "FAIL: $label compiled $type::$member ($count)" >&2; exit 1; }
        done
    }
    absence_keep="$DN2CPP_SAMPLE_PROJECT_DIR/keep-preserved-constructor.xml"
    for preprocessing in enabled disabled; do
        [ "$preprocessing" = enabled ] || preserve_full || continue
        absence_flags=()
        DN2CPP_OUT_SUFFIX=
        if [ "$preprocessing" = disabled ]; then
            absence_flags=(--no-ildiet)
            DN2CPP_OUT_SUFFIX=-no-ildiet
        fi
        corelib_diff_gate AbsenceOnly --link-xml "$absence_keep" ${absence_flags[@]+"${absence_flags[@]}"}
        out="$_CG_OUT"
        # A lookup keeps the member's row; the body and its callees stay out.
        for row in methtab_ILDietAbsenceOnly_LookupTarget fldtab_ILDietAbsenceOnly_LookupTarget; do
            grep -qw "$row" "$out"/generated* \
                || { echo "FAIL: a lookup-only member lost its metadata row: $row" >&2; exit 1; }
        done
        assert_absence_case "$out" "a lookup without invoke" \
            'LookupTarget::LookedUp LookupBodyOnly::* LookupFieldValue::*' 'LookupTarget::Called'
        # The Type[] filter selects Pick(int); Pick(string) is the sibling.
        [ "$(absence_definitions "$out" OverloadTarget Pick)" = '1 1' ] \
            || { echo "FAIL: an unselected sibling overload compiled: $(absence_definitions "$out" OverloadTarget Pick)" >&2; exit 1; }
        # Reading Selected boxes only Selected's type; Other and Payload are never boxed or allocated.
        assert_absence_case "$out" "an unrelated field type" \
            'UnrelatedValue::* UnrelatedPayload::*' 'SelectedValue::ToString'
        grep -qw tsthunk_ILDietAbsenceOnly_SelectedValue "$out"/generated* \
            && grep -qw fldget_ILDietAbsenceOnly_FieldOwner_f_Other "$out"/generated* \
            || { echo "FAIL: the reflected field owner lost its boxed field type or field row" >&2; exit 1; }
        if grep -Eqw '(tsthunk|ghthunk|eqthunk)_ILDietAbsenceOnly_(UnrelatedValue|LookupFieldValue)' "$out"/generated*; then
            echo "FAIL: an unreflected field's value type gained boxed-object thunks" >&2; exit 1
        fi
        # A preserved constructor allocates its type; only interface slots the program
        # calls dispatch to its bodies.
        assert_absence_case "$out" "a preserved constructor's type" \
            'PreservedService::Run' 'PreservedService::.ctor PreservedService::Use'
        # typeof names a type without triggering its initializer.
        assert_absence_case "$out" "an untouched type initializer" \
            'UnusedStatics::.cctor' 'UsedStatics::.cctor'
        grep -qw ti_ILDietAbsenceOnly_UnusedStatics "$out"/generated* \
            && grep -Eqw 'UsedStatics__cctor_m[0-9]+__done' "$out"/generated* \
            && grep -Fq '"ILDietAbsenceOnly.UsedStatics")' "$out"/generated* \
            || { echo "FAIL: the type initializer witnesses lost their type or guard" >&2; exit 1; }
        if grep -Eqw 'UnusedStatics__cctor_m[0-9]+(__done|__ensure)' "$out"/generated* \
                || grep -Fq '"ILDietAbsenceOnly.UnusedStatics")' "$out"/generated*; then
            echo "FAIL: an untouched type initializer kept a guard or startup root" >&2; exit 1
        fi
        # A name joined from a constant and an unresolved value admits the constant's
        # target and still reports the site for the unresolved one.
        assert_absence_case "$out" "a joined name with an unresolved alternative" \
            'JoinedTarget::Unnamed' 'JoinedTarget::Known'
        awk -F '\t' '$3 == "ILDietAbsenceOnly.Program::RunJoined()" { found = 1 } END { exit !found }' \
                "$out/reflection-unrecognized.tsv" \
            || { echo "FAIL: the joined name's unresolved alternative was not reported" >&2; exit 1; }
        # IsDefined and CustomAttributeData answer from rows alone; a typed instance read
        # constructs only the attribute type it names.
        assert_absence_case "$out" "an attribute metadata read" \
            'DefinedOnlyAttribute::.ctor DataOnlyAttribute::.ctor UnrequestedAttribute::.ctor' 'RequestedAttribute::.ctor'
        # Forming a generic type selects no constructor or method of its runtime
        # template; constructing the sibling selects its constructor only.
        assert_absence_case "$out" "a formed generic type" \
            'FormedOnly_[$A-Za-z0-9]+::* FormedOnlyHelper::* ConstructedTemplate_[$A-Za-z0-9]+::Unasked' \
            'ConstructedTemplate_[$A-Za-z0-9]+::.ctor ConstructedTemplateHelper::.cctor'
        # A framework attribute's metadata read renders its row without constructing it;
        # the typed instance read constructs the attribute it names.
        generated=$(cat "$out"/generated*.cpp "$out"/generated.h)
        grep -Eq '^// System\.Reflection\.DefaultMemberAttribute::\.ctor( |$)' <<< "$generated" \
            || { echo 'FAIL: a framework attribute instance read lost its positive control' >&2; exit 1; }
        if grep -Eq '^// System\.Diagnostics\.DebuggerDisplayAttribute::' <<< "$generated" \
                || grep -Eqw 'DebuggerDisplayAttribute__ctor_m[0-9]+' <<< "$generated"; then
            echo 'FAIL: a framework attribute metadata read compiled its constructor' >&2; exit 1
        fi
    done
)

echo "== A reflection target from a parameter, a collection, a written-back argument array, a boxed type argument, an array type over a parameter or a runtime member name, an omitted or never-boxed constructed type, a runtime-made instantiation's member, a parameter type's static read-only field initializer, a name bound per instantiation or an uninitialized parameter type warns and refuses until its printed remedy preserves it; a virtual delegate's unknown method needs its attribute constructor preserved =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ILDietControl/UnknownTargetOnly
    DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    unknown_expected=gates/expected/preserve-control-unknown-target.txt
    unknown_rows=gates/expected/preserve-control-unknown-target.tsv
    unknown_keep="$DN2CPP_SAMPLE_PROJECT_DIR/keep-parameter-targets.xml"
    unknown_library="$DN2CPP_SAMPLE_PROJECT_DIR/bin/$CONFIG/$TFM/ActivationTargets.dll"
    unknown_warnings="$STALE_ROOT/unknown-target.stderr"
    unknown_attribute_kept=false
    eval "$(declare -f invoke_cli | sed '1s/invoke_cli/invoke_unknown_target_cli/')"
    invoke_cli() {
        local status=0
        invoke_unknown_target_cli "$@" 2> "$unknown_warnings" || status=$?
        cat "$unknown_warnings" >&2
        return "$status"
    }
    gate_extra_asserts() {
        local out="$1" native before prefix
        native=$(run_bounded "$out/UnknownTargetOnly$EXE_EXT") || return $?
        native=$(strip_cr_win "$native")
        before=$(DN2CPP_BEFORE_UNKNOWN_TARGET=1 run_bounded "$out/UnknownTargetOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== reflection targets from parameters ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'reflection targets from parameters end' <<< "$native" \
            || { echo 'FAIL: the parameter-target section did not run' >&2; return 1; }
        grep -Fxq 'reflection targets from collections end' <<< "$native" \
            || { echo 'FAIL: the collection-target section did not run' >&2; return 1; }
        grep -Fxq 'construction of omitted and never-boxed types end' <<< "$native" \
            || { echo 'FAIL: the construction section did not run' >&2; return 1; }
        grep -Fxq 'reflection targets on runtime-made instantiations end' <<< "$native" \
            || { echo 'FAIL: the runtime-made instantiation section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_WRITTEN_TARGET=1 run_bounded "$out/UnknownTargetOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== reflection targets written back or boxed from a type argument ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        # A value-type argument's box keeps its exact type, so its target is recognized.
        grep -Fxq 'boxed value=value' <<< "$native" \
            && grep -Fxq 'reflection targets written back or boxed from a type argument end' <<< "$native" \
            || { echo 'FAIL: the written-back and boxed-argument section did not run' >&2; return 1; }
        grep -Fxq 'reflection targets through an array type or a runtime name end' <<< "$native" \
            || { echo 'FAIL: the array-type and runtime-name section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_INITONLY_FIELD=1 run_bounded "$out/UnknownTargetOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== a static readonly field set on a parameter type ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        grep -Fxq 'a static readonly field set on a parameter type end' <<< "$native" \
            || { echo 'FAIL: the static read-only field section did not run' >&2; return 1; }
        before=$(DN2CPP_BEFORE_DISPATCHED_TARGET=1 run_bounded "$out/UnknownTargetOnly$EXE_EXT") || return $?
        prefix=$(awk '/^== a virtual delegate.s method, a name bound per instantiation and an uninitialized parameter type ==$/ { exit } { print }' <<< "$native")
        assert_output "$prefix" "$(strip_cr_win "$before")"
        if [ "$unknown_attribute_kept" = true ]; then
            grep -Fxq 'virtual command=derived' <<< "$native" \
                || { echo 'FAIL: the preserved attribute constructor did not serve the unknown virtual owner' >&2; return 1; }
        else
            grep -q '^virtual command refused: PlatformNotSupportedException: ' <<< "$native" \
                || { echo 'FAIL: an unknown virtual owner constructed an unpreserved attribute' >&2; return 1; }
        fi
        grep -Fxq "a virtual delegate's method, a name bound per instantiation and an uninitialized parameter type end" <<< "$native" \
            || { echo 'FAIL: the virtual-delegate, per-instantiation and uninitialized-type section did not run' >&2; return 1; }
        # A definition's member rule runs on the clone its template makes; it mints no
        # closed instantiation.
        if grep -Eq '^extern const Dn2CppTypeInfo ti_ILDietUnknownTargetOnly_TemplateTarget_[A-Za-z]' "$out/generated.h"; then
            echo 'FAIL: the runtime-made instantiation was compiled as a closed type' >&2; return 1
        fi
    }
    # A preserve cannot make an unknown value known: both arms report the same sites,
    # once on stderr with the TSV's fix text and once in the TSV.
    assert_unknown_target_report() {
        local out="$1" warnings kind assembly method offset api reason fix rows=0
        assert_output "$(strip_cr_win "$(cat "$out/reflection-unrecognized.tsv")")" "$(cat "$unknown_rows")"
        warnings=$(strip_cr_win "$(cat "$unknown_warnings")")
        while IFS=$'\t' read -r kind assembly method offset api reason fix; do
            rows=$((rows + 1))
            grep -Fxq -- "dn2cpp: warning: reflection target not recognized: $assembly $method $offset $api: $reason" <<< "$warnings" \
                && grep -Fxq -- "  fix: $fix" <<< "$warnings" \
                || { echo "FAIL: stderr does not report the $kind site $method $offset" >&2; exit 1; }
        done < "$unknown_rows"
        [ "$(grep -c '^dn2cpp: warning: reflection target not recognized: ' <<< "$warnings")" = "$rows" ] \
            && grep -Fq "dn2cpp: reflection: $rows unrecognized sites ($rows user, 0 framework, 0 backend-covered) -> " <<< "$warnings" \
            || { echo "FAIL: stderr reported other reflection sites than the TSV" >&2; exit 1; }
    }
    corelib_freeze_gate UnknownTargetOnly "$unknown_expected" System.Linq -r "$unknown_library"
    assert_unknown_target_report "$_CG_OUT"
    # Each refusal's remedy is the preserve the second arm passes, verbatim.
    # Ordinary member fixes fill in their build-time placeholders; the cases below
    # check where runtime identity makes the remedy more specific or boxing widens it.
    remedies=$(sed -n 's/^.* refused: PlatformNotSupportedException: .*; fix: \[Preserve\] on the target, or link\.xml //p' "$unknown_expected")
    [ "$(wc -l <<< "$remedies" | tr -d ' ')" = 19 ] \
        || { echo "FAIL: the frozen refusals do not print every remedy" >&2; exit 1; }
    diff -u <(sort -u <<< "$remedies") <(grep -o '<assembly .*</assembly>' "$unknown_keep" | sort) \
        || { echo "FAIL: the preserve is not the set of printed remedies" >&2; exit 1; }
    [ "$(grep -c '^unboxed struct refused: .*, which this image never allocates; fix: .* preserve="all" /></assembly>$' "$unknown_expected")" = 1 ] \
        && [ "$(grep -c ' preserve="all" ' <<< "$remedies")" = 1 ] \
        || { echo "FAIL: only the never-boxed struct's refusal preserves its whole type" >&2; exit 1; }
    # SetValue on a static read-only field runs its owner's initializer before it
    # rejects the field, so the refusal names that initializer.
    grep -q '^initonly field refused: PlatformNotSupportedException: ILDietUnknownTargetOnly\.InitOnlyTarget::\.cctor in UnknownTargetOnly: ' "$unknown_expected" \
        || { echo "FAIL: the static read-only field refusal does not name its type initializer" >&2; exit 1; }
    # A runtime member name leaves only a generated owner known: its build-time fix
    # preserves the whole type while its refusal names the member, and both escape the name.
    generated_owner='<assembly fullname="UnknownTargetOnly"><type fullname="&lt;&gt;f__AnonymousType0`1"'
    [ "$(grep -Fc "$generated_owner preserve=\"all\" /></assembly>" "$unknown_rows")" = 1 ] \
        && [ "$(grep -Fc "$generated_owner><method name=\"get_Value\" /></type></assembly>" <<< "$remedies")" = 1 ] \
        || { echo "FAIL: a generated owner's fix or remedy does not escape its name" >&2; exit 1; }
    # One site bound per instantiation fixes every bound owner, in ordinal order, while
    # each instantiation's refusal names its own member.
    merged_owners='<assembly fullname="UnknownTargetOnly"><type fullname="ILDietUnknownTargetOnly.MergedA" preserve="all" /></assembly><assembly fullname="UnknownTargetOnly"><type fullname="ILDietUnknownTargetOnly.MergedB" preserve="all" /></assembly>'
    [ "$(grep -c 'Program::BindNamed(' "$unknown_rows")" = 1 ] \
        && [ "$(grep -Fc "$merged_owners" "$unknown_rows")" = 1 ] \
        && [ "$(grep -Ec 'ILDietUnknownTargetOnly\.Merged[AB]"><method name="Run" />' <<< "$remedies")" = 2 ] \
        || { echo "FAIL: a site bound per instantiation does not fix every owner" >&2; exit 1; }
    # An uninitialized instance of a parameter type nothing else allocates would run its
    # base's ToString, so it refuses with its site's constructor fix; a copied list's one
    # site refuses once per element.
    [ "$(awk -F'\t' '$3 == "ILDietUnknownTargetOnly.Program::Blank(System.Type)" && index($6, "Type comes from parameter 1 ") == 1 && $6 ~ /no type allocated$/' "$unknown_rows" | wc -l | tr -d ' ')" = 1 ] \
        && grep -q '^blank refused: PlatformNotSupportedException: ILDietUnknownTargetOnly\.BlankTarget in UnknownTargetOnly: GetUninitializedObject makes an instance of ILDietUnknownTargetOnly\.BlankTarget, which this image never allocates; fix: ' "$unknown_expected" \
        && [ "$(grep -c '^copy refused: ' "$unknown_expected")" = 2 ] \
        || { echo "FAIL: the uninitialized-type refusal or the copied list's refusals changed" >&2; exit 1; }
    # An unknown virtual attribute owner gets a generic build-time fix. The runtime
    # row identifies its constructor, so preserving only that member is sufficient.
    [ "$(awk -F'\t' '$1 == "AttributeInstances" && $3 == "ILDietUnknownTargetOnly.Program::VirtualCommand(ILDietUnknownTargetOnly.CommandBase)" && $7 == "[Preserve] on the target, or link.xml <assembly fullname=\"ASSEMBLY\"><type fullname=\"TYPE\" preserve=\"all\" /></assembly>"' "$unknown_rows" | wc -l | tr -d ' ')" = 1 ] \
        && [ "$(grep -Fxc '<assembly fullname="UnknownTargetOnly"><type fullname="ILDietUnknownTargetOnly.CommandAttribute"><method name=".ctor" /></type></assembly>' <<< "$remedies")" = 1 ] \
        || { echo 'FAIL: the unknown attribute remedy does not name only its constructor' >&2; exit 1; }
    diff -u <(grep -Fv "$generated_owner" "$unknown_rows" | grep -Fv 'Program::BindNamed(' | grep -Fv 'Program::VirtualCommand(' | cut -f7 | sed 's/^\[Preserve\] on the target, or link\.xml //' | sort) \
        <(grep -Fv "$generated_owner" <<< "$remedies" | grep -Ev 'ILDietUnknownTargetOnly\.(MergedA|MergedB|AddedTarget|CommandAttribute)"' | sed 's/<assembly fullname="[^"]*">/<assembly fullname="ASSEMBLY">/; s/<type fullname="[^"]*">/<type fullname="TYPE">/; s|<type fullname="[^"]*" preserve="all" />|<type fullname="TYPE"><method name=".ctor" /></type>|' | sort) \
        || { echo "FAIL: a build-time fix does not name the member its refusal names" >&2; exit 1; }
    DN2CPP_OUT_SUFFIX=-kept
    unknown_attribute_kept=true
    corelib_diff_gate UnknownTargetOnly --link-xml "$unknown_keep" System.Linq -r "$unknown_library"
    assert_unknown_target_report "$_CG_OUT"
    grep -Fxq '// ILDietUnknownTargetOnly.TemplateTarget_$CnAny0::Describe' "$_CG_OUT"/generated*.cpp \
        || { echo 'FAIL: the member rule did not compile the runtime-instantiation template body' >&2; exit 1; }
)

echo "== Reflected event subscription retains accessor bodies through preprocessing =="
EVENT_APP="samples/dotnet/ILDietControl/EventBoundary/bin/$CONFIG/$TFM/EventBoundary.dll"
EVENT_DIET="$STALE_ROOT/event-boundary"
dotnet exec "$ILD_DLL" "$EVENT_APP" -r "$_CG_CORELIB" -o "$EVENT_DIET"
event_original=$(run_bounded dotnet "$EVENT_APP")
event_original=$(strip_cr_win "$event_original")
event_stripped=$(run_bounded dotnet exec --runtimeconfig "${EVENT_APP%.dll}.runtimeconfig.json" "$(ildiet_generation "$EVENT_DIET")/EventBoundary.dll")
event_stripped=$(strip_cr_win "$event_stripped")
assert_output "$(strip_cr_win "$event_stripped")" "$(strip_cr_win "$event_original")"
grep -Fxq 'event-operations=done' <<< "$event_stripped" \
    || { echo 'FAIL: reflected event subscription section did not run' >&2; exit 1; }
for preprocessing in enabled disabled; do
    [ "$preprocessing" = enabled ] || preserve_full || continue
    event_flags=()
    [ "$preprocessing" = disabled ] && event_flags=(--no-ildiet)
    event_out="${STALE_ROOT#"$PWD/"}/event-native-$preprocessing"
    invoke_cli "$EVENT_APP" -r "$_CG_CORELIB" ${event_flags[@]+"${event_flags[@]}"} -o "$event_out"
    compile_console "$event_out" EventBoundary
    event_native=$(run_bounded "$event_out/EventBoundary$EXE_EXT")
    assert_output "$(strip_cr_win "$event_native")" "$event_original"
done

echo "== Failed stripping preserves the last complete output and unrelated directories =="
TRANSACTION="$STALE_ROOT/transaction"
dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" -r "$RUNTIME_DLL" \
    --link-xml samples/dotnet/ILDietControl/keep-attribute-constructed.xml -o "$TRANSACTION"
compare_dlls "$(ildiet_generation "$DIET_OUT/ildiet")" "$(ildiet_generation "$TRANSACTION")"
transaction_generation=$(ildiet_generation "$TRANSACTION")
transaction_hashes=$(shasum -a 256 "$TRANSACTION/ildiet-result.xml" "$transaction_generation/"*.dll "$transaction_generation/preservation.xml")
printf '<linker><assembly' > "$STALE_ROOT/broken.xml"
OCCUPIED="$STALE_ROOT/occupied"
mkdir -p "$OCCUPIED"
printf 'unrelated-user-data' > "$OCCUPIED/keep.txt"
set +e
failed_output=$(dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" \
    -r "$RUNTIME_DLL" --link-xml samples/dotnet/ILDietControl/keep-attribute-constructed.xml \
    --link-xml "$STALE_ROOT/broken.xml" -o "$TRANSACTION" 2>&1)
failed_code=$?
occupied_output=$(dotnet exec "$ILD_DLL" "$DIET_APP" -r "$_CG_CORELIB" -r "$DIET_LIB" \
    -r "$RUNTIME_DLL" --link-xml samples/dotnet/ILDietControl/keep-attribute-constructed.xml -o "$OCCUPIED" 2>&1)
occupied_code=$?
set -e
[ "$failed_code" -ne 0 ] && grep -q 'broken.xml' <<<"$failed_output" \
    || { echo "FAIL: malformed XML did not fail before publication" >&2; exit 1; }
[ "$transaction_hashes" = "$(shasum -a 256 "$TRANSACTION/ildiet-result.xml" "$transaction_generation/"*.dll "$transaction_generation/preservation.xml")" ] \
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
compare_dlls "$(ildiet_generation "$OUT/ildiet")" "$(ildiet_generation "$CONSOLE_ILD")"

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

echo "== Definition graph scans, fallback and immutable publication =="
GRAPH_OUTPUT="$(mktemp -d "artifacts/ildiet-graph-tests-$CONFIG.XXXXXX")"
echo "Definition graph evidence: $GRAPH_OUTPUT"
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PYTHON gates/fixtures/ildiet-metadata-validation/check-graph.py "$ILD_DLL" \
    "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    "$_CG_CORELIB" "$GRAPH_OUTPUT"

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

echo "== A type initializer runs only after a type-use trigger or an explicit preserve =="
# Neither an untouched type nor a signature-only rewrite yields a startup root.
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
        case "$initializer_case" in signature|explicit) ;; *) preserve_full || continue ;; esac
        DN2CPP_SAMPLE_PROJECT_DIR="artifacts/ildiet-metadata-tests-$CONFIG/initializer-fixtures/$initializer_case/native"
        DN2CPP_OUT_SUFFIX="-initializer-$initializer_case"
        initializer_keep=()
        [ "$initializer_case" != explicit ] \
            || initializer_keep=(--link-xml "$DN2CPP_SAMPLE_PROJECT_DIR/link.xml")
        corelib_diff_gate InitializerFixture System.Console --no-ildiet ${initializer_keep[@]+"${initializer_keep[@]}"}
    done
)

echo "OK"
