#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace ValueTaskSourceStructSubset
{
    internal readonly struct Pair
    {
        internal Pair(int number, string text)
        {
            Number = number;
            Text = text;
        }

        internal int Number { get; }
        internal string Text { get; }
    }

    // A completed IValueTaskSource<TStruct> takes the same bridge used by
    // PipeReader.ReadAsync (ReadResult) and PipeWriter.FlushAsync (FlushResult).
    // The result carries both a scalar and a managed reference so copying only the
    // aggregate's first word, or placing it in unscanned storage, is observable.
    internal sealed class PairSource : IValueTaskSource<Pair>
    {
        private Pair _result;
        private bool _completed;

        internal static int LastGetResultToken { get; private set; }
        internal static int LastOnCompletedToken { get; private set; }

        internal PairSource(Pair result)
        {
            _result = result;
        }

        public Pair GetResult(short token)
        {
            LastGetResultToken = token;
            Pair result = _result;
            _result = default;
            return result;
        }

        public ValueTaskSourceStatus GetStatus(short token) =>
            _completed ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Pending;

        public void OnCompleted(Action<object?> continuation, object? state, short token,
            ValueTaskSourceOnCompletedFlags flags)
        {
            LastOnCompletedToken = token;
            _completed = true;
            continuation(state);
            // GetResult cleared the source before the bridge boxed its return. Collect
            // before the ValueTask ctor returns, when the box is the only intentional
            // owner of the dynamically allocated string.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    internal static class Program
    {
        private const short Version = unchecked((short)0x8123);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> Read<T>(IValueTaskSource<T> source) =>
            await new ValueTask<T>(source, Version);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Pair ReadPair()
        {
            string text = new string(new[] { 's', 't', 'r', 'u', 'c', 't', '-', 'v', 't', 's' });
            PairSource source = new PairSource(new Pair(17, text));
            text = string.Empty;
            return Read<Pair>(source).Result;
        }

        internal static void __GateEntry()
        {
            Pair result = ReadPair();
            GC.Collect();
            Console.WriteLine($"value-task-source struct: {result.Number},{result.Text}");
            Console.WriteLine("value-task-source tokens: "
                + $"{PairSource.LastOnCompletedToken},{PairSource.LastGetResultToken}");
        }
    }
}
