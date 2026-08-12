#!/usr/bin/env bash
# Godot .NET-module (mono module) scripted-node E2E gate: a real engine binary
# built from the pinned godotengine/godot clone loads the dn2cpp-produced
# mono-module library, and a scene whose nodes carry real C# scripts
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
# plus the GDExtension-lane parity surface on res://ParityNode.cs (below),
# plus a generic-base script (res://GenericConcrete.cs deriving the abstract
# generic GenericBase<T> : Node) whose _Ready fires only when the assembly's
# [AssemblyHasScripts] attribute survived transpilation with its open-generic
# base element intact (a dropped element takes the
# whole attribute and every script registration with it),
# plus the boundary-degrade probe on res://FaultProbe.cs: a managed
# fault raised at each of this lane's two engine<->managed boundaries — the
# script-call one GodotSharp owns and the frame-callback one dn2cpp owns —
# must cost its own call and nothing else. The failure mode is the second one
# taking the whole process down, so the assertion is not that an error appears
# but that EVERY marker above still does, in the same engine run, after it,
# plus — in the same file but on a different subject — the STARTUP
# STATIC-CONSTRUCTOR pass: a type whose .cctor fails is named in one end-of-init
# summary line on the engine's error channel, with an exact count, while the
# absent-class case that every headless template hits stays on the trace channel
# where it cannot become a standing false alarm; and the disabled type is then
# dead for real, its first use re-raising the original exception. Armed by
# DN2CPP_DM_SAMPLE_CCTOR_FAULT, which this gate alone sets. This one is NOT a
# boundary test and must not be pruned with the two above.
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

OUT=gates/out-godot-dotnet-sample
ROOT="$GODOT_DOTNET_ROOT"
PINNED_COMMIT=a13da4feb8d8aefc283c3763d33a2f170a18d541
ABI_EXPECTED=gates/expected/godot-dotnet-abi.sha256

if ! godot_dotnet_root_ok || [ ! -x "$GODOT_DOTNET_EDITOR" ] || [ ! -x "$GODOT_DOTNET_TEMPLATE" ] \
    || [ ! -f "$ROOT/pin.txt" ]; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/7 Pin + interop-ABI tripwire =="
godot_dotnet_pin_abi_check "$PINNED_COMMIT" "$ABI_EXPECTED"

echo "== 2/7 Building the mono-module shared library =="
godot_dotnet_transpile "$OUT"
# Everything past this point — the native link, import, export-pack, the engine
# run and its marker asserts — is a pure function of the transpile surface in
# $OUT, the gate-helper set the key hashes wholesale, the sample project,
# the app/GodotSharp
# assemblies and the pinned engine binaries (pin + editor/template identity live
# in the context string). The pin/ABI tripwire above stays always-on, so a
# drifted clone still fails on a hit. The link is deliberately below the check:
# a hit exits here and never names $DYLIB, so building it first is pure waste.
if gate_cache_check "$OUT" \
    "godot-dotnet-sample|pin=$(file_text "$ROOT/pin.txt")|editor=$(file_sig_deref "$GODOT_DOTNET_EDITOR")|template=$(file_sig_deref "$GODOT_DOTNET_TEMPLATE")" \
    "$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll" \
    "$GODOT_DOTNET_GODOTSHARP" \
    "$GODOT_DOTNET_SAMPLE_DIR" \
    samples/godot-dotnet/GameExtensionProbe \
    gates/stage-gdextension-libs.sh; then
    { gate_cache_hit_msg; exit 0; }
fi
godot_dotnet_link_lib "$OUT"
DYLIB="$OUT/$(lib_name DotnetSample)"

echo "== 3/7 Importing the sample project (mono editor, headless) =="
RUN="$OUT/run"
rm -rf "$RUN"
mkdir -p "$RUN"
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 300 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$GODOT_DOTNET_SAMPLE_DIR" --import >"$RUN/import.log" 2>&1 || true

echo "== 4/7 Exporting the scripted-scene pack (--export-pack) =="
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

echo "== 5/7 Assembling the loose run dir =="
# try_load_native_aot_library opens <exe_dir>/data_<AssemblyName>_<platform>_<arch>/
# <AssemblyName>.<dylib|dll|so> (no lib prefix — the NativeAOT publish naming,
# not ours). Both halves are per-platform and neither is $DN2CPP_OS: the engine
# maps Linux to "linuxbsd" (see GODOT_DOTNET_PLATFORM in _godot_dotnet.sh).
ARCH="$(godot_dotnet_host_arch)"
cp -L "$GODOT_DOTNET_TEMPLATE" "$RUN/godot$EXE_EXT"
chmod +x "$RUN/godot$EXE_EXT"
mkdir -p "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH"
cp -f "$DYLIB" "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH/DotnetSample.$LIB_EXT"

