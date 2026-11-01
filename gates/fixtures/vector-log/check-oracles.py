#!/usr/bin/env python3
"""Directed MPFR reference with a preselected one-real-result-ULP native gate.

The ceiling is a regression criterion, not a universal libm/.NET accuracy or
accelerated-CLR bit-parity guarantee. It follows the separate sub-one-ULP
analyses of GNU libc's logf, log, log2f and log2 algorithms; it does not assert
source-to-binary reproducibility of the installed library. CLR errors remain
diagnostic. Each operation has its own directed MPFR reference, and unresolved
intervals fail rather than loosening the bound. A host MPFR runtime is required.

Primary algorithm analyses:
https://raw.githubusercontent.com/bminor/glibc/glibc-2.41/sysdeps/ieee754/flt-32/e_logf.c
https://raw.githubusercontent.com/bminor/glibc/glibc-2.41/sysdeps/ieee754/dbl-64/e_log.c
https://sourceware.org/legacy-ml/glibc-cvs/2017-q3/msg00838.html
https://raw.githubusercontent.com/bminor/glibc/glibc-2.41/sysdeps/ieee754/dbl-64/e_log2.c
"""
import argparse
import ctypes
import ctypes.util
from fractions import Fraction
from functools import lru_cache
import json
from pathlib import Path
import re
import struct


def require(ok, message):
    if not ok:
        raise ValueError(message)


def parameters(kind):
    return (23, 8, 127) if kind == 'f' else (52, 11, 1023)


