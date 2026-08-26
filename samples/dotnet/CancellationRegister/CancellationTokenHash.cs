using System;
using System.Collections.Generic;
using System.Threading;

namespace CancellationTokenHash
{
    internal sealed record TokenEnvelope(CancellationToken Token);

    internal static class TokenConstrained<T>
        where T : struct, IEquatable<CancellationToken>
    {
        internal static bool Equals(T value, CancellationToken other) => value.Equals(other);
    }

    internal static class Program
    {
        private static int ConstrainedHash<T>(T value) where T : struct => value.GetHashCode();

        private static bool ConstrainedObjectEquals<T>(T value, object other)
            where T : struct => value.Equals(other);

        internal static void __GateEntry()
        {
            using var firstSource = new CancellationTokenSource();
            using var secondSource = new CancellationTokenSource();
            CancellationToken first = firstSource.Token;
            CancellationToken copy = first;
            CancellationToken second = secondSource.Token;
            var comparer = EqualityComparer<CancellationToken>.Default;

            Console.WriteLine("token comparer="
                + $"{comparer.Equals(first, copy)}:{!comparer.Equals(first, second)}"
                + $":{comparer.GetHashCode(first) == comparer.GetHashCode(copy)}"
                + $":{first.GetHashCode() == comparer.GetHashCode(first)}");

            var keys = new Dictionary<CancellationToken, string>
            {
                [first] = "first",
                [second] = "second",
                [default] = "none",
            };
            Console.WriteLine($"token dictionary={keys[copy]}:{keys[second]}:{keys[default]}");

            object boxedFirst = first;
            object boxedCopy = copy;
            object boxedSecond = second;
            Console.WriteLine("token boxed="
                + $"{boxedFirst.Equals(boxedCopy)}:{!boxedFirst.Equals(boxedSecond)}"
                + $":{boxedFirst.GetHashCode() == boxedCopy.GetHashCode()}"
                + $":{first.Equals(boxedCopy)}:{!first.Equals(boxedSecond)}");

            Console.WriteLine("token constrained hash="
                + (ConstrainedHash(first) == ConstrainedHash(copy)));
            Console.WriteLine("token constrained object="
                + $"{ConstrainedObjectEquals(first, boxedCopy)}"
                + $":{!ConstrainedObjectEquals(first, boxedSecond)}");
            Console.WriteLine("token constrained typed="
                + $"{TokenConstrained<CancellationToken>.Equals(first, copy)}"
                + $":{!TokenConstrained<CancellationToken>.Equals(first, second)}");
            var envelope = new TokenEnvelope(first);
            var envelopeCopy = new TokenEnvelope(copy);
            var otherEnvelope = new TokenEnvelope(second);
            Console.WriteLine("token record="
                + $"{envelope == envelopeCopy}:{envelope != otherEnvelope}"
                + $":{envelope.GetHashCode() == envelopeCopy.GetHashCode()}");
        }
    }
}
