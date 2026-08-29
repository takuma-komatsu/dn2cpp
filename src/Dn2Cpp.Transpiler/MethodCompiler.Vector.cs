using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    // ---- portable SIMD vectors (software-emulated) ----
    //
    // The .NET portable-SIMD types (System.Runtime.Intrinsics.Vector64/128/256/512<T>
    // and System.Numerics.Vector<T>) lower to the scalar-emulated dn2cpp_vec_* runtime
    // (runtime/core/dn2cpp_vectors.h). The C++ vector type is keyed only on byte WIDTH
    // (Dn2CppVector128 == Dn2CppVec<16> for every element type), so two same-width
    // operands share a type regardless of element; the element only selects the template
    // T. This layer has no hardware intrinsics of its own: the portable
    // IsSupported/IsHardwareAccelerated getters emit the DN2CPP_SIMD_HW_ACCEL token (0
    // by default, so the BCL keeps its portable-vector fallback). The platform-ISA
    // facades (AdvSimd, Sse2, …) are a separate axis: a Lowered family's IsSupported is a
    // run-time token and its instructions call dn2cpp_isa_* helpers
    // (MethodCompiler.PlatformIsa.cs), so a BCL arm guarded by AdvSimd.IsSupported alone
    // runs, and the Dn2CppVec<N> values it hands those helpers come from this layer.

    /// <summary>The token emitted for IsSupported / IsHardwareAccelerated of the portable
    /// vector types. Inside the System.Linq assembly this is the separate
    /// DN2CPP_SIMD_HW_ACCEL_LINQ token, so that lane's vectorized-vs-scalar choice stays an
    /// independent lever (runtime/core/dn2cpp_vectors.h); everywhere else the gate is
    /// DN2CPP_SIMD_HW_ACCEL. Both builds emit the same text, preserving the self-host text
    /// fixpoint.</summary>
    private string SimdHwAccelToken =>
        Module.AssemblyName == "System.Linq" ? "DN2CPP_SIMD_HW_ACCEL_LINQ" : "DN2CPP_SIMD_HW_ACCEL";

    /// <summary>The byte width of a software-vector C++ type.</summary>
    internal static int VecWidthBytes(string vecCpp) => vecCpp switch
    {
        "Dn2CppVector64" => 8,
        "Dn2CppVector128" or "Dn2CppVectorT" => 16,
        "Dn2CppVector256" => 32,
        "Dn2CppVector512" => 64,
        _ => throw new NotSupportedException($"'{vecCpp}' is not a software-vector C++ type"),
    };

    /// <summary>The vector C++ type name of a closed Vector64/128/256/512&lt;T&gt; /
    /// Vector&lt;T&gt; TypeDesc, or null when it is not a software-vector type.</summary>
    private static string? VecCppName(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.IntrinsicCppName: { } icn }
        && icn.StartsWith("Dn2CppVector", StringComparison.Ordinal) ? icn : null;

    /// <summary>The element type of a closed software-vector TypeDesc, or null.</summary>
    private static TypeDesc? VecElem(TypeDesc t) =>
        VecCppName(t) is not null && t.Class!.Context.TypeArgs.Length == 1 ? t.Class.Context.TypeArgs[0] : null;

    /// <summary>Whether <c>Vector*&lt;T&gt;.IsSupported</c> is true in real .NET — the
    /// public supported-element set (byte/sbyte/short/ushort/int/uint/long/ulong/
    /// nint/nuint/float/double). Everything else (decimal, char, bool, Int128,
    /// user structs) reports false, keeping the BCL's vectorized fast paths
    /// (e.g. Enumerable.Sum&lt;T&gt;) on their scalar arms — an unsupported closed
    /// instantiation's other members are real throwing stubs, so its gate must
    /// never fold to the backend token.</summary>
    private static bool VectorElemIsSupported(TypeDesc elem) => elem is
    {
        Kind: TypeKind.Primitive,
        Primitive: PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte
            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
            or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double,
    };

    /// <summary>The byte size of a vector lane (vectors are over the blittable primitives).</summary>
    private int VecElemBytes(TypeDesc elem) => elem.Kind == TypeKind.Primitive
        ? elem.Primitive switch
        {
            PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Boolean => 1,
            PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => 2,
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => 4,
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double
                or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => 8,
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: vector element {elem.Primitive} is not supported"),
        }
        : throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: vector element {elem} must be a primitive");

    /// <summary>The half-width vector C++ type of a software-vector C++ type
    /// (Vector256 -&gt; Vector128, etc.), or null for the indivisible Vector64.</summary>
    private static string? VecHalfCppName(string vecCpp) => vecCpp switch
    {
        "Dn2CppVector512" => "Dn2CppVector256",
        "Dn2CppVector256" => "Dn2CppVector128",
        "Dn2CppVector128" => "Dn2CppVector64",
        _ => null,
    };

    /// <summary>Vector256/512&lt;T&gt; store their two halves as private <c>_lower</c> /
    /// <c>_upper</c> fields that the BCL's SIMD search paths read directly (e.g.
    /// IndexOfAnyAsciiSearcher). The intrinsic-mapped vector types model no member fields,
    /// so a field read would fault token resolution — route it to the get_lower/get_upper
    /// vector op (same emission as the GetLower()/GetUpper() methods). Returns false for any
    /// non-vector / non-half field, leaving the normal ldfld path. The result half is pushed
    /// by value (its element type rides the consuming op's signature, as GetLower does).</summary>
    private bool TryEmitVectorHalfField(int token, bool wantAddress)
    {
        var (parent, fname) = PeekFieldRef(token);
        if (parent is null || fname is not ("_lower" or "_upper")) return false;
        if (VecCppName(parent) is not { } vecCpp) return false;
        if (VecElem(parent) is not { } elem) return false;
        if (VecHalfCppName(vecCpp) is not { } half)
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: {vecCpp}.{fname} has no half-width vector");
        string et = CppTypes.StorageOf(elem);
        int n = VecWidthBytes(vecCpp);
        string fn = fname == "_lower" ? "get_lower" : "get_upper";
        var v = Pop();
        string val = v.Kind == StackKind.Ptr ? $"(*({vecCpp}*)({v.Expr}))" : Cast(v, vecCpp);
        string expr = $"dn2cpp_vec_{fn}<{et}, {n}>({val})";
        if (wantAddress)
        {
            // ldflda: materialize the half into an addressable temp and push its address.
            string tmp = NewTemp(half);
            Emit($"{tmp} = {expr};");
            Push(StackKind.Ptr, half + "*", $"&{tmp}");
            return true;
        }
        Push(StackKind.Struct, half, expr);
        return true;
    }

    /// <summary>Finds the primary vector of a helper signature (return type first, then
    /// parameters) and recovers its C++ type / byte width / element. Drives the
    /// static-helper dispatch where the element is not otherwise known.</summary>
    private bool TryVectorPrimary(MethodSignature<TypeDesc> sig, out string vecCpp, out int width, out TypeDesc elem)
    {
        if (!sig.ReturnType.IsVoid && VecCppName(sig.ReturnType) is { } rc)
        {
            vecCpp = rc;
            width = VecWidthBytes(rc);
            elem = sig.ReturnType.Class!.Context.TypeArgs[0];
            return true;
        }
        foreach (var p in sig.ParameterTypes)
            if (VecCppName(p) is { } pc)
            {
                vecCpp = pc;
                width = VecWidthBytes(pc);
                elem = p.Class!.Context.TypeArgs[0];
                return true;
            }
        vecCpp = "";
        width = 0;
        elem = sig.ReturnType;
        return false;
    }

    /// <summary>A conversion operand of the System.Numerics reinterpret family: a concrete
    /// Vector2/3/4/Quaternion/Plane struct (8/12/16 bytes, <paramref name="concrete"/> true)
    /// or Vector128&lt;float&gt; (16 bytes, false — the software-vector pivot). Returns false
    /// for anything else. The byte width fixes the reinterpret copy size.</summary>
    private static bool VecConvOperand(TypeDesc t, out int bytes, out bool concrete)
    {
        concrete = true;
        bytes = t is { Kind: TypeKind.Class, Class: { } c }
            ? c.FullName switch
            {
                "System.Numerics.Vector2" => 8,
                "System.Numerics.Vector3" => 12,
                "System.Numerics.Vector4" or "System.Numerics.Quaternion"
                    or "System.Numerics.Plane" => 16,
                _ => 0,
            }
            : 0;
        if (bytes != 0) return true;
        concrete = false;
        if (VecCppName(t) == "Dn2CppVector128") { bytes = 16; return true; }
        bytes = 0;
        return false;
    }

    /// <summary>Lowers the System.Numerics reinterpret family: the
    /// AsVector2/3/4(+Unsafe), AsQuaternion/AsPlane and AsVector128(+Unsafe) conversions
    /// whose operand or result is a concrete Vector2/3/4/Quaternion/Plane struct. In real .NET each pivots through
    /// Vector128&lt;float&gt; and is a pure byte reinterpret: copy the low min(src,dst) bytes
    /// into a zero-initialized destination. Vector2/3/4 stay real transpiled structs, and
    /// their packed <c>{float X,Y,Z,W}</c> layout is byte-identical to the software-vector
    /// POD (<c>Dn2CppVec&lt;16&gt;</c> is <c>{unsigned char b[16]}</c>). The safe variants
    /// zero-extend the unused lanes as .NET does; the *Unsafe variants leave them undefined
    /// in .NET and are zeroed here for determinism — nothing may observe those lanes.
    /// Returns false (operand stack untouched) for a name/shape it does not own, so the
    /// caller falls through to the software-vector As* path.</summary>
    private bool TryEmitVector234Conversion(string name, MethodSignature<TypeDesc> sig)
    {
        if (name is not ("AsVector2" or "AsVector3" or "AsVector4" or "AsVector3Unsafe"
            or "AsVector4Unsafe" or "AsVector128" or "AsVector128Unsafe"
            or "AsQuaternion" or "AsPlane"))
            return false;
        // The extension receiver is the single parameter; the return is the destination.
        if (sig.ParameterTypes is not [{ } srcType]) return false;
        if (!VecConvOperand(srcType, out int srcBytes, out bool srcConcrete)) return false;
        if (!VecConvOperand(sig.ReturnType, out int dstBytes, out bool dstConcrete)) return false;
        // At least one side must be a concrete Vector2/3/4: a Vector128<->Vector128 (or any
        // software-vector-only) reinterpret belongs to the As* / ToVector* arms, not here.
        if (!srcConcrete && !dstConcrete) return false;

        var src = Pop();
        string srcCpp = CppTypes.StorageOf(srcType);
        string dstCpp = CppTypes.StorageOf(sig.ReturnType);
        // Force-emit the destination struct layout (a no-op for the intrinsic Vector128).
        NoteBuiltStructLayout(sig.ReturnType);
        // Read the source bytes from an address operand directly, else spill the value.
        string srcAddr;
        if (src.Kind == StackKind.Ptr)
            srcAddr = $"(const void*)({src.Expr})";
        else
        {
            string s = NewTemp(srcCpp);
            Emit($"{s} = {Cast(src, srcCpp)};");
            srcAddr = $"&{s}";
        }
        string d = NewTemp(dstCpp);
        Emit($"{d} = {dstCpp}{{}};");
        Emit($"std::memcpy(&{d}, {srcAddr}, {Math.Min(srcBytes, dstBytes)});");
        Push(StackKind.Struct, dstCpp, d);
        return true;
    }

    /// <summary>Lowers a portable-SIMD vector call (a Vector64/128/256/512&lt;T&gt; or
    /// System.Numerics.Vector&lt;T&gt; member / static helper) to the software-vector
    /// runtime. Returns false for an unrecognized name so the caller falls through; a
    /// recognized name with an unsupported shape/element throws (so the gap surfaces in
    /// the reality-check rather than silently miscompiling). <paramref name="vecCpp"/> /
    /// <paramref name="widthBytes"/> / <paramref name="elem"/> describe the primary
    /// vector; same-width operands of any element share that C++ type.</summary>
    private bool TryEmitVectorOp(string vecCpp, int widthBytes, TypeDesc elem, string name,
        MethodSignature<TypeDesc> sig, TypeDesc[]? methodArgs)
    {
        string et = CppTypes.StorageOf(elem);   // the template element type T
        int n = widthBytes;
        int lanes = n / VecElemBytes(elem);
        var ps = sig.ParameterTypes;

        // dn2cpp_vec_<fn><T, N>(...) and the width-only dn2cpp_vec_<fn><N>(...) prefixes.
        string Vec(string fn) => $"dn2cpp_vec_{fn}<{et}, {n}>";
        string VecW(string fn) => $"dn2cpp_vec_{fn}<{n}>";
        // A popped vector operand as an rvalue of the given C++ vector type: an
        // address (instance receiver / `in` param) is dereferenced, a value cast through.
        string Val(StackEntry e, string ct) => e.Kind == StackKind.Ptr ? $"(*({ct}*)({e.Expr}))" : Cast(e, ct);
        void PushVec(string expr) => Push(StackKind.Struct, vecCpp, expr);
        void PushScalar(string expr) => Push(CppTypes.KindOf(sig.ReturnType), CppTypes.Of(sig.ReturnType), expr);
        void PushBool(string boolExpr) => Push(StackKind.I4, "int32_t", $"({boolExpr} ? 1 : 0)");
        bool IsVec(TypeDesc t) => VecCppName(t) is not null;
        bool signedElem = elem.Kind == TypeKind.Primitive && elem.Primitive is
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32
            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr;
        // The data pointer of the element 0/index of a packed array, for load/store.
        string ArrayBase(StackEntry arr, string idx) => RepOf(elem) switch
        {
            ArrRep.I4 => $"(void*)&(({Cast(arr, "Dn2CppArrayI4*")})->data[{idx}])",
            ArrRep.N => $"(void*)dn2cpp_elem_addr({Cast(arr, "Dn2CppArrayN*")}, {idx})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: a vector over a reference-typed array element is not supported"),
        };
        string? SpanCpp(TypeDesc t) => t is { Kind: TypeKind.Class, Class: { } sc }
            && Comp.GenericDefFullName(sc) is "System.Span" or "System.ReadOnlySpan" ? CppTypes.Of(t) : null;
        // Binary (2 vectors -> vector / mask), unary, and bool-reduction helpers.
        bool Bin(string fn) { var b = Pop(); var a = Pop(); PushVec($"{Vec(fn)}({Val(a, vecCpp)}, {Val(b, vecCpp)})"); return true; }
        bool Un(string fn) { var a = Pop(); PushVec($"{Vec(fn)}({Val(a, vecCpp)})"); return true; }
        bool Red(string fn) { var b = Pop(); var a = Pop(); PushBool($"{Vec(fn)}({Val(a, vecCpp)}, {Val(b, vecCpp)})"); return true; }
        bool RedNeg(string fn) { var b = Pop(); var a = Pop(); PushBool($"!{Vec(fn)}({Val(a, vecCpp)}, {Val(b, vecCpp)})"); return true; }
        // Build the vector from the construction args (the Create static-helper shapes,
        // shared by the `new Vector<T>(...)` ctor), pushing the value. The ctor forms
        // (broadcast / array / span) are a subset of these.
        bool EmitCreate()
        {
            // Hierarchical Create(lower, upper) of two half-width vectors.
            if (ps.Length == 2 && IsVec(ps[0]) && IsVec(ps[1]))
            {
                string half = VecCppName(ps[0])!;
                var upper = Pop();
                var lower = Pop();
                PushVec($"{Vec("with_upper")}({Vec("with_lower")}({Vec("zero")}(), {Val(lower, half)}), {Val(upper, half)})");
                return true;
            }
            // Create(T[] values [, int startIndex]) -> load from the array data.
            if (ps.Length >= 1 && ps[0].Kind == TypeKind.SZArray)
            {
                string idx = ps.Length == 2 ? Cast(Pop(), "int32_t") : "0";
                var arr = Pop();
                PushVec($"{VecW("load")}({ArrayBase(arr, idx)})");
                return true;
            }
            // Create(ReadOnlySpan<T> / Span<T> values) -> load from the span data.
            if (ps.Length == 1 && SpanCpp(ps[0]) is { } spanCt)
            {
                var span = Pop();
                string sv = SpanValue(span, spanCt);
                PushVec($"{VecW("load")}((const void*)({sv}.f__reference))");
                return true;
            }
            // Create(T value) -> broadcast scalar to every lane.
            if (ps.Length == 1)
            {
                var s = Pop();
                PushVec($"{Vec("create_broadcast")}({Cast(s, et)})");
                return true;
            }
            // Create(e0, e1, ..., e{lanes-1}) -> element list spilled to a temp array.
            if (ps.Length == lanes)
            {
                string arr = NewLocalArray(et, lanes);
                var vals = new StackEntry[lanes];
                for (int i = lanes - 1; i >= 0; i--)
                    vals[i] = Pop();
                for (int i = 0; i < lanes; i++)
                    Emit($"{arr}[{i}] = {Cast(vals[i], et)};");
                PushVec($"{Vec("create_elems")}({arr})");
                return true;
            }
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Vector Create/ctor with {ps.Length} arg(s) is not supported");
        }

        switch (name)
        {
            // Fixed-width intrinsic vectors use invariant lane formatting. The
            // System.Numerics.Vector<T> family additionally exposes the format/provider
            // overloads and uses the selected NumberGroupSeparator between lanes.
            case "ToString":
            {
                string nfi = "nullptr";
                string format = "nullptr";
                for (int i = ps.Length - 1; i >= 0; i--)
                {
                    var arg = Pop();
                    if (ps[i].IsString)
                        format = Cast(arg, "Dn2CppString*");
                    else
                        nfi = Cast(arg, "const Dn2CppNumberFormatInfo*");
                }
                var v = Pop();
                bool numericsVector = vecCpp == "Dn2CppVectorT";
                if (!numericsVector && ps.Length != 0)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: fixed-width vector ToString overload with arguments is not supported");
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_vec_tostring<{et}, {n}>({Val(v, vecCpp)}, {format}, {nfi}, {(numericsVector ? 1 : 0)})");
                return true;
            }
            // --- constants / nullary constructors ---
            case "get_Zero": PushVec($"{Vec("zero")}()"); return true;
            case "get_AllBitsSet": PushVec($"{Vec("all_bits_set")}()"); return true;
            case "get_One": PushVec($"{Vec("create_broadcast")}(({et})1)"); return true;
            case "get_Indices": PushVec($"{Vec("create_indices")}()"); return true;
            // get_Count is Vector*<T>.Count; get_ElementCount is the same value as the
            // ISimdVector<TSelf,T> static-abstract member (the lane count).
            case "get_Count" or "get_ElementCount": Push(StackKind.I4, "int32_t", lanes.ToString()); return true;
            // IsSupported is per-element (real .NET: false for char/bool even
            // though the software-vector runtime can carry them — see
            // VectorElemIsSupported); IsHardwareAccelerated is element-blind.
            case "get_IsSupported":
                Push(StackKind.I4, "int32_t", VectorElemIsSupported(elem) ? SimdHwAccelToken : "0");
                return true;
            case "get_IsHardwareAccelerated":
                Push(StackKind.I4, "int32_t", SimdHwAccelToken);
                return true;

            // --- construction ---
            case "Create": return EmitCreate();
            case ".ctor":
            {
                // The address-init ctor form (`ldloca dst; <args>; call .ctor`): build the
                // value from the args (above `this` on the stack), then store it into the
                // receiver address that sits below them. The newobj-expression form routes
                // through TranslateNewobj -> "Create".
                if (!EmitCreate())
                    return false;
                var val = Pop();
                var addr = Pop();
                Emit($"*({vecCpp}*)({addr.Expr}) = {Cast(val, vecCpp)};");
                return true;
            }
            case "CreateScalar":
            { var s = Pop(); PushVec($"{Vec("create_scalar")}({Cast(s, et)})"); return true; }
            case "CreateScalarUnsafe":
            { var s = Pop(); PushVec($"{Vec("create_scalar_unsafe")}({Cast(s, et)})"); return true; }
            case "CreateSequence":
            {
                // start + i*step per lane; the lane count is known at compile time.
                var stepE = Pop();
                var startE = Pop();
                string start = NewTemp(et);
                Emit($"{start} = {Cast(startE, et)};");
                string step = NewTemp(et);
                Emit($"{step} = {Cast(stepE, et)};");
                string r = NewTemp(vecCpp);
                Emit($"{r} = {Vec("zero")}();");
                for (int i = 0; i < lanes; i++)
                    Emit($"dn2cpp_vec_set<{et}, {n}>({r}, {i}, ({et})({start} + ({et})({i}) * {step}));");
                Push(StackKind.Struct, vecCpp, r);
                return true;
            }

            // --- element / lane access ---
            case "GetElement" or "get_Item":
            { var idx = Pop(); var v = Pop(); PushScalar($"{Vec("get_element")}({Val(v, vecCpp)}, {Cast(idx, "int32_t")})"); return true; }
            case "WithElement":
            {
                var val = Pop();
                var idx = Pop();
                var v = Pop();
                PushVec($"{Vec("with_element")}({Val(v, vecCpp)}, {Cast(idx, "int32_t")}, {Cast(val, et)})");
                return true;
            }
            case "ToScalar":
            { var v = Pop(); PushScalar($"{Vec("to_scalar")}({Val(v, vecCpp)})"); return true; }
            case "GetLower" or "GetUpper":
            {
                // The static Vector128.GetUpper<T>(Vector128<T>) names the half as its
                // return type, which is the primary vector here; the operand's width is
                // the parameter's.
                var v = Pop();
                string srcCpp = ps.Length >= 1 && VecCppName(ps[0]) is { } pc0 ? pc0 : vecCpp;
                int srcW = VecWidthBytes(srcCpp);
                string half = VecCppName(sig.ReturnType)!;
                string fn = name == "GetLower" ? "get_lower" : "get_upper";
                Push(StackKind.Struct, half, $"dn2cpp_vec_{fn}<{et}, {srcW}>({Val(v, srcCpp)})");
                return true;
            }
            case "WithLower":
            { var lo = Pop(); var v = Pop(); string half = VecCppName(ps[^1])!; PushVec($"{Vec("with_lower")}({Val(v, vecCpp)}, {Val(lo, half)})"); return true; }
            case "WithUpper":
            { var hi = Pop(); var v = Pop(); string half = VecCppName(ps[^1])!; PushVec($"{Vec("with_upper")}({Val(v, vecCpp)}, {Val(hi, half)})"); return true; }

            // --- arithmetic ---
            case "Add" or "op_Addition": return Bin("add");
            case "Subtract" or "op_Subtraction": return Bin("subtract");
            case "Divide" or "op_Division": return Bin("divide");
            // *Native variants share our scalar semantics: a single emulated lane
            // path has no platform-specific NaN/ordering quirk to differ from.
            case "Min" or "MinNative": return Bin("min");
            case "Max" or "MaxNative": return Bin("max");
            case "AddSaturate": return Bin("add_saturate");
            case "SubtractSaturate": return Bin("subtract_saturate");
            case "CopySign": return Bin("copysign");
            case "Ceiling": return Un("ceiling");
            case "Floor": return Un("floor");
            case "Round": return Un("round");
            case "Truncate": return Un("truncate");
            case "Negate" or "op_UnaryNegation": return Un("negate");
            case "Abs": return Un("abs");
            case "Sqrt": return Un("sqrt");
            // Float-only transcendentals / unit conversion (scalar libm per lane).
            case "Asin": return Un("asin");
            case "Cos": return Un("cos");
            case "Exp": return Un("exp");
            case "Hypot": return Bin("hypot");
            case "DegreesToRadians": return Un("degrees_to_radians");
            case "Sum": { var v = Pop(); PushScalar($"{Vec("sum")}({Val(v, vecCpp)})"); return true; }
            case "Dot": { var b = Pop(); var a = Pop(); PushScalar($"{Vec("dot")}({Val(a, vecCpp)}, {Val(b, vecCpp)})"); return true; }
            case "Clamp" or "ClampNative":
            { var hi = Pop(); var lo = Pop(); var v = Pop(); PushVec($"{Vec("clamp")}({Val(v, vecCpp)}, {Val(lo, vecCpp)}, {Val(hi, vecCpp)})"); return true; }
            case "FusedMultiplyAdd":
            { var c = Pop(); var b = Pop(); var a = Pop(); PushVec($"{Vec("fma")}({Val(a, vecCpp)}, {Val(b, vecCpp)}, {Val(c, vecCpp)})"); return true; }
            case "Multiply" or "op_Multiply":
            {
                // vector*vector, vector*scalar, or scalar*vector (broadcast the scalar).
                var b = Pop();
                var a = Pop();
                string av = IsVec(ps[0]) ? Val(a, vecCpp) : $"{Vec("create_broadcast")}({Cast(a, et)})";
                string bv = IsVec(ps[1]) ? Val(b, vecCpp) : $"{Vec("create_broadcast")}({Cast(b, et)})";
                PushVec($"{Vec("multiply")}({av}, {bv})");
                return true;
            }

            // --- bitwise / shift ---
            case "And" or "BitwiseAnd" or "op_BitwiseAnd": return Bin("and");
            case "Or" or "BitwiseOr" or "op_BitwiseOr": return Bin("or");
            case "Xor" or "op_ExclusiveOr": return Bin("xor");
            case "AndNot": return Bin("andnot");
            case "OnesComplement" or "op_OnesComplement": return Un("ones_complement");
            case "ShiftLeft" or "op_LeftShift":
            { var c = Pop(); var a = Pop(); PushVec($"{Vec("shift_left")}({Val(a, vecCpp)}, {Cast(c, "int32_t")})"); return true; }
            case "ShiftRightLogical" or "op_UnsignedRightShift":
            { var c = Pop(); var a = Pop(); PushVec($"{Vec("shift_right_logical")}({Val(a, vecCpp)}, {Cast(c, "int32_t")})"); return true; }
            case "ShiftRightArithmetic":
            { var c = Pop(); var a = Pop(); PushVec($"{Vec("shift_right_arithmetic")}({Val(a, vecCpp)}, {Cast(c, "int32_t")})"); return true; }
            case "op_RightShift":
            { var c = Pop(); var a = Pop(); PushVec($"{Vec(signedElem ? "shift_right_arithmetic" : "shift_right_logical")}({Val(a, vecCpp)}, {Cast(c, "int32_t")})"); return true; }

            // --- comparison masks (2 vectors -> mask vector) ---
            case "GreaterThan": return Bin("greater_than");
            case "LessThan": return Bin("less_than");
            case "GreaterThanOrEqual": return Bin("greater_than_or_equal");
            case "LessThanOrEqual": return Bin("less_than_or_equal");
            case "Equals":
            {
                if (VecCppName(sig.ReturnType) is not null)
                    return Bin("equals");                                    // static Equals -> mask
                if (ps.Length >= 1 && IsVec(ps[0]))
                    return Red("equals_all");                               // instance Equals(Vector) -> bool
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Vector Equals(object)/GetHashCode is not supported");
            }

            // --- comparison reductions (2 vectors -> bool) ---
            case "EqualsAll" or "op_Equality": return Red("equals_all");
            case "op_Inequality": return RedNeg("equals_all");
            case "EqualsAny": return Red("equals_any");
            case "GreaterThanAll": return Red("gt_all");
            case "GreaterThanAny": return Red("gt_any");
            case "LessThanAll": return Red("lt_all");
            case "LessThanAny": return Red("lt_any");
            case "GreaterThanOrEqualAll": return RedNeg("lt_any");
            case "GreaterThanOrEqualAny": return RedNeg("lt_all");
            case "LessThanOrEqualAll": return RedNeg("gt_any");
            case "LessThanOrEqualAny": return RedNeg("gt_all");
            case "ConditionalSelect":
            { var b = Pop(); var a = Pop(); var m = Pop(); PushVec($"{Vec("conditional_select")}({Val(m, vecCpp)}, {Val(a, vecCpp)}, {Val(b, vecCpp)})"); return true; }
            case "ExtractMostSignificantBits":
            { var v = Pop(); PushScalar($"{Vec("extract_msb")}({Val(v, vecCpp)})"); return true; }

            // --- numeric-category predicates (1 vector -> mask vector) ---
            case "IsNaN": return Un("is_nan");
            case "IsFinite": return Un("is_finite");
            case "IsInfinity": return Un("is_infinity");
            case "IsPositiveInfinity": return Un("is_positive_infinity");
            case "IsNegativeInfinity": return Un("is_negative_infinity");
            case "IsNegative": return Un("is_negative");
            case "IsPositive": return Un("is_positive");
            case "IsZero": return Un("is_zero");
            case "IsNormal": return Un("is_normal");
            case "IsSubnormal": return Un("is_subnormal");
            case "IsInteger": return Un("is_integer");
            case "IsEvenInteger": return Un("is_even_integer");
            case "IsOddInteger": return Un("is_odd_integer");

            // --- element search / mask population (vector -> int / bool scalar) ---
            case "Count":
            { var val = Pop(); var v = Pop(); PushScalar($"{Vec("count")}({Val(v, vecCpp)}, {Cast(val, et)})"); return true; }
            case "IndexOf":
            { var val = Pop(); var v = Pop(); PushScalar($"{Vec("index_of")}({Val(v, vecCpp)}, {Cast(val, et)})"); return true; }
            case "LastIndexOf":
            { var val = Pop(); var v = Pop(); PushScalar($"{Vec("last_index_of")}({Val(v, vecCpp)}, {Cast(val, et)})"); return true; }
            case "CountWhereAllBitsSet":
            { var v = Pop(); PushScalar($"{Vec("count_all_bits")}({Val(v, vecCpp)})"); return true; }
            case "IndexOfWhereAllBitsSet":
            { var v = Pop(); PushScalar($"{Vec("index_of_all_bits")}({Val(v, vecCpp)})"); return true; }
            case "LastIndexOfWhereAllBitsSet":
            { var v = Pop(); PushScalar($"{Vec("last_index_of_all_bits")}({Val(v, vecCpp)})"); return true; }
            case "AllWhereAllBitsSet":
            { var v = Pop(); PushBool($"{Vec("all_all_bits")}({Val(v, vecCpp)})"); return true; }
            case "AnyWhereAllBitsSet":
            { var v = Pop(); PushBool($"{Vec("any_all_bits")}({Val(v, vecCpp)})"); return true; }
            case "All":
            { var val = Pop(); var v = Pop(); PushBool($"{Vec("all")}({Val(v, vecCpp)}, {Cast(val, et)})"); return true; }
            case "Any":
            { var val = Pop(); var v = Pop(); PushBool($"{Vec("any")}({Val(v, vecCpp)}, {Cast(val, et)})"); return true; }

            // --- reinterpret (As*) : byte-identical when widths match ---
            // The AsVector<elem> dozen is System.Numerics.Vector's own spelling of the
            // same reinterpret the Vector64/128/256/512 classes spell As<elem>: every
            // one is Vector<T> -> Vector<U>, and Vector<T> is one fixed-width register
            // whatever its element, so they always take the outW == srcW arm below.
            // The AsVector2/AsVector3/AsVector4 conversions are NOT here: those name a
            // concrete System.Numerics.Vector2/3/4 struct on one side (never a fixed-width
            // software-vector register), so they route through TryEmitVector234Conversion.
            case "As" or "AsByte" or "AsSByte" or "AsInt16" or "AsUInt16" or "AsInt32"
                or "AsUInt32" or "AsInt64" or "AsUInt64" or "AsSingle" or "AsDouble"
                or "AsNInt" or "AsNUInt" or "AsVector" or "AsVector64" or "AsVector128"
                or "AsVector128Unsafe" or "AsVector256" or "AsVector256Unsafe"
                or "AsVector512" or "AsVector512Unsafe"
                or "AsVectorByte" or "AsVectorSByte" or "AsVectorInt16" or "AsVectorUInt16"
                or "AsVectorInt32" or "AsVectorUInt32" or "AsVectorInt64" or "AsVectorUInt64"
                or "AsVectorNInt" or "AsVectorNUInt" or "AsVectorSingle" or "AsVectorDouble":
            {
                var src = Pop();
                string srcCpp = ps.Length >= 1 && VecCppName(ps[0]) is { } pc0 ? pc0 : vecCpp;
                int srcW = VecWidthBytes(srcCpp);
                string outCpp = VecCppName(sig.ReturnType)
                    ?? throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: vector {name} result {sig.ReturnType} is not a vector");
                int outW = VecWidthBytes(outCpp);
                if (outW == srcW)
                {
                    Push(StackKind.Struct, outCpp, Val(src, srcCpp));
                    return true;
                }
                if (outW > srcW)
                {
                    string expr = Val(src, srcCpp);
                    for (int w = srcW; w < outW; w *= 2)
                        expr = $"dn2cpp_vec_widen_bytes<{w}>({expr})";
                    Push(StackKind.Struct, outCpp, expr);
                    return true;
                }
                Push(StackKind.Struct, outCpp, $"dn2cpp_vec_get_lower<{et}, {srcW}>({Val(src, srcCpp)})");
                return true;
            }

            // --- widen storage to a bigger fixed width (zero-extend) ---
            case "ToVector128" or "ToVector128Unsafe" or "ToVector256" or "ToVector256Unsafe"
                or "ToVector512" or "ToVector512Unsafe":
            {
                var src = Pop();
                string srcCpp = ps.Length >= 1 && VecCppName(ps[0]) is { } pc0 ? pc0 : vecCpp;
                int srcW = VecWidthBytes(srcCpp);
                string outCpp = VecCppName(sig.ReturnType)!;
                int outW = VecWidthBytes(outCpp);
                string expr = Val(src, srcCpp);
                for (int w = srcW; w < outW; w *= 2)
                    expr = $"dn2cpp_vec_widen_bytes<{w}>({expr})";
                Push(StackKind.Struct, outCpp, expr);
                return true;
            }

            // --- element-type conversions ---
            case "ConvertToInt32" or "ConvertToUInt32" or "ConvertToInt64" or "ConvertToUInt64"
                or "ConvertToSingle" or "ConvertToDouble":
            {
                var v = Pop();
                string srcCpp = ps.Length >= 1 && VecCppName(ps[0]) is { } pc0 ? pc0 : vecCpp;
                TypeDesc srcElem = (ps.Length >= 1 ? VecElem(ps[0]) : null) ?? elem;
                TypeDesc dstElem = VecElem(sig.ReturnType)
                    ?? throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: vector {name} result is not a vector");
                Push(StackKind.Struct, VecCppName(sig.ReturnType)!,
                    $"dn2cpp_vec_convert<{CppTypes.StorageOf(srcElem)}, {CppTypes.StorageOf(dstElem)}, {n}>({Val(v, srcCpp)})");
                return true;
            }
            case "Narrow":
            {
                var upper = Pop();
                var lower = Pop();
                string srcCpp = VecCppName(ps[0])!;
                Push(StackKind.Struct, VecCppName(sig.ReturnType)!,
                    $"dn2cpp_vec_narrow<{CppTypes.StorageOf(VecElem(ps[0])!)}, {CppTypes.StorageOf(VecElem(sig.ReturnType)!)}, {n}>"
                    + $"({Val(lower, srcCpp)}, {Val(upper, srcCpp)})");
                return true;
            }
            case "WidenLower" or "WidenUpper":
            {
                var v = Pop();
                string srcCpp = ps.Length >= 1 && VecCppName(ps[0]) is { } pc0 ? pc0 : vecCpp;
                TypeDesc srcElem = (ps.Length >= 1 ? VecElem(ps[0]) : null) ?? elem;
                string fn = name == "WidenLower" ? "widen_lower" : "widen_upper";
                Push(StackKind.Struct, VecCppName(sig.ReturnType)!,
                    $"dn2cpp_vec_{fn}<{CppTypes.StorageOf(srcElem)}, {CppTypes.StorageOf(VecElem(sig.ReturnType)!)}, {n}>({Val(v, srcCpp)})");
                return true;
            }
            case "Widen":
            {
                // void Widen(Vector<TSrc> source, out Vector<TDst> low, out Vector<TDst>
                // high) — the two-out form (System.Numerics.Vector). Each out is a ByRef
                // to a same-width widened vector; emit the lower/upper halves into them.
                if (ps.Length == 3 && ps[1].Kind == TypeKind.ByRef && VecCppName(ps[1].Element!) is { } outCpp)
                {
                    var highRef = Pop();
                    var lowRef = Pop();
                    var src = Pop();
                    string srcCpp = VecCppName(ps[0])!;
                    string srcSt = CppTypes.StorageOf(VecElem(ps[0])!);
                    string dstSt = CppTypes.StorageOf(VecElem(ps[1].Element!)!);
                    string sv = NewTemp(srcCpp);
                    Emit($"{sv} = {Val(src, srcCpp)};");
                    Emit($"*({outCpp}*)({lowRef.Expr}) = dn2cpp_vec_widen_lower<{srcSt}, {dstSt}, {n}>({sv});");
                    Emit($"*({outCpp}*)({highRef.Expr}) = dn2cpp_vec_widen_upper<{srcSt}, {dstSt}, {n}>({sv});");
                    return true;
                }
                // (Vector<TDst> Lower, Vector<TDst> Upper) Widen(Vector<TSrc>) — the
                // tuple-returning form (Vector64/128/256/512; Guid.TryFormatCore's hex
                // path). The return is a plain transpiled ValueTuple`2 struct whose
                // fields are the widened vectors: fill Item1/Item2 from the halves.
                if (ps.Length == 1
                    && sig.ReturnType is { Kind: TypeKind.Class, Class: { IsValueType: true } tupleCls }
                    && tupleCls.Context.TypeArgs is [{ } dstVec, _]
                    && VecCppName(dstVec) is not null)
                {
                    var src = Pop();
                    string srcCpp = VecCppName(ps[0])!;
                    string srcSt = CppTypes.StorageOf(VecElem(ps[0])!);
                    string dstSt = CppTypes.StorageOf(VecElem(dstVec)!);
                    Comp.EnsureCompleted(tupleCls);
                    var item1 = tupleCls.Fields.First(f => f.Name == "Item1").CppName;
                    var item2 = tupleCls.Fields.First(f => f.Name == "Item2").CppName;
                    string tupleCpp = CppTypes.Of(sig.ReturnType);
                    string sv = NewTemp(srcCpp);
                    Emit($"{sv} = {Val(src, srcCpp)};");
                    string tv = NewTemp(tupleCpp);
                    Emit($"{tv} = {tupleCpp}{{}};");
                    Emit($"{tv}.{item1} = dn2cpp_vec_widen_lower<{srcSt}, {dstSt}, {n}>({sv});");
                    Emit($"{tv}.{item2} = dn2cpp_vec_widen_upper<{srcSt}, {dstSt}, {n}>({sv});");
                    Push(StackKind.Struct, tupleCpp, tv);
                    return true;
                }
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Vector Widen shape is not supported (expected the two-out or tuple-return form)");
            }

            // --- shuffle / interleave ---
            // The internal Vector128.UnpackLow/UnpackHigh (UNPCKL/UNPCKH, ZIP1/ZIP2) the
            // BCL's hex and ASCII arms reach once an ISA guard answers true.
            case "UnpackLow": return Bin("unpack_low");
            case "UnpackHigh": return Bin("unpack_high");
            case "Shuffle" or "ShuffleNative":
            {
                var idx = Pop();
                var v = Pop();
                string idxCpp = ps.Length >= 2 && VecCppName(ps[1]) is { } pc1 ? pc1 : vecCpp;
                PushVec($"{Vec("shuffle")}({Val(v, vecCpp)}, {Val(idx, idxCpp)})");
                return true;
            }

            // --- load / store / copy ---
            case "Load" or "LoadAligned" or "LoadAlignedNonTemporal" or "LoadUnsafe":
            {
                if (name == "LoadUnsafe" && ps.Length == 2)
                {
                    var off = Pop();
                    var src = Pop();
                    PushVec($"{VecW("load_off")}((const void*)({src.Expr}), (intptr_t)({off.Expr}) * (intptr_t)sizeof({et}))");
                    return true;
                }
                var p = Pop();
                PushVec($"{VecW("load")}((const void*)({p.Expr}))");
                return true;
            }
            case "Store" or "StoreAligned" or "StoreAlignedNonTemporal" or "StoreUnsafe":
            {
                if (name == "StoreUnsafe" && ps.Length == 3)
                {
                    var off = Pop();
                    var dest = Pop();
                    var v = Pop();
                    Emit($"{VecW("store")}({Val(v, vecCpp)}, (void*)((unsigned char*)({dest.Expr}) + (intptr_t)({off.Expr}) * (intptr_t)sizeof({et})));");
                    return true;
                }
                var d = Pop();
                var vv = Pop();
                Emit($"{VecW("store")}({Val(vv, vecCpp)}, (void*)({d.Expr}));");
                return true;
            }
            case "StoreLowerUnsafe":
            {
                // Store the lower half (a Vector128's lower Vector64, etc.) to
                // `ref T destination` at an optional element offset. The lower N/2 bytes
                // are the lower-half vector (get_lower), stored with the half-width store.
                var off = ps.Length == 3 ? Pop() : null;
                var dest = Pop();
                var v = Pop();
                string ptr = off is { } o
                    ? $"(void*)((unsigned char*)({dest.Expr}) + (intptr_t)({o.Expr}) * (intptr_t)sizeof({et}))"
                    : $"(void*)({dest.Expr})";
                Emit($"dn2cpp_vec_store<{n / 2}>(dn2cpp_vec_get_lower<{et}, {n}>({Val(v, vecCpp)}), {ptr});");
                return true;
            }
            case "CopyTo":
            {
                // Vector<T> exposes instance CopyTo(destination[, index]); the fixed-width
                // Vector64/128/256/512 facades expose extension helpers whose first explicit
                // parameter is the vector. Normalize both signatures to the target position.
                int targetArg = sig.Header.IsInstance ? 0 : 1;
                bool hasVectorOperand = sig.Header.IsInstance
                    || (ps.Length > 0 && IsVec(ps[0]));
                if (hasVectorOperand && ps.Length >= targetArg + 1 && ps.Length <= targetArg + 2
                    && ps[targetArg].Kind == TypeKind.SZArray)
                {
                    bool hasIndex = ps.Length == targetArg + 2;
                    string idx = hasIndex ? Cast(Pop(), "int32_t") : "0";
                    var arr = Pop();
                    var v = Pop();
                    Emit($"dn2cpp_vec_copy_array_check((Dn2CppArray*)({arr.Expr}), {idx}, {lanes}, {(hasIndex ? 1 : 0)});");
                    Emit($"{VecW("store")}({Val(v, vecCpp)}, {ArrayBase(arr, idx)});");
                    return true;
                }
                if (hasVectorOperand && ps.Length == targetArg + 1
                    && SpanCpp(ps[targetArg]) is { } spanCt)
                {
                    var span = Pop();
                    var v = Pop();
                    string sv = SpanValue(span, spanCt);
                    Emit($"dn2cpp_vec_copy_span_check({sv}.f__length, {lanes});");
                    Emit($"{VecW("store")}({Val(v, vecCpp)}, (void*)({sv}.f__reference));");
                    return true;
                }
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Vector CopyTo target shape is not supported");
            }

            default:
                return false;
        }
    }
}
