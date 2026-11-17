#nullable disable
using System;
using System.Collections.Concurrent;

// Runtime-modeled generics retain finite closed identities without runtime templates.
namespace ReflectIntrinsicTemplateSubset
{
    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("-- intrinsic open definition as MakeGenericType input --");
            Type open = typeof(BlockingCollection<>);
            Console.WriteLine("open: " + open.Name);
            try
            {
                Type closed = open.MakeGenericType(typeof(int));
                Console.WriteLine("closed: " + closed.Name);
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("closed: NotSupportedException");
            }
            try
            {
                Type argument = Type.GetType(new string("System.Double".ToCharArray()));
                _ = open.MakeGenericType(argument);
                Console.WriteLine("unnamed runtime type: constructed");
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("unnamed runtime type: " + e.GetType().Name + ": " + e.Message);
            }
            using var named = new BlockingCollection<long> { 7 };
            try
            {
                Type closed = open.MakeGenericType(typeof(long));
                Console.WriteLine("named closed: " + closed.Name + " " + named.Count);
                Console.WriteLine("identity: " + (closed == typeof(BlockingCollection<long>))
                    + " " + (closed == named.GetType())
                    + " " + (closed.GetGenericTypeDefinition() == open)
                    + " " + (closed.GetGenericArguments()[0] == typeof(long)));
                using var local = new System.Threading.ThreadLocal<string>();
                Type localType = typeof(System.Threading.ThreadLocal<>).MakeGenericType(typeof(string));
                Console.WriteLine("thread local identity: " + (localType == local.GetType())
                    + " " + (localType == typeof(System.Threading.ThreadLocal<string>))
                    + " " + (localType.GetGenericTypeDefinition() == typeof(System.Threading.ThreadLocal<>))
                    + " " + (localType.GetGenericArguments()[0] == typeof(string)));
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("named closed: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
