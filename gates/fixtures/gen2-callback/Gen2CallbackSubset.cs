using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gen2CallbackSubset;

// The native-only rewriter replaces the stubs below with references to CoreLib's
// private Gen2GcCallback. Reflective activation refuses before the target is
// allocated, so no callback ever runs.
internal static class Program
{
    private const string AllocationDiagnostic = "Allocation of this runtime type is not supported";
    private static int callbacks;

    private static bool Callback() { callbacks++; return true; }
    private static bool CallbackWithTarget(object marker) { callbacks++; return true; }

    private static Type TargetType() => throw new InvalidOperationException("fixture required");
    private static void StaticNewobj() => throw new InvalidOperationException("fixture required");
    private static void MalformedAddress() => throw new InvalidOperationException("fixture required");
    private static ConstructorInfo Constructor0() => TargetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Func<bool>) }, null);
    private static ConstructorInfo Constructor1() => TargetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Func<object, bool>), typeof(object) }, null);
    private static MethodInfo FinalizerMethod() => TargetType().GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic);

    private static void Check(string label, bool value)
    {
        Console.WriteLine("gen2-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }

    private static void Refused(string label, Action action)
    {
        try { action(); }
        catch (PlatformNotSupportedException error)
        {
            if (error.Message != AllocationDiagnostic) Console.WriteLine("gen2-unexpected-diagnostic:" + label + ":" + error.Message);
            Check(label, error.Message == AllocationDiagnostic);
            return;
        }
        throw new InvalidOperationException(label + " did not refuse");
    }

    public static void RunReflectionChecks()
    {
        Refused("ctor0-reflection", () => Constructor0().Invoke(new object[] { (Func<bool>)Callback }));
        Refused("ctor1-reflection", () => Constructor1().Invoke(new object[] { (Func<object, bool>)CallbackWithTarget, new object() }));
        Refused("uninitialized", () => RuntimeHelpers.GetUninitializedObject(TargetType()));
        Check("final-metadata", FinalizerMethod().Name == "Finalize");
        bool nullTarget = false;
        try { FinalizerMethod().Invoke(null, null); }
        catch (TargetException) { nullTarget = true; }
        Check("final-reflection-null-target", nullTarget);
        Check("callbacks-untouched-reflection", callbacks == 0);
    }

    public static void RunNewobjNegative() => StaticNewobj();

    public static void RunMemberRefNegative() => MalformedAddress();

    public static void MinimalMetadataAnchor(int value) { }

    private static Type MinimalTargetType() => typeof(Program).GetMethod(nameof(MinimalMetadataAnchor))
        .GetParameters()[0].GetOptionalCustomModifiers()[0];

    public static void RunMinimalChecks()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        MinimalMetadataAnchor(0);
        Refused("minimal-uninitialized", () => RuntimeHelpers.GetUninitializedObject(MinimalTargetType()));
    }
}
