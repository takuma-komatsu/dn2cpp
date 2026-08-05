#nullable disable
using System;
using System.Globalization;

namespace SampleSupport
{
    // Shared gate-sample support: a scoped CultureInfo.CurrentCulture change.
    //
    // Why it exists. A consolidated bucket is one process running N sections in
    // order, and CurrentCulture is process-global, so a section that changes it
    // changes the ORACLE's answer for every section after it — the native binary
    // is invariant unconditionally, only the real-.NET side moves. Hand-written
    // save/restore around such a section is fragile in the usual way (an early
    // return or a throw skips the restore) and, worse, fails SILENTLY on the
    // hosts anybody develops on: ja-JP and en-US agree with invariant on the
    // specifiers these buckets print, so a leaked pin would go red only on
    // somebody's de-DE machine.
    //
    // The two contracts are deliberately separate, because the two sections that
    // need them want different postconditions and one of them is not a
    // save/restore at all:
    //
    //   Use(enter)                    — set `enter`, put back whatever was
    //                                   current on the way out. The save/restore
    //                                   shape: for a section that borrows a
    //                                   culture and owes the bucket its ambient
    //                                   one back (ConvertParse/TryFormatSubset).
    //   Use(enter, restoreTo: exit)   — set `enter`, leave `exit` on the way out,
    //                                   whatever was current before. For a
    //                                   section whose postcondition is a FIXED
    //                                   culture rather than the previous one
    //                                   (RegexCore/RegexIgnoreCaseCultureSubset,
    //                                   which re-pins invariant so the eight
    //                                   sections after it stay host-independent).
    //                                   Restoring the previous culture there
    //                                   would hand those eight the HOST's culture
    //                                   — the exact hazard this type exists for.
    //
    // There is deliberately no `Use(string)` convenience: RegexCore's section
    // asserts both `new CultureInfo(name)` and `CultureInfo.GetCultureInfo(name)`
    // as distinct construction paths, and an overload taking a name would have to
    // pick one and hide the distinction at the call site.
    //
    // A struct, so a scope costs no allocation in a fixture whose emitted C++ is
    // itself under test; `using` over a concrete struct binds Dispose directly.
    internal readonly struct CultureScope : IDisposable
    {
        private readonly CultureInfo _exit;

        private CultureScope(CultureInfo exit)
        {
            _exit = exit;
        }

        internal static CultureScope Use(CultureInfo enter)
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = enter;
            return new CultureScope(previous);
        }

        internal static CultureScope Use(CultureInfo enter, CultureInfo restoreTo)
        {
            CultureInfo.CurrentCulture = enter;
            return new CultureScope(restoreTo);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _exit;
        }
    }
}
