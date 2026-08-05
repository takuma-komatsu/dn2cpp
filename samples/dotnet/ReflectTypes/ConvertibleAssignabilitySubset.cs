#nullable disable
using System;
using System.Globalization;

// Well-known-interface assignability over the row-less type-infos — one rule with
// isinst (dn2cpp_typeinfo_assignable's dn2cpp_wellknown_itf_bit/_mask fallback).
// The hand-written primitive runtime type-infos (dn2cpp_int64_type etc.), the
// string/decimal/date-time handles and the emitted per-enum type-infos carry no
// interface rows, so `typeof(IConvertible).IsAssignableFrom(typeof(long))` — and
// the same ask for an enum — answered False where real .NET answers True. That
// pair-gate is exactly Newtonsoft's ConvertUtils.TryConvertInternal enum
// conversion guard, so a Thrive `long -> Stage` JSON integer fell past the
// (intercepted, working) Enum.ToObject branch into TypeDescriptor and threw
// ("Could not cast or convert from System.Int64 to Stage", organelles.json
// RestrictedToStages). This section pins the oracle-measured (net10.0) fallback
// table on both surfaces of the one rule — IsAssignableFrom on the Type pair and
// `is` on a boxed value — including the shapes that keep the table honest:
//   - bool and string implement IConvertible+IComparable but NOT IFormattable;
//   - IntPtr implements IComparable/IFormattable but NOT IConvertible;
//   - a plain struct / class implements none of the four.
// The generic IComparable<T>/IEquatable<T> are a separate pair predicate over the
// same table — the verdict depends on the instantiation's ARGUMENT, not on the
// interface's identity — and are asserted by the BoxingPrimitives bucket's
// BoxedBuiltinItfDispatchSubset, beside the dispatch they must agree with.
// The TryConvertInternal transcription at the end drives the rule through a real
// converter shape: the IsConvertible pair-gate passes, IsInteger routes the boxed
// long into Enum.ToObject, and the typed enum value comes back out. Every line
// matches real .NET.

namespace ConvertibleAssignabilitySubset
{
    enum Stage
    {
        Microbe = 0,
        Multicellular = 1,
        Aware = 2,
    }

    enum WideStage : long
    {
        A = 0,
        B = 1,
    }

    struct PlainStruct
    {
        public int X;
    }

    class PlainClass
    {
    }

    static class Program
    {
        static void Bl(string label, bool v) => Console.WriteLine(label + " = " + v);

        // Newtonsoft ConvertUtils.IsConvertible, verbatim.
        static bool IsConvertible(Type t) => typeof(IConvertible).IsAssignableFrom(t);

