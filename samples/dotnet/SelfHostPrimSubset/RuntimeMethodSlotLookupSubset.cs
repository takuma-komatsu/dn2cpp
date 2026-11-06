using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeMethodSlotLookupSubset;

internal static class Program
{
    private interface IShape { int Read(); }
    private class Base { public virtual int Read() => 3; }
    private sealed class Derived : Base, IShape { public override int Read() => 7; }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("method-slot-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime method-slot lookup contract ==");
        MethodInfo at = typeof(RuntimeTypeHandle).GetMethod("GetMethodAt", BindingFlags.NonPublic | BindingFlags.Static,
            new[] { Type.GetType("System.RuntimeType"), typeof(int) });
        MethodInfo implementation = typeof(RuntimeTypeHandle).GetMethod("GetInterfaceMethodImplementation",
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(RuntimeTypeHandle), Type.GetType("System.RuntimeMethodHandleInternal") });
        foreach (MethodInfo method in new[] { at, implementation })
            Check(method.Name + "-shape", method.IsAssembly && !method.IsGenericMethod
                && method.IsStatic == (method.Name == "GetMethodAt")
                && method.ReturnType == Type.GetType("System.RuntimeMethodHandleInternal")
                && (method.Attributes & MethodAttributes.PinvokeImpl) == 0
                && (method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        Base value = new Derived();
        Check("public-virtual", value.Read() == 7 && ((IShape)value).Read() == 7);
        MethodInfo read = typeof(Derived).GetMethod("Read");
        Check("public-base-definition", read.GetBaseDefinition().DeclaringType == typeof(Base));
        Check("public-invoke", (int)read.Invoke(value, null) == 7);
        Console.WriteLine("Runtime method-slot lookup contract end");
    }

    // Artifact entry points execute only on native; CLR never calls private VM lookups.
    private static void AtDirect(Type type, int slot) => throw new InvalidOperationException("fixture missing");
    private static void AtPointer(Type type, int slot) => throw new InvalidOperationException("fixture missing");
    private static void AtUnsignedDirect(Type type, int slot) => throw new InvalidOperationException("fixture missing");
    private static void AtUnsignedPointer(Type type, int slot) => throw new InvalidOperationException("fixture missing");
    private static void ImplementationDirect(RuntimeTypeHandle type, RuntimeTypeHandle owner) => throw new InvalidOperationException("fixture missing");
    private static void ImplementationPointer(RuntimeTypeHandle type, RuntimeTypeHandle owner) => throw new InvalidOperationException("fixture missing");
    private static void AtReflection(Type type, int slot) => typeof(RuntimeTypeHandle).GetMethod("GetMethodAt",
        BindingFlags.NonPublic | BindingFlags.Static, new[] { Type.GetType("System.RuntimeType"), typeof(int) })
        .Invoke(null, new object[] { type, slot });
    private static void ImplementationReflection(RuntimeTypeHandle type, RuntimeTypeHandle owner) => typeof(RuntimeTypeHandle)
        .GetMethod("GetInterfaceMethodImplementation", BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(RuntimeTypeHandle), Type.GetType("System.RuntimeMethodHandleInternal") })
        .Invoke(type, new object[] { owner, null });

    private static void Expect(string member, string label, Action action, string expected)
    {
        try { action(); throw new InvalidOperationException("private method lookup accepted"); }
        catch (Exception error)
        {
            Exception inner = error is TargetInvocationException invocation ? invocation.InnerException : error;
            bool success = expected switch
            {
                "null" => inner is NullReferenceException,
                "handle" => inner.GetType() == typeof(ArgumentException) && inner.Message == "Invalid handle."
                    && ((ArgumentException)inner).ParamName is null,
                "slot" => inner.GetType() == typeof(ArgumentException) && inner.Message == "Specified argument was out of the range of valid values."
                    && ((ArgumentException)inner).ParamName is null,
                "default" => inner is ArgumentNullException argument && argument.ParamName is null
                    && argument.Message == "Invalid handle.",
                _ => inner is PlatformNotSupportedException && inner.Message == "System.RuntimeTypeHandle." + member
                    + " requires CoreCLR virtual-slot method descriptors. Use supported public Type and MethodInfo APIs instead."
            };
            Check(member + "-" + label, success);
        }
    }

    private static unsafe void RunRoute(string route, Action<Type, int> at,
        Action<RuntimeTypeHandle, RuntimeTypeHandle> implementation)
    {
        const string lookup = "GetMethodAt";
        Expect(lookup, route + "-null", () => at(null, 0), "null");
        Expect(lookup, route + "-null-negative", () => at(null, -1), "null");
        Type[] descriptors = { typeof(int).MakePointerType(), typeof(int).MakeByRefType(),
            typeof(Func<>).GetGenericArguments()[0], typeof(delegate*<void>) };
        string[] labels = { "pointer", "byref", "parameter", "function-pointer" };
        for (int i = 0; i < descriptors.Length; i++)
        {
            Type type = descriptors[i];
            Expect(lookup, route + "-" + labels[i], () => at(type, 0), "handle");
            Expect(lookup, route + "-" + labels[i] + "-negative", () => at(type, -1), "handle");
        }
        Expect(lookup, route + "-negative", () => at(typeof(Derived), -1), "slot");
        Type[] tables = { typeof(object), typeof(Derived), typeof(IShape), typeof(int), typeof(int[]), typeof(int[,]), typeof(Func<>) };
        string[] tableLabels = { "object", "class", "interface", "value", "array", "md-array", "open-generic" };
        for (int i = 0; i < tables.Length; i++)
        {
            Type type = tables[i];
            Expect(lookup, route + "-" + tableLabels[i], () => at(type, 0), "refuse");
        }
        Expect(lookup, route + "-large-slot", () => at(typeof(Derived), int.MaxValue), "refuse");
        const string dispatch = "GetInterfaceMethodImplementation";
        Expect(dispatch, route + "-both-default", () => implementation(default, default), "default");
        Expect(dispatch, route + "-receiver-default", () => implementation(default, typeof(IShape).TypeHandle), "default");
        Expect(dispatch, route + "-owner-default", () => implementation(typeof(Derived).TypeHandle, default), "default");
        Expect(dispatch, route + "-valid-handles", () => implementation(typeof(Derived).TypeHandle, typeof(IShape).TypeHandle), "refuse");
        // The wrapper checks handles, not their relation or the default MethodDesc.
        Expect(dispatch, route + "-non-interface-owner", () => implementation(typeof(Derived).TypeHandle, typeof(object).TypeHandle), "refuse");
        Expect(dispatch, route + "-pointer-receiver", () => implementation(typeof(int).MakePointerType().TypeHandle, typeof(IShape).TypeHandle), "refuse");
    }

    private static void RunUnsignedRoute(string route, Action<Type, int> at)
    {
        Expect("GetMethodAt", route + "-unsigned-max", () => at(typeof(Derived), unchecked((int)uint.MaxValue)), "slot");
        Expect("GetMethodAt", route + "-unsigned-high-bit", () => at(typeof(Derived), int.MinValue), "slot");
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("direct", AtDirect, ImplementationDirect);
        RunRoute("pointer", AtPointer, ImplementationPointer);
        RunRoute("reflection", AtReflection, ImplementationReflection);
        RunUnsignedRoute("direct", AtUnsignedDirect);
        RunUnsignedRoute("pointer", AtUnsignedPointer);
        Console.WriteLine("Runtime method-slot lookup boundary end");
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        RunRoute("reflection", AtReflection, ImplementationReflection);
        Console.WriteLine("Runtime method-slot lookup boundary end");
    }
}
