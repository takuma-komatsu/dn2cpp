#!/usr/bin/env bash
# ONE-OFF measurement (NOT a regression gate — `measure-*` name keeps it out of
# run-all-gates.sh's `build-and-run-*` glob, same treatment as measure-gcpause.sh
# / selfhost-measure.sh). Numbers are a point-in-time snapshot, non-deterministic
# across machines/loads; this prints them, it never asserts pass/fail.
#
# Compares two ways of packaging dn2cpp itself:
#   selfhost — dn2cpp transpiles its own full CLI (`dn2cpp.dll` = Transpiler +
#              Godot + DotnetModule + Runtime) to C++ and CMake-builds it
#              natively (reuses gates/selfhost-emit.sh's steps 1-4).
#   aot      — `dotnet publish -p:PublishAot=true` of the SAME Dn2Cpp.Cli
#              project (no csproj change — AOT is a command-line-only publish
#              property).
#
# Then times all THREE ways of running the identical task — re-transpiling
# `dn2cpp.dll` back to C++ with the same full reference set and --auto-ref —
# to isolate the JIT-vs-AOT-vs-transpiled-native execution-speed delta from
# everything else:
#   managed  — `dotnet exec dn2cpp.dll ...`   (JIT baseline)
#   selfhost — the native self-host binary above
#   aot      — the NativeAOT-published binary above
#
# Package-size comparison is the `selfhost` vs `aot` binaries directly (both
# self-contained, single native executables — InvariantGlobalization=true on
# Dn2Cpp.Cli.csproj already keeps NativeAOT's ICU footprint out of the picture).
source "$(dirname "$0")/_common.sh"

OUT="${1:-artifacts/selfhost-vs-aot}"
RUNS="${RUNS:-5}"

rm -rf "$OUT"
mkdir -p "$OUT"

echo "== 1/6 Building the full CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ]  || { echo "error: CLI not built: $CLI"   >&2; exit 1; }

echo "== 2/6 Locating the real net10.0 CoreLib + BCL + System.Text.Json =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
echo "corelib: $corelib"

# Same reference set as gates/selfhost-emit.sh: dn2cpp's own DLLs, real
# System.Text.Json (the --generate-bindings closure), and the real BCL
# assemblies (including the real System.Linq) the full CLI's reachable code
# touches.
#
# Dn2Cpp.Runtime.dll is deliberately NOT in it — same as gates/selfhost-emit.sh, and
# here it is load-set arithmetic rather than fidelity. Each of the three CLIs has to
# find its own sibling copy through the auto-reference, because TranspileDriver.Run's
# shim injection dedupes against the passed paths by FULL PATH: a CLI whose base
# directory holds a Dn2Cpp.Runtime.dll of its own, handed a -r naming a different
# file of the same assembly, loads the assembly TWICE. The NativeAOT publish output
# is exactly that CLI (`dotnet publish` copies the csproj's Content ProjectReference
# rows — measured, all four arrive), so passing it here made the three sides load
# different numbers of modules: measured on a HelloWorld transpile, the AOT binary
# given `-r <cli-bin>/Dn2Cpp.Runtime.dll` reports "3 assemblies, 47 types" where the
# managed CLI over the same arguments reports "2 assemblies, 42 types". This script's
# entire claim is that the three do the SAME work at different speeds; a side that
# loads a module twice is not doing it.
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

echo "== 3/6 Emitting + native-building the self-hosted dn2cpp (selfhost-emit.sh steps 1-4) =="
managed_out="$OUT/managed-emit"
mkdir -p "$managed_out"
dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$managed_out"
[ -f "$managed_out/generated.cpp" ] || { echo "error: no generated.cpp produced" >&2; exit 1; }
compile_console "$managed_out" dn2cpp
SELFHOST_BIN="$managed_out/dn2cpp"
[ -x "$SELFHOST_BIN" ] || { echo "error: native self-host binary not produced" >&2; exit 1; }
# Lay the binary out the way an installed toolchain is — bin/dn2cpp beside the
# managed support shim and the conditionally-referenced backends — so this side
# resolves the same sibling set from its own base directory as the other two do
# from theirs (managed: src/Dn2Cpp.Cli/bin/…; aot: the publish output, which carries
# all four itself). Same copies, same reason, as gates/selfhost-emit.sh step 4/5.
#
# Equalizing the LOAD SET is the point, and it is not a fidelity nicety: what the
# transpiler emits is a function of the load set, not of reachability, and one of
# these IS loaded here without a single reachable call — the --auto-ref closure pulls
# System.IO.Compression in through Dn2Cpp.Transpiler -> System.Reflection.Metadata,
# which is precisely DnZlib's injection trigger. A side missing the file records
# DefaultRefOutcome.NotFound, which prints NOTHING by design (falling back to the
# native route is normal behaviour), so the three would silently transpile three
# different programs and this script would report their speeds as comparable.
# gates/selfhost-emit.sh's header carries the argument in full, including why all
# three backends are copied when only DnZlib is injected today.
cp -f "$BIN/Dn2Cpp.Runtime.dll" "$managed_out/Dn2Cpp.Runtime.dll"
cp -f "$BIN/DnZlib.dll"   "$managed_out/DnZlib.dll"
cp -f "$BIN/DnBrotli.dll" "$managed_out/DnBrotli.dll"
cp -f "$BIN/DnHttp.dll"   "$managed_out/DnHttp.dll"
echo "self-host native binary: $SELFHOST_BIN"

