#!/usr/bin/env bash
# Consolidated string concat/interpolation gate: String.Concat (string/char/value
# operands, join-list), interpolated string handlers, interpolation via newobj,
# AppendFormat, and custom format providers. ConcatSequenceSubset drives
# string.Concat<T> and Join<T> over an array, a List<T>, a concrete collection and a
# bare IEnumerable<T> over an array or a list: unsigned elements print unsigned on
# both bare-interface branches, and a null sequence raises ArgumentNullException.
#
# A LIVE `dotnet $app` diff. It was a frozen snapshot because the custom-format
# section is culture-dependent — percent and currency formatting, where real .NET
# used the host's culture and the transpiler a fixed invariant one. The driver
# pins CurrentCulture, which makes both sides invariant and removes the
# asymmetry, so the expectation moves
# from "whatever the transpiler printed" to "what real .NET prints".
# Former gates: concat, concat-value, concat-join-list, interp, interp-handler,
# interpolation-newobj, append-format, custom-format.
# Custom numeric commas scale at the last integer placeholder and group earlier;
# quoted and escaped commas remain literals. Custom formats round decimal digit
# midpoints away from zero, keep integers and decimals exact, and sign only a
# value that is still negative after rounding.
# Custom sections are selected after rounding; placeholders, interleaved literals,
# and repeated decimal points follow the CLR's digit stream.
# Standard numeric formats terminate at NUL; raw output comparisons retain the
# byte that shell command substitution would discard.
# Decimal standard fixed formats round exact digits; explicit G precision rounds
# significant digits without changing the unqualified exact layout.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|comma-scaling-prefix:before-comma-scaling|custom-matrix-prefix:before-custom-matrix"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|standard-nul-prefix:before-standard-nul"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|standard-decimal-prefix:before-standard-decimal"

