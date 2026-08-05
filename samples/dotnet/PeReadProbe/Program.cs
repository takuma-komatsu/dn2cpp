using System;
using System.IO;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Globalization;

class Program
{
    static int Main(string[] args)
    {
        // Gate output must not depend on the host locale.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        string path = args.Length > 0 ? args[0] : "self";
        byte[] b = File.ReadAllBytes(path);
        Console.WriteLine($"len={b.Length} b0={b[0]:X2} b1={b[1]:X2} b2={b[2]:X2} b3={b[3]:X2}");
        var ia = b.ToImmutableArray();
        Console.WriteLine($"ia.Length={ia.Length} ia0={ia[0]:X2}");
        int lfanewBC = BitConverter.ToInt32(b, 0x3C);
        ushort u16BC = BitConverter.ToUInt16(b, 0);
        Console.WriteLine($"lfanew(BitConverter)=0x{lfanewBC:X} u16@0(BitConverter)=0x{u16BC:X}");
        int lfanewU = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>(ref b[0x3C]);
        uint peSig = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>(ref b[lfanewBC]);
        Console.WriteLine($"lfanew(Unsafe)=0x{lfanewU:X} peSig@lfanew=0x{peSig:X} (expect 0x4550)");
        var h = System.Runtime.InteropServices.GCHandle.Alloc(b, System.Runtime.InteropServices.GCHandleType.Pinned);
        unsafe
        {
            byte* p = (byte*)h.AddrOfPinnedObject();
            uint pinSig = *(uint*)(p + lfanewBC);
            Console.WriteLine($"pin b0={p[0]:X2} b1={p[1]:X2} b2={p[2]:X2} pinSig@lfanew=0x{pinSig:X} (expect 0x4550)");
        }
        h.Free();
        // Same path through ImmutableArray's backing array, which is what PEReader pins.
        var hia = System.Runtime.InteropServices.GCHandle.Alloc(System.Runtime.InteropServices.ImmutableCollectionsMarshal.AsArray(ia), System.Runtime.InteropServices.GCHandleType.Pinned);
        unsafe
        {
            byte* p = (byte*)hia.AddrOfPinnedObject();
            uint pinSig = *(uint*)(p + lfanewBC);
            Console.WriteLine($"iaPin b0={p[0]:X2} pinSig@lfanew=0x{pinSig:X} (expect 0x4550)");
        }
        hia.Free();
        // --- exception isinst / catch-filter correctness ---
        string caughtBy = "none";
        try { throw new BadImageFormatException("t"); }
        catch (NotSupportedException) { caughtBy = "NotSupportedException(WRONG)"; }
        catch (BadImageFormatException) { caughtBy = "BadImageFormatException(OK)"; }
        catch (Exception) { caughtBy = "Exception"; }
        object o = new BadImageFormatException("x");
        Console.WriteLine($"caughtBy={caughtBy} | is NSE={o is NotSupportedException}(exp F) is BIFE={o is BadImageFormatException}(exp T) is Exc={o is Exception}(exp T)");
        // general (non-exception) isinst
        object s = "hello"; object n = 42;
        Console.WriteLine($"\"hello\" is string={s is string}(exp T) is FormatException={s is FormatException}(exp F) | 42 is int={n is int}(exp T)");
        try
        {
            var pe = new PEReader(ia);
            var mr = pe.GetMetadataReader();
            Console.WriteLine($"typeDefs={mr.TypeDefinitions.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PEReader threw: {ex.GetType().Name}: {ex.Message}");
        }
        return 0;
    }
}
