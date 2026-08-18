#!/usr/bin/env bash
# Third-party custom async TASK TYPES — the GDTask / UniTask class of library.
#
# A task-like type carries [AsyncMethodBuilder(typeof(TheBuilder))] and ships its own builder,
# its own awaiter, and a promise machinery to back them. dn2cpp ADOPTS all three into its
# intrinsic Task family — the task type lowers to the ValueTask/Task runtime struct, the
# builder to Dn2CppAsyncBuilder, the awaiter to Dn2CppTaskAwaiter — so the library's IL is
# never transpiled at all. That is sound rather than lucky: the C# compiler REQUIRES a builder
# to expose Create / Start<TSM> / SetStateMachine / SetResult / SetException / get_Task /
# AwaitUnsafeOnCompleted, and the await pattern requires GetAwaiter / get_IsCompleted /
# GetResult / OnCompleted, so an adopted type's member names ARE the BCL's.
#
# The sample is a hand-rolled library in that mould (no NuGet): both arities, nested awaiters,
# the CRTP object pool the ticket fingered (TaskPool<T> where T : class, ITaskPoolNode<T>,
# closed by Runner<TStateMachine> : ITaskPoolNode<Runner<TStateMachine>>), a
# ConcurrentDictionary scheduler, a default-interface-method source bridge, and a set of
# internal async methods for reachability to sweep — plus a driver that awaits it, and a
# SECOND custom task type declared in the app assembly itself.
#
# That last one is not decoration. A MethodSpec's parent resolves down a different arm for a
# MethodDef (same assembly) than for a MemberRef/TypeRef (cross assembly), and the builder's
# Start<TSM> arrives as a MethodSpec — so a fix that handled only one arm would pass a gate
# that only tested the other. (It did: the first cut of the adoption resolved the MethodDef
# arm through Module.MethodMap, which never holds a generic method's template, and every
# within-assembly Start<TSM> silently fell back to the general pipeline. Assertion 2 caught
# it.)
#
# Three things are asserted, off ONE transpile:
#
#   1. CORRECTNESS. Exact diff vs real .NET. A correct adoption SHOULD match, so the program
#      is its own oracle. Every await is sequential: completion order must be a property of
#      the program, never of the clock (the async-combinators gate's live oracle already
#      flakes on a loaded machine, and this one must not be able to).
#
#   2. THE LIBRARY'S IL IS GONE. Its pool / runner / promise / scheduler symbols must not
#      appear in the generated C++ at all, and the app-declared promise must have no emitted
#      body. This is the direct assertion of the fix, and it is free and deterministic.
#
#   3. IT STAYS BOUNDED. Pre-fix this input exits 2 FAST (the builder's constrained
#      IAsyncStateMachine::MoveNext has no lowering), so it never reproduced the
#      instantiation runaway and assertion 3 cannot catch a resurrected one. What it
#      catches is a WRONG fix — one
#      that makes the sample transpile while still dragging the library's closure in. The
#      reproduction of the runaway itself is the depth bound, asserted over the real shape in
#      build-and-run-transpiler-limits.sh (GenericSignatureRecursionBad), and against the real
#      GDTask by hand.
#
# Then the SAME sample is transpiled a SECOND time with --no-adopt-async CustomAsyncTaskLib:
# the library's real IL — builder, Runner<TSM>, TaskPool, promises, the
# ConcurrentDictionary scheduler — goes through the general pipeline instead of being
# adopted. The mirror-image assertions ride that transpile:
#
#   4. THE LIBRARY'S IL IS IN. The same symbols assertion 2 forbids must now appear (proof
#      the opt-out really re-routed the assembly), while the APP-declared task type — not
#      named by the opt-out — must still be adopted (the lever is per-assembly).
#
#   5. IT IS CORRECT. The un-adopted native binary must exact-diff real .NET too. This is
#      the real-IL-builder end-to-end proof: Start<TSM> / AwaitUnsafeOnCompleted through a
#      generic-type generic method, constrained IAsyncStateMachine dispatch on generic state
#      machines, and the ValueTask/Yield/Configured awaiter OnCompleted(Action) forms.
#
#   6. IT STAYS BOUNDED WITHOUT ADOPTION. Demand-driven decode — a specialization's
#      members, and a method's signature, are decoded only when something asks —
#      is what makes the un-adopted closure terminate at all (the GDTask shape
#      ran away through the eager member-signature decode that preceded it); the cap on
#      this transpile is the regression tripwire for that, riding the same transpile the
#      diff uses. The printed adopted-vs-real instantiation ratio is the size argument for
#      keeping adoption the default.
#
# And a THIRD program (samples/dotnet/CustomAsyncTaskTypeExceed) EXCEEDS the contract with NO
# flags: it awaits CustomTask.Yield(), a static task-type member with no BCL counterpart —
# UniTask.Yield's shape, the very call that motivated the auto-decline. Adoption is decided
# per PROGRAM, which is why this is its own bucket rather than a section above: one
# out-of-contract MemberRef would un-adopt the in-contract program's library too.
#
#   7. THE ADOPTION AUTO-DECLINES. The cross-assembly MemberRef contract pre-scan
#      (Compilation.DeclineOutOfContractAdoptions) must decline CustomAsyncTaskLib on its
#      own — the stderr notice names the offending member — and the library's real
#      machinery must be in the tree with nothing on the command line. (--no-adopt-async
#      stays covered above as the manual override for the pre-scan's blind spot:
#      same-assembly out-of-contract calls, which have no MemberRef row.)
#
#   8. IT IS CORRECT. The auto-declined native binary must exact-diff real .NET too.
source "$(dirname "$0")/_common.sh"

