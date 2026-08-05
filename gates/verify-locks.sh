#!/usr/bin/env bash
# verify-locks.sh — a MANUAL harness for the advisory-lock machinery in
# gates/_common.sh: _pidlock_acquire / _pidlock_reclaim / _pidlock_release, the
# composable EXIT hooks over them, and the three locks they carry (sim_lock,
# _toolchain_stage_lock, suite_machine_lock) plus the gate-side re-entrant
# gate_machine_lock and its name-keyed dispatch table.
#
# It also covers the WATCHDOG's ownership guard (case 9c) — run_with_watchdog
# and _wd_owns_target, not a lock at all. That is deliberate and the header says so
# rather than leaving a later reader to prune the case as off-theme: it belongs here
# because it is asserted the same way (fork real processes, SIGKILL a parent
# mid-hold), it fails the same way (open, and therefore invisibly), and it is built
# on the same kill_tree these locks' reclamation paths use.
#
#     ./gates/verify-locks.sh                        # ~90s, everything
#     DN2CPP_VERIFY_LOCKS_QUICK=1 ./gates/verify-locks.sh   # skip the 60s-notice case
#
# WHY THIS IS NOT A GATE. It forks processes, takes locks, and SIGKILLs holders
# mid-hold to prove reclamation. That is not something the suite may do to the
# machine it is running on — and a gate that did it would be racing the very
# locks the suite is using. So it lives beside gates/measure-*.sh, outside the
# build-and-run-*.sh glob the runner globs, and is named in AGENTS.md with the
# other manual aids. Every case below runs against a PRIVATE lock root under
# $TMPDIR (DN2CPP_SUITE_MACHINE_LOCK_DIR is overridden before _common.sh is
# sourced); nothing here touches /tmp/dn2cpp-suite-machine-lock.d, so it is safe
# to run while a suite is running. Every process it kills is one it forked
# itself, by recorded pid — never by name, never by pattern.
#
# WHY IT EXISTS AT ALL. A lock that fails OPEN is invisible: it degrades
# to the contention it was written to remove, and the symptom — an engine launch
# starving past its watchdog — reads as "Godot is flaky", which is a diagnosis
# that never reaches this code. Each interesting case below had been verified by
# hand once and then thrown away.

DN2CPP_VERIFY_LOCKS_TMP=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-verify-locks.XXXXXX")
export DN2CPP_SUITE_MACHINE_LOCK_DIR="$DN2CPP_VERIFY_LOCKS_TMP/machine.d"

source "$(dirname "$0")/_common.sh"

DN2CPP_REPO="$PWD"
export DN2CPP_REPO
TMP="$DN2CPP_VERIFY_LOCKS_TMP"
LOCK="$DN2CPP_SUITE_MACHINE_LOCK_DIR"
GATES="$DN2CPP_REPO/gates"

N_PASS=0
N_FAIL=0
CHILD_PIDS=""

say()  { printf '\n\033[1;36m── %s\033[0m\n' "$*"; }
good() { printf '  \033[32mPASS\033[0m %s\n' "$*"; N_PASS=$((N_PASS + 1)); }
bad()  { printf '  \033[31mFAIL\033[0m %s\n' "$*" >&2; N_FAIL=$((N_FAIL + 1)); }

# Kill only pids this harness forked, recorded one by one. No pkill, no killall,
# no pattern: other lanes' suites are running on this machine.
reap_children() {
    local p
    for p in $CHILD_PIDS; do
        kill -9 "$p" 2>/dev/null || true
    done
    CHILD_PIDS=""
}
cleanup() {
    reap_children
    chmod -R u+w "$TMP" 2>/dev/null || true
    rm -rf "$TMP"
}
trap cleanup EXIT

# wait_for FILE SECONDS — poll for a file to appear.
wait_for() {
    local f="$1" limit="$2" n=0
    while [ ! -e "$f" ]; do
        n=$((n + 1))
        [ "$n" -gt $((limit * 10)) ] && return 1
        sleep 0.1
    done
    return 0
}

lock_owner() { cat "$LOCK/pid" 2>/dev/null || echo none; }

# ── child scripts ────────────────────────────────────────────────────────────
# They source the real _common.sh, so every case exercises the shipping code
# rather than a copy of it. Sourcing from outside gates/ leaves $PWD elsewhere,
# which is harmless here: every lock path in play is an absolute override.

cat > "$TMP/holder.sh" <<'EOF'
#!/usr/bin/env bash
# Take the lock, announce it, hold until told to stop (or until killed).
source "$DN2CPP_REPO/gates/_common.sh"
_pidlock_acquire "$DN2CPP_SUITE_MACHINE_LOCK_DIR" 60 holder "${HOLD_PATTERN:-}" || exit 1
printf '%s\n' "$$" > "$READY_FILE"
while [ ! -f "$STOP_FILE" ]; do sleep 0.2; done
_pidlock_release "$DN2CPP_SUITE_MACHINE_LOCK_DIR"
EOF

