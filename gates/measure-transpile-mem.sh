#!/usr/bin/env bash
# Transpiler peak-memory measurement — how much RAM dn2cpp itself needs to turn
# an assembly into C++, and which phase the heap grows in.
#
# This is a measurement aid, NOT a regression gate: peak RSS drifts with the
# runtime, the installed CoreLib, and machine load, so it prints numbers only
# and never asserts pass/fail. The `measure-*` name keeps it out of
# run-all-gates.sh, which discovers gates by the `build-and-run-*` glob.
#
# Three instruments, deliberately paired:
#
#   peak RSS   — /usr/bin/time (`-l` on macOS, `-v` on Linux). The number that
#                actually decides whether a machine survives the transpile.
#   heap curve — the transpiler's own DN2CPP_TIME=1 phase marks, each carrying
#                the managed heap size AND the model populations behind it
#                (classes / reached methods / generic instantiations / deepest
#                type-argument nesting). RSS says how bad; this says WHERE, and
#                the populations say WHY.
#   censuses   — DN2CPP_MODEL_CENSUS=1 counts what the MODEL holds,
#                DN2CPP_EMIT_CENSUS=1 what EMISSION holds. Neither is run from
#                here (both allocate, so they would perturb the RSS this script
#                exists to report) — run them on their own. They split the heap
#                along the line that decides which lever applies: a model term is
#                cut by tree-shaking, an emission term by streaming. Read the
#                census before attacking a phase: it is what shows, for instance,
#                that the interning pools are released two phases BEFORE the peak,
#                so a lever aimed at them cannot move it.
#
# The report's headline is `peakPhase`: the phase whose mark reported the
# largest heap. That is the phase to attack — but confirm it against peak RSS
# first. Measured on selfhost-emit (2026-07-29), the two disagree: the largest
# heap mark is `emit-bodies`, while sampling RSS every ~10 ms puts the actual
# high-water mark in the last ~70 ms of the run, at `emit-rest`/`write-files`.
#
# READ THE HEAP COLUMN CAREFULLY. It is GC.GetTotalMemory(false) — live data PLUS
# whatever garbage the collector has not got to yet — because forcing a collection
# at 15 phase boundaries would wreck the timings that share the same marks. So a
# phase that churns heavily but retains little (write-files: one ToString and one
# UTF-8 encode per translation unit) reads high, and, perversely, a change that
# REDUCES churn can make the number go UP: fewer allocations means fewer
# collections means more uncollected garbage standing at the mark. peak RSS is the
# ground truth; the heap curve is for attributing growth to a phase, and the
# populations for attributing it to a cause. Use the deltas between adjacent
# phases, not the absolute readings.
#
# Corpora (each skipped with a message when its inputs are absent):
#
#   selfhost-emit     the full CLI transpiling itself against the real net10
#                     CoreLib + BCL (--auto-ref) — the largest in-repo input.
#                     Same recipe as gates/selfhost-emit.sh step 3.
#   selfhost-measure  the same input in --measure mode (gates/selfhost-measure.sh).
#                     --measure emits no C++, so it isolates the front end
#                     (load / model / reachability / compile-bodies) from emission.
#   godot-dotnet      the --dotnet-module lane (needs DN2CPP_GODOT_DOTNET_ROOT).
#   godot-gdext       GodotSample as a GDExtension (needs extension_api.json).
#   external          an out-of-repo assembly, via DN2CPP_MEM_EXTRA_DLL — the hook
#                     for a real game (e.g. Thrive) without vendoring one in.
#
# A hard managed-heap cap (DOTNET_GCHeapHardLimit) is applied to every row so a
# runaway input fails fast instead of swapping the machine to death. A row that
# hits it is REPORTED, not fatal: the heap curve captured up to the OOM names
# the phase that ran away, which is exactly the datum we want from it.
#
# The cap USED to cost something — this header carried a +9% caveat, on the
# reasoning that telling the GC it may commit N bytes makes it size its budgets
# against N. Re-measured 2026-07-29 on selfhost-emit, three runs each: 510 MB
# capped vs 511 MB uncapped, i.e. nothing outside the noise. The caveat is
# retired rather than restated with new numbers; the methodology is constant
# across runs either way, so before/after comparisons were always sound.
# `DN2CPP_MEM_CAP_HEX=0` still turns the cap off, at the price of no safety net.
#
# WHAT THE NOISE BAND IS, because it decides what this script can prove. Peak RSS
# on selfhost-emit varies ±20 MB run to run (~4%) in the default GC mode, so a
# lever worth less than that is invisible here however many times you run it. Two
# were measured and rejected for exactly that reason: writing
# generated.h without materializing its 26 MB ToString, and releasing the
# inline-promoted-body buffer as it is copied into the header. Both remove real
# megabytes the emission census can see; neither moves peak RSS out of the band,
# because the GC's slack absorbs them. `DOTNET_GCConserveMemory=9` narrows the
# band to ~±3 MB and is the better A/B instrument — at +22% wall time, which is
# why it is a measurement setting and not a default.
#
#   ./gates/measure-transpile-mem.sh                    # every available corpus
#   DN2CPP_MEM_GC=1 ./gates/measure-transpile-mem.sh    # + the GC-knob A/B block
#   DN2CPP_MEM_CAP_HEX=0x400000000 ./gates/...          # raise the 8 GiB cap (16 GiB)
#   DN2CPP_MEM_CAP_HEX=0 ./gates/...                    # no cap (dangerous)
#
# External-corpus knobs:
#   DN2CPP_MEM_EXTRA_DLL    app assembly to transpile (required to enable the row)
#   DN2CPP_MEM_EXTRA_REFS   extra -r assemblies, space-separated (optional)
#   DN2CPP_MEM_EXTRA_ARGS   extra CLI args, e.g. "--dotnet-module" (optional)
#   DN2CPP_MEM_EXTRA_MODE   measure (default) | emit
#
# Everything lands under artifacts/memmeasure/ (gitignored via the blanket
# artifacts/ entry — NOT gates/out-*, which is not a gitignore glob).
source "$(dirname "$0")/_common.sh"

