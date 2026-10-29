using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;

namespace NamedActivationSubset;

public class PublicTarget { public int Value; public PublicTarget() { Value = 37; } }
public class ThrowingTarget { public ThrowingTarget() { throw new InvalidOperationException("named constructor"); } }
public class PrivateTarget { private PrivateTarget() { } }
public class MissingTarget { public MissingTarget(int value) { } }
public class PlannerOnly { public int Value; public PlannerOnly() { Value = 73; } public PlannerOnly(int unused) { } }

internal static class Program
{
    private const string App = "SelfHostPrimSubset";
    private const string Core = "System.Private.CoreLib";
    private static int Trace;
    private static void MetadataInputs()
    {
        GC.KeepAlive(new[] { typeof(PrivateTarget), typeof(MissingTarget), typeof(int?), typeof(int[]), typeof(int[,]) });
    }
    private static ObjectHandle Direct(string assembly, string type) => Activator.CreateInstance(assembly, type);
    private static ObjectHandle Pointer(string assembly, string type) => Activator.CreateInstance(assembly, type);
    private static Func<string, string, ObjectHandle> Address() => Activator.CreateInstance;
    private static ObjectHandle DelegateRoute(string assembly, string type) => Address()(assembly, type);
    private static ObjectHandle Reflect(string assembly, string type) => (ObjectHandle)Address().Method.Invoke(null, new object[] { assembly, type });
    private static string AssemblyProducer(string value) { Trace = Trace * 10 + 1; return value; }
    private static string TypeProducer(string value) { Trace = Trace * 10 + 2; return value; }
    private static string ThrowingTypeProducer(string value) { Trace = Trace * 10 + 2; throw new FormatException("type producer"); }
    private static ObjectHandle ProducerRoute(string assembly, string type, bool throwing)
        => Activator.CreateInstance(AssemblyProducer(assembly), throwing ? ThrowingTypeProducer(type) : TypeProducer(type));

    private static void Culture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    private static void Case(string route, string label, Func<string, string, ObjectHandle> call, string assembly, string type, string expected)
    {
        bool ok = false;
        try
        {
            ObjectHandle handle = call(assembly, type);
            if (expected == "null") ok = handle == null;
            else if (handle != null)
            {
                object value = handle.Unwrap();
                ok = handle.GetType() == typeof(ObjectHandle) && ReferenceEquals(value, handle.Unwrap())
                    && (expected == "class" ? value is PublicTarget target && target.Value == 37 : expected == "int" && value is int number && number == 0);
            }
        }
        catch (Exception error)
        {
            bool outer = route == "reflection";
            if (!outer || error is TargetInvocationException)
            {
                Exception leaf = outer ? error.InnerException : error;
                string category = leaf is ArgumentException argument ? leaf.GetType().Name + ":" + argument.ParamName
                    : leaf is TargetInvocationException invocation ? "TargetInvocationException:" + invocation.InnerException.GetType().Name
                    : leaf.GetType().Name;
                ok = category == expected;
            }
        }
        Console.WriteLine("named-activation-" + route + "-" + label + ":" + ok);
    }

    private static void Cases(string route, Func<string, string, ObjectHandle> call)
    {
        Case(route, "class", call, App, "NamedActivationSubset.PublicTarget", "class");
        Case(route, "int", call, Core, "System.Int32", "int");
        Case(route, "sz-array", call, Core, "System.Int32[]", "MissingMethodException");
        Case(route, "md-array", call, Core, "System.Int32[,]", "MissingMethodException");
        Case(route, "nullable", call, Core, "System.Nullable`1[[System.Int32, System.Private.CoreLib]]", "null");
        Case(route, "null-type", call, App, null, "ArgumentNullException:name");
        Case(route, "empty-type", call, App, "", "ArgumentException:name");
        Case(route, "empty-assembly-first", call, "", null, "ArgumentException:assemblyName");
        Case(route, "missing-assembly-first", call, "__MissingNamedActivationAssembly__", null, "FileNotFoundException");
        Case(route, "missing-type", call, App, "NamedActivationSubset.DoesNotExist", "TypeLoadException");
        Case(route, "private", call, App, "NamedActivationSubset.PrivateTarget", "MissingMethodException");
        Case(route, "missing-ctor", call, App, "NamedActivationSubset.MissingTarget", "MissingMethodException");
        Case(route, "throwing", call, App, "NamedActivationSubset.ThrowingTarget", "TargetInvocationException:InvalidOperationException");
    }

    private static void NativeNull(string route, Func<string, string, ObjectHandle> call)
    {
        bool ok = false;
        try { call(null, null); }
        catch (Exception error)
        {
            bool wrapped = error is TargetInvocationException;
            Exception leaf = wrapped ? error.InnerException : error;
            ok = wrapped == (route == "reflection") && leaf is PlatformNotSupportedException
                && leaf.Message == "Named activation with a null assembly name requires caller-sensitive assembly resolution";
        }
        Console.WriteLine("named-activation-" + route + "-native-null-assembly:" + ok);
    }

    private static void DomainNull() => AppDomain.CurrentDomain.CreateInstance(null, "ignored");

    private static void DomainChecks()
    {
        bool ok = false;
        try { DomainNull(); }
        catch (ArgumentNullException error) { ok = error.ParamName == "assemblyName"; }
        Console.WriteLine("named-activation-domain-null:" + ok);
    }

    private static void Producers()
    {
        foreach (bool throwing in new[] { false, true })
        {
            Trace = 0;
            bool ok = false;
            try { ProducerRoute("__MissingNamedActivationAssembly__", null, throwing); }
            catch (Exception error)
            {
                ok = throwing ? error is FormatException && error.Message == "type producer" : error is System.IO.FileNotFoundException;
            }
            Console.WriteLine("named-activation-producers-" + throwing + ":" + (ok && Trace == 12));
        }
    }

    internal static void RunOracleCases()
    {
        Culture();
        Console.WriteLine("== Named activation public contract ==");
        Cases("public", Activator.CreateInstance);
        DomainChecks();
        Console.WriteLine("Named activation public contract end");
    }

    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Named activation literal smoke ==");
        ObjectHandle handle = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationSubset.PublicTarget");
        object value = handle.Unwrap();
        Console.WriteLine("named-activation-smoke:" + (handle.GetType() == typeof(ObjectHandle)
            && value is PublicTarget target && target.Value == 37 && ReferenceEquals(value, handle.Unwrap())));
        Console.WriteLine("Named activation literal smoke end");
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Culture();
        MetadataInputs();
        Cases("direct", Direct); NativeNull("direct", Direct);
        Cases("calli", Pointer); NativeNull("calli", Pointer);
        Cases("delegate", DelegateRoute); NativeNull("delegate", DelegateRoute);
        Cases("reflection", Reflect); NativeNull("reflection", Reflect);
        Producers();
        DomainChecks();
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Culture();
        MetadataInputs();
        Cases("reflection", Reflect); NativeNull("reflection", Reflect);
    }

    internal static void RunPlannerChecks(string[] args)
    {
        Culture();
        ObjectHandle handle = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationSubset.PlannerOnly");
        object value = handle.Unwrap();
        // Field lookup consumes the result without alternate typeof/new/constructor roots.
        Console.WriteLine("named-activation-planner:" + ((int)value.GetType().GetField("Value").GetValue(value) == 73));
    }
}
