#nullable disable
using System;
using System.Globalization;
using System.Text;

namespace SpanFormatSubset
{
    // The params ReadOnlySpan<object> overloads Roslyn prefers over the params-array
    // forms (4+ loose Format args, any loose Join/Concat/AppendJoin args, or an explicit
    // span): string.Format, Console.Write/WriteLine, AppendFormat, Join/Concat/AppendJoin.
    internal static class Program
    {
        internal static void Run()
        {
            object a = 1, b = 22, c = 333, d = 4444;

            // --- string.Format(string, params ReadOnlySpan<object>) : 4+ args ---
            Console.WriteLine(string.Format("{0} {1} {2} {3}", a, b, c, d));
            Console.WriteLine(string.Format("{3}<{2}<{1}<{0}", a, b, c, d, (object)5.5));
            // alignment + format-spec holes through the span path
            Console.WriteLine(string.Format("[{0,6}][{1,-6}]|{2:X4}|{3:F2}|", a, b, 255, 2.5, (object)"x"));
            // escaped braces around span-held args
            Console.WriteLine(string.Format("{{{0}}} {1} {2} {3}", a, b, c, d));
            // explicit span argument (below the params threshold)
            Console.WriteLine(string.Format("{0}+{1}", (ReadOnlySpan<object>)new object[] { 7, 8 }));
            // null span elements format as empty
            Console.WriteLine(string.Format("[{0}][{1}][{2}][{3}]", a, null, c, null));

            // --- string.Format(IFormatProvider, string, params ReadOnlySpan<object>) ---
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:N2} {1} {2} {3}", 1234567.891, a, b, c));
            Console.WriteLine(string.Format(CultureInfo.GetCultureInfo("de-DE"), "{0:N2} {1:F1} {2} {3}", 1234567.891, 2.25, c, d));

            // --- Console.Write/WriteLine(string, params ReadOnlySpan<object>) ---
            Console.WriteLine("cw: {0} {1} {2} {3}", a, b, c, d);
            Console.Write("cwr: {0}|{1}|{2}|{3}\n", a, b, c, d);

            // --- StringBuilder.AppendFormat span overloads (+ provider) ---
            var sb = new StringBuilder();
            sb.AppendFormat("{0}.{1}.{2}.{3}", a, b, c, d).Append(' ');
            sb.AppendFormat(CultureInfo.GetCultureInfo("fr-FR"), "{0:F2}/{1}/{2}/{3}", 3.5, b, c, d);
            Console.WriteLine(sb.ToString());

            // --- span Join / Concat / AppendJoin (string and object elements,
            // string and char separators) ---
            Console.WriteLine(string.Join(", ", "x", "y", "z"));
            Console.WriteLine(string.Join(", ", a, b, c));
            Console.WriteLine(string.Join('-', "x", "y", "z"));
            Console.WriteLine(string.Join('|', a, b, c, d));
            Console.WriteLine(string.Join(",", "x", null, "z"));
            Console.WriteLine(string.Concat("p", "q", "r", "s", "t"));
            Console.WriteLine(string.Concat(a, b, c, d, (object)55));
            var aj = new StringBuilder();
            aj.AppendJoin(",", "x", "y", "z").Append(' ');
            aj.AppendJoin('-', a, b, c).Append(' ');
            aj.AppendJoin("+", "s1", "s2");
            Console.WriteLine(aj.ToString());
        }
    }
}
