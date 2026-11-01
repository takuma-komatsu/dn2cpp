using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal static class GapFixture
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _ = First();
        _ = Second();
        _ = TwoFailures();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<decimal> First() => Vector128.MaxNumber(default(Vector128<decimal>), default);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<decimal> Second() => Vector128.MinNumber(default(Vector128<decimal>), default);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<decimal> TwoFailures()
    {
        var first = Vector128.MaxNumber(default(Vector128<decimal>), default);
        return Vector128.MinNumber(first, default);
    }
}
