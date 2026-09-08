#!/usr/bin/env bash
# Managed DLL stripping and explicit preservation: unreachable metadata is removed,
# while PreserveAttribute and merged Unity-format link.xml keep selected bodies.
source "$(dirname "$0")/_common.sh"
PYTHON=$(resolve_python) || gate_skip "no working Python 3 interpreter for ILDiet validation"

PROJECT=PreserveControl
ROOT="samples/dotnet/$PROJECT"
LIBPROJECT=samples/dotnet/PreserveControlLib
ASSEMBLYPROJECT=samples/dotnet/PreserveAssemblyLib

echo "== Building app and checking both Runtime target frameworks =="
build_proj "$ROOT/$PROJECT.csproj"
build_proj samples/dotnet/ILDietControl/ILDietControl.csproj
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
build_gate_proj gates/fixtures/preserve-control/MetadataProbe.csproj
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
if grep -Fxq 'type PreserveControl.UnusedAppType' <<<"$stripped_app"; then
    echo "FAIL: an unused public application type survived ILDiet" >&2; exit 1
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

if gate_cache_check "$OUT" "preserve-control|ildiet|com|trim-reflection|cut=PreserveControlLib.UnusedType::UnusedMethod" \
        "$APP" "$LIBDLL" "$ASSEMBLYDLL" "$ROOT/link.xml" "$ROOT/Nested/link.xml" \
        gates/expected/preserve-control.txt; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    native=$("./$OUT/$PROJECT")
    assert_output "$(strip_cr_win "$native")" "$(cat gates/expected/preserve-control.txt)"
    gate_cache_commit
fi

echo "== IL dispatch, initialization and layout retain .NET behavior after stripping =="
DIET_LIB="samples/dotnet/ILDietControlLib/bin/$CONFIG/$TFM/ILDietControlLib.dll"
corelib_diff_gate ILDietControl -r "$DIET_LIB"
DIET_OUT="$_CG_OUT"
DIET_APP="$_CG_APP"
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
        'type ILDietControlLib.UnusedType'; do
    if grep -Fxq "$row" <<<"$diet_metadata"; then
        echo "FAIL: unused ordinary library code survived: $row" >&2; exit 1
    fi
done
[ -f "${DIET_LIB%.dll}.pdb" ] && [ ! -e "$DIET_OUT/ildiet/ILDietControlLib.pdb" ] \
    || { echo "FAIL: rewritten DLL retained stale debug symbols" >&2; exit 1; }

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

echo "OK"
