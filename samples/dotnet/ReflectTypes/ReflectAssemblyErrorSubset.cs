using System;
using System.Reflection;

namespace ReflectAssemblyErrorSubset;

internal static class Program
{
    internal static void Run()
    {
        Console.WriteLine("== assembly load invalid name ==");
        void Probe(string label, string name)
        {
            try
            {
                Assembly.Load(name);
                Console.WriteLine(label + ": no exception");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name + " | " + e.Message);
            }
        }
        Probe("blank", " ");
        Probe("tab", "\t");
        Probe("empty", "");
        Console.WriteLine("assembly load invalid name complete");
    }
}
