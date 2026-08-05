#!/usr/bin/env bash
# Embedded manifest resources and their explicit trim. The sample
# assembly is the only one in the tree that carries <EmbeddedResource> items — a
# UTF-8 text file, a 256-byte binary blob spanning every byte value, a
# namespace-scoped text file, a .resx compiled to a .resources SET, and a
# pre-compiled set carrying one entry per ResourceTypeCode the reader decodes — and
# the control arm diffs the native binary's stdout against real .NET running the same
# DLL. ONE program, transpiled six ways one flag (or one argv token) apart, plus two
# transpiles that must FAIL. Arms 7-8 cover the ResourceManager lookup over those same
# blobs; arm 9 is a second program, for the one declaration
# that cannot share an assembly with the first:
#
#   1. no flag -> diffed LIVE against real .NET. What that pins:
#      * GetManifestResourceStream(string) and (Type, string): the blob is carried
#        into the image as a .rodata byte array and served as a read-only stream.
#        dn2cpp hands back a MemoryStream over a copy where real .NET hands back an
#        UnmanagedMemoryStream over the mapped image — indistinguishable through the
#        Stream surface, which is why this can be a LIVE diff and not a freeze.
#      * GetManifestResourceNames / GetManifestResourceInfo, and the misses: a
#        resource that is not there is null, not an exception. That is exactly the
#        answer a loud PlatformNotSupportedException could not give
#        without lying, and it is why the blobs are carried for EVERY loaded module
#        rather than for the app module alone — hence the CoreLib section.
#      * The argument-validation shapes (null -> ArgumentNullException, empty ->
#        ArgumentException).
#
#   2. --no-manifest-resources System.Private.CoreLib -> diffed against a frozen
#      snapshot. The app's own resources still read exactly as in arm 1 (a drop is
#      per-assembly), and every CoreLib probe reports the catchable
#      PlatformNotSupportedException VERBATIM — naming the assembly and the remedy —
#      instead of the null/empty that would mean "no such resource": the registry
#      row's resourcesDropped bit is what makes the difference loud, the
#      DN2CPP_TF_METADATA_STRIPPED pattern one level up. Even the deliberate miss
#      (corelib-miss) throws here, because a null off a dropped assembly is a lie.
#
#   3. + --manifest-resource-root System.Private.CoreLib.Strings.resources -> a
#      second frozen snapshot in which exactly the rooted resource answers again
#      (corelib-set-big/corelib-info read it truthfully) while everything else still
#      throws: the enumeration (any list it could return would be incomplete), the
#      unrooted ILLink.Substitutions.xml, and the miss. A root keeps exactly the
#      resource it names and no more.
#
#   4. BOTH flags REPEATED — a third frozen snapshot. The ticket says both flags
#      are repeatable and arms 2-3 pass one occurrence each, so nothing above
#      asserts the word: this arm passes --no-manifest-resources twice
#      (System.Private.CoreLib, Dn2Cpp.Runtime) and --manifest-resource-root twice
#      (the Strings set and ILLink.Substitutions.xml). What it pins beyond
#      repetition itself:
#      * a second root keeps a second resource and no more — corelib-illink now
#        reads True where arm 3 froze it throwing, while the enumeration and the
#        miss still throw. That is the only line between this snapshot and arm 3's,
#        which is exactly the claim "a root keeps the resource it names".
#      * Dn2Cpp.Runtime carries NO embedded resource, and naming it is legitimate
#        (it is loaded) yet must set no bit: the bit is set only when resources were
#        actually SHED, because an assembly that has none answers every question the
#        same either way and its null/empty stays truthful. stdout cannot see that —
#        nothing in the program queries Dn2Cpp.Runtime — so this is the one thing the
#        arm asserts against the emitted C++ instead: its registry row must still be
#        the short byte-identical form with no resource tail at all.
#
#   5. --no-manifest-resources naming no loaded assembly -> the transpile must FAIL
#      (the --no-default-ref contract: a typo silently becoming a no-op drop would
#      keep shipping the very bytes the flag was written to shed).
#
#   6. --manifest-resource-root matching no embedded resource of any dropped
#      assembly -> the transpile must FAIL (the --reflection-root doctrine: a typo
#      silently becoming a no-op root would surface as a
#      PlatformNotSupportedException only in the shipped game).
#
#   7. The SAME program driven with argv "limits" -> a fourth frozen snapshot: the
#      ResourceManager carve-outs, i.e. every read whose honest answer
#      DIVERGES from real .NET and which arm 1's live diff therefore cannot hold. Its
#      own arm below says which and why.
#
#   8. arm 7's argv AND a drop of the app assembly with one set rooted back -> a
#      fifth frozen snapshot: the drop bit and the lookup are two refusals over one
#      table, and this is where they have to compose. A dropped set must not read as
#      an ABSENT one (both would throw, so only the MESSAGE tells them apart), and a
#      rooted set must still answer (or the flag pair silently reverts to
#      all-or-nothing). Its own arm below.
#
#   9. A SECOND program (samples/dotnet/ManifestResourcesSatellite) declaring
#      [NeutralResourcesLanguage(..., UltimateResourceFallbackLocation.Satellite)] ->
#      a sixth frozen snapshot. Assembly-level declarations cannot be mixed, and this
#      one inverts the sample above: with Satellite, real .NET reads a satellite for
#      EVERY ask including the culture-less one and never the main assembly's own
#      blob — so the read dn2cpp is otherwise surest of becomes the wrong answer, and
#      the refusal has to fire there. Its own arm below.
#
# Why the flag arms can only be FROZEN and not diffed live: real .NET does not
# drop, so it never throws here. The frozen text doubles as the assertion that the
# PNSE message names the assembly and the remedy — the message is in the snapshot
# verbatim.
#
# The blobs are emitted only when a body actually reaches one of these APIs
# (Compilation.ManifestResourcesUsed); every other gate in the suite is the
# negative control for that, since their output must be byte-identical to what it
# was before manifest resources existed — and, with no --no-manifest-resources
# passed, to what it was before the trim flag existed.
#
# The argless program's LAST section is about EXCEPTIONS, not resources
# (BclFaultMessageSubset): BCL faults, each caught and its Message read.
# It rides here because the arms that matter are 2-4 — with CoreLib's resources
# dropped, that Message read is where a resource-backed SR implementation would
# fault a second time, inside the diagnostic, uncatchably. dn2cpp folds SR text in
# at transpile time so it does not, and the section is what keeps that answer true;
# the default-flip decision rests on it. The message TEXT is diffed too — a
# runtime-raised fault carries the sentence real .NET raises, and the
# frozen arms show it surviving the drop, which is the whole claim. The one probe
# that stays type-only says at its own call site why.
#
# The flag arms' cache CONTEXT carries the CLI flags (the net10_bcl_diff_gate
# --cli-arg discipline): two arms of one gate that transpile the same project
# under different switches are two different gates, and each also has its own OUT
# (the cache slug is derived from OUT, so sharing one would overwrite the key
# file every run).
source "$(dirname "$0")/_common.sh"

