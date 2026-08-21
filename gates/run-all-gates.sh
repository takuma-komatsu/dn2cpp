#!/usr/bin/env bash
# run-all-gates.sh — run every dn2cpp regression gate as fast as possible.
#
# Strategy:
#   Phase 1  Build the CLI once; export DN2CPP_CLI_DLL so each child gate calls
#            `dotnet exec` instead of `dotnet run` (skips MSBuild on every
#            invocation — the single largest per-gate overhead).
#   Phase 2  Resolve System.Private.CoreLib (export DN2CPP_CORELIB) and prebuild
#            the CMake runtime once (export DN2CPP_CMAKE_RUNTIME_EXPORT). Then
#            build the shared/referenced projects SERIALLY in dependency order:
#            the shim libraries first, then the projects that ProjectReference
#            them. This is what makes the parallel build phase race-free — MSBuild
#            has no robust cross-process lock, so two processes must never build
#            the same project (or a shared ProjectReference) at once.
#   Phase 3  Build every remaining (dependency-free) C# sample in parallel,
#            except projects whose acquisition/build is owned by their gate.
#   Phase 4  Export DN2CPP_SKIP_BUILD (every suite-owned project is now built)
#            and run the
#            non-Godot gates in parallel (xargs -P). With the build skipped, a
#            gate is just transpile + CMake build + run, so nothing rebuilds a
#            shared project concurrently. Each gate logs to $LOGDIR/<name>.log;
#            failures are recorded in $LOGDIR/_failures.txt. Gates whose
#            inputs are unchanged since their last pass report "cached green"
#            without compiling or running; a gate_partial run is cached as
#            partial and replayed as "cached partial" on warm normal runs (see
#            "Gate result cache" in _common.sh; DN2CPP_GATE_CACHE=0 disables).
#   Phase 5  Run the Godot gates in parallel chains: gates that write the same
#            directories (a Godot project dir, a shared staging dir) stay
#            serial within one chain; independent chains run concurrently.
#            The whole phase runs under a MACHINE-wide lock (suite_machine_lock
#            in _common.sh): two runners with different LOGDIRs may overlap in
#            Phases 1-4, but their Godot/simulator phases are serialized so
#            they never fight over the one engine and one simulator this
#            machine has — the second runner waits, printing the owner's pid.
#            Opt out with DN2CPP_NO_MACHINE_LOCK=1.
#   Phase 6  Print the summary (per-gate timings, slowest gates, any failed
#            gates, any SKIPPED gates) and exit non-zero if any gate failed.
#
# Passed, skipped, failed — three outcomes, not two:
#   A handful of gates need something the repo cannot carry (an Android NDK, an
#   Xcode simulator, Emscripten, the pinned godot-dotnet artifacts). They opt out
#   via gate_skip (_common.sh), which exits 77; this runner counts those
#   SEPARATELY and says so prominently. It used to count them as passes — and
#   that is exactly how the mono-module lane shipped red on main for a week: its
#   sample gate could not transpile even its own sample, skipped on every machine
#   without the artifacts (the default), and the suite still reported all-green.
#   So: "141/141 passed" now means 141 gates actually ran and asserted.
#   DN2CPP_REQUIRE_ALL=1 turns any skip (and any partially-skipped section) into
#   a failure — the mode a pre-merge or CI run uses, where "not run" is not an
#   acceptable answer. It also refuses cached partials: a gate whose last run was
#   partial re-runs live, so a warm cache cannot hide a skipped section. A gate
#   that exits 0 having printed a SKIP line is failed outright: that is the old
#   silent pattern, and it must not come back.
#
# Compatibility: written for bash 3.2 (the macOS system /bin/bash) — no
# `mapfile`, no associative arrays. The same `bash` on PATH is reused by xargs to
# import the exported functions, so do not mix bash major versions.
#
# Containment: the suite runs in its own process group under a thin supervisor
# (the block right below `set -euo pipefail`), so a signal — or the death of
# the shell that launched it — anywhere in Phases 1–6 tears the whole tree
# down, and nothing the suite spawned can outlive it. Every gate runs under a
# hard per-gate ceiling (DN2CPP_GATE_WATCHDOG_SECS, default 10800), and the
# wrapper-family run steps carry their own tighter budget
# (DN2CPP_RUN_WATCHDOG_SECS, see run_bounded in _common.sh): a hang is a
# bounded, attributed failure, never an undetectable non-finish.
#
# Usage:
#   ./gates/run-all-gates.sh                 # all gates, jobs = CPU count
#   JOBS=4 ./gates/run-all-gates.sh          # cap parallelism
#   CONFIG=Debug ./gates/run-all-gates.sh    # Debug build
#   SKIP_GODOT=1 ./gates/run-all-gates.sh    # skip the Godot gates
#   DN2CPP_REQUIRE_ALL=1 ./gates/run-all-gates.sh  # every gate must RUN: a gate
#                                            # that opts out for a missing
#                                            # prerequisite fails instead, and
#                                            # cached partials are refused —
#                                            # those gates re-run live
#                                            # (pre-merge / CI mode)
#   DN2CPP_GATE_CACHE=0 ./gates/run-all-gates.sh  # disable the gate result cache
#                                            # (see "Gate result cache" in _common.sh)
#   LOGDIR=/path ./gates/run-all-gates.sh    # custom log dir (for concurrent runs)
#   DN2CPP_NO_MACHINE_LOCK=1 ./gates/run-all-gates.sh  # skip the machine-wide
#                                            # Godot/simulator phase lock (see
#                                            # suite_machine_lock in _common.sh;
#                                            # DN2CPP_MACHINE_LOCK_TIMEOUT_SECS
#                                            # tunes the wait ceiling, default 6h)

set -euo pipefail

# ── Containment: own process group + supervisor ──────────────────────────────
# Two failure modes shaped this block, both lived through and both ending in a
# reboot-only cleanup:
#   - No trap covered Phases 1–4, so a signal (or a hang) during the builds or
#     the xargs -P gate run cleaned up nothing: one transpiled binary spinning
#     in an infinite loop held its xargs worker slot forever, Phase 4 never
#     returned, and the runner became indistinguishable from a long run.
#   - The suite shared the launching shell's process group. When that shell
#     died (a closed terminal, a killed background zsh), the xargs workers,
#     their dotnet/cmake/ninja/clang children and any hung gate binary
#     survived as init-reparented orphans, still burning CPU.
# So the first invocation re-launches itself as a job-control background job —
# set -m gives the child a fresh pgid equal to its pid — and stays behind as a
# thin supervisor that only forwards signals and reaps. INT/TERM/HUP each
# forward one TERM to the suite's group; HUP is the closed-terminal path, and
# it MUST be forwarded, because the login shell HUPs only the pgroups it knows
# as jobs and the suite's is not one of them. The EXIT reap fires on a green
# run too, so it pays nothing when the group is already empty (the TERM
# probing it fails) and otherwise TERMs, waits 2s, then KILLs. One
# negative-pid kill takes down every descendant regardless of which xargs
# worker or gate subshell spawned it — nothing here relies on xargs forwarding
# signals to its children — and it can never take down the caller, whose
# process group the suite no longer shares. Windows (MSYS) keeps the old
# single-process shape: its process-group emulation is not trustworthy enough
# to re-exec through, and this incident class is a Unix-session one.
case "$(uname -s)" in
    CYGWIN*|MINGW*|MSYS*) ;;
    *)
        if [ -z "${DN2CPP_SUITE_SUPERVISED:-}" ]; then
            export DN2CPP_SUITE_SUPERVISED=1
            set -m
            "$0" "$@" </dev/null &
            SUITE_PID=$!
            set +m
            suite_reap() {
                kill -s TERM -- -"$SUITE_PID" 2>/dev/null || return 0
                sleep 2
                kill -s KILL -- -"$SUITE_PID" 2>/dev/null || true
            }
            trap 'kill -s TERM -- -"$SUITE_PID" 2>/dev/null || true' INT TERM HUP
            trap 'suite_reap' EXIT
            RC=0
            while :; do
                RC=0
                wait "$SUITE_PID" || RC=$?
                # A forwarded signal interrupts wait before the suite has
                # died; loop until the group leader is gone so RC is the
                # suite's real exit status, not 128+signal.
                kill -0 "$SUITE_PID" 2>/dev/null || break
            done
            exit "$RC"
        fi
        ;;
