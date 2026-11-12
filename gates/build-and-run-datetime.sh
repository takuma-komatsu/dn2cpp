#!/usr/bin/env bash
# Consolidated DateTime gate: DateTime core arithmetic, Now/UtcNow/Today
# invariants + Convert.ChangeType->DateTime, parsing, formatting, time zones, and
# DateTimeOffset. The program prints only clock-invariant facts (kinds, sanity
# booleans, fixed date literals), so it is deterministic.
#
# A LIVE `dotnet $app` diff. It was a frozen snapshot because DateTime.ToString is
# culture-dependent — real .NET formats in the host's culture (ja-JP yyyy/MM/dd)
# where the transpiler used a fixed one (MM/dd/yyyy). The driver now pins both
# CurrentCulture and CurrentUICulture, the second being what the time-zone section needs
# (TimeZoneInfo.StandardName is resource-localized: `Koordinierte Weltzeit` on a
# de-DE host). Both sides now format invariantly, so the expectation is what real
# .NET prints rather than what the transpiler printed.
#
# Per-culture DATE formatting is still a carve-out (docs/STATUS.md) and this gate
# does NOT close it: the pin means neither side reaches a named culture's date
# patterns, so what is asserted here is the invariant ones. A section that set
# CurrentCulture to a real culture and printed a date would be red, correctly.
# TzSerializedStringSubset covers TimeZoneInfo.FromSerializedString over a fixed
# serialized zone with adjustment rules — the deserialize path routes a transition
# time through the internal TimeOnly.ToDateTime().
# FromBinary/ToBinary and daylight checks cover kind flags and local-zone overlaps.
# DTFI names, patterns, private arithmetic and culture-name validation share one
# image. Private calls/calli and closed span delegates use exact CoreLib
# declarations; Calendar and Tokenize refusals have an explicitly frozen
# native-only mode, not a CLR parity claim.
# Former gates: datetime, datetime-now, datetime-parse, datetime-format,
# datetime-tz, datetimeoffset.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/ldftn-local/DateTimeInterceptFixture.cs gates/fixtures/ldftn-local/DateTimeTCharFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj gates/expected/datetime-refusals.txt"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|datetime-private-calls:fixture,direct-only-emission|public-ctor:no-corelib,no-ildiet|refusals:native-frozen|tchar:malformed-return-owner|cli:$(_gate_cli_hash)"

# Keep the rewritten input private to this gate; the suite prebuild stays intact.
_corelib_gate_core() {
    local project="$1" out="$2"; shift 2
    _CG_OUT="$out"
    _CG_CORELIB=$(locate_corelib)
    _CG_EXTRA_REFERENCE_INPUTS=(samples/dotnet/DateTimeOps/keep-dtfi-intercepts.xml)
    build_proj "samples/dotnet/$project/$project.csproj"
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    mkdir -p "$out/app"
    cp -R "samples/dotnet/$project/bin/$CONFIG/$TFM/." "$out/app/"
    _CG_APP="$out/app/$project.dll"
    gate_capture_ok "$out/fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --datetime-intercepts "$_CG_APP" "$_CG_CORELIB"
    gate_output witness "$out/fixture.stdout" \
        'AddAddress=ldftn+calli:System.Boolean System.DateTime::TryAddTicks(System.Int64,System.DateTime&)' \
        'DateSpanDelegate=ldftn+delegate:System.ReadOnlySpan`1<TChar> System.Globalization.DateTimeFormatInfo::DateSeparatorTChar()' \
        --count 1 --policy crlf
    invoke_cli "$_CG_APP" -r "$_CG_CORELIB" "$@" -o "$out"
}

