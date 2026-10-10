#!/usr/bin/env bash
# Transpiler resource bounds: the transpiler must not consume unbounded memory on
# a hostile or oversized input. It has to fail — fast, loudly, and with something
# the caller can act on — rather than take the machine into swap.
#
# Every other gate asserts what the transpiler PRODUCES. This one asserts what it
# REFUSES — and, on one axis, what it now deliberately HANDLES instead — plus the
# one carve-out lever the operator has for the rest:
#
#   1. Monomorphization has no natural fixpoint. A generic that calls itself at a
#      strictly deeper type argument (Descend<T> -> Descend<List<T>>) names a new,
#      never-coinciding instantiation every round; canonical sharing collapses
#      instantiations that COINCIDE, so it does not help. Unbounded, this
#      input allocates until the machine dies ("Out of memory"). It must be
#      rejected at the nesting bound, naming the recursion and the lever
#      (DN2CPP_MAX_GENERIC_DEPTH) — and the lever must actually move the bound.
#
#   1b. The same runaway with NO CALL to drop, in its two forms — and they have
#      DIFFERENT answers, which is the point of asserting both.
#
#      A generic whose METHOD signature names a deeper instantiation of itself —
#      Box<T>.Deeper() -> Box<Pair<bool,T>> — recurses with nothing calling
#      Deeper if completing a closed generic decodes every member's signature.
#      That is not a contrived shape: it is GDTask/UniTask's
#      GDTask<T>.SuppressCancellationThrow() -> GDTask<(bool, T)> — the
#      self-referential-signature runaway, which grows the heap at ~1 GB/s from
#      `-r GDTask.dll` alone.
#      Completing reached Box<int> can name Deeper's return-type shell, whose own
#      members must stay deferred. Original and retained application metadata both
#      stop at exactly two Box specializations. A regression
#      that decodes an unreached specialization's members makes this input recurse.
#
#      A generic whose FIELD does the same — Node<T>.Next : Node<List<T>> — still
#      recurses: its type is decoded on demand
#      too, but the DEMAND differs. A method's signature is read because something
#      called the method. A field's type is read because something needs its declaring
#      type's LAYOUT — and a layout is not a call: the emit set asks every class it emits
#      for one, so Node<int> being emitted names Node<List<int>>, whose layout names the
#      next. Nothing calls anything, so the reach chain is still empty and cannot localize
#      the fault; the bound is still the only thing that stops it, and the diagnostic still
#      has to name the driving MEMBER — and this asserts that it does.
#
#   2. The heap ceiling (--max-heap-mb / DN2CPP_MAX_HEAP_MB) is the operator's
#      lever for everything else: growth that is finite but bigger than this
#      machine can afford. Off by default (every other gate proves that — they
#      would all fail otherwise). Set, it must fail with exit 2 and name the phase
#      that ran away. Crucially it must fire under --measure too: that mode records
#      per-method exceptions as gap rows and keeps going, so a guard placed inside
#      that arm would be swallowed — and each swallowed overrun would add another
#      row to the very list that is already too big.
#
#   3. The model must scale with what the program REACHES, not with the metadata it
#      was handed. A `-r` assembly contributes a MethodInfo per method row and a
#      FieldInfo per field row it declares — tens of thousands of signatures and thousands
#      of field types for a program reaching a few hundred methods against the real
#      CoreLib — and decoding either is both the
#      expensive half and what mints the closed generics it names. Most are never asked
#      about, and must therefore never be decoded. Axis 1b proves that for a single
#      unreached member; this proves it in the aggregate, which is the only way to catch
#      the regression that actually happens: somebody writes `foreach (var c in Classes)
#      … c.Methods … .Signature` (or `… c.Fields … .Type`), every saving quietly goes
#      away, and every other gate stays green because the OUTPUT is unchanged.
#
#   4. --cut Type::Method is the operator's carve-out lever for what none of the above
#      can reach: a library method the transpiler models correctly but whose subtree is
#      not worth carrying (GDTask's TaskTracker, and its editor-only reflection tail).
#      Same semantics as the backend bounded sets — the subtree is cut at reachability
#      and the call sites yield the default — but per run, so no library name is baked
#      into dn2cpp. A spec resolving to nothing must fail loudly: a typo silently
#      becoming a no-op cut is a footgun, and one that would be found much later.
#
#   5. --measure's dangling-symbol sweep must RUN, and a clean corpus must
#      report zero rows. The sweep is AssertCalledBodiesEmitted's cut => route diff,
#      adapted to a mode that drops failing bodies (their symbols are unioned into
#      the defined set off their own gap rows), and it is the widest sweep of that
#      invariant the repository has — a --measure over a real 400-kLOC game walks
#      far past what any gate links. This section is the sweep's only in-suite
#      coverage, so it asserts presence, not just absence: the "N named symbols
#      diffed" count must be nonzero, because a silently skipped sweep and a clean
#      corpus would otherwise print the same zero.
#
#   6. The reflection-invoke route walks whole classes, and a walked class's bodies
#      mint the instantiations they name. A definition whose methods each name a
#      deeper instantiation of itself would grow a whole walk exponentially in the
#      program's nesting depth. The route walks a bounded number of each definition's
#      minted instantiations, so the transpile completes and the definition's
#      instantiations grow linearly in that bound instead.
#
# A sibling measurement aid, gates/measure-transpile-mem.sh, reports peak RSS and
# the per-phase heap curve. This gate asserts; that one measures.
# Canonical linking and synthesized-wrapper lowering must preserve fatal bounds
# while ordinary unsupported wrapper shapes still fall back.
# Executable ILDiet depth summaries abort on operator budgets, retain deep caller
# substitutions, and distinguish preserved dispatch/data bodies from execution.
# Closed copied shapes in unused bodies do not consume the execution budget.
# Reflection-only whole-class roots keep emission's bounded minted walk.
# Attribute rows retain bodies; only live attribute reads promote constructors
# and named setters, without executing getters or a hidden base setter.
# Late decoded reflected field boxes retain their Object overrides beyond the
# whole-class walk bounds, with and without shared generics or managed stripping.
# Branch summaries share boolean getter folds and known loaded type identities;
# explicit/inherited interface maps, class newslot identities and constrained
# primitive slots exclude unrelated bodies; ISA guards share the capability verdict.
# Primary-input CoreLib/runtime identity collisions fail before base-chain walks;
# embedded internal metadata types and ordinary intrinsic-BCL inputs still run.
# Validated ordinary cuts exclude body depth summaries while preserving signatures.
source "$(dirname "$0")/_common.sh"

out="artifacts/transpiler-limits"
sig_out="artifacts/transpiler-limits-sig"
sig_diet_out="artifacts/transpiler-limits-sig-ildiet"
cut_out="artifacts/transpiler-limits-cut"
mint_out="artifacts/transpiler-limits-mint"

echo "== 1/9 Locating the real CoreLib, building the sample assemblies =="
corelib=$(locate_corelib)
echo "corelib: $corelib"
build_proj samples/dotnet/GenericRecursionBad/GenericRecursionBad.csproj
build_proj samples/dotnet/GenericSignatureRecursionBad/GenericSignatureRecursionBad.csproj
build_proj samples/dotnet/GenericFieldRecursionBad/GenericFieldRecursionBad.csproj
build_proj samples/dotnet/GenericArrayFieldRecursionBad/GenericArrayFieldRecursionBad.csproj
build_proj samples/dotnet/StringCore/StringCore.csproj
build_proj samples/dotnet/ArrayCore/ArrayCore.csproj
build_proj samples/dotnet/SharedTrialMint/SharedTrialMint.csproj
build_proj samples/dotnet/TypeofMissingAsmBad/TypeofMissingAsmBad.csproj
# These compiler probes live outside the suite's samples-only prebuild.
build_gate_proj gates/fixtures/transpiler-limits/CanonicalLink/CanonicalLinkBound.csproj
build_gate_proj gates/fixtures/transpiler-limits/WrapperExceptions/WrapperExceptions.csproj
build_gate_proj gates/fixtures/transpiler-limits/ReflectionRouteNesting/ReflectionRouteNesting.csproj
build_gate_proj gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj
collision_fixture=gates/fixtures/transpiler-limits/CoreLibCollision
for shape in Object ValueType Unsafe Control; do
    build_gate_proj "$collision_fixture/$shape/Collision$shape.csproj"
done
rec_app="samples/dotnet/GenericRecursionBad/bin/$CONFIG/$TFM/GenericRecursionBad.dll"
sig_app="samples/dotnet/GenericSignatureRecursionBad/bin/$CONFIG/$TFM/GenericSignatureRecursionBad.dll"
fld_app="samples/dotnet/GenericFieldRecursionBad/bin/$CONFIG/$TFM/GenericFieldRecursionBad.dll"
afld_app="samples/dotnet/GenericArrayFieldRecursionBad/bin/$CONFIG/$TFM/GenericArrayFieldRecursionBad.dll"
big_app="samples/dotnet/StringCore/bin/$CONFIG/$TFM/StringCore.dll"
arr_app="samples/dotnet/ArrayCore/bin/$CONFIG/$TFM/ArrayCore.dll"
mint_app="samples/dotnet/SharedTrialMint/bin/$CONFIG/$TFM/SharedTrialMint.dll"
tma_app="samples/dotnet/TypeofMissingAsmBad/bin/$CONFIG/$TFM/TypeofMissingAsmBad.dll"
link_app="gates/fixtures/transpiler-limits/CanonicalLink/bin/$CONFIG/$TFM/CanonicalLinkBound.dll"
wrapper_app="gates/fixtures/transpiler-limits/WrapperExceptions/bin/$CONFIG/$TFM/WrapperExceptions.dll"
nest_app="gates/fixtures/transpiler-limits/ReflectionRouteNesting/bin/$CONFIG/$TFM/ReflectionRouteNesting.dll"
summary_app="gates/fixtures/transpiler-limits/ReflectionDepthSummary/bin/$CONFIG/$TFM/ReflectionDepthSummary.dll"
# The assembly section 8 withholds and then supplies. It sits beside the CoreLib in
# the shared framework; a requested reference that is absent is a hard failure, not
# a quietly dropped one — withholding it is the whole point of the section,
# so section 8 could not tell "refused because unresolvable" from "refused because
# the file was not there".
numerics_dll="$(dirname "$corelib")/System.Runtime.Numerics.dll"
[ -f "$numerics_dll" ] \
    || { echo "error: System.Runtime.Numerics.dll not found beside the CoreLib: $numerics_dll" >&2; exit 1; }

