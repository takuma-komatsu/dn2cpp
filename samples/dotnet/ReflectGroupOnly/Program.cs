using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: reflection members a program names only as method groups. Nothing here
// calls Activator.CreateInstance, a ConstructorInfo, MethodInfo.Invoke, a
// PropertyInfo accessor or CreateDelegate; each is bound to a delegate and run
// through it, so the binding must open the route a call opens, with ILDiet on or
// off: the constructors of types only a type token names, and the bodies a
// reflected member runs. Every reflected method is a virtual member of a
// constructed type, which ILDiet keeps without a call.
namespace ReflectGroupOnly;

sealed class Made
{
    public override string ToString() => "made";
}

sealed class Built
{
    private readonly string _tag;

    public Built() => _tag = "built";

    public override string ToString() => _tag;
}

class Greeter
{
    private string _mood = "calm";

    public virtual string Greet(string name) => "hello " + name;

    public virtual string Label => "label";

    public virtual string Mood
    {
        get => _mood;
        set => _mood = value;
    }
}

static class Program
{
    private static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Func<Type, object?> create = Activator.CreateInstance;
        Run("activator", () => create(typeof(Made))!.ToString()!);
        Run("constructor", () =>
        {
            Func<object?[]?, object> construct = typeof(Built).GetConstructor(Type.EmptyTypes)!.Invoke;
            return construct(null).ToString()!;
        });
        var greeter = new Greeter();
        MethodInfo greet = typeof(Greeter).GetMethod(nameof(Greeter.Greet))!;
        Run("method", () =>
        {
            Func<object?, object?[]?, object?> invoke = greet.Invoke;
            return (string)invoke(greeter, new object[] { "group" })!;
        });
        Run("property get", () =>
        {
            Func<object?, object?> read = typeof(Greeter).GetProperty(nameof(Greeter.Label))!.GetValue;
            return (string)read(greeter)!;
        });
        Run("property set", () =>
        {
            Action<object?, object?> write = typeof(Greeter).GetProperty(nameof(Greeter.Mood))!.SetValue;
            write(greeter, "glad");
            return greeter.Mood;
        });
        Run("static create delegate", () =>
        {
            Func<Type, object?, MethodInfo, Delegate> bind = Delegate.CreateDelegate;
            return ((Func<string, string>)bind(typeof(Func<string, string>), greeter, greet))("static");
        });
    }

    private static void Run(string label, Func<string> body)
    {
        string text;
        try
        {
            text = body();
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name;
        }
        Console.WriteLine(label + ": " + text);
    }
}
