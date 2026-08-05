#nullable enable
using System;
using System.Globalization;
using System.Reflection;

namespace ReflectActivatorSubset
{
    // SUBJECT: Activator.CreateInstance's argument overloads under DefaultBinder
    // ctor resolution — arity, boxed-argument admissibility, primitive widening,
    // most-specific disambiguation, the nonPublic and BindingFlags forms — plus
    // indexed properties. Exception TYPES are printed, never messages: trap
    // messages are unmodeled.
    class P
    {
        private readonly string _tag;
        public P() { _tag = "P()"; }
        public P(int x) { _tag = $"P(int {x})"; }
        public P(string s) { _tag = $"P(string {s})"; }
        public override string ToString() => _tag;
    }

    class Q
    {
        private readonly string _tag;
        public Q(string s) { _tag = $"Q(string {s})"; }
        public Q(object o) { _tag = $"Q(object {o})"; }
        public override string ToString() => _tag;
    }

    class R
    {
        private R() { }
        public override string ToString() => "R(private)";
    }

    class OnlyInt
    {
        private readonly int _x;
        public OnlyInt(int x) { _x = x; }
        public override string ToString() => $"OnlyInt({_x})";
    }

    class OnlyDouble
    {
        private readonly double _d;
        public OnlyDouble(double d) { _d = d; }
        public override string ToString() => $"OnlyDouble({_d})";
    }

    class Both
    {
        private readonly string _t;
        public Both(int x) { _t = "int"; }
        public Both(long x) { _t = "long"; }
        public override string ToString() => $"Both({_t})";
    }

    class TakesObj
    {
        private readonly object _o;
        public TakesObj(object o) { _o = o; }
        public override string ToString() => $"TakesObj({_o})";
    }

    abstract class Abs
    {
        public Abs() { }
    }

    struct SP
    {
        public int X;
        public SP(int x) { X = x; }
        public override string ToString() => $"SP({X})";
    }

    enum Day { Mon, Tue, Wed }

    class Indexed
    {
        private readonly string[] _d = { "a", "b", "c" };
        public string this[int i]
        {
            get => _d[i];
            set => _d[i] = value;
        }
        public string Plain { get; set; } = "plain";
    }

    class GridIndexed
    {
        private readonly int[,] _g = new int[3, 3];
        public int this[int r, int c]
        {
            get => _g[r, c];
            set => _g[r, c] = value;
        }
    }

    class SetOnlyIndexed
    {
        public string Last = "";
        public string this[int i]
        {
            set => Last = $"{i}={value}";
        }
    }

