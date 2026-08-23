using System;
using System.Linq.Expressions;

namespace MemberHandleSubset
{
    internal static class Program
    {
        private static readonly int LocalField = 42;

        internal static void Run()
        {
            Console.WriteLine("== ldtoken member handles ==");
            Probe("memberref-method", () =>
            {
                Expression<Func<string>> value = () => string.Concat("a", "b");
                Console.WriteLine(value.Body.NodeType);
            });
            Probe("methoddef", () =>
            {
                Expression<Func<int>> value = () => LocalMethod();
                Console.WriteLine(value.Body.NodeType);
            });
            Probe("methodspec", () =>
            {
                Expression<Func<int>> value = () => Identity<int>(1);
                Console.WriteLine(value.Body.NodeType);
            });
            Probe("fielddef", () =>
            {
                Expression<Func<int>> value = () => LocalField;
                Console.WriteLine(value.Body.NodeType);
            });
            Probe("memberref-field", () =>
            {
                Expression<Func<string>> value = () => string.Empty;
                Console.WriteLine(value.Body.NodeType);
            });
        }

        private static void Probe(string label, Action build)
        {
            try
            {
                build();
                Console.WriteLine(label + "=built");
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(label + "=" + e.GetType().Name);
            }
        }

        private static int LocalMethod() => 1;

        private static T Identity<T>(T value) => value;
    }
}