PROJECT=ManifestResources
EXPDIR="$(dirname "$0")/expected"
DROP_FLAGS=(--no-manifest-resources System.Private.CoreLib)
ROOT_FLAGS=("${DROP_FLAGS[@]}" --manifest-resource-root System.Private.CoreLib.Strings.resources)
# Both flags twice over. Dn2Cpp.Runtime is the resource-less second drop (see the
# header's arm 4): a legitimate name that must set no bit.
REPEAT_FLAGS=("${ROOT_FLAGS[@]}" --no-manifest-resources Dn2Cpp.Runtime
    --manifest-resource-root ILLink.Substitutions.xml)

# ── Arm 1: no flag — live diff against real .NET (the control) ────────────────
echo "== Arm 1/9: no flag, exact diff vs real .NET =="
corelib_diff_gate "$PROJECT"
APP="$_CG_APP"
CORELIB="$_CG_CORELIB"

# ── Arm 2: --no-manifest-resources — freeze (dropped assembly throws) ─────────
echo "== Arm 2/9: ${DROP_FLAGS[*]}, diff vs frozen snapshot =="
OUT=artifacts/manifestresources-drop
invoke_cli "$APP" -r "$CORELIB" "${DROP_FLAGS[@]}" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|cliargs:${DROP_FLAGS[*]}|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-dropped.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/manifest-resources-dropped.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 3: + --manifest-resource-root — freeze (rooted name still answers) ────
echo "== Arm 3/9: ${ROOT_FLAGS[*]}, diff vs frozen snapshot =="
OUT=artifacts/manifestresources-rooted
invoke_cli "$APP" -r "$CORELIB" "${ROOT_FLAGS[@]}" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|cliargs:${ROOT_FLAGS[*]}|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-rooted.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/manifest-resources-rooted.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 4: both flags REPEATED — freeze (a second root keeps a second name) ───
echo "== Arm 4/9: ${REPEAT_FLAGS[*]}, diff vs frozen snapshot =="
OUT=artifacts/manifestresources-repeat
invoke_cli "$APP" -r "$CORELIB" "${REPEAT_FLAGS[@]}" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|cliargs:${REPEAT_FLAGS[*]}|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-repeat.txt"; then
    gate_cache_hit_msg
