using System;
using System.Globalization;
using System.Reflection;

// SUBJECT: unknown generic arguments retain diagnostics and bodyless template
// identity, but do not select executable members from existing instantiations or
// templates. Independently called closed members remain usable. Missing bodies
// refuse with catchable PlatformNotSupportedException and an explicit remedy.
// Known finite arguments still form supported instances; an unproved interface
// constraint does not mint a static instantiation. Unknown definitions and method
// receivers report separately, and self-nesting formation does not open Wrap.
namespace ClosedGenericDiagnostics;

struct Cell<T>
{
    public string Describe() => "cell:" + typeof(T).Name;
}

sealed class Holder<T>
{
    public string Describe() => "holder:" + typeof(T).Name;
}

static class Names
{
    public static string Name<T>() => "name:" + typeof(T).Name;
}

sealed class Nest<T>
{
    public string Describe() => "nest:" + typeof(T).Name;

    public Nest<Nest<T>>? Wrap() => null;
}

abstract class Shape<T>
{
    public abstract string Describe();
}

sealed class Square<T> : Shape<T>
{
    public override string Describe() => "square:" + typeof(T).Name;
}

sealed class Product
{
    public override string ToString() => "product";
}

sealed class Factory<T>
{
    public object? Make() => Activator.CreateInstance(typeof(T));
}

sealed class Disposable : IDisposable
{
    public void Dispose() { }
}

interface ITag<T> { }

sealed class RightTag : ITag<string> { }

sealed class WrongTag : ITag<int> { }

sealed class NeedsDispose<T> where T : IDisposable
{
    public static int Marker = 1;

    public static string Run() => "dispose:" + typeof(T).Name;
}

sealed class NeedsTag<T> where T : ITag<string>
{
    public static int Marker = 1;

    public static string Run() => "tag:" + typeof(T).Name;
}

sealed class NeedsTemplateBodyTag<T> where T : ITag<string>
{
    public static string Run() => typeof(System.Collections.Generic.List<T>).Name;
}

sealed class NeedsTemplateMethodTag<T> where T : ITag<string>
{
    public static string Run<U>() => typeof(U).Name;
}

static class Program
{
    private static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("== closed generic diagnostics ==");
        Console.WriteLine("named " + new Cell<int>().Describe() + " " + new Holder<int>().Describe() + " "
            + Names.Name<int>());
        Show("cell named", () => MakeCell(typeof(int)));
        Show("cell unnamed", () => MakeCell(typeof(long)));
        Show("holder named", () => MakeHolder(typeof(int)));
        Show("holder unnamed", () => MakeHolder(typeof(long)));
        Show("method named", () => MakeName(typeof(int)));
        Show("method unnamed", () => MakeName(typeof(long)));
        Console.WriteLine("closed generic diagnostics end");

        Console.WriteLine("== closed generic receivers ==");
        Console.WriteLine("named " + new Nest<int>().Describe());
        Show("definition parameter", () => MakeFromDefinition(typeof(Holder<>)));
        Show("method parameter", () => MakeFromMethod(typeof(Names).GetMethod("Name")!));
        Show("self-nesting named", () => MakeNest(typeof(int)));
        Console.WriteLine("closed generic receivers end");

        Console.WriteLine("== closed generic templates ==");
        Show("abstract base unnamed", () => MakeShape(typeof(long)));
        Show("template body unnamed", () => MakeFactory(typeof(Product)));
        Show("clone argument unnamed", () => MakeWrapped(typeof(long)));
        Console.WriteLine("closed generic templates end");

        Console.WriteLine("== static generic constraint proof ==");
        Show("constraint interface control", () => (string)typeof(NeedsDispose<>).MakeGenericType(typeof(Disposable))
            .GetMethod("Run")!.Invoke(null, null)!);
        Show("constraint invariant control", () => (string)typeof(NeedsTag<>).MakeGenericType(typeof(RightTag))
            .GetMethod("Run")!.Invoke(null, null)!);
        Show("constraint array rejected", InvalidArray);
        Show("constraint invariant rejected", InvalidInvariant);
        Console.WriteLine("static generic constraint proof end");

