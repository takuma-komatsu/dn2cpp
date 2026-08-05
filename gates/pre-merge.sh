#!/usr/bin/env bash
# pre-merge.sh — THE merge gate. The single canonical thing to run before a
# merge to `main`, so that "which two commands, under which config, with which
# environment" stops living in somebody's memory.
#
#     ./gates/pre-merge.sh                # the real thing (~1-2h; both configs)
#     ./gates/pre-merge.sh --dry-run      # print the exact runs, execute nothing
#     ./gates/pre-merge.sh --keep-going   # run Debug even after Release fails
#     DN2CPP_PREMERGE_SELFTEST=1 ./gates/pre-merge.sh   # self-test, no suite run
#
# WHAT IT RUNS, AND WHY EXACTLY THIS. One harness and two full suites, in this
# order:
#
#   0. gates/verify-culture-invariance.sh
#   1. CONFIG=Release  DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
#   2. CONFIG=Debug    DN2CPP_REQUIRE_ALL=1  DN2CPP_GATE_CACHE=0
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
#
# WHY THIS IS NOT A GATE. It runs the suite — twice. It lives beside
# gates/verify-locks.sh and gates/measure-*.sh, outside the build-and-run-*.sh
# glob the runner globs, for the obvious reason that a gate which runs the whole
# gate suite does not terminate.
#
# WHAT IT DELIBERATELY DOES NOT RUN, so nobody grows it into a junk drawer:
# gates/verify-locks.sh, gates/selfhost-*.sh, gates/measure-*.sh,
# dist/smoke-test.sh, dist/nuget-smoke-test.sh. Each is a manual AID for a
# surface some gate already covers, or a measurement with no pass/fail verdict;
# AGENTS.md names them at the change that obliges them. A merge gate whose
# runtime doubles for things that answer no pass/fail question is a merge gate
# people stop running. The test that admitted verify-culture-invariance.sh and
# keeps the rest out is not "is it useful" — it is whether the surface is one NO
# suite run covers: verify-locks.sh exercises the lock machinery the suite
# itself runs under every time, and the selfhost harnesses assert a fixpoint the
# suite's own gates transpile through, whereas nothing in either suite can
# observe that a bucket's green was a fact about ja-JP.
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
# written `ON`.
_premerge_cmake_bool() {
    case "$(printf '%s' "$1" | tr 'a-z' 'A-Z')" in
        1|ON|TRUE|YES|Y)                   printf 'ON' ;;
        0|OFF|FALSE|NO|N|IGNORE|''|*NOTFOUND) printf 'OFF' ;;
        *)                                 printf '%s' "$1" ;;
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
    local c dir rel home home_phys stamp name want cached ov
    for c in "$art"/.cmake*/CMakeCache.txt "$art"/*/.cmake*/CMakeCache.txt; do
        [ -f "$c" ] || continue
        n_dirs=$((n_dirs + 1))
        dir=$(dirname "$c")
        rel=${dir#"$repo"/}
        home=$(LC_ALL=C sed -n 's/^CMAKE_HOME_DIRECTORY:INTERNAL=//p' "$c")
        home=${home%%$'\n'*}
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
        while read -r name want; do
            [ -n "$name" ] || continue
            cached=$(LC_ALL=C sed -n "s/^$name:BOOL=//p" "$c")
            cached=${cached%%$'\n'*}
            [ -n "$cached" ] || continue
            # An explicit -D in the recorded configure command IS the intent;
            # only where the configure said nothing does the CMakeLists default
            # get to be the expected value.
            ov=$(LC_ALL=C sed -n "s/^-D$name=//p" "$stamp" | tail -1)
            [ -n "$ov" ] && want="$ov"
            if [ "$(_premerge_cmake_bool "$cached")" != "$(_premerge_cmake_bool "$want")" ]; then
                frozen="$frozen$name $cached $want $rel
"
                stale_dirs="$stale_dirs$dir
"
            fi
        done <<OPTIONS
$defaults
OPTIONS
    done

    if [ "$n_dirs" -eq 0 ]; then
        note "no configured CMake build dir under artifacts/ — nothing can be stale."
        return 0
    fi

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

    # e2e NAME EXPECT_RC RELEASE_RC DEBUG_RC EXPECT_CALLS [EXTRA_ARG] [CULTURE_RC]
    e2e() {
        local name="$1" want_rc="$2" rel_rc="$3" dbg_rc="$4" want_calls="$5" extra="${6:-}" cult="${7:-0}"
        local root="$ST_TMP/e2e-$name" rc=0 calls
        mkdir -p "$root"
        DN2CPP_PREMERGE_SELFTEST=0 \
        DN2CPP_PREMERGE_LOGROOT="$root" \
        DN2CPP_STUB_CULTURE_RC="$cult" \
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
    exit 0
fi

rm -f "$RECEIPT"
bad "PRE-MERGE FAILED — do not merge."
note "Per-gate logs: $LOGROOT/{release,debug}/<gate>.log"
exit 1
