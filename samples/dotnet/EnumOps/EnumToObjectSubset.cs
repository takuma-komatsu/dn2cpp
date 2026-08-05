#nullable disable
using System;

namespace EnumToObjectSubset
{
    internal enum IntE
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Big = 1000,
    }

    internal enum ByteE : byte
    {
        A = 1,
        B = 2,
        C = 44,
    }

    internal enum SByteE : sbyte
    {
        M = -56,
        P = 5,
    }

    internal enum LongE : long
    {
        X = 5_000_000_000,
        Y = -3,
    }

    internal enum ULongE : ulong
    {
        One = 1,
        Max = ulong.MaxValue,
    }

    // Non-generic Enum.ToObject(Type, …) — the reflection path's boxing bridge
    // (Newtonsoft's EnumUtils.ParseEnum ends in exactly this call; Thrive's
    // SimulationParameters.LoadRegistry reaches it with a runtime enum Type).
    // Diffed EXACTLY vs real .NET: all eight integral overloads, the object
    // overload's Convert.GetTypeCode dispatch (boxed integrals/char/bool/enums
    // accepted, float/string rejected), truncation to the underlying width with
    // sign/zero re-extension (300 -> (ByteE)44, 200 -> (SByteE)-56, -1 ->
    // ULongE.Max), 64-bit underlyings at full width, undefined values passing
    // through, and the failure contract — a non-enum type or a non-integral
    // boxed value throws ArgumentException, a null type or value
    // ArgumentNullException (only the exception type is asserted; dn2cpp does
    // not reproduce .NET's message text).
    internal static class Program
    {
        private static void Show(string label, Func<object> f)
        {
            try
            {
                object o = f();
                Console.WriteLine(label + ": " + o + " (" + o.GetType().Name + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
            }
        }

        internal static void __GateEntry()
        {
            // The eight integral overloads, classified by argument TYPE.
            Show("int", () => Enum.ToObject(typeof(IntE), 2));
            Show("byte-arg", () => Enum.ToObject(typeof(IntE), (byte)1));
            Show("sbyte-arg", () => Enum.ToObject(typeof(IntE), (sbyte)-1));
            Show("short-arg", () => Enum.ToObject(typeof(IntE), (short)1000));
            Show("ushort-arg", () => Enum.ToObject(typeof(IntE), (ushort)2));
            Show("uint-arg", () => Enum.ToObject(typeof(IntE), 2u));
            Show("long-arg", () => Enum.ToObject(typeof(IntE), 2L));
            Show("ulong-arg", () => Enum.ToObject(typeof(IntE), 2UL));

            // Truncation to the underlying width, then sign/zero re-extension.
            Show("trunc-byte", () => Enum.ToObject(typeof(ByteE), 300));   // -> (ByteE)44
            Show("trunc-sbyte", () => Enum.ToObject(typeof(SByteE), 200)); // -> (SByteE)-56
            Show("neg-to-ulong", () => Enum.ToObject(typeof(ULongE), -1)); // -> Max

            // 64-bit underlyings carry the full width.
            Show("long-e", () => Enum.ToObject(typeof(LongE), 5_000_000_000L));
            Show("long-neg", () => Enum.ToObject(typeof(LongE), -3));
            // A uint operand ZERO-extends into a 64-bit underlying …
            Show("uint-wide-to-long", () => Enum.ToObject(typeof(LongE), 0x80000000u));
            // … an int operand sign-extends.
            Show("int-neg-to-long", () => (long)(LongE)Enum.ToObject(typeof(LongE), -3));

            // An undefined value passes through (and unboxes to its payload).
            Show("undef", () => Enum.ToObject(typeof(IntE), 42));
            Show("undef-int", () => (int)(IntE)Enum.ToObject(typeof(IntE), 42));

            // The object overload: boxed integrals, char, bool, and boxed enums
            // (read at their underlying) are accepted …
            Show("obj-int", () => Enum.ToObject(typeof(IntE), (object)1));
            Show("obj-boxed-enum", () => Enum.ToObject(typeof(IntE), (object)ByteE.B));
            Show("obj-boxed-same", () => Enum.ToObject(typeof(IntE), (object)IntE.Big));
            Show("obj-bool", () => Enum.ToObject(typeof(IntE), (object)true));
            Show("obj-char", () => Enum.ToObject(typeof(IntE), (object)'A'));
            // … everything else throws ArgumentException, null ArgumentNullException.
            Show("obj-string", () => Enum.ToObject(typeof(IntE), (object)"1"));
            Show("obj-float", () => Enum.ToObject(typeof(IntE), (object)1.0f));
            Show("obj-null", () => Enum.ToObject(typeof(IntE), (object)null));

            // The type contract: non-enum ArgumentException, null ArgumentNullException.
            Show("non-enum", () => Enum.ToObject(typeof(string), 1));
            Show("null-type", () => Enum.ToObject(null, 1));
        }
    }
}
