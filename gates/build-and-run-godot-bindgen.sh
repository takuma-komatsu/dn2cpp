#!/usr/bin/env bash
# End-to-end self-host gate for the Godot binding-generation path: transpile
# the GodotBindGen driver — which calls the REAL Dn2Cpp.Godot.BindingGenerator.Run
# (deserializing extension_api.json via the GodotApi source-gen System.Text.Json
# context, then emitting the engine shim C#) — to native C++ via dn2cpp itself,
# compile it, run it on the real ~6.4 MiB extension_api.json, and diff the emitted
# GodotShims against the managed (`dotnet`) output of the same driver. A byte match
# proves the native binding-generation path (real STJ source-gen IL + StringBuilder
# emit + file write) reproduces the managed output exactly — the self-host
# fixpoint for the `--generate-bindings` subtree, the one console-self-host
# excludes. Unlike build-and-run-json-godot.sh (which deserializes a tiny inline
# fragment via a copied DTO graph) this drives the real generator on the real dump.
#
# Like the JSON gates this pins net10.0 (resolve_net10_corelib): the 11.0 preview
# CoreLib shape skews the STJ transpile (see resolve_net10_corelib in _common.sh).
#
# Requires the gitignored extension_api.json at the repo root (the same engine API
# dump the Godot sample gates consume to generate their shims). When it is absent
# (a fresh checkout that has not copied it in) the gate SKIPS loudly with exit 0
# rather than failing the suite — mirroring how the Godot lane treats the dump as a
# provided prerequisite.
source "$(dirname "$0")/_common.sh"

API_JSON="extension_api.json"
if [ ! -f "$API_JSON" ]; then
    gate_skip "$PWD/$API_JSON not present (gitignored Godot API dump) — generate it with \`godot --headless --dump-extension-api\`"
fi

OUT="artifacts/godot-bindgen"

echo "== 1/6 Locating the real net10.0 CoreLib + System.Text.Json + System.Linq =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
REAL_JSON="$bcl/System.Text.Json.dll"
REAL_LINQ="$bcl/System.Linq.dll"
[ -f "$REAL_JSON" ] || { echo "error: real System.Text.Json not found: $REAL_JSON" >&2; exit 1; }
[ -f "$REAL_LINQ" ] || { echo "error: real System.Linq not found: $REAL_LINQ"      >&2; exit 1; }
echo "corlib:    $corelib"
echo "real json: $REAL_JSON"
echo "real linq: $REAL_LINQ"

echo "== 2/6 Building the GodotBindGen driver =="
build_proj samples/dotnet/GodotBindGen/GodotBindGen.csproj
DRVBIN="samples/dotnet/GodotBindGen/bin/$CONFIG/$TFM"
DRV="$DRVBIN/GodotBindGen.dll"
[ -f "$DRV" ]  || { echo "error: driver not built: $DRV"     >&2; exit 1; }

echo "== 3/6 Transpiling the driver + Dn2Cpp.Godot + real CoreLib + System.Text.Json (--auto-ref) =="
# Roots only BindingGenerator.Run -> GodotApi (+ BCL): tree-shaking prunes the
# GodotBackend / GodotCallIntrinsics transpile-side call map, so the closure is the
# binding-generation + STJ-source-gen-deserialize path alone. The real BCL
# System.Linq is transpiled directly (its OrderBy is stable), so the emitted
# shim text stays order-deterministic and byte-identical to the managed run.
invoke_cli "$DRV" \
    -r "$DRVBIN/Dn2Cpp.Godot.dll" \
    -r "$DRVBIN/Dn2Cpp.Transpiler.dll" \
    -r "$REAL_LINQ" \
    -r "$corelib" \
    -r "$REAL_JSON" \
    --auto-ref -o "$OUT"
# extension_api.json and the driver dll are inputs of the RUN too (both shim
# generations read them), so both sit in the key beside the transpile surface.
if gate_cache_check "$OUT" "godot-bindgen|$corelib" \
        "$DRV" "$DRVBIN/Dn2Cpp.Godot.dll" "$DRVBIN/Dn2Cpp.Transpiler.dll" \
        "${DRV%.dll}.runtimeconfig.json" "${DRV%.dll}.deps.json" "$API_JSON"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/6 Compiling the native binding generator =="
compile_console "$OUT" GodotBindGen

echo "== 5/6 Generating shims natively + with managed dotnet (same driver) =="
native_out="$OUT/native-shims.g.cs"
managed_out="$OUT/managed-shims.g.cs"
rm -f "$native_out" "$managed_out"
"./$OUT/GodotBindGen" "$API_JSON" "$native_out"
dotnet "$DRV" "$API_JSON" "$managed_out"
[ -f "$native_out" ]  || { echo "error: native run produced no output: $native_out"   >&2; exit 1; }
[ -f "$managed_out" ] || { echo "error: managed run produced no output: $managed_out" >&2; exit 1; }

echo "== 6/6 Diffing native vs managed generated shims (byte-identical = fixpoint) =="
if diff "$native_out" "$managed_out" >/dev/null; then
    echo "OK: native binding-gen output byte-identical to managed ($(wc -l <"$native_out" | tr -d ' ') lines)"
    gate_cache_commit
else
    echo "FAIL: native binding-gen output differs from managed:" >&2
    head -40 >&2 <<<"$(diff "$native_out" "$managed_out")"
    exit 1
fi
