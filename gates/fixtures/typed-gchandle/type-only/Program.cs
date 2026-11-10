using System;
using System.Globalization;
using System.Runtime.InteropServices;

public sealed class Holder<T> where T : class
{
    public GCHandle<T> Handle;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("typed-type-disposable:" + typeof(IDisposable).IsAssignableFrom(typeof(GCHandle<string>)));
        Console.WriteLine("typed-type-equatable:" + typeof(IEquatable<GCHandle<string>>).IsAssignableFrom(typeof(GCHandle<string>)));
        Console.WriteLine("typed-type-wrong-closed:" + !typeof(IEquatable<GCHandle<object>>).IsAssignableFrom(typeof(GCHandle<string>)));
        Console.WriteLine("typed-type-object-disposable:" + typeof(IDisposable).IsAssignableFrom(typeof(GCHandle<object>)));
        Console.WriteLine("typed-type-object-equatable:" + typeof(IEquatable<GCHandle<object>>).IsAssignableFrom(typeof(GCHandle<object>)));
        var fields = typeof(Holder<>).GetFields();
        Console.WriteLine("typed-type-open-field:" + (fields.Length == 1 && fields[0].Name == "Handle"));
        Console.WriteLine("Typed GC handle types end");
        return;
    }
}
