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

internal static class StaticAbstractGenericMethod
{
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
}
