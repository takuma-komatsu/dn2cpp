using System;
using System.Globalization;

namespace MathSubset
{
    // Consolidated gate driver: runs each math section's __GateEntry() in
    // order. Each section keeps its own namespace so its output and any
    // namespace-sensitive behavior stay identical to the original standalone
    // sample.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            MathIntMinMaxClamp.Program.__GateEntry();
            MathTrigSubset.Program.__GateEntry();
            MathRoundSubset.Program.__GateEntry();
            MathMinMaxSemantics.Program.__GateEntry();
            MathExceptionSemantics.Program.__GateEntry();
            IntDivideFaults.Program.__GateEntry();
            MathHyperbolicInverse.Program.__GateEntry();
            MathIeeeBits.Program.__GateEntry();
            MathRoundDigits.Program.__GateEntry();
            MathBigMul.Program.__GateEntry();
            MathEstimates.Program.__GateEntry();
            MathDecimalOps.Program.__GateEntry();
            MathFSweep.Program.__GateEntry();
            DoubleSingleStatics.Program.__GateEntry();
            DoubleSinglePiTrig.Program.__GateEntry();
            HalfBasics.Program.__GateEntry();
            HalfStatics.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-default-comparison")
                return;
            Console.WriteLine("== default comparison validation ==");
            MathGenericClampBounds.Program.Run();
            Console.WriteLine("default comparison validation end");
            if (args.Length > 0 && args[0] == "before-scalar-divrem")
                return;
            Dn2Cpp.ScalarDivRemSubset.Run();
        }
    }
}