else
    # The resource-less second drop sets no bit, and no probe in the program can
    # observe that — so it is asserted here, against the emitted registry row, which
    # must still be the short form (no resource pointer, no count, no bit).
    # Every generated TU is searched: EmitAssemblyRegistry's text lands in whichever
    # metadata TU is open when the registry is sealed, and that is not a fixed file.
    # The files are passed to grep directly rather than through `cat … | grep -q`.
    # That pipeline is unsound under this suite's `set -o pipefail`: -q exits on the
    # first match, `cat` then dies of SIGPIPE with the ~1.5 MB of resource bytes still
    # unwritten, and the PIPELINE's status becomes 141 — so a matching registry row
    # reports as a missing one. Measured here as a false FAIL whose own diagnostic dump
    # printed the very row it said was absent.
    if ! grep -Eq '"Dn2Cpp\.Runtime", nullptr, 0, "[0-9.]+", "neutral", nullptr \}' \
            "$OUT"/generated*.cpp; then
        echo "FAIL: --no-manifest-resources named the resource-less Dn2Cpp.Runtime and its" >&2
        echo "      registry row no longer has the short no-resource-tail form — a bit set" >&2
        echo "      for an assembly that shed nothing makes its truthful null/empty throw." >&2
        grep -ho 'dn2cpp_assembly_registry\[\] = {.*};' "$OUT"/generated*.cpp >&2 || true
        exit 1
    fi
    echo "registry OK: the resource-less second drop set no bit"
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/manifest-resources-repeat.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arms 5+6: the two typo shapes are hard errors ─────────────────────────────
# Not cached: each is a ~1s transpile that must NOT produce output, so there is
# nothing to key a cache on, and re-running them every time is cheap insurance on
# the typo-is-loud contract (the trim-reflection arm-4 posture).
assert_flag_hard_error() {
    local label="$1" needle="$2" out="$3"; shift 3
    rm -rf "$out"
    local err code
    set +e
    err=$(invoke_cli "$APP" -r "$CORELIB" "$@" -o "$out" 2>&1)
    code=$?
    set -e
    printf '%s\n' "$err" | tail -3
    if [ "$code" -eq 0 ]; then
        echo "FAIL: $label exited 0 — a typo became a silent no-op" >&2
        exit 1
    fi
    # A here-string, not `printf … | grep -q`: grep -q exits at the first match and
    # can beat printf's write, leaving no reader — printf takes SIGPIPE and the
    # pipeline reports 141 under `set -o pipefail`, so a needle that IS there reads
    # as missing. Same hazard as the registry probe in arm 4 below.
    grep -q "$needle" <<<"$err" \
        || { echo "FAIL: the failure did not say \"$needle\"" >&2; exit 1; }
    # generated*, not generated.cpp: emission streams the body/metadata TUs out
    # during compilation, so a probe on the last-written file cannot see a
    # transpile that died mid-emission. The rm -rf above makes any hit
    # this run's own.
    ! compgen -G "$out/generated*" >/dev/null \
        || { echo "FAIL: the failed transpile still emitted C++: $(ls -1 "$out" | tr '\n' ' ')" >&2; exit 1; }
    echo "hard-error OK: exit $code, named the typo, emitted nothing"
}

