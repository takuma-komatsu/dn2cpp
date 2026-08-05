using System;

// Math.Abs/Sign/Clamp exception semantics matched to .NET: the signed
// integer Abs overloads throw OverflowException on MinValue (two's
// complement has no positive counterpart), Sign throws ArithmeticException
// on NaN (NaN has no sign), and Clamp throws ArgumentException when
// min > max. Every throw is caught as the base Exception and reported by
// runtime type name, so the assertions don't depend on the typed catch
// hierarchy. The non-throwing neighbours assert the regular paths survive.
namespace MathExceptionSemantics;

static class Program
{
    internal static void __GateEntry()
    {
        // Abs(MinValue) overflows at every signed integer width.
        try { Console.WriteLine(Math.Abs(sbyte.MinValue)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // OverflowException
        try { Console.WriteLine(Math.Abs(short.MinValue)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // OverflowException
        try { Console.WriteLine(Math.Abs(int.MinValue)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // OverflowException
        try { Console.WriteLine(Math.Abs(long.MinValue)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // OverflowException
        try { Console.WriteLine(Math.Abs(nint.MinValue)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // OverflowException

        // One step above MinValue stays on the regular path.
        Console.WriteLine(Math.Abs((sbyte)(sbyte.MinValue + 1)));  // 127
        Console.WriteLine(Math.Abs((short)(short.MinValue + 1)));  // 32767
        Console.WriteLine(Math.Abs(int.MinValue + 1));             // 2147483647
        Console.WriteLine(Math.Abs(long.MinValue + 1));            // 9223372036854775807
        Console.WriteLine(Math.Abs(-42));                          // 42
        Console.WriteLine(Math.Abs((sbyte)100));                   // 100

        // Sign(NaN): NaN has no sign — ArithmeticException.
        try { Console.WriteLine(Math.Sign(double.NaN)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArithmeticException
        try { Console.WriteLine(MathF.Sign(float.NaN)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArithmeticException

        // Regular Sign paths, including the signed zeros and infinities.
        Console.WriteLine(Math.Sign(-0.0));                    // 0
        Console.WriteLine(Math.Sign(double.NegativeInfinity)); // -1
        Console.WriteLine(Math.Sign(double.PositiveInfinity)); // 1
        Console.WriteLine(MathF.Sign(-2.5f));                  // -1
        Console.WriteLine(MathF.Sign(0.0f));                   // 0

        // Clamp with min > max — ArgumentException for both families.
        try { Console.WriteLine(Math.Clamp(1, 5, 2)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException
        try { Console.WriteLine(Math.Clamp(1.0, 5.0, 2.0)); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().Name); }  // ArgumentException

        // Regular Clamp paths: in range, degenerate min == max, NaN value.
        Console.WriteLine(Math.Clamp(7, 5, 10));                            // 7
        Console.WriteLine(Math.Clamp(7, 7, 7));                             // 7
        Console.WriteLine(Math.Clamp(0.5, 1.0, 2.0));                       // 1
        Console.WriteLine(double.IsNaN(Math.Clamp(double.NaN, 1.0, 2.0)));  // True
    }
}
