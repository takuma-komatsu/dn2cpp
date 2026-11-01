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

    // Char is outside the numeric and managed-value Number admission sets.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<char> First() => Vector128.MaxNumber(default(Vector128<char>), default);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<char> Second() => Vector128.MinNumber(default(Vector128<char>), default);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<char> TwoFailures()
    {
        var first = Vector128.MaxNumber(default(Vector128<char>), default);
        return Vector128.MinNumber(first, default);
    }
}