echo "== 6/7 Running the scripted scene in the real engine =="
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
# DN2CPP_DM_SAMPLE_CCTOR_FAULT arms res://FaultProbe.cs's CctorFaultProbe
# — the deliberately-failing startup static constructor whose end-of-init summary
# line this gate asserts below. It is set HERE and only here: the same assembly is
# transpiled by the handshake, trim and wasm gates, each of which asserts that a
# clean init logs no error at all, so an unconditional failure would redden three
# gates to assert one.
run_with_watchdog 120 env DN2CPP_DM_TRACE=1 DN2CPP_DM_SAMPLE_CCTOR_FAULT=1 \
    "$PWD/$RUN/godot$EXE_EXT" --headless \
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
    "DN2CPP_DM_TRIMFALLBACK class=Sprite2D isNode2D=True name=TrimProbe posOk=True managed=Sprite2D" \
    "DN2CPP_DM_CONNECTED True" \
    "DN2CPP_DM_SIGNAL amount=7" \
    "DN2CPP_DM_TIMEOUT" \
    "DN2CPP_DM_TOSIGNAL" \
    "DN2CPP_DM_ASYNC resumeOnMain=True workerDiffers=True delayOnMain=True delayElapsedOk=True" \
    "DN2CPP_DM_SYNCCTX mainHasCtx=True workerCtxNull=True postedOnMain=True" \
    "DN2CPP_DM_GENERIC value=42" \
    "DN2CPP_DM_PROCESS"; do
    grep -qF "$marker" "$LOG" \
        || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
done
# Generic-base script: the "Generic" node runs GenericConcrete,
# whose abstract generic base GenericBase<T> : Node lands in the assembly's
# source-generated [AssemblyHasScripts] Type[] as an OPEN generic definition
# (typeof(GenericBase<>)). If CppEmitter cannot render that element the whole
# attribute drops and the real GodotSharp LookupScriptsInAssembly registers ZERO
# scripts — EVERY marker above would then vanish, but the value=42 marker below
# (the base's _Ready dispatching through GenericBase<int>.Make()) is the one that
# names the cause. See samples/.../GenericBase.cs.
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
# SWAP_RELEASED and STRESS are pin-tolerant booleans, not raw counts, and that
# is deliberate: Boehm is conservative, so it does not promise to collect any
# GIVEN object — it can pin a few through a stale stack word, and which few is
# codegen luck (identical IL released 1000/1000 under macOS/clang and 993..996
# under Windows/MSVC). "enough" allows a bounded shortfall; "ledgerOk" is what
# stops that tolerance from hiding a real bug, asserting off MyResource's per-id
# ledger that every object that DID finalize did so exactly once. Same rule as
# the finalizer-flood gate. A regression these still catch whole: a handle left
# strong releases NONE, not "a few fewer".
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
# Boundary degrade (res://FaultProbe.cs). Two faults, two owners, one
# requirement — the engine is still running afterwards, which every marker
# asserted above and below this block already proves by having been printed in
# the same run. What is checked here is that the faults HAPPENED and were
# REPORTED where a person would look:
#
#   - FAULTPOSTED/FAULTCALL prove each boundary was actually crossed. Without
#     them an unregistered script class — no fault, no error, no output — would
#     satisfy an assertion that only looked for the absence of a crash.
#   - The `^ERROR:` anchor is the load-bearing half. The shared reporter falls
#     back to stderr whenever the lane's sink is missing or itself throws, and
#     stderr is not the engine's error log: an exported game's player never sees
#     it and the editor's Errors panel never shows it. Matching the engine's own
#     error prefix is what tells "the sink worked" apart from "we degraded to
#     the fallback", which no plain substring grep can distinguish.
#   - The frame-callback report names the STAGE. That boundary has four of them
#     and only one had no managed handler beneath it; a report that could not
#     say which would leave the next reader to rediscover that.
#   - The _Process fault carries NO dn2cpp prefix on purpose: GodotSharp's own
#     CSharpInstanceBridge.Call catch handles it, through the very
#     ExceptionUtils.LogException the dn2cpp sink also calls. Asserting both
#     shapes is what pins the two lanes' reports to one route.
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
# The startup static-constructor pass says at the END which types it left
# unusable (res://FaultProbe.cs's CctorFaultProbe). Three separate claims,
# and each of them can fail while the other two hold:
#
#   - the per-failure report still fires and still names the TYPE, not just the
#     exception — the per-fault behaviour the summary indexes, not replaces;
#   - ONE summary line, on the engine's error channel, with an exact count. The
#     count is asserted as `1` rather than matched loosely because the whole
#     value of the line is that it is the only place the SET is visible: a
#     summary that quietly dropped a type would still read as a summary. This
#     run has exactly one loud cctor failure — the injected one — which is
#     itself worth pinning, since a second would mean a real regression the
#     other gates report as nothing at all;
#   - and the type is then genuinely DEAD: the first use re-raises the original
#     exception rather than reading a zeroed static or re-running the body.
#     Without this the summary could be truthful about a type that still worked.
grep -q "^ERROR: dn2cpp: unhandled managed exception in the startup static constructor of CctorFaultProbe$" "$LOG" \
    || { echo "FAIL: the startup cctor failure was not reported, or did not name the type" >&2; exit 1; }
