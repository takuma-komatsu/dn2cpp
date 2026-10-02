using System;

namespace MethodImplShapeSubset;

internal interface IPair<X>
{
    string Pick<U>(X value);
    string Pick<U>(object value);
}

internal sealed class ExplicitPair<X> : IPair<X>
{
    // Reverse the declaration order so a closed-signature first match is wrong.
    string IPair<X>.Pick<U>(object value) => "object";
    string IPair<X>.Pick<U>(X value) => "X";
}

internal interface IDefaultPair<X>
{
    string Pick<U>(X value) => "default-X";
    string Pick<U>(object value) => "default-object";
}

internal sealed class SinglePair<X> : IDefaultPair<X>
{
    string IDefaultPair<X>.Pick<U>(object value) => "single-object";
}

internal interface IDerivedPair<X> : IDefaultPair<X>
{
    string IDefaultPair<X>.Pick<U>(object value) => "derived-object";
    string IDefaultPair<X>.Pick<U>(X value) => "derived-X";
}

internal sealed class DerivedPair<X> : IDerivedPair<X>
{
}

internal interface IStaticPair<TSelf, X> where TSelf : IStaticPair<TSelf, X>
{
    static abstract string Pick<U>(X value);
    static abstract string Pick<U>(object value);
}

internal struct StaticPair<X> : IStaticPair<StaticPair<X>, X>
{
    static string IStaticPair<StaticPair<X>, X>.Pick<U>(object value) => "static-object";
    static string IStaticPair<StaticPair<X>, X>.Pick<U>(X value) => "static-X";
}

internal static class Program
{
    private static string PairX<X, U>(IPair<X> receiver) => receiver.Pick<U>(default(X));
    private static string PairObject<X, U>(IPair<X> receiver) => receiver.Pick<U>((object)null);
    private static string DefaultX<X, U>(IDefaultPair<X> receiver) => receiver.Pick<U>(default(X));
    private static string DefaultObject<X, U>(IDefaultPair<X> receiver) => receiver.Pick<U>((object)null);
    private static string StaticX<T, X, U>() where T : IStaticPair<T, X> => T.Pick<U>(default(X));
    private static string StaticObject<T, X, U>() where T : IStaticPair<T, X> => T.Pick<U>((object)null);

    internal static void Run()
    {
        Console.WriteLine("== ordinary MethodImpl definition shapes ==");
        var pair = new ExplicitPair<object>();
        Console.WriteLine("methodimpl-interface-object=" + PairX<object, int>(pair) + "/" + PairObject<object, int>(pair)
            + "/" + PairX<object, string>(pair) + "/" + PairObject<object, string>(pair));
        var different = new ExplicitPair<string>();
        Console.WriteLine("methodimpl-interface-string=" + PairX<string, int>(different) + "/" + PairObject<string, int>(different));
        var single = new SinglePair<object>();
        Console.WriteLine("methodimpl-single-default=" + DefaultX<object, int>(single) + "/" + DefaultObject<object, int>(single));
        var singleDifferent = new SinglePair<string>();
        Console.WriteLine("methodimpl-single-string=" + DefaultX<string, int>(singleDifferent) + "/" + DefaultObject<string, int>(singleDifferent));
        var derived = new DerivedPair<object>();
        Console.WriteLine("methodimpl-derived-default=" + DefaultX<object, int>(derived) + "/" + DefaultObject<object, int>(derived));
        Console.WriteLine("methodimpl-static-object=" + StaticX<StaticPair<object>, object, int>() + "/"
            + StaticObject<StaticPair<object>, object, int>() + "/" + StaticX<StaticPair<object>, object, string>());
        Console.WriteLine("methodimpl-static-string=" + StaticX<StaticPair<string>, string, int>() + "/"
            + StaticObject<StaticPair<string>, string, int>());
        Console.WriteLine("ordinary MethodImpl definition shapes end");
    }
}
