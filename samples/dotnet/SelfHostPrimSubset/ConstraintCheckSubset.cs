using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ConstraintCheckSubset;

internal static class Program
{
    public class Any<T> { }
    public class Reference<T> where T : class { }
    public static T Echo<T>(T value) => value;

    private static MethodInfo Bridge() => typeof(RuntimeTypeHandle).GetMethod("SatisfiesConstraints",
        BindingFlags.NonPublic | BindingFlags.Static, new[] { Type.GetType("System.RuntimeType"),
            Type.GetType("System.RuntimeType"), Type.GetType("System.Reflection.RuntimeMethodInfo"),
            Type.GetType("System.RuntimeType") });

    private static void Check(string label, bool success)
    {
        Console.WriteLine("constraint-check-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static string Fault(Action action)
    {
        try { action(); return "none"; }
        catch (Exception error) { return error.GetType().Name; }
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Private constraint-check contract ==");
        MethodInfo bridge = Bridge();
        Check("shape", bridge is not null && bridge.IsAssembly && bridge.IsStatic
            && !bridge.IsGenericMethod && bridge.ReturnType == typeof(bool)
            && (bridge.Attributes & MethodAttributes.PinvokeImpl) == 0
            && (bridge.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) == 0);
        Check("closed-type", typeof(List<>).MakeGenericType(typeof(string)) == typeof(List<string>));
        MethodInfo method = typeof(Program).GetMethod("Echo");
        Check("closed-method", method.MakeGenericMethod(typeof(string)).GetGenericArguments()[0] == typeof(string)
            && Echo("native") == "native");
        Type formal = typeof(Any<>).GetGenericArguments()[0];
        Type constrained = typeof(Reference<>).GetGenericArguments()[0];
        Check("parameter-metadata", constrained.IsGenericParameter
            && (constrained.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0);
        Check("open-valid", typeof(Reference<>).MakeGenericType(constrained).ContainsGenericParameters);
        Check("open-invalid", Fault(() => typeof(Reference<>).MakeGenericType(formal)) == "ArgumentException");
        Check("type-null", Fault(() => typeof(List<>).MakeGenericType((Type[])null)) == "ArgumentNullException");
        Check("type-arity", Fault(() => typeof(List<>).MakeGenericType(Type.EmptyTypes)) == "ArgumentException");
        Check("type-void", Fault(() => typeof(List<>).MakeGenericType(typeof(void))) == "ArgumentException");
        Check("type-byref", Fault(() => typeof(List<>).MakeGenericType(typeof(int).MakeByRefType())) == "ArgumentException");
        Check("method-null", Fault(() => method.MakeGenericMethod((Type[])null)) == "ArgumentNullException");
        Check("method-arity", Fault(() => method.MakeGenericMethod(Type.EmptyTypes)) == "ArgumentException");
        Console.WriteLine("Private constraint-check contract end");
    }
}
