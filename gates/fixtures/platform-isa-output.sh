#!/usr/bin/env bash
# Real shell APIs, synthetic files: no SDK/native build required.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT=$PWD
py=${1:-python3}
resolve_python() { printf '%s\n' "$py"; }
gate_output() { "$py" gates/gate-output.py "$@"; }
source gates/_platform_isa.sh
fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
"$py" - "$fixture" <<'PY'
from pathlib import Path
import sys
root = Path(sys.argv[1])
valid = b'== contract ==\nX86.Avx=True\n== invalid X86.Avx ==\nprobe imm=4=ArgumentOutOfRangeException\n'
for name, data in {
    'lf': valid,
    'crlf': valid.replace(b'\n', b'\r\n'),
    'result-crcrlf': valid.replace(b'Exception\n', b'Exception\r\r\n'),
    'duplicate': valid + b'== invalid X86.Avx ==\nprobe=ArgumentOutOfRangeException\n',
    'missing': b'== contract ==\nX86.Avx=True\n',
    'missing-result': valid.rsplit(b'probe', 1)[0],
    'reordered': b'== invalid X86.Avx ==\nprobe=ArgumentOutOfRangeException\n== contract ==\nX86.Avx=True\n',
    'wrong-result': valid.replace(b'ArgumentOutOfRangeException', b'PlatformNotSupportedException'),
    'malformed': valid.replace(b'== invalid X86.Avx ==', b'== invalid X86.Avx == junk'),
    'nested': b'== X86.Avx ==\nprobe=PlatformNotSupportedException\n',
    'nested-duplicate': b'== X86.Avx ==\nprobe=PlatformNotSupportedException\n' * 2,
    'nested-cr': b'== X86.Avx ==\nprobe=PlatformNotSupportedException\r\r\n',
    'contract-cr': b'== contract ==\nX86.Avx=True\r\r\n',
}.items():
    root.joinpath(name + '.raw').write_bytes(data)
root.joinpath('nul.view').write_bytes(valid + b'\0')
PY
for name in lf crlf result-crcrlf duplicate missing missing-result reordered wrong-result malformed nested nested-duplicate nested-cr contract-cr; do
    gate_output view "$fixture/$name.raw" "$fixture/$name.view" --policy crlf
done
for name in lf crlf; do
    diagnostics=$(platform_isa_invalid_immediate_violations "$fixture/$name.view")
    test -z "$diagnostics"
    block=$(platform_isa_contract_block "$fixture/$name.view")
    test "$block" = 'X86.Avx=True'
done
for name in result-crcrlf duplicate missing missing-result reordered wrong-result malformed nul; do
    diagnostics=$(platform_isa_invalid_immediate_violations "$fixture/$name.view")
    test -n "$diagnostics" || { echo "FAIL: ISA parser accepted $name" >&2; exit 1; }
done
test "$(platform_isa_output adjacent-count "$fixture/nested.view" '== X86.Avx ==' 'probe=PlatformNotSupportedException')" = 1
platform_isa_output adjacent-count "$fixture/nested.view" '== X86.Avx ==' 'probe=PlatformNotSupportedException' > "$fixture/count.out"
printf '1\n' > "$fixture/count.expected"
gate_output compare "$fixture/count.out" "$fixture/count.expected"
test "$(platform_isa_output adjacent-count "$fixture/nested.view" '== X86.Missing ==' 'probe=PlatformNotSupportedException')" = 0
test "$(platform_isa_output adjacent-count "$fixture/nested-duplicate.view" '== X86.Avx ==' 'probe=PlatformNotSupportedException')" = 2
if platform_isa_output adjacent-count "$fixture/nested-cr.view" '== X86.Avx ==' 'probe=PlatformNotSupportedException'; then
    echo 'FAIL: nested witness discarded payload CR' >&2; exit 1
fi
if platform_isa_contract_block "$fixture/contract-cr.view"; then
    echo 'FAIL: contract block discarded payload CR' >&2; exit 1
fi
_platform_isa_witness_normalization_self_check
printf 'row=X86.Aes\r\nexercise=X86.Aes\r\ninvalid=X86.Aes\r\n' > "$fixture/plan.raw"
gate_output view "$fixture/plan.raw" "$fixture/plan.view" --policy crlf
for kind in row exercise invalid; do
    test "$(platform_isa_output plan-values "$fixture/plan.view" "$kind")" = X86.Aes
