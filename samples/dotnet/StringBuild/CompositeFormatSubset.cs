#nullable disable
using System;
using System.Globalization;
using System.Text;

namespace CompositeFormatSubset
{
    // All five string.Format(CompositeFormat) overloads. dn2cpp reads the format string
    // back off the CompositeFormat and routes to the same runtime formatter, so what is
    // pinned is that a pre-parsed format and the string it came from produce identical
    // text. Two divergences stated at PopCompositeFormatString are deliberately not
    // asserted: a null CompositeFormat raises NullReferenceException rather than
    // ArgumentNullException, and the too-few-arguments FormatException carries a
    // different message (its TYPE does match, and is asserted below).
    internal static class Program
    {
        internal static void Run()
        {
            // The same template parsed once, formatted many ways.
            CompositeFormat three = CompositeFormat.Parse("{0}/{1}/{2}");
            Console.WriteLine(three.Format);
            Console.WriteLine(three.MinimumArgumentCount);

            // --- Format(IFormatProvider, CompositeFormat, params object[]) ---
            // The overload Microsoft.Extensions.Logging.LogValuesFormatter reaches.
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, three,
                new object[] { 1, "two", 3.5 }));
            CompositeFormat wide = CompositeFormat.Parse("[{0,6}][{1,-6}]|{2:X4}|{3:F2}|{{lit}}");
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, wide,
                new object[] { 1, 22, 255, 2.5 }));
            // A repeated and out-of-order index set, plus null holes (empty text).
            CompositeFormat reorder = CompositeFormat.Parse("{2}<{0}<{1}<{0}");
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, reorder,
                new object[] { "a", null, "c" }));

            // --- Format(IFormatProvider, CompositeFormat, params ReadOnlySpan<object>) ---
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, three,
                (ReadOnlySpan<object>)new object[] { 9, 8, 7 }));

            // --- Format<TArg0>, <TArg0,TArg1>, <TArg0,TArg1,TArg2> ---
            // Loose arguments bind to the generic overloads — a MethodSpecification token,
            // a different emit mouth. Each arity carries a value type as well as a
            // reference one, which is what makes the per-argument boxing observable.
            CompositeFormat one = CompositeFormat.Parse("<{0}>");
            CompositeFormat two = CompositeFormat.Parse("{0}={1}");
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, one, 42));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, one, "str"));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, two, "k", 7L));
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, three, 1, 2.5, true));
            // A struct argument with a format specifier, through the generic path.
            CompositeFormat spec = CompositeFormat.Parse("{0:X4}|{1:F3}|{2,8}");
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, spec, 48879, 1.5, 'z'));

            // --- a real, non-invariant culture through the pre-parsed path ---
            // de-DE carries the group separator; fr-FR stays decimal-only on purpose, since
            // pinning its U+202F group separator would assert the culture table rather than
            // CompositeFormat.
            CompositeFormat num = CompositeFormat.Parse("{0:N2} {1:F1}");
            CompositeFormat dec = CompositeFormat.Parse("{0:F2} {1:F1}");
            Console.WriteLine(string.Format(CultureInfo.GetCultureInfo("de-DE"), num, 1234567.891, 2.25));
            Console.WriteLine(string.Format(CultureInfo.GetCultureInfo("fr-FR"), dec, 1234.891, 2.25));

            // --- the pre-parsed and the string form must agree, character for character ---
            const string tmpl = "{0}-{1,4}-{2:F2}";
            CompositeFormat same = CompositeFormat.Parse(tmpl);
            string viaString = string.Format(CultureInfo.InvariantCulture, tmpl,
                new object[] { "x", 5, 6.125 });
            string viaComposite = string.Format(CultureInfo.InvariantCulture, same,
                new object[] { "x", 5, 6.125 });
            Console.WriteLine(viaString);
            Console.WriteLine(viaString == viaComposite);

            // --- too few arguments: the exception TYPE (not its message — see the header) ---
            try
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, three,
                    new object[] { 1 }));
            }
            catch (FormatException)
            {
                Console.WriteLine("caught FormatException (too few args)");
            }
            Console.WriteLine("composite-format section done");
        }
    }
}