cat > "$TMP/racer.sh" <<'EOF'
#!/usr/bin/env bash
# One contender in the atomicity race: take the lock, prove nobody else is
# inside the critical section, bump a counter non-atomically, release.
source "$DN2CPP_REPO/gates/_common.sh"
_pidlock_acquire "$DN2CPP_SUITE_MACHINE_LOCK_DIR" 120 racer || exit 1
printf '%s\n' "$$" > "$OCCUPANT"
n=$(cat "$COUNTER")
sleep 0.05
if [ "$(cat "$OCCUPANT")" != "$$" ]; then
    printf 'two holders at once: %s and %s\n' "$$" "$(cat "$OCCUPANT")" >> "$VIOLATIONS"
fi
printf '%s\n' "$((n + 1))" > "$COUNTER"
_pidlock_release "$DN2CPP_SUITE_MACHINE_LOCK_DIR"
EOF

cat > "$TMP/fakegate.sh" <<'EOF'
#!/usr/bin/env bash
# Stands in for a Godot gate: calls gate_machine_lock at the top, does its
# "work", and lets the EXIT hook release. Prints who owns the lock while it
# runs, so the caller can tell a re-entrant stand-down (owner unchanged) from a
# real acquire (owner == this pid).
source "$DN2CPP_REPO/gates/_common.sh"
gate_machine_lock
printf 'self=%s owner=%s\n' "$$" "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null || echo none)"
exit "${FAKEGATE_RC:-0}"
EOF

# The same, but calling NOTHING: whether it locks is decided entirely by
# whether its own file name is in _common.sh's DN2CPP_MACHINE_LOCK_GATES. Two
# copies of this are made below, one named like a Phase-5 gate and one not.
cat > "$TMP/autogate-body" <<'EOF'
#!/usr/bin/env bash
source "$DN2CPP_REPO/gates/_common.sh"
printf 'self=%s owner=%s\n' "$$" "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null || echo none)"
EOF

cat > "$TMP/hooks.sh" <<'EOF'
#!/usr/bin/env bash
# EXIT-hook composition, and the release-does-not-disarm property.
source "$DN2CPP_REPO/gates/_common.sh"
gate_add_exit_hook 'printf "one\n" >> "$HOOKLOG"'
gate_machine_lock
[ "$(cat "$DN2CPP_SUITE_MACHINE_LOCK_DIR/pid" 2>/dev/null)" = "$$" ] \
    || { printf 'gate_machine_lock did not take the lock\n' >&2; exit 1; }
# Release explicitly, mid-run. The hook stays armed; a second release must be a
# no-op rather than an error, and a hook added AFTER it must still run.
suite_machine_unlock
[ -d "$DN2CPP_SUITE_MACHINE_LOCK_DIR" ] \
    && { printf 'explicit release left the lock dir behind\n' >&2; exit 1; }
suite_machine_unlock
gate_add_exit_hook 'printf "two\n" >> "$HOOKLOG"'
exit "${HOOKS_RC:-0}"
EOF

cat > "$TMP/subshell.sh" <<'EOF'
#!/usr/bin/env bash
# A subshell must never release its parent's lock: in a subshell $$ still reads
# the parent's pid, so the owner test alone would pass there.
source "$DN2CPP_REPO/gates/_common.sh"
L="$DN2CPP_SUITE_MACHINE_LOCK_DIR"
_pidlock_acquire "$L" 30 subtest || exit 1
( _pidlock_release "$L" )
if [ ! -d "$L" ]; then printf 'a plain subshell released the parent lock\n' >&2; exit 1; fi
( _pidlock_release "$L" ) &
wait
if [ ! -d "$L" ]; then printf 'a background subshell released the parent lock\n' >&2; exit 1; fi
_pidlock_release "$L"
if [ -d "$L" ]; then printf 'the owner failed to release\n' >&2; exit 1; fi
printf 'subshell guard holds\n'
EOF

