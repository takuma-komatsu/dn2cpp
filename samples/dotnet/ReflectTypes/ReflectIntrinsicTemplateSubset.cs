#nullable disable
using System;
using System.Collections.Concurrent;

// typeof over an INTRINSIC-MAPPED open generic definition (BlockingCollection<T>
// lowers to a runtime type) in a program that also calls MakeGenericType: an
// intrinsic chain level is shape-ineligible as a runtime template, so the
// transpile must complete — not emit a template row naming a type-info nothing
// defines — and minting degrades to the catchable NotSupportedException naming
// the instantiation. Every BlockingCollection<T> is one runtime type, so not even
// an instantiation the program constructs resolves, and the refusal says to
// construct it directly. Real .NET constructs both; the freeze carries the
// divergence.
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
            var named = new BlockingCollection<long> { 7 };
            try
            {
                Type closed = open.MakeGenericType(typeof(long));
                Console.WriteLine("named closed: " + closed.Name + " " + named.Count);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("named closed: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
