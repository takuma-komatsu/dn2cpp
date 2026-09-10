using System;
using System.Globalization;
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
    }

    private static int RunStatic<T>(int value) where T : IStatic<T> => T.Evaluate(value);
}

public static class UnusedAppType
{
    public static int UnusedPublic() => -4;
}
