using System;
using System.Threading;

namespace RwLockRecursion
{
    // ReaderWriterLockSlim per-thread ownership: the recursion matrix under both
    // LockRecursionPolicy values, the lock-held queries, and release-without-hold — all
    // same-thread by definition, hence single-threaded. Without ownership tracking a
    // forbidden re-entry is a silent hang (the lock waits on itself), so the throw
    // shapes, type and message verbatim, are the assertion.
    internal static class Program
    {
        private static void Try(string label, Action a)
        {
            try
            {
                a();
                Console.WriteLine(label + ": ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name + " | " + e.Message);
            }
        }

        internal static void __GateEntry()
        {
            // -- NoRecursion (the parameterless-ctor default) --
            var rw = new ReaderWriterLockSlim();
            Console.WriteLine("policy default: " + (int)rw.RecursionPolicy);   // 0

            rw.EnterReadLock();
            Console.WriteLine("readheld: " + rw.IsReadLockHeld
                + " " + rw.IsWriteLockHeld + " " + rw.IsUpgradeableReadLockHeld);
            Try("read->read", () => rw.EnterReadLock());
            Try("read->write", () => rw.EnterWriteLock());
            Try("read->upg", () => rw.EnterUpgradeableReadLock());
            // TryEnter* applies the same recursion verdict BEFORE its timeout.
            Try("read->tryread0", () => rw.TryEnterReadLock(0));
            Try("read->trywrite0", () => rw.TryEnterWriteLock(0));
            rw.ExitReadLock();

            rw.EnterWriteLock();
            Console.WriteLine("writeheld: " + rw.IsReadLockHeld
                + " " + rw.IsWriteLockHeld + " " + rw.IsUpgradeableReadLockHeld);
            Try("write->write", () => rw.EnterWriteLock());
            Try("write->read", () => rw.EnterReadLock());
            Try("write->upg", () => rw.EnterUpgradeableReadLock());
            rw.ExitWriteLock();

            rw.EnterUpgradeableReadLock();
            Console.WriteLine("upgheld: " + rw.IsReadLockHeld
                + " " + rw.IsWriteLockHeld + " " + rw.IsUpgradeableReadLockHeld);
            Try("upg->upg", () => rw.EnterUpgradeableReadLock());
            // The upgradeable holder may take the read lock, granted immediately...
            Try("upg->read", () =>
            {
                rw.EnterReadLock();
                Console.WriteLine("  upg holds read: " + rw.IsReadLockHeld
                    + " readers=" + rw.CurrentReadCount);
                rw.ExitReadLock();
            });
            // ... and may upgrade to write, holding both.
            Try("upg->write", () =>
            {
                rw.EnterWriteLock();
                Console.WriteLine("  upgraded: " + rw.IsWriteLockHeld
                    + " " + rw.IsUpgradeableReadLockHeld);
                Try("  upgraded->read", () => rw.EnterReadLock());
                Try("  upgraded->write", () => rw.EnterWriteLock());
                Try("  upgraded->upg", () => rw.EnterUpgradeableReadLock());
                rw.ExitWriteLock();
            });
            rw.ExitUpgradeableReadLock();

            // Release-without-hold throws instead of corrupting the counts.
            var rw2 = new ReaderWriterLockSlim();
            Try("exitread unheld", () => rw2.ExitReadLock());
            Try("exitwrite unheld", () => rw2.ExitWriteLock());
            Try("exitupg unheld", () => rw2.ExitUpgradeableReadLock());
            rw2.EnterReadLock();
            Try("read exitwrite", () => rw2.ExitWriteLock());
            rw2.ExitReadLock();

            // -- SupportsRecursion: same-mode re-entries count and the write holder may
            // take everything, but the two deadlock-prone read->X shapes still throw. --
            var rr = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            Console.WriteLine("policy recursive: " + (int)rr.RecursionPolicy); // 1

            rr.EnterReadLock();
            Try("R read->read", () => { rr.EnterReadLock(); rr.ExitReadLock(); });
            Try("R read->write", () => rr.EnterWriteLock());
            Try("R read->upg", () => rr.EnterUpgradeableReadLock());
            rr.ExitReadLock();

            rr.EnterWriteLock();
            Try("R write->write", () => { rr.EnterWriteLock(); rr.ExitWriteLock(); });
            Try("R write->read", () => { rr.EnterReadLock(); rr.ExitReadLock(); });
            Try("R write->upg", () =>
            {
                rr.EnterUpgradeableReadLock();
                rr.ExitUpgradeableReadLock();
            });
            Console.WriteLine("R writeheld after: " + rr.IsWriteLockHeld
                + " " + rr.IsReadLockHeld + " " + rr.IsUpgradeableReadLockHeld);
            rr.ExitWriteLock();

            rr.EnterUpgradeableReadLock();
            Try("R upg->upg", () =>
            {
                rr.EnterUpgradeableReadLock();
                rr.ExitUpgradeableReadLock();
            });
            Try("R upg->read", () => { rr.EnterReadLock(); rr.ExitReadLock(); });
            Try("R upg->write", () => { rr.EnterWriteLock(); rr.ExitWriteLock(); });
            rr.ExitUpgradeableReadLock();
            Console.WriteLine("R all released: " + rr.IsReadLockHeld
                + " " + rr.IsWriteLockHeld + " " + rr.IsUpgradeableReadLockHeld
                + " " + rr.CurrentReadCount);
        }
    }
}
