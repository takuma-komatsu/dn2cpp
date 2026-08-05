#nullable disable
using System;
using System.Reflection;

// Custom attributes. Type/Field/Property/Method/Constructor/
// Parameter.GetCustomAttributes([attrType,] inherit) and IsDefined(attrType, inherit)
// materialize fresh attribute instances at call time — the ctor runs with its constant
// positional args and the named field/property args are then set. CoreLib only; diffed
// exact vs real .NET. Nullable is disabled so the compiler emits no Nullable* attributes.

namespace ReflectAttrSubset
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    sealed class InfoAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; set; }      // named property (int)
        public string Note = "";            // named field (string)
        public bool Flag { get; set; }      // named property (bool)
        public InfoAttribute(string name) { Name = name; }
        public InfoAttribute(string name, int order) { Name = name; Order = order; }
    }

    sealed class TagAttribute : Attribute
    {
        public TagAttribute() { }
    }

    sealed class KindAttribute : Attribute
    {
        public DayKind Kind { get; set; }   // named property (enum)
        public Type Target = typeof(object); // named field (Type)
        public KindAttribute() { }
    }

    enum DayKind { None, Work, Rest }

    // Float/double attribute arguments — positional and named, at both widths.
    // The emitted attribute-create function once rendered an integral-valued float
    // as the invalid C++ token `2f` (and dropped -0.0's sign / mangled the special
    // values), because the attribute path formatted floats differently from the body
    // path. Values chosen to exercise: an integral float (2f), a fractional float,
    // a negative, an integral double, -0.0 at both widths, and NaN/±Infinity.
    sealed class MetricAttribute : Attribute
    {
        public float FCtor { get; }
        public double DCtor { get; }
        public float FNamed { get; set; }
        public double DNamed { get; set; }
        public MetricAttribute(float f, double d) { FCtor = f; DCtor = d; }
    }

    [Metric(2f, 3.0, FNamed = -1.5f, DNamed = 2.5)]
    class MetricHost
    {
        [Metric(0f, -0.0, FNamed = -0.0f, DNamed = 4.0)]
        public int MF;

        [Metric(float.NaN, double.PositiveInfinity, FNamed = float.NegativeInfinity, DNamed = double.NaN)]
        public int MP { get; set; }
    }

    [Info("ClassA", Order = 5)]
    [Info("ClassB")]
    [Tag]
    [Kind(Kind = DayKind.Rest, Target = typeof(Subject))]
    class Subject
    {
        [Info("fieldF", Note = "fn")]
        public int F;

        [Info("propP", Order = 2, Flag = true)]
        public string P { get; set; } = "";

        [Info("methodM")]
        [Tag]
        public void M([Info("paramX")] int x) { }

        [Info("ctorC")]
        public Subject() { }
    }

    static class Program
    {
        static void DumpAttr(object a)
        {
            if (a is InfoAttribute i)
                Console.WriteLine($"  Info Name={i.Name} Order={i.Order} Note={i.Note} Flag={i.Flag}");
            else if (a is TagAttribute)
                Console.WriteLine("  Tag");
            else if (a is KindAttribute k)
                Console.WriteLine($"  Kind Kind={k.Kind} Target={k.Target.Name}");
            else
                Console.WriteLine($"  ?{a.GetType().Name}");
        }

        static void Dump(string label, object[] attrs)
        {
            Console.WriteLine(label);
            Array.Sort(attrs, (x, y) =>
            {
                int c = string.CompareOrdinal(x.GetType().Name, y.GetType().Name);
                if (c != 0) return c;
                string nx = x is InfoAttribute ix ? ix.Name : "";
                string ny = y is InfoAttribute iy ? iy.Name : "";
                return string.CompareOrdinal(nx, ny);
            });
            foreach (var a in attrs)
                DumpAttr(a);
        }internal static void Run()
        {
            Type t = typeof(Subject);
            Dump("== type all ==", t.GetCustomAttributes(false));
            Dump("== type Info ==", t.GetCustomAttributes(typeof(InfoAttribute), false));
            Console.WriteLine($"type IsDefined(Tag)={t.IsDefined(typeof(TagAttribute), false)}");
            Console.WriteLine($"type IsDefined(Info)={t.IsDefined(typeof(InfoAttribute), false)}");
            Console.WriteLine($"type IsDefined(Kind)={t.IsDefined(typeof(KindAttribute), false)}");

            FieldInfo f = t.GetField("F");
            Dump("== field F ==", f.GetCustomAttributes(false));

            PropertyInfo p = t.GetProperty("P");
            Dump("== prop P ==", p.GetCustomAttributes(false));

            MethodInfo m = t.GetMethod("M");
            Dump("== method M ==", m.GetCustomAttributes(false));
            ParameterInfo px = m.GetParameters()[0];
            Dump("== param x ==", px.GetCustomAttributes(false));

            ConstructorInfo c = t.GetConstructor(Type.EmptyTypes);
            Dump("== ctor ==", c.GetCustomAttributes(false));

            // == float/double attribute args ==
            // Values are described through a deterministic classifier (no float
            // ToString — its culture/formatting would differ from the diff oracle),
            // asserting the ctor + named args of both widths survived the emit intact.
            Type mh = typeof(MetricHost);
            DumpMetric("host", (MetricAttribute)mh.GetCustomAttributes(typeof(MetricAttribute), false)[0]);
            DumpMetric("field", (MetricAttribute)mh.GetField("MF").GetCustomAttributes(typeof(MetricAttribute), false)[0]);
            DumpMetric("prop", (MetricAttribute)mh.GetProperty("MP").GetCustomAttributes(typeof(MetricAttribute), false)[0]);
        }

        static string Fd(double v)
        {
            if (double.IsNaN(v)) return "NaN";
            if (double.IsPositiveInfinity(v)) return "+Inf";
            if (double.IsNegativeInfinity(v)) return "-Inf";
            if (v == 0.0) return double.IsNegative(v) ? "-0" : "+0";
            return ((long)(v * 1000)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        static void DumpMetric(string label, MetricAttribute m) =>
            Console.WriteLine($"  Metric {label} FCtor={Fd(m.FCtor)} DCtor={Fd(m.DCtor)} FNamed={Fd(m.FNamed)} DNamed={Fd(m.DNamed)}");
    }
}
