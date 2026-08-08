#!/usr/bin/env bash
# verify-culture-invariance.sh — a harness proving that no gate's verdict reads
# the host's culture. Run by gates/pre-merge.sh;
# also runnable by hand:
#
#     ./gates/verify-culture-invariance.sh              # every gate bucket: one
#                                                       # build + one run per locale each
#     ./gates/verify-culture-invariance.sh ConvertParse MathSubset   # named buckets only
#     DN2CPP_CULTURE_LOCALES="en_US.UTF-8 de_DE.UTF-8" ./gates/verify-culture-invariance.sh
#     DN2CPP_CULTURE_RUN_SECS=15 ./gates/verify-culture-invariance.sh   # per-run cap
#     JOBS=1 ./gates/verify-culture-invariance.sh       # one subject at a time
#
# WHAT IT ASSERTS, AND WHY A REAL-.NET RUN DECIDES IT. Every gate compares a
# transpiled binary against something: a live `dotnet $app` oracle, or a
# checked-in fixture. BOTH sides read the host's locale: dn2cpp's CurrentCulture
# resolves the host's culture instead of folding to invariant, so the native
# binary moves with the machine exactly as the oracle does. What the harness runs is the ORACLE, for every bucket including the
# ones whose gate never runs one, because a real-.NET run of the same source is
# the cheap decider for the property that matters on both sides: does this
# PROGRAM print anything a culture can move? If real .NET's output is
# byte-identical across four locales, the program names no culture-sensitive
# value and neither side of its gate can move; if it differs, the bucket needs
# the pin. No transpile and no C++ build are needed to decide it, which is why
# this takes minutes rather than hours.
#
# The two shapes fail differently, and the second one is newer and worse:
#   - a DIFF bucket compares two moving sides. They agree wherever dn2cpp's
#     culture table matches ICU for the developer's locale, and part ways where
#     it does not — so the gate is red on some machines only. The table covers
#     the locales a host can actually be set to, which makes the common case
#     agree; it does not make the hazard go away,
#     because what is left is every value the table gets subtly wrong for some
#     locale, and nobody can enumerate that set in advance.
#   - a FREEZE or SUBSET bucket compares a moving side against a FIXED fixture.
#     There is nothing to agree with: any culture-sensitive line is simply wrong
#     off the host the snapshot was taken on. These buckets are the larger half
#     of the subject set.
#
# WHY IT IS NOT A GATE, AND WHERE IT RUNS INSTEAD. It rebuilds every subject the
# derivation below returns and runs each once per locale, as many subjects at a
# time as the host has cores: minutes of wall clock to re-assert a
# property that changes only when somebody adds a bucket or a section. Paid on
# every suite run — including the dozens a day spent on an unrelated fix — that
# is a tax on work it can never inform. The alternative considered and rejected
# was folding the probe into `corelib_diff_gate` itself: it costs four extra
# program runs per bucket rather than one, it re-runs subjects whose determinism
# is only asserted against the oracle (a second and third run of each is new
# flake surface), and it needs its own copy of the vacuity refusal below, since a
# per-gate check on Windows would compare two runs of one locale and pass. So it
# is paid where the property is actually at risk of changing: `gates/pre-merge.sh`
# runs it, beside the Release and Debug suites, and a merge that added a bucket
# cannot go green without it. By hand, run it while touching samples or culture.
# The always-on defence remains the pin itself —
# `CultureInfo.CurrentCulture` AND `CultureInfo.CurrentUICulture` set to
# InvariantCulture at each bucket driver's first statements — and this harness is
# what proves the pins are real. Two lines, not one: the UI culture selects the
# resource set, which moves a DisplayName / TimeZoneInfo.StandardName / LCID
# without touching a separator, and it is a different reader of the host.
# Same shape as gates/verify-locks.sh: outside the build-and-run-*.sh glob the
# runner globs.
#
# IT REFUSES TO PASS VACUOUSLY. The harness drives .NET's culture through
# LANG/LC_ALL, which works on macOS and Linux and does NOTHING on Windows, where
# .NET reads the OS user default locale — so on some hosts every locale below is
# the same locale and "all buckets identical" would mean nothing at all. That is
# the fail-OPEN direction, and it is the one nobody would ever notice, so it is
# checked first: a 20-line probe program reports the CultureInfo each locale
# actually produced, and if they are not all distinct the harness dies saying so
# instead of reporting a green it did not earn.
set -u

