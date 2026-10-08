using System.Reflection;

namespace ReflectionDepthSummary;

#if UNREAD_ATTRIBUTE_CTOR_DEPTH
internal sealed class UnreadDepthConstructorAttribute : Attribute
{
    public UnreadDepthConstructorAttribute() => DepthAttributeRecurse<int>(0);
    private static void DepthAttributeRecurse<T>(int count)
    {
        if (count > 0) DepthAttributeRecurse<List<T>>(count - 1);
    }
}
#elif UNREAD_ATTRIBUTE_GETTER_DEPTH
internal sealed class UnreadDepthGetterAttribute : Attribute
{
    public int Level
    {
        get { DepthAttributeRecurse<int>(0); return 1; }
        set { }
    }

    private static void DepthAttributeRecurse<T>(int count)
    {
        if (count > 0) DepthAttributeRecurse<List<T>>(count - 1);
    }
}
#elif ATTRIBUTE_READ_DEPTH || ATTRIBUTE_GENERIC_READ_DEPTH
internal static class AttributeDepthDriver
{
    internal static void Read()
    {
#if ATTRIBUTE_GENERIC_READ_DEPTH
        var attribute = typeof(Program).GetCustomAttribute<ReadDepthAttribute>()!;
#else
        var attribute = (ReadDepthAttribute)typeof(Program).GetCustomAttributes(typeof(ReadDepthAttribute), false)[0];
#endif
        Console.WriteLine(attribute.Observed);
    }
}

internal class ReadDepthBaseAttribute : Attribute
{
    public int Level
    {
        get { DepthAttributeRecurse<int>(0); return 0; }
        set { DepthAttributeRecurse<int>(0); }
    }

    private static void DepthAttributeRecurse<T>(int count)
    {
        if (count > 0) DepthAttributeRecurse<List<T>>(count - 1);
    }
}

internal sealed class ReadDepthAttribute : ReadDepthBaseAttribute
{
    public int Observed;

    public ReadDepthAttribute()
    {
        Program.Seed<List<List<List<int>>>>();
        Console.WriteLine("attribute-ctor");
    }

    public new int Level
    {
        get { DepthAttributeRecurse<int>(0); return Observed; }
        set
        {
            Program.Seed<List<List<List<List<int>>>>>();
            Observed = value;
            Console.WriteLine("attribute-setter");
        }
    }

    private static void DepthAttributeRecurse<T>(int count)
    {
        if (count > 0) DepthAttributeRecurse<List<T>>(count - 1);
    }
}
#endif
