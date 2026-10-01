using System;
using System.Threading;
using System.Globalization;
internal static class Program
{
    private static void Run(string label, Func<bool> run)
    {
        try
        {
            Console.WriteLine(label + " result=" + run());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            Console.WriteLine(label + " param=" + (ex is ArgumentException argument ? argument.ParamName : ""));
            object? value = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (value is null ? "null" : value.GetType().Name + ":" + value));
        }
    }

    public static bool DirectSemInt(SemaphoreSlim value, int timeout) => value.Wait(timeout);
    public static bool DirectSemSpan(SemaphoreSlim value, TimeSpan timeout) => value.Wait(timeout);
    public static bool DirectMreInt(ManualResetEventSlim value, int timeout) => value.Wait(timeout);
    public static bool DirectMreSpan(ManualResetEventSlim value, TimeSpan timeout) => value.Wait(timeout);
    public static bool DirectWaitOneInt(WaitHandle value, int timeout) => value.WaitOne(timeout);
    public static bool DirectWaitOneSpan(WaitHandle value, TimeSpan timeout) => value.WaitOne(timeout);
    public static bool DirectCountdownInt(CountdownEvent value, int timeout) => value.Wait(timeout);
    public static bool DirectCountdownSpan(CountdownEvent value, TimeSpan timeout) => value.Wait(timeout);
    public static bool DirectBarrierInt(Barrier value, int timeout) => value.SignalAndWait(timeout);
    public static bool DirectBarrierSpan(Barrier value, TimeSpan timeout) => value.SignalAndWait(timeout);
    public static bool DirectReadInt(ReaderWriterLockSlim value, int timeout) => value.TryEnterReadLock(timeout);
    public static bool DirectReadSpan(ReaderWriterLockSlim value, TimeSpan timeout) => value.TryEnterReadLock(timeout);
    public static bool DirectWriteInt(ReaderWriterLockSlim value, int timeout) => value.TryEnterWriteLock(timeout);
    public static bool DirectWriteSpan(ReaderWriterLockSlim value, TimeSpan timeout) => value.TryEnterWriteLock(timeout);
    public static bool DirectUpgradeInt(ReaderWriterLockSlim value, int timeout) => value.TryEnterUpgradeableReadLock(timeout);
    public static bool DirectUpgradeSpan(ReaderWriterLockSlim value, TimeSpan timeout) => value.TryEnterUpgradeableReadLock(timeout);
    public static bool DirectThreadInt(Thread value, int timeout) => value.Join(timeout);
    public static bool DirectThreadSpan(Thread value, TimeSpan timeout) => value.Join(timeout);
    public static bool DirectLockInt(System.Threading.Lock value, int timeout) => value.TryEnter(timeout);
    public static bool DirectLockSpan(System.Threading.Lock value, TimeSpan timeout) => value.TryEnter(timeout);
    public static bool DirectSemTokenInt(SemaphoreSlim value, int timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectSemTokenSpan(SemaphoreSlim value, TimeSpan timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectMreTokenInt(ManualResetEventSlim value, int timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectMreTokenSpan(ManualResetEventSlim value, TimeSpan timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectCountdownTokenInt(CountdownEvent value, int timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectCountdownTokenSpan(CountdownEvent value, TimeSpan timeout, CancellationToken token) => value.Wait(timeout,token);
    public static bool DirectBarrierTokenInt(Barrier value, int timeout, CancellationToken token) => value.SignalAndWait(timeout,token);
    public static bool DirectBarrierTokenSpan(Barrier value, TimeSpan timeout, CancellationToken token) => value.SignalAndWait(timeout,token);
    public static bool DirectWaitOneIntExit(WaitHandle value,int timeout,bool exitContext) => value.WaitOne(timeout,exitContext);
    public static bool DirectWaitOneSpanExit(WaitHandle value,TimeSpan timeout,bool exitContext) => value.WaitOne(timeout,exitContext);
    public static void Main()
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        Run("Sem virtual int bad", () => ((SemaphoreSlim)null!).Wait(-2));
        Run("Sem virtual span bad", () => ((SemaphoreSlim)null!).Wait(TimeSpan.MinValue));
        Run("Mre virtual int bad", () => ((ManualResetEventSlim)null!).Wait(-2));
        Run("Mre virtual span bad", () => ((ManualResetEventSlim)null!).Wait(TimeSpan.MinValue));
        Run("WaitOne virtual int bad", () => ((WaitHandle)null!).WaitOne(-2));
        Run("WaitOne virtual span bad", () => ((WaitHandle)null!).WaitOne(TimeSpan.MinValue));
        Run("Countdown virtual int bad", () => ((CountdownEvent)null!).Wait(-2));
        Run("Countdown virtual span bad", () => ((CountdownEvent)null!).Wait(TimeSpan.MinValue));
        Run("Barrier virtual int bad", () => ((Barrier)null!).SignalAndWait(-2));
        Run("Barrier virtual span bad", () => ((Barrier)null!).SignalAndWait(TimeSpan.MinValue));
        Run("Read virtual int bad", () => ((ReaderWriterLockSlim)null!).TryEnterReadLock(-2));
        Run("Read virtual span bad", () => ((ReaderWriterLockSlim)null!).TryEnterReadLock(TimeSpan.MinValue));
        Run("Write virtual int bad", () => ((ReaderWriterLockSlim)null!).TryEnterWriteLock(-2));
        Run("Write virtual span bad", () => ((ReaderWriterLockSlim)null!).TryEnterWriteLock(TimeSpan.MinValue));
        Run("Upgrade virtual int bad", () => ((ReaderWriterLockSlim)null!).TryEnterUpgradeableReadLock(-2));
        Run("Upgrade virtual span bad", () => ((ReaderWriterLockSlim)null!).TryEnterUpgradeableReadLock(TimeSpan.MinValue));
        Run("Thread virtual int bad", () => ((Thread)null!).Join(-2));
        Run("Thread virtual span bad", () => ((Thread)null!).Join(TimeSpan.MinValue));
        Run("Lock virtual int bad", () => ((System.Threading.Lock)null!).TryEnter(-2));
        Run("Lock virtual span bad", () => ((System.Threading.Lock)null!).TryEnter(TimeSpan.MinValue));
        Run("Sem virtual int token bad", () => ((SemaphoreSlim)null!).Wait(-2,CancellationToken.None));
        Run("Sem virtual span token bad", () => ((SemaphoreSlim)null!).Wait(TimeSpan.MinValue,CancellationToken.None));
        Run("Mre virtual int token bad", () => ((ManualResetEventSlim)null!).Wait(-2,CancellationToken.None));
        Run("Mre virtual span token bad", () => ((ManualResetEventSlim)null!).Wait(TimeSpan.MinValue,CancellationToken.None));
        Run("Countdown virtual int token bad", () => ((CountdownEvent)null!).Wait(-2,CancellationToken.None));
        Run("Countdown virtual span token bad", () => ((CountdownEvent)null!).Wait(TimeSpan.MinValue,CancellationToken.None));
        Run("Barrier virtual int token bad", () => ((Barrier)null!).SignalAndWait(-2,CancellationToken.None));
        Run("Barrier virtual span token bad", () => ((Barrier)null!).SignalAndWait(TimeSpan.MinValue,CancellationToken.None));
        Run("WaitOne direct int exit bad", () => DirectWaitOneIntExit(null!, -2, false));
        Run("WaitOne direct span exit bad", () => DirectWaitOneSpanExit(null!, TimeSpan.MinValue, false));
        Run("WaitOne direct int exit valid", () => DirectWaitOneIntExit(null!, 0, false));
        Run("WaitOne direct span exit valid", () => DirectWaitOneSpanExit(null!, TimeSpan.Zero, false));
        Run("SemToken null Int bad", () => DirectSemTokenInt(null!, -2, CancellationToken.None));
        Run("SemToken null Span bad", () => DirectSemTokenSpan(null!, TimeSpan.FromTicks(-20000), CancellationToken.None));
        Run("MreToken null Int bad", () => DirectMreTokenInt(null!, -2, CancellationToken.None));
        Run("MreToken null Span bad", () => DirectMreTokenSpan(null!, TimeSpan.FromTicks(-20000), CancellationToken.None));
        Run("CountdownToken null Int bad", () => DirectCountdownTokenInt(null!, -2, CancellationToken.None));
        Run("CountdownToken null Span bad", () => DirectCountdownTokenSpan(null!, TimeSpan.FromTicks(-20000), CancellationToken.None));
        Run("BarrierToken null Int bad", () => DirectBarrierTokenInt(null!, -2, CancellationToken.None));
        Run("BarrierToken null Span bad", () => DirectBarrierTokenSpan(null!, TimeSpan.FromTicks(-20000), CancellationToken.None));
        Run("Sem null int bad", () => DirectSemInt(null!, -2));
        Run("Sem null span bad", () => DirectSemSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Sem null span max", () => DirectSemSpan(null!, TimeSpan.MaxValue));
        Run("Sem null int valid", () => DirectSemInt(null!, 0));
        Run("Sem null span valid", () => DirectSemSpan(null!, TimeSpan.Zero));
        Run("Mre null int bad", () => DirectMreInt(null!, -2));
        Run("Mre null span bad", () => DirectMreSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Mre null span max", () => DirectMreSpan(null!, TimeSpan.MaxValue));
        Run("Mre null int valid", () => DirectMreInt(null!, 0));
        Run("Mre null span valid", () => DirectMreSpan(null!, TimeSpan.Zero));
        Run("WaitOne null int bad", () => DirectWaitOneInt(null!, -2));
        Run("WaitOne null span bad", () => DirectWaitOneSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("WaitOne null span max", () => DirectWaitOneSpan(null!, TimeSpan.MaxValue));
        Run("WaitOne null int valid", () => DirectWaitOneInt(null!, 0));
        Run("WaitOne null span valid", () => DirectWaitOneSpan(null!, TimeSpan.Zero));
        Run("Countdown null int bad", () => DirectCountdownInt(null!, -2));
        Run("Countdown null span bad", () => DirectCountdownSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Countdown null span max", () => DirectCountdownSpan(null!, TimeSpan.MaxValue));
        Run("Countdown null int valid", () => DirectCountdownInt(null!, 0));
        Run("Countdown null span valid", () => DirectCountdownSpan(null!, TimeSpan.Zero));
        Run("Barrier null int bad", () => DirectBarrierInt(null!, -2));
        Run("Barrier null span bad", () => DirectBarrierSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Barrier null span max", () => DirectBarrierSpan(null!, TimeSpan.MaxValue));
        Run("Barrier null int valid", () => DirectBarrierInt(null!, 0));
        Run("Barrier null span valid", () => DirectBarrierSpan(null!, TimeSpan.Zero));
        Run("Read null int bad", () => DirectReadInt(null!, -2));
        Run("Read null span bad", () => DirectReadSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Read null span max", () => DirectReadSpan(null!, TimeSpan.MaxValue));
        Run("Read null int valid", () => DirectReadInt(null!, 0));
        Run("Read null span valid", () => DirectReadSpan(null!, TimeSpan.Zero));
        Run("Write null int bad", () => DirectWriteInt(null!, -2));
        Run("Write null span bad", () => DirectWriteSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Write null span max", () => DirectWriteSpan(null!, TimeSpan.MaxValue));
        Run("Write null int valid", () => DirectWriteInt(null!, 0));
        Run("Write null span valid", () => DirectWriteSpan(null!, TimeSpan.Zero));
        Run("Upgrade null int bad", () => DirectUpgradeInt(null!, -2));
        Run("Upgrade null span bad", () => DirectUpgradeSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Upgrade null span max", () => DirectUpgradeSpan(null!, TimeSpan.MaxValue));
        Run("Upgrade null int valid", () => DirectUpgradeInt(null!, 0));
        Run("Upgrade null span valid", () => DirectUpgradeSpan(null!, TimeSpan.Zero));
        Run("Thread null int bad", () => DirectThreadInt(null!, -2));
        Run("Thread null span bad", () => DirectThreadSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Thread null span max", () => DirectThreadSpan(null!, TimeSpan.MaxValue));


        Run("Lock null int bad", () => DirectLockInt(null!, -2));
        Run("Lock null span bad", () => DirectLockSpan(null!, TimeSpan.FromTicks(-20000)));
        Run("Lock null span max", () => DirectLockSpan(null!, TimeSpan.MaxValue));
        Run("Lock null int valid", () => DirectLockInt(null!, 0));
        Run("Lock null span valid", () => DirectLockSpan(null!, TimeSpan.Zero));
        Console.WriteLine("Blocking timeout call faults end");
    }
}