REPO="$(cd "$(dirname "$0")/.." && pwd)"
CONFIG="${CONFIG:-Release}"
LOCALES="${DN2CPP_CULTURE_LOCALES:-en_US.UTF-8 de_DE.UTF-8 fr_FR.UTF-8 ja_JP.UTF-8}"
# Lowering the cap is the fail-OPEN direction — a subject that times out is
# skipped, and coverage drops silently — so leave headroom over the slowest
# subject that legitimately sleeps.
RUN_SECS="${DN2CPP_CULTURE_RUN_SECS:-60}"
JOBS="${JOBS:-$(sysctl -n hw.logicalcpu 2>/dev/null || nproc 2>/dev/null || echo 4)}"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-culture.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT

n_pass=0; n_fail=0; n_skip=0

# Subjects are checked in parallel, and a counter incremented in a child never
# reaches the parent. Each subject writes one verdict file instead: line 1 is
# ok/fail/skip, every later line is display text tagged with the stream it
# belongs on (1 = stdout, 2 = stderr). The parent replays them in subject order,
# so the transcript does not move with the scheduler.
say()    { printf '1%s\n' "$*" >>"$VF"; }
sayerr() { printf '2%s\n' "$*" >>"$VF"; }
good() { printf 'ok\n'   >"$VF"; say    "$(printf '  \033[32mOK\033[0m        %s' "$*")"; }
bad()  { printf 'fail\n' >"$VF"; sayerr "$(printf '  \033[31mHOST-DEP\033[0m  %s' "$*")"; }
skip() { printf 'skip\n' >"$VF"; say    "$(printf '  \033[33mskip\033[0m      %s' "$*")"; }

# The subject set is DERIVED, never listed — a hand-written list is how a bucket
# added next month quietly stops being covered. Two derivations, because the
# corpus has two shapes of gate:
#
#   1. the helper family. Every function whose name ends in `_gate` takes its
#      project as an argument: corelib_diff_gate, corelib_diff_split_gate,
#      corelib_freeze_gate, corelib_subset_gate, net10_bcl_diff_gate, the
#      wasm/ios axis variants, and the two the compression gates define locally
#      (dnzlib_diff_gate, dnbrotli_diff_gate). Matching the SUFFIX rather than a
#      list of names is what makes the next helper covered on the day it is
#      written — and matching `_gate` rather than `_diff*_gate` is what brought
#      the freeze and subset buckets in, which cannot be ignored
#      (see the header: their fixture cannot move, so their subject must not).
#   2. the hand-rolled gates, which build a subject and run `dotnet "$app"`
#      themselves (shared-generics, shadow-stack, env-subset, failfast-subset,
#      file-real, args-entrypoint, pinvoke, trim-reflection, custom-async-task,
#      http-get). Their subject is the `project=` / `PROJECT=` they assign; only
#      when a gate assigns neither is the `samples/dotnet/<Name>` scan used, so
#      a gate that merely BUILDS a second project (shadow-stack builds the
#      hot-update pair) does not drag it in as a subject it never runs.
#
# One project is out of reach of both and is named here instead: `HotUpdatePatch`,
# the interpreted body build-and-run-hotupdate-subset.sh runs inside its base
# program. `dotnet HotUpdatePatch.dll` is that section's oracle only at MAINTENANCE
# time — the lines are hand-copied into a fixed expectation, so nothing runs it
# during a suite and no derivation can see it. Its transcript is host-dependent, so
# the base program pins the culture before interpreting it (which is what makes the
# GATE host-independent) and the gate's header carries the matching
# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 for anyone re-deriving the lines.
# THE ONE SUBJECT THAT MUST MOVE. `DefaultCulture` is the program
# build-and-run-default-culture.sh runs to assert that the default
# CurrentCulture IS the host's, and that dn2cpp and real .NET agree on what that
# means. Its output is host-dependent BY CONSTRUCTION, and the pin every other
# bucket carries is exactly what it must not have. It is excluded here rather
# than "fixed", and the exclusion is safe for the reason the rest of this file is
# unsafe without: that gate never compares against a written-down expectation. It
# runs both sides under the same forced locale and diffs them, so its verdict is
# already independent of the host in the only sense this harness cares about.
CULTURE_SUBJECT_BY_DESIGN="DefaultCulture"

