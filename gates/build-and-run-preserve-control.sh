#!/usr/bin/env bash
# Explicit stripping-control gate: built-in/custom/derived PreserveAttribute and
# recursively merged Unity-format link.xml retain otherwise unreachable bodies.
source "$(dirname "$0")/_common.sh"

PROJECT=PreserveControl
ROOT="samples/dotnet/$PROJECT"
LIBPROJECT=samples/dotnet/PreserveControlLib
ASSEMBLYPROJECT=samples/dotnet/PreserveAssemblyLib

echo "== Building app and checking both Runtime target frameworks =="
build_proj "$ROOT/$PROJECT.csproj"
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
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
OUT=artifacts/preserve-control
mkdir -p artifacts
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
transpile=$(invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --trim-reflection \
    --project-root "$ROOT" --project-root "$ROOT/./" --project-root "$STALE_ROOT" \
    --link-feature com -o "$OUT" 2>&1)
printf '%s\n' "$transpile"
grep -q "NoSuchType" <<<"$transpile" \
    || { echo "FAIL: a missing type did not produce a warning naming the target" >&2; exit 1; }
grep -q "NoSuch&Member" <<<"$transpile" \
    || { echo "FAIL: a missing member did not warn with its decoded entity" >&2; exit 1; }
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

echo "== Assembly element without children preserves the whole assembly =="
WHOLE=artifacts/preserve-control-whole
invoke_cli "$APP" -r "$LIBDLL" -r "$ASSEMBLYDLL" --auto-ref --project-root "$ROOT" \
    --link-feature remoting -o "$WHOLE"
grep -Fq "_DroppedMethod_" "$WHOLE"/generated* \
    || { echo "FAIL: childless assembly descriptor did not preserve all methods" >&2; exit 1; }

if gate_cache_check "$OUT" "preserve-control|com" \
        "$APP" "$LIBDLL" "$ASSEMBLYDLL" "$ROOT/link.xml" "$ROOT/Nested/link.xml" \
        gates/expected/preserve-control.txt; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    native=$("./$OUT/$PROJECT")
    assert_output "$(strip_cr_win "$native")" "$(cat gates/expected/preserve-control.txt)"
    gate_cache_commit
fi

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

echo "OK"