        // Newtonsoft ConvertUtils.IsInteger's decision (TypeCode-keyed).
        static bool IsInteger(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        // Newtonsoft ConvertUtils.TryConvertInternal's convertible branch — the
        // organelles.json RestrictedToStages path. Returns a tag + value so the
        // gate output names which branch answered.
        static string ConvertLikeNewtonsoft(object initialValue, Type targetType)
        {
            Type initialType = initialValue.GetType();
            if (targetType == initialType)
                return "identity:" + initialValue;
            if (IsConvertible(initialType) && IsConvertible(targetType))
            {
                if (targetType.IsEnum)
                {
                    if (initialValue is string s)
                        return "enum-parse:" + Enum.Parse(targetType, s, ignoreCase: true);
                    if (IsInteger(initialValue))
                        return "enum-toobject:" + Enum.ToObject(targetType, initialValue);
                }
                return "changetype:" + Convert.ChangeType(initialValue, targetType, CultureInfo.InvariantCulture);
            }
            return "no-conversion";
        }

        internal static void Run()
        {
            // -- IsAssignableFrom over the Type pair (the ConvertUtils gate). --
            Bl("IConvertible <- long", typeof(IConvertible).IsAssignableFrom(typeof(long)));
            Bl("IConvertible <- int", typeof(IConvertible).IsAssignableFrom(typeof(int)));
            Bl("IConvertible <- byte", typeof(IConvertible).IsAssignableFrom(typeof(byte)));
            Bl("IConvertible <- double", typeof(IConvertible).IsAssignableFrom(typeof(double)));
            Bl("IConvertible <- bool", typeof(IConvertible).IsAssignableFrom(typeof(bool)));
            Bl("IConvertible <- char", typeof(IConvertible).IsAssignableFrom(typeof(char)));
            Bl("IConvertible <- string", typeof(IConvertible).IsAssignableFrom(typeof(string)));
            Bl("IConvertible <- decimal", typeof(IConvertible).IsAssignableFrom(typeof(decimal)));
            Bl("IConvertible <- DateTime", typeof(IConvertible).IsAssignableFrom(typeof(DateTime)));
            Bl("IConvertible <- Stage", typeof(IConvertible).IsAssignableFrom(typeof(Stage)));
            Bl("IConvertible <- WideStage", typeof(IConvertible).IsAssignableFrom(typeof(WideStage)));
            Bl("IConvertible <- Enum", typeof(IConvertible).IsAssignableFrom(typeof(Enum)));
            // Negatives the oracle names: nint has everything BUT IConvertible;
            // TimeSpan likewise; a plain struct/class has none of the four.
            Bl("IConvertible <- nint", typeof(IConvertible).IsAssignableFrom(typeof(nint)));
            Bl("IConvertible <- TimeSpan", typeof(IConvertible).IsAssignableFrom(typeof(TimeSpan)));
            Bl("IConvertible <- PlainStruct", typeof(IConvertible).IsAssignableFrom(typeof(PlainStruct)));
            Bl("IConvertible <- PlainClass", typeof(IConvertible).IsAssignableFrom(typeof(PlainClass)));
            Bl("IConvertible <- object", typeof(IConvertible).IsAssignableFrom(typeof(object)));

            Bl("IComparable <- long", typeof(IComparable).IsAssignableFrom(typeof(long)));
            Bl("IComparable <- bool", typeof(IComparable).IsAssignableFrom(typeof(bool)));
            Bl("IComparable <- string", typeof(IComparable).IsAssignableFrom(typeof(string)));
            Bl("IComparable <- Stage", typeof(IComparable).IsAssignableFrom(typeof(Stage)));
            Bl("IComparable <- nint", typeof(IComparable).IsAssignableFrom(typeof(nint)));
            Bl("IComparable <- TimeSpan", typeof(IComparable).IsAssignableFrom(typeof(TimeSpan)));
            Bl("IComparable <- PlainStruct", typeof(IComparable).IsAssignableFrom(typeof(PlainStruct)));

            // bool/string do NOT implement IFormattable; the numerics and enums do.
            Bl("IFormattable <- long", typeof(IFormattable).IsAssignableFrom(typeof(long)));
            Bl("IFormattable <- bool", typeof(IFormattable).IsAssignableFrom(typeof(bool)));
            Bl("IFormattable <- string", typeof(IFormattable).IsAssignableFrom(typeof(string)));
            Bl("IFormattable <- Stage", typeof(IFormattable).IsAssignableFrom(typeof(Stage)));
            Bl("ISpanFormattable <- long", typeof(ISpanFormattable).IsAssignableFrom(typeof(long)));
            Bl("ISpanFormattable <- bool", typeof(ISpanFormattable).IsAssignableFrom(typeof(bool)));
            Bl("ISpanFormattable <- Stage", typeof(ISpanFormattable).IsAssignableFrom(typeof(Stage)));

            // -- The boxed side of the ONE rule: `is` on an instance. --
            Bl("(object)1L is IConvertible", (object)1L is IConvertible);
            Bl("(object)1 is IConvertible", (object)1 is IConvertible);
            Bl("(object)Stage.Aware is IConvertible", (object)Stage.Aware is IConvertible);
            Bl("(object)true is IConvertible", (object)true is IConvertible);
            Bl("(object)\"x\" is IConvertible", (object)"x" is IConvertible);
            Bl("(object)true is IFormattable", (object)true is IFormattable);
            Bl("(object)1L is IFormattable", (object)1L is IFormattable);
            Bl("(object)Stage.Aware is IComparable", (object)Stage.Aware is IComparable);
            Bl("(object)default(PlainStruct) is IConvertible", (object)default(PlainStruct) is IConvertible);
            // The type test widening must not leak into unrelated pairs.
            Bl("(object)1L is Stage", (object)1L is Stage);
            Bl("Stage.IsInstanceOfType(0L)", typeof(Stage).IsInstanceOfType((object)0L));

            // -- The Thrive flow: TryConvertInternal over a JSON integer. --
            Console.WriteLine("long->Stage: " + ConvertLikeNewtonsoft((object)2L, typeof(Stage)));
            Console.WriteLine("long->WideStage: " + ConvertLikeNewtonsoft((object)1L, typeof(WideStage)));
            Console.WriteLine("string->Stage: " + ConvertLikeNewtonsoft("multicellular", typeof(Stage)));
            Console.WriteLine("long->int: " + ConvertLikeNewtonsoft((object)7L, typeof(int)));
            Console.WriteLine("struct->Stage: " + ConvertLikeNewtonsoft(default(PlainStruct), typeof(Stage)));
        }
    }
}
