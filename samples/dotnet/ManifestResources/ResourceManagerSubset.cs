#nullable disable
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

// System.Resources.ResourceManager over the app assembly's own compiled .resx set — the
// half that answers EXACTLY as real .NET does, which is why it lives in the live-diff
// arm. dn2cpp does not transpile ResourceManager: the real lookup orders a culture
// fallback by walking CultureInfo.Parent and probes satellite assemblies, while dn2cpp
// lowers CultureInfo to a headerless number-format pointer with no Parent and has no
// assembly loader. The type is intrinsic instead, reading the embedded .rodata blob
// through a RuntimeResourceSet reader in the C++ runtime.
//
// What this section pins is that the substitute is not merely present but
// INDISTINGUISHABLE on the surface a program actually uses: a hit, a miss, the object
// form, the base name, the two ctors, the invariant-culture overload, and the argument
// shapes. The carve-outs — everything whose honest answer diverges from .NET — are in
// ResourceManagerLimitsSubset, which cannot be live-diffed and is frozen instead.
namespace ResourceManagerSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== resource manager ==");
            Assembly asm = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager("ManifestResources.Strings", asm);

            Console.WriteLine("rm-base " + rm.BaseName);
            Console.WriteLine("rm-hit " + rm.GetString("Greeting"));
            Console.WriteLine("rm-hit2 " + rm.GetString("Farewell"));
            // A missing KEY is null on both sides, never an exception.
            Console.WriteLine("rm-miss " + (rm.GetString("NoSuchKey") ?? "<null>"));
            // The invariant culture is the neutral ask spelled out. It reads the main
            // assembly's own set, which is the right answer with no satellite in the
            // picture — and it is the control for the culture carve-out in the limits
            // section: that one throws, this one must not.
            Console.WriteLine("rm-inv " + rm.GetString("Greeting", CultureInfo.InvariantCulture));
            // GetObject over a String entry is GetString's answer boxed — a managed
            // string already IS the object.
            Console.WriteLine("rm-obj " + Convert.ToString(rm.GetObject("Greeting")));
            Console.WriteLine("rm-obj-miss " + (rm.GetObject("NoSuchKey") == null));

            // ReleaseAllResources drops .NET's cached ResourceSets; dn2cpp caches
            // nothing (the sets are .rodata spans), so the contract is already met and
            // the call is a no-op. Either way the next lookup must still answer.
            rm.ReleaseAllResources();
            Console.WriteLine("rm-after-release " + rm.GetString("Greeting"));

            // new ResourceManager(Type): the base name is the type's FullName, so this
            // marker resolves to the very same "ManifestResources.Strings.resources"
            // set the string ctor above named.
            var byType = new ResourceManager(typeof(ManifestResources.Strings));
            Console.WriteLine("rm-type-base " + byType.BaseName);
            Console.WriteLine("rm-type-hit " + byType.GetString("Greeting"));

            // Argument shapes: a null key is ArgumentNullException on both sides.
            try
            {
                rm.GetString(null);
                Console.WriteLine("rm-null-key no-throw");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("rm-null-key ArgumentNullException");
            }

            // The assembly's DECLARED neutral culture. Program.cs marks this assembly
            // [NeutralResourcesLanguage("en-US", MainAssembly)], and a request naming
            // exactly that culture is answered from the set embedded here — not as a
            // relaxation of the satellite refusal, but because real .NET rewrites that
            // exact ask to the invariant one and probes no satellite for it.
            Console.WriteLine("rm-neutral-lang "
                + rm.GetString("Greeting", new CultureInfo("en-US")));
            // And the same ask spelled in a different case. CultureInfo normalizes it
            // on the real-.NET side and dn2cpp's compare folds ASCII case, so the two
            // agree — which is the point: the compare must not be a raw strcmp against
            // whatever spelling reached it.
            Console.WriteLine("rm-neutral-lang-case "
                + rm.GetString("Farewell", new CultureInfo("EN-us")));

            // GetObject over every primitive ResourceTypeCode plus byte[]. The reads
            // UNBOX rather than ToString the object, which is the stronger
            // assertion: an unbox tests the boxed type-info the reader stamped, so a
            // value decoded correctly into the wrong box still fails here.
            var mixed = new ResourceManager("ManifestResources.Mixed", asm);
            Console.WriteLine("prim-null " + (mixed.GetObject("p-null") == null));
            Console.WriteLine("prim-bool " + (bool)mixed.GetObject("p-bool"));
            Console.WriteLine("prim-char " + (char)mixed.GetObject("p-char"));
            Console.WriteLine("prim-byte " + (byte)mixed.GetObject("p-byte"));
            Console.WriteLine("prim-sbyte " + (sbyte)mixed.GetObject("p-sbyte"));
            Console.WriteLine("prim-int16 " + (short)mixed.GetObject("p-int16"));
            Console.WriteLine("prim-uint16 " + (ushort)mixed.GetObject("p-uint16"));
            Console.WriteLine("prim-int32 " + (int)mixed.GetObject("Answer"));
            Console.WriteLine("prim-uint32 " + (uint)mixed.GetObject("p-uint32"));
            Console.WriteLine("prim-int64 " + (long)mixed.GetObject("p-int64"));
            Console.WriteLine("prim-uint64 " + (ulong)mixed.GetObject("p-uint64"));
            Console.WriteLine("prim-single " + (float)mixed.GetObject("p-single"));
            Console.WriteLine("prim-double " + (double)mixed.GetObject("p-double"));
            Console.WriteLine("prim-decimal " + (decimal)mixed.GetObject("p-decimal"));
            // The boxed type-info's own name, once, so a value that unboxes only
            // because both sides agree on a WRONG type still shows up.
            Console.WriteLine("prim-type " + mixed.GetObject("p-decimal").GetType().FullName
                + " " + mixed.GetObject("p-bytes").GetType().FullName);

            // DateTime is stored as ToBinary(), i.e. ticks with the Kind in the top two
            // bits — so the three Kinds are three different decodes. Ticks are printed
            // rather than the formatted value for Utc/Unspecified; for the LOCAL one,
            // whose stored ticks are UTC and whose reconstruction is a timezone
            // conversion, the round trip back to UTC is what is stable across hosts.
            var dtUtc = (DateTime)mixed.GetObject("p-datetime-utc");
            Console.WriteLine("prim-dt-utc " + dtUtc.Ticks + " " + dtUtc.Kind);
            var dtUns = (DateTime)mixed.GetObject("p-datetime-unspec");
            Console.WriteLine("prim-dt-unspec " + dtUns.Ticks + " " + dtUns.Kind);
            var dtLoc = (DateTime)mixed.GetObject("p-datetime-local");
            Console.WriteLine("prim-dt-local " + dtLoc.ToUniversalTime().Ticks + " " + dtLoc.Kind);
            Console.WriteLine("prim-timespan " + ((TimeSpan)mixed.GetObject("p-timespan")).Ticks);

            var bytes = (byte[])mixed.GetObject("p-bytes");
            Console.WriteLine("prim-bytes " + bytes.Length + " " + bytes[0] + " " + bytes[5]);
            Console.WriteLine("prim-bytes-empty "
                + ((byte[])mixed.GetObject("p-bytes-empty")).Length);

            // GetString over a non-String entry is deliberately NOT asserted here, and
            // the reason is a real-.NET wart worth knowing before "fixing" the model to
            // match it: RuntimeResourceSet caches the objects it has already decoded, so
            // the FIRST GetString over an Int32 entry raises InvalidOperationException
            // ("call GetObject instead") while one issued after a GetObject on the same
            // key returns the cached box and raises InvalidCastException from the
            // `(string)o` cast. Measured both ways. dn2cpp caches nothing, so it cannot
            // have two answers; it raises the uncached one, and the message is pinned in
            // the frozen limits section where no ordering can move it.

            // A missing resource SET is MissingManifestResourceException on both sides.
            // This is the whole point of modeling the type: the catch clause below is the
            // one .NET's documentation tells a caller to write, so it has to match.
            try
            {
                new ResourceManager("ManifestResources.NoSuchSet", asm).GetString("Greeting");
                Console.WriteLine("rm-missing-set no-throw");
            }
            catch (MissingManifestResourceException)
            {
                Console.WriteLine("rm-missing-set MissingManifestResourceException");
            }

            // The sub-word primitives read through the BOX rather than through an unbox.
            // This block must stay LAST in the program: the frozen arms' output has to
            // remain an unchanged prefix of the bucket's.
            //
            // The prim-* block above casts — `(short)mixed.GetObject("p-int16")` — and an
            // unbox is exactly the reader that CANNOT see the defect these lines exist for.
            // dn2cpp's box payload convention is CppTypes.Of-width, so Boolean/Char/SByte/
            // Byte/Int16/UInt16 all occupy an int32 slot and every typed consumer narrows
            // back to the declared width on the way out, silently repairing a payload
            // written too narrow. object.ToString() does not: dn2cpp_object_tostring reads
            // a boxed sub-word primitive as *(const int32_t*)payload, so a box allocated at
            // sizeof(int16_t) reads two stray bytes and a NEGATIVE value comes back as its
            // unsigned widening. p-sbyte (-42) and p-int16 (-1234) are the values that make
            // that visible, which is why the assertion is a ToString and not another cast.
            Console.WriteLine("box-str bool=" + mixed.GetObject("p-bool").ToString()
                + " char=" + mixed.GetObject("p-char").ToString()
                + " byte=" + mixed.GetObject("p-byte").ToString());
            Console.WriteLine("box-str sbyte=" + mixed.GetObject("p-sbyte").ToString()
                + " int16=" + mixed.GetObject("p-int16").ToString()
                + " uint16=" + mixed.GetObject("p-uint16").ToString());
            // The same values through the erased object, where the concatenation calls the
            // virtual ToString rather than the statically bound one.
            object bxS = mixed.GetObject("p-sbyte");
            object bxI = mixed.GetObject("p-int16");
            Console.WriteLine("box-cat " + bxS + " " + bxI);
        }
    }
}

namespace ManifestResources
{
    // Base-name anchor for new ResourceManager(Type) above: the ctor uses
    // typeof(T).FullName, so this type's full name IS "ManifestResources.Strings" and
    // the set it resolves to is "ManifestResources.Strings.resources" — the same one
    // the string-ctor arm reads. No second resx is needed to cover that ctor.
    internal sealed class Strings
    {
    }
}
