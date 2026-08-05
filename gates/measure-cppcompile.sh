#!/usr/bin/env bash
# C++ compile-cost measurement — what it costs to turn the transpiler's OUTPUT
# into an object file, and how much of that is `generated.h` being re-parsed by
# every translation unit.
#
# A measurement aid, NOT a regression gate: the numbers move with the compiler,
# the machine and its load, so it prints and never asserts. The `measure-*` name
# keeps it out of run-all-gates.sh's `build-and-run-*` glob.
#
# The question it answers. Every `generated_b*.cpp` / `generated_m*.cpp`
# opens with `#include "generated.h"`, and most of that header is forward
# declarations plus inline-promoted bodies — so N translation units parse it N
# times. `DN2CPP_PCH` (runtime/CMakeLists.txt, default ON) precompiles it once;
# this script is the A/B that says what that is worth on a given corpus, and
# what is left for anyone tempted to split the declaration block from the inline
# bodies. Splitting changes emitted text, so it would have to buy more than the
# residual this reports — the self-host fixpoint and every frozen expectation are
# on the other side of that trade.
#
# READ CPU, NOT WALL, unless the machine is idle. `user` is work done and is
# robust to a loaded host; `real` and the per-edge durations from `.ninja_log`
# are elapsed time and inflate with anything else running. The two answer
# different questions and both are printed: CPU decides the parallel suite, where
# 121 gates saturate the cores; wall decides one gate run by hand. They can point
# opposite ways, and the reason is structural rather than noise — the PCH edge is
# serial and precedes every TU, so below the machine's parallel width it adds its
# whole duration to the critical path while still removing work.
#
# peak RSS is `/usr/bin/time`'s ru_maxrss over children: the largest single
# compiler process, not the sum of the parallel ones. Multiply by the job count
# for what the build actually asks of the machine.
#
# Corpora — each an OUT dir some gate or harness has already transpiled into,
# skipped with a message when absent. They deliberately straddle the parallel
# width, which is what decides the wall-clock direction:
#
#   arraycore          gates/build-and-run-array-core.sh
#   compressioncore    gates/build-and-run-compression-core.sh
#   selfhost-fullcli   gates/selfhost-emit.sh (the largest in-repo input)
#
# External corpus, the hook for a real game without vendoring one in:
#   DN2CPP_CC_EXTRA_OUT   transpiled OUT dir (required to enable the row)
#   DN2CPP_CC_EXTRA_BIN   its binary name (required with the above)
#
#   ./gates/measure-cppcompile.sh                  # every available corpus
#   ./gates/measure-cppcompile.sh artifacts/x:Bin  # explicit OUT:BIN corpora
#
# ccache is forced off: this measures compiler work, and a cache hit measures
# the cache. Each corpus's CMake build dir is removed on the way out — a dir
# left carrying `-DDN2CPP_PCH=OFF` would keep it, since `option()` never
# overwrites a cached value and no later gate passes the flag that would.
source "$(dirname "$0")/_common.sh"

# The timed inner build re-enters this script rather than writing a driver
# elsewhere: _common.sh derives the repo root from its caller's path, so the
# process /usr/bin/time measures has to be a script that lives in gates/.
if [ -n "${DN2CPP_CC_BUILD_ONLY:-}" ]; then
    compile_console "$1" "$2"
    exit
fi

OUT_ROOT="${DN2CPP_CC_OUT:-artifacts/ccmeasure}"
REPORT="$OUT_ROOT/report.txt"
mkdir -p "$OUT_ROOT"
exec > >(tee "$REPORT") 2>&1

TIME_BIN=/usr/bin/time
case "$(uname -s)" in
    Darwin) TIME_FLAG=-l ;;
    *)      TIME_FLAG=-v ;;
esac

# rss_of LOG — peak resident set size in bytes over the run's children.
rss_of() {
    case "$(uname -s)" in
        Darwin) awk '/maximum resident set size/ { print $1; exit }' "$1" ;;
        *) awk -F: '/Maximum resident set size/ { gsub(/ /, "", $2); print $2 * 1024; exit }' "$1" ;;
    esac
}

# secs_of LOG KIND — real|user seconds from the time report.
secs_of() {
    case "$(uname -s)" in
        # BSD time puts real/user/sys on one line: "  4.73 real  3.51 user  0.86 sys".
        Darwin) awk -v k="$2" '{ for (i = 2; i <= NF; i++) if ($i == k) { print $(i - 1); exit } }' "$1" ;;
        *) awk -F: -v k="$2" '
            /Elapsed \(wall clock\)/ && k == "real" { n = split($0, p, " "); split(p[n], t, ":");
                print (length(t) > 1 ? t[1] * 60 + t[2] : t[1]); exit }
            /User time/ && k == "user" { gsub(/ /, "", $2); print $2; exit }' "$1" ;;
    esac
}

