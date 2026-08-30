#!/usr/bin/env bash
# pre-merge.sh — THE merge gate. The single canonical thing to run before a
# merge to `main`, so that "which two commands, under which config, with which
# environment" stops living in somebody's memory.
#
#     ./gates/pre-merge.sh                # the real thing (~1-2h, plus a self-host
#                                         # build or a fork-cache refresh when one is due)
#     ./gates/pre-merge.sh --dry-run      # print the exact runs, execute nothing
#     ./gates/pre-merge.sh --keep-going   # run Debug even after Release fails
#     DN2CPP_PREMERGE_SELFTEST=1 ./gates/pre-merge.sh   # self-test, no suite run
#
# WHAT IT RUNS, AND WHY EXACTLY THIS. One harness, the inputs the suites cannot
# run without, and two full suites, in this order:
#
#   0. gates/verify-culture-invariance.sh
#   1. gates/selfhost-emit.sh — only when the binary no longer describes src/
#   2. gates/setup-godot-fork.sh (+ gates/setup-godot-fork-web.sh per flavor) —
#      only when the fork cache no longer describes ../godot-dn2cpp
#   3. CONFIG=Release  DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
#   4. CONFIG=Debug    DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
#
#   - REQUIRE_ALL, because "all N gates passed" must mean all N *ran*. Without
#     it a machine missing a prerequisite reports green over a hole, which is
#     the shape that let the mono-module lane ship red on `main` for a week.
#   - GATE_CACHE=0, because the cache key is Release-flavoured: a Debug run over
#     a warm cache serves Release-keyed greens and the Debug-only shared-generics
#     backstop (CppEmitter.AssertSharedBodySymbols) never arms. The Debug suite
#     is the ONLY run that asserts that backstop across the whole corpus, so a
#     cached Debug suite is a Debug suite that did not happen.
#   - Both configs, because a regression that lets a shared canonical body name a
#     grouped instantiation's vt_/rgctx_/sf_ symbol is red only in Debug.
#   - Release first: it is the configuration everything else in the tree is
#     measured in, so its failures are the ones with the most context attached.
#   - The culture harness FIRST, and it is the one exception to the junk-drawer
#     rule below. It answers a pass/fail question no suite run can (whether a
#     bucket's verdict is a property of the developer's locale), it is minutes
#     against hours, and — this is what makes it belong here rather than in the
#     suite — the property changes only when somebody adds a bucket or a
#     section, which is exactly the event a merge is. Both sides of every gate
#     read the host's locale, so the exposure includes the freeze
#     and subset buckets whose fixed fixture cannot move with it. Running it
#     first is cheap fail-fast: a bucket missing its pin is found before the
#     first suite starts rather than on top of two hours of green ones.
#     It exits 77 when the HOST cannot decide the question (Windows ignores
#     LANG), which is reported as not-performed rather than failed — see the
#     refusal at its site.
#   - The self-host build is here as an INPUT the suites require, not as a
#     verdict of its own: godot_fork_preflight (gates/_godot_fork.sh) demands
#     artifacts/selfhost-fullcli/dn2cpp and the src stamp beside it before every
#     fork-lane gate. Without the binary they gate_skip, which DN2CPP_REQUIRE_ALL=1
#     turns into a failure; with a stale one they FAIL outright — and either way
#     the news arrives hours into the run. It builds only when the stamp no longer
#     matches src_tree_hash, so a tree whose binary is current pays nothing here.
#   - The fork cache is here for the SAME reason and on the same terms, and it is
#     what makes this script the ONE thing to run after updating ../godot-dn2cpp
#     rather than the second thing. godot_fork_preflight demands a cache that
#     describes the fork worktree as it is now, and a stale one is worse than an
#     absent one: godot_fork_ctx's keys fingerprint the stale editor and
#     GodotTools.dll THEMSELVES, so nothing moves and the fork gates replay a warm
#     green over an editor that does not contain the edit under test. It refreshes
#     only when a stamp disagrees, so a box whose cache is current pays a couple
#     of tree hashes.
#     The Web templates ride along because DN2CPP_REQUIRE_ALL=1 makes their
#     absence a failure rather than a skip, and the Emscripten SDK they need is
#     unpacked here (gates/setup-emsdk.sh) when no mutable one resolves. The iOS
#     template stays manual because its repair consumes Xcode and an official
#     templates archive; its presence, provenance and host prerequisites are
#     checked up front, so REQUIRE_ALL does not discover the manual work inside
#     the suite. A HOST prerequisite this box lacks (no Xcode, no emcc even after
#     the unpack) is not a refusal: the suites still run, the affected gates are
#     red under REQUIRE_ALL, and the verdict lists the gaps. A suite red ONLY
#     for such gates does not withhold the other configuration either; a suite
#     with any other failure does. Only a repairable ARTIFACT — a stale or
#     missing zip on a host that could build it — refuses.
#
# WHY THIS IS NOT A GATE. It runs the suite — twice. It lives beside
# gates/verify-locks.sh and gates/measure-*.sh, outside the build-and-run-*.sh
# glob the runner globs, for the obvious reason that a gate which runs the whole
# gate suite does not terminate.
#
# WHAT IT DELIBERATELY DOES NOT RUN, so nobody grows it into a junk drawer:
# gates/verify-locks.sh, gates/selfhost-measure*.sh, gates/selfhost-emit-console.sh,
# gates/measure-*.sh, dist/smoke-test.sh, dist/nuget-smoke-test.sh. Each is a
# manual AID for a surface some gate already covers, or a measurement with no
# pass/fail verdict; AGENTS.md names them at the change that obliges them. A merge
# gate whose runtime doubles for things that answer no pass/fail question is a
# merge gate people stop running. The test that admitted
# verify-culture-invariance.sh and keeps those out is not "is it useful" — it is
# whether the surface is one NO suite run covers: verify-locks.sh exercises the
# lock machinery the suite itself runs under every time, and the console selfhost
# harness emits what the suite's own gates transpile through, whereas nothing in
# either suite can observe that a bucket's green was a fact about ja-JP.
# gates/selfhost-emit.sh, the two gates/setup-godot-fork*.sh aids and
# gates/setup-emsdk.sh are in on a different test again, and they are the only
# things that may be: they answer no verdict at all — they produce the files the
# suites refuse to run without. setup-emsdk.sh qualifies through the Web
# templates: without the SDK they cannot be baked, and the suites refuse to run
# without them. That is the whole entry condition, and "it is useful" is not it:
# dist/smoke-test.sh is useful and stays out, because the suite already covers
# what it looks at.
#
# WHY IT RE-DERIVES THE VERDICT INSTEAD OF TRUSTING THE EXIT CODE. The runner's
# exit code and this script's verdict are not the same question, and the gap is
# not hypothetical:
#
#   - DN2CPP_REQUIRE_ALL=1 is enforced INSIDE gate_skip (_common.sh), not by the
#     runner. run_gate treats ANY exit 77 as a skip and returns 0 for it,
#     whatever the mode. A gate that exits 77 without going through gate_skip —
#     a bare `exit 77`, a helper that forwards a child's status — is therefore
#     recorded as a skip in a REQUIRE_ALL run that still exits 0. Asserting
#     _skips.txt is empty is an independent check, not a restatement.
#   - GATE_CACHE=0 is honoured by each gate's cache helper, not by the runner.
#     Asserting no gate reported `cached` is what proves the cache was really
#     off — i.e. that the Debug run actually compiled anything.
#   - A gate that never produced a timing line at all (a chain that died between
#     gates) is caught by the runner's audit_chain only if the chain left no
#     sentinel. Asserting the timing count equals the gate count closes it from
#     the other side.
#
# IT WARNS ABOUT STALE CMAKE BUILD DIRS BEFORE THE FIRST SUITE, because a stale
# cache fails gates in a shape that reads as a regression in the very diff you
# are about to merge — the one moment where a false positive is most expensive
# and least likely to be doubted. Two forms, and they are not equally covered
# elsewhere:
#
#   - A cache carried along by a MOVED repository (or copied between worktrees)
#     still names the old source tree. CMake refuses such a dir loudly, and
#     _cmake_configure's stamp (gates/_common.sh) normally throws it away first,
#     because the stamp records the absolute -B path. So this check is a net
#     under a mechanism that lives in another file, and it is what covers the
#     build dirs nothing configures through that helper.
#   - A cache carrying an option()'s PRE-FLIP DEFAULT. This one nothing else
#     catches: `option()` never overwrites a value already in the cache, so
#     flipping a default in runtime/CMakeLists.txt leaves every warm build dir
#     on the old value — and the configure stamp cannot see it, because the
#     configure COMMAND did not change. Measured: re-running the identical
#     configure over a warm dir after flipping a default keeps the old value.
#     Detecting it needs the stamp, to tell a deliberate -D override on an axis
#     dir (-DDN2CPP_USE_CURL=OFF is what the nocurl axis IS) from a freeze; a
#     dir with no stamp is left alone, since _cmake_configure reconfigures those
#     from scratch on first use anyway.
#
# It WARNS and does not delete. A cold reconfigure is a ~20-minute rebuild, and
# a merge gate that imposes one unasked is a merge gate people stop running —
# which costs more than the false positive it would prevent. It also never
# changes the verdict: a stale dir is a thing to know before a two-hour run, not
# a reason to refuse one. A green run whose tree had warnings says so in its
# receipt, so the claim "this commit passed" carries what it passed over.

set -uo pipefail

REPO=$(cd "$(dirname "$0")/.." && pwd)

# ── the freshness probes (children of this script, never a source) ────────────
# The predicates are selfhost_bin_fresh and godot_fork_cache_fresh
# (gates/_common.sh, gates/_godot_fork.sh), and reaching either means BEING the
# script that sources _common.sh: it cd's to its sourcer's parent and turns on
# `set -euo pipefail`, so a `bash -c` sourcing it dies on its own `set -u` and
# this script must not inherit either option. Hence a child of itself in probe
# mode. Ahead of the self-test block on purpose: the self-test spawns both modes.
#
# `=1` is the SELF-HOST probe and its output line is FROZEN — `<0|1>
# <src-tree-hash> <binary path>` — because premerge_selfhost_state reads it
# positionally and the self-test asserts the shape. `=fork` is the FORK-CACHE
# probe: `<0|1|2|3> <fork-head> <engine-hash> <tools-hash> <reason…>` — 1 stale,
# 2 no cache on this box, 3 this OS has no arm for the lane. Four answers and
# not two because the phase acts differently on each: refresh, build, refuse.
# Collapsing 2 into 1 would report a cold build as a refresh, and collapsing 3
# into 2 would start a build that cannot succeed.
if [ "${DN2CPP_PREMERGE_PROBE:-0}" = "1" ]; then
    source "$REPO/gates/_common.sh"
    probe_rc=0
    selfhost_bin_fresh || probe_rc=1
    printf '%s %s %s\n' "$probe_rc" "$SELFHOST_SRC_NOW" "$SELFHOST_BIN_PATH"
    exit 0
fi
if [ "${DN2CPP_PREMERGE_PROBE:-0}" = "fork" ]; then
    source "$REPO/gates/_common.sh"
    source "$REPO/gates/_godot_fork.sh"
    probe_rc=0
    FORK_CACHE_HEAD=""
    FORK_CACHE_ENGINE_NOW=""
    FORK_CACHE_TOOLS_NOW=""
    case "${DN2CPP_OS:-}" in
        macos|windows|linux) ;;
        *)
            # 3, and the OS list must track gates/setup-godot-fork.sh's per-OS
            # arms exactly — godot_fork_preflight's rule: anywhere else "run
            # gates/setup-godot-fork.sh" is an instruction that cannot succeed,
            # and the phase reading this has to refuse instead of starting a build.
            probe_rc=3
            FORK_CACHE_WHY="the Godot editor-export fork lane has no ${DN2CPP_OS:-non-desktop} arm"
            ;;
    esac
    if [ "$probe_rc" = "0" ] && ! godot_fork_cache_complete; then
        probe_rc=2
        FORK_CACHE_WHY="no fork cache at $FORK_ROOT"
        # An absent cache still has a fork it WOULD describe, and the phase prints
        # it: "building a cache for <head>" is a different line from "building a
        # cache", and only one of them lets a reader notice the wrong worktree
        # before the scons run rather than after it.
        if godot_fork_resolve >/dev/null 2>&1; then
            FORK_CACHE_HEAD="$(git -C "$FORK" rev-parse --short HEAD 2>/dev/null || true)"
        fi
    elif [ "$probe_rc" = "0" ] && ! godot_fork_cache_fresh; then
        probe_rc=1
    fi
    # The reason is LAST and unquoted on purpose: it is prose with spaces in it,
    # and the reader (premerge_fork_state) takes it as the line's remainder.
    printf '%s %s %s %s %s\n' "$probe_rc" "${FORK_CACHE_HEAD:-unknown}" \
        "${FORK_CACHE_ENGINE_NOW:-unknown}" "${FORK_CACHE_TOOLS_NOW:-unknown}" \
        "${FORK_CACHE_WHY:-current}"
    exit 0
fi
# `=forkweb` names the Web template FLAVORS that have to be baked: `ok` alone when
# both are current, `ok <flavor…>` otherwise. The `ok` is load-bearing — it is how
# the reader tells "both current" from "the probe could not answer", which must
# mean bake, not skip.
#
# A third probe rather than more fields on `=fork` because it asks about a
# different artifact: the two zips can be stale while the desktop cache is current
# (an interrupted re-bake) and current while it is stale (an engine edit not yet
# built), so folding them into one answer would let either state hide the other.
if [ "${DN2CPP_PREMERGE_PROBE:-0}" = "forkweb" ]; then
    source "$REPO/gates/_common.sh"
    source "$REPO/gates/_godot_fork.sh"
    # No fork worktree, no provenance to compare against. Silence, not `ok`: the
    # reader then bakes, which is the safe half — and the `=fork` probe has
    # already made this run refuse, so nothing reaches the bake anyway.
    godot_fork_resolve >/dev/null 2>&1 || exit 0
    # The builder resolves the unpacked, mutable SDK rather than accepting the
    # frozen one bundled for exports. Use that exact resolver here: ambient
    # `command -v emcc` can name another SDK and make the probe disagree with the
    # build it decides whether to run.
    dn2cpp_emsdk_resolve --no-bundled >/dev/null 2>&1 || exit 0
    command -v emcc >/dev/null 2>&1 || exit 0
    probe_emcc="$(first_line "$(emcc --version 2>/dev/null)")"
    [ -n "$probe_emcc" ] || exit 0
    BASE_COMMIT="$(cat "$FORK_PIN_EXPECTED")"
    probe_want="$(godot_fork_engine_provenance)"
    probe_stale=""
    for probe_flavor in stock cri; do
        case "$probe_flavor" in
            stock)
                probe_release_zip="$FORK_ROOT/web_template.zip"
                probe_debug_zip="$FORK_ROOT/web_template_debug.zip"
                probe_release_emcc_stamp="$FORK_ROOT/web_emcc.txt"
                probe_debug_emcc_stamp="$FORK_ROOT/web_emcc_debug.txt"
                ;;
            cri)
                probe_release_zip="$FORK_ROOT/web_template_cri.zip"
                probe_debug_zip="$FORK_ROOT/web_template_cri_debug.zip"
                probe_release_emcc_stamp="$FORK_ROOT/web_emcc_cri.txt"
                probe_debug_emcc_stamp="$FORK_ROOT/web_emcc_cri_debug.txt"
                ;;
        esac
        if ! godot_fork_web_template_pair_fresh \
                "$probe_release_zip" "$probe_debug_zip" "$probe_flavor" \
                "$probe_release_emcc_stamp" "$probe_debug_emcc_stamp" \
                "$probe_emcc" "$probe_want"; then
            probe_stale="$probe_stale $probe_flavor"
        fi
    done
    printf 'ok%s\n' "$probe_stale"
    exit 0
fi
# `=forkios` is a readiness probe, not a builder. The iOS template needs Xcode and
# an official templates archive, so pre-merge never creates it; it does ensure a
# REQUIRE_ALL run does not discover its absence or stale provenance hours later.
# A `bad` answer carries the KIND godot_fork_ios_ready distinguishes — `artifact`
# (repairable here) or `prerequisite` (this host cannot) — because the reader
# refuses on one and continues red on the other.
if [ "${DN2CPP_PREMERGE_PROBE:-0}" = "forkios" ]; then
    source "$REPO/gates/_common.sh"
    source "$REPO/gates/_godot_fork.sh"
    godot_fork_resolve >/dev/null 2>&1 \
        || { printf 'bad artifact the fork worktree does not resolve\n'; exit 0; }
    BASE_COMMIT="$(cat "$FORK_PIN_EXPECTED")"
    probe_ios="$FORK_ROOT/ios_template.zip"
    if ! godot_fork_ios_ready "$probe_ios"; then
        printf 'bad %s %s\n' "${FORK_IOS_READY_KIND:-artifact}" "$FORK_IOS_READY_WHY"
        exit 0
    fi
    printf 'ok current\n'
    exit 0
fi

DRY_RUN=0
KEEP_GOING=0
for arg in "$@"; do
    case "$arg" in
        --dry-run)    DRY_RUN=1 ;;
        --keep-going) KEEP_GOING=1 ;;
        -h|--help)
            sed -n '2,10p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        *)
            echo "error: unknown argument: $arg (see --help)" >&2
            exit 2
            ;;
    esac
done

# One log root per repository directory, so two worktrees pre-merging at once do
# not destroy each other's logs (run-all-gates.sh wipes its LOGDIR at start and
# refuses a second runner sharing one).
LOGROOT=${DN2CPP_PREMERGE_LOGROOT:-/tmp/dn2cpp-premerge-$(basename "$REPO")}
RECEIPT="$LOGROOT/_receipt.txt"
TRANSCRIPT="$LOGROOT/_premerge.log"

