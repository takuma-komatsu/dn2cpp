using System;
using System.Globalization;

namespace BoxingPrimitives
{
    // Consolidated gate driver: boxing a PRIMITIVE value type — what `box` /
    // `unbox.any` do to a type the runtime models as an intrinsic rather than as an
    // emitted class, and what the Object virtuals recover from the resulting handle.
    // Each section keeps its own namespace so any name-sensitive output stays
    // identical to the standalone gate it came from; each is run in order below.
    //
    // Three of the five assert something other than "boxing works", and the header is
    // the only place that says so — prune this bucket by theme and you delete them:
    //   * BoxedComparableSubset's tail is a DEVIRTUALIZATION test (the unconstrained
    //     `(IComparable<T>)box.CompareTo` form, and the Comparer<T>.Default ordering
    //     real .NET routes through ObjectComparer<T>), ending in the one case that
    //     must throw on both sides — Vector128<T>, which is not orderable.
    //   * PrimitiveCompareHashSubset is an EMIT-ONLY intercept test (Int32/UInt32 are
    //     already intrinsic types, so reachability was already cutting the BCL
    //     bodies): int/uint CompareTo returning the SIGN rather than the difference,
    //     and the int/long/ulong/IntPtr GetHashCode folds. It is exercised through
    //     comparer lambdas as well as direct calls, because that is the shape
    //     dn2cpp's own catch-clause and flag-enum sorts reach it in.
    //   * BoxedBuiltinItfDispatchSubset is a RUNTIME ITABLE test: the
    //     boxed-built-in slot tables dn2cpp_resolve_interface_walk's last arm hands
    //     back, asserted against the type TEST that admits the same pairs. It also
    //     pins dn2cpp_object_compare's Decimal / date-time ordering arms, which
    //     nothing else in the suite reaches.
    //   * MouthAgreementSubset is a MOUTH-AGREEMENT test over three surfaces
    //     that are not boxing at heart: a boxed enum's cut ISpanFormattable.TryFormat
    //     against its IFormattable text, the self-instantiated IComparable<T>/
    //     IEquatable<T> type test against the dispatch it must admit, and the date
    //     family's format specifier through all four of its mouths (interpolation
    //     hole, string.Format hole, IFormattable, ISpanFormattable). It is also the
    //     only coverage of the runtime's 64-bit enum member table.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Order is load-bearing: it is the order the sections' output blocks are
            // asserted to appear in.
            BoxPrimitiveSubset.Program.Run();
            IntPtrBox.Program.Run();
            BoxedComparableSubset.Program.Run();
            PrimitiveCompareHashSubset.Program.Run();
            BoxedBuiltinItfDispatchSubset.Program.Run();
            MouthAgreementSubset.Program.Run();
        }
    }
}
