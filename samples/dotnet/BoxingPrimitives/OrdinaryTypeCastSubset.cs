using System;

namespace OrdinaryTypeCastSubset;

internal interface ITypeOwner
{
    Type GetType();
}

internal struct TypeOwner : ITypeOwner
{
    public new Type GetType() => typeof(string);
}

internal sealed class HiddenText
{
    public new string ToString() => "hidden";
}

internal class SlotText
{
    public new virtual string ToString() => "slot";
}

internal sealed class SlotTextLeaf : SlotText
{
    public override string ToString() => "slot-leaf";
}

internal struct Point
{
    public int X;
}

internal static class Program
{
    private static bool IsOf<T>(object o) => o is T;
    private static int? AsNullableInt(object o) => o as int?;
    private static Type TypeOf<T>(T value) => value.GetType();
    private static Type OwnedTypeOf<T>(T value) where T : ITypeOwner => value.GetType();

    private static string CastMessage(Func<object> call)
    {
        try
        {
            return "value " + call();
        }
        catch (InvalidCastException ex)
        {
            return ex.Message;
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== ordinary closed casts and owner checks ==");
        Console.WriteLine("closed casts: " + CastMessage(() => (int?)(object)"s")
            + " | " + CastMessage(() => (int?)(object)DayOfWeek.Friday)
            + " | " + CastMessage(() => (DayOfWeek?)(object)5)
            + " | " + CastMessage(() => (System.Collections.Generic.List<int>)(object)"s"));
        Console.WriteLine("nullable tests: " + IsOf<int?>(5) + " " + IsOf<int?>(DayOfWeek.Friday) + " " + IsOf<int?>(null)
            + " as=" + AsNullableInt(5) + "/" + AsNullableInt(DayOfWeek.Friday).HasValue
            + " assignable=" + typeof(int?).IsAssignableFrom(typeof(int)) + "/" + typeof(int).IsAssignableFrom(typeof(int?))
            + " instance=" + typeof(int?).IsInstanceOfType(5));
        object hidden = new HiddenText(), slot = new SlotTextLeaf();
        Console.WriteLine("object past new slots: " + hidden.ToString() + "/" + slot.ToString()
            + "/" + ((SlotText)slot).ToString());
        Console.WriteLine("GetType owner: " + OwnedTypeOf(new TypeOwner()));
        Point? held = new Point { X = 7 };
        var viaValue = Array.CreateInstance(TypeOf(new Point { X = 8 }), 1);
        var viaNullable = Array.CreateInstance(TypeOf(held), 1);
        Console.WriteLine("GetType Array element: " + (viaValue.GetType().GetElementType() == typeof(Point))
            + "/" + (viaNullable.GetType().GetElementType() == typeof(Point)));
        Console.WriteLine("enum value-type fold: " + typeof(DayOfWeek).IsValueType);
        Console.WriteLine("ordinary closed casts and owner checks end");
    }
}