# The whole run, on disk. Phase lines and the verdict live nowhere else — the
# per-gate logs are the runner's — so a scrollback that is lost or overwritten
# takes two hours of record with it.
#
# A pipeline and not `exec > >(tee …)`: a process substitution is not waited for
# at exit, so the tail of the run — which is the verdict — can be dropped.
# Three modes stay out: --dry-run, whose whole contract is to leave nothing
# behind; the probe, whose stdout IS its return value; and the self-test, which
# spawns runs of its own and would bury LOGROOT under theirs.
if [ "$DRY_RUN" = "0" ] && [ "${DN2CPP_PREMERGE_SELFTEST:-0}" != "1" ] \
   && [ "${DN2CPP_PREMERGE_TRANSCRIPT:-0}" != "1" ]; then
    mkdir -p "$LOGROOT"
    DN2CPP_PREMERGE_TRANSCRIPT=1 bash "$0" "$@" 2>&1 | tee "$TRANSCRIPT"
    exit "${PIPESTATUS[0]}"
fi

# The runner's own TOTAL: every build-and-run-*.sh, Godot gates included.
EXPECTED_GATES=$(ls "$REPO"/gates/build-and-run-*.sh 2>/dev/null | wc -l | tr -d ' ')

say()  { printf '\n\033[1;36m══ %s\033[0m\n' "$*"; }
good() { printf '\033[1;32m✔ %s\033[0m\n' "$*"; }
bad()  { printf '\033[1;31m✘ %s\033[0m\n' "$*" >&2; }
warn() { printf '\033[1;33m⚠ %s\033[0m\n' "$*"; }
note() { printf '  %s\n' "$*"; }

# ── stale CMake build dirs (warning only — see the header) ───────────────────

# The stamp file name belongs to gates/_common.sh; read it from there rather
# than keeping a second copy of the string, and fall back to the name it has
# today for a tree that has no _common.sh (the self-test's synthetic repos).
# A self-test asserts the read succeeds against the real _common.sh, so a
# rename there fails here loudly instead of quietly disabling the option check.
_premerge_stamp_name() {
    local n=""
    [ -f "$1/gates/_common.sh" ] && n=$(sed -n 's/^_CMAKE_CONFIGURE_STAMP=//p' "$1/gates/_common.sh")
    n=${n%%$'\n'*}
    printf '%s' "${n:-.dn2cpp-configure-stamp}"
}

# CMake's truthiness, narrowed to what a cache entry can hold. Compared
# normalized so a cache written as `1` never reads as a flip of a default
# written `ON`. Answers in _PCB_VALUE and folds case through `nocasematch`
# rather than `tr`: this sits in the innermost loop of a scan over every build
# dir, where a subshell per question is the whole cost of the scan.
_premerge_cmake_bool() {
    shopt -s nocasematch
    case "$1" in
        1|on|true|yes|y)                      _PCB_VALUE=ON ;;
        0|off|false|no|n|ignore|''|*notfound)  _PCB_VALUE=OFF ;;
        *)                                    _PCB_VALUE="$1" ;;
    esac
    shopt -u nocasematch
}

# _premerge_lookup NAME TABLE — sets _PL_VALUE to the LAST "NAME VALUE" line's
# value, empty when absent. `##` is greedy from the left, which is what makes
# the last -D of a configure command the one that counts.
_premerge_lookup() {
    local t=$'\n'"$2"
    case "$t" in
        *$'\n'"$1 "*) _PL_VALUE=${t##*$'\n'"$1 "}; _PL_VALUE=${_PL_VALUE%%$'\n'*} ;;
        *)            _PL_VALUE="" ;;
    esac
}

_premerge_nlines() { printf '%s' "$1" | grep -c . 2>/dev/null || true; }

# premerge_cmake_cache_warn REPO — warn about build dirs under REPO/artifacts/
# whose CMakeCache.txt no longer matches this tree. Deletes nothing, returns 0
# always, and sets PREMERGE_STALE to the number of build dirs warned about (the
# receipt reads it). Scans the per-axis runtime dirs (artifacts/.cmake-*) AND
# the per-gate app dirs (artifacts/<gate>/.cmake*): the option freeze reaches
# both, and the app dirs are the ones that feel trustworthy because they are
# reconfigured on every call — which does not help, since a reconfigure is
# exactly what does not correct a cached option.
premerge_cmake_cache_warn() {
    local repo="$1"
    PREMERGE_STALE=0
    local art="$repo/artifacts"
    if [ ! -d "$art" ]; then
        note "no artifacts/ in this tree — no build dir can be stale."
        return 0
    fi

    local repo_phys
    repo_phys=$(cd "$repo" 2>/dev/null && pwd -P) || repo_phys="$repo"
    local stampname
    stampname=$(_premerge_stamp_name "$repo")

    # This tree's option() defaults, one "NAME VALUE" per line. Parsed rather
    # than listed, so an option added upstream is covered the day it lands.
    local defaults=""
    [ -f "$repo/runtime/CMakeLists.txt" ] && defaults=$(sed -n \
        's/^[[:space:]]*option([[:space:]]*\([A-Za-z_][A-Za-z0-9_]*\)[[:space:]]*"[^"]*"[[:space:]]*\([A-Za-z0-9_]*\)[[:space:]]*).*$/\1 \2/p' \
        "$repo/runtime/CMakeLists.txt")

    # bash 3.2: no associative arrays, and `${#a[@]}` on an empty array is an
    # error under `set -u`. Both accumulators are newline-separated strings.
    local moved="" frozen="" stale_dirs="" n_dirs=0 n_stamped=0
    local caches=() c dir rel home home_phys stamp entries stampopts
    local name want cached cbool wbool
    for c in "$art"/.cmake*/CMakeCache.txt "$art"/*/.cmake*/CMakeCache.txt; do
        [ -f "$c" ] || continue
        caches[$n_dirs]="$c"
        n_dirs=$((n_dirs + 1))
    done

    if [ "$n_dirs" -eq 0 ]; then
        note "no configured CMake build dir under artifacts/ — nothing can be stale."
        return 0
    fi
    # The scan reads every one of them before it can say anything, so name the
    # size first: silence for the length of a scan reads as a hung run.
    note "scanning $n_dirs CMakeCache.txt under artifacts/ ..."

    for c in "${caches[@]}"; do
        dir=${c%/*}
        rel=${dir#"$repo"/}
        # ONE sed per cache file, feeding the pure-bash lookup above. Per-lookup
        # sed is a subshell per (build dir × option), which is the whole cost of
        # this scan and long enough to look like a hang.
        entries=$(LC_ALL=C sed -n \
            -e 's/^CMAKE_HOME_DIRECTORY:INTERNAL=/CMAKE_HOME_DIRECTORY /p' \
            -e 's/^\([A-Za-z_][A-Za-z0-9_]*\):BOOL=/\1 /p' "$c")
        _premerge_lookup CMAKE_HOME_DIRECTORY "$entries"
        home=$_PL_VALUE
        # The test is "names a source dir INSIDE this repository", not "equals
        # $repo/runtime": a build dir may legitimately be configured from
        # another source dir in the tree (the P/Invoke gates build
        # samples/native/dn2cpptest into their own OUT/.cmake-testlib), and
        # what makes a dir stale is that its source tree is somewhere else.
        #
        # Compared as RESOLVED paths, never as strings: /tmp is a symlink to
        # /private/tmp on macOS, a worktree may be reached through one, and a
        # recorded path that no longer resolves at all is stale by definition
        # (which is what a repository move leaves behind).
        home_phys=""
        [ -n "$home" ] && [ -d "$home" ] && home_phys=$(cd "$home" && pwd -P)
        case "$home_phys" in
            "$repo_phys"|"$repo_phys"/*) ;;
            *)
                moved="$moved$rel|$home
"
                stale_dirs="$stale_dirs$dir
"
                continue
                ;;
        esac
        stamp="$dir/$stampname"
        [ -f "$stamp" ] || continue
        n_stamped=$((n_stamped + 1))
        # Every -D of the recorded configure command in one pass; the lookup's
        # last-wins rule is what carries `tail -1` across.
        stampopts=$(LC_ALL=C sed -n 's/^-D\([A-Za-z_][A-Za-z0-9_]*\)=/\1 /p' "$stamp")
        while read -r name want; do
            [ -n "$name" ] || continue
            _premerge_lookup "$name" "$entries"
            cached=$_PL_VALUE
            [ -n "$cached" ] || continue
            # An explicit -D in the recorded configure command IS the intent;
            # only where the configure said nothing does the CMakeLists default
            # get to be the expected value.
            _premerge_lookup "$name" "$stampopts"
            [ -n "$_PL_VALUE" ] && want="$_PL_VALUE"
            _premerge_cmake_bool "$cached"
            cbool=$_PCB_VALUE
            _premerge_cmake_bool "$want"
            wbool=$_PCB_VALUE
            if [ "$cbool" != "$wbool" ]; then
                frozen="$frozen$name $cached $want $rel
"
                stale_dirs="$stale_dirs$dir
"
            fi
        done <<OPTIONS
$defaults
OPTIONS
    done

    if [ -n "$moved" ]; then
        while IFS='|' read -r rel home; do
            [ -n "$rel" ] || continue
            warn "$rel was configured for a DIFFERENT source tree."
            note "  its CMakeCache.txt names: ${home:-<no CMAKE_HOME_DIRECTORY entry>}"
            note "  this repository is:       $repo"
        done <<MOVED
$moved
MOVED
    fi

    if [ -n "$frozen" ]; then
        local key count examples n_more
        while read -r name cached want; do
            [ -n "$name" ] || continue
            key=$(printf '%s' "$frozen" | awk -v n="$name" -v c="$cached" -v w="$want" \
                '$1==n && $2==c && $3==w { print $4 }')
            count=$(_premerge_nlines "$key")
            examples=$(tr '\n' ' ' <<<"$(head -3 <<<"$key")")
            n_more=$((count - 3))
            [ "$n_more" -gt 0 ] && examples="$examples(+$n_more more)"
            warn "$name is cached $cached in $count build dir(s); this tree wants $want."
            note "  option() never overwrites a cached value, so a warm build dir keeps"
            note "  the default it was first configured with, and the configure stamp"
            note "  cannot see it — the configure command did not change."
            note "  $examples"
        done <<KEYS
$(printf '%s' "$frozen" | awk '{ print $1, $2, $3 }' | sort -u)
KEYS
    fi

    stale_dirs=$(printf '%s' "$stale_dirs" | sort -u)
    PREMERGE_STALE=$(_premerge_nlines "$stale_dirs")

    if [ "$PREMERGE_STALE" -eq 0 ]; then
        note "$n_dirs CMake build dir(s) under artifacts/ ($n_stamped stamped), none stale."
        return 0
    fi

    printf '\n'
    note "A stale build dir fails gates in a shape that reads as a regression in"
    note "the diff you are about to merge. Nothing above was deleted for you: a"
    note "cold reconfigure is a ~20-minute rebuild, and a merge gate that imposes"
    note "one unasked is a merge gate people stop running. To clear them:"
    if [ "$PREMERGE_STALE" -le 8 ]; then
        note "  rm -rf $(printf '%s' "$stale_dirs" | tr '\n' ' ')"
    else
        note "  rm -rf $repo/artifacts/.cmake-* $repo/artifacts/*/.cmake*"
    fi
    note "This run continues either way; the warning does not change the verdict."
    return 0
}

# ── the two runs, described in exactly one place ─────────────────────────────

# premerge_argv CONFIG LOGDIR — fills PREMERGE_ARGV with the command line for
# one of the two runs. --dry-run prints this array and the real path EXECUTES
# it, so what is printed cannot drift from what runs. bash 3.2: plain array.
premerge_argv() {
    local config="$1" logdir="$2"
    PREMERGE_ARGV=()
    # A suite is one to two hours. On macOS a sleeping machine turns that into a
    # run that never finishes and a person who blames the suite; caffeinate -i
    # only defers idle sleep, and it is shown in --dry-run so it is never a
    # hidden moving part. DN2CPP_PREMERGE_NO_CAFFEINATE=1 opts out.
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_ARGV+=(caffeinate -i)
    fi
    PREMERGE_ARGV+=(
        env
        "CONFIG=$config"
        "LOGDIR=$logdir"
        "DN2CPP_REQUIRE_ALL=1"
        "DN2CPP_GATE_CACHE=0"
        bash "$REPO/gates/run-all-gates.sh"
    )
}

# premerge_culture_argv — the culture harness's command line, in the same shape
# and for the same reason as premerge_argv: --dry-run prints THIS array and the
# real path executes it.
premerge_culture_argv() {
    PREMERGE_CULTURE_ARGV=()
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_CULTURE_ARGV+=(caffeinate -i)
    fi
    PREMERGE_CULTURE_ARGV+=(bash "$REPO/gates/verify-culture-invariance.sh")
}

# premerge_selfhost_argv — the self-host build's command line, same shape and
# same reason as the two above.
premerge_selfhost_argv() {
    PREMERGE_SELFHOST_ARGV=()
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_SELFHOST_ARGV+=(caffeinate -i)
    fi
    PREMERGE_SELFHOST_ARGV+=(bash "$REPO/gates/selfhost-emit.sh")
}

# premerge_selfhost_state — ask the probe above, into SELFHOST_FRESH (0 reuse,
# 1 rebuild), SELFHOST_SRC (the tree hash the answer was taken against) and
# SELFHOST_BIN (the file the predicate looked at). A probe that cannot answer at
# all means rebuild: the phase this feeds must never omit a build on a guess.
premerge_selfhost_state() {
    local out="" fresh="" src="" bin=""
    out=$(DN2CPP_PREMERGE_PROBE=1 bash "$REPO/gates/pre-merge.sh" 2>/dev/null) || out=""
    read -r fresh src bin <<<"$out" || :
    SELFHOST_FRESH=${fresh:-1}
    SELFHOST_SRC=${src:-unknown}
    SELFHOST_BIN=${bin:-artifacts/selfhost-fullcli/dn2cpp}
}

# premerge_fork_argv — the fork-cache setup's command line, same shape and same
# reason as the three above. The desktop aid only: the Web arm is a separate argv
# because it is conditional on a different artifact and needs a different
# environment.
premerge_fork_argv() {
    PREMERGE_FORK_ARGV=()
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_FORK_ARGV+=(caffeinate -i)
    fi
    PREMERGE_FORK_ARGV+=(bash "$REPO/gates/setup-godot-fork.sh")
}

# premerge_fork_web_argv FLAVOR — the Web template bake for `stock` or `cri`. The
# flavor is an ENV variable to the same script (CRI=1), not an argument, because
# that is the interface gates/setup-godot-fork-web.sh publishes and docs/RELEASE.md
# drives it by.
premerge_fork_web_argv() {
    PREMERGE_FORK_WEB_ARGV=()
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_FORK_WEB_ARGV+=(caffeinate -i)
    fi
    # CRI is pinned in BOTH arms, never merely set in one: a shell that exported
    # CRI=1 for a hand-run bake would otherwise leak it into the stock bake, which
    # then publishes a CRI-glued zip under the stock name — the one mislabel
    # godot_fork_web_template_flavor exists to refuse downstream.
    if [ "$1" = "cri" ]; then
        PREMERGE_FORK_WEB_ARGV+=(env "CRI=1")
    else
        PREMERGE_FORK_WEB_ARGV+=(env "CRI=0")
    fi
    PREMERGE_FORK_WEB_ARGV+=(bash "$REPO/gates/setup-godot-fork-web.sh")
}

# premerge_fork_state — ask the fork probe, into FORK_STATE (0 current, 1 stale,
# 2 absent), FORK_HEAD_SHORT, FORK_ENGINE, FORK_TOOLS and FORK_WHY. A probe that
# cannot answer at all means STALE rather than absent: premerge_selfhost_state's
# rule — never omit work on a guess — plus the narrower reading of the two, since
# "absent" would let the phase claim it built a cache from nothing.
premerge_fork_state() {
    local out="" st="" head="" eng="" tools="" why=""
    out=$(DN2CPP_PREMERGE_PROBE=fork bash "$REPO/gates/pre-merge.sh" 2>/dev/null) || out=""
    read -r st head eng tools why <<<"$out" || :
    FORK_STATE=${st:-1}
    FORK_HEAD_SHORT=${head:-unknown}
    FORK_ENGINE=${eng:-unknown}
    FORK_TOOLS=${tools:-unknown}
    FORK_WHY=${why:-the fork-cache probe returned nothing}
}

# premerge_fork_web_state — which Web template flavors this run has to bake, as a
# space-separated list in FORK_WEB_STALE (empty when both are current).
#
# Absent and stale are ONE answer here, unlike the desktop cache. Under
# DN2CPP_REQUIRE_ALL=1 a missing Web template is not a skip — gate_skip converts
# to a failure — and a present-but-stale one is godot_fork_template_check's hard
# refusal. So both zips have to exist AND be stamped with the engine provenance
# before the suite starts, and there is no third outcome to distinguish.
#
# The comparison is godot_fork_engine_provenance, the same string
# godot_fork_template_check demands, asked through the probe rather than here:
# this function holds no $FORK.
premerge_fork_web_state() {
    local out="" tag=""
    out=$(DN2CPP_PREMERGE_PROBE=forkweb bash "$REPO/gates/pre-merge.sh" 2>/dev/null) || out=""
    read -r tag FORK_WEB_STALE <<<"$out" || :
    if [ "${tag:-}" != "ok" ]; then
        # The probe did not answer. Bake both rather than believe nothing is due:
        # premerge_selfhost_state's rule — this phase must never omit work on a
        # guess — and here the guess would be the one that lets the suite start.
        FORK_WEB_STALE="stock cri"
        FORK_WEB_SDK=0
        return
    fi
    FORK_WEB_SDK=1
    FORK_WEB_STALE=${FORK_WEB_STALE:-}
}

# premerge_fork_ios_state — 0 when the manually-built iOS template and its host
# prerequisites are ready, 1 with FORK_IOS_KIND (prerequisite|artifact) and
# FORK_IOS_WHY otherwise. A probe that did not answer is read as `artifact`, the
# refusing side: continuing on a guess is the failure this phase exists to avoid.
premerge_fork_ios_state() {
    local out="" tag="" kind="" why=""
    out=$(DN2CPP_PREMERGE_PROBE=forkios bash "$REPO/gates/pre-merge.sh" 2>/dev/null) || out=""
    read -r tag kind why <<<"$out" || :
    FORK_IOS_KIND=${kind:-artifact}
    FORK_IOS_WHY=${why:-the iOS prerequisite probe returned nothing}
    [ "${tag:-}" = ok ]
}

# premerge_emsdk_argv — the pinned Emscripten SDK unpack, same shape and same
# reason as premerge_fork_argv. It is idempotent and exits early once unpacked,
# so running it is the cheap half of "does a mutable emcc resolve".
premerge_emsdk_argv() {
    PREMERGE_EMSDK_ARGV=()
    if [ "${DN2CPP_PREMERGE_NO_CAFFEINATE:-0}" != "1" ] && command -v caffeinate >/dev/null 2>&1; then
        PREMERGE_EMSDK_ARGV+=(caffeinate -i)
    fi
    PREMERGE_EMSDK_ARGV+=(bash "$REPO/gates/setup-emsdk.sh")
}

# ── the verdict, re-derived from the run's own artifacts ─────────────────────

# premerge_verdict LABEL RC LOGDIR EXPECTED_GATES — 0 when the run is a genuine
# green, 1 otherwise. Collects every problem rather than stopping at the first:
# a run this long should hand back everything it knows in one pass.
#
# A red run is also classified. VERDICT_PREREQ_ONLY=1 when every problem is a
# gate that called gate_skip under DN2CPP_REQUIRE_ALL=1 — its log carries
# gate_skip's fixed FAIL marker — with the failed gates in VERDICT_PREREQ_GATES.
# Such a run is still red (the merge needs a host that runs them), but it is not
# the caller's reason to withhold the other configuration: those gates cannot go
# green on this host whatever the tree says. A gate that failed for any other
# reason, a skip, a cached record or a missing record makes the run plainly red.
premerge_verdict() {
    local label="$1" rc="$2" logdir="$3" expected="$4"
    local timings="$logdir/_timings.txt"
    local skips="$logdir/_skips.txt"
    local fails="$logdir/_failures.txt"
    local bad_count=0 plain_red=0 n_fail=0 n_prereq=0
    local n_lines n_ran n_other
    VERDICT_PREREQ_ONLY=0
    VERDICT_PREREQ_GATES=""

    if [ "$rc" -ne 0 ]; then
        bad "$label: the runner exited $rc."
        bad_count=$((bad_count + 1))
    fi

    if [ ! -f "$timings" ]; then
        bad "$label: no $timings — the runner never reached its summary phase."
        bad_count=$((bad_count + 1))
        return 1
    fi

    if [ -s "$fails" ]; then
        n_fail=$(wc -l < "$fails" | tr -d ' ')
        bad "$label: $n_fail gate(s) FAILED:"
        # run_gate names the gate's log after the gate: $LOGDIR/<gate>.log. A
        # failure recorded without such a log (the runner's audit of gates that
        # left no record) has no marker and is plainly red.
        while IFS= read -r n; do
            if grep -q '^FAIL: prerequisite absent, and DN2CPP_REQUIRE_ALL=1 demands every gate run:' \
                "$logdir/$n.log" 2>/dev/null; then
                note "failed: $n (prerequisite absent)"
                n_prereq=$((n_prereq + 1))
                VERDICT_PREREQ_GATES="${VERDICT_PREREQ_GATES:+$VERDICT_PREREQ_GATES, }$n"
            else
                note "failed: $n"
                plain_red=1
            fi
        done < "$fails"
        bad_count=$((bad_count + 1))
    fi

    # Independent of the exit code on purpose — see the header. DN2CPP_REQUIRE_ALL
    # is enforced inside gate_skip, and the runner returns 0 for any exit 77.
    if [ -s "$skips" ]; then
        bad "$label: $(wc -l < "$skips" | tr -d ' ') gate(s) SKIPPED under DN2CPP_REQUIRE_ALL=1."
        note "A skip here means a gate exited 77 without passing through gate_skip,"
        note "which the runner records as a skip and still exits 0 for."
        while IFS= read -r line; do note "skipped: $line"; done < "$skips"
        bad_count=$((bad_count + 1))
    fi

    n_lines=$(wc -l < "$timings" | tr -d ' ')
    n_ran=$(grep -c ' ran$' "$timings" 2>/dev/null || true)
    [ -n "$n_ran" ] || n_ran=0
    n_other=$((n_lines - n_ran))
    if [ "$n_other" -ne 0 ]; then
        bad "$label: $n_other of $n_lines gate records are not 'ran' (cached, skipped or failed):"
        grep -v ' ran$' "$timings" | while IFS= read -r line; do note "$line"; done
        note "A 'cached' record under DN2CPP_GATE_CACHE=0 means the cache was not"
        note "actually off, so this configuration's asserts did not arm."
        bad_count=$((bad_count + 1))
    fi

    if [ "$n_lines" -ne "$expected" ]; then
        bad "$label: $n_lines gate records for $expected gates — $((expected - n_lines)) gate(s) left no record at all."
        bad_count=$((bad_count + 1))
    fi

    if [ "$bad_count" -ne 0 ]; then
        # Prerequisite-only means: the failed gates all hit gate_skip, no skip
        # or missing record, and the only non-'ran' records are those failures
        # (a cached record would make n_other exceed them). The runner's own
        # non-zero exit is the expected face of a failed gate, not extra news.
        if [ "$n_fail" -gt 0 ] && [ "$plain_red" -eq 0 ] && [ "$n_prereq" -eq "$n_fail" ] \
            && [ ! -s "$skips" ] && [ "$n_lines" -eq "$expected" ] && [ "$n_other" -eq "$n_fail" ]; then
            VERDICT_PREREQ_ONLY=1
            note "$label: prerequisite-only failures: $n_fail — every failed gate called gate_skip under REQUIRE_ALL."
        fi
        return 1
    fi
    good "$label: $n_lines/$expected gates ran and passed, 0 skipped, 0 cached."
    return 0
}

# ── self-test ────────────────────────────────────────────────────────────────
# Exercises the REAL premerge_argv and premerge_verdict against synthetic run
# artifacts. Nothing here is a stand-in for either function; the fixtures stand
# in for the run, which is the only part that cannot be conjured in a second.
if [ "${DN2CPP_PREMERGE_SELFTEST:-0}" = "1" ]; then
    ST_PASS=0
    ST_FAIL=0
    st_ok()  { printf '  \033[32mPASS\033[0m %s\n' "$*"; ST_PASS=$((ST_PASS + 1)); }
    st_bad() { printf '  \033[31mFAIL\033[0m %s\n' "$*" >&2; ST_FAIL=$((ST_FAIL + 1)); }

    ST_TMP=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-premerge-selftest.XXXXXX")
    trap 'rm -rf "$ST_TMP"' EXIT

    # fixture DIR N_GATES VERDICT... — build a LOGDIR whose _timings.txt has one
    # line per VERDICT, and skips/failures files derived from those verdicts.
    fixture() {
        local dir="$1"; shift
        local i=0 v
        mkdir -p "$dir"
        : > "$dir/_timings.txt"
        : > "$dir/_skips.txt"
        : > "$dir/_failures.txt"
        for v in "$@"; do
            i=$((i + 1))
            printf 'gate%02d 7 %s\n' "$i" "$v" >> "$dir/_timings.txt"
            case "$v" in
                skipped) printf 'gate%02d\tno prerequisite\n' "$i" >> "$dir/_skips.txt" ;;
                failed)  printf 'gate%02d\n' "$i" >> "$dir/_failures.txt" ;;
            esac
        done
    }

    say "premerge_argv"

    premerge_argv Release /tmp/x-rel
    ARGV_REL="${PREMERGE_ARGV[*]}"
    premerge_argv Debug /tmp/x-dbg
    ARGV_DBG="${PREMERGE_ARGV[*]}"

    for tok in "CONFIG=Release" "LOGDIR=/tmp/x-rel" "DN2CPP_REQUIRE_ALL=1" "DN2CPP_GATE_CACHE=0" "gates/run-all-gates.sh"; do
        case "$ARGV_REL" in
            *"$tok"*) st_ok "Release argv carries $tok" ;;
            *)        st_bad "Release argv is missing $tok: $ARGV_REL" ;;
        esac
    done
    case "$ARGV_DBG" in
        *"CONFIG=Debug"*) st_ok "Debug argv carries CONFIG=Debug" ;;
        *)                st_bad "Debug argv is missing CONFIG=Debug: $ARGV_DBG" ;;
    esac
    # The two runs must differ in the config and the log dir and in nothing else:
    # a shared LOGDIR would have the second run wipe the first one's evidence.
    if [ "$(printf '%s\n' "$ARGV_REL" | sed 's#Release#@#; s#/tmp/x-rel#@@#')" \
       = "$(printf '%s\n' "$ARGV_DBG" | sed 's#Debug#@#; s#/tmp/x-dbg#@@#')" ]; then
        st_ok "the two argvs differ only in CONFIG and LOGDIR"
    else
        st_bad "the two argvs differ beyond CONFIG/LOGDIR:\n  $ARGV_REL\n  $ARGV_DBG"
    fi
    case "$ARGV_REL" in
        *SKIP_GODOT*) st_bad "argv sets SKIP_GODOT — it contradicts DN2CPP_REQUIRE_ALL=1" ;;
        *)            st_ok "argv sets no SKIP_GODOT" ;;
    esac

    say "premerge_verdict"

    # A verdict case: NAME EXPECT(ok|bad) RC N_EXPECTED VERDICT...
    st_case() {
        local name="$1" expect="$2" rc="$3" expected="$4"; shift 4
        local dir="$ST_TMP/$(printf '%s' "$name" | tr -c 'A-Za-z0-9' '_')"
        local got=ok
        fixture "$dir" "$@"
        premerge_verdict "$name" "$rc" "$dir" "$expected" >/dev/null 2>&1 || got=bad
        if [ "$got" = "$expect" ]; then
            st_ok "$name -> $got"
        else
            st_bad "$name -> $got (expected $expect)"
        fi
    }

    st_case "all-green"          ok  0 3 ran ran ran
    st_case "runner-exit-1"      bad 1 3 ran ran ran
    st_case "one-failed-gate"    bad 1 3 ran ran failed
    # The case REQUIRE_ALL is supposed to prevent, arriving anyway: the runner
    # recorded a skip and still exited 0.
    st_case "skip-with-exit-0"   bad 0 3 ran ran skipped
    st_case "cached-under-nocache" bad 0 3 ran cached ran
    st_case "short-of-gate-count"  bad 0 4 ran ran ran

    # Two more that the fixture shape cannot express.
    mkdir -p "$ST_TMP/no-timings"
    if premerge_verdict "no-timings" 0 "$ST_TMP/no-timings" 3 >/dev/null 2>&1; then
        st_bad "missing _timings.txt -> ok (expected bad)"
    else
        st_ok "missing _timings.txt -> bad"
    fi
    # Exit code 0 with a non-empty _failures.txt (the runner died before Phase 6).
    fixture "$ST_TMP/fails-but-zero" ran ran ran
    printf 'gateXX\n' >> "$ST_TMP/fails-but-zero/_failures.txt"
    if premerge_verdict "fails-but-zero" 0 "$ST_TMP/fails-but-zero" 3 >/dev/null 2>&1; then
        st_bad "non-empty _failures.txt with rc 0 -> ok (expected bad)"
    else
        st_ok "non-empty _failures.txt with rc 0 -> bad"
    fi

    say "end-to-end (this exact script, against a synthetic repo)"

    # The driver below — the loop, the fail-fast, the receipt — is the one part
    # the unit cases above cannot reach, and it is not mockable from inside
    # without putting a test-only seam in the production path. So run the REAL
    # script, unmodified, against a repo-shaped directory whose run-all-gates.sh
    # is a stub. Same move as verify-locks.sh running the real lock functions
    # against a private lock root: the subject is real, the surroundings are not.
    FAKE="$ST_TMP/fakerepo"
    mkdir -p "$FAKE/gates"
    cp "$0" "$FAKE/gates/pre-merge.sh"
    chmod +x "$FAKE/gates/pre-merge.sh"
    : > "$FAKE/gates/build-and-run-a.sh"
    : > "$FAKE/gates/build-and-run-b.sh"
    cat > "$FAKE/gates/run-all-gates.sh" <<'STUB'
