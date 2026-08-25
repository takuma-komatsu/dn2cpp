using System;
using System.Threading;

namespace RegistrationToken
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var activeSource = new CancellationTokenSource();
            CancellationTokenRegistration active = activeSource.Token.Register(static () => { });
            Console.WriteLine($"registration active={active.Token == activeSource.Token}");

            active.Dispose();
            Console.WriteLine($"registration disposed={active.Token == activeSource.Token}");

            var firedSource = new CancellationTokenSource();
            CancellationTokenRegistration fired = firedSource.Token.Register(static () => { });
            firedSource.Cancel();
            Console.WriteLine($"registration fired={fired.Token == firedSource.Token}");

            var canceledSource = new CancellationTokenSource();
            canceledSource.Cancel();
            CancellationTokenRegistration immediate = canceledSource.Token.Register(static () => { });
            Console.WriteLine($"registration immediate={immediate.Token == CancellationToken.None}");

            CancellationTokenRegistration empty = default;
            Console.WriteLine($"registration default={empty.Token == CancellationToken.None}");

            CancellationTokenRegistration none = CancellationToken.None.Register(static () => { });
            Console.WriteLine($"registration none={none.Token == CancellationToken.None}");

            var unregisterSource = new CancellationTokenSource();
            CancellationTokenRegistration unregister = unregisterSource.Token.Register(static () => { });
            Console.WriteLine($"registration unregister={unregister.Unregister()}:{unregister.Unregister()}");

            var completedSource = new CancellationTokenSource();
            CancellationTokenRegistration completed = completedSource.Token.Register(static () => { });
            completedSource.Cancel();
            Console.WriteLine($"registration unregister-completed={completed.Unregister()}");

            var equalitySource = new CancellationTokenSource();
            CancellationTokenRegistration first = equalitySource.Token.Register(static () => { });
            CancellationTokenRegistration firstCopy = first;
            CancellationTokenRegistration second = equalitySource.Token.Register(static () => { });
            CancellationTokenRegistration defaultCopy = default;
            Console.WriteLine($"registration equality="
                + $"{first == firstCopy}:{first != second}:{defaultCopy == default}"
                + $":{first.Equals(firstCopy)}:{first.GetHashCode() == firstCopy.GetHashCode()}"
                + $":{first.Equals((object)firstCopy)}:{!first.Equals((object)second)}"
                + $":{!first.Equals(null)}:{!first.Equals("registration")}"
                + $":{defaultCopy.Equals((object)default(CancellationTokenRegistration))}");
        }
    }
}