# Constructors, arithmetic, offset parsing and microseconds preserve fault fields.
gate_extra_asserts() {
    local out="$1" native before prefix line
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/DateTimeOps$EXE_EXT"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded "./$out/DateTimeOps$EXE_EXT" before-date-validation
    before="$out/assert-before-2.stdout"
    prefix="$out/assert-prefix-12.stdout"
    gate_output select "$native" "$prefix" --before '== datetime argument fields ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== datetime argument fields ==' 'datetime constructor order end' \
        'datetime argument fields end' '== datetime offset boundaries ==' \
        'datetime offset boundaries end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: date validation witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before-3" run_bounded "./$out/DateTimeOps$EXE_EXT" before-date-binary
    before="$out/assert-before-3.stdout"
    prefix="$out/assert-prefix-13.stdout"
    gate_output select "$native" "$prefix" --before '== datetime binary ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== datetime binary ==' 'datetime binary end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: date binary witness missing: $line" >&2; exit 1; }
    done
    local zone expected
    for zone in Etc/UTC Asia/Tokyo America/New_York; do
        TZ="$zone" gate_capture_ok "$out/zone-$zone/assert-native-4" run_bounded "./$out/DateTimeOps$EXE_EXT" binary-only
        native="$out/zone-$zone/assert-native-4.stdout"
        TZ="$zone" gate_capture_ok "$out/zone-$zone/assert-expected-5" run_bounded dotnet "$_CG_APP" binary-only
        expected="$out/zone-$zone/assert-expected-5.stdout"
        gate_output compare "$native" "$expected"
    done
    gate_capture_ok "$out/assert-before-6" run_bounded dotnet "$_CG_APP" before-date-exact-spans
    before="$out/assert-before-6.stdout"
    gate_capture_ok "$out/assert-native-7" run_bounded "./$out/DateTimeOps$EXE_EXT"
    native="$out/assert-native-7.stdout"

    prefix="$out/assert-prefix-14.stdout"

    gate_output select "$native" "$prefix" --before '== datetime exact spans ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_output witness "$native" 'datetime exact spans end' --count any --policy crlf \
        || { echo "FAIL: exact date span parsing did not run" >&2; exit 1; }
    gate_capture_ok "$out/assert-before-8" run_bounded dotnet "$_CG_APP" before-date-utf8
    before="$out/assert-before-8.stdout"
    prefix="$out/assert-prefix-15.stdout"
    gate_output select "$native" "$prefix" --before '== UTF-8 date destinations ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_output witness "$native" 'UTF-8 date destinations end' --count any --policy crlf \
        || { echo "FAIL: UTF-8 date formatting did not run" >&2; exit 1; }
    gate_capture_ok "$out/assert-before-9" run_bounded dotnet "$_CG_APP" before-daylight
    before="$out/assert-before-9.stdout"
    prefix="$out/assert-prefix-16.stdout"
    gate_output select "$native" "$prefix" --before '== daylight saving time ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_output witness "$native" 'daylight saving time end' --count any --policy crlf \
        || { echo 'FAIL: daylight saving time section did not run' >&2; exit 1; }
    if [ "$EXE_EXT" != .exe ]; then
        for zone in Etc/UTC Asia/Tokyo America/New_York Australia/Sydney \
            Asia/Manila America/Sitka America/Juneau Pacific/Guam Pacific/Kiritimati; do
            TZ="$zone" gate_capture_ok "$out/zone-$zone/assert-native-10" run_bounded "./$out/DateTimeOps$EXE_EXT" daylight-only
            native="$out/zone-$zone/assert-native-10.stdout"
            TZ="$zone" gate_capture_ok "$out/zone-$zone/assert-expected-11" run_bounded dotnet "$_CG_APP" daylight-only
            expected="$out/zone-$zone/assert-expected-11.stdout"
            gate_output compare "$native" "$expected"
        done
    fi
    gate_capture_ok "$out/dtfi-before" run_bounded "./$out/DateTimeOps$EXE_EXT" before-dtfi-names
    gate_output select "$out/native.stdout" "$out/dtfi-prefix.stdout" --before '== DateTimeFormatInfo names ==' --policy crlf
    gate_output compare "$out/dtfi-prefix.stdout" "$out/dtfi-before.stdout"
    gate_output witness "$out/native.stdout" 'DateTimeFormatInfo names end' --count 1 --policy crlf

    gate_capture_ok "$out/intercepts-before" run_bounded dotnet "$_CG_APP" before-datetime-intercepts
    gate_output select "$out/native.stdout" "$out/intercepts-prefix.stdout" \
        --before '== DateTimeFormatInfo intercepts ==' --policy crlf
    gate_output compare "$out/intercepts-prefix.stdout" "$out/intercepts-before.stdout"
    gate_output witness "$out/native.stdout" 'DateTimeFormatInfo intercepts end' 'DTFI helpers end' \
        'DateOnly deconstruct end' 'DateTime private arithmetic end' 'CultureInfo string validator end' --count 1 --policy crlf
    for zone in Etc/UTC Asia/Tokyo America/New_York; do
        TZ="$zone" gate_capture_ok "$out/zone-$zone/private-native" run_bounded "./$out/DateTimeOps$EXE_EXT" private-datetime-only
        TZ="$zone" gate_capture_ok "$out/zone-$zone/private-oracle" run_bounded dotnet "$_CG_APP" private-datetime-only
        gate_output compare "$out/zone-$zone/private-native.stdout" "$out/zone-$zone/private-oracle.stdout"
    done
    gate_capture_ok "$out/refusals-native" run_bounded "./$out/DateTimeOps$EXE_EXT" dtfi-refusals
    gate_output compare "$out/refusals-native.stdout" gates/expected/datetime-refusals.txt --policy crlf

    local isolated="$out/direct-only" method
    mkdir -p "$isolated/input"
    cp -R "$(dirname "$_CG_APP")/." "$isolated/input/"
    gate_capture_ok "$isolated/fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --datetime-direct-only "$isolated/input/DateTimeOps.dll" "$_CG_CORELIB"
    gate_output witness "$isolated/fixture.stdout" 'datetime direct-only roots=Deconstruct,TryAddTicks,TryCreate,.ctor' --count 1 --policy crlf
    gate_capture_ok "$isolated/cli" invoke_cli "$isolated/input/DateTimeOps.dll" -r "$_CG_CORELIB" -o "$isolated/out"
    for method in 'System.DateOnly::Deconstruct' 'System.DateTime::TryAddTicks' 'System.DateTime::TryCreate' 'System.DateTime::.ctor'; do
        grep -Fqx "// $method" "$isolated/out"/generated*.cpp \
            || { echo "FAIL: direct MemberRef did not retain $method" >&2; return 1; }
    done

    local public_only="$out/public-only"
    mkdir -p "$public_only/input"
    cp -R "$(dirname "$_CG_APP")/." "$public_only/input/"
    gate_capture_ok "$public_only/fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --datetime-public-only "$public_only/input/DateTimeOps.dll" "$_CG_CORELIB"
    gate_output witness "$public_only/fixture.stdout" \
        'datetime public-only root=System.Void System.DateTime::.ctor(System.Int32,System.Int32,System.Int32)' --count 1 --policy crlf
    gate_capture_ok "$public_only/cli" invoke_cli "$public_only/input/DateTimeOps.dll" --no-ildiet -o "$public_only/out"

    local negative="$out/refusal-tchar"
    mkdir -p "$negative/input"
    cp -R "$(dirname "$_CG_APP")/." "$negative/input/"
    gate_capture_ok "$negative/fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --datetime-tchar-negative "$negative/input/DateTimeOps.dll" "$_CG_CORELIB"
    gate_output witness "$negative/fixture.stdout" 'DateSeparatorTChar return-owner=System.Span`1' --count 1 --policy crlf
    gate_capture "$negative/cli" invoke_cli "$negative/input/DateTimeOps.dll" --auto-ref --no-ildiet -o "$negative/out"
    [ "$(cat "$negative/cli.status")" = 2 ] \
        && grep -Fq 'DateTimeFormatInfo TChar accessor requires its exact MethodSpec MemberRef signature' "$negative/cli.stdout" "$negative/cli.stderr" \
        || { echo 'FAIL: malformed DTFI TChar return owner was not refused' >&2; return 1; }
}

corelib_diff_gate DateTimeOps --link-xml samples/dotnet/DateTimeOps/keep-dtfi-intercepts.xml
