#!/usr/bin/env bash
# Hot update (BPI interpretation): the base program is AOT-transpiled with
# --hotupdate-base (ABI-contract hash constant + base-abi.json sidecar), the
# patch assembly is baked into a Baked Patch Image by --emit-patch, and the
# native base binary loads the BPI at run time and executes its entry method in
# the runtime IL interpreter. This is the only gate that reaches
# dn2cpp_interp.cpp at all.
#
# What the patch exercises, in transcript order:
#   - the scalar-instruction surface (args/locals, branches, arithmetic,
#     comparisons, shifts, conversions, intra-patch calls)
#   - AOT base-image access: newobj on base types, instance/static calls, field
#     and static-field access, virtual dispatch through a base-typed variable
#   - patch-type inheritance, interpreted virtual overrides through the dynamic
#     vtable + N2M trampolines (incl. base.Describe() reaching the AOT body and
#     an override throw unwinding across an AOT frame into a patch handler)
#   - patch static state: lazy .cctor exactly once, re-entrant access from the
#     running initializer, and a THROWING initializer whose type stays
#     uninitialized (every later touch re-raises the recorded exception —
#     dn2cpp_cctor_run_once semantics)
#   - Object-inherited intrinsics imported through a DERIVED declaring type,
#     bound through the intrinsic table's base-chain fallback
#   - registry visibility: GetType().FullName/Name, default ToString,
#     isinst/castclass both directions, Type.GetType(string)
#   - patch-deriving-patch types, SZArrays over eight element kinds (incl.
#     catchable bounds/size faults and array covariance), interfaces, delegates,
#     base-image generics (type, delegate and method instantiations bound by
#     sigShape), String.Concat lowering, and exception handling
#   - an external-base exception whose base is the never-loaded External BCL
#     System.SystemException, driven from AOT and from a patch whose interpreted
#     `base(message, quota)` runs the real emitted ctor body
#   - the interpreter's eleven NULL checks, each caught by the interpreted code
#     that tripped it. They used to abort() rather than throw, so a regression
#     ends the process and the diff is the whole tail. Div-by-zero is
#     deliberately NOT probed and still aborts: the emitted AOT path lowers IL
#     div/rem to raw C++ / and %, so making only the interpreter catchable would
#     have it catch what the shipped path dies on.
#   - four "fault:" lines for the type-name registry: a BPI catch clause is a
#     type IMPORT bound by name, so a runtime-raised exception type the registry
#     does not carry cannot be caught by interpreted code at all. The load does
#     not refuse such an image (an unresolved type import binds to null on
#     purpose), so eh_catch_matches fails the first time the clause is tested.
#     Arithmetic/MissingMethod/ObjectDisposed/PlatformNotSupportedException are
#     opaque, so the emitted-class loop never sees them and the well-known seed
#     is the only route.
#   - `ldftn` delegate construction, which bypasses the loops' receiver guards:
#     a null 'this' is refused with real .NET's catchable ArgumentException
#     (CoreCLR CtorClosed semantics, measured — NOT an NRE), while a closure over
#     a static PATCH method answers a catchable NRE from the body's own guards,
#     because the BPI method record carries no static bit.
#   - two import-relabelling refusals on the interpreter's own side: a relabelled
#     delegate TYPE import (arity is dgcall's only evidence of staticness, so a
#     captured receiver would reach a static frame) and a relabelled METHOD
#     import (bound signature vs the delegate's Invoke row). Both sit at the
#     `newobj` / frame boundary rather than at the dereference, because the
#     receiver tests presume a reference slot holds an object.
#
# The expected output is fixed (not a real-.NET diff): on real .NET,
# Dn2Cpp.Runtime.HotUpdate.Run has no interpreter to run a BPI against — the
# managed placeholder body throws. Re-derive the interpreted section with
#
#     DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet HotUpdatePatch.dll
#
# and NOT with a bare `dotnet HotUpdatePatch.dll`: the patch prints doubles with
# no provider, so a de-DE developer bakes commas into the fixture. The env var
# pins the ORACLE; what pins the GATE is the culture pin at the head of
# HotUpdateBase's Main, since the interpreted body's Console.WriteLine routes
# through this process's CurrentCulture. The patch itself cannot carry either —
# it is a patch body, not a driver.
#
# The default bake is the register code format (Header.flags bit0); a
# stack-format section re-bakes the same patch with --patch-stackcode and
# replays the identical transcript through the v1 stack dispatch loop, which is
# the end-to-end equivalence proof for the two formats and also runs every null
# probe through both loops. A shadow-trace negative section reruns the same
# binary + BPI under --shadow-trace (the --shadow-stack freeze of that mode is
# in build-and-run-shadow-stack.sh): this base is built WITHOUT --shadow-stack,
# so the caught throw carries a kind-0 PC trace that must not name a single
# interpreted patch frame — interpreter PCs are dropped, never misattributed.
#
# Six negative sections assert the fences: a patch declaring a NEW virtual slot
# (newslot) is rejected by --emit-patch, so is one declaring an interface (a
# patch may implement a base-image interface, not declare one), so are a generic
# delegate binding (the missing-AOT-instantiation boundary) and a multicast
# delegate (`+=` — single-target only); a corrupted baseImageAbiHash makes the
# loader reject the stale patch (see docs/BPI-FORMAT.md); and an unknown header
# flag bit (bit31) makes it reject rather than silently run bytecode baked for a
# newer runtime.
#
# A conditional-default-reference section transpiles the same base against the
# real net10.0 CoreLib plus System.IO.Compression — a load-set TRIGGER the
# program never touches — so the base build injects the DnZlib shim while the
# patch converter (DefaultRefDir deliberately unset) does not. It asserts the
# sidecar records "DnZlib": "Injected", that the ABI hash and the baked BPI stay
# byte-identical to the untriggered build (the contract hashes EMITTED types,
# and an unreached shim emits none), and that the patch runs to the identical
# transcript. Its negative twin: a patch handed -r for a shim the base image
# does not carry is refused at bake, naming the shim, BEFORE the shim is loaded
# — load it first and the failure is an unrelated AOT-instantiation boundary.
#
# Two deployment sections drive HotUpdate.LoadDirectory / Load: a patch
# directory holding two same-FullName BPIs (deliberately misordered by filename
# so the asserted install order proves the patchVersion sort), a non-.bpi file
# (ignored), a stale BPI and an unknown-flags BPI (each header-skipped with a
# stderr diagnostic, never blocking the load) — asserting the loaded count, the
# newest-wins Describe line and the Type-identity probes; and an entry-less BPI
# that Load accepts as a registration-only image while Run rejects it.
#
# A final real-CoreLib-closure section builds a SECOND base
# (HotUpdateCoreLibBase) against the real net10.0 CoreLib — every other base
# here is intrinsic-BCL only. Such a base keeps every method row, and an
# interface row's signature-only invoker thunk can name a struct type the emit
# set never declared (Span<char> at ISpanFormattable.TryFormat; DictionaryEntry
# at IDictionaryEnumerator.get_Entry). The section asserts the base transpiles
# AND compiles+links, that the blocked rows carry trapping invmiss_ stubs
# (bindable row, catchable NotSupportedException naming the method and the
# missing layout), that a patch driving the marshallable surface binds and runs,
# and that a patch INVOKING a trapped row is refused loudly and catchably — today
# at the loader's one-shot bind pass, whose marshal surface (scalars +
# references) is strictly narrower than the invoker ABI; if that surface ever
# widens the call lands on the invmiss_ stub and raises the same exception.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/hotupdate-subset

