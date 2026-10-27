using System;
using System.Globalization;
using System.Reflection;

namespace MethodGenericCountSubset;

internal class Owner<T, U>
{
    public void Plain() { }
    public int One<V>() => 1;
    public int Two<V, W>() => 2;
    private void Unused<A, B, C>() { }
    public void Pick() { }
    public void Pick<V>() { }
    public void Pick<V, W>() { }
}

internal sealed class Derived : Owner<int, long> { }

internal static class Program
{
    private static void Check(string label, bool success)
    {
        Console.WriteLine("method-generic-count-" + label + ":" + success);
        if (!success) throw new InvalidOperationException(label);
    }

    private static void Matrix(string route, Func<MethodInfo, int> count)
    {
        // Closed rows stay in-image; Unused has only its definition metadata.
        var instance = new Owner<int, long>();
        instance.One<string>();
        instance.Two<int, string>();
        MethodInfo one = typeof(Owner<int, long>).GetMethod("One");
        MethodInfo two = typeof(Owner<int, long>).GetMethod("Two");
        MethodInfo inherited = typeof(Derived).GetMethod("One");
        Check(route + "-plain", count(typeof(Owner<int, long>).GetMethod("Plain")) == 0);
        Check(route + "-open-owner-plain", count(typeof(Owner<,>).GetMethod("Plain")) == 0);
        Check(route + "-one-definition", count(one) == 1);
        Check(route + "-one-closed", count(one.MakeGenericMethod(typeof(string))) == 1);
        Check(route + "-two-definition", count(two) == 2);
        Check(route + "-two-closed", count(two.MakeGenericMethod(typeof(int), typeof(string))) == 2);
        Check(route + "-unused-definition", count(typeof(Owner<int, long>).GetMethod("Unused",
            BindingFlags.NonPublic | BindingFlags.Instance)) == 3);
        Check(route + "-open-owner-definition", count(typeof(Owner<,>).GetMethod("One")) == 1);
        Check(route + "-open-owner-constructed", count(typeof(Owner<,>).GetMethod("One").MakeGenericMethod(typeof(string))) == 1);
        Check(route + "-partially-open", count(two.MakeGenericMethod(typeof(int), two.GetGenericArguments()[1])) == 2);
        Check(route + "-inherited-definition", count(inherited) == 1);
        Check(route + "-inherited-closed", count(inherited.MakeGenericMethod(typeof(string))) == 1);
        Check(route + "-roundtrip-definition", count(one.MakeGenericMethod(typeof(string)).GetGenericMethodDefinition()) == 1);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Method generic parameter count contract ==");
        MethodInfo getter = typeof(MethodInfo).GetMethod("get_GenericParameterCount", BindingFlags.NonPublic | BindingFlags.Instance);
        Check("shape", getter.IsAssembly && getter.IsVirtual && !getter.IsFinal && !getter.IsStatic
            && !getter.IsGenericMethod && getter.ReturnType == typeof(int) && getter.GetParameters().Length == 0
            && (getter.Attributes & (MethodAttributes.NewSlot | MethodAttributes.CheckAccessOnOverride | MethodAttributes.SpecialName))
                == (MethodAttributes.NewSlot | MethodAttributes.CheckAccessOnOverride | MethodAttributes.SpecialName));
        Matrix("public", m => m.GetGenericArguments().Length);
        for (int n = 0; n < 3; n++)
            Check("lookup-" + n, typeof(Owner<int, long>).GetMethod("Pick", n, Type.EmptyTypes).GetGenericArguments().Length == n);
        Check("lookup-missing", typeof(Owner<int, long>).GetMethod("Pick", 3, Type.EmptyTypes) is null);
        Console.WriteLine("Method generic parameter count contract end");
    }

    private static int Direct(MethodInfo method) => throw new InvalidOperationException("fixture missing");
    private static int Virtual(MethodInfo method) => throw new InvalidOperationException("fixture missing");
    private static int Pointer(MethodInfo method) => throw new InvalidOperationException("fixture missing");
    private static int VirtualPointer(MethodInfo method) => throw new InvalidOperationException("fixture missing");
    private static int Reflected(MethodInfo method) => (int)typeof(MethodInfo).GetMethod("get_GenericParameterCount",
        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(method, null);

    private static void NativeRoute(string route, Func<MethodInfo, int> count)
    {
        Matrix(route, count);
        try { count(null); throw new InvalidOperationException("null receiver accepted"); }
        catch (NullReferenceException) { Check(route + "-null", route != "reflection"); }
        catch (TargetException) { Check(route + "-null", route == "reflection"); }
        if (route == "reflection") return;
        MethodInfo method = typeof(Owner<int, long>).GetMethod("Two");
        count(method);
        long before = GC.GetAllocatedBytesForCurrentThread();
        int sum = 0;
        for (int n = 0; n < 128; n++) sum += count(method);
        long after = GC.GetAllocatedBytesForCurrentThread();
        Check(route + "-no-allocation", sum == 256 && before == after);
    }

    // Only the public oracle runs on CLR; rewritten internal calls run on native targets.
    public static void RunArtifactChecks(string[] args)
    {
        Run();
        NativeRoute("direct", Direct);
        NativeRoute("virtual", Virtual);
        NativeRoute("pointer", Pointer);
        NativeRoute("virtual-pointer", VirtualPointer);
        NativeRoute("reflection", Reflected);
        Console.WriteLine("Method generic parameter count routes end");
    }

    public static void RunReflectionChecks(string[] args)
    {
        Run();
        NativeRoute("reflection", Reflected);
        Console.WriteLine("Method generic parameter count routes end");
    }
}
