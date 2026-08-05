using System;
using System.Runtime.CompilerServices;

// The real System.Enum..cctor under dn2cpp. .NET 10 fills Enum.s_underlyingTypes with 26
// `(RuntimeType)typeof(bool)`-shaped entries — each a `castclass System.RuntimeType` on the
// Type object GetTypeFromHandle returns. Every dn2cpp Type object carries the shared
// &dn2cpp_type_type header, so without the identity routing in MethodCompiler.Tokens.cs
// (ReflectionHandleTypeInfo's System.RuntimeType arm) that castclass is structurally always
// false and the cctor throws InvalidCastException at boot.
//
// The RunClassConstructor call is half the trigger, not decoration: a USER-module call names
// RuntimeHelpers with a MemberRef token, and that scan-side mouth of the reach effect is what
// reaches the real cctor body. Drop it and the __ensure wrapper carries a nullptr body, the
// call silently initializes nothing, and this section stops covering the cast.
//
// The lines after the trigger pin the cctor's consumer surface (underlying-type queries)
// against real .NET.
namespace EnumCctorRuntimeTypeSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            // Runs the REAL System.Enum..cctor (or replays its recorded startup
            // failure — the pre-fix abort site). Real .NET: initializes quietly.
            RuntimeHelpers.RunClassConstructor(typeof(Enum).TypeHandle);
            Console.WriteLine("enum-cctor: ran");

            // The cctor's consumer surface, diffed against real .NET.
            Console.WriteLine($"enum-cctor: {Enum.GetUnderlyingType(typeof(DayOfWeek)).Name}");
            Console.WriteLine($"enum-cctor: {Enum.GetUnderlyingType(typeof(ByteBacked)).Name}");
            Console.WriteLine($"enum-cctor: {typeof(DayOfWeek).GetEnumUnderlyingType().FullName}");
        }

        private enum ByteBacked : byte
        {
            Zero = 0,
            One = 1,
        }
    }
}
