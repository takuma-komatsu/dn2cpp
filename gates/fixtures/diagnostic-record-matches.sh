#!/usr/bin/env bash
# End-anchored diagnostics must not discard a payload CR on Windows.
set -euo pipefail
cd "$(dirname "$0")/../.."
py=${1:-python3}
gate_output() { "$py" gates/gate-output.py "$@"; }
eval "$(sed -n '/^array_rank_diagnostic() {/,/^}/p' gates/build-and-run-array-core.sh)"
eval "$(sed -n '/^signature_abstract_refusal() {/,/^}/p' gates/build-and-run-reflect-types.sh)"
fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
"$py" - "$fixture" <<'PY'
from pathlib import Path
import sys
root = Path(sys.argv[1])
rows = {
    'rank': b"int lengths=System.TypeLoadException/80131522:'System.Int32' from assembly 'System.Private.CoreLib' has too many dimensions.",
    'abstract': b'sigabstract Int32,String Mixed PlatformNotSupportedException|80131539|method: this closed generic instantiation has no compiled code; fix: name the closed generic instantiation statically',
    'shadow': b'   at DeepChainSubset.Program.Depth5()',
}
for label, row in rows.items():
    for suffix, ending in [('lf', b'\n'), ('crlf', b'\r\n'), ('payload-cr', b'\r\r\n')]:
        root.joinpath(label + '.' + suffix).write_bytes(row + ending)
PY
for ending in lf crlf; do
    array_rank_diagnostic "$fixture/rank.$ending" 'int lengths'
    signature_abstract_refusal "$fixture/abstract.$ending" 'Int32,String'
    gate_output witness "$fixture/shadow.$ending" '   at DeepChainSubset.Program.Depth5()' --count any --policy crlf
done
if array_rank_diagnostic "$fixture/rank.payload-cr" 'int lengths'; then
    echo 'FAIL: array rank accepted payload CR' >&2; exit 1
fi
if signature_abstract_refusal "$fixture/abstract.payload-cr" 'Int32,String'; then
    echo 'FAIL: abstract refusal accepted payload CR' >&2; exit 1
fi
if gate_output witness "$fixture/shadow.payload-cr" '   at DeepChainSubset.Program.Depth5()' --count any --policy crlf; then
    echo 'FAIL: deepest shadow frame accepted payload CR' >&2; exit 1
fi
printf '%s\n' 'OK: actual diagnostic adapters retain payload CR'
