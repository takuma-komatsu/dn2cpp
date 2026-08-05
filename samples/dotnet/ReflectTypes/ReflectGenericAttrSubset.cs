#nullable disable
// Generic attributes (C# 11): [Box<int>] where `class BoxAttribute<T> : Attribute`.
// In metadata the CustomAttribute row's ctor is a MemberRef whose parent is a TypeSpec —
// the CLOSED generic BoxAttribute<int> — which is how a closed generic's member is always
// referenced, even inside its own assembly. A reader that only accepts a TypeRef parent
// drops the row outright, and the attribute then does not exist as far as reflection is
// concerned: IsDefined answers False, GetCustomAttributes answers empty. Nothing throws.
//
// The type argument is part of the attribute's IDENTITY: Box<int> and Box<string> are
// different attributes, so the negatives below are as load-bearing as the positives — the
// specialization, not the open template, has to be what lands in the attribute table.
// Covers: IsDefined (positive + two negatives), instance retrieval with ctor arg AND named
// arg, the generic GetCustomAttribute<T>() form closed over a generic attribute, a
// ref-type type argument (the canonical/shared-generics world), and generic attributes on
// a field, a property and a method. Diffed exact vs real .NET.
using System;
using System.Reflection;

namespace ReflectGenericAttrSubset;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class BoxAttribute<T> : Attribute
{
    public BoxAttribute() { }

    public BoxAttribute(int n) { N = n; }

    public int N;

    public string Note { get; set; }

    // Proves the instance really is the CLOSED specialization: the open template has no
    // type argument to report.
    public string ArgName => typeof(T).Name;
}

public sealed class PlainAttribute : Attribute
{
}

[Box<int>(42, Note = "int-box")]
[Box<string>(7, Note = "string-box")]
[Plain]
public class Tagged
{
    [Box<long>(1, Note = "field")]
    public int Field;

    [Box<byte>(2, Note = "prop")]
    public int Prop { get; set; }

    [Box<int>(3, Note = "method")]
    public void Method()
    {
    }
}

public static class Program
{
    private static void Dump(string label, object a)
    {
        if (a is BoxAttribute<int> bi)
            Console.WriteLine(label + " N=" + bi.N + " Note=" + bi.Note + " Arg=" + bi.ArgName);
        else if (a is BoxAttribute<string> bs)
            Console.WriteLine(label + " N=" + bs.N + " Note=" + bs.Note + " Arg=" + bs.ArgName);
        else if (a is BoxAttribute<long> bl)
            Console.WriteLine(label + " N=" + bl.N + " Note=" + bl.Note + " Arg=" + bl.ArgName);
        else if (a is BoxAttribute<byte> bb)
            Console.WriteLine(label + " N=" + bb.N + " Note=" + bb.Note + " Arg=" + bb.ArgName);
        else
            Console.WriteLine(label + " <unexpected " + a.GetType().Name + ">");
    }

    internal static void Run()
    {
        Type t = typeof(Tagged);

        // == IsDefined ==
        // Applied: the two closed forms on the type itself.
        Console.WriteLine("isDefined Box<int>=" + Attribute.IsDefined(t, typeof(BoxAttribute<int>)));
        Console.WriteLine("isDefined Box<string>=" + Attribute.IsDefined(t, typeof(BoxAttribute<string>)));
        // NOT applied to the type — only to a member. A reader that keyed the attribute on
        // the open template instead of the specialization would answer True here.
        Console.WriteLine("isDefined Box<long>=" + Attribute.IsDefined(t, typeof(BoxAttribute<long>)));
        // NOT applied at all, in any instantiation.
        Console.WriteLine("isDefined Box<char>=" + Attribute.IsDefined(t, typeof(BoxAttribute<char>)));
        // A non-generic attribute alongside them still works.
        Console.WriteLine("isDefined Plain=" + Attribute.IsDefined(t, typeof(PlainAttribute)));

        // == instances (ctor arg + named arg) ==
        Attribute[] ints = Attribute.GetCustomAttributes(t, typeof(BoxAttribute<int>));
        Console.WriteLine("count Box<int>=" + ints.Length);
        foreach (Attribute a in ints)
            Dump("type Box<int>", a);

        // A ref-type type argument: the specialization that shared generics canonicalizes.
        Attribute[] strs = Attribute.GetCustomAttributes(t, typeof(BoxAttribute<string>));
        Console.WriteLine("count Box<string>=" + strs.Length);
        foreach (Attribute a in strs)
            Dump("type Box<string>", a);

        // Filtering by a closed form that is not applied yields nothing (not everything).
        Console.WriteLine("count Box<char>="
            + Attribute.GetCustomAttributes(t, typeof(BoxAttribute<char>)).Length);

        // == the generic GetCustomAttribute<T>() form, closed over a generic attribute ==
        BoxAttribute<int> one = t.GetCustomAttribute<BoxAttribute<int>>();
        Console.WriteLine("getCustomAttribute<Box<int>> null=" + (one is null));
        if (one is not null)
            Dump("generic-form", one);

        // == members ==
        FieldInfo f = t.GetField("Field");
        Console.WriteLine("field isDefined Box<long>=" + Attribute.IsDefined(f, typeof(BoxAttribute<long>)));
        Console.WriteLine("field isDefined Box<int>=" + Attribute.IsDefined(f, typeof(BoxAttribute<int>)));
        foreach (Attribute a in Attribute.GetCustomAttributes(f, typeof(BoxAttribute<long>)))
            Dump("field", a);

        PropertyInfo p = t.GetProperty("Prop");
        Console.WriteLine("prop isDefined Box<byte>=" + Attribute.IsDefined(p, typeof(BoxAttribute<byte>)));
        foreach (Attribute a in Attribute.GetCustomAttributes(p, typeof(BoxAttribute<byte>)))
            Dump("prop", a);

        MethodInfo m = t.GetMethod("Method");
        Console.WriteLine("method isDefined Box<int>=" + Attribute.IsDefined(m, typeof(BoxAttribute<int>)));
        foreach (Attribute a in Attribute.GetCustomAttributes(m, typeof(BoxAttribute<int>)))
            Dump("method", a);
    }
}
