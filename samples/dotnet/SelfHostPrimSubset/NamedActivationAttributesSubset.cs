using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;

namespace NamedActivationAttributesSubset;

public class Probe
{
    public static int DefaultCalls;
    public static int IntCalls;
    public int Value;
    public Probe() { DefaultCalls++; Value = 31; }
    public Probe(int value) { IntCalls++; Value = value; }
}
public class OnlyInt { public OnlyInt(int value) { Probe.IntCalls++; } }
public class ThrowProbe { public ThrowProbe() { throw new InvalidOperationException("attributes constructor"); } }
public class PlannerOnly
{
    public int Value;
    public PlannerOnly() { Value = 47; }
    public PlannerOnly(int value) { Value = value; }
}
public class SmokeOnly { public int Value; public SmokeOnly() { Value = 53; } }

internal static class Program
{
    private const string App = "SelfHostPrimSubset";
    private const string TypeName = "NamedActivationAttributesSubset.Probe";
    private delegate ObjectHandle Create(string assembly, string type, object[] attrs);
    private static ObjectHandle Direct(string assembly, string type, object[] attrs)
        => Activator.CreateInstance(assembly, type, attrs);
    private static ObjectHandle Pointer(string assembly, string type, object[] attrs)
        => Activator.CreateInstance(assembly, type, attrs);
    private static Create Address() => Activator.CreateInstance;
    private static ObjectHandle DelegateRoute(string assembly, string type, object[] attrs)
        => Address()(assembly, type, attrs);
    private static ObjectHandle Reflect(string assembly, string type, object[] attrs)
        => (ObjectHandle)Address().Method.Invoke(null, new object[] { assembly, type, attrs });

    private static int Trace;
    private static string P1() { Trace = Trace * 10 + 1; return App; }
    private static string P2() { Trace = Trace * 10 + 2; return TypeName; }
    private static object[] P3(bool throwing)
    {
        Trace = Trace * 10 + 3;
        if (throwing) throw new FormatException("attributes producer");
        return new object[] { 7 };
    }
    private static ObjectHandle Producers(bool throwing) => Activator.CreateInstance(P1(), P2(), P3(throwing));
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Case(string route, string label, Create call, string expected,
        string assembly = App, string type = TypeName, object[] attrs = null)
    {
        int beforeDefault = Probe.DefaultCalls, beforeInt = Probe.IntCalls;
        bool ok = false;
        try
        {
            ObjectHandle handle = call(assembly, type, attrs);
            if (expected == "null") ok = handle == null;
            else if (handle != null)
            {
                object value = handle.Unwrap();
                ok = expected == "probe" && value is Probe probe && probe.Value == 31
                    && handle.GetType() == typeof(ObjectHandle) && ReferenceEquals(value, handle.Unwrap());
            }
        }
        catch (Exception error)
        {
            if (route != "reflection" || error is TargetInvocationException)
            {
                Exception leaf = route == "reflection" ? error.InnerException : error;
                string category = leaf is ArgumentException argument ? leaf.GetType().Name + ":" + argument.ParamName
                    : leaf is TargetInvocationException invocation ? "TargetInvocationException:" + invocation.InnerException.GetType().Name
                    : leaf.GetType().Name;
                ok = expected.StartsWith("message:", StringComparison.Ordinal)
                    ? leaf is PlatformNotSupportedException && leaf.Message == expected.Substring(8)
                    : category == expected;
            }
        }
        ok &= Probe.IntCalls == beforeInt && Probe.DefaultCalls == beforeDefault + (expected == "probe" ? 1 : 0);
        Console.WriteLine("named-attrs-" + route + "-" + label + ":" + ok);
    }
    private static void Cases(string route, Create call)
    {
        Case(route, "null-attrs", call, "probe");
        Case(route, "empty-attrs", call, "probe", attrs: Array.Empty<object>());
        Case(route, "attrs-not-args", call, "PlatformNotSupportedException", attrs: new object[] { 7 });
        Case(route, "attrs-before-missing-ctor", call, "PlatformNotSupportedException", type: "NamedActivationAttributesSubset.OnlyInt", attrs: new object[] { 7 });
        Case(route, "empty-missing-ctor", call, "MissingMethodException", type: "NamedActivationAttributesSubset.OnlyInt", attrs: Array.Empty<object>());
        Case(route, "wrapped", call, "TargetInvocationException:InvalidOperationException", type: "NamedActivationAttributesSubset.ThrowProbe");
        Case(route, "missing-assembly-first", call, "FileNotFoundException", assembly: "__MissingAttributesAssembly__", type: null, attrs: new object[] { 7 });
        Case(route, "null-type-first", call, "ArgumentNullException:name", type: null, attrs: new object[] { 7 });
        Case(route, "empty-type-first", call, "ArgumentException:name", type: "", attrs: new object[] { 7 });
        Case(route, "missing-type-first", call, "TypeLoadException", type: "NoSuchType", attrs: new object[] { 7 });
        Case(route, "nullable", call, "null", assembly: "System.Private.CoreLib", type: "System.Nullable`1[[System.Int32, System.Private.CoreLib]]");
    }
    private static void NativePolicy(string route, Create call)
    {
        Case(route, "native-null-assembly", call, "message:Named activation with a null assembly name requires caller-sensitive assembly resolution", assembly: null, type: null, attrs: new object[] { 7 });
    }
    internal static void RunOracleCases() { Culture(); Cases("public", Activator.CreateInstance); }
    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Named activation attributes literal smoke ==");
        ObjectHandle handle = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationAttributesSubset.SmokeOnly", null);
        object value = handle.Unwrap();
        Console.WriteLine("named-attrs-smoke:" + ((int)value.GetType().GetField("Value").GetValue(value) == 53 && ReferenceEquals(value, handle.Unwrap())));
        Console.WriteLine("Named activation attributes literal smoke end");
    }
    private static void MetadataInputs() { GC.KeepAlive(typeof(int?)); GC.KeepAlive(typeof(OnlyInt)); }
    internal static void RunArtifactChecks(string[] args)
    {
        Culture(); MetadataInputs();
        Cases("direct", Direct); NativePolicy("direct", Direct);
        Cases("calli", Pointer); NativePolicy("calli", Pointer);
        Cases("delegate", DelegateRoute); NativePolicy("delegate", DelegateRoute);
        Cases("reflection", Reflect); NativePolicy("reflection", Reflect);
        foreach (bool throwing in new[] { false, true })
        {
            Trace = 0;
            int beforeDefault = Probe.DefaultCalls, beforeInt = Probe.IntCalls;
            bool ok = false;
            try { Producers(throwing); }
            catch (Exception error)
            {
                ok = throwing ? error is FormatException && error.Message == "attributes producer" : error is PlatformNotSupportedException;
            }
            Console.WriteLine("named-attrs-producers-" + throwing + ":" + (ok && Trace == 123 && Probe.DefaultCalls == beforeDefault && Probe.IntCalls == beforeInt));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); MetadataInputs(); Cases("reflection", Reflect); NativePolicy("reflection", Reflect); }
    internal static void RunPlannerChecks(string[] args)
    {
        Culture();
        object value = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationAttributesSubset.PlannerOnly", new object[0]).Unwrap();
        Console.WriteLine("named-attrs-planner-zero:" + ((int)value.GetType().GetField("Value").GetValue(value) == 47));
    }
}
