using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskRuntimeIdentitySubset
{
    internal static class Program
    {
        private sealed class Marker { }
        private readonly struct Payload { }
        private enum Shade { Dark }

        private static async Task<string> AsyncTaskValue()
        {
            await Task.Yield();
            return "async";
        }

        private static async ValueTask<int> AsyncValueTaskValue()
        {
            await Task.Yield();
            return 8;
        }

        private static async ValueTask<Payload> AsyncValueTaskPayload()
        {
            await Task.Yield();
            return new Payload();
        }

        private static void Check(string label, object value, Type expected, Type wrong)
        {
            Console.WriteLine(label
                + " runtime=" + expected.IsAssignableFrom(value.GetType())
                + " expected=" + expected.IsInstanceOfType(value)
                + " wrong=" + wrong.IsInstanceOfType(value));
        }

        private static bool SharedArg<T>(T value) =>
            typeof(Task<T>).GetGenericArguments()[0] == typeof(T)
            && Task.FromResult(value) is Task<T>;

        internal static void Run()
        {
            Console.WriteLine("== task runtime identity ==");

            object fromResult = Task.FromResult("x");
            Check("from-result", fromResult, typeof(Task<string>), typeof(Task<int>));
            Console.WriteLine("from-result-exact=" + (fromResult.GetType() == typeof(Task<string>)));
            Console.WriteLine("task-metadata base=" + (typeof(Task<string>).BaseType == typeof(Task))
                + " task=" + (fromResult is Task)
                + " assign=" + typeof(Task).IsAssignableFrom(typeof(Task<string>))
                + " subclass=" + typeof(Task<string>).IsSubclassOf(typeof(Task))
                + " generic=" + typeof(Task<string>).IsGenericType
                + " def=" + (typeof(Task<string>).GetGenericTypeDefinition() == typeof(Task<>))
                + " arg=" + (typeof(Task<string>).GetGenericArguments()[0] == typeof(string)));
            try
            {
                _ = (Task<int>)fromResult;
                Console.WriteLine("cast-wrong=no-throw");
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("cast-wrong=InvalidCastException");
            }
            Console.WriteLine("cast-right=" + (((Task<string>)fromResult).Result == "x"));

            var source = new TaskCompletionSource<string>();
            object sourceObject = source;
            object pending = source.Task;
            Check("tcs-source", sourceObject, typeof(TaskCompletionSource<string>), typeof(Task<string>));
            Console.WriteLine("tcs-source-metadata arg="
                + (sourceObject.GetType().GetGenericArguments()[0] == typeof(string))
                + " task=" + (sourceObject is Task));
            Check("tcs-pending", pending, typeof(Task<string>), typeof(Task<int>));
            Console.WriteLine("tcs-exact source="
                + (sourceObject.GetType() == typeof(TaskCompletionSource<string>))
                + " task=" + (pending.GetType() == typeof(Task<string>)));
            source.SetResult("done");
            _ = new TaskCompletionSource<Marker>();
            Console.WriteLine("tcs-unobserved=True");

            Check("async-builder", AsyncTaskValue(), typeof(Task<string>), typeof(Task<int>));
            Check("from-exception", Task.FromException<int>(new Exception("fault")),
                typeof(Task<int>), typeof(Task<string>));
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                Check("from-canceled", Task.FromCanceled<int>(canceled.Token),
                    typeof(Task<int>), typeof(Task<string>));
            }

            Check("run", Task.Run(() => 3), typeof(Task<int>), typeof(Task<string>));
            Check("start-new", Task.Factory.StartNew(() => 4), typeof(Task<int>), typeof(Task<string>));
            Check("cold-ctor", new Task<int>(() => 4), typeof(Task<int>), typeof(Task<string>));
            Check("continue-with", Task.FromResult(1).ContinueWith(_ => "c"),
                typeof(Task<string>), typeof(Task<int>));
            Check("when-all", Task.WhenAll(Task.FromResult(1), Task.FromResult(2)),
                typeof(Task<int[]>), typeof(Task<string[]>));
            Task<Task<int>> whenAny = Task.WhenAny(Task.FromResult(1), Task.FromResult(2));
            Check("when-any", whenAny, typeof(Task<Task<int>>), typeof(Task<Task<string>>));
            Console.WriteLine("when-any-inner-arg="
                + (whenAny.GetType().GetGenericArguments()[0] == typeof(Task<int>)));
            Task<Task> nonGenericWhenAny = Task.WhenAny(new Task[] { Task.CompletedTask });
            Check("when-any-nongeneric", nonGenericWhenAny,
                typeof(Task<Task>), typeof(Task<Task<int>>));

            var waitSource = new TaskCompletionSource<int>();
            Task<int> wait = waitSource.Task.WaitAsync(CancellationToken.None);
            Check("wait-async", wait, typeof(Task<int>), typeof(Task<string>));
            waitSource.SetResult(5);
            Console.WriteLine("wait-result=" + wait.Result);

            Check("valuetask-sync", new ValueTask<int>(7).AsTask(),
                typeof(Task<int>), typeof(Task<string>));
            Check("valuetask-default", default(ValueTask<int>).AsTask(),
                typeof(Task<int>), typeof(Task<string>));
            Check("valuetask-builder", AsyncValueTaskValue().AsTask(),
                typeof(Task<int>), typeof(Task<string>));
            _ = AsyncValueTaskPayload().GetAwaiter().GetResult();
            Console.WriteLine("valuetask-builder-unobserved=True");
            Check("valuetask-from-result", ValueTask.FromResult(10).AsTask(),
                typeof(Task<int>), typeof(Task<string>));
            Check("valuetask-from-exception", ValueTask.FromException<int>(new Exception("vt")).AsTask(),
                typeof(Task<int>), typeof(Task<string>));
            using (var canceled = new CancellationTokenSource())
            {
                canceled.Cancel();
                Check("valuetask-from-canceled", ValueTask.FromCanceled<int>(canceled.Token).AsTask(),
                    typeof(Task<int>), typeof(Task<string>));
            }
            Task<int> carried = Task.FromResult(9);
            Console.WriteLine("valuetask-task-preserve="
                + ReferenceEquals(carried, new ValueTask<int>(carried).AsTask()));

            Check("arg-ref", Task.FromResult(new Marker()), typeof(Task<Marker>), typeof(Task<object>));
            Check("arg-struct", Task.FromResult(new Payload()), typeof(Task<Payload>), typeof(Task<int>));
            Check("arg-enum", Task.FromResult(Shade.Dark), typeof(Task<Shade>), typeof(Task<int>));
            Check("arg-array", Task.FromResult(new int[1]), typeof(Task<int[]>), typeof(Task<string[]>));
            Check("arg-mdarray", Task.FromResult(new int[1, 1]),
                typeof(Task<int[,]>), typeof(Task<int[]>));
            Console.WriteLine("arg-metadata ref="
                + (typeof(Task<Marker>).GetGenericArguments()[0] == typeof(Marker))
                + " struct=" + (typeof(Task<Payload>).GetGenericArguments()[0] == typeof(Payload))
                + " enum=" + (typeof(Task<Shade>).GetGenericArguments()[0] == typeof(Shade))
                + " array=" + (typeof(Task<int[]>).GetGenericArguments()[0] == typeof(int[]))
                + " mdarray=" + (typeof(Task<int[,]>).GetGenericArguments()[0] == typeof(int[,])));
            Console.WriteLine("shared-arg ref=" + SharedArg(new Marker())
                + " nested=" + SharedArg(Task.FromResult(1)));
        }
    }
}
