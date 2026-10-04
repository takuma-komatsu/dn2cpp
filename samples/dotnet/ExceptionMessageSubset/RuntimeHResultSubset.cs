using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ExceptionMessageSubset;

internal static class RuntimeHResultSubset
{
    private sealed class NoDefault
    {
        public NoDefault(int value) { }
    }

    private sealed class CustomCode : Exception
    {
        public CustomCode() { HResult = unchecked((int)0x81234567); }
    }

    private sealed class CustomArgument : ArgumentException { }

    private static object s_sink;

    private static void Show(string label, Exception exception)
    {
        Console.WriteLine(label + ": " + exception.GetType().Name + "/" + exception.HResult.ToString("X8"));
    }

    private static void Fault(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine(label + ": no exception");
        }
        catch (Exception exception)
        {
            GC.Collect();
            Show(label, exception);
        }
    }

    private static void Cast(object value) => s_sink = (string)value;
    private static void Unbox(object value) => s_sink = (int)value;
    private static void Convert(long value) => s_sink = checked((int)value);
    private static void Add(int value) => s_sink = checked(value + 1);
    private static void Divide(int value) => s_sink = 7 / value;
    private static void Remainder(int value) => s_sink = 7 % value;

    internal static void Run()
    {
        Console.WriteLine("== runtime exception HResult ==");
        string text = null;
        Fault("null receiver", () => s_sink = text.Length);
        Fault("throw null", () => { throw (Exception)null; });
        Fault("array index", () => s_sink = (new int[1])[1]);
        Fault("cast", () => Cast(1));
        Fault("unbox", () => Unbox("x"));
        Fault("checked conversion", () => Convert(long.MaxValue));
        Fault("checked addition", () => Add(int.MaxValue));
        Fault("integer division", () => Divide(0));
        Fault("integer remainder", () => Remainder(0));
        Fault("integer format", () => s_sink = int.Parse("x"));
        Fault("parse null", () => s_sink = int.Parse(null));
        Fault("substring range", () => s_sink = "abc".Substring(9));
        var dictionary = new Dictionary<string, int> { ["present"] = 1 };
        Fault("duplicate key", () => dictionary.Add("present", 2));
        Fault("missing key", () => s_sink = dictionary["missing"]);
        Fault("read-only collection", () => ((ICollection<int>)Array.AsReadOnly(new int[1])).Add(1));
        var list = new List<int> { 1 };
        var iterator = list.GetEnumerator();
        list.Add(2);
        Fault("changed collection", () => s_sink = iterator.MoveNext());
        var stream = new MemoryStream();
        stream.Dispose();
        Fault("disposed stream", () => s_sink = stream.ReadByte());
        Fault("missing constructor", () => s_sink = Activator.CreateInstance(typeof(NoDefault)));
        Fault("array rank", () => Array.Reverse((Array)new int[1, 1]));
        Fault("array element type", () => Array.Copy(new int[1], new string[1], 1));

        Show("constructed null", new NullReferenceException());
        Show("constructed index", new IndexOutOfRangeException());
        Show("constructed cast", new InvalidCastException());
        Show("constructed overflow", new OverflowException());
        Show("constructed division", new DivideByZeroException());
        Show("constructed format", new FormatException());
        Show("constructed base", new Exception());
        Show("custom constructor code", new CustomCode());
        Show("custom argument", new CustomArgument());
        Show("aggregate default", new AggregateException(new Exception("inner")));
        var assigned = new Exception { HResult = 123 };
        Fault("assigned thrown code", () => { throw assigned; });
        Fault("explicit HRESULT", () => Marshal.ThrowExceptionForHR(unchecked((int)0x81234567)));
        Console.WriteLine("runtime exception HResult end");
    }
}