projects_under_gates() {
    {
        # Join backslash-continued lines first: net10_bcl_diff_gate's project name
        # is routinely on a continuation line, and a line-oriented grep would
        # silently drop exactly those gates.
        cat "$REPO"/gates/build-and-run-*.sh \
        | sed -e :a -e '/\\$/N; s/\\\n//; ta' \
        | grep -E '[A-Za-z0-9_]+_gate[ \t]' \
        | tr ' \t' '\n\n' \
        | while read -r tok; do
              case "$tok" in ''|*/*) continue;; esac
              [ -d "$REPO/samples/dotnet/$tok" ] && echo "$tok"
          done

        for g in "$REPO"/gates/build-and-run-*.sh; do
            # Any `dotnet "$something"` — the oracle variable is named after what
            # it holds ($app, $APP, $sig_app, $mint_app, $tlsapp, $beyondapp), so
            # a fixed pair of names is a list in disguise and misses two gates.
            grep -qE 'dotnet "\$[A-Za-z_][A-Za-z0-9_]*"' "$g" || continue
            named="$(grep -ohE '^[[:space:]]*(project|PROJECT)=("?)[A-Za-z0-9_]+' "$g" \
                     | sed -E 's/.*=("?)//' | sort -u)"
            if [ -z "$named" ]; then
                named="$(grep -ohE 'samples/dotnet/[A-Za-z0-9_]+' "$g" \
                         | sed 's|samples/dotnet/||' | sort -u)"
            fi
            for tok in $named; do
                [ -d "$REPO/samples/dotnet/$tok" ] && echo "$tok"
            done
        done
    } | sort -u | grep -vxF "$CULTURE_SUBJECT_BY_DESIGN"
}

# .NET has no timeout(1) dependency to lean on here and macOS ships none; a
# sample that hangs must not hang the harness.
#
# The trailing `2>/dev/null` silences the SHELL's own "Abort trap: 6" job notice,
# not the program's stderr — several subjects (Finalizers, UnhandledExitSubset,
# FailFastSubset) abort on purpose, and their real stderr is already captured in
# the output file being compared. An abort that only one locale takes therefore
# still shows up, as a difference in that file. An abort that EVERY locale takes
# is a different matter and is handled at the call site: it means the sweep saw
# only the run's prefix, so the subject is reported as a skip rather than a green.
run_bounded_locale() {  # LOCALE DLL OUTFILE CWD
    ( cd "$4" && LANG="$1" LC_ALL="$1" \
        perl -e 'alarm shift; exec @ARGV' "$RUN_SECS" dotnet "$2" \
        </dev/null >"$3" 2>&1 ) 2>/dev/null
}

# --- the fail-closed precondition -------------------------------------------
# Build a throwaway program that does nothing but name its CurrentCulture, and
# require the locale list to move it. Without this the whole harness degrades to
# "N identical runs of the same program", which passes everywhere and means
# nothing anywhere.
probe_locales() {
    local pd="$WORK/_probe"
    mkdir -p "$pd"
    cat >"$pd/_probe.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
</Project>
CSPROJ
    cat >"$pd/Program.cs" <<'CS'
using System;
using System.Globalization;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(CultureInfo.CurrentCulture.Name);
    }
}
CS
    dotnet build "$pd/_probe.csproj" -c Release -o "$pd/bin" >"$pd/build.log" 2>&1 || {
        echo "verify-culture-invariance: the probe program failed to build" >&2
        LC_ALL=C sed 's/^/    /' "$pd/build.log" >&2
        exit 1
    }
    local names="" name
    for L in $LOCALES; do
        name="$(LANG="$L" LC_ALL="$L" dotnet "$pd/bin/_probe.dll" 2>/dev/null)"
        [ -n "$name" ] || name="(invariant)"
        printf '  %-14s -> %s\n' "$L" "$name"
        names="$names$name
"
    done
    local n_locales n_distinct
    n_locales=$(printf '%s' "$LOCALES" | wc -w | tr -d ' ')
    n_distinct=$(printf '%s' "$names" | sort -u | grep -c .)
    if [ "$n_distinct" -lt "$n_locales" ]; then
        echo >&2
        echo "verify-culture-invariance: REFUSING TO RUN — $n_locales locales produced" >&2
        echo "  only $n_distinct distinct cultures, so this host ignores LANG/LC_ALL (Windows" >&2
        echo "  reads the OS user default locale; .NET never consults the environment there)." >&2
        echo "  Every comparison below would compare a run against itself and pass." >&2
        # 77, the tree's skip status, not 1: the refusal says "this host cannot
        # decide the question", which is not the same claim as "a bucket is
        # host-dependent". gates/pre-merge.sh runs this harness and has to tell
        # the two apart — a Windows merge gate must report the check as not
        # performed rather than fail over a property it has no way to inspect.
        exit 77
    fi
}

if [ $# -gt 0 ]; then
    SUBJECTS="$*"
else
    SUBJECTS="$(projects_under_gates)"
fi

echo "locales: $LOCALES"
probe_locales
echo

check_subject() {
    local p="$1" VF csproj o dll first same rc L why
    VF="$WORK/_verdict/$p"
    csproj="$REPO/samples/dotnet/$p/$p.csproj"
    if [ ! -f "$csproj" ]; then skip "$p (no csproj)"; return; fi
    if ! grep -qi '<OutputType>Exe' "$csproj"; then skip "$p (not an exe)"; return; fi
    # <InvariantGlobalization> used to be counted as immunity. It is not, and
    # it is worse than not: the property is a runtimeconfig knob that
    # only REAL .NET reads, so it pins the oracle and this harness's probe while
    # leaving the transpiled binary — which reads the host locale like everything
    # else — entirely unpinned. A bucket carrying it is therefore one this harness
    # is structurally unable to judge, and it also drops ICU, so a section added
    # later with `new CultureInfo("de-DE")` asserts nothing. There is one
    # mechanism for this and it is the driver pin. Fail naming it
    # rather than reporting a green nobody earned.
    # The ELEMENT, not the word: the csprojs that used to set it now carry a
    # comment saying why they do not, and a word-match would fail them for
    # documenting the rule they follow.
    if grep -q '<InvariantGlobalization>' "$csproj"; then
        bad "$p — <InvariantGlobalization> in $p.csproj blinds this probe."
        sayerr "      It pins real .NET only; the transpiled subject still reads the host"
        sayerr "      locale. Remove it and pin the driver's first statements instead —"
        sayerr "        CultureInfo.CurrentCulture   = CultureInfo.InvariantCulture;"
        sayerr "        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;"
        return
    fi

    o="$WORK/$p"; mkdir -p "$o"
    if ! dotnet build "$csproj" -c "$CONFIG" -o "$o/bin" >"$o/build.log" 2>&1; then
        bad "$p (build failed; see $o/build.log)"; return
    fi
    dll="$o/bin/$p.dll"
    [ -f "$dll" ] || { bad "$p (no $p.dll)"; return; }

    first=""; same=1; rc=0
    for L in $LOCALES; do
        run_bounded_locale "$L" "$dll" "$o/$L.out" "$o"; rc=$?
        # One incomplete run already settles the subject as a skip below, and the
        # remaining locales cannot unsettle it. Reading only the LAST locale's
        # status was also wrong: locale 1 dying and locale 4 completing left rc=0
        # and compared two truncated prefixes.
        [ "$rc" -eq 0 ] || break
        if [ -z "$first" ]; then first="$L"
        elif ! diff -q "$o/$first.out" "$o/$L.out" >/dev/null 2>&1; then same=0; fi
    done

    # A TRUNCATED SWEEP IS NOT AN INVARIANT ONE, AND IT LOOKS EXACTLY LIKE ONE.
    # The oracle here is the sample run STANDALONE — no gate around it — so a
    # subject that needs something the gate builds dies on the first line that
    # needs it. Its four runs are then four identical prefixes, and the comparison
    # below is perfectly satisfied by them. Same fail-OPEN shape as a host that
    # ignores LANG, one subject at a time, and it was live: `PInvokeNative` was
    # reported OK while its oracle exited 134 at its first P/Invoke — the native
    # library is the gate's, not the sample's — and meanwhile
    # build-and-run-pinvoke-wasm.sh was RED on a de-DE host over `7.5` vs `7,5`,
    # a line the sweep never reached.
    #
    # The signal is the oracle's exit STATUS, and it is used without trying to be
    # clever about it: a run that did not complete did not sweep what this harness
    # claims to have swept, whether it stopped for a missing companion or on
    # purpose (Finalizers, FailFastSubset and UnhandledExitSubset abort by
    # design — their prefixes are swept and their tails are not, which is the same
    # statement). Such a subject is a named SKIP, never a green: what the summary
    # then says is "this was not decided here", which is what sends the reader to
    # the driver pin instead of to a verdict nobody earned.
    if [ "$rc" -ne 0 ]; then
        # 128+SIGALRM is the cap firing, which is a different subject from one
        # that dies at once: it never finishes standalone at any cap, so a reader
        # must not go looking for a regression that made it slow.
        if [ "$rc" -eq 142 ]; then
            why="the oracle reached the ${RUN_SECS}s cap (DN2CPP_CULTURE_RUN_SECS) — it does not run to completion standalone at any cap"
        else
            why="the oracle exited $rc — did not complete standalone"
        fi
        # Say which of the two situations it is, because the remedy differs and
        # the wrong one wastes the reader's time: a driver that already pins is
        # covered by the pin and merely unverifiable HERE, while one that does not
        # is an open hole this harness has just declined to call green.
        if grep -q 'CurrentCulture = CultureInfo.InvariantCulture' \
                "$REPO/samples/dotnet/$p/Program.cs" 2>/dev/null; then
            skip "$p ($why, so only its prefix was swept; the driver DOES pin, which is what covers it)"
        else
            skip "$p ($why, so only its prefix was swept; the driver does NOT pin — add it, nothing here can decide this subject)"
        fi
        return
    fi

    if [ "$same" = 1 ]; then
        good "$p"
    else
        bad "$p — the oracle moves with the host locale:"
        for L in $LOCALES; do
            diff -q "$o/$first.out" "$o/$L.out" >/dev/null 2>&1 && continue
            say "$(printf '      vs %s (%s lines):' "$L" \
                "$(diff "$o/$first.out" "$o/$L.out" | grep -c '^[<>]')")"
            LC_ALL=C sed 's/^/1        /' \
                <<<"$(head -6 <<<"$(diff "$o/$first.out" "$o/$L.out")")" >>"$VF"
        done
        say "      remedy: pin the driver's first statements — BOTH of them, since"
        say "      the UI culture selects the resource set (a DisplayName, a"
        say "      TimeZoneInfo.StandardName, an LCID) without touching a separator:"
        say "        CultureInfo.CurrentCulture   = CultureInfo.InvariantCulture;"
        say "        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;"
    fi
}

mkdir -p "$WORK/_verdict"
export -f check_subject run_bounded_locale good bad skip say sayerr
export REPO WORK CONFIG LOCALES RUN_SECS
printf '%s\n' $SUBJECTS | xargs -P "$JOBS" -I{} bash -c 'check_subject "$@"' _ {}

# Replay in $SUBJECTS order, never completion order: a transcript that moves with
# the scheduler cannot be diffed against a previous run.
for p in $SUBJECTS; do
    vf="$WORK/_verdict/$p"
    if [ ! -s "$vf" ]; then
        printf '  \033[31mHOST-DEP\033[0m  %s\n' "$p (produced no verdict)" >&2
        n_fail=$((n_fail + 1)); continue
    fi
    { read -r verdict
      while IFS= read -r line; do
          case "$line" in
              2*) printf '%s\n' "${line#?}" >&2 ;;
              *)  printf '%s\n' "${line#?}" ;;
          esac
      done
    } <"$vf"
    case "$verdict" in
        ok)   n_pass=$((n_pass + 1)) ;;
        fail) n_fail=$((n_fail + 1)) ;;
        *)    n_skip=$((n_skip + 1)) ;;
    esac
done

echo
echo "culture invariance: $n_pass ok, $n_fail host-dependent, $n_skip skipped"
[ "$n_fail" -eq 0 ]
