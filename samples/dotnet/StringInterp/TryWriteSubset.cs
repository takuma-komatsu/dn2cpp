using System;
using System.Globalization;

namespace TryWriteSubset;

internal static class Program
{
    private sealed class Provider : IFormatProvider, ICustomFormatter
    {
        internal int Mode;
        internal int Calls;
        internal string Events = "";
        public object GetFormat(Type type)
        {
            Calls++;
            Events += type == typeof(ICustomFormatter) ? "G" : "?";
            if (Mode == 3 || Mode == 6 && Calls > 1) throw new FormatException("lookup");
            if (Mode == 0 || Mode == 4 && Calls > 1) return null;
            if (Mode == 2 || Mode == 5 && Calls > 1 || Mode == 7 && Calls == 1) return new object();
            return this;
        }
        public string Format(string format, object arg, IFormatProvider provider)
        {
            Events += ReferenceEquals(provider, this) && arg is int && (int)arg == 7 && format == "X" ? "F" : "!";
            if (Mode == 9) throw new ArgumentException("format");
            return Mode == 8 ? null : "custom";
        }
    }
    private sealed class DerivedCulture : CultureInfo
    {
        internal int Calls;
        internal DerivedCulture() : base("") { }
        public override object GetFormat(Type type) { Calls++; return null; }
    }
    internal static void __LiteralGateEntry()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== TryWrite literal formatter detection ==");
        for (int mode = 0; mode != 4; mode++)
            foreach (int capacity in new[] { 0, 6, 7, 8 })
            {
                var provider = new Provider { Mode = mode };
                Span<char> output = new char[capacity];
                try
                {
                    bool success = output.TryWrite(provider, $"literal", out int written);
                    Console.WriteLine("literal:" + mode + ":" + capacity + ":" + success + ":" + written + ":" + new string(output.Slice(0, written)) + ":" + provider.Events);
                }
                catch (Exception error) { Console.WriteLine("literal:" + mode + ":" + capacity + ":" + error.GetType().Name + ":" + provider.Events); }
            }
        foreach (IFormatProvider provider in new IFormatProvider[] { null, CultureInfo.InvariantCulture })
            foreach (int capacity in new[] { 0, 7 })
            {
                Span<char> output = new char[capacity];
                bool success = output.TryWrite(provider, $"literal", out int written);
                Console.WriteLine("literal-builtin:" + (provider is null ? "null" : "culture") + ":" + capacity + ":" + success + ":" + written);
            }
        Console.WriteLine("TryWrite literal formatter detection end");
    }
    internal static void __TextGateEntry()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== TryWrite text formatter boundary ==");
        for (int mode = 0; mode != 10; mode++)
            foreach (int capacity in new[] { 0, 1, 8, 32 })
            {
                var provider = new Provider { Mode = mode };
                Span<char> output = new char[capacity];
                try
                {
                    bool success = output.TryWrite(provider, $"[{"s"}]", out int written);
                    Console.WriteLine("text:" + mode + ":" + capacity + ":" + success + ":" + written + ":" + new string(output.Slice(0, written)) + ":" + provider.Events);
                }
                catch (Exception error) { Console.WriteLine("text:" + mode + ":" + capacity + ":" + error.GetType().Name + ":" + provider.Events); }
            }
        Span<char> buffer = new char[64];
        var multi = new Provider { Mode = 1 };
        bool mixed = buffer.TryWrite(multi, $"{"s"}|{"s",8}|{"ab".AsSpan()}", out int count);
        Console.WriteLine("text-mixed:" + mixed + ":" + new string(buffer.Slice(0, count)) + ":" + multi.Events);
        var wrong = new Provider { Mode = 2 };
        bool literal = buffer.TryWrite(wrong, $"literal", out count);
        Console.WriteLine("text-noholes:" + literal + ":" + count + ":" + wrong.Events);
        Console.WriteLine("TryWrite text formatter boundary end");
    }
    internal static void __GateEntry()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== TryWrite custom formatter boundary ==");
        for (int mode = 0; mode != 10; mode++)
            foreach (int capacity in new[] { 0, 1, 8, 32 })
            {
                var provider = new Provider { Mode = mode };
                Span<char> output = new char[capacity];
                try
                {
                    bool success = output.TryWrite(provider, $"[{7:X}]", out int written);
                    Console.WriteLine("trywrite:" + mode + ":" + capacity + ":" + success + ":" + written + ":" + new string(output.Slice(0, written)) + ":" + provider.Events);
                }
                catch (Exception error) { Console.WriteLine("trywrite:" + mode + ":" + capacity + ":" + error.GetType().Name + ":" + provider.Events); }
            }
        Span<char> buffer = new char[64];
        var multi = new Provider { Mode = 1 };
        bool mixed = buffer.TryWrite(multi, $"{7:X}|{7,8:X}|{(object)7:X}|{"s"}|{"ab".AsSpan()}", out int mixedCount);
        Console.WriteLine("mixed:" + mixed + ":" + new string(buffer.Slice(0, mixedCount)) + ":" + multi.Events);
        var wrong = new Provider { Mode = 2 };
        bool literal = buffer.TryWrite(wrong, $"literal", out int count);
        Console.WriteLine("noholes:" + literal + ":" + count + ":" + wrong.Events);
        foreach (IFormatProvider provider in new IFormatProvider[] { null, CultureInfo.InvariantCulture })
        {
            bool success = buffer.TryWrite(provider, $"{7}:{"s",3}:{(object)8}:{"ab".AsSpan()}", out count);
            Console.WriteLine("ordinary:" + success + ":" + new string(buffer.Slice(0, count)) + ":" + (provider is DerivedCulture derived ? derived.Calls : 0));
        }
        Console.WriteLine("TryWrite custom formatter boundary end");
    }
}
