using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace InterpolationHelpers;
internal static class Program
{
    private delegate bool Query(IFormatProvider? provider);
    private delegate int Length(int literal, int count);
    private static bool Direct(IFormatProvider? provider) => throw new Exception("rewrite");
    private static int DirectLength(int literal, int count) => throw new Exception("rewrite");
    private static Query Address() => throw new Exception("rewrite");
    private static Length AddressLength() => throw new Exception("rewrite");
    private static bool Reflected(IFormatProvider? provider) => ((Query)typeof(DefaultInterpolatedStringHandler).GetMethod("HasCustomFormatter", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(typeof(Query)))(provider);
    private static int ReflectedLength(int literal, int count) => ((Length)typeof(DefaultInterpolatedStringHandler).GetMethod("GetDefaultLength", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(typeof(Length)))(literal, count);
    private sealed class Provider : IFormatProvider, ICustomFormatter
    {
        internal int Mode, Calls;
        public object? GetFormat(Type? type)
        {
            if (type != typeof(ICustomFormatter)) throw new Exception("type identity");
            Calls++;
            if (Mode == 3 || Mode == 5 && Calls == 2) throw new FormatException("provider");
            if (Mode == 4) return Calls == 1 ? this : Calls == 2 ? null : new object();
            if (Mode == 5) return null;
            return Mode == 0 ? null : Mode == 1 ? this : new object();
        }
        public string Format(string? format, object? arg, IFormatProvider? provider) => throw new Exception("must not format");
    }
    private sealed class Derived : CultureInfo
    {
        internal int Calls;
        internal Derived() : base("") { }
        public override object? GetFormat(Type? type) { Calls++; return new object(); }
    }
    private static void Check(string name, bool ok)
    {
        Console.WriteLine(name + ":" + ok);
        if (!ok) throw new Exception(name);
    }
    private static void Test(Query query, Length length, bool reflection)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("culture", !query(CultureInfo.InvariantCulture));
        try { query(null); throw new Exception("null accepted"); }
        catch (NullReferenceException) { Check("null", !reflection); }
        catch (TargetInvocationException error) { Check("null", reflection && error.InnerException is NullReferenceException); }
        for (int mode = 0; mode < 4; mode++)
        {
            var provider = new Provider { Mode = mode };
            try { bool result = query(provider); Check("result" + mode, mode != 3 && result == (mode != 0)); }
            catch (FormatException) { Check("throw", !reflection && mode == 3); }
            catch (TargetInvocationException error) { Check("throw", reflection && mode == 3 && error.InnerException is FormatException); }
            Check("calls" + mode, provider.Calls == 1);
        }

        var stateful = new Provider { Mode = 4 };
        Check("stateful-true-null-wrong", query(stateful) && !query(stateful) && query(stateful) && stateful.Calls == 3);
        stateful = new Provider { Mode = 5 };
        Check("stateful-before-throw", !query(stateful));
        try { query(stateful); Check("stateful-throw", false); }
        catch (FormatException) { Check("stateful-throw", stateful.Calls == 2); }
        int[] literals = { 0, 255, 257, 0, 0, int.MaxValue, int.MinValue, 0, 256, -1 };
        int[] counts = { 0, 0, 0, 23, 24, 1, 0, int.MaxValue, int.MinValue, -1 };
        int[] expected = { 256, 256, 257, 256, 264, 256, 256, 2147483637, 256, 256 };
        for (int i = 0; i < literals.Length; i++)
            Check("length" + i, length(literals[i], counts[i]) == expected[i]);
        Console.WriteLine("interpolation-helpers:end");
    }
    private ref struct RefLike
    {
        public static int Value(int value) { invokeCalls++; return value; }
    }
    private ref struct GenericRefLike<T>
    {
        public static int Value() => 1;
    }
    private static int invokeCalls;
    private static int Ordinary(int value) { invokeCalls++; return value; }
    private static void RunInvoke()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var provider = new Provider { Mode = 3 };
        foreach (string name in new[] { "HasCustomFormatter", "GetDefaultLength" })
        {
            var method = typeof(DefaultInterpolatedStringHandler).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (object?[]? values in new object?[]?[] { null, Array.Empty<object>(), new object?[] { provider }, new object?[] { "wrong", "wrong" }, new object?[] { 1, 2 } })
                foreach (object? receiver in new object?[] { null, new object() })
                    foreach (BindingFlags flags in new[] { BindingFlags.Default, BindingFlags.DoNotWrapExceptions })
                    {
                        try { method.Invoke(receiver, flags, null, values, null); Check("invoke-refused", false); }
                        catch (NotSupportedException error) { Check("invoke-refused", error.GetType() == typeof(NotSupportedException)); }
                    }
        }
        Check("invoke-provider-not-called", provider.Calls == 0);
        var own = typeof(RefLike).GetMethod("Value")!;
        try { own.Invoke(null, new object[] { 42 }); Check("user-refused", false); }
        catch (NotSupportedException error) { Check("user-refused", error.GetType() == typeof(NotSupportedException)); }
        Check("user-not-called", invokeCalls == 0);
        var bound = (Func<int, int>)own.CreateDelegate(typeof(Func<int, int>));
        Check("user-delegate", bound(42) == 42 && invokeCalls == 1);
        var ordinary = typeof(Program).GetMethod("Ordinary", BindingFlags.Static | BindingFlags.NonPublic)!;
        Check("ordinary-invoke", (int)ordinary.Invoke(null, new object[] { 43 })! == 43 && invokeCalls == 2);
        try { typeof(GenericRefLike<>).GetMethod("Value")!.Invoke(null, null); Check("open-precedence", false); }
        catch (InvalidOperationException) { Check("open-precedence", true); }
        Console.WriteLine("interpolation-invoke:end");
    }
    private static void RunRetained()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("retained-type", typeof(DefaultInterpolatedStringHandler).IsByRefLike);
        Check("retained-has-row", typeof(DefaultInterpolatedStringHandler).GetMethod("HasCustomFormatter", BindingFlags.Static | BindingFlags.NonPublic) is not null);
        Check("retained-length-row", typeof(DefaultInterpolatedStringHandler).GetMethod("GetDefaultLength", BindingFlags.Static | BindingFlags.NonPublic) is not null);
        Console.WriteLine("retained-helpers:end");
    }
    private static void RunLiteral() => TryWriteSubset.Program.__LiteralGateEntry();
    private static void RunText() => TryWriteSubset.Program.__TextGateEntry();
    private static void RunPublic() => TryWriteSubset.Program.__GateEntry();
    private static void RunAll()
    {
        Main();
        RunAddress();
        RunReflection();
        RunInvoke();
        RunLiteral();
    }
    private static void RunDerived()
    {
        var derived = new Derived();
        Check("derived", Direct(derived) && derived.Calls == 1);
    }
    private static void Main() => Test(Direct, DirectLength, false);
    private static void RunAddress() => Test(Address(), AddressLength(), false);
    private static void RunReflection() => Test(Reflected, ReflectedLength, false);
}