project=CustomAsyncTaskType
project_exceed=CustomAsyncTaskTypeExceed
out="artifacts/customasynctask"

echo "== 1/10 Locating the real CoreLib =="
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"

echo "== 2/10 Building the drivers + the custom-async-task library =="
build_proj "samples/dotnet/$project/$project.csproj"
build_proj "samples/dotnet/$project_exceed/$project_exceed.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
app_exceed="samples/dotnet/$project_exceed/bin/$CONFIG/$TFM/$project_exceed.dll"
lib="samples/dotnet/$project/bin/$CONFIG/$TFM/CustomAsyncTaskLib.dll"
[ -f "$lib" ] || { echo "FAIL: the library was not built beside the app: $lib" >&2; exit 1; }

echo "== 3/10 Transpiling app + library + real CoreLib, under the generic-instantiation bounds =="
# The library must be passed explicitly: --auto-ref is scoped to the shared-framework
# directory and never pulls a user assembly in.
#
# Both caps ride the ONE transpile the diff uses, so the diffed output cannot have come from
# an unbounded run. A cap can only turn a run into an abort or back — it never perturbs the
# output of a transpile that succeeds (AGENTS.md) — so the generated bytes, and the gate cache
# key over them, are unaffected.
#
#   DN2CPP_MAX_INSTANTIATIONS is the sharper of the two: _instances + _methodInstances is a
#   deterministic function of the input, where the heap number counts uncollected garbage and
#   drifts with the GC. Measured 3,662 here (inst 3,649 + minst 13); the cap is ~11x that, so
#   BCL drift across .NET patch releases cannot false-fail it, while a library closure dragged
#   back in (this one alone is worth thousands per state machine) trips it at once. For scale:
#   the whole selfhost corpus — the CLI plus the entire BCL under --auto-ref — creates ~20,000,
#   and the default cap is 1,000,000.
#
#   --max-heap-mb is the belt, for a blowup that is not instantiation-count-shaped. Measured
#   peak 70 MB; the cap is ~3.7x.
#
# (export inside the subshell: invoke_cli is a shell function, and bash does not reliably scope
# `VAR=x func` — same reason as build-and-run-transpiler-limits.sh.)
refs=(-r "$corelib" -r "$lib" -r "$bcl/System.Collections.Concurrent.dll")
# DN2CPP_TIME=1 rides both transpiles for the instantiation counters (stderr-only
# instrumentation: it cannot perturb the generated bytes or the cache key).
rc=0
err=$( export DN2CPP_MAX_INSTANTIATIONS=40000 DN2CPP_TIME=1
       invoke_cli "$app" "${refs[@]}" --max-heap-mb 256 -o "$out" 2>&1 >/dev/null ) || rc=$?
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the transpile no longer fits in 40,000 instantiations / 256 MB (exit $rc)." >&2
    echo "      A custom async task type is being monomorphized through the general generic" >&2
    echo "      pipeline again instead of adopted into the intrinsic Task family." >&2
    echo "$err" >&2
    exit 1
fi
echo "OK (bounded: <=40,000 instantiations, <=256 MB heap)"