done
printf 'row=X86.Aes\r\r\n' > "$fixture/plan.raw"
gate_output view "$fixture/plan.raw" "$fixture/plan.view" --policy crlf
if platform_isa_output plan-values "$fixture/plan.view" row; then
    echo 'FAIL: coverage plan discarded payload CR' >&2; exit 1
fi
if platform_isa_output validate "$fixture/plan.view"; then
    echo 'FAIL: native witness validation discarded payload CR' >&2; exit 1
fi
# Native N/NM concatenate independently emitted shards. In particular the
# x86-base shard can have only contract rows, without an invalid-immediate body.
"$py" - "$fixture" <<'PYCODE'
from pathlib import Path
import sys
root = Path(sys.argv[1])
preamble = b'families=all\n== contract ==\n'
empty = preamble + b'X86.X86Base=True\n'
avx = preamble + b'X86.Avx=True\n== invalid X86.Avx ==\nprobe imm=32=ArgumentOutOfRangeException\n'
avx2 = preamble + b'X86.Avx2=False\n== invalid X86.Avx2 ==\nprobe imm=9=PlatformNotSupportedException\n'
positive = {
    'leading-empty': empty + avx,
    'multiple-empty': empty + empty + avx,
    'trailing-empty': avx + empty,
    'multiple-invalid': avx + avx2,
    # The shared parser also receives an unsharded Wasm selection.
    'wasm-selection': avx.replace(b'families=all', b'families=Wasm.PackedSimd'),
}
negative = {
    'wrong-selection': empty + avx.replace(b'families=all', b'families=bogus'),
    'empty-selection': empty + avx.replace(b'families=all', b'families='),
    'preamble-space': empty + avx.replace(b'families=all', b'families=all '),
    'orphan-preamble': avx + empty + b'families=all\n',
    'duplicate-preamble': empty + b'families=all\n' + avx,
    'nonadjacent-blank': empty + avx.replace(preamble, b'families=all\n\n== contract ==\n'),
    'nonadjacent-junk': empty + avx.replace(preamble, b'families=all\njunk\n== contract ==\n'),
    'missing-lf': empty.rstrip(b'\n') + avx,
    'missing-result': empty + avx.split(b'probe')[0] + avx2,
    'duplicate-invalid': empty + avx + avx,
    'wrong-exception': empty + avx.replace(b'ArgumentOutOfRangeException', b'PlatformNotSupportedException'),
    'no-invalid': empty + empty,
}
for kind, cases in [('good', positive), ('bad', negative)]:
    for name, data in cases.items():
        for eol in ['lf', 'crlf']:
            raw = data if eol == 'lf' else data.replace(b'\n', b'\r\n')
            root.joinpath(f'shard-{kind}-{name}-{eol}.raw').write_bytes(raw)
for name, data in {
    'preamble-crcrlf': (empty + avx).replace(b'families=all\n', b'families=all\r\r\n'),
    'result-crcrlf': (empty + avx).replace(b'Exception\n', b'Exception\r\r\n'),
}.items():
    root.joinpath(f'shard-bad-{name}.raw').write_bytes(data)
root.joinpath('shard-nul.view').write_bytes(empty + avx.replace(b'families=all', b'families=all\0'))
PYCODE
for raw in "$fixture"/shard-good-*.raw "$fixture"/shard-bad-*.raw; do
    view="${raw%.raw}.view"
    gate_output view "$raw" "$view" --policy crlf
    diagnostics=$(platform_isa_invalid_immediate_violations "$view")
    case "$raw" in
        */shard-good-*) test -z "$diagnostics" || { printf 'FAIL: %s: %s\n' "$raw" "$diagnostics" >&2; exit 1; } ;;
        */shard-bad-*) test -n "$diagnostics" || { printf 'FAIL: accepted %s\n' "$raw" >&2; exit 1; } ;;
    esac
done
test -n "$(platform_isa_invalid_immediate_violations "$fixture/shard-nul.view")"

printf '%s\n' 'OK: ISA binary file parsers preserve one-normalization boundary'
