using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace NamedActivationFlagsSubset;

public class Probe { public int Value; public Probe() { Value = 1; } public Probe(int value) { Value = value; } }
public class PrivateProbe { public int Value; private PrivateProbe() { Value = 9; } }
public class ThrowProbe { public ThrowProbe() { throw new InvalidOperationException("flags constructor"); } }
public class PlannerInt { public int Value; public PlannerInt(int value) { Value = value; } public PlannerInt(string unused) { } private PlannerInt() { } }
public class PlannerPrivate { public int Value; private PlannerPrivate() { Value = 29; } public PlannerPrivate(int unused) { } }
public class SmokeOnly { public int Value; public SmokeOnly(int value) { Value = value; } }

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
    private const string App = "SelfHostPrimSubset";
    private const string TypeName = "NamedActivationFlagsSubset.Probe";
    private delegate ObjectHandle Create(string assembly, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs);
    private static ObjectHandle Direct(string assembly, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Activator.CreateInstance(assembly, type, ignoreCase, flags, binder, args, culture, attrs);
    private static ObjectHandle Pointer(string assembly, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Activator.CreateInstance(assembly, type, ignoreCase, flags, binder, args, culture, attrs);
    private static Create Address() => Activator.CreateInstance;
    private static ObjectHandle DelegateRoute(string assembly, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => Address()(assembly, type, ignoreCase, flags, binder, args, culture, attrs);
    private static ObjectHandle Reflect(string assembly, string type, bool ignoreCase, BindingFlags flags, Binder binder, object[] args, CultureInfo culture, object[] attrs)
        => (ObjectHandle)Address().Method.Invoke(null, new object[] { assembly, type, ignoreCase, flags, binder, args, culture, attrs });

    private static int Trace;
    private static string P1(string value) { Trace = Trace * 10 + 1; return value; }
    private static string P2(string value) { Trace = Trace * 10 + 2; return value; }
    private static bool P3() { Trace = Trace * 10 + 3; return false; }
    private static BindingFlags P4() { Trace = Trace * 10 + 4; return 0; }
    private static Binder P5() { Trace = Trace * 10 + 5; return null; }
    private static object[] P6() { Trace = Trace * 10 + 6; return null; }
    private static CultureInfo P7(bool throwing) { Trace = Trace * 10 + 7; if (throwing) throw new FormatException("culture producer"); return CultureInfo.InvariantCulture; }
    private static object[] P8() { Trace = Trace * 10 + 8; return null; }
    private static ObjectHandle Producers(bool throwing)
        => Activator.CreateInstance(P1("__MissingFlagsAssembly__"), P2(null), P3(), P4(), P5(), P6(), P7(throwing), P8());
    private static Binder CustomBinder() => (Binder)RuntimeHelpers.GetUninitializedObject(typeof(RefusalBinder));
    private static void Culture() { CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture; }

    private static void Case(string route, string label, Create call, string expected, string assembly = App, string type = TypeName,
        bool ignore = false, BindingFlags flags = 0, Binder binder = null, object[] args = null, CultureInfo culture = null, object[] attrs = null)
    {
        bool ok = false;
        try
        {
            ObjectHandle handle = call(assembly, type, ignore, flags, binder, args, culture, attrs);
            if (expected == "null") ok = handle == null;
            else if (handle != null)
            {
                object value = handle.Unwrap();
                int actual = value is Probe probe ? probe.Value : value is PrivateProbe privateProbe ? privateProbe.Value : -1;
                ok = handle.GetType() == typeof(ObjectHandle) && ReferenceEquals(value, handle.Unwrap()) && expected == actual.ToString(CultureInfo.InvariantCulture);
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
                if (expected.StartsWith("message:", StringComparison.Ordinal)) ok = leaf is PlatformNotSupportedException && leaf.Message == expected.Substring(8);
                else ok = category == expected;
            }
        }
        Console.WriteLine("named-flags-" + route + "-" + label + ":" + ok);
    }

    private static void Cases(string route, Create call)
    {
        Case(route, "default", call, "1");
        Case(route, "int-args", call, "7", args: new object[] { 7 });
        Case(route, "ignorecase", call, "1", type: "namedactivationflagssubset.probe", ignore: true);
        Case(route, "case-sensitive", call, "TypeLoadException", type: "namedactivationflagssubset.probe");
        Case(route, "private", call, "9", type: "NamedActivationFlagsSubset.PrivateProbe", flags: BindingFlags.Instance | BindingFlags.NonPublic);
        Case(route, "wrapped", call, "TargetInvocationException:InvalidOperationException", type: "NamedActivationFlagsSubset.ThrowProbe");
        Case(route, "unwrapped", call, "InvalidOperationException", type: "NamedActivationFlagsSubset.ThrowProbe", flags: BindingFlags.DoNotWrapExceptions);
        Case(route, "culture", call, "8", args: new object[] { (short)8 }, culture: CultureInfo.GetCultureInfo("fr-FR"));
        Case(route, "empty-attrs", call, "1", attrs: Array.Empty<object>());
        Case(route, "attrs", call, "PlatformNotSupportedException", attrs: new object[] { new object() });
        Case(route, "missing-assembly-first", call, "FileNotFoundException", assembly: "__MissingFlagsAssembly__", type: null, attrs: new object[] { new object() });
        Case(route, "null-type-first", call, "ArgumentNullException:name", type: null, attrs: new object[] { new object() });
        Case(route, "missing-type-first", call, "TypeLoadException", type: "NoSuchType", attrs: new object[] { new object() });
        Case(route, "nullable", call, "null", assembly: "System.Private.CoreLib", type: "System.Nullable`1[[System.Int32, System.Private.CoreLib]]");
    }

    private static void NativePolicy(string route, Create call)
    {
        Case(route, "attrs-before-binder", call, "message:Activation Attributes are not supported.", binder: CustomBinder(), attrs: new object[] { new object() });
        Case(route, "native-null-assembly", call, "message:Named activation with a null assembly name requires caller-sensitive assembly resolution", assembly: null, type: null);
        Case(route, "native-binder", call, "message:a custom reflection Binder is not supported in AOT-compiled code (pass null / Type.DefaultBinder)", binder: CustomBinder());
    }

    internal static void RunOracleCases()
    {
        Culture();
        Cases("public", Activator.CreateInstance);
        Case("public", "default-binder", Activator.CreateInstance, "1", binder: Type.DefaultBinder);
    }

    internal static void Run()
    {
        Culture();
        Console.WriteLine("== Named activation flags literal smoke ==");
        ObjectHandle handle = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationFlagsSubset.SmokeOnly", false,
            BindingFlags.Public | BindingFlags.Instance, null, new object[] { 19 }, null, null);
        object value = handle.Unwrap();
        Console.WriteLine("named-flags-smoke:" + ((int)value.GetType().GetField("Value").GetValue(value) == 19 && ReferenceEquals(value, handle.Unwrap())));
        Console.WriteLine("Named activation flags literal smoke end");
    }

    private static void MetadataInputs() => GC.KeepAlive(typeof(int?));
    internal static void RunArtifactChecks(string[] args)
    {
        Culture(); MetadataInputs();
        Cases("direct", Direct); NativePolicy("direct", Direct);
        Cases("calli", Pointer); NativePolicy("calli", Pointer);
        Cases("delegate", DelegateRoute); NativePolicy("delegate", DelegateRoute);
        Cases("reflection", Reflect); NativePolicy("reflection", Reflect);
        foreach (bool throwing in new[] { false, true })
        {
            Trace = 0; bool ok = false;
            try { Producers(throwing); }
            catch (Exception error) { ok = throwing ? error is FormatException && error.Message == "culture producer" : error is System.IO.FileNotFoundException; }
            Console.WriteLine("named-flags-producers-" + throwing + ":" + (ok && Trace == (throwing ? 1234567 : 12345678)));
        }
    }
    internal static void RunReflectionChecks(string[] args) { Culture(); MetadataInputs(); Cases("reflection", Reflect); NativePolicy("reflection", Reflect); }
    internal static void RunPlannerChecks(string[] args)
    {
        Culture();
        object first = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationFlagsSubset.PlannerInt", false,
            BindingFlags.Public | BindingFlags.Instance, null, new object[] { 17 }, null, null).Unwrap();
        object second = Activator.CreateInstance("SelfHostPrimSubset", "NamedActivationFlagsSubset.PlannerPrivate", false,
            BindingFlags.NonPublic | BindingFlags.Instance, null, null, null, null).Unwrap();
        Console.WriteLine("named-flags-planner-int:" + ((int)first.GetType().GetField("Value").GetValue(first) == 17));
        Console.WriteLine("named-flags-planner-private:" + ((int)second.GetType().GetField("Value").GetValue(second) == 29));
    }
}
