#!/usr/bin/env bash
# Godot .NET-module (mono module) scripted-node E2E gate, UNDER --trim-reflection
# AND --trim-godot-classes.
# Identical in every assertion to build-and-run-godot-dotnet-sample.sh — the same real
# engine binary, the same scripted scene, the same marker battery — except that the
# transpile passes both trims. It is the real-engine oracle for the flags: the
# console gate (build-and-run-trim-reflection.sh) proves the strip semantics against a
# synthetic library, and THIS one proves the trimmed metadata path survives the full
# GodotSharp round trip that ships. That round trip reflects for real — ClassDB script
# registration reads a private static NativeName field BY REFLECTION off the first engine-
# wrapper base (the base-chain closure in the keep-set is what keeps that field alive on a
# stripped Godot.Node), [Export] property scanning, [Signal] emission, delegate-backed
# Callable connect, and an awaited ToSignal continuation all run here — so a keep-set that
# stripped one type too many would surface as a null class name, a missing export, or a
# dead signal in a real engine, not as a console PNSE. The trim strips ~1,858 types on this
# sample and every marker below still fires. --trim-godot-classes stacks the
# engine-wrapper allowlist trim on top (released-count tripwire + the
# DN2CPP_DM_TRIMFALLBACK ancestor-wrapper probe; see godot_dotnet_transpile).
#
# ---- everything from here down mirrors the untrimmed sample gate ----
#
# A real engine binary built from the pinned godotengine/godot clone loads the
# dn2cpp-produced mono-module library, and a scene whose nodes carry real C# scripts
# (res://Player.cs + res://Probe.cs) runs end-to-end — script resource load
# (managed script bridge maps the path to the type registered from its
# ScriptPathAttribute), script-instance creation (uninitialized allocation +
# in-place constructor invoke, tied to the native Object*), _Ready/_Process
# dispatch through the source-generated InvokeGodotClassMethod, GD.Print
# reaching the engine's stdout, [Export] properties (scene-baked override in
# through the instance Set bridge, defaults surviving, cross-node reads back
# out through the Variant Get path), [Signal]/engine-signal parity (C#
# event round trip through the engine's connection dispatch, delegate-backed
# Callable connect + managed hash/equality identity + trampoline invoke, and
# an awaited ToSignal resuming an async continuation from the signal
# callback), async-interop (the frame callback's scheduler pump resumes an
# awaited Task.Run continuation and a real-time Task.Delay on the main
# thread — the native mirror of GodotTaskScheduler.Activate), and RefCounted
# lifetime (res://LifetimeProbe.cs +
# res://MyResource.cs: the strong/weak GCHandle swap dance on engine
# reference/unreference, finalizer-driven native disposal, the
# instance-binding path for engine-created RefCounteds, explicit
# Dispose()/Free(), and a 1000-instance create/store/drop stress loop),
# plus the GDExtension-lane parity surface on res://ParityNode.cs (below).
#
# Parity record vs the GDExtension lane's sample gate
# (gates/build-and-run-godot-sample.sh scene phase / MyNode.cs) — both lanes
# now cover, in one engine run each:
#   - virtuals: _EnterTree, _Ready, _Process (delta sane), _PhysicsProcess
#     (delta > 0), _ExitTree (fires on the quit-driven scene teardown),
#     _Notification with the exact raw constants (ENTER_TREE=10, READY=13,
#     EXIT_TREE=11), _Input and _UnhandledInput.
#   - input injection (headless): a synthesized InputEventKey (Key.A, pressed)
#     pushed through get_viewport().push_input — here built and pushed from
#     C# itself (the GDExtension lane uses a GDScript injector), which
#     additionally proves C#-side engine-class construction (new
#     InputEventKey()) and enum/bool property set/get round trips through the
#     generated NativeCalls ptrcalls before the engine ever sees the object.
#   - borrowed-event dispatch: both input virtuals receive the engine-owned
#     event as a typed managed wrapper; IsPressed() + a Keycode read prove the
#     handle and payload survive (RefCounted incoming-argument semantics).
#   - reverse-direction engine calls (DM_ENGINE marker): bool return
#     (IsInsideTree/HasNode/IsAncestorOf), String return (GetClass), String
#     property round trip (EditorDescription), Vector2 property round trip
#     (Position on a Node2D), NodePath return (GetPath), and an Object return
#     (GetParent) whose wrapper drives a further call (.Name).
#
# Flow mirrors the handshake gate: build the mono-module dylib (shared
# pipeline) -> pack the scripted main scene with the mono *editor*
# (--export-pack, preset "sample-pack" whose custom_features carries the
# "dotnet" feature the template's GDMono::should_initialize() gates on; the
# default feature-untagged run/main_scene is the scripted scene) -> assemble a
# loose run dir laid out like an exported game -> run headless with
# DN2CPP_DM_TRACE=1 and assert the script markers (constructor exactly once,
# _Ready with the node name proving the native tie, _Process exactly once)
# plus the init-stage markers (a failed .NET init only logs an error — the
# engine keeps running and exits 0, so absence of errors proves nothing).
#
# Requires the artifacts of gates/setup-godot-dotnet.sh via
# DN2CPP_GODOT_DOTNET_ROOT (here: editor_bin + template_bin too); gate_skip's
# when absent, which the runner counts and reports as a SKIP, NOT as a pass
# (this gate's own history is why — see the skip protocol in _common.sh).
# Registered in run-all-gates.sh's SERIAL Godot phase (it launches the engine);
# every engine/editor launch runs under a watchdog — a broken Godot run hangs
# rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

