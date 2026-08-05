using System;

namespace GenericMathConvert;

// The generic-math conversion
// intrinsics INumberBase<T>.CreateTruncating<TOther> / CreateChecked<TOther> on
// concrete integer primitives. These reach selfhost-measure via real BCL code
// (Enum.GetNameInlined uses uint.CreateTruncating, Enum.AreSequentialFromZero
// uses ulong.CreateTruncating, etc.); on a concrete primitive they are a pure
// numeric cast (IL2CPP lowers them the same way). MethodCompiler.Translate-
// GenericIntrinsic lowers CreateTruncating to a wraparound cast and CreateChecked
// to a round-trip + sign bounds check that raises OverflowException out of range.
// This section calls them directly across narrow/widen and signed/unsigned
// pairings, covering the wraparound and the checked-overflow exception.
internal static class CreateConversions
{
    static string Checked(Func<object> f)
    {
        try { return f().ToString(); }
        catch (OverflowException) { return "OVF"; }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== CreateTruncating (wraparound / reinterpret) ==");
        // narrow + sign reinterpret
        Console.WriteLine($"uint<-int(-1) = {uint.CreateTruncating(-1)}");                      // 4294967295
        Console.WriteLine($"int<-uint(0xFFFFFFFF) = {int.CreateTruncating(0xFFFFFFFFu)}");       // -1
        // narrow from a wider source (drop high bits)
        Console.WriteLine($"uint<-long(0x1_0000_0001) = {uint.CreateTruncating(0x1_0000_0001L)}"); // 1
        Console.WriteLine($"int<-long(0x1_0000_0001) = {int.CreateTruncating(0x1_0000_0001L)}");    // 1
        // widen with sign / zero extension
        Console.WriteLine($"ulong<-int(-1) = {ulong.CreateTruncating(-1)}");                    // 18446744073709551615
        Console.WriteLine($"ulong<-long(-1) = {ulong.CreateTruncating(-1L)}");                  // 18446744073709551615
        Console.WriteLine($"long<-ulong(max) = {long.CreateTruncating(0xFFFF_FFFF_FFFF_FFFFUL)}"); // -1
        // sub-word targets (signed sign-extends, unsigned zero-extends back to stack)
        Console.WriteLine($"byte<-int(257) = {byte.CreateTruncating(257)}");                    // 1
        Console.WriteLine($"byte<-int(-1) = {byte.CreateTruncating(-1)}");                      // 255
        Console.WriteLine($"sbyte<-int(255) = {sbyte.CreateTruncating(255)}");                  // -1
        Console.WriteLine($"short<-int(0x1_0001) = {short.CreateTruncating(0x10001)}");         // 1
        Console.WriteLine($"ushort<-int(-1) = {ushort.CreateTruncating(-1)}");                  // 65535
        Console.WriteLine($"ushort<-long(0x1_FFFF) = {ushort.CreateTruncating(0x1FFFFL)}");     // 65535
        // identity
        Console.WriteLine($"uint<-uint(123) = {uint.CreateTruncating(123u)}");                  // 123

        Console.WriteLine("== CreateChecked (in range; OVF = OverflowException) ==");
        Console.WriteLine($"int<-long(100) = {Checked(() => int.CreateChecked(100L))}");            // 100
        Console.WriteLine($"int<-long(3e9) = {Checked(() => int.CreateChecked(3000000000L))}");     // OVF
        Console.WriteLine($"int<-uint(5) = {Checked(() => int.CreateChecked(5u))}");                // 5
        Console.WriteLine($"int<-uint(0xFFFFFFFF) = {Checked(() => int.CreateChecked(0xFFFFFFFFu))}"); // OVF
        Console.WriteLine($"uint<-int(7) = {Checked(() => uint.CreateChecked(7))}");                // 7
        Console.WriteLine($"uint<-int(-1) = {Checked(() => uint.CreateChecked(-1))}");              // OVF
        Console.WriteLine($"byte<-int(200) = {Checked(() => byte.CreateChecked(200))}");            // 200
        Console.WriteLine($"byte<-int(257) = {Checked(() => byte.CreateChecked(257))}");            // OVF
        Console.WriteLine($"short<-int(40000) = {Checked(() => short.CreateChecked(40000))}");      // OVF
        Console.WriteLine($"short<-int(-40000) = {Checked(() => short.CreateChecked(-40000))}");    // OVF
        Console.WriteLine($"long<-ulong(123) = {Checked(() => long.CreateChecked(123UL))}");        // 123
        Console.WriteLine($"long<-ulong(max) = {Checked(() => long.CreateChecked(0xFFFF_FFFF_FFFF_FFFFUL))}"); // OVF
        Console.WriteLine($"ulong<-long(-1) = {Checked(() => ulong.CreateChecked(-1L))}");          // OVF
        Console.WriteLine($"ulong<-long(123) = {Checked(() => ulong.CreateChecked(123L))}");        // 123

        // A FLOATING-POINT source into an integer target. .NET saturates (NOT wraps) for
        // both Truncating and Saturating: NaN -> 0, +Inf/over-max -> T.MaxValue, -Inf/
        // under-min -> T.MinValue (signed) or 0 (unsigned), else truncate toward zero.
        // MethodCompiler.TranslateGenericIntrinsic routes this arm to dn2cpp_convert_to_
        // integer_native (saturating) / dn2cpp_create_checked (checked) — the wraparound
        // cast the all-integer block uses would be undefined behaviour here.
        Console.WriteLine("== CreateTruncating (float source -> integer: saturating) ==");
        Console.WriteLine($"uint<-NaN = {uint.CreateTruncating(float.NaN)}");                    // 0
        Console.WriteLine($"uint<-+Inf = {uint.CreateTruncating(float.PositiveInfinity)}");      // 4294967295
        Console.WriteLine($"uint<-(-1.0f) = {uint.CreateTruncating(-1.0f)}");                    // 0
        Console.WriteLine($"uint<-3.9f = {uint.CreateTruncating(3.9f)}");                        // 3
        Console.WriteLine($"uint<-5e9(dbl) = {uint.CreateTruncating(5000000000.0)}");            // 4294967295
        Console.WriteLine($"int<-NaN(dbl) = {int.CreateTruncating(double.NaN)}");                // 0
        Console.WriteLine($"int<-3e9f = {int.CreateTruncating(3e9f)}");                          // 2147483647
        Console.WriteLine($"int<-(-3e9f) = {int.CreateTruncating(-3e9f)}");                      // -2147483648
        Console.WriteLine($"int<-(-1.9dbl) = {int.CreateTruncating(-1.9)}");                     // -1
        Console.WriteLine($"ulong<-+Inf(dbl) = {ulong.CreateTruncating(double.PositiveInfinity)}"); // 18446744073709551615
        Console.WriteLine($"ulong<-(-2.0dbl) = {ulong.CreateTruncating(-2.0)}");                 // 0
        Console.WriteLine($"ulong<-1.8e19 = {ulong.CreateTruncating(1.8e19)}");                  // 18000000000000000000
        Console.WriteLine($"long<--Inf(dbl) = {long.CreateTruncating(double.NegativeInfinity)}"); // -9223372036854775808
        Console.WriteLine($"byte<-300.0f = {byte.CreateTruncating(300.0f)}");                    // 255
        Console.WriteLine($"byte<-(-5.0f) = {byte.CreateTruncating(-5.0f)}");                    // 0
        Console.WriteLine($"sbyte<-200.0(dbl) = {sbyte.CreateTruncating(200.0)}");               // 127

        Console.WriteLine("== CreateSaturating / CreateChecked (float source) ==");
        Console.WriteLine($"uint.Sat<-(-3.0f) = {uint.CreateSaturating(-3.0f)}");                // 0
        Console.WriteLine($"int.Sat<-9e9(dbl) = {int.CreateSaturating(9e9)}");                   // 2147483647
        Console.WriteLine($"int.Chk<-3e9(dbl) = {Checked(() => int.CreateChecked(3e9))}");       // OVF
        Console.WriteLine($"int.Chk<-NaN(dbl) = {Checked(() => int.CreateChecked(double.NaN))}"); // OVF
        Console.WriteLine($"int.Chk<-100.0(dbl) = {Checked(() => int.CreateChecked(100.0))}");   // 100
    }
}
