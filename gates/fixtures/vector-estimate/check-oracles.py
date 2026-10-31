#!/usr/bin/env python3
"""Validate the bounded estimate corpus against explicit IEEE rounding profiles.

NaN payload/sign selection is recorded but is not a cross-runtime contract.
Every target's non-NaN default results must match one complete rounding profile;
no arbitrary per-case selection is accepted. No-AVX2/FMA is an independently
verified input, not an assumption made from a process environment variable.
"""
import argparse
from pathlib import Path
import re

TARGETS = tuple(width + kind for width in ("64", "128", "256", "512", "N")
                for kind in ("f", "d"))
HEADER = "-- vector multiply add estimate --"
WITNESS = "vector estimate ten direct delegate calli lane and order targets passed"
NAN_CASES = frozenset((9, 10, 12, 13, 14, 15))
# Two round-to-nearest, ties-to-even operations, with gradual underflow.
# Values are explicit expected bits, not copied or learned from the oracle.
UNFUSED = {
    "f": (0x41200000, 0, 0x7F800000, 1, 0x00400000,
          0x80000000, 0, 0x80000000, 0, None, None, 0xFF800000,
          None, None, None, None, 0x3F800000, 0x3F800002,
          0x007FFFFF, 0x80400001),
    "d": (0x4024000000000000, 0, 0x7FF0000000000000, 1,
          0x0008000000000000, 0x8000000000000000, 0,
          0x8000000000000000, 0, None, None, 0xFFF0000000000000,
          None, None, None, None, 0x3FF0000000000000,
          0x3FF0000000000002, 0x000FFFFFFFFFFFFF, 0x8008000000000001),
}
# Single final rounding differs at cancellation, overflow rescue, underflow
# rescue and the normal/subnormal boundary.
FUSED = {kind: tuple({
    "f": {1: 0xA8800000, 2: 0x7F7FFFFF, 3: 2, 18: 0x007FFFFE},
    "d": {1: 0xB970000000000000, 2: 0x7FEFFFFFFFFFFFFF,
          3: 2, 18: 0x000FFFFFFFFFFFFE},
}[kind].get(index, bits) for index, bits in enumerate(values))
         for kind, values in UNFUSED.items()}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def is_nan(bits, kind):
    exponent, fraction = ((0x7F800000, 0x007FFFFF) if kind == "f"
                          else (0x7FF0000000000000, 0x000FFFFFFFFFFFFF))
    return bits & exponent == exponent and bits & fraction != 0


def read(path):
    lines = Path(path).read_text(encoding="utf-8").splitlines()
    require(len(lines) == 202, f"{path}: expected 200 records and two markers")
    require(lines[0] == HEADER and lines[-1] == WITNESS,
            f"{path}: missing exact execution markers")
    result = {}
    for offset, target in enumerate(TARGETS):
        digits = 8 if target[-1] == "f" else 16
        values = []
        for case in range(20):
            line = lines[1 + offset * 20 + case]
            match = re.fullmatch(re.escape(target) + ":" + str(case)
                                 + ":([0-9A-F]{" + str(digits) + "})", line)
            require(match is not None, f"{path}: wrong or duplicate record: {line}")
            value = int(match.group(1), 16)
            require(is_nan(value, target[-1]) == (case in NAN_CASES),
                    f"{path}: {target} case {case}: wrong NaN classification")
            values.append(value)
        result[target] = tuple(values)
    return result


def matches(actual, expected):
    return all(wanted is None or got == wanted
               for got, wanted in zip(actual, expected))


def nonfused(data, label):
    for target in TARGETS:
        expected = UNFUSED[target[-1]]
        for case, (got, wanted) in enumerate(zip(data[target], expected)):
            require(wanted is None or got == wanted,
                    f"{label}: {target} case {case}: {got:X} != nonfused {wanted:X}"
                    if wanted is not None else "")


def differences(left, right, nan):
    return sum(a != b for target in TARGETS
               for case, (a, b) in enumerate(zip(left[target], right[target]))
               if (case in NAN_CASES) == nan)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("default")
    parser.add_argument("nohw")
    parser.add_argument("nofma")
    parser.add_argument("--native")
    parser.add_argument("--third-profile", choices=("no-avx2-fma", "hardware-disabled-fallback"),
                        default="no-avx2-fma")
    args = parser.parse_args()
    default, nohw, nofma = map(read, (args.default, args.nohw, args.nofma))
    nonfused(nohw, "hardware-disabled CLR")
    third_label = ("no-AVX2/FMA CLR" if args.third_profile == "no-avx2-fma"
                   else "hardware-disabled fallback CLR")
    nonfused(nofma, third_label)
    policies = []
    for target in TARGETS:
        kind = target[-1]
        allowed = [name for name, profile in (("fused", FUSED), ("unfused", UNFUSED))
                   if matches(default[target], profile[kind])]
        require(bool(allowed), f"default CLR: {target}: inconsistent rounding profile")
        policies.append(target + "=" + "/".join(allowed))
    print("default CLR profiles: " + ",".join(policies))
    print("default versus software: non-NaN-bit differences="
          + str(differences(default, nohw, False)) + "; NaN-bit differences="
          + str(differences(default, nohw, True)))
    print(third_label + " versus software: non-NaN-bit differences="
          + str(differences(nofma, nohw, False)) + "; NaN-bit differences="
          + str(differences(nofma, nohw, True)))
    if args.native:
        native = read(args.native)
        nonfused(native, "native software-estimate profile")
        for target in TARGETS:
            for case in range(20):
                if case not in NAN_CASES:
                    require(native[target][case] == nohw[target][case],
                            f"native: {target} case {case}: software CLR differs")
        print("native software estimate: non-NaN bits and NaN classifications match")
        print("native versus software: NaN-bit differences="
              + str(differences(native, nohw, True)))
    print("vector estimate oracle contract: ten targets and all records passed")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError) as error:
        raise SystemExit(str(error)) from error
