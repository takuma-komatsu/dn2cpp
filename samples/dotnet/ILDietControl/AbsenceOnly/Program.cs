using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace ILDietAbsenceOnly;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("absence-only prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RETAINED_ABSENCE") == "1")
            return;
        Console.WriteLine("== unrequested bodies stay absent ==");
        MethodInfo looked = typeof(LookupTarget).GetMethod("LookedUp")!;
        FieldInfo lookedField = typeof(LookupTarget).GetField("LookedUpField")!;
        Console.WriteLine("lookup=" + looked.Name + ":" + looked.ReturnType.Name + "/"
            + lookedField.Name + ":" + lookedField.FieldType.Name);
        Console.WriteLine("called=" + LookupTarget.Called());
        MethodInfo pick = typeof(OverloadTarget).GetMethod("Pick", new[] { typeof(int) })!;
        Console.WriteLine("overload=" + pick.Invoke(null, new object[] { 7 }));
        var owner = new FieldOwner();
        Console.WriteLine("field=" + typeof(FieldOwner).GetField("Selected")!.GetValue(owner));
        IUsed used = new OrdinaryUser();
        Console.WriteLine("used=" + used.Use());
        Console.WriteLine("unused-type=" + typeof(UnusedStatics).Name);
        Console.WriteLine("used-static=" + UsedStatics.Value);
        Console.WriteLine("unrequested bodies stay absent end");
        Console.WriteLine("== joined names admit only their known alternatives ==");
        Console.WriteLine("joined=" + RunJoined());
        Console.WriteLine("joined names end");
        Console.WriteLine("== attribute metadata reads construct no attribute ==");
        Console.WriteLine("defined=" + typeof(MetadataOwner).IsDefined(typeof(DefinedOnlyAttribute), false));
        foreach (CustomAttributeData data in typeof(MetadataOwner).GetCustomAttributesData())
            Console.WriteLine("data=" + data);
        Console.WriteLine("filtered=" + typeof(FilteredOwner).GetCustomAttribute<RequestedAttribute>()!.Text);
        Console.WriteLine("attribute metadata reads end");
        Console.WriteLine("== formed generic types construct nothing ==");
        Console.WriteLine("formed=" + typeof(FormedOnly<>).MakeGenericType(typeof(int)).Name);
        Console.WriteLine("constructed=" + Activator.CreateInstance(typeof(ConstructedTemplate<>).MakeGenericType(typeof(int))));
        Console.WriteLine("formed generic types end");
        Console.WriteLine("== framework attribute metadata reads construct no attribute ==");
        Console.WriteLine("framework-defined="
            + typeof(FrameworkMetadataOwner).IsDefined(typeof(DebuggerDisplayAttribute), false));
        foreach (CustomAttributeData data in typeof(FrameworkMetadataOwner).GetCustomAttributesData())
            Console.WriteLine("framework-data=" + data);
        Console.WriteLine("framework-instance=" + ((DefaultMemberAttribute)typeof(FrameworkInstanceOwner)
            .GetCustomAttribute(typeof(DefaultMemberAttribute))!).MemberName);
        Console.WriteLine("framework attribute metadata reads end");
    }

    // The name joins a constant with a value no summary resolves: the constant's
    // target runs, the site still warns for the other alternative, and no sibling
    // body is compiled on its behalf.
    private static string RunJoined()
    {
        string name = Environment.GetEnvironmentVariable("DN2CPP_JOINED_TARGET_NAME") ?? "Known";
        return (string)typeof(JoinedTarget).GetMethod(name)!.Invoke(null, null)!;
    }
}

