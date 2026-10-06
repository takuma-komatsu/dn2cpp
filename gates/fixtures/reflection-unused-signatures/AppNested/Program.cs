using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace ReflectionNestedSignature;

internal sealed class Holder<T>
{
    public Ext.Box<T>? Value = default;
}

internal sealed class NestedHolder<T>
{
    public Broken<T>? Value = default;
}

internal sealed class Broken<T> : Ext.LayoutBase<T> { }

internal static class Subject
{
    public static int Used() => 42;
    public static U Supported<U>(U value) => value;
    public static Holder<int> Pick<U>(Holder<int> value, U fallback) => value;
    public static NestedHolder<long> First<U>(NestedHolder<long> value, U fallback) => value;
    public static Broken<long> Second<U>(Broken<long> value, U fallback) => value;
    public static NestedHolder<int> OwnerFirst<U>(NestedHolder<int> value, U fallback) => value;
    public static Broken<int> OwnerSecond<U>(Broken<int> value, U fallback) => value;
    public static Task<Broken<long>> TaskShape<U>(Task<Broken<long>> value, U fallback) => value;
    public static Task<Holder<long>> TaskLayout<U>(Task<Holder<long>> value, U fallback) => value;
}

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== nested generic signature ==");
        Console.WriteLine("used=" + Subject.Used());
        Console.WriteLine("supported=" + Subject.Supported<int>(7));
        MethodInfo supported = typeof(Subject).GetMethod("Supported")!;
        Console.WriteLine("definition=" + supported.IsGenericMethodDefinition
            + " parameter=" + supported.GetGenericArguments()[0].Name);
#if BODY_NEEDED
        var required = new Holder<int>();
        Console.WriteLine("body needed=" + (required.Value is null));
#endif
        if (args.Length > 0 && args[0] == "describe-unused-layouts")
        {
            Console.WriteLine("== unused layout definitions ==");
            foreach (string name in new[] { "Pick", "First", "Second", "OwnerFirst", "OwnerSecond", "TaskShape", "TaskLayout" })
            {
                MethodInfo? method = typeof(Subject).GetMethod(name);
                Console.WriteLine(name + " found=" + (method is not null));
                if (method is null)
                    continue;
                Type result = method.ReturnType;
                Type parameter = method.GetParameters()[0].ParameterType;
                Type returnParameter = method.ReturnParameter.ParameterType;
                Console.WriteLine(name + " types=" + result.Name + "/" + parameter.Name + "/" + returnParameter.Name);
                Console.WriteLine(name + " identity=" + ReferenceEquals(result, parameter)
                    + "/" + ReferenceEquals(result, returnParameter));
            }
            Console.WriteLine("unused layout definitions end");
        }
        Console.WriteLine("nested generic signature end");
    }
}
