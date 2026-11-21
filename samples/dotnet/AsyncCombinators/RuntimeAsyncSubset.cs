// Runtime-async methods (MethodImplAttributes.Async): Roslyn emits the body as
// synchronous IL around System.Runtime.CompilerServices.AsyncHelpers calls instead of a
// state machine. Each method here opts in by attribute, so the rest of the bucket keeps
// its compiler state machines. Covers the helpers only user code reaches: Await over
// Task, Task<T>, ValueTask and ValueTask<T>; AwaitAwaiter (INotifyCompletion);
// UnsafeAwaitAwaiter (ICriticalNotifyCompletion and Task.Yield); a value-type instance
// method, which works on a copy of its receiver; a generic method; and suspensions
// inside try/catch/finally with a value live on the evaluation stack.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    // Roslyn matches this attribute by name; the reference assemblies do not carry it.
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeAsyncMethodGenerationAttribute : Attribute
    {
        public RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) { }
    }
}

namespace RuntimeAsyncSubset
{
    internal sealed class Signal
    {
        public Action Continuation;
        public bool Done;
        public int Value;

        public void Fire(int value)
        {
            Value = value;
            Done = true;
            Action continuation = Continuation;
            Continuation = null;
            continuation?.Invoke();
        }
    }

    internal struct NotifyAwaiter : INotifyCompletion
    {
        private readonly Signal _signal;
        public NotifyAwaiter(Signal signal) => _signal = signal;
        public NotifyAwaiter GetAwaiter() => this;
        public bool IsCompleted => _signal.Done;
        public int GetResult() => _signal.Value;

        public void OnCompleted(Action continuation)
        {
            Program.Log("OnCompleted");
            _signal.Continuation = continuation;
        }
    }

    internal sealed class CriticalAwaiter<T> : ICriticalNotifyCompletion
    {
        private readonly Signal _signal;
        private readonly T _result;

        public CriticalAwaiter(Signal signal, T result)
        {
            _signal = signal;
            _result = result;
        }

        public CriticalAwaiter<T> GetAwaiter() => this;
        public bool IsCompleted => _signal.Done;
        public T GetResult() => _signal.Value < 0 ? throw new InvalidOperationException("negative") : _result;
        public void OnCompleted(Action continuation) => throw new NotSupportedException();

        public void UnsafeOnCompleted(Action continuation)
        {
            Program.Log("UnsafeOnCompleted");
            _signal.Continuation = continuation;
        }
    }

