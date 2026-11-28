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

    class OnlyChar
    {
        private readonly char _value;
        public OnlyChar(char value) { _value = value; }
        public override string ToString() => $"OnlyChar({(int)_value})";
    }

    class OnlyUShort
    {
        private readonly ushort _value;
        public OnlyUShort(ushort value) { _value = value; }
        public override string ToString() => $"OnlyUShort({_value})";
    }

    class CharOrUShort
    {
        private readonly string _which;
        public CharOrUShort(ushort value) { _which = "ushort"; }
        public CharOrUShort(char value) { _which = "char"; }
        public override string ToString() => _which;
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

    class WithInitializer
    {
        public WithInitializer() { }
        static WithInitializer() { }
        public override string ToString() => "WithInitializer()";
    }

    class WithCctorFirst
    {
        static WithCctorFirst() { }
        public WithCctorFirst() { }
        public override string ToString() => "WithCctorFirst()";
    }

    class WithBeforeFieldInit
    {
        public static int Marker = 1;
        public WithBeforeFieldInit() { }
        public override string ToString() => "WithBeforeFieldInit()";
    }

    class WithInitializerPrivate
    {
        static WithInitializerPrivate() { }
        private WithInitializerPrivate() { }
        public override string ToString() => "WithInitializerPrivate(private)";
    }

    class WithInitializerNoDefault
    {
        static WithInitializerNoDefault() { }
        public WithInitializerNoDefault(int x) { }
    }

    class WithInitializerInvokeMemberOnly
    {
        static WithInitializerInvokeMemberOnly() { }
        public WithInitializerInvokeMemberOnly() { }
        public override string ToString() => "WithInitializerInvokeMemberOnly()";
    }

    class WithInitializerBoxedFlags
    {
        static WithInitializerBoxedFlags() { }
        public WithInitializerBoxedFlags() { }
        public override string ToString() => "WithInitializerBoxedFlags()";
    }

    class WithInitializerBoxedInvokeMember
    {
        static WithInitializerBoxedInvokeMember() { }
        public WithInitializerBoxedInvokeMember() { }
        public override string ToString() => "WithInitializerBoxedInvokeMember()";
    }

    sealed class StaticLookupBox<T>
    {
        public StaticLookupBox() { }
    }

    static class StructInitTracker
    {
        public static int PlainRuns;
        public static int CtorRuns;
    }

    struct StructWithInitializer
    {
        static StructWithInitializer() { StructInitTracker.PlainRuns++; }
        public int Value;
        public StructWithInitializer(int value) { Value = value; }
        public override string ToString() => $"StructWithInitializer({Value})";
    }

    struct StructWithInitializerAndCtor
    {
        static StructWithInitializerAndCtor() { StructInitTracker.CtorRuns++; }
        public int Value;
        public StructWithInitializerAndCtor() { Value = 7; }
        public override string ToString() => $"StructWithInitializerAndCtor({Value})";
    }

    sealed class VarargFirst
    {
        public static int Init;
        public VarargFirst(__arglist) { }
        static VarargFirst() { Init++; }
        public override string ToString() => "VarargFirst()";
    }

    sealed class VarargBeforeBare
    {
        private readonly string _tag;
        public VarargBeforeBare(__arglist) { _tag = "vararg"; }
        private VarargBeforeBare() { _tag = "bare-private"; }
        public override string ToString() => $"VarargBeforeBare({_tag})";
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
        public static int EchoChar(char value) => value;

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

        internal static void RunPrimitiveBinder()
        {
            Console.WriteLine("== default binder primitive widening ==");
            Show("binder ushort to char", () => Activator.CreateInstance(typeof(OnlyChar), (ushort)65));
            Show("binder flags ushort to char", () => Activator.CreateInstance(typeof(OnlyChar),
                BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder,
                new object[] { ushort.MaxValue }, CultureInfo.InvariantCulture));
            Show("binder byte to char", () => Activator.CreateInstance(typeof(OnlyChar), (byte)65));
            Show("binder char exact", () => Activator.CreateInstance(typeof(OnlyChar), 'A'));
            Show("binder null to char", () => Activator.CreateInstance(typeof(OnlyChar), new object?[] { null }));
            Show("binder sbyte to char", () => Activator.CreateInstance(typeof(OnlyChar), (sbyte)65));
            Show("binder short to char", () => Activator.CreateInstance(typeof(OnlyChar), (short)65));
            Show("binder int to char", () => Activator.CreateInstance(typeof(OnlyChar), 65));
            Show("binder char to ushort", () => Activator.CreateInstance(typeof(OnlyUShort), char.MaxValue));
            Show("binder char to int", () => Activator.CreateInstance(typeof(OnlyInt), char.MaxValue));
            Show("binder ushort to int", () => Activator.CreateInstance(typeof(OnlyInt), ushort.MaxValue));
            Show("binder float to double", () => Activator.CreateInstance(typeof(OnlyDouble), 1.5f));
            Show("binder double to int", () => Activator.CreateInstance(typeof(OnlyInt), 1.5));
            Show("binder byte prefers char", () => Activator.CreateInstance(typeof(CharOrUShort), (byte)65));
            Show("binder ushort prefers ushort", () => Activator.CreateInstance(typeof(CharOrUShort), (ushort)65));
            Show("invoke ushort to char", () => typeof(Program).GetMethod(nameof(EchoChar))!
                .Invoke(null, new object[] { ushort.MaxValue }));
            Show("constructor invoke ushort to char", () => typeof(OnlyChar).GetConstructor(new[] { typeof(char) })!
                .Invoke(new object[] { ushort.MaxValue }));
            Console.WriteLine("default binder primitive widening end");
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

        static void ShowExact(string label, Func<object?> f)
        {
            try
            {
                object? r = f();
                Console.WriteLine($"{label} => {r ?? "null"}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{label} => threw {e.GetType().Name}: {e.Message}");
            }
        }

        internal static void RunDefaultConstructorVisibility()
        {
            const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            Show("public default, nonpublic flags", () => Activator.CreateInstance(typeof(P), nonPublicInstance, null, null, null));
            Show("public default, public flags", () => Activator.CreateInstance(typeof(P), publicInstance, null, null, null));
            Show("private default, nonpublic flags", () => Activator.CreateInstance(typeof(R), nonPublicInstance, null, null, null));
        }

        internal static void RunStaticConstructorLookup()
        {
            const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags staticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
            const BindingFlags staticPublic = BindingFlags.Static | BindingFlags.Public;
            const BindingFlags staticNonPublicInstance = staticNonPublic | BindingFlags.Instance;
            const BindingFlags allConstructors = staticNonPublic | publicInstance;
            Console.WriteLine("== Activator static constructor lookup ==");
            _ = Activator.CreateInstance(typeof(WithInitializer));
            _ = Activator.CreateInstance(typeof(WithBeforeFieldInit));
            foreach (var type in new[] { typeof(P), typeof(WithInitializer), typeof(WithBeforeFieldInit) })
            {
                Show($"{type.Name}, static nonpublic", () => Activator.CreateInstance(type, staticNonPublic, null, null, null));
                Show($"{type.Name}, static nonpublic instance", () => Activator.CreateInstance(type, staticNonPublicInstance, null, null, null));
                Show($"{type.Name}, all constructors", () => Activator.CreateInstance(type, allConstructors, null, null, null));
                Show($"{type.Name}, static public", () => Activator.CreateInstance(type, staticPublic, null, null, null));
            }
            ShowExact("ctor first ambiguous message", () => Activator.CreateInstance(typeof(WithInitializer), allConstructors, null, null, null));
            ShowExact("cctor first ambiguous message", () => Activator.CreateInstance(typeof(WithCctorFirst), allConstructors, null, null, null));
            Show("cctor private default, static nonpublic", () => Activator.CreateInstance(typeof(WithInitializerPrivate), staticNonPublic, null, null, null));
            Show("cctor private default, static nonpublic instance", () => Activator.CreateInstance(typeof(WithInitializerPrivate), staticNonPublicInstance, null, null, null));
            Show("cctor no default, static nonpublic", () => Activator.CreateInstance(typeof(WithInitializerNoDefault), staticNonPublic, null, null, null));
            Show("cctor invoke-member retention, static nonpublic", () => typeof(WithInitializerInvokeMemberOnly).InvokeMember(
                "", BindingFlags.CreateInstance | staticNonPublic, null, null, null));
            object boxedFlags = publicInstance;
            Show("cctor boxed flags, public instance", () => Activator.CreateInstance(typeof(WithInitializerBoxedFlags), (BindingFlags)boxedFlags, null, null, null));
            object boxedInvokeMemberFlags = BindingFlags.CreateInstance | staticNonPublic;
            Show("cctor boxed invoke-member, static nonpublic", () => typeof(WithInitializerBoxedInvokeMember).InvokeMember(
                "", (BindingFlags)boxedInvokeMemberFlags, null, null, null));
            Show("generic template static public miss", () => Activator.CreateInstance(
                typeof(StaticLookupBox<>).MakeGenericType(typeof(string)), staticPublic, null, null, null));
            Show("struct no cctor, static nonpublic", () => Activator.CreateInstance(typeof(SP), staticNonPublic, null, null, null));
            object? structWithInit = Activator.CreateInstance(typeof(StructWithInitializer), staticNonPublic, null, null, null);
            int structInitBeforeToString = StructInitTracker.PlainRuns;
            string structWithInitText = structWithInit?.ToString() ?? "null";
            Console.WriteLine($"struct with cctor, static nonpublic => {structWithInitText} (beforeToString={structInitBeforeToString}, afterToString={StructInitTracker.PlainRuns})");
            Show("struct with cctor and ctor, all constructors", () => Activator.CreateInstance(typeof(StructWithInitializerAndCtor), allConstructors, null, null, null));
            Show("struct with cctor and ctor, static nonpublic", () => Activator.CreateInstance(typeof(StructWithInitializerAndCtor), staticNonPublic, null, null, null));
            ShowExact("void static nonpublic", () => Activator.CreateInstance(typeof(void), staticNonPublic, null, null, null));
            ShowExact("void pointer static nonpublic", () => Activator.CreateInstance(typeof(void*), staticNonPublic, null, null, null));
            ShowExact("argiterator static nonpublic", () => Activator.CreateInstance(typeof(ArgIterator), staticNonPublic, null, null, null));
            Show("bool static nonpublic", () => Activator.CreateInstance(typeof(bool), staticNonPublic, null, null, null));
            Show("timespan static nonpublic", () => Activator.CreateInstance(typeof(TimeSpan), staticNonPublic, null, null, null));
            Show("datetimeoffset static nonpublic", () => Activator.CreateInstance(typeof(DateTimeOffset), staticNonPublic, null, null, null));
            ShowExact("waithandle all constructors", () => Activator.CreateInstance(typeof(System.Threading.WaitHandle), allConstructors, null, null, null));
            ShowExact("waithandle static nonpublic", () => Activator.CreateInstance(typeof(System.Threading.WaitHandle), staticNonPublic, null, null, null));
            ShowExact("waithandle nonmatching arg", () => Activator.CreateInstance(typeof(System.Threading.WaitHandle), new object[] { 1 }));
            ShowExact("idisposable default", () => Activator.CreateInstance(typeof(IDisposable)));
            Show("task<int> nonpublic instance", () => Activator.CreateInstance(typeof(System.Threading.Tasks.Task<int>), nonPublicInstance, null, null, null)?.GetType());
            Show("yieldawaiter static nonpublic", () => Activator.CreateInstance(typeof(System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter), staticNonPublic, null, null, null));
            Show("yieldawaitable static nonpublic", () => Activator.CreateInstance(typeof(System.Runtime.CompilerServices.YieldAwaitable), staticNonPublic, null, null, null));
            ShowExact("vararg first ambiguous message", () => Activator.CreateInstance(typeof(VarargFirst), allConstructors, null, null, null));
            Show("vararg before bare, public default", () => Activator.CreateInstance(typeof(VarargBeforeBare)));
            Show("vararg before bare, public flags", () => Activator.CreateInstance(typeof(VarargBeforeBare), publicInstance, null, null, null));
            ShowExact("vararg before bare, all instance", () => Activator.CreateInstance(typeof(VarargBeforeBare), publicInstance | BindingFlags.NonPublic, null, null, null));
        }
    }
}
