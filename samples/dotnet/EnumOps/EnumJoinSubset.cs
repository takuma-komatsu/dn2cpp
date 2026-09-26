using System;
using System.Collections.Generic;
using System.Text;

// string.Join<T> and StringBuilder.AppendJoin<T> over enum elements: each element formats
// by name, as Enum.ToString does — an undefined value as its number, a [Flags] combination
// by its names — at every underlying width, from an array and from a List<T>; a null
// sequence is rejected with .NET's ArgumentNullException.
namespace EnumJoinSubset
{
    internal enum Tone { Low = 1, High = 7 }
    internal enum Small : byte { A = 3, B = 250 }
    internal enum Tiny : sbyte { N = -100, P = 100 }
    internal enum Half : short { N = -30000, P = 30000 }
    internal enum Wide : uint { A = 1, B = uint.MaxValue }
    internal enum Big : long { N = long.MinValue, P = long.MaxValue }
    [Flags] internal enum Perm { None = 0, Read = 1, Write = 2 }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== enum Join ==");
            Console.WriteLine("int: " + string.Join(",", new[] { Tone.Low, Tone.High, (Tone)3 }));
            Console.WriteLine("byte: " + string.Join(",", new[] { Small.B, Small.A }));
            Console.WriteLine("sbyte: " + string.Join(",", new[] { Tiny.N, Tiny.P, (Tiny)(-1) }));
            Console.WriteLine("short: " + string.Join(",", new[] { Half.N, Half.P }));
            Console.WriteLine("uint: " + string.Join(",", new[] { Wide.B, Wide.A }));
            Console.WriteLine("long: " + string.Join(",", new[] { Big.N, Big.P }));
            Console.WriteLine("flags: " + string.Join(" | ", new[] { Perm.Read | Perm.Write, Perm.None }));
            var tones = new List<Tone> { Tone.High, Tone.Low };
            Console.WriteLine("list: " + string.Join(';', tones));
            Console.WriteLine("append: " + new StringBuilder("[").AppendJoin(", ", new List<Small> { Small.A, Small.B }).Append(']'));
            Tone[] none = null;
            try
            {
                Console.WriteLine("null: " + string.Join(",", none));
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("null: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
