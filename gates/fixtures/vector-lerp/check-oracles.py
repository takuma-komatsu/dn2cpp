#!/usr/bin/env python3
"""Independent rational IEEE oracle; no CLR/native outputs supply expected bits."""
import argparse
from fractions import Fraction
from pathlib import Path
import re

INPUTS = {'fa': [1073741824, 1065353217, 2139095039, 1, 8388608, 2147483648, 0, 2139095040, 4286578688, 2143363909, 2139169605, 1065353216, 3212836864, 1065353217, 2139095039, 4286578687, 1056964608, 1065353216, 2147483649, 1065353215, 8388608, 2155872256, 1073741824, 1073741824], 'fb': [1082130432, 3212836864, 4286578687, 2147483649, 0, 2147483648, 2147483648, 1065353216, 2139095040, 1065353216, 4290847557, 2143634209, 2139169605, 3212836865, 2139095039, 4286578687, 1065353216, 0, 1, 1065353217, 2147483649, 1, 1082130432, 1082130432], 'ft': [1056964608, 1056964609, 1056964608, 1056964608, 1065353215, 2147483648, 0, 1065353216, 0, 1056964608, 1065353216, 0, 1056964608, 1065353215, 3212836864, 1073741824, 1051372203, 2139095040, 1056964608, 864026624, 1056964608, 1056964608, 3212836864, 1073741824], 'da': [4611686018427387904, 4607182418800017409, 9218868437227405311, 1, 4503599627370496, 9223372036854775808, 0, 9218868437227405312, 18442240474082181120, 9221140253039434428, 9218888453225749180, 4607182418800017408, 13830554455654793216, 4607182418800017409, 9218868437227405311, 18442240474082181119, 4602678819172646912, 4607182418800017408, 9223372036854775809, 4607182418800017407, 4503599627370496, 9227875636482146304, 4611686018427387904, 4611686018427387904], 'db': [4616189618054758400, 13830554455654793216, 18442240474082181119, 9223372036854775809, 0, 9223372036854775808, 9223372036854775808, 4607182418800017408, 9218868437227405312, 4607182418800017408, 18444584847926384248, 9221212811071608440, 9218888453225749180, 13830554455654793217, 9218868437227405311, 18442240474082181119, 4607182418800017408, 0, 1, 4607182418800017409, 9223372036854775809, 1, 4616189618054758400, 4616189618054758400], 'dt': [4602678819172646912, 4602678819172646913, 4602678819172646912, 4602678819172646912, 4607182418800017407, 9223372036854775808, 0, 4607182418800017408, 0, 4602678819172646912, 4607182418800017408, 0, 4602678819172646912, 4607182418800017407, 13830554455654793216, 4611686018427387904, 4599676419421066581, 9218868437227405312, 4602678819172646912, 4368491638549381120, 4602678819172646912, 4602678819172646912, 13830554455654793216, 4611686018427387904]}


def require(ok, label):
    if not ok:
        raise ValueError(label)


def format_for(kind):
    return (23, 8, 127) if kind == 'f' else (52, 11, 1023)


def decode(bits, kind):
    p, e, bias = format_for(kind)
    sign = bits >> (p + e)
    exponent = (bits >> p) & ((1 << e) - 1)
    fraction = bits & ((1 << p) - 1)
    if exponent == (1 << e) - 1:
        return ('nan' if fraction else 'inf', sign, Fraction(0))
    mantissa = fraction if exponent == 0 else (1 << p) | fraction
    shift = (1 - bias if exponent == 0 else exponent - bias) - p
    value = Fraction(mantissa) * Fraction(2) ** shift
    return ('finite', sign, -value if sign else value)


def round_bits(value, negative_zero, kind):
    p, e, bias = format_for(kind)
    sign = value < 0 or value == 0 and negative_zero
    sign_bits = int(sign) << (p + e)
    if value == 0:
        return sign_bits
    value = abs(value)
    exponent = value.numerator.bit_length() - value.denominator.bit_length()
    if value < Fraction(2) ** exponent:
        exponent -= 1
    quantum = max(exponent, 1 - bias) - p
    scaled = value / Fraction(2) ** quantum
    quotient, remainder = divmod(scaled.numerator, scaled.denominator)
    if 2 * remainder > scaled.denominator or 2 * remainder == scaled.denominator and quotient & 1:
        quotient += 1
    if quotient == 0:
        return sign_bits
    if quotient >= 1 << (p + 1):
        quotient >>= 1
        quantum += 1
    exponent = quantum + p
    if exponent > bias:
        return sign_bits | (((1 << e) - 1) << p)
    if quotient < 1 << p:
        return sign_bits | quotient
    return sign_bits | ((exponent + bias) << p) | (quotient - (1 << p))


def operation(a, b, kind, multiply=False, addend=None):
    # None denotes any NaN payload/sign: classification only is portable.
    if a is None or b is None:
        return None
    x, y = decode(a, kind), decode(b, kind)
    p, e, bias = format_for(kind)
    inf = ((1 << e) - 1) << p
    if x[0] == 'nan' or y[0] == 'nan':
        return None
    if multiply:
        sign = x[1] ^ y[1]
        if x[0] == 'inf' or y[0] == 'inf':
            if x[0] == 'finite' and x[2] == 0 or y[0] == 'finite' and y[2] == 0:
                return None
            product = (sign << (p + e)) | inf
            return operation(product, addend, kind) if addend is not None else product
        value = x[2] * y[2]
        if addend is None:
            return round_bits(value, sign, kind)
        z = decode(addend, kind)
        if z[0] == 'nan':
            return None
        if z[0] == 'inf':
            return addend
        return round_bits(value + z[2], value == 0 and z[2] == 0 and sign and z[1], kind)
    if x[0] == 'inf' or y[0] == 'inf':
        if x[0] == y[0] == 'inf' and x[1] != y[1]:
            return None
        return a if x[0] == 'inf' else b
    return round_bits(x[2] + y[2], x[2] == y[2] == 0 and x[1] and y[1], kind)


