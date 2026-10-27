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

    // Rewritten entry points are native-only; CLR uses the public assignability oracle.
    private static void Direct(RuntimeTypeHandle source, RuntimeTypeHandle target) => throw new InvalidOperationException("fixture missing");
    private static void Pointer(RuntimeTypeHandle source, RuntimeTypeHandle target) => throw new InvalidOperationException("fixture missing");
    private static void Reflection(RuntimeTypeHandle source, RuntimeTypeHandle target) => typeof(RuntimeTypeHandle).GetMethod("VerifyInterfaceIsImplemented",
        BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(RuntimeTypeHandle) }).Invoke(source, new object[] { target });

    private static void ReflectionNullArgument(RuntimeTypeHandle source, RuntimeTypeHandle target) => typeof(RuntimeTypeHandle).GetMethod("VerifyInterfaceIsImplemented",
        BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(RuntimeTypeHandle) }).Invoke(source, new object[] { null });

    private static void Expect(string label, Action<RuntimeTypeHandle, RuntimeTypeHandle> action,
        RuntimeTypeHandle source, RuntimeTypeHandle target, string message, bool isDefault = false)
    {
        Exception caught = null;
        try { action(source, target); }
        catch (Exception error) { caught = error is TargetInvocationException invocation ? invocation.InnerException : error; }
        Check(label, message is null ? caught is null : caught is ArgumentException argument
            && (isDefault ? caught is ArgumentNullException : caught.GetType() == typeof(ArgumentException))
            && argument.ParamName is null && argument.Message == message
            && argument.HResult == (isDefault ? unchecked((int)0x80004003) : unchecked((int)0x80070057)));
    }

    private static unsafe void RunRoute(string route, Action<RuntimeTypeHandle, RuntimeTypeHandle> action)
    {
        Type[] sources = Sources();
        Type[] targets = Targets();
        string[] labels = Labels();
        for (int i = 0; i < sources.Length; i++)
            Expect(route + "-" + labels[i], action, sources[i].TypeHandle, targets[i].TypeHandle, i < 15 ? null : "Interface not found.");
        RuntimeTypeHandle valid = typeof(Child).TypeHandle;
        RuntimeTypeHandle itf = typeof(IBase).TypeHandle;
        Expect(route + "-default-source", action, default, itf, "Invalid handle.", true);
        Expect(route + "-default-target", action, valid, default, "Invalid handle.", true);
        Expect(route + "-default-both", action, default, default, "Invalid handle.", true);
        Expect(route + "-interface-source", action, typeof(IDerived).TypeHandle, itf, "'this' type cannot be an interface itself.");
        Expect(route + "-interface-before-target", action, itf, valid, "'this' type cannot be an interface itself.");
        Expect(route + "-class-target", action, valid, typeof(Base).TypeHandle, "Type passed must be an interface.");
        Expect(route + "-identity-not-interface", action, valid, valid, "Type passed must be an interface.");
        RuntimeTypeHandle parameter = typeof(Parameter<>).GetGenericArguments()[0].TypeHandle;
        Expect(route + "-parameter-source", action, parameter, itf, "Invalid handle.");
        Expect(route + "-parameter-before-target", action, parameter, valid, "Invalid handle.");
        Expect(route + "-parameter-target", action, valid, parameter, "Type passed must be an interface.");
        Type[] shapes = { typeof(int).MakePointerType(), typeof(int).MakeByRefType(), typeof(delegate*<void>) };
        string[] shapeLabels = { "pointer", "byref", "function-pointer" };
        for (int i = 0; i < shapes.Length; i++)
        {
            Expect(route + "-" + shapeLabels[i], action, shapes[i].TypeHandle, itf, "Interface not found.");
            Expect(route + "-" + shapeLabels[i] + "-before-target", action, shapes[i].TypeHandle, valid, "Interface not found.");
            Expect(route + "-default-before-" + shapeLabels[i], action, shapes[i].TypeHandle, default, "Invalid handle.", true);
        }
        Expect(route + "-default-before-interface", action, itf, default, "Invalid handle.", true);
        Expect(route + "-default-before-parameter", action, parameter, default, "Invalid handle.", true);
    }

    private static void RunBoundary(bool reflectionOnly)
    {
        Run();
        if (!reflectionOnly)
        {
            RunRoute("direct", Direct);
            RunRoute("pointer", Pointer);
        }
        RunRoute("reflection", Reflection);
        Expect("reflection-null-target", ReflectionNullArgument, typeof(Child).TypeHandle, default, "Invalid handle.", true);
        bool nullReceiver = false;
        try
        {
            typeof(RuntimeTypeHandle).GetMethod("VerifyInterfaceIsImplemented", BindingFlags.Instance | BindingFlags.NonPublic,
                new[] { typeof(RuntimeTypeHandle) }).Invoke(null, new object[] { typeof(IBase).TypeHandle });
        }
        catch (TargetException) { nullReceiver = true; }
        Check("reflection-null-receiver", nullReceiver);
        Console.WriteLine("Runtime type interface check artifact end");
    }

    internal static void RunArtifactChecks(string[] args) => RunBoundary(false);
    internal static void RunReflectionChecks(string[] args) => RunBoundary(true);
}
