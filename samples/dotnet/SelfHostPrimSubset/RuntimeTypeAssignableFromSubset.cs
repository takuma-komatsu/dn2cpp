using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace RuntimeTypeAssignableFromSubset;

internal static class AssignableFromChecks
{
    private interface IMarker { }
    private interface IBase { }
    private interface IDerived : IBase { }
    private interface IOut<out T> { }
    private interface IIn<in T> { }
    private class Base { }
    private sealed class Child : Base, IDerived, IMarker { }
    private sealed class Producer : IOut<string> { }
    private sealed class Consumer : IIn<object> { }
    private class Open<T> : IOut<T>, IMarker { }
    private class Parameter<T> where T : Base, IOut<string> { }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("assignable-from-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }
    private static Exception Error(Action action)
    {
        try { action(); }
        catch (TargetInvocationException error) { return error.InnerException; }
        catch (Exception error) { return error; }
        return null;
    }
    private static bool Public(Type target, Type source) => target.IsAssignableFrom(source);

    // Oracle-only raw entry into the public managed override. Native fixtures replace
    // every route that calls this method; no private CLR helper is invoked here.
    private static bool RawOracle(Type target, Type source)
    {
        Type runtimeType = typeof(object).GetType();
        MethodInfo method = runtimeType.GetMethod("IsAssignableFrom", new[] { typeof(Type) });
        var body = new DynamicMethod("RawAssignableFrom", typeof(bool), new[] { typeof(Type), typeof(Type) },
            typeof(AssignableFromChecks).Module, true);
        ILGenerator il = body.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, runtimeType);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, method);
        il.Emit(OpCodes.Ret);
        return ((Func<Type, Type, bool>)body.CreateDelegate(typeof(Func<Type, Type, bool>)))(target, source);
    }
    private static bool Direct(Type target, Type source) => RawOracle(target, source);
    private static bool Pointer(Type target, Type source) => RawOracle(target, source);
    private static bool Virtual(Type target, Type source) => Public(target, source);
    private static bool VirtualPointer(Type target, Type source) => Public(target, source);
    private static bool Constrained(Type target, Type source) => Public(target, source);
    private static Func<Type, bool> Address(Type target)
    {
        if (target is null) throw new ArgumentException();
        return source => RawOracle(target, source);
    }
    private static Func<Type, bool> VirtualAddress(Type target)
    {
        if (target is null) throw new NullReferenceException();
        return source => RawOracle(target, source);
    }
    private static Func<Type, bool> FlowAddress(Type target, bool virt) => virt ? VirtualAddress(target) : Address(target);
    private static bool AddressQuery(Type target, Type source) => Address(target)(source);
    private static bool VirtualAddressQuery(Type target, Type source) => VirtualAddress(target)(source);
    private static bool FlowDirect(Type target, Type source) => FlowAddress(target, false)(source);
    private static bool FlowVirtual(Type target, Type source) => FlowAddress(target, true)(source);
    private static bool ReflectionLeaf(Type target, Type source) => RawOracle(target, source);
    private static bool Reflection(Type target, Type source) => (bool)typeof(AssignableFromChecks)
        .GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { target, source });

    private static void Route(string route, Func<Type, Type, bool> query)
    {
        Type parameter = typeof(Parameter<>).GetGenericArguments()[0];
        Type openSource = typeof(Open<>).MakeGenericType(parameter);
        Type openTarget = typeof(IOut<>).MakeGenericType(parameter);
        Type[] targets = { typeof(string), typeof(Base), typeof(Child), typeof(IBase), typeof(IDerived), typeof(IMarker),
            typeof(IOut<object>), typeof(IIn<string>), typeof(IIn<object>), typeof(object[]), typeof(IEnumerable<object>),
            typeof(IEnumerable), typeof(IEnumerable<string>), typeof(IEnumerable<uint>), typeof(int?), typeof(int),
            typeof(IOut<>), typeof(IMarker), openTarget, typeof(Base), typeof(IOut<string>), typeof(object),
            typeof(int).MakePointerType(), typeof(int).MakeByRefType() };
        Type[] sources = { typeof(string), typeof(Child), typeof(Base), typeof(Child), typeof(IBase), typeof(object),
            typeof(Producer), typeof(Consumer), typeof(Producer), typeof(string[]), typeof(string[]),
            typeof(string[,]), typeof(string[,]), typeof(int[]), typeof(int), typeof(int?),
            typeof(Open<>), typeof(Open<>), openSource, parameter, parameter, null,
            typeof(uint).MakePointerType(), typeof(uint).MakeByRefType() };
        bool[] expected = { true, true, false, true, false, false, true, true, false, true, true, true, false,
            true, true, false, false, true, true, true, true, false, true, true };
        string[] labels = { "identity", "base", "reverse-base", "interface", "reverse-interface", "missing-interface",
            "covariance", "contravariance", "wrong-variance", "array", "array-variance", "md-array", "md-generic",
            "integral-array", "nullable", "reverse-nullable", "unbound", "definition-marker", "open-constructed",
            "parameter-base", "parameter-interface", "null-source", "pointer", "byref" };
        for (int i = 0; i < targets.Length; i++)
            Check(route + "-" + labels[i], query(targets[i], sources[i]) == expected[i]);
        Check(route + "-done", true);
    }
    private static void Nulls(string route, Func<Type, Type, bool> query, bool raw)
    {
        Check(route + "-null-runtime", Error(() => query(null, typeof(string))) is NullReferenceException);
        Check(route + "-null-both", raw ? !query(null, null) : Error(() => query(null, null)) is NullReferenceException);
        Check(route + "-nonnull-null", !query(typeof(object), null));
    }
    internal static void Run(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime assignable from ==");
        Route("public", Public); Nulls("public", Public, false);
        Console.WriteLine("Runtime assignable from end");
    }
    internal static void RunArtifact(string[] args)
    {
        Run(args);
        Route("direct", Direct); Nulls("direct", Direct, true);
        Route("calli", Pointer); Nulls("calli", Pointer, true);
        Route("virtual", Virtual); Nulls("virtual", Virtual, false);
        Route("virtual-calli", VirtualPointer); Nulls("virtual-calli", VirtualPointer, false);
        Route("constrained", Constrained); Nulls("constrained", Constrained, false);
        Route("address", AddressQuery);
        Route("virtual-address", VirtualAddressQuery); Nulls("virtual-address", VirtualAddressQuery, false);
        Route("flow-direct", FlowDirect);
        Route("flow-virtual", FlowVirtual); Nulls("flow-virtual", FlowVirtual, false);
        Check("address-bind-null", Error(() => Address(null)) is ArgumentException);
        Check("virtual-address-bind-null", Error(() => VirtualAddress(null)) is NullReferenceException);
        Check("flow-direct-bind-null", Error(() => FlowAddress(null, false)) is ArgumentException);
        Check("flow-virtual-bind-null", Error(() => FlowAddress(null, true)) is NullReferenceException);
        RunReflectionArtifact(Array.Empty<string>());
        if (args.Length > 0 && args[0] == "native-policy")
        {
            DelegateMetadata("address", Address(typeof(Base)));
            DelegateMetadata("virtual-address", VirtualAddress(typeof(Base)));
            DelegateMetadata("flow-direct", FlowAddress(typeof(Base), false));
            DelegateMetadata("flow-virtual", FlowAddress(typeof(Base), true));
        }
        Console.WriteLine("Runtime assignable from routes end");
    }
    private static void DelegateMetadata(string route, Func<Type, bool> query)
    {
        MethodInfo method = query.Method;
        Check("native-" + route + "-row", method.Name == "IsAssignableFrom" && method.ReturnType == typeof(bool)
            && !method.IsStatic && method.DeclaringType == ((object)typeof(int)).GetType()
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Type));
        Check("native-" + route + "-invoke", (bool)method.Invoke(typeof(Base), new object[] { typeof(Child) })
            && !(bool)method.Invoke(typeof(Child), new object[] { typeof(Base) })
            && !(bool)method.Invoke(typeof(Base), new object[] { null }));
        Check("native-" + route + "-invoke-null-target", Error(() => method.Invoke(null, new object[] { null })) is TargetException);
        var bound = (Func<Type, bool>)method.CreateDelegate(typeof(Func<Type, bool>), typeof(Base));
        Check("native-" + route + "-bound", bound(typeof(Child)) && !bound(typeof(object)) && !bound(null));
        Delegate erased = bound;
        Check("native-" + route + "-dynamic", (bool)erased.DynamicInvoke(typeof(Child))
            && !(bool)erased.DynamicInvoke(typeof(object)) && !(bool)erased.DynamicInvoke(new object[] { null }));
    }
    internal static void RunReflectionArtifact(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime assignable from reflection ==");
        Route("reflection", Reflection); Nulls("reflection", Reflection, true);
        if (args.Length > 0 && args[0] == "native-policy")
        {
            MethodInfo method = typeof(AssignableFromChecks).GetMethod(nameof(ReflectionLeaf), BindingFlags.Static | BindingFlags.NonPublic);
            Check("native-reflection-wrapper-row", method.Name == nameof(ReflectionLeaf)
                && method.DeclaringType == typeof(AssignableFromChecks) && method.IsStatic);
            Check("native-runtime-lookup-unavailable", ((object)typeof(int)).GetType()
                .GetMethod("IsAssignableFrom", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, new[] { typeof(Type) }, null) is null);
        }
        Console.WriteLine("Runtime assignable from reflection end");
    }
}
