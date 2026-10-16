using System;

namespace RepeatedAlternatives;

// Each step enumerates both fields of every alternative the previous step resolved, and
// both field types are Node. Admission keeps one Node per step; repeating the alternatives
// instead doubles them at every step.
public sealed class Node
{
    public Node? A;
    public Node? B;
}

public static class Program
{
    public static int Main()
    {
        Type t = typeof(Node);
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        t = t.GetFields()[0].FieldType;
        object? made = Activator.CreateInstance(t);
        Console.WriteLine(made?.GetType().Name);
        return 0;
    }
}
