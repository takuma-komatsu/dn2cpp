using System;
using System.Globalization;

internal static class Entry
{
#if UINT128_NOALLOC
    [Dn2Cpp.Runtime.HotPath(NoAlloc = true)]
    private static UInt128 Convert(long value) => UInt128.CreateChecked(value);
#endif
    public static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if UINT128_NOALLOC
        if (Convert(7) != (UInt128)7) throw new InvalidOperationException("NoAlloc positive");
        bool overflow = false;
        try { _ = Convert(-7); } catch (OverflowException) { overflow = true; }
        if (!overflow || Convert(9) != (UInt128)9) throw new InvalidOperationException("NoAlloc overflow");
        Console.WriteLine("uint128-checked-noalloc:True");
#elif UINT128_DISCARD_NEGATIVE
        bool overflow = false;
        try { _ = UInt128.CreateChecked<long>(-7); } catch (OverflowException) { overflow = true; }
        if (!overflow) throw new InvalidOperationException("discarded overflow missing");
        Console.WriteLine("uint128-checked-discard-negative:True");
#elif UINT128_DISCARD
        _ = UInt128.CreateChecked<long>(7);
#elif UINT128_ISOLATED
        UInt128CheckedSubset.Program.RunSelected();
#else
        UInt128CheckedSubset.Program.__GateEntry();
#endif
    }
}

#if UINT128_NOALLOC
namespace Dn2Cpp.Runtime
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        public bool SkipBoundsChecks { get; set; }
        public bool NoAlloc { get; set; }
    }
}
#endif
