#!/usr/bin/env python3
"""Require the exact Vector4 JIT/fallback exception contrast without rewriting it."""
import pathlib
import re
import sys


def lines(path):
    return pathlib.Path(path).read_text(encoding="utf-8").splitlines()


if len(sys.argv) != 5:
    raise SystemExit("usage: check-oracles.py hw.stdout hw.stderr software.stdout software.stderr")
hw_out, hw_err, soft_out, soft_err = map(lines, sys.argv[1:])
assert hw_out == soft_out, "valid-lane or callable witnesses differ between CLR profiles"
assert len(soft_out) == 3, "unexpected stdout"
assert soft_out[0] == "-- concrete vector elements --"
assert soft_out[1] == "concrete vector six delegate and calli targets passed"
assert re.fullmatch(r"concrete vector element bit/copy/bounds/order checks=[1-9][0-9]*", soft_out[2])
message = "Specified argument was out of the range of valid values."
expected_hw = []
expected_soft = []
for width in (2, 3, 4):
    for index in (-1, -2147483648, width, 2147483647):
        for operation in ("get", "with"):
            prefix = f"v{width} {operation} {index}:"
            expected_soft.append(prefix + "index:" + message + " (Parameter 'index')")
            expected_hw.append(prefix + (":" + message if width == 4
                else "index:" + message + " (Parameter 'index')"))
assert soft_err == expected_soft, "software CLR exception diagnostics differ from the exact contract"
assert hw_err == expected_hw, "hardware CLR exception diagnostics differ from the pinned JIT contrast"
assert sum(left != right for left, right in zip(hw_err, soft_err)) == 8
print("concrete vector CLR oracles: exact eight Vector4 JIT exception contrasts")
