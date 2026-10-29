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
# Former gates: datetime, datetime-now, datetime-parse, datetime-format,
# datetime-tz, datetimeoffset.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_abbreviated-day.sh"
source "$(dirname "$0")/_abbreviated-month.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_abbreviated-day.sh gates/fixtures/abbreviated-day samples/dotnet/DateTimeOps/AbbreviatedDaySubset.cs gates/fixtures/ldftn-local/AbbreviatedDayFixture.cs gates/fixtures/ldftn-local/AbbreviatedDayGuards.cs gates/fixtures/ldftn-local/RuntimeTypeDefaultCtorFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/fixtures/full-day/FullDay.csproj gates/fixtures/full-day/minimal/MinimalFullDay.csproj gates/fixtures/full-day/link.xml samples/dotnet/DateTimeOps/FullDayPublicSubset.cs"
DN2CPP_GATE_EXTRA_INPUTS="$DN2CPP_GATE_EXTRA_INPUTS gates/_abbreviated-month.sh gates/fixtures/abbreviated-month samples/dotnet/DateTimeOps/AbbreviatedMonthSubset.cs samples/dotnet/DateTimeOps/AbbreviatedMonthPublicSubset.cs gates/fixtures/ldftn-local/AbbreviatedMonthFixture.cs gates/fixtures/ldftn-local/AbbreviatedMonthGuards.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|abbreviated-day:invariant,raw,diet,reflection-only,minimal,NoAlloc-rejection|full-day:invariant,raw,diet,reflection-only,minimal,NoAlloc-rejection|abbreviated-month:exact-int32-scalar-v1|abbreviated-month-negatives:${DN2CPP_ABBREVIATED_MONTH_NEGATIVES:-return byref address-return address-byref}|day-name-modes:abbreviated,full|abbreviated-day-negatives:${DN2CPP_ABBREVIATED_DAY_NEGATIVES:-return byref address-return address-byref}"

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
    gate_capture_ok "$out/day-before" run_bounded "./$out/DateTimeOps$EXE_EXT" before-abbreviated-day
    gate_capture_ok "$out/day-full" run_bounded "./$out/DateTimeOps$EXE_EXT"
    gate_output select "$out/day-full.stdout" "$out/day-prefix.stdout" --before '== Abbreviated invariant day names ==' --policy crlf
    gate_output compare "$out/day-prefix.stdout" "$out/day-before.stdout"
    gate_output witness "$out/day-full.stdout" '== Abbreviated invariant day names ==' 'abbreviated-day-array-copy-neighbor:True' 'abbreviated-day-separator-clone-neighbor:True' --count 1 --policy crlf
    abbreviated_day_focused_gate "$out/abbreviated-day"
    gate_capture_ok "$out/full-day-before" run_bounded "./$out/DateTimeOps$EXE_EXT" before-full-day
    gate_output select "$out/day-full.stdout" "$out/full-day-prefix.stdout" --before '== Full invariant day public neighbor ==' --policy crlf
    gate_output compare "$out/full-day-prefix.stdout" "$out/full-day-before.stdout"
    gate_output witness "$out/day-full.stdout" '== Full invariant day public neighbor ==' 'full-day-public-0:True' 'full-day-public-6:True' 'Full invariant day public neighbor end' --count 1 --policy crlf
    full_day_focused_gate "$out/full-day"
    gate_capture_ok "$out/month-before" run_bounded "./$out/DateTimeOps$EXE_EXT" before-abbreviated-month
    gate_output select "$out/day-full.stdout" "$out/month-prefix.stdout" --before '== Abbreviated invariant month public neighbor ==' --policy crlf
    gate_output compare "$out/month-prefix.stdout" "$out/month-before.stdout"
    gate_output witness "$out/day-full.stdout" '== Abbreviated invariant month public neighbor ==' 'abbreviated-month-public-1:True' 'abbreviated-month-public-13:True' 'abbreviated-month-public-empty:True' 'Abbreviated invariant month public neighbor end' --count 1 --policy crlf
    abbreviated_month_focused_gate "$out/abbreviated-month"

}

corelib_diff_gate DateTimeOps
