using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace PathActivationSubset;

public class Probe
{
    public static int Calls;
    public Probe() { Calls++; }
}
internal static class Program
{
    private const string Refusal = "Assembly.LoadFrom is not available in AOT-compiled code (no assembly loading)";
    private delegate ObjectHandle Create(string file, string type);
    private static ObjectHandle Direct(string file, string type) => Activator.CreateInstanceFrom(file, type);
    private static ObjectHandle Pointer(string file, string type) => Activator.CreateInstanceFrom(file, type);
    private static Create Address() => Activator.CreateInstanceFrom;
    private static ObjectHandle DelegateRoute(string file, string type) => Address()(file, type);
    private static ObjectHandle Reflect(string file, string type) => (ObjectHandle)Address().Method.Invoke(null, new object[] { file, type });
    private static int Trace;
    private static string P1(int throwing)
    {
        Trace = Trace * 10 + 1;
        if (throwing == 1) throw new FormatException("file producer");
        return null;
    }
    private static string P2(int throwing)
    {
        Trace = Trace * 10 + 2;
        if (throwing == 2) throw new FormatException("type producer");
        return null;
    }
    private static ObjectHandle Producers(int throwing) => Activator.CreateInstanceFrom(P1(throwing), P2(throwing));
    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }
    private static void Cases(string route, Create call)
    {
        string[] paths = { null, "", "SelfHostPrimSubset", "SelfHostPrimSubset.dll", "__missing_path__/file.dll" };
        for (int index = 0; index < paths.Length; index++)
        {
            int calls = Probe.Calls;
            bool ok = false;
            try { call(paths[index], index % 2 == 0 ? null : "PathActivationSubset.Probe"); }
            catch (Exception error)
            {
                if (route != "reflection" || error is TargetInvocationException)
                {
                    Exception leaf = route == "reflection" ? error.InnerException : error;
                    ok = leaf is PlatformNotSupportedException && leaf.Message == Refusal;
                }
            }
            Console.WriteLine("path-activation-" + route + "-" + index + ":" + (ok && Probe.Calls == calls));
        }
    }
    private static ObjectHandle Domain(AppDomain domain, string file, string type) => domain.CreateInstanceFrom(file, type);
    private static void DomainChecks()
    {
        // Actual managed AppDomain storage; this fixture does not expose CurrentDomain support.
        var domain = (AppDomain)RuntimeHelpers.GetUninitializedObject(typeof(AppDomain));
        bool forwarded = false, receiver = false;
        try { Domain(domain, null, null); }
        catch (PlatformNotSupportedException error) { forwarded = error.Message == Refusal; }
        try { Domain(null, null, null); }
        catch (NullReferenceException) { receiver = true; }
        Console.WriteLine("path-activation-domain-forward:" + forwarded);
        Console.WriteLine("path-activation-domain-null-receiver:" + receiver);
    }
    internal static void RunArtifactChecks(string[] args)
    {
        Culture();
        Cases("direct", Direct); Cases("calli", Pointer); Cases("delegate", DelegateRoute); Cases("reflection", Reflect);
        DomainChecks();
        for (int throwing = 0; throwing < 3; throwing++)
        {
            Trace = 0;
            bool ok = false;
            try { Producers(throwing); }
            catch (Exception error)
            {
                ok = throwing == 0 ? error is PlatformNotSupportedException && error.Message == Refusal
                    : error is FormatException && error.Message == (throwing == 1 ? "file producer" : "type producer");
            }
            Console.WriteLine("path-activation-producers-" + throwing + ":" + (ok && Trace == (throwing == 1 ? 1 : 12)));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); Cases("reflection", Reflect); }
    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Path activation public neighbor ==");
        Assembly assembly = typeof(Program).Assembly;
        Console.WriteLine("path-activation-linked-name:" + (assembly == Assembly.Load("SelfHostPrimSubset")));
        Console.WriteLine("Path activation public neighbor end");
    }
    internal static void RunOracleCases()
    {
        Culture();
        string file = typeof(Program).Assembly.Location;
        int before = Probe.Calls;
        ObjectHandle handle = Activator.CreateInstanceFrom(file, "PathActivationSubset.Probe");
        Console.WriteLine("path-public-known:" + (handle.GetType() == typeof(ObjectHandle) && handle.Unwrap() is Probe && ReferenceEquals(handle.Unwrap(), handle.Unwrap()) && Probe.Calls == before + 1));
        PublicFailure("null", null, null, "ArgumentNullException:assemblyFile");
        PublicFailure("empty", "", null, "ArgumentException:path");
        PublicFailure("missing", "__missing_path__/file.dll", null, "FileNotFoundException");
        PublicFailure("null-type", file, null, "ArgumentNullException:name");
        PublicFailure("missing-type", file, "NoSuchType", "TypeLoadException");
    }
    private static void PublicFailure(string label, string file, string type, string expected)
    {
        bool ok = false;
        try { Activator.CreateInstanceFrom(file, type); }
        catch (Exception error)
        {
            string category = error is ArgumentException argument ? error.GetType().Name + ":" + argument.ParamName : error.GetType().Name;
            ok = category == expected;
        }
        Console.WriteLine("path-public-" + label + ":" + ok);
    }
}
