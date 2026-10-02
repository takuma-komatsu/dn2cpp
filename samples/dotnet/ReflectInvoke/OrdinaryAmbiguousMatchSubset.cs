#nullable enable
using System;
using System.Reflection;
[assembly: OrdinaryAmbiguousMatchSubset.FirstAttribute]
[assembly: OrdinaryAmbiguousMatchSubset.SecondAttribute]
namespace OrdinaryAmbiguousMatchSubset;
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class BaseAttribute : Attribute { }
sealed class FirstAttribute : BaseAttribute { }
sealed class SecondAttribute : BaseAttribute { }
class Overloads
{
    public void M(int value) { }
    public void M(string value) { }
    public int this[int index] => index;
    public int this[string index] => index.Length;
    [First, Second] public void Tagged() { }
}
class SameParameters
{
    public void Choose(int value) { }
    public void Choose<T>(int value) { }
}
class Constructors
{
    public Constructors(string value) { }
    public Constructors(int[] value) { }
}
static class Program
{
    private static void Probe(string label, Func<object?> call)
    {
        try
        {
            object? result = call();
            Console.WriteLine(label + ": " + (result is null ? "null" : result is MemberInfo member ? member.Name : result.GetType().Name));
        }
        catch (AmbiguousMatchException error)
        {
            Console.WriteLine(label + ": " + error.HResult.ToString("X8") + " " + error.Message);
        }
    }
    internal static void Run()
    {
        Console.WriteLine("== ordinary ambiguous messages ==");
        Probe("methods", () => typeof(Overloads).GetMethod("M"));
        new SameParameters().Choose<string>(0);
        Probe("same parameters", () => typeof(SameParameters).GetMethod("Choose", new[] { typeof(int) }));
        Probe("selected arity", () => typeof(SameParameters).GetMethod("Choose", 0, new[] { typeof(int) }));
        Probe("properties", () => typeof(Overloads).GetProperty("Item"));
        Probe("constructors", () => Activator.CreateInstance(typeof(Constructors), new object?[] { null }));
        Probe("member attributes", () => Attribute.GetCustomAttribute(typeof(Overloads).GetMethod("Tagged")!, typeof(BaseAttribute)));
        Probe("assembly attributes", () => typeof(Program).Assembly.GetCustomAttribute(typeof(BaseAttribute)));
        Console.WriteLine("ordinary ambiguous messages end");
    }
}
