#!/usr/bin/env bash
# pre-merge.sh — THE merge gate. The single canonical thing to run before a
# merge to `main`, so that "which two commands, under which config, with which
# environment" stops living in somebody's memory.
#
#     ./gates/pre-merge.sh                # the real thing (~1-2h, + a self-host build when one is due)
#     ./gates/pre-merge.sh --dry-run      # print the exact runs, execute nothing
#     ./gates/pre-merge.sh --keep-going   # run Debug even after Release fails
#     DN2CPP_PREMERGE_SELFTEST=1 ./gates/pre-merge.sh   # self-test, no suite run
#
# WHAT IT RUNS, AND WHY EXACTLY THIS. One harness, one build and two full
# suites, in this order:
#
#   0. gates/verify-culture-invariance.sh
#   1. gates/selfhost-emit.sh — only when the binary no longer describes src/
#   2. CONFIG=Release  DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
#   3. CONFIG=Debug    DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
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
# gates/selfhost-emit.sh is in on a different test again, and it is the only thing
# that may be: it answers no verdict at all — it produces a file the suites refuse
# to run without.
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

# ── the self-host freshness probe (a child of this script, never a source) ────
# The predicate is selfhost_bin_fresh (gates/_common.sh), and reaching it means
# BEING the script that sources _common.sh: it cd's to its sourcer's parent and
# turns on `set -euo pipefail`, so a `bash -c` sourcing it dies on its own `set
# -u` and this script must not inherit either option. Hence a child of itself in
# probe mode, printing `<0|1> <src-tree-hash> <binary path>` on one line.
# Ahead of the self-test block on purpose: the self-test spawns this mode.
if [ "${DN2CPP_PREMERGE_PROBE:-0}" = "1" ]; then
    source "$REPO/gates/_common.sh"
    probe_rc=0
    selfhost_bin_fresh || probe_rc=1
    printf '%s %s %s\n' "$probe_rc" "$SELFHOST_SRC_NOW" "$SELFHOST_BIN_PATH"
    exit 0
fi

DRY_RUN=0
KEEP_GOING=0
for arg in "$@"; do
    case "$arg" in
        --dry-run)    DRY_RUN=1 ;;
        --keep-going) KEEP_GOING=1 ;;
        -h|--help)
            sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'
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

# ── the verdict, re-derived from the run's own artifacts ─────────────────────

# premerge_verdict LABEL RC LOGDIR EXPECTED_GATES — 0 when the run is a genuine
# green, 1 otherwise. Collects every problem rather than stopping at the first:
# a run this long should hand back everything it knows in one pass.
premerge_verdict() {
    local label="$1" rc="$2" logdir="$3" expected="$4"
    local timings="$logdir/_timings.txt"
    local skips="$logdir/_skips.txt"
    local fails="$logdir/_failures.txt"
    local bad_count=0
    local n_lines n_ran n_other

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
        bad "$label: $(wc -l < "$fails" | tr -d ' ') gate(s) FAILED:"
        while IFS= read -r n; do note "failed: $n"; done < "$fails"
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
# green suite writes, and exits the status this case asked for.
mkdir -p "$LOGDIR"
: > "$LOGDIR/_skips.txt"
: > "$LOGDIR/_failures.txt"
printf 'gate01 1 ran\ngate02 1 ran\n' > "$LOGDIR/_timings.txt"
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
    # it is a self-test nobody runs. The fake repo carries no gates/_common.sh, so
    # the freshness probe cannot answer there and every e2e case takes the rebuild
    # arm, which is what makes this stub the ONLY selfhost-emit.sh reachable here.
    cat > "$FAKE/gates/selfhost-emit.sh" <<'SSTUB'
#!/usr/bin/env bash
echo "stub self-host build"
printf 'called\n' >> "${DN2CPP_STUB_SELFHOST_MARK:-/dev/null}"
exit "${DN2CPP_STUB_SELFHOST_RC:-0}"
SSTUB
    chmod +x "$FAKE/gates/selfhost-emit.sh"

    # e2e NAME EXPECT_RC RELEASE_RC DEBUG_RC EXPECT_CALLS [EXTRA_ARG] [CULTURE_RC] [SELFHOST_RC]
    e2e() {
        local name="$1" want_rc="$2" rel_rc="$3" dbg_rc="$4" want_calls="$5" extra="${6:-}" cult="${7:-0}" sh_rc="${8:-0}"
        local root="$ST_TMP/e2e-$name" rc=0 calls
        mkdir -p "$root"
        DN2CPP_PREMERGE_SELFTEST=0 \
        DN2CPP_PREMERGE_LOGROOT="$root" \
        DN2CPP_STUB_CULTURE_RC="$cult" \
        DN2CPP_STUB_SELFHOST_RC="$sh_rc" \
        DN2CPP_STUB_SELFHOST_MARK="$root/_selfhost_calls.txt" \
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
[ -n "$(git -C "$REPO" status --porcelain 2>/dev/null)" ] && DIRTY=" (working tree DIRTY)"

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
OVERALL=0
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
    fi
fi

for cfg in $CONFIGS; do
    logdir="$LOGROOT/$(printf '%s' "$cfg" | tr 'A-Z' 'a-z')"
    if [ "$OVERALL" -ne 0 ] && [ "$KEEP_GOING" != "1" ]; then
        bad "$cfg: NOT RUN — an earlier run failed (pass --keep-going to run both regardless)."
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
note "Per-gate logs: $LOGROOT/{release,debug}/<gate>.log"
note "This run's own output: $TRANSCRIPT"
exit 1
