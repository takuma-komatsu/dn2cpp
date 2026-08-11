#!/usr/bin/env bash
# End-to-end SDK-compat pipeline: standard Godot C# (compiled against the dn2cpp
# GodotSharp shim, NOT the real Godot.NET.Sdk package) -> IL -> C++ -> GDExtension
# dylib -> run in Godot. Proves an unmodified-style Godot C# node transpiles and
# runs through the "fake GodotSharp" shim (Approach 1).
source "$(dirname "$0")/_common.sh"

OUT=artifacts/sdk
GODOT=${GODOT:-godot}
PROJECT=samples/godot/godot-project-sdk
SAMPLE=samples/godot/SdkSample

echo "== 1/6 Building sample C# assembly (against the GodotSharp shim) =="
build_proj "$SAMPLE/SdkSample.csproj"

echo "== 2/6 Transpiling IL -> C++ (GDExtension mode, shim passed as reference) =="
invoke_cli \
    "$SAMPLE/bin/$CONFIG/$TFM/SdkSample.dll" \
    -r "$SAMPLE/bin/$CONFIG/$TFM/GodotSharp.dll" \
    -o "$OUT" --gdextension

# Cache: with the generated output, the input dlls, the engine version, and
# the Godot project unchanged since the last green pass, the compile and the
# engine run would only repeat that result.
if gate_cache_check "$OUT" \
        "sdk-sample|godot=$(or_none "$(first_line "$("$GODOT" --version 2>/dev/null)")")" \
        "$SAMPLE/bin/$CONFIG/$TFM/SdkSample.dll" \
        "$SAMPLE/bin/$CONFIG/$TFM/GodotSharp.dll" \
        "$PROJECT"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/6 Compiling GDExtension shared library =="
mkdir -p "$PROJECT/bin"
compile_gdextension "$OUT" "$PROJECT/bin/$(lib_name "$DN2CPP_GDEXT_LIB")"

echo "== 4/6 Importing Godot project (registers the extension) =="
# The first import may abort during editor teardown (Godot headless doc-gen bug;
# harmless — .godot/extension_list.cfg is already written).
"$GODOT" --headless --path "$PROJECT" --import >/dev/null 2>&1 || true

echo "== 5/6 Running inside Godot (headless, GDScript -> standard C# node) =="
"$GODOT" --headless --path "$PROJECT" --script res://test.gd
gate_cache_commit
