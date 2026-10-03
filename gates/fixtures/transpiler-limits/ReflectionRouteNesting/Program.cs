using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReflectionRouteNesting;

public struct W1<T> { }
public struct W2<T> { }
public struct W3<T> { }
public struct W4<T> { }

// Each method names a deeper instantiation of the definition, so walking every
// instantiation within a nesting depth D would mint about 4^D classes.
public class Nest<T>
{
    public Nest<W1<T>> A() => new Nest<W1<T>>();
    public Nest<W2<T>> B() => new Nest<W2<T>>();
    public Nest<W3<T>> C() => new Nest<W3<T>>();
    public Nest<W4<T>> D() => new Nest<W4<T>>();
    public string Name() => "nest:" + typeof(T).Name;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // A deep framework instantiation sets the nesting the reflection route walks within.
        var deep = new List<List<List<List<List<List<int>>>>>>();
        Console.WriteLine("deep " + deep.Count);
        Console.WriteLine(typeof(Nest<int>).GetMethod("Name")!.Invoke(new Nest<int>(), null));
    }
}