grep -q "^ERROR: dn2cpp: 1 type(s) left unusable by a failed startup static constructor (every use of one re-raises its failure): CctorFaultProbe$" "$LOG" \
    || { echo "FAIL: the startup pass emitted no end-of-init summary of the types it disabled" >&2; exit 1; }
grep -qF "DN2CPP_DM_CCTORPROBE reraised=InvalidOperationException" "$LOG" \
    || { echo "FAIL: a failed startup cctor did not re-raise at the first use of its type" >&2; exit 1; }
# The absent-class arm of the same pass reports on the TRACE channel, never the
# error one, and that split is what this must not undo: every
# headless template hits it (GodotSharp declares editor-only engine wrappers
# unconditionally), so a loud line would be a standing false alarm in every
# shipped game's Errors panel. Asserting the trace line is what keeps the case
# covered rather than merely quiet — and it is a real engine build with real
# absent classes, so an empty list here means the filter stopped matching.
grep -q "^dn2cpp-dm: [1-9][0-9]* type(s) unusable - class absent from this engine build: " "$LOG" \
    || { echo "FAIL: the startup pass did not summarize the absent-class types on the trace channel" >&2; exit 1; }
# Exactly-once markers: a double construction (the ctor-invoke risk: a
# reflective ctor invoke allocating a NEW instance instead of running
# in-place), a broken _Process latch, a double-dispatched signal handler or a
# double-resumed awaiter shows up as a count != 1.
for once in "DN2CPP_DM_CTOR" "DN2CPP_DM_READY" "DN2CPP_DM_PROCESS" \
    "DN2CPP_DM_GENERIC value=42" \
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
    "DN2CPP_DM_FAULTPOSTED" "DN2CPP_DM_FAULTCALL" "DN2CPP_DM_FAULTPROBE" \
    "DN2CPP_DM_CCTORPROBE" "1 type(s) left unusable"; do
    n="$(grep -c "$once" "$LOG" || true)"
    if [ "$n" -ne 1 ]; then
        echo "FAIL: marker $once appeared $n times (expected exactly 1)" >&2
        exit 1
    fi
done
# Negative markers: the mono module must not have fallen over (it only logs and
# keeps running, so these never affect the exit code).
#
# The sweep runs over the log with the FaultProbe reports subtracted, and that is
# a scoped subtraction rather than a weakened check: the removed lines are
# exactly the ones asserted present above, every other engine error still
# reddens the gate, and the alternative — a second export and a second engine
# run — would have bought a weaker result, since what makes the degrade
# convincing is that the rest of THIS run's markers survived it. Both removed
# shapes are unique to that section (the injected token, and the reporter's own
# phrase), and GD.PushError's continuation lines carry no "ERROR" prefix, so
# nothing else is taken with them.
#
# The third removed shape is the cctor summary line, which carries neither of the
# other two markers — it names no exception (there is none: it is a fact about
# the pass) and it is not itself a fault report. It is subtracted on the same
# terms: asserted present, verbatim and exactly once, a few lines above.
FILTERED="$RUN/sample.no-expected-faults.log"
grep -v -e "DN2CPP_DM_FAULT_INJECTED" -e "unhandled managed exception in" \
    -e "left unusable by a failed startup static constructor" "$LOG" >"$FILTERED" || true
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

