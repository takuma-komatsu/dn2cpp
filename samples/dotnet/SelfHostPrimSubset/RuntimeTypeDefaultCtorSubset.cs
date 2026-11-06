using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeDefaultCtorSubset;

internal static partial class DefaultCtorChecks
{
    private static int _publicCalls, _privateCalls;
    private sealed class PublicClass { public int Value; public PublicClass() { _publicCalls++; Value = 41; } }
    private sealed class PrivateClass { private PrivateClass() { _privateCalls++; } }
    private sealed class ThrowingPublic { public ThrowingPublic() { _publicCalls++; throw new InvalidOperationException("ctor"); } }
    private sealed class ThrowingPrivate { private ThrowingPrivate() { _privateCalls++; throw new InvalidOperationException("ctor"); } }
    private sealed class PlannerPrivateOnly { private PlannerPrivateOnly() { } }
    private sealed class PlannerPublicOnly { private PlannerPublicOnly() { } }
    private sealed class PlannerUnknownOnly { private PlannerUnknownOnly() { } }
    private sealed class GenericOnly { private GenericOnly() { } public GenericOnly(int value) { } }
    private struct PlannerValueOnly { public int Value; }
    private sealed class NoDefault { public NoDefault(int value) { } }
    private abstract class AbstractClass { }
    private interface IMarker { }
    private struct Plain { public int Value; }
    private struct Explicit { public int Value; public Explicit() { Value = 42; } }
    private enum EnumValue : short { One = 1 }
    private static void Check(string label, bool success)
    {
        Console.WriteLine("runtime-type-default-ctor-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }
    private static Exception Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }
    // The instance null check follows the retained method's receiver dereference.
    // All non-null oracle execution uses public Activator, never a private VM entry.
    private static object Public(Type type, bool publicOnly, bool wrap)
    {
        if (type is null) throw new NullReferenceException();
        return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public
            | (publicOnly ? 0 : BindingFlags.NonPublic) | (wrap ? 0 : BindingFlags.DoNotWrapExceptions),
            null, Array.Empty<object>(), null);
    }
    private static object Direct(Type type, bool publicOnly, bool wrap) => Public(type, publicOnly, wrap);
    private static object Virtual(Type type, bool publicOnly, bool wrap) => Public(type, publicOnly, wrap);
    private static object Pointer(Type type, bool publicOnly, bool wrap) => Public(type, publicOnly, wrap);
    private static Func<bool, bool, object> Address(Type type) => (publicOnly, wrap) => Public(type, publicOnly, wrap);
    private static object AddressQuery(Type type, bool publicOnly, bool wrap)
    {
        if (type is null) throw new NullReferenceException();
        return Address(type)(publicOnly, wrap);
    }
    private static object ReflectionLeaf(Type type, bool publicOnly, bool wrap) => Public(type, publicOnly, wrap);
    private static object Reflection(Type type, bool publicOnly, bool wrap)
    {
        try
        {
            return typeof(DefaultCtorChecks).GetMethod(nameof(ReflectionLeaf), BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { type, publicOnly, wrap });
        }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    // Each fixture body has a sole typeof receiver and the exact instance call.
    private static object PlannerPrivate() => Public(typeof(PlannerPrivateOnly), false, true);
    private static object PlannerPublic() => Public(typeof(PlannerPublicOnly), true, true);
    private static object PlannerUnknown(bool publicOnly) => Public(typeof(PlannerUnknownOnly), publicOnly, true);
    private static object GenericPlanner<T>() => Public(typeof(T), false, true);
    private static object PlannerValue() => Public(typeof(PlannerValueOnly), true, true);
    // Dynamic receiver routes need explicit roots; planner-only types are absent.
    private static void ConstructorInputs()
    {
        Activator.CreateInstance(typeof(PublicClass), true);
        Activator.CreateInstance(typeof(PrivateClass), true);
        Error(() => Activator.CreateInstance(typeof(ThrowingPublic), true));
        Error(() => Activator.CreateInstance(typeof(ThrowingPrivate), true));
        Activator.CreateInstance(typeof(Plain), true);
        Activator.CreateInstance(typeof(Explicit), true);
        Activator.CreateInstance(typeof(int), true);
        Activator.CreateInstance(typeof(int?), true);
        Activator.CreateInstance(typeof(EnumValue?), true);
        Activator.CreateInstance(typeof(Plain?), true);
    }
    private static void Route(string route, Func<Type, bool, bool, object> create)
    {
        for (int n = 0; n != 2; ++n)
        for (int w = 0; w != 2; ++w)
        {
            bool publicOnly = n != 0, wrap = w != 0;
            string label = route + "-" + n + "-" + w;
            int before = _publicCalls;
            Check(label + "-class", create(typeof(PublicClass), publicOnly, wrap) is PublicClass c && c.Value == 41 && _publicCalls == before + 1);
            before = _privateCalls;
            Check(label + "-private", !publicOnly
                ? create(typeof(PrivateClass), publicOnly, wrap) is PrivateClass && _privateCalls == before + 1
                : Error(() => create(typeof(PrivateClass), publicOnly, wrap)) is MissingMethodException && _privateCalls == before);
            before = _publicCalls;
            Exception publicError = Error(() => create(typeof(ThrowingPublic), publicOnly, wrap));
            Check(label + "-throw-public", _publicCalls == before + 1 && (wrap ? publicError is TargetInvocationException t && t.InnerException is InvalidOperationException
                : publicError is InvalidOperationException));
            before = _privateCalls;
            Exception privateError = Error(() => create(typeof(ThrowingPrivate), publicOnly, wrap));
            Check(label + "-throw-private", publicOnly ? privateError is MissingMethodException && _privateCalls == before
                : _privateCalls == before + 1 && (wrap ? privateError is TargetInvocationException privateTarget && privateTarget.InnerException is InvalidOperationException
                : privateError is InvalidOperationException));
            Check(label + "-plain", create(typeof(Plain), publicOnly, wrap) is Plain p && p.Value == 0);
            Check(label + "-explicit", create(typeof(Explicit), publicOnly, wrap) is Explicit e && e.Value == 42);
            Check(label + "-int", create(typeof(int), publicOnly, wrap) is int value && value == 0);
            Check(label + "-nullable-int", create(typeof(int?), publicOnly, wrap) is null);
            Check(label + "-nullable-enum", create(typeof(EnumValue?), publicOnly, wrap) is null);
            Check(label + "-nullable-struct", create(typeof(Plain?), publicOnly, wrap) is null);
            Check(label + "-missing", Error(() => create(typeof(NoDefault), publicOnly, wrap)) is MissingMethodException);
            Check(label + "-abstract", Error(() => create(typeof(AbstractClass), publicOnly, wrap)) is MissingMethodException);
            Check(label + "-interface", Error(() => create(typeof(IMarker), publicOnly, wrap)) is MissingMethodException);
            Check(label + "-open", Error(() => create(typeof(Nullable<>), publicOnly, wrap)) is ArgumentException);
            Check(label + "-null", Error(() => create(null, publicOnly, wrap)) is NullReferenceException);
        }
    }
    private static void PlannerChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("planner-generic", GenericPlanner<GenericOnly>() is GenericOnly);
        Check("planner-private", PlannerPrivate() is PlannerPrivateOnly);
        Check("planner-public", Error(() => PlannerPublic()) is MissingMethodException);
        Check("planner-value", PlannerValue() is PlannerValueOnly value && value.Value == 0);
        for (int p = 0; p != 2; p++)
            Check("planner-unknown-" + p, p == 0 ? PlannerUnknown(false) is PlannerUnknownOnly
                : Error(() => PlannerUnknown(true)) is MissingMethodException);
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== RuntimeType default construction ==");
        ConstructorInputs();
        Route("public", Public);
        Console.WriteLine("RuntimeType default construction end");
    }
    internal static void RunArtifact(string[] args)
    {
        Run();
        Route("direct", Direct); Route("virtual", Virtual); Route("calli", Pointer); Route("address", AddressQuery);
        PlannerChecks();
        RunReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            var factory = Address(typeof(int?));
            MethodInfo method = factory.Method;
            Check("address-row", method.Name == "CreateInstanceDefaultCtor" && !method.IsStatic
                && method.ReturnType == typeof(object) && method.GetParameters().Length == 2);
            Check("address-invoke", method.Invoke(typeof(int?), new object[] { false, true }) is null);
            var bound = (Func<bool, bool, object>)method.CreateDelegate(typeof(Func<bool, bool, object>), typeof(int?));
            Check("address-bound", bound(true, false) is null);
            Check("address-dynamic", ((Delegate)bound).DynamicInvoke(false, false) is null);
            Check("address-wrap", Error(() => method.Invoke(typeof(ThrowingPublic), new object[] { false, false }))
                is TargetInvocationException outer && outer.InnerException is InvalidOperationException);
        }
    }
    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== RuntimeType default reflection ==");
        ConstructorInputs();
        Route("reflection", Reflection);
        Console.WriteLine("RuntimeType default reflection end");
    }
}
