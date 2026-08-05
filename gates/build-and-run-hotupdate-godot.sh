#!/usr/bin/env bash
# In-engine BPI deployment: the gate composing --gdextension with
# --hotupdate-base. The GDExtension base image carries the hot-update extras
# (ABI hash constant, base-abi.json, N2M trampolines, sigShape metadata); a
# pure-C# patch assembly is baked into a BPI; and a real headless Godot run
# drives the whole deployment: the scene node's _Ready applies the patch
# directory via HotUpdate.LoadDirectory (the version-ordered discovery flow,
# skipping a deliberately stale BPI at the header check). The patch directory
# path reaches the base as an OS filesystem path through the
# DN2CPP_HOTPATCH_DIR environment variable
# (Environment.GetEnvironmentVariable is an AOT intrinsic).
#
# Three patched surfaces are observed on engine-driven frames:
# 1. Hook pattern (plain-class virtual override): the patch entry installs a
#    subclass override into the base's static hook slot, and HotNode._Process
#    reads it each frame — an AOT callvirt through the instance vtable into
#    the N2M trampoline and the interpreted override.
# 2. Engine-virtual dispatch + patch node instantiation: the patch entry
#    `new`s a Node-derived patch class (the interpreter's allocation hook
#    constructs the engine object under the nearest ClassDB-registered
#    ancestor, SpawnedNode), the base adds it to the scene tree, and the
#    ENGINE's own _process ptrcall lands on the interpreted override through
#    the hotupdate base's vtable-routed virtual trampoline (the base method
#    must never run — asserted negatively).
# 3. Engine calls from interpreted patch code: the hot-update base emits a
#    synthesized invoker-compatible wrapper for every reachable engine-shim
#    method, so the patch binds and calls the shim surface — a static call
#    (FileAccess.FileExists) from the patch entry, and instance calls on the
#    spawned patch node (AddToGroup/IsInGroup string-arg/bool-return
#    round-trip; GetChildCount int return). IsInGroup is base-unused and
#    rooted through hotupdate-refs.txt (the plain-method root form).
#
# A negative deployment run then proves the placeholder fence is loud: Mathf
# and the Godot.Collections wrappers carry placeholder `return default`
# bodies (their call sites are lowered inline, so AOT code works — the base
# anchors Mathf.Sqrt), and a hot-update base excludes them from the
# reflection fnPtr/invoker wiring. A patch importing Mathf.Sqrt must fail at
# load with the standard unresolved-import diagnostic (surfaced by the
# base's catch around LoadDirectory) instead of silently binding the
# placeholder and computing with a default value.
#
# Remaining fences (documented in docs/BPI-FORMAT.md §Deployment, not
# exercised here): patch types are not ClassDB-visible (a patch instance
# lives under its base's registered class; GDScript .new() / .tscn cannot
# name a patch class), and patch-callable engine methods are limited to the
# shim surface with supported marshalling shapes that the base reaches (or
# roots via hotupdate-refs.txt) — builtin value-type methods (Vector3, …) and
# the GD utility class beyond Print stay outside it.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/hotupdate-godot
GODOT=${GODOT:-godot}
PROJECT=samples/godot/godot-project-hotupdate

echo "== 1/7 Building base + patch C# assemblies =="
build_proj samples/godot/HotUpdateGodotSample/HotUpdateGodotSample.csproj
build_proj samples/dotnet/HotUpdateGodotPatch/HotUpdateGodotPatch.csproj
build_proj samples/dotnet/HotUpdateGodotBadPatchMathf/HotUpdateGodotBadPatchMathf.csproj
base_app="samples/godot/HotUpdateGodotSample/bin/$CONFIG/$TFM/HotUpdateGodotSample.dll"
patch_app="samples/dotnet/HotUpdateGodotPatch/bin/$CONFIG/$TFM/HotUpdateGodotPatch.dll"
badmathf_app="samples/dotnet/HotUpdateGodotBadPatchMathf/bin/$CONFIG/$TFM/HotUpdateGodotBadPatchMathf.dll"

echo "== 2/7 Transpiling the base (--gdextension --hotupdate-base) =="
invoke_cli "$base_app" \
    -r "samples/godot/HotUpdateGodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
    -o "$OUT" --gdextension --hotupdate-base \
    --hotupdate-refs samples/godot/HotUpdateGodotSample/hotupdate-refs.txt
[ -f "$OUT/base-abi.json" ] || { echo "FAIL: base-abi.json sidecar missing" >&2; exit 1; }
grep -q dn2cpp_base_image_abi_hash "$OUT/generated.cpp" \
    || { echo "FAIL: dn2cpp_base_image_abi_hash constant missing from generated.cpp" >&2; exit 1; }

