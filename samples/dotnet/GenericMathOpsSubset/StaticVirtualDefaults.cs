using System;

namespace GenericMathStaticVirtuals;

// Static virtual interface members carrying a *default body* (`static virtual`
// with an implementation), dispatched through a constrained generic helper. Real
// .NET binds the concrete TSelf's implementation when one exists and the
// interface's own default body otherwise — a struct's override (implicit or
// explicit) must win over the default, and a struct providing no body must run
// the default. Covers a string-flavored member and an arithmetic one whose
// default body itself constrained-calls a static-abstract sibling.
internal interface ITagged<TSelf> where TSelf : ITagged<TSelf>
{
    static virtual string Tag() => "default";
}

internal interface IScalable<TSelf> where TSelf : IScalable<TSelf>
{
    static abstract TSelf Add(TSelf a, TSelf b);
    static virtual TSelf Twice(TSelf v) => TSelf.Add(v, v);
}

internal readonly struct DefaultTag : ITagged<DefaultTag>
{
}

internal readonly struct CustomTag : ITagged<CustomTag>
{
    public static string Tag() => "custom";
}

internal readonly struct ExplicitTag : ITagged<ExplicitTag>
{
    static string ITagged<ExplicitTag>.Tag() => "explicit";
}

internal readonly struct DefaultScale : IScalable<DefaultScale>
{
    public readonly int Value;
    public DefaultScale(int value) => Value = value;
    public static DefaultScale Add(DefaultScale a, DefaultScale b) => new DefaultScale(a.Value + b.Value);
}

internal readonly struct TenfoldScale : IScalable<TenfoldScale>
{
    public readonly int Value;
    public TenfoldScale(int value) => Value = value;
    public static TenfoldScale Add(TenfoldScale a, TenfoldScale b) => new TenfoldScale(a.Value + b.Value);
    public static TenfoldScale Twice(TenfoldScale v) => new TenfoldScale(v.Value * 10);
}

internal readonly struct ExplicitScale : IScalable<ExplicitScale>
{
    public readonly int Value;
    public ExplicitScale(int value) => Value = value;
    public static ExplicitScale Add(ExplicitScale a, ExplicitScale b) => new ExplicitScale(a.Value + b.Value);
    static ExplicitScale IScalable<ExplicitScale>.Twice(ExplicitScale v) => new ExplicitScale(v.Value * 100);
}

internal static class StaticVirtualDefaults
{
    static string TagOf<T>() where T : ITagged<T> => T.Tag();
    static T TwiceOf<T>(T v) where T : IScalable<T> => T.Twice(v);

    internal static void __GateEntry()
    {
        Console.WriteLine("== Static-virtual default bodies (constrained call) ==");
        Console.WriteLine($"default tag   {TagOf<DefaultTag>()}");
        Console.WriteLine($"override tag  {TagOf<CustomTag>()}");
        Console.WriteLine($"explicit tag  {TagOf<ExplicitTag>()}");
        Console.WriteLine($"default twice {TwiceOf(new DefaultScale(21)).Value}");
        Console.WriteLine($"override twice {TwiceOf(new TenfoldScale(21)).Value}");
        Console.WriteLine($"explicit twice {TwiceOf(new ExplicitScale(21)).Value}");
    }
}
