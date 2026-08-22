using System;
using System.Globalization;

namespace ZStringSample;

public class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        ZStringFormatSubset.Program.__GateEntry();
        ZStringBuilderSubset.Program.__GateEntry();
    }
}
