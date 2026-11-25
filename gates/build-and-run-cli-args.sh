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
# Accepted flags share the worker-count runs; rejections remain isolated.
# Floating-point mode selection must survive re-emission without a stale opt-in.
# AOT requires a CoreLib manifest before writing output. ISA reports can omit it.
# Auto references follow the explicitly selected CoreLib, not app-local copies.
# ILDiet arguments stay literal, preserve protocol ownership and are inert when disabled.
# Assertions:
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
#   7. Both frontends accept `--jobs 1` and `--jobs 2`.
#   8. Both frontends reject a missing, zero, negative, or non-numeric jobs value.
#   9. Both frontends accept the global metadata compression opt-out.
source "$(dirname "$0")/_common.sh"

PROJECT=HelloWorld

echo "== Building the sample assembly =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
build_proj src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj
APP="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
CONSOLE_CLI="src/Dn2Cpp.Cli.Console/bin/$CONFIG/$TFM/dn2cpp-console.dll"
[ -f "$CONSOLE_CLI" ] \
    || { echo "FAIL: console CLI not built: $CONSOLE_CLI" >&2; exit 1; }
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
if gate_cache_check "$OUT" "cli-args|combined-acceptance|ildiet-arg:literal-budget-request|auto-ref:selected-corelib|corelib:aot-required,isa-optional|cli:$(_gate_cli_hash)" \
        "$APP" "$CONSOLE_CLI" "$CORELIB" "$(dirname "$CORELIB")/System.Runtime.dll"; then
    gate_cache_hit_msg
    exit 0
fi

printf 'strict\n' > "$OUT/fp-strict.expected"
printf 'precise\n' > "$OUT/fp-precise.expected"

# run_frontend FRONTEND -- CLI-ARGS...
run_frontend() {
    local frontend="$1"; shift
    case "$frontend" in
        full) invoke_cli "$@" ;;
        console) dotnet exec "$CONSOLE_CLI" "$@" ;;
        *) echo "FAIL: unknown CLI frontend '$frontend'" >&2; return 2 ;;
    esac
}