echo "== 1/5 Building base + patch C# assemblies =="
build_proj samples/dotnet/HotUpdateBase/HotUpdateBase.csproj
build_proj samples/dotnet/HotUpdatePatch/HotUpdatePatch.csproj
build_proj samples/dotnet/HotUpdateBadPatch/HotUpdateBadPatch.csproj
build_proj samples/dotnet/HotUpdateBadPatchItf/HotUpdateBadPatchItf.csproj
build_proj samples/dotnet/HotUpdateBadPatchDelegate/HotUpdateBadPatchDelegate.csproj
build_proj samples/dotnet/HotUpdateBadPatchMulticast/HotUpdateBadPatchMulticast.csproj
build_proj samples/dotnet/HotUpdateDirPatch1/HotUpdateDirPatch1.csproj
build_proj samples/dotnet/HotUpdateDirPatch2/HotUpdateDirPatch2.csproj
build_proj samples/dotnet/HotUpdateRecvPatch/HotUpdateRecvPatch.csproj
build_proj samples/dotnet/HotUpdateArgPatch/HotUpdateArgPatch.csproj
build_proj samples/dotnet/HotUpdateInvokerPatch/HotUpdateInvokerPatch.csproj
build_proj samples/dotnet/HotUpdateFtnPatch/HotUpdateFtnPatch.csproj
build_proj samples/dotnet/HotUpdateDgRecvPatch/HotUpdateDgRecvPatch.csproj
build_proj samples/dotnet/HotUpdateDgSigPatch/HotUpdateDgSigPatch.csproj
build_proj samples/dotnet/HotUpdateCoreLibBase/HotUpdateCoreLibBase.csproj
build_proj samples/dotnet/HotUpdateCoreLibPatch/HotUpdateCoreLibPatch.csproj
build_proj samples/dotnet/HotUpdateCoreLibBadPatch/HotUpdateCoreLibBadPatch.csproj
# The CoreLib-less Console.Error section at the end of this gate. InterpBench is
# gates/measure-interp.sh's base program; this bucket borrows it rather than
# adding a sample, because the surface under test is exactly this bucket's own
# posture (a base image with no CoreLib in the load set).
build_proj samples/dotnet/InterpBench/InterpBench.csproj
base_app="samples/dotnet/HotUpdateBase/bin/$CONFIG/$TFM/HotUpdateBase.dll"
patch_app="samples/dotnet/HotUpdatePatch/bin/$CONFIG/$TFM/HotUpdatePatch.dll"
bad_app="samples/dotnet/HotUpdateBadPatch/bin/$CONFIG/$TFM/HotUpdateBadPatch.dll"
baditf_app="samples/dotnet/HotUpdateBadPatchItf/bin/$CONFIG/$TFM/HotUpdateBadPatchItf.dll"
baddg_app="samples/dotnet/HotUpdateBadPatchDelegate/bin/$CONFIG/$TFM/HotUpdateBadPatchDelegate.dll"
badmc_app="samples/dotnet/HotUpdateBadPatchMulticast/bin/$CONFIG/$TFM/HotUpdateBadPatchMulticast.dll"
dir1_app="samples/dotnet/HotUpdateDirPatch1/bin/$CONFIG/$TFM/HotUpdateDirPatch1.dll"
dir2_app="samples/dotnet/HotUpdateDirPatch2/bin/$CONFIG/$TFM/HotUpdateDirPatch2.dll"
recv_app="samples/dotnet/HotUpdateRecvPatch/bin/$CONFIG/$TFM/HotUpdateRecvPatch.dll"
arg_app="samples/dotnet/HotUpdateArgPatch/bin/$CONFIG/$TFM/HotUpdateArgPatch.dll"
inv_app="samples/dotnet/HotUpdateInvokerPatch/bin/$CONFIG/$TFM/HotUpdateInvokerPatch.dll"
ftn_app="samples/dotnet/HotUpdateFtnPatch/bin/$CONFIG/$TFM/HotUpdateFtnPatch.dll"
dgrecv_app="samples/dotnet/HotUpdateDgRecvPatch/bin/$CONFIG/$TFM/HotUpdateDgRecvPatch.dll"
dgsig_app="samples/dotnet/HotUpdateDgSigPatch/bin/$CONFIG/$TFM/HotUpdateDgSigPatch.dll"

echo "== 2/5 Transpiling the base with --hotupdate-base =="
# --hotupdate-refs force-emits the closed generic instantiations the patch binds
# but the base program never uses (the generic type Holder<string> and the generic
# method Counter.Echo<int>/<string>) — HybridCLR's AOTGenericReferences.
invoke_cli "$base_app" --hotupdate-base \
    --hotupdate-refs samples/dotnet/HotUpdatePatch/hotupdate-refs.txt -o "$OUT"
[ -f "$OUT/base-abi.json" ] || { echo "FAIL: base-abi.json sidecar missing" >&2; exit 1; }
grep -q dn2cpp_base_image_abi_hash "$OUT/generated.cpp" \
    || { echo "FAIL: dn2cpp_base_image_abi_hash constant missing from generated.cpp" >&2; exit 1; }

# Beyond the base's transpile surface, everything below is a function of the
# patch assemblies (baked by --emit-patch, whose rejections are transpiler
# BEHAVIOR — hence the _gate_cli_hash term standing in for the transpiler; see
# that helper's doc) and the refs list. The transpiler-behavior env axis rides
# in the context too: the bakes' outputs (.bpi) are not in the key's surface,
# so an ambient cap/assert/drain knob changing them must move the context.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
# The conditional-default-reference section transpiles the same base
# against the REAL net10.0 CoreLib, so which CoreLib that resolves to is an input
# of this gate the same way it is of net10_bcl_diff_gate — a runtime bump must
# not be served a green recorded against the previous one.
if gate_cache_check "$OUT" "hotupdate-subset|cli:$(_gate_cli_hash)|$tenv|corelib:$(resolve_net10_corelib)" \
        "$base_app" "$patch_app" "$bad_app" "$baditf_app" "$baddg_app" \
        "$badmc_app" "$dir1_app" "$dir2_app" "$dgrecv_app" "$dgsig_app" \
        samples/dotnet/HotUpdateCoreLibBase/bin/$CONFIG/$TFM/HotUpdateCoreLibBase.dll \
        samples/dotnet/HotUpdateCoreLibPatch/bin/$CONFIG/$TFM/HotUpdateCoreLibPatch.dll \
        samples/dotnet/HotUpdateCoreLibBadPatch/bin/$CONFIG/$TFM/HotUpdateCoreLibBadPatch.dll \
        samples/dotnet/InterpBench/bin/$CONFIG/$TFM/InterpBench.dll \
        samples/dotnet/HotUpdatePatch/hotupdate-refs.txt; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/5 Baking the patch BPI (--emit-patch) =="
invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/base-abi.json" -o "$OUT"

echo "== 4/5 Compiling C++ =="
compile_console "$OUT" HotUpdateBase

echo "== 5/5 Running (AOT base + interpreted patch) =="
expected="base: start
warm:7
double counter:2
render double counter:2
16
2
4
6
via badge:base#9
12
base-each:1
base-each:2
base-each:3
base-greeting
HotUpdateBase.Counter
7
holder
21
base quota/none#7
True
outer/inner cause#-8
Hello, interpreted world
55
5050

8
14
6
-13
96
-128
536870784
True
False
True

1099511627776
366503875925
6294
-1099511627776
366503875925.3333
183251937962
2.5

0
1030301
patch methods can call each other
zero

81
12
3
24
patched
42
double counter:2
patched:24

before TaggedCounter
TaggedCounter ctor after base
44
first
10
13
counter
15
35
TaggedCounter.Describe intercepts
counter:35
TaggedCounter.Describe intercepts
render counter:35
TaggedCounter.Describe intercepts
counter:35
weight limit
8589934598
8589934602

before first Registry touch
Registry cctor: begin
registry
111
Registry cctor: end
112
113
updated
1024
1048
before first Gauge touch
Gauge cctor
41
51
before first Doomed touch
doomed cctor
doomed cctor
doomed cctor

HotUpdatePatch.TaggedCounter
TaggedCounter
HotUpdatePatch.TaggedCounter
True
False
False
first
Unable to cast object of type 'HotUpdatePatch.TaggedCounter' to type 'HotUpdateBase.DoubleCounter'.
True
HotUpdatePatch.TaggedCounter