gate_extra_asserts() {
    local out="$1" native before prefix line long_sign
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/StringInterp"
    gate_output select "$out/assert-native-1.stdout" "$out/text-witnesses.stdout" \
        --through 'Standard numeric NUL termination end' --policy crlf
    native="$out/text-witnesses.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-concat-sequences
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-11.stdout"

    gate_output select "$native" "$prefix" --before '== Concat<T> and Join<T> sequence shapes ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== Concat<T> and Join<T> sequence shapes ==' \
        'bare ulong[]: 18446744073709551615,2 | 184467440737095516152'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" before-comma-scaling
    before="$out/assert-before-3.stdout"
    prefix="$out/assert-prefix-12.stdout"
    gate_output select "$native" "$prefix" --before '== Custom numeric comma scaling ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-4" run_bounded "./$out/StringInterp" before-comma-scaling
    before="$out/assert-before-4.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Custom numeric comma scaling ==' 'trillion=100' \
        'fraction=123.457' 'integer=1235' 'grouped=1,235' \
        'grouped fraction=1,234,567.89' 'negative=(1.23)' 'zero=zero' \
        'ordinary grouping=1,234,567' 'quoted=1234567,' 'escaped=1234567,' \
        'fraction commas=1234.5' 'percent=123.46%' 'interpolation=1.234' \
        'midpoint long=1235' 'midpoint double=3' 'midpoint fraction=1.3' \
        'midpoint unscaled=3' 'midpoint negative=-3' 'midpoint float=0.13' \
        'midpoint percent=0.13%' 'midpoint exponent=1.3E+0' \
        'exact long=9,223,372,036,854,775,807' 'scaled ulong=18446744073709552' \
        'negative int to zero=0' 'negative int to nothing=[]' \
        'negative short fraction=0.00' 'negative interpolation=0' \
        'negative literal=x' 'negative double to zero=-0' \
        'negative double to nothing=[]' 'negative double literal=-x' \
        'negative zero double=-0' 'negative decimal to zero=0.00' \
        'decimal integer=1234567890123456' \
        'decimal fraction=0.1234567890123456' \
        'decimal grouped=12,345,678,901,234,567,890,123.5' \
        'decimal scaled=79228162514264337593543950' \
        'decimal midpoint=-1234567890123456790' 'decimal optional=1.5' \
        'decimal exponent=1.23E+27' \
        'Custom numeric comma scaling end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: numeric comma scaling witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-5" run_bounded dotnet "$_CG_APP" before-custom-matrix
    before="$out/assert-before-5.stdout"
    prefix="$out/assert-prefix-13.stdout"
    gate_output select "$native" "$prefix" --before '== Custom numeric sections and placeholders ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-6" run_bounded "./$out/StringInterp" before-custom-matrix
    before="$out/assert-before-6.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Custom numeric sections and placeholders ==' \
        'rounded section=0' 'empty negative=-5' 'fraction exponent=.5E+0' \
        'mixed zero=05' 'integer literal=123x4' 'repeated point=1.5' \
        'exponent width=1E+0000000003' 'embedded nul=[5]' \
        'nul negative section=[-5]' \
        'matrix 0/0=[(1)]' 'matrix 1/1=[-5]' 'matrix 17/5=[0,005]' \
        'Custom numeric sections and placeholders end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: custom numeric matrix witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-7" run_bounded dotnet "$_CG_APP" before-standard-nul
    before="$out/assert-before-7.stdout"
    prefix="$out/assert-prefix-14.stdout"
    gate_output select "$native" "$prefix" --before '== Standard numeric NUL termination ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-8" run_bounded "./$out/StringInterp" before-standard-nul
    before="$out/assert-before-8.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Standard numeric NUL termination ==' \
        'matrix 2/decimal=[1,235]' 'matrix 4/int=[1,234.00]' \
        'matrix 5/double=[1,234.60]' 'matrix 5/float=[1,234.60]' \
        'matrix 8/int=[1234]' 'matrix 9/double=[1235x]' \
        'significant=[1.23E+03]' 'hex=[FF]' 'padded=[005]' \
        'decimal exact=[1234567890123456789012345678]' \
        'negative decimal=[0.00]' \
        'float G0=[0.1]' 'float G NUL=[0.1]' \
        'hex X0=[0]' 'hex X NUL=[0]' \
        'float g0 NaN=[NAN-E]' 'float g NUL NaN=[NAN-E]' \
        'float g0 negative=[NEG1]' 'float g NUL negative=[NEG1]' \
        'float g0 exponent=[1e+20]' 'float g NUL exponent=[1e+20]' \
        'float r NaN=[NAN-E]' 'double r NaN=[NAN-E]' \
        'float r negative=[NEG1]' 'double r negative=[NEG1]' \
        'Standard numeric NUL termination end'; do
        gate_output witness "$native" "$line" --count 1 --policy crlf \
            || { echo "FAIL: standard numeric NUL witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/standard-nul-oracle" run_bounded dotnet "$_CG_APP"
    gate_output compare "$out/assert-native-1.stdout" "$out/standard-nul-oracle.stdout"
    gate_capture_ok "$out/standard-decimal-before-oracle" run_bounded dotnet "$_CG_APP" before-standard-decimal
    gate_capture_ok "$out/standard-decimal-before-native" run_bounded "./$out/StringInterp" before-standard-decimal
    gate_output byte-prefix "$out/standard-nul-oracle.stdout" "$out/standard-decimal-before-oracle.stdout"
    gate_output byte-prefix "$out/assert-native-1.stdout" "$out/standard-decimal-before-native.stdout"
    printf 'raw=[5.00]\000END\n' > "$out/standard-nul-witness.bin"
    gate_output byte-suffix "$out/standard-decimal-before-oracle.stdout" "$out/standard-nul-witness.bin"
    gate_output byte-suffix "$out/standard-decimal-before-native.stdout" "$out/standard-nul-witness.bin"
    gate_output select "$out/assert-native-1.stdout" "$out/decimal-witnesses.stdout" \
        --from '== Decimal standard rounding and precision ==' --policy crlf
    native="$out/decimal-witnesses.stdout"
    for line in '== Decimal standard rounding and precision ==' \
        'decimal 1/0=[1,235]' 'decimal 6/0=[1.23E+03]' \
        'decimal 8/0=[1234.5]' 'decimal 7/0=[1.23e+03]' \
        'decimal 6/10=[1.25]' \
        'defaults=[12.35|12.35|¤12.35|1,234.50 %]' \
        'provider N0=[NEG1.235]' 'provider G3=[NEG1,23E+03]' \
        'Decimal standard rounding and precision end'; do
        gate_output witness "$native" "$line" --count any --policy crlf || {
            echo "FAIL: decimal standard witness missing: $line" >&2; exit 1; }
    done
    printf -v long_sign '%200s' ''
    long_sign=${long_sign// /+}
    gate_output witness "$native" "long exponent sign=[1.23E${long_sign}03]" --count any --policy crlf || {
        echo "FAIL: long decimal exponent sign witness missing" >&2; exit 1; }
}

corelib_diff_gate StringInterp