# Cache: the base transpile output (incl. base-abi.json) is in the key, and the
# patch dlls + refs file are keyed too because the BPI bakes in step 4/7 consume
# them after this point — unchanged inputs mean the compile, both bakes, and
# both engine runs would only repeat the last green result.
if gate_cache_check "$OUT" \
        "hotupdate-godot|godot=$(or_none "$(first_line "$("$GODOT" --version 2>/dev/null)")")" \
        "$base_app" "$patch_app" "$badmathf_app" \
        samples/godot/HotUpdateGodotSample/hotupdate-refs.txt \
        "$PROJECT"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/7 Compiling the GDExtension shared library =="
mkdir -p "$PROJECT/bin"
compile_gdextension "$OUT" "$PROJECT/bin/$(lib_name hotupdategodot)"

echo "== 4/7 Baking the patch BPIs + assembling the deployment directories =="
invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/base-abi.json" --patch-version 1 -o "$OUT"
rm -rf "$OUT/patches"
mkdir -p "$OUT/patches"
cp "$OUT/HotUpdateGodotPatch.bpi" "$OUT/patches/hook.bpi"
# A stale sibling: flip one byte of the header's baseImageAbiHash (offset 16) —
# the loader must header-skip it with a stderr diagnostic, never blocking the
# load. The non-.bpi file is ignored by discovery. loaded:1 proves both.
cp "$OUT/HotUpdateGodotPatch.bpi" "$OUT/patches/stale.bpi"
printf '\xFF' | dd of="$OUT/patches/stale.bpi" bs=1 seek=16 count=1 conv=notrunc 2>/dev/null
echo "not a patch" > "$OUT/patches/notes.txt"
# The negative deployment directory: a lone patch importing a placeholder-
# bodied shim method (Mathf.Sqrt), which the load must reject as unresolved.
invoke_cli --emit-patch "$badmathf_app" --base-abi "$OUT/base-abi.json" --patch-version 1 -o "$OUT"
rm -rf "$OUT/patches-mathf"
mkdir -p "$OUT/patches-mathf"
cp "$OUT/HotUpdateGodotBadPatchMathf.bpi" "$OUT/patches-mathf/mathf.bpi"

echo "== 5/7 Importing Godot project (registers the extension) =="
# Note: the first import may abort during editor teardown (Godot headless
# doc-gen bug; harmless — .godot/extension_list.cfg is already written).
run_with_watchdog 300 "$GODOT" --headless --path "$PROJECT" --import >/dev/null 2>&1 || true

echo "== 6/7 Running the scene (deploy from _Ready, observe on _Process frames) =="
rc=0
SCENE_OUT=$(run_with_watchdog 120 env DN2CPP_HOTPATCH_DIR="$PWD/$OUT/patches" \
    "$GODOT" --headless --path "$PROJECT" main.tscn --quit-after 6 2>&1) || rc=$?
echo "$SCENE_OUT"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: engine exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi

# The base image initialized inside the engine and the node came alive.
grep -q "HotNode._Ready: start" <<<"$SCENE_OUT" \
    || { echo "FAIL: _Ready start marker missing (base image init)" >&2; exit 1; }
# Pre-load contrast: before LoadDirectory the hook is the base implementation.
grep -q "hot: before base tick" <<<"$SCENE_OUT" \
    || { echo "FAIL: pre-patch hook line missing" >&2; exit 1; }
# Directory deployment inside the engine: exactly the one valid BPI loaded —
# the stale sibling was header-skipped, the non-.bpi file ignored.
grep -q "hot: loaded:1" <<<"$SCENE_OUT" \
    || { echo "FAIL: LoadDirectory count (expected loaded:1 — stale skipped)" >&2; exit 1; }
grep -q "stale BPI (base ABI hash mismatch)" <<<"$SCENE_OUT" \
    || { echo "FAIL: stale-BPI skip diagnostic missing" >&2; exit 1; }
# The BPI's entry method ran in the interpreter (Console goes to stdout).
grep -q "patch: installing hook" <<<"$SCENE_OUT" \
    || { echo "FAIL: patch entry marker missing (BPI entry did not run)" >&2; exit 1; }
# The patched virtual lands on engine-driven frames: the AOT callvirt in
# _Process dispatches through the instance vtable into the N2M trampoline and
# the interpreted override — on the first frame after load and still on later
# frames.
grep -q "frame 0: patched tick" <<<"$SCENE_OUT" \
    || { echo "FAIL: patched hook missing on frame 0" >&2; exit 1; }
grep -q "frame 2: patched tick" <<<"$SCENE_OUT" \
    || { echo "FAIL: patched hook missing on frame 2" >&2; exit 1; }
