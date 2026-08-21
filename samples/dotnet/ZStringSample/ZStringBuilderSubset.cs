using System;
using System.Text;
using Cysharp.Text;

namespace ZStringBuilderSubset;

public class Program
{
    public static void __GateEntry()
    {
        // 1. Utf16ValueStringBuilder
        using (var sb = ZString.CreateStringBuilder())
        {
            sb.Append("Header: ");
            sb.Append(12345);
            sb.Append(", ");
            sb.AppendFormat("formatted={0:X4}", 42);
            sb.AppendLine();
            sb.Append("Footer: ");
            sb.AppendJoin(';', new[] { "A", "B", "C" });

            string result = sb.ToString();
            Console.WriteLine($"[sb-utf16]\n{result}");
        }

        // 2. Utf8ValueStringBuilder
        using (var utf8Sb = ZString.CreateUtf8StringBuilder())
        {
            utf8Sb.Append("UTF8: ");
            utf8Sb.Append(9876);
            utf8Sb.Append(", ");
            utf8Sb.AppendFormat("val={0:F1}", 12.34);
            utf8Sb.AppendLine();
            utf8Sb.Append("items: ");
            utf8Sb.AppendJoin(',', new[] { 1, 2, 3, 4 });

            byte[] bytes = utf8Sb.AsSpan().ToArray();
            string str = Encoding.UTF8.GetString(bytes);
            Console.WriteLine($"[sb-utf8]\n{str}");
        }

        // 3. Nested / multiple builds
        for (int i = 0; i < 3; i++)
        {
            using var sb = ZString.CreateStringBuilder();
            sb.Append("Iter ");
            sb.Append(i);
            sb.Append(": ");
            sb.Append(ZString.Concat("val-", i * 10));
            Console.WriteLine($"[sb-loop] {sb.ToString()}");
        }
    }
}
