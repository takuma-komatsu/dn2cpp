using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskSchedulerSyncContextSubset;

internal static class Program
{
    private sealed class InstalledContext : SynchronizationContext
    {
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== task scheduler sync context ==");

        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            TaskScheduler.FromCurrentSynchronizationContext();
            Console.WriteLine("null: created");
        }
        catch (Exception e)
        {
            Console.WriteLine("null: " + e.GetType().Name + ": " + e.Message);
        }

        SynchronizationContext.SetSynchronizationContext(new InstalledContext());
        try
        {
            TaskScheduler.FromCurrentSynchronizationContext();
            Console.WriteLine("installed: created");
        }
        catch (Exception e)
        {
            Console.WriteLine("installed: " + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