# The header's own shape: the ticket's premise was a claim about this table.
header_shape() {
    LC_ALL=C awk '
        /^\/\/ ---- / { if (sec != "") { printf "      %-42s %8d lines %7.2f MB\n", sec, n, b / 1e6 }
                        sec = substr($0, 9); n = 0; b = 0; next }
        { n++; b += length($0) + 1 }
        END { if (sec != "") printf "      %-42s %8d lines %7.2f MB\n", sec, n, b / 1e6 }
    ' "$1"
}

# one_build OUT BIN PCH TAG — a cold CMake build of OUT's generated C++.
one_build() {
    local out="$1" bin="$2" pch="$3" tag="$4"
    local bd; bd="$(_cmake_app_builddir "$out")"
    rm -rf "$bd"
    (
        export DN2CPP_NO_CCACHE=1
        export DN2CPP_EXTRA_CMAKE_ARGS="-DDN2CPP_PCH=$pch"
        export DN2CPP_CC_BUILD_ONLY=1
        "$TIME_BIN" "$TIME_FLAG" "$PWD/gates/measure-cppcompile.sh" "$out" "$bin" \
            2> "$OUT_ROOT/$tag.time" > "$OUT_ROOT/$tag.log"
    ) || { echo "  FAILED: see $OUT_ROOT/$tag.log"; return 1; }
    cp "$bd/.ninja_log" "$OUT_ROOT/$tag.ninjalog" 2>/dev/null || true
}

# edge_report NINJALOG — elapsed span and the per-kind edge breakdown.
edge_report() {
    LC_ALL=C awk -F'\t' '
        /^#/ { next }
        { d = ($2 - $1) / 1000; sum += d
          if ($1 < lo || n == 0) lo = $1
          if ($2 > hi) hi = $2
          n++
          if ($4 ~ /pch/)                 { np++; tp += d }
          else if ($4 ~ /\/generated_b/)  { nb++; tb += d }
          else if ($4 ~ /\/generated_m/)  { nm++; tm += d }
          else if ($4 ~ /generated\.cpp/) { ng++; tg += d } }
        END { printf "      edges %d  ninja span %.2fs  edge-time sum %.1fs\n", n, (hi - lo) / 1000, sum
              if (np) printf "      pch %.2fs |", tp
              else    printf "      pch    -- |"
              printf " generated.cpp %.2fs | body %d avg %.2fs | metadata %d avg %.2fs\n",
                     tg, nb, (nb ? tb / nb : 0), nm, (nm ? tm / nm : 0) }
    ' "$1"
}

measure_corpus() {
    local out="$1" bin="$2" who="$3"
    if [ ! -f "$out/generated.h" ]; then
        echo "== $out — SKIPPED (not transpiled; run $who)"
        return 0
    fi
    local tus; tus=$(ls "$out"/generated*.cpp 2>/dev/null | wc -l | tr -d ' ')
    echo "== $out ($bin) — $tus TUs, generated.h $(wc -c < "$out/generated.h" | tr -d ' ') bytes"
    header_shape "$out/generated.h"
    local tag pch
    for pch in ON OFF; do
        tag="$(basename "$out")-pch$pch"
        one_build "$out" "$bin" "$pch" "$tag" || continue
        printf '   PCH=%-3s  real %ss  user %ss  peak clang RSS %s MB\n' "$pch" \
            "$(secs_of "$OUT_ROOT/$tag.time" real)" \
            "$(secs_of "$OUT_ROOT/$tag.time" user)" \
            "$(( $(rss_of "$OUT_ROOT/$tag.time") / 1000000 ))"
        [ -f "$OUT_ROOT/$tag.ninjalog" ] && edge_report "$OUT_ROOT/$tag.ninjalog"
    done
    # Leave nothing configured off-default; see the header.
    rm -rf "$(_cmake_app_builddir "$out")"
}

echo "== C++ compile cost — DN2CPP_PCH A/B, ccache off, $(uname -s) $(nproc 2>/dev/null || sysctl -n hw.ncpu) cores"
if [ "$#" -gt 0 ]; then
    for spec in "$@"; do
        measure_corpus "${spec%%:*}" "${spec##*:}" "the harness that produces it"
    done
else
    measure_corpus artifacts/arraycore ArrayCore "gates/build-and-run-array-core.sh"
    measure_corpus artifacts/compressioncore-default CompressionCore "gates/build-and-run-compression-core.sh"
    measure_corpus artifacts/selfhost-fullcli dn2cpp "gates/selfhost-emit.sh"
    if [ -n "${DN2CPP_CC_EXTRA_OUT:-}" ]; then
        [ -n "${DN2CPP_CC_EXTRA_BIN:-}" ] \
            || { echo "error: DN2CPP_CC_EXTRA_OUT needs DN2CPP_CC_EXTRA_BIN" >&2; exit 1; }
        measure_corpus "$DN2CPP_CC_EXTRA_OUT" "$DN2CPP_CC_EXTRA_BIN" "your own transpile"
    fi
fi
echo "report: $REPORT"
