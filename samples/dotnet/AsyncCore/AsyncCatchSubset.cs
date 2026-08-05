#nullable disable
using System;
using System.Threading.Tasks;

namespace AsyncCatchSubset
{
    // A `catch (System.Exception)` must catch every managed exception. The C#
    // compiler wraps every async method's body in `catch (Exception) ->
    // SetException`, so a faulting await must fault the awaiting task rather than
    // escape the scheduler. A `try/catch` *inside* an async method catches a
    // faulting await, and a plain `catch (Exception)` works too.
    internal static class Program
    {
        private static async Task<int> Fail()
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }

        // try/catch inside an async method catches the faulting await directly.
        private static async Task<int> CatchInside()
        {
            try
            {
                return await Fail();
            }
            catch (InvalidOperationException)
            {
                return 1;
            }
        }

        // The general catch clause the async builder itself uses.
        private static async Task<int> CatchGeneral()
        {
            try
            {
                return await Fail();
            }
            catch (Exception)
            {
                return 2;
            }
        }

        // A plain (non-async) catch (Exception) over a synchronous throw.
        private static int PlainCatch()
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception)
            {
                return 3;
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(CatchInside().Result);  // 1
            Console.WriteLine(CatchGeneral().Result); // 2
            Console.WriteLine(PlainCatch());          // 3
        }
    }
}
