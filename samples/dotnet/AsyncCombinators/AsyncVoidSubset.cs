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
    // later clock turn), each recording its effect in a shared log. The suspending chain
    // settles a separate promise after its final write, preserving the async-void shape
    // while giving the exact-output oracle an explicit completion to await.
    internal static class Program
    {
        private static string s_log = "";

        private static async void FireSync(int x)
        {
            await Task.CompletedTask;          // already complete -> no suspension
            s_log += "S" + x;
        }

        private static async void FireSuspend(int x, TaskCompletionSource<int> completion)
        {
            await Task.Delay(x);               // suspends; resumes on a later clock turn
            s_log += "D" + x;
            await Task.Delay(x);               // a second suspension in the same chain
            s_log += "E" + x;
            completion.SetResult(x);
        }

        private static async Task Run()
        {
            FireSync(7);                       // runs synchronously to completion here
            s_log += "|";

            var completion = new TaskCompletionSource<int>();
            FireSuspend(20, completion);       // still no Task returned by async void
            await completion.Task;             // observe its final logged effect explicitly
            s_log += "|done";
        }

        internal static void __GateEntry()
        {
            Run().Wait();
            Console.WriteLine(s_log);          // S7|D20E20|done
        }
    }
}
