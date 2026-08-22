using System;
using System.Globalization;

namespace DecimalOps
{
    // Consolidated gate driver: System.Decimal as an intrinsic value type backed by
    // the runtime's 96-bit Dn2CppDecimal. Each section keeps its own namespace so
    // any name-sensitive output stays identical to the standalone gate it came from;
    // each is run in order below.
    //
    // The two sections are the two halves of one story: DecimalSubset drives the
    // STATICALLY TYPED surface (the call sites the transpiler lowers to
    // dn2cpp_decimal_* helpers, because the real corelib Decimal.ToString reaches
    // Number.FormatDecimal -> ArrayPool -> EventSource -> calli), and
    // BoxedDecimalSubset drives the same value once it has been BOXED, where there
    // is no call site to lower and the Object virtuals have to recover the payload
    // from the shared dn2cpp_decimal_type handle instead.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Order is load-bearing: it is the order the sections' output blocks are
            // asserted to appear in.
            DecimalSubset.Program.Run();
            BoxedDecimalSubset.Program.Run();
            NegativeZeroSubset.Program.Run();
        }
    }
}
