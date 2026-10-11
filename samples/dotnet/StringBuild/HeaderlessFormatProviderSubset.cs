using System;
using System.Globalization;
using System.Text;

namespace HeaderlessFormatProviderSubset;

internal static class Program
{
    private sealed class ThrowNumberProvider : IFormatProvider
    {
        public object GetFormat(Type type)
        {
            if (type == typeof(NumberFormatInfo))
                throw new InvalidOperationException("number format requested");
            return null;
        }
    }

    internal static void Run()
    {
        Console.WriteLine("== managed format provider consumption ==");
        IFormatProvider provider = new ThrowNumberProvider();
        Console.WriteLine("provider literal=" + string.Format(provider, "literal", Array.Empty<object>()));
        Console.WriteLine("provider text=" + string.Format(provider, "<{0:X}>", (object)"text"));
        Console.WriteLine("provider array=" + string.Format(provider, "{0}/{1}", new object[] { "text", 'x' }));
        Console.WriteLine("provider span=" + string.Format(provider, "{0}/{1}", (ReadOnlySpan<object>)new object[] { "text", 'x' }));
        var composite = CompositeFormat.Parse("<{0:X}>");
        Console.WriteLine("provider composite=" + string.Format(provider, composite, "text"));
        Console.WriteLine("provider composite span=" + string.Format(provider, composite, (ReadOnlySpan<object>)new object[] { "text" }));
        var builder = new StringBuilder();
        builder.AppendFormat(provider, "{0:X}", (object)"text");
        builder.AppendFormat(provider, "/{0}", (ReadOnlySpan<object>)new object[] { 'x' });
        Console.WriteLine("provider builder=" + builder.ToString());
        Console.WriteLine("provider datetime=" + string.Format(provider, "{0:yyyy-MM-dd}", new DateTime(2020, 1, 2)));
        try { string.Format(provider, (string)null, (object)"text"); }
        catch (ArgumentNullException exception) { Console.WriteLine("provider null format=" + exception.ParamName); }
        var culture = new CultureInfo("en-US");
        culture.NumberFormat.NumberDecimalSeparator = "~";
        Console.WriteLine("provider numeric payload=" + string.Format(culture, "{0:F1}/{1:F1}", 1.5, 2.5m));
        Console.WriteLine("managed format provider consumption end");
    }
}