# The whole gate asserts transpiler BEHAVIOR — what the transpiler REFUSES
# (exit codes, diagnostics, the census ceilings), most of which produces no
# output surface to key on — so the cache key stands in for the transpiler
# itself via _gate_cli_hash (see that helper's doc). The out dirs are EMPTIED —
# cleared and recreated — BEFORE the check, so the key's surface term is the
# stable `no-generated` marker; on a miss the sections below rewrite them. The
# keyed dir has to EXIST: an absent one is unreadable rather than empty, and
# gate_cache_check answers that with a warning and no key, which would
# leave this gate uncacheable since it clears the dirs on every run.
rm -rf "$out" "$sig_out" "$sig_diet_out" "$cut_out" "$mint_out"; mkdir -p "$out"
if gate_cache_check "$out" "transpiler-limits|recursive-body:ildiet+no-ildiet|canonical-cap:no-ildiet:1,2|canonical-refs:none|wrapper-exceptions|mint-cap:no-ildiet|depth-summary-env:default,depth8,depth64,count128,count2m,depth1-abort,count1-abort,deep64,uncalled-virtual,uncalled-interface,unallocated-receiver,construction-accessor,generic-accessor,generic-ctor,executed-generic-argument,event,late-construction-accessor:depth3-abort,direct-accessor,direct-event-add,invoke-uncalled-virtual,generic-factory:direct,class,method,identity|unused-copied:low2,default32:ildiet+no-ildiet|ordinary-cut:ildiet+no-ildiet|execution-provenance:base-call,base-ctor,object-slot,primitive-dead-branch,abstract-app,abstract-library,nominal-dead-branch,methodimpl,constrained-primitive,const-getter,unused-default-interface,inherited-interface-map,protected-interface-map,class-newslot-map,isa-getter:Sve+Sve2+Arm64:optimize=true|attributes:unread-ctor,unread-getter,read,generic-read:depth3+depth4-abort:field-boxes=ildiet,no-ildiet,no-shared-generics:prefix=before-reflected-field-boxes|sig:no-ildiet+ildiet|collision:no-ildiet,corelib+intrinsic|cli:$(_gate_cli_hash)|$corelib" \
        "$rec_app" "$sig_app" "$fld_app" "$afld_app" "$big_app" "$arr_app" "$mint_app" "$tma_app" \
        gates/fixtures/transpiler-limits/CanonicalLink/Program.cs \
        gates/fixtures/transpiler-limits/CanonicalLink/CanonicalLinkBound.csproj \
        gates/fixtures/transpiler-limits/WrapperExceptions/Program.cs \
        gates/fixtures/transpiler-limits/WrapperExceptions/WrapperExceptions.csproj \
        gates/fixtures/transpiler-limits/ReflectionRouteNesting/Program.cs \
        gates/fixtures/transpiler-limits/ReflectionRouteNesting/ReflectionRouteNesting.csproj \
        gates/fixtures/transpiler-limits/ReflectionDepthSummary \
        "$collision_fixture" \
        "$link_app" "$wrapper_app" "$nest_app" "$summary_app" \
        "${link_app%.dll}.runtimeconfig.json" "${link_app%.dll}.deps.json" \
        "${wrapper_app%.dll}.runtimeconfig.json" "${wrapper_app%.dll}.deps.json" \
        "${nest_app%.dll}.runtimeconfig.json" "${nest_app%.dll}.deps.json" \
        "${sig_app%.dll}.runtimeconfig.json" "${sig_app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

# Every diagnostic match below reads a here-string, never `printf … | grep -q`.
# That pipeline is unsound under this suite's `set -o pipefail`: grep -q exits at the
# first match, and if it wins the race against printf's write the pipe has no reader,
# printf dies of SIGPIPE and the PIPELINE reports 141 — so a message that IS present
# reads as absent. It is not a big-output hazard: measured here at 3,083 bytes, well
# under the pipe buffer, because the race is against grep's EXIT and not against the
# buffer filling. A here-string is fully materialized before grep starts.
echo "== 2/9 A self-deepening generic must hit the monomorphization bound =="
for rec_cap in 32 40; do
    summary_rec_rc=0
    summary_rec_out="$out/summary-rec-$rec_cap"
    summary_rec_err=$(export DN2CPP_MAX_GENERIC_DEPTH="$rec_cap"; \
        invoke_cli "$rec_app" -r "$corelib" -o "$summary_rec_out" 2>&1 >/dev/null) || summary_rec_rc=$?
    if [ "$summary_rec_rc" -ne 2 ] || [ -f "$summary_rec_out/ildiet/GenericRecursionBad.dll" ] \
            || ! grep -q "ILDiet depth summary needs generic nesting depth $((rec_cap + 1)), past the $rec_cap-level limit (DN2CPP_MAX_GENERIC_DEPTH)" <<<"$summary_rec_err" \
            || ! grep -q 'while summarizing .*Descend' <<<"$summary_rec_err" \
            || ! grep -q '\[chain: .*Descend.*Main' <<<"$summary_rec_err"; then
        echo "FAIL: the depth summary must abort at $((rec_cap + 1)) without rewritten IL (exit $summary_rec_rc)" >&2
        echo "$summary_rec_err" >&2
        exit 1
    fi
    echo "OK (summary abort: depth $((rec_cap + 1)), limit $rec_cap, no rewritten assembly)"
done
# Bypass the companion to independently exercise emission's fatal bound and chain.
rec_rc=0
rec_err=$(invoke_cli "$rec_app" -r "$corelib" --no-ildiet -o "$out" 2>&1 >/dev/null) || rec_rc=$?
if [ "$rec_rc" -ne 2 ]; then
    echo "FAIL: transpiling a self-deepening generic exited $rec_rc (expected 2)" >&2
    echo "$rec_err" >&2
    exit 1
fi
if ! grep -q "past the .*-level limit" <<<"$rec_err"; then
    echo "FAIL: the rejection does not name the monomorphization depth bound:" >&2
    echo "$rec_err" >&2
    exit 1
fi
if ! grep -q "DN2CPP_MAX_GENERIC_DEPTH" <<<"$rec_err"; then
    echo "FAIL: the rejection does not name the lever that raises the bound:" >&2
    echo "$rec_err" >&2
    exit 1
fi
# A ceiling, not a ban: raising it cannot make this input terminate, but it must be
# the DEPTH that stops it, at the new level. (invoke_cli is a shell function, so the
# override goes inside the subshell — bash does not reliably scope `VAR=x func`.)
deep_rc=0
deep_err=$(export DN2CPP_MAX_GENERIC_DEPTH=40; invoke_cli "$rec_app" -r "$corelib" --no-ildiet -o "$out" 2>&1 >/dev/null) || deep_rc=$?
if [ "$deep_rc" -ne 2 ] || ! grep -q "nested 41 deep" <<<"$deep_err"; then
    echo "FAIL: DN2CPP_MAX_GENERIC_DEPTH=40 did not move the bound to 41 (exit $deep_rc)" >&2
    echo "$deep_err" >&2
    exit 1
fi
echo "OK (rejected at the bound, names its lever, and the lever moves it)"

echo "== 3/9 A self-deepening METHOD signature must simply not recurse =="
# Original metadata keeps the unused method visible to the lazy decoder.
# This is the self-referential-signature runaway's shape —
# GDTask<T>.SuppressCancellationThrow() ->
# GDTask<(bool, T)> — which an eager member decode could only REFUSE at the depth bound.
# Reached Box<int>'s declarations can name its deeper return type, but
# Compilation.CompleteMembers must not recursively complete that specialization.
#
# So this asserts that the transpile COMPLETES. That is not a
# weak assertion, and it is the reason it lives here rather than in
# a memory gate — it is the only check in the suite that proves the deferral is real. A
# regression that quietly decodes an unreached specialization's members cannot pass it:
# this input recurses again and fails.
#
# And "it completed" is not the whole assertion: the binary must BEHAVE. The
# shape reaches emission, so the shell it leaves behind is emitted
# too — and an exact diff against real .NET is what proves that a specialization whose
# members were never decoded still lays out, links and runs correctly, rather than merely
# failing to blow the model up. (It is also the program section 6 cuts, so it has to build.)
for mode in "--measure" ""; do
    label=${mode:-emit}
    sig_rc=0
    sig_err=$(invoke_cli "$sig_app" -r "$corelib" --no-ildiet $mode -o "$sig_out" 2>&1 >/dev/null) || sig_rc=$?
    if [ "$sig_rc" -ne 0 ]; then
        echo "FAIL: the self-deepening METHOD shape ($label) exited $sig_rc — nothing calls Deeper(), so nothing" >&2
        echo "      should recursively complete the deeper specialization:" >&2
        echo "$sig_err" >&2
        exit 1
    fi
    echo "OK ($label: completed — the unreached specialization does not recurse)"
done
# And it stopped at ONE step, not thirty-two: Box<int> is reached (Main reads its field), so
# its members ARE decoded, which names Box<Pair<bool,int>> — a shell nothing reaches, whose
# own Deeper is therefore never read. Two Box types, no more. Asserting the count, not just
# the exit code, is what separates "the deferral worked" from "the bound happened to be
# generous".
# Distinct type names, not lines: each is declared once and defined once.
boxes=$(grep -o 't_GenericSignatureRecursionBad_Box_[A-Za-z0-9_]*' "$sig_out/generated.h" | sort -u | wc -l | tr -d ' ')
if [ "$boxes" -ne 2 ]; then
    echo "FAIL: expected exactly 2 Box specializations (the reached one and the shell its" >&2
    echo "      signature names), got $boxes — the chain did not stop at one step:" >&2
    grep -o 't_GenericSignatureRecursionBad_Box_[A-Za-z0-9_]*' "$sig_out/generated.h" | sort -u >&2
    exit 1
