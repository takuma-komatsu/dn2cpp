using System;
using System.Globalization;

namespace ConvertParse
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ConvertSubset.Program.__GateEntry();
            ConvertBaseSubset.Program.__GateEntry();
            ConvertFromBaseMoreSubset.Program.__GateEntry();
            ConvertBase64Subset.Program.__GateEntry();
            ConvertHexSubset.Program.__GateEntry();
            ConvertObjectSubset.Program.__GateEntry();
            ConvertChangeTypeSubset.Program.__GateEntry();
            ConvertChangeTypeExtSubset.Program.__GateEntry();
            ConvertChangeTypeUInt64Subset.Program.__GateEntry();
            ParseSubset.Program.__GateEntry();
            NumberStylesSubset.Program.__GateEntry();
            SubWordOverloadSubset.Program.__GateEntry();
            ConvertNarrowSubset.Program.__GateEntry();
            ConvertDecimalDateTimeSubset.Program.__GateEntry();
            BitConverterSubset.Program.__GateEntry();
            BinaryPrimitivesSubset.Program.__GateEntry();
            UriBasicSubset.Program.__GateEntry();
            TypeConverterSrSubset.Program.__GateEntry();
            FloatParseSubset.Program.__GateEntry();
            FloatRoundtripSubset.Program.__GateEntry();
            RoundTripFormatSubset.Program.__GateEntry();
            ParseThrowSubset.Program.__GateEntry();
            ConvertT2Subset.Program.__GateEntry();
            // Integer-primitive TryFormat (+ the "B"/"b" binary specifier, the UTF-8
            // IUtf8SpanFormattable twin and the float overload), driven from its own
            // tail. It pins CurrentCulture for its own duration.
            TryFormatSubset.Program.__GateEntry();
            // Appended so the consolidated gate's prior output remains a prefix.
            FloatParseFormatInfoSubset.Program.__GateEntry();
        }
    }
}
