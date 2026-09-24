using System;
using System.Text;

namespace GenericMathStaticAbstractGvm;

// A static abstract GENERIC method on a generic interface, dispatched through a
// constrained call (the serializer-generator shape: IPackable<TSelf>.Pack<TSink>).
// An explicit implementation's .override row names an open generic MemberRef no
// MethodInfo models, so the constrained call must template-match the body —
// implicit and dotted explicit alike — and instantiate it at the caller's method
// args, for a struct and a class TSelf, with class and struct TSink method args.
internal interface ISink
{
    void Put(string s);
}

internal sealed class ListSink : ISink
{
    public readonly StringBuilder Sb = new StringBuilder();
    public void Put(string s) { Sb.Append(s).Append(';'); }
}

internal struct CountSink : ISink
{
    public int N;
    public string Last;
    public void Put(string s) { N++; Last = s; }
}

internal interface IPackable<TSelf>
{
    static abstract void Pack<TSink>(ref TSink sink, ref TSelf value) where TSink : ISink;
    static abstract TSelf Seed();
}

internal struct PointPk : IPackable<PointPk>
{
    public int X, Y;
    public static void Pack<TSink>(ref TSink sink, ref PointPk value) where TSink : ISink
        => sink.Put($"P({value.X},{value.Y})");
    public static PointPk Seed() => new PointPk { X = 3, Y = 4 };
}

internal sealed class LabelPk : IPackable<LabelPk>
{
    public string Name;
    static void IPackable<LabelPk>.Pack<TSink>(ref TSink sink, ref LabelPk value)
        => sink.Put($"L({value.Name})");
    static LabelPk IPackable<LabelPk>.Seed() => new LabelPk { Name = "lbl" };
}

internal interface IStaticRow<TSelf>
{
    static abstract string Pick<T>();
}

internal struct StaticRow : IStaticRow<StaticRow>
{
    public static string Pick<T>() => "plain";
    static string IStaticRow<StaticRow>.Pick<T>() => "explicit";
}

internal interface IStaticOverload<TSelf> where TSelf : IStaticOverload<TSelf>
{
    static abstract string Pick<T>(T generic);
    static abstract string Pick<T>(int number);
}

// Both overloads close to Pick<int>(int); their MethodImpl rows still name
// different slots, so the T overload binds the plain body.
internal struct StaticOverload : IStaticOverload<StaticOverload>
{
    public static string Pick<T>(T generic) => "plain";
    static string IStaticOverload<StaticOverload>.Pick<T>(int number) => "explicit-int";
}

internal interface IStaticBase<TSelf>
{
    static abstract string Tag();
}

internal interface IStaticDerived<TSelf> : IStaticBase<TSelf>
{
    static string IStaticBase<TSelf>.Tag() => "derived";
}

internal class StaticDerived : IStaticDerived<StaticDerived>
{
    public static string Tag() => "class";
}

internal interface IStaticInheritedBase<TSelf>
{
    static abstract string Pick<T>();
    static abstract string Tag();
    static virtual string Name<T>() => "base";
    static virtual string Label() => "base";
}

internal interface IStaticInherited<TSelf> : IStaticInheritedBase<TSelf>
{
    static string IStaticInheritedBase<TSelf>.Pick<T>() => "derived:" + typeof(T).Name;
    static string IStaticInheritedBase<TSelf>.Tag() => "derived";
    static string IStaticInheritedBase<TSelf>.Name<T>() => "derived";
    static string IStaticInheritedBase<TSelf>.Label() => "derived";
}

internal interface IStaticInheritedMost<TSelf> : IStaticInherited<TSelf>
{
    static string IStaticInheritedBase<TSelf>.Pick<T>() => "most:" + typeof(T).Name;
    static string IStaticInheritedBase<TSelf>.Label() => "most";
}

// None of these declares a static body, so each call binds the most specific
// derived interface's explicit body instead of the declaration.
internal class StaticInherits : IStaticInherited<StaticInherits>
{
}

internal struct StaticInheritsValue : IStaticInherited<StaticInheritsValue>
{
}

internal class StaticInheritsGeneric<TArg> : IStaticInherited<StaticInheritsGeneric<TArg>>
{
}

internal class StaticInheritsMost : IStaticInheritedMost<StaticInheritsMost>
{
}

internal static class StaticAbstractGenericMethod
{
    static string PickStatic<T>() where T : IStaticRow<T> => T.Pick<int>();
    static string TagStatic<T>() where T : IStaticBase<T> => T.Tag();
    static string OverloadByT<T>() where T : IStaticOverload<T> => T.Pick<int>(generic: 5);
    static string OverloadByInt<T>() where T : IStaticOverload<T> => T.Pick<int>(number: 5);
    static string InheritedStatic<T>() where T : IStaticInheritedBase<T>
        => $"{T.Pick<int>()}/{T.Pick<string>()}/{T.Tag()}/{T.Name<int>()}/{T.Label()}";
    // constrained. !!T; call IPackable<T>::Pack<TSink> — both type dimensions closed
    // by the caller: T by the class context, TSink by the method args.
    static string ViaListSink<T>() where T : IPackable<T>
    {
        var sink = new ListSink();
        T v = T.Seed();
        T.Pack(ref sink, ref v);
        T.Pack(ref sink, ref v);
        return sink.Sb.ToString();
    }

    static string ViaCountSink<T>() where T : IPackable<T>
    {
        var sink = new CountSink();
        T v = T.Seed();
        T.Pack(ref sink, ref v);
        T.Pack(ref sink, ref v);
        return $"{sink.N}:{sink.Last}";
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Static-abstract generic method (constrained call) ==");
        Console.WriteLine($"struct/list   {ViaListSink<PointPk>()}");
        Console.WriteLine($"class/list    {ViaListSink<LabelPk>()}");
        Console.WriteLine($"struct/count  {ViaCountSink<PointPk>()}");
        Console.WriteLine($"class/count   {ViaCountSink<LabelPk>()}");
    }

    internal static void __GateImplSelectionEntry()
    {
        Console.WriteLine("== Static interface implementation selection ==");
        Console.WriteLine($"static explicit {PickStatic<StaticRow>()}");
        Console.WriteLine($"static class {TagStatic<StaticDerived>()}");
        Console.WriteLine($"static overload {OverloadByT<StaticOverload>()}/{OverloadByInt<StaticOverload>()}");
        Console.WriteLine($"static inherited class {InheritedStatic<StaticInherits>()}");
        Console.WriteLine($"static inherited struct {InheritedStatic<StaticInheritsValue>()}");
        Console.WriteLine($"static inherited generic {InheritedStatic<StaticInheritsGeneric<string>>()}");
        Console.WriteLine($"static inherited most {InheritedStatic<StaticInheritsMost>()}");
    }
}
