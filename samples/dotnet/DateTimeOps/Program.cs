using System;
using System.Globalization;

namespace DateTimeOps
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            if (args.Length > 0 && args[0] == "binary-only")
            {
                DateTimeBoundaryParitySubset.Program.RunBinary();
                return;
            }

            DateTimeSubset.Program.__GateEntry();
            DateTimeNow.Program.__GateEntry();
            DateTimeParse.Program.__GateEntry();
            DateTimeFormat.Program.__GateEntry();
            DateTimeTz.Program.__GateEntry();
            DateTimeTzInfo.Program.__GateEntry();
            DateTimeOffsetSubset.Program.__GateEntry();
            DateOnlySubset.Program.__GateEntry();
            TimeOnlySubset.Program.__GateEntry();
            TryFormatDatesSubset.Program.__GateEntry();
            TimeSpanConstrained.Program.__GateEntry();
            DateTimeThrowSubset.Program.__GateEntry();
            TzSerializedString.Program.__GateEntry();
            ParseExactDotElision.Program.__GateEntry();
            if (args.Length > 0 && args[0] == "before-date-validation")
                return;
            DateTimeArgumentFieldsSubset.Program.Run();
            DateTimeBoundaryParitySubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-date-binary")
                return;
            DateTimeBoundaryParitySubset.Program.RunBinary();
            if (args.Length > 0 && args[0] == "before-date-exact-spans")
                return;
            DateTimeBoundaryParitySubset.Program.RunExactSpans();
            if (args.Length > 0 && args[0] == "before-date-utf8")
                return;
            TryFormatDatesSubset.Program.RunUtf8();
        }
    }
}
