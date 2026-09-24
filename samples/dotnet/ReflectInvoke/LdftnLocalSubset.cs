using System;
using System.Runtime.CompilerServices;

namespace LdftnLocalSubset
{
    static class Program
    {
        static int Add(int value) => value + 7;
        static int Subtract(int value) => value - 3;
        static string Decorate(string prefix, string value) => prefix + value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> Stored() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> NopSeparated() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> NativeConvert() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> SnapshotBeforeOverwrite() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> Selected(bool first) => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<int, int> StackJoin(bool first) => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Func<string, string> ClosedStored() => throw new InvalidOperationException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int RawCalli() => throw new InvalidOperationException();

        public static void Run()
        {
            Console.WriteLine("ldftn-local-begin");
            var stored = Stored();
            Console.WriteLine("ldftn-local-direct=" + stored(5) + "/" + stored.Method.Name);
            var separated = NopSeparated();
            Console.WriteLine("ldftn-local-nop=" + separated(5) + "/" + separated.Method.Name);
            var converted = NativeConvert();
            Console.WriteLine("ldftn-local-conv=" + converted(5) + "/" + converted.Method.Name);
            var snapshot = SnapshotBeforeOverwrite();
            Console.WriteLine("ldftn-local-snapshot=" + snapshot(5) + "/" + snapshot.Method.Name);
            var first = Selected(true);
            var second = Selected(false);
            Console.WriteLine("ldftn-local-selected=" + first(5) + "/" + first.Method.Name
                + "/" + second(5) + "/" + second.Method.Name);
            var stackFirst = StackJoin(true);
            var stackSecond = StackJoin(false);
            Console.WriteLine("ldftn-local-stack-join=" + stackFirst(5) + "/" + stackFirst.Method.Name
                + "/" + stackSecond(5) + "/" + stackSecond.Method.Name);
            var closed = ClosedStored();
            Console.WriteLine("ldftn-local-closed=" + closed("x") + "/" + closed.Method.Name);
            Console.WriteLine("ldftn-local-calli=" + RawCalli());
            Console.WriteLine("ldftn-local-end");
        }
    }
}
