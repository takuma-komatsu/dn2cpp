namespace Dn2Cpp;

/// <summary>A growable little-endian byte buffer for BPI emission (the
/// hand-rolled stand-in for BinaryWriter keeps the converter transpilable by
/// dn2cpp itself).</summary>
internal sealed class LeBuffer
{
    private byte[] _bytes = new byte[256];
    private int _length;

    public int Length => _length;

    public void Byte(byte v)
    {
        if (_length == _bytes.Length)
        {
            var grown = new byte[_bytes.Length * 2];
            Array.Copy(_bytes, grown, _length);
            _bytes = grown;
        }
        _bytes[_length] = v;
        _length++;
    }

    public void U16(uint v)
    {
        Byte((byte)v);
        Byte((byte)(v >> 8));
    }

    public void U32(uint v)
    {
        Byte((byte)v);
        Byte((byte)(v >> 8));
        Byte((byte)(v >> 16));
        Byte((byte)(v >> 24));
    }

    public void U64(ulong v)
    {
        U32((uint)v);
        U32((uint)(v >> 32));
    }

    /// <summary>UTF-8-encodes and appends (hand-rolled — see AbiContract.HashUtf8
    /// for the same self-host rationale). Returns the byte count appended.</summary>
    public int Utf8(string s)
    {
        int before = _length;
        for (int i = 0; i < s.Length; i++)
        {
            int cp = s[i];
            if (cp >= 0xD800 && cp <= 0xDBFF && i + 1 < s.Length
                && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF)
            {
                cp = 0x10000 + ((cp - 0xD800) << 10) + (s[i + 1] - 0xDC00);
                i++;
            }
            if (cp <= 0x7F)
            {
                Byte((byte)cp);
            }
            else if (cp <= 0x7FF)
            {
                Byte((byte)(0xC0 | (cp >> 6)));
                Byte((byte)(0x80 | (cp & 0x3F)));
            }
            else if (cp <= 0xFFFF)
            {
                Byte((byte)(0xE0 | (cp >> 12)));
                Byte((byte)(0x80 | ((cp >> 6) & 0x3F)));
                Byte((byte)(0x80 | (cp & 0x3F)));
            }
            else
            {
                Byte((byte)(0xF0 | (cp >> 18)));
                Byte((byte)(0x80 | ((cp >> 12) & 0x3F)));
                Byte((byte)(0x80 | ((cp >> 6) & 0x3F)));
                Byte((byte)(0x80 | (cp & 0x3F)));
            }
        }
        return _length - before;
    }

    public void Align(int alignment)
    {
        while (_length % alignment != 0)
            Byte(0);
    }

    public void CopyTo(byte[] destination, int offset)
    {
        Array.Copy(_bytes, 0, destination, offset, _length);
    }
}

/// <summary>Assembles a Baked Patch Image blob (docs/BPI-FORMAT.md v1):
/// interned string pools, fixed-layout record tables, pre-resolved code, and
/// the 64-byte header + section table. Only populated sections are emitted.
/// All emission is append-order deterministic.</summary>
internal sealed class BpiWriter
{
    private readonly LeBuffer _strings = new();      // StringPool (1)
    private readonly LeBuffer _imports = new();      // ImportTable (2)
    private readonly LeBuffer _types = new();        // TypeTable (3)
    private readonly LeBuffer _fields = new();       // FieldTable (4)
    private readonly LeBuffer _methods = new();      // MethodTable (5)
    private readonly LeBuffer _code = new();         // CodeSection (6)
    private readonly LeBuffer _eh = new();           // EHTable (7)
    private readonly LeBuffer _vtdesc = new();       // VtableDescTable (8)
    private readonly LeBuffer _localSig = new();     // LocalSigTable (9)
    private readonly LeBuffer _userStrings = new();  // UserStringPool (10)
    private readonly LeBuffer _itfdesc = new();      // ItfDescTable (12)

    private readonly Dictionary<string, uint> _stringOffsets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, uint> _userStringOffsets = new(StringComparer.Ordinal);

