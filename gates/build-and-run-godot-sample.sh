#!/usr/bin/env bash
# End-to-end Godot pipeline: C# -> IL -> C++ -> GDExtension dylib -> run in Godot.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/godot
GODOT=${GODOT:-godot}
PROJECT=samples/godot/godot-project

# This gate writes through user:// and may run where the real user profile is
# read-only. Keep all engine-owned state process-private on every host.
GODOT_USER_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-godot-sample.XXXXXX")
godot_sample_cleanup() {
    rm -rf "$GODOT_USER_ROOT"
}
gate_add_exit_hook godot_sample_cleanup

echo "== 1/7 Building sample C# assembly =="
build_proj samples/godot/GodotSample/GodotSample.csproj

echo "== 2/7 Transpiling IL -> C++ (GDExtension mode, shim + tree-shaken CoreLib) =="
# The real CoreLib rides along for the Task.Run pool probe (async/await +
# Func<T> need it); the tree-shake keeps the pulled-in surface small.
corelib=$(locate_corelib)
invoke_cli \
    "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
    -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
    -r "$corelib" \
    -o "$OUT" --gdextension

# Cache: the transpile is deterministic, so with the generated output, the
# input dlls, the engine version, and the Godot project unchanged since the
# last green pass, the compile and both engine runs would only repeat it.
if gate_cache_check "$OUT" \
        "godot-sample|godot=$(or_none "$(first_line "$(run_godot_isolated "$GODOT_USER_ROOT" "$GODOT" --version 2>/dev/null)")")" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
        "$PROJECT"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/7 Compiling GDExtension shared library =="
mkdir -p "$PROJECT/bin"
compile_gdextension "$OUT" "$PROJECT/bin/$(lib_name "$DN2CPP_GDEXT_LIB")"

echo "== 4/7 Importing Godot project (registers the extension) =="
# Note: the first import may abort during editor teardown (Godot headless
# doc-gen bug; harmless — .godot/extension_list.cfg is already written).
run_with_watchdog 300 run_godot_isolated "$GODOT_USER_ROOT" \
    "$GODOT" --headless --path "$PROJECT" --import >/dev/null 2>&1 || true

