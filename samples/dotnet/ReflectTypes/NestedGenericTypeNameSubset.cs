using System;

namespace NestedGenericTypeNameSubset;

internal sealed class Box<T>
{
    public void Touch() { }
}

internal sealed class Outer_Box<T>
{
    public void Touch() { }
}

internal sealed class Outer_002BBox<T>
{
    public void Touch() { }
}

internal static class Outer
{
    internal sealed class Box<T>
    {
        public void Touch() { }
    }

    internal struct Point<T>
    {
        public void Touch() { }
    }

    internal static class Middle
    {
        internal sealed class Box<T>
        {
            public void Touch() { }
        }

        internal struct Point<T>
        {
            public void Touch() { }
        }
    }
}

internal static class GenericOuter<TEnclosing>
{
    internal sealed class Box<T>
    {
        public void Touch() { }
    }

    internal struct Point<T>
    {
        public void Touch() { }
    }

    internal sealed class Node
    {
        public void Touch() { }
    }

    internal static class Middle<TMiddle>
    {
        internal sealed class Box<T>
        {
            public void Touch() { }
        }

        internal struct Point<T>
        {
            public void Touch() { }
        }
    }
}

internal static class Program
{
    private static void Show(string label, Type type)
    {
        Console.WriteLine(label + " Name=" + type.Name);
        Console.WriteLine(label + " FullName=" + type.FullName);
        Console.WriteLine(label + " ToString=" + type.ToString());
    }

    private static void Names<T>(string label, T value, Action method)
    {
        Show(label + " typeof", typeof(T));
        Show(label + " instance", value!.GetType());
        Show(label + " delegate", method.Method.DeclaringType!);
    }

    internal static void Run()
    {
        Console.WriteLine("== nested generic type names ==");
        var top = new Box<string>();
        Names("top", top, top.Touch);
        var one = new Outer.Box<string>();
        Names("one class", one, one.Touch);
        var onePoint = new Outer.Point<int>();
        Names("one struct", onePoint, onePoint.Touch);
        var two = new Outer.Middle.Box<int>();
        Names("two class", two, two.Touch);
        var twoPoint = new Outer.Middle.Point<string>();
        Names("two struct", twoPoint, twoPoint.Touch);
        var generic = new GenericOuter<string>.Box<int>();
        Names("enclosing class", generic, generic.Touch);
        var genericPoint = new GenericOuter<int>.Point<string>();
        Names("enclosing struct", genericPoint, genericPoint.Touch);
        var node = new GenericOuter<string>.Node();
        Names("enclosing node", node, node.Touch);
        var deep = new GenericOuter<string>.Middle<int>.Box<long>();
        Names("deep enclosing class", deep, deep.Touch);
        var deepPoint = new GenericOuter<int>.Middle<string>.Point<long>();
        Names("deep enclosing struct", deepPoint, deepPoint.Touch);
        var flat = new Outer_Box<long>();
        Names("flat neighbor", flat, flat.Touch);
        var escape = new Outer_002BBox<byte>();
        Names("escape neighbor", escape, escape.Touch);
        Type nestedDefinition = typeof(Outer.Box<string>).GetGenericTypeDefinition();
        Console.WriteLine("neighbor nested definition=" + nestedDefinition.FullName);
        Console.WriteLine("neighbor flat definition=" + typeof(Outer_Box<>).FullName);
        Console.WriteLine("neighbor definitions distinct=" + (nestedDefinition != typeof(Outer_Box<>)));
        var sharedString = new dn2cpp_nested_neighbors.Closed<string>();
        var sharedObject = new dn2cpp_nested_neighbors.Closed<object>();
        Console.WriteLine("reserved shared=" + sharedString.ArrayName() + "/" + sharedObject.ArrayName());
        Console.WriteLine("reserved closed definition=" + (sharedString.GetType().GetGenericTypeDefinition() == typeof(dn2cpp_nested_neighbors.Closed<>)));
        Console.WriteLine("reserved open-only=" + typeof(dn2cpp_nested_neighbors.Open<>).FullName);
        Console.WriteLine("nested generic type names end");
    }
}