# The transpile half of the pipeline, overriding the shared one from
# _godot_dotnet.sh to add --trim-reflection AND --trim-godot-classes. Everything
# else — the sample build, the GodotSharp + net10 CoreLib references — is
# identical to the untrimmed gate, and the mono-module link is now literally the
# shared godot_dotnet_link_lib rather than a second copy of its one line (that
# half sits behind the cache check for every consumer). The engine run
# below is thereby also the real-engine oracle for the wrapper trim: the scene's
# TrimProbe node (a Sprite2D, a type the C# never names, so its registry lambda
# is redirected) is fetched with the non-generic GetNode and driven through
# GetClass/is/cast/property round trips on the ancestor-typed wrapper — the
# DN2CPP_DM_TRIMFALLBACK marker in the shared battery asserts every answer
# matches the untrimmed run's.
godot_dotnet_transpile() {
    local out="$1"
    echo "-- Generating the sample's nuget.config (local feed + nuget.org)"
    godot_dotnet_nuget_config "$GODOT_DOTNET_SAMPLE_DIR"
    echo "-- Building the sample game assembly (real Godot.NET.Sdk)"
    dotnet build "$GODOT_DOTNET_SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease --nologo -v q
    local app="$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
    [ -f "$app" ] || { echo "error: not built: $app" >&2; return 1; }
    echo "-- Transpiling (--dotnet-module --trim-reflection --trim-godot-classes, real GodotSharp + net10 CoreLib)"
    build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
    local corelib; corelib=$(resolve_net10_corelib)
    rm -rf "$out"
    mkdir -p "$out"   # tee opens its file before the CLI creates the dir
    invoke_cli "$app" --dotnet-module -r "$corelib" -r "$GODOT_DOTNET_GODOTSHARP" \
        --auto-ref --trim-reflection --trim-godot-classes -o "$out" | tee "$out/transpile.log"
    # The godot-class-trim summary line proves the flag actually armed — a
    # silently dropped flag would keep every wrapper in and leave this gate
    # green for the wrong reason — and all three counts carry a tripwire:
    #   released <= 200: this hello-world-sized sample names on the order of a
    #     dozen wrappers, so more means a release trigger started cascading
    #     (e.g. counting GodotSharp-internal mentions) and the trim no longer
    #     trims.
    #   registered >= 900: registered counts the registry lambdas whose bodies
    #     the shape peek recognized (955 on the pinned GodotSharp). The peek
    #     falls back to "reach normally" on an unrecognized shape — safe, but
    #     silent — so a compiler/generator change to the lambda IL would zero
    #     this count, quietly disarm the whole cut, and leave every other
    #     assertion green.
    #   redirected >= 700: registered minus released must actually be routed;
    #     a redirect table that stopped filling is a cut without a route.
    # The transpile log must also carry no 'godot-class-trim warning:' lines —
    # the unreleased-internal-cast diagnostic must not fire on the shipped
    # GodotSharp corpus (a hit here means a GodotSharp-internal cast now
    # targets a wrapper the triggers do not release: fix the trigger set or
    # add the wrapper to the gate's roots, but do not let it ride as a
    # run-time InvalidCastException).
    local released registered redirected
    released=$(LC_ALL=C sed -n 's/^dn2cpp: godot-class-trim: \([0-9][0-9]*\) released of .*/\1/p' "$out/transpile.log")
    registered=$(LC_ALL=C sed -n 's/^dn2cpp: godot-class-trim: [0-9][0-9]* released of \([0-9][0-9]*\) registered .*/\1/p' "$out/transpile.log")
    redirected=$(LC_ALL=C sed -n 's/^dn2cpp: godot-class-trim: .* registered engine wrappers, \([0-9][0-9]*\) lambdas redirected$/\1/p' "$out/transpile.log")
    [ -n "$released" ] && [ -n "$registered" ] && [ -n "$redirected" ] \
        || { echo "FAIL: no parseable 'dn2cpp: godot-class-trim:' line in the transpile output — --trim-godot-classes did not arm" >&2; return 1; }
    [ "$released" -le 200 ] || { echo "FAIL: godot-class-trim released $released wrappers (> 200) — a release trigger is cascading" >&2; return 1; }
    [ "$registered" -ge 900 ] || { echo "FAIL: godot-class-trim registered only $registered lambdas (< 900) — the registry-lambda shape peek stopped recognizing the generated IL, so the cut is disarmed" >&2; return 1; }
    [ "$redirected" -ge 700 ] || { echo "FAIL: godot-class-trim redirected only $redirected lambdas (< 700) — the redirect table stopped filling (a cut without a route)" >&2; return 1; }
    if grep -q "godot-class-trim warning:" "$out/transpile.log"; then
        echo "FAIL: the unreleased-internal-cast diagnostic fired on the shipped GodotSharp corpus (above)" >&2
        grep "godot-class-trim warning:" "$out/transpile.log" >&2
        return 1
    fi
    echo "-- godot-class-trim armed: $released released / $registered registered / $redirected redirected, no internal-cast warnings"
}

