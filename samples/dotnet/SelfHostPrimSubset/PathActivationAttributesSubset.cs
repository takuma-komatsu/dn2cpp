using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace PathActivationAttributesSubset;

public class Probe
{
    public static int Calls;
    public Probe() { Calls++; }
    public Probe(int value) { Calls++; }
}
internal static class Program
{
    private const string Refusal = "Assembly.LoadFrom is not available in AOT-compiled code (no assembly loading)";
    private delegate ObjectHandle Create(string file, string type, object[] attrs);
    private static ObjectHandle Direct(string file, string type, object[] attrs) => Activator.CreateInstanceFrom(file, type, attrs);
    private static ObjectHandle Pointer(string file, string type, object[] attrs) => Activator.CreateInstanceFrom(file, type, attrs);
    private static Create Address() => Activator.CreateInstanceFrom;
    private static ObjectHandle DelegateRoute(string file, string type, object[] attrs) => Address()(file, type, attrs);
    private static ObjectHandle Reflect(string file, string type, object[] attrs) => (ObjectHandle)Address().Method.Invoke(null, new object[] { file, type, attrs });
    private static int Trace;
    private static void Step(int position, int throwing)
    {
        Trace = Trace * 10 + position;
        if (position == throwing) throw new FormatException("producer " + position);
    }
    private static string P1(int n) { Step(1, n); return null; }
    private static string P2(int n) { Step(2, n); return null; }
    private static object[] P3(int n) { Step(3, n); return new object[] { 7 }; }
    private static ObjectHandle Producers(int n) => Activator.CreateInstanceFrom(P1(n), P2(n), P3(n));
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Cases(string route, Create call)
    {
        string[] files = { null, "", "SelfHostPrimSubset", "SelfHostPrimSubset.dll", "__missing_file__.dll" };
        object[][] attributes = { null, Array.Empty<object>(), new object[] { 7 } };
        for (int file = 0; file < files.Length; file++)
        for (int attr = 0; attr < attributes.Length; attr++)
        {
            bool ok = false;
            int calls = Probe.Calls;
            try { call(files[file], "PathActivationAttributesSubset.Probe", attributes[attr]); }
            catch (Exception error)
            {
                if (route != "reflection" || error is TargetInvocationException)
                {
                    Exception leaf = route == "reflection" ? error.InnerException : error;
                    ok = leaf is PlatformNotSupportedException && leaf.Message == Refusal;
                }
            }
            Console.WriteLine("path-attrs-" + route + "-" + file + "-" + attr + ":" + (ok && Probe.Calls == calls));
        }
    }
    private static ObjectHandle Domain(AppDomain domain) => domain.CreateInstanceFrom(null, null, new object[] { 7 });
    private static void DomainChecks()
    {
        var domain = (AppDomain)RuntimeHelpers.GetUninitializedObject(typeof(AppDomain));
        bool forwarded = false, nullReceiver = false;
        try { Domain(domain); }
        catch (PlatformNotSupportedException error) { forwarded = error.Message == Refusal; }
        try { Domain(null); }
        catch (NullReferenceException) { nullReceiver = true; }
        Console.WriteLine("path-attrs-domain-forward:" + forwarded);
        Console.WriteLine("path-attrs-domain-null-receiver:" + nullReceiver);
    }
    internal static void RunArtifactChecks(string[] args)
    {
        Culture();
        Cases("direct", Direct); Cases("calli", Pointer); Cases("delegate", DelegateRoute); Cases("reflection", Reflect);
        DomainChecks();
        for (int throwing = 0; throwing <= 3; throwing++)
        {
            Trace = 0;
            bool ok = false;
            try { Producers(throwing); }
            catch (Exception error)
            {
                ok = throwing == 0 ? error is PlatformNotSupportedException && error.Message == Refusal
                    : error is FormatException && error.Message == "producer " + throwing;
            }
            int expected = throwing == 1 ? 1 : throwing == 2 ? 12 : 123;
            Console.WriteLine("path-attrs-producers-" + throwing + ":" + (ok && Trace == expected));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); Cases("reflection", Reflect); }
    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Path activation attributes public neighbor ==");
        Console.WriteLine("path-attrs-linked-name:" + (typeof(Program).Assembly == Assembly.Load("SelfHostPrimSubset")));
        Console.WriteLine("Path activation attributes public neighbor end");
    }
    internal static void RunOracleCases()
    {
        Culture();
        string file = typeof(Program).Assembly.Location;
        int before = Probe.Calls;
        ObjectHandle handle = Activator.CreateInstanceFrom(file, "PathActivationAttributesSubset.Probe", null);
        Console.WriteLine("path-attrs-public-known:" + (handle.GetType() == typeof(ObjectHandle) && handle.Unwrap() is Probe
            && ReferenceEquals(handle.Unwrap(), handle.Unwrap()) && Probe.Calls == before + 1));
        bool nullFile = false, nonempty = false;
        try { Activator.CreateInstanceFrom(null, null, new object[] { 7 }); }
        catch (ArgumentNullException error) { nullFile = error.ParamName == "assemblyFile"; }
        before = Probe.Calls;
        try { Activator.CreateInstanceFrom(file, "PathActivationAttributesSubset.Probe", new object[] { 7 }); }
        catch (PlatformNotSupportedException) { nonempty = Probe.Calls == before; }
        Console.WriteLine("path-attrs-public-null-file-first:" + nullFile);
        Console.WriteLine("path-attrs-public-nonempty:" + nonempty);
    }
}