OUT_ROOT="${DN2CPP_MEM_OUT:-artifacts/memmeasure}"
REPORT="$OUT_ROOT/report.txt"
SUMMARY="$OUT_ROOT/summary.tsv"
# 8 GiB. No in-repo corpus comes near it; a runaway dies here instead of in swap.
CAP_HEX="${DN2CPP_MEM_CAP_HEX:-0x200000000}"

mkdir -p "$OUT_ROOT"
: > "$SUMMARY"
# Mirror the whole run into the report file (gitignored, see header).
exec > >(tee "$REPORT") 2>&1

# ── measurement primitives ───────────────────────────────────────────────────

# The program's stderr and /usr/bin/time's report share one log: the child's
# DN2CPP_TIME phase marks go to stderr, and time appends its resource summary
# after the child exits, so the two never interleave mid-line.
TIME_BIN=/usr/bin/time
case "$(uname -s)" in
    Darwin) TIME_FLAG=-l ;;
    *)      TIME_FLAG=-v ;;
esac

# rss_of LOG — peak resident set size in bytes, or empty when unavailable.
rss_of() {
    case "$(uname -s)" in
        # BSD time: "  1234567890  maximum resident set size" (bytes, value first)
        Darwin) awk '/maximum resident set size/ { print $1; exit }' "$1" ;;
        # GNU time: "Maximum resident set size (kbytes): 1234567"
        *) awk -F: '/Maximum resident set size/ { gsub(/ /, "", $2); print $2 * 1024; exit }' "$1" ;;
    esac
}

# heap_curve LOG — the transpiler's phase marks, in order.
heap_curve() { grep '^dn2cpp-time:' "$1" || true; }

# peak_phase LOG — "<heapMB>\t<phase>\t<counters>" of the mark with the largest
# heap. The counters (classes/reach/inst/minst/gdepth) are what name the cause.
peak_phase() {
    heap_curve "$1" | LC_ALL=C awk '
        {
            heap = ""; phase = $2; counters = ""
            for (i = 1; i <= NF; i++) {
                if ($i == "heap") heap = $(i + 1)
                if ($i == "MB")   { for (j = i + 1; j <= NF; j++) counters = counters (counters ? " " : "") $j; break }
            }
            # Strictly greater, so a tie names the EARLIEST phase to reach the peak —
            # the one the growth happened in, not a later one that merely held it.
            if (heap != "" && heap + 0 > best + 0) { best = heap; bp = phase; bc = counters }
        }
        END { if (bp != "") printf "%s\t%s\t%s\n", best, bp, bc }'
}

# fmt_mb BYTES — bytes as whole MB, or "?" when the input is empty.
fmt_mb() { [ -n "${1:-}" ] && echo $(( $1 / 1048576 )) || echo '?'; }

