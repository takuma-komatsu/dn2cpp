using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CoreClrActivationInfoSubset;

public static unsafe class ActivationChecks
{
    const string Diagnostic = "System.RuntimeTypeHandle.GetActivationInfo requires CoreCLR allocator and constructor function-pointer ABI. Use supported public construction APIs instead.";
    public sealed class PublicValue { public int Value = 42; }
    enum Choice { One = 1, Two = 2 }
    delegate void Route(Type type, ref delegate* managed<void*, object> allocator, ref void* argument,
        ref delegate* managed<object, void> referenceCtor, ref delegate* managed<ref byte, void> valueCtor, ref bool isPublic);
    public delegate void Bound(ref delegate* managed<void*, object> allocator, ref void* argument,
        ref delegate* managed<object, void> referenceCtor, ref delegate* managed<ref byte, void> valueCtor, ref bool isPublic);

    // These placeholders are never private CLR oracles. Native fixture rewriting is mandatory.
    static void Direct(Type type, ref delegate* managed<void*, object> a, ref void* b,
        ref delegate* managed<object, void> c, ref delegate* managed<ref byte, void> d, ref bool e) => throw new InvalidOperationException("Unrewritten activation fixture");
    static void Pointer(Type type, ref delegate* managed<void*, object> a, ref void* b,
        ref delegate* managed<object, void> c, ref delegate* managed<ref byte, void> d, ref bool e) => throw new InvalidOperationException("Unrewritten activation fixture");
    static Bound Address(Type type) => throw new InvalidOperationException("Unrewritten activation fixture");
    static void AddressRoute(Type type, ref delegate* managed<void*, object> a, ref void* b,
        ref delegate* managed<object, void> c, ref delegate* managed<ref byte, void> d, ref bool e) => Address(type)(ref a, ref b, ref c, ref d, ref e);
    static void Check(string label, bool value)
    {
        Console.WriteLine("activation-info-" + label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== CoreCLR activation boundary public controls ==");
        Check("public-construction", Activator.CreateInstance<PublicValue>().Value == 42);
        Check("public-enum", Enum.GetName(Choice.Two) == "Two" && Enum.GetValues<Choice>().Length == 2);
        Console.WriteLine("CoreCLR activation boundary public controls end");
    }
    static void Exercise(string name, Route route)
    {
        for (int i = 0; i < 2; i++)
        {
            bool initial = i != 0, flag = initial;
            var a = (delegate* managed<void*, object>)(void*)0x1234;
            void* b = (void*)0x2345;
            var c = (delegate* managed<object, void>)(void*)0x3456;
            var d = (delegate* managed<ref byte, void>)(void*)0x4567;
            bool refused = false;
            try { route(i == 0 ? typeof(PublicValue) : null, ref a, ref b, ref c, ref d, ref flag); }
            catch (PlatformNotSupportedException ex) { refused = ex.Message == Diagnostic; }
            Check(name + "-sentinels-" + i, refused && (void*)a == (void*)0x1234 && b == (void*)0x2345
                && (void*)c == (void*)0x3456 && (void*)d == (void*)0x4567 && flag == initial);
        }
        bool nullRefused = false;
        try
        {
            route(null, ref *(delegate* managed<void*, object>*)0, ref *(void**)0,
                ref *(delegate* managed<object, void>*)0, ref *(delegate* managed<ref byte, void>*)0,
                ref Unsafe.NullRef<bool>());
        }
        catch (PlatformNotSupportedException ex) { nullRefused = ex.Message == Diagnostic; }
        Check(name + "-nullrefs", nullRefused);
    }
    public static void RunArtifact()
    {
        Run();
        Exercise("direct", Direct);
        Exercise("calli", Pointer);
        Exercise("delegate", AddressRoute);
        ReflectionChecks();
    }
    public static void RunReflectionArtifact() { Run(); ReflectionChecks(); }
    static void ReflectionChecks()
    {
        var method = Address(typeof(PublicValue)).Method;
        Check("row", method.Name == "GetActivationInfo" && method.IsStatic && method.GetParameters().Length == 6);
        var arguments = new object[] { typeof(PublicValue), null, null, null, null, true };
        bool refused = false;
        try { method.Invoke(null, arguments); }
        catch (NullReferenceException) { refused = true; }
        Check("reflection-null-policy", refused && arguments[1] == null && arguments[2] == null
            && arguments[3] == null && arguments[4] == null && (bool)arguments[5]);
        arguments[1] = (IntPtr)0x1234;
        refused = false;
        try { method.Invoke(null, arguments); }
        catch (ArgumentException) { refused = true; }
        Check("reflection-pointer-policy", refused && (IntPtr)arguments[1] == (IntPtr)0x1234 && (bool)arguments[5]);
    }
}
