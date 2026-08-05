#nullable enable
using System;

// Raw pointer comparison against null and against another pointer, over both
// codegen paths: the value-producing `ceq`/`cgt.un`/`clt.un` and the branch
// `beq`/`bne.un`/`b*.un` a loop condition lowers to. Equality compares as void*;
// ordering compares as uintptr_t, so a 64-bit address is never truncated.
namespace PointerCompareSubset;

unsafe class Program
{
    // The guard form, which lowers to brfalse rather than ceq.
    static int FirstOrMinusOne(int* p)
    {
        if (p == null)
            return -1;
        return *p;
    }

    internal static void __GateEntry()
    {
        // 1. Against null, in expression position (ceq).
        int* np = null;
        Console.WriteLine(np == null ? "null" : "notnull");   // null
        int x = 42;
        int* xp = &x;
        Console.WriteLine(xp == null ? "null" : "notnull");   // notnull
        Console.WriteLine(xp != null ? "valid" : "invalid");  // valid

        // 2. The guard form (brfalse), over both inputs.
        Console.WriteLine(FirstOrMinusOne(np));               // -1
        Console.WriteLine(FirstOrMinusOne(xp));               // 42

        // 3. Pointer walk: `q < end` is a blt.un/bge.un branch, so both operands
        //    must compare at full width.
        int* buf = stackalloc int[5];
        for (int i = 0; i < 5; i++)
            buf[i] = (i + 1) * 10;
        int* end = buf + 5;
        int sum = 0;
        for (int* q = buf; q < end; q++)
            sum += *q;
        Console.WriteLine(sum);                               // 150

        // 4. Pointer vs pointer, in expression position.
        int* a = buf;
        int* b = buf + 2;
        Console.WriteLine(a == b ? "eq" : "ne");              // ne
        Console.WriteLine(a == buf ? "eq" : "ne");            // eq
        Console.WriteLine(b > a ? "gt" : "le");               // gt  (cgt.un)
        Console.WriteLine(a < b ? "lt" : "ge");               // lt  (clt.un)

        // 5. byte* walk with a `!=` condition (bne.un), at sub-int element size.
        byte* bp = stackalloc byte[4];
        bp[0] = 1; bp[1] = 2; bp[2] = 3; bp[3] = 4;
        byte* bend = bp + 4;
        int bsum = 0;
        for (byte* r = bp; r != bend; r++)
            bsum += *r;
        Console.WriteLine(bsum);                              // 10

        // 6. ble.un/bge.un, rounding out the operators.
        int le = 0;
        for (int* q = buf; q <= buf + 4; q++)
            le++;
        Console.WriteLine(le);                                // 5
    }
}
