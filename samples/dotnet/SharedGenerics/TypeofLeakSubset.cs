#nullable enable
// typeof(T) leaking OUT of a generic body over reference arguments: a shared body
// cannot bake one Type identity for a whole group, so a body whose typeof escapes
// falls back per instantiation. Either way the observable identity must stay
// exact. Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace TypeofLeakSubset;

class Marker { }

class Holder<T>
{
    public Type Get() => typeof(T);
    public Type GetArr() => typeof(T[]);
    public string Name() => typeof(T).Name;
    public Type Stored;
    public Holder() { Stored = typeof(T); }
}

class Program
{
    static Type GetTypeOf<T>() => typeof(T);

    internal static void __GateEntry()
    {
        Console.WriteLine("h str=" + (new Holder<string>().Get() == typeof(string)));
        Console.WriteLine("h marker=" + (new Holder<Marker>().Get() == typeof(Marker)));
        Console.WriteLine("h cross=" + (new Holder<string>().Get() == typeof(Marker)));
        Console.WriteLine("h obj=" + (new Holder<object>().Get() == typeof(object)));
        Console.WriteLine("h arr=" + (new Holder<string>().GetArr() == typeof(string[])));
        Console.WriteLine("h name str=" + new Holder<string>().Name());
        Console.WriteLine("h name marker=" + new Holder<Marker>().Name());
        Console.WriteLine("h stored=" + (new Holder<Marker>().Stored == typeof(Marker)));
        Console.WriteLine("m str=" + (GetTypeOf<string>() == typeof(string)));
        Console.WriteLine("m marker=" + GetTypeOf<Marker>().Name);
        Console.WriteLine("m list=" + (GetTypeOf<List<string>>() == typeof(List<string>)));
    }
}