# assert_rejected LABEL NEEDLE FRONTEND -- CLI-ARGS...
# Run the CLI with the given args; the run must exit NONZERO and its stderr must
# contain NEEDLE (the offending token).
assert_rejected() {
    local label="$1" needle="$2" frontend="$3"; shift 3
    local err rc
    set +e
    err=$(run_frontend "$frontend" "$@" 2>&1 >/dev/null)
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

# assert_accepted LABEL FRONTEND -- CLI-ARGS...
# The mirror of assert_rejected: the run must exit ZERO. Guards against an
# over-rejecting arg loop, which would pass every rejection assertion above while
# being useless.
assert_accepted() {
    local label="$1" frontend="$2"; shift 2
    local err rc
    set +e
    err=$(run_frontend "$frontend" "$@" 2>&1 >/dev/null)
    rc=$?
    set -e
    if [ "$rc" -ne 0 ]; then
        echo "FAIL: [$label] the CLI exited $rc on arguments it should have accepted" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    echo "OK [$label]: exit 0"
}

assert_native_metadata() {
    local frontend="$1"
    if grep -Eq 'md_record_[[:alnum:]_]+\[\] =' "$OUT"/generated*.cpp; then
        echo "FAIL: $frontend emitted packed metadata with compression disabled" >&2
        exit 1
    fi
    grep -Eq 'md_native_[[:alnum:]_]+\[\] =' "$OUT"/generated*.cpp \
        || { echo "FAIL: $frontend emitted no native metadata" >&2; exit 1; }
}

assert_ildiet_budget() {
    gate_python - "$OUT/ildiet/ildiet-result.xml" <<'PY'
import sys
import xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
assert root.get("fallback") == "budget", "forwarded ILDiet budget did not trigger fallback"
assert root.find("counters").get("work.budget") == "1", "ILDiet did not receive the literal budget value"
print("OK: ILDiet received the additional work-budget arguments")
PY
}

# ── Assertion 1: baseline — valid flags must still transpile (exit 0) ──────────
echo "== 1/10 baseline: valid input + flags transpiles and exits 0 =="
set +e
invoke_cli "$APP" -r "$CORELIB" -o "$OUT" --jobs 1 \
    --no-default-ref DnZlib --no-metadata-compression \
    --ildiet-arg --work-budget --ildiet-arg 1 >/dev/null 2>&1
base_rc=$?
set -e
if [ "$base_rc" -ne 0 ]; then
    echo "FAIL: a valid transpile with correct flags exited $base_rc — the unknown-arg check is over-rejecting" >&2
    exit 1
fi
[ -f "$OUT/generated.cpp" ] \
    || { echo "FAIL: the valid transpile produced no $OUT/generated.cpp" >&2; exit 1; }
echo "OK: valid transpile exit 0, emitted generated.cpp"
assert_native_metadata full
assert_ildiet_budget
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-strict.expected"

# ── Assertion 2: a bogus flag is a hard error naming the token ─────────────────
echo "== 2/10 an unrecognized flag must fail loudly =="
assert_rejected "bogus flag" "--bogus-flag" full "$APP" -r "$CORELIB" -o "$OUT" --bogus-flag

# ── Assertion 3: a near-miss typo of a real flag is rejected, not dropped ───────
# The concrete failure: `--tirm-reflection` (transposed) must NOT be silently
# ignored and leave reflection metadata IN while the caller believes it was stripped.
echo "== 3/10 a typo of a real flag must fail loudly, not be silently dropped =="
assert_rejected "typo flag" "--tirm-reflection" full "$APP" -r "$CORELIB" -o "$OUT" --tirm-reflection

# ── Assertion 4: a second positional is a hard error ───────────────────────────
# `extra.dll` need not exist: the arg loop rejects the extra positional before any
# file access, so this stays a pure, deterministic arg-loop check.
echo "== 4/10 an extra positional argument must fail loudly =="
assert_rejected "extra positional" "extra.dll" full "$APP" extra.dll -o "$OUT"

# ── Assertion 5: --no-default-ref with a KNOWN name is accepted ────────────────
# HelloWorld references no System.IO.Compression, so nothing would have been
# injected anyway — that is the point. The flag must be accepted on its own
# terms, whether or not the shim it declines was ever in play; a name-set check
# that ran only when the shim was live would let a typo through on every other
# program.
echo "== 5/10 --no-default-ref with a known shim name must be accepted (exit 0) =="
echo "OK: --no-default-ref DnZlib accepted by the baseline run"

# ── Assertion 6: --no-default-ref with an UNKNOWN name is a hard error ─────────
# A silently-accepted typo is the worst outcome available here: the caller
# believes the shim was declined, the shim is injected anyway, and the only
# symptom is a backend they did not choose.
echo "== 6/10 --no-default-ref with an unknown name must fail loudly =="
assert_rejected "no-default-ref unknown" "DnBogus" full "$APP" -r "$CORELIB" -o "$OUT" --no-default-ref DnBogus

echo "== 7/10 full CLI accepts explicit sequential and parallel worker counts =="
echo "OK: --jobs 1 accepted by the baseline run"
assert_accepted "full jobs 2 and precise FP" full "$APP" -r "$CORELIB" -o "$OUT" --jobs 2 --fp-mode precise
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-precise.expected"
precise_surface=$(_gate_surface_lines "$OUT")

echo "== 8/10 console CLI accepts explicit sequential and parallel worker counts =="
assert_accepted "console jobs 1, metadata opt-out and precise FP" console "$APP" -r "$CORELIB" -o "$OUT" --jobs 1 --no-metadata-compression --fp-mode precise \
    --ildiet-arg --work-budget --ildiet-arg 1
assert_native_metadata console
assert_ildiet_budget
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-precise.expected"
assert_accepted "console jobs 2 and strict FP" console "$APP" -r "$CORELIB" -o "$OUT" --jobs 2 --fp-mode strict
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-strict.expected"
[ "$precise_surface" != "$(_gate_surface_lines "$OUT")" ] \
    || { echo 'FAIL: floating-point mode is absent from the gate-cache surface' >&2; exit 1; }

echo "== 9/10 full CLI rejects malformed worker counts =="
assert_rejected "full jobs missing" "--jobs" full "$APP" -r "$CORELIB" -o "$OUT" --jobs
assert_rejected "full jobs zero" "0" full "$APP" -r "$CORELIB" -o "$OUT" --jobs 0
assert_rejected "full jobs negative" "-2" full "$APP" -r "$CORELIB" -o "$OUT" --jobs -2
assert_rejected "full jobs non-numeric" "many" full "$APP" -r "$CORELIB" -o "$OUT" --jobs many

echo "== 10/10 console CLI rejects malformed worker counts =="
assert_rejected "console jobs missing" "--jobs" console "$APP" -r "$CORELIB" -o "$OUT" --jobs
assert_rejected "console jobs zero" "0" console "$APP" -r "$CORELIB" -o "$OUT" --jobs 0
assert_rejected "console jobs negative" "-2" console "$APP" -r "$CORELIB" -o "$OUT" --jobs -2
assert_rejected "console jobs non-numeric" "many" console "$APP" -r "$CORELIB" -o "$OUT" --jobs many

echo "OK: both baseline worker-count runs verified --no-metadata-compression"

echo "== Additional ILDiet arguments reject invalid values and protocol overrides =="
for frontend in full console; do
    assert_rejected "$frontend ILDiet argument missing" '--ildiet-arg expects an argument' "$frontend" "$APP" --ildiet-arg
    assert_rejected "$frontend ILDiet unknown argument" 'unknown argument: --bogus-ildiet-option' "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg --bogus-ildiet-option
    assert_rejected "$frontend ILDiet literal argument" 'unknown argument: --work-budget 1' "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg '--work-budget 1'
    assert_rejected "$frontend ILDiet budget missing" 'missing value for --work-budget' "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg --work-budget
    assert_rejected "$frontend ILDiet invalid budget" 'work budget must be a positive integer' "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg --work-budget --ildiet-arg 0
    for protocol_option in --request --result; do
        assert_rejected "$frontend ILDiet $protocol_option override" "cannot override reserved ILDiet option: $protocol_option" "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
            --ildiet-arg "$protocol_option" --ildiet-arg "$OUT/override.xml"
    done
    assert_rejected "$frontend ILDiet output override" "direct input option '-o'" "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg -o --ildiet-arg ildiet-output
    assert_rejected "$frontend ILDiet retention override" "direct input option '--stripping-level'" "$frontend" "$APP" -r "$CORELIB" -o "$OUT" \
        --ildiet-arg --stripping-level --ildiet-arg high
done
[ ! -e "$OUT/override.xml" ] \
    || { echo 'FAIL: additional ILDiet arguments wrote to a substituted protocol path' >&2; exit 1; }

echo "== Floating-point modes reject invalid values and reset to strict =="
for frontend in full console; do
    assert_rejected "$frontend FP mode missing" '--fp-mode' "$frontend" "$APP" --fp-mode
    assert_rejected "$frontend FP mode fast" 'fast' "$frontend" "$APP" --fp-mode fast
    assert_rejected "$frontend FP mode typo" 'presice' "$frontend" "$APP" --fp-mode presice
done
# Do not let a file named CoreLib next to the app select the BCL directory,
# or let a renamed but explicitly loaded CoreLib trigger a host fallback.
shadow="$OUT/shadow"
selected="$OUT/selected framework"
mkdir -p "$shadow" "$selected"
cp "$APP" "$shadow/$PROJECT.dll"
: > "$shadow/System.Private.CoreLib.dll"
: > "$shadow/System.Runtime.dll"
cp "$CORELIB" "$selected/Target.Core.dll"
cp "$(dirname "$CORELIB")/System.Runtime.dll" "$selected/System.Runtime.dll"
gate_capture_ok "$OUT/selected-closure" run_frontend full "$shadow/$PROJECT.dll" \
    -r "$selected/Target.Core.dll" --auto-ref -o "$OUT" --fp-mode strict \
    --no-ildiet --ildiet-output "$OUT/disabled-full" --ildiet-arg --bogus-ildiet-option
registry=$(grep -hF 'const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] =' "$OUT"/generated*.cpp)
if ! grep -qwF '"System.Runtime"' <<< "$registry" ||
    ! grep -qwF '"System.Private.CoreLib"' <<< "$registry" ||
    grep -qwF '"System.Console"' <<< "$registry" ||
    ! grep -q '^dn2cpp: 4 assemblies,' "$OUT/selected-closure.stdout"; then
    echo "FAIL: --auto-ref must use the selected CoreLib's siblings without loading the host CoreLib" >&2
    printf '%s\n' "$registry" >&2
    cat "$OUT/selected-closure.stdout" >&2
    exit 1
fi
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-strict.expected"
assert_accepted 'console precise FP before reset' console "$APP" -r "$CORELIB" -o "$OUT" --fp-mode precise
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-precise.expected"
assert_accepted 'console default FP reset without ILDiet' console "$APP" -r "$CORELIB" -o "$OUT" \
    --no-ildiet --ildiet-output "$OUT/disabled-console" --ildiet-arg --bogus-ildiet-option
gate_output compare "$OUT/generated-fp-mode.txt" "$OUT/fp-strict.expected"
[ ! -e "$OUT/disabled-full" ] && [ ! -e "$OUT/disabled-console" ] \
    || { echo 'FAIL: --no-ildiet invoked the companion with additional arguments' >&2; exit 1; }

echo "== AOT requires a CoreLib manifest before writing output =="
cold="$OUT/missing-corelib-output"
previous="$OUT/previous-output"
mkdir -p "$previous"
cp "$OUT/generated.cpp" "$previous/"
cp -R "$previous" "$previous-before"
cp "$selected/System.Runtime.dll" "$shadow/System.Private.CoreLib.dll"
gate_capture "$OUT/no-corelib" run_frontend full "$APP" --hotupdate-base -o "$cold"
gate_capture "$OUT/misnamed-corelib" run_frontend console "$APP" -r "$shadow/System.Private.CoreLib.dll" --no-ildiet -o "$previous"
for refusal in "$OUT/no-corelib" "$OUT/misnamed-corelib"; do
    gate_output status "$(cat "$refusal.status")" 2
    gate_output match-record "$refusal.stderr" '.*System\.Private\.CoreLib.*-r .*--auto-ref.*managed CLI.*' --policy crlf
done
[ ! -e "$cold" ] || { echo "FAIL: missing CoreLib created $cold" >&2; exit 1; }
diff -r "$previous-before" "$previous"

gate_capture_ok "$OUT/isa-no-corelib" run_frontend full "$APP" --dump-isa-surface "$OUT/isa-no-corelib.tsv" -o "$cold"
gate_output witness "$OUT/isa-no-corelib.tsv" $'corelib\t'
[ ! -e "$cold" ] || { echo "FAIL: ISA report created $cold" >&2; exit 1; }

gate_cache_commit
echo "PASS: both CLIs accept worker counts, metadata and floating-point options; invalid arguments remain hard errors"
echo "PASS: ILDiet arguments preserve literal values, protocol paths and disabled preprocessing"