    public uint ImportCount { get; private set; }
    public uint TypeCount { get; private set; }
    public uint FieldCount { get; private set; }
    public uint MethodCount { get; private set; }
    public uint InsnCount { get; private set; }
    public uint EHCount { get; private set; }
    public uint LocalSigCount { get; private set; }
    public int CodeByteLength => _code.Length;

    /// <summary>Interns a UTF-8 name into the StringPool; returns its offset.
    /// Entry shape: <c>{ u16 len; u8 utf8[len] }</c>.</summary>
    public uint InternName(string s)
    {
        if (_stringOffsets.TryGetValue(s, out uint existing))
            return existing;
        // The u16 length prefix precedes the bytes, so learn the UTF-8 byte
        // count via a throwaway encode first (no back-patching in LeBuffer).
        var scratch = new LeBuffer();
        int byteLen = scratch.Utf8(s);
        if (byteLen > 0xFFFF)
            throw new NotSupportedException($"BPI string too long ({byteLen} bytes)");
        uint off = (uint)_strings.Length;
        _strings.U16((uint)byteLen);
        _strings.Utf8(s);
        _stringOffsets[s] = off;
        return off;
    }

    /// <summary>Interns a UTF-16 ldstr literal into the UserStringPool; returns
    /// its offset. Entry shape: <c>{ u32 charLen; u16 utf16[charLen]; u16 0 }</c>,
    /// 4-byte-aligned so the runtime can reference the character data in place —
    /// the uncounted trailing NUL satisfies Dn2CppString's chars contract.</summary>
    public uint InternUserString(string s)
    {
        if (_userStringOffsets.TryGetValue(s, out uint existing))
            return existing;
        _userStrings.Align(4);
        uint off = (uint)_userStrings.Length;
        _userStrings.U32((uint)s.Length);
        for (int i = 0; i < s.Length; i++)
            _userStrings.U16(s[i]);
        _userStrings.U16(0);
        _userStringOffsets[s] = off;
        return off;
    }

    /// <summary>Appends an ImportRecord (24 bytes); returns its table index.
    /// For methods, aux1 is the LocalSigTable index of the import's
    /// [return, params...] EntityRef run; for fields, the field type's
    /// EntityRef (docs/BPI-FORMAT.md "ImportTable").</summary>
    public uint AddImport(uint kind, uint nameOff, uint declTypeImport, uint sigShapeOff, uint aux0, uint aux1)
    {
        _imports.U32(kind);
        _imports.U32(nameOff);
        _imports.U32(declTypeImport);
        _imports.U32(sigShapeOff);
        _imports.U32(aux0);
        _imports.U32(aux1);
        return ImportCount++;
    }

    /// <summary>Appends a TypeRecord (32 bytes); returns its table index.
    /// <paramref name="itfDescOff"/> is the ItfDescTable byte offset of this
    /// type's interface-implementation run (0xFFFFFFFF = none); it occupies the
    /// record's former (always-zero) instanceSize word — the loader owns the
    /// numeric layout, so no size is ever baked (docs/BPI-FORMAT.md).</summary>
    public uint AddType(uint nameOff, uint baseRef, uint flags, uint itfDescOff,
        uint fieldStart, uint fieldCount, uint methodStart, uint methodCount, uint vtableDescOff)
    {
        if (fieldStart > 0xFFFF || fieldCount > 0xFFFF)
            throw new NotSupportedException("BPI: FieldTable exceeds the 16-bit fieldStart/fieldCount pack");
        _types.U32(nameOff);
        _types.U32(baseRef);
        _types.U32(flags);
        _types.U32(itfDescOff);
        _types.U32((fieldStart << 16) | (fieldCount & 0xFFFF));
        _types.U32(methodStart);
        _types.U32(methodCount);
        _types.U32(vtableDescOff);
        return TypeCount++;
    }