esac

# Whole-run signal net (the worker side of the supervisor above): INT/TERM/HUP
# anywhere — Phase 1's dotnet build as much as Phase 5's chains — takes the
# whole process group down. Everything the suite spawns shares this pgid (the
# supervisor gave it a fresh one, and monitor mode stays off in here), so
# kill 0 reaches the xargs workers, the gate scripts and every child those
# spawned, including the background chains whose default-SIGINT-ignoring is
# why Phase 5 once needed a trap of its own. Reset first so kill 0, which
# signals this shell too, cannot re-enter the trap.
trap 'trap - INT TERM HUP; kill 0' INT TERM HUP

cd "$(dirname "$0")/.."

CONFIG=${CONFIG:-Release}
TFM=${TFM:-net10.0}
GODOT=${GODOT:-godot}
SKIP_GODOT=${SKIP_GODOT:-0}
REQUIRE_ALL=${DN2CPP_REQUIRE_ALL:-0}
export DN2CPP_REQUIRE_ALL="$REQUIRE_ALL"   # read by gate_skip/gate_partial in _common.sh

# Contradiction: "run everything" and "run everything except the Godot gates".
# Fail fast rather than quietly honouring one of them.
if [ "$REQUIRE_ALL" = "1" ] && [ "$SKIP_GODOT" = "1" ]; then
    echo "error: DN2CPP_REQUIRE_ALL=1 demands every gate run, but SKIP_GODOT=1 skips the Godot gates." >&2
    echo "       Drop one of them." >&2
    exit 2
fi

JOBS=${JOBS:-$(sysctl -n hw.logicalcpu 2>/dev/null || nproc 2>/dev/null || echo 4)}

# Per-run log dir. Wiped at start so a stale failure from a previous run can't
# leak into this run's summary. Override LOGDIR to run several copies at once.
LOGDIR=${LOGDIR:-/tmp/dn2cpp-gates}

# One runner per log dir. Two suites sharing it (two worktrees, two sessions —
# they all default to the same /tmp path) destroy each other: the second one's
# `rm -rf` pulls the first one's logs, timings and chain sentinels out from under
# it, and what comes out is not a noisy result but a FICTIONAL one — gates
# reported failed that in fact ran green, gates reported green that never ran.
# The summary is only worth trusting if nothing else is writing to it, so refuse
# rather than interleave. A stale pid (the previous runner died) is not a
# conflict and is simply overwritten.
if [ -f "$LOGDIR/_runner.pid" ]; then
    _other=$(cat "$LOGDIR/_runner.pid" 2>/dev/null || true)
    if [ -n "$_other" ] && kill -0 "$_other" 2>/dev/null \
            && grep -q run-all-gates <<<"$(ps -o command= -p "$_other" 2>/dev/null)"; then
        echo "error: another gate suite (pid $_other) is already using $LOGDIR." >&2
        echo "       Two runners sharing one log dir overwrite each other's results and the" >&2
        echo "       summary becomes fiction. Wait for it to finish, or give this run its own:" >&2
        echo "           LOGDIR=/tmp/dn2cpp-gates-\$\$ ./gates/run-all-gates.sh" >&2
        echo "       (Two runners still share the CPU, but their Godot/simulator phases are" >&2
        echo "        serialized by a machine-wide lock — suite_machine_lock in _common.sh —" >&2
        echo "        so this run's engine gates would wait their turn, not go flaky red.)" >&2
        exit 2
    fi
fi
rm -rf "$LOGDIR"
mkdir -p "$LOGDIR"
printf '%s\n' "$$" > "$LOGDIR/_runner.pid"
FAILFILE="$LOGDIR/_failures.txt"
: > "$FAILFILE"
# Gates that opted out (gate_skip -> exit 77). One "name<TAB>reason" line each.
# Kept apart from the greens so the summary can never conflate the two.
SKIPFILE="$LOGDIR/_skips.txt"
: > "$SKIPFILE"
# Gates that PASSED while declaring a permanent, elsewhere-covered hole
# (gate_expected_partial -> "EXPECTED PARTIAL:" on stdout). One "name<TAB>reason"
# line each. These count as greens — and the summary still names every one of
# them, so "green" never quietly means "green except for something".
EXPECTEDFILE="$LOGDIR/_expected_partials.txt"
: > "$EXPECTEDFILE"
# Gates that PASSED while printing a maintenance warning (gate_warn ->
# "GATE-WARNING:" on stdout). Two producers today: the CA-bundle age check in
# build-and-run-http-get.sh, and gate_cache_check's unreadable-surface arm
# arm, which runs the step live and records no key when it cannot read the
# transpile output it would key on. One "name<TAB>reason" line each. Full
# greens in every mode, DN2CPP_REQUIRE_ALL=1 included — a warning names upkeep
# that is due (a stale vendored input, a step whose cache key cannot be
# computed), never a coverage hole — but the summary still surfaces each one,
# because a warning only a log file ever sees reaches nobody.
WARNFILE="$LOGDIR/_warnings.txt"
: > "$WARNFILE"

# ── helpers ──────────────────────────────────────────────────────────────────

# A declared expected-partial reason is a full paragraph — the whole argument for
# why the hole is permanent and where it is covered instead. The summary is an
# INDEX, not the explanation: it says which gates carry one so a reader can go
# look. The full text lives in $LOGDIR/<gate>.log and at the gate's
# gate_expected_partial call, both of which the summary points at.
EXPECTED_GIST_MAX=100
# Byte escape, not the literal character: macOS ships bash 3.2, which drops the
# left operand when a multibyte literal is concatenated into a variable
# ("$g…" yields the ellipsis alone). Inside a printf FORMAT the literal is fine
# — as the rest of this file's ✔/⊘/ⓘ show — it is assignment that breaks.
EXPECTED_GIST_ELLIPSIS=$'\xe2\x80\xa6'
# expected_partial_gist REASON — first clause (up to the first ": "), else the
# leading EXPECTED_GIST_MAX characters; either way ellipsized when it overruns.
expected_partial_gist() {
    local reason="$*" gist
    gist=${reason%%: *}
    if [ "${#gist}" -gt "$EXPECTED_GIST_MAX" ]; then
        gist="${gist:0:$EXPECTED_GIST_MAX}$EXPECTED_GIST_ELLIPSIS"
    elif [ "$gist" != "$reason" ]; then
        gist="$gist$EXPECTED_GIST_ELLIPSIS"
    fi
    printf '%s\n' "$gist"
}

