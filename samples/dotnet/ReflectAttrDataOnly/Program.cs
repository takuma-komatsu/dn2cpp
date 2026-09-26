using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

// A program that reads its custom attributes only through CustomAttributeData views
// still renders every row. The types are internal, so no public app member roots
// the attribute, and nothing constructs it: the views alone must reach its
// constructor and named setters and declare what its arguments name, here an array
// of a type nothing else names.
namespace ReflectAttrDataOnly
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    internal sealed class MarkAttribute : Attribute
    {
        public MarkAttribute(string label) => Label = label;
        public string Label { get; }
        public int Weight { get; set; }
        public Type Kind;
    }

    // Named by nothing but an attribute blob below.
    internal class Lonely { }

    [Mark("type", Weight = 3)]
    [Mark("array", Kind = typeof(Lonely[]))]
    internal sealed class Holder
    {
        [Mark("method")]
        public void Touch() { }
    }

    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Print("type data", typeof(Holder).GetCustomAttributesData());
            Print("method attributes", typeof(Holder).GetMethod(nameof(Holder.Touch)).CustomAttributes);
        }

        private static void Print(string label, IEnumerable<CustomAttributeData> rows)
        {
            var lines = new List<string>();
            foreach (CustomAttributeData row in rows)
                lines.Add("  " + row.AttributeType.Name + " " + row);
            lines.Sort(StringComparer.Ordinal);
            Console.WriteLine(label + ": " + lines.Count);
            foreach (string line in lines)
                Console.WriteLine(line);
        }
    }
}