echo "== 5/7 Running inside Godot (headless, GDScript -> C#) =="
rc=0
SCRIPT_OUT=$(run_with_watchdog 120 run_godot_isolated "$GODOT_USER_ROOT" \
    "$GODOT" --headless --path "$PROJECT" --script res://test.gd 2>&1) || rc=$?
echo "$SCRIPT_OUT"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: script run exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi
# test.gd's asserts abort the script on failure, but Godot still exits 0, so the
# script run alone can't gate. Assert the marker each section prints on success;
# a parse error or failed assert stops the script before its marker is reached.

# ClassDB registration has no fixed class limit. Two assertions, because
# they fail differently: the marker below proves every registered class was also
# instantiable, while this one reads the total straight out of the runtime's own
# init line — so it stays honest even if somebody later trims the sample's
# GateManyClassNN set back under the old cap without noticing what it was for.
registered=$(LC_ALL=C sed -n 's/.*GDExtension initialized:.*[^0-9]\([0-9][0-9]*\) class(es) total.*/\1/p' \
    <<<"$SCRIPT_OUT")
registered=${registered%%$'\n'*}
if [ -z "$registered" ]; then
    echo "FAIL: runtime did not report a registered class total" >&2
    exit 1
fi
if [ "$registered" -le 64 ]; then
    echo "FAIL: only $registered classes registered — the sample must exceed the" >&2
    echo "      64-entry cap that was removed, or this gate proves nothing" >&2
    exit 1
fi
grep -q "registration past the old 64-class cap works" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: past-64 class registration marker missing" >&2; exit 1; }

grep -q "richer Variant payloads matches native" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: richer Variant payloads marker missing" >&2; exit 1; }
grep -q "bare Variant export boundary matches native" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: bare Variant export boundary marker missing" >&2; exit 1; }
grep -q "Dictionary marshalling matches native" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Dictionary marshalling marker missing" >&2; exit 1; }
grep -q "Object Variant payload matches native" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Object Variant payload marker missing" >&2; exit 1; }
grep -q "Object->typed-shim wrap matches native" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Object->typed-shim wrap marker missing" >&2; exit 1; }
grep -q "Variant-held RefCounted lifetime matches expected" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Variant-held RefCounted lifetime (decode must keep a reference)" >&2; exit 1; }
grep -q "Godot.RefCounted lifecycle matches expected" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: RefCounted lifecycle marker missing (newobj construction + init_ref + return-value wrap)" >&2; exit 1; }
grep -q "Godot.RefCounted ClassDB construction matches expected" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: RefCounted ClassDB-driven construction marker missing (GDScript .new())" >&2; exit 1; }
grep -q "Godot.RefCounted C#-new reclamation drained" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: RefCounted C#-new reclamation drain marker missing" >&2; exit 1; }
# Registered RefCounted built by C# `new` must be reclaimed, not leaked.
# After the deterministic GC drain the section must exit clean; a surviving
# engine object shows up here as Godot's ObjectDB leak warning.
grep -qE "ObjectDB instances (were )?leaked at exit" <<<"$SCRIPT_OUT" \
    && { echo "FAIL: RefCounted C#-new leaked (ObjectDB instances leaked at exit)" >&2; exit 1; }
# A registered RefCounted whose ctor throws must still register its finalizer
# before the ctor runs, so the already-constructed engine object is reclaimed
# rather than leaked. The marker proves the section ran; the shared ObjectDB
# check above catches a leak of the throwing-ctor engine object.
grep -q "throwing-ctor registered RefCounted reclaimed" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: throwing-ctor registered RefCounted marker missing" >&2; exit 1; }
# A registered RefCounted subclass handed to the caller as a Variant return
# value, with the C# side's own local dropped the instant the call returns:
# the strong/weak toggle must keep the shim (state + overrides) alive while
# the caller still holds the only remaining reference, instead of degrading
# to the engine's default (override-less) behavior.
grep -q "RefCounted engine hand-off keeps C# behavior alive" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: RefCounted engine hand-off did not keep C# behavior alive" >&2; exit 1; }
# Once every holder (C# and the caller) drops the instance, the toggle must
# release it back to the ordinary collectible+finalizable path — proven a few
# frames later, once a per-frame sweep has released the anchor.
grep -q "RefCounted engine hand-off reclaimed once the engine dropped it too" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: RefCounted engine hand-off was never reclaimed" >&2; exit 1; }
# A StringName reference leaked by the engine-object construction helpers is
# invisible to ObjectDB, so the runtime reports a non-zero net at library deinit.
grep -q "construct StringName leak" <<<"$SCRIPT_OUT" \
    && { echo "FAIL: construct helper leaked a StringName reference" >&2; exit 1; }
# Engine worker thread calling managed code: WorkerThreadPool runs the callable
# on an engine-spawned thread; the entry bridge must GC-register it first. A
# missing registration crashes the process before this marker prints.
grep -q "engine worker thread managed call = Hello, worker" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: engine worker-thread managed call (GC thread registration)" >&2; exit 1; }
# Signal argument carrying a non-Node engine object (RefCounted-derived
# registered class): the emit-side conversion must yield an Object Variant.
grep -q "signal Object argument round-trips as a live object" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: signal Object argument (non-Node shim converted to NIL)" >&2; exit 1; }
# Manual-drain starvation: with no managed node left in the tree, queued
# finalizers must still run every frame (node-independent main-loop frame
# drain), not pile up until GC.WaitForPendingFinalizers or library deinit.
grep -q "per-frame finalizer drain runs with no managed node in tree" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: finalizer drain starvation (no node-independent per-frame drain)" >&2; exit 1; }
# A plain managed class with a real C# destructor (~T()) must transpile (the
# compiler-generated base.Finalize() MemberRef + .override MethodImpl row) and
# its Finalize body must run through the same per-frame drain.
grep -q "managed destructor ran via per-frame drain" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: managed C# destructor (transpile or drain)" >&2; exit 1; }
# Generalized engine virtuals: the engine dispatches to NON-lifecycle `_*`
# virtuals overridden in C#, through the generic reverse-dispatch bridge (the
# lifecycle/input asserts in the scene run below now ride the same mechanism).
# GDScript builds a C# MyControl via ClassDB .new() and calls the public engine
# methods that invoke the overrides: _GetMinimumSize (Vector2 return) and
# _GetTooltip (Vector2 argument + String return) — two distinct non-void shapes.
grep -q "GDScript: engine virtual _GetMinimumSize dispatch matches" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: engine virtual _GetMinimumSize (Vector2 return reverse dispatch)" >&2; exit 1; }
grep -q "GDScript: engine virtual _GetTooltip dispatch matches" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: engine virtual _GetTooltip (Vector2 arg + String return reverse dispatch)" >&2; exit 1; }
# The export boundary (GdTypeOf) over the kinds it used to reject: a user-enum
# [Export] property/method registers and marshals as INT, an enum-argument
# signal registers (it used to fail the transpile outright), and
# Quaternion/Plane/AABB cross as their own Variant types — properties both
# directions, plus method argument + return for Quaternion and AABB.
grep -q "GDScript: enum export property + method round trip" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: enum [Export] property / exported method (GdTypeOf enum arm)" >&2; exit 1; }
grep -q "GDScript: enum signal registered + dispatches" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: enum-argument signal registration/dispatch (GdTypeOf enum arm)" >&2; exit 1; }
grep -q "GDScript: Quaternion/Plane/AABB export properties round trip" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Quaternion/Plane/AABB [Export] property round trip" >&2; exit 1; }
grep -q "GDScript: Quaternion/AABB exported method args round trip" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: Quaternion/AABB exported method argument+return" >&2; exit 1; }
# The virtual trampoline's Variant arm: an engine-dispatched override with a
# Variant ARGUMENT (and Variant return) — _post_process_key_value driven by a
# real AnimationPlayer advance. Guards the POD→shim decode in family 3
# (CopyPodToShim) and the return encode against real engine behavior.
grep -q "GDScript: virtual _PostProcessKeyValue Variant arm round-trips" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: virtual trampoline Variant argument arm (_PostProcessKeyValue)" >&2; exit 1; }
# A leading-underscore user method that is not an engine virtual (the `_On...`
# handler convention) must be registered as a normal ClassDB method bind.
grep -q "GDScript MyNode _OnScoreHandler(5) = 12" <<<"$SCRIPT_OUT" \
    || { echo "FAIL: leading-underscore user method not registered as a method bind" >&2; exit 1; }
grep -qE "SCRIPT ERROR|Parse Error" <<<"$SCRIPT_OUT" \
    && { echo "FAIL: GDScript reported a script/parse error" >&2; exit 1; }

echo "== 6/7 Running scene with a C# node (_Ready/_Process bridge) =="
rc=0
SCENE_OUT=$(run_with_watchdog 120 run_godot_isolated "$GODOT_USER_ROOT" \
    "$GODOT" --headless --path "$PROJECT" main.tscn --quit-after 3 2>&1) || rc=$?
echo "$SCENE_OUT"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: scene run exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi

# A managed fault escaping into an engine frame must DEGRADE, not end the run.
# MyFaultNode._Ready throws unconditionally; the bridge catches it at the
# virtual-dispatch boundary (NodeCallVirtualWithData) and routes it through
# dn2cpp_report_boundary_exception into the engine's error log, rather than
# letting it unwind through engine C frames. The mechanism predates every gate
# and no gate ran it: MyThrowingResource's throw is caught by C# in C#, and
# MyNode's SingletonSection wrapper is the opposite — it makes a throw fail.
# Three asserts, in the order they can fail:
#
#   1. the pre-throw marker, so the fixture is known to have run. Without it,
#      "no boundary report in the output" reads the same for the regression this
#      section exists to catch (the reporter went silent) and for a fixture that
#      never faulted at all — a class that quietly stopped registering with
#      ClassDB, or a _Ready that stopped resolving as the engine's `_ready`.
#      That second one is what would make the whole section vacuous.
#   2. the report itself, carrying the boundary NAME, the exception TYPE and its
#      MESSAGE. The namespace qualifier is optional in the pattern because the
#      reported name is the CLR type name (Dn2CppTypeInfo::name) and not the
#      short ClassDB name; what the assertion pins is the class and the virtual,
#      which are what point a reader at the code that threw.
#   3. exactly one report. dn2cpp_report_boundary_exception falls back to a
#      stderr print when the sink itself throws, and that fallback prints the
#      same sentence — a second copy means the engine channel did NOT carry the
#      message, which is the entire reason the Godot lane installs a sink.
#
# Nothing prints "survived": survival is what every MyNode / MyNode3D assertion
# below already proves. MyFaultNode is the first child of Root and _ready
# propagates to children in order, so all of them come from a run the engine
# only reached because the fault was contained. Line ORDER is deliberately not
# asserted — GD.Print goes to stdout and print_error to stderr, and a pipe
# buffers the two independently.
grep -q "MyFaultNode._Ready: about to throw across the engine boundary" <<<"$SCENE_OUT" \
    || { echo "FAIL: MyFaultNode._Ready never ran (class not registered, or _Ready not bound to the engine virtual) — the boundary assert below would be vacuous" >&2; exit 1; }
grep -qE "unhandled managed exception in ([A-Za-z0-9_]+\.)*MyFaultNode\._ready: System\.InvalidOperationException: .*DN2CPP_GD_FAULT_INJECTED" <<<"$SCENE_OUT" \
    || { echo "FAIL: engine->script boundary fault not reported with its boundary name, exception type and message" >&2; exit 1; }
boundary_reports=$(grep -cE "unhandled managed exception in ([A-Za-z0-9_]+\.)*MyFaultNode\._ready" <<<"$SCENE_OUT" || true)
[ "$boundary_reports" -eq 1 ] \
    || { echo "FAIL: boundary fault reported $boundary_reports times (expected 1; a second copy is the stderr fallback, i.e. the engine sink did not carry it)" >&2; exit 1; }

# String marshalling: assert the String-marshalling engine calls in MyNode._Ready.
# GetClass() must report the registered extension class name (String return);
# the SetEditorDescription/GetEditorDescription round trip must preserve the
# string verbatim (String argument + String return).
grep -q "MyNode: GetClass() = MyNode" <<<"$SCENE_OUT" \
    || { echo "FAIL: GetClass() String return" >&2; exit 1; }
grep -q "MyNode: GetEditorDescription() = hello from C#" <<<"$SCENE_OUT" \
    || { echo "FAIL: String arg+return round trip" >&2; exit 1; }

# StringName + NodePath marshalling. SetName/GetName is a StringName
# round trip; HasNode(".") exercises a NodePath argument; GetPath() returns a
# NodePath whose last segment is the renamed node name.
grep -q "MyNode: GetName() after SetName = RenamedNode" <<<"$SCENE_OUT" \
    || { echo "FAIL: StringName arg+return round trip" >&2; exit 1; }
grep -q 'MyNode: HasNode(".") = True' <<<"$SCENE_OUT" \
    || { echo "FAIL: NodePath argument" >&2; exit 1; }
grep -q "MyNode: GetPath() = .*RenamedNode" <<<"$SCENE_OUT" \
    || { echo "FAIL: NodePath return" >&2; exit 1; }

# Object-return marshalling: an engine method returning an Object, wrapped as a managed shim
# instance. GetParent() returns the "Root" node; calling GetName() on the wrapper
# drives a further engine call through the borrowed handle.
grep -q "MyNode: GetParent().GetName() = Root" <<<"$SCENE_OUT" \
    || { echo "FAIL: Object return wrapping" >&2; exit 1; }

# The _Notification(int) engine virtual, bridged via the GDExtension
# notification_func slot (distinct from the _Ready/_Process virtual-call path).
# The node receives NOTIFICATION_ENTER_TREE (10) and NOTIFICATION_READY (13) — both
# fire deterministically in headless — and the printed value must equal the exact
# Godot constant, proving the int argument is marshalled correctly.
# Virtual resolution must match parameter types, not just name+arity: the
# sample's unrelated _Process(int) overload must never be bound to the
# engine's _process(double) (it prints this FAIL line if dispatched).
grep -q "FAIL: _Process(int) dispatched as an engine virtual" <<<"$SCENE_OUT" \
    && { echo "FAIL: _Process(int) was bound to the engine virtual (type check missing)" >&2; exit 1; }
grep -q "MyNode._Notification: ENTER_TREE what=10" <<<"$SCENE_OUT" \
    || { echo "FAIL: _Notification ENTER_TREE (int arg marshalling)" >&2; exit 1; }
grep -q "MyNode._Notification: READY what=13" <<<"$SCENE_OUT" \
    || { echo "FAIL: _Notification READY (int arg marshalling)" >&2; exit 1; }

# Input engine virtuals with InputEvent object-arg marshalling. injector.gd
# synthesizes a key press and pushes it through the viewport; the engine delivers
# _Input (and _UnhandledInput) to the C# node. The bridge wraps the borrowed
# InputEvent engine object into a managed shim, and IsPressed() on that wrapper
# (an engine call through the planted handle) returns True for the pressed key.
grep -q "MyNode._Input: received InputEvent, IsPressed=True" <<<"$SCENE_OUT" \
    || { echo "FAIL: _Input InputEvent object-arg marshalling" >&2; exit 1; }
grep -q "MyNode._UnhandledInput: received InputEvent, IsPressed=True" <<<"$SCENE_OUT" \
    || { echo "FAIL: _UnhandledInput InputEvent object-arg marshalling" >&2; exit 1; }

# Enums & bitfields: engine-class enum types generated from the API dump and
# marshalled through the generic ptrcall (64-bit int on the wire, int32-backed
# C# enum). Covers an enum argument + enum return via the set/get methods, the
# enum-typed property accessors, a [Flags] bitfield composed with | that
# round-trips through the engine, and the generated global enum constants.
grep -q "MyNode: GetProcessMode() after SetProcessMode(Always) = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: enum arg+return round trip (SetProcessMode/GetProcessMode)" >&2; exit 1; }
grep -q "MyNode: ProcessMode property round trip (Pausable) = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: enum-typed property accessors" >&2; exit 1; }
grep -q "MyNode: ProcessThreadMessages bitfield round trip (All) = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: bitfield flags round trip" >&2; exit 1; }
grep -q "MyNode: global enum constants match engine values = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: global enum constant generation" >&2; exit 1; }