#!/usr/bin/env bash
# Stub runner: records the config it was called with, writes the artifacts a
# green suite writes, and exits the status this case asked for. With
# DN2CPP_STUB_FAIL_<CONFIG>=prereq|mixed it fails gate02 the way run_gate records
# a gate_skip under REQUIRE_ALL (the marker in $LOGDIR/gate02.log), and `mixed`
# fails gate01 plainly beside it.
mkdir -p "$LOGDIR"
: > "$LOGDIR/_skips.txt"
: > "$LOGDIR/_failures.txt"
eval "fail_mode=\${DN2CPP_STUB_FAIL_$CONFIG:-}"
case "$fail_mode" in
    prereq|mixed)
        printf 'FAIL: prerequisite absent, and DN2CPP_REQUIRE_ALL=1 demands every gate run: stub tool missing\n' > "$LOGDIR/gate02.log"
        if [ "$fail_mode" = mixed ]; then
            printf 'stub assertion failed\n' > "$LOGDIR/gate01.log"
            printf 'gate01 1 failed\ngate02 1 failed\n' > "$LOGDIR/_timings.txt"
            printf 'gate01\ngate02\n' > "$LOGDIR/_failures.txt"
        else
            printf 'gate01 1 ran\ngate02 1 failed\n' > "$LOGDIR/_timings.txt"
            printf 'gate02\n' > "$LOGDIR/_failures.txt"
        fi
        ;;
    *) printf 'gate01 1 ran\ngate02 1 ran\n' > "$LOGDIR/_timings.txt" ;;
esac
printf '%s\n' "$CONFIG" >> "$(dirname "$LOGDIR")/_stub_calls.txt"
eval "exit \${DN2CPP_STUB_RC_$CONFIG:-0}"
STUB
    chmod +x "$FAKE/gates/run-all-gates.sh"
    # Stub culture harness: exits the status the case asks for. Without it the
    # driver would run the REAL harness inside the self-test — minutes of sample
    # builds — and
    # against the fake repo it would not even find a sample to build.
    cat > "$FAKE/gates/verify-culture-invariance.sh" <<'CSTUB'
#!/usr/bin/env bash
echo "stub culture harness"
exit "${DN2CPP_STUB_CULTURE_RC:-0}"
CSTUB
    chmod +x "$FAKE/gates/verify-culture-invariance.sh"
    # Stub self-host build, for the reason the culture stub exists — and one more:
    # the real one is a many-minute native link, and a self-test that could reach
    # it is a self-test nobody runs. The fake repo's stub gates/_common.sh below
    # answers the freshness probe STALE, so every e2e case takes the rebuild arm
    # and this stub is the only selfhost-emit.sh reachable here.
    cat > "$FAKE/gates/selfhost-emit.sh" <<'SSTUB'
#!/usr/bin/env bash
echo "stub self-host build"
printf 'called\n' >> "${DN2CPP_STUB_SELFHOST_MARK:-/dev/null}"
exit "${DN2CPP_STUB_SELFHOST_RC:-0}"
SSTUB
    chmod +x "$FAKE/gates/selfhost-emit.sh"
    # Stub fork setup aids, for the reason the culture and self-host stubs exist:
    # the real ones drive scons and Emscripten. Each records that it was called,
    # so a case can assert the phase built exactly when it should have.
    cat > "$FAKE/gates/setup-godot-fork.sh" <<'FSTUB'
#!/usr/bin/env bash
echo "stub fork setup"
printf 'called\n' >> "${DN2CPP_STUB_FORK_MARK:-/dev/null}"
rc="${DN2CPP_STUB_FORK_RC:-0}"
[ "$rc" != 0 ] || [ "${DN2CPP_STUB_FORK_NO_MARK:-0}" = 1 ] \
    || : > "$DN2CPP_GODOT_FORK_ROOT/.stub-fork-fresh"
exit "$rc"
FSTUB
    chmod +x "$FAKE/gates/setup-godot-fork.sh"
    cat > "$FAKE/gates/setup-godot-fork-web.sh" <<'WSTUB'
#!/usr/bin/env bash
echo "stub web template bake (CRI=${CRI:-0})"
if [ "${CRI:-0}" = "1" ]; then flavor=cri; else flavor=stock; fi
printf '%s\n' "$flavor" >> "${DN2CPP_STUB_FORKWEB_MARK:-/dev/null}"
exit "${DN2CPP_STUB_FORKWEB_RC:-0}"
WSTUB
    chmod +x "$FAKE/gates/setup-godot-fork-web.sh"
    # Stub SDK unpack: records the call, and on success creates the READY file the
    # stub resolver below reads — so a case can prove the phase re-probed after
    # provisioning instead of trusting the unpack's exit 0.
    cat > "$FAKE/gates/setup-emsdk.sh" <<'ESTUB'