fi
echo "OK (the chain stopped at one step: 2 Box types, the second an undecoded shell)"

compile_console "$sig_out" GenericSignatureRecursionBad
set +e
sig_native=$("./$sig_out/GenericSignatureRecursionBad"); sig_native_rc=$?
sig_expected=$(dotnet "$sig_app"); sig_expected_rc=$?
set -e
assert_output "$sig_native" "$sig_expected"
assert_exit_code "$sig_native_rc" "$sig_expected_rc"
echo "OK (and the undecoded shell still lays out, links and runs — output matches real .NET)"

# Application metadata keeps Deeper's signature without reaching its body.
# Its return type creates the same single shell as unstripped metadata.
invoke_cli "$sig_app" -r "$corelib" -o "$sig_diet_out"
diet_boxes=$(grep -o 't_GenericSignatureRecursionBad_Box_[A-Za-z0-9_]*' "$sig_diet_out/generated.h" | sort -u | wc -l | tr -d ' ')
[ "$diet_boxes" -eq 2 ] || { echo "FAIL: expected exactly 2 Box specializations after ILDiet, got $diet_boxes" >&2; exit 1; }
for emitted in "$sig_out" "$sig_diet_out"; do
    if grep -q '^// GenericSignatureRecursionBad\.Box.*::Deeper[[:space:]]*$' "$emitted"/generated*; then
        echo "FAIL: an unused application signature rooted Deeper's executable body" >&2
        exit 1
    fi
done
compile_console "$sig_diet_out" GenericSignatureRecursionBad
set +e
diet_native=$("./$sig_diet_out/GenericSignatureRecursionBad"); diet_native_rc=$?
set -e
assert_output "$diet_native" "$sig_expected"
assert_exit_code "$diet_native_rc" "$sig_expected_rc"
echo "OK (ILDiet retains the unused signature without rooting its body; exactly 2 Box types and .NET output)"

echo "== 3b/9 A self-deepening FIELD must still hit the bound, and name the member =="
# A field is not a method — not because of any eager decode: its type is on demand
# too, and what differs is the DEMAND. A method's signature is read because something CALLED the
# method, so the self-deepening METHOD shape above simply stops. A field's type is read because something needs
# its declaring type's LAYOUT, and a layout is not a call: the emit set asks every class it
# emits for one. So this shape still recurses — Node<int> is emitted, its layout names
# Node<List<int>>, whose layout names the next — with nothing calling anything. The reach chain
# is therefore still empty and cannot localize the fault, the diagnostic still has to name the
# driving MEMBER, and the bound is still the only thing that stops it.
#
# The --measure arm asserts that --measure walks the type-layout closure at all.
# It compiles reachable bodies, and a mode that STOPPED there would be sound only if a
# field's type were decoded the moment its declaring type was NAMED. Deferred, such a
# mode would exit 0 here — nothing ever asks Node.Next what it is — and would be
# reporting a program as transpilable that emission cannot transpile. That is the one lie a
# feasibility harness must not tell, and this is what catches it. (The bound must also still
# ESCAPE the gap-row machinery, for the reason the heap ceiling is asserted here too: --measure
# records a transpile exception as a row and keeps draining, which for THIS exception would not
# merely defeat the bound but feed it.)
for mode in "" "--measure"; do
    label=${mode:-emit}
    fld_rc=0
    fld_err=$(invoke_cli "$fld_app" -r "$corelib" $mode -o "$out" 2>&1 >/dev/null) || fld_rc=$?
    if [ "$fld_rc" -ne 2 ]; then
        echo "FAIL: a field-driven self-deepening generic ($label) exited $fld_rc (expected 2)" >&2
        echo "$fld_err" >&2
        exit 1
    fi
    if ! grep -q "past the .*-level limit" <<<"$fld_err"; then
        echo "FAIL: the rejection ($label) does not name the monomorphization depth bound:" >&2
        echo "$fld_err" >&2
        exit 1
    fi
    if ! grep -q "DN2CPP_MAX_GENERIC_DEPTH" <<<"$fld_err"; then
        echo "FAIL: the rejection ($label) does not name the lever that raises the bound:" >&2
        echo "$fld_err" >&2
        exit 1
    fi
    if ! grep -q "Driven by the signature of field .*Node.*\.Next" <<<"$fld_err"; then
        echo "FAIL: the rejection ($label) does not name the field whose signature drove it:" >&2
        echo "$fld_err" >&2
        exit 1
    fi
    echo "OK ($label: rejected at the bound, named its lever and the driving field signature)"
done

echo "== 3c/9 A self-deepening FIELD via an ARRAY wrapper must hit the bound too =="
# The array-deepening twin of 3b, and the regression proof for the wrappers-count-a-depth-level
# rule. A TypeArgDepth that
# treats an array/byref/pointer wrapper as transparent — Box<int>, Box<int[]>, Box<int[][]> all
# measuring depth 1 — lets `Box<T>.Next : Box<T[]>` slip past the DEPTH cap and burn the
# instantiation COUNT cap (10^6) instead: gigabytes of ClassInfo shells, and a diagnostic that
# blames the reference closure rather than the recursion. With each wrapper counting a level, the
# measured depth climbs every round and trips the depth bound promptly — same assertions as 3b,
# same field-driven demand (a layout is not a call, so nothing reaches Next; the emit-set and the
# --measure layout closure decode it anyway).
for mode in "" "--measure"; do
    label=${mode:-emit}
    afld_rc=0
    afld_err=$(invoke_cli "$afld_app" -r "$corelib" $mode -o "$out" 2>&1 >/dev/null) || afld_rc=$?
    if [ "$afld_rc" -ne 2 ]; then
        echo "FAIL: an array-deepening field-driven generic ($label) exited $afld_rc (expected 2)" >&2
        echo "$afld_err" >&2
        exit 1
    fi
    if ! grep -q "past the .*-level limit" <<<"$afld_err"; then
        echo "FAIL: the rejection ($label) does not name the monomorphization depth bound:" >&2
        echo "$afld_err" >&2
        exit 1
    fi
    if ! grep -q "DN2CPP_MAX_GENERIC_DEPTH" <<<"$afld_err"; then
        echo "FAIL: the rejection ($label) does not name the lever that raises the bound:" >&2
        echo "$afld_err" >&2
        exit 1
    fi
    if ! grep -q "Driven by the signature of field .*Box.*\.Next" <<<"$afld_err"; then
        echo "FAIL: the rejection ($label) does not name the field whose signature drove it:" >&2
        echo "$afld_err" >&2
        exit 1
    fi
    echo "OK ($label: array-deepening field rejected at the DEPTH bound, not the count cap)"
done

echo "== 3d/9 The bound must ESCAPE the emit side's swallowing arms =="
# Sections 2-3c trip the bound in the MODEL, where nothing catches anything. This one
# trips it inside CppEmitter, where several arms deliberately swallow a
# NotSupportedException and degrade — and InstantiationBoundException IS a
# NotSupportedException, so a catch filtered on the type alone eats it. The
# shared-generics planning pass is the arm that matters: it compiles a canonical trial
# body, records a NotSupportedException as `SharedTaint[m] = "unsupported"`, and carries
# on. Swallowed there the overrun does not merely survive, it FEEDS itself — and it also
# quietly changes which bodies get shared, which no output diff can see.
#
# It is a live arm, not a theoretical one: a canonical trial mints instantiations
# nothing else in the run names. samples/dotnet/SharedTrialMint is built around the
# cleanest such mint — an `int[]` flowing into an `IEnumerable<int>` parameter inside a
# SHARED generic method, which wires the array's collection-interface map and
# instantiates the emitter-invented Dn2Cpp.Runtime.SZArrayEnumerable<int> wrapper. No IL
# token names that type, so the reachability scan never mints it; the three concrete
# Run<T> bodies are forwarders that are never compiled, so the canonical trial is the
# only place it is ever created.
#
# The trip point is a COUNT, so the gate has to FIND it rather than hardcode one: a
# baked-in threshold drifts with every BCL and transpiler change and would silently stop
# testing anything. "The transpile succeeds" is monotone in the cap, so bisect for the
# smallest cap the program completes under — one below it is then exactly the last
# instantiation the run creates, and the trial's mints sit a handful of counts below that.
mint_so="$out/mint-stdout.txt"
mint_se="$out/mint-stderr.txt"
mint_run() { # <cap> — transpile under that cap; stdout/stderr to the files above
    rm -rf "$mint_out"
    (export DN2CPP_MAX_INSTANTIATIONS="$1" DN2CPP_SHARED_DUMP=1
     invoke_cli "$mint_app" --no-ildiet -r "$corelib" -o "$mint_out") >"$mint_so" 2>"$mint_se"
}
# Invariant of the search: mint_lo fails, mint_hi succeeds. A cap of 1 always fails (the
# program creates more than one instantiation); the ceiling doubles until one succeeds.
mint_lo=1
mint_hi=128
while ! mint_run "$mint_hi"; do
    mint_hi=$(( mint_hi * 2 ))
    if [ "$mint_hi" -gt 65536 ]; then
        echo "FAIL: SharedTrialMint does not transpile under any cap up to 65536" >&2
        cat "$mint_se" >&2
        exit 1
    fi
done
if mint_run "$mint_lo"; then
    echo "FAIL: a cap of $mint_lo transpiled SharedTrialMint — the count bound is not armed" >&2
    exit 1
fi
while [ $(( mint_hi - mint_lo )) -gt 1 ]; do
    mint_mid=$(( (mint_lo + mint_hi) / 2 ))
    if mint_run "$mint_mid"; then mint_hi=$mint_mid; else mint_lo=$mint_mid; fi
