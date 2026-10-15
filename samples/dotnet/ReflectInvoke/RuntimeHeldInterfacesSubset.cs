#nullable enable
#pragma warning disable SYSLIB0050, SYSLIB0051
using System;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if RUNTIME_HELD_ASSEMBLY_ATTRIBUTES
[assembly: ReflectInvoke.RuntimeHeldInterfacesSubset.Program.MarkerAttribute]
[module: ReflectInvoke.RuntimeHeldInterfacesSubset.Program.MarkerAttribute]
#endif

namespace ReflectInvoke;

internal static class RuntimeHeldInterfacesSubset
{
    internal static class Program
    {
        [AttributeUsage(AttributeTargets.All)]
        internal sealed class MarkerAttribute : Attribute { }

        [Marker]
        private sealed class Subject
        {
            [Marker] public int Number = 7;
            [Marker] public int Value { get; set; } = 11;
            [Marker] public Subject() { }
            [Marker] public int Echo([Marker] int value) => value + Number;
            [Marker] public event Action Changed { add { } remove { } }
        }

        private sealed class UserException : Exception
        {
            public UserException() : base("user") { }
            [Obsolete("Formatter-based serialization is obsolete.")]
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("UserMarker", 37);
            }
        }

        private sealed class InheritedException : Exception
        {
            public InheritedException() : base("inherited") { }
        }

#if PROVIDER_IDENTITY_ONLY
        private sealed class UnusedException : Exception
        {
            [Obsolete("Formatter-based serialization is obsolete.")]
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                int value = 17;
                TypedReference reference = __makeref(value);
                info.AddValue("unused", __refvalue(reference, int));
            }
        }
