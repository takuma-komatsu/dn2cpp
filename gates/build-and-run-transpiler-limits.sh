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
#      Methods are decoded on demand, so nothing reaches Deeper, nothing reads its
#      signature, and the deeper Box is never named. The bound has nothing to
#      catch — so this asserts that the transpile COMPLETES, and that it stopped at
#      one step. That is the suite's proof that the deferral is real: a regression
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
# A sibling measurement aid, gates/measure-transpile-mem.sh, reports peak RSS and
# the per-phase heap curve. This gate asserts; that one measures.
source "$(dirname "$0")/_common.sh"

out="artifacts/transpiler-limits"
sig_out="artifacts/transpiler-limits-sig"
cut_out="artifacts/transpiler-limits-cut"
mint_out="artifacts/transpiler-limits-mint"

echo "== 1/8 Locating the real CoreLib, building the sample assemblies =="
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
rec_app="samples/dotnet/GenericRecursionBad/bin/$CONFIG/$TFM/GenericRecursionBad.dll"
sig_app="samples/dotnet/GenericSignatureRecursionBad/bin/$CONFIG/$TFM/GenericSignatureRecursionBad.dll"
fld_app="samples/dotnet/GenericFieldRecursionBad/bin/$CONFIG/$TFM/GenericFieldRecursionBad.dll"
afld_app="samples/dotnet/GenericArrayFieldRecursionBad/bin/$CONFIG/$TFM/GenericArrayFieldRecursionBad.dll"
big_app="samples/dotnet/StringCore/bin/$CONFIG/$TFM/StringCore.dll"
arr_app="samples/dotnet/ArrayCore/bin/$CONFIG/$TFM/ArrayCore.dll"
mint_app="samples/dotnet/SharedTrialMint/bin/$CONFIG/$TFM/SharedTrialMint.dll"
tma_app="samples/dotnet/TypeofMissingAsmBad/bin/$CONFIG/$TFM/TypeofMissingAsmBad.dll"
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
# leave this gate uncacheable since it clears the dirs on every run. The
# transpiler-behavior env axis rides in
# the context for the same reason: this gate asserts the very knobs an ambient
# export would move (an ambient DN2CPP_MAX_INSTANTIATIONS or MAX_HEAP_MB fails
# transpiles this gate expects to complete), and there is no surface in the key
# to catch that.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
rm -rf "$out" "$sig_out" "$cut_out" "$mint_out"; mkdir -p "$out"
if gate_cache_check "$out" "transpiler-limits|cli:$(_gate_cli_hash)|$corelib|$tenv" \
        "$rec_app" "$sig_app" "$fld_app" "$afld_app" "$big_app" "$arr_app" "$mint_app" "$tma_app" \
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
echo "== 2/8 A self-deepening generic must hit the monomorphization bound =="
rec_rc=0
rec_err=$(invoke_cli "$rec_app" -r "$corelib" -o "$out" 2>&1 >/dev/null) || rec_rc=$?
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
deep_err=$(export DN2CPP_MAX_GENERIC_DEPTH=40; invoke_cli "$rec_app" -r "$corelib" -o "$out" 2>&1 >/dev/null) || deep_rc=$?
if [ "$deep_rc" -ne 2 ] || ! grep -q "nested 41 deep" <<<"$deep_err"; then
    echo "FAIL: DN2CPP_MAX_GENERIC_DEPTH=40 did not move the bound to 41 (exit $deep_rc)" >&2
    echo "$deep_err" >&2
    exit 1
fi
echo "OK (rejected at the bound, names its lever, and the lever moves it)"

echo "== 3/8 A self-deepening METHOD signature must simply not recurse =="
# This is the self-referential-signature runaway's shape —
# GDTask<T>.SuppressCancellationThrow() ->
# GDTask<(bool, T)> — which an eager member decode could only REFUSE at the depth bound.
# Members are decoded on demand (Compilation.CompleteMembers), and
# nothing calls Box<T>.Deeper(), so its signature is never read and the deeper Box is never
# named. The recursion has no step to take.
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
    sig_err=$(invoke_cli "$sig_app" -r "$corelib" $mode -o "$sig_out" 2>&1 >/dev/null) || sig_rc=$?
    if [ "$sig_rc" -ne 0 ]; then
        echo "FAIL: the self-deepening METHOD shape ($label) exited $sig_rc — nothing calls Deeper(), so nothing" >&2
        echo "      should decode its signature and the transpile should complete:" >&2
        echo "$sig_err" >&2
        exit 1
    fi
    echo "OK ($label: completed — an unreached member's signature is never decoded)"
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

echo "== 3b/8 A self-deepening FIELD must still hit the bound, and name the member =="
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

echo "== 3c/8 A self-deepening FIELD via an ARRAY wrapper must hit the bound too =="
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

echo "== 3d/8 The bound must ESCAPE the emit side's swallowing arms =="
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
     invoke_cli "$mint_app" -r "$corelib" -o "$mint_out") >"$mint_so" 2>"$mint_se"
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

echo "== 4/8 The heap ceiling fires — in emit AND in --measure =="
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

echo "== 5/8 A member nobody asks about must not be decoded =="
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
census=$(export DN2CPP_MODEL_CENSUS=1; invoke_cli "$arr_app" -r "$corelib" -o "$out" 2>&1 >/dev/null)
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

echo "== 6/8 --cut: a named method's subtree falls out; call sites yield the default =="
# The generic carve-out lever (the TaskTracker-shaped cut, without baking library names
# into dn2cpp — same semantics as the backend bounded sets, per run). The step-3 program
# transpiles again with Tracker.Tracked cut: its body AND the Helper subtree only it
# reaches must be gone from the C++, the neutralized call site yields the default (null,
# printed "cut"), and the rest of the program is untouched. A spec resolving to nothing is
# a hard error — asserted for a missing method, a missing type, and a malformed spec (a
# typo silently becoming a no-op cut is a footgun).
invoke_cli "$sig_app" -r "$corelib" --cut "GenericSignatureRecursionBad.Tracker::Tracked" -o "$cut_out" >/dev/null
for sym in Tracked Helper; do
    if grep -q "m_GenericSignatureRecursionBad_Tracker_${sym}" "$cut_out"/generated*; then
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

echo "== 7/8 --measure runs the dangling-symbol sweep; a clean corpus has zero rows =="
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
echo "== 8/8 A typeof naming an unloaded assembly's type must REFUSE, not fold to null =="
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

gate_cache_commit
