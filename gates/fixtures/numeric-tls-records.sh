#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
py=${1:-python3}
resolve_python() { printf '%s\n' "$py"; }
gate_output() { "$py" gates/gate-output.py "$@"; }
eval "$(sed -n '/^suppress_stat_number() {/,/^}/p' gates/measure-suppress-scan.sh)"
eval "$(sed -n '/^http_tls_served_record() {/,/^}/p' gates/build-and-run-http-get.sh)"
fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
"$py" - "$fixture" <<'PY'
from pathlib import Path
import sys
root = Path(sys.argv[1])
for name, data in {
    'stats': b'slots walked:  42 \n set entries added: 7\n',
    'stats-cr': b'slots walked: 42\r\nset entries added: 7\r\n',
    'stats-duplicate': b'slots walked: 42\nslots walked: 43\n',
    'stats-missing': b'none\n',
    'tls-lf': b'served path=/hello.txt tls=TLSv1.3 cipher=TLS_AES_256_GCM_SHA384\n',
    'tls-crlf': b'served path=/hello.txt tls=TLSv1.3 cipher=TLS_AES_256_GCM_SHA384\r\n',
    'tls-crcrlf': b'served path=/hello.txt tls=TLSv1.3 cipher=TLS_AES_256_GCM_SHA384\r\r\n',
    'tls-duplicate': b'served path=/hello.txt tls=TLSv1.3 cipher=TLS_AES_256_GCM_SHA384\n' * 2,
}.items():
    root.joinpath(name).write_bytes(data)
PY
test "$(suppress_stat_number "$fixture/stats" walked)" = 42
test "$(suppress_stat_number "$fixture/stats" added)" = 7
for name in stats-cr stats-duplicate stats-missing; do
    if suppress_stat_number "$fixture/$name" walked; then
        echo "FAIL: numeric adapter accepted $name" >&2; exit 1
    fi
done
if suppress_stat_number "$fixture/stats-cr" added; then
    echo 'FAIL: numeric adapter accepted payload CR on bound' >&2; exit 1
fi
http_tls_served_record "$fixture/tls-lf"
http_tls_served_record "$fixture/tls-crlf"
for name in tls-crcrlf tls-duplicate; do
    if http_tls_served_record "$fixture/$name"; then
        echo "FAIL: TLS adapter accepted $name" >&2; exit 1
    fi
done
printf '%s\n' 'OK: numeric and TLS record adapters preserve payload CR and counts'