TaggedCounter ctor after base
47
gold
8
9
7
GoldTagged.Describe adds a bonus
9
TaggedCounter.Describe intercepts
counter:7
GoldTagged.Describe adds a bonus
9
TaggedCounter.Describe intercepts
counter:7
GoldTagged.Describe adds a bonus
9
TaggedCounter.Describe intercepts
render counter:7
HotUpdatePatch.GoldTagged
True
True
TaggedCounter ctor after base
48
TaggedCounter.Describe intercepts
render counter:2
Silver

5
16
55
50
beta
3
True
3
gamma
TaggedCounter ctor after base
49
TaggedCounter ctor after base
50
2
TaggedCounter.Describe intercepts
counter:1
GoldTagged.Describe adds a bonus
3
TaggedCounter.Describe intercepts
render counter:2
True
False
lead
index out of range
negative array length
55
4
4
110
3435973836
3.375
2.5
False
True
False
218

interfaces
hello
7
via hello#7
badge:gold
via badge:gold#3
True
False
ticket
42
via ticket#42
True
False

delegates
21
40
18
5
6
7
105
9
30
18
patch-greeting

generics
42
holder
gen
20
7
gen2

strings
hello world
world-wide
greetings, world-wide
a-world-z
null is empty:!

exception ctors
io boom
13
plain boom
t

external-base exception
patch quota
patch quota/none#21
patch
patch outer/patch inner#-8

derived intrinsics
HotUpdatePatch.PatchIoEx: probe

patch says boom
body
finally A
finally B
7
finally on unwind
cross the finally
inner catch
rethrown
counter underflow
20
nre: patch call
nre: patch field load
nre: patch field store
nre: base call
nre: base field load
nre: base field store
nre: array length
nre: array load
nre: array store
nre: delegate invoke
nre: intrinsic call
fault: arithmetic
fault: missing method
fault: object disposed
fault: platform not supported
arg: delegate over null this (intrinsic)
arg: delegate over null this (aot)
arg: delegate over null this (patch)
nre: tostring on null
delegate GetType: HotUpdateBase.Counter
base caught: escaped to base
HotUpdatePatch.TaggedCounter
base: done"
# Exit status captured explicitly (`$(...)` inline would swallow it): a base
# that aborts in teardown AFTER printing the full transcript must not pass.
set +e
hu_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdatePatch.bpi"); hu_rc=$?
set -e
assert_output "$(strip_cr_win "$hu_out")" "$expected"
assert_exit_code "$hu_rc" 0

echo "-- stack format: --patch-stackcode bake replays the identical transcript --"
# The same patch baked in the v1 stack code format (Header.flags bit0 clear):
# the unchanged base binary selects the dispatch loop per image, so the full
# transcript above re-running identically is the end-to-end equivalence proof
# for the two encodings.
invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/base-abi.json" --patch-stackcode -o "$OUT/stack"
# flags bit0 clear in the header (offset 12, LE u32 = 00 00 00 00) — and the
# default bake above must have set it (register code is the default).
[ "$(od -An -tx1 -j12 -N4 "$OUT/stack/HotUpdatePatch.bpi" | tr -d ' ')" = "00000000" ] \
    || { echo "FAIL: --patch-stackcode set header flags bits" >&2; exit 1; }
[ "$(od -An -tx1 -j12 -N4 "$OUT/HotUpdatePatch.bpi" | tr -d ' ')" = "01000000" ] \
    || { echo "FAIL: the default bake did not set header flags bit0 (register code)" >&2; exit 1; }
set +e
hu_out=$("./$OUT/HotUpdateBase" "$OUT/stack/HotUpdatePatch.bpi"); hu_rc=$?
set -e
assert_output "$(strip_cr_win "$hu_out")" "$expected"
assert_exit_code "$hu_rc" 0
echo "OK (stack-format bake, identical transcript)"

echo "-- conditional default refs: a base whose load set carries a TRIGGER --"
# --emit-patch is structurally outside the conditional default-reference
# injection: the BASE build goes through TranspileDriver and therefore injects a
# shipped shim whenever its trigger assembly is in the load set, while the patch
# converter leaves TranspileOptions.DefaultRefDir unset on purpose (its model must
# be a copy of the base's -r set). The two load sets are therefore asymmetric by
# exactly the injected shims, and this section is the measurement of what that
# does, frozen as a regression.
#
# The base here is the SAME HotUpdateBase program, transpiled against the real
# net10.0 CoreLib plus System.IO.Compression. The program touches no compression
# at all — the trigger is a LOAD-SET fact, not a reachability one — so DnZlib is
# injected, contributes an assembly-registry row and zero method bodies, and the
# converter never sees it. What is asserted:
#
#   1. the sidecar RECORDS the verdict ("DnZlib": "Injected"), which is the whole
#      point of the record: the asymmetry is now visible instead of ambient;
#   2. the ABI hash and the baked BPI are byte-identical to the same base built
#      WITHOUT the trigger. That is the load-bearing one. The contract hashes the
#      base's EMITTED types — exactly the surface a patch's name-based imports can
#      bind against — and an unreached shim emits none, so injecting it must not
#      move the hash. If it ever does, two provably interchangeable images start
#      rejecting each other's patches;
#   3. the patch LOADS AND RUNS against the trigger-carrying base, producing the
#      identical transcript. The measured answer to "succeeds / fails loud / binds
#      wrong" is SUCCEEDS, and the reason is structural: a patch reaches the base
#      only through name-based imports and never re-lowers a base body, so a shim
#      the base injected is invisible from the converter's side.
#
# (When the shim IS reached the base emits its types, they enter the contract as
# ordinary T/L/F/V lines and the hash moves on its own — so the dangerous
# direction is already covered by the existing staleness guard.)
trig_corelib=$(resolve_net10_corelib)
trig_bcl=$(dirname "$trig_corelib")
trig_comp="$trig_bcl/System.IO.Compression.dll"
[ -f "$trig_comp" ] || { echo "FAIL: real System.IO.Compression not found: $trig_comp" >&2; exit 1; }
for arm in trigger notrigger; do
    trig_refs=(-r "$trig_corelib")
    [ "$arm" = trigger ] && trig_refs+=(-r "$trig_comp")
    invoke_cli "$base_app" "${trig_refs[@]}" --auto-ref --hotupdate-base \
        --hotupdate-refs samples/dotnet/HotUpdatePatch/hotupdate-refs.txt -o "$OUT/$arm"
    invoke_cli --emit-patch "$patch_app" --base-abi "$OUT/$arm/base-abi.json" -o "$OUT/$arm"
done
# 1. the verdict is recorded, and the two arms disagree about it — otherwise the
#    rest of this section would be comparing a base against itself.
grep -q '"DnZlib": "Injected"' "$OUT/trigger/base-abi.json" \
    || { echo "FAIL: the trigger arm's sidecar does not record DnZlib as Injected" >&2; exit 1; }
grep -q '"DnZlib": "TriggerAbsent"' "$OUT/notrigger/base-abi.json" \
    || { echo "FAIL: the no-trigger arm's sidecar does not record DnZlib as TriggerAbsent" >&2; exit 1; }
grep -q '"DnZlib"' "$OUT/trigger/generated.cpp" \
    || { echo "FAIL: the trigger arm emitted no DnZlib assembly-registry row — the shim was not injected" >&2; exit 1; }
# 2. the injected-but-unreached shim moved neither the ABI hash nor the BPI.
trig_hash=$(grep -o '"hash": "0x[0-9a-f]*"' "$OUT/trigger/base-abi.json")
notrig_hash=$(grep -o '"hash": "0x[0-9a-f]*"' "$OUT/notrigger/base-abi.json")
[ "$trig_hash" = "$notrig_hash" ] \
    || { echo "FAIL: injecting an unreached shim moved the ABI contract hash ($trig_hash vs $notrig_hash) — a patch baked against one of two interchangeable bases would now be rejected by the other" >&2; exit 1; }
cmp -s "$OUT/trigger/HotUpdatePatch.bpi" "$OUT/notrigger/HotUpdatePatch.bpi" \
    || { echo "FAIL: the baked BPI differs between the trigger and no-trigger bases" >&2; exit 1; }
