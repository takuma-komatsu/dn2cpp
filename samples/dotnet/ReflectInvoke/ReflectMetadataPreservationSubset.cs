using System;
using System.Reflection;

namespace ReflectMetadataPreservationSubset;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
sealed class RecordAttribute : Attribute
{
    public readonly string Text;
    public readonly long Number;

    public RecordAttribute(string text, long number)
    {
        Text = text;
        Number = number;
    }
}

enum SignedBoundary : long
{
    Minimum = long.MinValue,
    Negative = -1,
    Maximum = long.MaxValue,
}

enum UnsignedBoundary : ulong
{
    Maximum = ulong.MaxValue,
}

[Record(null, long.MinValue)]
[Record("", -1)]
[Record("共有接尾辞_Ω_𐐀", long.MaxValue)]
class Subject
{
    public string 長い識別子を保持するための名前_共有接尾辞;
    public string 共有接尾辞;
    public int Value { get; init; }
    public string Read(ref int index, string[] values) => values[index++];
}

sealed class Derived : Subject { }

static class Program
{
    internal static void Run()
    {
        Console.WriteLine("metadata-preservation-begin");
        Type type = typeof(Subject);
        FieldInfo longName = type.GetFields()[0];
        FieldInfo suffix = type.GetFields()[1];
        Console.WriteLine("metadata-long-name=" + longName.Name);
        Console.WriteLine("metadata-suffix-name=" + suffix.Name);
        Console.WriteLine("metadata-distinct-rows=" + (longName.MetadataToken != suffix.MetadataToken));
        Console.WriteLine("metadata-inherited-token=" + (typeof(Derived).GetFields()[1].MetadataToken == suffix.MetadataToken));
        Console.WriteLine("metadata-inherited-identity=" + ReferenceEquals(typeof(Derived).GetFields()[1], suffix));
        Console.WriteLine("metadata-repeat-identity=" + ReferenceEquals(type.GetFields()[1], suffix));

        MethodInfo method = type.GetMethod("Read");
        ParameterInfo[] parameters = method.GetParameters();
        Console.WriteLine("metadata-display=" + method);
        Console.WriteLine("metadata-parameter-display=" + parameters[0]);
        Console.WriteLine("metadata-parameter-owner=" + ReferenceEquals(parameters[0].Member, method));
        Console.WriteLine("metadata-empty-modifiers=" + parameters[0].GetRequiredCustomModifiers().Length + "/" + parameters[0].GetOptionalCustomModifiers().Length);
        Type[] modifiers = type.GetProperty("Value").GetSetMethod().ReturnParameter.GetRequiredCustomModifiers();
        Console.WriteLine("metadata-required-modifier=" + modifiers.Length + "/" + modifiers[0].FullName);

        object[] first = type.GetCustomAttributes(typeof(RecordAttribute), false);
        object[] second = type.GetCustomAttributes(typeof(RecordAttribute), false);
        Console.WriteLine("metadata-attribute-count=" + first.Length);
        for (int i = 0; i < first.Length; i++)
        {
            RecordAttribute attribute = (RecordAttribute)first[i];
            Console.WriteLine("metadata-attribute=" + (attribute.Text is null ? "<null>" : "[" + attribute.Text + "]") + "/" + attribute.Number);
            Console.WriteLine("metadata-attribute-fresh=" + !ReferenceEquals(first[i], second[i]));
        }

        Console.WriteLine("metadata-enum-min=" + (long)(SignedBoundary)typeof(SignedBoundary).GetField("Minimum").GetValue(null));
        Console.WriteLine("metadata-enum-negative=" + (long)(SignedBoundary)typeof(SignedBoundary).GetField("Negative").GetValue(null));
        Console.WriteLine("metadata-enum-max=" + (long)(SignedBoundary)typeof(SignedBoundary).GetField("Maximum").GetValue(null));
        Console.WriteLine("metadata-enum-unsigned=" + (ulong)(UnsignedBoundary)typeof(UnsignedBoundary).GetField("Maximum").GetValue(null));
        Console.WriteLine("metadata-preservation-end");
    }
}