chmod +x "$TMP"/*.sh

# ═════════════════════════════════════════════════════════════════════════════
say "1. mkdir atomicity under a real race (16 contenders, one counter)"
# The arbiter is mkdir(2)'s all-or-nothing create. Each racer reads a counter,
# sleeps, and writes back — a sequence that loses updates the instant two
# processes are inside at once — and independently claims an "occupant" file it
# re-reads before leaving.
COUNTER="$TMP/counter"; OCCUPANT="$TMP/occupant"; VIOLATIONS="$TMP/violations"
printf '0\n' > "$COUNTER"; : > "$VIOLATIONS"
export COUNTER OCCUPANT VIOLATIONS
RACERS=16
i=0
while [ "$i" -lt "$RACERS" ]; do
    bash "$TMP/racer.sh" &
    CHILD_PIDS="$CHILD_PIDS $!"
    i=$((i + 1))
done
wait || true
CHILD_PIDS=""
final=$(cat "$COUNTER")
if [ "$final" = "$RACERS" ]; then
    good "all $RACERS increments survived (counter=$final) — no lost update"
else
    bad "counter=$final, expected $RACERS: increments were lost, so two racers held the lock"
fi
if [ -s "$VIOLATIONS" ]; then
    bad "occupant check tripped:"; cat "$VIOLATIONS" >&2
else
    good "no racer ever saw a second occupant inside the critical section"
fi
[ -d "$LOCK" ] && bad "the race left the lock dir behind" || good "every racer released on the way out"

# ═════════════════════════════════════════════════════════════════════════════
say "2. a dead owner is reclaimed by the live-pid check"
READY="$TMP/ready.2"; STOP="$TMP/stop.2"
rm -f "$READY" "$STOP"
READY_FILE="$READY" STOP_FILE="$STOP" bash "$TMP/holder.sh" &
HOLDER=$!
if ! wait_for "$READY" 30; then
    bad "the holder never took the lock"
else
    kill -9 "$HOLDER" 2>/dev/null || true
    wait "$HOLDER" 2>/dev/null || true
    if [ ! -d "$LOCK" ]; then
        bad "SIGKILL did not leak the lock dir — the reclaim path was never exercised"
    else
        t0=$SECONDS
        rc=0
        _pidlock_acquire "$LOCK" 30 reclaim-test || rc=$?
        dt=$((SECONDS - t0))
        if [ "$rc" -eq 0 ] && [ "$(lock_owner)" = "$$" ]; then
            good "a SIGKILLed owner's lock was reclaimed in ${dt}s"
        else
            bad "the lock of a dead owner was not reclaimed (rc=$rc, owner=$(lock_owner))"
        fi
        _pidlock_release "$LOCK"
    fi
fi
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "3. pid reuse is caught by the ps pattern (and a MATCHING pattern is not)"
# Hand-build the state the OS produces when a crashed owner's pid is recycled:
# the recorded pid is alive, but the process wearing it is somebody else.
sleep 300 &
IMPOSTOR=$!
CHILD_PIDS="$CHILD_PIDS $IMPOSTOR"
mkdir -p "$LOCK"
printf 'dn2cpp-verify-locks-no-such-owner\n' > "$LOCK/owner_pattern"
printf '%s\n' "$IMPOSTOR" > "$LOCK/pid"
rc=0
_pidlock_acquire "$LOCK" 20 reuse-test || rc=$?
if [ "$rc" -eq 0 ] && [ "$(lock_owner)" = "$$" ]; then
    good "an alive-but-unrelated owner pid was reclaimed"
else
    bad "the recycled-pid guard did not fire (rc=$rc, owner=$(lock_owner))"
fi
_pidlock_release "$LOCK"

# The other direction: a pattern that DOES match must never reclaim. A false
# reclaim is the one failure that breaks mutual exclusion outright, so it is
# worth an explicit case.
mkdir -p "$LOCK"
printf 'sleep\n' > "$LOCK/owner_pattern"
printf '%s\n' "$IMPOSTOR" > "$LOCK/pid"
rc=0
out=$(_pidlock_acquire "$LOCK" 4 nosteal-test 2>&1) || rc=$?
if [ "$rc" -ne 0 ] && [ "$(lock_owner)" = "$IMPOSTOR" ]; then
    good "a live owner whose ps line still matches was NOT stolen from"
else
    bad "a live matching owner was stolen from (rc=$rc, owner=$(lock_owner))"
fi
kill -9 "$IMPOSTOR" 2>/dev/null || true
wait "$IMPOSTOR" 2>/dev/null || true
CHILD_PIDS=""
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "4. the loud timeout, and the first waiter notice"
READY="$TMP/ready.4"; STOP="$TMP/stop.4"
rm -f "$READY" "$STOP"
READY_FILE="$READY" STOP_FILE="$STOP" HOLD_PATTERN=holder.sh bash "$TMP/holder.sh" &
HOLDER=$!
CHILD_PIDS="$CHILD_PIDS $HOLDER"
if ! wait_for "$READY" 30; then
    bad "the holder never took the lock"
else
    rc=0
    out=$(_pidlock_acquire "$LOCK" 4 timeout-test 2>&1) || rc=$?
    if [ "$rc" -ne 0 ]; then
        good "the wait failed rather than hanging (rc=$rc)"
    else
        bad "a held lock was acquired anyway"
        _pidlock_release "$LOCK"
    fi
    if grep -q 'gave up after' <<<"$out"; then
        _gaveup="$(grep 'gave up' <<<"$out")"
        good "the timeout is loud: ${_gaveup%%$'\n'*}"
    else
        bad "the timeout printed no 'gave up after' line: $out"
    fi
    if grep -q "waiting on .*held by pid $(cat "$READY")" <<<"$out"; then
        good "the first blocked poll named the holder's pid"
    else
        bad "the waiter did not name the holder: $out"
    fi

    # ── The owner-pattern rule: the pattern that decides a reclaim is
    # the OWNER's, not the waiter's. This lock is held by a gate-shaped owner;
    # a waiter passing run-all-gates must still not steal it.
    rc=0
    out=$(_pidlock_acquire "$LOCK" 4 crossshape-test run-all-gates 2>&1) || rc=$?
    if [ "$rc" -ne 0 ] && [ "$(lock_owner)" = "$(cat "$READY")" ]; then
        good "a run-all-gates waiter did not steal a gate-owned lock"
    else
        bad "the waiter's own pattern was used and the lock was stolen (rc=$rc)"
        _pidlock_release "$LOCK"
    fi

    if [ "${DN2CPP_VERIFY_LOCKS_QUICK:-0}" = "1" ]; then
        printf '  \033[33mSKIP\033[0m the 60s repeat notice (DN2CPP_VERIFY_LOCKS_QUICK=1)\n'
    else
        say "4b. the waiter notice repeats every 60s (this case takes ~62s)"
        rc=0
        out=$(_pidlock_acquire "$LOCK" 62 repeat-notice-test 2>&1) || rc=$?
        n=$(printf '%s\n' "$out" | grep -c 'waiting on' || true)
        if [ "$rc" -ne 0 ] && [ "$n" -ge 2 ]; then
            good "the waiter printed $n progress notices over 62s (0s and 60s)"
        else
            bad "expected >=2 progress notices, got $n (rc=$rc)"
        fi
    fi
    : > "$STOP"
    wait "$HOLDER" 2>/dev/null || true
fi
CHILD_PIDS=""
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "5. a subshell must not release its parent's lock"
rc=0
out=$(bash "$TMP/subshell.sh" 2>&1) || rc=$?
if [ "$rc" -eq 0 ]; then
    good "plain and background subshells both declined; the owner released"
else
    bad "the subshell guard failed: $out"
fi
[ -n "${BASHPID:-}" ] \
    && printf '  note: BASHPID is present (bash %s) — the guard reads it directly\n' "${BASH_VERSION%%(*}" \
    || printf '  note: no BASHPID (bash %s) — the guard used the exec-sh PPID fallback,\n        which is the path the macOS system bash takes\n' "${BASH_VERSION%%(*}"
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "6. releasing does not disarm the EXIT hooks"
HOOKLOG="$TMP/hooklog"; : > "$HOOKLOG"
export HOOKLOG
rc=0
out=$(bash "$TMP/hooks.sh" 2>&1) || rc=$?
if [ "$rc" -ne 0 ]; then
    bad "the hook child failed: $out"
else
    got=$(tr '\n' ' ' < "$HOOKLOG" | sed 's/ *$//')
    if [ "$got" = "two one" ]; then
        good "both hooks ran, most-recent-first, across an explicit release"
    else
        bad "hook log was '$got', expected 'two one'"
    fi
    good "a second release of an already-released lock was a no-op"
fi
# Exit status must survive the dispatcher.
: > "$HOOKLOG"
rc=0
HOOKS_RC=7 bash "$TMP/hooks.sh" >/dev/null 2>&1 || rc=$?
[ "$rc" -eq 7 ] && good "the exit status survives the hook dispatcher (7)" \
                || bad "exit status was $rc, expected 7"
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "7. a lock dir that cannot be reclaimed fails loudly"
# The shipped default lives on the world-shared, sticky /tmp, where ANOTHER
# user's abandoned dir can never be removed by us. Standing in for that here
# with a read-only parent, which produces the same refusal from rm(1). The
# failure mode this replaced was a silent spin to the 6-hour timeout.
if [ "$(id -u)" = "0" ]; then
    printf '  \033[33mSKIP\033[0m running as root: no rm(1) refusal to observe\n'
else
    sh -c 'exit 0' &
    DEADPID=$!
    wait "$DEADPID" 2>/dev/null || true
    RO="$TMP/readonly-parent"
    mkdir -p "$RO/stale.d"
    printf '%s\n' "$DEADPID" > "$RO/stale.d/pid"
    chmod a-w "$RO"
    rc=0
    out=$(_pidlock_acquire "$RO/stale.d" 30 unreclaimable-test 2>&1) || rc=$?
    chmod u+w "$RO"
    if [ "$rc" -ne 0 ] && grep -q 'cannot reclaim' <<<"$out"; then
        good "it named the problem instead of spinning: ${out%%$'\n'*}"
        grep -q 'DN2CPP_SUITE_MACHINE_LOCK_DIR' <<<"$out" \
            && good "the message names a way out" \
            || bad "the message names no remedy"
    else
        bad "an unreclaimable stale dir did not fail loudly (rc=$rc): $out"
    fi

    # …and it must name the RIGHT way out. There is more than one machine-wide
    # lock on that sticky /tmp (the simulator mutex sits beside the suite and
    # xcodebuild ones), and a remedy line hardcoding one lock's
    # variable is worse than none for the others: it reads as actionable and
    # relocates nothing. The caller passes its own variable name; prove it
    # arrives, and that omitting it still yields the old text (asserted above).
    mkdir -p "$RO/stale2.d"
    printf '%s\n' "$DEADPID" > "$RO/stale2.d/pid"
    chmod a-w "$RO"
    rc=0
    out=$(_pidlock_acquire "$RO/stale2.d" 30 named-override-test "" DN2CPP_SIM_LOCK_DIR 2>&1) || rc=$?
    chmod u+w "$RO"
    if [ "$rc" -ne 0 ] && grep -q 'DN2CPP_SIM_LOCK_DIR=' <<<"$out"; then
        good "the remedy names the caller's own override variable"
    else
        bad "the reclaim message did not name the caller's override (rc=$rc): $out"
    fi
fi

# ═════════════════════════════════════════════════════════════════════════════
say "8. gate_machine_lock stands down under a runner that holds the lock"
READY="$TMP/ready.8"; STOP="$TMP/stop.8"
rm -f "$READY" "$STOP"
READY_FILE="$READY" STOP_FILE="$STOP" HOLD_PATTERN=holder.sh bash "$TMP/holder.sh" &
HOLDER=$!
CHILD_PIDS="$CHILD_PIDS $HOLDER"
if ! wait_for "$READY" 30; then
    bad "the holder never took the lock"
else
    HP=$(cat "$READY")

    # (a) The handshake: HELD names the pid the lock dir records. Stand down.
    t0=$SECONDS
    rc=0
    out=$(DN2CPP_MACHINE_LOCK_HELD="$HP" bash "$TMP/fakegate.sh" 2>&1) || rc=$?
    dt=$((SECONDS - t0))
    if [ "$rc" -eq 0 ] && [ "$dt" -le 3 ] && [ "$(lock_owner)" = "$HP" ]; then
        good "a gate under its own runner passed straight through in ${dt}s ($out)"
    else
        bad "the re-entrant path did not fire (rc=$rc, ${dt}s, owner=$(lock_owner), out=$out)"
    fi

    # (b) A leaked/stale claim naming a pid that is NOT the owner corroborates
    # nothing, so the gate must block. This is the failure the env handshake
    # would otherwise have: a variable anybody can set silently disabling the
    # lock. Time-boxed so the case is a few seconds, not six hours.
    rc=0
    out=$(DN2CPP_MACHINE_LOCK_HELD=999999 DN2CPP_MACHINE_LOCK_TIMEOUT_SECS=4 \
            bash "$TMP/fakegate.sh" 2>&1) || rc=$?
    if [ "$rc" -ne 0 ] && [ "$(lock_owner)" = "$HP" ]; then
        good "an uncorroborated DN2CPP_MACHINE_LOCK_HELD did not let the gate past"
    else
        bad "a bogus handshake value bypassed the lock (rc=$rc, owner=$(lock_owner))"
    fi
    : > "$STOP"
    wait "$HOLDER" 2>/dev/null || true
fi
CHILD_PIDS=""
rm -rf "$LOCK"

say "9. a standalone gate takes the lock for real, and releases it"
rc=0
out=$(bash "$TMP/fakegate.sh" 2>&1) || rc=$?
self=$(printf '%s\n' "$out" | sed -n 's/.*self=\([0-9]*\).*/\1/p')
owner=$(printf '%s\n' "$out" | sed -n 's/.*owner=\([0-9a-z]*\).*/\1/p')
if [ "$rc" -eq 0 ] && [ -n "$self" ] && [ "$self" = "$owner" ]; then
    good "with no runner around, the gate owned the lock while it ran"