echo "== 7/7 The game's OWN GDExtension in the drop-in run layout =="
# SUBJECT: not this lane's C#, and not dn2cpp's emitted anything — the ENGINE's
# resolution of a *game-owned* GDExtension binary against a drop-in run
# directory, and gates/stage-gdextension-libs.sh, which is what puts it there.
# This section lives in this bucket because this bucket already owns the
# assembled run layout (a template engine binary + godot.pck + data_<Assembly>_*
# built by hand at 5/7), and that layout IS the subject: it is the one shape in
# which nothing stages the binary, because `--export-pack` runs no export
# platform and therefore no GDExtension export plugin, and therefore
# `add_shared_object()` is never called. An .app / .apk / web export has no such
# hole and needs no such staging. Do not prune this by theme: it asserts nothing
# about scripted nodes, and its subject has no other gate.
#
# It runs on its own throwaway project (samples/godot-dotnet/GameExtensionProbe)
# rather than on DotnetSample. Six gates plus dist/smoke-test.sh transpile,
# pack or run DotnetSample, four of them asserting a clean engine log, so giving
# that project an extension nothing else stages would have reddened all of them
# to assert one thing here.
#
# NEGATIVE CONTROL FIRST, and it is the half that makes the positive one mean
# something: a run whose engine simply ignored the extension would satisfy any
# "no error" assert, so the section proves the engine really is looking — and
# looking exactly where the mechanism says — before it proves the staging fixes
# it. On macOS the failure it reproduces reads
#     ERROR: Can't open dynamic library: lib/libgameextprobe.dylib.
#     ERROR: Can't open GDExtension dynamic library: 'res://lib/probe_extension.gdextension'.
# which is the bare RELATIVE path a res:// spelling globalizes to when
# ProjectSettings::resource_path is empty (a pck-backed run). Windows takes
# open_library's other arm ("GDExtension dynamic library not found") because its
# open_dynamic_library tests FileAccess::exists first, so the asserts below
# anchor on the substring the two spellings share.
PROBE_SRC=samples/godot-dotnet/GameExtensionProbe
PROBE_PROJ="$OUT/extprobe/project"
PROBE_RUN="$OUT/extprobe/run"
rm -rf "$OUT/extprobe"
mkdir -p "$PROBE_PROJ" "$PROBE_RUN"
cp -R "$PROBE_SRC/." "$PROBE_PROJ/"

# The stub extension. Built here rather than committed because a checked-in
# .dylib is unreviewable and single-platform; the vendored header is the same
# one runtime/godot builds dn2cpp's own bridge from, so no godot-cpp is needed.
#
# -fvisibility=hidden with one __attribute__((visibility("default"))) on the
# entry symbol: the same discipline the Web lane documents, and here it also
# means `dump_exports` on the artifact is a one-line answer to "is the entry
# symbol spelled the way the .gdextension says".
PROBE_LIB="$PROBE_PROJ/lib/$(lib_name gameextprobe)"
if is_msvc_compiler; then
    # The engine loads what the host toolchain produced, so the stub has to come
    # from the compiler the rest of the run was built with. `-`-prefixed switches
    # (cl.exe takes '-' and '/' alike) sidestep Git Bash's argv path-mangling, and
    # the source's __declspec(dllexport) is what -fvisibility=hidden's single
    # default-visibility entry stands in for on the GNU side.
    probe_cc=(cl.exe -nologo -LD -I third_party -Fo:"$OUT/extprobe/" -Fe:"$PROBE_LIB")
else
    probe_cc=(clang -shared -O0 -fvisibility=hidden -I third_party -o "$PROBE_LIB")
    [ "$DN2CPP_OS" = windows ] || probe_cc+=(-fPIC)
fi
if ! "${probe_cc[@]}" "$PROBE_PROJ/probe_extension.c" \
        >"$OUT/extprobe/cc.log" 2>&1; then
    echo "FAIL: could not build the stub GDExtension" >&2
    cat "$OUT/extprobe/cc.log" >&2
    exit 1
fi

run_with_watchdog 300 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$PWD/$PROBE_PROJ" --import >"$OUT/extprobe/import.log" 2>&1 || true
[ -f "$PROBE_PROJ/.godot/extension_list.cfg" ] || {
    echo "FAIL: the import wrote no .godot/extension_list.cfg — the project's own" >&2
    echo "      extension was not registered, so the rest of this section would" >&2
    echo "      assert nothing" >&2
    cat "$OUT/extprobe/import.log" >&2
    exit 1
}

PROBE_PCK="$PWD/$PROBE_RUN/godot.pck"
if ! godot_export_step 300 "$OUT/extprobe/export.log" "$PROBE_PCK" \
    "$GODOT_DOTNET_EDITOR" --headless \
    --path "$PWD/$PROBE_PROJ" --export-pack probe-pack "$PROBE_PCK"; then
    echo "FAIL: --export-pack probe-pack failed (see below)" >&2
    cat "$OUT/extprobe/export.log" >&2
    exit 1
