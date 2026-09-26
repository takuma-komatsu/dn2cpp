using System;
using System.Globalization;

// A program that never reads its custom attributes still emits every attribute row
// whose constructor another route reached, here a direct construction. Each row
// names the type-info of its Type and array arguments, so the transpile declares
// them all: arrays of a type nothing else names, single-dimensional, jagged and
// multi-dimensional, and an enum array argument.
namespace ReflectAttrUnread
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TagAttribute : Attribute
    {
        public TagAttribute(object value) => Value = value;
        public object Value;
    }

    // Named by nothing but the attribute blobs below.
    public class Lonely { }

    public enum Shade { Dim = 2, Lit = 9 }

    [Tag(typeof(Lonely[]))] [Tag(typeof(Lonely[][]))] [Tag(typeof(Lonely[,]))] [Tag(new[] { Shade.Lit })]
    public sealed class Holder { }

    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var tag = new TagAttribute(5);
            Console.WriteLine("constructed: " + tag.Value);
            Console.WriteLine("holder: " + new Holder().GetType().Name);
        }
    }
}