OUT=gates/out-godot-dotnet-trim
ROOT="$GODOT_DOTNET_ROOT"
PINNED_COMMIT=a13da4feb8d8aefc283c3763d33a2f170a18d541
ABI_EXPECTED=gates/expected/godot-dotnet-abi.sha256

if ! godot_dotnet_root_ok || [ ! -x "$GODOT_DOTNET_EDITOR" ] || [ ! -x "$GODOT_DOTNET_TEMPLATE" ] \
    || [ ! -f "$ROOT/pin.txt" ]; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/6 Pin + interop-ABI tripwire =="
godot_dotnet_pin_abi_check "$PINNED_COMMIT" "$ABI_EXPECTED"

echo "== 2/6 Building the mono-module shared library =="
godot_dotnet_transpile "$OUT"
# Everything past this point — the native link, import, export-pack, the engine
# run and its marker asserts — is a pure function of the transpile surface in
# $OUT, this script (which carries the trimmed transpile half), the shared link
# helper, the sample project, the app/GodotSharp assemblies and the pinned
# engine binaries (pin + editor/template identity live in the context string).
# The pin/ABI tripwire above stays always-on, so a drifted clone still fails on
# a hit. The link is deliberately below the check: a hit exits here and never
# names $DYLIB, so building it first is pure waste.
if gate_cache_check "$OUT" \
    "godot-dotnet-trim|pin=$(file_text "$ROOT/pin.txt")|editor=$(file_sig_deref "$GODOT_DOTNET_EDITOR")|template=$(file_sig_deref "$GODOT_DOTNET_TEMPLATE")" \
    "$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll" \
    "$GODOT_DOTNET_GODOTSHARP" \
    "$GODOT_DOTNET_SAMPLE_DIR"; then
    { gate_cache_hit_msg; exit 0; }
fi
godot_dotnet_link_lib "$OUT"
DYLIB="$OUT/$(lib_name DotnetSample)"

echo "== 3/6 Importing the sample project (mono editor, headless) =="
RUN="$OUT/run"
rm -rf "$RUN"
mkdir -p "$RUN"
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 300 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$GODOT_DOTNET_SAMPLE_DIR" --import >"$RUN/import.log" 2>&1 || true