# No frame may still see the base implementation after the _Ready-time load.
grep -q "frame .*: base tick" <<<"$SCENE_OUT" \
    && { echo "FAIL: a frame saw the unpatched hook after load" >&2; exit 1; }
# The patch entry constructed a Node-derived patch instance (interpreter
# newobj -> engine-backing allocation hook) and the base put it in the tree.
grep -q "patch: spawning node" <<<"$SCENE_OUT" \
    || { echo "FAIL: patch spawn marker missing (patch entry did not construct the node)" >&2; exit 1; }
grep -q "hot: spawned pending node" <<<"$SCENE_OUT" \
    || { echo "FAIL: spawned node was not handed back / added to the tree" >&2; exit 1; }
# Engine-driven virtual dispatch reaches the interpreted override: the
# engine's _process ptrcall goes through SpawnedNode's vtable-routed
# trampoline into the patch type's vtable copy (N2M trampoline, interpreter).
grep -q "spawned patched tick" <<<"$SCENE_OUT" \
    || { echo "FAIL: engine-driven patched _Process marker missing" >&2; exit 1; }
# The AOT base body must never run on the patch instance: seeing it means the
# virtual trampoline bypassed the receiver's vtable.
grep -q "spawned base tick" <<<"$SCENE_OUT" \
    && { echo "FAIL: the engine drove the AOT base _Process instead of the patch override" >&2; exit 1; }
# Engine calls from interpreted patch code (the synthesized shim wrappers):
# the AOT-side anchors first prove the same wrappers' shapes in the base…
grep -q "hot: base child count ok" <<<"$SCENE_OUT" \
    || { echo "FAIL: base engine-call anchor missing (GetChildCount)" >&2; exit 1; }
grep -q "hot: base sees project file" <<<"$SCENE_OUT" \
    || { echo "FAIL: base engine-call anchor missing (FileAccess.FileExists)" >&2; exit 1; }
# The AOT-side Mathf anchor: the placeholder-bodied shim's call sites are
# lowered inline, so the base image computes the real value.
grep -q "hot: base mathf ok" <<<"$SCENE_OUT" \
    || { echo "FAIL: base Mathf anchor missing (inline-lowered Mathf.Sqrt)" >&2; exit 1; }
# …then the interpreter binds them through the wrappers' fnPtr/invoker rows:
# a static shim call from the patch entry, and instance calls on the spawned
# patch node (string-arg/bool-return round-trip + int return; IsInGroup is
# rooted only via hotupdate-refs.txt).
grep -q "patch: static engine call ok" <<<"$SCENE_OUT" \
    || { echo "FAIL: interpreted static engine call missing/failed (FileAccess.FileExists)" >&2; exit 1; }
grep -q "patch: engine group round-trip ok" <<<"$SCENE_OUT" \
    || { echo "FAIL: interpreted engine group round-trip missing/failed (AddToGroup/IsInGroup)" >&2; exit 1; }
grep -q "patch: engine child count ok" <<<"$SCENE_OUT" \
    || { echo "FAIL: interpreted engine child count missing/failed (GetChildCount)" >&2; exit 1; }

echo "== 7/7 Negative deployment: a patch importing a placeholder shim body =="
# Same scene, patches-mathf as the deployment dir: the lone BPI imports
# Mathf.Sqrt, whose reached placeholder body is excluded from fnPtr/invoker
# wiring in a hot-update base — LoadDirectory must throw the standard
# unresolved-import bind failure (caught + surfaced by the base), the patch
# entry must never run, and the engine keeps running unpatched.
rc=0
NEG_OUT=$(run_with_watchdog 120 env DN2CPP_HOTPATCH_DIR="$PWD/$OUT/patches-mathf" \
    "$GODOT" --headless --path "$PROJECT" main.tscn --quit-after 6 2>&1) || rc=$?
echo "$NEG_OUT"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: negative run exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi
grep -q "hot: load failed: BPI bind: unresolved method import" <<<"$NEG_OUT" \
    || { echo "FAIL: placeholder shim import did not fail as unresolved at load" >&2; exit 1; }
grep -q "patch: mathf" <<<"$NEG_OUT" \
    && { echo "FAIL: the rejected patch entry ran (placeholder body bound?)" >&2; exit 1; }
grep -q "frame 0: base tick" <<<"$NEG_OUT" \
    || { echo "FAIL: the engine did not keep running unpatched after the rejected load" >&2; exit 1; }

gate_cache_commit
echo "OK (in-engine BPI deployment: hook reload + engine-driven patch node + interpreted engine calls + loud placeholder fence)"
