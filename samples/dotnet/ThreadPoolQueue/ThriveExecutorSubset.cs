#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// A real game's task executor, transcribed. UserThreadTaskSubset covers the hand-off
// SHAPE; this covers what that leaves out — worker threads that NEVER exit (so the
// live-thread set is permanently non-empty, which is the state a defeated-wait verdict is
// evaluated against), fire-and-forget work watched by a poller, and a RunTasks batch whose
// caller is both runner and waiter over tasks settled by three different principals, driven
// per frame rather than once. Progress is the assertion: a wait that stops being
// satisfiable shows up here as a HANG, which the gate's bounded run catches.
namespace ThriveExecutor;

static class Program
{
    sealed class Executor
    {
        private const int ThreadSleepAfterNoWorkFor = 160;

        private readonly object threadNotifySync = new object();
        private readonly ConcurrentQueue<Task?> queuedTasks = new ConcurrentQueue<Task?>();
        private readonly List<Task> mainThreadTaskStorage = new List<Task>();
        private readonly List<Thread> threads = new List<Thread>();
        private volatile bool running = true;

        public Executor(int parallelTasks)
        {
            for (int i = 0; i < parallelTasks; ++i)
            {
                var thread = new Thread(RunExecutorThread);
                thread.IsBackground = true;
                thread.Name = "TaskThread_" + (i + 1);
                thread.Start();
                threads.Add(thread);
            }
        }

        // Fire-and-forget: nothing ever waits on this task.
        public void AddTask(Task task, bool wakeWorkerThread = true)
        {
            queuedTasks.Enqueue(task);

            if (wakeWorkerThread)
            {
                lock (threadNotifySync)
                    Monitor.Pulse(threadNotifySync);
            }
        }

        // Queue all but the first, run the first inline on THIS thread, then wait for all.
        public void RunTasks(List<Task> tasks)
        {
            Task? firstTask = null;

            foreach (var task in tasks)
            {
                if (firstTask != null)
                {
                    AddTask(task, false);
                }
                else
                {
                    firstTask = task;
                }

                mainThreadTaskStorage.Add(task);
            }

            if (firstTask == null)
                return;

            lock (threadNotifySync)
                Monitor.PulseAll(threadNotifySync);

            // The caller is a runner before it is a waiter.
            firstTask.RunSynchronously();

            foreach (var task in mainThreadTaskStorage)
                task.Wait();

            mainThreadTaskStorage.Clear();
        }

        // The same batch-run with the pending list per CALL, so two threads can be inside
        // it at once. The original keeps that list in an instance field, which is the
        // caller's own race; what is asserted here is what a CORRECT caller is owed — two
        // threads each a runner and a waiter, over batches settled by each other's workers.
        public void RunTasksReentrant(List<Task> tasks)
        {
            var pending = new List<Task>();
            Task? firstTask = null;

            foreach (var task in tasks)
            {
                if (firstTask != null)
                {
                    AddTask(task, false);
                }
                else
                {
                    firstTask = task;
                }

                pending.Add(task);
            }

            if (firstTask == null)
                return;

            lock (threadNotifySync)
                Monitor.PulseAll(threadNotifySync);

            firstTask.RunSynchronously();

            foreach (var task in pending)
                task.Wait();
        }

        public void Quit()
        {
            running = false;
            for (int i = 0; i < threads.Count; ++i)
                queuedTasks.Enqueue(null); // the Quit command

            lock (threadNotifySync)
                Monitor.PulseAll(threadNotifySync);

            foreach (var t in threads)
                t.Join();
        }

        private void RunExecutorThread()
        {
            int noWorkCounter = 0;

            while (running)
            {
                if (noWorkCounter > ThreadSleepAfterNoWorkFor)
                {
                    lock (threadNotifySync)
                        Monitor.Wait(threadNotifySync, 10);
                }

                if (queuedTasks.TryDequeue(out Task? command))
                {
                    if (command is null)
                        return; // Quit

                    command.RunSynchronously();
                    noWorkCounter = 0;
                }
                else
                {
                    ++noWorkCounter;
                }
            }
        }
    }

    // A fire-and-forget load, watched by a poller.
    static int s_loadDone;
    static int s_loadValue;

    static int Triangle(int n)
    {
        int s = 0;
        for (int i = 1; i <= n; i++)
            s += i;
        return s;
    }

    public static void __GateEntry()
    {
        // 4 is enough to have both a runner and idle waiters.
        var executor = new Executor(4);

        // 1. Fire-and-forget: nobody waits on this task at all; the frame loop below polls
        //    the flag it sets.
        executor.AddTask(new Task(() =>
        {
            s_loadValue = Triangle(1000);
            Volatile.Write(ref s_loadDone, 1);
        }));

        // 2. The per-frame driver: each frame runs a RunTasks batch and polls the
        //    fire-and-forget flag. Progress must continue across frames, not just the first.
        const int frames = 200;
        int accumulated = 0;
        int loadSeenOnFrame = -1;

        for (int frame = 0; frame < frames; ++frame)
        {
            var batch = new List<Task>();
            int[] slots = new int[4];
            for (int i = 0; i < 4; ++i)
            {
                int k = i;
                batch.Add(new Task(() => slots[k] = (k + 1) * (frame + 1)));
            }

            executor.RunTasks(batch);

            for (int i = 0; i < 4; ++i)
                accumulated += slots[i];

            if (loadSeenOnFrame < 0 && Volatile.Read(ref s_loadDone) == 1)
                loadSeenOnFrame = frame;
        }

        // 3. Three principals at once: one inline on this thread, one on an executor
        //    worker, one on the real pool.
        var mixed = new List<Task>();
        int inlineHit = 0;
        int workerHit = 0;
        mixed.Add(new Task(() => inlineHit = 1));
        mixed.Add(new Task(() => workerHit = 1));
        var poolTask = Task.Run(() => Triangle(50));
        executor.RunTasks(mixed);
        int poolValue = poolTask.Result;

        // 4. Two threads inside the batch-run AT ONCE, each a runner and a waiter: the case
        //    where a waiter is itself a settling principal for somebody else's wait.
        int[] aSlots = new int[3];
        int[] bSlots = new int[3];
        var second = new Thread(() =>
        {
            for (int round = 0; round < 20; ++round)
            {
                var batch = new List<Task>();
                for (int i = 0; i < 3; ++i)
                {
                    int k = i;
                    batch.Add(new Task(() => bSlots[k] += k + 1));
                }

                executor.RunTasksReentrant(batch);
            }
        });
        second.Start();
        for (int round = 0; round < 20; ++round)
        {
            var batch = new List<Task>();
            for (int i = 0; i < 3; ++i)
            {
                int k = i;
                batch.Add(new Task(() => aSlots[k] += k + 1));
            }

            executor.RunTasksReentrant(batch);
        }

        second.Join();

        int aTotal = 0;
        int bTotal = 0;
        for (int i = 0; i < 3; ++i)
        {
            aTotal += aSlots[i];
            bTotal += bSlots[i];
        }

        // Each round adds 1+2+3 == 6, twenty rounds each == 120 per side.
        Console.WriteLine("thrive-exec concurrent: " + aTotal + " " + bTotal);

        executor.Quit();

        // 10 * (1+2+...+200) == 10 * 20100 == 201000
        Console.WriteLine("thrive-exec frames: " + accumulated);
        Console.WriteLine("thrive-exec fireforget: " + Volatile.Read(ref s_loadDone) + " " + s_loadValue
            + " seen=" + (loadSeenOnFrame >= 0));
        Console.WriteLine("thrive-exec mixed: " + inlineHit + workerHit + " pool=" + poolValue);
    }
}
