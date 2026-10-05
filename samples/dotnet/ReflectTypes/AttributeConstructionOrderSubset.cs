using System;
using System.Reflection;

[assembly: AttributeConstructionOrderSubset.First]
[assembly: AttributeConstructionOrderSubset.Other]
[assembly: AttributeConstructionOrderSubset.Second]
[assembly: AttributeConstructionOrderSubset.Third]

namespace AttributeConstructionOrderSubset;

internal static class Construction
{
    internal static string Trace = "";
    internal static int FaultAt;

    internal static void Record(string value)
    {
        Trace += (Trace.Length == 0 ? "" : ",") + value;
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal abstract class OrderedAttribute : Attribute
{
    internal readonly int Position;

    protected OrderedAttribute(int position)
    {
        Position = position;
        Construction.Record("ctor:" + position);
        if (Construction.FaultAt == position)
            throw new InvalidOperationException("attribute fault " + position);
    }

    public override string ToString()
    {
        Construction.Record("display:" + Position);
        return "ordered-" + Position;
    }
}

internal sealed class FirstAttribute : OrderedAttribute
{
    public FirstAttribute() : base(1) { }
}

internal sealed class SecondAttribute : OrderedAttribute
{
    public SecondAttribute() : base(2) { }
}

internal sealed class ThirdAttribute : OrderedAttribute
{
    public ThirdAttribute() : base(3) { }
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class OtherAttribute : Attribute
{
    public OtherAttribute()
    {
        Construction.Record("other");
        throw new InvalidOperationException("nonmatching attribute constructed");
    }
}

internal sealed class MissingAttribute : OrderedAttribute
{
    public MissingAttribute() : base(4) { }
}

[First, Other, Second, Third]
internal sealed class Tagged
{
    [First, Other, Second, Third]
    public static int Field;

    [First, Other, Second, Third]
    public static int Property { get; set; }

    [First, Other, Second, Third]
    public static void Method([First, Other, Second, Third] int value) { }
}

internal static class Program
{
    private static void Probe(string label, Func<Attribute?> query, int faultAt)
    {
        Construction.Trace = "";
        Construction.FaultAt = faultAt;
        string result;
        try
        {
            Attribute? value = query();
            result = value is null ? "null" : "value:" + ((OrderedAttribute)value).Position;
        }
        catch (Exception error)
        {
            result = error.GetType().Name + "|" + error.HResult.ToString("X8") + "|" + error.Message;
        }
        Console.WriteLine(label + "=" + result + " trace=" + Construction.Trace);
        Construction.FaultAt = 0;
    }

    private static void Duplicate(string label, Func<Attribute?> query)
    {
        Probe(label + "/no-fault", query, 0);
        Probe(label + "/second-fault", query, 2);
        Probe(label + "/third-fault", query, 3);
    }

    private static void Defined(string label, Func<bool> query)
    {
        Construction.Trace = "";
        Construction.FaultAt = 1;
        bool defined = query();
        Console.WriteLine(label + "=" + defined + " trace=" + Construction.Trace);
        Construction.FaultAt = 0;
    }

    private static void Member(string label, MemberInfo member)
    {
        Duplicate(label + " static", () => Attribute.GetCustomAttribute(member, typeof(OrderedAttribute), false));
        Duplicate(label + " extension", () => member.GetCustomAttribute(typeof(OrderedAttribute), false));
        Duplicate(label + " generic", () => member.GetCustomAttribute<OrderedAttribute>(false));
        Probe(label + " single-static", () => Attribute.GetCustomAttribute(member, typeof(FirstAttribute), false), 2);
        Probe(label + " single-generic", () => member.GetCustomAttribute<FirstAttribute>(false), 2);
        Probe(label + " absent-static", () => Attribute.GetCustomAttribute(member, typeof(MissingAttribute), false), 1);
        Probe(label + " absent-generic", () => member.GetCustomAttribute<MissingAttribute>(false), 1);
        Defined(label + " defined-static", () => Attribute.IsDefined(member, typeof(OrderedAttribute), false));
        Defined(label + " defined-instance", () => member.IsDefined(typeof(OrderedAttribute), false));
        Defined(label + " absent-defined", () => member.IsDefined(typeof(MissingAttribute), false));
    }

    private static void Parameter(ParameterInfo parameter)
    {
        Duplicate("parameter static", () => Attribute.GetCustomAttribute(parameter, typeof(OrderedAttribute), false));
        Duplicate("parameter extension", () => parameter.GetCustomAttribute(typeof(OrderedAttribute), false));
        Duplicate("parameter generic", () => parameter.GetCustomAttribute<OrderedAttribute>(false));
        Probe("parameter single-static", () => Attribute.GetCustomAttribute(parameter, typeof(FirstAttribute), false), 2);
        Probe("parameter single-generic", () => parameter.GetCustomAttribute<FirstAttribute>(false), 2);
        Probe("parameter absent-static", () => Attribute.GetCustomAttribute(parameter, typeof(MissingAttribute), false), 1);
        Probe("parameter absent-generic", () => parameter.GetCustomAttribute<MissingAttribute>(false), 1);
        Defined("parameter defined-static", () => Attribute.IsDefined(parameter, typeof(OrderedAttribute), false));
        Defined("parameter defined-instance", () => parameter.IsDefined(typeof(OrderedAttribute), false));
        Defined("parameter absent-defined", () => parameter.IsDefined(typeof(MissingAttribute), false));
    }

    private static void Assembly(Assembly assembly)
    {
        Duplicate("assembly static", () => Attribute.GetCustomAttribute(assembly, typeof(OrderedAttribute)));
        Duplicate("assembly extension", () => assembly.GetCustomAttribute(typeof(OrderedAttribute)));
        Duplicate("assembly generic", () => assembly.GetCustomAttribute<OrderedAttribute>());
        Probe("assembly single-static", () => Attribute.GetCustomAttribute(assembly, typeof(FirstAttribute)), 2);
        Probe("assembly single-generic", () => assembly.GetCustomAttribute<FirstAttribute>(), 2);
        Probe("assembly absent-static", () => Attribute.GetCustomAttribute(assembly, typeof(MissingAttribute)), 1);
        Probe("assembly absent-generic", () => assembly.GetCustomAttribute<MissingAttribute>(), 1);
        Defined("assembly defined-static", () => Attribute.IsDefined(assembly, typeof(OrderedAttribute)));
        Defined("assembly defined-instance", () => assembly.IsDefined(typeof(OrderedAttribute), false));
        Defined("assembly absent-defined", () => assembly.IsDefined(typeof(MissingAttribute), false));
    }

    internal static void Run()
    {
        Console.WriteLine("== single attribute construction order ==");
        Tagged.Method(0);
        Tagged.Property = 7;
        Tagged.Field = Tagged.Property;
        Type type = typeof(Tagged);
        MethodInfo method = type.GetMethod(nameof(Tagged.Method))!;
        Member("type", type);
        Member("method", method);
        Member("field", type.GetField(nameof(Tagged.Field))!);
        Member("property", type.GetProperty(nameof(Tagged.Property))!);
        Parameter(method.GetParameters()[0]);
        Assembly(typeof(Program).Assembly);
        Console.WriteLine("single attribute construction order end");
    }
}
