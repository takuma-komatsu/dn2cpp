#nullable enable
using System;
using System.Threading.Tasks;

namespace AsyncVoidSubset
{
    // async void — the fire-and-forget method shape (UI / event handlers, Godot
    // signal callbacks). Its state machine is driven by AsyncVoidMethodBuilder, which
    // lowers to the same Dn2CppAsyncBuilder over a task as the Task/ValueTask builders;
    // only its ends differ (no observable Task, SetResult takes no value). Covers a
    // synchronously-completing async void (an already-completed await never suspends, so
    // the body runs straight through before the call returns) and a suspending one (await
    // Task.Delay -> AwaitUnsafeOnCompleted boxes the state machine and resumes it on a
    // later clock turn), each recording its effect in a shared log observed after the
    // scheduler has drained.
    //
    // Exactly ONE fire-and-forget chain is in flight at a time and it is drained by a much
    // longer awaited delay before the next write: real .NET resumes async void
    // continuations on the ThreadPool, so two concurrently-suspended async voids appending
    // to one string would race the oracle. A single serial chain, well separated in time,
    // is deterministic on both the ThreadPool and dn2cpp's cooperative virtual clock.
    internal static class Program
    {
        private static string s_log = "";

        private static async void FireSync(int x)
        {
            await Task.CompletedTask;          // already complete -> no suspension
            s_log += "S" + x;
        }

        private static async void FireSuspend(int x)
        {
            await Task.Delay(x);               // suspends; resumes on a later clock turn
            s_log += "D" + x;
            await Task.Delay(x);               // a second suspension in the same chain
            s_log += "E" + x;
        }

        private static async Task Run()
        {
            FireSync(7);                       // runs synchronously to completion here
            s_log += "|";

            FireSuspend(20);                   // single fire-and-forget chain
            await Task.Delay(200);             // drain well past 2*20ms
            s_log += "|done";
        }

        internal static void __GateEntry()
        {
            Run().Wait();
            Console.WriteLine(s_log);          // S7|D20E20|done
        }
    }
}
