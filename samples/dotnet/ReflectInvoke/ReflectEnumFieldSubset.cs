#nullable enable
using System;
using System.Globalization;
using System.Reflection;

namespace ReflectEnumFieldSubset
{
    // SUBJECT: an enum type's reflection FIELD surface — one public instance
    // `value__` row typed at the underlying primitive, then one public static
    // literal row per member typed at the enum, in metadata declaration order,
    // with GetField(name).GetValue(null) answering the boxed value. The tail is
    // the serializer loop that path serves, plus the null-handle contract: a
    // missing field table makes GetField answer null, and the null handle's
    // GetValue must raise a catchable NRE rather than crash.
    enum Compound { Nothing = 0, Glucose = 1, Ammonia = 2, Phosphates = 5 }
    enum Tier : byte { Low = 1, Mid = 2, High = 4 }
    [Flags]
    enum Wide : long { A = 1, B = 1L << 40 }

    static class Program
    {
        static void DumpFields(Type t)
        {
            Console.WriteLine($"== {t.Name} ==");
            foreach (var f in t.GetFields())
                Console.WriteLine($"{f.Name} ft={f.FieldType.Name} static={f.IsStatic} " +
                    $"literal={f.IsLiteral} public={f.IsPublic} special={f.IsSpecialName} " +
                    $"attrs=0x{(int)f.Attributes:X}");
        }

        internal static void Run()
        {
            DumpFields(typeof(Compound));
            DumpFields(typeof(Tier));
            DumpFields(typeof(Wide));

            // GetField(name) -> boxed literal value (no static storage exists).
            FieldInfo g = typeof(Compound).GetField("Glucose")!;
            object v = g.GetValue(null)!;
            Console.WriteLine($"Glucose value={v} int={(int)(Compound)v}");
            Console.WriteLine("missing=" + (typeof(Compound).GetField("Nope") is null ? "<null>" : "<found>"));

            // GetFields(Public|Static): the literal member rows only, no value__.
            foreach (var f in typeof(Tier).GetFields(BindingFlags.Public | BindingFlags.Static))
                Console.WriteLine($"ps {f.Name} -> {f.GetValue(null)}");

            // The instance value__ row answers the underlying-typed payload.
            FieldInfo vf = typeof(Tier).GetField("value__")!;
            object uv = vf.GetValue(Tier.Mid)!;
            Console.WriteLine($"value__={uv} type={uv.GetType().Name}");
            FieldInfo wf = typeof(Wide).GetField("value__")!;
            object wv = wf.GetValue(Wide.B)!;
            Console.WriteLine($"wide value__={wv} type={wv.GetType().Name}");

            // The serializer loop shape: name -> field -> boxed value -> numeric
            // conversion and enum cast.
            foreach (string name in Enum.GetNames(typeof(Compound)))
            {
                FieldInfo f = typeof(Compound).GetField(name, BindingFlags.Public | BindingFlags.Static)!;
                object o = f.GetValue(null)!;
                Console.WriteLine($"nj {name} -> {o} num={Convert.ToInt64(o, CultureInfo.InvariantCulture)} cast={(Compound)o}");
            }

            // A null handle's GetValue: the catchable NRE, never a native crash.
            FieldInfo? nf = typeof(Compound).GetField("Nope");
            try
            {
                nf!.GetValue(null);
                Console.WriteLine("null fieldref => no throw");
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("null fieldref => NullReferenceException");
            }
        }
    }
}