done
echo "OK (smallest cap this program transpiles under: $mint_hi)"

# Every cap below that must fail LOUDLY: exit 2, naming the bound on stderr. And the
# bound's text must never appear on STDOUT — that is where a swallowing arm reports its
# degrade (the planning pass's `shared-generics unsupported <method>: <message>` line),
# so the message showing up there means an arm ate a must-escape exception and the run
# kept going. The window is deliberately wider than the trial's mints need, so a BCL that
# shifts the tail by a few instantiations does not walk off the end of it.
mint_saw_trial=0
for cap in $(seq $(( mint_hi - 20 )) $(( mint_hi - 1 ))); do
    mint_rc=0
    mint_run "$cap" || mint_rc=$?
    if [ "$mint_rc" -ne 2 ]; then
        echo "FAIL: cap $cap (below the $mint_hi the program needs) exited $mint_rc (expected 2)" >&2
        cat "$mint_se" >&2
        exit 1
    fi
    if ! grep -q "instantiation count passed the $cap limit" "$mint_se"; then
        echo "FAIL: cap $cap did not fail on the count bound:" >&2
        cat "$mint_se" >&2
        exit 1
    fi
    if grep -q "instantiation count passed" "$mint_so"; then
        echo "FAIL: cap $cap — the bound was SWALLOWED and reported as a degrade on stdout." >&2
        echo "      An emit-side catch (NotSupportedException) is missing its" >&2
        echo "      'when (!Compilation.IsMustEscape(e))' filter; see AGENTS.md." >&2
        grep "instantiation count passed" "$mint_so" >&2
        exit 1
    fi
    # SZArrayEnumerable<T> is minted only while a canonical body is being compiled, and
    # the planning trial compiles it first — so a cap that fails naming it is the proof
    # that the swept window really does reach the swallowing arm. Asserting presence, not
    # just absence: a window that drifted past the trial would otherwise pass by testing
    # nothing, the same way a silently skipped sweep and a clean corpus both say zero.
    if grep -q "while instantiating SZArrayEnumerable" "$mint_se"; then
        mint_saw_trial=1
    fi
done
if [ "$mint_saw_trial" -ne 1 ]; then
    echo "FAIL: no cap in [$(( mint_hi - 20 )), $(( mint_hi - 1 ))] tripped the bound inside the" >&2
    echo "      shared-generics canonical trial — the window no longer covers the arm this" >&2
    echo "      section exists to test. Widen it, or re-derive it from the sample." >&2
    exit 1
fi
echo "OK (every cap below the bound failed loudly; none was swallowed into a shared-body taint)"

# And the program BEHAVES: the shared canonical body, the array-to-collection boundary it
# wires and the wrapper it mints have to be right, not merely bounded.
invoke_cli "$mint_app" -r "$corelib" -o "$mint_out" > /dev/null
compile_console "$mint_out" SharedTrialMint
set +e
mint_native=$("./$mint_out/SharedTrialMint"); mint_native_rc=$?
mint_expected=$(dotnet "$mint_app"); mint_expected_rc=$?
set -e
assert_output "$mint_native" "$mint_expected"
assert_exit_code "$mint_native_rc" "$mint_expected_rc"
echo "OK (and the shared body's array-to-collection boundary runs — output matches real .NET)"

echo "== 3e/9 Canonical linking must preserve the instantiation bound =="
# No BCL reference: the only instantiations are Id<string> and its canonical
# Id<CnRef>. The second mint occurs inside canonical linking's fallback arm.
for mode in "" "--measure"; do
    label=${mode:-emit}
    link_dir="$out/canonical-${label#--}"
    link_so="$out/canonical-${label#--}.stdout"
    link_se="$out/canonical-${label#--}.stderr"
    link_rc=0
    (export DN2CPP_MAX_INSTANTIATIONS=1
     invoke_cli "$link_app" --no-ildiet $mode -o "$link_dir") >"$link_so" 2>"$link_se" || link_rc=$?
    if [ "$link_rc" -ne 2 ] \
            || ! grep -q 'instantiation count passed the 1 limit while instantiating CanonicalLinkBound.Program::Id<CnRef>' "$link_se" \
            || ! grep -q 'DN2CPP_MAX_INSTANTIATIONS' "$link_se" \
            || grep -q 'instantiation count passed' "$link_so"; then
        echo "FAIL: canonical linking ($label) must fail with exit 2 and the count-bound diagnostic; got $link_rc" >&2
        cat "$link_so" "$link_se" >&2
        exit 1
    fi

    (export DN2CPP_MAX_INSTANTIATIONS=2
     invoke_cli "$link_app" --no-ildiet $mode -o "$link_dir") >"$link_so" 2>"$link_se"
    if [ -z "$mode" ]; then
        if ! grep -Eq '^inline .* Program_Id_TisCnRef_m[0-9]+\([^;]*\)$' "$link_dir/generated.h"; then
            echo "FAIL: raising the bound did not produce the canonical Id<CnRef> body" >&2
            exit 1
        fi
    elif ! grep -q '0 gaps total' "$link_so" \
            || [ ! -f "$link_dir/s0-gaps.tsv" ] || [ -s "$link_dir/s0-gaps.tsv" ]; then
        echo "FAIL: raising the canonical-link bound did not produce a clean measure report" >&2
        cat "$link_so" "$link_se" >&2
        exit 1
    fi
    echo "OK ($label: canonical-link bound escaped; raising it permits canonical sharing)"
done

echo "== 3f/9 Wrapper lowering must distinguish unsupported shapes from fatal bounds =="
wrapper_transpiler="$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll"
wrapper_rc=0
wrapper_result=$(dotnet "$wrapper_app" "$wrapper_transpiler") || wrapper_rc=$?
assert_output "$(strip_cr_win "$wrapper_result")" 'wrapper NotSupportedException: OK
wrapper InstantiationBoundException: OK
wrapper StrictCompletionException: OK'
assert_exit_code "$wrapper_rc" 0
echo "OK (ordinary wrapper failure falls back; fatal exceptions escape unchanged)"

echo "== 3g/9 Operator budgets do not change successful ILDiet depth summaries =="
summary_diet="$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/ildiet/ILDiet.dll"
summary_library="gates/fixtures/transpiler-limits/ReflectionDepthSummary/Library/bin/$CONFIG/$TFM/DepthData.dll"
summary_expected=$(dotnet "$summary_app")
summary_expected=$(strip_cr_win "$summary_expected")
grep -Fxq second <<< "$summary_expected" \
    || { echo 'FAIL: depth-summary fixture did not exercise its second field context' >&2; exit 1; }
for summary_case in default depth8 depth64 count128 count2m; do
    summary_dir="$out/depth-summary-$summary_case"
    (
        unset DN2CPP_MAX_GENERIC_DEPTH DN2CPP_MAX_INSTANTIATIONS
        case "$summary_case" in
            depth8) export DN2CPP_MAX_GENERIC_DEPTH=8 ;;
            depth64) export DN2CPP_MAX_GENERIC_DEPTH=64 ;;
            count128) export DN2CPP_MAX_INSTANTIATIONS=128 ;;
            count2m) export DN2CPP_MAX_INSTANTIATIONS=2000000 ;;
        esac
        dotnet exec "$summary_diet" "$summary_app" -r "$corelib" -o "$summary_dir"
    )
    cmp "$out/depth-summary-default/ReflectionDepthSummary.dll" "$summary_dir/ReflectionDepthSummary.dll"
    summary_actual=$(dotnet exec --runtimeconfig "${summary_app%.dll}.runtimeconfig.json" \
        "$summary_dir/ReflectionDepthSummary.dll")
    assert_output "$(strip_cr_win "$summary_actual")" "$(strip_cr_win "$summary_expected")"
    echo "OK ($summary_case: identical rewritten IL and original CLR output)"
done
(
    DN2CPP_SAMPLE_PROJECT_DIR=gates/fixtures/transpiler-limits/ReflectionDepthSummary
    DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    DN2CPP_SKIP_BUILD=1
    corelib_diff_gate ReflectionDepthSummary
)

# A budget too small for the summary must abort before any rewritten assembly.
for summary_case in depth1 count1; do
    summary_dir="$out/depth-summary-$summary_case"
    summary_rc=0
    (
        unset DN2CPP_MAX_GENERIC_DEPTH DN2CPP_MAX_INSTANTIATIONS
        case "$summary_case" in
            depth1) export DN2CPP_MAX_GENERIC_DEPTH=1 ;;
            count1) export DN2CPP_MAX_INSTANTIATIONS=1 ;;
        esac
        dotnet exec "$summary_diet" "$summary_app" -r "$corelib" -o "$summary_dir"
    ) >"$out/depth-summary-$summary_case.stdout" 2>"$out/depth-summary-$summary_case.stderr" || summary_rc=$?
    [ "$summary_case" = depth1 ] && summary_knob=DN2CPP_MAX_GENERIC_DEPTH || summary_knob=DN2CPP_MAX_INSTANTIATIONS
    if [ "$summary_rc" -ne 1 ] || [ -f "$summary_dir/ReflectionDepthSummary.dll" ] \
            || ! grep -q 'ILDiet depth summary' "$out/depth-summary-$summary_case.stderr" \
            || ! grep -q "$summary_knob" "$out/depth-summary-$summary_case.stderr"; then
        echo "FAIL: $summary_case must abort the depth summary without partial rewritten IL" >&2
        cat "$out/depth-summary-$summary_case.stdout" "$out/depth-summary-$summary_case.stderr" >&2
        exit 1
    fi
    echo "OK ($summary_case: named depth-summary budget; no rewritten assembly)"
done

# An operator-raised bound must preserve caller substitutions beyond the default.
summary_deep="$out/depth-summary-deep-input"
mkdir -p "$summary_deep/bin/$CONFIG/$TFM"
cp "$summary_library" "$summary_deep/bin/$CONFIG/$TFM/DepthData.dll"
dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
    -c "$CONFIG" -p:DefineConstants=DEEP_DEPTH_SUMMARY -p:BuildProjectReferences=false \
    -o "$summary_deep/bin/$CONFIG/$TFM" --nologo -v quiet
