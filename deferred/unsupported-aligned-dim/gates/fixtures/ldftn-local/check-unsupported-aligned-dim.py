#!/usr/bin/env python3
"""Validate the finite DIM runtime transcript, independently of output parity."""
import argparse
import itertools
import re
from pathlib import Path

PAIRS = [(0, "Decimal"), *itertools.product((64, 128, 256, 512), ("Char", "Int128", "UInt128"))]
ROUTES = ("direct", "address", "calli", "delegate")
MODES = ("aligned", "null", "misaligned", "aligned16-not32", "address-throw", "array-bounds", "array-null")


def read(path):
    data = Path(path).read_bytes()
    assert b"\r" not in data.replace(b"\r\n", b""), "bare carriage return"
    return data.replace(b"\r\n", b"\n")


def check(data, retained_only=False):
    lines = data.decode("utf-8").splitlines()
    assert lines[0] == "unsupported-aligned-dim:start"
    assert lines[-1] == "unsupported-aligned-dim:passed"
    match = re.fullmatch(r"unsupported-aligned-dim:profile:(16|32|64)", lines[1])
    assert match, "missing actual supported Vector<byte>.Count profile"
    numerics_alignment = int(match[1])
    expected = set()
    for width, element in PAIRS:
        for operation in ("LoadAligned", "StoreAligned"):
            for route in ("retained-address", "retained-delegate"):
                expected.add(f"unsupported-aligned-dim:{width}:{element}:{operation}:{route}")
            if not retained_only:
                modes = MODES if operation == "LoadAligned" else (*MODES[:4], "value-throw", *MODES[4:])
                for route, mode in itertools.product(ROUTES, modes):
                    expected.add(f"unsupported-aligned-dim:{width}:{element}:{operation}:{route}:{mode}")
    seen = set()
    cases = acquisitions = 0
    for line in lines[2:-1]:
        fields = line.split("|")
        key = fields[0]
        assert key in expected and key not in seen, "unknown or duplicate record: " + key
        seen.add(key)
        parts = key.split(":")
        _, width, element, operation, route, *tail = parts
        if route.startswith("retained-"):
            assert fields[1:] == ["acquired", "1", "effects=", "canary=unchanged"], line
            acquisitions += 1
            continue
        assert len(fields) == 7 and len(tail) == 1, line
        mode = tail[0]
        store = operation == "StoreAligned"
        effects = "V" if mode == "value-throw" else "VA" if store else "A"
        assert fields[4:] == ["effects=" + effects, "acquisitions=" + ("1" if route in ("address", "delegate") else "0"), "canary=unchanged"], line
        alignment = numerics_alignment if width == "0" else int(width) // 8
        if mode in ("value-throw", "address-throw"):
            assert fields[1:4] == ["System.InvalidOperationException", mode.replace("-throw", "-producer"), "-2146233079"], line
        elif mode == "array-bounds":
            assert fields[1:4] == ["System.IndexOutOfRangeException", "Index was outside the bounds of the array.", "-2146233080"], line
        elif mode == "array-null":
            assert fields[1:4] == ["System.NullReferenceException", "Object reference not set to an instance of an object.", "-2147467261"], line
        elif mode == "misaligned" or mode == "aligned16-not32" and alignment > 16:
            assert fields[1:4] == ["System.AccessViolationException", "Attempted to read or write protected memory. This is often an indication that other memory is corrupt.", "-2147467261"], line
        else:
            assert fields[1:4] == ["System.NotSupportedException", "Specified type is not supported", "-2146233067"], line
        for retained in ("retained-address", "retained-delegate"):
            assert f"unsupported-aligned-dim:{width}:{element}:{operation}:{retained}" in seen, "invoked before retained-only acquisition"
        cases += 1
    assert seen == expected, "incomplete exact operation/route/case matrix"
    assert acquisitions == 52 and cases == (0 if retained_only else 780)
    return cases, acquisitions, numerics_alignment


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output")
    parser.add_argument("--oracle")
    parser.add_argument("--prefix", help="unchanged original 540-record numeric DIM transcript")
    parser.add_argument("--retained-only", action="store_true")
    args = parser.parse_args()
    data = read(args.output)
    if args.prefix:
        prefix = read(args.prefix)
        records = prefix.decode("utf-8").splitlines()
        assert len(records) == 540 and len(set(records)) == 540
        assert all(re.fullmatch(r"vector-alignment-dim:(0|64|128|256|512):(Byte|SByte|Int16|UInt16|Int32|UInt32|Int64|UInt64|IntPtr|UIntPtr|Single|Double):(direct|address|calli):[012]:pass", line) for line in records)
        assert data.startswith(prefix), "existing numeric DIM output changed"
        data = data[len(prefix):]
    cases, acquisitions, alignment = check(data, args.retained_only)
    if args.oracle:
        oracle = read(args.oracle)
        check(oracle, args.retained_only)
        assert data == oracle, "DIM output differs from the genuine CLR oracle"
    print(f"unsupported-aligned-dim-check:cases={cases}:retained={acquisitions}:numerics-alignment={alignment}:pass")


if __name__ == "__main__":
    main()
