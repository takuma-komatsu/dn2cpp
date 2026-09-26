using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

// A custom attribute argument typed object or object[] carries its value's own
// encoded type — a primitive, string, enum, Type or an array of those, or null —
// and GetCustomAttributes materializes the attribute with the value boxed at that
// type, positional and named alike; a named argument stores at its member's
// declared type, and a 64-bit enum keeps its high bits. CustomAttributeData.ToString
// spells a boxed value with its encoded type and lists named arguments fields
// first, then properties. A Type argument or an enum's type may name a nested
// type, an array or a closed generic, which decodes to the same type identity
// typeof names; CustomAttributeData.ToString spells a closed generic's type
// arguments assembly-qualified.
namespace ReflectAttrBoxedSubset
{
    public enum Tone { Low = 1, High = 7 }
    public enum Small : byte { A = 3, B = 250 }
    public enum Big : long { X = 1L << 40 }
    public enum UBig : ulong { Max = ulong.MaxValue }

    public class Outer
    {
        public enum Mode : byte { Off, On = 200 }
        public class Inner { }
    }

    public class Box<T> { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class BoxedAttribute : Attribute
    {
        public BoxedAttribute(object value) => Value = value;
        public object Value;
        public object Named { get; set; }
        public object NamedField;
        public object[] NamedArray { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ListAttribute : Attribute
    {
        public ListAttribute(object[] values) => Values = values;
        public object[] Values;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ParamsAttribute : Attribute
    {
        public ParamsAttribute(params object[] values) => Values = values;
        public object[] Values;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TonesAttribute : Attribute
    {
        public TonesAttribute(Tone[] tones) => Tones = tones;
        public Tone[] Tones;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TypedAttribute : Attribute
    {
        public TypedAttribute(Big big, UBig ubig, char letter, Type type)
        {
            BigValue = big;
            UBigValue = ubig;
            Letter = letter;
            Type = type;
        }
        public Big BigValue;
        public UBig UBigValue;
        public char Letter;
        public Type Type;
        public Outer.Mode Mode { get; set; }
        public int Order;
    }

    [Boxed(5)] [Boxed("text")] [Boxed(Tone.High)] [Boxed(Small.B)] [Boxed(Big.X)] [Boxed(UBig.Max)]
    public sealed class Scalars { }

    [Boxed(null)] [Boxed('c')] [Boxed(true)] [Boxed(1.5)] [Boxed(2.5f)] [Boxed(9L)] [Boxed(9UL)]
    [Boxed((byte)200)] [Boxed((sbyte)-3)] [Boxed((short)-4)] [Boxed((ushort)65000)] [Boxed(4000000000U)]
    [Boxed('\'')]
    public sealed class Primitives { }

    [Boxed(new int[] { 1, 2 })] [Boxed(new string[] { "a", null })] [Boxed(new Tone[] { Tone.Low, Tone.High })]
    [Boxed(new Type[] { typeof(string), null })] [Boxed(new byte[] { 1, 255 })]
    [Boxed(new object[] { 1, "x", null, Tone.Low, typeof(int), new int[] { 3 } })]
    [Tones(null)] [Tones(new Tone[] { Tone.High })]
    public sealed class Arrays { }

    [List(new object[] { 1, "x", null, Small.A, typeof(string), new object[] { 2, "y" } })]
    [List(null)] [List(new object[0])] [Params(1, "two", Tone.High)] [Params]
    public sealed class ObjectArrays { }

    [Boxed(0, Named = 7, NamedField = "f", NamedArray = new object[] { Tone.Low, null })]
    [Boxed(1, Named = null, NamedField = null, NamedArray = null)]
    [Boxed(2, Named = Big.X, NamedField = typeof(Outer))]
    [Boxed(3, Named = new int[] { 4, 5 }, NamedField = 'n')]
    public sealed class Named { }