summary_rc=0
(
    unset DN2CPP_MAX_GENERIC_DEPTH DN2CPP_MAX_INSTANTIATIONS
    dotnet exec "$summary_diet" "$summary_deep/bin/$CONFIG/$TFM/ReflectionDepthSummary.dll" \
        -r "$corelib" -o "$out/depth-summary-deep-default"
) >"$out/depth-summary-deep-default.stdout" 2>"$out/depth-summary-deep-default.stderr" || summary_rc=$?
if [ "$summary_rc" -ne 1 ] || [ -f "$out/depth-summary-deep-default/ReflectionDepthSummary.dll" ] \
        || ! grep -q 'generic nesting depth 33, past the 32-level limit (DN2CPP_MAX_GENERIC_DEPTH)' "$out/depth-summary-deep-default.stderr"; then
    echo 'FAIL: deep profile must cross the default summary bound without emitting partial IL' >&2
    cat "$out/depth-summary-deep-default.stdout" "$out/depth-summary-deep-default.stderr" >&2
    exit 1
fi
(
    export DN2CPP_MAX_GENERIC_DEPTH=64
    unset DN2CPP_MAX_INSTANTIATIONS
    DN2CPP_SAMPLE_PROJECT_DIR="$summary_deep"
    DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:DEEP_DEPTH_SUMMARY|depth:64"
    DN2CPP_OUT_SUFFIX=-deep-summary
    DN2CPP_SKIP_BUILD=1
    corelib_diff_gate ReflectionDepthSummary
)
echo "OK (raised depth bound: deep caller substitution preserves the second field context)"

# Original dispatch bodies remain available without making unused slots executable.
for summary_dispatch in VIRTUAL INTERFACE RECEIVER; do
    summary_define="UNCALLED_${summary_dispatch}_DEPTH"
    summary_dispatch_expected=True
    if [ "$summary_dispatch" = RECEIVER ]; then
        summary_define=UNALLOCATED_RECEIVER_DEPTH
        summary_dispatch_expected=Safe
    fi
    summary_prepared="$out/depth-summary-uncalled-$summary_dispatch-input"
    mkdir -p "$summary_prepared/bin/$CONFIG/$TFM"
    cp "$summary_library" "$summary_prepared/bin/$CONFIG/$TFM/DepthData.dll"
    dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
        -c "$CONFIG" -p:DefineConstants="$summary_define" -p:BuildProjectReferences=false \
        -o "$summary_prepared/bin/$CONFIG/$TFM" --nologo -v quiet
    (
        DN2CPP_SAMPLE_PROJECT_DIR="$summary_prepared"
        DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:$summary_define"
        DN2CPP_OUT_SUFFIX="-uncalled-$summary_dispatch"
        DN2CPP_SKIP_BUILD=1
        corelib_diff_gate ReflectionDepthSummary
    )
    summary_native=$(run_bounded "./artifacts/reflectiondepthsummary-uncalled-$summary_dispatch/ReflectionDepthSummary")
    assert_output "$(strip_cr_win "$summary_native")" "$summary_dispatch_expected"
    if grep -Eq '^[^;]* (UncalledNode[^ ]*_Wrap|UncalledWorker_(Run|Descend)|ReceiverHolder_Uncalled|ColdReceiver[^ ]*_(Run|Descend))_m[0-9]+\(' \
            "artifacts/reflectiondepthsummary-uncalled-$summary_dispatch"/generated*.cpp; then
        echo 'FAIL: an uncalled dispatch body became executable' >&2
        exit 1
    fi
    echo "OK ($summary_dispatch: unused original dispatch bodies do not seed recursive depth contexts)"
done

# Copied framework types in unused bodies do not consume an execution budget.
for summary_copied in low default; do
    summary_define=UNUSED_COPIED_SHAPE_DEPTH
    summary_limit=2
    if [ "$summary_copied" = default ]; then
        summary_define=UNUSED_DEEP_COPIED_SHAPE_DEPTH
        summary_limit=32
    fi
    summary_prepared="$out/depth-summary-unused-copied-$summary_copied-input"
    mkdir -p "$summary_prepared/bin/$CONFIG/$TFM"
    cp "$summary_library" "$summary_prepared/bin/$CONFIG/$TFM/DepthData.dll"
    dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
        -c "$CONFIG" -p:DefineConstants="$summary_define" -p:BuildProjectReferences=false \
        -o "$summary_prepared/bin/$CONFIG/$TFM" --nologo -v quiet
    for summary_bypass in current original; do
        (
            unset DN2CPP_MAX_GENERIC_DEPTH DN2CPP_MAX_INSTANTIATIONS
            export DN2CPP_MAX_GENERIC_DEPTH="$summary_limit"
            DN2CPP_SAMPLE_PROJECT_DIR="$summary_prepared"
            DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:$summary_define|depth:$summary_limit|preprocessing:$summary_bypass"
            DN2CPP_OUT_SUFFIX="-unused-copied-$summary_copied-$summary_bypass"
            DN2CPP_SKIP_BUILD=1
            if [ "$summary_bypass" = current ]; then
                corelib_diff_gate ReflectionDepthSummary
            else
                corelib_diff_gate ReflectionDepthSummary --no-ildiet
            fi
        )
        summary_native=$(run_bounded "./artifacts/reflectiondepthsummary-unused-copied-$summary_copied-$summary_bypass/ReflectionDepthSummary")
        assert_output "$(strip_cr_win "$summary_native")" True
        if grep -Eq '^[^;]* CopiedShapeWorker_Uncalled_m[0-9]+\(' \
                "artifacts/reflectiondepthsummary-unused-copied-$summary_copied-$summary_bypass"/generated*.cpp; then
            echo 'FAIL: an unused copied-shape body became executable' >&2
            exit 1
        fi
    done
    echo "OK ($summary_copied: unused copied shape preserves CLR behavior below its retained body depth)"
done

