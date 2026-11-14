using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RuntimeTypeInterfaceCheckSubset;

internal static class Program
{
    private interface IMarker { }
    private interface IBase { void M(); }
    private interface IDerived : IBase { }
    private interface IOut<out T> { T Get(); }
    private interface IIn<in T> { void Set(T value); }
    private class Base : IBase { public void M() { } }
    private sealed class Child : Base, IDerived, IMarker { }
    private sealed class Producer : IOut<string> { public string Get() => "value"; }
    private sealed class Consumer : IIn<object> { public void Set(object value) { } }
    private sealed class Open<T> : IMarker, IOut<T> { public T Get() => default; }
    private sealed class Parameter<T> where T : IBase { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("interface-check-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static Type OpenSource() => typeof(Open<>).MakeGenericType(typeof(Parameter<>).GetGenericArguments()[0]);
    private static Type BoundInterface() => typeof(IOut<>).MakeGenericType(typeof(Parameter<>).GetGenericArguments()[0]);

    private static Type[] Sources() => new[] { typeof(Child), typeof(Child), typeof(Child), typeof(Producer), typeof(Consumer),
        typeof(Open<>), OpenSource(), typeof(Open<string>), typeof(int), typeof(DayOfWeek), typeof(string[]), typeof(string[,]),
        typeof(string[]), typeof(string[]), typeof(int[]), typeof(object), typeof(Producer), typeof(Open<>), typeof(string[,]) };
    private static Type[] Targets() => new[] { typeof(IDerived), typeof(IBase), typeof(IMarker), typeof(IOut<object>), typeof(IIn<string>),
        typeof(IMarker), BoundInterface(), typeof(IOut<object>), typeof(IComparable), typeof(IComparable), typeof(IEnumerable), typeof(IEnumerable),
        typeof(IEnumerable<string>), typeof(IEnumerable<object>), typeof(IEnumerable<uint>), typeof(IMarker), typeof(IIn<object>), typeof(IOut<>), typeof(IEnumerable<string>) };
    private static string[] Labels() => new[] { "direct", "inherited", "marker", "covariant", "contravariant", "definition-marker", "open-constructed", "closed",
        "primitive", "enum", "array", "md-array", "generic-array", "covariant-array", "integral-array", "missing", "wrong-variant", "unbound", "md-generic" };

    private static RuntimeTypeHandle EchoHandle(RuntimeTypeHandle handle) => handle;
    private static object EchoObject(object value) => value;

    private static void PublicHandleInvocation()
    {
        MethodInfo method = typeof(Program).GetMethod(nameof(EchoHandle), BindingFlags.Static | BindingFlags.NonPublic);
        RuntimeTypeHandle handle = typeof(Child).TypeHandle;
        Check("public-invoke-handle", ((RuntimeTypeHandle)method.Invoke(null, new object[] { handle })).Equals(handle));
        Check("public-invoke-default", ((RuntimeTypeHandle)method.Invoke(null, new object[] { default(RuntimeTypeHandle) })).Equals(default(RuntimeTypeHandle)));
        Check("public-invoke-null", ((RuntimeTypeHandle)method.Invoke(null, new object[] { null })).Equals(default(RuntimeTypeHandle)));
        var bound = (Func<RuntimeTypeHandle, RuntimeTypeHandle>)method.CreateDelegate(typeof(Func<RuntimeTypeHandle, RuntimeTypeHandle>));
        Check("public-bound-handle", bound(handle).Equals(handle) && bound(default).Equals(default(RuntimeTypeHandle)));
        Func<RuntimeTypeHandle, RuntimeTypeHandle> direct = EchoHandle;
        Check("public-dynamic-handle", ((RuntimeTypeHandle)direct.DynamicInvoke(handle)).Equals(handle));
        Check("public-dynamic-default", ((RuntimeTypeHandle)bound.DynamicInvoke(default(RuntimeTypeHandle))).Equals(default(RuntimeTypeHandle)));
        object target = new object();
        MethodInfo reference = typeof(Program).GetMethod(nameof(EchoObject), BindingFlags.Static | BindingFlags.NonPublic);
        Check("public-object-control", ReferenceEquals(reference.Invoke(null, new[] { target }), target));
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type interface check ==");
        MethodInfo bridge = typeof(RuntimeTypeHandle).GetMethod("VerifyInterfaceIsImplemented",
            BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(RuntimeTypeHandle) });
        Check("shape", bridge is not null && bridge.IsAssembly && !bridge.IsStatic && !bridge.IsVirtual && !bridge.IsGenericMethod
            && bridge.ReturnType == typeof(void) && (bridge.Attributes & MethodAttributes.PinvokeImpl) == 0
            && (bridge.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        Type[] sources = Sources();
        Type[] targets = Targets();
        string[] labels = Labels();
        for (int i = 0; i < sources.Length; i++)
            Check("public-" + labels[i], targets[i].IsAssignableFrom(sources[i]) == (i < 15));
        Check("public-open-shape", sources[6].ContainsGenericParameters && !sources[6].IsGenericTypeDefinition
            && targets[6].ContainsGenericParameters && !targets[6].IsGenericTypeDefinition);
        Check("public-shape-separate", typeof(IBase).IsAssignableFrom(typeof(IDerived))
            && typeof(Base).IsAssignableFrom(typeof(Child)) && typeof(IBase).IsAssignableFrom(typeof(Parameter<>).GetGenericArguments()[0]));
        PublicHandleInvocation();
        Console.WriteLine("Runtime type interface check contract end");
    }
}
