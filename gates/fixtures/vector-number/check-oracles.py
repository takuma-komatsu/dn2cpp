#!/usr/bin/env python3
"""Check selected values using integer IEEE encodings, without floating arithmetic.

NaN results must preserve the selected operand sign and payload. The quiet bit
may be set for a signaling operand by the CLR ABI; this is recorded separately,
not treated as an interchangeable NaN. Native results preserve every selected bit.
Number operations with one NaN must select
the exact numeric operand. Every non-NaN result and preserved input is bit-exact.

The public ISimdVector contract does not choose a payload when both inputs are
NaN. Accelerated CLR may select either original NaN; that default-profile choice
is counted. The software CLR and native scalar-helper profiles remain strict to
the managed source's left-operand selection. No arbitrary NaN payload is accepted.
See dotnet/runtime v10.0.12 System/Runtime/Intrinsics/ISimdVector_2.cs and
VectorMath.cs.
"""
import argparse
import json
import itertools
from pathlib import Path

VALUES = {'f': [0, 2147483648, 1065353216, 3212836864, 1065353217, 3212836865, 1073741824, 3221225472, 1, 2147483649, 8388607, 2155872255, 8388608, 2155872256, 2139095039, 4286578687, 2139095040, 4286578688, 2143363909, 4291117857, 2143634209, 4290847557, 2139169605, 4286923553], 'd': [0, 9223372036854775808, 4607182418800017408, 13830554455654793216, 4607182418800017409, 13830554455654793217, 4611686018427387904, 13835058055282163712, 1, 9223372036854775809, 4503599627370495, 9227875636482146303, 4503599627370496, 9227875636482146304, 9218868437227405311, 18442240474082181119, 9218868437227405312, 18442240474082181120, 9221140253039434428, 18444584847926384248, 9221212811071608440, 18444512289894210236, 9218888453225749180, 18442333048112699000]}
OPERATIONS = ("MaxNumber", "MinNumber")
FAMILIES = ("64f", "64d", "128f", "128d", "256f", "256d", "512f", "512d", "nf", "nd", "c2", "c3", "c4")


GUARD_MARKER = "vector-number:managed-guards:start"
GUARD_END = "vector-number:managed-guards-passed"
GUARD_NAMES = tuple(prefix + ":" + element
                    for prefix in ("vector64", "vector128", "vector256", "vector512", "numerics")
                    for element in ("decimal", "enum", "struct", "reference-struct", "int128", "uint128"))


def managed_guards(lines):
    assert lines and lines[0] == GUARD_MARKER and lines[-1] == GUARD_END, "missing managed guard witnesses"
    expected = set(itertools.product(GUARD_NAMES, OPERATIONS, range(4), range(3)))
    expected_acquired = set(itertools.product(GUARD_NAMES, OPERATIONS, ("delegate", "calli", "retained")))
    records, acquired = {}, set()
    for line in lines[1:-1]:
        parts = line.split("|")
        fields = dict(part.split("=", 1) for part in parts[1:])
        if parts[0].startswith("guard-acquired:"):
            assert len(parts) == 4 and set(fields) == {"op", "route", "effects"}, ("invalid acquisition", line)
            key = parts[0][len("guard-acquired:"):], fields["op"], fields["route"]
            assert key in expected_acquired and key not in acquired, ("unknown/duplicate acquisition", key)
            assert fields["effects"] == "", ("acquisition evaluated operands", key)
            acquired.add(key)
            continue
        assert parts[0].startswith("guard:") and len(parts) == 7, ("invalid guard record", line)
        assert set(fields) == {"op", "route", "fail", "exception", "message", "effects"}, line
        key = parts[0][len("guard:"):], fields["op"], int(fields["route"]), int(fields["fail"])
        assert key in expected and key not in records, ("unknown/duplicate guard", key)
        name, op, route, fail = key
        if route:
            assert (name, op, ("delegate", "calli", "retained")[route - 1]) in acquired, ("invoked before acquisition witness", key)
        exception = "System.NotSupportedException" if fail == 0 else "System.InvalidOperationException"
        # The invariant-culture exact-CoreLib CLR executions establish this resource text.
        message = ("Specified type is not supported", "left-producer", "right-producer")[fail]
        assert fields["exception"] == exception and fields["message"] == message, (key, fields)
        assert fields["effects"] == ("L" if fail == 1 else "LR"), (key, fields)
        records[key] = fields
    assert set(records) == expected, ("missing guard cases", sorted(expected - set(records)))
    assert acquired == expected_acquired, ("missing callable acquisitions", sorted(expected_acquired - acquired))
    return records, acquired


def guard_bytes(path):
    lines = Path(path).read_bytes().splitlines(keepends=True)
    boundary = [i for i, line in enumerate(lines) if line.rstrip(b"\r\n") == b"vector-number:all-routes-passed"]
    assert len(boundary) == 1, "missing/duplicate supported prefix boundary"
    return b"".join(lines[boundary[0] + 1:])