# Accessor/event association and generic data retention preserve original bodies
# without executing them. Real calls and a later constructor route still promote.
for summary_surface in CONSTRUCTION_ACCESSOR GENERIC_ACCESSOR GENERIC_CTOR EXECUTED_GENERIC_ARGUMENT UNCALLED_EVENT LATE_CONSTRUCTION_ACCESSOR DIRECT_ACCESSOR DIRECT_EVENT_ADD INVOKE_UNCALLED_VIRTUAL GENERIC_FACTORY_DIRECT GENERIC_FACTORY_CLASS GENERIC_FACTORY_METHOD GENERIC_FACTORY_IDENTITY; do
    summary_define="${summary_surface}_DEPTH"
    summary_surface_expected=True
    case "$summary_surface" in
        LATE_CONSTRUCTION_ACCESSOR|DIRECT_ACCESSOR|DIRECT_EVENT_ADD) summary_surface_expected=second ;;
        INVOKE_UNCALLED_VIRTUAL) summary_surface_expected=$'7\nTrue' ;;
        GENERIC_FACTORY_*)
            summary_factory_kind=${summary_surface#GENERIC_FACTORY_}
            summary_factory_kind=$(printf '%s' "$summary_factory_kind" | tr '[:upper:]' '[:lower:]')
            summary_surface_expected=$(printf 'second\ngeneric-factory-%s-ran' "$summary_factory_kind") ;;

    esac
    summary_prepared="$out/depth-summary-$summary_surface-input"
    mkdir -p "$summary_prepared/bin/$CONFIG/$TFM"
    cp "$summary_library" "$summary_prepared/bin/$CONFIG/$TFM/DepthData.dll"
    dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
        -c "$CONFIG" -p:DefineConstants="$summary_define" -p:BuildProjectReferences=false \
        -o "$summary_prepared/bin/$CONFIG/$TFM" --nologo -v quiet
    (
        DN2CPP_SAMPLE_PROJECT_DIR="$summary_prepared"
        DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:$summary_define|reference:DepthData"
        DN2CPP_OUT_SUFFIX="-surface-$summary_surface"
        DN2CPP_SKIP_BUILD=1
        corelib_diff_gate ReflectionDepthSummary -r "$summary_library"
    )
    summary_native=$(run_bounded "./artifacts/reflectiondepthsummary-surface-$summary_surface/ReflectionDepthSummary")
    assert_output "$(strip_cr_win "$summary_native")" "$summary_surface_expected"
    if [ "$summary_surface" = INVOKE_UNCALLED_VIRTUAL ]; then
        (
            DN2CPP_SAMPLE_PROJECT_DIR="$summary_prepared"
            DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:$summary_define|no-ildiet"
            DN2CPP_OUT_SUFFIX=-surface-invoke-original
            DN2CPP_SKIP_BUILD=1
            corelib_diff_gate ReflectionDepthSummary --no-ildiet -r "$summary_library"
        )
    elif grep -Eq '^[^;]* (AccessorData_get_Value|AccessorData_Descend|ConstructorData_Descend|EventHolder_add_E|EventHolder_Descend|DirectAccessorData_set_Value|DirectAccessorData_Descend|DirectEventHolder_remove_E|DirectEventHolder_Descend)[^ ]*_m[0-9]+\(' \
            "artifacts/reflectiondepthsummary-surface-$summary_surface"/generated*.cpp; then
        echo "FAIL: $summary_surface emitted an uncalled accessor/event/data body" >&2
        exit 1
    fi
    if [ "$summary_surface" = LATE_CONSTRUCTION_ACCESSOR ]; then
        summary_rc=0
        (export DN2CPP_MAX_GENERIC_DEPTH=3
         invoke_cli "$summary_prepared/bin/$CONFIG/$TFM/ReflectionDepthSummary.dll" -r "$corelib" \
             -r "$summary_library" -o "$out/late-accessor-depth3") \
            >"$out/late-accessor-depth3.stdout" 2>"$out/late-accessor-depth3.stderr" || summary_rc=$?
        if [ "$summary_rc" -ne 2 ] || [ -f "$out/late-accessor-depth3/ildiet/ReflectionDepthSummary.dll" ] \
                || ! grep -q 'ILDiet depth summary needs generic nesting depth 4, past the 3-level limit (DN2CPP_MAX_GENERIC_DEPTH)' "$out/late-accessor-depth3.stderr"; then
            echo 'FAIL: a late constructor route did not promote its retained library getter depth context' >&2
            cat "$out/late-accessor-depth3.stdout" "$out/late-accessor-depth3.stderr" >&2
            exit 1
        fi
    fi
    echo "OK ($summary_surface: independent CLR parity; conservative bodies and executable depth roots stay distinct)"
done

# Exact calls and base constructors do not imply virtual dispatch or allocation.
# Depth summaries share emission's known identity and boolean getter branch graph.
for summary_execution in NONVIRTUAL_BASE_CALL BASE_CONSTRUCTOR INHERITED_OBJECT_SLOT DEAD_PRIMITIVE_BRANCH ABSTRACT_APPLICATION ABSTRACT_LIBRARY DEAD_NOMINAL_BRANCH METHODIMPL_PRIORITY CONSTRAINED_PRIMITIVE CONST_GETTER_BRANCH UNUSED_DEFAULT_INTERFACE INHERITED_INTERFACE_MAPPING PROTECTED_INTERFACE_MAPPING CLASS_NEWSLOT_MAPPING ISA_GETTER_BRANCH UNREAD_ATTRIBUTE_CTOR UNREAD_ATTRIBUTE_GETTER ATTRIBUTE_READ ATTRIBUTE_GENERIC_READ; do
    summary_define="${summary_execution}_DEPTH"
    summary_execution_refs=(-r "$summary_library")
    case "$summary_execution" in
        UNREAD_ATTRIBUTE_CTOR) summary_execution_expected=unread-attribute-ctor-ran ;;
        UNREAD_ATTRIBUTE_GETTER) summary_execution_expected=unread-attribute-getter-ran ;;
        ATTRIBUTE_READ|ATTRIBUTE_GENERIC_READ) summary_execution_expected=$'attribute-ctor\nattribute-setter\n7\nattribute-read-ran\n== late reflected field boxes ==\nsecond\nfield equals=True\nfield hash=29\nlate reflected field boxes end' ;;
        NONVIRTUAL_BASE_CALL) summary_execution_expected=base-call-ran ;;
        BASE_CONSTRUCTOR) summary_execution_expected=$'derived\nbase-constructor-ran' ;;
        INHERITED_OBJECT_SLOT) summary_execution_expected=$'derived\ninherited-object-slot-ran' ;;
        DEAD_PRIMITIVE_BRANCH) summary_execution_expected=dead-primitive-branch-ran ;;
        ABSTRACT_APPLICATION) summary_execution_expected=abstract-application-ran ;;
        ABSTRACT_LIBRARY) summary_execution_expected=abstract-library-ran ;;
        DEAD_NOMINAL_BRANCH) summary_execution_expected=dead-nominal-branch-ran ;;
        METHODIMPL_PRIORITY) summary_execution_expected=$'explicit-slot\nmethodimpl-priority-ran' ;;
        INHERITED_INTERFACE_MAPPING) summary_execution_expected=$'inherited-interface-ok\ninherited-interface-map-ran' ;;
        PROTECTED_INTERFACE_MAPPING) summary_execution_expected=$'protected-interface-ok\nprotected-interface-map-ran' ;;
        CLASS_NEWSLOT_MAPPING) summary_execution_expected=$'original-class-slot-ok\nclass-newslot-map-ran' ;;
        ISA_GETTER_BRANCH) summary_execution_expected=isa-getter-branch-ran; summary_execution_refs+=(System.Runtime.Intrinsics) ;;
        CONSTRAINED_PRIMITIVE) summary_execution_expected=$'True\n1\nconstrained-primitive-ran' ;;
        CONST_GETTER_BRANCH) summary_execution_expected=const-getter-branch-ran; summary_execution_refs+=(System.Diagnostics.Debug) ;;
        UNUSED_DEFAULT_INTERFACE) summary_execution_expected=$'7\nunused-default-interface-ran' ;;
    esac
    summary_prepared="$out/depth-summary-$summary_execution-input"
    mkdir -p "$summary_prepared/bin/$CONFIG/$TFM"
    cp "$summary_library" "$summary_prepared/bin/$CONFIG/$TFM/DepthData.dll"
    dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
        -c "$CONFIG" -p:DefineConstants="$summary_define" -p:Optimize=true -p:BuildProjectReferences=false \
        -o "$summary_prepared/bin/$CONFIG/$TFM" --nologo -v quiet
    (
        DN2CPP_SAMPLE_PROJECT_DIR="$summary_prepared"
        DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR|defines:$summary_define|Optimize:true|reference:DepthData"
        DN2CPP_OUT_SUFFIX="-execution-$summary_execution"
        DN2CPP_SKIP_BUILD=1
        case "$summary_execution" in
            ATTRIBUTE_READ|ATTRIBUTE_GENERIC_READ)
                gate_extra_asserts() {
                    local out="$1" axis output line
                    run_bounded "$out/ReflectionDepthSummary$EXE_EXT" > "$out/field-boxes.native.stdout"
                    run_bounded dotnet "$_CG_APP" > "$out/field-boxes.dotnet.stdout"
                    run_bounded "$out/ReflectionDepthSummary$EXE_EXT" before-reflected-field-boxes \
                        > "$out/field-boxes-before.native.stdout"
                    run_bounded dotnet "$_CG_APP" before-reflected-field-boxes \
                        > "$out/field-boxes-before.dotnet.stdout"
                    for axis in native dotnet; do
                        output=$(strip_cr_win_file "$out/field-boxes.$axis.stdout")
                        awk '/^== late reflected field boxes ==$/ { exit } { print }' \
                            <<< "$output" > "$out/field-boxes-prefix.$axis.stdout"
                        diff -u <(strip_cr_win_file "$out/field-boxes-before.$axis.stdout") \
                            <(strip_cr_win_file "$out/field-boxes-prefix.$axis.stdout")
                        assert_output "$(strip_cr_win_file "$out/field-boxes-prefix.$axis.stdout")" \
                            $'attribute-ctor\nattribute-setter\n7\nattribute-read-ran'
                        for line in '== late reflected field boxes ==' second 'field equals=True' \
                                'field hash=29' 'late reflected field boxes end'; do
                            [ "$(grep -Fxc -- "$line" <<< "$output")" = 1 ] \
                                || { echo "FAIL: reflected field box witness must run once ($axis): $line" >&2; return 1; }
                        done
                    done
                } ;;
        esac
        corelib_diff_gate ReflectionDepthSummary "${summary_execution_refs[@]}"
        case "$summary_execution" in
            ATTRIBUTE_READ|ATTRIBUTE_GENERIC_READ)
                DN2CPP_OUT_SUFFIX="-execution-$summary_execution-original" \
                    corelib_diff_gate ReflectionDepthSummary --no-ildiet "${summary_execution_refs[@]}"
                DN2CPP_OUT_SUFFIX="-execution-$summary_execution-unshared" \
                    corelib_diff_gate ReflectionDepthSummary --no-ildiet --no-shared-generics "${summary_execution_refs[@]}" ;;
        esac
    )
    summary_native=$(run_bounded "./artifacts/reflectiondepthsummary-execution-$summary_execution/ReflectionDepthSummary")
    assert_output "$(strip_cr_win "$summary_native")" "$summary_execution_expected"
    if grep -Eq '^[^;]* (BaseCallDerived_Run|BaseCallDerived_Descend|ObjectSlotParent_ToString|ObjectSlotParent_Descend|Program_DeadDescend|AbstractApplicationTarget_ToString|AbstractApplicationTarget_Descend|AbstractConstructionData_Reset|AbstractConstructionData_Descend|Program_DeadNominalDescend|Program_DeadGetterDescend|PrioritySlotReceiver_Run|PrioritySlotReceiver_Descend|PrimitiveSlotReceiver_CompareTo|PrimitiveSlotReceiver_Descend|IUnusedDefaultSlot_Run|IUnusedDefaultSlot_Descend|InheritedMappingDerived_Run|InheritedMappingDerived_Descend|ProtectedMappingDerived_Run|ProtectedMappingDerived_Descend|SeparateClassSlotDerived_Run|SeparateClassSlotDerived_Descend|Program_DeadIsaDescend)[^ ]*_m[0-9]+\(' \
            "artifacts/reflectiondepthsummary-execution-$summary_execution"/generated*.cpp; then
        echo "FAIL: $summary_execution emitted a nonexecuting dispatch/allocation/branch body" >&2
        exit 1
    fi
    case "$summary_execution" in
        UNREAD_ATTRIBUTE_CTOR|UNREAD_ATTRIBUTE_GETTER|ATTRIBUTE_READ|ATTRIBUTE_GENERIC_READ)
            if grep -Eq '^// .*::DepthAttributeRecurse' \
                    "artifacts/reflectiondepthsummary-execution-$summary_execution"/generated*.cpp; then
                echo "FAIL: $summary_execution emitted an unread attribute body or named getter/base setter" >&2
                exit 1
            fi ;;
    esac
    case "$summary_execution" in
        ATTRIBUTE_READ|ATTRIBUTE_GENERIC_READ)
            for attribute_cap in 3 4; do
                attribute_budget_out="$out/attribute-$summary_execution-depth$attribute_cap"
                attribute_budget_rc=0
                attribute_budget_err=$(export DN2CPP_MAX_GENERIC_DEPTH="$attribute_cap";
                    invoke_cli "$summary_prepared/bin/$CONFIG/$TFM/ReflectionDepthSummary.dll" \
                        -r "$corelib" -r "$summary_library" -o "$attribute_budget_out" 2>&1 >/dev/null) \
                    || attribute_budget_rc=$?
                [ "$attribute_cap" = 3 ] && attribute_budget_method=.ctor || attribute_budget_method=set_Level
                if [ "$attribute_budget_rc" -ne 2 ] \
                        || [ -f "$attribute_budget_out/ildiet/ReflectionDepthSummary.dll" ] \
                        || ! grep -Fq "ILDiet depth summary needs generic nesting depth $((attribute_cap + 1))" <<< "$attribute_budget_err" \
                        || ! grep -Fq "ReadDepthAttribute.$attribute_budget_method" <<< "$attribute_budget_err"; then
                    echo "FAIL: executable attribute $attribute_budget_method did not supply its required depth context" >&2
                    echo "$attribute_budget_err" >&2
                    exit 1
                fi
            done ;;
    esac
    echo "OK ($summary_execution: CLR parity; exact execution provenance excludes uncalled bodies)"
