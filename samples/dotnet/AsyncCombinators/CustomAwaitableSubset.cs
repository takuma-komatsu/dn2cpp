#nullable enable
// A custom (non-Task) awaitable whose awaiter genuinely suspends.
//
// Such an awaiter implements INotifyCompletion.OnCompleted(Action) (or
// ICriticalNotifyCompletion.UnsafeOnCompleted) — the C# async lowering hands it a
// continuation it must invoke once it completes. dn2cpp synthesizes that continuation
// as a System.Action wrapping the boxed state machine's MoveNext and calls the
// awaiter's OnCompleted/UnsafeOnCompleted; the awaiter here bridges to a Task, so the
// continuation fires off the virtual-time scheduler. Covers: an int-result suspending
// awaiter (INotifyCompletion); a string-result one via UnsafeOnCompleted
// (ICriticalNotifyCompletion); a *synchronous* awaiter (IsCompleted == true, never
// suspends) as a regression; interleaving with a real Task.Delay; and out-of-order
// completion (a later await with a shorter delay).

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

// Suspending awaiter, INotifyCompletion, int result. Backed by a Task.Delay; the
// continuation is registered on the underlying task's awaiter.
namespace CustomAwaitableSubset;

struct DelayValue
{
    readonly int _ms;
    public DelayValue(int ms) => _ms = ms;
    public DelayValueAwaiter GetAwaiter() => new DelayValueAwaiter(_ms);
}

struct DelayValueAwaiter : INotifyCompletion
{
    readonly Task _t;
    readonly int _ms;
    public DelayValueAwaiter(int ms) { _t = Task.Delay(ms); _ms = ms; }
    public bool IsCompleted => _t.IsCompleted;
    public void OnCompleted(Action continuation) => _t.GetAwaiter().OnCompleted(continuation);
    public int GetResult() => _ms * 2;
}

// Suspending awaiter via ICriticalNotifyCompletion (UnsafeOnCompleted), string result.
struct TagValue
{
    readonly string _s;
    readonly int _ms;
    public TagValue(string s, int ms) { _s = s; _ms = ms; }
    public TagValueAwaiter GetAwaiter() => new TagValueAwaiter(_s, _ms);
}

struct TagValueAwaiter : ICriticalNotifyCompletion
{
    readonly Task _t;
    readonly string _s;
    public TagValueAwaiter(string s, int ms) { _t = Task.Delay(ms); _s = s; }
    public bool IsCompleted => _t.IsCompleted;
    public void OnCompleted(Action continuation) => _t.GetAwaiter().OnCompleted(continuation);
    public void UnsafeOnCompleted(Action continuation) => _t.GetAwaiter().UnsafeOnCompleted(continuation);
    public string GetResult() => "<" + _s + ">";
}

// Synchronous awaiter: IsCompleted is always true, so the await never suspends and the
// AwaitOnCompleted suspend path is never taken (regression guard for the sync case).
struct Immediate
{
    readonly int _v;
    public Immediate(int v) => _v = v;
    public ImmediateAwaiter GetAwaiter() => new ImmediateAwaiter(_v);
}

struct ImmediateAwaiter : INotifyCompletion
{
    readonly int _v;
    public ImmediateAwaiter(int v) => _v = v;
    public bool IsCompleted => true;
    public void OnCompleted(Action continuation) => continuation();
    public int GetResult() => _v + 1;
}

static class Program
{
    static async Task<string> Run()
    {
        int a = await new DelayValue(30);            // 60 — suspends
        int s = await new Immediate(5);              // 6  — synchronous, no suspend
        int b = await new DelayValue(10);            // 20 — suspends, shorter delay
        await Task.Delay(7);                         // interleave a genuine Task await
        string t = await new TagValue("hi", 15);     // <hi> — suspends via UnsafeOnCompleted
        return a + "," + s + "," + b + "," + t;
    }internal static void __GateEntry()
    {
        Console.WriteLine(Run().Result);
    }
}