header() { printf '\n\033[1;36m══ %s \033[0m\n' "$*"; }
ok()     { printf '\033[1;32m✔ %s\033[0m\n' "$*"; }
fail()   { printf '\033[1;31m✘ %s\033[0m\n' "$*" >&2; }

# Whole-second wall clock — bash 3.2 has no EPOCHREALTIME and macOS date no %N,
# but gates run for tens of seconds so second precision is plenty.
now() { date +%s; }

# in_list NEEDLE ITEM... — true if NEEDLE equals one of the ITEMs.
in_list() {
    local needle="$1"; shift
    local x
    for x in "$@"; do [ "$x" = "$needle" ] && return 0; done
    return 1
}

# gate_name SCRIPT — strip the dir, the "build-and-run-" prefix and ".sh" suffix.
gate_name() {
    local n=${1##*/}
    n=${n#build-and-run-}
    printf '%s' "${n%.sh}"
}

# ── Phase 1: build the CLI ───────────────────────────────────────────────────

header "Phase 1/5 — Building CLI"
T_SUITE=$(now)
T_PHASE=$T_SUITE
dotnet build src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj -c "$CONFIG" --nologo -v q

CLI_DLL="$(pwd)/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll"
if [ ! -f "$CLI_DLL" ]; then
    echo "error: expected CLI DLL not found at $CLI_DLL" >&2
    exit 1
fi
export DN2CPP_CLI_DLL="$CLI_DLL"
ok "CLI DLL: $CLI_DLL"
ok "Phase 1 elapsed: $(( $(now) - T_PHASE ))s"

# ── Phase 2: shared prerequisites + dependency-ordered serial builds ──────────

header "Phase 2/5 — Resolving prerequisites and building shared projects"
T_PHASE=$(now)

# The shared helpers (locate_corelib, ensure_cmake_runtime, the cache-key
# hashers below) — sourced before anything Phase 2 resolves, so the runner
# cannot drift from the helpers the gates themselves use.
# shellcheck source=gates/_common.sh
source gates/_common.sh

# CoreLib (impl assembly from the highest installed shared runtime), resolved
# by the same locate_corelib every gate calls — the runner used to re-implement
# it inline, and the copy had already drifted (no CRLF strip). The export
# inside locate_corelib dies with the command substitution's subshell, so
# re-export here.
CORELIB="$(locate_corelib)"
export DN2CPP_CORELIB="$CORELIB"
ok "CoreLib: $CORELIB"

# Prebuild the CMake runtime once so the parallel gates import a ready set of
# runtime libraries instead of recompiling runtime/core (+ runtime/godot) per
# gate; building here (once, serially) also sidesteps the cold-start thundering
# herd at the start of Phase 4. ensure_cmake_runtime lives in _common.sh.
# DN2CPP_NO_GC is honoured inside it (-DDN2CPP_USE_GC=OFF → calloc fallback); the
# Boehm-GC and 64-bit-atomic compile/link flags are resolved by
# runtime/CMakeLists.txt itself.
DN2CPP_CMAKE_RUNTIME_EXPORT="$(ensure_cmake_runtime)"
export DN2CPP_CMAKE_RUNTIME_EXPORT
ok "CMake runtime exported: $DN2CPP_CMAKE_RUNTIME_EXPORT"

# The scalar-axis gates (build-and-run-*-scalar.sh) build the runtime with
# -DDN2CPP_USE_HIGHWAY=OFF into a separate dir (the default build carries
# Highway). Prebuild it once here too so the parallel scalar gates import it
# instead of racing to configure/build the shared scalar runtime dir
# concurrently. Built once, imported many times; the subshell keeps SCALAR
# scoped to this single call.
DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR="$(export SCALAR=1; ensure_cmake_runtime)"
export DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR
ok "CMake scalar runtime exported: $DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR"

# The wasm-axis gates (wasm-console, pinvoke-wasm, shadow-stack's arm 4) build
# the Emscripten runtime with WASM=1 into artifacts/.cmake-runtime-wasm.
# ensure_cmake_runtime configures only on a cold build dir and takes no lock,
# so on a cold worktree those gates, parallel in Phase 4, race the configure of
# that one dir and corrupt its build.ninja ("ninja: warning: premature end of
# file") — failing one of them nondeterministically. Prebuild it once here,
# exactly like the scalar axis above; the subshell keeps WASM scoped to this
# single call. On a machine without the Emscripten toolchain there is nothing
# to prebuild and no race to close — every wasm gate opts out without it — so
# skip with a note.
# Resolved HERE, with the answer its consumers reach: those gates are the whole
# consumer set of this export and every one of them declines the bundled SDK, so
# the prebuild must decline it too. A gate wanting the other answer resolves it
# itself — resolve PREPENDS, so a child's choice outranks this inherited PATH.
dn2cpp_emsdk_resolve --no-bundled
if command -v emcmake >/dev/null 2>&1 && command -v em++ >/dev/null 2>&1; then
    DN2CPP_CMAKE_RUNTIME_EXPORT_WASM="$(export WASM=1; ensure_cmake_runtime)"
    export DN2CPP_CMAKE_RUNTIME_EXPORT_WASM
    ok "CMake wasm runtime exported: $DN2CPP_CMAKE_RUNTIME_EXPORT_WASM"
else
    ok "CMake wasm runtime not prebuilt (no Emscripten toolchain — the wasm gates will skip)"
fi

# Gate-result-cache key inputs shared by every gate (see "Gate result cache" in
# _common.sh): computed once here so the parallel gates don't each re-hash the
# runtime tree and re-run `dotnet --list-runtimes`.
DN2CPP_RUNTIME_HASH="$(_gate_runtime_hash)"
export DN2CPP_RUNTIME_HASH
DN2CPP_DOTNET_RUNTIMES_HASH="$(_gate_dotnet_hash)"
export DN2CPP_DOTNET_RUNTIMES_HASH
ok "Gate cache inputs: runtime-tree=$DN2CPP_RUNTIME_HASH dotnet-runtimes=$DN2CPP_DOTNET_RUNTIMES_HASH"

# Projects that are a ProjectReference target, or that reference one. These MUST
# be built serially and before the parallel phase: building a referencing project
# also builds its reference, so two processes could otherwise write the same
# shared project's obj/bin at once. Order matters — libraries before dependents.
ORDERED_PROJECTS=(
    "src/Dn2Cpp.Runtime/Dn2Cpp.Runtime.csproj"
    "src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj"
    "samples/dotnet/PreserveControlLib/PreserveControlLib.csproj"
    "samples/dotnet/PreserveAssemblyLib/PreserveAssemblyLib.csproj"
    "samples/dotnet/PreserveControl/PreserveControl.csproj"
    "samples/dotnet/MiniCorlib/MiniCorlib.csproj"
    "samples/dotnet/XGenericMethodLib/XGenericMethodLib.csproj"
    "src/GodotSharpShim/GodotSharp.csproj"
    "samples/dotnet/MultiAssembly/MultiAssembly.csproj"
    "samples/dotnet/XAsmGenericMethod/XAsmGenericMethod.csproj"
    "samples/dotnet/ConvOpLib/ConvOpLib.csproj"
    "samples/dotnet/ConvOpSubset/ConvOpSubset.csproj"
    "samples/dotnet/TrimReflectLib/TrimReflectLib.csproj"
    "samples/dotnet/TrimReflect/TrimReflect.csproj"
    "samples/dotnet/PInvokeRefLib/PInvokeRefLib.csproj"
    "samples/dotnet/PInvokeNative/PInvokeNative.csproj"
    "samples/dotnet/CustomAsyncTaskLib/CustomAsyncTaskLib.csproj"
    "samples/dotnet/CustomAsyncTaskType/CustomAsyncTaskType.csproj"
    "samples/dotnet/CustomAsyncTaskTypeExceed/CustomAsyncTaskTypeExceed.csproj"
    "samples/dotnet/CustomAsyncTaskTypeShapeExceed/CustomAsyncTaskTypeShapeExceed.csproj"
    "samples/dotnet/CustomAsyncTaskTypeSharedExceed/CustomAsyncTaskTypeSharedExceed.csproj"
    "samples/dotnet/HotUpdateBase/HotUpdateBase.csproj"
    "samples/dotnet/HotUpdatePatch/HotUpdatePatch.csproj"
    "samples/dotnet/HotUpdateRecvPatch/HotUpdateRecvPatch.csproj"
    "samples/dotnet/HotUpdateArgPatch/HotUpdateArgPatch.csproj"
    "samples/dotnet/HotUpdateInvokerPatch/HotUpdateInvokerPatch.csproj"
    "samples/dotnet/HotUpdateFtnPatch/HotUpdateFtnPatch.csproj"
    "samples/dotnet/HotUpdateDgRecvPatch/HotUpdateDgRecvPatch.csproj"
    "samples/dotnet/HotUpdateDgSigPatch/HotUpdateDgSigPatch.csproj"
    "samples/dotnet/HotUpdateBadPatch/HotUpdateBadPatch.csproj"
    "samples/dotnet/HotUpdateBadPatchItf/HotUpdateBadPatchItf.csproj"
    "samples/dotnet/HotUpdateBadPatchDelegate/HotUpdateBadPatchDelegate.csproj"
    "samples/dotnet/HotUpdateBadPatchMulticast/HotUpdateBadPatchMulticast.csproj"
    "samples/dotnet/HotUpdateDirPatch1/HotUpdateDirPatch1.csproj"
    "samples/dotnet/HotUpdateDirPatch2/HotUpdateDirPatch2.csproj"
    "samples/dotnet/HotUpdateCoreLibBase/HotUpdateCoreLibBase.csproj"
    "samples/dotnet/HotUpdateCoreLibPatch/HotUpdateCoreLibPatch.csproj"
    "samples/dotnet/HotUpdateCoreLibBadPatch/HotUpdateCoreLibBadPatch.csproj"
    "samples/dotnet/InterpBench/InterpBench.csproj"
    "samples/dotnet/InterpBenchPatch/InterpBenchPatch.csproj"
    "samples/godot/GodotSample/GodotSample.csproj"
    "samples/godot/HotUpdateGodotSample/HotUpdateGodotSample.csproj"
    "samples/dotnet/HotUpdateGodotPatch/HotUpdateGodotPatch.csproj"
    "samples/dotnet/HotUpdateGodotBadPatchMathf/HotUpdateGodotBadPatchMathf.csproj"
    "samples/godot/SdkSample/SdkSample.csproj"
    # These three reference a library project (internal/DnZlib, internal/DnBrotli,
    # src/Dn2Cpp.Godot) rather than another sample. Each is that library's only
    # referencer today, so the parallel phase happened not to race over them —
    # but the invariant the Phase-3 pre-flight asserts is the simple one every
    # future reader can hold: a sample that builds a reference builds it here,
    # serially.
    "samples/dotnet/DnZlibBench/DnZlibBench.csproj"
    "samples/dotnet/DnBrotliBench/DnBrotliBench.csproj"
    "samples/dotnet/GodotBindGen/GodotBindGen.csproj"
    # DnHttp sits under internal/ (the Phase 3 find below only scans samples/) and
    # the http-get app pulls it in with --auto-ref at transpile time, not a
    # ProjectReference, so no sample's pre-build connected-builds it the way the
    # compression apps build DnZlib/DnBrotli. Without this line a cold worktree has
    # no DnHttp.dll and http-get fails, while build_proj inside the gate no-ops
    # under SKIP_BUILD — a warm worktree hid it, a cold one does not.
    "internal/DnHttp/src/DnHttp/DnHttp.csproj"
)

# Projects whose restore/build is part of the gate's asserted acquisition path.
# Their gates call build_gate_proj even under DN2CPP_SKIP_BUILD; prebuilding one
# here would turn a missing optional prerequisite into a Phase-3 failure before
# the gate can report the sanctioned skip.
GATE_OWNED_PROJECTS=(
    "samples/dotnet/MessagePipeSample/MessagePipeSample.csproj"
    "samples/dotnet/R3Sample/R3Sample.csproj"
    "samples/dotnet/UniTaskSample/UniTaskSample.csproj"
    "samples/dotnet/ZLinqSample/ZLinqSample.csproj"
    "samples/dotnet/ZStringSample/ZStringSample.csproj"
)

for proj in "${GATE_OWNED_PROJECTS[@]}"; do
    [ -f "$proj" ] || { echo "error: missing gate-owned project $proj" >&2; exit 1; }
done

for proj in "${ORDERED_PROJECTS[@]}"; do
    [ -f "$proj" ] || { echo "error: missing project $proj" >&2; exit 1; }
    dotnet build "$proj" -c "$CONFIG" --nologo -v q
done
ok "Shared/referenced projects built (${#ORDERED_PROJECTS[@]})"
ok "Phase 2 elapsed: $(( $(now) - T_PHASE ))s"

# ── Phase 3: build all remaining samples in parallel ─────────────────────────

header "Phase 3/5 — Building independent samples in parallel (jobs=$JOBS)"
T_PHASE=$(now)

# Pre-flight: every ProjectReference-bearing sample MUST be in ORDERED_PROJECTS.
# Building a referencing project also builds its reference, so one that slips
# into the parallel build below can race another build of the same shared
# reference. A warm worktree hides the miss (every build is a no-op); a cold
# one turns it into a nondeterministic Phase-3 failure — so fail it loudly
# here instead. (samples/godot-dotnet/ is exempt: never pre-built at all, see
# the note below.)
MISSING_ORDERED=""
while IFS= read -r csproj; do
    in_list "$csproj" "${ORDERED_PROJECTS[@]}" && continue
    in_list "$csproj" "${GATE_OWNED_PROJECTS[@]}" && continue
    case "$csproj" in samples/godot-dotnet/*) continue ;; esac
    grep -q '<ProjectReference' "$csproj" || continue
    MISSING_ORDERED="$MISSING_ORDERED $csproj"
done < <(find samples -name "*.csproj" | sort)
if [ -n "$MISSING_ORDERED" ]; then
    echo "error: ProjectReference-bearing sample project(s) not in ORDERED_PROJECTS:" >&2
    for csproj in $MISSING_ORDERED; do
        echo "         $csproj" >&2
    done
    echo "       Add them there (libraries before dependents) so the parallel phase" >&2
    echo "       cannot race a shared reference build — see the ORDERED_PROJECTS comment." >&2
    exit 1
fi

# Every sample .csproj except the dependency-ordered ones already built above.
PARALLEL_CSPROJS=()
while IFS= read -r csproj; do
    in_list "$csproj" "${ORDERED_PROJECTS[@]}" && continue
    in_list "$csproj" "${GATE_OWNED_PROJECTS[@]}" && continue
    # samples/godot-dotnet/ is never pre-built here. Those projects resolve
    # Godot.NET.Sdk (and, for GDTaskSample, GDTask) through a nuget.config their
    # own gate generates — it carries machine-specific absolute feed paths, so it
    # cannot be checked in, and the MSBuild SDK resolver needs it *before* any
    # restore flag applies. They also build in the Sdk's own ExportRelease
    # configuration, not -c Release. Their gates do both themselves (and do it
    # even under DN2CPP_SKIP_BUILD), so a pre-build here would produce a Release
    # output nothing reads — and, on a fresh clone that has never run the gates,
    # would fail outright for want of the nuget.config. (The editor-export sample
    # is doubly excluded: its Godot.NET.Sdk comes from the *fork's* feed, and the
    # only build that matters is the `dotnet publish` the forked editor drives
    # during the export.)
    case "$csproj" in samples/godot-dotnet/*) continue ;; esac
    PARALLEL_CSPROJS+=("$csproj")
done < <(find samples -name "*.csproj" | sort)

build_one_project() {
    local csproj="$1"
    dotnet build "$csproj" -c "$CONFIG" --nologo -v q \
        >"$LOGDIR/_build_$(basename "$csproj" .csproj).log" 2>&1
}
export -f build_one_project
export CONFIG LOGDIR

if ! printf '%s\n' "${PARALLEL_CSPROJS[@]}" | \
        xargs -P "$JOBS" -I{} bash -c 'build_one_project "$@"' _ {}; then
    echo "error: one or more sample builds failed — see $LOGDIR/_build_*.log" >&2
    exit 1
fi
ok "Suite-owned samples built (${#PARALLEL_CSPROJS[@]} parallel + ${#ORDERED_PROJECTS[@]} serial; ${#GATE_OWNED_PROJECTS[@]} gate-owned)"
ok "Phase 3 elapsed: $(( $(now) - T_PHASE ))s"

# Everything is built; gates may now skip their per-invocation dotnet build.
export DN2CPP_SKIP_BUILD=1

# ── Phase 4: run non-Godot gates in parallel ─────────────────────────────────

header "Phase 4/5 — Running non-Godot gates in parallel (jobs=$JOBS)"
T_PHASE=$(now)

# The Godot/engine gates run in Phase 5, grouped into chains: gates in one
# chain WRITE the same directories (a Godot project dir's bin/ + .godot import
# cache, a shared staging dir) or lazily build the same shared CMake runtime,
# so they stay serial within the chain; distinct chains touch disjoint write
# sets and run concurrently. There is no data dependency between a chain's
# gates (godot-ios-sim re-exports its own Xcode project; it does not consume
# godot-ios-export's) — the chain is purely mutual exclusion. The two
# simulator-launching gates (A's godot-ios-sim, E's godot-editor-export-ios)
# additionally serialize their simulator window through sim_lock (_common.sh),
# which is machine-wide and therefore also holds against the one simulator gate
# that is NOT in a chain: ios-sim-console runs back in Phase 4, and the note at
# DN2CPP_MACHINE_LOCK_GATES says why moving it here would be the wrong fix.
# The godot-dotnet chain shares the DotnetSample project with the
# parallel-phase lib gate — the Phase 4/5 boundary is what keeps them from
# overlapping.
CHAIN_A=(   # samples/godot/godot-project (bin/ + .godot) + iOS/Android runtime caches
    "gates/build-and-run-godot-sample.sh"
    "gates/build-and-run-godot-ios-export.sh"
    "gates/build-and-run-godot-ios-sim.sh"
    "gates/build-and-run-android-gdext.sh"
)
CHAIN_B=(   # samples/godot/godot-project-hotupdate
    "gates/build-and-run-hotupdate-godot.sh"
)
CHAIN_C=(   # samples/godot/godot-project-sdk
    "gates/build-and-run-sdk-sample.sh"
)
CHAIN_D=(   # samples/godot-dotnet/DotnetSample (nuget.config, .godot/mono)
    "gates/build-and-run-godot-dotnet-handshake.sh"
    "gates/build-and-run-godot-dotnet-sample.sh"
    # The same scripted-scene E2E as the sample gate, but the transpile passes
    # --trim-reflection: the real-engine oracle for the flag (the console side is
    # build-and-run-trim-reflection.sh). Shares the DotnetSample project write set, so it
    # stays in this chain, serial with the untrimmed sample gate above.
    "gates/build-and-run-godot-dotnet-trim.sh"
    # Needs no engine — it plays the engine's role itself, under node. It is in a
    # chain because the chains are a write-set partition, not a needs-Godot one, and
    # this gate builds the same DotnetSample project: the Phase 4/5 boundary is what
    # keeps it off the parallel-phase lib gate's toes, exactly as the note above says.
    "gates/build-and-run-godot-dotnet-wasm.sh"
)
CHAIN_E=(   # fork editor exports. The shared $FORK_GODOTSHARP/Dn2Cpp +
    # artifacts/toolchain staging no longer needs this chain for safety:
    # stage_editor_toolchain (gates/_common.sh) stages it atomically under
    # a lock held beside that tree, so it is safe even across suites. The chain
    # is kept because concurrent fork-editor launches contend for resources.
    "gates/build-and-run-godot-editor-export.sh"
    "gates/build-and-run-godot-editor-export-ios.sh"
    "gates/build-and-run-godot-editor-export-android.sh"
    "gates/build-and-run-godot-editor-export-web.sh"
    # The same export with the host's Emscripten SDK taken away, proving the
    # bundle's own is sufficient and stays read-only.
    "gates/build-and-run-godot-editor-export-web-hermetic.sh"
    # CRI ADX LE Android E2E: stages the external CriWare sample, exports it
    # through the fork editor (same editor-launch + toolchain-staging write set
    # as the gates above), and runs it on an attached adb device when present.
    "gates/build-and-run-cri-android.sh"
    # CRI ADX LE Web E2E: the same staged sample through the mono-module Web
    # lane against the CRI-enabled template, run in a real Chrome when present.
    "gates/build-and-run-cri-web.sh"
)
CHAIN_F=(   # samples/godot-dotnet/GDTaskSample + gates/out-gdtask*
    # Alone in its chain because the chains are a write-set partition, and this
    # gate's write set is disjoint from every other: its own project dir, its own
    # out dirs. It shares only read-only things (the pinned editor/template
    # binaries, the prebuilt CMake runtime) and ~/.nuget, whose restores are
    # already concurrency-safe.
    "gates/build-and-run-gdtask.sh"
)
GODOT_GATES=(
    "${CHAIN_A[@]}" "${CHAIN_B[@]}" "${CHAIN_C[@]}" "${CHAIN_D[@]}" "${CHAIN_E[@]}"
    "${CHAIN_F[@]}"
)

# Parity: the chains above and DN2CPP_MACHINE_LOCK_GATES in _common.sh —
# the table that decides which gate takes the machine lock for itself when it is
# run by hand — are two statements of one fact. Diff them here, on every suite
# (this runs before the SKIP_GODOT branch, so even a smoke run checks it), and
# die naming the difference. The direction that matters is a gate added to a
# chain and not to the table: it would quietly stop locking, which is the
# fail-OPEN failure this whole mechanism exists to make impossible, and nothing
# downstream would ever say so.
_chain_names=$(printf '%s\n' "${GODOT_GATES[@]}" | sed 's#^gates/##' | sort -u)
_table_names=$(printf '%s\n' "$DN2CPP_MACHINE_LOCK_GATES" | sort -u)
if [ "$_chain_names" != "$_table_names" ]; then
    echo "error: the Phase-5 chains and DN2CPP_MACHINE_LOCK_GATES (gates/_common.sh) disagree." >&2
    echo "       Every gate in a chain must be in that table, and nothing else may be:" >&2
    diff <(printf '%s\n' "$_chain_names") <(printf '%s\n' "$_table_names") \
        | sed 's/^</       only in a chain: /; s/^>/       only in the table: /' >&2
    exit 1
fi
unset _chain_names _table_names

NON_GODOT_GATES=()
while IFS= read -r g; do
    in_list "$g" "${GODOT_GATES[@]}" && continue
    NON_GODOT_GATES+=("$g")
done < <(ls gates/build-and-run-*.sh | sort)

# run_gate SCRIPT [TAG] — run one gate, log output, record duration + verdict in
# _timings.txt (one "name seconds ran|cached|skipped|failed" line per gate; the
# summary aggregates it), record failures and skips by name. TAG prefixes the
# status line with the Phase-5 chain the gate ran in.
#
# Three outcomes. Exit 0 is a pass, exit 77 (GATE_SKIP_RC, from gate_skip) is an
# opt-out for an absent prerequisite, anything else is a failure. A skip is
# recorded in SKIPFILE and reported apart from the greens — never as one.
run_gate() {
    local script="$1" tag="${2:+[$2] }"
    local name; name=$(gate_name "$script")
    local t0=$SECONDS dt verdict note="" rc=0 reason
    # Per-gate backstop ceiling: no single gate may block the suite forever.
    # The engine launches inside the Godot gates carry their own per-step
    # watchdogs (up to 3600s) and the wrapper-family run steps are bounded by
    # run_bounded, so tripping this means dotnet/cmake/ninja or a bespoke gate
    # step wedged — rare, and exactly the shape that once made a running suite
    # undetectable as finished. 10800s (3h) clears the largest per-step budget
    # sum in the tree (godot-editor-export-ios, ~5700s worst case) with
    # headroom to spare: the point is finiteness and attribution, not
    # tightness. The expired dog's WATCHDOG line lands in the gate's log.
    run_with_watchdog "${DN2CPP_GATE_WATCHDOG_SECS:-10800}" bash "$script" \
        >"$LOGDIR/$name.log" 2>&1 || rc=$?
    dt=$((SECONDS - t0))
    if [ "$rc" -eq 0 ]; then
        # Tripwire on the pattern gate_skip replaced: a gate that announces a
        # SKIP and then exits 0 is indistinguishable from one that passed, which
        # is precisely how a broken gate once hid inside an all-green suite. Fail
        # it, and name the fix.
        if grep -q '^SKIP' "$LOGDIR/$name.log"; then
            printf '%s %s failed\n' "$name" "$dt" >> "$LOGDIR/_timings.txt"
            printf '%s\n' "$name" >> "$FAILFILE"
            printf '\033[1;31m✘ %s%-45s %4ss  (exited 0 after printing SKIP — call gate_skip instead; see _common.sh)\033[0m\n' \
                "$tag" "$name" "$dt" >&2
            return 1
        fi
        verdict=ran
        # The gate result cache's hit markers (see gate_cache_hit_msg in
        # _common.sh) — a replayed partial counts as cached too. Multi-section
        # gates count as cached when any section hit; the verdict is
        # informational, not part of the pass/fail contract.
        if grep -Eq 'cached (green|partial)' "$LOGDIR/$name.log"; then
            verdict=cached
            note=' (cached)'
        fi
        # A reduced-coverage pass (gate_partial, live or replayed from the
        # cache): flag it on the pass line. Visibility only — REQUIRE_ALL is
        # what gives a partial teeth.
        if grep -q '^PARTIAL:' "$LOGDIR/$name.log"; then
            note="$note (partial)"
        fi
        # A DECLARED permanent hole (gate_expected_partial, live or replayed).
        # It is a pass in every mode including REQUIRE_ALL — the declaration's
        # preconditions are in _common.sh — but it is recorded by name and
        # reason so the summary can list it. Visibility is the whole point: the
        # cost this replaces was a human re-explaining "that one is known".
        if grep -q '^EXPECTED PARTIAL:' "$LOGDIR/$name.log"; then
            note="$note (expected partial)"
            reason=$(LC_ALL=C sed -n 's/^EXPECTED PARTIAL: //p' "$LOGDIR/$name.log")
            reason=${reason%%$'\n'*}
            reason=$(expected_partial_gist "$reason")
            printf '%s\t%s\n' "$name" "${reason:-(no reason given)}" >> "$EXPECTEDFILE"
        fi
        # A maintenance warning (gate_warn). Still a pass in every mode,
        # REQUIRE_ALL included — a warning names upkeep that is due, not a
        # coverage hole — but it is recorded by name and reason so the summary
        # surfaces it. The grep wants GATE-WARNING, never a bare WARNING:
        # Godot's own output prints "WARNING:" at line start routinely, and
        # engine noise must not become a suite-level warning.
        if grep -q '^GATE-WARNING:' "$LOGDIR/$name.log"; then
            note="$note (warning)"
            reason=$(LC_ALL=C sed -n 's/^GATE-WARNING: //p' "$LOGDIR/$name.log")
            reason=${reason%%$'\n'*}
            reason=$(expected_partial_gist "$reason")
            printf '%s\t%s\n' "$name" "${reason:-(no reason given)}" >> "$WARNFILE"
        fi
        printf '%s %s %s\n' "$name" "$dt" "$verdict" >> "$LOGDIR/_timings.txt"
        printf '\033[1;32m✔ %s%-45s %4ss%s\033[0m\n' "$tag" "$name" "$dt" "$note"
        return 0
    fi
    if [ "$rc" -eq 77 ]; then    # GATE_SKIP_RC — prerequisite absent, gate not run
        reason=$(LC_ALL=C sed -n 's/^SKIP: //p' "$LOGDIR/$name.log")
        reason=${reason%%$'\n'*}
        printf '%s %s skipped\n' "$name" "$dt" >> "$LOGDIR/_timings.txt"
        printf '%s\t%s\n' "$name" "${reason:-(no reason given)}" >> "$SKIPFILE"
        printf '\033[1;33m⊘ %s%-45s %4ss  SKIPPED: %s\033[0m\n' \
            "$tag" "$name" "$dt" "${reason:-(no reason given)}"
        return 0
    fi
    # A gate that DIED BY SIGNAL must say so on its own line. Such a run ends
    # mid-sentence: the log's last line is whatever it had printed, and pointing
    # at it is pointing at a file whose contents are, by construction, not the
    # cause. A bare "exit 137, empty stderr" carries no attribution at all —
    # and the one discriminator that matters is free, because a watchdog that
    # kills its own subject announces it into this very log first: a 137 WITH a
    # WATCHDOG line is a run that overran its budget, and a 137 WITHOUT one came
    # from outside the gate entirely, which is a fact about the machine and not
    # about the tree. Nothing here retries or excuses; it names what happened so
    # the next occurrence does not need this ticket re-derived.
    note=""
    if [ "$rc" -ge 128 ]; then
        if grep -q '^WATCHDOG: no exit after' "$LOGDIR/$name.log"; then
            note="  killed by its OWN watchdog, signal $((rc - 128)) — it overran a budget"
        else
            note="  KILLED FROM OUTSIDE, signal $((rc - 128)) — the log ends mid-run and is not the cause"
        fi
    fi
    printf '%s %s failed\n' "$name" "$dt" >> "$LOGDIR/_timings.txt"
    printf '%s\n' "$name" >> "$FAILFILE"
    printf '\033[1;31m✘ %s%-45s %4ss  (see %s/%s.log)%s\033[0m\n' \
        "$tag" "$name" "$dt" "$LOGDIR" "$name" "$note" >&2
    return 1
}
# kill_tree/run_with_watchdog travel with run_gate: the xargs -P workers are
# fresh bash processes that see only exported functions, not _common.sh.
# _wd_owns_target is part of that set and is not optional: a dog that cannot
# call it stands down, so leaving it behind would disarm every per-gate
# watchdog in the suite and say nothing.
export -f run_gate gate_name kill_tree run_with_watchdog _wd_owns_target \
    expected_partial_gist
export EXPECTED_GIST_MAX EXPECTED_GIST_ELLIPSIS
export DN2CPP_CLI_DLL DN2CPP_CORELIB DN2CPP_SKIP_BUILD
export CONFIG TFM LOGDIR FAILFILE SKIPFILE EXPECTEDFILE WARNFILE
# The prebuilt CMake runtime export reaches child gates so each per-app build
# imports it instead of rebuilding the runtime. The scalar-axis export reaches the
# *-scalar.sh gates the same way (they re-derive it via SCALAR=1 in ensure_cmake_runtime),
# and the wasm-axis export the WASM=1 gates likewise — guarded, because it is set
# only when the Emscripten toolchain was present at the Phase 2 prebuild.
export DN2CPP_CMAKE_RUNTIME_EXPORT
export DN2CPP_CMAKE_RUNTIME_EXPORT_SCALAR
[ -z "${DN2CPP_CMAKE_RUNTIME_EXPORT_WASM:-}" ] || export DN2CPP_CMAKE_RUNTIME_EXPORT_WASM

printf '%s\n' "${NON_GODOT_GATES[@]}" | \
    xargs -P "$JOBS" -I{} bash -c 'run_gate "$@"' _ {} || true
ok "Phase 4 elapsed: $(( $(now) - T_PHASE ))s"

# ── Phase 5: Godot gates (parallel chains) ───────────────────────────────────

# run_chain TAG GATE... — run the chain's gates serially (continuing past a
# failed gate, exactly like the old serial Phase 5), then drop a .done
# sentinel. The sentinel is the liveness audit: if the subshell dies between
# gates (set -e on a non-gate command, an external kill), audit_chain below
# turns its un-run gates into recorded failures instead of letting them count
# as silently passed. Always exits 0 — failures travel through FAILFILE.
run_chain() {
    local tag="$1"; shift
    local g
    for g in "$@"; do
        run_gate "$g" "$tag" || true
    done
    : > "$LOGDIR/_chain_$tag.done"
}

# audit_chain TAG GATE... — after the chains finish, fail every gate of a
# chain that never completed (no .done sentinel) and left no timing record.
audit_chain() {
    local tag="$1"; shift
    [ -f "$LOGDIR/_chain_$tag.done" ] && return 0
    local g name
    for g in "$@"; do
        name=$(gate_name "$g")
        grep -q "^$name " "$LOGDIR/_timings.txt" 2>/dev/null && continue
        printf '%s\n' "$name" >> "$FAILFILE"
        fail "$name  (chain $tag died before running it)"
    done
}

if [ "$SKIP_GODOT" = "1" ]; then
    header "Phase 5/5 — Godot gates SKIPPED (SKIP_GODOT=1)"
else
    header "Phase 5/5 — Running Godot gates in parallel chains"
    T_PHASE=$(now)
    # Machine-wide engine/simulator lock: one runner's Godot phase at a
    # time across ALL runners on this machine, whatever their LOGDIRs — two
    # concurrent Phase 5s fight over one Godot engine and one iOS simulator,
    # and the loser's engine launches starve past their watchdog budgets and
    # go flaky red. Taken ONCE here, not per gate or per chain, so the chains
    # below keep their full within-runner parallelism; a second runner blocks
    # at this line, printing whom it is waiting on (pid) while it waits. The
    # EXIT trap releases it if the runner dies mid-phase (a stale lock is also
    # reclaimed by the next waiter's live-pid check); the explicit unlock
    # after the chains keeps the summary phase outside the held window. See
    # suite_machine_lock in _common.sh; opt out with DN2CPP_NO_MACHINE_LOCK=1.
    #
    # DN2CPP_MACHINE_LOCK_HELD is the re-entrancy handshake: the Godot gates
    # take this same lock themselves, so that a hand-run gate
    # contends too, they must stand down under a runner that already holds it.
    # It is exported for exactly the held window and unset after the release —
    # a value outliving the hold would be a claim with nothing behind it, and
    # gate_machine_lock refuses such a claim anyway by checking the value
    # against the lock dir's own owner pid. Set AFTER the acquire: the pid it
    # names has to be the recorded owner, and before the acquire this runner
    # is not.
    trap 'suite_machine_unlock' EXIT
    suite_machine_lock
    export DN2CPP_MACHINE_LOCK_HELD=$$
    printf 'chain A: %s\n' "$(printf '%s ' "${CHAIN_A[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    printf 'chain B: %s\n' "$(printf '%s ' "${CHAIN_B[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    printf 'chain C: %s\n' "$(printf '%s ' "${CHAIN_C[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    printf 'chain D: %s\n' "$(printf '%s ' "${CHAIN_D[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    printf 'chain E: %s\n' "$(printf '%s ' "${CHAIN_E[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    printf 'chain F: %s\n' "$(printf '%s ' "${CHAIN_F[@]}" | sed 's/gates\/build-and-run-//g;s/\.sh//g')"
    # The chains (and their godot/xcodebuild children) fall under the
    # whole-run trap installed at the top: background children of a
    # non-interactive shell ignore SIGINT by default, but kill 0's TERM
    # reaches them all the same.
    run_chain A "${CHAIN_A[@]}" &
    run_chain B "${CHAIN_B[@]}" &
    run_chain C "${CHAIN_C[@]}" &
    run_chain D "${CHAIN_D[@]}" &
    run_chain E "${CHAIN_E[@]}" &
    run_chain F "${CHAIN_F[@]}" &
    wait
    audit_chain A "${CHAIN_A[@]}"
    audit_chain B "${CHAIN_B[@]}"
    audit_chain C "${CHAIN_C[@]}"
    audit_chain D "${CHAIN_D[@]}"
    audit_chain E "${CHAIN_E[@]}"
    audit_chain F "${CHAIN_F[@]}"
    suite_machine_unlock
    unset DN2CPP_MACHINE_LOCK_HELD
    ok "Phase 5 elapsed: $(( $(now) - T_PHASE ))s"
fi

# ── Phase 6: summary ─────────────────────────────────────────────────────────

header "Summary"

TOTAL=${#NON_GODOT_GATES[@]}
[ "$SKIP_GODOT" = "0" ] && TOTAL=$((TOTAL + ${#GODOT_GATES[@]}))

FAILED=()
if [ -s "$FAILFILE" ]; then
    while IFS= read -r n; do FAILED+=("$n"); done < "$FAILFILE"
fi
N_SKIPPED=0
[ -s "$SKIPFILE" ] && N_SKIPPED=$(wc -l < "$SKIPFILE" | tr -d ' ')
N_PASSED=$(( TOTAL - ${#FAILED[@]} - N_SKIPPED ))

if [ -f "$LOGDIR/_timings.txt" ]; then
    N_CACHED=$(grep -c ' cached$' "$LOGDIR/_timings.txt" || true)
    N_RAN=$(grep -c ' ran$' "$LOGDIR/_timings.txt" || true)
    echo ""
    echo "ran: $N_RAN  cached: $N_CACHED  skipped: $N_SKIPPED  (full per-gate timings: $LOGDIR/_timings.txt)"
    echo "Slowest gates:"
    # The top-10 cut is awk's, not `| head -10`'s, and that is load-bearing rather
    # than tidy. `head` exits after ten lines, `sort` dies of SIGPIPE with the other
    # ninety unwritten, and under this suite's `set -o pipefail` the PIPELINE reports
    # 141 — which `set -e` then treats as a fatal error, so the runner aborts HERE.
    # Everything below this line stops printing: the elapsed time, the skipped-gate
    # block, and the verdict itself. Measured on a run where all 100 gates passed:
    # the last output was this list, and the runner exited 141. Both directions are
    # bad and the second is the quiet one — a green suite reports as failed, and a
    # run with skips never reaches the block that names them. awk reads to EOF and
    # closes nothing early.
    sort -k2,2rn "$LOGDIR/_timings.txt" \
        | awk 'NR <= 10 { printf "  %5ss  %s (%s)\n", $2, $1, $3 }'
    echo "suite elapsed: $(( $(now) - T_SUITE ))s"
fi

# Skipped gates get their own block, above the verdict, whether or not anything
# failed: a gate that did not run is a hole in the suite's coverage, and the one
# thing it must never do is read as a pass.
if [ "$N_SKIPPED" -gt 0 ]; then
    echo ""
    printf '\033[1;33m⊘ %d gate(s) SKIPPED — NOT run, NOT passed:\033[0m\n' "$N_SKIPPED"
    while IFS=$'\t' read -r n reason; do
        printf '\033[1;33m    %-42s %s\033[0m\n' "$n" "$reason"
    done < "$SKIPFILE"
    echo "  Install the missing prerequisites to close these holes, or run with"
    echo "  DN2CPP_REQUIRE_ALL=1 to turn any skip into a failure (pre-merge / CI mode)."
fi
# Declared expected partials get a block too. These gates PASSED — including
# under DN2CPP_REQUIRE_ALL=1 — so the block sits among the greens rather than
# above the verdict; what it must not do is let a green suite imply that every
# section of every gate asserted. Each line names the gate and its stated reason.
N_EXPECTED=0
[ -s "$EXPECTEDFILE" ] && N_EXPECTED=$(wc -l < "$EXPECTEDFILE" | tr -d ' ')
if [ "$N_EXPECTED" -gt 0 ]; then
    echo ""
    printf '\033[1;36mⓘ %d gate(s) passed with a declared expected-partial section:\033[0m\n' "$N_EXPECTED"
    while IFS=$'\t' read -r n reason; do
        printf '\033[1;36m    %-42s %s\033[0m\n' "$n" "$reason"
    done < "$EXPECTEDFILE"
    echo "  Each is a permanent structural hole whose surface another gate asserts for"
    echo "  real; the reason at the gate's gate_expected_partial call names that gate."
    echo "  Reasons are abridged here — the full text is in $LOGDIR/<gate>.log."
fi
# Maintenance warnings (gate_warn) get a block too. These gates PASSED — in
# every mode, DN2CPP_REQUIRE_ALL=1 included — and a warning is not a coverage
# hole: every assert ran and held. What it names is upkeep that is due — a
# stale vendored input (the CA bundle's age), or a step whose cache key could
# not be computed because its transpile surface was unreadable — and
# the reason it must appear HERE is that both producers print live on every
# run precisely so somebody eventually acts: one line inside one log of a
# 150-gate suite reaches nobody. Neither is replayed from a cached green — the
# clock one runs ahead of the cache check, and the key one only fires on a run
# that recorded no key to replay.
N_WARNED=0
[ -s "$WARNFILE" ] && N_WARNED=$(wc -l < "$WARNFILE" | tr -d ' ')
if [ "$N_WARNED" -gt 0 ]; then
    echo ""
    printf '\033[1;33m⚠ %d gate(s) passed with a maintenance warning:\033[0m\n' "$N_WARNED"
    while IFS=$'\t' read -r n reason; do
        printf '\033[1;33m    %-42s %s\033[0m\n' "$n" "$reason"
    done < "$WARNFILE"
    echo "  A warning never fails the suite; it names upkeep that is due."
    echo "  Reasons are abridged here — the full text is in $LOGDIR/<gate>.log."
fi
if [ "$SKIP_GODOT" = "1" ]; then
    echo ""
    printf '\033[1;33m⊘ %d Godot gate(s) NOT RUN (SKIP_GODOT=1) — this is a smoke check, not the gate.\033[0m\n' \
        "${#GODOT_GATES[@]}"
fi

echo ""
if [ ${#FAILED[@]} -eq 0 ]; then
    if [ "$N_SKIPPED" -eq 0 ] && [ "$SKIP_GODOT" = "0" ]; then
        if [ "$N_EXPECTED" -gt 0 ]; then
            printf '\033[1;32m✔ All %d gates passed\033[0m\033[1;36m — %d with a declared expected-partial section (listed above).\033[0m\n' \
                "$TOTAL" "$N_EXPECTED"
        else
            printf '\033[1;32m✔ All %d gates passed.\033[0m\n' "$TOTAL"
        fi
    else
        # Never "all N passed" when some of the N never ran.
        printf '\033[1;32m✔ %d of %d gates passed\033[0m\033[1;33m — %d skipped (see above), 0 failed.\033[0m\n' \
            "$N_PASSED" "$TOTAL" "$N_SKIPPED"
    fi
    exit 0
else
    printf '\033[1;31m✘ %d of %d gates FAILED:\033[0m\n' "${#FAILED[@]}" "$TOTAL" >&2
    for n in "${FAILED[@]}"; do
        printf '\033[1;31m    %s\033[0m\n' "$n" >&2
    done
    echo "" >&2
    echo "View a failing gate's output with:  cat $LOGDIR/<gate-name>.log" >&2
    exit 1
fi
