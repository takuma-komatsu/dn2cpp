using System;
using System.Collections.Generic;
using System.Reflection;

namespace OrdinaryReflectionTypeLeaves;

internal static class PropertyAccessorRowsSubset
{
    internal static void Run()
    {
        Console.WriteLine("== retained property accessor rows ==");
        Type type = new List<int>().GetType();
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
        int index = 0;
        foreach (Type definition in OpenDefinitionFlagTypes.Cases())
            PrintFlags(index++, definition);
        Console.WriteLine("direct nested flags=" + typeof(OpenDefinitionFlagTypes.Public<>).IsNested + ":"
            + typeof(OpenDefinitionFlagTypes.Struct<>).IsValueType + ":"
            + typeof(OpenDefinitionFlagTypes.Struct<>).IsSealed + ":"
            + typeof(OpenDefinitionFlagTypes.Interface<>).IsClass + ":"
            + typeof(OpenDefinitionFlagTypes.RefStruct<>).IsByRefLike);
        Console.WriteLine("nested open definition identity end");
    }

    private static void PrintFlags(int index, Type definition)
    {
        Console.WriteLine("open flags:" + index + "=" + definition.IsNested + ":"
            + definition.IsPublic + ":" + definition.IsNotPublic + ":" + definition.IsVisible + ":"
            + definition.IsNestedPublic + ":" + definition.IsNestedPrivate + ":"
            + definition.IsNestedAssembly + ":" + definition.IsNestedFamily + ":"
            + definition.IsNestedFamANDAssem + ":" + definition.IsNestedFamORAssem + ":"
            + definition.IsClass + ":" + definition.IsValueType + ":" + definition.IsInterface + ":"
            + definition.IsAbstract + ":" + definition.IsSealed + ":" + definition.IsByRefLike + ":"
            + (int)definition.Attributes);
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

public class OpenDefinitionFlagTypes
{
    public class Public<T> { }
    private class Private<T> { }
    internal class Internal<T> { }
    protected class Protected<T> { }
    private protected class FamilyAndAssembly<T> { }
    protected internal class FamilyOrAssembly<T> { }
    public struct Struct<T> { }
    public ref struct RefStruct<T> { }
    public interface Interface<T> { }
    public abstract class Abstract<T> { }
    public static class Static<T> { }
    private class Hidden { public class Child<T> { } }

    internal static Type[] Cases() => new[] {
        typeof(Public<>), typeof(Private<>), typeof(Internal<>), typeof(Protected<>),
        typeof(FamilyAndAssembly<>), typeof(FamilyOrAssembly<>), typeof(Struct<>),
        typeof(RefStruct<>), typeof(Interface<>), typeof(Abstract<>), typeof(Static<>),
        typeof(Hidden.Child<>)
    };
}