echo "== 4/10 Transpiling AGAIN, un-adopted (--no-adopt-async: the library's real IL) =="
# The library's whole promise/pool/scheduler closure now transpiles, and it costs
# strikingly little: 1,386 instantiations against the adopted transpile's 1,106 — the
# closure is IN the tree, and it is only a quarter bigger, because demand-driven decode
# never mints the un-called signature closure that used to dwarf it.
# The cap is deliberately far above that, because what it is a tripwire FOR is not drift
# but a resurrected signature-driven runaway (the GDTask shape:
# GDTask<T>.SuppressCancellationThrow() -> GDTask<(bool,T)> -> ...), which does not
# overshoot by a factor — it does not stop. It rides the same transpile the diff below
# uses. Measured peak heap 71 MB; the belt is ~3.6x.
rc=0
err_real=$( export DN2CPP_MAX_INSTANTIATIONS=12000 DN2CPP_TIME=1
       invoke_cli "$app" "${refs[@]}" --no-adopt-async CustomAsyncTaskLib \
           --max-heap-mb 256 -o "$out-real" 2>&1 >/dev/null ) || rc=$?
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the UN-adopted transpile no longer fits in 12,000 instantiations / 256 MB (exit $rc)." >&2
    echo "      Either the library's real-IL path regressed, or the signature-recursion" >&2
    echo "      runaway demand-driven decode removed is back (something now walks every" >&2
    echo "      class and reads members or signatures it does not need)." >&2
    echo "$err_real" >&2
    exit 1
fi
# The adoption lever's size argument, from the counters both transpiles printed.
adopted_n=$(printf '%s\n' "$err" | grep -oE 'inst [0-9]+ minst [0-9]+' | tail -1 \
    | awk '{print $2 + $4}')
real_n=$(printf '%s\n' "$err_real" | grep -oE 'inst [0-9]+ minst [0-9]+' | tail -1 \
    | awk '{print $2 + $4}')
echo "OK (bounded: <=12,000 instantiations; adopted $adopted_n vs un-adopted $real_n instantiations)"

echo "== 5/10 Transpiling the EXCEEDING program, NO flags (the adoption must auto-decline) =="
# The same caps as the un-adopted arm: the library's real closure is what transpiles here
# too, only routed by the pre-scan instead of the flag.
rc=0
err_exceed=$( export DN2CPP_MAX_INSTANTIATIONS=12000
       invoke_cli "$app_exceed" "${refs[@]}" --max-heap-mb 256 \
           -o "$out-exceed" 2>&1 >/dev/null ) || rc=$?
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the out-of-contract program no longer transpiles with no flags (exit $rc)." >&2
    echo "      CustomTask.Yield() is a cross-assembly MemberRef outside the adoption" >&2
    echo "      contract, so the pre-scan must auto-decline the library's adoption and" >&2
    echo "      transpile its real IL — an emit-time 'no intrinsic mapping' here means" >&2
    echo "      the pre-scan missed the reference." >&2
    echo "$err_exceed" >&2
    exit 1
fi
# The notice is the decline's only surface in a SUCCEEDING transpile, and stderr-only by
# contract (the generated bytes, and the cache key over them, must not move).
if ! grep -q "adoption declined: CustomAsyncTaskLib.CustomTask::Yield" <<<"$err_exceed"; then
    echo "FAIL: the transpile succeeded but never printed the adoption-declined notice" >&2
    echo "      naming CustomAsyncTaskLib.CustomTask::Yield. Either the decline did not" >&2
    echo "      fire (the run may be adopted and miscompiled) or the notice regressed." >&2
    printf '%s\n' "$err_exceed" >&2
    exit 1
fi
echo "OK (declined: the notice names CustomTask::Yield; bounded: <=12,000 instantiations)"

# One cache key covers ALL THREE transpiles: gate_cache_check hashes $out's generated
# surface itself, and the un-adopted + auto-declined surfaces fold in through the context
# string (hashed the same way, before any build products land in the directories).
real_surface=$( (cd "$out-real" && ls -1 | LC_ALL=C sort | grep -E '^generated' \
    | tr '\n' '\0' | xargs -0 shasum -a 256) | shasum -a 256 | awk '{print $1}')
exceed_surface=$( (cd "$out-exceed" && ls -1 | LC_ALL=C sort | grep -E '^generated' \
    | tr '\n' '\0' | xargs -0 shasum -a 256) | shasum -a 256 | awk '{print $1}')
if gate_cache_check "$out" \
        "custom-async-task|$corelib|real:$real_surface|exceed:$exceed_surface" \
        "$app" "$app_exceed" "$lib" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        "${app_exceed%.dll}.runtimeconfig.json" "${app_exceed%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 6/10 Adopted: the library's async plumbing must not be in the tree =="
# Adopted, the task type / builder / awaiter lower to runtime structs, so the pool + runner +
# promise + scheduler hanging off them are never reached and tree-shaking drops them whole.
# The compiler-generated state machines of the library's async METHODS are ordinary user code
# and DO appear — only the plumbing must not.
for sym in TaskPool Runner ITaskPoolNode ICustomTaskSource IStateMachineRunnerPromise \
           AsyncCustomTaskMethodBuilder CustomTaskScheduler FaultedSource CompletedSource; do
    if grep -qh "CustomAsyncTaskLib_${sym}" "$out"/generated*.cpp "$out/generated.h"; then
        echo "FAIL: the library's $sym is being transpiled — the custom task type was not adopted:" >&2
        grep -h -m 3 "CustomAsyncTaskLib_${sym}" "$out"/generated*.cpp >&2
        exit 1
    fi