echo "== 4/6 Exporting the scripted-scene pack (--export-pack) =="
# Named after the run dir's engine binary: a release export template rejects
# every path/scene CLI override, so the only way it finds a project is the
# exported-game convention <exe_basename>.pck next to the executable; the
# packed default run/main_scene is the scripted main.tscn.
PCK="$PWD/$RUN/godot.pck"
if ! godot_export_step 300 "$RUN/export.log" "$PCK" \
    "$GODOT_DOTNET_EDITOR" --headless \
    --path "$GODOT_DOTNET_SAMPLE_DIR" --export-pack sample-pack "$PCK"; then
    echo "FAIL: --export-pack sample-pack failed (see below)" >&2
    cat "$RUN/export.log" >&2
    exit 1
fi
[ -f "$PCK" ] || { echo "FAIL: export produced no $PCK" >&2; cat "$RUN/export.log" >&2; exit 1; }

echo "== 5/6 Assembling the loose run dir =="
# try_load_native_aot_library opens <exe_dir>/data_<AssemblyName>_<platform>_<arch>/
# <AssemblyName>.<dylib|dll|so> (no lib prefix — the NativeAOT publish naming,
# not ours). Both halves are per-platform and neither is $DN2CPP_OS: the engine
# maps Linux to "linuxbsd" (see GODOT_DOTNET_PLATFORM in _godot_dotnet.sh).
ARCH="$(godot_dotnet_host_arch)"
cp -L "$GODOT_DOTNET_TEMPLATE" "$RUN/godot$EXE_EXT"
chmod +x "$RUN/godot$EXE_EXT"
mkdir -p "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH"
cp -f "$DYLIB" "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH/DotnetSample.$LIB_EXT"

echo "== 6/6 Running the scripted scene in the real engine =="
# The scripted quit: Player's awaited ToSignal continuation flags the sibling
# ParityNode (a cross-script static), which calls GetTree().Quit() only once
# its own input/physics markers are out too (~0.05s in), so a green run ends
# deterministically and no marker races the quit; the _ExitTree /
# NOTIFY_EXIT_TREE markers then fire on the quit-driven scene teardown.
# --quit-after is only the backstop for a run whose managed side broke before
# reaching the quit (headless frames tick at the low-processor-usage pace, so
# 900 frames ≈ several seconds — far past the 0.05s timer, far short of the
# watchdog).
LOG="$RUN/sample.log"
rc=0
run_with_watchdog 120 env DN2CPP_DM_TRACE=1 "$PWD/$RUN/godot$EXE_EXT" --headless \
    --quit-after 900 >"$LOG" 2>&1 || rc=$?
cat "$LOG"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: engine exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi
# Positive markers: every init stage reached, the engine's main loop drove the
# wrapped FrameCallback, and the script instances went through the full
# surface: construction/_Ready/_Process (with the node's real name proving the
# native tie), exported properties (the scene's baked Speed override arrived
# through the instance Set bridge, the untouched ones kept their C# defaults,
# and the sibling Probe read them back through the engine's Variant Get path),
# and signals (a C# [Signal] emitted through the engine back into a C#
# handler, a delegate-backed Callable connection found by managed hash/
# equality and invoked through its trampoline, and an awaited ToSignal
# continuation resumed by the signal callback).
for marker in \
    "dn2cpp-dm: interop received" \
    "dn2cpp-dm: managed callbacks written" \
    "dn2cpp-dm: init ok" \
    "dn2cpp-dm: frame" \
    "DN2CPP_DM_READY name=Player" \
    "DN2CPP_DM_EXPORT speed=100 label=default-label factor=1.5 spawn=(3, 4)" \
    "DN2CPP_DM_GET speed=100 label=default-label" \
    "DN2CPP_DM_TRIMFALLBACK class=Sprite2D isNode2D=True name=TrimProbe posOk=True managed=Node2D" \
    "DN2CPP_DM_CONNECTED True" \
    "DN2CPP_DM_SIGNAL amount=7" \
    "DN2CPP_DM_TIMEOUT" \
    "DN2CPP_DM_TOSIGNAL" \
    "DN2CPP_DM_ASYNC resumeOnMain=True workerDiffers=True delayOnMain=True delayElapsedOk=True" \
    "DN2CPP_DM_SYNCCTX mainHasCtx=True workerCtxNull=True postedOnMain=True" \
    "DN2CPP_DM_PROCESS"; do
    grep -qF "$marker" "$LOG" \
        || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