# run_row NAME LOGBASE CMD... — run one corpus under the peak-RSS timer with the
# phase instrumentation on, append its row to the summary, and print its curve.
# A failing run (typically the heap cap firing) is reported, never fatal — its
# truncated curve is the measurement.
run_row() {
    local name="$1" log="$2"; shift 2
    local timefile rc=0 rss wall peak heapmb phase counters status
    timefile="$(mktemp)"
    local TIMEFORMAT='%R'
    echo
    echo "-- $name"
    # DN2CPP_TIME writes the phase marks to the child's stderr, which lands in $log
    # alongside time's summary; the child's stdout goes to $log.out.
    { time DN2CPP_TIME=1 "$TIME_BIN" "$TIME_FLAG" "$@" > "$log.out" 2> "$log"; } 2> "$timefile" || rc=$?
    wall="$(cat "$timefile")"; rm -f "$timefile"
    rss="$(rss_of "$log")"
    peak="$(peak_phase "$log")"
    heapmb="$(printf '%s' "$peak" | cut -f1)"
    phase="$(printf '%s' "$peak" | cut -f2)"
    counters="$(printf '%s' "$peak" | cut -f3)"
    status=$([ "$rc" -eq 0 ] && echo ok || echo "FAILED(rc=$rc)")

    printf '   peak RSS   %s MB\n' "$(fmt_mb "$rss")"
    printf '   wall       %s s   (%s)\n' "$wall" "$status"
    printf '   peak phase %s MB heap at "%s"  [%s]\n' "${heapmb:-?}" "${phase:-?}" "${counters:-?}"
    echo "   -- heap curve --"
    heap_curve "$log" | LC_ALL=C sed 's/^/   /'
    if [ "$rc" -ne 0 ]; then
        echo "   -- tail of the failing run --"
        tail -5 "$log" | LC_ALL=C sed 's/^/   /'
    fi
    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "$name" "$(fmt_mb "$rss")" "$wall" "${heapmb:-?}" "${phase:-?}" "$status" "${counters:-?}" >> "$SUMMARY"
    return 0
}

# ── header ───────────────────────────────────────────────────────────────────

echo "measure-transpile-mem — $(date '+%Y-%m-%d %H:%M:%S')"
echo "commit: $(git rev-parse --short HEAD 2>/dev/null) ($(git branch --show-current 2>/dev/null))"
echo "host:   $(uname -sm), $(getconf _NPROCESSORS_ONLN 2>/dev/null || echo '?') cpus"
echo "dotnet: $(dotnet --version 2>/dev/null)"
if [ "$CAP_HEX" = "0" ]; then
    echo "heap cap: DISABLED (a runaway input will swap this machine)"
else
    export DOTNET_GCHeapHardLimit="$CAP_HEX"
    echo "heap cap: DOTNET_GCHeapHardLimit=$CAP_HEX ($(( $((CAP_HEX)) / 1073741824 )) GiB) — a row that hits it is reported, not fatal"
fi

echo
echo "== Building the CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: CLI not built: $CLI" >&2; exit 1; }

# ── the selfhost reference set (gates/selfhost-emit.sh step 3) ───────────────

corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); return 0; }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Godot.dll"
add_ref "$BIN/Dn2Cpp.DotnetModule.dll"
add_ref "$BIN/Dn2Cpp.Runtime.dll"
add_ref "$corelib"
add_ref "$bcl/System.Text.Json.dll"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.Encodings.Web System.Threading System.Threading.Tasks; do
    add_ref "$bcl/$dep.dll"
done

echo
echo "== Corpora =="

# 1. selfhost, emit — the largest in-repo input, full pipeline including emission.
rm -rf "$OUT_ROOT/selfhost-emit"; mkdir -p "$OUT_ROOT/selfhost-emit"
run_row "selfhost-emit" "$OUT_ROOT/selfhost-emit.log" \
    dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$OUT_ROOT/selfhost-emit"

# 2. selfhost, measure — the same input with no emission: the front end alone.
#    This is the mode a real game's --measure run blew 20 GB in.
rm -rf "$OUT_ROOT/selfhost-measure"; mkdir -p "$OUT_ROOT/selfhost-measure"
run_row "selfhost-measure" "$OUT_ROOT/selfhost-measure.log" \
    dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref --measure -o "$OUT_ROOT/selfhost-measure"

# 3. godot-dotnet — the mono-module drop-in lane against the real GodotSharp.
GODOT_DOTNET_ROOT="${DN2CPP_GODOT_DOTNET_ROOT:-}"
GODOT_DOTNET_SHARP="$GODOT_DOTNET_ROOT/GodotSharp/Api/Release/GodotSharp.dll"
DOTNET_APP="samples/godot-dotnet/DotnetSample/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
if [ -n "$GODOT_DOTNET_ROOT" ] && [ -f "$GODOT_DOTNET_SHARP" ] && [ -f "$DOTNET_APP" ]; then
    rm -rf "$OUT_ROOT/godot-dotnet"; mkdir -p "$OUT_ROOT/godot-dotnet"
    run_row "godot-dotnet" "$OUT_ROOT/godot-dotnet.log" \
        dotnet exec "$CLI" "$DOTNET_APP" --dotnet-module -r "$corelib" -r "$GODOT_DOTNET_SHARP" \
        --auto-ref -o "$OUT_ROOT/godot-dotnet"