// IsDefined and CustomAttributeData read rows, never instances, so neither attribute
// on MetadataOwner is constructed; the typed read on FilteredOwner constructs only the
// requested attribute.
[AttributeUsage(AttributeTargets.All)]
internal sealed class DefinedOnlyAttribute : Attribute
{
    public DefinedOnlyAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class DataOnlyAttribute : Attribute
{
    public DataOnlyAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class RequestedAttribute : Attribute
{
    public RequestedAttribute(string text) => Text = text;

    public string Text { get; }
}

[AttributeUsage(AttributeTargets.All)]
internal sealed class UnrequestedAttribute : Attribute
{
    public UnrequestedAttribute(string text) => Text = text;

    public string Text { get; }
}

[DefinedOnly("defined")]
[DataOnly("data")]
internal sealed class MetadataOwner
{
}

[Requested("requested")]
[Unrequested("unrequested")]
internal sealed class FilteredOwner
{
}

// A framework attribute's row answers a metadata read without its constructor; the
// typed instance read on FrameworkInstanceOwner constructs its attribute.
[DebuggerDisplay("metadata-only")]
internal sealed class FrameworkMetadataOwner
{
}

[DefaultMember("Selected")]
internal sealed class FrameworkInstanceOwner
{
}

// MakeGenericType forms FormedOnly<int> for its name alone: neither the constructor nor
// the initializer it touches is compiled. ConstructedTemplate<int> is constructed, which
// compiles its constructor and that initializer but not the method nothing invokes.
internal sealed class FormedOnly<T>
{
    public readonly string Text = FormedOnlyHelper.Value;

    public string Unasked() => "unasked:" + typeof(T).Name;
}

internal static class FormedOnlyHelper
{
    public static readonly string Value = Environment.GetEnvironmentVariable("DN2CPP_FORMED_ONLY") ?? "formed-only";
}

internal sealed class ConstructedTemplate<T>
{
    public readonly string Text = ConstructedTemplateHelper.Value;

    public string Unasked() => "unasked:" + typeof(T).Name;

    public override string ToString() => Text + ":" + typeof(T).Name;
}

internal static class ConstructedTemplateHelper
{
    public static readonly string Value = Environment.GetEnvironmentVariable("DN2CPP_CONSTRUCTED_TEMPLATE") ?? "constructed";
}

internal static class JoinedTarget
{
    public static string Known() => "known";

    public static string Unnamed() => "unnamed";
}

internal sealed class LookupTarget
{
    public LookupFieldValue LookedUpField;

    public static string LookedUp() => LookupBodyOnly.Read();

    public static string Called() => "called-body";
}

internal static class LookupBodyOnly
{
    public static string Read() => "lookup-body";
}

internal struct LookupFieldValue
{
    public int Amount;

    public override string ToString() => "lookup-field:" + Amount;
}

internal static class OverloadTarget
{
    public static string Pick(int value) => "int:" + value;

    public static string Pick(string value) => "string:" + value;
}

internal sealed class FieldOwner
{
    public SelectedValue Selected = new SelectedValue(5);
    public UnrelatedValue Other;
    public UnrelatedPayload? Payload;
}

internal struct SelectedValue
{
    public int Amount;

    public SelectedValue(int amount) => Amount = amount;

    public override string ToString() => "selected:" + Amount;
}

internal struct UnrelatedValue
{
    public int Amount;

    public override string ToString() => "unrelated:" + Amount;
}

internal sealed class UnrelatedPayload
{
    public int Amount;

    public UnrelatedPayload() => Amount = 3;

    public override string ToString() => "payload:" + Amount;
}

internal interface IUsed
{
    string Use();
}

internal interface IUnused
{
    string Run();
}

internal sealed class OrdinaryUser : IUsed
{
    public string Use() => "ordinary";
}

// keep-preserved-constructor.xml preserves only the constructor.
internal sealed class PreservedService : IUsed, IUnused
{
    public int State;

    public PreservedService() => State = 1;

    public string Use() => "preserved-use";

    public string Run() => "preserved-run";
}

internal static class UnusedStatics
{
    public static readonly string Value = Environment.GetEnvironmentVariable("DN2CPP_UNUSED_STATICS") ?? "unused";
}

internal static class UsedStatics
{
    public static readonly string Value = Environment.GetEnvironmentVariable("DN2CPP_USED_STATICS") ?? "used";
}
