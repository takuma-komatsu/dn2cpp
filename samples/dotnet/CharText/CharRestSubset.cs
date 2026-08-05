#nullable disable
using System;

namespace CharRestSubset
{
    // the remaining System.Char classification members as ASCII/invariant
    // intrinsics, extending — IsControl (C0/DEL), IsPunctuation, IsSymbol
    // (verified ASCII category sets), IsSeparator (space), IsNumber (digits) and
    // GetNumericValue ('0'..'9' -> 0..9, else -1). Higher code points are out of
    // scope (Unicode categories beyond Latin-1 are not modeled).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine(char.IsControl('\t'));        // True
            Console.WriteLine(char.IsControl('A'));         // False
            Console.WriteLine(char.IsPunctuation('!'));     // True
            Console.WriteLine(char.IsPunctuation('-'));     // True
            Console.WriteLine(char.IsPunctuation('$'));     // False ($ is a Symbol)
            Console.WriteLine(char.IsSymbol('+'));          // True
            Console.WriteLine(char.IsSymbol('='));          // True
            Console.WriteLine(char.IsSymbol('!'));          // False
            Console.WriteLine(char.IsSeparator(' '));       // True
            Console.WriteLine(char.IsSeparator('\t'));      // False (tab is Control)
            Console.WriteLine(char.IsNumber('7'));          // True
            Console.WriteLine(char.IsNumber('x'));          // False
            Console.WriteLine(char.GetNumericValue('3'));   // 3
            Console.WriteLine(char.GetNumericValue('Z'));   // -1
        }
    }
}
