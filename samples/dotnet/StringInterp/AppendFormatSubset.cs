#nullable disable
using System;
using System.Text;

namespace AppendFormatSubset
{
    // StringBuilder.AppendFormat over the same {index[,align][:spec]} grammar as
    // string.Format, appending fluently. Covers the 1-3 object args and object[].
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} and {1}", 1, 2);
            sb.AppendFormat(" [{0:F2}]", 3.14159);
            sb.Append('|');
            sb.AppendFormat("{0,5}", 42).AppendLine();            // fluent chaining
            sb.AppendFormat("{0:X}-{1}-{2}", 255, "z", true);
            sb.AppendLine();
            sb.AppendFormat("{0}-{1}-{2}-{3}", new object[] { 1, 2, 3, 4 });
            Console.WriteLine(sb.ToString());
        }
    }
}