else
    echo
    echo "-- godot-dotnet: SKIP (needs DN2CPP_GODOT_DOTNET_ROOT and an ExportRelease build of"
    echo "   samples/godot-dotnet/DotnetSample; see gates/setup-godot-dotnet.sh)"
fi

# 4. godot-gdext — the GDExtension lane. Small; a fast sanity row, not a stress test.
if [ -f extension_api.json ]; then
    build_proj samples/godot/GodotSample/GodotSample.csproj
    GDEXT_APP="samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll"
    rm -rf "$OUT_ROOT/godot-gdext"; mkdir -p "$OUT_ROOT/godot-gdext"
    run_row "godot-gdext" "$OUT_ROOT/godot-gdext.log" \
        dotnet exec "$CLI" "$GDEXT_APP" \
        -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" -r "$corelib" \
        --gdextension -o "$OUT_ROOT/godot-gdext"
else
    echo
    echo "-- godot-gdext: SKIP (no extension_api.json at the repository root;"
    echo "   generate with: godot --headless --dump-extension-api)"
fi

# 5. external — a real out-of-repo game assembly. The 20 GB repro lives here.
if [ -n "${DN2CPP_MEM_EXTRA_DLL:-}" ]; then
    if [ ! -f "$DN2CPP_MEM_EXTRA_DLL" ]; then
        echo
        echo "-- external: SKIP (DN2CPP_MEM_EXTRA_DLL not found: $DN2CPP_MEM_EXTRA_DLL)"
    else
        extra=()
        for r in ${DN2CPP_MEM_EXTRA_REFS:-}; do extra+=(-r "$r"); done
        for a in ${DN2CPP_MEM_EXTRA_ARGS:-}; do extra+=("$a"); done
        [ "${DN2CPP_MEM_EXTRA_MODE:-measure}" = "measure" ] && extra+=(--measure)
        rm -rf "$OUT_ROOT/external"; mkdir -p "$OUT_ROOT/external"
        echo
        echo "   external input: $DN2CPP_MEM_EXTRA_DLL"
        run_row "external" "$OUT_ROOT/external.log" \
            dotnet exec "$CLI" "$DN2CPP_MEM_EXTRA_DLL" "${extra[@]}" --auto-ref -o "$OUT_ROOT/external"
    fi
fi

# ── GC-knob A/B (opt-in: it re-runs the largest corpus three more times) ─────
# These are the env equivalents of the csproj properties, so the block decides
# whether to bake ServerGarbageCollection / ConcurrentGarbageCollection /
# GCConserveMemory into Dn2Cpp.Cli.csproj with zero code change.
#
# It has been run, and the verdict is that the transpiler's peak is more GC
# POLICY than transpiler retention — which is the single most useful thing to
# know before spending a week on a retention lever. Measured 2026-07-29 on
# selfhost-emit against a 493 MB default-mode baseline:
#
#   GCConserveMemory=9   421-428 MB (-14%)   at +22% wall time
#   GCConserveMemory=5   473-474 MB ( -4%)   at +8%
#   gcConcurrent=0       624-632 MB (+27%)   — background GC is already earning
#                                              its keep; do not turn it off
#
# None of the three is baked in. Conserve buys 14% of a number no machine is
# short of, and pays a fifth of the transpile's wall time for it on every gate
# run; concurrent is already on and already the right default.
if [ "${DN2CPP_MEM_GC:-0}" = "1" ]; then
    echo
    echo "== GC-knob A/B on selfhost-emit =="
    gc_ab() { # gc_ab LABEL VAR VALUE — one run of the largest corpus under one knob
        local label="$1" var="$2" val="$3"
        rm -rf "$OUT_ROOT/gc-$label"; mkdir -p "$OUT_ROOT/gc-$label"
        # Subshell so the knob (and only it) is set for this one run.
        ( export "$var=$val"
          run_row "gc:$label" "$OUT_ROOT/gc-$label.log" \
              dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$OUT_ROOT/gc-$label" )
    }
    gc_ab "concurrent-off" DOTNET_gcConcurrent 0
    gc_ab "conserve-9"     DOTNET_GCConserveMemory 9
    gc_ab "server-on"      DOTNET_gcServer 1
fi

# ── summary ──────────────────────────────────────────────────────────────────

echo
echo "== Summary =="
printf '%-18s %10s %8s %10s %-22s %-14s %s\n' \
    corpus peakRSS_MB wall_s peakHeapMB peakPhase status populations
while IFS=$'\t' read -r name rss wall heap phase status counters; do
    printf '%-18s %10s %8s %10s %-22s %-14s %s\n' \
        "$name" "$rss" "$wall" "$heap" "$phase" "$status" "$counters"
done < "$SUMMARY"

echo
echo "Report saved to $REPORT"
