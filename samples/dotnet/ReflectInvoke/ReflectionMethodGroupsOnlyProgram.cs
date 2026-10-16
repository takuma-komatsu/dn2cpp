using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: reflection members a program names only as method groups. Nothing here
// calls Activator.CreateInstance, a ConstructorInfo, MethodInfo.Invoke,
// PropertyInfo.GetValue/SetValue, CreateDelegate, MakeGenericType or
// GetCustomAttributes; each is bound to a delegate and run through it. The
// transpile cannot tell what a call through such a delegate selects, so it reports
// every site, and keep-method-groups.xml preserves the targets; with ILDiet on or
// off, the delegates then run them as a direct call would.
// CreateDelegate, MakeGenericType and GetCustomAttributes are virtual, so their
// groups bind through a reflection object the runtime owns. A preserved static method
// of a type with a static constructor sees that initializer's effect, as .NET runs it
// before the method.
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

sealed class Box<T>
{
    public override string ToString() => "box:" + typeof(T).Name;
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

sealed class TagAttribute : Attribute
{
}

[Tag]
sealed class Tagged
{
}

static class Registry
{
    public static int Count;
}

static class Plugin
{
    static Plugin() => Registry.Count++;

    public static string Start() => "start:" + Registry.Count;
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
        Run("create delegate", () =>
        {
            Func<Type, object?, Delegate> bind = greet.CreateDelegate;
            return ((Func<string, string>)bind(typeof(Func<string, string>), greeter))("bind");
        });
        Run("make generic", () =>
        {
            Func<Type[], Type> close = typeof(Box<>).MakeGenericType;
            return create(close(new[] { typeof(string) }))!.ToString()!;
        });
        Run("attributes", () =>
        {
            Func<bool, object[]> attributes = typeof(Tagged).GetCustomAttributes;
            return attributes(false)[0].GetType().Name;
        });
        Run("preserved initializer", () =>
        {
            Func<object?, object?[]?, object?> invoke = typeof(Plugin).GetMethod(nameof(Plugin.Start))!.Invoke;
            return (string)invoke(null, null)!;
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