#!/usr/bin/env bash
echo "stub emsdk unpack"
printf 'called\n' >> "${DN2CPP_STUB_EMSDK_MARK:-/dev/null}"
rc="${DN2CPP_STUB_EMSDK_RC:-0}"
[ "$rc" != 0 ] || [ -z "${DN2CPP_STUB_EMSDK_READY:-}" ] || : > "$DN2CPP_STUB_EMSDK_READY"
exit "$rc"
ESTUB
    chmod +x "$FAKE/gates/setup-emsdk.sh"
    # Stub HELPERS, not just stub scripts, and this pair is what lets the two fork
    # probes ANSWER inside the fake repo. The probes are the subject here — their
    # exit-code-to-state mapping is the thing under test — so the predicates
    # beneath them are what gets faked, exactly as run-all-gates.sh is faked
    # beneath the suite phases.
    #
    # The self-host half stays deliberately STALE: every pre-existing e2e case
    # asserts the rebuild arm ran the stub, and a stub that answered "fresh" would
    # silently delete those assertions rather than fail them.
    cat > "$FAKE/gates/_common.sh" <<'CMSTUB'
# Stub _common.sh: only what gates/pre-merge.sh's probe arms read from it — the
# cd to the repo root (which is how FORK_PIN_EXPECTED resolves), an OS name, and
# the self-host freshness predicate.
set -euo pipefail
cd -P "$(dirname "${BASH_SOURCE[1]}")/.."
DN2CPP_OS="${DN2CPP_STUB_OS:-linux}"
EXE_EXT=
first_line() { local x="$1"; printf '%s\n' "${x%%$'\n'*}"; }
dn2cpp_emsdk_resolve() {
    [ -n "${DN2CPP_STUB_EMSDK_READY:-}" ] && [ -f "$DN2CPP_STUB_EMSDK_READY" ] && return 0
    return "${DN2CPP_STUB_EMSDK_RESOLVE:-0}"
}
emcc() { printf '%s\n' "${DN2CPP_STUB_EMCC_VERSION:-stub-emcc}"; }
xcodebuild() { :; }
lipo() { :; }
xcrun() { printf 'iPhone 16 (stub) (Booted)\n'; }
selfhost_bin_fresh() {
    SELFHOST_BIN_PATH="artifacts/selfhost-fullcli/dn2cpp"
    SELFHOST_SRC_STAMPED="<no stamp>"
    SELFHOST_SRC_NOW="stubsrc"
    return 1
}
CMSTUB
    cat > "$FAKE/gates/_godot_fork.sh" <<'GFSTUB'
# Stub _godot_fork.sh: the two cache predicates the fork probes call, plus the
# little the forkweb probe reads. Each answer is an env knob, so a case names the
# state it wants instead of building a fork to have one.
FORK_ROOT="${DN2CPP_GODOT_FORK_ROOT:-/nonexistent/stub-fork-root}"
FORK_PIN_EXPECTED=gates/expected/godot-fork-pin.txt
godot_fork_resolve() { FORK="${DN2CPP_STUB_FORK_CLONE:-/nonexistent/stub-fork}"; return "${DN2CPP_STUB_FORK_RESOLVE:-0}"; }
godot_fork_engine_provenance() { printf 'engine=stubengine base=stubbase\n'; }
godot_fork_cache_complete() {
    [ -f "$FORK_ROOT/.stub-fork-fresh" ] && return 0
    return "${DN2CPP_STUB_FORK_COMPLETE:-0}"
}
godot_fork_cache_fresh() {
    FORK_CACHE_HEAD="${DN2CPP_STUB_FORK_HEAD:-deadbee}"
    FORK_CACHE_ENGINE_NOW="${DN2CPP_STUB_FORK_ENGINE:-stubengine}"
    FORK_CACHE_TOOLS_NOW="${DN2CPP_STUB_FORK_TOOLS:-stubtools}"
    FORK_CACHE_WHY="${DN2CPP_STUB_FORK_WHY:-the stub says stale}"
    [ -f "$FORK_ROOT/.stub-fork-fresh" ] && return 0
    return "${DN2CPP_STUB_FORK_FRESH:-1}"
}
godot_fork_web_template_fresh() {
    local zip="$1" flavor="$2" emcc_stamp="$3" emcc="$4" engine="$5"
    [ -f "$zip" ] && [ "$(head -1 "$zip")" = "$flavor" ] \
        && [ "$(head -1 "$emcc_stamp" 2>/dev/null || true)" = "$emcc" ] \
        && [ "$(head -1 "$zip.provenance" 2>/dev/null || true)" = "$engine" ]
}
godot_fork_web_template_pair_fresh() {
    local release_zip="$1" debug_zip="$2" flavor="$3" release_stamp="$4"
    local debug_stamp="$5" emcc="$6" engine="$7"
    godot_fork_web_template_fresh "$release_zip" "$flavor" "$release_stamp" "$emcc" "$engine" \
        && godot_fork_web_template_fresh "$debug_zip" "$flavor" "$debug_stamp" "$emcc" "$engine" \
        && ! cmp -s "$release_zip" "$debug_zip"
}
godot_fork_ios_ready() {
    local zip="$1"
    FORK_IOS_READY_KIND="${DN2CPP_STUB_IOS_KIND:-artifact}"
    FORK_IOS_READY_WHY="the stub iOS template is not ready"
    if [ "$FORK_IOS_READY_KIND" = prerequisite ]; then
        FORK_IOS_READY_WHY="the stub host has no xcodebuild"
        return 1
    fi
    [ -f "$zip" ] \
        && [ "$(head -1 "$zip.provenance" 2>/dev/null || true)" = 'engine=stubengine base=stubbase' ]
}
GFSTUB

    # A fork artifact root the forkweb probe can read for real. The probe logic is
    # the subject — "which flavors are due" — so its INPUTS are faked and it runs
    # unmodified: four zips, each stamped with the provenance the stub helper
    # reports, which is the state "nothing due" looks like.
    ST_FORKROOT="$ST_TMP/forkroot"
    mkdir -p "$ST_FORKROOT" "$FAKE/gates/expected"
    printf 'stubbase\n' > "$FAKE/gates/expected/godot-fork-pin.txt"
    for z in web_template web_template_debug web_template_cri web_template_cri_debug; do
        case "$z" in web_template|web_template_debug) flavor=stock ;; *) flavor=cri ;; esac
        case "$z" in *_debug) config=debug ;; *) config=release ;; esac
        printf '%s\n%s\n' "$flavor" "$config" > "$ST_FORKROOT/$z.zip"
        printf 'engine=stubengine base=stubbase\n' > "$ST_FORKROOT/$z.zip.provenance"
    done
    printf 'stub-emcc\n' > "$ST_FORKROOT/web_emcc.txt"
    printf 'stub-emcc\n' > "$ST_FORKROOT/web_emcc_debug.txt"
    printf 'stub-emcc\n' > "$ST_FORKROOT/web_emcc_cri.txt"
    printf 'stub-emcc\n' > "$ST_FORKROOT/web_emcc_cri_debug.txt"
    printf 'not a real iOS template\n' > "$ST_FORKROOT/ios_template.zip"
    printf 'engine=stubengine base=stubbase\n' > "$ST_FORKROOT/ios_template.zip.provenance"

    # e2e NAME EXPECT_RC RELEASE_RC DEBUG_RC EXPECT_CALLS [EXTRA_ARG] [CULTURE_RC] [SELFHOST_RC]
    e2e() {
        local name="$1" want_rc="$2" rel_rc="$3" dbg_rc="$4" want_calls="$5" extra="${6:-}" cult="${7:-0}" sh_rc="${8:-0}"
        local root="$ST_TMP/e2e-$name" rc=0 calls
        mkdir -p "$root"
        rm -f "${DN2CPP_STUB_FORKROOT:-$ST_FORKROOT}/.stub-fork-fresh"
        DN2CPP_PREMERGE_SELFTEST=0 \
        DN2CPP_PREMERGE_LOGROOT="$root" \
        DN2CPP_STUB_CULTURE_RC="$cult" \
        DN2CPP_STUB_SELFHOST_RC="$sh_rc" \
        DN2CPP_STUB_SELFHOST_MARK="$root/_selfhost_calls.txt" \
        DN2CPP_GODOT_FORK_ROOT="${DN2CPP_STUB_FORKROOT:-$ST_FORKROOT}" \
        DN2CPP_STUB_FORK_MARK="$root/_fork_calls.txt" \
        DN2CPP_STUB_FORKWEB_MARK="$root/_forkweb_calls.txt" \
        DN2CPP_STUB_EMSDK_MARK="$root/_emsdk_calls.txt" \
        DN2CPP_STUB_EMSDK_READY="$root/_emsdk_ready" \
        DN2CPP_STUB_RC_Release="$rel_rc" \
        DN2CPP_STUB_RC_Debug="$dbg_rc" \
            bash "$FAKE/gates/pre-merge.sh" $extra >"$root/_out.txt" 2>&1 || rc=$?
        # cat|tr, not `tr < file`: the culture-red case never reaches the runner,
        # so the file legitimately does not exist and a `<` redirection failure is
        # reported by the SHELL, where `2>/dev/null` on tr cannot reach it.
        calls=$(cat "$root/_stub_calls.txt" 2>/dev/null | tr '\n' ',')
        if [ "$rc" = "$want_rc" ] && [ "$calls" = "$want_calls" ]; then
            st_ok "$name: exit $rc, runner called with [$calls]"
        else
            st_bad "$name: exit $rc (want $want_rc), calls [$calls] (want [$want_calls]) — see $root/_out.txt"
        fi
        # The receipt is the auditable claim "the merge gate passed on this
        # commit"; it must exist exactly when the whole thing passed.
        if [ "$want_rc" = "0" ]; then
            [ -f "$root/_receipt.txt" ] && st_ok "$name: receipt written" \
                || st_bad "$name: no receipt after a green run"
        else
            [ -f "$root/_receipt.txt" ] && st_bad "$name: receipt written after a RED run" \
                || st_ok "$name: no receipt after a red run"
        fi
    }

    e2e both-green   0 0 0 "Release,Debug,"
    e2e red-release  1 1 0 "Release,"
    e2e red-debug    1 0 1 "Release,Debug,"
    e2e keep-going   1 1 1 "Release,Debug," --keep-going

    # A Release whose only failures are gate_skips under REQUIRE_ALL is red but
    # does not withhold Debug: those gates cannot pass on this host, and the
    # verdict lists them as a host gap. One plainly failed gate beside them
    # withholds Debug as before.
    DN2CPP_STUB_FAIL_Release=prereq e2e red-release-prereq-only 1 1 0 "Release,Debug,"
    if grep -q 'host prerequisites absent here: Release: gate02 (prerequisite absent)' \
        "$ST_TMP/e2e-red-release-prereq-only/_out.txt"; then
        st_ok "red-release-prereq-only: the verdict lists the gate as a host gap"
    else
        st_bad "red-release-prereq-only: the verdict does not list gate02 as a host gap — see $ST_TMP/e2e-red-release-prereq-only/_out.txt"
    fi
    DN2CPP_STUB_FAIL_Release=mixed e2e red-release-mixed 1 1 0 "Release,"

    # Phase 0's three outcomes. The one that matters is the middle case: a red
    # culture harness must stop the suites BEFORE they run (empty call list), or
    # the fail-fast the phase exists for is decoration.
    e2e culture-red           1 0 0 ""                 ""            1
    e2e culture-red-keepgoing 1 0 0 "Release,Debug,"   --keep-going  1
    e2e culture-not-performed 0 0 0 "Release,Debug,"   ""            77
    for c in culture-red culture-not-performed; do
        want=$([ "$c" = culture-red ] && echo "culture=RED" || echo "culture=not-performed")
        if grep -q "$want" "$ST_TMP/e2e-$c/_out.txt"; then
            st_ok "$c: verdict names $want"
        else
            st_bad "$c: verdict does not name $want — see $ST_TMP/e2e-$c/_out.txt"
        fi
    done
    # A receipt that did not say the check was skipped would read as it having
    # been performed — the one thing a receipt exists to prevent.
    if grep -q 'culture=not-performed' "$ST_TMP/e2e-culture-not-performed/_receipt.txt"; then
        st_ok "culture-not-performed: the receipt says so"
    else
        st_bad "culture-not-performed: the receipt hides it"
    fi

    # Phase 1's two outcomes that the driver owns. A red self-host build must stop
    # the suites before they run, for the same reason a red culture harness does.
    e2e selfhost-red           1 0 0 ""               ""            0 1
    e2e selfhost-red-keepgoing 1 0 0 "Release,Debug," --keep-going  0 1
    if grep -q 'selfhost=RED' "$ST_TMP/e2e-selfhost-red/_out.txt"; then
        st_ok "selfhost-red: verdict names selfhost=RED"
    else
        st_bad "selfhost-red: verdict does not name selfhost=RED — see $ST_TMP/e2e-selfhost-red/_out.txt"
    fi
    # The stub is reached through the FAKE repo's gates/ — proof that no self-test
    # path can spend minutes in the real native link.
    if [ -f "$ST_TMP/e2e-both-green/_selfhost_calls.txt" ]; then
        st_ok "both-green: the self-host phase ran the fake repo's stub"
    else
        st_bad "both-green: no self-host build was attempted at all"
    fi
    if grep -q '^selfhost: rebuilt' "$ST_TMP/e2e-both-green/_receipt.txt" 2>/dev/null; then
        st_ok "both-green: the receipt records the self-host binary's provenance"
    else
        st_bad "both-green: the receipt does not record the self-host build"
    fi

    # ── phase 2, the fork cache ───────────────────────────────────────────────
    # The state the stub helpers report is what each case names; the assertions
    # are on the two things a reader of this phase depends on — whether the setup
    # aid RAN, and what the verdict and receipt SAY it ran over. Both matter: a
    # phase that rebuilds silently and a phase that reports a rebuild it skipped
    # are the two ways this can be wrong, and only one of them is visible in the
    # exit code.
    #
    # every pre-existing case above already exercised the STALE arm, because the
    # stub answers stale by default — so what is left is the other three answers.
    DN2CPP_STUB_FORK_FRESH=0 e2e fork-fresh 0 0 0 "Release,Debug,"
    if [ -f "$ST_TMP/e2e-fork-fresh/_fork_calls.txt" ]; then
        st_bad "fork-fresh: the phase rebuilt a cache it was told was current"
    else
        st_ok "fork-fresh: a current cache is not rebuilt"
    fi
    if grep -q '^fork:    reused' "$ST_TMP/e2e-fork-fresh/_receipt.txt" 2>/dev/null; then
        st_ok "fork-fresh: the receipt records the cache as reused"
    else
        st_bad "fork-fresh: the receipt does not record the fork cache — see $ST_TMP/e2e-fork-fresh/_receipt.txt"
    fi

    # The stale arm's positive half, asserted where the default already puts us.
    if [ -f "$ST_TMP/e2e-both-green/_fork_calls.txt" ]; then
        st_ok "both-green: a stale cache is refreshed through the fake repo's stub"
    else
        st_bad "both-green: no fork-cache refresh was attempted at all"
    fi
    if grep -q '^fork:    rebuilt' "$ST_TMP/e2e-both-green/_receipt.txt" 2>/dev/null; then
        st_ok "both-green: the receipt records the fork cache's provenance"
    else
        st_bad "both-green: the receipt does not record the fork rebuild"
    fi
    if grep -q '^forkweb: reused' "$ST_TMP/e2e-both-green/_receipt.txt" 2>/dev/null; then
        st_ok "both-green: the receipt records the Web templates as current"
    else
        st_bad "both-green: the receipt does not record the Web templates"
    fi

    # A failed refresh must stop the suites, exactly as a failed self-host build
    # does: an empty call list is the assertion, not a nicety.
    DN2CPP_STUB_FORK_RC=1 e2e fork-red 1 0 0 ""
    if grep -q 'fork=RED' "$ST_TMP/e2e-fork-red/_out.txt"; then
        st_ok "fork-red: the verdict names fork=RED"
    else
        st_bad "fork-red: the verdict does not name fork=RED — see $ST_TMP/e2e-fork-red/_out.txt"
    fi

    # Absent is a DIFFERENT answer from stale, and the phase has to say so before
    # spending tens of minutes: a reader who sees "refreshing" where the truth is
    # "building from nothing" has no way to notice the wrong worktree.
    DN2CPP_STUB_FORK_COMPLETE=1 e2e fork-absent 0 0 0 "Release,Debug,"
    if grep -q 'no cache on this box yet' "$ST_TMP/e2e-fork-absent/_out.txt"; then
        st_ok "fork-absent: the phase says it is building a cache, not refreshing one"
    else
        st_bad "fork-absent: an absent cache is reported as a stale one — see $ST_TMP/e2e-fork-absent/_out.txt"
    fi
    if grep -q '^fork:    rebuilt (fork deadbee, engine stubengine, tools stubtools)' \
        "$ST_TMP/e2e-fork-absent/_receipt.txt" 2>/dev/null; then
        st_ok "fork-absent: receipt carries post-build fork hashes"
    else
        st_bad "fork-absent: receipt retained pre-build unknowns"
    fi

    DN2CPP_STUB_FORK_NO_MARK=1 e2e fork-false-green 1 0 0 ""
    if grep -q 'setup exited 0 but the rebuilt cache is not current' \
        "$ST_TMP/e2e-fork-false-green/_out.txt"; then
        st_ok "fork-false-green: setup exit 0 is re-probed and refused"
    else
        st_bad "fork-false-green: setup exit 0 was trusted without a fresh cache"
    fi

    # A host with no arm for the lane: refused up front with exit 2, the same
    # status and the same reason as the SKIP_GODOT refusal. Not exit 1 — this is
    # not a red merge gate, it is a run that cannot be performed here.
    DN2CPP_STUB_OS=android e2e fork-no-arm 2 0 0 ""
    if grep -q 'no pre-merge run on this host' "$ST_TMP/e2e-fork-no-arm/_out.txt"; then
        st_ok "fork-no-arm: refused with the reason, before the first suite"
    else
        st_bad "fork-no-arm: refused without saying why — see $ST_TMP/e2e-fork-no-arm/_out.txt"
    fi

    # The iOS template's two not-ready shapes. A missing HOST prerequisite (no
    # Xcode) continues: the suites run, forkios is red in the results, and the
    # verdict names the gap — with no receipt, because red is red. A repairable
    # ARTIFACT (the zip is absent on a host that could build it) keeps the exit 2
    # refusal before the first suite.
    DN2CPP_STUB_IOS_KIND=prerequisite e2e forkios-no-xcode 1 0 0 "Release,Debug,"
    if grep -q 'forkios=RED' "$ST_TMP/e2e-forkios-no-xcode/_out.txt" \
        && grep -q 'host prerequisites absent here' "$ST_TMP/e2e-forkios-no-xcode/_out.txt"; then
        st_ok "forkios-no-xcode: suites ran, verdict names forkios=RED and the host gap"
    else
        st_bad "forkios-no-xcode: verdict lacks forkios=RED or the host gap — see $ST_TMP/e2e-forkios-no-xcode/_out.txt"
    fi
    ST_IOSLESS="$ST_TMP/forkroot-iosless"
    mkdir -p "$ST_IOSLESS"
    for f in web_template.zip web_template.zip.provenance web_template_debug.zip \
            web_template_debug.zip.provenance web_template_cri.zip \
            web_template_cri.zip.provenance web_template_cri_debug.zip \
            web_template_cri_debug.zip.provenance web_emcc.txt web_emcc_debug.txt \
            web_emcc_cri.txt web_emcc_cri_debug.txt; do
        cp "$ST_FORKROOT/$f" "$ST_IOSLESS/$f"
    done
    DN2CPP_STUB_FORKROOT="$ST_IOSLESS" e2e forkios-stale-artifact 2 0 0 ""
    if grep -q 'setup-godot-fork-ios.sh' "$ST_TMP/e2e-forkios-stale-artifact/_out.txt"; then
        st_ok "forkios-stale-artifact: refused naming the iOS aid, before the first suite"
    else
        st_bad "forkios-stale-artifact: refused without naming gates/setup-godot-fork-ios.sh"
    fi

    # The Web templates, on a box with no mutable Emscripten SDK. The SDK is
    # provisioned, not refused: the phase runs gates/setup-emsdk.sh once, re-probes,
    # and bakes. When the unpack fails the suites still run and forkweb is a red
    # host gap at the verdict, not a withheld run.
    ST_WEBLESS="$ST_TMP/forkroot-webless"
    mkdir -p "$ST_WEBLESS"
    cp "$ST_FORKROOT/ios_template.zip" "$ST_WEBLESS/ios_template.zip"
    cp "$ST_FORKROOT/ios_template.zip.provenance" "$ST_WEBLESS/ios_template.zip.provenance"
    DN2CPP_STUB_EMSDK_RESOLVE=1 DN2CPP_STUB_FORKROOT="$ST_WEBLESS" \
        e2e forkweb-provision 0 0 0 "Release,Debug,"
    ST_EMSDK_CALLS="$(cat "$ST_TMP/e2e-forkweb-provision/_emsdk_calls.txt" 2>/dev/null | tr '\n' ',')"
    ST_BAKED="$(cat "$ST_TMP/e2e-forkweb-provision/_forkweb_calls.txt" 2>/dev/null | tr '\n' ',')"
    if [ "$ST_EMSDK_CALLS" = "called," ] && [ "$ST_BAKED" = "stock,cri," ]; then
        st_ok "forkweb-provision: the SDK was unpacked once, then both flavors baked"
    else
        st_bad "forkweb-provision: emsdk calls [$ST_EMSDK_CALLS] (want [called,]), bakes [$ST_BAKED] (want [stock,cri,])"
    fi
    if grep -q '^forkweb: rebaked' "$ST_TMP/e2e-forkweb-provision/_receipt.txt" 2>/dev/null; then
        st_ok "forkweb-provision: the receipt records the rebake"
    else
        st_bad "forkweb-provision: the receipt does not record the rebake"
    fi
    DN2CPP_STUB_EMSDK_RESOLVE=1 DN2CPP_STUB_EMSDK_RC=1 DN2CPP_STUB_FORKROOT="$ST_WEBLESS" \
        e2e forkweb-provision-red 1 0 0 "Release,Debug,"
    if grep -q 'forkweb=RED' "$ST_TMP/e2e-forkweb-provision-red/_out.txt" \
        && grep -q 'host prerequisites absent here' "$ST_TMP/e2e-forkweb-provision-red/_out.txt" \
        && grep -q '_emsdk.log' "$ST_TMP/e2e-forkweb-provision-red/_out.txt"; then
        st_ok "forkweb-provision-red: suites ran, verdict names forkweb=RED, the host gap and _emsdk.log"
    else
        st_bad "forkweb-provision-red: verdict lacks forkweb=RED, the host gap or _emsdk.log — see $ST_TMP/e2e-forkweb-provision-red/_out.txt"
    fi

    # The bake arm itself, reachable on any host: EMSDK set is what the phase
    # reads as "an SDK is provisioned", and the stub records the CRI pin each
    # flavor was invoked under — an ambient CRI=1 leaking into the stock bake
    # (premerge_fork_web_argv's mislabel) would surface here as a wrong line.
    DN2CPP_STUB_FORKROOT="$ST_WEBLESS" e2e forkweb-bake 0 0 0 "Release,Debug,"
    ST_BAKED="$(cat "$ST_TMP/e2e-forkweb-bake/_forkweb_calls.txt" 2>/dev/null | tr '\n' ',')"
    if [ "$ST_BAKED" = "stock,cri," ]; then
        st_ok "forkweb-bake: both flavors baked, each under its own CRI pin"
    else
        st_bad "forkweb-bake: flavors recorded [$ST_BAKED] (want [stock,cri,])"
    fi
    if grep -q '^forkweb: rebaked' "$ST_TMP/e2e-forkweb-bake/_receipt.txt" 2>/dev/null; then
        st_ok "forkweb-bake: the receipt records the rebake"
    else
        st_bad "forkweb-bake: the receipt does not record the rebake"
    fi

    # The forkweb probe must schedule exactly the flavor whose builder contract
    # is broken: flavor, emcc and engine provenance are independent terms.
    web_probe_case() {
        local name="$1" root="$ST_TMP/webprobe-$1" want="$2" got
        cp -R "$ST_FORKROOT" "$root"
        case "$name" in
            missing-emcc) rm -f "$root/web_emcc.txt" ;;
            missing-debug) rm -f "$root/web_template_debug.zip" ;;
            missing-debug-emcc) rm -f "$root/web_emcc_cri_debug.txt" ;;
            wrong-flavor) printf 'cri\nrelease\n' > "$root/web_template.zip" ;;
            wrong-debug-flavor) printf 'cri\ndebug\n' > "$root/web_template_debug.zip" ;;
            identical-pair) cp "$root/web_template.zip" "$root/web_template_debug.zip" ;;
            stale-engine) printf 'engine=old base=stubbase\n' > "$root/web_template_cri.zip.provenance" ;;
            stale-debug-engine) printf 'engine=old base=stubbase\n' > "$root/web_template_cri_debug.zip.provenance" ;;
        esac
        got="$(DN2CPP_GODOT_FORK_ROOT="$root" DN2CPP_PREMERGE_PROBE=forkweb \
            bash "$FAKE/gates/pre-merge.sh" 2>/dev/null || true)"
        if [ "$got" = "$want" ]; then
            st_ok "forkweb $name -> [$got]"
        else
            st_bad "forkweb $name -> [$got] (wanted [$want])"
        fi
    }
    web_probe_case missing-emcc "ok stock"
    web_probe_case missing-debug "ok stock"
    web_probe_case missing-debug-emcc "ok cri"
    web_probe_case wrong-flavor "ok stock"
    web_probe_case wrong-debug-flavor "ok stock"
    web_probe_case identical-pair "ok stock"
    web_probe_case stale-engine "ok cri"
    web_probe_case stale-debug-engine "ok cri"

    say "fork artifact freshness predicates (real helpers, synthetic artifacts)"

    ST_WEB_REAL="$ST_TMP/web-real"
    mkdir -p "$ST_WEB_REAL/good" "$ST_WEB_REAL/debug" "$ST_WEB_REAL/bad"
    printf 'plain stock glue\n' > "$ST_WEB_REAL/good/godot.js"
    printf '__cpp_exception\n' > "$ST_WEB_REAL/good/godot.wasm"
    printf 'side\n' > "$ST_WEB_REAL/good/godot.side.wasm"
    (cd "$ST_WEB_REAL/good" && zip -q "$ST_WEB_REAL/good.zip" godot.js godot.wasm godot.side.wasm)
    printf 'plain stock debug glue\n' > "$ST_WEB_REAL/debug/godot.js"
    printf '__cpp_exception debug\n' > "$ST_WEB_REAL/debug/godot.wasm"
    printf 'debug side\n' > "$ST_WEB_REAL/debug/godot.side.wasm"
    (cd "$ST_WEB_REAL/debug" && zip -q "$ST_WEB_REAL/debug.zip" godot.js godot.wasm godot.side.wasm)
    printf 'plain stock glue\n' > "$ST_WEB_REAL/bad/godot.js"
    printf '__cpp_exception\n' > "$ST_WEB_REAL/bad/godot.wasm"
    (cd "$ST_WEB_REAL/bad" && zip -q "$ST_WEB_REAL/bad.zip" godot.js godot.wasm)
    for z in good debug bad; do
        printf 'stub-emcc\n' > "$ST_WEB_REAL/$z.emcc"
        printf 'engine=stub base=stubbase\n' > "$ST_WEB_REAL/$z.zip.provenance"
    done
    web_real_fresh() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_WEB_REAL"
            DN2CPP_OS=linux
            source "$REPO/gates/_godot_fork.sh"
            godot_fork_web_template_fresh "$ST_WEB_REAL/$1.zip" stock \
                "$ST_WEB_REAL/$1.emcc" stub-emcc 'engine=stub base=stubbase'
        )
    }
    if web_real_fresh good; then
        st_ok "Web predicate accepts matching flavor/emcc/engine plus dlink/EH structure"
    else
        st_bad "Web predicate rejected a structurally current synthetic template"
    fi
    if web_real_fresh bad; then
        st_bad "Web predicate accepted a template with no godot.side.wasm"
    else
        st_ok "Web predicate rejects a structurally incomplete template"
    fi
    web_real_pair_fresh() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_WEB_REAL"
            DN2CPP_OS=linux
            source "$REPO/gates/_godot_fork.sh"
            godot_fork_web_template_pair_fresh "$ST_WEB_REAL/$1.zip" \
                "$ST_WEB_REAL/$2.zip" stock "$ST_WEB_REAL/$1.emcc" \
                "$ST_WEB_REAL/$2.emcc" stub-emcc 'engine=stub base=stubbase'
        )
    }
    if web_real_pair_fresh good debug; then
        st_ok "Web pair predicate accepts fresh, distinct Release/Debug modules"
    else
        st_bad "Web pair predicate rejected a fresh, distinct configuration pair"
    fi
    if web_real_pair_fresh good good; then
        st_bad "Web pair predicate accepted byte-identical Release/Debug modules"
    else
        st_ok "Web pair predicate rejects byte-identical Release/Debug modules"
    fi

    ST_IOS_REAL="$ST_TMP/ios-real"
    ST_IOS_SLICE=libgodot.ios.debug.xcframework/ios-arm64_x86_64-simulator/libgodot.a
    mkdir -p "$ST_IOS_REAL/bad/$(dirname "$ST_IOS_SLICE")" \
             "$ST_IOS_REAL/good/$(dirname "$ST_IOS_SLICE")"
    printf 'X86_ONLY\n' > "$ST_IOS_REAL/bad/$ST_IOS_SLICE"
    printf 'ARM64\n' > "$ST_IOS_REAL/good/$ST_IOS_SLICE"
    (cd "$ST_IOS_REAL/bad" && zip -q -r "$ST_IOS_REAL/bad.zip" libgodot.ios.debug.xcframework)
    (cd "$ST_IOS_REAL/good" && zip -q -r "$ST_IOS_REAL/good.zip" libgodot.ios.debug.xcframework)
    for z in bad good; do
        printf 'engine=stub base=stubbase\n' > "$ST_IOS_REAL/$z.zip.provenance"
    done
    ios_real_ready() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_IOS_REAL"
            DN2CPP_OS=macos
            xcodebuild() { :; }
            xcrun() { printf 'iPhone 16 (stub) (Booted)\n'; }
            lipo() {
                local f="$2"
                if grep -q ARM64 "$f"; then
                    printf 'Architectures in the fat file: %s are: x86_64 arm64\n' "$f"
                else
                    printf 'Non-fat file: %s is architecture: x86_64\n' "$f"
                fi
            }
            source "$REPO/gates/_godot_fork.sh"
            godot_fork_engine_provenance() { printf 'engine=stub base=stubbase\n'; }
            godot_fork_ios_ready "$ST_IOS_REAL/$1.zip"
        )
    }
    if ios_real_ready bad; then
        st_bad "iOS readiness accepts an x86_64-only simulator slice"
    else
        st_ok "iOS readiness rejects an unrepaired simulator template"
    fi
    if ios_real_ready good; then
        st_ok "iOS readiness accepts a readable, current arm64 simulator template"
    else
        st_bad "iOS readiness rejected a repaired simulator template"
    fi

    ST_SDK_FORK="$ST_TMP/sdk-fork"
    ST_SDK_ROOT="$ST_TMP/sdk-root"
    mkdir -p "$ST_SDK_FORK" "$ST_SDK_ROOT/nuget"
    cat > "$ST_SDK_FORK/version.py" <<'VERSION'
