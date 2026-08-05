// Port of c/common/platform.h (the BROTLI_MIN_MAX helpers only; the load/store
// and bit-manipulation macros map to Unsafe/BitOperations at their call sites,
// see PORTING.md).

using System.Runtime.CompilerServices;

namespace DnBrotli.Common;

/// <summary>
/// <c>BROTLI_MIN(T, A, B)</c> / <c>BROTLI_MAX(T, A, B)</c> from platform.h: the C macro
/// expands to a per-type <c>brotli_min_##T</c> / <c>brotli_max_##T</c> inline function whose
/// body is the plain ternary (<c>a &lt; b ? a : b</c>); the explicit type parameter becomes
/// C# overload resolution. The engine calls these instead of <see cref="Math.Min(nuint,nuint)"/>
/// et al. for two reasons: the ternary is the exact C semantics (std::fmin-style NaN handling
/// never enters the cost models), and the dn2cpp transpiler lowers the unsigned/native-sized
/// <c>Math.Min</c>/<c>Math.Max</c> overloads through its floating-point intrinsic
/// (<c>std::fmin</c>, an R8 stack slot — wrong kind at merge points and lossy above 2^53),
/// while these port as ordinary inlineable calls.
/// </summary>
internal static class Platform
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double BROTLI_MIN(double a, double b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double BROTLI_MAX(double a, double b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float BROTLI_MIN(float a, float b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float BROTLI_MAX(float a, float b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte BROTLI_MIN(byte a, byte b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte BROTLI_MAX(byte a, byte b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BROTLI_MIN(int a, int b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BROTLI_MAX(int a, int b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long BROTLI_MIN(long a, long b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long BROTLI_MAX(long a, long b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BROTLI_MIN(uint a, uint b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BROTLI_MAX(uint a, uint b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BROTLI_MIN(ulong a, ulong b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BROTLI_MAX(ulong a, ulong b) => a > b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BROTLI_MIN(nuint a, nuint b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static nuint BROTLI_MAX(nuint a, nuint b) => a > b ? a : b;
}
