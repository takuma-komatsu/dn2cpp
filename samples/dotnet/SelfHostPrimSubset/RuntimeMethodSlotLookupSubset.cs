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
}