    /// <summary>Appends a FieldRecord (20 bytes); returns its table index. For
    /// a static field, <paramref name="staticSlot"/> is its image-consecutive
    /// static-storage index and <paramref name="offset"/> is 0
    /// (docs/BPI-FORMAT.md "FieldTable").</summary>
    public uint AddField(uint nameOff, uint typeRef, uint offset, uint flags, uint staticSlot)
    {
        _fields.U32(nameOff);
        _fields.U32(typeRef);
        _fields.U32(offset);
        _fields.U32(flags);
        _fields.U32(staticSlot);
        return FieldCount++;
    }

    /// <summary>Appends a MethodRecord (40 bytes); returns its table index.</summary>
    public uint AddMethod(uint nameOff, uint declTypeIdx, uint sigShapeOff, uint maxStack,
        uint localCount, uint argCount, uint localSigOff, uint codeOff, uint codeInsnCount,
        uint ehStart, uint ehCount, uint vtableSlot)
    {
        _methods.U32(nameOff);
        _methods.U32(declTypeIdx);
        _methods.U32(sigShapeOff);
        _methods.U16(maxStack);
        _methods.U16(localCount);
        _methods.U32(argCount);
        _methods.U32(localSigOff);
        _methods.U32(codeOff);
        _methods.U32(codeInsnCount);
        _methods.U32(ehStart);
        _methods.U16(ehCount);
        _methods.U16(vtableSlot);
        return MethodCount++;
    }

    /// <summary>Appends one pre-resolved instruction (12 bytes).</summary>
    public void AddInsn(uint op, uint pfx, uint a, uint b)
    {
        _code.U16(op);
        _code.U16(pfx);
        _code.U32(a);
        _code.U32(b);
        InsnCount++;
    }

    /// <summary>Appends an EHRecord (28 bytes); returns its table index. All
    /// ranges are half-open instruction-index ranges; <paramref name="catchTypeRef"/>
    /// is an EntityRef for catch clauses and 0xFFFFFFFF otherwise
    /// (docs/BPI-FORMAT.md "EHTable").</summary>
    public uint AddEH(uint kind, uint tryStart, uint tryEnd, uint handlerStart,
        uint handlerEnd, uint filterStart, uint catchTypeRef)
    {
        _eh.U32(kind);
        _eh.U32(tryStart);
        _eh.U32(tryEnd);
        _eh.U32(handlerStart);
        _eh.U32(handlerEnd);
        _eh.U32(filterStart);
        _eh.U32(catchTypeRef);
        return EHCount++;
    }

    /// <summary>Appends one LocalSigTable EntityRef; returns its flat index.</summary>
    public uint AddLocalSig(uint entityRef)
    {
        _localSig.U32(entityRef);
        return LocalSigCount++;
    }

    /// <summary>Appends one VtableDescTable entry —
    /// <c>{ u32 vtableLen; u32 overrideCount; { u32 slot; u32 patchMethodIdx }[] }</c>
    /// — and returns its byte offset (TypeRecord.vtableDescOff).
    /// <paramref name="vtableLen"/> is the base type's total slot count (the
    /// loader's vtable copy length); each override pairs a frozen base slot
    /// with the overriding patch method's MethodTable index
    /// (docs/BPI-FORMAT.md "VtableDescTable").</summary>
    public uint AddVtableDesc(uint vtableLen, IReadOnlyList<(uint Slot, uint MethodIdx)> overrides)
    {
        uint off = (uint)_vtdesc.Length;
        _vtdesc.U32(vtableLen);
        _vtdesc.U32((uint)overrides.Count);
        foreach (var (slot, methodIdx) in overrides)
        {
            _vtdesc.U32(slot);
            _vtdesc.U32(methodIdx);
        }
        return off;
    }