done
# Parity markers (ParityNode): the full engine-virtual set incl. the exact raw
# notification constants, headless input injection (a C#-built InputEventKey
# whose properties round-trip through ptrcalls before the push — EVKEY),
# both input virtuals receiving the borrowed engine event as a typed wrapper
# with the injected keycode intact, the reverse-direction engine-call battery
# (ENGINE), and the teardown-driven exit pair (EXIT_TREE fires after Quit()).
for marker in \
    "DN2CPP_DM_ENTER_TREE" \
    "DN2CPP_DM_NOTIFY_ENTER_TREE what=10" \
    "DN2CPP_DM_NOTIFY_READY what=13" \
    "DN2CPP_DM_ENGINE inTree=True class=Node2D pos=(30.5, 40.5) desc=True parent=Main path=True hasSelf=True selfAncestor=False" \
    "DN2CPP_DM_PARITY_PROCESS deltaOk=True" \
    "DN2CPP_DM_PHYSICS deltaOk=True" \
    "DN2CPP_DM_EVKEY roundtrip=True" \
    "DN2CPP_DM_INPUT pressed=True keycodeOk=True" \
    "DN2CPP_DM_UNHANDLED pressed=True keycodeOk=True" \
    "DN2CPP_DM_PARITY_DONE" \
    "DN2CPP_DM_NOTIFY_EXIT_TREE what=11" \
    "DN2CPP_DM_EXIT_TREE"; do
    grep -qF "$marker" "$LOG" \
        || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
done
# RefCounted lifetime markers (LifetimeProbe + MyResource): the strong/weak
# GCHandle swap kept a C#-created script Resource (and its managed-only Tag)
# alive and identical while engine storage referenced it (SWAP), the wrapper
# died and freed the native object once the engine dropped its last reference
# (SWAP_RELEASED / RELEASED), an engine-created RefCounted round-tripped the
# instance-binding path with wrapper identity and finalizer-driven unref
# (BINDING), explicit Dispose()/Free() deleted the native side exactly once
# with double disposal a no-op (DISPOSE), and 1000 create/store/drop cycles —
# two handle swaps each — finalized with no refcount drift (STRESS; drift in
# the other direction would show as "ObjectDB instances leaked" below).
#
# SWAP_RELEASED and STRESS are pin-tolerant booleans self-checked against a
# per-id ledger, not raw counts — a conservative collector does not promise to
# collect any GIVEN object. Full reasoning in the sibling comment in
# gates/build-and-run-godot-dotnet-sample.sh.
for marker in \
    "DN2CPP_DM_RC_SWAP first=True tag=5678 valid=True" \
    "DN2CPP_DM_RC_SWAP_RELEASED enough=True ledgerOk=True" \
    "DN2CPP_DM_RC_RELEASED heldAlive=True finalized=True nativeFreed=True" \
    "DN2CPP_DM_RC_BINDING alive=True identity=True freed=True" \
    "DN2CPP_DM_RC_DISPOSE resFreed=True resPtrCleared=True nodeFreed=True nodePtrCleared=True" \
    "DN2CPP_DM_RC_STRESS created=True enough=True ledgerOk=True" \
    "DN2CPP_DM_RC_DONE"; do
    grep -qF "$marker" "$LOG" \
        || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
done
# Boundary degrade (res://FaultProbe.cs), asserted here for the same
# reason every other marker is: the trims must not change it. A keep-set that
# stripped the exception's own metadata, or the ExceptionUtils pair the sink
# calls, would show up as a missing report rather than as a console PNSE.
# Full reasoning for each of these greps — and why the ^ERROR: anchor is the
# load-bearing part — is in the sibling block in
# gates/build-and-run-godot-dotnet-sample.sh.
grep -qF "DN2CPP_DM_FAULTPOSTED" "$LOG" \
    || { echo "FAIL: FaultProbe never posted its SynchronizationContext fault" >&2; exit 1; }