major = 4
minor = 8
patch = 1
status = "stable"
VERSION
    managed_version() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_SDK_ROOT"
            DN2CPP_OS=linux
            source "$REPO/gates/_godot_fork.sh"
            FORK="$ST_SDK_FORK"
            godot_fork_managed_package_version
        )
    }
    sed 's/status = "stable"/status = "beta3"/' "$ST_SDK_FORK/version.py" \
        > "$ST_SDK_FORK/version.py.tmp"
    mv "$ST_SDK_FORK/version.py.tmp" "$ST_SDK_FORK/version.py"
    if [ "$(managed_version)" = 4.8.1-beta.3 ]; then
        st_ok "managed package version matches build_assemblies.py prerelease spelling"
    else
        st_bad "managed package version does not normalize beta3 to beta.3"
    fi
    sed 's/status = "beta3"/status = "stable"/' "$ST_SDK_FORK/version.py" \
        > "$ST_SDK_FORK/version.py.tmp"
    mv "$ST_SDK_FORK/version.py.tmp" "$ST_SDK_FORK/version.py"
    managed_feed_fresh() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_SDK_ROOT"
            DN2CPP_OS=linux
            source "$REPO/gates/_godot_fork.sh"
            FORK="$ST_SDK_FORK"
            godot_fork_managed_feed_fresh
        )
    }
    : > "$ST_SDK_ROOT/nuget/Godot.NET.Sdk.4.8.0.nupkg"
    if managed_feed_fresh; then
        st_bad "SDK feed accepts the wrong sole package"
    else
        st_ok "SDK feed rejects a stale sole package"
    fi
    : > "$ST_SDK_ROOT/nuget/Godot.NET.Sdk.4.8.1.nupkg"
    if managed_feed_fresh; then
        st_bad "SDK feed accepts mixed package versions"
    else
        st_ok "SDK feed rejects ambiguous package versions"
    fi
    rm -f "$ST_SDK_ROOT/nuget/Godot.NET.Sdk.4.8.0.nupkg"
    for pkg in Godot.SourceGenerators GodotSharp GodotSharpEditor; do
        : > "$ST_SDK_ROOT/nuget/$pkg.4.8.1.nupkg"
    done
    if managed_feed_fresh; then
        st_ok "managed feed accepts one current package for all four products"
    else
        st_bad "managed feed rejected its four current packages"
    fi
    rm -f "$ST_SDK_ROOT/nuget/Godot.SourceGenerators.4.8.1.nupkg"
    if managed_feed_fresh; then
        st_bad "managed feed accepts a missing SourceGenerators sibling"
    else
        st_ok "managed feed rejects a missing SourceGenerators sibling"
    fi
    : > "$ST_SDK_ROOT/nuget/Godot.SourceGenerators.4.8.1.nupkg"
    : > "$ST_SDK_ROOT/nuget/GodotSharpEditor.4.8.0.nupkg"
    if managed_feed_fresh; then
        st_bad "managed feed accepts mixed GodotSharpEditor versions"
    else
        st_ok "managed feed rejects mixed GodotSharpEditor versions"
    fi
    rm -f "$ST_SDK_ROOT/nuget/GodotSharpEditor.4.8.0.nupkg"
    : > "$ST_SDK_ROOT/nuget/Godot.NET.Sdk.4.8.0.nupkg"
    if (source "$REPO/gates/_common.sh"; godot_nuget_sdk_version "$ST_SDK_ROOT/nuget") \
        >/dev/null 2>&1; then
        st_bad "godot_nuget_sdk_version selected one package from an ambiguous feed"
    else
        st_ok "godot_nuget_sdk_version rejects an ambiguous feed"
    fi

    ST_CACHE_FORK="$ST_TMP/cache-fork"
    ST_CACHE_ROOT="$ST_TMP/cache-root"
    mkdir -p "$ST_CACHE_FORK/modules/mono/mono_gd" \
             "$ST_CACHE_FORK/modules/mono/glue/GodotSharp/GodotSharp/Generated" \
             "$ST_CACHE_FORK/bin/GodotSharp/Api/Release" \
             "$ST_CACHE_FORK/bin/GodotSharp/Dn2Cpp" "$ST_CACHE_ROOT/nuget"
    cp "$ST_SDK_FORK/version.py" "$ST_CACHE_FORK/version.py"
    printf 'marker\n' > "$ST_CACHE_FORK/modules/mono/mono_gd/gd_mono.cpp"
    printf 'bin/\nmodules/mono/glue/GodotSharp/GodotSharp/Generated/\n' > "$ST_CACHE_FORK/.gitignore"
    git -C "$ST_CACHE_FORK" init -q
    git -C "$ST_CACHE_FORK" add .
    git -C "$ST_CACHE_FORK" -c user.name=stub -c user.email=stub@example.invalid commit -qm base
    ST_CACHE_BASE="$(git -C "$ST_CACHE_FORK" rev-parse HEAD)"
    printf '%s\n' "$ST_CACHE_BASE" > "$ST_CACHE_ROOT/pin.txt"
    printf '%s\n' "$ST_CACHE_BASE" > "$ST_TMP/cache-pin.txt"
    ST_CACHE_EDITOR="$ST_CACHE_FORK/bin/godot.linuxbsd.editor.x86_64.mono"
    ST_CACHE_TEMPLATE="$ST_CACHE_FORK/bin/godot.linuxbsd.template_release.x86_64.mono"
    printf '#!/bin/sh\n' > "$ST_CACHE_EDITOR"; chmod +x "$ST_CACHE_EDITOR"
    printf '#!/bin/sh\n' > "$ST_CACHE_TEMPLATE"; chmod +x "$ST_CACHE_TEMPLATE"
    printf '%s\n' "$ST_CACHE_EDITOR" > "$ST_CACHE_ROOT/editor.txt"
    printf '%s\n' "$ST_CACHE_TEMPLATE" > "$ST_CACHE_ROOT/template.txt"
    printf '%s\n' "$ST_CACHE_FORK" > "$ST_CACHE_ROOT/clone.txt"
    printf 'api\n' > "$ST_CACHE_FORK/bin/GodotSharp/Api/Release/GodotSharp.dll"
    printf '{}\n' > "$ST_CACHE_FORK/bin/GodotSharp/Dn2Cpp/manifest.json"
    printf 'glue\n' > "$ST_CACHE_FORK/modules/mono/glue/GodotSharp/GodotSharp/Generated/GeneratedIncludes.props"
    for pkg in Godot.NET.Sdk Godot.SourceGenerators GodotSharp GodotSharpEditor; do
        : > "$ST_CACHE_ROOT/nuget/$pkg.4.8.1.nupkg"
    done
    cache_helper() {
        (
            export DN2CPP_GODOT_FORK_ROOT="$ST_CACHE_ROOT"
            DN2CPP_OS=linux
            source "$REPO/gates/_godot_fork.sh"
            FORK_PIN_EXPECTED="$ST_TMP/cache-pin.txt"
            "$@"
        )
    }
    # Compute and stamp in one helper shell so FORK/BASE_COMMIT use the same
    # dynamic-scope contract as the production predicate.
    (
        export DN2CPP_GODOT_FORK_ROOT="$ST_CACHE_ROOT"
        DN2CPP_OS=linux
        source "$REPO/gates/_godot_fork.sh"
        FORK_PIN_EXPECTED="$ST_TMP/cache-pin.txt"
        godot_fork_resolve >/dev/null
        BASE_COMMIT="$ST_CACHE_BASE"
        eng="$(godot_fork_engine_hash)"
        tools="$(godot_fork_tools_hash)"
        printf '%s\n' "$eng" > "$FORK_EDITOR.engine-hash"
        printf '%s\n' "$eng" > "$FORK_TEMPLATE.engine-hash"
        printf '%s\n' "$eng" > "$FORK/$FORK_GLUE_MARKER.engine-hash"
        printf '%s\n' "$tools" > "$(godot_fork_tools_stamp)"
        desktop="$(godot_fork_desktop_template "$FORK_ROOT")"
        printf 'desktop\n' > "$desktop"
        printf 'engine=%s base=%s\n' "$eng" "$BASE_COMMIT" > "$desktop.provenance"
    )
    if cache_helper godot_fork_cache_fresh; then
        st_ok "fork cache predicate accepts a complete current synthetic cache"
    else
        st_bad "fork cache predicate rejected a complete current synthetic cache"
    fi
    ST_CACHE_DESKTOP="$ST_CACHE_ROOT/linux_template.$(cache_helper godot_fork_host_arch)"
    rm -f "$ST_CACHE_DESKTOP.provenance"
    if cache_helper godot_fork_cache_fresh; then
        st_bad "fork cache predicate accepts an unstamped desktop template"
    else
        st_ok "fork cache predicate schedules repair for an unstamped desktop template"
    fi

    # ── the probes' own output shapes ─────────────────────────────────────────
    # Against the REAL repo, because that is where the real predicates live. The
    # self-host line is FROZEN: premerge_selfhost_state reads it positionally, and
    # a field added in front of it would be read as the freshness verdict.
    ST_PROBE="$(DN2CPP_PREMERGE_PROBE=1 bash "$0" 2>/dev/null || true)"
    if [ "$(printf '%s\n' "$ST_PROBE" | wc -l | tr -d ' ')" = "1" ] \
        && [ "$(printf '%s' "$ST_PROBE" | wc -w | tr -d ' ')" = "3" ]; then
        st_ok "the self-host probe still prints one line of three fields"
    else
        st_bad "the self-host probe's frozen output shape changed: [$ST_PROBE]"
    fi
    case "$ST_PROBE" in
        0\ *|1\ *) st_ok "the self-host probe's first field is a verdict" ;;
        *)         st_bad "the self-host probe does not lead with 0 or 1: [$ST_PROBE]" ;;
    esac
    # The fork line carries prose last, so it is asserted on its FIRST fields
    # only — the reason is deliberately unbounded.
    ST_FPROBE="$(DN2CPP_PREMERGE_PROBE=fork bash "$0" 2>/dev/null || true)"
    case "$ST_FPROBE" in
        0\ *|1\ *|2\ *|3\ *) st_ok "the fork probe leads with one of its four states" ;;
        *)                   st_bad "the fork probe's first field is not a state: [$ST_FPROBE]" ;;
    esac
    if [ "$(printf '%s' "$ST_FPROBE" | wc -w | tr -d ' ')" -ge 5 ]; then
        st_ok "the fork probe prints its four fields and a reason"
    else
        st_bad "the fork probe printed fewer fields than its readers take: [$ST_FPROBE]"
    fi
    # An unanswerable Web probe must mean BAKE, never "nothing due": the wrong
    # default here is the one that lets the suite start over an absent template.
    ST_WEB_OUT="$(DN2CPP_PREMERGE_PROBE=forkweb bash "$0" 2>/dev/null || true)"
    case "$ST_WEB_OUT" in
        ok|ok\ *) st_ok "the forkweb probe answers with its ok sentinel" ;;
        *)        st_ok "the forkweb probe could not answer here — the reader bakes both" ;;
    esac

    # The transcript, and specifically its LAST line: a form that loses the tail
    # loses the verdict, which is the one line the transcript exists for.
    ST_LOG="$ST_TMP/e2e-both-green/_premerge.log"
    if [ -f "$ST_LOG" ]; then
        st_ok "both-green: a transcript was written"
    else
        st_bad "both-green: no transcript at $ST_LOG"
    fi
    if grep -q 'PRE-MERGE PASSED' "$ST_LOG" 2>/dev/null; then
        st_ok "both-green: the transcript carries the verdict"
    else
        st_bad "both-green: the transcript does not reach the verdict — $ST_LOG"
    fi
    if [ "$(tail -1 "$ST_LOG" 2>/dev/null)" = "$(tail -1 "$ST_TMP/e2e-both-green/_out.txt")" ]; then
        st_ok "both-green: the transcript's last line is the run's last line"
    else
        st_bad "both-green: the transcript is short of the run's last line — $ST_LOG"
    fi

    say "self-host freshness (the real predicate, against a synthetic tree)"

    # A repo-shaped git checkout whose gates/_common.sh is one line delegating to
    # the real one: _common.sh cd's to ITS SOURCER's parent, so the real
    # selfhost_bin_fresh and src_tree_hash then run over THIS tree.
    SH_REPO="$ST_TMP/selfhostrepo"
    mkdir -p "$SH_REPO/gates" "$SH_REPO/src" "$SH_REPO/artifacts/selfhost-fullcli"
    printf 'source "%s"\n' "$REPO/gates/_common.sh" > "$SH_REPO/gates/_common.sh"
    cp "$0" "$SH_REPO/gates/pre-merge.sh"
    : > "$SH_REPO/gates/build-and-run-a.sh"
    printf 'the transpiler\n' > "$SH_REPO/src/a.cs"
    git -C "$SH_REPO" init -q

    # sh_probe — the real predicate's answer for $SH_REPO: "<0|1> <hash> <bin>".
    # The binary's NAME comes back from the probe rather than being spelled here,
    # so the fixtures below write the file EXE_EXT actually makes it look for.
    sh_probe() { DN2CPP_PREMERGE_PROBE=1 bash "$SH_REPO/gates/pre-merge.sh" 2>/dev/null; }
    sh_bin() { local o; o=$(sh_probe); printf '%s' "${o##* }"; }
    sh_hash() { local o r; o=$(sh_probe); r=${o#* }; printf '%s' "${r%% *}"; }

    # sh_case NAME WANT — assert the freshness answer (0 fresh, 1 rebuild).
    sh_case() {
        local got o
        o=$(sh_probe)
        got=${o%% *}
        if [ "$got" = "$2" ]; then
            st_ok "$1 -> $([ "$2" = 0 ] && echo fresh || echo rebuild)"
        else
            st_bad "$1 -> '$got' (expected $2); probe said '$o'"
        fi
    }

    SH_BIN=$(sh_bin)
    SH_STAMP="$SH_REPO/artifacts/selfhost-fullcli/dn2cpp.src-hash"
    if [ -n "$SH_BIN" ]; then
        st_ok "the probe answers for a synthetic tree: $SH_BIN"
    else
        st_bad "the probe produced no answer for $SH_REPO — see gates/_common.sh"
    fi

    sh_case "no binary at all" 1
    printf '#!/bin/sh\n' > "$SH_REPO/$SH_BIN"
    chmod +x "$SH_REPO/$SH_BIN"
    # Unknown provenance, not a pass: this is the state a hand-copied binary is in.
    sh_case "a binary with no stamp" 1
    printf 'deadbeefdeadbeef\n' > "$SH_STAMP"
    sh_case "a binary stamped for other sources" 1
    printf '%s\n' "$(sh_hash)" > "$SH_STAMP"
    sh_case "a binary stamped for these sources" 0
    # Content, not mtime: an edit under src/ that leaves the stamp file alone is
    # what a `git checkout` between branches looks like from here.
    printf 'the transpiler, edited\n' > "$SH_REPO/src/a.cs"
    sh_case "a source edit under a matching stamp" 1

    # --dry-run must answer the reuse question BEFORE the hours, in both states.
    printf '%s\n' "$(sh_hash)" > "$SH_STAMP"
    SH_DRY=0
    DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$ST_TMP/e2e-selfhost-dry" \
        bash "$SH_REPO/gates/pre-merge.sh" --dry-run >"$ST_TMP/selfhost-dry.txt" 2>&1 || SH_DRY=$?
    if [ "$SH_DRY" = 0 ] && grep -q 'NOT rebuilt' "$ST_TMP/selfhost-dry.txt"; then
        st_ok "--dry-run reports a current binary as not-rebuilt"
    else
        st_bad "--dry-run (exit $SH_DRY) does not report the reuse — see $ST_TMP/selfhost-dry.txt"
    fi
    rm -f "$SH_STAMP"
    DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$ST_TMP/e2e-selfhost-dry" \
        bash "$SH_REPO/gates/pre-merge.sh" --dry-run >"$ST_TMP/selfhost-dry2.txt" 2>&1 || SH_DRY=$?
    if grep -q 'gates/selfhost-emit.sh' "$ST_TMP/selfhost-dry2.txt"; then
        st_ok "--dry-run prints the command a stale binary would run"
    else
        st_bad "--dry-run does not name gates/selfhost-emit.sh — see $ST_TMP/selfhost-dry2.txt"
    fi

    # The three modes the transcript must not wrap. --dry-run and the probe are
    # asserted by what they leave on disk: neither may create a LOGROOT at all.
    ST_NOLOG="$ST_TMP/nolog-dryrun"
    DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$ST_NOLOG" \
        bash "$FAKE/gates/pre-merge.sh" --dry-run >/dev/null 2>&1
    if [ -e "$ST_NOLOG" ]; then
        st_bad "--dry-run created $ST_NOLOG"
    else
        st_ok "--dry-run writes no transcript and no LOGROOT"
    fi
    ST_NOLOG="$ST_TMP/nolog-probe"
    DN2CPP_PREMERGE_PROBE=1 DN2CPP_PREMERGE_LOGROOT="$ST_NOLOG" \
        bash "$SH_REPO/gates/pre-merge.sh" >/dev/null 2>&1
    if [ -e "$ST_NOLOG" ]; then
        st_bad "the probe created $ST_NOLOG"
    else
        st_ok "the probe writes no transcript and no LOGROOT"
    fi
    # And the self-test: reaching this line un-re-executed IS the assertion.
    if [ "${DN2CPP_PREMERGE_TRANSCRIPT:-0}" = "1" ]; then
        st_bad "the self-test is running inside a transcript re-exec"
    else
        st_ok "the self-test is not wrapped in a transcript"
    fi

    say "premerge_selfhost_argv"

    premerge_selfhost_argv
    ARGV_SH="${PREMERGE_SELFHOST_ARGV[*]}"
    case "$ARGV_SH" in
        *"$REPO/gates/selfhost-emit.sh"*) st_ok "the argv names this repo's selfhost-emit.sh" ;;
        *) st_bad "the argv does not name gates/selfhost-emit.sh: $ARGV_SH" ;;
    esac
    # The one thing the argv cannot prove about itself: that the script is there.
    if [ -f "$REPO/gates/selfhost-emit.sh" ]; then
        st_ok "gates/selfhost-emit.sh exists"
    else
        st_bad "gates/selfhost-emit.sh does not exist — the phase would die at exec"
    fi

    # A pre-merge that honours SKIP_GODOT would be a merge gate with seventeen
    # gates missing; the refusal has to come from here, not as a mystery exit 2
    # out of run #1.
    SG_RC=0
    SKIP_GODOT=1 DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$ST_TMP/e2e-skipgodot" \
        bash "$FAKE/gates/pre-merge.sh" >"$ST_TMP/skipgodot.txt" 2>&1 || SG_RC=$?
    if [ "$SG_RC" = "2" ] && grep -q 'SKIP_GODOT=1 is set' "$ST_TMP/skipgodot.txt"; then
        st_ok "SKIP_GODOT=1 is refused up front (exit 2)"
    else
        st_bad "SKIP_GODOT=1 -> exit $SG_RC without the refusal message"
    fi

    say "premerge_cmake_cache_warn"

    # The table lookup the scan reads both a cache and a configure stamp
    # through. The third case is the load-bearing one: a name given twice is the
    # last one cmake acted on.
    ST_TBL='DN2CPP_ALPHA ON