    /// <summary>Appends one ItfDescTable entry —
    /// <c>{ u32 itfCount; { u32 itfImport; u32 fullSlotCount; u32 implCount;
    /// { u32 slot; u32 patchMethodIdx }[implCount] }[itfCount] }</c> — and
    /// returns its byte offset (TypeRecord.itfDescOff). Each interface entry
    /// pairs a base-image interface import with the (slot, implementing patch
    /// method) overrides this type provides; the loader fills the remaining slots
    /// from the base's interface map (docs/BPI-FORMAT.md "ItfDescTable").</summary>
    public uint AddItfDesc(
        IReadOnlyList<(uint ItfImport, uint FullSlotCount, List<(uint Slot, uint MethodIdx)> Impls)> entries)
    {
        uint off = (uint)_itfdesc.Length;
        _itfdesc.U32((uint)entries.Count);
        foreach (var (itfImport, fullSlotCount, impls) in entries)
        {
            _itfdesc.U32(itfImport);
            _itfdesc.U32(fullSlotCount);
            _itfdesc.U32((uint)impls.Count);
            foreach (var (slot, methodIdx) in impls)
            {
                _itfdesc.U32(slot);
                _itfdesc.U32(methodIdx);
            }
        }
        return off;
    }

    /// <summary>Assembles the final blob: header, section table, then each
    /// populated section on a 16-byte boundary. <paramref name="flags"/> is the
    /// header's flags word (bit0 selects the register interpretation of the
    /// CodeSection; 0 = the default stack encoding).</summary>
    public byte[] Finish(ulong baseImageAbiHash, uint entryMethodIdx, uint patchVersion = 0, uint flags = 0)
    {
        // (kind, buffer, record count) for every populated section, in kind order.
        var sections = new List<(uint Kind, LeBuffer Buf, uint Count)>();
        if (_strings.Length > 0)
            sections.Add((1u, _strings, 0u));
        if (ImportCount > 0)
            sections.Add((2u, _imports, ImportCount));
        if (TypeCount > 0)
            sections.Add((3u, _types, TypeCount));
        if (FieldCount > 0)
            sections.Add((4u, _fields, FieldCount));
        if (MethodCount > 0)
            sections.Add((5u, _methods, MethodCount));
        if (InsnCount > 0)
            sections.Add((6u, _code, InsnCount));
        if (EHCount > 0)
            sections.Add((7u, _eh, EHCount));
        if (_vtdesc.Length > 0)
            sections.Add((8u, _vtdesc, 0u));
        if (LocalSigCount > 0)
            sections.Add((9u, _localSig, LocalSigCount));
        if (_userStrings.Length > 0)
            sections.Add((10u, _userStrings, 0u));
        if (_itfdesc.Length > 0)
            sections.Add((12u, _itfdesc, 0u));

        const int headerSize = 64;
        const int sectionEntrySize = 16;
        int sectionTableOff = headerSize;

        // Lay the sections out: each starts on a 16-byte boundary.
        int cursor = sectionTableOff + sections.Count * sectionEntrySize;
        var offsets = new int[sections.Count];
        for (int i = 0; i < sections.Count; i++)
        {
            cursor = (cursor + 15) & ~15;
            offsets[i] = cursor;
            cursor += sections[i].Buf.Length;
        }

        var head = new LeBuffer();
        head.Utf8("DN2BPI");
        head.Byte(0);
        head.Byte(0);
        head.U32(1);                    // formatVersion
        head.U32(flags);                // flags (bit0 = register code format)
        head.U64(baseImageAbiHash);
        head.U32((uint)sections.Count);
        head.U32((uint)sectionTableOff);
        head.U32(TypeCount);
        head.U32(ImportCount);
        head.U32(entryMethodIdx);
        head.U32(patchVersion);         // deployment ordering key (0 = unversioned)
        while (head.Length < headerSize)
            head.Byte(0);

        for (int i = 0; i < sections.Count; i++)
        {
            head.U32(sections[i].Kind);
            head.U32((uint)offsets[i]);
            head.U32((uint)sections[i].Buf.Length);
            head.U32(sections[i].Count);
        }

        var blob = new byte[cursor];
        head.CopyTo(blob, 0);
        for (int i = 0; i < sections.Count; i++)
            sections[i].Buf.CopyTo(blob, offsets[i]);
        return blob;
    }
}
