using System;

namespace AttributeTypePropertySubset
{
    // A Type-valued named property reaches its setter as the managed System.Type
    // pointer the setter's C++ signature declares.
    internal sealed class LinkAttribute : Attribute
    {
        public Type Target { get; set; }
    }

    internal sealed class LinkTarget { }

    [Link(Target = typeof(LinkTarget))]
    internal sealed class LinkHost { }

    internal static class Program
    {
        public static void Run()
        {
            var link = (LinkAttribute)typeof(LinkHost).GetCustomAttributes(typeof(LinkAttribute), false)[0];
            Console.WriteLine("attribute Type property: " + link.Target.Name);
        }
    }
}