DN2CPP_BETA OFF
DN2CPP_ALPHA OFF'
    # st_lookup NAME WANT
    st_lookup() {
        _premerge_lookup "$1" "$ST_TBL"
        if [ "$_PL_VALUE" = "$2" ]; then
            st_ok "_premerge_lookup $1 -> '${2:-<empty>}'"
        else
            st_bad "_premerge_lookup $1 -> '$_PL_VALUE' (expected '$2')"
        fi
    }
    st_lookup DN2CPP_GAMMA ""
    st_lookup DN2CPP_BETA OFF
    st_lookup DN2CPP_ALPHA OFF

    # st_repo NAME — a repo-shaped dir declaring two options (ALPHA ON, BETA
    # OFF) and carrying the real _common.sh's stamp-name assignment, so the
    # fixtures below use whatever name that file uses today.
    st_repo() {
        local r="$ST_TMP/repo-$1"
        mkdir -p "$r/runtime" "$r/artifacts" "$r/gates"
        cat > "$r/runtime/CMakeLists.txt" <<'OPTS'
option(DN2CPP_ALPHA "alpha" ON)
option(DN2CPP_BETA  "beta"  OFF)
OPTS
        grep '^_CMAKE_CONFIGURE_STAMP=' "$REPO/gates/_common.sh" > "$r/gates/_common.sh" 2>/dev/null || :
        printf '%s' "$r"
    }

    # st_builddir REPO RELDIR HOME STAMP CACHE — STAMP is the stamp file's
    # content, or "-" for a build dir that carries none.
    st_builddir() {
        local repo="$1" dir="$1/$2" home="$3" stamp="$4" cache="$5"
        mkdir -p "$dir"
        { printf 'CMAKE_HOME_DIRECTORY:INTERNAL=%s\n' "$home"; printf '%s' "$cache"; } \
            > "$dir/CMakeCache.txt"
        if [ "$stamp" = "-" ]; then
            rm -f "$dir/$(_premerge_stamp_name "$repo")"
        else
            printf '%s\n' "$stamp" > "$dir/$(_premerge_stamp_name "$repo")"
        fi
    }

    # st_warn REPO — run the REAL check. Not through a command substitution:
    # PREMERGE_STALE has to survive, and a subshell would eat it.
    st_warn() {
        ST_WARN_OUT="$1/_warn.txt"
        PREMERGE_STALE=-1
        premerge_cmake_cache_warn "$1" > "$ST_WARN_OUT" 2>&1
    }

    # st_stale NAME EXPECT_COUNT — assert the last st_warn's dir count.
    st_stale() {
        if [ "$PREMERGE_STALE" = "$2" ]; then
            st_ok "$1: $PREMERGE_STALE stale dir(s)"
        else
            st_bad "$1: $PREMERGE_STALE stale dir(s), expected $2 — see $ST_WARN_OUT"
        fi
    }

    # st_says NAME PATTERN — the warning has to NAME the thing; a count with no
    # directory in it is not actionable.
    st_says() {
        if grep -q -- "$2" "$ST_WARN_OUT"; then
            st_ok "$1: output carries '$2'"
        else
            st_bad "$1: output lacks '$2' — see $ST_WARN_OUT"
        fi
    }

    # The stamp name is _common.sh's to define. A rename there must reach this
    # script, because the option check silently covers NOTHING without a stamp.
    if grep -q '^_CMAKE_CONFIGURE_STAMP=' "$REPO/gates/_common.sh" 2>/dev/null; then
        st_ok "the real gates/_common.sh still defines _CMAKE_CONFIGURE_STAMP"
    else
        st_bad "gates/_common.sh no longer defines _CMAKE_CONFIGURE_STAMP — the option check would read no stamp"
    fi
    ST_REAL_STAMP=$(_premerge_stamp_name "$REPO")
    ST_FILE_STAMP=$(sed -n 's/^_CMAKE_CONFIGURE_STAMP=//p' "$REPO/gates/_common.sh")
    if [ -n "$ST_REAL_STAMP" ] && [ "$ST_REAL_STAMP" = "${ST_FILE_STAMP%%$'\n'*}" ]; then
        st_ok "stamp name read from _common.sh: $ST_REAL_STAMP"
    else
        st_bad "stamp name '$ST_REAL_STAMP' does not match _common.sh's"
    fi

    # The option parser against the REAL runtime/CMakeLists.txt, so a reformat
    # there cannot quietly reduce the check to zero options. Both halves are
    # re-derived from the file — no number is written down here.
    ST_OPTS=$(sed -n \
        's/^[[:space:]]*option([[:space:]]*\([A-Za-z_][A-Za-z0-9_]*\)[[:space:]]*"[^"]*"[[:space:]]*\([A-Za-z0-9_]*\)[[:space:]]*).*$/\1 \2/p' \
        "$REPO/runtime/CMakeLists.txt")
    ST_OPT_N=$(printf '%s' "$ST_OPTS" | grep -c . || true)
    ST_OPT_DECL=$(grep -c '^[[:space:]]*option(' "$REPO/runtime/CMakeLists.txt" || true)
    if [ "$ST_OPT_N" -gt 0 ] && [ "$ST_OPT_N" = "$ST_OPT_DECL" ]; then
        st_ok "parsed every option() in runtime/CMakeLists.txt ($ST_OPT_N)"
    else
        st_bad "parsed $ST_OPT_N of $ST_OPT_DECL option() lines in runtime/CMakeLists.txt"
    fi
    if [ -z "$(printf '%s\n' "$ST_OPTS" | grep -v '^[A-Za-z_][A-Za-z0-9_]* \(ON\|OFF\)$')" ]; then
        st_ok "every parsed default is a plain ON/OFF"
    else
        st_bad "a parsed option default is not ON/OFF: $(head -1 <<<"$(grep -v '^[A-Za-z_][A-Za-z0-9_]* \(ON\|OFF\)$' <<<"$ST_OPTS")")"
    fi

    # A tree with nothing built yet, and one with an artifacts/ holding no
    # cache: both are silent-but-accounted-for, not a warning.
    ST_R=$(st_repo empty); rm -rf "$ST_R/artifacts"
    st_warn "$ST_R"; st_stale "no artifacts/" 0
    st_says "no artifacts/" 'no artifacts/'
    ST_R=$(st_repo nocache)
    st_warn "$ST_R"; st_stale "artifacts/ with no build dir" 0

    ST_STAMP_OK='cmake