done

echo "== 4/9 The heap ceiling fires — in emit AND in --measure =="
# A program big enough that the model alone passes a small budget: the real
# CoreLib's reachable closure. 16 MB is far below anything a real transpile needs,
# so the guard is guaranteed to trip well before the run would have finished.
for mode in "" "--measure"; do
    label=${mode:-emit}
    cap_rc=0
    cap_err=$(invoke_cli "$big_app" -r "$corelib" --auto-ref --max-heap-mb 16 $mode -o "$out" 2>&1 >/dev/null) || cap_rc=$?
    if [ "$cap_rc" -ne 2 ]; then
        echo "FAIL: --max-heap-mb 16 ($label) exited $cap_rc (expected 2)" >&2
        echo "$cap_err" >&2
        exit 1
    fi
    if ! grep -q "exceeded its 16 MB heap budget during" <<<"$cap_err"; then
        echo "FAIL: the overrun ($label) does not name the budget and the phase:" >&2
        echo "$cap_err" >&2
        exit 1
    fi
    echo "OK ($label: failed at the budget, named the phase)"
done

# Off by default: the very same transpile, with no budget, must succeed. (This is
# what every other gate relies on, asserted once here explicitly.)
invoke_cli "$big_app" -r "$corelib" --auto-ref -o "$out" > /dev/null
[ -f "$out/generated.cpp" ] || { echo "FAIL: the unguarded transpile produced no output" >&2; exit 1; }
echo "OK (no budget by default)"

echo "== 5/9 A member nobody asks about must not be decoded =="
# The aggregate form of 1b, on both tiers. This program reaches a few hundred methods and holds
# tens of thousands of MethodInfos and thousands of FieldInfos — one per member row of every
# loaded assembly — and decoding either a signature or a field type is the expensive half, as
# well as what mints the closed generics it names. Nearly all of them are never asked about, and
# the rate is deliberately NOT quoted here: it is what the census below prints on every run, and
# quoting it is how it went stale twice (measured 2026-07-30 at nearly double the 5% this
# comment and AGENTS.md had both been restating for the field types).
#
# The ceilings are loose deliberately. The BCL drifts, and the regression worth catching — a
# walk over every class that reads every member — lands near 100%, not near the measured rate.
# What this pins is the SHAPE of the model's cost: it must follow what the program reaches, not
# what -r was pointed at. And it is the only check in the suite that can catch that regression
# at all, because such a walk changes no output: every other gate would stay green while the
# saving quietly went away.
# The census prints to stderr, once per phase; take the last report (@emit — the whole run).
census=$(export DN2CPP_MODEL_CENSUS=1; invoke_cli "$arr_app" -r "$corelib" -r "$(dirname "$corelib")/System.Collections.dll" -o "$out" 2>&1 >/dev/null)
check_decode_rate() { # <label> <census-line-pattern> <ceiling> <what-is-read>
    local line pct
    line=$(grep "$2" <<<"$census" | tail -1)
    pct=$(printf '%s' "$line" | LC_ALL=C sed -n 's/.*(\([0-9][0-9]*\)%).*/\1/p')
    if [ -z "$pct" ]; then
        echo "FAIL: DN2CPP_MODEL_CENSUS=1 reported no $1 census — the instrument is gone" >&2
        exit 1
    fi
    if [ "$pct" -gt "$3" ]; then
        echo "FAIL: $pct% of the held $1 were decoded (ceiling $3%). Something is reading" >&2
        echo "      $4 across the whole model rather than the set the program reaches:" >&2
        echo "      $line" >&2
        exit 1
    fi
    echo "OK ($pct% of held $1 decoded — nobody asked about the rest)"
}
check_decode_rate "method signatures" "signatures DECODED"  40 "signatures"
check_decode_rate "field types"       "field types DECODED" 25 "field types"

echo "== 6/9 --cut: a named method's subtree falls out; call sites yield the default =="
# The generic carve-out lever (the TaskTracker-shaped cut, without baking library names
# into dn2cpp — same semantics as the backend bounded sets, per run). The step-3 program
# transpiles again with Tracker.Tracked cut: its body AND the Helper subtree only it
# reaches must be gone from the C++, the neutralized call site yields the default (null,
# printed "cut"), and the rest of the program is untouched. A spec resolving to nothing is
# a hard error — asserted for a missing method, a missing type, and a malformed spec (a
# typo silently becoming a no-op cut is a footgun).
invoke_cli "$sig_app" -r "$corelib" --cut "GenericSignatureRecursionBad.Tracker::Tracked" -o "$cut_out" >/dev/null
for sym in Tracked Helper; do
    if grep -q "^// GenericSignatureRecursionBad.Tracker::${sym}$" "$cut_out"/generated*; then
        echo "FAIL: --cut left Tracker.$sym in the generated C++" >&2
        exit 1
    fi
done
compile_console "$cut_out" GenericSignatureRecursionBad
# Literal expected text -> strip the native side's \r (the strip_cr_win rule
# for literal asserts; never applied to a live-oracle diff). Exit status
# captured explicitly (`$(...)` inline would swallow it).
set +e
cut_native=$("./$cut_out/GenericSignatureRecursionBad"); cut_code=$?
set -e
assert_output "$(strip_cr_win "$cut_native")" "$(printf '1\ncut')"
assert_exit_code "$cut_code" 0

# A finite CLR body still has an unbounded generic call graph. Cutting its
# nongeneric wrapper must exclude that body from the companion's depth summary.
summary_cut="$out/depth-summary-cut-input"
mkdir -p "$summary_cut/bin/$CONFIG/$TFM"
cp "$summary_library" "$summary_cut/bin/$CONFIG/$TFM/DepthData.dll"
dotnet build gates/fixtures/transpiler-limits/ReflectionDepthSummary/ReflectionDepthSummary.csproj \
    -c "$CONFIG" -p:DefineConstants=ORDINARY_CUT_DEPTH -p:BuildProjectReferences=false \
    -o "$summary_cut/bin/$CONFIG/$TFM" --nologo -v quiet
for summary_cut_mode in ildiet no-ildiet; do
    (
        DN2CPP_SAMPLE_PROJECT_DIR="$summary_cut"
        DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|defines:ORDINARY_CUT_DEPTH|cut:ReflectionDepthSummary.Program::Cut"
        DN2CPP_SKIP_BUILD=1
        DN2CPP_OUT_SUFFIX="-ordinary-cut-$summary_cut_mode"
        gate_extra_asserts() {
            local out="$1" native prefix
            native=$(run_bounded "$out/ReflectionDepthSummary$EXE_EXT") || return $?
            native=$(strip_cr_win "$native")
            prefix=$(awk '/^== ordinary cut depth summary ==$/ { exit } { print }' <<< "$native")
            assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$summary_expected")"
            grep -Fxq 'cut-complete' <<< "$native" \
                || { echo 'FAIL: ordinary cut depth section did not run' >&2; return 1; }
            if grep -Eq '^// ReflectionDepthSummary.Program::(Cut|Descend)$' "$out"/generated*.cpp; then
                echo 'FAIL: a cut body or its generic subtree was emitted' >&2
                return 1
            fi
        }
        if [ "$summary_cut_mode" = ildiet ]; then
            corelib_diff_gate ReflectionDepthSummary --cut ReflectionDepthSummary.Program::Cut -r "$summary_library"
        else
            corelib_diff_gate ReflectionDepthSummary --no-ildiet --cut ReflectionDepthSummary.Program::Cut -r "$summary_library"
        fi
    )
done
for bypass in "--no-ildiet" ""; do
    invoke_cli "$sig_app" -r "$corelib" $bypass --cut 'CutNested::Unused' -o "$cut_out" >/dev/null
    for generic in 'GenericSignatureRecursionBad.CutBase_Int32::Value' \
            'GenericSignatureRecursionBad.ICutInterface_Int32::Identity'; do
        generic_log=$(invoke_cli "$sig_app" -r "$corelib" $bypass --cut "$generic" \
            --cut 'CutNested::Unused' --cut 'GenericSignatureRecursionBad.Tracker::Tracked' -o "$cut_out")
        if [ -z "$bypass" ]; then
            grep -q 'copying all assemblies for post-model validation of generic --cut selectors' <<<"$generic_log" \
                || { echo "FAIL: generic --cut did not explain its intact-copy validation" >&2; exit 1; }
            cmp "$sig_app" "$cut_out/ildiet/GenericSignatureRecursionBad.dll"
        fi
        if grep -q '^// GenericSignatureRecursionBad.Tracker::Tracked$' "$cut_out"/generated*; then
            echo "FAIL: a generic --cut disabled a simultaneous ordinary cut" >&2
            exit 1
        fi
    done
    for bad in 'GenericSignatureRecursionBad.Box::Deeper' 'GenericSignatureRecursionBad.Box`1::Deeper' \
            'GenericSignatureRecursionBad.Box_Int32::Deeper' 'CutNested::GenericUnused' \
            'GenericSignatureRecursionBad.CutBase_Nonexistent::Value' \
            'GenericSignatureRecursionBad.Tracker+CutNested::Unused'; do
        bad_rc=0
        rm -rf "${cut_out}-invalid"
        bad_err=$(invoke_cli "$sig_app" -r "$corelib" $bypass --cut "$bad" \
            --cut 'CutNested::Unused' -o "${cut_out}-invalid" 2>&1 >/dev/null) || bad_rc=$?
        if [ "$bad_rc" -eq 0 ] || ! grep -q -- "--cut" <<<"$bad_err"; then
            echo "FAIL: --cut selector $bad ($bypass) did not fail loudly (exit $bad_rc):" >&2
            echo "$bad_err" >&2
            exit 1
        fi
        [ ! -f "${cut_out}-invalid/generated.cpp" ] \
            || { echo "FAIL: invalid --cut emitted C++ before failing" >&2; exit 1; }
    done
