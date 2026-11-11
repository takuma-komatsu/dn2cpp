#!/usr/bin/env python3
"""Valid metadata controls must reach both callable and constrained adapters."""
from pathlib import Path
import re
import sys

mode, directory = sys.argv[1:]
output = Path(directory)
sources = [output / "generated.h", *sorted(output.glob("generated*.cpp"))]
text = "\n".join(path.read_text(encoding="utf-8") for path in sources)


def body(name):
    match = re.search(r"// NumericSignatures\.Program::" + name + r"\n[^\n]+\n\{(.*?)\n\}", text, re.S)
    if match is None:
        raise SystemExit("FAIL: numeric control body is missing: " + name)
    return match[1]


main = body("Main")
if mode.startswith("bigmul-"):
    owners = ("Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr") if mode == "bigmul-control" else ("Int32",)
    for owner in owners:
        symbol = owner + r"_BigMul_\w+"
        if not mode.endswith("-address") and not re.search(r"= " + symbol + r"\(", main):
            raise SystemExit("FAIL: BigMul control lost direct call: " + owner)
        if not mode.endswith("-call") and not re.search(r"\(void\*\)&" + symbol + r";", main):
            raise SystemExit("FAIL: BigMul control lost ldftn: " + owner)
        if not re.search(r"\b" + symbol + r"\([^;]*?\)\s*\{", text):
            raise SystemExit("FAIL: BigMul control lost callable adapter: " + owner)
else:
    owner = "Int128" if mode == "int128-control" else "UInt128"
    if not re.search(r"= " + owner + r"_CreateChecked_\w+\(", main):
        raise SystemExit("FAIL: checked control lost concrete factory: " + owner)
    for source in (main, body("ConstrainedControl")):
        if ".f__lower = " not in source or ".f__upper = " not in source:
            raise SystemExit("FAIL: checked control lost constrained adapter: " + owner)
print("numeric-signature-emission:" + mode)