echo "== 4/6 NativeAOT publish of the same Dn2Cpp.Cli project =="
case "$DN2CPP_OS-$(uname -m)" in
    macos-arm64)               RID=osx-arm64 ;;
    macos-x86_64)              RID=osx-x64 ;;
    linux-aarch64|linux-arm64) RID=linux-arm64 ;;
    linux-x86_64)              RID=linux-x64 ;;
    *) echo "error: unsupported OS/arch for NativeAOT publish: $DN2CPP_OS/$(uname -m)" >&2; exit 1 ;;
esac
aot_out="$OUT/aot-publish"
rm -rf "$aot_out"
dotnet publish src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj -c "$CONFIG" -r "$RID" \
    --self-contained -p:PublishAot=true -o "$aot_out"
AOT_BIN="$aot_out/dn2cpp"
[ -x "$AOT_BIN" ] || { echo "error: NativeAOT binary not produced: $AOT_BIN" >&2; exit 1; }
echo "NativeAOT binary: $AOT_BIN"
echo "-- $aot_out contents --"
ls -la "$aot_out"

# time_runs LABEL N OUTDIR CMD... — run CMD N times (OUTDIR wiped+recreated each
# iteration so no stale files skew the run), collect wall-clock via `time`,
# print each run and the median. Leaves OUTDIR holding the LAST run's output
# (used below for the fixpoint sanity diff). Sets MEDIAN as a side effect.
time_runs() {
    local label="$1" n="$2" outdir="$3"; shift 3
    local times=() t tf
    local TIMEFORMAT='%R'
    for ((i = 1; i <= n; i++)); do
        rm -rf "$outdir"; mkdir -p "$outdir"
        tf="$(mktemp)"
        { time "$@" >/dev/null; } 2>"$tf"
        t="$(cat "$tf")"; rm -f "$tf"
        times+=("$t")
        echo "  [$label] run $i/$n: ${t}s"
    done
    local sorted mid
    sorted=($(printf '%s\n' "${times[@]}" | sort -n))
    mid=$(( (n - 1) / 2 ))
    MEDIAN="${sorted[$mid]}"
    echo "  [$label] median: ${MEDIAN}s  (sorted: ${sorted[*]})"
}

echo "== 5/6 Timing $RUNS runs of each (re-transpile dn2cpp.dll -> C++) =="
echo "-- managed (dotnet exec) --"
time_runs managed "$RUNS" "$OUT/timing-managed" \
    dotnet exec "$CLI" "$CLI" "${refs[@]}" --auto-ref -o "$OUT/timing-managed"
MANAGED_MEDIAN="$MEDIAN"

echo "-- selfhost (native, transpiled-to-C++) --"
time_runs selfhost "$RUNS" "$OUT/timing-selfhost" \
    "./$SELFHOST_BIN" "$CLI" "${refs[@]}" --auto-ref -o "$OUT/timing-selfhost"
SELFHOST_MEDIAN="$MEDIAN"

echo "-- aot (NativeAOT) --"
time_runs aot "$RUNS" "$OUT/timing-aot" \
    "./$AOT_BIN" "$CLI" "${refs[@]}" --auto-ref -o "$OUT/timing-aot"
AOT_MEDIAN="$MEDIAN"

echo "== 6/6 Sanity: all three re-transpile to the identical generated.cpp =="
fail=0
for variant in selfhost aot; do
    for f in generated.h "$managed_out"/generated*.cpp; do
        f="${f##*/}"
        if ! diff -q "$managed_out/$f" "$OUT/timing-$variant/$f" >/dev/null 2>&1; then
            echo "FAIL: $variant output differs from managed in $f" >&2
            fail=1
        fi
    done
done
[ "$fail" -eq 0 ] && echo "OK: managed / selfhost / aot all agree byte-for-byte"

selfhost_size=$(wc -c < "$SELFHOST_BIN" | tr -d ' ')
aot_size=$(wc -c < "$AOT_BIN" | tr -d ' ')

echo
echo "════════════════════════════════════════════════════════════════"
echo " Package size (single native executable)"
echo "════════════════════════════════════════════════════════════════"
printf "  %-10s %12d bytes  (%s)\n" selfhost "$selfhost_size" "$(du -h "$SELFHOST_BIN" | cut -f1)"
printf "  %-10s %12d bytes  (%s)\n" aot      "$aot_size"      "$(du -h "$AOT_BIN" | cut -f1)"
printf "  ratio (aot / selfhost): %.2fx\n" "$(echo "$aot_size $selfhost_size" | awk '{print $1/$2}')"
echo
echo "════════════════════════════════════════════════════════════════"
echo " Re-transpile dn2cpp.dll -> C++ (median of $RUNS runs)"
echo "════════════════════════════════════════════════════════════════"
printf "  %-10s %8ss\n" managed  "$MANAGED_MEDIAN"
printf "  %-10s %8ss\n" selfhost "$SELFHOST_MEDIAN"
printf "  %-10s %8ss\n" aot      "$AOT_MEDIAN"
echo
awk -v m="$MANAGED_MEDIAN" -v s="$SELFHOST_MEDIAN" -v a="$AOT_MEDIAN" 'BEGIN {
    printf "  selfhost vs managed: %.2fx faster\n", m/s
    printf "  aot vs managed:      %.2fx faster\n", m/a
    printf "  selfhost vs aot:     %.2fx %s\n", (a>s ? a/s : s/a), (a>s ? "faster" : "slower")
}'
