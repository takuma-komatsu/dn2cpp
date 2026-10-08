using System;
using System.Globalization;

// SUBJECT: a reflected abstract slot can first name a closed generic class after
// the attribute walk, without reaching another body or allocating another owner.
// Its attributes still need their constructors. This driver never opens the
// reflection-ctor route, which would reach those constructors independently.
namespace ReflectAttributeDiscoveryOnly;

[AttributeUsage(AttributeTargets.Class)]
sealed class StampAttribute : Attribute
{
    public StampAttribute(int value) => Value = value;

    public int Value { get; }
}

[Stamp(17)]
sealed class ReturnRow<T>
{
}

abstract class AbstractRows
{
    public abstract ReturnRow<int> IntReturn();
}

static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("reflection discovery baseline=23");
        if (args.Length > 0 && args[0] == "before-attribute-discovery")
            return;

        Console.WriteLine("== reflection route final-pass attributes ==");
        if (args.Length > 0)
            typeof(AbstractRows).GetMethod(nameof(AbstractRows.IntReturn))!
                .CreateDelegate(typeof(Func<object>));
        Type returned = typeof(AbstractRows).GetMethod(nameof(AbstractRows.IntReturn))!.ReturnType;
        object[] rows = returned.GetCustomAttributes(typeof(StampAttribute), false);
        Console.WriteLine("discovery return rows=" + rows.Length);
        if (rows.Length != 0)
            Console.WriteLine("discovery return value=" + ((StampAttribute)rows[0]).Value);
        Console.WriteLine("reflection route final-pass attributes end");
    }
}