# Math value types + RID in engine calls: the builtin PODs pass through the
# generic ptrcall by pointer to their native-layout shim storage. Covers a
# Color argument+return round trip, a Rect2 return, an RID return (opaque
# 8-byte id), a Vector3 round trip, engine-computed Transform3D/Basis returns
# (validating the column-major transpose against real engine math), and a
# Transform3D argument.
grep -q "MyNode: Modulate Color round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Color arg+return round trip (Modulate)" >&2; exit 1; }
grep -q "MyNode: GetViewportRect() Rect2 return = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Rect2 return (GetViewportRect)" >&2; exit 1; }
grep -q "MyNode: GetViewportRid() RID is valid = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: RID return (GetViewportRid)" >&2; exit 1; }
grep -q "MyNode3D: Vector3 position round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Vector3 arg+return round trip (Node3D position)" >&2; exit 1; }
grep -q "MyNode3D: Transform3D return decodes engine rotation = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Transform3D return transpose (engine-computed rotation)" >&2; exit 1; }
grep -q "MyNode3D: Basis return decodes engine rotation = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Basis return transpose (engine-computed rotation)" >&2; exit 1; }
grep -q "MyNode3D: Transform3D argument round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Transform3D argument transpose" >&2; exit 1; }

# Packed arrays <-> C# arrays: engine methods taking/returning Packed*Array
# marshal to/from plain managed arrays at the call boundary (build engine
# value -> ptrcall -> destroy; returns copy out element-wise). Covers a byte[]
# round trip, an int[]/long[]/double[] scalar set, float[]+Color[] parallel
# arrays, Vector2[] struct elements, an engine-computed Vector3[] return, and
# a string[] return with per-element String conversion.
grep -q "MyNode: byte\[\] PackedByteArray round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: byte[] PackedByteArray round trip" >&2; exit 1; }
grep -q "MyNode: int\[\] PackedInt32Array round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: int[] PackedInt32Array round trip" >&2; exit 1; }
grep -q "MyNode: long\[\] PackedInt64Array return = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: long[] PackedInt64Array return" >&2; exit 1; }
grep -q "MyNode: float\[\]/Color\[\] packed round trips = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: float[]/Color[] packed round trips (Gradient)" >&2; exit 1; }
grep -q "MyNode: double\[\] PackedFloat64Array round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: double[] PackedFloat64Array round trip" >&2; exit 1; }
grep -q "MyNode: Vector2\[\] PackedVector2Array round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Vector2[] PackedVector2Array round trip" >&2; exit 1; }
grep -q "MyNode: Vector3\[\] PackedVector3Array return = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Vector3[] PackedVector3Array return (Curve3D bake)" >&2; exit 1; }
grep -q "MyNode: string\[\] PackedStringArray return = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: string[] PackedStringArray return (RegEx groups)" >&2; exit 1; }

