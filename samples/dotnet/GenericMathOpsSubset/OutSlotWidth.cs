using System;
using System.Numerics;

namespace GenericMathOutWidth;

// The INumberBase<T> conversions write their `out` through a byref the CALLER
// chooses, so the destination is not always an int32-promoted local slot: `out
// arr[i]` is a packed array element and `out s.Field` is a storage-width struct
// field. Both must receive a storage-width store; a stack-width one carries over
// into the neighbouring element/field. The members are `protected static
// abstract`, so the caller has to be a type deriving from the interface — hence
// the helper interface below, whose base list is the whole reason it exists.
//
// Only the (source, target) pairs real .NET answers `true` for are driven. The
// transpiler's inline lowering always reports success, which is sound where the
// BCL reaches it (CreateChecked tries the other direction on false and dn2cpp
// intercepts CreateChecked itself) but is a real divergence when the member is
// called directly — so a pair whose oracle returns false would pin that
// divergence, not the store width. Do not "complete" the matrix here.
internal interface IConvOut : INumberBase<byte>
{
    // out -> packed array element (ldelema: `uint8_t*` / `int16_t*` / …).
    public static bool ToElemChecked<TFrom, TTo>(TFrom v, TTo[] dst, int i)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        => TTo.TryConvertFromChecked(v, out dst[i]);

    public static bool ToElemSaturating<TFrom, TTo>(TFrom v, TTo[] dst, int i)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        => TTo.TryConvertFromSaturating(v, out dst[i]);

    // The TryConvertTo* direction — the source type answers about the target.
    public static bool FromElemTruncating<TFrom, TTo>(TFrom v, TTo[] dst, int i)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        => TFrom.TryConvertToTruncating(v, out dst[i]);

    // out -> an int32-PROMOTED local slot (ldloca), the other half of the same
    // convention: the storage-width store leaves the slot's upper bytes stale, so
    // a negative sub-word result must still read back sign-extended.
    public static TTo ToLocalChecked<TFrom, TTo>(TFrom v)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
    {
        TTo r;
        TTo.TryConvertFromChecked(v, out r);
        return r;
    }

    // out -> struct field (ldflda: the field's storage width).
    public static bool ToFieldChecked<TFrom, TTo>(TFrom v, ref Quad<TTo> q)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        => TTo.TryConvertFromChecked(v, out q.A);
}

// Four fields, so a stack-width store at &A stays inside the value for every
// sub-word target — the corruption is then observable as B/C/D turning zero
// rather than as an overrun past the struct.
internal struct Quad<T>
{
    public T A;
    public T B;
    public T C;
    public T D;
}

internal static class OutSlotWidth
{
    // Explicit array overload of string.Join over a hand-filled string[]: the
    // params-span form and Array.ConvertAll are both outside the transpiled
    // surface this bucket is allowed to lean on (AGENTS.md, self-host shapes).
    static string Show<T>(T[] a)
    {
        var s = new string[a.Length];
        for (int i = 0; i < a.Length; i++)
            s[i] = a[i].ToString();
        return string.Join(",", s);
    }

    static void ElemFrom<TFrom, TTo>(string tag, TFrom v, TTo fill)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
    {
        var a = new TTo[6];
        for (int i = 0; i < a.Length; i++) a[i] = fill;
        bool ok = IConvOut.ToElemChecked(v, a, 0);
        Console.WriteLine($"{tag} from-checked elem0 ok={ok} [{Show(a)}]");

        for (int i = 0; i < a.Length; i++) a[i] = fill;
        ok = IConvOut.ToElemSaturating(v, a, 2);
        Console.WriteLine($"{tag} from-sat     elem2 ok={ok} [{Show(a)}]");
    }

    static void ElemTo<TFrom, TTo>(string tag, TFrom v, TTo fill)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
    {
        var a = new TTo[6];
        for (int i = 0; i < a.Length; i++) a[i] = fill;
        bool ok = IConvOut.FromElemTruncating(v, a, 1);
        Console.WriteLine($"{tag} to-trunc     elem1 ok={ok} [{Show(a)}]");
    }

    static void FieldFrom<TFrom, TTo>(string tag, TFrom v, TTo fill)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
    {
        var q = new Quad<TTo> { A = fill, B = fill, C = fill, D = fill };
        bool ok = IConvOut.ToFieldChecked(v, ref q);
        Console.WriteLine($"{tag} from-checked field ok={ok} [{q.A},{q.B},{q.C},{q.D}]");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== INumberBase out-slot width (packed element / struct field) ==");
        ElemFrom<int, sbyte>("sbyte <-int ", 7, (sbyte)-100);
        ElemFrom<int, short>("short <-int ", 7, (short)-30000);
        ElemFrom<uint, byte>("byte  <-uint", 7u, (byte)200);
        ElemFrom<uint, ushort>("ushort<-uint", 7u, (ushort)60000);
        ElemFrom<double, short>("short <-dbl ", 7.0, (short)-30000);
        ElemFrom<decimal, byte>("byte  <-dec ", 7m, (byte)200);
        ElemFrom<decimal, ushort>("ushort<-dec ", 7m, (ushort)60000);
        // Not sub-word: the store width already equals the stack width, so these
        // are the control rows — they read identically before and after.
        ElemFrom<long, int>("int   <-long", 7L, -1);
        ElemFrom<int, long>("long  <-int ", 7, -1L);

        ElemTo<int, byte>("byte  <-int ", 7, (byte)200);
        ElemTo<int, ushort>("ushort<-int ", 7, (ushort)60000);
        ElemTo<long, byte>("byte  <-long", 7L, (byte)200);
        ElemTo<int, uint>("uint  <-int ", 7, 4000000000u);

        FieldFrom<int, sbyte>("sbyte <-int ", 7, (sbyte)-100);
        FieldFrom<int, short>("short <-int ", 7, (short)-30000);
        FieldFrom<uint, byte>("byte  <-uint", 7u, (byte)200);
        FieldFrom<uint, ushort>("ushort<-uint", 7u, (ushort)60000);
        FieldFrom<double, short>("short <-dbl ", 7.0, (short)-30000);
        FieldFrom<decimal, ushort>("ushort<-dec ", 7m, (ushort)60000);
        FieldFrom<long, int>("int   <-long", 7L, -1);

        // Negative sub-word results into a promoted local slot: the value read back
        // must be sign-extended, which a storage-width store alone does not leave it.
        Local<int, sbyte>("sbyte <-int ", -100);
        Local<int, short>("short <-int ", -30000);
        Local<double, short>("short <-dbl ", -30000.0);
        Local<long, int>("int   <-long", -100000L);
        // No decimal row here: every decimal -> sub-word pair whose oracle answers
        // true has an UNSIGNED target, so a stale upper byte would read the same.
    }

    static void Local<TFrom, TTo>(string tag, TFrom v)
        where TFrom : INumberBase<TFrom>
        where TTo : INumberBase<TTo>
        => Console.WriteLine($"{tag} from-checked local [{IConvOut.ToLocalChecked<TFrom, TTo>(v)}]");
}
