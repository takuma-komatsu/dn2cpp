#!/usr/bin/env bash
# CLI argument-validation gate.
#
# The CLI's arg loop used to have no unknown-arg branch: once `input` was set, an
# unrecognized flag simply fell through and was silently dropped, so the tool could
# exit 0 having done something other than what it was asked. That is exactly how a
# stale bundled toolchain handed `--trim-reflection` emitted UNTRIMMED output and
# reported success. An unrecognized argument is now a hard error, and this gate is
# what asserts it stays one.
#
# Nothing else in the suite covers CLI arg rejection — every other gate passes only
# tokens the CLI recognizes, so a regression that reinstated the silent-ignore
# fall-through would sail through all of them green. This gate is deliberately pure
# CLI arg handling: it transpiles (never compiles), so it needs no C++/Godot/
# Emscripten/NDK toolchain and always RUNS (never gate_skip).
#
# Four assertions, one flag apart:
#   1. Baseline — the same input with correct flags transpiles and exits 0. The
#      regression guard that keeps the unknown-arg check from over-rejecting valid
#      flags (a check that rejected everything would pass 2-4 while being useless).
#   2. A bogus flag (`--bogus-flag`) -> nonzero exit, stderr naming the token.
#   3. A near-miss typo (`--tirm-reflection`, the very class of mistake the ticket
#      was filed over) -> nonzero exit, stderr naming the token.
#   4. A second positional (`app.dll extra.dll`) -> nonzero exit, stderr naming it.
#   5. `--no-default-ref DnZlib` on an otherwise valid run -> exit 0. The
#      over-rejection guard for the newest two-token flag: a value-taking flag
#      whose value the arg loop failed to consume would leave `DnZlib` looking
#      like a second positional and get rejected by assertion 4's own branch.
#   6. `--no-default-ref DnBogus` -> nonzero exit, stderr naming the token. The
#      name set is closed on purpose (Compilation.IsDefaultRefName): a typo that
#      was accepted as a no-op would leave the shim injected while the caller
#      believed it had been declined — the same silent-ignore failure shape the
#      whole gate exists to prevent, one flag later.
source "$(dirname "$0")/_common.sh"

PROJECT=HelloWorld

echo "== Building the sample assembly =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
APP="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
CORELIB=$(locate_corelib)
echo "corelib: $CORELIB"

# The whole gate asserts transpiler BEHAVIOR (a rejected run returns before the
# transpile and produces no output surface to key on), so its cache key stands
# in for the transpiler itself via _gate_cli_hash — see that helper's doc. OUT
# is emptied BEFORE the check so the key's surface term is stable: an EXISTING
# but empty directory keys as the explicit `no-generated` marker. It must exist
# — an absent OUT is how the surface term fails to be readable at all, and
# gate_cache_check answers that with a warning and no key, which would
# make this gate permanently uncacheable since it clears OUT on every run.
OUT=artifacts/cli-args-baseline
rm -rf "$OUT"; mkdir -p "$OUT"
# Transpiler-behavior env axis: with no output surface in the key, an ambient
# knob that changes what the transpiler DOES (a cap turning the baseline
# transpile into an abort, strict/assert modes, drain order) must move the
# context explicitly, or a warm hit would replay a green a live run cannot give.
tenv="tenv:${DN2CPP_MAX_GENERIC_DEPTH:-}/${DN2CPP_MAX_INSTANTIATIONS:-}/${DN2CPP_MAX_HEAP_MB:-}/${DN2CPP_SHARED_ASSERT:-}/${DN2CPP_STRICT_COMPLETION:-}/${DN2CPP_SPEC_DRAIN:-}"
if gate_cache_check "$OUT" "cli-args|cli:$(_gate_cli_hash)|$CORELIB|$tenv" "$APP"; then
    gate_cache_hit_msg
    exit 0
fi