grep -qF "DN2CPP_DM_FAULTCALL" "$LOG" \
    || { echo "FAIL: FaultProbe._Process never ran (script class not registered?)" >&2; exit 1; }
grep -q "^ERROR: dn2cpp: unhandled managed exception in the engine frame callback (SynchronizationContext drain)$" "$LOG" \
    || { echo "FAIL: the frame-callback boundary did not report through the engine error log" >&2; exit 1; }
grep -q "^ERROR: System.InvalidOperationException: DN2CPP_DM_FAULT_INJECTED raised from a SynchronizationContext callback" "$LOG" \
    || { echo "FAIL: the frame-callback report carried no exception type/message" >&2; exit 1; }
grep -q "^ERROR: System.InvalidOperationException: DN2CPP_DM_FAULT_INJECTED raised from an engine-invoked _Process" "$LOG" \
    || { echo "FAIL: the script-call boundary did not report the _Process fault" >&2; exit 1; }
grep -qF "DN2CPP_DM_FAULTPROBE posted=True method=True" "$LOG" \
    || { echo "FAIL: FaultProbe did not survive both faults (engine died, or a fault never fired)" >&2; exit 1; }
# Exactly-once markers: a double construction (the ctor-invoke risk: a
# reflective ctor invoke allocating a NEW instance instead of running
# in-place), a broken _Process latch, a double-dispatched signal handler or a
# double-resumed awaiter shows up as a count != 1.
for once in "DN2CPP_DM_CTOR" "DN2CPP_DM_READY" "DN2CPP_DM_PROCESS" \
    "DN2CPP_DM_EXPORT" "DN2CPP_DM_GET" "DN2CPP_DM_TRIMFALLBACK" "DN2CPP_DM_CONNECTED" \
    "DN2CPP_DM_SIGNAL" "DN2CPP_DM_TIMEOUT" "DN2CPP_DM_TOSIGNAL" \
    "DN2CPP_DM_ASYNC" "DN2CPP_DM_SYNCCTX" \
    "DN2CPP_DM_RC_SWAP first=" "DN2CPP_DM_RC_SWAP_RELEASED" \
    "DN2CPP_DM_RC_RELEASED" "DN2CPP_DM_RC_BINDING" "DN2CPP_DM_RC_DISPOSE" \
    "DN2CPP_DM_RC_STRESS" "DN2CPP_DM_RC_DONE" \
    "DN2CPP_DM_ENTER_TREE" "DN2CPP_DM_NOTIFY_ENTER_TREE" \
    "DN2CPP_DM_NOTIFY_READY" "DN2CPP_DM_ENGINE" \
    "DN2CPP_DM_PARITY_PROCESS" "DN2CPP_DM_PHYSICS" "DN2CPP_DM_EVKEY" \
    "DN2CPP_DM_INPUT" "DN2CPP_DM_UNHANDLED" "DN2CPP_DM_PARITY_DONE" \
    "DN2CPP_DM_NOTIFY_EXIT_TREE" "DN2CPP_DM_EXIT_TREE" \
    "DN2CPP_DM_FAULTPOSTED" "DN2CPP_DM_FAULTCALL" "DN2CPP_DM_FAULTPROBE"; do
    n="$(grep -c "$once" "$LOG" || true)"
    if [ "$n" -ne 1 ]; then
        echo "FAIL: marker $once appeared $n times (expected exactly 1)" >&2
        exit 1
    fi
done
# Negative markers: the mono module must not have fallen over (it only logs and
# keeps running, so these never affect the exit code). The FaultProbe reports
# this gate asserts above are subtracted first — a scoped subtraction, argued in
# the sibling comment in gates/build-and-run-godot-dotnet-sample.sh.
FILTERED="$RUN/trim.no-expected-faults.log"
grep -v -e "DN2CPP_DM_FAULT_INJECTED" -e "unhandled managed exception in" "$LOG" >"$FILTERED" || true
for bad in \
    "GodotPlugins initialization failed" \
    "Failed to load hostfxr" \
    "ERROR" \
    "SCRIPT ERROR" \
    "ObjectDB instances leaked"; do
    if grep -q "$bad" "$FILTERED"; then
        echo "FAIL: log contains \"$bad\"" >&2
        exit 1
    fi
done
gate_cache_commit
echo "OK"
