using System;
using System.Globalization;
using System.Reflection;

namespace RuntimeMethodEnumerationSubset;

internal static class Program
{
    public sealed class Target
    {
        public Target() { }
        public int Read() => 37;
        public static int StaticRead() => 41;
    }

    internal static void Run()
    {
        Console.WriteLine("== Runtime method enumeration contract ==");
        MethodInfo first = typeof(RuntimeTypeHandle).GetMethod("GetFirstIntroducedMethod", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo next = typeof(RuntimeTypeHandle).GetMethod("GetNextIntroducedMethod", BindingFlags.NonPublic | BindingFlags.Static);
        Console.WriteLine("method-enum-first-shape:" + (first.IsPrivate && first.IsStatic && !first.IsGenericMethod
            && first.ReturnType.FullName == "System.RuntimeMethodHandleInternal"
            && first.GetParameters().Length == 1
            && first.GetParameters()[0].ParameterType.FullName == "System.RuntimeType"));
        Console.WriteLine("method-enum-next-shape:" + (next.IsPrivate && next.IsStatic && !next.IsGenericMethod
            && next.ReturnType == typeof(void) && next.GetParameters().Length == 1
            && next.GetParameters()[0].ParameterType.FullName == "System.RuntimeMethodHandleInternal&"));
        var target = new Target();
        MethodInfo read = typeof(Target).GetMethod("Read");
        Console.WriteLine("method-enum-public-methods:" + (typeof(Target).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Length == 2));
        Console.WriteLine("method-enum-public-ctors:" + (typeof(Target).GetConstructors().Length == 1));
        Console.WriteLine("method-enum-public-invoke:" + ((int)read.Invoke(target, null) == 37));
        Console.WriteLine("method-enum-public-delegate:" + (((Func<int>)read.CreateDelegate(typeof(Func<int>), target))() == 37));
        Console.WriteLine("Runtime method enumeration contract end");
    }

    // Only the native artifact replaces these bodies with exact CoreLib signatures.
    private static void FirstDirect() => throw new InvalidOperationException("fixture missing");
    private static void FirstPointer() => throw new InvalidOperationException("fixture missing");
    private static void NextDirect() => throw new InvalidOperationException("fixture missing");
    private static void NextPointer() => throw new InvalidOperationException("fixture missing");
    private static void EnumeratorConstructor() => throw new InvalidOperationException("fixture missing");
    private static bool DefaultMoveNext() => throw new InvalidOperationException("fixture missing");

    private static void Refuse(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (TargetInvocationException error) { Console.WriteLine(label + ":" + error.InnerException.GetType().Name + ":" + error.InnerException.Message); }
        catch (Exception error) { Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message); }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Run();
        Refuse("method-enum-first-direct", FirstDirect);
        Refuse("method-enum-first-pointer", FirstPointer);
        Refuse("method-enum-next-direct", NextDirect);
        Refuse("method-enum-next-pointer", NextPointer);
        Refuse("method-enum-constructor", EnumeratorConstructor);
        Refuse("method-enum-first-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetFirstIntroducedMethod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { typeof(Target) }));
        Refuse("method-enum-next-reflection", () => typeof(RuntimeTypeHandle).GetMethod("GetNextIntroducedMethod", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { null }));
        Console.WriteLine("method-enum-default-empty:" + !DefaultMoveNext());
        Console.WriteLine("Runtime method enumeration boundary end");
    }
}
