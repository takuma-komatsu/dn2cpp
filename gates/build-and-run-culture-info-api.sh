#!/usr/bin/env bash
# CultureInfo's object model — the parts that answer from per-culture METADATA
# rather than from the NumberFormatInfo separators the formatting gates cover.
# NativeName / DisplayName, GetCultures(CultureTypes), CultureInfo.GetFormat(Type)
# (the direct IFormatProvider implementation on CultureInfo — a plain call token,
# NOT the interface slot), and Calendar.
#
# THREE SECTIONS ARE HERE FOR THEIR SUBJECT, NOT FOR THE HEADING, and each would
# look prunable to a reader tidying this bucket by theme:
#
#   -- LCID -- is a two-directional test of CultureInfo.LCID and the
#      GetCultureInfo(int) reverse lookup over the runtime's culture table. Its
#      point is the THIRD answer: a culture the table does not carry reports 4096
#      (LOCALE_CUSTOM_UNSPECIFIED), the value real .NET reports for a culture it
#      has no LCID for, rather than the invariant 127 that was indistinguishable
#      from actually being invariant.
#
#   -- table breadth -- is a NUMBER-FORMATTING test, despite sitting under an
#      object-model heading. It asserts the rendering — not merely the Name — of
#      cultures that the six-entry table used to answer with invariant SYMBOLS
#      under a correct NAME. That failure was only ever visible as "a diff gate
#      red on one developer's machine", so a name-only assert would have passed
#      throughout. Every value is produced with an EXPLICIT culture, so the host
#      cannot move it. It is also the only place the table's CLDR-REVISED rows can
#      be asserted at all — ur-PK's 3-then-2 grouping and its zero currency decimal
#      digits, which the ICU a host ships answers differently from the ICU the table
#      was cut from, so no live-diff bucket can hold them.
#
#   -- CurrentUICulture -- is the only place in the corpus that can see the second
#      culture axis at all. Every bucket driver pins it as its second statement,
#      which makes a working setter and a discarded-argument no-op produce
#      identical output everywhere else; this section sets it explicitly, asserts
#      it is a slot INDEPENDENT of CurrentCulture (an aliased getter or setter
#      fails here and nowhere else), and restores the pin before the next section.
#
# Uses a frozen snapshot (gates/expected/culture-info-api.txt) rather than a live
# `dotnet $app` diff, for THREE deliberate divergences. Two are CultureInfo's own:
# dn2cpp constructs no Calendar object (the real getter reads intrinsic-mapped
# CultureInfo fields and a synthesized GregorianCalendar would pull the unmodeled
# CalendarData subtree), so it traps loudly (PlatformNotSupportedException) instead
# of returning a GregorianCalendar — the "a stripped type throws, never answers
# empty" discipline; and GetCultures / NativeName / DisplayName answer for the
# invariant culture alone, those being ICU inventory and display-name questions
# dn2cpp carries no table for. The third belongs to the escape sections below: the
# loud trap for SearchValues<T> prints a refusal where real .NET prints "ok"
# (Assembly/Module wrap instead, so their lines print the "ok" real .NET prints).
# Everything else is byte-identical against real .NET under en_US and de_DE.
# CoreLib only.
#
# Its LAST TWO sections are not about CultureInfo's API at all; do not prune this
# bucket by theme.
#   - CultureEscapeSubset: the object-escape wrap for the HEADERLESS intrinsic
#     representation (CultureInfo/NumberFormatInfo/TextInfo/IFormatProvider all
#     lower to `const Dn2CppNumberFormatInfo*`) at the mainstream boundaries.
#   - CultureEscapeResidueSubset: the boundaries whose IL spells no
#     conversion — a type-erased ldind.ref/stind.ref byref, a covariant method
#     group AND the delegate VARIANCE conversion (which emits no IL at all, so
#     f_method carries one NFI-erased ABI), a `constrained.` GetType in a generic
#     body, the erased interface slot of a NON-GENERIC implementer (which failed
#     silently, with an empty CultureInfo.Name), and the reflection Invoke thunk
#     shape. Its tail asserts the other headerless representations:
#     Assembly/Module WRAP (f-asm/f-mod print real .NET's "ok"; the deep asserts
#     are ReflectTypes' AssemblyIdentitySubset), while SearchValues<T> — whose
#     .NET identity is a private per-shape subclass with no fixed handle to
#     carry — keeps the LOUD TRAP.
source "$(dirname "$0")/_common.sh"

corelib_freeze_gate CultureInfoApi "$(dirname "$0")/expected/culture-info-api.txt"
