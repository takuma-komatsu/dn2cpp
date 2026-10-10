#nullable enable
using System;

namespace DelegateCloneFieldTypeSubset;

static class Program
{
    public static Func<int>? Shape;
    public static int Value() => 7;

    internal static void Run()
    {
        var type = typeof(Program).GetField(nameof(Shape))!.FieldType;
        var method = typeof(Program).GetMethod(nameof(Value))!;
        Delegate original = method.CreateDelegate(type);
        Func<object> clone = original.Clone;
        Func<object> alias = ((ICloneable)original).Clone;
        var copy = (Delegate)clone();
        Console.WriteLine("delegate clone reflected field: " + (clone == alias) + "/"
            + (Delegate.Remove(clone, alias) is null) + "/" + !ReferenceEquals(original, copy)
            + "/" + (original.GetType() == copy.GetType()) + "/" + copy.DynamicInvoke());
    }
}
