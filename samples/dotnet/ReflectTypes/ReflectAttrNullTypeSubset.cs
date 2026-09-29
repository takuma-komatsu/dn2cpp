#nullable disable
using System;

namespace ReflectAttrNullTypeSubset;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class NullTypeAttribute : Attribute
{
    internal Type Positional { get; }
    internal Type[] Elements { get; }
    public Type Named { get; set; }

    public NullTypeAttribute(Type positional, Type[] elements)
    {
        Positional = positional;
        Elements = elements;
    }
}

[NullType(null, new Type[] { typeof(string), null }, Named = null)]
internal sealed class Subject
{
}

internal static class Program
{
    internal static void Run()
    {
        object[] attributes = typeof(Subject).GetCustomAttributes(typeof(NullTypeAttribute), false);
        Console.WriteLine("null-type-attribute-count=" + attributes.Length);
        var attribute = (NullTypeAttribute)attributes[0];
        Console.WriteLine("null-type-positional=" + (attribute.Positional is null));
        Console.WriteLine("null-type-elements=" + attribute.Elements.Length + "/"
            + attribute.Elements[0].Name + "/" + (attribute.Elements[1] is null));
        Console.WriteLine("null-type-named=" + (attribute.Named is null));
    }
}
