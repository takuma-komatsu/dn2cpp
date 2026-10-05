using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace NestedTypeNameProbe;

internal sealed class Plain
{
}

internal sealed class Outer<T>
{
    internal sealed class Node
    {
    }

    internal sealed class Inner<U>
    {
        internal sealed class Leaf<V>
        {
        }
    }
}

internal static class ClosedNestedReflectionNamesSubset
{
    static void Names(string label, Type type)
    {
        Console.WriteLine(label + " Name=" + type.Name);
        Console.WriteLine(label + " FullName=" + (type.FullName ?? "<null>"));
        Console.WriteLine(label + " AssemblyQualifiedName=" + (type.AssemblyQualifiedName ?? "<null>"));
        Console.WriteLine(label + " ToString=" + type.ToString());
        Console.WriteLine(label + " Namespace=" + (type.Namespace ?? "<null>"));
    }

    static void DisposedName<T>(string label)
    {
        var collection = new BlockingCollection<T>();
        collection.Dispose();
        try
        {
            _ = collection.Count;
            Console.WriteLine(label + " disposed type=returned");
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine(label + " disposed type=" + ex.GetType().Name);
            Console.WriteLine(label + " disposed ObjectName=" + ex.ObjectName);
        }
    }

    static void AssemblyOwnership(string label, Type type)
    {
        Assembly assembly = type.Assembly;
        string fullName = type.FullName!;
        Console.WriteLine(label + " assembly roundtrip=" + (assembly.GetType(fullName) == type));
        Console.WriteLine(label + " assembly explicit=" + (assembly.GetType(fullName, false, false) == type));
        try
        {
            Console.WriteLine(label + " assembly throwing=" + (assembly.GetType(fullName, true, false) == type));
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + " assembly throwing=" + ex.GetType().Name);
        }
        if (type.IsArray)
            Console.WriteLine(label + " assembly ignore-case=" + (assembly.GetType(fullName.ToLowerInvariant(), false, true) == type));
        Console.WriteLine(label + " member module=" + (((MemberInfo)type).Module == type.Module));
    }

    internal static void RunChecks()
    {
        Console.WriteLine("== closed nested reflection names ==");
        Names("global", typeof(global::Program));
        Names("plain", typeof(Plain));
        Names("outer-reference", typeof(Outer<string>));
        Names("outer-value", typeof(Outer<int>));
        Names("nested-definition", typeof(Outer<>.Node));
        Names("nested-reference", typeof(Outer<string>.Node));
        Names("nested-value", typeof(Outer<int>.Node));
        Names("nested-generic", typeof(Outer<string>.Inner<int>));
        Names("nested-generic-value", typeof(Outer<int>.Inner<string>));
        Names("nested-deep", typeof(Outer<string>.Inner<int>.Leaf<long>));
        Names("nested-array-arguments", typeof(Outer<string[]>.Inner<int[]>));
        Names("nested-array", typeof(Outer<string>.Node[]));
        Names("nested-jagged-array", typeof(Outer<string>.Node[][]));
        Names("nested-rank2-array", typeof(Outer<string>.Node[,]));
        Names("nested-user-array-arguments", typeof(Outer<Plain[]>.Inner<Outer<string>.Node[]>));
        DisposedName<Plain>("plain");
        DisposedName<Outer<string>>("outer-reference");
        DisposedName<Outer<string>.Node>("nested-reference");
        DisposedName<Outer<int>.Node>("nested-value");
        DisposedName<Outer<string>.Inner<int>>("nested-generic");
        DisposedName<Outer<int>.Inner<string>>("nested-generic-value");
        DisposedName<Outer<string>.Inner<int>.Leaf<long>>("nested-deep");
        DisposedName<Outer<string[]>.Inner<int[]>>("nested-array-arguments");
        DisposedName<Outer<string>.Node[]>("nested-array");
        DisposedName<Outer<string>.Node[][]>("nested-jagged-array");
        DisposedName<Outer<string>.Node[,]>("nested-rank2-array");
        DisposedName<Outer<Plain[]>.Inner<Outer<string>.Node[]>>("nested-user-array-arguments");
        Console.WriteLine("closed nested reflection names end");
        Console.WriteLine("== array assembly ownership ==");
        AssemblyOwnership("plain-array", typeof(Plain[]));
        AssemblyOwnership("plain-jagged-array", typeof(Plain[][]));
        AssemblyOwnership("plain-rank2-array", typeof(Plain[,]));
        AssemblyOwnership("closed-generic", typeof(Outer<string>.Inner<int>));
        Console.WriteLine("array assembly ownership end");
    }
}
