#!/usr/bin/env bash
# Transpile parallelism measurement aid. This is NOT a regression gate: wall
# time and peak RSS depend on the machine and its load, so this script prints
# observations and never enforces a speedup. The `measure-*` name keeps it out
# of run-all-gates.sh's `build-and-run-*` discovery glob.
#
# The full managed CLI transpiles itself at jobs=1,2,4 and the automatic default.
# DN2CPP_TIME supplies planning/body durations and worker/retry counts; the host
# time utility supplies end-to-end wall time and peak RSS. Every repetition gets
# a fresh output directory so stale chunks and filesystem reuse cannot change
# the workload.
#
#   ./gates/measure-transpile-parallel.sh
#   DN2CPP_PARALLEL_REPS=5 ./gates/measure-transpile-parallel.sh
#   DN2CPP_PARALLEL_OUT=artifacts/my-run ./gates/measure-transpile-parallel.sh
source "$(dirname "$0")/_common.sh"

OUT_ROOT="${DN2CPP_PARALLEL_OUT:-artifacts/transpile-parallel}"
REPS="${DN2CPP_PARALLEL_REPS:-3}"
RESULTS="$OUT_ROOT/runs.tsv"

case "$OUT_ROOT" in
    ''|/|.|..) echo "error: refusing unsafe DN2CPP_PARALLEL_OUT '$OUT_ROOT'" >&2; exit 1 ;;
esac
case "$REPS" in
    ''|*[!0-9]*|0) echo "error: DN2CPP_PARALLEL_REPS must be a positive integer" >&2; exit 1 ;;
esac

echo "== 1/3 Building the full CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
BIN="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM"
CLI="$BIN/dn2cpp.dll"
[ -f "$CLI" ] || { echo "error: CLI not built: $CLI" >&2; exit 1; }

echo "== 2/3 Locating the net10 CoreLib and self-host references =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
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

mkdir -p "$OUT_ROOT"
printf 'jobs\trep\ttotal_ms\tplanning_ms\tcompile_bodies_ms\tpeak_rss_bytes\trequested\teffective\tpeak_workers\tretries\n' \
    > "$RESULTS"

TIME_BIN=/usr/bin/time
case "$(uname -s)" in
    Darwin) TIME_FLAG=-l ;;
    *) TIME_FLAG=-v ;;
esac

elapsed_ms() {
    case "$(uname -s)" in
        Darwin)
            awk '$2 == "real" { printf "%.0f\n", $1 * 1000; exit }' "$1"
            ;;
        *)
            awk '/Elapsed \(wall clock\)/ {
                fields=split($0, f, /[[:space:]]+/); value=f[fields]; n=split(value, p, ":");
                if (n == 3) seconds=p[1] * 3600 + p[2] * 60 + p[3];
                else if (n == 2) seconds=p[1] * 60 + p[2];
                else seconds=p[1];
                printf "%.0f\n", seconds * 1000; exit
            }' "$1"
            ;;
    esac
}

rss_bytes() {
    case "$(uname -s)" in
        Darwin) awk '/maximum resident set size/ { print $1; exit }' "$1" ;;
        *) awk -F: '/Maximum resident set size/ { gsub(/ /, "", $2); print $2 * 1024; exit }' "$1" ;;
    esac
}

phase_ms() {
    local phase="$1" log="$2"
    awk -v phase="$phase" '$1 == "dn2cpp-time:" && $2 == phase { print $3; exit }' "$log"
}

echo "== 3/3 Measuring jobs=1,2,4,auto ($REPS repetitions each) =="
for jobs in 1 2 4 auto; do
    for rep in $(seq 1 "$REPS"); do
        out="$OUT_ROOT/jobs-$jobs-rep-$rep"
        log="$OUT_ROOT/jobs-$jobs-rep-$rep.log"
        rm -rf "$out"
        mkdir -p "$out"

        set +e
        if [ "$jobs" = auto ]; then
            DN2CPP_TIME=1 "$TIME_BIN" "$TIME_FLAG" dotnet exec "$CLI" "$CLI" \
                "${refs[@]}" --auto-ref -o "$out" > /dev/null 2> "$log"
        else
            DN2CPP_TIME=1 "$TIME_BIN" "$TIME_FLAG" dotnet exec "$CLI" "$CLI" \
                "${refs[@]}" --auto-ref --jobs "$jobs" -o "$out" \
                > /dev/null 2> "$log"
        fi
        rc=$?
        set -e
        if [ "$rc" -ne 0 ]; then
            echo "error: jobs=$jobs repetition $rep exited $rc; see $log" >&2
            exit "$rc"
        fi

        total=$(elapsed_ms "$log")
        planning=$(phase_ms plan-bodies "$log")
        bodies=$(phase_ms compile-bodies "$log")
        rss=$(rss_bytes "$log")
        worker_line=$(sed -n 's/^dn2cpp-time: parallel-bodies requested \([0-9][0-9]*\) effective \([0-9][0-9]*\) peak \([0-9][0-9]*\) retries \([0-9][0-9]*\).*$/\1\t\2\t\3\t\4/p' "$log")
        worker_line=${worker_line:-$'?\t?\t?\t?'}
        printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
            "$jobs" "$rep" "${total:-?}" "${planning:-?}" "${bodies:-?}" \
            "${rss:-?}" "$worker_line" | tee -a "$RESULTS"
    done
done

echo
echo "medians (milliseconds except RSS):"
printf '  %-6s %10s %10s %10s %14s\n' jobs total planning bodies peak_rss_bytes
for jobs in 1 2 4 auto; do
    awk -F'\t' -v wanted="$jobs" '$1 == wanted { print $3, $4, $5, $6 }' "$RESULTS" \
        | awk -v label="$jobs" '{ total[NR]=$1; planning[NR]=$2; bodies[NR]=$3; rss[NR]=$4 }
            END {
                lo=int((NR + 1) / 2); hi=int((NR + 2) / 2);
                for (i=1; i<=NR; i++)
                    for (j=i+1; j<=NR; j++) {
                        if (total[i] > total[j]) { t=total[i]; total[i]=total[j]; total[j]=t }
                        if (planning[i] > planning[j]) { t=planning[i]; planning[i]=planning[j]; planning[j]=t }
                        if (bodies[i] > bodies[j]) { t=bodies[i]; bodies[i]=bodies[j]; bodies[j]=t }
                        if (rss[i] > rss[j]) { t=rss[i]; rss[i]=rss[j]; rss[j]=t }
                    }
                printf "  %-6s %10.0f %10.0f %10.0f %14.0f\n", label,
                    (total[lo] + total[hi]) / 2,
                    (planning[lo] + planning[hi]) / 2,
                    (bodies[lo] + bodies[hi]) / 2,
                    (rss[lo] + rss[hi]) / 2
            }'
done
echo "per-run data: $RESULTS"
