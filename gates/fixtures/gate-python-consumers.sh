#!/usr/bin/env bash
# Real helper paths without _common.sh's host or SDK setup.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT="$PWD"
source gates/_python.sh
eval "$(sed -n '/^gate_output() {/,/^}/p; /^gate_capture() {/,/^}/p; /^gate_capture_ok() {/,/^}/p; /^unzip_strip() {/,/^}/p; /^run_bounded() {/,/^}/p' gates/_common.sh)"
source gates/_ildiet.sh
source gates/_path-activation.sh
fixture="$1"
mkdir -p "$fixture"
gate_python - "$fixture" <<'PY'
from pathlib import Path
import sys
import zipfile
root = Path(sys.argv[1])
(root / 'utf8.txt').write_text('\u3042', encoding='utf-8')
with zipfile.ZipFile(root / 'fixture.zip', 'w') as archive:
    archive.write(root / 'utf8.txt', 'prefix/utf8.txt')
(root / 'ildiet-result.xml').write_text('<ildietResult version="2" generation="g-0123456789abcdef"/>', encoding='utf-8')
(root / 'g-0123456789abcdef').mkdir(exist_ok=True)
PY
unzip_strip "$fixture/fixture.zip" "$fixture/unpacked" 1
gate_output compare "$fixture/utf8.txt" "$fixture/unpacked/utf8.txt"
generation=$(ildiet_generation "$fixture")
gate_python -c 'from pathlib import Path; import sys; assert Path(sys.argv[1]).samefile(sys.argv[2])' \
    "$generation" "$fixture/g-0123456789abcdef"
cat > "$fixture/generated.cpp" <<'CPP'
t_System_Runtime_Remoting_ObjectHandle* Activator_CreateInstanceFrom_m1(Dn2CppString* a0, Dn2CppString* a1)
{
[[maybe_unused]] t_System_Runtime_Remoting_ObjectHandle* t0; (void)(a0); (void)(a1); dn2cpp_throw_platform_not_supported("Assembly.LoadFrom is not available in AOT-compiled code (no assembly loading)"); t0 = nullptr; return t0;
}
CPP
gate_capture_ok "$fixture/body" path_activation_body_check "$fixture"
gate_output witness "$fixture/body.stdout" 'path-activation-body:exact-refusal' --policy crlf
gate_capture "$fixture/failure" gate_python -c 'raise SystemExit(23)'
gate_output status "$(cat "$fixture/failure.status")" 23
# Background and watchdog jobs must replace their shell, so PID cleanup remains
# directed at the interpreter rather than leaving a child server behind.
(gate_python_exec -c 'import os,sys; print(os.getpid()); sys.stdout.flush()') > "$fixture/background.pid" &
pid=$!
wait "$pid"
case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) ;; # MSYS and native Windows PIDs use different namespaces.
    *) test "$(cat "$fixture/background.pid")" = "$pid" ;;
esac
run_with_watchdog() {
    shift
    "$@" &
    local child=$!
    wait "$child"
}
run_bounded gate_python_exec -c 'print("bounded launcher")' > "$fixture/bounded.stdout"
gate_output witness "$fixture/bounded.stdout" 'bounded launcher' --policy crlf
printf 'OK: gate Python consumer paths\n'
