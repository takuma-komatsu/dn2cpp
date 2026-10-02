using System;

namespace AttributeTypePropertySubset
{
    // A Type-valued named property reaches its setter as the managed System.Type
    // pointer the setter's C++ signature declares.
    internal sealed class LinkAttribute : Attribute
    {
        public Type Target { get; set; }
    }

    internal sealed class LinkTarget
    {
        public LinkTarget() { }
        public override string ToString() => "constructed from attribute";
    }

    [Link(Target = typeof(LinkTarget))]
    internal sealed class LinkHost { }

    internal static class Program
    {
        public static void Run()
        {
            var link = (LinkAttribute)typeof(LinkHost).GetCustomAttributes(typeof(LinkAttribute), false)[0];
            Console.WriteLine("attribute Type property: " + link.Target.Name);
            Console.WriteLine("attribute construction: " + Activator.CreateInstance(link.Target));
        }
    }
}

namespace AttributeTypePropertySubset
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ChainAttribute : Attribute
    {
        public object Value;

        public ChainAttribute(object value) => Value = value;
    }

    [Chain(typeof(ChainSecond<int>))]
    internal sealed class ChainFirst<T> { }

    [Chain(typeof(ChainThird<int>))]
    internal sealed class ChainSecond<T> { }

    [Chain(typeof(ChainFourth<int>))]
    internal sealed class ChainThird<T> { }

    internal sealed class ChainFourth<T> { }

    [Chain(typeof(ChainFirst<int>))]
    internal sealed class ChainHolder { }

    internal static class ChainedTypes
    {
        private static Type Next(string label, Type type)
        {
            object[] attributes = type.GetCustomAttributes(false);
            Console.WriteLine("chain " + label + "=" + attributes.Length);
            return attributes.Length == 0 ? typeof(object) : (Type)((ChainAttribute)attributes[0]).Value;
        }

        internal static void Run()
        {
            Console.WriteLine("== chained attribute Type roots ==");
            Type first = Next("holder", typeof(ChainHolder));
            GC.Collect();
            Type second = Next("first", first);
            GC.Collect();
            Type third = Next("second", second);
            GC.Collect();
            Type fourth = Next("third", third);
            Console.WriteLine("chain last=" + fourth.Name);
            Console.WriteLine("chained attribute Type roots end");
        }
    }
}
