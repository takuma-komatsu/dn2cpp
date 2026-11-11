using System;
using System.Globalization;
using System.Runtime.CompilerServices;

internal static unsafe class Program
{
    private delegate nint SignedMultiply(nint left, nint right, out nint low);
    private delegate nuint UnsignedMultiply(nuint left, nuint right, out nuint low);

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nint Signed(int route, nint left, nint right, out nint low)
    {
        if (route == 0) return nint.BigMul(left, right, out low);
        if (route == 1)
        {
            delegate* managed<nint, nint, out nint, nint> multiply = &nint.BigMul;
            return multiply(left, right, out low);
        }
        SignedMultiply call = nint.BigMul;
        return call(left, right, out low);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static nuint Unsigned(int route, nuint left, nuint right, out nuint low)
    {
        if (route == 0) return nuint.BigMul(left, right, out low);
        if (route == 1)
        {
            delegate* managed<nuint, nuint, out nuint, nuint> multiply = &nuint.BigMul;
            return multiply(left, right, out low);
        }
        UnsignedMultiply call = nuint.BigMul;
        return call(left, right, out low);
    }

    private static void SignedCase(int route, nint left, nint right)
    {
        // Widen fixed-width operands, not native operands, for the target32 oracle.
        long product = (long)(int)left * (int)right;
        int upper = (int)(product >> 32), lower = unchecked((int)product);
        nint before = unchecked((int)0x87654321), after = 0x76543210;
        nint* slots = stackalloc nint[3];
        slots[0] = before;
        slots[1] = 42;
        slots[2] = after;
        nint high = Signed(route, left, right, out slots[1]);
        Require((int)high == upper && (int)slots[1] == lower, "signed product");
        Require(slots[0] == before && slots[2] == after, "signed canary");
        Console.WriteLine("native32:s:" + route + ":" + (int)left + ":" + (int)right + ":" + (int)high + ":" + (int)slots[1]);

        nint alias = left;
        high = Signed(route, alias, right, out alias);
        Require((int)high == upper && (int)alias == lower, "signed alias left");
        alias = right;
        high = Signed(route, left, alias, out alias);
        Require((int)high == upper && (int)alias == lower, "signed alias right");
        product = (long)(int)left * (int)left;
        alias = left;
        high = Signed(route, alias, alias, out alias);
        Require((int)high == (int)(product >> 32) && (int)alias == unchecked((int)product), "signed alias both");
    }

    private static void UnsignedCase(int route, nuint left, nuint right)
    {
        ulong product = (ulong)(uint)left * (uint)right;
        uint upper = (uint)(product >> 32), lower = unchecked((uint)product);
        // Both sides remain native-width storage even when the high bit is set.
        nuint before = 0xfedcba98u, after = 0x89abcdefu;
        nuint* slots = stackalloc nuint[3];
        slots[0] = before;
        slots[1] = 42;
        slots[2] = after;
        nuint high = Unsigned(route, left, right, out slots[1]);
        Require((uint)high == upper && (uint)slots[1] == lower, "unsigned product");
        Require(slots[0] == before && slots[2] == after, "unsigned canary");
        Console.WriteLine("native32:u:" + route + ":" + (uint)left + ":" + (uint)right + ":" + (uint)high + ":" + (uint)slots[1]);

        nuint alias = left;
        high = Unsigned(route, alias, right, out alias);
        Require((uint)high == upper && (uint)alias == lower, "unsigned alias left");
        alias = right;
        high = Unsigned(route, left, alias, out alias);
        Require((uint)high == upper && (uint)alias == lower, "unsigned alias right");
        product = (ulong)(uint)left * (uint)left;
        alias = left;
        high = Unsigned(route, alias, alias, out alias);
        Require((uint)high == (uint)(product >> 32) && (uint)alias == unchecked((uint)product), "unsigned alias both");
    }

    private static void NullOutputs(int route)
    {
        bool signed = false, unsigned = false;
        ref nint signedLow = ref Unsafe.NullRef<nint>();
        ref nuint unsignedLow = ref Unsafe.NullRef<nuint>();
        try { Signed(route, -3, 7, out signedLow); }
        catch (NullReferenceException) { signed = true; }
        try { Unsigned(route, 3, 7, out unsignedLow); }
        catch (NullReferenceException) { unsigned = true; }
        Require(signed && unsigned, "native null output route " + route);
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Require(sizeof(nint) == 4 && sizeof(nuint) == 4 && IntPtr.Size == 4 && UIntPtr.Size == 4, "wasm native width");
        Console.WriteLine("primitive native width=4");
        Dn2Cpp.PrimitiveBigMulSubset.Run();
        int[] signed = { 0, 1, -1, int.MinValue, int.MaxValue, -123456789, 987654321 };
        uint[] unsigned = { 0, 1, uint.MaxValue, 0x80000000u, 0x7fffffffu, 0x12345678u, 0xfedcba98u };
        for (int route = 0; route < 3; route++)
        {
            foreach (int left in signed)
                foreach (int right in signed)
                    SignedCase(route, left, right);
            foreach (uint left in unsigned)
                foreach (uint right in unsigned)
                    UnsignedCase(route, left, right);
            NullOutputs(route);
            Console.WriteLine("native32:route" + route + ":canary/alias/null=True");
        }
        Console.WriteLine("primitive native32 BigMul end");
    }
}
