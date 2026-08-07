using System.Reflection.Metadata;

namespace Dn2Cpp;

internal enum StackKind
{
    I4,
    I8,
    R8,
    Ref,
    Struct, // value type by value
    Ptr,    // managed pointer (ldloca/ldarga/ldflda)
}

internal static class CppTypes
{
    public static string Of(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive switch
        {
            PrimitiveTypeCode.Void => "void",
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or
            PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or
            PrimitiveTypeCode.Int32 => "int32_t",
            PrimitiveTypeCode.UInt32 => "uint32_t",
            PrimitiveTypeCode.Int64 => "int64_t",
            PrimitiveTypeCode.UInt64 => "uint64_t",
            PrimitiveTypeCode.Single => "float",
            PrimitiveTypeCode.Double => "double",
            PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => "intptr_t",
            PrimitiveTypeCode.String => "Dn2CppString*",
            PrimitiveTypeCode.Object => "Dn2CppObject*",
            _ => throw new NotSupportedException($"Primitive type {t.Primitive} is not supported yet"),
        },
        // Task-family async types lower to a hand-written runtime struct;
        // the marker carries the exact C++ type (pointer-suffixed for the
        // reference forms, e.g. Task<T> -> "Dn2CppTask*").
        TypeKind.Class when t.Class!.IntrinsicCppName is { } icn => icn,
        // The special-type table shared with the External arm below
        // (CoreIntrinsics.SpecialTypeCppName — the entries' rationale lives at the table).
        // Probed after IntrinsicCppName, which must win.
        TypeKind.Class when CoreIntrinsics.SpecialTypeCppName(t.Class!.FullName) is { } stn => stn,
        // Class-arm-scoped, NOT in the shared table: the External arm does not map
        // System.Enum — it throws below — and a shared entry would silently turn that throw
        // into an answer. A System.Enum-typed value (`obj is Enum e`) is a boxed reference to
        // SOME concrete enum, never to System.Enum itself, so its shape is the erased object
        // pointer rather than the t_System_Enum* the class fallback would give. KindOf agrees
        // (StackKind.Ref); both answers rest on System.Enum being modeled as a reference type.
        TypeKind.Class when t.Class!.FullName == "System.Enum" => "Dn2CppObject*",
        // An enum value rides the int32 stack model — except a 64-bit-backed
        // enum (e.g. System.Uri's `Flags : ulong`, whose host-type bits live
        // above bit 31): its values need the full 8-byte slot, or every
        // local/argument store would silently truncate the high bits.
        TypeKind.Class => t.Class!.IsEnum
            ? (t.Class!.EnumUnderlying is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                ? "int64_t" : "int32_t")
            : t.Class!.IsValueType ? t.Class!.CppStructName
            : t.Class!.CppStructPtrName,
        TypeKind.External => t.ExternalName switch
        {
            // The special-type table shared with the Class arm above
            // (CoreIntrinsics.SpecialTypeCppName — the entries' rationale lives at
            // the table). Every arm below is External-arm-scoped: a loaded Class of
            // the same name would answer differently (see the table's doc comment).
            _ when CoreIntrinsics.SpecialTypeCppName(t.ExternalName) is { } stn => stn,
            // A Class-kind System.Object/System.Type never takes these shapes (an
            // `object`/typeof slot decodes as Primitive/intrinsic; a loaded TypeDef
            // of the name would fall through to the transpiled t_* pointer), so the
            // mapping is scoped to the unresolved-TypeRef form.
            "System.Object" => "Dn2CppObject*",
            "System.Type" => "Dn2CppType*",
            // The runtime-raised exception types referenced without the CoreLib
            // IL (typed catch locals, throw temps): the same managed object
            // reference — their shared runtime handles carry the type identity.
            // (A LOADED exception class transpiles normally — Class-arm fallback —
            // which is why this predicate arm is not in the shared table.)
            _ when CoreIntrinsics.RuntimeExceptionTypeInfo(t.ExternalName) is not null
                => "Dn2CppObject*",
            // Any other unloaded BCL exception name (System.SystemException, …): the same
            // managed object reference. Reached when a corelib-less app exception derives
            // from one and a signature names the base. No shared runtime handle exists for
            // these, so a typed catch/isinst of one still fails loudly at token mapping;
            // only the REFERENCE shape is answered here.
            _ when t.ExternalName is { } exn && CoreIntrinsics.IsExternalBclExceptionName(exn)
                => "Dn2CppObject*",
            // ParallelOptions lives in the System.Threading.Tasks.Parallel.dll facade
            // (like ParallelLoopResult in the shared table), so when only CoreLib is
            // referenced its TypeRef resolves to External rather than a loaded Class.
            // It is a runtime object (newobj-intercepted, like Timer/SemaphoreSlim),
            // so the same opaque "Dn2CppObject*" reference shape as those. External-
            // scoped: unlike ParallelLoopResult it has NO IntrinsicCppName, so a
            // loaded Class of the name would render as its transpiled t_* pointer.
            "System.Threading.Tasks.ParallelOptions" => "Dn2CppObject*",
            // ParallelLoopState — the Break/Stop object the runtime passes into the
            // Action<T, ParallelLoopState> body overloads. Same facade-assembly split
            // and same External-only scoping as ParallelOptions above; it has no
            // public constructor (only the runtime ever creates one), so it is never
            // newobj-intercepted — every member is dispatched through runtime helper
            // calls instead of direct field access, hence the same opaque
            // "Dn2CppObject*" shape as ParallelOptions.
            "System.Threading.Tasks.ParallelLoopState" => "Dn2CppObject*",
            // The synchronization primitives that live in the System.Threading.dll facade
            // rather than in CoreLib, for exactly the ParallelOptions reason above: with
            // only CoreLib referenced their TypeRef resolves to External, and they are
            // newobj-intercepted runtime objects (dn2cpp_countdown_new / _barrier_new /
            // _rwlock_new), so the opaque managed reference is the right shape.
            //
            // Only a REAL IL local of the type asks for a rendering — a `new CountdownEvent(2)`
            // consumed inline never materializes one — so without these arms the transpile
            // fails with "External type ... is not supported yet" over code whose ctor
            // intercept, members and IDisposable row all already work.
            "System.Threading.CountdownEvent" => "Dn2CppObject*",
            "System.Threading.Barrier" => "Dn2CppObject*",
            "System.Threading.ReaderWriterLockSlim" => "Dn2CppObject*",
            // Dynamic-code-generation surface types (DynamicMethod, CallSite, ...)
            // referenced without their assembly loaded: every constructor/member is
            // intercepted to a runtime PlatformNotSupportedException throw, so an
            // instance can never exist — the reference shape is a plain opaque
            // object pointer.
            _ when CoreIntrinsics.IsDynamicCodegenType(t.ExternalName ?? "") => "Dn2CppObject*",
            _ => throw new NotSupportedException($"External type {t.ExternalName} is not supported yet"),
        },
        TypeKind.SZArray => ArrayCppType(t.Element!),
        TypeKind.MDArray => "Dn2CppMDArray*",
        // A managed pointer (`ref T`) must stride by T's real storage width, not the
        // int32-promoted stack type — otherwise `ref byte` (e.g. ReadOnlySpan<byte>'s
        // backing reference) becomes int32_t* and indexing advances 4 bytes per
        // element, reading the wrong data. StorageOf matches the packed array storage
        // (byte/sbyte -> 1 byte; everything else equals Of).
        TypeKind.ByRef => StorageOf(t.Element!) + "*",
        // Unmanaged/function pointers are opaque for now: rendered only when a
        // reached member actually uses one (otherwise tree-shaken away). A
        // reached use that needs real pointer semantics is the cue to add an
        // intrinsic in MethodCompiler.
        TypeKind.Pointer => "void*",
        // An unresolved generic parameter (ECMA-335 `!0` / `!!0`) or shared-generics
        // template placeholder has no native storage width — it can only be mapped once
        // bound to a real type argument. Reaching here means an OPEN generic definition
        // (or a placeholder-typed member) leaked into the layout/emit set instead of the
        // closed instantiation that should stand in for it; the reach side minted it,
        // usually by decoding a signature under a context that left the parameter open.
        // The message names both shape and cause, so the leak is chaseable.
        TypeKind.GenericVar or TypeKind.Template => throw new NotSupportedException(
            $"unresolved generic parameter '{t}' reached native type mapping — an open "
            + "generic definition leaked into the emit/layout set (the closed instantiation "
            + "should have been used); this is a reachability/decode bug, not an input one"),
        _ => throw new NotSupportedException(t.ToString()),
    };

