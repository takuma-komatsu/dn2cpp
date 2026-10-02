#nullable disable
using System;
using System.Collections.Generic;
namespace ExceptionMessageSubset;
internal static class OrdinaryReflectionArgumentSubset
{
    private static void ProbeFields(string label, Func<object> call)
    {
        try
        {
            Console.WriteLine(label + ": returned " + (call() ?? "<null>"));
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine(label + ": " + e.GetType().Name);
        }
        catch (ArgumentException e)
        {
            object actual = (e as ArgumentOutOfRangeException)?.ActualValue;
            Console.WriteLine(label + ": " + e.GetType().Name + " param=" + (e.ParamName ?? "<null>")
                + " actual=" + (actual ?? "<null>") + " actualType=" + (actual?.GetType().Name ?? "<null>"));
            Console.WriteLine("  message=" + e.Message.Replace("\r", "").Replace("\n", "|"));
        }
    }
    internal static void Run()
    {
        Console.WriteLine("-- ordinary reflection argument fields --");

    // A Type.GetEnum* receiver is a callvirt's: null faults before the enumType check.
    Type noType = null;
    ProbeFields("Type.GetEnumUnderlyingType null receiver", () => noType.GetEnumUnderlyingType());
    ProbeFields("Type.GetEnumNames null receiver", () => noType.GetEnumNames());
    ProbeFields("Type.GetEnumValuesAsUnderlyingType null receiver", () => noType.GetEnumValuesAsUnderlyingType());
    ProbeFields("Type.GetEnumUnderlyingType non-enum", () => typeof(int).GetEnumUnderlyingType());
    ProbeFields("Type.GetEnumNames non-enum", () => typeof(int).GetEnumNames());
    ProbeFields("Type.GetEnumValuesAsUnderlyingType non-enum", () => typeof(int).GetEnumValuesAsUnderlyingType());
    ProbeFields("Enum.GetUnderlyingType(null)", () => Enum.GetUnderlyingType(null));

    // Every lookup overload that declares Type[] types rejects a null array, as
    // MakeGenericType does, and one that declares genericParameterCount a negative count.
    const System.Reflection.BindingFlags publicInstance =
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
    const System.Reflection.CallingConventions anyCall = System.Reflection.CallingConventions.Any;
    ProbeFields("GetMethod(name, null)", () => typeof(object).GetMethod("ToString", (Type[])null)?.Name);
    ProbeFields("GetMethod(name, flags, null)", () => typeof(object).GetMethod("ToString", publicInstance, (Type[])null)?.Name);
    ProbeFields("GetMethod(name, null, modifiers)", () => typeof(object).GetMethod("ToString", (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, flags, binder, null, modifiers)", () => typeof(object).GetMethod("ToString", publicInstance, null, (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, flags, binder, callConv, null, modifiers)", () => typeof(object).GetMethod("ToString", publicInstance, null, anyCall, (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, 0, null)", () => typeof(object).GetMethod("ToString", 0, (Type[])null)?.Name);
    ProbeFields("GetMethod(name, 0, null, modifiers)", () => typeof(object).GetMethod("ToString", 0, (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, 0, flags, null)", () => typeof(object).GetMethod("ToString", 0, publicInstance, (Type[])null)?.Name);
    ProbeFields("GetMethod(name, 0, flags, binder, null, modifiers)", () => typeof(object).GetMethod("ToString", 0, publicInstance, null, (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, 0, flags, binder, callConv, null, modifiers)", () => typeof(object).GetMethod("ToString", 0, publicInstance, null, anyCall, (Type[])null, null)?.Name);
    ProbeFields("GetMethod(name, 0, empty)", () => typeof(OrdinaryReflectionArgumentSubset).GetMethod(nameof(Run), 0, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, Type.EmptyTypes)?.Name);
    ProbeFields("GetMethod(name, -1, empty)", () => typeof(object).GetMethod("ToString", -1, Type.EmptyTypes)?.Name);
    ProbeFields("GetMethod(name, -1, null)", () => typeof(object).GetMethod("ToString", -1, (Type[])null)?.Name);
    ProbeFields("GetMethod(name, -2, flags, empty)", () => typeof(object).GetMethod("ToString", -2, publicInstance, Type.EmptyTypes)?.Name);
    ProbeFields("GetMethod(null, -1, null)", () => typeof(object).GetMethod(null, -1, (Type[])null)?.Name);
    ProbeFields("GetConstructor(null)", () => typeof(object).GetConstructor((Type[])null)?.Name);
    ProbeFields("GetConstructor(flags, null)", () => typeof(object).GetConstructor(publicInstance, (Type[])null)?.Name);
    ProbeFields("GetConstructor(flags, binder, null, modifiers)", () => typeof(object).GetConstructor(publicInstance, null, (Type[])null, null)?.Name);
    ProbeFields("GetConstructor(flags, binder, callConv, null, modifiers)", () => typeof(object).GetConstructor(publicInstance, null, anyCall, (Type[])null, null)?.Name);
    ProbeFields("GetProperty(name, null)", () => typeof(string).GetProperty("Length", (Type[])null)?.Name);
    ProbeFields("GetProperty(name, returnType, null)", () => typeof(string).GetProperty("Length", typeof(int), (Type[])null)?.Name);
    ProbeFields("GetProperty(name, returnType, null, modifiers)", () => typeof(string).GetProperty("Length", typeof(int), (Type[])null, null)?.Name);
    ProbeFields("GetProperty(name, flags, binder, returnType, null, modifiers)", () => typeof(string).GetProperty("Length", publicInstance, null, typeof(int), (Type[])null, null)?.Name);
    ProbeFields("GetProperty(name, null returnType)", () => typeof(string).GetProperty("Length", (Type)null)?.Name);
    ProbeFields("GetProperty(null, null)", () => typeof(string).GetProperty(null, (Type[])null)?.Name);
    ProbeFields("MakeGenericType(null)", () => typeof(List<>).MakeGenericType((Type[])null)?.Name);
        Console.WriteLine("ordinary reflection argument fields end");
    }
}
