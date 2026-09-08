#!/usr/bin/env bash
# Opt-in feasibility probe for transpiling the ILDiet companion and Mono.Cecil.
# Kept outside the regression-gate glob until native stripping parity is proven.
source "$(dirname "$0")/_common.sh"

stage="${1:-build}"
case "$stage" in
    emit|build) ;;
    *) echo "usage: $0 [emit|build]" >&2; exit 1 ;;
esac

mkdir -p artifacts
OUT="$(mktemp -d "artifacts/ildiet-native-$CONFIG.XXXXXX")"
echo "ILDiet native probe artifacts: $OUT"

build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj 2>&1 | tee "$OUT/managed-build.log"
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
DIET="src/ILDiet/bin/$CONFIG/$TFM"
corelib="$(resolve_net10_corelib)"

# Use the original IL so this probe does not depend on ILDiet stripping itself.
dotnet exec "$BIN/dn2cpp.dll" "$DIET/ILDiet.dll" \
    -r "$DIET/Mono.Cecil.dll" -r "$corelib" \
    --auto-ref --no-ildiet -o "$OUT" 2>&1 | tee "$OUT/emit.log"
[ -f "$OUT/generated.cpp" ] || { echo "error: no generated.cpp" >&2; exit 1; }
[ "$stage" = build ] || exit 0

compile_console "$OUT" ILDiet 2>&1 | tee "$OUT/native-build.log"
dotnet exec "$DIET/ILDiet.dll" --help > "$OUT/managed-help.txt"
"$OUT/ILDiet" --help > "$OUT/native-help.txt"
diff -u "$OUT/managed-help.txt" "$OUT/native-help.txt"
echo "OK: native ILDiet starts and reproduces managed help; stripping parity remains unproven"
