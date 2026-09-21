#!/usr/bin/env bash
# Cooked UnrealSharp gameplay, runtime-image and module-shutdown proof.
source "$(dirname "$0")/_common.sh"

: "${UNREALSHARP_APP:?Set UNREALSHARP_APP to the cooked Baseline.app}"
: "${UNREALSHARP_BACKEND:?Set UNREALSHARP_BACKEND to Clr or Dn2Cpp}"
case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp package smoke requires macOS arm64" ;; esac
app=$(cd "$UNREALSHARP_APP" && pwd -P)
work="${UNREALSHARP_RESULT_DIR:-$PWD/artifacts/unrealsharp-package-smoke/$UNREALSHARP_BACKEND}"
mkdir -p "$work"
work=$(cd "$work" && pwd -P)
# Keep LLM enabled: UE's startup clear can race an AppKit allocation scope.
args=(-LLM -Unattended -NullRHI -NoSound -Dn2CppSmoke)
case "$UNREALSHARP_BACKEND" in
    Clr) clr=present ;;
    Dn2Cpp)
        clr=absent
        args+=(-Dn2CppSmokeExpectNative)
        python3 - "$app" <<'PY'
import pathlib
import sys
for path in pathlib.Path(sys.argv[1]).rglob('*'):
    name = path.name.lower()
    if path.is_file() and (name.endswith('.dll') or any(part in name for part in ('hostfxr', 'coreclr', 'hostpolicy', 'clrjit'))):
        raise SystemExit(f'CLR execution artifact in native app: {path}')
PY
        ;;
    *) echo "error: UNREALSHARP_BACKEND must be Clr or Dn2Cpp" >&2; exit 1 ;;
esac
codesign --verify --deep --strict "$app"
executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Contents/Info.plist")
runtime_work=$(python3 - "$app" "$work" <<'PYCODE'
import os
from pathlib import Path
import plistlib
import subprocess
import sys
import tempfile
app, evidence = map(Path, sys.argv[1:])
raw = subprocess.check_output(['codesign', '-d', '--entitlements', ':-', str(app)], stderr=subprocess.DEVNULL)
entitlements = plistlib.loads(raw) if raw.strip() else {}
if entitlements.get('com.apple.security.app-sandbox', False):
    with (app / 'Contents/Info.plist').open('rb') as file:
        identifier = plistlib.load(file)['CFBundleIdentifier']
    if not identifier or '/' in identifier or identifier in ('.', '..'):
        raise SystemExit('Invalid application bundle identifier')
    directory = Path.home() / 'Library/Containers' / identifier / 'Data/tmp'
    directory.mkdir(parents=True, exist_ok=True)
    print(tempfile.mkdtemp(prefix='dn2cpp-smoke-', dir=directory))
else:
    print(evidence)
PYCODE
)
pending_evidence="${DN2CPP_SMOKE_PENDING_FILE:-}"
late_evidence="${DN2CPP_SMOKE_LATE_CALLBACK_FILE:-}"
copy_runtime_evidence() {
    if [ "$runtime_work" != "$work" ]; then
        for name in result.txt lifecycle.txt run.log; do
            [ ! -f "$runtime_work/$name" ] || cp "$runtime_work/$name" "$work/$name"
        done
        if [ -n "$pending_evidence" ] && [ -f "$runtime_work/pending.txt" ]; then
            mkdir -p "$(dirname "$pending_evidence")"
            cp "$runtime_work/pending.txt" "$pending_evidence"
        fi
        if [ -n "$late_evidence" ] && [ -f "$runtime_work/late-callback.txt" ]; then
            mkdir -p "$(dirname "$late_evidence")"
            cp "$runtime_work/late-callback.txt" "$late_evidence"
        fi
    fi
}
gate_add_exit_hook copy_runtime_evidence
rm -f "$work/result.txt" "$work/lifecycle.txt" "$work/run.log"
printf '%s\n' "$runtime_work" > "$work/runtime-directory.txt"
export DN2CPP_SMOKE_LIFECYCLE_FILE="$runtime_work/lifecycle.txt"
if [ "$runtime_work" != "$work" ]; then
    [ -z "$pending_evidence" ] || export DN2CPP_SMOKE_PENDING_FILE="$runtime_work/pending.txt"
    [ -z "$late_evidence" ] || export DN2CPP_SMOKE_LATE_CALLBACK_FILE="$runtime_work/late-callback.txt"
fi
DN2CPP_RUN_WATCHDOG_SECS=120 run_bounded "$app/Contents/MacOS/$executable" \
    "${args[@]}" \
    "-Dn2CppSmokeResult=$runtime_work/result.txt" "-abslog=$runtime_work/run.log" > "$work/stdout.log" 2>&1
copy_runtime_evidence
if grep -E 'Native shutdown failed|Native tick failed|UnrealSharp callback failure' "$work/stdout.log" "$work/run.log" 2>/dev/null; then
    echo "error: native shutdown or callback boundary failure" >&2
    exit 1
fi
expected=$'engine-smoke=OK\ncounter=45\nblueprint-override=OK\nasync=1023\nassemblies=42\nlifetime=255\ndestroyed=7\nfeatures=511\nref-out=42:True\nabi-shapes=7\nclr='
assert_output "$(cat "$work/result.txt")" "$expected$clr"
if [ "$UNREALSHARP_BACKEND" = Dn2Cpp ]; then
    assert_output "$(cat "$work/lifecycle.txt")" $'assembly-dependency-start=17\nassembly-main-start=17\nassembly-main-stop=17\nassembly-dependency-stop=17'
else
    assert_output "$(cat "$work/lifecycle.txt")" $'assembly-dependency-start=17\nassembly-main-start=17'
fi
echo "unrealsharp cooked $UNREALSHARP_BACKEND smoke OK"