else
    bad "the standalone gate did not take the lock (rc=$rc, out=$out)"
fi
[ -d "$LOCK" ] && bad "the gate leaked the lock dir on exit" \
               || good "the EXIT hook released it on the way out"

# A gate that FAILS must release too — the common case in practice.
rc=0
FAKEGATE_RC=9 bash "$TMP/fakegate.sh" >/dev/null 2>&1 || rc=$?
if [ "$rc" -eq 9 ] && [ ! -d "$LOCK" ]; then
    good "a failing gate released the lock and kept its exit status"
else
    bad "a failing gate leaked the lock or lost its status (rc=$rc, dir=$([ -d "$LOCK" ] && echo present || echo gone))"
fi

rc=0
out=$(DN2CPP_NO_MACHINE_LOCK=1 bash "$TMP/fakegate.sh" 2>&1) || rc=$?
if [ "$rc" -eq 0 ] && [ ! -d "$LOCK" ]; then
    good "DN2CPP_NO_MACHINE_LOCK=1 is still a complete opt-out"
else
    bad "the opt-out did not opt out (rc=$rc, out=$out)"
fi
rm -rf "$LOCK"

say "9b. the dispatch is by gate NAME, from _common.sh alone"
# The seventeen gates carry no call: sourcing _common.sh takes the lock for
# them, keyed on $0. Prove both directions with a script whose only difference
# from its twin is its file name.
cp "$TMP/autogate-body" "$TMP/build-and-run-godot-sample.sh"
cp "$TMP/autogate-body" "$TMP/build-and-run-no-such-gate.sh"
chmod +x "$TMP/build-and-run-godot-sample.sh" "$TMP/build-and-run-no-such-gate.sh"
rc=0
out=$(bash "$TMP/build-and-run-godot-sample.sh" 2>&1) || rc=$?
self=$(printf '%s\n' "$out" | sed -n 's/.*self=\([0-9]*\).*/\1/p')
owner=$(printf '%s\n' "$out" | sed -n 's/.*owner=\([0-9a-z]*\).*/\1/p')
if [ "$rc" -eq 0 ] && [ -n "$self" ] && [ "$self" = "$owner" ]; then
    good "a script named build-and-run-godot-sample.sh locked with no call of its own"
