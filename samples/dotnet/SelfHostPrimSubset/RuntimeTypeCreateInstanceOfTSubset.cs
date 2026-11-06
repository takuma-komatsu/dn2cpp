using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeDefaultCtorSubset;

internal static partial class DefaultCtorChecks
{
    private sealed class OfTPlanPublic { public OfTPlanPublic() { } public OfTPlanPublic(int value) { } }
    private sealed class OfTPlanPrivate { private OfTPlanPrivate() { } public OfTPlanPrivate(int value) { } }
    private sealed class OfTPlanGeneric { public OfTPlanGeneric() { } public OfTPlanGeneric(int value) { } }
    private sealed class OfTPlanGenericPrivate { private OfTPlanGenericPrivate() { } public OfTPlanGenericPrivate(int value) { } }
    // Only reference types pass through CreateInstanceOfT in public Activator<T> IL.
    private static object OfTPublic(Type type)
    {
        if (type is null) throw new NullReferenceException();
        if (type == typeof(PublicClass)) return Activator.CreateInstance<PublicClass>();
        if (type == typeof(PrivateClass)) return Activator.CreateInstance<PrivateClass>();
        if (type == typeof(ThrowingPublic)) return Activator.CreateInstance<ThrowingPublic>();
        if (type == typeof(ThrowingPrivate)) return Activator.CreateInstance<ThrowingPrivate>();
        if (type == typeof(NoDefault)) return Activator.CreateInstance<NoDefault>();
        if (type == typeof(AbstractClass)) return Activator.CreateInstance<AbstractClass>();
        if (type == typeof(IMarker)) return Activator.CreateInstance<IMarker>();
        throw new InvalidOperationException("OfT oracle only covers declared reference fixtures");
    }
    private static object OfTDirect(Type type) => OfTPublic(type);
    private static object OfTVirtual(Type type) => OfTPublic(type);
    private static object OfTPointer(Type type) => OfTPublic(type);
    private static Func<object> OfTAddress(Type type) => () => OfTPublic(type);
    private static object OfTAddressQuery(Type type)
    {
        if (type is null) throw new NullReferenceException();
        return OfTAddress(type)();
    }
    private static object OfTReflectionLeaf(Type type) => OfTPublic(type);
    private static object OfTReflection(Type type)
    {
        try
        {
            return typeof(DefaultCtorChecks).GetMethod(nameof(OfTReflectionLeaf), BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { type });
        }
        catch (TargetInvocationException error) { throw error.InnerException; }
    }
    private static object OfTPlannerPublic() => Activator.CreateInstance<OfTPlanPublic>();
    private static object OfTPlannerPrivate() => Activator.CreateInstance<OfTPlanPrivate>();
    private static object OfTGenericPlanner<T>() => Activator.CreateInstance<T>();
    private static void OfTPlannerChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Check("oft-planner-public", OfTPlannerPublic() is OfTPlanPublic);
        Check("oft-planner-private", Error(() => OfTPlannerPrivate()) is MissingMethodException);
        Check("oft-planner-generic", OfTGenericPlanner<OfTPlanGeneric>() is OfTPlanGeneric);
        Check("oft-planner-generic-private", Error(() => OfTGenericPlanner<OfTPlanGenericPrivate>()) is MissingMethodException);
    }
    private static void OfTRoute(string route, Func<Type, object> create)
    {
        string label = "oft-" + route;
        int before = _publicCalls;
        Check(label + "-public", create(typeof(PublicClass)) is PublicClass c && c.Value == 41 && _publicCalls == before + 1);
        before = _privateCalls;
        Check(label + "-private", Error(() => create(typeof(PrivateClass))) is MissingMethodException && _privateCalls == before);
        Check(label + "-private-throw", Error(() => create(typeof(ThrowingPrivate))) is MissingMethodException && _privateCalls == before);
        before = _publicCalls;
        Check(label + "-throw", Error(() => create(typeof(ThrowingPublic))) is TargetInvocationException t
            && t.InnerException is InvalidOperationException && _publicCalls == before + 1);
        Check(label + "-missing", Error(() => create(typeof(NoDefault))) is MissingMethodException);
        Check(label + "-abstract", Error(() => create(typeof(AbstractClass))) is MissingMethodException);
        Check(label + "-interface", Error(() => create(typeof(IMarker))) is MissingMethodException);
        Check(label + "-null", Error(() => create(null)) is NullReferenceException);
    }
    internal static void RunOfT()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== RuntimeType CreateInstanceOfT ==");
        ConstructorInputs();
        OfTRoute("public", OfTPublic);
        Console.WriteLine("RuntimeType CreateInstanceOfT end");
    }
    internal static void RunOfTArtifact(string[] args)
    {
        RunOfT();
        OfTRoute("direct", OfTDirect); OfTRoute("virtual", OfTVirtual);
        OfTRoute("calli", OfTPointer); OfTRoute("address", OfTAddressQuery);
        OfTPlannerChecks();
        RunOfTReflectionArtifact(args);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            // Native boxed-value policy is separate from the reference-type CLR oracle.
            Check("oft-policy-int", OfTDirect(typeof(int)) is int i && i == 0);
            Check("oft-policy-plain", OfTDirect(typeof(Plain)) is Plain p && p.Value == 0);
            Check("oft-policy-explicit", OfTDirect(typeof(Explicit)) is Explicit e && e.Value == 42);
            Check("oft-policy-nullable", OfTDirect(typeof(int?)) is null);
            Check("oft-policy-nullable-enum", OfTDirect(typeof(EnumValue?)) is null);
            Check("oft-policy-nullable-struct", OfTDirect(typeof(Plain?)) is null);
            var factory = OfTAddress(typeof(int?));
            MethodInfo method = factory.Method;
            Check("oft-policy-row", method.Name == "CreateInstanceOfT" && !method.IsStatic
                && !method.IsGenericMethod && method.ReturnType == typeof(object) && method.GetParameters().Length == 0);
            Check("oft-policy-invoke", method.Invoke(typeof(int?), Array.Empty<object>()) is null);
            var bound = (Func<object>)method.CreateDelegate(typeof(Func<object>), typeof(int?));
            Check("oft-policy-bound", bound() is null);
            Check("oft-policy-dynamic", ((Delegate)bound).DynamicInvoke(Array.Empty<object>()) is null);
            Check("oft-policy-wrap", Error(() => method.Invoke(typeof(ThrowingPublic), Array.Empty<object>()))
                is TargetInvocationException outer && outer.InnerException is TargetInvocationException inner
                && inner.InnerException is InvalidOperationException);
        }
    }
    internal static void RunOfTReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== RuntimeType CreateInstanceOfT reflection ==");
        ConstructorInputs();
        OfTRoute("reflection", OfTReflection);
        Console.WriteLine("RuntimeType CreateInstanceOfT reflection end");
    }
}
