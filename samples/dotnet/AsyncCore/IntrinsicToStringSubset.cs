#nullable enable
using System;
using System.Threading.Tasks;

namespace AsyncIntrinsicToStringSubset
{
    internal static class Program
    {
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
        }
    }
}