# assert_rejected LABEL NEEDLE -- CLI-ARGS...
# Run the CLI with the given args; the run must exit NONZERO and its stderr must
# contain NEEDLE (the offending token).
assert_rejected() {
    local label="$1" needle="$2"; shift 2
    local err rc
    set +e
    err=$(invoke_cli "$@" 2>&1 >/dev/null)
    rc=$?
    set -e
    if [ "$rc" -eq 0 ]; then
        echo "FAIL: [$label] the CLI exited 0 on an argument it should have rejected" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    if ! grep -qF -- "$needle" <<<"$err"; then
        echo "FAIL: [$label] stderr did not name the offending token '$needle':" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    echo "OK [$label]: exit $rc, stderr named '$needle'"
    printf '%s\n' "$err" | LC_ALL=C sed 's/^/    /'
}

# assert_accepted LABEL -- CLI-ARGS...
# The mirror of assert_rejected: the run must exit ZERO. Guards against an
# over-rejecting arg loop, which would pass every rejection assertion above while
# being useless.
assert_accepted() {
    local label="$1"; shift
    local err rc
    set +e
    err=$(invoke_cli "$@" 2>&1 >/dev/null)
    rc=$?
    set -e
    if [ "$rc" -ne 0 ]; then
        echo "FAIL: [$label] the CLI exited $rc on arguments it should have accepted" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    echo "OK [$label]: exit 0"
}

# ── Assertion 1: baseline — valid flags must still transpile (exit 0) ──────────
echo "== 1/6 baseline: valid input + flags transpiles and exits 0 =="
set +e
invoke_cli "$APP" -r "$CORELIB" -o "$OUT" >/dev/null 2>&1
base_rc=$?
set -e
if [ "$base_rc" -ne 0 ]; then
    echo "FAIL: a valid transpile with correct flags exited $base_rc — the unknown-arg check is over-rejecting" >&2
    exit 1
fi
[ -f "$OUT/generated.cpp" ] \
    || { echo "FAIL: the valid transpile produced no $OUT/generated.cpp" >&2; exit 1; }
echo "OK: valid transpile exit 0, emitted generated.cpp"

# ── Assertion 2: a bogus flag is a hard error naming the token ─────────────────
echo "== 2/6 an unrecognized flag must fail loudly =="
assert_rejected "bogus flag" "--bogus-flag" "$APP" -r "$CORELIB" -o "$OUT" --bogus-flag

# ── Assertion 3: a near-miss typo of a real flag is rejected, not dropped ───────
# The concrete failure: `--tirm-reflection` (transposed) must NOT be silently
# ignored and leave reflection metadata IN while the caller believes it was stripped.
echo "== 3/6 a typo of a real flag must fail loudly, not be silently dropped =="
assert_rejected "typo flag" "--tirm-reflection" "$APP" -r "$CORELIB" -o "$OUT" --tirm-reflection

# ── Assertion 4: a second positional is a hard error ───────────────────────────
# `extra.dll` need not exist: the arg loop rejects the extra positional before any
# file access, so this stays a pure, deterministic arg-loop check.
echo "== 4/6 an extra positional argument must fail loudly =="
assert_rejected "extra positional" "extra.dll" "$APP" extra.dll -o "$OUT"

# ── Assertion 5: --no-default-ref with a KNOWN name is accepted ────────────────
# HelloWorld references no System.IO.Compression, so nothing would have been
# injected anyway — that is the point. The flag must be accepted on its own
# terms, whether or not the shim it declines was ever in play; a name-set check
# that ran only when the shim was live would let a typo through on every other
# program.
echo "== 5/6 --no-default-ref with a known shim name must be accepted (exit 0) =="
assert_accepted "no-default-ref known" "$APP" -r "$CORELIB" -o "$OUT" --no-default-ref DnZlib

# ── Assertion 6: --no-default-ref with an UNKNOWN name is a hard error ─────────
# A silently-accepted typo is the worst outcome available here: the caller
# believes the shim was declined, the shim is injected anyway, and the only
# symptom is a backend they did not choose.
echo "== 6/6 --no-default-ref with an unknown name must fail loudly =="
assert_rejected "no-default-ref unknown" "DnBogus" "$APP" -r "$CORELIB" -o "$OUT" --no-default-ref DnBogus

gate_cache_commit
echo "PASS: unrecognized options, extra positionals and unknown --no-default-ref names are hard errors; valid flags still transpile"
