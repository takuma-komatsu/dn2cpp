#!/usr/bin/env bash
# Generated-output size & native-compile-time measurement — how many bytes of
# C++ the emitter writes and how long clang takes to turn them into a binary,
# for two representative inputs:
#
#   selfhost — the full CLI (dn2cpp.dll = Transpiler + Godot + DotnetModule)
#              transpiling itself, the same emission gates/selfhost-emit.sh
#              performs in its step 3 (--auto-ref, real net10 CoreLib + BCL),
#              then compiled as a console executable.
#   godot    — the GodotSample GDExtension input of
#              gates/build-and-run-godot-sample.sh, transpile + gdextension
#              compile only (the engine is never launched, so no GODOT binary
#              is needed; skipped when extension_api.json is absent).
#
# This is a measurement aid, NOT a regression gate: byte counts and timings
# drift with the toolchain and the installed CoreLib, so it prints numbers only
# and never asserts pass/fail. The `measure-*` name keeps it out of
# run-all-gates.sh, which discovers gates by the `build-and-run-*` glob.
#
# For each input it reports: per-file size of generated.h + every
# generated*.cpp, a per-section byte breakdown of generated.h / generated.cpp
# (the emitter's `// ---- name ----` markers), the cold native build wall time
# (ccache off, fresh per-app build dir; the shared runtime libraries are
# prebuilt outside the timed region), and the slowest build steps parsed from
# ninja's .ninja_log.
#
# Everything (transpile output, build dirs, the report) lives under
# artifacts/gensize/ — gitignored via the blanket artifacts/ entry, same
# placement as measure-size.sh's artifacts/sizemeasure.
#
#   ./gates/measure-gensize.sh                 # both inputs
#   DN2CPP_GENSIZE_OUT=... ./gates/measure-gensize.sh
source "$(dirname "$0")/_common.sh"

OUT_ROOT="${DN2CPP_GENSIZE_OUT:-artifacts/gensize}"
REPORT="$OUT_ROOT/report.txt"
SELF_OUT="$OUT_ROOT/selfhost"
GODOT_OUT="$OUT_ROOT/godot"

mkdir -p "$OUT_ROOT"
# Mirror the whole run into the report file (gitignored, see header).
exec > >(tee "$REPORT") 2>&1

# ── report helpers ────────────────────────────────────────────────────────────

# gen_file_table DIR — byte size + line count of generated.h and every
# generated*.cpp the transpile left in DIR, with a TOTAL row.
gen_file_table() {
    local dir="$1" f b l tb=0 tl=0
    printf '  %12s  %9s  %s\n' 'bytes' 'lines' 'file'
    for f in "$dir/generated.h" "$dir"/generated*.cpp; do
        [ -f "$f" ] || continue
        b=$(wc -c < "$f" | tr -d ' ')
        l=$(wc -l < "$f" | tr -d ' ')
        printf '  %12d  %9d  %s\n' "$b" "$l" "$(basename "$f")"
        tb=$((tb + b)); tl=$((tl + l))
    done
    printf '  %12d  %9d  TOTAL\n' "$tb" "$tl"
}

# section_breakdown FILE [N] — bytes per emitter section (`// ---- name ----`
# markers), sorted descending, top N (default 15). Bytes before the first
# marker are credited to "(preamble)"; each marker line counts toward its own
# section. LC_ALL=C keeps length() counting bytes, not multibyte characters.
section_breakdown() {
    local file="$1" top="${2:-15}"
    head -"$top" <<<"$(LC_ALL=C awk '
        BEGIN { cur = "(preamble)" }
        /^\/\/ ---- .* ----$/ {
            s = $0; sub(/^\/\/ ---- /, "", s); sub(/ ----$/, "", s); cur = s
        }
        { b[cur] += length($0) + 1; l[cur]++ }
        END { for (k in b) printf "%d\t%d\t%s\n", b[k], l[k], k }
    ' "$file" | sort -rn)" | LC_ALL=C awk -F'\t' '
        { printf "  %12d B  %9d lines  %s\n", $1, $2, $3 }'
}

# run_timed LOG CMD... — run CMD with all output to LOG; print its wall-clock
# seconds on success, dump LOG and fail otherwise.
run_timed() {
    local log="$1"; shift
    local timefile rc=0
    timefile="$(mktemp)"
    local TIMEFORMAT='%R'
    { time "$@" > "$log" 2>&1; } 2> "$timefile" || rc=$?
    if [ "$rc" -ne 0 ]; then
        cat "$log" >&2
        rm -f "$timefile"
        return "$rc"
    fi
    cat "$timefile"
    rm -f "$timefile"
}

# cold_build OUT NAME MODE — compile OUT's generated C++ from a fresh per-app
# build dir with ccache off ("" = console executable, gdext = GDExtension
# shared library). The dir wipe stays outside the caller's timed region only
# by being cheap; the configure+build is what dominates.
cold_build() {
    local out="$1" name="$2" mode="$3"
    rm -rf "$(_cmake_app_builddir "$out")"
    (
        export DN2CPP_NO_CCACHE=1
        if [ "$mode" = gdext ]; then
            compile_gdextension "$out" "$out/$(lib_name "$name")"
        else
            compile_console "$out" "$name"
        fi
    )
}

