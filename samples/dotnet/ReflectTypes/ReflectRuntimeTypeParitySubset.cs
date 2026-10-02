using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectRuntimeTypeParitySubset;

static class Program
{
    static void Fault(string label, Func<Type> run)
    {
        try
        {
            Console.WriteLine(label + "=" + run());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name + ": " + ex.Message);
        }
    }

    static void FaultType(string label, Func<Type> run)
    {
        try
        {
            Console.WriteLine(label + "=" + run());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name);
        }
    }

    static void MemberFault(string label, Func<object> run)
    {
        try
        {
            Console.WriteLine(label + "=" + run());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name + ":" +
                (ex.InnerException?.GetType().Name ?? "null"));
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== runtime type reflection ==");
        var length = typeof(string).GetProperty("Length")!;
        var chars = typeof(string).GetProperty("Chars")!;
        var props = typeof(string).GetProperties(BindingFlags.Public | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
        bool hasLength = false;
        bool hasChars = false;
        foreach (var prop in props)
        {
            hasLength |= prop.Name == "Length";
            hasChars |= prop.Name == "Chars";
        }
        Console.WriteLine("string props=" + props.Length + ":" + hasLength + ":" + hasChars);
        Console.WriteLine("length info=" + length.PropertyType.Name + ":" +
            length.DeclaringType!.Name + ":" + length.CanRead + ":" + length.CanWrite +
            ":" + length.GetIndexParameters().Length);
        Console.WriteLine("length value=" + length.GetValue("abc") + ":" +
            length.GetGetMethod()!.Invoke("abc", null) + ":" +
            length.GetGetMethod()!.IsSpecialName);
        Console.WriteLine("length getter=" + length.GetGetMethod() + ":" +
            (length.GetGetMethod() == typeof(string).GetMethod("get_Length")));
        Console.WriteLine("chars info=" + chars.PropertyType.Name + ":" +
            chars.GetIndexParameters()[0].ParameterType.Name + ":" +
            chars.GetGetMethod()!.Name + ":" + chars.GetValue("abc", new object[] { 1 }));
        Console.WriteLine("chars getter=" + chars.GetGetMethod());
        Console.WriteLine("chars null index=" + chars.GetValue("abc", new object[] { null! }) +
            ":" + chars.GetGetMethod()!.Invoke("abc", new object[] { null! }));
        MemberFault("chars wrong index", () => chars.GetValue("abc", new object[] { "1" })!);
        MemberFault("chars past end", () => chars.GetValue("abc", new object[] { 3 })!);

        Console.WriteLine("open close=" +
            (typeof(List<>).MakeGenericType(typeof(int)) == typeof(List<int>)));
        Fault("closed close", () => typeof(List<int>).MakeGenericType(typeof(string)));
        Fault("closed null", () => typeof(List<int>).MakeGenericType((Type[])null!));
        Fault("plain close", () => typeof(int).MakeGenericType(typeof(string)));
        FaultType("span array", () => typeof(Span<int>).MakeArrayType());
        FaultType("span rank1", () => typeof(Span<int>).MakeArrayType(1));
        FaultType("span rank2", () => typeof(Span<int>).MakeArrayType(2));
        FaultType("span rank0", () => typeof(Span<int>).MakeArrayType(0));
        Console.WriteLine("runtime type reflection end");
    }
}