fi
cp -L "$GODOT_DOTNET_TEMPLATE" "$PROBE_RUN/godot$EXE_EXT"
chmod +x "$PROBE_RUN/godot$EXE_EXT"

probe_run() {   # probe_run LOGFILE — one bounded engine run of the probe game
    local log="$1" rc=0
    run_with_watchdog 120 "$PWD/$PROBE_RUN/godot$EXE_EXT" --headless \
        --quit-after 300 >"$log" 2>&1 || rc=$?
    [ "$rc" -eq 0 ] || { echo "FAIL: probe engine run exited $rc" >&2; cat "$log" >&2; exit 1; }
}

UNSTAGED_LOG="$OUT/extprobe/unstaged.log"
probe_run "$UNSTAGED_LOG"
if ! grep -q "GAMEEXT_PROBE_SCENE_READY" "$UNSTAGED_LOG"; then
    echo "FAIL: the probe game did not reach its scene without the extension —" >&2
    echo "      a failed GDExtension load must be an engine error, not a fatal," >&2
    echo "      or this section's negative control is measuring the wrong thing" >&2
    cat "$UNSTAGED_LOG" >&2; exit 1
fi
if ! grep -q "GDExtension dynamic library" "$UNSTAGED_LOG"; then
    echo "FAIL: the unstaged run did NOT fail to open the game's own GDExtension." >&2
    echo "      Either the engine stopped resolving res:// library paths against the" >&2
    echo "      run directory, or the pack no longer carries the .gdextension — in" >&2
    echo "      both cases the staging this section then asserts would be asserting" >&2
    echo "      nothing." >&2
    cat "$UNSTAGED_LOG" >&2; exit 1
fi
if grep -q "GAMEEXT_PROBE_EXTENSION_INIT" "$UNSTAGED_LOG"; then
    echo "FAIL: the extension initialized WITHOUT being staged — the run dir must" >&2
    echo "      not be inheriting it from anywhere else" >&2
    cat "$UNSTAGED_LOG" >&2; exit 1
fi

# The staging step, run exactly as a caller of the drop-in flow runs it.
if ! ./gates/stage-gdextension-libs.sh "$PROBE_PROJ" "$PROBE_RUN" \
        >"$OUT/extprobe/stage.log" 2>&1; then
    echo "FAIL: stage-gdextension-libs.sh failed" >&2
    cat "$OUT/extprobe/stage.log" >&2; exit 1
fi
cat "$OUT/extprobe/stage.log"
# It stages FLAT, by basename — tier 2 of every desktop open_dynamic_library —
# and not into a res://-shaped lib/ subdirectory, which resolves only through
# tier 1 and so only while the CWD happens to be the run dir. Pinning that here
# is what stops a later "tidy" from moving it into lib/ and passing anyway.
if [ ! -f "$PROBE_RUN/$(lib_name gameextprobe)" ]; then
    echo "FAIL: staging did not place $(lib_name gameextprobe) flat in $PROBE_RUN" >&2
    ls -la "$PROBE_RUN" >&2; exit 1
fi
# The release entry, never the .debug. one — whose key carries MORE tags and so
# would win any selection rule that ranked by tag count without testing the tags
# themselves. The project deliberately ships no lib/debug/, so that mistake has
# somewhere wrong to point rather than silently staging the same file.
if [ -e "$PROBE_PROJ/lib/debug" ]; then
    echo "FAIL: the probe project must not carry a lib/debug — its absence is what" >&2
    echo "      makes a debug-selecting regression fail loudly instead of passing" >&2
    exit 1
fi

STAGED_LOG="$OUT/extprobe/staged.log"
probe_run "$STAGED_LOG"
for marker in "GAMEEXT_PROBE_EXTENSION_INIT" "GAMEEXT_PROBE_SCENE_READY"; do
    if ! grep -q "$marker" "$STAGED_LOG"; then
        echo "FAIL: staged run is missing marker \"$marker\"" >&2
        cat "$STAGED_LOG" >&2; exit 1
    fi
done
if grep -qE "Can't open|dynamic library not found" "$STAGED_LOG"; then
    echo "FAIL: the staged run still failed to open a dynamic library" >&2
    cat "$STAGED_LOG" >&2; exit 1
fi
echo "   the game's own GDExtension loads from the drop-in run dir after staging"

gate_cache_commit
echo "OK"
