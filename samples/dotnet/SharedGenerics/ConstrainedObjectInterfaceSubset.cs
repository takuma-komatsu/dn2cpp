using System;
using System.Runtime.CompilerServices;

namespace ConstrainedObjectInterfaceSubset;

internal interface IObjectNames
{
    int GetHashCode();
    string ToString();
    bool Equals(object other);
}

internal readonly struct ExplicitNames : IObjectNames
{
    public override int GetHashCode() => 11;
    public override string ToString() => "object";
    public override bool Equals(object other) => false;

    int IObjectNames.GetHashCode() => 23;
    string IObjectNames.ToString() => "interface";
    bool IObjectNames.Equals(object other) => other is null;
}

internal readonly struct BareNames : IObjectNames
{
    internal static int HashCalls;

    int IObjectNames.GetHashCode() => 30 + ++HashCalls;
    string IObjectNames.ToString() => "bare interface";
    bool IObjectNames.Equals(object other) => other is null;
}

internal enum Shade { Blue = 7 }

internal static class BooleanReceiverContext<TContext>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string Convert<T>(TContext context, T value) where T : IConvertible =>
        Program.ContextName(context) + "/" + value.ToBoolean(null);
}

internal static class Program
{
    private static string Invoke<T>(T value) where T : struct, IObjectNames =>
        value.GetHashCode() + "/" + value.ToString() + "/" + value.Equals(null);

    private static string InvokeObject<T>(T value) where T : struct =>
        value.GetHashCode() + "/" + value.ToString() + "/" + value.Equals(null);

    private static int Convert<T>(T value) where T : struct, IConvertible =>
        value.ToInt32(null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ConvertBoolean<T>(T value) where T : IConvertible =>
        value.ToBoolean(null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string ContextName(object context) => context.GetType().Name;

    internal static void Run()
    {
        var value = new ExplicitNames();
        Console.WriteLine("constrained interface=" + Invoke(value));
        Console.WriteLine("constrained bare=" + Invoke(new BareNames()) + "/" + BareNames.HashCalls);
        Console.WriteLine("constrained object=" + InvokeObject(value));
        Console.WriteLine("constrained boxed primitive=" + Convert(42));
        Console.WriteLine("constrained boxed enum=" + Convert(Shade.Blue));
    }

    internal static void RunBooleanReceivers()
    {
        Console.WriteLine("== constrained Boolean conversion ==");
        Console.WriteLine("constrained boolean=" + ConvertBoolean(false) + "/" + ConvertBoolean(true));
        Console.WriteLine("constrained boolean siblings=" + ConvertBoolean(0) + "/" + ConvertBoolean(7)
            + "/" + ConvertBoolean((Shade)0) + "/" + ConvertBoolean(Shade.Blue));
        Console.WriteLine("constrained boolean reference=" + ConvertBoolean("False") + "/" + ConvertBoolean("True")
            + "/" + ConvertBoolean<IConvertible>(false) + "/" + ConvertBoolean<IConvertible>(true));
        Console.WriteLine("constrained boolean scalar-context=" + BooleanReceiverContext<int>.Convert(0, false)
            + "/" + BooleanReceiverContext<int>.Convert(0, true) + "/" + BooleanReceiverContext<Shade>.Convert(Shade.Blue, false)
            + "/" + BooleanReceiverContext<Shade>.Convert(Shade.Blue, true));
        Console.WriteLine("constrained boolean reference-context=" + BooleanReceiverContext<string>.Convert("context", false)
            + "/" + BooleanReceiverContext<string>.Convert("context", true) + "/" + BooleanReceiverContext<object>.Convert(new object(), false)
            + "/" + BooleanReceiverContext<object>.Convert(new object(), true));
        Console.WriteLine("constrained Boolean conversion end");
    }
}
