using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

/// <summary>Builds the base image's canonical ABI contract (see
/// docs/BPI-FORMAT.md, "Definition of baseImageAbiHash") and hashes it with
/// 64-bit FNV-1a. A <c>--hotupdate-base</c> build emits the hash into the base
/// binary as the <c>dn2cpp_base_image_abi_hash</c> constant and writes the
/// <c>base-abi.json</c> sidecar the patch converter stamps into each BPI, so a
/// base rebuild that changes any layout/slot input mechanically invalidates
/// stale patches.</summary>
internal static class AbiContract
{
    /// <summary>Version of the generics-canonicalization policy in effect when
    /// shared generics are enabled, mixed into the contract as its leading
    /// <c>P canon=</c> line (a build with sharing disabled stamps 0). Two base
    /// images that agree on every layout/slot line can still disagree on which
    /// concrete function a bound symbol resolves to once canonical bodies and
    /// per-real forwarders enter the picture, so a BPI stamped against a
    /// non-shared (or differently-canonicalized) base is rejected at load
    /// instead of mis-binding. Bump on any change to the canonicalization
    /// rules (placeholder widths, CnRef collapsing, rgctx shape) shipped
    /// after a released base image.</summary>
    public const int CanonPolicyVersion = 1;

    /// <summary>Version of the field-layout policy the emitter lays types
    /// out under, mixed into the contract's <c>P</c> line as <c>layout=</c>.
    /// The contract's F lines are symbolic (field type names, no numeric
    /// offsets), so a change to how those same declarations map to bytes —
    /// the uniform narrowing of value-type instance fields to their real
    /// storage width — moves every real offset/size while changing no F/L/V
    /// line. Stamping the policy version rejects every BPI converted against a
    /// differently-laid-out base at load instead of mis-binding field offsets.
    /// (CanonPolicyVersion alone cannot carry this: it stamps 0 when sharing is
    /// disabled, and layout policy applies to both modes.) Each value below moved
    /// real offsets without moving a single F/L/V line, which is exactly the case
    /// this key exists for: 0 = int32-promoted sub-word struct fields (never stamped);
    /// 1 = value types packed at real storage width; 2 = an int32 hresult slot added
    /// to the Dn2CppExceptionObject prefix; 3 = a trace slot added to that prefix;
    /// 4 = reference-type instance fields and all static fields narrowed to real
    /// storage width; 5 = <c>System.DateTime</c> repacked from 16 bytes to .NET's 8.
    /// Bump on any future layout-mapping change.</summary>
    public const int LayoutPolicyVersion = 5;

