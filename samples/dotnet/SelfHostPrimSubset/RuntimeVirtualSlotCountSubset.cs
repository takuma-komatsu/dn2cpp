using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeVirtualSlotCountSubset;

internal static class Program
{
    private interface IShape
    {
        static abstract int First();
        int Instance();
        static virtual int DefaultStatic() => 3;
        static int Helper() => 4;
        private int Hidden() => 5;
        int DefaultInstance() => 6;
        T Echo<T>(T value);
    }

    private class Base
    {
        public virtual int Value() => 10;
        public virtual T Echo<T>(T value) => value;
    }

    private sealed class Derived : Base, IShape
    {
        public static int First() => 2;
        public int Instance() => 7;
        public override int Value() => 11;
        public override T Echo<T>(T value) => value;
    }

    private static void Check(string label, bool success)
    {
        Console.WriteLine("virtual-slot-count-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static int StaticDispatch<T>() where T : IShape => T.First() + T.DefaultStatic();

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime virtual-slot count contract ==");
        foreach (string name in new[] { "GetNumVirtuals", "GetNumVirtualsAndStaticVirtuals" })
        {
            MethodInfo method = typeof(RuntimeTypeHandle).GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Static, new[] { Type.GetType("System.RuntimeType") });
            Check(name + "-shape", method is not null && method.IsAssembly && method.IsStatic
                && !method.IsGenericMethod && method.ReturnType == typeof(int)
                && (method.Attributes & MethodAttributes.PinvokeImpl) == 0
                && ((method.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0)
                    == (name == "GetNumVirtuals"));
        }
        Base value = new Derived();
        IShape shape = (IShape)value;
        Check("public-virtual", value.Value() == 11);
        Check("public-generic-virtual", value.Echo("text") == "text" && shape.Echo(12) == 12);
        Check("public-interface", shape.Instance() == 7 && shape.DefaultInstance() == 6);
        Check("public-static-virtual", StaticDispatch<Derived>() == 5 && IShape.Helper() == 4);
        MethodInfo methodInfo = typeof(Derived).GetMethod("Value");
        Check("public-base-definition", methodInfo.GetBaseDefinition().DeclaringType == typeof(Base));
        Check("public-invoke", (int)methodInfo.Invoke(value, Array.Empty<object>()) == 11);
        Console.WriteLine("Runtime virtual-slot count contract end");
    }

    // Only artifact copies call these private VM operations; CLR runs public dispatch.
    private static int CountDirect(Type type) => throw new InvalidOperationException("fixture missing");
    private static int CountPointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static int CombinedDirect(Type type) => throw new InvalidOperationException("fixture missing");
    private static int CombinedPointer(Type type) => throw new InvalidOperationException("fixture missing");
    private static int CountReflection(Type type) => (int)typeof(RuntimeTypeHandle).GetMethod("GetNumVirtuals",
        BindingFlags.NonPublic | BindingFlags.Static, new[] { Type.GetType("System.RuntimeType") })
        .Invoke(null, new object[] { type });
    private static int CombinedReflection(Type type) => (int)typeof(RuntimeTypeHandle).GetMethod("GetNumVirtualsAndStaticVirtuals",
        BindingFlags.NonPublic | BindingFlags.Static, new[] { Type.GetType("System.RuntimeType") })
        .Invoke(null, new object[] { type });

    private static void Refuse(string member, string label, Func<int> action)
    {
        try { action(); throw new InvalidOperationException("private slot count accepted"); }
        catch (Exception error)
        {
            Exception inner = error is TargetInvocationException invocation ? invocation.InnerException : error;
            Check(member + "-" + label, inner is PlatformNotSupportedException
                && inner.Message == "System.RuntimeTypeHandle." + member
                    + " requires CoreCLR virtual-slot layout. Use supported public Type and MethodInfo APIs instead.");
        }
    }

    private static void RunRoute(string route, Func<Type, int> count, Func<Type, int> combined)
    {
        Type[] types = { typeof(object), typeof(Derived), typeof(IShape), typeof(int), typeof(int[]),
            typeof(int).MakePointerType(), typeof(int).MakeByRefType(), typeof(Func<>).GetGenericArguments()[0], null };
        string[] labels = { "object", "class", "interface", "value", "array", "pointer", "byref", "parameter", "null" };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            Refuse("GetNumVirtuals", route + "-" + labels[i], () => count(type));
            Refuse("GetNumVirtualsAndStaticVirtuals", route + "-" + labels[i], () => combined(type));
        }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        RunRoute("direct", CountDirect, CombinedDirect);
        RunRoute("pointer", CountPointer, CombinedPointer);
        RunRoute("reflection", CountReflection, CombinedReflection);
        Console.WriteLine("Runtime virtual-slot count boundary end");
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        RunRoute("reflection", CountReflection, CombinedReflection);
        Console.WriteLine("Runtime virtual-slot count boundary end");
    }
}