else
    bad "the name-keyed dispatch did not fire (rc=$rc, out=$out)"
fi
[ -d "$LOCK" ] && { bad "it leaked the lock"; rm -rf "$LOCK"; } \
               || good "and released it on exit"
rc=0
out=$(bash "$TMP/build-and-run-no-such-gate.sh" 2>&1) || rc=$?
owner=$(printf '%s\n' "$out" | sed -n 's/.*owner=\([0-9a-z]*\).*/\1/p')
if [ "$rc" -eq 0 ] && [ "$owner" = "none" ]; then
    good "a gate-shaped name that is NOT in the table took no lock"
else
    bad "a non-member locked anyway (rc=$rc, out=$out)"
fi
rm -rf "$LOCK"

# ═════════════════════════════════════════════════════════════════════════════
say "9c. an ORPHANED watchdog must not signal a pid it no longer owns"
# Same machinery, same reason it cannot be a gate: the case is made by SIGKILLing
# a parent mid-`wait` and watching what its abandoned dog does when the budget
# expires. Before the guard, that dog woke up and ran kill_tree against a pid
# that had been released — and pids wrap at 99999, with the ALLOCATOR sampled
# through a `DN2CPP_GATE_CACHE=0` suite on the development Mac (2026-07-30) at
# 150–1,500 pids/s in its parallel phase, i.e. a full wrap every one to eleven
# minutes against budgets of 600s and 10800s. So under a suite it is holding a
# stranger's number, not a stale one. (The rate this replaced was a fork loop's,
# which measures how fast a tight loop CAN consume pids rather than how fast a
# suite does; _common.sh's watchdog comment and AGENTS.md carry the same
# numbers.) The victim's own log said nothing, because the dog's
# announcement goes to the stderr of the parent that is already dead.
#
# (a) The stand-down. The guarded command is left alive and orphaned, which is
# also what makes the assert cheap: no pid has to be recycled to show that the
# dog no longer signals what it does not own.
wd_parent="$TMP/wd-parent.sh"
cat > "$wd_parent" <<EOF
source "$GATES/_common.sh"
run_with_watchdog 8 sleep 300
EOF
bash "$wd_parent" >"$TMP/wd-a.out" 2>"$TMP/wd-a.err" &
wd_pa=$!
CHILD_PIDS="$CHILD_PIDS $wd_pa"
sleep 1
wd_cmd=$(pgrep -P "$wd_pa" sleep 2>/dev/null || true)
wd_cmd=${wd_cmd%%$'\n'*}
CHILD_PIDS="$CHILD_PIDS $wd_cmd"
kill -9 "$wd_pa" 2>/dev/null || true
wait "$wd_pa" 2>/dev/null || true
sleep 10
if [ -n "$wd_cmd" ] && kill -0 "$wd_cmd" 2>/dev/null; then
    good "an orphaned dog left pid $wd_cmd alone after its budget expired"
