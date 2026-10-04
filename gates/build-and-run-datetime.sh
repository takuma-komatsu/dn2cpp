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
# FromBinary/ToBinary cover kind flags, local-zone round trips and invalid payloads.
# Former gates: datetime, datetime-now, datetime-parse, datetime-format,
# datetime-tz, datetimeoffset.
source "$(dirname "$0")/_common.sh"

# Constructors, arithmetic, offset parsing and microseconds preserve fault fields.
gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/DateTimeOps$EXE_EXT")
    native=$(strip_cr_win "$native")
    before=$(run_bounded "./$out/DateTimeOps$EXE_EXT" before-date-validation)
    prefix=$(awk '/^== datetime argument fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== datetime argument fields ==' 'datetime constructor order end' \
        'datetime argument fields end' '== datetime offset boundaries ==' \
        'datetime offset boundaries end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: date validation witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded "./$out/DateTimeOps$EXE_EXT" before-date-binary)
    prefix=$(awk '/^== datetime binary ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== datetime binary ==' 'datetime binary end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: date binary witness missing: $line" >&2; exit 1; }
    done
    local zone expected
    for zone in Etc/UTC Asia/Tokyo America/New_York; do
        native=$(TZ="$zone" run_bounded "./$out/DateTimeOps$EXE_EXT" binary-only)
        expected=$(TZ="$zone" run_bounded dotnet "$_CG_APP" binary-only)
        assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$expected")"
    done
    before=$(run_bounded dotnet "$_CG_APP" before-date-exact-spans)
    native=$(run_bounded "./$out/DateTimeOps$EXE_EXT")
    native=$(strip_cr_win "$native")
    prefix=$(awk '/^== datetime exact spans ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'datetime exact spans end' <<< "$native" \
        || { echo "FAIL: exact date span parsing did not run" >&2; exit 1; }
    before=$(run_bounded dotnet "$_CG_APP" before-date-utf8)
    prefix=$(awk '/^== UTF-8 date destinations ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'UTF-8 date destinations end' <<< "$native" \
        || { echo "FAIL: UTF-8 date formatting did not run" >&2; exit 1; }
}

corelib_diff_gate DateTimeOps
