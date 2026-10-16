using System.Reflection;

namespace ReflectReturnLib;

// Library targets RecognizedReflectionOnly reaches only through recognized reflection.

// A fieldless generic definition no code instantiates: MakeGenericType serves it from
// its runtime-instantiation template, whose clone runs only the members a site selects.
public class LibraryTemplate<T>
{
    public static string Describe() => "library template:" + typeof(T).Name;

    public static string Unselected() => "unselected";
}

// Allocated by the program; only lookups whose results it counts name Listed.
[DefaultMember(nameof(Listed))]
public class LibraryMembers
{
    public string Listed() => "listed";
}

public static class BinderLog
{
    public static string? Text;
}

// A precise initializer: running it at all is observable.
public class BinderBase
{
    static BinderBase() => BinderLog.Text = "base initialized";

    public static string Pick(object value) => "base:" + value;
}

public class BinderDerived : BinderBase
{
    public static string Pick(string value) => "derived:" + value;
}

// A derived type's lookup associates no private accessor of an inherited property.
public class EntityBase
{
    public int Id { get; private set; }
}

// Only a lookup names Make; nothing calls or instantiates it.
public static class GenericListed
{
    public static string Make<T>() => typeof(T).Name;
}

// The public arity-one constructor binds a widened argument; the omitted public
// parameterless and internal arity-one constructors must not refuse it.
public sealed class WidenedOptions
{
    public readonly string Made;

    public WidenedOptions() => Made = "default";

    public WidenedOptions(long value) => Made = "long:" + value;

    internal WidenedOptions(WidenedOptions other) => Made = "copy:" + other.Made;
}