# 3. and the patch loads, binds and runs against the trigger-carrying base.
compile_console "$OUT/trigger" HotUpdateBase
set +e
trig_out=$("./$OUT/trigger/HotUpdateBase" "$OUT/trigger/HotUpdatePatch.bpi"); trig_rc=$?
set -e
assert_output "$(strip_cr_win "$trig_out")" "$expected"
assert_exit_code "$trig_rc" 0
echo "OK (trigger-carrying base: shim injected + recorded, ABI hash and BPI unmoved, patch runs)"

echo "-- negative: a patch -r'ing a shim the base image does not carry --"
# The reverse direction, and the one that is NOT benign. A caller who hands
# --emit-patch a -r for a shim the base never loaded gets a patch model that can
# resolve names against classes existing nowhere in the base; the baked import
# then binds against nothing at load time, with the cause long gone. The
# converter refuses at bake instead, reading the base's own recorded verdict.
# Asserted against the no-trigger arm above, whose sidecar says TriggerAbsent.
#
# The check runs BEFORE the shim is loaded, and that placement is the assertion:
# load it first and Create dies on an AOT-instantiation boundary naming
# Span<Byte>, which says nothing about a shim. The grep below is what pins that
# down — it must be the shim diagnostic, not the boundary one.
cli_dir="${DN2CPP_CLI_DLL:+$(dirname "$DN2CPP_CLI_DLL")}"
cli_dir="${cli_dir:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM}"
[ -f "$cli_dir/DnZlib.dll" ] || { echo "FAIL: DnZlib.dll not found beside the CLI: $cli_dir" >&2; exit 1; }
dr_rc=0
dr_err=$(invoke_cli --emit-patch "$patch_app" -r "$cli_dir/DnZlib.dll" \
    --base-abi "$OUT/notrigger/base-abi.json" -o "$OUT/notrigger-neg" 2>&1 >/dev/null) || dr_rc=$?
if [ "$dr_rc" -ne 2 ]; then
    echo "FAIL: --emit-patch with a -r shim the base lacks exited $dr_rc (expected 2)" >&2
    echo "$dr_err" >&2
    exit 1
fi
if ! grep -q "carries the shim 'DnZlib', which the base image does not" <<<"$dr_err"; then
    echo "FAIL: the rejection did not name the shim (a load-first check reports an AOT-instantiation boundary instead):" >&2
    echo "$dr_err" >&2
    exit 1
fi
echo "OK (a shim the base lacks is refused at bake, naming the shim)"

echo "-- negative: --shadow-trace on a PLAIN base — kind-0 drops interp frames --"
# The same binary + BPI, in the mode build-and-run-shadow-stack.sh arm 3
# freezes under --shadow-stack. This base was built WITHOUT the flag, so no
# AOT guard is planted and the loader materializes no interpreter frame names
# (the image's frameNames stays null): the throw is stamped as a kind-0 PC
# trace. The chain still runs and the catch still lands — exit 0 and the
# verdict line — but the trace text is best-effort nearest-symbol resolution
# under -O2, so nothing is frozen here. The one hard assert on the
# trace: NO interpreted patch frame is ever named. The base binary carries no
# patch symbols, so an "at HotUpdatePatch." line could only be a
# misattribution — kind-0 must drop interpreter PCs, never mislabel them.
set +e
st_out=$("./$OUT/HotUpdateBase" --shadow-trace "$OUT/HotUpdatePatch.bpi"); st_rc=$?
set -e
st_out=$(strip_cr_win "$st_out")
if [ "$st_rc" -ne 0 ]; then
    echo "FAIL: --shadow-trace exited $st_rc (the throw is caught in the AOT base; expected 0)" >&2
    printf '%s\n' "$st_out" >&2
    exit 1
fi
if grep -q 'at HotUpdatePatch\.' <<<"$st_out"; then
    echo "FAIL: plain-base kind-0 trace names an interpreted patch frame (kind-0 must drop interpreter PCs, never misattribute them):" >&2
    printf '%s\n' "$st_out" >&2
    exit 1
fi
if ! grep -q '^shadow-trace: caught in AOT base$' <<<"$st_out"; then
    echo "FAIL: --shadow-trace verdict line missing — the mode did not complete:" >&2
    printf '%s\n' "$st_out" >&2
    exit 1
fi
echo "OK (plain base: interp frames dropped, never misattributed; verdict line present)"

echo "-- negative: a patch declaring a new virtual slot must be rejected --"
bad_rc=0
bad_err=$(invoke_cli --emit-patch "$bad_app" --base-abi "$OUT/base-abi.json" -o "$OUT" 2>&1 >/dev/null) || bad_rc=$?
if [ "$bad_rc" -ne 2 ]; then
    echo "FAIL: --emit-patch on a new-virtual-slot patch exited $bad_rc (expected 2)" >&2
    echo "$bad_err" >&2
    exit 1
fi
if ! grep -q "must not declare new virtual slots yet" <<<"$bad_err"; then
    echo "FAIL: --emit-patch rejection message missing the new-virtual-slot fence:" >&2
    echo "$bad_err" >&2
    exit 1
fi
echo "OK (new virtual slot rejected)"

echo "-- negative: a patch declaring an interface must be rejected --"
baditf_rc=0
baditf_err=$(invoke_cli --emit-patch "$baditf_app" --base-abi "$OUT/base-abi.json" -o "$OUT" 2>&1 >/dev/null) || baditf_rc=$?
if [ "$baditf_rc" -ne 2 ]; then
    echo "FAIL: --emit-patch on an interface-declaring patch exited $baditf_rc (expected 2)" >&2
    echo "$baditf_err" >&2
    exit 1
fi
if ! grep -q "must be a plain class" <<<"$baditf_err"; then
    echo "FAIL: --emit-patch rejection message missing the interface-declaration fence:" >&2
    echo "$baditf_err" >&2
    exit 1
fi
echo "OK (interface declaration rejected)"

echo "-- negative: a patch binding a method into a generic delegate must be rejected --"
baddg_rc=0
baddg_err=$(invoke_cli --emit-patch "$baddg_app" --base-abi "$OUT/base-abi.json" -o "$OUT" 2>&1 >/dev/null) || baddg_rc=$?
if [ "$baddg_rc" -ne 2 ]; then
    echo "FAIL: --emit-patch on a generic-delegate patch exited $baddg_rc (expected 2)" >&2
    echo "$baddg_err" >&2
    exit 1
fi
# A generic delegate (Func<>/Action<>) is a closed generic instantiation the AOT
# image must carry: rejected either at Compilation build (the external generic
# instantiation) or, if the instantiation is AOT-present, at the delegate newobj
# — both name Func and exit 2.
if ! grep -q "Func" <<<"$baddg_err"; then
    echo "FAIL: --emit-patch rejection message missing the generic-delegate fence:" >&2
    echo "$baddg_err" >&2
    exit 1
fi
echo "OK (generic delegate rejected)"

echo "-- negative: a patch building a multicast delegate (+=) must be rejected --"
badmc_rc=0
badmc_err=$(invoke_cli --emit-patch "$badmc_app" --base-abi "$OUT/base-abi.json" -o "$OUT" 2>&1 >/dev/null) || badmc_rc=$?
if [ "$badmc_rc" -ne 2 ]; then
    echo "FAIL: --emit-patch on a multicast-delegate patch exited $badmc_rc (expected 2)" >&2
    echo "$badmc_err" >&2
    exit 1
fi
if ! grep -q "combining delegates" <<<"$badmc_err"; then
    echo "FAIL: --emit-patch rejection message missing the multicast fence:" >&2
    echo "$badmc_err" >&2
    exit 1
fi
echo "OK (multicast delegate rejected)"

