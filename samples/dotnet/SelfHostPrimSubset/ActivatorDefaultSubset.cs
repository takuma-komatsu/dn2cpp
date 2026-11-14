using System;
using System.Globalization;
using System.Reflection;

namespace ActivatorDefaultSubset;

internal static class ActivatorChecks
{
    private sealed class PublicClass { public int Value; public PublicClass() { Value = 41; } }
    private sealed class PrivateClass { private PrivateClass() { } }
    private sealed class ThrowingPublic { public ThrowingPublic() { throw new InvalidOperationException("ctor"); } }
    private sealed class ThrowingPrivate { private ThrowingPrivate() { throw new InvalidOperationException("ctor"); } }
    private sealed class PlannerOnly { private PlannerOnly() { } }
    private sealed class NoDefault { public NoDefault(int value) { } }
    private abstract class AbstractClass { }
    private interface IMarker { }
    private struct Plain { public int Value; }
    private struct Explicit { public int Value; public Explicit() { Value = 42; } }
    private enum EnumValue : short { One = 1 }
    private static void Check(string label, bool success)
    {
        Console.WriteLine("activator-default-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }
    private static Exception Error(Action action)
    {
        try { action(); }
        catch (Exception error) { return error; }
        return null;
    }
    private static object Public(Type type, bool nonPublic, bool wrap)
        => Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public
            | (nonPublic ? BindingFlags.NonPublic : 0) | (wrap ? 0 : BindingFlags.DoNotWrapExceptions),
            null, Array.Empty<object>(), null);
    // Runtime-selected routes need concrete constructor roots. PlannerOnly is deliberately absent.
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
            bool nonPublic = n != 0, wrap = w != 0;
            string label = route + "-" + n + "-" + w;
            Check(label + "-class", create(typeof(PublicClass), nonPublic, wrap) is PublicClass c && c.Value == 41);
            Check(label + "-private", nonPublic
                ? create(typeof(PrivateClass), nonPublic, wrap) is PrivateClass
                : Error(() => create(typeof(PrivateClass), nonPublic, wrap)) is MissingMethodException);
            Exception publicError = Error(() => create(typeof(ThrowingPublic), nonPublic, wrap));
            Check(label + "-throw-public", wrap ? publicError is TargetInvocationException t && t.InnerException is InvalidOperationException
                : publicError is InvalidOperationException);
            Exception privateError = Error(() => create(typeof(ThrowingPrivate), nonPublic, wrap));
            Check(label + "-throw-private", !nonPublic ? privateError is MissingMethodException
                : wrap ? privateError is TargetInvocationException privateTarget && privateTarget.InnerException is InvalidOperationException
                : privateError is InvalidOperationException);
            Check(label + "-plain", create(typeof(Plain), nonPublic, wrap) is Plain p && p.Value == 0);
            Check(label + "-explicit", create(typeof(Explicit), nonPublic, wrap) is Explicit e && e.Value == 42);
            Check(label + "-int", create(typeof(int), nonPublic, wrap) is int value && value == 0);
            Check(label + "-nullable-int", create(typeof(int?), nonPublic, wrap) is null);
            Check(label + "-nullable-enum", create(typeof(EnumValue?), nonPublic, wrap) is null);
            Check(label + "-nullable-struct", create(typeof(Plain?), nonPublic, wrap) is null);
            Check(label + "-missing", Error(() => create(typeof(NoDefault), nonPublic, wrap)) is MissingMethodException);
            Check(label + "-abstract", Error(() => create(typeof(AbstractClass), nonPublic, wrap)) is MissingMethodException);
            Check(label + "-interface", Error(() => create(typeof(IMarker), nonPublic, wrap)) is MissingMethodException);
            Check(label + "-open", Error(() => create(typeof(Nullable<>), nonPublic, wrap)) is ArgumentException);
            Check(label + "-null", Error(() => create(null, nonPublic, wrap)) is ArgumentNullException a && a.ParamName == "type");
        }
    }
    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Activator default construction ==");
        ConstructorInputs();
        Route("public", Public);
        Check("public-one-arg-nullable", Activator.CreateInstance(typeof(int?)) is null);
        Check("public-two-arg-nullable", Activator.CreateInstance(typeof(Plain?), true) is null);
        Check("public-array-sibling", Activator.CreateInstance(typeof(PublicClass), Array.Empty<object>(), Array.Empty<object>())
            is PublicClass direct && direct.Value == 41);
        Func<Type, object[], object[], object> sibling = Activator.CreateInstance;
        Check("public-array-sibling-address", sibling(typeof(PublicClass), Array.Empty<object>(), Array.Empty<object>())
            is PublicClass bound && bound.Value == 41);
        Console.WriteLine("Activator default construction end");
    }
}
