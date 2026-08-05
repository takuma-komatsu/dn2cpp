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
# Former gates: datetime, datetime-now, datetime-parse, datetime-format,
# datetime-tz, datetimeoffset.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate DateTimeOps
