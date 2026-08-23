using System;

namespace ExceptionMessageSubset
{
    // TypeLoadException.Message calls SetMessageField, whose VM-only formatter is
    // statically reachable even though every public constructor sets _message first.
    internal static class TypeLoadExceptionMessageSubset
    {
        internal static void Run()
        {
            var empty = new TypeLoadException();
            Console.WriteLine("tle default: " + empty.Message);
            Console.WriteLine("tle default name: [" + empty.TypeName + "]");

            var named = new TypeLoadException("load failed");
            Console.WriteLine("tle named: " + named.Message);
            Console.WriteLine("tle named name: [" + named.TypeName + "]");

            var inner = new TypeLoadException("outer", new InvalidOperationException("inner"));
            Console.WriteLine("tle inner: " + inner.Message + " / " + inner.InnerException!.Message);
        }
    }
}
