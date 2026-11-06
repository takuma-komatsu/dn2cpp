#!/usr/bin/env python3
"""Verify the public unsupported-type oracle and exact native parity."""
import argparse
import itertools
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("oracle", type=Path)
parser.add_argument("--candidate", type=Path)
args = parser.parse_args()
expected = set(itertools.product(
    tuple(prefix + name for prefix in ("", "vector64:", "vector128:", "vector256:", "vector512:")
          for name in ("decimal", "int128", "uint128", "enum", "struct", "reference-struct")),
    range(4), range(3), range(3)))
expected.update(itertools.product(
    ("number:" + name for name in ("decimal", "int128", "uint128", "enum", "struct", "reference-struct")),
    range(2), range(3), range(3)))

def read(path):
    lines = path.read_text().splitlines()
    assert lines.pop() == "vector Number unsupported public routes passed", "missing final witness"
    assert lines[1081] == "fixed vector magnitude unsupported public routes passed", "missing fixed prefix witness"
    del lines[1081]
    assert lines[216] == "vector magnitude unsupported public routes passed", "missing Numerics prefix witness"
    del lines[216]
    results = {}
    for line in lines:
        parts = line.split("|")
        assert len(parts) == 7, ("unexpected record", line)
        fields = dict(part.split("=", 1) for part in parts[1:])
        assert set(fields) == {"op", "route", "fail", "exception", "message", "effects"}
        key = (parts[0], int(fields["op"]), int(fields["route"]), int(fields["fail"]))
        assert key in expected and key not in results, ("unknown or duplicate case", key)
        fail = key[3]
        exception = "System.NotSupportedException" if fail == 0 else "System.InvalidOperationException"
        # This resource text is established by the invariant-culture pinned CLR oracle.
        message = ("Specified type is not supported", "left-producer", "right-producer")[fail]
        assert fields["exception"] == exception, (key, fields)
        assert fields["message"] == message, (key, fields)
        assert fields["effects"] == ("L" if fail == 1 else "LR"), (key, fields)
        results[key] = fields
    assert set(results) == expected, ("missing cases", sorted(expected - set(results)))
    return results

oracle = read(args.oracle)
if args.candidate:
    assert read(args.candidate) == oracle, "native/software differs from public CLR oracle"
print(json.dumps({"cases": len(oracle), "candidateCompared": args.candidate is not None}, sort_keys=True))
