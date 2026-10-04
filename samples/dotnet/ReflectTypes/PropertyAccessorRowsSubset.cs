using System;
using System.Collections.Generic;
using System.Reflection;

namespace OrdinaryReflectionTypeLeaves;

internal static class PropertyAccessorRowsSubset
{
    internal static void Run()
    {
        Console.WriteLine("== retained property accessor rows ==");
        Type type = typeof(List<int>);
        foreach (string name in new[] { "Capacity", "Count", "Item", "System.Collections.Generic.ICollection<T>.IsReadOnly" })
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
            MethodInfo? getter = property.GetGetMethod(true);
            MethodInfo? setter = property.GetSetMethod(true);
            Console.WriteLine(name + "=" + property.CanRead + ":" + property.CanWrite + ":" +
                (getter?.Name ?? "<null>") + ":" + (setter?.Name ?? "<null>"));
            if (getter is null)
                throw new InvalidOperationException("Property getter description was lost: " + name);
            Console.WriteLine(name + " identity=" + (getter.DeclaringType == type) + ":" + (getter.ReturnType == property.PropertyType));
        }
        Console.WriteLine("retained property accessor rows end");
        Console.WriteLine("== nested open definition identity ==");
        Type first = typeof(Converters.First<>);
        Type second = typeof(Converters.Second<>);
        Type third = typeof(Outer<>.Inner<>);
        Console.WriteLine("definitions=" + (first is not null) + ":" + (second is not null) + ":" + (first != second));
        Console.WriteLine("first=" + first!.FullName + ":" + first.IsGenericTypeDefinition + ":" + first.ContainsGenericParameters);
        Console.WriteLine("second=" + second!.FullName + ":" + second.IsGenericTypeDefinition);
        Console.WriteLine("generic outer=" + third.FullName + ":" + third.IsGenericTypeDefinition);
        Console.WriteLine("nested open definition identity end");
    }

    private static class Converters
    {
        internal sealed class First<T> { }
        internal sealed class Second<T> { }
    }

    private sealed class Outer<T>
    {
        internal sealed class Inner<U> { }
    }
}