echo "-- negative: a stale BPI (baseImageAbiHash mismatch) must be rejected --"
cp "$OUT/HotUpdatePatch.bpi" "$OUT/stale.bpi"
# Flip one byte of the header's baseImageAbiHash (offset 16).
printf '\xFF' | dd of="$OUT/stale.bpi" bs=1 seek=16 count=1 conv=notrunc 2>/dev/null
if "./$OUT/HotUpdateBase" "$OUT/stale.bpi" >/dev/null 2>&1; then
    echo "FAIL: a stale BPI (ABI hash mismatch) was accepted" >&2
    exit 1
fi
echo "OK (stale BPI rejected)"

echo "-- negative: a BPI with unknown header flag bits must be rejected --"
cp "$OUT/HotUpdatePatch.bpi" "$OUT/flagged.bpi"
# Set bit31 of the header's flags u32 (offset 12, little-endian: top byte at
# offset 15) — a bit outside DN2CPP_BPI_FLAGS_SUPPORTED that no runtime will
# ever claim, so this fence stays valid as flag bits are legalized.
printf '\x80' | dd of="$OUT/flagged.bpi" bs=1 seek=15 count=1 conv=notrunc 2>/dev/null
flag_rc=0
flag_err=$("./$OUT/HotUpdateBase" "$OUT/flagged.bpi" 2>&1 >/dev/null) || flag_rc=$?
if [ "$flag_rc" -eq 0 ]; then
    echo "FAIL: a BPI with unknown header flag bits was accepted" >&2
    exit 1
fi
if ! grep -q "unsupported header flags" <<<"$flag_err"; then
    echo "FAIL: rejection message missing the unknown-flags fence:" >&2
    echo "$flag_err" >&2
    exit 1
fi
echo "OK (unknown header flags rejected)"

echo "-- a type-confused intrinsic import is refused, not dereferenced --"
# A method import binds by NAME alone — declaring type, method name, sigShape and
# staticness, every one of them a string out of the image's own pool — so a
# malformed .bpi can label a call site with a row of the interpreter's intrinsic
# table its receiver does not satisfy, and the kShapeRefRetObj helpers cast raw
# (dn2cpp_type_name reads ((Dn2CppType*)receiver)->typeInfo with no header test).
# Nothing signs a .bpi; hotupdate_read_file loads whatever is on disk.
#
# HotUpdateRecvPatch exists for this and for nothing else: its import table holds
# exactly ONE kShapeRefRetObj call (System.Object::ToString on a base-image
# Counter), and both pooled names rewritten below occur exactly once in the baked
# image — which is what lets the corruption be located by CONTENT instead of by a
# hardcoded record offset that the next sample edit would silently invalidate.
# The uniqueness is asserted before the writes, so a fixture that grows a second
# use fails here loudly rather than corrupting some unrelated bytes.
invoke_cli --emit-patch "$recv_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Positive control first: the receiver test must not reject the legitimate call.
# ToString on a RecvProbe is Object's, so it prints the instance's type FullName.
recv_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateRecvPatch.bpi")
grep -q '^HotRecvPatch\.RecvProbe$' <<<"$(strip_cr_win "$recv_out")" \
    || { echo "FAIL: the uncorrupted receiver fixture did not print its ToString:" >&2
         printf '%s\n' "$recv_out" >&2; exit 1; }
cp "$OUT/HotUpdateRecvPatch.bpi" "$OUT/confused.bpi"
recv_objoff=$(LC_ALL=C grep -abo -- 'System.Object' "$OUT/confused.bpi" | cut -d: -f1)
recv_tsoff=$(LC_ALL=C grep -abo -- 'ToString' "$OUT/confused.bpi" | cut -d: -f1)
[ "$(printf '%s\n' "$recv_objoff" | wc -l | tr -d ' ')" = "1" ] \
    && [ "$(printf '%s\n' "$recv_tsoff" | wc -l | tr -d ' ')" = "1" ] \
    || { echo "FAIL: HotUpdateRecvPatch no longer names System.Object/ToString exactly once — the corruption cannot be located by content" >&2; exit 1; }
# "System.Object" (13) -> "System.Type" (11): the u16 length prefix two bytes
# ahead moves too, and the two orphaned tail bytes stay as dead pool filler —
# pool offsets are absolute, so nothing downstream shifts. "ToString" (8) ->
# "get_Name" (8) needs no prefix write. Together they re-label that one import
# onto the System.Type::get_Name row, whose receiver is still a RecvProbe.
printf '\x0b\x00System.Type' | dd of="$OUT/confused.bpi" bs=1 seek=$((recv_objoff - 2)) conv=notrunc 2>/dev/null
printf 'get_Name' | dd of="$OUT/confused.bpi" bs=1 seek="$recv_tsoff" conv=notrunc 2>/dev/null
set +e
recv_err=$("./$OUT/HotUpdateBase" "$OUT/confused.bpi" 2>&1 >/dev/null); recv_rc=$?
set -e
# Measured with the receiver test removed, this exact image exits 139 (SIGSEGV)
# inside dn2cpp_type_name. Exit 0 would be worse still — a wrong answer.
if [ "$recv_rc" -eq 0 ]; then
    echo "FAIL: a type-confused intrinsic import was accepted" >&2
    exit 1
fi
if ! grep -q "receiver is not an instance of the import's declared type" <<<"$recv_err"; then
    echo "FAIL: the type-confused import did not raise the receiver-type refusal:" >&2
    printf '%s\n' "$recv_err" >&2
    exit 1
fi
echo "OK (type-confused intrinsic receiver refused)"

echo "-- a type-confused intrinsic ARGUMENT is refused, not dereferenced --"
# The section above, one axis over. The same name-only bind puts an arbitrary
# object in an ARGUMENT position too, and the three reference-argument shapes'
# helpers cast their operands just as raw. The sharpest of them is
# String.Concat(string[]): it shares its C++ signature (Dn2CppObject*
# (*)(Dn2CppString*)) with Type.GetType(string) and reinterpret_casts the operand
# straight to a Dn2CppArrayRef — so the argument kinds have to be per ROW and per
# POSITION, not per shape, and this is the pair that proves it.
#
# HotUpdateArgPatch exists for this and nothing else: it holds exactly ONE
# String.Concat import, so the single pooled name rewritten below occurs exactly
# once in the baked image and the corruption is located by CONTENT rather than by
# a record offset the next sample edit would silently invalidate. Only the
# SIGNATURE SHAPE moves — the declaring type and method name are already the ones
# the target row wants — which makes this the minimal corruption of the two.
invoke_cli --emit-patch "$arg_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Positive control first: the argument test must not reject the legitimate call.
arg_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateArgPatch.bpi")
grep -q '^arg-probe$' <<<"$(strip_cr_win "$arg_out")" \
    || { echo "FAIL: the uncorrupted argument fixture did not print its concatenation:" >&2
         printf '%s\n' "$arg_out" >&2; exit 1; }
cp "$OUT/HotUpdateArgPatch.bpi" "$OUT/confused-arg.bpi"
arg_shapeoff=$(LC_ALL=C grep -abo -- '(String,String):String' "$OUT/confused-arg.bpi" | cut -d: -f1)
[ "$(printf '%s\n' "$arg_shapeoff" | wc -l | tr -d ' ')" = "1" ] \
    || { echo "FAIL: HotUpdateArgPatch no longer names (String,String):String exactly once — the corruption cannot be located by content" >&2; exit 1; }
# "(String,String):String" (22) -> "(String[]):String" (17): the u16 length prefix
# two bytes ahead moves with it and the five orphaned tail bytes stay as dead pool
# filler, pool offsets being absolute. That re-labels the one import from the
# Concat(String,String) row onto the Concat(String[]) row, whose helper reads
# `length` out of the operand's array header — while the operand is still the
# string the patch's own code pushed.
printf '\x11\x00(String[]):String' | dd of="$OUT/confused-arg.bpi" bs=1 seek=$((arg_shapeoff - 2)) conv=notrunc 2>/dev/null
set +e
arg_err=$("./$OUT/HotUpdateBase" "$OUT/confused-arg.bpi" 2>&1 >/dev/null); arg_rc=$?
set -e
# Measured with intrinsic_arg_ok forced to true, this exact image exits 138
# (SIGBUS, thrice out of three) inside dn2cpp_string_concat_objects, which walks
# the string's char storage as an object array. Exit 0 would be worse still — a
# wrong answer.
if [ "$arg_rc" -eq 0 ]; then
    echo "FAIL: a type-confused intrinsic argument was accepted" >&2
    exit 1