done
for bad in "GenericSignatureRecursionBad.Tracker::Nope" "No.Such.Type::Tracked" "MissingSeparator"; do
    bad_rc=0
    bad_err=$(invoke_cli "$sig_app" -r "$corelib" --cut "$bad" -o "$cut_out" 2>&1 >/dev/null) || bad_rc=$?
    if [ "$bad_rc" -eq 0 ] || ! grep -q -- "--cut" <<<"$bad_err"; then
        echo "FAIL: bogus spec --cut $bad did not fail loudly (exit $bad_rc):" >&2
        echo "$bad_err" >&2
        exit 1
    fi
done
echo "OK (--cut: subtree gone, default at the call site, bogus specs loud)"

echo "== 7/9 --measure runs the dangling-symbol sweep; a clean corpus has zero rows =="
# The widest sweep of the cut => route invariant: --measure diffs every
# body-named method symbol against the compiled bodies UNION the dropped (gap-row)
# bodies, and a survivor becomes a `dangling` gap row — the class of defect that is
# otherwise visible only where some gate happens to link. Asserted on a corpus the
# suite proves clean (section 4's unguarded arm transpiles this very input to C++,
# and build-and-run-string-core links it): the sweep line must be PRESENT with a
# NONZERO named count — the proof the sweep ran, since a silently skipped sweep and
# a clean corpus would otherwise both say zero — and the dangling count must be 0,
# with no `dangling` row in the TSV. A genuine violation fixture would need a real
# cut-without-route bug baked into the transpiler, so the row format itself is
# asserted at zero here and exercised for real only when a sweep finds one.
meas_out=$(invoke_cli "$big_app" -r "$corelib" --auto-ref --measure -o "$out")
sweep=$(grep "dangling-symbol sweep:" <<<"$meas_out" || true)
if [ -z "$sweep" ]; then
    echo "FAIL: --measure printed no dangling-symbol sweep line — the sweep is gone:" >&2
    echo "$meas_out" >&2
    exit 1
fi
if ! grep -Eq "sweep: [1-9][0-9]* named symbols diffed, 0 dangling" <<<"$sweep"; then
    echo "FAIL: the sweep did not diff a nonzero named set to zero dangling rows:" >&2
    echo "$sweep" >&2
    exit 1
fi
# The file must EXIST before its rows mean anything: grep on an absent path
# exits 2, the `if` reads false, and the row-format assertion evaporates with
# an OK line — one level down from the same "a skipped sweep and a clean corpus
# both say zero" failure this section's comment describes.
[ -f "$out/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure wrote no $out/s0-gaps.tsv — the dangling-row assertion below would read nothing" >&2; exit 1; }
if grep -q "^dangling" "$out/s0-gaps.tsv"; then
    echo "FAIL: the per-gap TSV carries dangling rows on a clean corpus:" >&2
    grep "^dangling" "$out/s0-gaps.tsv" >&2
    exit 1
fi
echo "OK ($sweep)"
echo "== 8/9 A typeof naming an unloaded assembly's type must REFUSE, not fold to null =="
# `typeof(System.Numerics.Complex)` with System.Runtime.Numerics absent fails OPEN
# without this: the TypeRef degrades to an External, TypeInfoExprOf has no arm for
# it, and the ldtoken folds to a literal `nullptr`. Nothing downstream could
# see it — the fold names no symbol, so the C++ compiled and LINKED (unlike every
# other External use, which CppTypes.Of rejects loudly), and the shipped binary
# answered a NULL Type. That is the failure mode the --reflection-root typo hard
# error exists to prevent, one level down: a diagnostic that only ever fires in a
# customer's game reaches nobody.
#
# Both directions are asserted, and the second is what keeps the first honest — a
# refusal that fired on every typeof would pass the negative arm alone.
tma_rc=0
tma_err=$(invoke_cli "$tma_app" -r "$corelib" -o "$out" 2>&1 >/dev/null) || tma_rc=$?
if [ "$tma_rc" -ne 2 ]; then
    echo "FAIL: a typeof naming an unloaded assembly's type exited $tma_rc (expected 2)" >&2
    echo "$tma_err" >&2
    exit 1
fi
# The type AND the assembly to pass: the type alone would not tell the caller what
# to add to the command line, which is the whole content of the diagnostic.
if ! grep -q "System.Numerics.Complex" <<<"$tma_err"; then
    echo "FAIL: the refusal does not name the type:" >&2
    echo "$tma_err" >&2
    exit 1
fi
if ! grep -q "System.Runtime.Numerics" <<<"$tma_err"; then
    echo "FAIL: the refusal does not name the assembly to pass with -r:" >&2
    echo "$tma_err" >&2
    exit 1
fi
# Supplied, the same program must transpile — and the emitted token must be a real
# type-info, not the nullptr the refusal replaced.
invoke_cli "$tma_app" -r "$corelib" -r "$numerics_dll" -o "$out" >/dev/null
grep -q "ti_System_Numerics_Complex" "$out"/generated*.cpp "$out"/generated.h \
    || { echo "FAIL: with the reference supplied, typeof(Complex) named no type-info" >&2; exit 1; }
echo "OK (refused without the reference naming both, transpiled with it)"

echo "== 9/9 The reflection-invoke route walks a bounded number of a self-nesting definition's instantiations =="
# Nest<T>'s methods each name a deeper Nest, and a deep framework instantiation sets the
# nesting the route walks within, so a whole walk would mint Nest instantiations
# exponentially in that depth. The ceiling admits the per-definition bound's walks with
# room to spare and stays a small fraction of a whole walk's count.
nest_rc=0
nest_err=$(invoke_cli "$nest_app" -r "$corelib" -o "$out" 2>&1 >/dev/null) || nest_rc=$?
if [ "$nest_rc" -ne 0 ]; then
    echo "FAIL: the self-nesting reflection-route transpile exited $nest_rc" >&2
    echo "$nest_err" >&2
    exit 1
fi
# The invoked row's body is the walk's own witness: without it the count proves nothing.
grep -qx '// ReflectionRouteNesting.Nest_Int32::Name' "$out"/generated*.cpp \
    || { echo "FAIL: the reflection route did not reach Nest<int>.Name" >&2; exit 1; }
nest_count=$(cat "$out"/generated*.cpp | grep -o -E 'ti_ReflectionRouteNesting_Nest_[A-Za-z0-9_]+ = ' | sort -u | wc -l | tr -d ' ')
if [ "$nest_count" -gt 2000 ]; then
    echo "FAIL: the reflection route emitted $nest_count Nest instantiations (ceiling 2000)" >&2
    exit 1
fi
echo "OK ($nest_count Nest instantiations, Nest<int>.Name reached)"

echo "== CoreLib type identity collisions =="
for shape in Object ValueType Unsafe; do
    collision_app="$collision_fixture/$shape/bin/$CONFIG/$TFM/Collision$shape.dll"
    collision_oracle=$(run_bounded dotnet "$collision_app")
    case "$shape" in
        Object) collision_name=System.Object; collision_expected='application Object: 17' ;;
        ValueType) collision_name=System.ValueType; collision_expected='application ValueType: ValueType' ;;
        Unsafe) collision_name=System.Runtime.CompilerServices.Unsafe; collision_expected='application Unsafe: 123' ;;
    esac
    assert_output "$(strip_cr_win "$collision_oracle")" "$collision_expected"
    for axis in corelib intrinsic; do
        collision_refs=()
        [ "$axis" != corelib ] || collision_refs=(-r "$corelib")
        collision_out="${out}-collision-$shape-$axis"
        rm -rf "$collision_out"
        collision_rc=0
        collision_err=$(run_with_watchdog 30 invoke_cli "$collision_app" \
            ${collision_refs[@]+"${collision_refs[@]}"} --no-ildiet -o "$collision_out" 2>&1 >/dev/null) \
            || collision_rc=$?
        if [ "$collision_rc" -ne 2 ] \
                || ! grep -Fq "input type '$collision_name' in assembly 'Collision$shape' conflicts with a CoreLib/runtime type identity" <<<"$collision_err"; then
            echo "FAIL: $shape collision ($axis) was not rejected with its type and assembly (exit $collision_rc):" >&2
            echo "$collision_err" >&2
            exit 1
        fi
        [ ! -f "$collision_out/generated.cpp" ] \
            || { echo "FAIL: $shape collision emitted C++ before failing" >&2; exit 1; }
    done
done
control_app="$collision_fixture/Control/bin/$CONFIG/$TFM/CollisionControl.dll"
control_oracle=$(run_bounded dotnet "$control_app")
assert_output "$(strip_cr_win "$control_oracle")" "$(printf 'embedded attribute: 5\nordinary body: control-body\nordinary input end')"
for axis in corelib intrinsic; do
    collision_refs=()
    [ "$axis" != corelib ] || collision_refs=(-r "$corelib")
    control_out="${out}-collision-control-$axis"
    invoke_cli "$control_app" ${collision_refs[@]+"${collision_refs[@]}"} --no-ildiet -o "$control_out" >/dev/null
    compile_console "$control_out" CollisionControl
    control_native=$(run_bounded "./$control_out/CollisionControl")
    assert_output "$(strip_cr_win "$control_native")" "$(strip_cr_win "$control_oracle")"
done
echo "OK (CoreLib identities rejected; embedded metadata, helper polyfill and ordinary input preserved)"

gate_cache_commit
