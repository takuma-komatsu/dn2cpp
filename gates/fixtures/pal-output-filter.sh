#!/usr/bin/env bash
# Exercise the actual sink adapter without building the reference runtime.
set -euo pipefail
cd "$(dirname "$0")/../.."
py=${1:-python3}
resolve_python() { printf '%s\n' "$py"; }
eval "$(sed -n '/^pal_remove_sink_tags() {/,/^}/p' gates/build-and-run-pal-reference.sh)"
fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
"$py" - "$fixture" <<'PY'
from pathlib import Path
import sys
root = Path(sys.argv[1])
# This is a once-normalized view: remaining CR before LF is payload, not EOL.
root.joinpath('input').write_bytes(b'[O]payload\r\n[E]nul\0\n\n[E[O]][O[E]]\n[O]unterminated\r')
root.joinpath('expected').write_bytes(b'payload\r\nnul\0\n\n[E][O]\nunterminated\r')
PY
pal_remove_sink_tags "$fixture/input" "$fixture/output"
"$py" gates/gate-output.py compare "$fixture/output" "$fixture/expected"
printf '%s\n' 'OK: PAL sink tag adapter preserves binary records and payload CR'
