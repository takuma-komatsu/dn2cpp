using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gen2CallbackSubset;

internal static class Program
{
    private const string BodyDiagnostic = "Gen2 GC notification objects are not supported";
    private const string AllocationDiagnostic = "Allocation of this runtime type is not supported";
    private static int callbacks;
    private static int trace;
    private static int checks;

    internal delegate void Callback0(Func<bool> callback);
    internal delegate void Callback1(Func<object, bool> callback, object marker);
    internal delegate void Finish();

    private static bool Callback() { callbacks++; return true; }
    private static bool CallbackWithTarget(object marker) { callbacks++; return true; }
    private static Func<bool> ProduceCallback() { trace = trace * 10 + 1; return Callback; }
    private static Func<object, bool> ProduceTargetCallback() { trace = trace * 10 + 1; return CallbackWithTarget; }
    private static object ProduceTarget() { trace = trace * 10 + 2; return new object(); }
    private static object ThrowTarget() { trace = trace * 10 + 2; throw new FormatException("target producer"); }

    // The native artifact rewrites these methods to actual CoreLib members.
    private static Type TargetType() => throw new InvalidOperationException("fixture required");
    private static void Direct0(object receiver, Func<bool> callback) => throw new InvalidOperationException("fixture required");
    private static void Direct1(object receiver, Func<object, bool> callback, object marker) => throw new InvalidOperationException("fixture required");
    private static void DirectFinal(object receiver) => throw new InvalidOperationException("fixture required");
    private static void Pointer0(object receiver, Func<bool> callback) => throw new InvalidOperationException("fixture required");
    private static void Pointer1(object receiver, Func<object, bool> callback, object marker) => throw new InvalidOperationException("fixture required");
    private static void PointerFinal(object receiver) => throw new InvalidOperationException("fixture required");
    private static Callback0 Address0(object receiver) => throw new InvalidOperationException("fixture required");
    private static Callback1 Address1(object receiver) => throw new InvalidOperationException("fixture required");
    private static Finish AddressFinal(object receiver) => throw new InvalidOperationException("fixture required");
    private static ConstructorInfo Constructor0() => TargetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Func<bool>) }, null);
    private static ConstructorInfo Constructor1() => TargetType().GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Func<object, bool>), typeof(object) }, null);
    private static MethodInfo FinalizerMethod() => TargetType().GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic);
    private static void Register0(Func<bool> callback) => throw new InvalidOperationException("fixture required");
    private static void Register1(Func<object, bool> callback, object marker) => throw new InvalidOperationException("fixture required");
    private static void StaticNewobj() => throw new InvalidOperationException("fixture required");

    private static void Check(string label, bool value)
    {
        Console.WriteLine("gen2-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
        checks++;
    }

    private static void Refused(string label, Action action, string diagnostic = BodyDiagnostic)
    {
        try { action(); }
        catch (PlatformNotSupportedException error)
        {
            if (error.Message != diagnostic) Console.WriteLine("gen2-unexpected-diagnostic:" + label + ":" + error.Message);
            Check(label, error.Message == diagnostic);
            return;
        }
        throw new InvalidOperationException(label + " did not refuse");
    }

    private static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public static void RunDirectChecks()
    {
        PinCulture();
        Refused("ctor0-direct-null", () => Direct0(null, Callback));
        Refused("ctor1-direct-null", () => Direct1(null, CallbackWithTarget, new object()));
        Refused("final-direct-null", () => DirectFinal(null));
        Refused("ctor0-pointer-null", () => Pointer0(null, Callback));
        Refused("ctor1-pointer-null", () => Pointer1(null, CallbackWithTarget, new object()));
        Refused("final-pointer-null", () => PointerFinal(null));
        trace = 0;
        Refused("producer0", () => Direct0(null, ProduceCallback()));
        Check("producer0-once", trace == 1);
        trace = 0;
        Refused("producer1", () => Direct1(null, ProduceTargetCallback(), ProduceTarget()));
        Check("producer1-order", trace == 12);
        trace = 0;
        try { Direct1(null, ProduceTargetCallback(), ThrowTarget()); }
        catch (FormatException e)
        {
            Check("producer-throws-first", e.Message == "target producer" && trace == 12);
            Check("callbacks-untouched-direct", callbacks == 0);
            return;
        }
        throw new InvalidOperationException("throwing producer did not preempt constructor");
    }

    public static void RunDelegateChecks()
    {
        PinCulture();
        BindingRefused("ctor0-null-delegate", () => Address0(null));
        BindingRefused("ctor1-null-delegate", () => Address1(null));
        BindingRefused("final-null-delegate", () => AddressFinal(null));
        RunSuppliedDelegateChecks(null);
        Check("callbacks-untouched-delegate", callbacks == 0);
    }

    private static void BindingRefused(string label, Action action)
    {
        try { action(); }
        catch (ArgumentException error)
        {
            Check(label, error.Message == "Delegate to an instance method cannot have null 'this'.");
            return;
        }
        throw new InvalidOperationException(label + " did not reject binding");
    }

    // The native probe supplies header-only storage, never a managed Gen2 object.
    public static void RunSuppliedDelegateChecks(object receiver)
    {
        if (receiver == null) return;
        Refused("ctor0-supplied-delegate", () => Address0(receiver)(Callback));
        Refused("ctor1-supplied-delegate", () => Address1(receiver)(CallbackWithTarget, new object()));
        Refused("final-supplied-delegate", () => AddressFinal(receiver)());
        try { FinalizerMethod().Invoke(receiver, null); }
        catch (TargetInvocationException error)
        {
            Check("final-supplied-reflection", error.InnerException is PlatformNotSupportedException && error.InnerException.Message == BodyDiagnostic);
            Check("callbacks-untouched-supplied-delegate", callbacks == 0);
            return;
        }
        throw new InvalidOperationException("supplied finalizer reflection did not refuse");
    }

    public static void RunReflectionChecks()
    {
        PinCulture();
        // The allocator fence precedes target allocation and target invocation.
        Refused("ctor0-reflection", () => Constructor0().Invoke(new object[] { (Func<bool>)Callback }), AllocationDiagnostic);
        Refused("ctor1-reflection", () => Constructor1().Invoke(new object[] { (Func<object, bool>)CallbackWithTarget, new object() }), AllocationDiagnostic);
        Refused("uninitialized", () => RuntimeHelpers.GetUninitializedObject(TargetType()), AllocationDiagnostic);
        Check("final-metadata", FinalizerMethod().Name == "Finalize");
        bool nullTarget = false;
        try { FinalizerMethod().Invoke(null, null); }
        catch (TargetException) { nullTarget = true; }
        Check("final-reflection-null-target", nullTarget);
        Check("callbacks-untouched-reflection", callbacks == 0);
        // Positive native allocator/finalizer instrumentation control, not Gen2 semantics.
        GC.KeepAlive(new AllocationControl());
    }

    public static void RunRegisterChecks()
    {
        PinCulture();
        Register0(Callback);
        Register1(CallbackWithTarget, new object());
        Check("register-policy-no-callback", callbacks == 0);
    }

    public static void RunArtifactChecks()
    {
        RunDirectChecks();
        RunDelegateChecks();
        RunReflectionChecks();
        RunRegisterChecks();
        Console.WriteLine("gen2-native-end:" + checks);
    }

    public static void RunNewobjNegative()
    {
        PinCulture();
        StaticNewobj();
    }
}

internal sealed class AllocationControl { ~AllocationControl() { } }