def expected(a, b, t, kind):
    p, e, bias = format_for(kind)
    one = bias << p
    complement = operation(one, t ^ (1 << (p + e)), kind)
    right = operation(b, t, kind, multiply=True)
    if complement is None or right is None:
        return None, None
    left = operation(a, complement, kind, multiply=True)
    separate = operation(left, right, kind)
    fused = operation(a, complement, kind, multiply=True, addend=right)
    return separate, fused


def same(bits, wanted, kind):
    return decode(bits, kind)[0] == 'nan' if wanted is None else bits == wanted


def check(path, profile):
    targets = [w + k for w in ('64', '128', '256', '512', 'n') for k in ('f', 'd')]
    targets += [f'v{d}-{m}' for d in (2, 3, 4) for m in ('vector', 'scalar')]
    data = {t: {} for t in targets}
    markers = []
    for line in Path(path).read_text().splitlines():
        if line.startswith('order:') or line == 'vector-lerp:all-ten-routes-and-concrete-passed':
            markers.append(line)
            continue
        match = re.fullmatch(r'([^:]+):(\d+):(\d+):([0-9A-F]+):([0-9A-F]+):([0-9A-F]+)', line)
        require(match is not None, f'{path}: malformed record {line}')
        target, case, lane, actual, separate, fused = match.groups()
        require(target in data, f'unknown target {target}')
        key = (int(case), int(lane))
        require(key not in data[target], f'duplicate {target}:{key}')
        data[target][key] = tuple(int(v, 16) for v in (actual, separate, fused))
    want_markers = [f'order:{w}{k}:direct-delegate-calli' for w in ('64','128','256','512','n') for k in ('f','d')]
    want_markers += [f'order:v{d}:scalar-vector' for d in (2,3,4)] + ['vector-lerp:all-ten-routes-and-concrete-passed']
    require(markers == want_markers, f'{path}: missing/duplicate execution marker')
    policies = []
    local_diagnostic_mismatches = {"separate": 0, "fused": 0}
    for target, values in data.items():
        concrete = target.startswith('v')
        kind = 'f' if concrete else target[-1]
        lanes = max((lane for case, lane in values), default=-1) + 1
        if concrete:
            require(lanes == int(target[1]), f'{target}: wrong dimension')
        elif target[0] != 'n':
            require(lanes == int(target[:-1]) // (32 if kind == 'f' else 64), f'{target}: wrong lanes')
        else:
            require(lanes in ((4,8,16) if kind == 'f' else (2,4,8)), f'{target}: invalid numerics width')
        require(set(values) == {(case,lane) for case in range(24) for lane in range(lanes)}, f'{target}: missing records')
        allowed = {'separate', 'fused'} if profile == 'default' else {'separate'}
        for (case,lane), (actual,separate,fused) in values.items():
            k = (case + lane) % 24
            ti = case if concrete and target.endswith('scalar') else k
            a,b,t = INPUTS[kind+'a'][k], INPUTS[kind+'b'][k], INPUTS[kind+'t'][ti]
            want_s, want_f = expected(a,b,t,kind)
            if profile == 'native':
                # Optimizers may eliminate pure identity Round calls despite noinline.
                # Native oracle columns are diagnostics, never acceptance evidence.
                local_diagnostic_mismatches['separate'] += not same(separate,want_s,kind)
                local_diagnostic_mismatches['fused'] += not same(fused,want_f,kind)
            else:
                require(same(separate,want_s,kind), f'{target}:{case}:{lane}: typed-round oracle differs')
                require(same(fused,want_f,kind), f'{target}:{case}:{lane}: fused oracle differs')
            if not same(actual,want_s,kind): allowed.discard('separate')
            if not same(actual,want_f,kind): allowed.discard('fused')
        require(bool(allowed), f'{path}: {target} violates {profile} rounding profile')
        policies.append(target+'='+ '/'.join(sorted(allowed)))
    require(len(data['nf']) == 2 * len(data['nd']), f'{path}: inconsistent Numerics widths')
    if profile == 'native':
        print('native-local-oracle-diagnostics-only:rational-mismatches:'
              + ','.join(name + '=' + str(count) for name,count in local_diagnostic_mismatches.items()))
    print(profile + ': ' + ','.join(policies))
    print('vector-lerp-oracle:records=' + str(sum(map(len,data.values()))) + ':targets=16')
    return data


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output')
    parser.add_argument('--profile', choices=('default','software','native'), required=True)
    parser.add_argument('--reference', help='independent software CLR stdout for native comparison')
    args = parser.parse_args()
    try:
        actual = check(args.output,args.profile)
        if args.profile == 'native':
            require(args.reference is not None, 'native requires independent CLR reference')
            reference = check(args.reference,'software')
            for target, records in actual.items():
                kind = 'f' if target.startswith('v') else target[-1]
                for (case,lane), values in records.items():
                    key = (case,lane) if target.startswith('v') else ((case+lane)%24,0)
                    wanted = reference[target][key][1]
                    if decode(wanted,kind)[0] == 'nan': wanted = None
                    require(same(values[0],wanted,kind), f'{target}:{case}:{lane}: native differs from CLR Separate')
            print('native versus independent CLR Separate: matched')
    except (ValueError, OSError) as error:
        raise SystemExit(str(error)) from error
