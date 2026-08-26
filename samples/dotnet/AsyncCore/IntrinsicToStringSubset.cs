#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncIntrinsicToStringSubset
{
    internal static class Program
    {
        private struct Plain { }

        private sealed class Display
        {
            public override string ToString() => "display";
        }

        private static string Generic<T>(T value) => value!.ToString()!;

        internal static void __GateEntry()
        {
            var taskAwaiter = Task.CompletedTask.GetAwaiter();
            var genericTaskAwaiter = Task.FromResult(1).GetAwaiter();
            var valueTaskAwaiter = new ValueTask<int>(2).GetAwaiter();
            var configuredTaskAwaiter = Task.FromResult(3).ConfigureAwait(false).GetAwaiter();
            var configuredValueTaskAwaiter = new ValueTask<int>(4).ConfigureAwait(false).GetAwaiter();
            Console.WriteLine($"awaiter tostring={taskAwaiter}|{genericTaskAwaiter}|{valueTaskAwaiter}");
            Console.WriteLine($"awaiter generic={Generic(genericTaskAwaiter)}|{(object)valueTaskAwaiter}");
            Console.WriteLine($"awaiter configured={configuredTaskAwaiter}|{configuredValueTaskAwaiter}");

            ValueTask<int> value = new(42);
            ValueTask untyped = default;
            ValueTask<short> shortValue = new(-1);
            ValueTask<float> floatValue = new(1.5f);
            ValueTask<object> virtualValue = new(new Display());
            ValueTask<Plain> defaultStruct = default;
            ValueTask<string?> nullValue = new((string?)null);
            var pendingSource = new TaskCompletionSource<int>();
            ValueTask<int> pending = new(pendingSource.Task);
            ValueTask<int> faulted = ValueTask.FromException<int>(new InvalidOperationException("boom"));
            using var canceledSource = new CancellationTokenSource();
            canceledSource.Cancel();
            ValueTask<int> canceled = ValueTask.FromCanceled<int>(canceledSource.Token);

            Console.WriteLine($"valuetask success={value.ToString()}|{(object)value}|{Generic(value)}");
            Console.WriteLine($"valuetask nongeneric={untyped.ToString()}|{(object)untyped}|{untyped}|{Generic(untyped)}");
            Console.WriteLine($"valuetask numeric={shortValue}|{floatValue}");
            Console.WriteLine($"valuetask virtual={virtualValue}");
            Console.WriteLine($"valuetask default struct={defaultStruct}");
            Console.WriteLine($"valuetask null=<{nullValue.ToString()}> pending=<{pending.ToString()}>");
            Console.WriteLine($"valuetask faulted=<{faulted.ToString()}> canceled=<{canceled.ToString()}>");
        }
    }
}