def corpus(kind):
    p, e, bias = parameters(kind)
    limit, infinity, sign, one = 1 << p, ((1 << e) - 1) << p, 1 << (p + e), bias << p
    special = [0, sign, infinity, sign | infinity, infinity | (limit >> 1) | 123,
               sign | infinity | (limit >> 1) | 321, infinity | 123, sign | 1,
               sign | one, 1, limit - 1, limit, infinity - 1, one, one - 1, one + 1]
    result = [special[(rotation + lane) % 16] for rotation in range(16) for lane in range(16)]
    for exponent in range(1, (1 << e) - 1):
        value = exponent << p
        result.extend((value - 1, value, value + 1))
    for bit in range(p):
        value = 1 << bit
        result.extend((value - 1, value, value + 1))
    result.extend(one + delta for delta in range(-64, 65))
    for exponent in (1, bias - 1, bias, bias + 1, (1 << e) - 2):
        for mantissa in (1, limit // 3, limit // 2, 2 * limit // 3, limit - 2):
            value = (exponent << p) + mantissa
            result.extend((value - 1, value, value + 1))
    state = 0x5A17
    for _ in range(256):
        state = (state * 6364136223846793005 + 1442695040888963407) & ((1 << 64) - 1)
        result.append(((1 + state % ((1 << e) - 2)) << p) | ((state >> 11) & (limit - 1)))
    return result


def decode(bits, kind):
    p, e, bias = parameters(kind)
    sign, exponent, mantissa = bits >> (p + e), (bits >> p) & ((1 << e) - 1), bits & ((1 << p) - 1)
    if exponent == (1 << e) - 1:
        return ('nan' if mantissa else 'inf'), sign, Fraction(0)
    if exponent:
        mantissa |= 1 << p
    value = Fraction(mantissa) * Fraction(2) ** ((exponent - bias if exponent else 1 - bias) - p)
    return 'finite', sign, -value if sign else value


def binary(bits, kind):
    return struct.unpack('>f' if kind == 'f' else '>d', bits.to_bytes(4 if kind == 'f' else 8, 'big'))[0]


def bits_of(value, kind):
    return int.from_bytes(struct.pack('>f' if kind == 'f' else '>d', value), 'big')


def binade(value):
    value = abs(value)
    require(value != 0, 'zero has no binade')
    result = value.numerator.bit_length() - value.denominator.bit_length()
    return result - int(value < Fraction(2) ** result)


# MPFR's documented public ABI uses long precision/exponent, int sign, limb pointer.
class Number(ctypes.Structure):
    _fields_ = [('precision', ctypes.c_long), ('sign', ctypes.c_int),
                ('exponent', ctypes.c_long), ('limbs', ctypes.POINTER(ctypes.c_ulong))]


class Oracle:
    def __init__(self):
        name = ctypes.util.find_library('mpfr')
        require(name is not None, 'MPFR runtime is required for independent logarithm validation')
        self.lib = ctypes.CDLL(name)
        ptr = ctypes.POINTER(Number)
        signatures = {
            'mpfr_init2': ([ptr, ctypes.c_long], None),
            'mpfr_clear': ([ptr], None),
            'mpfr_set_d': ([ptr, ctypes.c_double, ctypes.c_int], ctypes.c_int),
            'mpfr_get_d': ([ptr, ctypes.c_int], ctypes.c_double),
            'mpfr_log': ([ptr, ptr, ctypes.c_int], ctypes.c_int),
            'mpfr_log2': ([ptr, ptr, ctypes.c_int], ctypes.c_int),
            'mpfr_get_str': ([ctypes.c_void_p, ctypes.POINTER(ctypes.c_long), ctypes.c_int,
                              ctypes.c_size_t, ptr, ctypes.c_int], ctypes.c_void_p),
            'mpfr_free_str': ([ctypes.c_void_p], None),
            'mpfr_get_version': ([], ctypes.c_char_p),
        }
        for name, (args, result) in signatures.items():
            function = getattr(self.lib, name)
            function.argtypes, function.restype = args, result
        self.version = self.lib.mpfr_get_version().decode('ascii')
        # Do not assume a successful load proves the platform's ABI layout.
        number = Number()
        self.lib.mpfr_init2(ctypes.byref(number), 256)
        try:
            require(number.precision == 256 and bool(number.limbs), 'unsupported MPFR ABI')
            require(self.lib.mpfr_set_d(ctypes.byref(number), 1.25, 0) == 0, 'MPFR input conversion is inexact')
            require(self.rational(number) == Fraction(5, 4), 'MPFR ABI/rational conversion failed')
            require(self.lib.mpfr_get_d(ctypes.byref(number), 0) == 1.25, 'MPFR ABI roundtrip failed')
        finally:
            self.lib.mpfr_clear(ctypes.byref(number))

    def rational(self, number):
        exponent = ctypes.c_long()
        address = self.lib.mpfr_get_str(None, ctypes.byref(exponent), 2, 0, ctypes.byref(number), 0)
        require(bool(address), 'MPFR could not allocate endpoint text')
        try:
            text = ctypes.string_at(address).decode('ascii')
        finally:
            self.lib.mpfr_free_str(address)
        sign = -1 if text.startswith('-') else 1
        digits = text.lstrip('-')
        require(re.fullmatch('[01]+', digits) is not None, 'nonfinite MPFR endpoint')
        return sign * Fraction(int(digits, 2)) * Fraction(2) ** (exponent.value - len(digits))

    @lru_cache(maxsize=None)
    def interval(self, operation, kind, input_bits, precision):
        x, lower, upper, rounded = Number(), Number(), Number(), Number()
        p = parameters(kind)[0] + 1
        for number, bits in ((x, precision), (lower, precision), (upper, precision), (rounded, p)):
            self.lib.mpfr_init2(ctypes.byref(number), bits)
        try:
            value = binary(input_bits, kind)
            require(self.lib.mpfr_set_d(ctypes.byref(x), value, 0) == 0, 'input not represented exactly')
            require(bits_of(self.lib.mpfr_get_d(ctypes.byref(x), 0), kind) == input_bits, 'input bit roundtrip changed')
            require(self.rational(x) == decode(input_bits, kind)[2], 'input rational changed')
            function = self.lib.mpfr_log if operation == 'Log' else self.lib.mpfr_log2
            function(ctypes.byref(lower), ctypes.byref(x), 3)  # RNDD
            function(ctypes.byref(upper), ctypes.byref(x), 2)  # RNDU
            function(ctypes.byref(rounded), ctypes.byref(x), 0)  # p-bit RNDN, no double rounding
            lo, hi = self.rational(lower), self.rational(upper)
            require(lo <= hi, 'invalid directed interval')
            return lo, hi, bits_of(self.lib.mpfr_get_d(ctypes.byref(rounded), 0), kind)
        finally:
            for number in (x, lower, upper, rounded):
                self.lib.mpfr_clear(ctypes.byref(number))

    @lru_cache(maxsize=None)
    def error(self, operation, kind, source, actual, strict):
        target = decode(actual, kind)[2]
        for precision in (256, 512, 1024, 2048, 4096):
            lo, hi, nearest = self.interval(operation, kind, source, precision)
            if lo == 0 or hi == 0 or (lo < 0) != (hi < 0) or binade(lo) != binade(hi):
                continue
            ulp = Fraction(2) ** (binade(lo) - parameters(kind)[0])
            upper = max(abs(target - lo), abs(target - hi)) / ulp
            lower = (lo - target if target < lo else target - hi if target > hi else Fraction(0)) / ulp
            if not strict or upper <= 1 or lower > 1:
                require(not strict or upper <= 1,
                        f'{operation}/{kind}/{source:X}: error exceeds frozen one-real-result-ULP gate ({float(lower):.9g}..{float(upper):.9g})')
                return upper, nearest
        raise ValueError(f'uncertified interval/accuracy decision: {operation}/{kind}/{source:X}')


def check_value(oracle, operation, kind, source, actual, strict):
    p, e, bias = parameters(kind)
    infinity = ((1 << e) - 1) << p
    sign_bit = 1 << (p + e)
    category, sign, value = decode(source, kind)
    actual_category = decode(actual, kind)[0]
    if category == 'nan' or sign and (category == 'inf' or value != 0):
        require(actual_category == 'nan', 'domain/NaN input must produce NaN')
        return None, None
    if category == 'inf':
        require(actual == infinity, '+infinity must produce +infinity')
        return Fraction(0), infinity
    if value == 0:
        require(actual == sign_bit | infinity, 'both zeros must produce -infinity')
        return Fraction(0), sign_bit | infinity
    require(actual_category == 'finite', 'positive finite input must produce finite output')
    if value == 1:
        require(actual == 0, 'one must produce positive zero')
        return Fraction(0), 0
    if operation == 'Log2' and value.numerator & (value.numerator - 1) == 0 and value.denominator & (value.denominator - 1) == 0:
        exponent = value.numerator.bit_length() - value.denominator.bit_length()
        wanted = bits_of(float(exponent), kind)
        require(actual == wanted, 'Log2 power-of-two anchor must be the exact integer exponent')
        return Fraction(0), wanted
    return oracle.error(operation, kind, source, actual, strict)


def check(path, profile):
    oracle = Oracle()
    inputs = {kind: corpus(kind) for kind in ('f', 'd')}
    tags = [width + kind for width in ('64', '128', '256', '512', 'n') for kind in ('f', 'd')] + ['v2', 'v3', 'v4']
    keys = {(op, tag) for op in ('Log', 'Log2') for tag in tags}
    seen, widths, orders, statistics = {}, {}, set(), {}
    complete = 0
    pattern = re.compile(r'(Log2?):(64[fd]|128[fd]|256[fd]|512[fd]|n[fd]|v[234]):([0-9]+):([0-9]+):([0-9A-F]+):([0-9A-F]+)')
    for line in Path(path).read_text().splitlines():
        if line == 'vector-log:all-twenty-routes-and-concrete-passed':
            complete += 1
            continue
        if line.startswith('order:'):
            parts = line.split(':')
            require(len(parts) == 4 and parts[3] == 'direct-delegate-calli', 'invalid order record')
            key = tuple(parts[1:3])
            require(key in keys and key not in orders, 'duplicate/unknown order record')
            orders.add(key)
            continue
        match = pattern.fullmatch(line)
        require(match is not None, 'unexpected output: ' + line)
        operation, tag, batch, lane, source_text, actual_text = match.groups()
        batch, lane = int(batch), int(lane)
        kind = 'f' if tag.startswith('v') else tag[-1]
        digits = 8 if kind == 'f' else 16
        require(len(source_text) == digits and len(actual_text) == digits, 'invalid raw bit width')
        source, actual = int(source_text, 16), int(actual_text, 16)
        key = (operation, tag)
        position = (batch, lane)
        bucket = seen.setdefault(key, set())
        require(position not in bucket, 'duplicate lane record')
        bucket.add(position)
        widths[key] = max(widths.get(key, 0), lane + 1)
        require(source == inputs[kind][(batch + lane) % len(inputs[kind])], 'input corpus mismatch')
        error, nearest = check_value(oracle, operation, kind, source, actual, profile == 'native')
        stat = statistics.setdefault(operation + '/' + kind, {'records': 0, 'nan_records': 0, 'non_nearest_records': 0, 'max_real_ulp_upper': 0.0})
        stat['records'] += 1
        if error is None:
            stat['nan_records'] += 1
        else:
            stat['non_nearest_records'] += actual != nearest
            stat['max_real_ulp_upper'] = max(stat['max_real_ulp_upper'], float(error))
    require(complete == 1 and orders == keys and set(seen) == keys, 'missing completion, route, or lane records')
    for operation, tag in sorted(keys):
        key = (operation, tag)
        kind = 'f' if tag.startswith('v') else tag[-1]
        width = widths[key]
        if tag.startswith('v'):
            require(width == int(tag[1:]), 'concrete vector width mismatch')
        elif tag.startswith('n'):
            allowed = (4, 8, 16) if kind == 'f' else (2, 4, 8)
            require(width in allowed, 'unsupported Numerics vector width')
            if profile in ('native', 'software'):
                require(width == (4 if kind == 'f' else 2), 'fixed/software Numerics width mismatch')
        else:
            require(width == int(tag[:-1]) // (32 if kind == 'f' else 64), 'fixed vector width mismatch')
        expected = {(batch, lane) for batch in range(0, len(inputs[kind]), width) for lane in range(width)}
        require(seen[key] == expected, 'missing or unexpected lane positions: ' + str(key))
    print(json.dumps({'profile': profile, 'reference': 'MPFR directed interval', 'mpfr_version': oracle.version,
                      'numeric_acceptance': 'one real-result ULP' if profile == 'native' else 'diagnostic only',
                      'input_counts': {kind: len(values) for kind, values in inputs.items()},
                      'route_groups': len(keys), 'statistics': statistics}, sort_keys=True, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('output', nargs='?')
    parser.add_argument('--profile', choices=('native', 'default', 'software'))
    parser.add_argument('--check-prerequisites', action='store_true')
    args = parser.parse_args()
    if args.check_prerequisites:
        print('MPFR ' + Oracle().version)
    else:
        require(args.output is not None and args.profile is not None, 'output and profile are required')
        check(args.output, args.profile)
