using System;

namespace IntPtrBox
{
    // box / unbox.any of IntPtr/UIntPtr as a primitive. Previously
    // `box of System.IntPtr is not supported` (TypeInfoExpr's Primitive switch
    // lacked IntPtr/UIntPtr). Each boxes to object, ToStrings (signed for IntPtr,
    // unsigned for UIntPtr) via Object.ToString / boxed virtual ToString / value
    // concat / interpolation holes, unbox-round-trips, and `is` discriminates the
    // two pointer types. App-module code only (no BCL cascade). Diffed exact vs
    // real.NET.
    internal static class Program
    {
        internal static void Run()
        {
            // --- GetType ---
            object o = (IntPtr)42;
            Console.WriteLine(o.GetType().FullName);     // System.IntPtr
            object ou = (UIntPtr)42;
            Console.WriteLine(ou.GetType().FullName);    // System.UIntPtr

            // --- ToString (statically typed, value type) ---
            Console.WriteLine(((IntPtr)42).ToString());   // 42
            Console.WriteLine(((IntPtr)(-5)).ToString());  // -5 (signed)
            Console.WriteLine(((UIntPtr)42).ToString());   // 42 (unsigned)

            // --- boxed virtual ToString (Object.ToString on a boxed value) ---
            Console.WriteLine(o.ToString());              // 42
            object on = (IntPtr)(-5);
            Console.WriteLine(on.ToString());             // -5
            Console.WriteLine(ou.ToString());             // 42

            // --- value concat (Concat(object) -> Object.ToString) ---
            Console.WriteLine("p=" + o);                  // p=42
            Console.WriteLine("n=" + on);                 // n=-5
            Console.WriteLine("u=" + ou);                 // u=42

            // --- unbox.any round trip ---
            Console.WriteLine(o is IntPtr);               // True
            IntPtr backp = (IntPtr)o;
            Console.WriteLine(backp == (IntPtr)42);        // True
            UIntPtr backu = (UIntPtr)ou;
            Console.WriteLine(backu == (UIntPtr)42);       // True

            // --- `is` discriminates the two pointer types ---
            Console.WriteLine(o is UIntPtr);              // False
            Console.WriteLine(ou is IntPtr);              // False

            // --- interpolation holes (no spec; direct and via object) ---
            Console.WriteLine($"{(IntPtr)42}");           // 42
            Console.WriteLine($"{(IntPtr)(-5)}");          // -5
            Console.WriteLine($"{(UIntPtr)42}");           // 42
            Console.WriteLine($"{(object)(IntPtr)42}");    // 42
            Console.WriteLine($"{(object)(UIntPtr)42}");   // 42

            // --- 8-byte boundary values (> int range) ---
            IntPtr big = (IntPtr)9000000000L;
            Console.WriteLine(big.ToString());            // 9000000000
            object obig = big;
            Console.WriteLine(obig.ToString());           // 9000000000
            UIntPtr ubig = (UIntPtr)18000000000UL;
            Console.WriteLine(ubig.ToString());           // 18000000000
            object oubig = ubig;
            Console.WriteLine(oubig.ToString());          // 18000000000

            // --- boxed Equals / GetHashCode ---
            object a1 = (IntPtr)42;
            object a2 = (IntPtr)42;
            Console.WriteLine(a1.Equals(a2));             // True (same type + value)
            Console.WriteLine(a1.GetHashCode() == a2.GetHashCode()); // True
            object u1 = (UIntPtr)42;
            Console.WriteLine(a1.Equals(u1));             // False (IntPtr != UIntPtr)
        }
    }
}