# Bare Variant args/returns in engine calls: Object's meta storage gives a
# real encode -> engine -> decode round trip per payload kind (scalars,
# Vector3, a boxed Rect2, RID, a packed byte[]); Object.Set/Get routes a
# Variant through a *typed* engine property (the engine must understand the
# payload, not just echo it) — incl. a Transform3D whose transpose is
# cross-checked against the typed getter and engine euler math; a StringName
# payload decodes as its text; GD.Print stringifies a non-scalar Variant via
# the engine; and a RefCounted handed off through a Variant argument survives
# the C# reference being dropped and GC'd (the engine Variant holds its own
# reference), read back live a frame later.
grep -q "MyNode: Variant int/float/string meta round trips = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant int/float/string meta round trips" >&2; exit 1; }
grep -q "MyNode: Variant Vector3 meta round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Vector3 meta round trip" >&2; exit 1; }
grep -q "MyNode: Variant Rect2 meta round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Rect2 meta round trip (boxed big-POD payload)" >&2; exit 1; }
grep -q "MyNode: NIL Variant to big POD defaults = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: NIL Variant to big-POD conversion (must yield default, not crash)" >&2; exit 1; }
grep -q "MyNode: Variant Rid meta round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Rid meta round trip" >&2; exit 1; }
grep -q "MyNode: Variant byte\[\] meta round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant byte[] meta round trip (packed payload)" >&2; exit 1; }
grep -q "MyNode: Variant Set/Get position round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Set/Get through typed position property" >&2; exit 1; }
grep -q "MyNode: Variant StringName payload reads back = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant StringName payload decode" >&2; exit 1; }
grep -q "MyNode: variant print Vector3 = (1.5, 2.5, 3.5)" <<<"$SCENE_OUT" \
    || { echo "FAIL: GD.Print of a Vector3 Variant payload" >&2; exit 1; }
