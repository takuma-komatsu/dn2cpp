using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: CreateDelegate as a program's only reflection call. Nothing here calls
// MethodInfo.Invoke, a PropertyInfo accessor or a ConstructorInfo, and nothing
// calls the methods it binds, so the binding alone must reach their bodies: a
// static method (bound by Delegate.CreateDelegate, and by the generic
// MethodInfo.CreateDelegate over a delegate type nothing else names), an instance
// method, an override through its base row, and an interface's static member.
namespace ReflectBindOnly;

class Shape
{
    public virtual string Name() => "shape";
}

sealed class Circle : Shape
{
    public override string Name() => "circle";
}

sealed class Greeter
{
    public string Greet(string name) => "hello " + name;
}

interface IUnit
{
    static string Unit() => "unit";
}

static class Program
{
    private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private static int Twice(int value) => value * 2;

    private static long Thrice(long value) => value * 3;

    private static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        MethodInfo twice = typeof(Program).GetMethod(nameof(Twice), AnyStatic)!;
        Bind("static", () => ((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), twice))(21).ToString());
        // Nothing else names Func<long, long>: the generic form alone must emit it.
        Bind("generic", () => typeof(Program).GetMethod(nameof(Thrice), AnyStatic)!
            .CreateDelegate<Func<long, long>>()(4).ToString());
        Bind("instance", () => ((Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>),
            new Greeter(), typeof(Greeter).GetMethod(nameof(Greeter.Greet))!))("bind"));
        Bind("override", () => ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new Circle(),
            typeof(Shape).GetMethod(nameof(Shape.Name))!))());
        Bind("interface static", () => ((Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
            typeof(IUnit).GetMethod(nameof(IUnit.Unit))!))());
        Console.WriteLine("bind-only end");
    }

    private static void Bind(string label, Func<string> bind)
    {
        string text;
        try
        {
            text = bind();
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name;
        }
        Console.WriteLine(label + ": " + text);
    }
}