else
    bad "an orphaned dog signalled pid ${wd_cmd:-?} — the pid was no longer its own"
fi
# A stand-down that still printed "killing process tree" would put a kill nobody
# performed into the log, which is worse than silence: the next reader attributes
# a real failure to it.
if grep -q WATCHDOG "$TMP/wd-a.err"; then
    bad "the stand-down announced a kill it did not perform"
else
    good "a stand-down says nothing — it does not claim a kill"
fi
kill -9 "$wd_cmd" 2>/dev/null || true

# (b) The guard must not have disarmed the watchdog. This is the direction that
# fails silently in the other sense: a dog that always stands down turns every
# wedged run back into an unbounded one, which is exactly what run_bounded and
# run_gate's ceiling exist to prevent.
# `set +e` inside the child, `|| true` outside it: _common.sh is sourced under
# `set -euo pipefail`, so a 137 the watchdog produced on purpose would abort
# both shells before either could report it — the child would print no rc and
# this harness would exit 137 mid-run, which is the very symptom under test
# wearing a disguise.
cat > "$wd_parent" <<EOF
source "$GATES/_common.sh"
set +e
run_with_watchdog 4 sleep 300
echo "rc=\$?"
EOF
bash "$wd_parent" >"$TMP/wd-b.out" 2>"$TMP/wd-b.err" || true
if grep -q '^rc=137$' "$TMP/wd-b.out" && grep -q 'no exit after 4s' "$TMP/wd-b.err"; then
    good "a live parent's dog still kills a hung command (rc 137) and names it"
else
    bad "the watchdog no longer fires: out='$(cat "$TMP/wd-b.out")' err='$(cat "$TMP/wd-b.err")'"
fi

# (c) …including from inside a command substitution, which is how every caller
# of run_bounded invokes it. That shape is why the owner cannot be `$$`: the
# substitution's subshell is what forks the command, while `$$` still reads the
# top-level shell, so an owner test written against `$$` would stand down on
# every real run step in the suite.
cat > "$wd_parent" <<EOF
source "$GATES/_common.sh"
set +e
out=\$(run_with_watchdog 4 /bin/echo captured); rc=\$?
echo "out=\$out rc=\$rc"
out2=\$(run_with_watchdog 4 sleep 300); echo "rc2=\$?"
EOF
bash "$wd_parent" >"$TMP/wd-c.out" 2>"$TMP/wd-c.err" || true
if grep -q '^out=captured rc=0$' "$TMP/wd-c.out" && grep -q '^rc2=137$' "$TMP/wd-c.out"; then
    good "the owner test survives \$( ) capture: output intact, dog still fires"
else
    bad "run_bounded's capture shape broke: out='$(cat "$TMP/wd-c.out")'"
fi

# (d) The workers get the guard. run_gate reaches an xargs -P worker as an
# exported function and _common.sh is never sourced there, so a dog that cannot
# call _wd_owns_target takes the `|| exit 0` arm and stands down — disarming
# every per-gate ceiling in the suite, silently.
if grep -q 'export -f .*_wd_owns_target' "$GATES/run-all-gates.sh" \
   || awk '/^export -f /{ e = 1 } e && /_wd_owns_target/ { found = 1 } e && !/\\$/ { e = 0 } END { exit(found ? 0 : 1) }' "$GATES/run-all-gates.sh"; then
    good "run-all-gates.sh exports _wd_owns_target to the xargs workers"