fi
if ! grep -q "argument is not an instance of the import's declared parameter type" <<<"$arg_err"; then
    echo "FAIL: the type-confused argument did not raise the parameter-type refusal:" >&2
    printf '%s\n' "$arg_err" >&2
    exit 1
fi
echo "OK (type-confused intrinsic argument refused)"

# The two sections below: same name-only bind, one axis further out — the
# shapes the intrinsic TABLE does not produce. Those are bound from the declaring
# type's own metadata, so the confusion is not "which row answers" but "which
# base-image method answers", and the receiver the patch's own baked code pushed
# is not an instance of its declaring type. Both fixtures are re-labelled by ONE
# in-place pooled write with no length prefix to move, "HotUpdateBase.QuotaEx"
# and "HotUpdateBase.Counter" both being 21 bytes:
#
#   "HotUpdateBase.QuotaEx"  ->  "HotUpdateBase.Counter"
#
# which moves the single `Describe` import from QuotaEx's non-virtual reader onto
# Counter's VIRTUAL Describe. Neither fixture may construct or catch a QuotaEx —
# a ctor import or an EH clause would follow the same pooled string and break the
# load before the confusion could fire — which is why both take the instance from
# Counter.SeedQuota, a well-known static on a different type. The uniqueness is
# asserted before each write, so a fixture that grows a second user of the name
# fails here loudly rather than corrupting unrelated bytes.
bpi_relabel() { # bpi_relabel SRC DST — the one pooled write, located by content
    cp "$1" "$2"
    local off
    off=$(LC_ALL=C grep -abo -- 'HotUpdateBase.QuotaEx' "$2" | cut -d: -f1)
    [ "$(printf '%s\n' "$off" | wc -l | tr -d ' ')" = "1" ] && [ -n "$off" ] \
        || { echo "FAIL: $(basename "$1") no longer names HotUpdateBase.QuotaEx exactly once — the corruption cannot be located by content" >&2; exit 1; }
    printf 'HotUpdateBase.Counter' | dd of="$2" bs=1 seek="$off" conv=notrunc 2>/dev/null
}

echo "-- a type-confused kShapeInvoker receiver is refused, not dispatched --"
# The metadata-bound call shape. A `callvirt` reads self->type->vtable[slot] at a
# slot index that came from the DECLARED type, so a receiver that is not one
# carries no length for it — an out-of-range read followed by a call through
# whatever it held, not merely a wrong answer.
invoke_cli --emit-patch "$inv_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Positive control first: the receiver test must not reject the legitimate call.
inv_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateInvokerPatch.bpi")
grep -q '^seed/none#1$' <<<"$(strip_cr_win "$inv_out")" \
    || { echo "FAIL: the uncorrupted invoker fixture did not print QuotaEx.Describe:" >&2
         printf '%s\n' "$inv_out" >&2; exit 1; }
bpi_relabel "$OUT/HotUpdateInvokerPatch.bpi" "$OUT/confused-inv.bpi"
set +e
inv_err=$("./$OUT/HotUpdateBase" "$OUT/confused-inv.bpi" 2>&1 >/dev/null); inv_rc=$?
set -e
# Measured with the receiver test forced true, this exact image exits 134 in the
# runtime's own missing-slot trap ("dn2cpp fatal: virtual call on
# HotUpdateBase.QuotaEx through a slot with no emitted implementation", three
# runs of three) — an UNCATCHABLE abort, and only because Counter's Describe slot
# happens to land inside QuotaEx's vtable at all. Nothing bounds it there: the
# index is the declared type's.
if [ "$inv_rc" -eq 0 ]; then
    echo "FAIL: a type-confused invoker receiver was accepted" >&2
    exit 1
fi
if ! grep -q "receiver is not an instance of the import's declared type" <<<"$inv_err"; then
    echo "FAIL: the type-confused invoker receiver did not raise the receiver-type refusal:" >&2
    printf '%s\n' "$inv_err" >&2
    exit 1
fi
echo "OK (type-confused kShapeInvoker receiver refused)"

echo "-- a type-confused ldftn delegate capture is refused, not bound --"
# The OTHER mouth, and the reason this is a re-opening rather than a gap: an
# import's fnPtr becomes an AOT delegate's `method` and is called later with the
# captured `this`, never crossing a dispatch loop's call arm. The delegate
# `newobj` is the only point at which the interpreter ever sees that receiver —
# an Invoke re-enters through the delegate's own thunk, which knows nothing about
# the import — so the refusal has to be at construction, beside the null-'this'
# refusal. The fixture takes the method GROUP and nothing else.
invoke_cli --emit-patch "$ftn_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
ftn_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateFtnPatch.bpi")
grep -q '^seed/none#1$' <<<"$(strip_cr_win "$ftn_out")" \
    || { echo "FAIL: the uncorrupted ldftn fixture did not print QuotaEx.Describe:" >&2
         printf '%s\n' "$ftn_out" >&2; exit 1; }
bpi_relabel "$OUT/HotUpdateFtnPatch.bpi" "$OUT/confused-ftn.bpi"
set +e
ftn_err=$("./$OUT/HotUpdateBase" "$OUT/confused-ftn.bpi" 2>&1 >/dev/null); ftn_rc=$?
set -e
# Measured with the capture test forced true, this exact image exits 0 and prints
# ":-2146233088" where "seed/none#1" belongs — Counter.Describe reading the
# exception object's prefix as its own fields (a null Label, and COR_E_EXCEPTION
# read as _count). A silent wrong answer at exit 0 is the worst of the three
# outcomes these shapes can produce, and it is the one an untested mouth gives.
if [ "$ftn_rc" -eq 0 ]; then
    echo "FAIL: a type-confused ldftn delegate capture was accepted" >&2
    exit 1
fi
if ! grep -q "receiver is not an instance of the import's declared type" <<<"$ftn_err"; then
    echo "FAIL: the type-confused delegate capture did not raise the receiver-type refusal:" >&2
    printf '%s\n' "$ftn_err" >&2
    exit 1
fi
echo "OK (type-confused ldftn delegate capture refused)"

# The two sections below. Everything above types what an import hands to
# BASE-IMAGE code; these type what the interpreter installs on its own side — the
# receiver a PATCH frame is entered with, and the signature an AOT delegate's
# target is called under. Both corruptions are pooled writes located by content,
# and both fixtures exist for this and nothing else.

echo "-- a delegate whose Invoke arity is not the capture's is refused --"
# A patch method bound into a base-image delegate carries its captured `this` in
# a loader-built closure, and dn2cpp_interp_dgcall derives instance-ness from
# ARITY alone: a MethodTable record carries no static bit, so a frame that wants
# exactly as many slots as the delegate passes reads as static and the captured
# receiver is dropped — while the thunk that called in packed one argument too
# many, so the frame's first slot is whatever the AOT caller's ABI left in the
# register. That is a receiver in a parameter's clothing, and the patch body
# ahead of it does `ldfld`/`stfld` at the DECLARING type's loader-assigned
# offsets.
#
# The corruption is one in-place pooled write with no length prefix to move,
# "HotUpdateBase.Describer" and "HotUpdateBase.Annotator" both being 23 bytes,
# which swaps a no-argument delegate type for a one-argument one AT THE `newobj`
# — so the thunk installed is the wrong one while the AOT side still invokes
# through Describer's ABI. The name occurs twice in the baked image: once as the
# delegate type import's own pooled entry and once inside Counter.Announce's
# sigShape. Only the first may move — rewriting the sigShape too would leave the
# Announce import unresolvable and the load would fail for the wrong reason — and
# it is identified as the occurrence preceded by its own u16 length prefix
# (23 = 0x17 0x00), the other being preceded by '('.
invoke_cli --emit-patch "$dgrecv_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Positive control first: the receiver tests must not reject the legitimate call.
dgrecv_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateDgRecvPatch.bpi")
grep -q '^probe#7$' <<<"$(strip_cr_win "$dgrecv_out")" \
    || { echo "FAIL: the uncorrupted delegate-receiver fixture did not print its probe:" >&2
         printf '%s\n' "$dgrecv_out" >&2; exit 1; }