    internal sealed class InspectingAwaiter : INotifyCompletion
    {
        public Action Continuation;
        public string Seen = "unset";
        public bool Same;
        public InspectingAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            object target = continuation.Target;
            Seen = (target is not null) + "/" + (target?.GetType() is not null)
                + "/" + (target?.ToString() is not null) + "/" + (target?.GetHashCode() == target?.GetHashCode())
                + "/" + (target is null || target.Equals(target));
            if (Continuation is not null)
                Same = ReferenceEquals(Continuation, continuation);
            Continuation = continuation;
        }
    }

    internal sealed class CountingAwaiter : INotifyCompletion
    {
        public Action Continuation;
        public int Calls;
        public CountingAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public int GetResult() => 1;

        public void OnCompleted(Action continuation)
        {
            Calls++;
            Continuation = continuation;
        }
    }

    // Registration goes through the interface method: the public same-named method is
    // not the implementation.
    internal struct ExplicitAwaiter : INotifyCompletion
    {
        public ExplicitAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public int GetResult() => 3;
        public void OnCompleted(Action continuation) => Program.Log("public");
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            Program.Log("explicit");
            continuation();
        }
    }

    internal class BaseAwaiter : INotifyCompletion
    {
        public BaseAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public int GetResult() => 4;
        public virtual void OnCompleted(Action continuation) => Program.Log("base");
    }

    internal sealed class DerivedAwaiter : BaseAwaiter
    {
        public override void OnCompleted(Action continuation)
        {
            Program.Log("derived");
            continuation();
        }
    }

    // A default interface method is the registration body of a struct awaiter.
    internal interface IDefaultRegistration : INotifyCompletion
    {
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            Program.Log("default");
            continuation();
        }
    }

    internal struct DefaultAwaiter : IDefaultRegistration
    {
        public DefaultAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public int GetResult() => 5;
    }

    // The interface hides OnCompleted; registration still goes through INotifyCompletion.
    internal interface IHidingAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        int GetResult();
        new void OnCompleted(Action continuation);
    }

    internal sealed class HidingAwaiter : IHidingAwaiter
    {
        public bool IsCompleted => false;
        public int GetResult() => 6;
        public void OnCompleted(Action continuation) => Program.Log("hidden");
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            Program.Log("slot");
            continuation();
        }
    }

    internal sealed class HidingAwaitable
    {
        public IHidingAwaiter GetAwaiter() => new HidingAwaiter();
    }

    // A non-sealed static type that only declares the registration method abstractly.
    internal abstract class AbstractAwaiter : INotifyCompletion
    {
        public AbstractAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public int GetResult() => 7;
        public abstract void OnCompleted(Action continuation);
    }

    internal sealed class ConcreteAwaiter : AbstractAwaiter
    {
        public override void OnCompleted(Action continuation)
        {
            Program.Log("concrete");
            continuation();
        }
    }

    // A class between the interface implementer and the sealed awaiter hides the
    // implementing method with a new virtual slot; the interface slot is unchanged.
    internal class SlotBase : INotifyCompletion
    {
        public int GetResult() => 9;
        public virtual void OnCompleted(Action continuation)
        {
            Program.Log("slot-base");
            continuation();
        }
    }

    internal class SlotMiddle : SlotBase
    {
        public new virtual void OnCompleted(Action continuation) => Program.Log("slot-middle");
    }

    internal sealed class SlotLeaf : SlotMiddle
    {
        public SlotLeaf GetAwaiter() => this;
        public bool IsCompleted => false;
    }

    // The awaiter's static type is the completion interface itself.
    internal sealed class CriticalOnly : ICriticalNotifyCompletion
    {
        public void OnCompleted(Action continuation)
        {
            Program.Log("typed-plain");
            continuation();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            Program.Log("typed-unsafe");
            continuation();
        }
    }

    internal struct Counter
    {
        public int N;

        [RuntimeAsyncMethodGeneration(true)]
        public async Task<int> AddAsync(int d)
        {
            N += d;
            await Task.Yield();
            N += d;
            return N;
        }

        // Suspends without reading the receiver.
        [RuntimeAsyncMethodGeneration(true)]
        public async Task<int> Next(int d)
        {
            await Task.Yield();
            return d + 1;
        }

        // Reads the receiver once, before its only suspension.
        [RuntimeAsyncMethodGeneration(true)]
        public async Task<int> ReadThenAdd(int d)
        {
            int n = N;
            await Task.Yield();
            return n + d;
        }
    }

    internal sealed class AddressHolder
    {
        public AddressHolder(object? target, IntPtr address) { }
    }

    internal static class Program
    {
        // A lambda plus a (object, native int) constructor, the delegate-constructor
        // shape, make every 64-bit local carry a method-address tag.
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<long> LongBeforeSuspension(Task<long> pending)
        {
            Func<long, long> widen = x => x * 2;
            _ = new AddressHolder(null, IntPtr.Zero);
            long held = widen(21);
            return held + await pending + held;
        }

        // A byref-like value left on the evaluation stack reads as default after a
        // suspension and is intact when the await completes synchronously.
        private static int SpanLength(ReadOnlySpan<char> s, int v) => s.Length + v;
        private static int IntSpanLength(Span<int> s, int v) => s.Length + v;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> SpanOnStack(Task<int> pending) =>
            SpanLength("abc".AsSpan(), await pending);

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> SpanOnStack(CountingAwaiter pending) =>
            SpanLength("abc".AsSpan(), await pending);

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> SpanOverLocalOnStack(CountingAwaiter pending)
        {
            int local = 1;
            return IntSpanLength(MemoryMarshal.CreateSpan(ref local, 1), await pending);
        }

        // A ref struct with an intrinsic native type reads as default after a suspension.
        private static DefaultInterpolatedStringHandler MakeHandler(string text)
        {
            var handler = new DefaultInterpolatedStringHandler(0, 0);
            handler.AppendLiteral(text);
            return handler;
        }

        private static int HandlerLength(DefaultInterpolatedStringHandler handler, int v) =>
            handler.ToStringAndClear().Length + v;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> HandlerOnStack(CountingAwaiter pending) =>
            HandlerLength(MakeHandler("abc"), await pending);

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> HandlerOnStack(Task<int> pending) =>
            HandlerLength(MakeHandler("abc"), await pending);

        // A runtime handle is program-lifetime data, so it survives a suspension.
        private static bool IsInt(RuntimeTypeHandle handle, int v) =>
            Type.GetTypeFromHandle(handle) == typeof(int) && v == 1;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<bool> HandleOnStack(CountingAwaiter pending) =>
            IsInt(typeof(int).TypeHandle, await pending);

        private static string s_log = "";
        private static string s_nested = "";
        private static int s_runs;
        private static int s_tail;

        internal static void Log(string entry) => s_log += entry + ";";

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitTasks(Task gate, Task<int> value)
        {
            await gate;
            return await value + 1;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async ValueTask<int> AwaitValueTasks(ValueTask gate, ValueTask<int> value)
        {
            await gate;
            return await value * 2;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<T> Echo<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<string> AwaitAwaiters(Signal first, Signal second)
        {
            string result = "";
            try
            {
                result += await new NotifyAwaiter(first);
                result += await new CriticalAwaiter<string>(second, "critical");
            }
            catch (InvalidOperationException e)
            {
                result += "/" + e.Message;
            }
            finally
            {
                result += "/finally";
            }
            return result;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<string> AwaitInspecting(InspectingAwaiter awaiter)
        {
            await awaiter;
            return "inspected";
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitTwice(InspectingAwaiter awaiter)
        {
            await awaiter;
            await awaiter;
            return 2;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitThenSetContext(InspectingAwaiter awaiter, byte[] payload)
        {
            await awaiter;
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            GC.KeepAlive(payload);
            return payload.Length;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> SetContextBeforeSuspension(InspectingAwaiter awaiter)
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            await awaiter;
            return 1;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> SetContextOnly()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            return 1;
        }

        // Invokes its own continuation while resumed, then runs on or suspends again.
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> Reenter(CountingAwaiter first, CountingAwaiter second)
        {
            int v = await first;
            s_runs++;
            if (s_runs == 1)
            {
                try
                {
                    first.Continuation();
                    s_nested = "returned";
                }
                catch (Exception e)
                {
                    s_nested = e.GetType().Name;
                }
            }
            if (second is not null)
                v += await second;
            s_tail++;
            return v;
        }

        // Invoked again while it awaits `pending`, the stored callback waits for that task.
        // The final suspension keeps the method unfinished however the resumptions after
        // `pending` interleave; its registration count shows whether `pending` was passed.
        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitPendingAfterStored(CountingAwaiter first, Task<int> pending, CountingAwaiter last)
        {
            await first;
            int v = await pending;
            await last;
            return v;
        }

        private static string Outcome(Task<int> task, Action invoke)
        {
            string outer;
            try
            {
                invoke();
                outer = "returned";
            }
            catch (Exception e)
            {
                outer = e.GetType().Name + "/" + e.Message;
            }
            return "nested " + s_nested + " runs " + s_runs + " tail " + s_tail + " outer " + outer + " "
                + task.Status + "/" + (task.IsFaulted ? task.Exception.InnerException.GetType().Name : "-");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference StartHolding(InspectingAwaiter awaiter)
        {
            var payload = new byte[4096];
            AwaitThenSetContext(awaiter, payload);
            return new WeakReference(payload);
        }

        private static string Context() => SynchronizationContext.Current is null ? "null" : "set";

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitRegistration(BaseAwaiter derived)
        {
            int explicitResult = await new ExplicitAwaiter();
            return explicitResult + await derived;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitHiding(HidingAwaitable awaitable) => await awaitable;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitDefault() => await new DefaultAwaiter();

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitAbstract(AbstractAwaiter awaiter) => await awaiter;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitSealedHidden(SlotLeaf awaiter) => await awaiter;

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<int> AwaitTyped(CriticalOnly awaiter)
        {
            AsyncHelpers.AwaitAwaiter<INotifyCompletion>(awaiter);
            AsyncHelpers.UnsafeAwaitAwaiter<ICriticalNotifyCompletion>(awaiter);
            return 8;
        }

        private static async Task<int> AwaitShapesMachine(HidingAwaitable hiding, AbstractAwaiter abstractAwaiter)
        {
            int sum = await hiding;
            sum += await new DefaultAwaiter();
            return sum + await abstractAwaiter;
        }

        [RuntimeAsyncMethodGeneration(true)]
        private static async Task<string> InvalidOptions()
        {
            string result = "";
            try
            {
                await Task.CompletedTask.ConfigureAwait((ConfigureAwaitOptions)8);
                result += "no-throw";
            }
            catch (ArgumentOutOfRangeException e)
            {
                result += e.ParamName + "/" + e.Message;
            }
            try
            {
                await Task.FromResult(1).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                result += "|no-throw";
            }
            catch (ArgumentOutOfRangeException e)
            {
                result += "|" + e.ParamName + "/" + e.Message;
            }
            try
            {
                await Task.FromResult(1).ConfigureAwait((ConfigureAwaitOptions)10);
                result += "|no-throw";
            }
            catch (ArgumentOutOfRangeException e)
            {
                result += "|" + e.ParamName + "/" + e.Message;
            }
            try
            {
                await Task.CompletedTask.ConfigureAwait((ConfigureAwaitOptions)(-1));
                result += "|no-throw";
            }
            catch (ArgumentOutOfRangeException e)
            {
                result += "|" + e.ParamName + "/" + e.Message;
            }
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ForceYielding);
            return result + "|valid";
        }

        internal static void Run()
        {
            Console.WriteLine("== runtime async ==");
            var gate = new TaskCompletionSource();
            var value = new TaskCompletionSource<int>();
            Task<int> tasks = AwaitTasks(gate.Task, value.Task);
            ValueTask<int> valueTasks = AwaitValueTasks(new ValueTask(gate.Task), new ValueTask<int>(value.Task));
            Console.WriteLine("pending: " + tasks.IsCompleted + "/" + valueTasks.IsCompleted);
            gate.SetResult();
            value.SetResult(20);
            Task<int> settled = AwaitTasks(Task.CompletedTask, Task.FromResult(1));
            Console.WriteLine("tasks: " + tasks.Result + "/" + valueTasks.AsTask().Result
                + " settled: " + settled.IsCompleted + "/" + settled.Result);

            var counter = new Counter { N = 1 };
            Console.WriteLine("struct: " + counter.AddAsync(2).Result + "/" + counter.N);
            Console.WriteLine("generic: " + Echo(7).Result + "/" + Echo("text").Result + "/" + Echo(long.MaxValue).Result);

            var first = new Signal();
            var second = new Signal();
            Task<string> awaiters = AwaitAwaiters(first, second);
            Console.WriteLine("awaiter pending: " + awaiters.IsCompleted + " " + s_log);
            first.Fire(4);
            Console.WriteLine("awaiter resumed: " + awaiters.IsCompleted + " " + s_log);
            second.Fire(0);
            Console.WriteLine("awaiter result: " + awaiters.Result);
            var negative = new Signal();
            Task<string> caught = AwaitAwaiters(new Signal { Done = true, Value = 5 }, negative);
            negative.Fire(-1);
            Console.WriteLine("awaiter throw: " + caught.Result + " " + s_log);
            var inspecting = new InspectingAwaiter();
            Task<string> inspected = AwaitInspecting(inspecting);
            Console.WriteLine("continuation target: " + inspecting.Seen);
            inspecting.Continuation();
            Console.WriteLine("continuation resumed: " + inspected.Result);
            Console.WriteLine("invalid options: " + InvalidOptions().Result);
            s_log = "";
            Task<int> registered = AwaitRegistration(new DerivedAwaiter());
            Console.WriteLine("registration: " + registered.IsCompleted + " " + s_log);
            s_log = "";
            Task<int> hidden = AwaitHiding(new HidingAwaitable());
            Console.WriteLine("registration interface: " + hidden.IsCompleted + " " + s_log);
            s_log = "";
            Task<int> defaulted = AwaitDefault();
            Console.WriteLine("registration default: " + defaulted.IsCompleted + " " + s_log);
            s_log = "";
            Task<int> abstracted = AwaitAbstract(new ConcreteAwaiter());
            Console.WriteLine("registration abstract: " + abstracted.IsCompleted + " " + s_log);
            s_log = "";
            Task<int> machine = AwaitShapesMachine(new HidingAwaitable(), new ConcreteAwaiter());
            Console.WriteLine("registration machine: " + machine.IsCompleted + " " + machine.Result + " " + s_log);
            s_log = "";
            Task<int> sealedHidden = AwaitSealedHidden(new SlotLeaf());
            Console.WriteLine("registration sealed hidden: " + sealedHidden.IsCompleted + " " + s_log);
            s_log = "";
            Task<int> typed = AwaitTyped(new CriticalOnly());
            Console.WriteLine("registration typed: " + typed.IsCompleted + " " + typed.Result + " " + s_log);
            var twice = new InspectingAwaiter();
            Task<int> twiceTask = AwaitTwice(twice);
            twice.Continuation();
            Console.WriteLine("continuation identity: " + twice.Same + " " + twiceTask.IsCompleted);
            twice.Continuation();
            Console.WriteLine("continuation settled: " + twiceTask.Result);
            var holding = new InspectingAwaiter();
            WeakReference payload = StartHolding(holding);
            Action retained = holding.Continuation;
            retained();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("completed frame: " + payload.IsAlive + " " + Context());
            SynchronizationContext.SetSynchronizationContext(null);
            string spent = "none";
            try
            {
                retained();
            }
            catch (InvalidOperationException)
            {
                spent = "InvalidOperationException";
            }
            Console.WriteLine("continuation spent: " + spent);
            SetContextOnly();
            string synchronous = Context();
            SynchronizationContext.SetSynchronizationContext(null);
            var early = new InspectingAwaiter();
            SetContextBeforeSuspension(early);
            string suspended = Context();
            SynchronizationContext.SetSynchronizationContext(null);
            early.Continuation();
            Console.WriteLine("context restored: " + synchronous + " " + suspended + " " + Context());
            var reentered = new CountingAwaiter();
            Task<int> reenteredTask = Reenter(reentered, null);
            Console.WriteLine("continuation re-entered: " + Outcome(reenteredTask, reentered.Continuation));
            s_runs = 0;
            s_tail = 0;
            var again = new CountingAwaiter();
            var next = new CountingAwaiter();
            Task<int> againTask = Reenter(again, next);
            string registeredAgain = Outcome(againTask, again.Continuation);
            Console.WriteLine("continuation re-entered then suspended: " + registeredAgain
                + " calls " + next.Calls + " registered " + (next.Continuation is not null));
            var receiver = new Counter { N = 5 };
            Console.WriteLine("struct receiver: " + receiver.Next(1).Result + "/" + receiver.ReadThenAdd(2).Result);
            var longGate = new TaskCompletionSource<long>();
            Task<long> longSum = LongBeforeSuspension(longGate.Task);
            longGate.SetResult(100);
            Console.WriteLine("long across suspension: " + longSum.Result);
            var spanGate = new CountingAwaiter();
            Task<int> spanTask = SpanOnStack(spanGate);
            spanGate.Continuation();
            var overLocalGate = new CountingAwaiter();
            Task<int> overLocalTask = SpanOverLocalOnStack(overLocalGate);
            overLocalGate.Continuation();
            Console.WriteLine("byref-like on stack: " + spanTask.Result + " " + overLocalTask.Result
                + " " + SpanOnStack(Task.FromResult(1)).Result);
            var handlerGate = new CountingAwaiter();
            Task<int> handlerTask = HandlerOnStack(handlerGate);
            handlerGate.Continuation();
            Console.WriteLine("handler on stack: " + handlerTask.Result + " " + HandlerOnStack(Task.FromResult(1)).Result);
            var handleGate = new CountingAwaiter();
            Task<bool> handleTask = HandleOnStack(handleGate);
            handleGate.Continuation();
            Console.WriteLine("handle across suspension: " + handleTask.Result);
            var storedGate = new CountingAwaiter();
            var storedSource = new TaskCompletionSource<int>();
            var storedLast = new CountingAwaiter();
            Task<int> storedWaiting = AwaitPendingAfterStored(storedGate, storedSource.Task, storedLast);
            storedGate.Continuation();
            // A live thread other than the blocked one keeps the wait satisfiable.
            var storedRelease = new ManualResetEventSlim();
            var storedKeeper = new Thread(() => storedRelease.Wait());
            storedKeeper.Start();
            var storedInvoker = new Thread(() =>
            {
                try { storedGate.Continuation(); } catch (Exception) { }
            });
            storedInvoker.Start();
            Thread.Sleep(200);
            Console.WriteLine("callback while awaiting pending: " + storedWaiting.IsCompleted + " " + storedLast.Calls);
            try { storedSource.SetResult(1); } catch (Exception) { }
            storedInvoker.Join();
            storedRelease.Set();
            storedKeeper.Join();
            Console.WriteLine("runtime async end");
        }
    }
}
