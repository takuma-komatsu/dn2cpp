#!/usr/bin/env python3
"""Check selected values using integer IEEE encodings, without floating arithmetic.

NaN results must preserve the selected operand sign and payload. The quiet bit
may be set for a signaling operand by the tested ABI; this is recorded separately,
not treated as an interchangeable NaN. Number operations with one NaN must select
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
from pathlib import Path

VALUES = {'f': [0, 2147483648, 1065353216, 3212836864, 1065353217, 3212836865, 1073741824, 3221225472, 1, 2147483649, 8388607, 2155872255, 8388608, 2155872256, 2139095039, 4286578687, 2139095040, 4286578688, 2143363909, 4291117857, 2143634209, 4290847557, 2139169605, 4286923553], 'd': [0, 9223372036854775808, 4607182418800017408, 13830554455654793216, 4607182418800017409, 13830554455654793217, 4611686018427387904, 13835058055282163712, 1, 9223372036854775809, 4503599627370495, 9227875636482146303, 4503599627370496, 9227875636482146304, 9218868437227405311, 18442240474082181119, 9218868437227405312, 18442240474082181120, 9221140253039434428, 18444584847926384248, 9221212811071608440, 18444512289894210236, 9218888453225749180, 18442333048112699000]}
OPERATIONS = ("MaxMagnitude", "MinMagnitude", "MaxMagnitudeNumber", "MinMagnitudeNumber")
FAMILIES = ("64f", "64d", "128f", "128d", "256f", "256d", "512f", "512d", "nf", "nd", "c2", "c3", "c4")


def selected(op, left, right, width):
    sign = 1 << (width - 1)
    infinity = 0x7F800000 if width == 32 else 0x7FF0000000000000
    lm, rm = left & (sign - 1), right & (sign - 1)
    ln, rn = lm > infinity, rm > infinity
    if ln or rn:
        if op.endswith("Number"):
            return left if rn else right
        return left if ln else right
    if lm == rm:
        # On a magnitude tie, maximum prefers + and minimum prefers -.
        want_negative = op.startswith("Min")
        return left if bool(left & sign) == want_negative else right
    choose_left = lm > rm if op.startswith("Max") else lm < rm
    return left if choose_left else right


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output")
    parser.add_argument("--profile", choices=("default", "software", "native"), required=True)
    args = parser.parse_args()
    widths, counts, orders = {}, {}, set()
    seen = set()
    payload_differences = {"direct": 0, "delegate": 0, "calli": 0}
    quiet_only = {"direct": 0, "delegate": 0, "calli": 0}
    both_nan_right = {"direct": 0, "delegate": 0, "calli": 0}
    nan_results = non_nan_results = records = 0
    finished = False
    keys = {op + family: (op, family) for op in OPERATIONS for family in FAMILIES}
    with Path(args.output).open() as stream:
        for number, raw in enumerate(stream, 1):
            line = raw.rstrip("\n")
            assert not finished, (number, "output after completion")
            if line == "vector-magnitude:all-routes-passed":
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
    print(json.dumps({"profile": args.profile, "lane_records": records, "routes": len(keys),
                      "non_nan_results": non_nan_results, "nan_results": nan_results,
                      "selected_nan_bit_differences": payload_differences,
                      "quiet_bit_only_differences": quiet_only,
                      "default_both_nan_right_selections": both_nan_right,
                      "non_nan_selected_bits_exact": True, "input_bits_preserved": True}, sort_keys=True))


if __name__ == "__main__":
    main()
