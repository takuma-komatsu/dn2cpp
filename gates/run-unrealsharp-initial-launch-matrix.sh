#!/usr/bin/env bash
# Repeated first launches of signed native UnrealSharp packages.
set -euo pipefail

repo=$(cd "$(dirname "$0")/.." && pwd -P)
case "$(uname -s):$(uname -m)" in
    Darwin:arm64) ;;
    *) echo 'error: initial-launch matrix requires macOS arm64' >&2; exit 1 ;;
esac

: "${UNREALSHARP_DEVELOPMENT_APP:?Set UNREALSHARP_DEVELOPMENT_APP to the native Development app}"
: "${UNREALSHARP_SHIPPING_APP:?Set UNREALSHARP_SHIPPING_APP to the native Shipping app}"

resolve_app() {
    local app=$1
    [ -f "$app/Contents/Info.plist" ] || { echo "error: no Mac app at $app" >&2; return 1; }
    (cd "$app" && pwd -P)
}

collect_crash_reports() {
    local evidence=$1 executable=$2 marker=$3
    local reports="$HOME/Library/Logs/DiagnosticReports"
    [ -d "$reports" ] || return 0
    mkdir -p "$evidence/crash-reports"
    for ((poll = 0; poll <= 30; poll++)); do
        find "$reports" -maxdepth 1 -type f -name "$executable*.ips" \
            -newer "$marker" -print > "$evidence/crash-report-paths.txt"
        if [ -s "$evidence/crash-report-paths.txt" ]; then
            find "$reports" -maxdepth 1 -type f -name "$executable*.ips" \
                -newer "$marker" -exec cp -p '{}' "$evidence/crash-reports/" \;
            return 0
        fi
        [ "$poll" -eq 30 ] || sleep 1
    done
}

development=$(resolve_app "$UNREALSHARP_DEVELOPMENT_APP")
shipping=$(resolve_app "$UNREALSHARP_SHIPPING_APP")
root="${UNREALSHARP_STARTUP_RESULT_DIR:-$repo/artifacts/unrealsharp-initial-launch-matrix}"
mkdir -p "$root"
root=$(cd "$root" && pwd -P)
session=$(mktemp -d "$root/matrix.XXXXXXXX")
session_name=${session##*/}
summary="$session/summary.tsv"
printf 'configuration\tplacement\trun\tcopy_exit\tgate_exit\tevidence\n' > "$summary"
printf 'Initial-launch matrix: %s\n' "$session"

for configuration in Development Shipping; do
    case "$configuration" in
        Development) source_app=$development ;;
        Shipping) source_app=$shipping ;;
    esac
    executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$source_app/Contents/Info.plist")
    normal_root="$(dirname "$source_app")/dn2cpp-initial-launch/$session_name"
    for placement in normal relocated; do
        for ((run = 1; run <= 20; run++)); do
            run_name=$(printf 'run-%02d' "$run")
            evidence="$session/$configuration/$placement/$run_name"
            if [ "$placement" = normal ]; then
                copy_parent="$normal_root/$run_name"
            else
                copy_parent="$session/relocated path/$configuration/$run_name"
            fi
            copy="$copy_parent/$(basename "$source_app")"
            mkdir -p "$evidence" "$copy_parent"
            echo "RUN: $configuration $placement $run_name"
            printf '%s\n' "$source_app" > "$evidence/source-app.txt"
            printf '%s\n' "$copy" > "$evidence/copied-app.txt"
            copy_exit=0
            cp -cR "$source_app" "$copy" > "$evidence/copy.log" 2>&1 || copy_exit=$?
            printf '%s\n' "$copy_exit" > "$evidence/copy-exit.txt"
            if [ "$copy_exit" -ne 0 ]; then
                printf '%s\t%s\t%s\t%s\t-\t%s\n' \
                    "$configuration" "$placement" "$run_name" "$copy_exit" "$evidence" >> "$summary"
                echo "FAIL: $configuration $placement $run_name copy exited $copy_exit; inspect $evidence" >&2
                exit 1
            fi

            gate_exit=0
            date -u '+%Y-%m-%dT%H:%M:%SZ' > "$evidence/launch-start-utc.txt"
            touch "$evidence/launch-start.marker"
            UNREALSHARP_APP="$copy" \
                UNREALSHARP_BACKEND=Dn2Cpp \
                UNREALSHARP_RESULT_DIR="$evidence" \
                bash "$repo/gates/run-unrealsharp-package-smoke.sh" \
                > "$evidence/gate.log" 2>&1 || gate_exit=$?
            printf '%s\n' "$gate_exit" > "$evidence/gate-exit.txt"
            printf '%s\t%s\t%s\t%s\t%s\t%s\n' \
                "$configuration" "$placement" "$run_name" "$copy_exit" "$gate_exit" "$evidence" >> "$summary"
            if [ "$gate_exit" -ne 0 ]; then
                collect_crash_reports "$evidence" "$executable" "$evidence/launch-start.marker" \
                    || echo "warning: could not collect crash reports for $evidence" >&2
                echo "FAIL: $configuration $placement $run_name gate exited $gate_exit; inspect $evidence" >&2
                exit 1
            fi
            echo "PASS: $configuration $placement $run_name"
        done
    done
done

echo "PASS: all 80 initial launches; evidence: $session"