cp "$OUT/HotUpdateDgRecvPatch.bpi" "$OUT/confused-dgrecv.bpi"
dgrecv_off=""
for o in $(LC_ALL=C grep -abo -- 'HotUpdateBase.Describer' "$OUT/confused-dgrecv.bpi" | cut -d: -f1); do
    [ "$(od -An -tx1 -j $((o - 2)) -N2 "$OUT/confused-dgrecv.bpi" | tr -d ' ')" = "1700" ] || continue
    [ -z "$dgrecv_off" ] \
        || { echo "FAIL: HotUpdateDgRecvPatch names HotUpdateBase.Describer as a pooled entry more than once — the corruption cannot be located by content" >&2; exit 1; }
    dgrecv_off=$o
done
[ -n "$dgrecv_off" ] \
    || { echo "FAIL: HotUpdateDgRecvPatch no longer carries HotUpdateBase.Describer as a pooled entry — the corruption cannot be located by content" >&2; exit 1; }
printf 'HotUpdateBase.Annotator' | dd of="$OUT/confused-dgrecv.bpi" bs=1 seek="$dgrecv_off" conv=notrunc 2>/dev/null
set +e
dgrecv_err=$("./$OUT/HotUpdateBase" "$OUT/confused-dgrecv.bpi" 2>&1 >/dev/null); dgrecv_rc=$?
set -e
# Measured with the closure/frame contradiction test removed, this exact image
# exits 139 (SIGSEGV, three runs of three) — and it does so INSIDE the `call`
# arm's own receiver test, whose self->type read has the premise every receiver
# test in this interpreter has: that a slot in a reference position holds an
# object. A register the caller never wrote does not, which is why the
# contradiction is refused where it reads as one.
if [ "$dgrecv_rc" -eq 0 ]; then
    echo "FAIL: a delegate closure with a captured receiver was run as a static frame" >&2
    exit 1
fi
if ! grep -q "captured receiver reached a static patch frame" <<<"$dgrecv_err"; then
    echo "FAIL: the arity-confused delegate closure did not raise the refusal:" >&2
    printf '%s\n' "$dgrecv_err" >&2
    exit 1
fi
echo "OK (arity-confused patch delegate closure refused)"

echo "-- an AOT delegate target of the wrong signature is refused --"
# The other side of the same install. The sections above gate the capture on the
# RECEIVER and on the call shape; this one compares the target's signature with
# the delegate's own. An AOT delegate's `method` is the bound import's raw function pointer and
# is invoked through the delegate's Invoke C++ ABI, so a target of a different
# shape is called with registers the caller never set — and a reference parameter
# left unset is a pointer forged out of whatever was live, not a wrong value.
#
# Two in-place pooled writes, both length-preserving: "Rate" -> "Warn" (4 bytes)
# and "(Single):String" -> "(String):String" (15). The bind then resolves
# QuotaEx.Warn(string) while the delegate stays a Tuner, whose Invoke passes its
# argument in the floating-point register. Each pooled string occurs exactly once
# in the baked image (the sigShape entry is shared with the Tuner::Invoke import,
# which binds by name and never reads it), and the uniqueness is asserted before
# the writes so a fixture that grows a second user fails here loudly.
invoke_cli --emit-patch "$dgsig_app" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Positive control first: the signature test must not reject the legitimate one.
dgsig_out=$("./$OUT/HotUpdateBase" "$OUT/HotUpdateDgSigPatch.bpi")
grep -q '^rate#3$' <<<"$(strip_cr_win "$dgsig_out")" \
    || { echo "FAIL: the uncorrupted delegate-signature fixture did not print its rate:" >&2
         printf '%s\n' "$dgsig_out" >&2; exit 1; }
cp "$OUT/HotUpdateDgSigPatch.bpi" "$OUT/confused-dgsig.bpi"
dgsig_nameoff=$(LC_ALL=C grep -abo -- 'Rate' "$OUT/confused-dgsig.bpi" | cut -d: -f1)
dgsig_shapeoff=$(LC_ALL=C grep -abo -- '(Single):String' "$OUT/confused-dgsig.bpi" | cut -d: -f1)
[ "$(printf '%s\n' "$dgsig_nameoff" | wc -l | tr -d ' ')" = "1" ] && [ -n "$dgsig_nameoff" ] \
    && [ "$(printf '%s\n' "$dgsig_shapeoff" | wc -l | tr -d ' ')" = "1" ] && [ -n "$dgsig_shapeoff" ] \
    || { echo "FAIL: HotUpdateDgSigPatch no longer names Rate/(Single):String exactly once — the corruption cannot be located by content" >&2; exit 1; }
printf 'Warn' | dd of="$OUT/confused-dgsig.bpi" bs=1 seek="$dgsig_nameoff" conv=notrunc 2>/dev/null
printf '(String):String' | dd of="$OUT/confused-dgsig.bpi" bs=1 seek="$dgsig_shapeoff" conv=notrunc 2>/dev/null
set +e
dgsig_err=$("./$OUT/HotUpdateBase" "$OUT/confused-dgsig.bpi" 2>&1 >/dev/null); dgsig_rc=$?
set -e
# Measured with dg_aot_signature_ok forced true, this exact image exits 134
# (three runs of three): Warn concatenates the string its pointer parameter names
# and the parameter is register residue, so the GC is asked for
# "18014398509260944 KiB" and aborts out of memory. Exit 0 would be worse still —
# a wrong answer built out of whatever those bytes were.
if [ "$dgsig_rc" -eq 0 ]; then
    echo "FAIL: an AOT delegate target of the wrong signature was accepted" >&2
    exit 1
fi
if ! grep -q "is not the delegate's Invoke signature" <<<"$dgsig_err"; then
    echo "FAIL: the signature-confused delegate target did not raise the signature refusal:" >&2
    printf '%s\n' "$dgsig_err" >&2
    exit 1
fi
echo "OK (signature-confused AOT delegate target refused)"

echo "-- deployment: a *.bpi directory loads version-ordered, newest wins --"
invoke_cli --emit-patch "$dir1_app" --base-abi "$OUT/base-abi.json" --patch-version 1 -o "$OUT"
invoke_cli --emit-patch "$dir2_app" --base-abi "$OUT/base-abi.json" --patch-version 2 -o "$OUT"
rm -rf "$OUT/patches"
mkdir -p "$OUT/patches"
# Deliberately misordered by filename: name order alone would apply version 2
# (a-newer) before version 1 (z-older), so asserting "install v1" before
# "install v2" proves the ascending-patchVersion sort. The non-.bpi file is
# ignored by enumeration; the stale BPI and the unknown-flags BPI are
# header-skipped with a stderr diagnostic — loaded:2 proves none blocked or
# counted.
cp "$OUT/HotUpdateDirPatch2.bpi" "$OUT/patches/a-newer.bpi"
cp "$OUT/HotUpdateDirPatch1.bpi" "$OUT/patches/z-older.bpi"
echo "not a patch" > "$OUT/patches/notes.txt"
cp "$OUT/stale.bpi" "$OUT/patches/stale.bpi"
cp "$OUT/flagged.bpi" "$OUT/patches/flagged.bpi"
# True: the last-installed probe (v2) is an instance of the registration
# Type.GetType now resolves; False: the first install keeps v1's type-info.
expected="install v1
install v2
loaded:2
probe v2 describing
counter:22
True
False"
dir_err_file=$(mktemp)
set +e
dir_out=$("./$OUT/HotUpdateBase" --load-dir "$OUT/patches" 2>"$dir_err_file"); dir_rc=$?
set -e
assert_output "$(strip_cr_win "$dir_out")" "$expected"
assert_exit_code "$dir_rc" 0
if ! grep -q "unsupported BPI header flags" "$dir_err_file"; then
    echo "FAIL: directory load stderr missing the unknown-flags skip diagnostic:" >&2
    cat "$dir_err_file" >&2
    rm -f "$dir_err_file"
    exit 1
