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

    private class AllocatedFault : Exception
    {
    }

    private sealed class NeverAllocatedFault : Exception
    {
    }

    private sealed class DescribedFault : Exception
    {
        public override string ToString() => "described fault";
        public override bool Equals(object? other) => other is DescribedFault;
        public override int GetHashCode() => 91;
    }

    internal static void RunExceptionMembers()
    {
        Console.WriteLine("== exception Object member lookup ==");
        DumpExceptionMembers(typeof(Exception));
        DumpExceptionMembers(typeof(AllocatedFault));
        DumpExceptionMembers(typeof(NeverAllocatedFault));
        DumpExceptionMembers(typeof(DescribedFault));
        DumpExceptionMembers(typeof(NullReferenceException));
        CheckExceptionMembers("base", new Exception("plain"));
        CheckExceptionMembers("allocated", new AllocatedFault());
        CheckExceptionMembers("override", new DescribedFault());
        try
        {
            object absent = null!;
            GC.KeepAlive(absent.GetType());
        }
        catch (NullReferenceException ex)
        {
            CheckExceptionMembers("raised", ex);
        }
        try
        {
            var array = new int[1];
            GC.KeepAlive(array[array.Length]);
        }
        catch (Exception ex)
        {
            DumpExceptionMembers(ex.GetType());
            CheckExceptionMembers("unbound raised", ex);
        }
        Console.WriteLine("exception Object member lookup end");
    }

    private static void DumpExceptionMembers(Type type)
    {
        foreach (string name in new[] { "GetHashCode", "Equals", "ToString", "GetType" })
        {
            MethodInfo? method = type.GetMethod(name);
            Console.WriteLine(type.Name + "." + name + "=" + (method is null ? "missing" :
                method.DeclaringType!.Name + ":" + method.ReflectedType!.Name + ":" +
                method.IsVirtual + ":" + method.IsStatic + ":" + (int)method.Attributes +
                ":base=" + method.GetBaseDefinition().DeclaringType!.Name +
                ":same=" + ReferenceEquals(method, type.GetMethod(name))));
        }
    }

    private static void CheckExceptionMembers(string label, Exception receiver)
    {
        Type type = receiver.GetType();
        MethodInfo? text = type.GetMethod("ToString");
        MethodInfo? kind = type.GetMethod("GetType");
        MethodInfo? equals = type.GetMethod("Equals");
        MethodInfo? hash = type.GetMethod("GetHashCode");
        if (text is null || kind is null || equals is null || hash is null)
        {
            Console.WriteLine(label + " invoke=missing");
            return;
        }
        Console.WriteLine(label + " invoke=" + Equals(text.Invoke(receiver, null), receiver.ToString()) + ":" +
            ReferenceEquals(kind.Invoke(receiver, null), type) + ":" +
            Equals(equals.Invoke(receiver, new object[] { receiver }), receiver.Equals(receiver)) + ":" +
            Equals(hash.Invoke(receiver, null), receiver.GetHashCode()));
        MemberFault(label + " null receiver", () => kind.Invoke(null, null)!);
        MemberFault(label + " wrong receiver", () => kind.Invoke(new object(), null)!);
    }
}