    /// <summary>The precise native-ABI C++ type for a P/Invoke parameter/return that
    /// is **blittable** — an integer/floating primitive, <c>IntPtr</c>/<c>UIntPtr</c>,
    /// an unmanaged pointer, or an enum over a blittable underlying — or null when the
    /// type is not blittable (string/array/bool/char/struct/object), which the P/Invoke
    /// lowering rejects as a carve-out.
    ///
    /// Unlike <see cref="Of"/>, sub-word integers keep their real width (<c>byte</c>→
    /// <c>uint8_t</c>, <c>short</c>→<c>int16_t</c>, …) rather than the int32 stack model,
    /// so the <c>extern "C"</c> declaration matches the native callee's signature exactly.
    /// <c>bool</c> and <c>char</c> are intentionally excluded: their marshalling (4-byte
    /// Win32 BOOL vs 1-byte; ANSI/UTF-16 charset) needs explicit marshalling, not a silent default.
    /// <paramref name="isReturn"/> permits <c>void</c> only in return position.</summary>
    public static string? NativeAbiType(TypeDesc t, bool isReturn = false)
    {
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return t.Primitive switch
                {
                    PrimitiveTypeCode.Void => isReturn ? "void" : null,
                    PrimitiveTypeCode.SByte => "int8_t",
                    PrimitiveTypeCode.Byte => "uint8_t",
                    PrimitiveTypeCode.Int16 => "int16_t",
                    PrimitiveTypeCode.UInt16 => "uint16_t",
                    PrimitiveTypeCode.Int32 => "int32_t",
                    PrimitiveTypeCode.UInt32 => "uint32_t",
                    PrimitiveTypeCode.Int64 => "int64_t",
                    PrimitiveTypeCode.UInt64 => "uint64_t",
                    PrimitiveTypeCode.Single => "float",
                    PrimitiveTypeCode.Double => "double",
                    PrimitiveTypeCode.IntPtr => "intptr_t",
                    PrimitiveTypeCode.UIntPtr => "uintptr_t",
                    _ => null, // Boolean / Char / String / Object: carve-out
                };
            case TypeKind.Pointer:
                return "void*";
            // An enum marshals as its underlying integer (which is blittable here).
            case TypeKind.Class when t.Class!.IsEnum:
                return EnumStorage(t.Class!.EnumUnderlying);
            default:
                return null;
        }
    }

    /// <summary>The native-ABI C++ type a P/Invoke parameter/return marshals to in the
    /// <c>extern "C"</c> declaration. Blittable types keep their precise native
    /// width (<see cref="NativeAbiType"/>); a <c>string</c> marshals as <c>void*</c>
    /// (a NUL-terminated buffer — UTF-8 by default); a <b>blittable array</b>
    /// marshals as <c>void*</c> (a pointer to its first element — parameter
    /// position only). The actual managed⇄native conversion is woven at the call site
    /// (MethodCompiler.EmitPInvokeCall). Null = not marshallable (carve-out).</summary>
    public static string? PInvokeNativeType(TypeDesc t, bool isReturn = false, bool charUnicode = false,
        System.Runtime.InteropServices.UnmanagedType? marshalAs = null)
    {
        // An explicit [MarshalAs(UnmanagedType.*)] overrides the native width / shape for
        // this parameter or return, taking precedence over the type + CharSet
        // default below. Only the supported subset yields a non-null type; any other
        // combination returns null so EmitPInvokeCall reports a precise carve-out.
        if (marshalAs is { } u)
            return MarshalAsNativeType(t, isReturn, u);

        return t.IsString ? "void*"
        // A string[] -> char** / char16_t**: an array of pointers, each to a
        // NUL-terminated UTF-8 (default/Ansi) or UTF-16 (CharSet.Unicode) buffer; a null
        // element marshals as a null pointer. Parameter position only (a string-array
        // return is not a marshallable shape). The per-element encode is woven at the
        // call site (EmitPInvokeCall), which also rejects [Out]/[In,Out] write-back as a
        // carve-out — the default direction is [In].
        : !isReturn && t.Kind == TypeKind.SZArray && t.Element!.IsString ? "void**"
        // A blittable array (blittable element type) -> a pointer to element 0. Only in
        // parameter position: a native function returning an array is not a blittable
        // shape (it would return a bare pointer, marshalled as IntPtr) — carve-out. A
        // char[] also qualifies, both charsets (parameter position only): under
        // CharSet.Unicode its packed char16_t storage IS the 2-byte UTF-16
        // buffer .NET marshals, so the data pointer is passed directly like a blittable
        // array; under the default/Ansi CharSet .NET encodes the whole array
        // as a variable-length UTF-8 buffer (NUL-terminated, embedded NULs included) and
        // decodes [In,Out]/[Out] write-backs back into the array — the encode/decode is
        // woven at the call site (EmitPInvokeCall). An array of a blittable value-type
        // struct (Point[]) also marshals as void*, but — unlike a primitive
        // blittable array, which .NET pins and passes by pointer — .NET marshals a struct
        // array BY COPY with [In]/[Out] direction semantics (probe-confirmed); that
        // copy-in / copy-back is woven at the call site (EmitPInvokeCall).
        : !isReturn && t.Kind == TypeKind.SZArray
            && (NativeAbiType(t.Element!) is not null
                || t.Element! is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }
                || IsBlittableStruct(t.Element!))
            ? "void*"
        // A StringBuilder -> void*: a caller-allocated [In,Out] buffer the
        // native fills (the Win32 idiom). Parameter position only — a native function
        // returning a StringBuilder is not a marshallable shape (a return falls through
        // to NativeAbiType, which is null for a class = carve-out). The buffer alloc /
        // content copy / write-back is woven at the call site (EmitPInvokeCall).
        : !isReturn && t.Kind == TypeKind.Class && t.Class?.FullName == "System.Text.StringBuilder" ? "void*"
        // A managed delegate with a blittable Invoke signature -> void* (a native C
        // function pointer). The call site parks the delegate in the persistent,
        // GC-rooted per-delegate-type thunk pool (the same machinery
        // Marshal.GetFunctionPointerForDelegate<T> uses) and passes the stable pool
        // pointer, so the native may STORE it and invoke it after the call returns (the
        // collision-filter / error-callback idiom) — a lifetime the transpiler cannot
        // statically rule out. Parameter position only: a delegate-typed RETURN of a
        // P/Invoke is still a carve-out — a native that hands back a function pointer
        // returns it as IntPtr and the explicit Marshal.GetDelegateForFunctionPointer<T>
        // intrinsic wraps it.
        : !isReturn && IsBlittableCallbackDelegate(t) ? "void*"
        // A byref/[Out] string (out string / ref string) -> void**: the
        // native takes a pointer-to-pointer and writes the allocated string through it.
        // dn2cpp passes &tmp (a void* temp); the decode / conditional-free / store back
        // into the ByRef slot is woven at the call site (EmitPInvokeCall). Parameter
        // position only — a byref return is not a marshallable shape (carve-out).
        : !isReturn && t.Kind == TypeKind.ByRef && t.Element!.IsString ? "void**"
        // A byref/in/out of a blittable type (ref/out/in scalar/enum/IntPtr/struct) ->
        // a pointer to its native-layout storage. .NET pins the managed
        // location and passes its address with no copy, so the native reads/writes it
        // directly and write-backs (out / ref) are automatically visible. The managed
        // byref is already StorageOf(element)* = the native element pointer, so the call
        // site just casts it. Parameter position only (a byref return is a carve-out);
        // ref bool/char are excluded (their width/charset marshalling needs a temp).
        : !isReturn && t.Kind == TypeKind.ByRef && PInvokeByRefNativeType(t.Element!) is { } brn ? brn
        // A ref/out/in of a non-blittable but marshalable value struct (string/bool/
        // blittable/nested-blittable fields) -> a pointer to its native-layout marshalling
        // struct (tn_<Name>). The managed t_<Name> layout differs field for field (a string
        // is a Dn2CppString*, a bool is 1 byte vs the 4-byte BOOL), so the call site
        // marshals in/out around the call instead of pinning the managed storage.
        : !isReturn && t.Kind == TypeKind.ByRef && IsMarshalableNonBlittableStruct(t.Element!)
            ? MarshalStructName(t.Element!.Class!) + "*"
        // A blittable value struct -> its standard-layout C++ struct, passed/returned by
        // value. Both parameter and return position.
        : IsBlittableStruct(t) ? t.Class!.CppStructName
        // A non-blittable but marshalable value struct passed BY VALUE [In] -> its native-
        // layout marshalling struct (tn_<Name>), filled field by field at the call site.
        // Return position is a carve-out (falls through to NativeAbiType = null).
        : !isReturn && IsMarshalableNonBlittableStruct(t) ? MarshalStructName(t.Class!)
        // A bool marshals as a 4-byte Win32 BOOL by default (UnmanagedType.Bool); the
        // 0/1 managed value and the non-zero->true return normalization are
        // woven at the call site (MethodCompiler.EmitPInvokeCall).
        : t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean ? "int32_t"
        // A char marshals as 1 byte under the default/Ansi CharSet (UTF-8 first byte on
        // Unix) or 2 bytes under CharSet.Unicode (raw UTF-16 code unit); the managed⇄
        // native conversion is woven at the call site. charUnicode comes from
        // the importing method's CharSet (the caller passes the same value here and at
        // the call site so the extern decl and the call agree).
        : t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Char ? (charUnicode ? "uint16_t" : "uint8_t")
        : NativeAbiType(t, isReturn);
    }

    /// <summary>The native-ABI type a supported <c>[MarshalAs(UnmanagedType.*)]</c> override
    /// forces, or null when the override is not supported for <paramref name="t"/>
    /// (a carve-out the call site turns into a precise NotSupportedException): a string
    /// encoding (<c>LPStr</c>/<c>LPUTF8Str</c>/<c>LPWStr</c> → a NUL-terminated buffer, so
    /// <c>void*</c> either way; the UTF-8 vs UTF-16 choice is woven at the call site),
    /// <c>LPArray</c> on a blittable-element array (a pointer to element 0, parameter
    /// position only — identical to the default array marshalling), or a bool width
    /// (<c>I1</c>/<c>U1</c> = 1-byte, <c>Bool</c> = the 4-byte default). A width override on
    /// a fixed-size integer/enum is intentionally NOT supported: .NET requires the unmanaged
    /// width to match the managed integer's size (so <c>[MarshalAs(U1)] int</c> is a
    /// <c>MarshalDirectiveException</c>, and a matching width is a redundant no-op), leaving
    /// bool — whose default width genuinely differs — the only useful numeric case.</summary>
    private static string? MarshalAsNativeType(
        TypeDesc t, bool isReturn, System.Runtime.InteropServices.UnmanagedType u)
    {
        if (t.IsString && u is System.Runtime.InteropServices.UnmanagedType.LPStr
            or System.Runtime.InteropServices.UnmanagedType.LPUTF8Str
            or System.Runtime.InteropServices.UnmanagedType.LPWStr)
            return "void*";
        if (!isReturn && t.Kind == TypeKind.SZArray
            && u == System.Runtime.InteropServices.UnmanagedType.LPArray
            && NativeAbiType(t.Element!) is not null)
            return "void*";
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean)
            return BoolMarshalCType(u);
        return null;
    }

    /// <summary>The native C type a bool <c>[MarshalAs]</c> width forces — <c>I1</c>/<c>U1</c>
    /// = a 1-byte C bool, <c>Bool</c> = the 4-byte Win32 BOOL (the default) — or null for any
    /// other <c>UnmanagedType</c> (<c>VariantBool</c> is a COM carve-out). Shared by
    /// the extern declaration (<see cref="MarshalAsNativeType"/>) and the call-site cast.</summary>
    public static string? BoolMarshalCType(System.Runtime.InteropServices.UnmanagedType u) =>
        u switch
        {
            System.Runtime.InteropServices.UnmanagedType.I1 => "int8_t",
            System.Runtime.InteropServices.UnmanagedType.U1 => "uint8_t",
            System.Runtime.InteropServices.UnmanagedType.Bool => "int32_t",
            _ => null,
        };

    /// <summary>The native pointer type a byref/in/out blittable P/Invoke parameter
    /// marshals to: a <c>ref</c>/<c>out</c>/<c>in</c> of a blittable
    /// scalar/enum/<c>IntPtr</c> (-> <c>"&lt;width&gt;*"</c>) or a blittable value struct
    /// (-> <c>"t_&lt;Name&gt;*"</c>). .NET pins the managed location and passes its
    /// address with no copy, so the native reads/writes it directly and any write-back is
    /// automatically visible — no marshalling temp is needed. Returns null when the
    /// element is not byref-marshallable: a <c>string</c> (a pointer-to-pointer),
    /// <c>bool</c>/<c>char</c> (their 4-byte-BOOL / charset width differs from the managed
    /// storage, needing a temp), or a non-blittable struct/object (carve-out).</summary>
    public static string? PInvokeByRefNativeType(TypeDesc element) =>
        NativeAbiType(element) is { } n ? n + "*"
        : IsBlittableStruct(element) ? element.Class!.CppStructName + "*"
        : null;

    /// <summary>The narrow-vs-wide encoding a P/Invoke string parameter/return marshals
    /// with. Ansi = the host default narrow encoding (real .NET's default: the system
    /// ANSI code page + best-fit on Windows, UTF-8 on Unix); Unicode = UTF-16; Utf8 =
    /// explicit UTF-8. The integer values are stable and mirror the runtime's
    /// <c>Dn2CppStrEnc</c> enum, so a call site that passes the selector to a flag-taking
    /// helper (byref string decode) can emit the literal int.</summary>
    public enum PInvokeStrEnc
    {
        Ansi = 0,
        Unicode = 1,
        Utf8 = 2,
    }

    /// <summary>How a string parameter/return marshals: a per-parameter
    /// <c>[MarshalAs]</c> forces the encoding (<c>LPWStr</c> = UTF-16, <c>LPStr</c> =
    /// Ansi, <c>LPUTF8Str</c> = UTF-8), each overriding the importing method's CharSet
    /// (<paramref name="methodUnicode"/>) — which, when no string <c>[MarshalAs]</c> is
    /// present, selects UTF-16 (CharSet.Unicode) or the default Ansi.</summary>
    public static PInvokeStrEnc StringMarshalEncoding(
        System.Runtime.InteropServices.UnmanagedType? marshalAs, bool methodUnicode) =>
        marshalAs switch
        {
            System.Runtime.InteropServices.UnmanagedType.LPWStr => PInvokeStrEnc.Unicode,
            System.Runtime.InteropServices.UnmanagedType.LPStr => PInvokeStrEnc.Ansi,
            System.Runtime.InteropServices.UnmanagedType.LPUTF8Str => PInvokeStrEnc.Utf8,
            _ => methodUnicode ? PInvokeStrEnc.Unicode : PInvokeStrEnc.Ansi,
        };

    /// <summary>True when <paramref name="t"/> is a value-type struct that is blittable
    /// — a standard-layout C++ struct (<c>t_&lt;Name&gt;</c>) whose every instance field
    /// is itself blittable — so it can be passed/returned by value across a P/Invoke
    /// boundary exactly as the equivalent C struct. Excludes enums
    /// (marshalled as their underlying integer instead), intrinsic-modeled value types
    /// (DateTime/TimeSpan/Decimal — not a standard C++ layout), and <c>[InlineArray]</c>
    /// fixed buffers.
    ///
    /// A <c>ref struct</c> (ByRefLike) is NOT excluded on that bit alone: the marshal is a
    /// byref/by-value COPY of the struct's bytes, never a box, so "not heap-storable" says
    /// nothing about interop blittability. It is blittable iff it holds no managed reference
    /// anywhere in its field graph, which the <see cref="TypeDesc.ContainsGcReferences"/>
    /// guard states up front and the field walk below enforces anyway. So Godot's
    /// <c>godot_variant</c> — a <c>ref struct</c> over a union of unmanaged pointers and
    /// value structs — qualifies, while <c>Span&lt;T&gt;</c> does not (its interior
    /// <c>ref T</c> field is a managed pointer). Overlapping <c>[FieldOffset]</c> arms are
    /// fine: a union has no blittability of its own beyond its arms, and the emitted C++
    /// honors the offsets and pins the size with a <c>static_assert</c>. Terminates because
    /// a value type cannot contain itself by value, so the field graph is acyclic.
    ///
    /// <para>A field <c>[MarshalAs]</c> that is not an exact no-op takes the struct OUT of
    /// the blittable set (<see cref="StructFieldDescriptorSupported"/>): real .NET raises
    /// <c>TypeLoadException</c> the moment such a struct crosses a P/Invoke in any position
    /// — by value, byref, or nested inside another struct — while a width-matching
    /// descriptor (<c>[MarshalAs(I4)] int</c>) passes as the no-op it is. The demotion
    /// routes the struct to <see cref="IsMarshalableNonBlittableStruct"/>, whose descriptor
    /// gate refuses the crossing at transpile with the field named, instead of letting it
    /// cross raw and answer where .NET throws.</para></summary>
    public static bool IsBlittableStruct(TypeDesc t)
    {
        if (t.Kind != TypeKind.Class
            || t.Class is not { IsValueType: true, IsEnum: false,
                                InlineArrayLength: 0, IntrinsicCppName: null } cls)
            return false;
        if (cls.IsByRefLike && t.ContainsGcReferences())
            return false;
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            if (NativeAbiType(f.Type) is null && !IsBlittableStruct(f.Type))
                return false;
            if (!StructFieldDescriptorSupported(f))
                return false;
        }
        return true;
    }

    /// <summary>The native marshalling-struct name (<c>tn_&lt;Name&gt;</c>) for a
    /// non-blittable but marshalable value struct — its native-layout counterpart of the
    /// managed <c>t_&lt;Name&gt;</c> (<see cref="ClassInfo.CppStructName"/>). The two
    /// differ field for field (a string field is a managed object pointer in
    /// <c>t_</c> but a NUL-terminated buffer pointer in <c>tn_</c>; a bool is 1 byte in
    /// <c>t_</c> but the 4-byte Win32 BOOL in <c>tn_</c>), so the call site marshals
    /// between them. (Sub-word integers, per the real-storage-width field-layout
    /// policy, are already at their real width in
    /// <c>t_</c>.)</summary>
    public static string MarshalStructName(ClassInfo cls) => "tn_" + cls.CppName;

    /// <summary>The native-ABI C++ type a single field of a P/Invoke-marshalled struct
    /// occupies in its <c>tn_&lt;Name&gt;</c> layout, or null when the field type is not
    /// marshalable (so the struct as a whole is a carve-out): a <c>string</c> -> a
    /// NUL-terminated buffer pointer (<c>void*</c>, the UTF-8 vs UTF-16 choice made by the
    /// struct's CharSet at emit time); a <c>bool</c> -> the 4-byte Win32 BOOL
    /// (<c>int32_t</c>); a blittable scalar/enum/<c>IntPtr</c>/pointer -> its precise
    /// native width (<see cref="NativeAbiType"/>); a nested blittable value struct ->
    /// that struct copied as-is (<c>t_&lt;Nested&gt;</c> — under the real-storage-width
    /// field-layout policy a value type's
    /// managed layout IS the packed native layout, so no per-field check remains). A
    /// <c>char</c> field, an array field, or a nested non-blittable struct returns null. A
    /// <c>[MarshalAs(ByValArray)]</c> inline fixed-length array field is NOT a single native
    /// type (it lays out as <c>&lt;elem&gt;[N]</c>) — it is handled separately via
    /// <see cref="IsByValArrayField"/> / <see cref="ByValArrayElemNative"/>, so this returns
    /// null for it and the field loop branches on the ByValArray case.</summary>
    public static string? PInvokeStructFieldNativeType(TypeDesc ft) =>
        ft.IsString ? "void*"
        : ft.Kind == TypeKind.Primitive && ft.Primitive == PrimitiveTypeCode.Boolean ? "int32_t"
        : NativeAbiType(ft) is { } n ? n
        : IsBlittableStruct(ft) ? ft.Class!.CppStructName
        : null;

    /// <summary>True when a struct field is a <c>[MarshalAs(UnmanagedType.ByValArray,
    /// SizeConst = N)]</c> inline fixed-length array over a blittable scalar/enum element
    /// — so the native struct embeds the elements directly as <c>&lt;elem&gt;[N]</c> while the
    /// managed struct holds a managed array reference. A blittable element has the same
    /// packed stride managed and native, so the marshal in/out is a bounded memcpy. A
    /// ByValArray of a non-blittable element (a <c>string[]</c> or struct array — an inline
    /// array of pointers / nested structs), or a plain array field with no SizeConst, is
    /// excluded — the containing struct is then a precise carve-out.</summary>
    public static bool IsByValArrayField(FieldInfo f) =>
        f.ByValArraySize >= 0
        && f.Type.Kind == TypeKind.SZArray
        && NativeAbiType(f.Type.Element!) is not null;

    /// <summary>The native-ABI element type of a ByValArray struct field — its blittable
    /// scalar/enum element at its precise native width; the native layout is
    /// <c>&lt;this&gt;[f.ByValArraySize]</c>. Caller has confirmed
    /// <see cref="IsByValArrayField"/>.</summary>
    public static string ByValArrayElemNative(FieldInfo f) => NativeAbiType(f.Type.Element!)!;

    /// <summary>Whether a ByValArray field's managed array uses the dedicated 4-byte i4
    /// representation (<c>Dn2CppArrayI4</c>, for int/uint/4-byte-enum elements) rather than
    /// the general <c>Dn2CppArrayN</c> — selects the copy-back allocator. Caller has
    /// confirmed <see cref="IsByValArrayField"/>.</summary>
    public static bool ByValArrayRepIsI4(FieldInfo f) => Of(f.Type) == "Dn2CppArrayI4*";

    /// <summary>True when <paramref name="t"/> is a value-type struct that is NOT blittable
    /// (so the by-value/by-ref blittable path does not apply) yet every instance field is
    /// individually marshalable — a <c>string</c>/<c>bool</c>/blittable-scalar/nested-blittable
    /// field (<see cref="PInvokeStructFieldNativeType"/> non-null) or a blittable-element
    /// <c>[MarshalAs(ByValArray)]</c> inline array (<see cref="IsByValArrayField"/>) —
    /// AND no field carries a <c>[MarshalAs]</c> descriptor the <c>tn_</c> emitter does not
    /// honour (<see cref="StructFieldDescriptorSupported"/>). Such a
    /// struct crosses a P/Invoke boundary through a generated field-by-field marshaller into
    /// its native-layout <c>tn_&lt;Name&gt;</c>. A struct with a non-marshalable field (a
    /// <c>char</c>, a plain/non-ByValArray array, a ByValArray of non-blittable elements, a
    /// nested non-blittable struct) is excluded so the lowering reports a precise carve-out
    /// rather than corrupting the layout — and so is one whose field descriptor the emitter
    /// would silently ignore, since a <c>[MarshalAs(ByValTStr)]</c> string field crossing as
    /// a pointer contradicts the marshalled-layout model
    /// (<c>Compilation.MarshalLayout.cs</c>), which sizes it as an inline buffer. The
    /// descriptor gate turns that into a transpile-time refusal rather than a cross-check
    /// <c>static_assert</c> failing the C++ compile.</summary>
    public static bool IsMarshalableNonBlittableStruct(TypeDesc t)
    {
        if (t.Kind != TypeKind.Class
            || t.Class is not { IsValueType: true, IsEnum: false, IsByRefLike: false,
                                InlineArrayLength: 0, IntrinsicCppName: null } cls)
            return false;
        // A blittable struct is passed/returned directly (the blittable path); only a
        // genuinely non-blittable struct (a string/bool/ByValArray field makes it so)
        // marshals here.
        if (IsBlittableStruct(t))
            return false;
        bool anyField = false;
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            anyField = true;
            if (PInvokeStructFieldNativeType(f.Type) is null && !IsByValArrayField(f))
                return false;
            if (!StructFieldDescriptorSupported(f))
                return false;
        }
        return anyField;
    }

    /// <summary>Whether a struct field's <c>[MarshalAs]</c> descriptor is one the P/Invoke
    /// struct marshaller (<c>CppEmitter.EmitPInvokeMarshalStructs</c>) actually implements —
    /// the field-descriptor half of <see cref="IsMarshalableNonBlittableStruct"/>. The
    /// emitter reads exactly one descriptor kind (<c>ByValArray</c>, via
    /// <see cref="IsByValArrayField"/>) and lays every other field out from its TYPE alone,
    /// so a descriptor is acceptable only when it is that implemented form or an exact
    /// no-op — one naming the form the untouched lowering already emits (the same no-op set
    /// <c>Compilation.MarshalDescribedExtent</c> accepts): <c>Bool</c> on a <c>bool</c> (the
    /// 4-byte default), <c>Struct</c> on a value-struct field, or a width-naming descriptor
    /// equal to the field's natural native width (<c>[MarshalAs(U4)] uint</c>). Everything
    /// else — <c>ByValTStr</c> (an inline character buffer the emitter would pass as a
    /// pointer), <c>I1</c>/<c>U1</c> on a bool (a 1-byte width the emitter would pass as 4),
    /// a string encoding override against the struct CharSet, a ByValArray of non-blittable
    /// elements — takes the struct out of the marshalable set so the P/Invoke lowering
    /// refuses loudly, naming the field (<see cref="StructFieldDescriptorReject"/>), instead
    /// of emitting a layout the descriptor contradicts. Only the boundary crossing refuses:
    /// the marshalled-layout model keeps answering <c>Marshal.SizeOf</c>/<c>OffsetOf</c> for
    /// the shapes it models.
    ///
    /// <para>TWO askers, one predicate: <see cref="IsBlittableStruct"/> asks it too, so a
    /// width-MISMATCHED descriptor on a blittable-typed field (<c>[MarshalAs(I2)] int</c>)
    /// demotes the struct out of the blittable fast path into the same loud refusal — which
    /// is right, because real .NET raises <c>TypeLoadException</c> for such a struct at the
    /// P/Invoke in every position.</para></summary>
    public static bool StructFieldDescriptorSupported(FieldInfo f)
    {
        var u = f.MarshalAs;
        if (u == default)
            return true;
        if (u == System.Runtime.InteropServices.UnmanagedType.ByValArray)
            return IsByValArrayField(f);
        var t = f.Type;
        if (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean)
            return u == System.Runtime.InteropServices.UnmanagedType.Bool;
        if (u == System.Runtime.InteropServices.UnmanagedType.Struct)
            return t.Kind == TypeKind.Class && t.Class is { IsValueType: true };
        int named = u switch
        {
            System.Runtime.InteropServices.UnmanagedType.I1
                or System.Runtime.InteropServices.UnmanagedType.U1 => 1,
            System.Runtime.InteropServices.UnmanagedType.I2
                or System.Runtime.InteropServices.UnmanagedType.U2 => 2,
            System.Runtime.InteropServices.UnmanagedType.I4
                or System.Runtime.InteropServices.UnmanagedType.U4
                or System.Runtime.InteropServices.UnmanagedType.R4 => 4,
            System.Runtime.InteropServices.UnmanagedType.I8
                or System.Runtime.InteropServices.UnmanagedType.U8
                or System.Runtime.InteropServices.UnmanagedType.R8 => 8,
            System.Runtime.InteropServices.UnmanagedType.SysInt
                or System.Runtime.InteropServices.UnmanagedType.SysUInt => 8,
            _ => -1,
        };
        // A pointer-WIDE field takes a pointer-width descriptor and not a fixed-width one:
        // the two coincide at 64 bits, where NativeAbiWidth reads them, so comparing numbers
        // alone would accept [MarshalAs(U8)] IntPtr, which real .NET raises TypeLoadException
        // for. Still laxer than .NET on an unmanaged POINTER field, which takes no descriptor
        // at all — SysInt included — and is a standing over-acceptance, not a rule.
        bool ptrWide = t.Kind == TypeKind.Pointer
            || (t.Kind == TypeKind.Primitive
                && t.Primitive is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr);
        bool namesPtr = u is System.Runtime.InteropServices.UnmanagedType.SysInt
            or System.Runtime.InteropServices.UnmanagedType.SysUInt;
        return named > 0 && NativeAbiWidth(t) == named && ptrWide == namesPtr;
    }

    /// <summary>The byte width of a blittable field type on the native ABI — the numeric
    /// twin of <see cref="NativeAbiType"/>, used only to test whether a width-naming
    /// <c>[MarshalAs]</c> descriptor is the no-op it claims to be. -1 for anything that is
    /// not a blittable scalar/enum/pointer.</summary>
    private static int NativeAbiWidth(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => PrimitiveAbiWidth(t.Primitive),
        TypeKind.Pointer => 8,
        TypeKind.Class when t.Class is { IsEnum: true } ec => PrimitiveAbiWidth(ec.EnumUnderlying),
        _ => -1,
    };

    private static int PrimitiveAbiWidth(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 1,
        PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => 2,
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => 4,
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double => 8,
        PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => 8,
        _ => -1,
    };

    /// <summary>The precise sentence for a struct whose ONLY obstacle to crossing a
    /// P/Invoke boundary is a field descriptor <see cref="StructFieldDescriptorSupported"/>
    /// rejects — null when no field is descriptor-rejected (the caller then falls back to
    /// the generic non-marshalable message). Names the field and the descriptor so the
    /// refusal says what to change. Recurses into nested value-struct fields, because that
    /// is where real .NET's own diagnostic points (its <c>TypeLoadException</c> names the
    /// INNER struct's field); terminates for the acyclicity reason
    /// <see cref="IsBlittableStruct"/> states.</summary>
    public static string? StructFieldDescriptorReject(TypeDesc t)
    {
        if (t.Kind != TypeKind.Class
            || t.Class is not { IsValueType: true, IsEnum: false } cls)
            return null;
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            if (!StructFieldDescriptorSupported(f))
                return $"its field '{f.Name}' carries [MarshalAs(UnmanagedType.{f.MarshalAs})], "
                    + "which the P/Invoke struct marshaller does not implement (deliberate "
                    + "carve-out — the marshalled-layout model still sizes what it models for "
                    + "Marshal.SizeOf/OffsetOf, but a boundary crossing must refuse, not "
                    + "answer; real .NET refuses the mismatched-width forms too, with a "
                    + "TypeLoadException at the call)";
            if (StructFieldDescriptorReject(f.Type) is { } nested)
                return $"its field '{f.Name}' ({f.Type}) is descriptor-rejected: {nested}";
        }
        return null;
    }

    /// <summary>True when <paramref name="t"/> is a delegate type whose <c>Invoke</c>
    /// signature is fully native-marshallable as a native C function pointer — every
    /// parameter and the return passes <see cref="IsCallbackMarshalableType"/>. A shape
    /// that does not (a <c>string</c>/array/<c>char</c>/delegate, or a byref to a
    /// non-blittable struct) is excluded, so the P/Invoke lowering reports a precise
    /// carve-out rather than silently passing an incompatible pointer. The accepted set is
    /// exactly what <c>CppEmitter.EmitMarshalFnPtrThunks</c>'s <c>NativeOf</c> can emit —
    /// classifier and thunk must never disagree.</summary>
    public static bool IsBlittableCallbackDelegate(TypeDesc t)
    {
        if (t.Kind != TypeKind.Class || t.Class is not { IsDelegate: true } cls)
            return false;
        var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
        if (invoke is null)
            return false;
        if (!IsCallbackMarshalableType(invoke.Signature.ReturnType, isReturn: true))
            return false;
        foreach (var p in invoke.Signature.ParameterTypes)
            if (!IsCallbackMarshalableType(p, isReturn: false))
                return false;
        return true;
    }

    /// <summary>Whether a single parameter/return type of a delegate <c>Invoke</c> can
    /// cross a native-callback (reverse-P/Invoke) boundary — the per-position half of
    /// <see cref="IsBlittableCallbackDelegate"/>. Its accepted shapes are precisely those
    /// <c>CppEmitter.EmitMarshalFnPtrThunks</c>'s <c>NativeOf</c> lowers, so the two stay
    /// in lockstep:
    /// <list type="bullet">
    /// <item>a blittable scalar/enum/<c>IntPtr</c>/unmanaged pointer at its precise native
    /// width (<see cref="NativeAbiType"/>; <c>void</c> only in return position);</item>
    /// <item>a blittable value struct passed/returned by value as its standard C++
    /// layout;</item>
    /// <item>a <c>bool</c> — the 4-byte Win32 BOOL, matching dn2cpp's forward-P/Invoke bool
    /// policy and .NET's default delegate bool marshalling; the thunk writes a clean 0/1,
    /// so a native callee reading a 1-byte <c>bool</c> (its low byte) is correct on every
    /// supported ABI;</item>
    /// <item>a byref (<c>ref</c>/<c>in</c>/<c>out</c>) to a blittable scalar or struct, in
    /// PARAMETER position only — a pointer to the pinned native-layout storage. The native
    /// passes the address and the layouts match (blittable), so the thunk forwards it to
    /// the delegate's byref parameter with no marshalling. A byref RETURN, or a byref to a
    /// non-blittable struct, is not a callback shape. This is the
    /// <c>bool(ref BlittableStruct)</c> collision-filter shape (Thrive's
    /// <c>OnCollisionFilterCallback</c>).</item>
    /// </list></summary>
    /// <summary><c>null</c> when <paramref name="t"/> is a blittable callback delegate;
    /// otherwise a precise reason naming the first <c>Invoke</c> parameter/return that
    /// cannot cross a native-callback boundary — so a delegate-typed P/Invoke parameter's
    /// carve-out names the exact offending sub-type (a byref to a non-blittable struct, a
    /// managed object/string/array, a char) rather than the generic "not marshallable".</summary>
    public static string? CallbackDelegateRejectReason(TypeDesc t)
    {
        if (t.Kind != TypeKind.Class || t.Class is not { IsDelegate: true } cls)
            return "not a delegate type";
        var invoke = cls.Methods.FirstOrDefault(m => m.Name == "Invoke");
        if (invoke is null)
            return "the delegate has no Invoke method";
        var rt = invoke.Signature.ReturnType;
        if (!IsCallbackMarshalableType(rt, isReturn: true))
            return $"its Invoke return type {rt} is not callback-marshalable";
        var ps = invoke.Signature.ParameterTypes;
        for (int i = 0; i < ps.Length; i++)
            if (!IsCallbackMarshalableType(ps[i], isReturn: false))
                return $"its Invoke parameter {i} of type {ps[i]} is not callback-marshalable"
                    + (ps[i].Kind == TypeKind.ByRef
                        ? $" (a byref to {(IsBlittableStruct(ps[i].Element!) ? "a blittable" : "the NON-blittable")} type {ps[i].Element})"
                        : "");
        return null;
    }

    public static bool IsCallbackMarshalableType(TypeDesc t, bool isReturn) =>
        NativeAbiType(t, isReturn) is not null
        || IsBlittableStruct(t)
        || (t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.Boolean)
        || (!isReturn && t.Kind == TypeKind.ByRef
            && (NativeAbiType(t.Element!) is not null || IsBlittableStruct(t.Element!)));

    private static string ArrayCppType(TypeDesc element)
    {
        // Must mirror MethodCompiler.RepOf.
        if (KindOf(element) == StackKind.Ref)
            return "Dn2CppArrayRef*";
        if (element.Kind == TypeKind.Primitive
            && element.Primitive is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32)
            return "Dn2CppArrayI4*";
        // An enum's array uses the i4 rep only for a 4-byte underlying (int/uint);
        // a sub-word or 64-bit underlying packs into Dn2CppArrayN at its real width,
        // matching the underlying-width ldelem/stelem opcodes Roslyn emits.
        if (element.Kind == TypeKind.Class && element.Class!.IsEnum)
            return EnumArrayIsI4(element.Class!.EnumUnderlying) ? "Dn2CppArrayI4*" : "Dn2CppArrayN*";
        return "Dn2CppArrayN*";
    }

    /// <summary>An enum's underlying integer storage type for packed-array element
    /// access — its real width, not the int32 stack model. Char is not a
    /// legal enum underlying, so it is not listed.</summary>
    public static string EnumStorage(PrimitiveTypeCode underlying) => underlying switch
    {
        PrimitiveTypeCode.Byte => "uint8_t",
        PrimitiveTypeCode.SByte => "int8_t",
        PrimitiveTypeCode.Int16 => "int16_t",
        PrimitiveTypeCode.UInt16 => "uint16_t",
        PrimitiveTypeCode.UInt32 => "uint32_t",
        PrimitiveTypeCode.Int64 => "int64_t",
        PrimitiveTypeCode.UInt64 => "uint64_t",
        _ => "int32_t",   // Int32 (and any unexpected code) -> the int32 model
    };

    /// <summary>True when an enum's array uses the dedicated 4-byte i4 representation
    /// (Dn2CppArrayI4) — i.e. a 4-byte underlying (int/uint), whose elements Roslyn
    /// accesses with ldelem.i4. Sub-word and 64-bit underlyings pack into
    /// Dn2CppArrayN instead.</summary>
    public static bool EnumArrayIsI4(PrimitiveTypeCode underlying) =>
        underlying is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32;

    /// <summary>The natural in-array storage type for an element. Sub-word
    /// integers (<c>byte</c>/<c>sbyte</c>/<c>short</c>/<c>ushort</c>/<c>char</c>/
    /// <c>bool</c>) pack to their real width instead of the int32 stack-promoted type,
    /// so e.g. <c>char[]</c>/<c>short[]</c> are packed buffers matching the .NET layout
    /// and the InitializeArray RVA blob. For every other type this equals
    /// <see cref="Of"/>, so non-sub-word array codegen is unchanged.</summary>
    public static string StorageOf(TypeDesc t) =>
        t.Kind == TypeKind.Primitive
            ? t.Primitive switch
            {
                PrimitiveTypeCode.Byte => "uint8_t",
                PrimitiveTypeCode.SByte => "int8_t",
                PrimitiveTypeCode.Boolean => "uint8_t",     // .NET bool is 1 byte
                PrimitiveTypeCode.Int16 => "int16_t",
                PrimitiveTypeCode.UInt16 => "uint16_t",
                PrimitiveTypeCode.Char => "char16_t",       // UTF-16 code unit, 2 bytes
                _ => Of(t),
            }
            // An enum packs at its underlying integer width, so a byte-backed
            // enum array is a packed uint8_t buffer like byte[] — not the int32 stack
            // type Of(enum) reports.
            : t.Kind == TypeKind.Class && t.Class!.IsEnum
                ? EnumStorage(t.Class!.EnumUnderlying)
                : Of(t);

    /// <summary>The C++ type a field occupies in its declaring type's emitted layout:
    /// its real .NET storage width (<see cref="StorageOf"/>) — <c>uint8_t</c> for
    /// <c>bool</c>/<c>byte</c>, <c>char16_t</c> for <c>char</c>, the enum's underlying
    /// integer — for <b>every</b> field, instance or static, of a value type or of a
    /// reference type. Field loads widen implicitly (matching ldfld's zero/sign
    /// extension) and stores truncate on assignment, so only the struct body, the
    /// static-storage declaration and the address-taking/storing sites need this type.
    ///
    /// <para>There is exactly one storage width per field and it is the CLR's. The
    /// int32-promoted model (<see cref="Of"/>) is the *stack* model — what a value looks
    /// like once loaded — and it must not leak into storage, because a managed pointer is
    /// already a real-width pointer (<see cref="Of"/>'s ByRef arm is
    /// <c>StorageOf(Element)*</c>). A 4-byte slot behind a 2-byte <c>ref ushort</c> is the
    /// hazard: the narrow store through the ref fills the low half while the next field read
    /// consumes all four, so the field reads back as garbage whenever a previous
    /// sign-extended store left the high bytes set (a <c>ref short</c> written 5 over a slot
    /// holding -1 reads -65531).</para>
    ///
    /// <para>Consequences: <c>sizeof(t_X)</c> — and the <c>instanceSize</c> stamped from it
    /// — shrinks for a class with sub-word fields; <c>stfld</c>/<c>stsfld</c> truncate for
    /// free via the assignment, as ECMA-335 specifies; and every real field offset moves,
    /// which no symbolic ABI-contract line records, so
    /// <see cref="HotUpdate.AbiContract.LayoutPolicyVersion"/> carries it. The box payload
    /// is deliberately still <see cref="Of"/>-width — a boxed <c>byte</c> is 4 bytes,
    /// matching <c>dn2cpp_byte_type.instanceSize</c> — so the reflection field thunks read
    /// and write at <see cref="Of"/> and let C++ widen/truncate at the member.</para></summary>
    public static string FieldOf(FieldInfo f) => StorageOf(f.Type);

    public static StackKind KindOf(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive switch
        {
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or
            PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 => StackKind.I4,
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 => StackKind.I8,
            PrimitiveTypeCode.Single or PrimitiveTypeCode.Double => StackKind.R8,
            PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => StackKind.I8,
            PrimitiveTypeCode.String or PrimitiveTypeCode.Object => StackKind.Ref,
            _ => throw new NotSupportedException($"Primitive type {t.Primitive} is not supported yet"),
        },
        // 64-bit-backed enums are I8 on the stack (mirroring Of above); every
        // other underlying rides the int32 model.
        TypeKind.Class when t.Class!.IsEnum =>
            t.Class!.EnumUnderlying is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                ? StackKind.I8 : StackKind.I4,
        TypeKind.Class when t.Class!.IsValueType => StackKind.Struct,
        // An External RuntimeTypeHandle rides the pointer model (its Of — via
        // CoreIntrinsics.SpecialTypeCppName — is the raw type-info pointer). Deliberately
        // NOT folded into that shared table: the table carries the C++ type STRING, on which
        // the two Of arms agree, whereas the stack kind is arm-divergent (a loaded
        // Class-kind RuntimeTypeHandle is IsValueType -> Struct above).
        TypeKind.External when t.ExternalName == "System.RuntimeTypeHandle" => StackKind.Ptr,
        TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray => StackKind.Ref,
        TypeKind.ByRef or TypeKind.Pointer => StackKind.Ptr,
        _ => throw new NotSupportedException(t.ToString()),
    };

    public static string DefaultForKind(StackKind k) => k switch
    {
        StackKind.I4 => "int32_t",
        StackKind.I8 => "int64_t",
        StackKind.R8 => "double",
        StackKind.Ref => "Dn2CppObject*",
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    public static string ZeroInit(string cppType) =>
        cppType.EndsWith('*') ? "nullptr"
        : cppType is "int32_t" or "uint32_t" or "int64_t" or "uint64_t"
                  or "float" or "double" or "intptr_t" ? "0"
        // Value structs (managed t_* layouts and the runtime async structs
        // Dn2CppAsyncBuilder/Dn2CppTaskAwaiter) zero-init by aggregate.
        : "{}";

    /// <summary>The zero value as a standalone expression: the aggregate form needs
    /// the type spelled (<c>t_Foo{}</c>) where <see cref="ZeroInit"/>'s bare
    /// <c>{}</c> only works as an initializer.</summary>
    public static string ZeroInitExpr(string cppType)
    {
        string zero = ZeroInit(cppType);
        return zero == "{}" ? cppType + "{}" : zero;
    }

    /// <summary>A floating-point constant as a valid C++ literal/expression, shared by
    /// the body path (<c>ldc.r4</c> / <c>ldc.r8</c>) and the custom-attribute argument
    /// path so the two cannot drift. .NET's <c>ToString("R")</c> renders the special
    /// values as the non-C++ tokens "NaN"/"Infinity"/"-Infinity" and drops the decimal
    /// point on an integral value (yielding a bare integer that, negated, silently loses
    /// -0.0's sign) — both handled here: NaN reproduces the exact bit pattern (its sign
    /// bit is observable through BitConverter), ±Infinity map to the INFINITY macro, and
    /// an integral-valued literal gets a ".0" forcing a floating literal. A float value
    /// is passed widened to double; the result narrows back losslessly at its use.</summary>
    public static string FloatLiteral(double d)
    {
        if (double.IsNaN(d))
            return $"dn2cpp_bits_r8(UINT64_C(0x{System.BitConverter.DoubleToUInt64Bits(d):X16}))";
        if (double.IsPositiveInfinity(d))
            return "(double)INFINITY";
        if (double.IsNegativeInfinity(d))
            return "-(double)INFINITY";
        string lit = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (!lit.Contains('.') && !lit.Contains('E') && !lit.Contains('e'))
            lit += ".0";
        return lit;
    }
}