else
    bad "run-all-gates.sh does not export _wd_owns_target — every worker's dog would stand down"
fi

# (e) the orphan must LEAVE, not hold its `sleep` to expiry. The guard
# above makes an orphan harmless; it does not make it cheap, and a gate-scope
# dog parks a three-hour sleep per orphan. The budget here is deliberately far
# larger than both the poll interval and this check's own wait, so a dog that
# still slept its budget in one piece would plainly be alive when asked —
# whereas (a)'s 8s budget is deliberately BELOW the poll, so it keeps exercising
# the single-sleep path and the ownership test at the kill site. The dog is
# picked out of the parent's children as the one that is not the guarded
# `sleep`: its own bite-sized sleep is a grandchild and never in that set.
cat > "$wd_parent" <<EOF
source "$GATES/_common.sh"
run_with_watchdog 60 sleep 300
EOF
bash "$wd_parent" >"$TMP/wd-e.out" 2>"$TMP/wd-e.err" &
wd_pe=$!
CHILD_PIDS="$CHILD_PIDS $wd_pe"
sleep 1
wd_ecmd=$(pgrep -P "$wd_pe" sleep 2>/dev/null || true)
wd_ecmd=${wd_ecmd%%$'\n'*}
wd_dog=""
for p in $(pgrep -P "$wd_pe" 2>/dev/null || true); do
    [ "$p" = "$wd_ecmd" ] || wd_dog="$p"
done
CHILD_PIDS="$CHILD_PIDS $wd_ecmd $wd_dog"
kill -9 "$wd_pe" 2>/dev/null || true
wait "$wd_pe" 2>/dev/null || true
sleep 25
if [ -n "$wd_dog" ] && ! kill -0 "$wd_dog" 2>/dev/null; then
    good "an orphaned dog left within a poll, not at its 60s budget"
else
    bad "an orphaned dog (budget 60s) was still holding its sleep as ${wd_dog:-?} 25s after being orphaned"
fi
if grep -q WATCHDOG "$TMP/wd-e.err"; then
    bad "the early stand-down announced a kill it did not perform"
else
    good "the early stand-down says nothing either"
fi
kill -9 "$wd_ecmd" 2>/dev/null || true

# ═════════════════════════════════════════════════════════════════════════════
say "10. structural invariants (grep, not fork)"
# (a) All three unlockers must go through _pidlock_release. A hand-rolled copy
# is how the acquire side drifted before _pidlock_acquire existed.
miss=""
for fn in sim_unlock suite_machine_unlock _toolchain_stage_unlock; do
    awk -v fn="$fn" '
        $0 ~ "^"fn"\\(\\) \\{" { inside = 1 }
        inside && /_pidlock_release/ { found = 1 }
        inside && /^\}/ { exit }
        END { exit(found ? 0 : 1) }
    ' "$GATES/_common.sh" || miss="$miss $fn"
done
[ -z "$miss" ] && good "sim_unlock / suite_machine_unlock / _toolchain_stage_unlock all delegate to _pidlock_release" \
               || bad "these unlockers do not delegate to _pidlock_release:$miss"

# (b) A gate that takes the machine lock may not write a bare `trap … EXIT`:
# the single trap slot would evict the hook dispatcher, and with it the
# release. This is the invariant stated at gate_add_exit_hook, and it is the
# one thing in this file a grep can prove for the whole corpus. It also fails
# for a gate that never locks but whose file NAME is in the table, which is
# the point: the table is what decides.
offenders=""
for name in $DN2CPP_MACHINE_LOCK_GATES; do
    [ -f "$GATES/$name" ] || { offenders="$offenders $name(missing)"; continue; }
    grep -qE '^[[:space:]]*trap[[:space:]].*[[:space:]]EXIT([[:space:]]|$)' "$GATES/$name" \
        && offenders="$offenders $name"
done
[ -z "$offenders" ] && good "no machine-locking gate writes a bare 'trap … EXIT'" \
                    || bad "these gates would evict the EXIT-hook dispatcher:$offenders"