grep -q "MyNode3D: Variant RefCounted hand-off survives GC = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant RefCounted hand-off (engine Variant must hold its own reference)" >&2; exit 1; }
grep -q "MyNode3D: Variant Transform3D matches typed getter = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Transform3D return (boxed payload + transpose)" >&2; exit 1; }
grep -q "MyNode3D: Variant Transform3D argument decodes engine rotation = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Transform3D argument (encode-side transpose)" >&2; exit 1; }
# Engine containers (Array / Dictionary / typed arrays) as engine-backed
# wrapper classes with identity semantics. Covers: a typedarray::Node return
# with element access driving further engine calls; a typedarray::StringName
# return read as strings; an untyped Array argument the engine evaluates
# (Expression.Execute reads the elements); an Array carried in a Variant whose
# container is SHARED (a post-SetMeta Add is visible in the GetMeta read-back,
# and an indexer write through the read-back wrapper is visible in the
# original); a Dictionary argument the engine reads (HTTPClient query string);
# a Dictionary Variant round trip plus Count/indexer/ContainsKey/Keys; an
# engine-computed Dictionary return (RegExMatch named groups); and a
# typedarray::Dictionary return with per-element Dictionary access.
grep -q "MyNode: typed Array<Node> return + element access = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: typed Array<Node> return / element access (GetChildren)" >&2; exit 1; }
grep -q "MyNode: typed Array<string> groups = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: typed Array<string> return (GetGroups)" >&2; exit 1; }
grep -q "MyNode: typed Array<float> add + element read = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: typed Array<float> element round trip (Single payload)" >&2; exit 1; }
grep -q "MyNode: Array argument via Expression.Execute = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Array argument (Expression.Execute must read the elements)" >&2; exit 1; }
grep -q "MyNode: Array Variant round trip shares the container = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Array Variant payload / shared-container identity semantics" >&2; exit 1; }
grep -q "MyNode: Dictionary argument query string = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Dictionary argument (HTTPClient.QueryStringFromDict)" >&2; exit 1; }
grep -q "MyNode: Dictionary Variant round trip + Keys = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Dictionary Variant round trip / ContainsKey / Keys" >&2; exit 1; }
grep -q "MyNode: Dictionary missing-key read is non-mutating = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Dictionary missing-key read (operator[] must not insert)" >&2; exit 1; }
grep -q "MyNode: Dictionary.Values + foreach enumerators = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Dictionary.Values / duck-typed foreach enumerators" >&2; exit 1; }
grep -q "MyNode: Dictionary return (RegExMatch.Names) = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Dictionary return (RegExMatch named groups)" >&2; exit 1; }
grep -q "MyNode: typed Array<Dictionary> property list = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: typed Array<Dictionary> return (GetPropertyList)" >&2; exit 1; }
# Callable & Signal as first-class engine-call values. Covers: a standard
# target+method Callable argument through the generic ptrcall (the re-routed
# Connect — its behavior is asserted by the IsProcessing lines above — plus
# IsConnected reading the connection back); a delegate-backed Callable
# (callable_custom_create2) connected to a registered signal and invoked by
# the engine with the emitted argument; Callable.Call through the engine's
# vararg `call` for both forms; a Callable returned by a real engine getter
# (SceneMultiplayer.auth_callback) round-tripping both a custom callable
# (delegates recovered via its userdata) and a standard one (target + method
# decoded, the wrapped target driving further engine calls); a Variant-carried
# Callable surviving the engine meta store and still reaching the managed
# lambda; and a Signal Variant payload rebuilding its (owner, name) pair.
grep -q "MyNode: Callable argument IsConnected = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Callable argument (IsConnected via generic ptrcall)" >&2; exit 1; }
grep -q "MyNode: delegate Callable signal dispatch = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: delegate-backed Callable (custom callable trampoline)" >&2; exit 1; }
grep -q "MyNode: Callable.Call standard + zero-arg = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Callable.Call (engine vararg call routing)" >&2; exit 1; }
grep -q "MyNode: Callable return round trips (custom + standard) = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Callable return decode (SceneMultiplayer.auth_callback)" >&2; exit 1; }
grep -q "MyNode: Variant Callable round trip invokes = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Callable payload round trip" >&2; exit 1; }
grep -q "MyNode: Variant Signal round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Variant Signal payload round trip" >&2; exit 1; }
# Varargs + static engine methods. Object.Call routes through the variant-call
# interface (object_method_bind_call) with a params Variant[] tail — has_method
# via Call returns True for a real method and False for a bogus one, so the tail
# argument genuinely reaches the engine and the result Variant decodes to bool.
# JSON.Stringify / JSON.ParseString are static (null-instance) engine methods:
# Stringify(42) round-trips a Variant arg + String return, ParseString a String
# arg + Variant return.
grep -q "MyNode: vararg Object.Call round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: vararg Object.Call (object_method_bind_call variant-call path)" >&2; exit 1; }
grep -q "MyNode: static JSON round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: static engine method (JSON.Stringify/ParseString null-instance call)" >&2; exit 1; }
# Engine singletons. Before global_get_singleton there was no way to produce a
# Godot.ProjectSettings holding the engine's singleton pointer, so every call on
# one silently no-oped and GlobalizePath returned null. Both spellings must reach
# the engine: the static facade `ProjectSettings.GlobalizePath(...)` (what real
# game code writes, and what the mono-module lane compiles against the real
# GodotSharp) and the instance `ProjectSettings.Singleton.GlobalizePath(...)`.
# The assert is on a NON-EMPTY answer cross-checked against OS.GetUserDataDir(),
# not merely on the two agreeing: the broken build returned null from both, and
# "" == "" would have passed a comparison-only check.
grep -q "MyNode: singleton both spellings non-empty + agree = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: engine singleton receiver (facade + .Singleton must both reach a non-empty engine answer)" >&2; exit 1; }
# Scalar (int/bool) returns ptrcalled through a singleton receiver.
grep -q "MyNode: singleton scalar returns = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: singleton scalar returns (Time.GetTicksMsec / Engine.IsEditorHint)" >&2; exit 1; }
# A singleton absent from this build must degrade, not crash. EditorInterface is
# the measured case: editor-only, so a non-editor run (this one, and any exported
# game) does not have it and global_get_singleton hands back null. Both halves of
# the contract are asserted — it reads as null, and a call through the absent
# facade no-ops to a default rather than dereferencing null.
grep -q "MyNode: absent singleton degrades to null = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: absent singleton must degrade to null + no-op (EditorInterface in a non-editor run)" >&2; exit 1; }
# The absence is cached and reported ONCE, not re-asked (and re-reported) per
# frame. The scene runs 3 frames; more than one report means the null is not
# being cached.
absent_reports=$(grep -c "engine singleton EditorInterface is not present" <<<"$SCENE_OUT" || true)
[ "$absent_reports" -eq 1 ] \
    || { echo "FAIL: absent singleton reported $absent_reports times (must be cached + reported once)" >&2; exit 1; }