-S
runtime
-G
Ninja'

    ST_R=$(st_repo clean)
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_R/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=ON
DN2CPP_BETA:BOOL=OFF
'
    st_warn "$ST_R"; st_stale "a current build dir" 0
    st_says "a current build dir" 'none stale'

    # Form 1: a cache carried along by a moved repository.
    ST_R=$(st_repo moved)
    st_builddir "$ST_R" artifacts/.cmake-runtime /somewhere/else/dn2cpp/runtime "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=ON
'
    st_warn "$ST_R"; st_stale "a cache naming another tree" 1
    st_says "a cache naming another tree" 'artifacts/.cmake-runtime'
    st_says "a cache naming another tree" '/somewhere/else/dn2cpp/runtime'
    st_says "a cache naming another tree" 'rm -rf'

    # ...but a build dir configured from a DIFFERENT source dir inside this
    # same repository is not stale: the P/Invoke gates build
    # samples/native/dn2cpptest into their own build dir.
    ST_R=$(st_repo othersrc)
    mkdir -p "$ST_R/samples/native/dn2cpptest"
    st_builddir "$ST_R" artifacts/pinvoke/.cmake-testlib "$ST_R/samples/native/dn2cpptest" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=ON
'
    st_warn "$ST_R"; st_stale "another source dir in the same repo" 0

    # The old path still EXISTING is not a defence: a second checkout at the
    # path the cache remembers is exactly what a repository copy leaves behind,
    # and the dir is no less stale for it.
    ST_R=$(st_repo othertree)
    mkdir -p "$ST_TMP/some-other-checkout/runtime"
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_TMP/some-other-checkout/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=ON
'
    st_warn "$ST_R"; st_stale "a cache naming another EXISTING tree" 1

    # ...and neither is one whose recorded path reaches this tree through a
    # symlink. /tmp is a symlink to /private/tmp on macOS, so a string compare
    # would call every build dir under it stale.
    ST_R=$(st_repo symlinked)
    ln -sfn "$ST_R" "$ST_TMP/link-to-symlinked"
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_TMP/link-to-symlinked/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=ON
'
    st_warn "$ST_R"; st_stale "a home dir reached through a symlink" 0

    # Form 2: an option()'s default flipped after the dir was configured. The
    # stamp is identical to the clean case — that is the whole point.
    ST_R=$(st_repo frozen)
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_R/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=OFF
'
    st_warn "$ST_R"; st_stale "an option frozen at its pre-flip default" 1
    st_says "an option frozen at its pre-flip default" 'DN2CPP_ALPHA is cached OFF'
    st_says "an option frozen at its pre-flip default" 'wants ON'
    st_says "an option frozen at its pre-flip default" 'artifacts/.cmake-runtime'

    # The false positive that would make the check unusable: an axis dir whose
    # configure DELIBERATELY passed the non-default value (-DDN2CPP_USE_CURL=OFF
    # is what the nocurl axis is).
    ST_R=$(st_repo override)
    st_builddir "$ST_R" artifacts/.cmake-runtime-nocurl "$ST_R/runtime" "$ST_STAMP_OK
-DDN2CPP_ALPHA=OFF" \
'DN2CPP_ALPHA:BOOL=OFF
'
    st_warn "$ST_R"; st_stale "a deliberate -D override" 0

    # A dir with no stamp cannot be judged — the -D set is unknown — and does
    # not need to be: _cmake_configure throws such a dir away on first use.
    ST_R=$(st_repo nostamp)
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_R/runtime" - \
'DN2CPP_ALPHA:BOOL=OFF
'
    st_warn "$ST_R"; st_stale "a stamp-less build dir" 0

    # CMake truthiness: a cache written `1` is not a flip of a default written `ON`.
    ST_R=$(st_repo truthy)
    st_builddir "$ST_R" artifacts/.cmake-runtime "$ST_R/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=1
DN2CPP_BETA:BOOL=0
'
    st_warn "$ST_R"; st_stale "ON/1 and OFF/0 compared normalized" 0

    # The per-gate app dirs are scanned too, and one freeze across several dirs
    # is ONE warning naming the count — not one line per dir.
    ST_R=$(st_repo appdirs)
    for st_g in one two; do
        st_builddir "$ST_R" "artifacts/$st_g/.cmake" "$ST_R/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=OFF
'
    done
    st_warn "$ST_R"; st_stale "a freeze across two app build dirs" 2
    if [ "$(grep -c 'DN2CPP_ALPHA is cached OFF' "$ST_WARN_OUT")" = "1" ]; then
        st_ok "one grouped warning for two dirs"
    else
        st_bad "the grouped warning was not emitted exactly once — see $ST_WARN_OUT"
    fi
    st_says "a freeze across two app build dirs" '2 build dir(s)'

    # Past a handful of dirs the remedy is the glob: a `rm -rf` naming dozens of
    # paths is one nobody reads, let alone runs.
    ST_R=$(st_repo many)
    st_i=0
    while [ "$st_i" -lt 9 ]; do
        st_builddir "$ST_R" "artifacts/g$st_i/.cmake" "$ST_R/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=OFF
'
        st_i=$((st_i + 1))
    done
    st_warn "$ST_R"; st_stale "nine stale dirs" 9
    st_says "nine stale dirs" 'rm -rf .*artifacts/\.cmake-\*'

    say "end-to-end (the warning does not change the verdict)"

    # The whole script over a tree with a stale build dir: green stays green,
    # the warning is printed, and the receipt records what the green was
    # produced over.
    STALE_REPO="$ST_TMP/stalerepo"
    mkdir -p "$STALE_REPO/runtime" "$STALE_REPO/artifacts"
    cp -R "$FAKE/gates" "$STALE_REPO/gates"
    cat > "$STALE_REPO/runtime/CMakeLists.txt" <<'OPTS'
option(DN2CPP_ALPHA "alpha" ON)
OPTS
    st_builddir "$STALE_REPO" artifacts/.cmake-runtime "$STALE_REPO/runtime" "$ST_STAMP_OK" \
'DN2CPP_ALPHA:BOOL=OFF
'
    SR_ROOT="$ST_TMP/e2e-stalecache"
    mkdir -p "$SR_ROOT"
    SR_RC=0
    DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$SR_ROOT" \
    DN2CPP_GODOT_FORK_ROOT="$ST_FORKROOT" \
    DN2CPP_STUB_RC_Release=0 DN2CPP_STUB_RC_Debug=0 \
        bash "$STALE_REPO/gates/pre-merge.sh" >"$SR_ROOT/_out.txt" 2>&1 || SR_RC=$?
    if [ "$SR_RC" = "0" ] && grep -q 'DN2CPP_ALPHA is cached OFF' "$SR_ROOT/_out.txt"; then
        st_ok "stale cache: warned, and the run still passed (exit 0)"
    else
        st_bad "stale cache: exit $SR_RC without the warning — see $SR_ROOT/_out.txt"
    fi
    if grep -q '^stale:   1 CMake build dir' "$SR_ROOT/_receipt.txt" 2>/dev/null; then
        st_ok "stale cache: the receipt records what the green was produced over"
    else
        st_bad "stale cache: the receipt does not record the warning — $SR_ROOT/_receipt.txt"
    fi
    # A clean tree's receipt must NOT carry the line: a receipt that always says
    # something about staleness says nothing.
    if grep -q '^stale:' "$ST_TMP/e2e-both-green/_receipt.txt" 2>/dev/null; then
        st_bad "a clean run's receipt carries a 'stale:' line"
    else
        st_ok "a clean run's receipt carries no 'stale:' line"
    fi
    # --dry-run answers the question before the two hours, not after them.
    DR_RC=0
    DN2CPP_PREMERGE_SELFTEST=0 DN2CPP_PREMERGE_LOGROOT="$ST_TMP/e2e-dryrun" \
        bash "$STALE_REPO/gates/pre-merge.sh" --dry-run >"$ST_TMP/dryrun.txt" 2>&1 || DR_RC=$?
    if [ "$DR_RC" = "0" ] && grep -q 'DN2CPP_ALPHA is cached OFF' "$ST_TMP/dryrun.txt"; then
        st_ok "--dry-run reports the stale build dir (exit 0)"
    else
        st_bad "--dry-run did not report the stale build dir (exit $DR_RC) — see $ST_TMP/dryrun.txt"
    fi

    say "self-test summary"
    printf '  %d passed, %d failed\n' "$ST_PASS" "$ST_FAIL"
    [ "$ST_FAIL" -eq 0 ] || exit 1
    exit 0
fi

# ── refusals ─────────────────────────────────────────────────────────────────

# SKIP_GODOT and REQUIRE_ALL are a contradiction the runner already refuses
# (exit 2). Catch it here so the message names the pre-merge contract rather
# than arriving as a mystery non-zero from run #1.
if [ "${SKIP_GODOT:-0}" = "1" ]; then
    bad "SKIP_GODOT=1 is set. The merge gate demands every gate run; there is no"
    bad "pre-merge run that skips the seventeen Godot gates. Unset it."
    exit 2
fi

if [ "$EXPECTED_GATES" -lt 1 ]; then
    bad "found no gates/build-and-run-*.sh under $REPO — wrong directory?"
    exit 2
fi

# ── drive ────────────────────────────────────────────────────────────────────

HEAD_SHA=$(git -C "$REPO" rev-parse --short HEAD 2>/dev/null || echo "unknown")
HEAD_BRANCH=$(git -C "$REPO" rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
DIRTY=""
[ -n "$(git -C "$REPO" status --porcelain --untracked-files=all 2>/dev/null)" ] && DIRTY=" (working tree DIRTY)"

say "dn2cpp pre-merge gate"
note "repo:   $REPO"
note "commit: $HEAD_BRANCH @ $HEAD_SHA$DIRTY"
note "gates:  $EXPECTED_GATES"
note "logs:   $LOGROOT"
[ "$DRY_RUN" = "0" ] && note "record: $TRANSCRIPT"

# Before anything long starts, and before --dry-run prints its plan: a person
# deciding whether to spend two hours is exactly who needs to know the tree
# they would spend them on carries a stale build dir.
say "CMake build-dir freshness"
PREMERGE_STALE=0
premerge_cmake_cache_warn "$REPO"

CONFIGS="Release Debug"
if [ "$DRY_RUN" = "1" ]; then
    say "dry run — nothing below is executed"
    premerge_culture_argv
    printf '\n  culture-invariance harness:\n    '
    printf '%s ' "${PREMERGE_CULTURE_ARGV[@]}"
    printf '\n    then assert: exit 0 (green) or 77 (host cannot decide); 1 is a red merge gate\n'
    premerge_selfhost_state
    printf '\n  self-hosted native CLI:\n'
    if [ "$SELFHOST_FRESH" = "0" ]; then
        printf '    %s already describes src %s — NOT rebuilt\n' "$SELFHOST_BIN" "$SELFHOST_SRC"
    else
        premerge_selfhost_argv
        printf '    %s does not describe src %s; this would run:\n      ' "$SELFHOST_BIN" "$SELFHOST_SRC"
        printf '%s ' "${PREMERGE_SELFHOST_ARGV[@]}"
        printf '\n'
    fi
    premerge_fork_state
    printf '\n  godot fork cache:\n'
    case "$FORK_STATE" in
        0)
            printf '    already describes fork %s (engine %s, tools %s) — NOT rebuilt\n' \
                "$FORK_HEAD_SHORT" "$FORK_ENGINE" "$FORK_TOOLS"
            ;;
        3)
            printf '    %s — this host has no pre-merge run at all (exit 2)\n' "$FORK_WHY"
            ;;
        *)
            premerge_fork_argv
            printf '    %s; this would run:\n      ' "$FORK_WHY"
            printf '%s ' "${PREMERGE_FORK_ARGV[@]}"
            printf '\n'
            ;;
    esac
    if [ "$FORK_STATE" != "3" ]; then
        printf '\n  godot fork iOS template (manual prerequisite):\n'
        if premerge_fork_ios_state; then
            printf '    current and host prerequisites are available\n'
        elif [ "$FORK_IOS_KIND" = prerequisite ]; then
            printf '    %s — a host prerequisite; this run would continue, and the iOS gates go red at the verdict\n' "$FORK_IOS_WHY"
        else
            printf '    %s — this pre-merge run would refuse before its suites\n' "$FORK_IOS_WHY"
        fi
        premerge_fork_web_state
        printf '\n  godot fork web templates:\n'
        if [ -z "$FORK_WEB_STALE" ]; then
            printf '    both flavors are baked from these engine sources — NOT rebaked\n'
        else
            if [ "$FORK_WEB_SDK" != 1 ]; then
                premerge_emsdk_argv
                printf '    no mutable emcc resolves; this would run first, then bake:\n      '
                printf '%s ' "${PREMERGE_EMSDK_ARGV[@]}"
                printf '\n'
            fi
            for flavor in $FORK_WEB_STALE; do
                premerge_fork_web_argv "$flavor"
                printf '    %s is absent or stale; this would run:\n      ' "$flavor"
                printf '%s ' "${PREMERGE_FORK_WEB_ARGV[@]}"
                printf '\n'
            done
        fi
    fi
    for cfg in $CONFIGS; do
        logdir="$LOGROOT/$(printf '%s' "$cfg" | tr 'A-Z' 'a-z')"
        premerge_argv "$cfg" "$logdir"
        printf '\n  %s run:\n    ' "$cfg"
        # Quote each word so the printed line is a runnable command.
        for w in "${PREMERGE_ARGV[@]}"; do
            case "$w" in
                *[!A-Za-z0-9_=/.-]*) printf "'%s' " "$w" ;;
                *)                   printf '%s ' "$w" ;;
            esac
        done
        printf '\n'
        printf '    then assert: exit 0, %s timing records, all "ran", %s empty, %s empty\n' \
            "$EXPECTED_GATES" "$logdir/_skips.txt" "$logdir/_failures.txt"
    done
    printf '\n'
    note "Both runs must be green. --keep-going runs Debug even after a red Release."
    note "Receipt on success: $RECEIPT"
    exit 0
