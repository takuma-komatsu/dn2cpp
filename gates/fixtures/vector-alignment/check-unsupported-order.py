"""Check the CLR transcript and project its address controls onto the selected CoreLib."""
from itertools import product
from pathlib import Path
import sys


def records(path):
    data = Path(path).read_bytes().replace(b"\r\n", b"\n")
    if b"\r" in data or not data.endswith(b"\n"):
        raise ValueError(f"invalid line endings in {path}")
    return data[:-1].decode("utf-8").split("\n")


layout, output, *projection = sys.argv[1:]
layouts = [row.split() for row in records(layout)]
alignment = {family: int(host) for family, host, _ in layouts}
target_alignment = {family: int(target) for family, _, target in layouts}
lines = records(output)
prefix = "unsupported aligned profile "
assert lines[0].startswith(prefix) and lines[-1] == "unsupported aligned order end"
alignment["Vector"] = int(lines[0][len(prefix):])
assert alignment["Vector"] in (16, 32, 64)
shapes = [("Vector", "decimal"), *product(("Vector64", "Vector128", "Vector256", "Vector512"), ("char", "Int128", "UInt128"))]
expected = []
for vector, element in shapes:
    for operation, route in product(("load", "store"), ("direct", "address", "calli", "delegate")):
        modes = ["aligned", "null", "misaligned", "aligned16-not32", "address-throw", "array-bounds", "array-null"]
        if operation == "store":
            modes.insert(4, "value-throw")
        if route in ("address", "delegate"):
            modes.insert(0, "retain")
        for mode in modes:
            result = {
                "retain": "acquired",
                "value-throw": "ArgumentException",
                "address-throw": "ArgumentException",
                "array-bounds": "IndexOutOfRangeException",
                "array-null": "NullReferenceException",
            }.get(mode, "NotSupportedException")
            if mode == "misaligned" or (mode == "aligned16-not32" and alignment[vector] > 16):
                result = "AccessViolationException"
            expected.append(f"{vector}<{element}> {operation} {route} {mode} {result}")

expected.append("numeric aligned memory passed")
if len(lines[1:-1]) != len(expected):
    raise SystemExit(f"{output}: incomplete transcript, expected {len(expected)} records, found {len(lines) - 2}")
for observed, wanted in zip(lines[1:-1], expected):
    if observed != wanted:
        raise SystemExit(f"{output}: expected {wanted!r}, got {observed!r}")

if projection:
    if len(projection) != 1 or alignment["Vector"] != 16:
        raise ValueError("the target projection requires the 128-bit CLR transcript")
    projected = lines.copy()
    controls = {}
    for i, line in enumerate(lines[1:-2], 1):
        shape, operation, route, mode, result = line.split()
        family = shape.split("<", 1)[0]
        if mode in ("aligned", "misaligned"):
            controls[shape, operation, route, mode] = result
        elif mode == "aligned16-not32" and family != "Vector":
            # Classify the very same address using the target getter. Only its
            # result may differ; take it from this CLR call's measured control.
            target_misaligned = target_alignment[family] > 16
            if target_misaligned != (alignment[family] > 16):
                control = "misaligned" if target_misaligned else "aligned"
                projected[i] = f"{shape} {operation} {route} {mode} {controls[shape, operation, route, control]}"
    Path(projection[0]).write_bytes(("\n".join(projected) + "\n").encode("utf-8"))