        if (args.Length > 0 && args[0] == "before-template-constraint-fallbacks")
            return;
        Console.WriteLine("== template fallback constraint proof ==");
        Show("constraint template body control", () => (string)typeof(NeedsTemplateBodyTag<>).MakeGenericType(typeof(RightTag))
            .GetMethod("Run")!.Invoke(null, null)!);
        Show("constraint template method control", () => (string)typeof(NeedsTemplateMethodTag<>).MakeGenericType(typeof(RightTag))
            .GetMethod("Run")!.MakeGenericMethod(typeof(int)).Invoke(null, null)!);
        Show("constraint template body rejected", InvalidTemplateBody);
        Show("constraint template method rejected", InvalidTemplateMethod);
        Console.WriteLine("template fallback constraint proof end");
    }

    private static string InvalidArray() => (string)typeof(NeedsDispose<>).MakeGenericType(typeof(int[]))
        .GetMethod("Run")!.Invoke(null, null)!;

    private static string InvalidInvariant() => (string)typeof(NeedsTag<>).MakeGenericType(typeof(WrongTag))
        .GetMethod("Run")!.Invoke(null, null)!;

    private static string InvalidTemplateBody() => (string)typeof(NeedsTemplateBodyTag<>).MakeGenericType(typeof(WrongTag))
        .GetMethod("Run")!.Invoke(null, null)!;

    private static string InvalidTemplateMethod() => (string)typeof(NeedsTemplateMethodTag<>).MakeGenericType(typeof(WrongTag))
        .GetMethod("Run")!.MakeGenericMethod(typeof(int)).Invoke(null, null)!;

    private static string MakeShape(Type argument)
    {
        Type shape = typeof(Shape<>).MakeGenericType(argument);
        Type square = typeof(Square<>).MakeGenericType(argument);
        return (string)shape.GetMethod("Describe")!.Invoke(Activator.CreateInstance(square), null)!;
    }

    // Make constructs whatever argument the clone has, which nothing allocates.
    private static string MakeFactory(Type argument)
    {
        Type closed = typeof(Factory<>).MakeGenericType(argument);
        return "made " + closed.GetMethod("Make")!.Invoke(Activator.CreateInstance(closed), null);
    }

    private static string MakeWrapped(Type argument)
    {
        Type closed = typeof(Cell<>).MakeGenericType(typeof(Holder<>).MakeGenericType(argument));
        return (string)closed.GetMethod("Describe")!.Invoke(Activator.CreateInstance(closed), null)!;
    }

    // The definition and the generic method are parameters as well: nothing is selected
    // for them, and their sites report.
    private static string MakeFromDefinition(Type definition) => definition.MakeGenericType(typeof(int)).Name;

    private static string MakeFromMethod(MethodInfo method) => method.MakeGenericMethod(typeof(int)).Name;

    // Naming Nest<int> independently does not authorize its uncalled Wrap body.
    private static string MakeNest(Type argument)
    {
        Type closed = typeof(Nest<>).MakeGenericType(argument);
        return "nest:" + (closed.GetMethod("Wrap")!.Invoke(Activator.CreateInstance(closed), null) is null);
    }

    // Each argument is a parameter, which the summary does not follow into callers.
    private static string MakeCell(Type argument)
    {
        Type closed = typeof(Cell<>).MakeGenericType(argument);
        return (string)closed.GetMethod("Describe")!.Invoke(Activator.CreateInstance(closed), null)!;
    }

    private static string MakeHolder(Type argument)
    {
        Type closed = typeof(Holder<>).MakeGenericType(argument);
        return (string)closed.GetMethod("Describe")!.Invoke(Activator.CreateInstance(closed), null)!;
    }

    private static string MakeName(Type argument) =>
        (string)typeof(Names).GetMethod("Name")!.MakeGenericMethod(argument).Invoke(null, null)!;

    private static void Show(string label, Func<string> run)
    {
        string result;
        try
        {
            result = run();
        }
        catch (Exception e)
        {
            Exception shown = e is TargetInvocationException { InnerException: { } inner } ? inner : e;
            result = shown.GetType().Name + " catchable=" + (shown is NotSupportedException) + " " + shown.Message;
        }
        Console.WriteLine(label + ": " + result);
    }
}
