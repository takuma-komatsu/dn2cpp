using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace PathActivationFlagsSubset;

public class Probe
{
    public static int Calls;
    public int Value;
    public Probe(int value) { Calls++; Value = value; }
}
internal sealed class RefusalBinder : Binder
{
    private static Exception Refused() => new InvalidOperationException("binder callback executed");
    public override FieldInfo BindToField(BindingFlags flags, FieldInfo[] match, object value, CultureInfo culture) => throw Refused();
    public override MethodBase BindToMethod(BindingFlags flags, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state) => throw Refused();
    public override object ChangeType(object value, Type type, CultureInfo culture) => throw Refused();
    public override void ReorderArgumentArray(ref object[] args, object state) => throw Refused();
    public override MethodBase SelectMethod(BindingFlags flags, MethodBase[] match, Type[] types, ParameterModifier[] modifiers) => throw Refused();
    public override PropertyInfo SelectProperty(BindingFlags flags, PropertyInfo[] match, Type result, Type[] indexes, ParameterModifier[] modifiers) => throw Refused();
}

internal static class Program
{
    private const string Refusal = "Assembly.LoadFrom is not available in AOT-compiled code (no assembly loading)";
    private delegate ObjectHandle Create(string file, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs);
    private static ObjectHandle Direct(string file, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Activator.CreateInstanceFrom(file, type, ignoreCase, flags, binder, args, culture, attrs);
    private static ObjectHandle Pointer(string file, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Activator.CreateInstanceFrom(file, type, ignoreCase, flags, binder, args, culture, attrs);
    private static Create Address() => Activator.CreateInstanceFrom;
    private static ObjectHandle DelegateRoute(string file, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Address()(file, type, ignoreCase, flags, binder, args, culture, attrs);
    private static ObjectHandle Reflect(string file, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => (ObjectHandle)Address().Method.Invoke(null, new object[] { file, type, ignoreCase, flags, binder, args, culture, attrs });
    private static int Trace;
    private static void Step(int position, int throwing)
    {
        Trace = Trace * 10 + position;
        if (position == throwing) throw new FormatException("producer " + position);
    }
    private static string P1(int n) { Step(1, n); return null; }
    private static string P2(int n) { Step(2, n); return null; }
    private static bool P3(int n) { Step(3, n); return true; }
    private static BindingFlags P4(int n) { Step(4, n); return (BindingFlags)(-1); }
    private static Binder P5(int n) { Step(5, n); return CustomBinder(); }
    private static object[] P6(int n) { Step(6, n); return new object[] { 7 }; }
    private static CultureInfo P7(int n) { Step(7, n); return CultureInfo.InvariantCulture; }
    private static object[] P8(int n) { Step(8, n); return new object[] { 8 }; }
    private static ObjectHandle Producers(int n) => Activator.CreateInstanceFrom(P1(n), P2(n), P3(n), P4(n), P5(n), P6(n), P7(n), P8(n));
    private static Binder CustomBinder() => (Binder)RuntimeHelpers.GetUninitializedObject(typeof(RefusalBinder));
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Cases(string route, Create call)
    {
        string[] paths = { null, "", "SelfHostPrimSubset", "SelfHostPrimSubset.dll", "__missing_file__.dll" };
        for (int index = 0; index < paths.Length; index++)
        {
            bool ok = false;
            int calls = Probe.Calls;
            try { call(paths[index], null, true, (BindingFlags)(-1), CustomBinder(), new object[] { 7 }, CultureInfo.InvariantCulture, new object[] { 8 }); }
            catch (Exception error)
            {
                if (route != "reflection" || error is TargetInvocationException)
                {
                    Exception leaf = route == "reflection" ? error.InnerException : error;
                    ok = leaf is PlatformNotSupportedException && leaf.Message == Refusal;
                }
            }
            Console.WriteLine("path-flags-" + route + "-" + index + ":" + (ok && Probe.Calls == calls));
        }
    }
    private static ObjectHandle Domain(AppDomain domain)
        => domain.CreateInstanceFrom(null, null, true, (BindingFlags)(-1), CustomBinder(), new object[] { 7 }, CultureInfo.InvariantCulture, new object[] { 8 });
    private static void DomainChecks()
    {
        var domain = (AppDomain)RuntimeHelpers.GetUninitializedObject(typeof(AppDomain));
        bool forwarded = false, nullReceiver = false;
        try { Domain(domain); }
        catch (PlatformNotSupportedException error) { forwarded = error.Message == Refusal; }
        try { Domain(null); }
        catch (NullReferenceException) { nullReceiver = true; }
        Console.WriteLine("path-flags-domain-forward:" + forwarded);
        Console.WriteLine("path-flags-domain-null-receiver:" + nullReceiver);
    }
    internal static void RunArtifactChecks(string[] args)
    {
        Culture();
        Cases("direct", Direct); Cases("calli", Pointer); Cases("delegate", DelegateRoute); Cases("reflection", Reflect);
        DomainChecks();
        for (int throwing = 0; throwing <= 8; throwing++)
        {
            Trace = 0;
            bool ok = false;
            try { Producers(throwing); }
            catch (Exception error)
            {
                ok = throwing == 0 ? error is PlatformNotSupportedException && error.Message == Refusal
                    : error is FormatException && error.Message == "producer " + throwing;
            }
            int expected = 0;
            for (int position = 1; position <= (throwing == 0 ? 8 : throwing); position++) expected = expected * 10 + position;
            Console.WriteLine("path-flags-producers-" + throwing + ":" + (ok && Trace == expected));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); Cases("reflection", Reflect); }
    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Path activation flags public neighbor ==");
        Console.WriteLine("path-flags-linked-name:" + (typeof(Program).Assembly == Assembly.Load("SelfHostPrimSubset")));
        Console.WriteLine("Path activation flags public neighbor end");
    }
    internal static void RunOracleCases()
    {
        Culture();
        string file = typeof(Program).Assembly.Location;
        int before = Probe.Calls;
        ObjectHandle handle = Activator.CreateInstanceFrom(file, "PathActivationFlagsSubset.Probe", false, BindingFlags.Public | BindingFlags.Instance,
            null, new object[] { 37 }, CultureInfo.InvariantCulture, null);
        Console.WriteLine("path-flags-public-known:" + (handle.GetType() == typeof(ObjectHandle) && handle.Unwrap() is Probe probe && probe.Value == 37
            && ReferenceEquals(handle.Unwrap(), handle.Unwrap()) && Probe.Calls == before + 1));
        bool nullPath = false;
        try { Activator.CreateInstanceFrom(null, null, true, (BindingFlags)(-1), CustomBinder(), new object[] { 7 }, CultureInfo.InvariantCulture, new object[] { 8 }); }
        catch (ArgumentNullException error) { nullPath = error.ParamName == "assemblyFile"; }
        Console.WriteLine("path-flags-public-null-file-first:" + nullPath);
    }
}
