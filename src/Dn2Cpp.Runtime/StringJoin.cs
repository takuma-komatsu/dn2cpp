using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dn2Cpp.Runtime;

internal static class StringJoin
{
    internal static string Single(string? separator, IEnumerable<float> values)
    {
        using var enumerator = values.GetEnumerator();
        if (!enumerator.MoveNext())
            return string.Empty;
        float first = enumerator.Current;
        if (!enumerator.MoveNext())
            return first.ToString();

        // JoinCore captures the culture after the second MoveNext and before Current.
        // Its NumberFormat can still change while the enumerator advances.
        var provider = CultureInfo.CurrentCulture;
        var result = new StringBuilder(first.ToString(provider));
        do
        {
            result.Append(separator);
            result.Append(enumerator.Current.ToString(provider));
        }
        while (enumerator.MoveNext());
        return result.Length == 0 ? string.Empty : result.ToString();
    }
}
