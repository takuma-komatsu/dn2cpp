using System;
using Cysharp.Text;

namespace ZStringFormatSubset;

public class Program
{
    public static void __GateEntry()
    {
        // 1. ZString.Format with various primitives
        string f1 = ZString.Format("int={0}, str={1}, double={2:F2}, bool={3}", 42, "hello", 3.14159, true);
        Console.WriteLine($"[format-basic] {f1}");

        // 2. Numeric formats: Hex, padding, precision
        string f2 = ZString.Format("hex={0:X8}, pad={1:D5}, num={2:N0}", 255, 7, 1000000);
        Console.WriteLine($"[format-numeric] {f2}");

        // 3. Multi-args format (5+ args)
        string f3 = ZString.Format("a={0}, b={1}, c={2}, d={3}, e={4}, f={5}", 1, 2, 3, 4, 5, 6);
        Console.WriteLine($"[format-multi] {f3}");

        // 4. Concat
        string c1 = ZString.Concat("foo", 123, "bar", true, 45.67);
        Console.WriteLine($"[concat] {c1}");

        // 5. Join
        int[] nums = { 10, 20, 30, 40 };
        string j1 = ZString.Join(", ", nums);
        Console.WriteLine($"[join-ints] {j1}");

        string[] words = { "ant", "bat", "cat" };
        string j2 = ZString.Join(" -> ", words);
        Console.WriteLine($"[join-strings] {j2}");

        // 6. DateTime & TimeSpan format
        var dt = new DateTime(2026, 8, 21, 14, 30, 45, DateTimeKind.Utc);
        string fdt = ZString.Format("date={0:yyyy-MM-dd HH:mm:ss}", dt);
        Console.WriteLine($"[format-datetime] {fdt}");

        var ts = new TimeSpan(3, 4, 5, 6);
        string fts = ZString.Format("time={0:c}", ts);
        Console.WriteLine($"[format-timespan] {fts}");
    }
}
