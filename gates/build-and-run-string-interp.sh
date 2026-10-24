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
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|comma-scaling-prefix:before-comma-scaling|custom-matrix-prefix:before-custom-matrix"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|standard-nul-prefix:before-standard-nul"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/StringInterp")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-concat-sequences)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Concat<T> and Join<T> sequence shapes ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Concat<T> and Join<T> sequence shapes ==' \
        'bare ulong[]: 18446744073709551615,2 | 184467440737095516152'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-comma-scaling)
    prefix=$(awk '/^== Custom numeric comma scaling ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/StringInterp" before-comma-scaling)
    assert_output "$prefix" "$(strip_cr_win "$before")"
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
        [ "$(grep -Fxc "$line" <<< "$native")" = 1 ] \
            || { echo "FAIL: numeric comma scaling witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-custom-matrix)
    prefix=$(awk '/^== Custom numeric sections and placeholders ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/StringInterp" before-custom-matrix)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== Custom numeric sections and placeholders ==' \
        'rounded section=0' 'empty negative=-5' 'fraction exponent=.5E+0' \
        'mixed zero=05' 'integer literal=123x4' 'repeated point=1.5' \
        'exponent width=1E+0000000003' 'embedded nul=[5]' \
        'nul negative section=[-5]' \
        'matrix 0/0=[(1)]' 'matrix 1/1=[-5]' 'matrix 17/5=[0,005]' \
        'Custom numeric sections and placeholders end'; do
        [ "$(grep -Fxc "$line" <<< "$native")" = 1 ] \
            || { echo "FAIL: custom numeric matrix witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-standard-nul)
    prefix=$(awk '/^== Standard numeric NUL termination ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/StringInterp" before-standard-nul)
    assert_output "$prefix" "$(strip_cr_win "$before")"
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
        [ "$(grep -Fxc "$line" <<< "$native")" = 1 ] \
            || { echo "FAIL: standard numeric NUL witness missing: $line" >&2; exit 1; }
    done
    run_bounded "./$out/StringInterp" > "$out/standard-nul-native.out"
    run_bounded dotnet "$_CG_APP" > "$out/standard-nul-dotnet.out"
    tr -d '\r' < "$out/standard-nul-native.out" > "$out/standard-nul-native-lf.out"
    tr -d '\r' < "$out/standard-nul-dotnet.out" > "$out/standard-nul-dotnet-lf.out"
    cmp "$out/standard-nul-dotnet-lf.out" "$out/standard-nul-native-lf.out" || {
        echo "FAIL: standard numeric NUL raw bytes differ from real .NET" >&2; exit 1; }
    tail -c 15 "$out/standard-nul-dotnet-lf.out" | cmp - <(printf 'raw=[5.00]\000END\n') || {
        echo "FAIL: standard numeric NUL raw-byte witness missing" >&2; exit 1; }
}

corelib_diff_gate StringInterp
