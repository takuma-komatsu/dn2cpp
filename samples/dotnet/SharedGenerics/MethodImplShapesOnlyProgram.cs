#if METHODIMPL_SHAPES_ONLY
using System.Globalization;

internal static class MethodImplShapesOnlyProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        MethodImplShapeSubset.Program.Run();
    }
}
#endif
