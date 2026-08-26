using System;
using System.Threading;

namespace CancellationIntrinsicToString
{
    internal static class Program
    {
        private static string Generic<T>(T value) => value!.ToString()!;

        internal static void __GateEntry()
        {
            var source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            CancellationTokenRegistration registration = token.Register(static () => { });

            Console.WriteLine($"token tostring={token.ToString()}|{(object)token}|{token}");
            Console.WriteLine($"registration tostring={registration.ToString()}|{(object)registration}|{registration}");
            Console.WriteLine($"generic tostring={Generic(token)}|{Generic(registration)}");
        }
    }
}
