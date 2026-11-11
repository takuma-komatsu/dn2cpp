#!/usr/bin/env python3
"""Check native32 halves with independent, unbounded integer multiplication."""
from pathlib import Path
import sys

raw = Path(sys.argv[1]).read_bytes().replace(b"\r\n", b"\n")
if b"\0" in raw:
    raise SystemExit("BigMul wasm32 output contains NUL")
expected = [
    "primitive native width=4",
    "== primitive BigMul ==",
    "primitive fixed direct=True",
    "primitive native direct/canary/alias/evaluation=True",
    "primitive all-six address=True",
    "primitive all-six delegate=True",
    "primitive BigMul end",
    "primitive null output direct=NullReferenceException address=NullReferenceException",
]
signed = (0, 1, -1, -(1 << 31), (1 << 31) - 1, -123456789, 987654321)
unsigned = (0, 1, (1 << 32) - 1, 0x80000000, 0x7fffffff, 0x12345678, 0xfedcba98)
mask = (1 << 32) - 1

def signed32(value):
    value &= mask
    return value - (1 << 32) if value & (1 << 31) else value

for route in range(3):
    for left in signed:
        for right in signed:
            product = left * right
            expected.append(f"native32:s:{route}:{left}:{right}:{signed32(product >> 32)}:{signed32(product)}")
    for left in unsigned:
        for right in unsigned:
            product = left * right
            expected.append(f"native32:u:{route}:{left}:{right}:{(product >> 32) & mask}:{product & mask}")
    expected.append(f"native32:route{route}:canary/alias/null=True")
expected.append("primitive native32 BigMul end")
wanted = ("\n".join(expected) + "\n").encode("ascii")
if raw != wanted:
    actual_lines, wanted_lines = raw.split(b"\n"), wanted.split(b"\n")
    for index, (actual, wanted_line) in enumerate(zip(actual_lines, wanted_lines)):
        if actual != wanted_line:
            raise SystemExit(f"BigMul wasm32 record {index + 1}: {actual!r} != {wanted_line!r}")
    raise SystemExit(f"BigMul wasm32 record count differs: {len(actual_lines)} != {len(wanted_lines)}")
print("BigMul wasm32 independent arithmetic oracle OK")
