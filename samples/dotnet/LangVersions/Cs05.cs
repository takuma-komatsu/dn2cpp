// C# 5.0: async/await over `Task` and `Task<T>` (sequential multiple awaits,
// `Task.FromResult`, and exception propagation observed through try/catch),
// and the caller-info attributes.
//
// `CallerFilePath` returns an absolute path, which is environment-dependent,
// so it is reduced to just the file name via `Path.GetFileName` before
// printing. `CallerLineNumber` only varies if this file's line numbers
// change, which is stable across runs of the same build, so it is printed
// as-is. `__GateEntry` itself stays synchronous (its signature is fixed by
// the gate driver contract), so the async work is observed by blocking on
// `GetAwaiter().GetResult()` — and every await below is sequential, so no
// completion-order nondeterminism (e.g. from racing `Task.Run` calls) can
// leak into the output.
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Cs05;

internal static class CallerInfo
{
    internal static string Where(
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileName(filePath);
        return memberName + "@" + fileName + ":" + lineNumber;
    }
}

internal static class Program
{
    private static async Task<int> AddOneAsync(int x)
    {
        await Task.Yield();
        return x + 1;
    }

    private static async Task<int> SumAsync(int a, int b)
    {
        int first = await AddOneAsync(a);
        int second = await AddOneAsync(b);
        int fromResult = await Task.FromResult(a + b);
        return first + second + fromResult;
    }

    private static async Task ThrowingAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }

    private static async Task<string> RunAllAsync()
    {
        int sum = await SumAsync(2, 3);
        string result = "sum=" + sum;

        try
        {
            await ThrowingAsync();
            result += " unreachable";
        }
        catch (InvalidOperationException e)
        {
            result += " caught:" + e.Message;
        }

        return result;
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 5.0 ==");

        string outcome = RunAllAsync().GetAwaiter().GetResult();
        Console.WriteLine(outcome);

        Console.WriteLine("caller=" + CallerInfo.Where());
    }
}