fi

mkdir -p "$LOGROOT"
# OVERALL is the verdict; INPUTS_RED is the narrower "may the suites start". They
# part where a HOST prerequisite is missing: that makes the verdict red but is no
# reason to withhold every gate this host can run. Only a broken suite INPUT —
# culture, self-host binary, fork cache, a bake that failed — withholds them.
# HOST_GAPS lists the missing prerequisites for the verdict.
OVERALL=0
INPUTS_RED=0
SUITE_RED=0
HOST_GAPS=""
RESULTS=""
T0=$(date +%s)

# ── phase 0: culture invariance ──────────────────────────────────────────────
# Before the hours, not after: a bucket added without its culture pin is found
# here in minutes, and its remedy is a one-line edit that would otherwise be
# discovered on top of two green suites that have to be run again.
say "culture invariance (gates/verify-culture-invariance.sh)"
premerge_culture_argv
CULTURE_RC=0
CULTURE_NOTE="(unset)"
"${PREMERGE_CULTURE_ARGV[@]}" 2>&1 | tee "$LOGROOT/_culture.log"
CULTURE_RC=${PIPESTATUS[0]}
case "$CULTURE_RC" in
    0)
        CULTURE_NOTE="verified host-independent"
        good "culture invariance: every gate bucket is host-independent."
        RESULTS="${RESULTS}culture=green "
        ;;
    77)
        # Not a pass and not a failure: this host cannot move .NET's culture from
        # the environment, so the harness compared nothing. Named in the verdict
        # AND in the receipt, because a receipt that hid it would read as the
        # check having been performed.
        CULTURE_NOTE="NOT PERFORMED (this host ignores LANG/LC_ALL)"
        note "culture invariance: NOT PERFORMED — this host ignores LANG/LC_ALL"
        note "(Windows). The property still has to be checked somewhere; a POSIX"
        note "host's pre-merge run is where."
        RESULTS="${RESULTS}culture=not-performed "
        ;;
    *)
        bad "culture invariance: FAILED (exit $CULTURE_RC) — see $LOGROOT/_culture.log"
        note "A bucket's verdict is a property of the developer's locale. The remedy"
        note "is the driver pin the harness names; do not merge without it."
        RESULTS="${RESULTS}culture=RED "
        OVERALL=1
        INPUTS_RED=1
        ;;
esac

# ── phase 1: the self-hosted native CLI the suites require ───────────────────
# The reuse condition here MUST be godot_fork_preflight's accept condition, which
# is why neither is written twice: a build this phase skips and the fork gates
# then demand is the worst failure shape available — hours of green, then a
# REQUIRE_ALL skip over a file this run could have produced in minutes.
say "self-host native CLI (gates/selfhost-emit.sh)"
premerge_selfhost_state
SELFHOST_NOTE="(unset)"
if [ "$SELFHOST_FRESH" = "0" ]; then
    SELFHOST_NOTE="reused (src $SELFHOST_SRC)"
    good "self-host: $SELFHOST_BIN already describes these sources."
    note "reusing the binary stamped src $SELFHOST_SRC — nothing to rebuild."
    RESULTS="${RESULTS}selfhost=reused "
else
    premerge_selfhost_argv
    "${PREMERGE_SELFHOST_ARGV[@]}" 2>&1 | tee "$LOGROOT/_selfhost.log"
    SELFHOST_RC=${PIPESTATUS[0]}
    if [ "$SELFHOST_RC" -eq 0 ]; then
        SELFHOST_NOTE="rebuilt (src $SELFHOST_SRC)"
        good "self-host: rebuilt $SELFHOST_BIN from src $SELFHOST_SRC."
        RESULTS="${RESULTS}selfhost=rebuilt "
    else
        # dist/package-toolchain.sh downgrades this same failure to a warning,
        # because it wants the binary step 4 links and a step-5 fixpoint
        # divergence is a separate question. A merge gate has no such licence:
        # the divergence is precisely a thing that must not merge.
        SELFHOST_NOTE="FAILED (exit $SELFHOST_RC)"
        bad "self-host: FAILED (exit $SELFHOST_RC) — see $LOGROOT/_selfhost.log"
        RESULTS="${RESULTS}selfhost=RED "
        OVERALL=1
        INPUTS_RED=1
    fi
fi

# ── phase 2: the fork cache the Godot lane requires ──────────────────────────
# Phase 1's argument, for the other input the fork gates demand, and AFTER it
# because it consumes it: step 3 of gates/setup-godot-fork.sh packages the
# self-hosted CLI into the export toolchain the editor runs.
#
# The reuse condition here MUST be godot_fork_preflight's accept condition, for
# the same reason phase 1 shares selfhost_bin_fresh: a refresh this phase skips
# and the fork gates then refuse is hours of green followed by a REQUIRE_ALL
# failure over a cache this run could have rebuilt first.
say "godot fork cache (gates/setup-godot-fork.sh)"
premerge_fork_state
FORK_NOTE="(unset)"
FORK_RUN=0
case "$FORK_STATE" in
    0)
        FORK_NOTE="reused (fork $FORK_HEAD_SHORT, engine $FORK_ENGINE, tools $FORK_TOOLS)"
        good "fork: the cache already describes fork $FORK_HEAD_SHORT."
        note "engine $FORK_ENGINE, tools $FORK_TOOLS — nothing to rebuild."
        RESULTS="${RESULTS}fork=reused "
        ;;
    3)
        # Refused before the first suite, not discovered inside it. This box
        # cannot produce a fork cache at all, and DN2CPP_REQUIRE_ALL=1 turns the
        # seventeen Godot gates' skips into failures — so there is no pre-merge
        # run here, and saying so now costs nothing where saying it later costs
        # the whole Release suite.
        bad "fork: $FORK_WHY."
        bad "There is no pre-merge run on this host: gates/setup-godot-fork.sh"
        bad "builds an engine only for macOS, Windows and Linux, and the merge gate"
        bad "demands every Godot gate run. Use a desktop host."
        exit 2
        ;;
    2)
        note "fork: no cache on this box yet — building one for fork ${FORK_HEAD_SHORT}."
        note "($FORK_WHY. A cold build is tens of minutes when the fork edits engine C++.)"
        FORK_RUN=1
        ;;
    *)
        note "fork: the cache no longer describes fork ${FORK_HEAD_SHORT} — refreshing it."
        note "($FORK_WHY)"
        FORK_RUN=1
        ;;
esac
if [ "$FORK_RUN" = "1" ]; then
    premerge_fork_argv
    "${PREMERGE_FORK_ARGV[@]}" 2>&1 | tee "$LOGROOT/_fork.log"
    FORK_RC=${PIPESTATUS[0]}
    if [ "$FORK_RC" -eq 0 ]; then
        # Re-ask before the suites, both to prove setup's exit 0 produced the
        # promised cache and to record POST-build hashes. A cold-cache probe has
        # no engine/tools values, so reusing its fields would put `unknown` in the
        # receipt that exists to identify what the green ran over.
        premerge_fork_state
        if [ "$FORK_STATE" = 0 ]; then
            FORK_NOTE="rebuilt (fork $FORK_HEAD_SHORT, engine $FORK_ENGINE, tools $FORK_TOOLS)"
            good "fork: rebuilt the cache for fork $FORK_HEAD_SHORT."
            note "engine $FORK_ENGINE, tools $FORK_TOOLS — post-build freshness verified."
            RESULTS="${RESULTS}fork=rebuilt "
        else
            FORK_NOTE="FAILED (setup exited 0; post-build probe: $FORK_WHY)"
            bad "fork: setup exited 0 but the rebuilt cache is not current."
            note "$FORK_WHY"
            RESULTS="${RESULTS}fork=RED "
            OVERALL=1
            INPUTS_RED=1
        fi
    else
        FORK_NOTE="FAILED (exit $FORK_RC)"
        bad "fork: FAILED (exit $FORK_RC) — see $LOGROOT/_fork.log"
        RESULTS="${RESULTS}fork=RED "
        OVERALL=1
        INPUTS_RED=1
    fi
fi

# The iOS artifact stays manual: unlike desktop and Web, producing it needs an
# official templates archive and an Xcode simulator toolchain. The two ways it
# can be not ready are treated differently. A repairable ARTIFACT (stale or
# missing zip on a host with Xcode) refuses before the suites, rather than
# converting the iOS gate's late REQUIRE_ALL skip into an hour-delayed surprise
# over minutes of manual work. A missing HOST prerequisite cannot be repaired
# here, so refusing would withhold every other gate for nothing: the run
# continues, the iOS gates go red under REQUIRE_ALL, and the verdict names the
# gap. Red, never skipped — the merge still needs a host that can run them.
if [ "$INPUTS_RED" -eq 0 ] && ! premerge_fork_ios_state; then
    if [ "$FORK_IOS_KIND" = prerequisite ]; then
        bad "fork iOS template: $FORK_IOS_WHY"
        note "A host prerequisite, not a repairable artifact: the suites still run,"
        note "and the iOS gates are reported red at the verdict."
        RESULTS="${RESULTS}forkios=RED "
        HOST_GAPS="${HOST_GAPS}iOS template ($FORK_IOS_WHY); "
        OVERALL=1
    else
        bad "fork iOS template: $FORK_IOS_WHY"
        note "Prepare it manually, then rerun pre-merge:"
        note "  FORCE=1 ./gates/setup-godot-fork-ios.sh"
        exit 2
    fi
fi

# The Web export templates, on the same argument and with one difference: absent
# and stale are ONE answer, because under DN2CPP_REQUIRE_ALL=1 a missing template
# is no longer a skip. premerge_fork_web_state says why.
#
# Only reached when the desktop cache is good, since the bake reads the fork
# editor's version and the engine sources the provenance is taken over — baking
# over a broken cache would stamp the zips against a tree the next step moves.
# A missing iOS prerequisite is not such a reason: the Web bake does not read it.
FORK_WEB_NOTE="not asked"
if [ "$INPUTS_RED" -eq 0 ]; then
    premerge_fork_web_state
    FORK_WEB_BAKE=1
    if [ -z "$FORK_WEB_STALE" ]; then
        FORK_WEB_NOTE="reused (both flavors current)"
        good "fork web templates: both flavors are baked from these engine sources."
        RESULTS="${RESULTS}forkweb=reused "
        FORK_WEB_BAKE=0
    elif [ "$FORK_WEB_SDK" != 1 ]; then
        # Provisioned, not refused: the bake needs the mutable Emscripten SDK, and
        # gates/setup-emsdk.sh unpacks the pinned one into the path the resolver
        # searches. Asked once — a second miss after a successful unpack means the
        # SDK is unusable on this host, which is a host gap, not a retry.
        note "fork web templates: no mutable Emscripten SDK — unpacking the pinned one."
        note "(gates/setup-emsdk.sh; the first run downloads the archive, later runs skip.)"
        premerge_emsdk_argv
        "${PREMERGE_EMSDK_ARGV[@]}" 2>&1 | tee "$LOGROOT/_emsdk.log"
        rc_emsdk=${PIPESTATUS[0]}
        [ "$rc_emsdk" -eq 0 ] && premerge_fork_web_state
        if [ "$rc_emsdk" -ne 0 ] || [ "$FORK_WEB_SDK" != 1 ]; then
            FORK_WEB_NOTE="FAILED (SDK provisioning; flavors due: $FORK_WEB_STALE)"
            bad "fork web templates: gates/setup-emsdk.sh did not yield a usable emcc (exit $rc_emsdk)"
            note "See $LOGROOT/_emsdk.log"
            RESULTS="${RESULTS}forkweb=RED "
            HOST_GAPS="${HOST_GAPS}Web templates (no emcc); "
            OVERALL=1
            FORK_WEB_BAKE=0
        fi
    fi
    if [ "$FORK_WEB_BAKE" = 1 ]; then
        FORK_WEB_NOTE="rebaked ($FORK_WEB_STALE)"
        bake_rc=0
        for flavor in $FORK_WEB_STALE; do
            note "fork web templates: baking the $flavor flavor."
            premerge_fork_web_argv "$flavor"
            "${PREMERGE_FORK_WEB_ARGV[@]}" 2>&1 | tee "$LOGROOT/_forkweb-$flavor.log"
            rc_web=${PIPESTATUS[0]}
            if [ "$rc_web" -ne 0 ]; then
                # The zips are now a broken INPUT, not a host gap: the suites
                # would fail over them, so they are withheld like a red fork cache.
                FORK_WEB_NOTE="FAILED ($flavor, exit $rc_web)"
                bad "fork web templates: the $flavor bake FAILED (exit $rc_web)"
                note "See $LOGROOT/_forkweb-$flavor.log"
                RESULTS="${RESULTS}forkweb=RED "
                OVERALL=1
                INPUTS_RED=1
                bake_rc=1
                break
            fi
        done
        # godot_fork_template_check is the one that re-asks, in every consumer,
        # for the reason written at the desktop arm above.
        [ "$bake_rc" -eq 0 ] && {
            good "fork web templates: rebaked $FORK_WEB_STALE."
            RESULTS="${RESULTS}forkweb=rebaked "
        }
    fi
fi

for cfg in $CONFIGS; do
    logdir="$LOGROOT/$(printf '%s' "$cfg" | tr 'A-Z' 'a-z')"
    # Withheld after a broken input or a plainly red suite, never for a host
    # gap: a red Release already answers the merge question, but a Release whose
    # only failures are gates this host lacks the prerequisites for says nothing
    # about the gates it can run — so Debug runs, and the gap is listed instead.
    if { [ "$INPUTS_RED" -ne 0 ] || [ "$SUITE_RED" -ne 0 ]; } && [ "$KEEP_GOING" != "1" ]; then
        bad "$cfg: NOT RUN — an earlier input or suite failed (pass --keep-going to run both regardless)."
        RESULTS="$RESULTS$cfg=not-run "
        continue
    fi
    premerge_argv "$cfg" "$logdir"
    say "$cfg suite (DN2CPP_REQUIRE_ALL=1, DN2CPP_GATE_CACHE=0)"
    t_cfg=$(date +%s)
    rc=0
    "${PREMERGE_ARGV[@]}" || rc=$?
    printf '\n'
    if premerge_verdict "$cfg" "$rc" "$logdir" "$EXPECTED_GATES"; then
        RESULTS="$RESULTS$cfg=green "
    else
        RESULTS="$RESULTS$cfg=RED "
        OVERALL=1
        if [ "$VERDICT_PREREQ_ONLY" = 1 ]; then
            note "$cfg: red only for prerequisites this host lacks; the other configuration is not withheld."
            HOST_GAPS="${HOST_GAPS}$cfg: $VERDICT_PREREQ_GATES (prerequisite absent); "
        else
            SUITE_RED=1
        fi
    fi
    note "$cfg elapsed: $(( $(date +%s) - t_cfg ))s   logs: $logdir"
done

ELAPSED=$(( $(date +%s) - T0 ))

say "pre-merge verdict"
note "commit:  $HEAD_BRANCH @ $HEAD_SHA$DIRTY"
note "results: $RESULTS"
note "elapsed: ${ELAPSED}s"

if [ "$OVERALL" -eq 0 ]; then
    # The receipt is what makes "I ran the merge gate" checkable by somebody who
    # was not there. It records the commit, because a green run of a different
    # tree is not evidence about this one.
    {
        printf 'dn2cpp pre-merge PASSED\n'
        printf 'when:    %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
        printf 'repo:    %s\n' "$REPO"
        printf 'commit:  %s @ %s%s\n' "$HEAD_BRANCH" "$HEAD_SHA" "$DIRTY"
        printf 'gates:   %s per config\n' "$EXPECTED_GATES"
        printf 'culture: %s\n' "$CULTURE_NOTE"
        # Which sources the binary the fork lane ran on was built from: a green
        # produced over somebody else's self-host binary is a different claim.
        printf 'selfhost: %s\n' "$SELFHOST_NOTE"
        # And which FORK the Godot lane ran on. The editor drives that whole
        # pipeline, so a green produced over another fork head — or over a cache
        # that predates this one — is likewise a different claim, and the fork's
        # head is in no other line of this receipt.
        printf 'fork:    %s\n' "$FORK_NOTE"
        printf 'forkweb: %s\n' "$FORK_WEB_NOTE"
        printf 'runs:    %s\n' "$RESULTS"
        printf 'elapsed: %ss\n' "$ELAPSED"
        # What the green was produced over. A reader who was not there cannot
        # otherwise tell that these two suites ran against build dirs this tree
        # did not configure.
        [ "$PREMERGE_STALE" -gt 0 ] && \
            printf 'stale:   %s CMake build dir(s) warned about before the run\n' "$PREMERGE_STALE"
    } > "$RECEIPT"
    good "PRE-MERGE PASSED — both configs, $EXPECTED_GATES gates each, every gate ran."
    note "receipt: $RECEIPT"
    note "record:  $TRANSCRIPT"
    exit 0
fi

rm -f "$RECEIPT"
bad "PRE-MERGE FAILED — do not merge."
if [ -n "$HOST_GAPS" ]; then
    note "host prerequisites absent here: $HOST_GAPS"
    note "Those gates cannot pass on this host; the suites were not withheld for them."
fi
note "Per-gate logs: $LOGROOT/{release,debug}/<gate>.log"
note "This run's own output: $TRANSCRIPT"
exit 1
