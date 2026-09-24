using System;
using System.Globalization;
using System.Reflection;
using ILDietControlLib;

namespace ILDietControl;

internal static class Program
{
    private static unsafe void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("initializers=" + Initialization.Value + ":" + StaticInitialization.Read());
        IFoo inherited = new Derived();
        Console.WriteLine("inherited=" + inherited.Foo());
        IGeneric<int> number = new ExplicitGeneric<int>();
        IGeneric<string> text = new ExplicitGeneric<string>();
        Console.WriteLine("generic=" + number.Identity(8) + ":" + text.Identity("kept"));
        Console.WriteLine("static-interface=" + RunStatic<StaticValue>(3));
        Func<int, int> callback = Callbacks.ManagedDelegate;
        Console.WriteLine("delegate=" + callback(5));
        var layout = new Layout { Used = 7, Tail = 4 };
        Console.WriteLine("layout=" + sizeof(Layout) + ":" + (layout.Used + layout.Tail));
        // Loose's type token precedes the first constructing call and the others
        // follow it: constructors survive in either order.
        Console.WriteLine("activator=" + ((Shape)Activator.CreateInstance(typeof(Loose))!).Who());
        Console.WriteLine("activator-closed=" + ((Shape)Activator.CreateInstance(typeof(Box<int>))!).Who());
        Type made = typeof(Box<>).MakeGenericType(typeof(string));
        Console.WriteLine("activator-made=" + ((Shape)Activator.CreateInstance(made)!).Who());
        ConstructorInfo empty = typeof(Seeded).GetConstructor(Type.EmptyTypes)!;
        ConstructorInfo sized = typeof(Seeded).GetConstructor(new[] { typeof(int) })!;
        Console.WriteLine("ctor-info=" + ((Shape)empty.Invoke(null)).Who() + ":"
            + ((Shape)sized.Invoke(new object[] { 5 })).Who());
        Console.WriteLine("called-only=" + CalledOnly.Name());
    }

    private static int RunStatic<T>(int value) where T : IStatic<T> => T.Evaluate(value);
}

public static class UnusedAppType
{
    public static int UnusedPublic() => -4;
}

internal abstract class Shape
{
    public abstract string Who();
}

internal sealed class Loose : Shape
{
    public override string Who() => "loose";
}

internal sealed class Box<T> : Shape
{
    public override string Who() => "box:" + typeof(T).Name;
}

internal sealed class Seeded : Shape
{
    private readonly int _value;

    public Seeded() => _value = -1;

    public Seeded(int value) => _value = value;

    public override string Who() => "seeded:" + _value;
}

// A static call token alone never selects the instance constructor.
internal sealed class CalledOnly
{
    public static string Name() => "called-only";
}