echo "== Arm 5/9: --no-manifest-resources naming no loaded assembly must FAIL =="
assert_flag_hard_error "--no-manifest-resources No.Such.Assembly" \
    "no loaded assembly is named No.Such.Assembly" \
    artifacts/manifestresources-typo-asm \
    --no-manifest-resources No.Such.Assembly

echo "== Arm 6/9: --manifest-resource-root matching no dropped resource must FAIL =="
assert_flag_hard_error "--manifest-resource-root No.Such.Resource" \
    "carries an embedded resource named No.Such.Resource" \
    artifacts/manifestresources-typo-root \
    "${DROP_FLAGS[@]}" --manifest-resource-root No.Such.Resource

# ── Arm 7: the ResourceManager carve-outs — freeze ────────────────────────────
# The SAME program, no flags, driven with argv "limits" instead of argless. That
# argv is the whole difference, and it exists because arm 1's control cannot hold
# these lines: every one of them is a read whose HONEST answer diverges from real
# .NET, so a live diff would be asserting that dn2cpp reproduces an answer it
# deliberately refuses to give.
#
# What the frozen text pins, message by message (the messages are in the snapshot
# verbatim, which is the assertion that each refusal names what it refused and the
# way out):
#   * culture-fr — real .NET answers the NEUTRAL string here, because this sample
#     builds no fr satellite. dn2cpp links no satellite assembly at all, so from
#     inside the image "no fr satellite was built" and "one exists and I cannot
#     reach it" are one state; serving the neutral would be right in the first case
#     and a silent wrong answer in the second, in a shipped game. It throws instead.
#   * missing-set — dn2cpp raises the type .NET raises, with its own message,
#     so arm 1 diffs the TYPE live and only the message is pinned here. What must not
#     happen either way is the null a missing KEY gives (arm 1's rm-miss): those are
#     different bugs with different fixes.
#   * get-stream — the signature names UnmanagedMemoryStream, so unlike
#     Assembly.GetManifestResourceStream no MemoryStream can stand in for it.
#   * non-string — .NET's InvalidOperationException message names the type it found;
#     dn2cpp raises the same family with its own wording, diffed live in arm 1.
#   * stream-obj / stream-string — GetObject answers every primitive code and
#     byte[], so a Stream entry is what is left that it must refuse, for
#     get-stream's exact reason: the ANSWER is an UnmanagedMemoryStream, not bytes.
# The `neutral` and `mixed-string` lines are the CONTROLS: without them a section
# full of expected throws would read exactly the same if the lookup were broken
# outright.
#
# Its own OUT (and so its own transpile) rather than a reuse of arm 1's: the cache
# slug is derived from OUT, so two arms sharing one would overwrite each other's key
# file every run — the trap this gate's header already states for the flag arms. A
# reuse would also break on a warm arm 1, which serves its green without compiling
# and so leaves no binary to run.
echo "== Arm 7/9: argv 'limits' — the ResourceManager carve-outs, vs frozen snapshot =="
OUT=artifacts/manifestresources-limits
invoke_cli "$APP" -r "$CORELIB" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|argv:limits|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-limits.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT" limits); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/manifest-resources-limits.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 8: the two honesty mechanisms COMPOSE ─────────────────────────────────
# The app assembly's own resources dropped, with exactly one set rooted back, read
# through ResourceManager instead of through Assembly. This is the arm worth having:
# the drop bit and the ResourceManager lookup are two refusals over the same
# table, and the way they fail together is silent in BOTH directions.
#
#   * A dropped set must not read as an ABSENT set. Nothing in the table
#     distinguishes them, and the lookup's answer for an absent set is already a
#     throw — so a lookup that skipped the bit would still throw, still be catchable,
#     and still name a resource, while telling the reader to fix a name that is
#     correct. The frozen text is what pins which of the two messages arrives.
#   * A rooted set must still ANSWER. A refusal that fired on the assembly's bit
#     rather than on a miss would swallow the root, and the flag pair would silently
#     become "all or nothing", which is what the root flag exists to prevent.
#
# Both directions are in this one arm because the flags admit them at once:
# `neutral` and `mixed-string` read the same program state and must disagree, one
# answering off the rooted Strings set and the other refusing for the unrooted Mixed
# one. The messages name ResourceManager.GetString / .GetObject, not Assembly.*,
# which is the other half of the claim: the refusal is threaded from the caller, so
# it points at the API somebody actually called.
DROP_APP_FLAGS=(--no-manifest-resources "$PROJECT"
    --manifest-resource-root ManifestResources.Strings.resources)
