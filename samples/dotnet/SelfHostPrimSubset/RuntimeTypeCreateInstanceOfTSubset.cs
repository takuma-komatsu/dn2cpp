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
}
