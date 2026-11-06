using System;

namespace UnsafePtr;

internal static unsafe class FunctionPointerAssemblySubset
{
    private struct LocalResult { }

    internal static void Run()
    {
        Console.WriteLine("== function pointer assembly ==");
        Console.WriteLine("local return=" + (typeof(delegate*<LocalResult>).Assembly == typeof(LocalResult).Assembly));
        Console.WriteLine("core return=" + (typeof(delegate*<LocalResult, int>).Assembly == typeof(int).Assembly));
        Console.WriteLine("pointer return=" + (typeof(delegate*<LocalResult*>).Assembly == typeof(LocalResult).Assembly));
        Console.WriteLine("nested return=" + (typeof(delegate*<delegate*<LocalResult>>).Assembly == typeof(LocalResult).Assembly));
        Console.WriteLine("pointer wrapper=" + (typeof(delegate*<LocalResult>*).Assembly == typeof(LocalResult).Assembly));
        Console.WriteLine("function pointer assembly end");
    }
}
