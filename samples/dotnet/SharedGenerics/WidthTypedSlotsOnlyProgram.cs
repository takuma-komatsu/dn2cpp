#if WIDTH_TYPED_SLOTS_ONLY
using System.Globalization;

internal static class WidthTypedSlotsOnlyProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        WidthJoinSubset.Program.RunTypedSlots();
    }
}
#endif