# THE PAYOFF: a real System.IO.FileStream writing and reading a file under the
# globalized user:// root, byte-verified and deleted. First proof that System.IO
# works in the Godot lane at all — and it runs under the incremental GC (the
# Godot default), which is what exercises the kernel-write bounce.
grep -q "MyNode: FileStream round trip under user:// = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: System.IO.FileStream round trip under the globalized user:// root" >&2; exit 1; }
# The RefCounted hand-off must not over-count either — and neither may the
# container wrappers leak engine values: a leaked reference shows up as
# Godot's ObjectDB leak report at scene exit.
grep -qE "ObjectDB instances (were )?leaked at exit" <<<"$SCENE_OUT" \
    && { echo "FAIL: Variant RefCounted hand-off leaked (ObjectDB instances leaked at exit)" >&2; exit 1; }
# The construction-helper StringName leak also surfaces in the scene run's deinit
# (Node construction routes through the same helper).
grep -q "construct StringName leak" <<<"$SCENE_OUT" \
    && { echo "FAIL: construct helper leaked a StringName reference (scene run)" >&2; exit 1; }
# Task.Run inside the engine: the awaited value must round-trip (the
# continuation resumes via a later frame's scheduler pump), and starting the
# pool arms the unload half — Deinitialize must stop and join the workers
# before the engine dlcloses this library, or they keep executing unmapped
# code. Exit-code checks can't gate that deterministically (a parked worker
# rarely crashes in time); the runtime's quiesce marker is printed only after
# every worker was actually joined.
grep -q "MyNode: Task.Run pool round trip = True" <<<"$SCENE_OUT" \
    || { echo "FAIL: Task.Run inside the engine (pool round trip / await resume)" >&2; exit 1; }
