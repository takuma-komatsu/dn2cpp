using System;
using R3;

namespace R3CoreSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            SubjectPipeline();
            ErrorFlow();
            ReactivePropertyFlow();
            R3AsyncSubset.Program.__GateEntry();
        }

        private static void SubjectPipeline()
        {
            using var subject = new Subject<int>();
            var filtered = subject
                .Where(static x => (x & 1) == 0)
                .Select(static x => x * 10)
                .Subscribe(static x => Console.WriteLine("[pipeline] " + x));
            using var scanned = subject
                .Scan(0, static (sum, x) => sum + x)
                .Subscribe(static x => Console.WriteLine("[scan] " + x));

            subject.OnNext(1);
            subject.OnNext(2);
            filtered.Dispose();
            subject.OnNext(4);
            subject.OnCompleted();
        }

        private static void ErrorFlow()
        {
            using var resumable = new Subject<int>();
            using var resumed = resumable.Subscribe(
                static x => Console.WriteLine("[resume next] " + x),
                static e => Console.WriteLine("[resume error] " + e.Message),
                static r => Console.WriteLine("[resume completed] " + r.IsSuccess));
            resumable.OnNext(1);
            resumable.OnErrorResume(new InvalidOperationException("keep-going"));
            resumable.OnNext(2);
            resumable.OnCompleted();

            using var failing = new Subject<int>();
            using var failed = failing
                .OnErrorResumeAsFailure()
                .Subscribe(
                    static x => Console.WriteLine("[failure next] " + x),
                    static e => Console.WriteLine("[failure error] " + e.Message),
                    static r => Console.WriteLine("[failure completed] " + r.IsFailure + ":" + r.Exception.Message));
            failing.OnNext(3);
            failing.OnErrorResume(new ArgumentException("stop"));
            failing.OnNext(4);
        }

        private static void ReactivePropertyFlow()
        {
            using var property = new ReactiveProperty<int>(7);
            using var subscription = property.Subscribe(static x => Console.WriteLine("[property] " + x));
            property.Value = 7;
            property.Value = 8;
            property.OnNext(8);
        }
    }
}
