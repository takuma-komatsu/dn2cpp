using System;
using System.Collections.Generic;

namespace SharedGenericFrame
{
    // A throw from inside a shared-generics CANONICAL body. Box<string> and
    // Box<List<int>> share a layout, so one Box<__Canon>.Poke serves both; a
    // canonical body cannot name its placeholder in a user-facing frame, so it
    // bakes the group's representative — the Ordinal-least name, deterministic
    // whatever order grouping ran in — plus " [shared generic]". Both
    // instantiations therefore render the IDENTICAL leaf frame line.
    //
    // Declared divergence: real .NET names each exact instantiation and carries
    // no such mark.
    internal sealed class Box<T>
    {
        private readonly T _value;

        public Box(T value) { _value = value; }

        // Deliberately T-independent: reading typeof(T) or comparing T values
        // would taint the canonical trial into per-instantiation bodies.
        public void Poke()
        {
            throw new InvalidOperationException("thrown in Box<T>.Poke");
        }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== SharedGenericFrame ==");
            string traceA = null;
            try
            {
                new Box<string>("payload").Poke();
            }
            catch (InvalidOperationException ex)
            {
                traceA = ex.StackTrace;
            }
            Console.WriteLine("Box<string> trace:");
            Console.WriteLine(traceA);

            string traceB = null;
            try
            {
                new Box<List<int>>(new List<int>()).Poke();
            }
            catch (InvalidOperationException ex)
            {
                traceB = ex.StackTrace;
            }
            Console.WriteLine("Box<List<int>> trace:");
            Console.WriteLine(traceB);

            string firstA = traceA.Split('\n')[0];
            string firstB = traceB.Split('\n')[0];
            Console.WriteLine("leaf frame carries shared-generic mark: "
                + firstA.Contains(" [shared generic]"));
            Console.WriteLine("same canonical frame line for both: " + (firstA == firstB));
        }

        internal static void __GateSmoke()
        {
            try
            {
                new Box<string>("payload").Poke();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Box<string> caught: " + ex.Message);
            }
            try
            {
                new Box<List<int>>(new List<int>()).Poke();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Box<List<int>> caught: " + ex.Message);
            }
        }
    }
}
