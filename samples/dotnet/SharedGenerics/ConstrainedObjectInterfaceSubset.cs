using System;

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

internal static class Program
{
    private static string Invoke<T>(T value) where T : struct, IObjectNames =>
        value.GetHashCode() + "/" + value.ToString() + "/" + value.Equals(null);

    private static string InvokeObject<T>(T value) where T : struct =>
        value.GetHashCode() + "/" + value.ToString() + "/" + value.Equals(null);

    private static int Convert<T>(T value) where T : struct, IConvertible =>
        value.ToInt32(null);

    internal static void Run()
    {
        var value = new ExplicitNames();
        Console.WriteLine("constrained interface=" + Invoke(value));
        Console.WriteLine("constrained bare=" + Invoke(new BareNames()) + "/" + BareNames.HashCalls);
        Console.WriteLine("constrained object=" + InvokeObject(value));
        Console.WriteLine("constrained boxed primitive=" + Convert(42));
        Console.WriteLine("constrained boxed enum=" + Convert(Shade.Blue));
    }
}