    static class Program
    {
        static void Show(string label, Func<object?> f)
        {
            try
            {
                object? r = f();
                Console.WriteLine($"{label} => {r ?? "null"}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label} => threw {e.GetType().Name}");
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== Activator args ==");
            Show("empty args", () => Activator.CreateInstance(typeof(P), Array.Empty<object>()));
            Show("null args", () => Activator.CreateInstance(typeof(P), (object?[]?)null));
            Show("(int)", () => Activator.CreateInstance(typeof(P), 5));
            Show("(string)", () => Activator.CreateInstance(typeof(P), "abc"));
            Show("short widens", () => Activator.CreateInstance(typeof(P), (short)5));
            Show("long no-narrow", () => Activator.CreateInstance(typeof(P), 5L));
            Show("no match", () => Activator.CreateInstance(typeof(P), 1.5m, 1.5m));
            Show("null ambiguous", () => Activator.CreateInstance(typeof(P), (object?)null));
            Show("null most-specific", () => Activator.CreateInstance(typeof(Q), (object?)null));
            Show("string most-specific", () => Activator.CreateInstance(typeof(Q), "s"));
            Show("int/long exact", () => Activator.CreateInstance(typeof(Both), 5));
            Show("short picks int", () => Activator.CreateInstance(typeof(Both), (short)5));
            Show("float widens", () => Activator.CreateInstance(typeof(OnlyDouble), 1.5f));
            Show("enum as underlying", () => Activator.CreateInstance(typeof(OnlyInt), Day.Wed));
            Show("bool no-widen", () => Activator.CreateInstance(typeof(OnlyInt), true));
            Show("null to int ctor", () => Activator.CreateInstance(typeof(OnlyInt), (object?)null));
            Show("ref widening", () => Activator.CreateInstance(typeof(TakesObj), "up"));
            Show("struct args", () => Activator.CreateInstance(typeof(SP), 3));
            Show("abstract", () => Activator.CreateInstance(typeof(Abs), Array.Empty<object>()));
            Show("value default", () => Activator.CreateInstance(typeof(int), Array.Empty<object>()));

            Console.WriteLine("== Activator nonPublic / flags / attrs ==");
            Show("nonPublic false", () => Activator.CreateInstance(typeof(R), false));
            Show("nonPublic true", () => Activator.CreateInstance(typeof(R), true));
            Show("1-arg private ctor", () => Activator.CreateInstance(typeof(R)));
            Show("1-arg no parameterless", () => Activator.CreateInstance(typeof(OnlyInt)));
            Show("activation attrs", () => Activator.CreateInstance(typeof(P), new object[] { 5 }, new object[] { new object() }));
            Show("empty activation attrs", () => Activator.CreateInstance(typeof(P), new object[] { 5 }, Array.Empty<object>()));
            Show("null activation attrs", () => Activator.CreateInstance(typeof(P), new object[] { 5 }, null));
            Show("flags public", () => Activator.CreateInstance(typeof(P), BindingFlags.Public | BindingFlags.Instance, null, new object[] { 7 }, CultureInfo.InvariantCulture));
            Show("flags nonpublic parameterless", () => Activator.CreateInstance(typeof(P), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { 7 }, null));

            Console.WriteLine("== indexed properties ==");
            var idx = new Indexed();
            PropertyInfo item = typeof(Indexed).GetProperty("Item")!;
            ParameterInfo[] ips = item.GetIndexParameters();
            Console.WriteLine($"Item index params: {ips.Length} [{ips[0].ParameterType.Name} {ips[0].Name} @{ips[0].Position}]");
            Console.WriteLine($"indexed get => {item.GetValue(idx, new object[] { 2 })}");
            item.SetValue(idx, "set!", new object[] { 1 });
            Console.WriteLine($"indexed set => {idx[1]}");
            PropertyInfo plain = typeof(Indexed).GetProperty("Plain")!;
            Console.WriteLine($"Plain index params: {plain.GetIndexParameters().Length}");
            Console.WriteLine($"plain get w/ empty index => {plain.GetValue(idx, Array.Empty<object>())}");
            plain.SetValue(idx, "replug", null);
            Console.WriteLine($"plain set w/ null index => {idx.Plain}");

            var grid = new GridIndexed();
            PropertyInfo gi = typeof(GridIndexed).GetProperty("Item")!;
            ParameterInfo[] gps = gi.GetIndexParameters();
            Console.WriteLine($"Grid index params: {gps.Length} [{gps[0].Name},{gps[1].Name}]");
            gi.SetValue(grid, 42, new object[] { 2, 1 });
            Console.WriteLine($"grid get => {gi.GetValue(grid, new object[] { 2, 1 })}");

            var so = new SetOnlyIndexed();
            PropertyInfo sp = typeof(SetOnlyIndexed).GetProperty("Item")!;
            ParameterInfo[] sps = sp.GetIndexParameters();
            Console.WriteLine($"SetOnly index params: {sps.Length} [{sps[0].ParameterType.Name}] canRead={sp.CanRead} canWrite={sp.CanWrite}");
            sp.SetValue(so, "v", new object[] { 7 });
            Console.WriteLine($"setonly set => {so.Last}");

            // The BindingFlags forms on an already-resolved PropertyInfo.
            Console.WriteLine($"flags get => {gi.GetValue(grid, BindingFlags.Public | BindingFlags.Instance, null, new object[] { 2, 1 }, CultureInfo.InvariantCulture)}");
            gi.SetValue(grid, 43, BindingFlags.Public | BindingFlags.Instance, null, new object[] { 2, 1 }, null);
            Console.WriteLine($"flags set => {grid[2, 1]}");

            Console.WriteLine("== reflection long-tail ==");
            // GetTypeInfo is the identity here (RuntimeType : TypeInfo posture).
            TypeInfo ti = typeof(Day).GetTypeInfo();
            Console.WriteLine($"GetTypeInfo enum={ti.IsEnum} same={ReferenceEquals(ti, typeof(Day))} isTypeInfo={typeof(Day) is TypeInfo}");
            // MethodInfo.ReturnParameter: the -1-positioned return pseudo-parameter.
            ParameterInfo rp = typeof(Indexed).GetProperty("Item")!.GetGetMethod()!.ReturnParameter;
            Console.WriteLine($"ReturnParameter {rp.ParameterType.Name} pos={rp.Position} name={(rp.Name is null ? "null" : rp.Name)}");
            // Array.CreateInstanceFromArrayType: element off the array type itself.
            Array cifat = Array.CreateInstanceFromArrayType(typeof(int[]), 3);
            cifat.SetValue(5, 1);
            Console.WriteLine($"CreateInstanceFromArrayType {cifat.GetType()} len={cifat.Length} [1]={cifat.GetValue(1)}");
        }
    }
}