done
# The app-declared task type's promise: its metadata rides the app module's reflection tables
# like every app type's, but not one of its METHODS may be emitted — the builder that reaches
# them is adopted, so nothing does.
if grep -qh "m_CustomTaskLocalSubset_LocalPromise_" "$out"/generated*.cpp; then
    echo "FAIL: the app-declared custom task type's promise has emitted bodies:" >&2
    grep -oh -m 3 "m_CustomTaskLocalSubset_LocalPromise_[A-Za-z0-9_]*" "$out"/generated*.cpp >&2
    exit 1
fi
# The scheduler's ConcurrentDictionary is the single fattest thing the closure would drag in.
if grep -qh "ConcurrentDictionary" "$out"/generated*.cpp; then
    echo "FAIL: the library scheduler's ConcurrentDictionary was pulled into the tree" >&2
    exit 1
fi
echo "OK (the library's pool / runner / promise / scheduler never entered the tree)"

echo "== 7/10 Un-adopted: the library's async plumbing MUST be in the tree =="
# The exact inverse of step 6, over the --no-adopt-async output: the library's real
# IL entered the tree, so its plumbing symbols exist. A silently re-fired adoption
# (the worst failure: everything still runs, through the wrong machinery) fails here.
for sym in TaskPool Runner ITaskPoolNode AsyncCustomTaskMethodBuilder CustomTaskScheduler; do
    if ! grep -qh "CustomAsyncTaskLib_${sym}" "$out-real"/generated*.cpp "$out-real/generated.h"; then
        echo "FAIL: --no-adopt-async CustomAsyncTaskLib did not put the library's $sym in the" >&2
        echo "      tree — the assembly is still being adopted." >&2
        exit 1
    fi
done
# The opt-out is per-ASSEMBLY: the app-declared task type was not named, so it must
# still be adopted — its promise has no emitted bodies even in the un-adopted output.
if grep -qh "m_CustomTaskLocalSubset_LocalPromise_" "$out-real"/generated*.cpp; then
    echo "FAIL: the app-declared custom task type lost its adoption — --no-adopt-async" >&2
    echo "      CustomAsyncTaskLib must scope to that one assembly:" >&2
    grep -oh -m 3 "m_CustomTaskLocalSubset_LocalPromise_[A-Za-z0-9_]*" "$out-real"/generated*.cpp >&2
    exit 1
fi
echo "OK (the library's real IL is in the tree; the app's own task type stays adopted)"

echo "== 8/10 Auto-declined: the library's async plumbing MUST be in the tree, flag-free =="
# The same symbol set step 7 asserts for the manual opt-out, over the NO-flags transpile
# of the exceeding program: the pre-scan's decline must have routed the library's real IL
# through the general pipeline. A silently re-fired adoption cannot get this far (the
# out-of-contract Yield would fail loud at emit), so what this catches is a decline that
# fired for the wrong reason or a stale output directory.
for sym in TaskPool Runner ITaskPoolNode AsyncCustomTaskMethodBuilder CustomTaskScheduler; do
    if ! grep -qh "CustomAsyncTaskLib_${sym}" "$out-exceed"/generated*.cpp "$out-exceed/generated.h"; then
        echo "FAIL: the auto-declined transpile did not put the library's $sym in the tree." >&2
        exit 1
    fi
done
echo "OK (the library's real machinery is in the tree with no flag)"

echo "== 9/10 Compiling C++ (all three) =="
compile_console "$out" "$project"
compile_console "$out-real" "$project"
compile_console "$out-exceed" "$project_exceed"

echo "== 10/10 Running (exact diff vs real .NET, all three) =="
set +e
expected=$(dotnet "$app");   expected_code=$?
native=$("./$out/$project"); native_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
set +e
native_real=$("./$out-real/$project"); native_real_code=$?
set -e
assert_output "$native_real" "$expected"
assert_exit_code "$native_real_code" "$expected_code"
set +e
expected_exceed=$(dotnet "$app_exceed");                  expected_exceed_code=$?
native_exceed=$("./$out-exceed/$project_exceed");         native_exceed_code=$?
set -e
assert_output "$native_exceed" "$expected_exceed"
assert_exit_code "$native_exceed_code" "$expected_exceed_code"
gate_cache_commit