    /// <summary>Serializes canonical contract v1 over the emitted classes. The
    /// serialization is symbolic — type names and base chains, layout
    /// attributes, field declarations in order, vtable slot signatures — not
    /// numeric offsets: the emitter only knows <c>instanceSize</c> as a C++
    /// <c>sizeof</c> expression, and any base change that can move a real
    /// offset/size/slot changes one of these lines. The leading <c>P</c> line
    /// carries <paramref name="canonPolicy"/> (see
    /// <see cref="CanonPolicyVersion"/>) and the field-layout policy
    /// (<see cref="LayoutPolicyVersion"/>).</summary>
    public static string Serialize(IEnumerable<ClassInfo> emitted, int canonPolicy)
    {
        var sb = new StringBuilder();
        sb.Append("P canon=").Append(canonPolicy)
          .Append(" layout=").Append(LayoutPolicyVersion).Append('\n');
        foreach (var c in emitted.OrderBy(CanonicalName, StringComparer.Ordinal))
        {
            sb.Append("T ").Append(CanonicalName(c)).Append(" : ");
            for (var b = c.BaseClass; b is not null; b = b.BaseClass)
            {
                if (b != c.BaseClass)
                    sb.Append(',');
                sb.Append(CanonicalName(b));
            }
            sb.Append('\n');

            sb.Append("L vt=").Append(c.IsValueType ? 1 : 0)
              .Append(" expl=").Append(c.IsExplicitLayout ? 1 : 0)
              .Append(" pack=").Append(c.LayoutPack)
              .Append(" size=").Append(c.LayoutSize)
              .Append(" inline=").Append(c.InlineArrayLength)
              .Append(" enum=").Append(c.IsEnum ? 1 : 0)
              .Append('\n');

            // Declaration order; literals occupy no layout and never bind.
            int declIndex = 0;
            foreach (var f in c.Fields)
            {
                if (f.IsLiteral)
                    continue;
                sb.Append("F ").Append(declIndex).Append(' ').Append(f.Name)
                  .Append(' ').Append(f.Type)
                  .Append(" static=").Append(f.IsStatic ? 1 : 0)
                  .Append(" exploff=").Append(f.ExplicitOffset)
                  .Append(" byval=").Append(f.ByValArraySize)
                  .Append('\n');
                declIndex++;
            }

            for (int slot = 0; slot < c.SlotOwners.Count; slot++)
                sb.Append("V ").Append(slot).Append(' ')
                  .Append(c.SlotOwners[slot].SigKey).Append('\n');

            // I lines: an interface's method slots (its VtableSlot ordering =
            // declaration order), so a patch type can resolve an
            // interface-implementation override to its frozen slot against the
            // manifest — the same freeze the V lines give class vtables. Contract
            // v1 line-form addition (docs/BPI-FORMAT.md); it only affects
            // --hotupdate-base builds, which are the only ones that hash.
            if (c.IsInterface)
                for (int slot = 0; slot < c.Methods.Count; slot++)
                    sb.Append("I ").Append(slot).Append(' ')
                      .Append(c.Methods[slot].SigKey).Append('\n');

            // D line: a delegate's Invoke signature. It is neither a vtable slot
            // nor an interface slot, so a base change to the Invoke shape moves no
            // V/I/F/L line — this line freezes it, so a stale patch that bound a
            // method into the delegate against the old shape is rejected. Only
            // delegate types emit it; a base with no emitted delegates hashes
            // identically (contract v1 line-form addition; --hotupdate-base only).
            if (c.IsDelegate && c.Methods.FirstOrDefault(m => m.Name == "Invoke") is { } inv)
                sb.Append("D ").Append(inv.SigKey).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>The contract's per-type sort/display key: FullName with the
    /// nested-type qualifier prefix, plus the closed type arguments — two
    /// specializations of one definition must serialize as distinct types.</summary>
    private static string CanonicalName(ClassInfo c)
    {
        string name = c.CppNamePrefix + c.FullName;
        if (c.Context.TypeArgs.Length == 0)
            return name;
        var sb = new StringBuilder(name).Append('[');
        for (int i = 0; i < c.Context.TypeArgs.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(c.Context.TypeArgs[i]);
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>The v1 <c>sigShape</c> string of a method signature — its
    /// parameter and return types in <see cref="TypeDesc.ToString"/> rendering,
    /// <c>(paramTypes):retType</c> (i.e. <see cref="MethodInfo.SigKey"/> without
    /// the leading name). It is the method-import overload discriminator + bridge
    /// ABI shape (BPI-FORMAT.md). The single renderer is shared by the converter
    /// (which computes it from the patch IL under a generic context) and the base
    /// emitter (which bakes it into each reflected method row of a
    /// <c>--hotupdate-base</c> build), so the two always agree byte-for-byte —
    /// the loader disambiguates same-<c>(name, arity, static)</c> overloads,
    /// including the several instantiations a generic method emits under one name,
    /// by string-equality on it.</summary>
    public static string SigShape(MethodSignature<TypeDesc> sig)
    {
        var sb = new StringBuilder("(");
        for (int i = 0; i < sig.ParameterTypes.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(sig.ParameterTypes[i]);
        }
        sb.Append("):").Append(sig.ReturnType);
        return sb.ToString();
    }

    /// <summary>The base⇄converter lookup key for a closed generic
    /// instantiation: the arity-stripped open-definition full name plus the
    /// closed type arguments rendered by <see cref="TypeDesc.ToString"/>, e.g.
    /// <c>System.Collections.Generic.List[Int32]</c>. Both sides compute it from
    /// metadata alone — the base build from the emitted specialization, the
    /// patch converter from the patch IL's TypeSpec — so the manifest's
    /// <c>instantiations</c> map (this key → the mangled registry name the base
    /// owns) lets the converter bind a base-image generic type it never
    /// loaded. The rendering is deterministic for non-generic arguments (the
    /// H-08a surface); a nested closed-generic argument would embed the base's
    /// mangled inner name and is a converter-side rejection.</summary>
    public static string InstantiationKey(string openDefFullName, IReadOnlyList<TypeDesc> args)
    {
        var sb = new StringBuilder(openDefFullName).Append('[');
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(args[i]);
        }
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>64-bit FNV-1a over the UTF-8 encoding of <paramref name="text"/>.
    /// The UTF-8 step is hand-rolled so the managed and self-hosted transpilers
    /// hash identically with no encoder dependency.</summary>
    public static ulong HashUtf8(string text)
    {
        ulong h = 14695981039346656037UL;
        for (int i = 0; i < text.Length; i++)
        {
            int cp = text[i];
            if (cp >= 0xD800 && cp <= 0xDBFF && i + 1 < text.Length
                && text[i + 1] >= 0xDC00 && text[i + 1] <= 0xDFFF)
            {
                cp = 0x10000 + ((cp - 0xD800) << 10) + (text[i + 1] - 0xDC00);
                i++;
            }
            if (cp <= 0x7F)
            {
                h = Step(h, (byte)cp);
            }
            else if (cp <= 0x7FF)
            {
                h = Step(h, (byte)(0xC0 | (cp >> 6)));
                h = Step(h, (byte)(0x80 | (cp & 0x3F)));
            }
            else if (cp <= 0xFFFF)
            {
                h = Step(h, (byte)(0xE0 | (cp >> 12)));
                h = Step(h, (byte)(0x80 | ((cp >> 6) & 0x3F)));
                h = Step(h, (byte)(0x80 | (cp & 0x3F)));
            }
            else
            {
                h = Step(h, (byte)(0xF0 | (cp >> 18)));
                h = Step(h, (byte)(0x80 | ((cp >> 12) & 0x3F)));
                h = Step(h, (byte)(0x80 | ((cp >> 6) & 0x3F)));
                h = Step(h, (byte)(0x80 | (cp & 0x3F)));
            }
        }
        return h;
    }

    private static ulong Step(ulong h, byte b) => unchecked((h ^ b) * 1099511628211UL);

    /// <summary>The base-abi.json sidecar body (schema in BPI-FORMAT.md).
    /// Besides the contract hash, it carries the vtable layout of every emitted
    /// class that has a live vtable — per canonical type name, the SigKey of
    /// each slot in order — so the patch converter can resolve a virtual
    /// override to its frozen base-image slot without loading the base
    /// assembly. The hash already covers the same V lines, so a stale manifest
    /// is rejected at load through the BPI's baseImageAbiHash stamp.</summary>
    public static string ManifestJson(ulong hash, IEnumerable<ClassInfo> vtableClasses,
        IEnumerable<ClassInfo> interfaces,
        IEnumerable<(string Key, string Mangled)>? instantiations = null,
        IEnumerable<(string Shim, Compilation.DefaultRefOutcome Outcome)>? defaultRefs = null)
    {
        var sb = new StringBuilder();
        sb.Append("{ \"contractVersion\": 1, \"hash\": \"0x").Append(Hex16(hash)).Append("\",\n");
        sb.Append("  \"vtables\": {");
        bool firstType = true;
        foreach (var c in vtableClasses.OrderBy(CanonicalName, StringComparer.Ordinal))
        {
            if (c.SlotOwners.Count == 0)
                continue;
            sb.Append(firstType ? "\n" : ",\n");
            firstType = false;
            sb.Append("    \"").Append(JsonEscape(CanonicalName(c))).Append("\": [");
            for (int slot = 0; slot < c.SlotOwners.Count; slot++)
            {
                if (slot > 0)
                    sb.Append(", ");
                sb.Append('"').Append(JsonEscape(c.SlotOwners[slot].SigKey)).Append('"');
            }
            sb.Append(']');
        }
        sb.Append("\n  },\n");

        // "interfaces": per emitted interface, its method-slot SigKeys in slot
        // (declaration) order — the same I lines the hash freezes. The patch
        // converter resolves an interface-implementation override to its frozen
        // slot here without loading the base assembly.
        sb.Append("  \"interfaces\": {");
        firstType = true;
        foreach (var c in interfaces.OrderBy(CanonicalName, StringComparer.Ordinal))
        {
            if (c.Methods.Count == 0)
                continue;
            sb.Append(firstType ? "\n" : ",\n");
            firstType = false;
            sb.Append("    \"").Append(JsonEscape(CanonicalName(c))).Append("\": [");
            for (int slot = 0; slot < c.Methods.Count; slot++)
            {
                if (slot > 0)
                    sb.Append(", ");
                sb.Append('"').Append(JsonEscape(c.Methods[slot].SigKey)).Append('"');
            }
            sb.Append(']');
        }
        sb.Append("\n  },\n");

        // "instantiations": every emitted closed generic reference-type
        // instantiation, its base⇄converter lookup key (InstantiationKey) mapped
        // to its mangled registry name (the ClassInfo.FullName the type-name
        // registry is keyed by). The patch converter resolves a base-image
        // generic type it never loaded to this name, and rejects a key the map
        // does not carry as the missing-AOT-instantiation boundary. Only the
        // --hotupdate-base build computes this; normal builds never do.
        sb.Append("  \"instantiations\": {");
        firstType = true;
        var seenInst = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (key, mangled) in (instantiations ?? Enumerable.Empty<(string, string)>())
                     .OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            if (!seenInst.Add(key))
                continue;
            sb.Append(firstType ? "\n" : ",\n");
            firstType = false;
            sb.Append("    \"").Append(JsonEscape(key)).Append("\": \"")
              .Append(JsonEscape(mangled)).Append('"');
        }
        sb.Append("\n  },\n");

        // "defaultRefs": the conditional default-reference verdict the BASE build
        // reached for each shipped shim. The base injects and the converter
        // deliberately does not, so without this record the converter cannot tell
        // that expected asymmetry from a caller who handed --emit-patch a shim the
        // base never carried, whose imports would bind against nothing at load time.
        //
        // NOT hashed, deliberately: the contract hashes the base's EMITTED types —
        // the surface a patch's name-based imports bind against — and an
        // injected-but-unreached shim emits no type, only an assembly-registry row.
        // Hashing the injected set would reject interchangeable base images. Once
        // the shim IS reached its types enter the contract as ordinary T/L/F/V lines
        // and the hash moves on its own, so the dangerous case is already covered.
        sb.Append("  \"defaultRefs\": {");
        firstType = true;
        foreach (var (shim, outcome) in
                 defaultRefs ?? Enumerable.Empty<(string, Compilation.DefaultRefOutcome)>())
        {
            sb.Append(firstType ? "\n" : ",\n");
            firstType = false;
            sb.Append("    \"").Append(JsonEscape(shim)).Append("\": \"")
              .Append(outcome).Append('"');
        }
        sb.Append("\n  }\n}\n");
        return sb.ToString();
    }

    /// <summary>Minimal JSON string escaping for the manifest (type names and
    /// SigKeys contain no control characters).</summary>
    private static string JsonEscape(string s)
    {
        if (s.IndexOf('"') < 0 && s.IndexOf('\\') < 0)
            return s;
        var sb = new StringBuilder(s.Length + 4);
        foreach (char ch in s)
        {
            if (ch == '"' || ch == '\\')
                sb.Append('\\');
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>Fixed-width lowercase hex (16 digits), hand-rolled so the managed
    /// and self-hosted transpilers format identically.</summary>
    public static string Hex16(ulong value)
    {
        var digits = new char[16];
        for (int i = 15; i >= 0; i--)
        {
            int nibble = (int)(value & 0xF);
            digits[i] = (char)(nibble < 10 ? '0' + nibble : 'a' + (nibble - 10));
            value >>= 4;
        }
        return new string(digits);
    }
}
