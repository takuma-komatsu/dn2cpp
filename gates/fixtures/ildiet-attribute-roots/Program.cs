using System;

namespace AttributeRoots;

// No reflection route is armed and nothing else names the By* types: only Holder's
// attribute arguments retain them. Unnamed is the control that stripping removes.
internal static class Program
{
    private static int Main() => typeof(Holder).Name.Length;
}

internal sealed class SelectAttribute : Attribute
{
    public SelectAttribute(Type argument) => Argument = argument;

    public Type Argument { get; }

    public Type? Field;

    public Type? Property { get; set; }
}

internal sealed class SelectAllAttribute : Attribute
{
    public SelectAllAttribute(params Type[] types) => Types = types;

    public Type[] Types { get; }
}

internal sealed class BoxedAttribute : Attribute
{
    public BoxedAttribute(object value) => Value = value;

    public object Value { get; }
}

[Select(typeof(ByArgument), Field = typeof(ByField), Property = typeof(ByProperty))]
[SelectAll(typeof(ByArrayFirst), typeof(ByArraySecond))]
[Boxed(typeof(ByObject))]
internal static class Holder { }

internal sealed class ByArgument { }

internal sealed class ByField { }

internal sealed class ByProperty { }

internal sealed class ByArrayFirst { }

internal sealed class ByArraySecond { }

internal sealed class ByObject { }

internal sealed class Unnamed { }
