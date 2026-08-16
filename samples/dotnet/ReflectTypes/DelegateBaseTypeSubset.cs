using System;

namespace DelegateBaseTypeSubset
{
    internal static class Owner
    {
        internal delegate void Handler(double value);
    }

    internal sealed class GenericOwner<T>
    {
        internal delegate TResult Handler<TResult>(T value);
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== app delegate base types ==");

            Report("nested", (Owner.Handler)(_ => { }));
            Report("nested-generic", (GenericOwner<int>.Handler<string>)(value => value.ToString()));
        }

        private static void Report(string name, object value)
        {
            Type type = value.GetType();
            Console.WriteLine(name
                + " base=" + type.BaseType.FullName
                + " basebase=" + type.BaseType.BaseType.FullName
                + " isDelegate=" + (value is Delegate)
                + " isMulticast=" + (value is MulticastDelegate)
                + " castDelegate=" + (((Delegate)value) is not null)
                + " castMulticast=" + (((MulticastDelegate)value) is not null));
        }
    }
}