# (c) Every gate the runner puts in a Phase-5 chain must be in the table, and
# nothing else may be. Parity is the whole claim, and run-all-gates.sh
# asserts it on every suite; this is the same diff, available without a suite.
chain_gates=$(awk '
    /^CHAIN_[A-F]=\(/ { inside = 1; next }
    inside && /^\)/   { inside = 0; next }
    inside && /gates\/build-and-run-.*\.sh/ {
        line = $0
        sub(/^[^"]*"/, "", line); sub(/".*$/, "", line)
        sub(/^gates\//, "", line)
        print line
    }
' "$GATES/run-all-gates.sh" | sort -u)
table=$(printf '%s\n' "$DN2CPP_MACHINE_LOCK_GATES" | sort -u)
diffout=$(diff <(printf '%s\n' "$chain_gates") <(printf '%s\n' "$table") || true)
if [ -z "$diffout" ]; then
    good "DN2CPP_MACHINE_LOCK_GATES is exactly the runner's Phase-5 chains ($(printf '%s\n' "$chain_gates" | wc -l | tr -d ' ') gates)"
else
    bad "the table and the Phase-5 chains disagree:"
    printf '%s\n' "$diffout" >&2
fi

# (c2) …and the runner must make that diff itself, so a drift is caught by a
# suite rather than only by this manual harness.
grep -q 'DN2CPP_MACHINE_LOCK_GATES' "$GATES/run-all-gates.sh" \
    && good "run-all-gates.sh checks the same parity on every suite" \
    || bad "run-all-gates.sh does not check chain/table parity"

# (c3) A gate that died BY SIGNAL must be reported as such, and the two cases
# must be told apart. This is a grep rather than a fork because run_gate
# lives in run-all-gates.sh and cannot be sourced without starting a suite; what
# it pins is that both arms still exist, since a diagnostic nobody exercises is
# the kind that rots quietly and is missed exactly when it is needed.
if awk '
    /^run_gate\(\) \{/ { inside = 1 }
    inside && /rc" -ge 128/ { armed = 1 }
    inside && /WATCHDOG: no exit after/ { disc = 1 }
    inside && /KILLED FROM OUTSIDE/ { outside = 1 }
    inside && /^\}/ { exit }
    END { exit((armed && disc && outside) ? 0 : 1) }
' "$GATES/run-all-gates.sh"; then
    good "run_gate names a signal death and tells its own watchdog apart from an outside kill"
else
    bad "run_gate no longer distinguishes a signal death from an ordinary failure"
fi

# (d) The runner must export the handshake, and unset it after the release.
grep -q 'export DN2CPP_MACHINE_LOCK_HELD=\$\$' "$GATES/run-all-gates.sh" \
    && grep -q 'unset DN2CPP_MACHINE_LOCK_HELD' "$GATES/run-all-gates.sh" \
    && good "run-all-gates.sh exports the handshake for the held window and unsets it after" \
    || bad "run-all-gates.sh does not export/unset DN2CPP_MACHINE_LOCK_HELD"

# (e) the simulator mutex must be MACHINE-wide. It guards one per-user
# simulator set under ~/Library/Developer/CoreSimulator, so a lock dir under
# the worktree's own artifacts/ serialized two runners sharing a repo and gave
# two worktrees — how this tree is normally driven — no exclusion at all. That
# is the fail-OPEN direction again, and a relative path is the whole of the
# bug, so an absolute path outside any worktree is the whole of the test.
case "$DN2CPP_SIM_LOCK_DIR" in
    /*) case "$DN2CPP_SIM_LOCK_DIR" in
            "$DN2CPP_REPO"/*) bad "the sim lock dir is inside the worktree: $DN2CPP_SIM_LOCK_DIR" ;;
            *) good "the sim lock dir is machine-wide ($DN2CPP_SIM_LOCK_DIR)" ;;
        esac ;;
    *)  bad "the sim lock dir is repo-relative, so two worktrees do not serialize: $DN2CPP_SIM_LOCK_DIR" ;;
esac

# (f) …and every window that touches the simulator must take it. Two checks,
# because there are two kinds of window and a single per-gate test cannot see
# both: a gate whose own file drives the device, and the one window that lives
# in a _common.sh helper instead. The first draft asked only "does this gate
# end up locked somehow", and its own negative control passed — deleting the
# console gate's boot lock left the helper's spawn lock standing and the check
# said nothing. The defect was precisely an unlocked window, so the test has to
# be per window.
#
# (f1) The subject set is derived, not listed: any gate naming
# ensure_booted_sim, `simctl spawn` or `simctl launch` drives the device from
# its own file and must carry a bare sim_lock call of its own.
sim_users=$(grep -lE 'ensure_booted_sim|simctl (spawn|launch)' "$GATES"/build-and-run-*.sh | sort)
unlocked=""
for g in $sim_users; do
    # A bare call on its own line, not a mention in a comment.
    grep -qE '^[[:space:]]*sim_lock[[:space:]]*$' "$g" && continue
    unlocked="$unlocked $(basename "$g")"
done
[ -z "$unlocked" ] \
    && good "every simulator-touching gate calls sim_lock itself ($(printf '%s\n' "$sim_users" | wc -l | tr -d ' ') gates)" \
    || bad "these gates touch the simulator without sim_lock:$unlocked"

# (f2) …and the simulator window that is NOT in a gate — ios_sim_corelib_diff_gate
# runs the console gate's three spawns — locks around it in the helper.
awk '/^ios_sim_corelib_diff_gate\(\) \{/ { i = 1 }
     i && /^[[:space:]]*sim_lock[[:space:]]*$/ { f = 1 }
     i && /^\}/ { exit }
     END { exit(f ? 0 : 1) }' "$GATES/_common.sh" \
    && good "ios_sim_corelib_diff_gate takes sim_lock around its simctl spawn" \
    || bad "ios_sim_corelib_diff_gate spawns on the simulator without sim_lock"

# ═════════════════════════════════════════════════════════════════════════════
printf '\n'
if [ "$N_FAIL" -eq 0 ]; then
    printf '\033[1;32m✔ verify-locks: %d checks passed\033[0m\n' "$N_PASS"
    exit 0
fi
printf '\033[1;31m✘ verify-locks: %d passed, %d FAILED\033[0m\n' "$N_PASS" "$N_FAIL" >&2
exit 1