# ninja_steps OUT N — the N slowest build steps of OUT's app build dir, from
# ninja's .ninja_log (v7+: start_ms, end_ms, mtime, output, hash), plus the sum
# over all steps. The dir is wiped per cold build, so the log holds exactly one
# build's entries.
ninja_steps() {
    local out="$1" top="${2:-10}"
    local log; log="$(_cmake_app_builddir "$out")/.ninja_log"
    [ -f "$log" ] || { echo "  (no .ninja_log found)"; return 0; }
    head -"$top" <<<"$(LC_ALL=C awk -F'\t' 'NR > 1 && NF >= 4 {
        n = split($4, p, "/"); printf "%.3f\t%s\n", ($2 - $1) / 1000.0, p[n]
    }' "$log" | sort -rn)" | LC_ALL=C awk -F'\t' '
        { printf "  %9.2f s  %s\n", $1, $2 }'
    LC_ALL=C awk -F'\t' 'NR > 1 && NF >= 4 { s += ($2 - $1) / 1000.0 }
        END { printf "  %9.2f s  sum over all build steps (parallel: wall time is lower)\n", s }' "$log"
}

# report_input LABEL OUT NAME MODE — the full per-input report: file table,
# section breakdowns, cold build, slowest steps.
report_input() {
    local label="$1" out="$2" name="$3" mode="$4"
    echo
    echo "════════════════════════════════════════════════════════════════"
    echo " $label — generated output in $out"
    echo "════════════════════════════════════════════════════════════════"
    echo "-- generated files (bytes / lines) --"
    gen_file_table "$out"
    echo
    echo "-- generated.h sections (top 15 by bytes) --"
    section_breakdown "$out/generated.h" 15
    echo
    echo "-- generated.cpp sections (top 15 by bytes) --"
    section_breakdown "$out/generated.cpp" 15
    echo
    echo "-- cold native build (fresh build dir, DN2CPP_NO_CCACHE=1) --"
    local secs
    secs=$(run_timed "$OUT_ROOT/build-$name.log" cold_build "$out" "$name" "$mode")
    echo "  configure + compile + link wall time: $secs s"
    echo
    echo "-- slowest build steps (.ninja_log, top 10) --"
    ninja_steps "$out" 10
}

# ── header ────────────────────────────────────────────────────────────────────

echo "measure-gensize — $(date '+%Y-%m-%d %H:%M:%S')"
echo "commit: $(git rev-parse --short HEAD 2>/dev/null) ($(git branch --show-current 2>/dev/null))"
echo "host:   $(uname -sm), $(getconf _NPROCESSORS_ONLN 2>/dev/null || echo '?') cpus"
echo "cc:     $(first_line "$(cc --version 2>/dev/null)")"

echo
echo "== 1/3 Building the full CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: CLI not built: $CLI" >&2; exit 1; }
export DN2CPP_CLI_DLL="$PWD/$CLI"
# Prebuild the shared runtime libraries once, outside the timed cold builds.
ensure_cmake_runtime > /dev/null

# ── selfhost input ────────────────────────────────────────────────────────────

echo
echo "== 2/3 selfhost: full CLI transpiles itself =="
# Same reference set and flags as gates/selfhost-emit.sh step 3: net10-pinned
# CoreLib (11-preview shape skews the transpile), real System.Text.Json for the
# --generate-bindings closure, --auto-ref for the sibling Dn2Cpp.Runtime.dll.
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"
refs=()
add_ref() { [ -f "$1" ] && refs+=(-r "$1"); return 0; }
add_ref "$BIN/Dn2Cpp.Transpiler.dll"
add_ref "$BIN/Dn2Cpp.Godot.dll"
add_ref "$BIN/Dn2Cpp.DotnetModule.dll"
add_ref "$corelib"
add_ref "$bcl/System.Text.Json.dll"
for dep in System.Linq System.Reflection.Metadata System.Collections.Immutable System.Collections \
           System.Runtime System.Memory System.Console System.Linq.Expressions \
           System.Text.Encodings.Web System.Threading System.Threading.Tasks; do
    add_ref "$bcl/$dep.dll"
done

rm -rf "$SELF_OUT"
mkdir -p "$SELF_OUT"
secs=$(run_timed "$OUT_ROOT/transpile-selfhost.log" \
    dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$SELF_OUT")
[ -f "$SELF_OUT/generated.cpp" ] || { echo "error: no generated.cpp produced" >&2; exit 1; }
echo "transpile wall time: $secs s"
report_input "selfhost (full CLI, console)" "$SELF_OUT" dn2cpp ""

# ── godot input ───────────────────────────────────────────────────────────────

echo
echo "== 3/3 godot: GodotSample GDExtension (transpile + compile only) =="
if [ ! -f extension_api.json ]; then
    # A section skip, not a whole-run one (the console/selfhost measurements
    # above already ran): gate_partial states the reduced coverage on stdout
    # and fails under DN2CPP_REQUIRE_ALL=1 — never "SKIP + exit 0", which is
    # indistinguishable from a full run.
    gate_partial "godot input not measured: extension_api.json not found at the repository root (needed to build the GodotSharpShim; generate with: godot --headless --dump-extension-api)"
else
    build_proj samples/godot/GodotSample/GodotSample.csproj
    rm -rf "$GODOT_OUT"
    mkdir -p "$GODOT_OUT"
    secs=$(run_timed "$OUT_ROOT/transpile-godot.log" \
        invoke_cli \
            "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
            -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
            -r "$corelib" \
            -o "$GODOT_OUT" --gdextension)
    [ -f "$GODOT_OUT/generated.cpp" ] || { echo "error: no generated.cpp produced" >&2; exit 1; }
    echo "transpile wall time: $secs s"
    report_input "godot (GodotSample, gdextension)" "$GODOT_OUT" godotsample gdext
fi

echo
echo "Report saved to $REPORT"
