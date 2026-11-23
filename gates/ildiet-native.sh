#!/usr/bin/env bash
# Build the distributed native ILDiet and compare its stripping with managed ILDiet.
source "$(dirname "$0")/_common.sh"
source "$PWD/gates/_ildiet.sh"

stage="${1:-build}"
case "$stage" in
    emit|build|verify) ;;
    *) echo "usage: $0 [emit|build|verify]" >&2; exit 1 ;;
esac
if [ "$stage" = verify ]; then
    gate_python -c 'pass' >/dev/null || gate_skip "no working Python 3 interpreter for ILDiet native parity"
fi

mkdir -p artifacts
OUT="$(mktemp -d "artifacts/ildiet-native-$CONFIG.XXXXXX")"
echo "ILDiet native artifacts: $OUT"

DIET="src/ILDiet/bin/$CONFIG/$TFM"
corelib="$(resolve_net10_corelib)"

if [ "$stage" = emit ]; then
    ildiet_emit_native "$OUT" "$corelib"
    exit 0
fi

ildiet_stage_native "$OUT/relocated tool/ildiet"
# The native artifact may be reused after the managed build outputs were removed.
[ -f "$DIET/ILDiet.dll" ] || build_proj src/ILDiet/ILDiet.csproj 2>&1 | tee "$OUT/managed-build.log"
dotnet exec "$DIET/ILDiet.dll" --help > "$OUT/managed-help.txt" 2> "$OUT/managed-help.stderr.log"
PATH= DOTNET_ROOT= DOTNET_ROOT_X64= DOTNET_ROOT_ARM64= DOTNET_MULTILEVEL_LOOKUP=0 \
    "$OUT/relocated tool/ildiet/ILDiet$EXE_EXT" --help \
    > "$OUT/native-help.txt" 2> "$OUT/native-help.stderr.log"
diff -u "$OUT/managed-help.txt" "$OUT/native-help.txt"
if [ "$stage" = build ]; then
    echo "OK: distributed native ILDiet starts without dotnet and reproduces managed help; stripping parity remains unproven"
    exit 0
fi

build_proj samples/dotnet/ILDietControl/ILDietControl.csproj 2>&1 | tee "$OUT/fixture-build.log"
build_gate_proj gates/fixtures/preserve-control/MetadataProbe.csproj 2>&1 | tee "$OUT/probe-build.log"
build_gate_proj gates/fixtures/ildiet-metadata-validation/MetadataValidation.csproj \
    2>&1 | tee "$OUT/metadata-validation-build.log"
APP="samples/dotnet/ILDietControl/bin/$CONFIG/$TFM"
gate_python gates/fixtures/ildiet-native/check.py "$DIET/ILDiet.dll" "$OUT/relocated tool/ildiet/ILDiet$EXE_EXT" \
    "gates/fixtures/preserve-control/bin/$CONFIG/$TFM/MetadataProbe.dll" \
    "$APP/ILDietControl.dll" "$APP/ILDietControlLib.dll" "$APP/Dn2Cpp.Runtime.dll" \
    "$corelib" "$OUT/parity" \
    --execution-link-xml samples/dotnet/ILDietControl/keep-attribute-constructed.xml \
    --metadata-validation "gates/fixtures/ildiet-metadata-validation/bin/$CONFIG/$TFM/MetadataValidation.dll" \
    2>&1 | tee "$OUT/parity.log"