fi
rm -f "$dir_err_file"
echo "OK (directory deployment)"

echo "-- deployment: an entry-less BPI loads via Load, is rejected by Run --"
cp "$OUT/HotUpdateDirPatch1.bpi" "$OUT/noentry.bpi"
# Blank the header's entryMethodIdx (offset 40) to the 0xFFFFFFFF "none"
# sentinel: a registration-only image, as a converter for entry-less patch
# assemblies would produce.
printf '\xff\xff\xff\xff' | dd of="$OUT/noentry.bpi" bs=1 seek=40 count=4 conv=notrunc 2>/dev/null
set +e
noentry_out=$("./$OUT/HotUpdateBase" --load "$OUT/noentry.bpi"); noentry_rc=$?
set -e
assert_output "$(strip_cr_win "$noentry_out")" "registered"
assert_exit_code "$noentry_rc" 0
if "./$OUT/HotUpdateBase" "$OUT/noentry.bpi" >/dev/null 2>&1; then
    echo "FAIL: Run accepted a BPI with no entry method" >&2
    exit 1
fi
echo "OK (entry-less BPI)"

echo "-- a hot-update base over a real-CoreLib closure --"
# Every base above is intrinsic-BCL only; this one is transpiled against the
# real net10.0 CoreLib, whose member tables a --hotupdate-base build keeps in
# full — including interface rows whose signature-only invoker thunks name
# struct types the emit set never declared (Span<char>/ReadOnlySpan<char> at
# ISpanFormattable.TryFormat, pulled in by System.Enum's real bodies;
# DictionaryEntry at IDictionaryEnumerator.get_Entry, pulled in by a
# Dictionary<string,bool> the base never enumerates to entry values). Such a row
# would otherwise emit a thunk over an undeclared t_ struct and fail the
# compile_console below with "unknown type name"; each carries a trapping
# invmiss_ stub instead, so the FIRST assert is simply that this base
# compiles and links at all. The greps pin the mechanism — the two staged rows
# really were blocked and really got the stub — so a future change that
# silently un-blocks or un-traps them is caught even while the compile stays
# green.
cl_base="samples/dotnet/HotUpdateCoreLibBase/bin/$CONFIG/$TFM/HotUpdateCoreLibBase.dll"
cl_patch="samples/dotnet/HotUpdateCoreLibPatch/bin/$CONFIG/$TFM/HotUpdateCoreLibPatch.dll"
cl_bad="samples/dotnet/HotUpdateCoreLibBadPatch/bin/$CONFIG/$TFM/HotUpdateCoreLibBadPatch.dll"
invoke_cli "$cl_base" -r "$(resolve_net10_corelib)" --auto-ref --hotupdate-base -o "$OUT/corelib"
grep -q 'System.ISpanFormattable.TryFormat: no reflection invoker in this image (the signature names t_System_Span_Char' \
        "$OUT/corelib"/generated_m*.cpp \
    || { echo "FAIL: the blocked ISpanFormattable.TryFormat row carries no invmiss_ trap" >&2; exit 1; }
grep -q 'System.Collections.IDictionaryEnumerator.get_Entry: no reflection invoker in this image (the signature names t_System_Collections_DictionaryEntry' \
        "$OUT/corelib"/generated_m*.cpp \
    || { echo "FAIL: the blocked IDictionaryEnumerator.get_Entry row carries no invmiss_ trap" >&2; exit 1; }
invoke_cli --emit-patch "$cl_patch" --base-abi "$OUT/corelib/base-abi.json" -o "$OUT/corelib"
invoke_cli --emit-patch "$cl_bad" --base-abi "$OUT/corelib/base-abi.json" -o "$OUT/corelib"
compile_console "$OUT/corelib" HotUpdateCoreLibBase

# The well-behaved patch binds (static imports + an interface-typed callvirt on
# the very CoreLib interface whose OTHER row is trapped) and runs to its fixed
# transcript. The interpreted lines' oracle is
# `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet HotUpdateCoreLibPatch.dll` run
# against the base's post-Main dictionary state (alpha/beta present) — the env
# var for the same reason the classic transcript's oracle carries it, see the
# header. This transcript happens to print no double today, which is exactly why
# the instruction belongs here: the first one somebody adds would otherwise be
# baked in whatever separator their machine uses.
cl_expected="base: start
-1
True
2
alpha=True
beta=False
patch: start
3
True
False
3
patch: done
base: done"
set +e
cl_out=$("./$OUT/corelib/HotUpdateCoreLibBase" "$OUT/corelib/HotUpdateCoreLibPatch.bpi"); cl_rc=$?
set -e
assert_output "$(strip_cr_win "$cl_out")" "$cl_expected"
assert_exit_code "$cl_rc" 0

# Invoking the trapped row: HotUpdateCoreLibBadPatch callvirts get_Entry. The
# refusal is LOUD and CATCHABLE — the loader's one-shot bind pass rejects the
# image with a NotSupportedException the base's own catch prints, because the
# interpreter's marshal surface (scalars + references) is strictly narrower
# than the invoker ABI and the import's signature names the missing struct. It
# must never be a silent bind, an abort, or a base that failed to compile in the
# first place. If the marshal surface ever widens,
# the call proceeds onto the invmiss_ stub and this transcript changes to that
# stub's message — same exception type, then naming the method and the layout;
# re-freeze deliberately when that happens.
cl_bad_expected="base: start
-1
True
2
alpha=True
beta=False
base caught: BPI bind: unresolved type import in a signature
base: done"
set +e
cl_bad_out=$("./$OUT/corelib/HotUpdateCoreLibBase" "$OUT/corelib/HotUpdateCoreLibBadPatch.bpi"); cl_bad_rc=$?
set -e
assert_output "$(strip_cr_win "$cl_bad_out")" "$cl_bad_expected"
assert_exit_code "$cl_bad_rc" 0
echo "OK (real-CoreLib base: compiles, blocked rows trapped, patch runs, trapped-row invoke refused catchably)"

# ---- Console.Error in a CoreLib-less base image ------------------------------
# The mirror image of the section above: no CoreLib at all in the load set, which
# is this bucket's default posture and the one gates/measure-interp.sh transpiles
# under. Console.get_Error cannot answer with the managed Dn2CppConsoleWriter
# there — the shim's System.IO.TextWriter base is an unresolved External name, so
# the subtype has no base vtable, its chained TextWriter::.ctor() resolves to
# nothing, and its get_Encoding slot names an untypeable External
# System.Text.Encoding — so the route falls back to the header-less runtime
# writer, which is the whole of what this load set models.
#
# It is asserted HERE, in the suite, because InterpBench is the only CoreLib-less
# program in the tree that touches Console.Error, and the only other thing that
# runs it is a measurement aid outside the build-and-run-* glob. A regression
# here once stood for over two weeks. Capturing the two streams separately is
# the point — a fallback that wrote the marker to stdout would still "work".
echo "== CoreLib-less Console.Error =="
interp_app="samples/dotnet/InterpBench/bin/$CONFIG/$TFM/InterpBench.dll"
invoke_cli "$interp_app" --hotupdate-base -o "$OUT/interp"
compile_console "$OUT/interp" InterpBench
set +e
ie_out=$("./$OUT/interp/InterpBench" noop 1 2>"$OUT/interp/native.err"); ie_rc=$?
set -e
assert_output "$(strip_cr_win "$ie_out")" "checksum:noop:0"
assert_output "$(strip_cr_win "$(cat "$OUT/interp/native.err")")" "impl:Kernel"
assert_exit_code "$ie_rc" 0
echo "OK (CoreLib-less Console.Error reaches stderr and only stderr)"
gate_cache_commit