def selected(op, left, right, width):
    sign = 1 << (width - 1)
    infinity = 0x7F800000 if width == 32 else 0x7FF0000000000000
    lm, rm = left & (sign - 1), right & (sign - 1)
    ln, rn = lm > infinity, rm > infinity
    if ln or rn:
        return left if rn else right
    if lm == rm == 0:
        # Signed zero is numerically tied; prefer - for minimum and + for maximum.
        return (left | right) if op == "MinNumber" else (left & right)
    # Transform IEEE encodings into monotonically ordered unsigned integer keys.
    # Negative encodings reverse magnitude order; positives follow negatives.
    full = (1 << width) - 1
    lk = (~left & full) if left & sign else (left | sign)
    rk = (~right & full) if right & sign else (right | sign)
    choose_left = lk > rk if op == "MaxNumber" else lk < rk
    return left if choose_left else right


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output")
    parser.add_argument("--profile", choices=("default", "software", "native"), required=True)
    parser.add_argument("--guard-oracle", help="require byte-exact appended guard output against this CLR run")
    args = parser.parse_args()
    widths, counts, orders = {}, {}, set()
    seen = set()
    payload_differences = {"direct": 0, "delegate": 0, "calli": 0}
    quiet_only = {"direct": 0, "delegate": 0, "calli": 0}
    both_nan_right = {"direct": 0, "delegate": 0, "calli": 0}
    nan_results = non_nan_results = records = 0
    finished = False
    guard_lines = []
    keys = {op + family: (op, family) for op in OPERATIONS for family in FAMILIES}
    with Path(args.output).open() as stream:
        for number, raw in enumerate(stream, 1):
            line = raw.rstrip("\n")
            if finished:
                guard_lines.append(line)
                continue
            if line == "vector-number:all-routes-passed":
                finished = True
                continue
            fields = line.split(":")
            if fields[0] == "width":
                assert len(fields) == 3 and fields[1] in keys and fields[1] not in widths, (number, line)
                key, lanes = fields[1], int(fields[2])
                _, family = keys[key]
                if family.startswith("c"):
                    expected = int(family[1:])
                elif family.startswith("n"):
                    valid = (4, 8, 16) if family.endswith("f") else (2, 4, 8)
                    assert lanes in valid, (number, line)
                    expected = (4 if family.endswith("f") else 2) if args.profile != "default" else lanes
                else:
                    expected = int(family[:-1]) // (32 if family.endswith("f") else 64)
                assert lanes == expected, (number, line, expected)
                widths[key] = lanes
                counts[key] = 0
                continue
            if fields[0] == "order":
                assert len(fields) == 3 and fields[1] in keys and fields[2] == "direct-delegate-calli", (number, line)
                assert fields[1] not in orders
                orders.add(fields[1])
                continue
            assert len(fields) == 10 and fields[0] in widths, (number, line)
            key, test, lane = fields[0], int(fields[1]), int(fields[2])
            op, family = keys[key]
            width = 64 if family.endswith("d") else 32
            vals = VALUES["d" if width == 64 else "f"]
            assert 0 <= test < len(vals) ** 2 and 0 <= lane < widths[key], (number, line)
            identity = key, test, lane
            assert identity not in seen, (number, "duplicate lane")
            seen.add(identity)
            assert all(len(x) == width // 4 for x in fields[3:]), (number, "wrong encoding width")
            left, right, actual, delegated, indirect, after_left, after_right = (int(x, 16) for x in fields[3:])
            pair = (test + lane) % (len(vals) ** 2)
            assert (left, right) == (vals[pair // len(vals)], vals[pair % len(vals)]), (number, "wrong corpus inputs")
            assert (after_left, after_right) == (left, right), (number, "operand mutated")
            expected = selected(op, left, right, width)
            mask = (1 << (width - 1)) - 1
            infinity = 0x7F800000 if width == 32 else 0x7FF0000000000000
            quiet = 1 << (22 if width == 32 else 51)
            for route, value in (("direct", actual), ("delegate", delegated), ("calli", indirect)):
                if expected & mask > infinity:
                    assert value & mask > infinity, (number, route, "NaN required", line)
                    if args.profile == "native":
                        assert value == expected, (number, route, "native selected NaN bits changed", line)
                    selected_nan = expected
                    if (args.profile == "default" and left & mask > infinity and right & mask > infinity
                            and value | quiet != expected | quiet):
                        assert value | quiet == right | quiet, (number, route, "neither NaN operand", line)
                        selected_nan = right
                        both_nan_right[route] += 1
                    assert value | quiet == selected_nan | quiet, (number, route, "wrong NaN operand", hex(expected), line)
                    assert not selected_nan & quiet or value & quiet, (number, route, "quiet NaN became signaling")
                    nan_results += 1
                    if value != expected:
                        payload_differences[route] += 1
                        if value | quiet == expected | quiet:
                            quiet_only[route] += 1
                else:
                    assert value == expected, (number, route, hex(expected), line)
                    non_nan_results += 1
            counts[key] += 1
            records += 1
    assert finished and set(widths) == set(keys) and orders == set(keys), "missing route/completion witness"
    assert all(counts[k] == widths[k] * len(VALUES["f"]) ** 2 for k in keys), "missing lane records"
    guards, acquired = managed_guards(guard_lines)
    if args.guard_oracle:
        oracle = guard_bytes(args.guard_oracle)
        managed_guards(oracle.decode("utf-8").splitlines())
        assert guard_bytes(args.output) == oracle, "managed guard bytes differ from CLR oracle"
    print(json.dumps({"managed_guard_records": len(guards), "callable_acquisitions": len(acquired),
                      "managed_guard_byte_parity": args.guard_oracle is not None, "profile": args.profile, "lane_records": records, "routes": len(keys),
                      "non_nan_results": non_nan_results, "nan_results": nan_results,
                      "selected_nan_bit_differences": payload_differences,
                      "quiet_bit_only_differences": quiet_only,
                      "default_both_nan_right_selections": both_nan_right,
                      "non_nan_selected_bits_exact": True, "input_bits_preserved": True}, sort_keys=True))


if __name__ == "__main__":
    main()