grep -q "\[dn2cpp\] quiesced .* background thread" <<<"$SCENE_OUT" \
    || { echo "FAIL: background threads not quiesced before library unload" >&2; exit 1; }

echo "== 7/7 --cut on a ClassDB registration slot must fail the TRANSPILE =="
# The ClassDB registration epilogue is emitted TEXT, not a method body, so the
# named-symbol backstop (CppEmitter.AssertCalledBodiesEmitted, which diffs what
# BODIES name) structurally cannot see the four method symbols it spells: the ctor
# slot, the _Notification slot, an engine-virtual trampoline, and a method bind's
# gateway. Nothing fires in a normal build because AdditionalRootMethods roots
# them — but a root is not a guard, and `--cut` on one used to give a GREEN
# transpile whose generated.cpp then died at the C++ compile on
# "use of undeclared identifier m_GodotSample_MyNode__Ready_274", with clang
# suggesting a DIFFERENT class's symbol. Measured before the fix: all four.
#
# Each slot must now be refused at transpile time, exit 2, naming the method — the
# alternative to the loud refusal is not "it works", it is a silent degrade (a
# nullptr ctor slot, a dropped method bind) that only shows up in the shipped game.
# Transpile-only: no compile, no engine, no cache of its own.
cut_out="artifacts/godot-cut-refusal"
for spec in ".ctor:Godot ClassDB ctor slot"             "_Notification:Godot ClassDB notification slot"             "_Ready:Godot engine-virtual bridge"             "MultiplyByTwo:Godot method bind"; do
    member=${spec%%:*}
    want=${spec#*:}
    rc=0
    err=$(invoke_cli \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
        -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
        -r "$corelib" \
        -o "$cut_out" --gdextension --cut "GodotSample.MyNode::$member" 2>&1 >/dev/null) || rc=$?
    if [ "$rc" -eq 0 ]; then
        echo "FAIL: --cut GodotSample.MyNode::$member transpiled GREEN — the registration" >&2
        echo "      epilogue names a body nothing defines; the C++ compile is where this lands." >&2
        exit 1
    fi
    grep -q "^error: $want" <<<"$err" \
        || { echo "FAIL: --cut $member failed, but not with the $want diagnostic:" >&2
             printf '%s\n' "$err" >&2; exit 1; }
    grep -q "GodotSample.MyNode::$member" <<<"$err" \
        || { echo "FAIL: --cut $member diagnostic does not name the method:" >&2
             printf '%s\n' "$err" >&2; exit 1; }
done
echo "OK (all four ClassDB registration slots refuse a --cut method, loudly and by name)"

gate_cache_commit
true