    [Typed(Big.X, UBig.Max, 'q', typeof(Outer), Order = 2)] [Typed(Big.X, UBig.Max, 'r', null)]
    public sealed class Typed { }

    [Boxed(typeof(Outer))] [Boxed(typeof(Outer.Inner))] [Boxed(typeof(int))] [Boxed(typeof(int[]))]
    [Boxed(typeof(Outer[]))] [Boxed(typeof(Box<int>))] [Boxed(typeof(Box<string>))] [Boxed(typeof(Box<>))]
    [Boxed(typeof(Box<Outer.Inner>))] [Boxed(typeof(int[,]))]
    public sealed class Types { }

    [Boxed(Outer.Mode.On)]
    [Boxed(4, Named = Outer.Mode.On, NamedArray = new object[] { Outer.Mode.Off, typeof(Box<int>) })]
    [Typed(Big.X, UBig.Max, 's', typeof(Outer.Inner), Mode = Outer.Mode.On, Order = 3)]
    public sealed class Nested { }

    internal static class Program
    {
        private static string Describe(object value)
        {
            if (value is null)
                return "null";
            if (value is Type type)
                return "Type:" + (type.IsGenericType
                    ? type.Name + (type.IsGenericTypeDefinition ? "(def)" : "<" + type.GetGenericArguments()[0].Name + ">")
                    : type.FullName);
            if (value is Array array)
            {
                var parts = new List<string>();
                foreach (object item in array)
                    parts.Add(Describe(item));
                return value.GetType().Name + "{" + string.Join(",", parts) + "}";
            }
            return value.GetType().Name + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string Describe(Attribute attribute) => attribute switch
        {
            BoxedAttribute b => "Boxed " + Describe(b.Value) + " named=" + Describe(b.Named)
                + " field=" + Describe(b.NamedField) + " array=" + Describe(b.NamedArray),
            ListAttribute l => "List " + Describe(l.Values),
            ParamsAttribute p => "Params " + Describe(p.Values),
            TonesAttribute t => "Tones " + Describe(t.Tones),
            TypedAttribute t => "Typed " + t.BigValue + " " + t.UBigValue + " " + t.Letter + " "
                + Describe(t.Type) + " " + t.Mode + " " + t.Order,
            _ => attribute.GetType().Name,
        };

        private static void Dump(Type holder, bool showData)
        {
            object[] attributes = holder.GetCustomAttributes(false);
            Console.WriteLine(holder.Name + ": " + attributes.Length + " defined=" + holder.IsDefined(typeof(BoxedAttribute), false));
            var lines = new List<string>();
            foreach (Attribute attribute in attributes)
                lines.Add("  " + Describe(attribute));
            if (showData)
                foreach (CustomAttributeData data in holder.GetCustomAttributesData())
                    lines.Add("  data " + data);
            lines.Sort(StringComparer.Ordinal);
            foreach (string line in lines)
                Console.WriteLine(line);
        }

        public static void Run()
        {
            Console.WriteLine("== boxed attribute arguments ==");
            Dump(typeof(Scalars), true);
            Dump(typeof(Primitives), true);
            Dump(typeof(Arrays), true);
            Dump(typeof(ObjectArrays), true);
            Dump(typeof(Named), true);
            Dump(typeof(Typed), true);
            Dump(typeof(Types), true);
            Dump(typeof(Nested), true);
            var expected = new List<Type>
            {
                typeof(Outer), typeof(Outer.Inner), typeof(int), typeof(int[]), typeof(Outer[]),
                typeof(Box<int>), typeof(Box<string>), typeof(Box<>), typeof(Box<Outer.Inner>), typeof(int[,]),
            };
            int same = 0;
            foreach (BoxedAttribute boxed in typeof(Types).GetCustomAttributes(typeof(BoxedAttribute), false))
                if (boxed.Value is Type type && expected.Contains(type))
                    same++;
            Console.WriteLine("Type identity: " + same + " of " + expected.Count);
        }
    }
}
