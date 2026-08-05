using System;
using System.Runtime.CompilerServices;
using System.Text;

// An Unsafe intrinsic is the ONLY mouth these structs have into the program, so
// the C++ struct is declared only if the intrinsic asks for the layout; get that
// wrong and the transpile succeeds while the C++ compile fails on an undeclared
// t_<Name>. Two rules keep that visible: the struct must come from the
// referenced BCL (an app-module class is an unconditional emit seed), and each
// section needs its OWN, since one section's fix reaches its struct
// program-wide. TimeSpan/DateTime/DateTimeOffset are already in the reachable
// closure; confirm any replacement with a `t_<Name>` error before adopting it.
namespace UnreachedStructSubset;

class Program
{
    internal static unsafe void __GateEntry()
    {
        // The result is an int, so the sizeof is the only thing naming the struct.
        Console.WriteLine(Unsafe.SizeOf<Rune>());                       // 4

        // A reinterpret's OUTPUT type: the round-trip keeps the Range inside the
        // intrinsics, so no member is called and no slot is typed at it.
        Console.WriteLine(Unsafe.BitCast<Range, long>(Unsafe.BitCast<long, Range>(42L)));   // 42

        byte* buf = stackalloc byte[32];

        // A mint out of a bare address: the ReadUnaligned/BitCast pair is the only
        // mouth, and both name the storage type by value.
        Unsafe.WriteUnaligned(buf, 7);
        Console.WriteLine(Unsafe.BitCast<Index, int>(Unsafe.ReadUnaligned<Index>(buf)));    // 7

        // A width over a bare address: Copy's operands are pointers (AsRef<Guid>
        // needs no definition) but its width is sizeof(Guid), which needs the whole
        // struct — proven by the move being 16 bytes wide.
        Unsafe.WriteUnaligned(buf + 16, 72623859790382856L);
        Unsafe.WriteUnaligned(buf + 24, 1230066625199609624L);
        Unsafe.Copy(buf, ref Unsafe.AsRef<Guid>(buf + 16));
        Console.WriteLine(Unsafe.ReadUnaligned<long>(buf));                                 // 72623859790382856
        Console.WriteLine(Unsafe.ReadUnaligned<long>(buf + 8));                             // 1230066625199609624
    }
}
