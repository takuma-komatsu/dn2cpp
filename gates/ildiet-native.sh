#!/usr/bin/env bash
# Opt-in native ILDiet companion build and managed/native stripping validation.
source "$(dirname "$0")/_common.sh"

stage="${1:-build}"
case "$stage" in
    emit|build|verify) ;;
    *) echo "usage: $0 [emit|build|verify]" >&2; exit 1 ;;
esac
if [ "$stage" = verify ]; then
    PYTHON=$(resolve_python) || gate_skip "no working Python 3 interpreter for ILDiet native parity"
fi

mkdir -p artifacts
OUT="$(mktemp -d "artifacts/ildiet-native-$CONFIG.XXXXXX")"
echo "ILDiet native probe artifacts: $OUT"

build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj 2>&1 | tee "$OUT/managed-build.log"
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
DIET="src/ILDiet/bin/$CONFIG/$TFM"
corelib="$(resolve_net10_corelib)"

# Use original IL. ILDiet supplies no signing key and never re-signs outputs;
# Cecil's signing branches are unreachable, while deterministic MVID hashing runs.
dotnet exec "$BIN/dn2cpp.dll" "$DIET/ILDiet.dll" \
    -r "$DIET/Mono.Cecil.dll" -r "$corelib" \
    --cut 'Mono.Cecil.CryptoService::GetPublicKey' \
    --cut 'Mono.Cecil.CryptoService::StrongName' \
    --auto-ref --no-ildiet -o "$OUT" 2>&1 | tee "$OUT/emit.log"
[ -f "$OUT/generated.cpp" ] || { echo "error: no generated.cpp" >&2; exit 1; }
[ "$stage" != emit ] || exit 0

compile_console "$OUT" ILDiet 2>&1 | tee "$OUT/native-build.log"
dotnet exec "$DIET/ILDiet.dll" --help > "$OUT/managed-help.txt" 2> "$OUT/managed-help.stderr.log"
"$OUT/ILDiet" --help > "$OUT/native-help.txt" 2> "$OUT/native-help.stderr.log"
diff -u "$OUT/managed-help.txt" "$OUT/native-help.txt"
if [ "$stage" = build ]; then
    echo "OK: native ILDiet starts and reproduces managed help; stripping parity remains unproven"
    exit 0
fi

build_proj samples/dotnet/ILDietControl/ILDietControl.csproj 2>&1 | tee "$OUT/fixture-build.log"
build_gate_proj gates/fixtures/preserve-control/MetadataProbe.csproj 2>&1 | tee "$OUT/probe-build.log"
build_gate_proj gates/fixtures/ildiet-metadata-validation/MetadataValidation.csproj \
    2>&1 | tee "$OUT/metadata-validation-build.log"
APP="samples/dotnet/ILDietControl/bin/$CONFIG/$TFM"
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PYTHON gates/fixtures/ildiet-native/check.py "$DIET/ILDiet.dll" "$OUT/ILDiet" \
    "gates/fixtures/preserve-control/bin/$CONFIG/$TFM/MetadataProbe.dll" \
    "$APP/ILDietControl.dll" "$APP/ILDietControlLib.dll" "$APP/Dn2Cpp.Runtime.dll" \
    "$corelib" "$OUT/parity" \
    --metadata-validation "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    2>&1 | tee "$OUT/parity.log"
