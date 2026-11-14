#!/usr/bin/env bash
# Embedded resources, ResourceManager lookups and explicit per-assembly trimming.
# The default runs CLR parity and the untrimmed limits on one binary, then frozen
# repeated-root and app-drop/root composition checks on separate binaries.
# DN2CPP_MANIFEST_RESOURCES_FULL=1 also selects individual trim arms, satellite
# fallback and local DateTime kind.
# Frozen snapshots pin deliberate refusals that cannot be compared with CLR.
source "$(dirname "$0")/_common.sh"
case "${DN2CPP_MANIFEST_RESOURCES_FULL:-0}" in
    0|1) DN2CPP_MANIFEST_RESOURCES_FULL=${DN2CPP_MANIFEST_RESOURCES_FULL:-0} ;;
    *) echo 'DN2CPP_MANIFEST_RESOURCES_FULL must be 0 or 1' >&2; exit 1 ;;
esac
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|manifest-resources-full:$DN2CPP_MANIFEST_RESOURCES_FULL"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/expected/manifest-resources-limits.txt"
if [ "$DN2CPP_MANIFEST_RESOURCES_FULL" = 1 ]; then
    echo 'manifest resource coverage: full trim, route and preprocessing matrix'
else
    echo 'manifest resource coverage: CLR parity, repeated roots and trim/lookup composition'
    echo 'manifest resource extended checks: skipped (not selected); not counted as passed'
fi

DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|resource-receivers-prefix:${DN2CPP_BEFORE_RM_RECEIVERS:-}"

gate_extra_asserts() {
    local out="$1"
    gate_output byte-witness "$out/native.stdout" 'resource manager receivers end' --count any --policy crlf
    DN2CPP_BEFORE_RM_RECEIVERS=1 gate_capture_ok "$out/before-resource-receivers" \
        run_bounded "$out/ManifestResources$EXE_EXT"
    gate_output select "$out/native.stdout" "$out/resource-receivers-prefix.stdout" \
        --before '== resource manager receivers ==' --policy crlf
    gate_output compare "$out/resource-receivers-prefix.stdout" "$out/before-resource-receivers.stdout"

    # Run before the control's cache commit; a warm cache need not retain a binary.
    gate_capture_ok "$out/limits" run_bounded "$out/ManifestResources$EXE_EXT" limits
    gate_output compare "$out/limits.stdout" "$EXPDIR/manifest-resources-limits.txt" --policy crlf

    # Private reader type guards reached by name match .NET; BinaryFormatter
    # construction, which .NET performs, is refused.
    gate_capture_ok "$out/reader-guards-oracle" run_bounded dotnet "$_CG_APP" reader-guards
    gate_capture_ok "$out/reader-guards" run_bounded "$out/ManifestResources$EXE_EXT" reader-guards
    gate_output byte-witness "$out/reader-guards.stdout" 'resource reader type guards end' --count 1 --policy crlf
    gate_output compare "$out/reader-guards-oracle.stdout" "$out/reader-guards.stdout" --policy crlf
    gate_capture_ok "$out/formatter-limit" run_bounded "$out/ManifestResources$EXE_EXT" formatter-limit
    gate_assert_text "$out/formatter-limit.stdout" 'create-formatter -> NotSupportedException: BinaryFormatter-serialized resource types are not supported. Store the value as one of the primitive resource types (string, numeric types, byte[]) instead.
'

}

PROJECT=ManifestResources
EXPDIR="$(dirname "$0")/expected"
DROP_FLAGS=(--no-manifest-resources System.Private.CoreLib)
ROOT_FLAGS=("${DROP_FLAGS[@]}" --manifest-resource-root System.Private.CoreLib.Strings.resources)
# Both flags twice over. Dn2Cpp.Runtime is the resource-less second drop (see the
# repeat arm): a legitimate name that must set no bit.
REPEAT_FLAGS=("${ROOT_FLAGS[@]}" --no-manifest-resources Dn2Cpp.Runtime
    --manifest-resource-root ILLink.Substitutions.xml)

# ── Arm 1: no flag — live diff against real .NET (the control) ────────────────
echo "== Arm 1/9: no flag, exact diff vs real .NET =="
corelib_diff_gate "$PROJECT"
APP="$_CG_APP"
CORELIB="$_CG_CORELIB"

if [ "$DN2CPP_MANIFEST_RESOURCES_FULL" = 1 ]; then
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
    gate_capture_ok "$OUT/native" run_bounded "./$OUT/$PROJECT"
    gate_output compare "$OUT/native.stdout" "$EXPDIR/manifest-resources-dropped.txt" --policy crlf
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
    gate_capture_ok "$OUT/native" run_bounded "./$OUT/$PROJECT"
    gate_output compare "$OUT/native.stdout" "$EXPDIR/manifest-resources-rooted.txt" --policy crlf
    gate_cache_commit
fi

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
    gate_capture_ok "$OUT/native" run_bounded "./$OUT/$PROJECT"
    gate_output compare "$OUT/native.stdout" "$EXPDIR/manifest-resources-repeat.txt" --policy crlf
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

# Arm 7's untrimmed limits share arm 1's executable and cache assertion hook.

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
    gate_capture_ok "$OUT/native" run_bounded "./$OUT/$PROJECT" limits
    gate_output compare "$OUT/native.stdout" "$EXPDIR/manifest-resources-limits-dropped.txt" --policy crlf
    gate_cache_commit
fi

if [ "$DN2CPP_MANIFEST_RESOURCES_FULL" != 1 ]; then
    echo "OK"
    exit 0
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
    gate_capture_ok "$OUT/native" run_bounded "./$OUT/$PROJECT2"
    gate_output compare "$OUT/native.stdout" "$EXPDIR/manifest-resources-satellite.txt" --policy crlf
    gate_cache_commit
fi

echo "OK"

# Binary DateTime kind 3 carries the Local bit and must survive strict validation.
kind_app="gates/fixtures/runtime-resource-kind/bin/$CONFIG/$TFM/RuntimeResourceKind.dll"
build_gate_proj gates/fixtures/runtime-resource-kind/RuntimeResourceKind.csproj
kind_out=artifacts/runtime-resource-kind
invoke_cli "$kind_app" -r "$CORELIB" -o "$kind_out"
if gate_cache_check "$kind_out" "resource-kind3|$CORELIB|cli:$(_gate_cli_hash)" \
        "$kind_app" "${kind_app%.dll}.runtimeconfig.json" "${kind_app%.dll}.deps.json" \
        gates/fixtures/runtime-resource-kind/MixedKind3.resources; then
    gate_cache_hit_msg
else
    compile_console "$kind_out" RuntimeResourceKind
    gate_capture_ok "$kind_out/oracle" run_bounded dotnet "$kind_app"
    gate_capture_ok "$kind_out/native" run_bounded "./$kind_out/RuntimeResourceKind$EXE_EXT"
    gate_output compare "$kind_out/native.stdout" "$kind_out/oracle.stdout"
    gate_output byte-witness "$kind_out/native.stdout" --policy crlf --count any \
        'resource local kind=Local' 'resource local kind end'
    gate_cache_commit
fi
