#if BOOLEAN_CONVERTIBLE_ONLY
using System.Globalization;

internal static class BooleanConvertibleOnlyProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        ConstrainedObjectInterfaceSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-boolean-receivers")
            return;
        ConstrainedObjectInterfaceSubset.Program.RunBooleanReceivers();
    }
}
#endif
