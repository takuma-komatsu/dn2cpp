using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;

namespace ActivationSubset;

public class DefaultTarget { public int Value; public DefaultTarget() { Value = 37; } }
public class ArgumentTarget { public int Value; public ArgumentTarget(int value) { Value = value; } }

// Literal assembly and type names let reachability retain the named constructors.
internal static class Program
{
    private static int Value(object value) => (int)value.GetType().GetField("Value").GetValue(value);

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Named activation literal smoke ==");
        ObjectHandle handle = Activator.CreateInstance("SelfHostPrimSubset", "ActivationSubset.DefaultTarget");
        object value = handle.Unwrap();
        Console.WriteLine("named-activation-smoke:" + (handle.GetType() == typeof(ObjectHandle)
            && value is DefaultTarget target && target.Value == 37 && ReferenceEquals(value, handle.Unwrap())));
        handle = Activator.CreateInstance("SelfHostPrimSubset", "ActivationSubset.ArgumentTarget", false,
            BindingFlags.Public | BindingFlags.Instance, null, new object[] { 19 }, null, null);
        Console.WriteLine("named-flags-smoke:" + (Value(handle.Unwrap()) == 19));
        handle = Activator.CreateInstance("SelfHostPrimSubset", "ActivationSubset.DefaultTarget", null);
        Console.WriteLine("named-attrs-smoke:" + (Value(handle.Unwrap()) == 37));
        Console.WriteLine("path-activation-linked-name:" + (typeof(Program).Assembly == Assembly.Load("SelfHostPrimSubset")));
        Console.WriteLine("Named activation literal smoke end");
    }
}
