#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhenAllSubset
{
    // Task.WhenAll<TResult>(Task<TResult>[]) -> Task<TResult[]>. Each input
    // task suspends (Task.Yield) and resumes on the cooperative scheduler; WhenAll
    // joins them by registering a continuation on each, and on the last completion
    // builds the result array. The element kind selects the array representation
    // (int -> int[], reference -> string/object[], long/double -> 8-byte element[]).
    internal static class Program
    {
        private enum ByteEnum : byte { Zero }
        private enum SByteEnum : sbyte { Zero }
        private enum ShortEnum : short { Zero }
        private enum UShortEnum : ushort { Zero }
        private enum IntEnum : int { Zero }
        private enum UIntEnum : uint { Zero }
        private enum LongEnum : long { Zero }
        private enum ULongEnum : ulong { Zero }

        private static async Task<int> Square(int x)
        {
            await Task.Yield();
            return x * x;
        }

        private static async Task<string> Label(int x)
        {
            await Task.Yield();
            return "v" + x;
        }

        private static async Task<long> Big(int x)
        {
            await Task.Yield();
            return (long)x * 1000000000L;
        }

        private static async Task<int> Run()
        {
            int[] squares = await Task.WhenAll(new Task<int>[] { Square(2), Square(3), Square(4) });
            int sumSq = squares[0] + squares[1] + squares[2]; // 4 + 9 + 16 = 29

            string[] labels = await Task.WhenAll(new Task<string>[] { Label(1), Label(2) });
            string joined = labels[0] + labels[1]; // "v1v2"

            long[] bigs = await Task.WhenAll(new Task<long>[] { Big(2), Big(3) });
            long bigSum = bigs[0] + bigs[1]; // 5_000_000_000

            Console.WriteLine(sumSq);    // 29
            Console.WriteLine(joined);   // v1v2
            Console.WriteLine(bigSum);   // 5000000000
            return sumSq;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result); // 29
        }

        private static async Task<T> YieldValue<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        private static bool Matches<T>(T[] values, T first, T second, T third) where T : struct
        {
            return values.GetType() == typeof(T[]) && values.Length == 3
                && values[0].Equals(first) && values[1].Equals(second) && values[2].Equals(third);
        }

        private static T[] EnumResults<T>(string name, T first, T second, T third) where T : struct
        {
            Task<T[]> empty = Task.WhenAll(new Task<T>[0]);
            Task<T[]> settled = Task.WhenAll(new[]
                { Task.FromResult(first), Task.FromResult(second), Task.FromResult(third) });
            T[] yielded = Task.WhenAll(new[]
                { YieldValue(first), YieldValue(second), YieldValue(third) }).Result;
            T[] enumerable = Task.WhenAll((IEnumerable<Task<T>>)new List<Task<T>>
                { Task.FromResult(first), Task.FromResult(second), Task.FromResult(third) }).Result;
            var pendingFirst = new TaskCompletionSource<T>();
            var pendingSecond = new TaskCompletionSource<T>();
            Task<T[]> pending = Task.WhenAll(new[]
                { pendingFirst.Task, pendingSecond.Task, Task.FromResult(third) });
            bool initiallyComplete = pending.IsCompleted;
            pendingSecond.SetResult(second);
            bool partlyComplete = pending.IsCompleted;
            pendingFirst.SetResult(first);
            T[] values = pending.Result;
            bool completed = pending.IsCompleted;
            Console.WriteLine("enum " + name + ": empty=" + empty.IsCompleted + "/"
                + empty.Result.Length + "/" + (empty.Result.GetType() == typeof(T[]))
                + " settled=" + settled.IsCompleted + "/" + Matches(settled.Result, first, second, third)
                + " yielded=" + Matches(yielded, first, second, third)
                + " enumerable=" + Matches(enumerable, first, second, third)
                + " pending=" + initiallyComplete + "/" + partlyComplete + "/" + completed
                + "/" + Matches(values, first, second, third));
            return values;
        }

        internal static void RunEnumResults()
        {
            Console.WriteLine("== enum WhenAll results ==");
            ByteEnum[] bytes = EnumResults("byte", (ByteEnum)255, (ByteEnum)1, (ByteEnum)128);
            Console.WriteLine("enum byte values: " + (byte)bytes[0] + "," + (byte)bytes[1] + "," + (byte)bytes[2]);
            SByteEnum[] sbytes = EnumResults("sbyte", (SByteEnum)(-128), (SByteEnum)127, (SByteEnum)(-1));
            Console.WriteLine("enum sbyte values: " + (sbyte)sbytes[0] + "," + (sbyte)sbytes[1] + "," + (sbyte)sbytes[2]);
            ShortEnum[] shorts = EnumResults("short", (ShortEnum)(-32768), (ShortEnum)32767, (ShortEnum)(-1));
            Console.WriteLine("enum short values: " + (short)shorts[0] + "," + (short)shorts[1] + "," + (short)shorts[2]);
            UShortEnum[] ushorts = EnumResults("ushort", (UShortEnum)65535, (UShortEnum)1, (UShortEnum)32768);
            Console.WriteLine("enum ushort values: " + (ushort)ushorts[0] + "," + (ushort)ushorts[1] + "," + (ushort)ushorts[2]);
            IntEnum[] ints = EnumResults("int", (IntEnum)int.MinValue, (IntEnum)int.MaxValue, (IntEnum)(-1));
            Console.WriteLine("enum int values: " + (int)ints[0] + "," + (int)ints[1] + "," + (int)ints[2]);
            UIntEnum[] uints = EnumResults("uint", (UIntEnum)uint.MaxValue, (UIntEnum)1, (UIntEnum)2147483648U);
            Console.WriteLine("enum uint values: " + (uint)uints[0] + "," + (uint)uints[1] + "," + (uint)uints[2]);
            LongEnum[] longs = EnumResults("long", (LongEnum)long.MinValue, (LongEnum)long.MaxValue, (LongEnum)(-1));
            Console.WriteLine("enum long values: " + (long)longs[0] + "," + (long)longs[1] + "," + (long)longs[2]);
            ULongEnum[] ulongs = EnumResults("ulong", (ULongEnum)ulong.MaxValue, (ULongEnum)1, (ULongEnum)9223372036854775808UL);
            Console.WriteLine("enum ulong values: " + (ulong)ulongs[0] + "," + (ulong)ulongs[1] + "," + (ulong)ulongs[2]);
            Console.WriteLine("enum WhenAll results end");
        }
    }
}