echo "== Arm 8/9: ${DROP_APP_FLAGS[*]} + argv 'limits', vs frozen snapshot =="
OUT=artifacts/manifestresources-limits-drop
invoke_cli "$APP" -r "$CORELIB" "${DROP_APP_FLAGS[@]}" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|argv:limits|cliargs:${DROP_APP_FLAGS[*]}|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-limits-dropped.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT" limits); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" \
        "$(cat "$EXPDIR/manifest-resources-limits-dropped.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 9: the Satellite ultimate fallback ────────────────────────────────────
# A SECOND program, and the second one is forced: [NeutralResourcesLanguage] is an
# assembly-level declaration, so an assembly cannot declare MainAssembly (which the
# arms above need, and which is what makes the "en" ask answerable) and Satellite at
# once.
#
# Satellite says the neutral resources live in a satellite assembly and the copy
# embedded in the main assembly is NOT the ultimate fallback. Measured against real
# .NET with no satellite built, all three asks — the culture-less one included —
# raise MissingSatelliteAssemblyException; with a satellite present they read it.
# Either way real .NET never reads the main assembly's own blob, which is the only
# thing dn2cpp can read. So this is the one arm where the answer dn2cpp is otherwise
# SUREST of (the culture-less read) is the wrong one, and the refusal has to fire on
# a path that has no other reason to check anything.
#
# Frozen rather than live-diffed for the usual reason: both sides refuse, in
# different families and with different messages. The `embedded` control line is what
# keeps this from passing for the wrong reason — the resource really is in the image.
PROJECT2=ManifestResourcesSatellite
echo "== Arm 9/9: $PROJECT2 (UltimateResourceFallbackLocation.Satellite), vs frozen =="
build_proj "samples/dotnet/$PROJECT2/$PROJECT2.csproj"
APP2="samples/dotnet/$PROJECT2/bin/$CONFIG/$TFM/$PROJECT2.dll"
OUT=artifacts/manifestresources-satellite
invoke_cli "$APP2" -r "$CORELIB" -o "$OUT"
if gate_cache_check "$OUT" "manifest-resources|project:$PROJECT2|$CORELIB" \
        "$APP2" "${APP2%.dll}.runtimeconfig.json" "${APP2%.dll}.deps.json" \
        "$EXPDIR/manifest-resources-satellite.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT2"
    set +e
    native=$("./$OUT/$PROJECT2"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" \
        "$(cat "$EXPDIR/manifest-resources-satellite.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

echo "OK"