#endif

        private struct RefResult
        {
            public object? Value;
            public RefResult(object? value) => Value = value;
        }

        private readonly struct EmptyResult { }

        private readonly struct PaddedResult
        {
            public readonly byte First;
            public readonly int Second;
            public PaddedResult(byte first, int second) { First = first; Second = second; }
        }

        private readonly struct NestedResult
        {
            public readonly PaddedResult Inner;
            public readonly float Number;
            public readonly byte Tail;
            public NestedResult(PaddedResult inner, float number, byte tail)
            {
                Inner = inner;
                Number = number;
                Tail = tail;
            }
        }

        private enum CacheShade : byte { None, Value }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct ExplicitResult
        {
#pragma warning disable CS0649
            [FieldOffset(0)] public byte First;
#pragma warning restore CS0649
        }

        [StructLayout(LayoutKind.Sequential, Size = 4)]
        private struct SequentialResult
        {
#pragma warning disable CS0649
            public byte First;
#pragma warning restore CS0649
        }

        private readonly struct ParallelResultWrapper
        {
            public readonly ParallelLoopResult Value;
            public ParallelResultWrapper(ParallelLoopResult value) => Value = value;
        }

        private readonly struct DependentResultWrapper
        {
            public readonly System.Runtime.DependentHandle Handle;
            public readonly int Value;
            public DependentResultWrapper(System.Runtime.DependentHandle handle, int value)
            {
                Handle = handle;
                Value = value;
            }
        }

        private sealed class TrackingProvider : IFormatProvider
        {
            private readonly object? _result;
            public int Calls;
            public int NumericCalls;
            public bool NumericOnly = true;
            public TrackingProvider(object? result) => _result = result;
            public object? GetFormat(Type? formatType)
            {
                Calls++;
                NumericOnly &= formatType == typeof(NumberFormatInfo);
                if (formatType == typeof(NumberFormatInfo))
                {
                    NumericCalls++;
                    return _result;
                }
                return null;
            }
        }

        private sealed class UnusedProvider : IFormatProvider
        {
            public object? GetFormat(Type? formatType) => throw new InvalidOperationException("unused provider body");
        }

        private sealed class ThrowingProvider : IFormatProvider
        {
            public int Calls;
            public object? GetFormat(Type? formatType)
            {
                Calls++;
                throw new InvalidOperationException("provider fault");
            }
        }

        private sealed class Converter : IFormatterConverter
        {
            public object Convert(object value, Type type) => System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture)!;
            public object Convert(object value, TypeCode typeCode) => System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture)!;
            public bool ToBoolean(object value) => System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            public byte ToByte(object value) => System.Convert.ToByte(value, CultureInfo.InvariantCulture);
            public char ToChar(object value) => System.Convert.ToChar(value, CultureInfo.InvariantCulture);
            public DateTime ToDateTime(object value) => System.Convert.ToDateTime(value, CultureInfo.InvariantCulture);
            public decimal ToDecimal(object value) => System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            public double ToDouble(object value) => System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            public short ToInt16(object value) => System.Convert.ToInt16(value, CultureInfo.InvariantCulture);
            public int ToInt32(object value) => System.Convert.ToInt32(value, CultureInfo.InvariantCulture);
            public long ToInt64(object value) => System.Convert.ToInt64(value, CultureInfo.InvariantCulture);
            public sbyte ToSByte(object value) => System.Convert.ToSByte(value, CultureInfo.InvariantCulture);
            public float ToSingle(object value) => System.Convert.ToSingle(value, CultureInfo.InvariantCulture);
            public string ToString(object value) => System.Convert.ToString(value, CultureInfo.InvariantCulture)!;
            public ushort ToUInt16(object value) => System.Convert.ToUInt16(value, CultureInfo.InvariantCulture);
            public uint ToUInt32(object value) => System.Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            public ulong ToUInt64(object value) => System.Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        private static object IdentityProvider(IFormatProvider value) => value;

        internal static void RunProviderIdentity()
        {
            var provider = new UnusedProvider();
            object value = IdentityProvider(provider);
            Console.WriteLine("unused provider identity=" + ReferenceEquals(value, provider) + "/" + (value is IFormatProvider));
            Console.WriteLine("unused provider unrelated numeric=" + 12.5.ToString("F1", NumberFormatInfo.InvariantInfo) + "/"
                + (-12).ToString(CultureInfo.InvariantCulture) + "/" + 23.ToString((IFormatProvider?)null));
            IConvertible converted = 65;
            Console.WriteLine("unused provider convertible metadata=" + converted.GetTypeCode() + "/" + typeof(IConvertible).GetMethods().Length);
#if PROVIDER_IDENTITY_ONLY
            Console.WriteLine("unused exception metadata=" + typeof(UnusedException).IsSubclassOf(typeof(Exception)));
#endif
        }

        private static void Attributes(string label, ICustomAttributeProvider provider)
        {
            object[] all = provider.GetCustomAttributes(false);
            object[] filtered = provider.GetCustomAttributes(typeof(MarkerAttribute), false);
            Console.WriteLine("attributes " + label + "=" + filtered.Length + "/"
                + provider.IsDefined(typeof(MarkerAttribute), false) + "/" + (all.Length >= filtered.Length));
        }

        internal static void RunReflection()
        {
            Type type = typeof(Subject);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            MethodInfo method = type.GetMethod(nameof(Subject.Echo))!;
            Attributes("type", type);
            Attributes("method", method);
            Attributes("constructor", type.GetConstructor(Type.EmptyTypes)!);
            Attributes("field", type.GetField(nameof(Subject.Number))!);
            Attributes("property", type.GetProperty(nameof(Subject.Value))!);
            Attributes("event", type.GetEvent(nameof(Subject.Changed))!);
            Attributes("parameter", method.GetParameters()[0]);
            Attributes("assembly", type.Assembly);
            Attributes("module", type.Module);
            IReflect reflect = type;
            Console.WriteLine("reflect single=" + reflect.GetField(nameof(Subject.Number), flags)!.Name + "/"
                + reflect.GetMethod(nameof(Subject.Echo), flags)!.Name + "/"
                + reflect.GetMethod(nameof(Subject.Echo), flags, null, new[] { typeof(int) }, null)!.Name + "/"
                + reflect.GetProperty(nameof(Subject.Value), flags)!.Name + "/"
                + reflect.GetProperty(nameof(Subject.Value), flags, null, typeof(int), Type.EmptyTypes, null)!.Name);
            Console.WriteLine("reflect arrays=" + reflect.GetFields(flags).Length + "/"
                + reflect.GetMethods(flags).Length + "/" + reflect.GetProperties(flags).Length + "/"
                + reflect.GetMember(nameof(Subject.Echo), flags).Length + "/" + reflect.GetMembers(flags).Length);
            Console.WriteLine("reflect type identities=" + ReferenceEquals(reflect.UnderlyingSystemType, type) + "/"
                + ReferenceEquals(((ICloneable)type).Clone(), type) + "/"
                + ReferenceEquals(((IReflectableType)type).GetTypeInfo(), type.GetTypeInfo()));
        }

        private static SerializationInfo Info(object value) => new SerializationInfo(value.GetType(), new Converter());

        private static void Serialize(string label, ISerializable value)
        {
            var info = Info(value);
            try
            {
                value.GetObjectData(info, new StreamingContext(StreamingContextStates.All));
                Console.WriteLine("serialize " + label + "=completed");
                if (label == "argument")
                    Console.WriteLine("serialize argument parameter=" + info.GetString("ParamName"));
                if (label == "StringBuilder")
                {
                    Console.WriteLine("serialize builder payload=" + info.GetInt32("m_MaxCapacity") + "/"
                        + (info.GetInt32("Capacity") == ((StringBuilder)value).Capacity) + "/"
                        + info.GetString("m_StringValue") + "/" + info.GetInt32("m_currentThread"));
                    try { value.GetObjectData(null!, default); }
                    catch (ArgumentNullException ex) { Console.WriteLine("serialize builder null=" + ex.ParamName); }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("serialize " + label + "=" + ex.GetType().Name);
            }
        }

        private static int Divide(int value) => 9 / value;

        internal static void RunSerialization(string route = "all")
        {
            if (route == "all" || route == "plain")
                Serialize("plain", new Exception("plain"));
            if (route == "all" || route == "bcl")
            {
                Serialize("bcl", new InvalidOperationException("constructed"));
                Serialize("argument", new ArgumentException("argument", "value"));
            }
            if (route == "all" || route == "user")
            {
                Serialize("inherited user", new InheritedException());
                ISerializable user = new UserException();
                var info = Info(user);
                user.GetObjectData(info, default);
                Console.WriteLine("serialize user=" + info.GetInt32("UserMarker"));
            }
            if (route == "all" || route == "raised")
            {
                try { Divide(0); }
                catch (Exception ex) { Serialize("raised " + ex.GetType().Name, ex); }
            }
            if (route == "all" || route == "values")
            {
                Serialize("StringBuilder", new StringBuilder("runtime-held"));
                Serialize("Assembly", typeof(Program).Assembly);
                Serialize("Decimal", 12.5m);
                Serialize("DateTime", new DateTime(2020, 2, 3, 4, 5, 6, DateTimeKind.Utc));
            }
        }

        private static string WaitOutcome(WaitHandle handle)
        {
            try { return handle.WaitOne(0).ToString(); }
            catch (ObjectDisposedException) { return nameof(ObjectDisposedException); }
        }

        private static string TaskHandleOutcome(IAsyncResult result)
        {
            try { return result.AsyncWaitHandle.WaitOne(0).ToString(); }
            catch (ObjectDisposedException) { return nameof(ObjectDisposedException); }
        }

        private static async Task<int> DelayedResult()
        {
            await Task.Delay(1);
            return 43;
        }

#pragma warning disable CS1998
        private static async Task SynchronousTask() { }
        private static async Task<int> SynchronousTask(int value) => value;
        private static async ValueTask SynchronousValueTask() { }
        private static async ValueTask<int> SynchronousValueTask(int value) => value;
        private static async ValueTask<T> SynchronousValueTask<T>(T value) => value;
#pragma warning restore CS1998

        private static async ValueTask<T> YieldedValueTask<T>(T value)
        {
            await Task.Yield();
            return value;
        }

        private static async Task SuspendedTask(Task pending)
        {
            await pending;
        }

        private static async Task<int> SuspendedTask(Task pending, int value)
        {
            await pending;
            return value;
        }

        private static async ValueTask SuspendedValueTask(Task pending)
        {
            await pending;
        }

        private static async ValueTask<int> SuspendedValueTask(Task pending, int value)
        {
            await pending;
            return value;
        }

        private static void RunAsyncCompletionDisposal()
        {
            ValueTaskDisposal("sync task", SynchronousTask());
            ValueTaskDisposal("sync task cached int", SynchronousTask(0));
            ValueTaskDisposal("sync task uncached int", SynchronousTask(31));
            ValueTaskDisposal("sync valuetask", SynchronousValueTask().AsTask());
            ValueTaskDisposal("sync valuetask cached int", SynchronousValueTask(0).AsTask());
            ValueTaskDisposal("sync valuetask uncached int", SynchronousValueTask(31).AsTask());
            var pending = new TaskCompletionSource<int>();
            Task suspendedVoid = SuspendedTask(pending.Task);
            Task<int> suspendedInteger = SuspendedTask(pending.Task, 0);
            Task suspendedValueVoid = SuspendedValueTask(pending.Task).AsTask();
            Task<int> suspendedValueInteger = SuspendedValueTask(pending.Task, 0).AsTask();
            pending.SetResult(1);
            suspendedVoid.Wait();
            suspendedInteger.Wait();
            suspendedValueVoid.Wait();
            suspendedValueInteger.Wait();
            ValueTaskDisposal("suspended task", suspendedVoid);
            ValueTaskDisposal("suspended task int", suspendedInteger);
            ValueTaskDisposal("suspended valuetask", suspendedValueVoid);
            ValueTaskDisposal("suspended valuetask int", suspendedValueInteger);
            pending.Task.Dispose();
        }

        private static void ValueTaskDisposal(string label, Task task)
        {
            IAsyncResult result = task;
            WaitHandle handle = result.AsyncWaitHandle;
            task.Dispose();
            Console.WriteLine("valuetask " + label + "=" + result.IsCompleted + "/"
                + WaitOutcome(handle) + "/" + TaskHandleOutcome(result));
        }

        private static void TaskCacheValue<T>(string label, T value)
        {
            ValueTaskDisposal("cache from-result " + label, Task.FromResult(value));
            ValueTaskDisposal("cache value " + label, new ValueTask<T>(value).AsTask());
            ValueTaskDisposal("cache sync " + label, SynchronousValueTask(value).AsTask());
        }

        private static void RunTaskCacheStorage()
        {
            TaskCacheValue("empty", default(EmptyResult));
            TaskCacheValue("padded default", default(PaddedResult));
            TaskCacheValue("padded zero", new PaddedResult(0, 0));
            TaskCacheValue("padded value", new PaddedResult(1, 7));
            TaskCacheValue("nested default", default(NestedResult));
            TaskCacheValue("nested zero", new NestedResult(new PaddedResult(0, 0), 0.0f, 0));
            TaskCacheValue("nested value", new NestedResult(new PaddedResult(1, 7), 2.5f, 3));
            TaskCacheValue("nested negative zero", new NestedResult(default, -0.0f, 0));
            TaskCacheValue("float zero", 0.0f);
            TaskCacheValue("float negative zero", -0.0f);
            TaskCacheValue("enum default", CacheShade.None);
            TaskCacheValue("enum value", CacheShade.Value);
            TaskCacheValue<int?>("nullable null", null);
            TaskCacheValue<int?>("nullable value", 0);
            TaskCacheValue("parallel loop default", default(ParallelLoopResult));
            TaskCacheValue("dependent handle default", default(System.Runtime.DependentHandle));
            TaskCacheValue("parallel wrapper default", default(ParallelResultWrapper));
            TaskCacheValue("dependent wrapper default", default(DependentResultWrapper));
            ExplicitResult explicitZero = MemoryMarshal.Read<ExplicitResult>(new byte[] { 0, 0, 0, 0 });
            ExplicitResult explicitValue = MemoryMarshal.Read<ExplicitResult>(new byte[] { 0, 1, 0, 0 });
            Console.WriteLine("task cache explicit fields=" + explicitZero.First + "/" + explicitValue.First);
            TaskCacheValue("explicit storage zero", explicitZero);
            TaskCacheValue("explicit storage value", explicitValue);
            SequentialResult sequentialZero = MemoryMarshal.Read<SequentialResult>(new byte[] { 0, 0, 0, 0 });
            SequentialResult sequentialValue = MemoryMarshal.Read<SequentialResult>(new byte[] { 0, 1, 0, 0 });
            Console.WriteLine("task cache sequential fields=" + sequentialZero.First + "/" + sequentialValue.First);
            TaskCacheValue("sequential storage zero", sequentialZero);
            TaskCacheValue("sequential storage value", sequentialValue);
            PaddedResult alignmentZero = MemoryMarshal.Read<PaddedResult>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            PaddedResult alignmentValue = MemoryMarshal.Read<PaddedResult>(new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 });
            Console.WriteLine("task cache alignment fields=" + alignmentZero.First + "/" + alignmentZero.Second + "/"
                + alignmentValue.First + "/" + alignmentValue.Second);
            TaskCacheValue("alignment storage zero", alignmentZero);
            TaskCacheValue("alignment storage value", alignmentValue);
            TaskCacheValue("date offset default", default(DateTimeOffset));
            byte[] dateOffsetStorage = new byte[16];
            TaskCacheValue("date offset storage zero", MemoryMarshal.Read<DateTimeOffset>(dateOffsetStorage));
            dateOffsetStorage[12] = 1;
            TaskCacheValue("date offset storage value", MemoryMarshal.Read<DateTimeOffset>(dateOffsetStorage));
            TaskCacheValue("empty storage zero", MemoryMarshal.Read<EmptyResult>(new byte[] { 0 }));
            TaskCacheValue("empty storage value", MemoryMarshal.Read<EmptyResult>(new byte[] { 1 }));
            TaskCacheValue("yield awaiter storage zero",
                MemoryMarshal.Read<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(new byte[] { 0 }));
            TaskCacheValue("yield awaiter storage value",
                MemoryMarshal.Read<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter>(new byte[] { 1 }));

            _ = YieldedValueTask(default(EmptyResult)).GetAwaiter().GetResult();
            Console.WriteLine("valuetask yielded cache empty=completed");
            PaddedResult padded = YieldedValueTask(new PaddedResult(1, 7)).GetAwaiter().GetResult();
            Console.WriteLine("valuetask yielded cache padded=" + padded.First + "/" + padded.Second);
            NestedResult nested = YieldedValueTask(default(NestedResult)).GetAwaiter().GetResult();
            Console.WriteLine("valuetask yielded cache nested=" + nested.Inner.First + "/" + nested.Inner.Second + "/"
                + nested.Number + "/" + nested.Tail);
        }

        internal static void RunTasks()
        {
            object state = new object();
            object? observed = null;
            using var task = new Task(value => observed = value, state);
            IAsyncResult result = task;
            Console.WriteLine("task cold=" + ReferenceEquals(result.AsyncState, state) + "/"
                + result.IsCompleted + "/" + result.CompletedSynchronously);
            WaitHandle handle = result.AsyncWaitHandle;
            Console.WriteLine("task handle cold=" + ReferenceEquals(handle, result.AsyncWaitHandle) + "/" + WaitOutcome(handle));
            task.RunSynchronously();
            Console.WriteLine("task completed=" + ReferenceEquals(observed, state) + "/" + result.IsCompleted + "/"
                + result.CompletedSynchronously + "/" + ReferenceEquals(handle, result.AsyncWaitHandle) + "/" + WaitOutcome(handle));
            task.Dispose();
            Console.WriteLine("task disposed=" + WaitOutcome(handle) + "/" + TaskHandleOutcome(result));

            using var generic = new Task<int>(value => ReferenceEquals(value, state) ? 23 : -1, state);
            IAsyncResult genericResult = generic;
            Console.WriteLine("task generic cold=" + ReferenceEquals(genericResult.AsyncState, state) + "/" + genericResult.IsCompleted);
            generic.RunSynchronously();
            Console.WriteLine("task generic completed=" + generic.Result + "/" + genericResult.AsyncWaitHandle.WaitOne(0));
            var source = new TaskCompletionSource<int>(state);
            IAsyncResult sourceResult = source.Task;
            WaitHandle sourceHandle = sourceResult.AsyncWaitHandle;
            Console.WriteLine("task source cold=" + ReferenceEquals(sourceResult.AsyncState, state) + "/" + WaitOutcome(sourceHandle));
            source.SetResult(31);
            Console.WriteLine("task source completed=" + source.Task.Result + "/" + WaitOutcome(sourceHandle));
            source.Task.Dispose();
            Console.WriteLine("task source disposed=" + WaitOutcome(sourceHandle));
            var faulted = new TaskCompletionSource<int>();
            IAsyncResult faultedResult = faulted.Task;
            WaitHandle faultedHandle = faultedResult.AsyncWaitHandle;
            faulted.SetException(new InvalidOperationException("task fault"));
            Console.WriteLine("task faulted=" + faultedResult.IsCompleted + "/" + faultedResult.CompletedSynchronously + "/"
                + ReferenceEquals(faultedHandle, faultedResult.AsyncWaitHandle) + "/" + WaitOutcome(faultedHandle));
            faulted.Task.Dispose();
            Console.WriteLine("task faulted disposed=" + WaitOutcome(faultedHandle));
            var canceled = new TaskCompletionSource<int>();
            IAsyncResult canceledResult = canceled.Task;
            WaitHandle canceledHandle = canceledResult.AsyncWaitHandle;
            canceled.SetCanceled();
            Console.WriteLine("task canceled=" + canceledResult.IsCompleted + "/" + canceledResult.CompletedSynchronously + "/"
                + ReferenceEquals(canceledHandle, canceledResult.AsyncWaitHandle) + "/" + WaitOutcome(canceledHandle));
            canceled.Task.Dispose();
            Console.WriteLine("task canceled disposed=" + WaitOutcome(canceledHandle));
            var incomplete = new TaskCompletionSource<int>();
            try { incomplete.Task.Dispose(); }
            catch (InvalidOperationException) { Console.WriteLine("task incomplete dispose=InvalidOperationException"); }
            incomplete.SetResult(19);
            IAsyncResult incompleteResult = incomplete.Task;
            WaitHandle closedHandle = incompleteResult.AsyncWaitHandle;
            closedHandle.Dispose();
            Console.WriteLine("task closed handle=" + ReferenceEquals(closedHandle, incompleteResult.AsyncWaitHandle) + "/" + WaitOutcome(closedHandle));
            incomplete.Task.Dispose();
            Console.WriteLine("task closed handle disposed=" + TaskHandleOutcome(incompleteResult));
            IAsyncResult completed = Task.CompletedTask;
            Console.WriteLine("task factory completed=" + (completed.AsyncState is null) + "/" + completed.IsCompleted + "/"
                + completed.CompletedSynchronously + "/" + completed.AsyncWaitHandle.WaitOne(0));
            WaitHandle completedHandle = completed.AsyncWaitHandle;
            ((Task)completed).Dispose();
            Console.WriteLine("task factory disposed=" + ReferenceEquals(completedHandle, completed.AsyncWaitHandle) + "/"
                + WaitOutcome(completedHandle) + "/" + TaskHandleOutcome(completed));
            Task<bool> cachedBoolean = Task.FromResult(true);
            IAsyncResult booleanResult = cachedBoolean;
            WaitHandle booleanHandle = booleanResult.AsyncWaitHandle;
            cachedBoolean.Dispose();
            Console.WriteLine("task cached bool=" + cachedBoolean.Result + "/"
                + ReferenceEquals(booleanHandle, booleanResult.AsyncWaitHandle) + "/"
                + WaitOutcome(booleanHandle) + "/" + TaskHandleOutcome(booleanResult));
            Task<int> cachedInteger = Task.FromResult(0);
            IAsyncResult integerResult = cachedInteger;
            WaitHandle integerHandle = integerResult.AsyncWaitHandle;
            cachedInteger.Dispose();
            Console.WriteLine("task cached int=" + cachedInteger.Result + "/"
                + ReferenceEquals(integerHandle, integerResult.AsyncWaitHandle) + "/"
                + WaitOutcome(integerHandle) + "/" + TaskHandleOutcome(integerResult));
            Task<int> uncachedInteger = Task.FromResult(31);
            IAsyncResult uncachedResult = uncachedInteger;
            WaitHandle uncachedHandle = uncachedResult.AsyncWaitHandle;
            uncachedInteger.Dispose();
            Console.WriteLine("task uncached int=" + uncachedInteger.Result + "/"
                + WaitOutcome(uncachedHandle) + "/" + TaskHandleOutcome(uncachedResult));
            var pending = new TaskCompletionSource<int>();
            object? continuationObserved = null;
            Task continuation = pending.Task.ContinueWith((antecedent, value) => { continuationObserved = value; },
                state, TaskContinuationOptions.ExecuteSynchronously);
            Task<int> genericContinuation = pending.Task.ContinueWith((antecedent, value) =>
                ReferenceEquals(value, state) ? antecedent.Result + 1 : -1, state, TaskContinuationOptions.ExecuteSynchronously);
            IAsyncResult continuationResult = continuation;
            IAsyncResult genericContinuationResult = genericContinuation;
            WaitHandle continuationHandle = continuationResult.AsyncWaitHandle;
            Console.WriteLine("task continuation cold=" + ReferenceEquals(continuationResult.AsyncState, state) + "/"
                + ReferenceEquals(genericContinuationResult.AsyncState, state) + "/" + WaitOutcome(continuationHandle));
            pending.SetResult(41);
            continuation.Wait();
            genericContinuation.Wait();
            Console.WriteLine("task continuation completed=" + ReferenceEquals(continuationObserved, state) + "/"
                + continuationResult.IsCompleted + "/" + WaitOutcome(continuationHandle) + "/" + genericContinuation.Result);
            continuation.Dispose();
            genericContinuation.Dispose();
            pending.Task.Dispose();
            Console.WriteLine("task continuation disposed=" + WaitOutcome(continuationHandle) + "/" + TaskHandleOutcome(continuationResult));
            using var delayed = Task.Delay(1);
            IAsyncResult delayedResult = delayed;
            WaitHandle delayedHandle = delayedResult.AsyncWaitHandle;
            Console.WriteLine("task delay timed=" + delayedHandle.WaitOne(5000) + "/" + delayedResult.IsCompleted + "/"
                + ReferenceEquals(delayedHandle, delayedResult.AsyncWaitHandle));
            using Task<int> awaited = DelayedResult();
            IAsyncResult awaitedResult = awaited;
            Console.WriteLine("task await untimed=" + awaitedResult.AsyncWaitHandle.WaitOne() + "/" + awaitedResult.IsCompleted + "/" + awaited.Result);
            using var anyTask = Task.Delay(1);
            IAsyncResult anyResult = anyTask;
            WaitHandle anyHandle = anyResult.AsyncWaitHandle;
            Console.WriteLine("task delay waitany=" + WaitHandle.WaitAny(new[] { anyHandle }) + "/"
                + anyResult.IsCompleted + "/" + anyHandle.WaitOne(0));
            ValueTaskDisposal("default int", default(ValueTask<int>).AsTask());
            ValueTaskDisposal("cached bool", new ValueTask<bool>(true).AsTask());
            ValueTaskDisposal("cached int", new ValueTask<int>(0).AsTask());
            ValueTaskDisposal("uncached int", new ValueTask<int>(31).AsTask());
            ValueTaskDisposal("null string", new ValueTask<string?>((string?)null).AsTask());
            ValueTaskDisposal("value string", new ValueTask<string>("value").AsTask());
            ValueTaskDisposal("value object", new ValueTask<object>(new object()).AsTask());
            ValueTaskDisposal("default reference", default(ValueTask<object>).AsTask());
            ValueTaskDisposal("default struct reference", default(ValueTask<RefResult>).AsTask());
            RunAsyncCompletionDisposal();
            RunTaskCacheStorage();
        }

        internal static void RunCultures()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            var cultureClone = (CultureInfo)((ICloneable)culture).Clone();
            cultureClone.NumberFormat.NumberDecimalSeparator = "~";
            Console.WriteLine("clone culture=" + !ReferenceEquals(cultureClone, culture) + "/"
                + culture.NumberFormat.NumberDecimalSeparator + "/" + cultureClone.NumberFormat.NumberDecimalSeparator);
            NumberFormatInfo number = NumberFormatInfo.InvariantInfo;
            var numberClone = (NumberFormatInfo)((ICloneable)number).Clone();
            numberClone.NumberDecimalSeparator = ":";
            Console.WriteLine("clone number=" + !ReferenceEquals(numberClone, number) + "/"
                + number.NumberDecimalSeparator + "/" + numberClone.NumberDecimalSeparator);
            TextInfo text = culture.TextInfo;
            var textClone = (TextInfo)((ICloneable)text).Clone();
            Console.WriteLine("clone text=" + !ReferenceEquals(textClone, text) + "/"
                + textClone.ListSeparator + "/" + textClone.ToUpper("clone"));
            DateTimeFormatInfo date = DateTimeFormatInfo.InvariantInfo;
            var dateClone = (DateTimeFormatInfo)((ICloneable)date).Clone();
            dateClone.DateSeparator = "!";
            Console.WriteLine("clone date=" + !ReferenceEquals(dateClone, date) + "/"
                + date.DateSeparator + "/" + dateClone.DateSeparator);
            try
            {
                ((IDeserializationCallback)textClone).OnDeserialization(null);
                Console.WriteLine("text deserialization=completed");
            }
            catch (Exception ex) { Console.WriteLine("text deserialization=" + ex.GetType().Name); }
            object boxed = 12.5m;
            ((IDeserializationCallback)boxed).OnDeserialization(null);
            Console.WriteLine("deserialization callbacks=" + textClone.ListSeparator + "/" + (decimal)boxed);
            CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
            IDeserializationCallback compare = compareInfo;
            Console.WriteLine("compare deserialization before=" + (compareInfo.Compare("a", "b") < 0));
            compare.OnDeserialization(null);
            Console.WriteLine("compare deserialization null=completed/" + (compareInfo.Compare("a", "b") < 0));
            compare.OnDeserialization(new object());
            Console.WriteLine("compare deserialization sender=completed/" + (compareInfo.Compare("a", "b") < 0));
        }

        internal static void RunNumbers()
        {
            IBinaryInteger<int> number = -258;
            byte[] big = new byte[4];
            byte[] little = new byte[4];
            Console.WriteLine("binary integer counts=" + number.GetByteCount() + "/" + number.GetShortestBitLength());
            bool bigOk = number.TryWriteBigEndian(big, out int bigWritten);
            bool littleOk = number.TryWriteLittleEndian(little, out int littleWritten);
            Console.WriteLine("binary integer bytes=" + bigOk + "/" + bigWritten + "/" + Convert.ToHexString(big) + "/"
                + littleOk + "/" + littleWritten + "/" + Convert.ToHexString(little));
            Console.WriteLine("binary integer short=" + number.TryWriteBigEndian(new byte[1], out int shortWritten) + "/" + shortWritten);
            IConvertible converted = 65;
            Console.WriteLine("convertible slots=" + converted.GetTypeCode() + "/" + converted.ToBoolean(null) + "/"
                + converted.ToByte(null) + "/" + converted.ToChar(null) + "/" + converted.ToDecimal(null) + "/"
                + converted.ToDouble(null) + "/" + converted.ToInt16(null) + "/" + converted.ToInt32(null) + "/"
                + converted.ToInt64(null) + "/" + converted.ToSByte(null) + "/" + converted.ToSingle(null) + "/"
                + converted.ToString(null) + "/" + converted.ToUInt16(null) + "/" + converted.ToUInt32(null) + "/"
                + converted.ToUInt64(null) + "/" + converted.ToType(typeof(long), null));
            try { converted.ToDateTime(null); }
            catch (InvalidCastException) { Console.WriteLine("convertible datetime=InvalidCastException"); }
            Type convertibleType = typeof(IConvertible);
            MethodInfo[] methods = convertibleType.GetMethods();
            bool signatures = true;
            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                signatures &= method.ReturnType != typeof(void)
                    && method.DeclaringType == convertibleType && !method.IsStatic
                    && ReferenceEquals(method, convertibleType.GetMethod(method.Name));
                if (method.Name == nameof(IConvertible.GetTypeCode))
                    signatures &= parameters.Length == 0 && method.ReturnType == typeof(TypeCode);
                else if (method.Name == nameof(IConvertible.ToType))
                    signatures &= parameters.Length == 2 && parameters[0].ParameterType == typeof(Type)
                        && parameters[1].ParameterType == typeof(IFormatProvider) && method.ReturnType == typeof(object);
                else
                    signatures &= parameters.Length == 1 && parameters[0].ParameterType == typeof(IFormatProvider);
            }
            Console.WriteLine("convertible metadata=" + methods.Length + "/" + signatures);
            Console.WriteLine("convertible invoke=" + convertibleType.GetMethod(nameof(IConvertible.GetTypeCode))!.Invoke(converted, null) + "/"
                + convertibleType.GetMethod(nameof(IConvertible.ToInt32))!.Invoke(converted, new object?[] { null }) + "/"
                + convertibleType.GetMethod(nameof(IConvertible.ToString))!.Invoke(converted, new object?[] { null }));
        }

        internal static void RunProviders()
        {
            var numbers = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
            numbers.NumberDecimalSeparator = "~";
            numbers.NegativeSign = "minus";
            var valid = new TrackingProvider(numbers);
            Console.WriteLine("provider valid=" + 12.5.ToString("F1", valid) + "/"
                + ReferenceEquals(NumberFormatInfo.GetInstance(valid), numbers) + "/" + valid.Calls + "/" + valid.NumericOnly);
            Console.WriteLine("provider integer=" + (-12).ToString(valid) + "/" + valid.Calls + "/" + valid.NumericOnly);
            var wrong = new TrackingProvider(new object());
            Console.WriteLine("provider wrong=" + 12.5m.ToString("F1", wrong) + "/"
                + ReferenceEquals(NumberFormatInfo.GetInstance(wrong), NumberFormatInfo.CurrentInfo) + "/" + wrong.Calls + "/" + wrong.NumericOnly);
            var empty = new TrackingProvider(null);
            Console.WriteLine("provider null=" + 1234.ToString("N1", empty) + "/"
                + ReferenceEquals(NumberFormatInfo.GetInstance(empty), NumberFormatInfo.CurrentInfo) + "/" + empty.Calls + "/" + empty.NumericOnly);
            try { 12.5.ToString("F1", new ThrowingProvider()); }
            catch (InvalidOperationException ex) { Console.WriteLine("provider exception=" + ex.Message); }
            var boxedProvider = new TrackingProvider(numbers);
            IFormattable boxedNumber = 12.5m;
            Console.WriteLine("provider boxed formattable=" + boxedNumber.ToString("F1", boxedProvider) + "/" + boxedProvider.NumericCalls);
            var compositeProvider = new TrackingProvider(numbers);
            Console.WriteLine("provider composite=" + string.Format(compositeProvider, "number={0:F1};integer={1:N1}", 12.5, 1234)
                + "/" + compositeProvider.NumericCalls);
            var ignoredProvider = new ThrowingProvider();
            IFormattable positive = 12;
            Console.WriteLine("provider boxed ignored=" + positive.ToString(null, ignoredProvider) + "/"
                + positive.ToString("", ignoredProvider) + "/" + positive.ToString("D4", ignoredProvider) + "/"
                + positive.ToString("X4", ignoredProvider) + "/" + positive.ToString("B8", ignoredProvider) + "/" + ignoredProvider.Calls);
            var negativeProvider = new TrackingProvider(numbers);
            IFormattable negative = -12;
            Console.WriteLine("provider boxed negative=" + negative.ToString(null, negativeProvider) + "/"
                + negative.ToString("D4", negativeProvider) + "/" + negativeProvider.NumericCalls);
            var defaultCompositeProvider = new TrackingProvider(numbers);
            Console.WriteLine("provider composite default=" + string.Format(defaultCompositeProvider, "default={0};empty={1:}", -12, 12.5)
                + "/" + defaultCompositeProvider.NumericCalls);
        }

        private static void RunBoxedTypeHandleSerialization()
        {
            Console.WriteLine("== boxed runtime type handle serialization ==");
            SerializeHandle(default, "empty");
            SerializeHandle(typeof(int).TypeHandle, "populated");
            Console.WriteLine("boxed runtime type handle serialization end");
        }

        private static void SerializeHandle(RuntimeTypeHandle value, string label)
        {
            object boxed = RuntimeHelpers.Box(ref Unsafe.As<RuntimeTypeHandle, byte>(ref value),
                typeof(RuntimeTypeHandle).TypeHandle)!;
            try
            {
                ((ISerializable)boxed).GetObjectData(null!, default);
                Console.WriteLine("boxed type handle " + label + "=returned");
            }
            catch (Exception ex)
            {
                Console.WriteLine("boxed type handle " + label + "=" + ex.GetType().Name);
            }
        }

        internal static void Run(string route = "all", bool includeHandleSerialization = true)
        {
            Console.WriteLine("== runtime-held interface calls ==");
            if (route == "all" || route == "reflection") RunReflection();
            if (route == "all" || route.StartsWith("serialization", StringComparison.Ordinal))
                RunSerialization(route.Length > 14 ? route.Substring(14) : "all");
            if (route == "all" || route == "tasks") RunTasks();
            if (route == "all" || route == "cultures") RunCultures();
            if (route == "all" || route == "numbers") RunNumbers();
            if (route == "all" || route == "providers") RunProviders();
            Console.WriteLine("runtime-held interface calls end");
            if (route == "all" && includeHandleSerialization)
                RunBoxedTypeHandleSerialization();
        }
    }
}
