using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>True for the binary-integer primitives whose generic-math conversions
    /// (CreateTruncating/CreateChecked/CreateSaturating) lower as a plain numeric cast.
    /// Char/Boolean and the floating types are excluded: a float source needs saturating
    /// round-to-nearest semantics, not the integer wraparound cast, and does not reach this
    /// path. Forwards to <see cref="CoreIntrinsics.IsIntegerPrimitiveCode"/>, the single copy
    /// of the set the reachability cut for the same conversions also asks.</summary>
    private static bool IsIntegerPrimitive(PrimitiveTypeCode p) =>
        CoreIntrinsics.IsIntegerPrimitiveCode(p);

    /// <summary>The (byte width, signedness) of an integral primitive, or null
    /// for a non-integral one. Drives interpolated-string integer formatting so a
    /// hole renders with its declared CLR type's sign and hex width. A byte width
    /// of 0 marks a native int whose width is the target's pointer size (see
    /// <see cref="ByteWidthExpr"/>). Char/Boolean are handled separately and are
    /// intentionally excluded.</summary>
    private static (int byteWidth, bool isSigned)? IntegralHoleInfo(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.SByte => (1, true),
        PrimitiveTypeCode.Byte => (1, false),
        PrimitiveTypeCode.Int16 => (2, true),
        PrimitiveTypeCode.UInt16 => (2, false),
        PrimitiveTypeCode.Int32 => (4, true),
        PrimitiveTypeCode.UInt32 => (4, false),
        PrimitiveTypeCode.Int64 => (8, true),
        PrimitiveTypeCode.UInt64 => (8, false),
        PrimitiveTypeCode.IntPtr => (0, true),
        PrimitiveTypeCode.UIntPtr => (0, false),
        _ => null,
    };

    /// <summary>The C++ expression for a format-call byte-width argument. A
    /// native int (byte width 0 in the integral tables) has the compilation
    /// target's pointer width — a property of the C++ target, not the IL — so it
    /// is deferred to C++ compile time as sizeof(intptr_t) and one generated
    /// output stays correct on both 32- and 64-bit targets; fixed widths remain
    /// literals.</summary>
    private static string ByteWidthExpr(int byteWidth) =>
        byteWidth == 0 ? "(int32_t)sizeof(intptr_t)" : byteWidth.ToString();

    /// <summary>The Dn2CppString* expression for a generic Enum.Parse/TryParse source
    /// argument: the string overloads pass it through, the ReadOnlySpan&lt;char&gt; overloads
    /// (Enum.TryParse&lt;T&gt;(ReadOnlySpan&lt;char&gt;, out T) — reached from
    /// System.Text.Json's EnumConverter.TryParseEnumFromString) realize the span as a
    /// string so the same dn2cpp_enum_parse worker serves both.</summary>
    private string EnumParseSourceString(StackEntry s, TypeDesc paramType)
    {
        if (IsSpanOf(paramType, PrimitiveTypeCode.Char))
        {
            string sv = SpanValue(s, CppTypes.Of(paramType));
            return $"dn2cpp_string_from_chars((const char16_t*){sv}.f__reference, {sv}.f__length)";
        }

        return Cast(s, "Dn2CppString*");
    }

    /// <summary>Enum reflection-lite: lower the generic `System.Enum` statics
    /// (GetValues/GetNames/GetName/Parse/TryParse/IsDefined) inline from the type
    /// argument's (name, value) table, at the enum's model width — int32, or int64 for a
    /// long/ulong underlying — so values are integer constants and member names are string
    /// literals; no runtime metadata is read. The case-insensitive `Parse`/`TryParse`
    /// overloads (the trailing `bool ignoreCase`) fold case via the runtime flag through
    /// `dn2cpp_string_equals_ci`.</summary>
    private void TranslateEnumReflection(string name, MethodSpecificationHandle msh, TypeDesc[] methodArgs)
    {
        var t = methodArgs[0];
        if (t.Kind != TypeKind.Class || t.Class is not { IsEnum: true } enumClass)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Enum.{name}<T> requires a concrete enum type argument");
        // Model width, not int32: a long/ulong-underlying enum's members do not survive
        // the int32 table, and its dedupe collapses distinct members.
        bool wide = IsWideEnum(enumClass);
        string cty = wide ? "int64_t" : "int32_t";
        var members = EnumMembersModel(enumClass);

        // The instantiated call signature, to disambiguate overloads by parameter count.
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
        var ms = _reader.GetMethodSpecification(msh);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx)
            : _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx);
        int pcount = sig.ParameterTypes.Length;

        // Pop the optional trailing `bool ignoreCase` arg and materialize it
        // into an int32 temp, returning that temp's name (or null for the
        // case-sensitive overload). `extraParamCount` is the param count of the
        // ignoreCase form (one more than the case-sensitive base).
        string? PopIgnoreCase(int baseParamCount, int extraParamCount)
        {
            if (pcount == baseParamCount)
                return null;
            if (pcount != extraParamCount)
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: unexpected Enum.{name}<T> overload");
            var icv = Pop();
            string ic = NewTemp("int32_t");
            Emit($"{ic} = {Cast(icv, "int32_t")};");
            return ic;
        }

        List<(long value, string name)> SortedMembers() => members.OrderBy(EnumSortKey(enumClass)).ToList();

        switch (name)
        {
            // GetValues<T> -> T[]: an array of the declared values, typed as the enum
            // (SZArray static type) so element reads format as enum members. The array
            // representation follows the enum's underlying width: a 4-byte
            // underlying uses Dn2CppArrayI4; a sub-word or 64-bit underlying packs into
            // Dn2CppArrayN, matching how the runtime reads the elements.
            case "GetValues":
            {
                EmitPackedEnumValuesArray(t, enumClass);
                return;
            }
            // GetNames<T> -> string[]: the member names as string literals.
            case "GetNames":
            {
                var ordered = SortedMembers();
                // Precisely typed for the same reason as GetValues above: a string[] is a
                // reference array either way, but the shared handle makes GetType() answer
                // "System.Object[]".
                var strElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.String);
                _c.NoteArrayElementType(strElem);
                string arr = NewTemp("Dn2CppArrayRef*");
                Emit($"{arr} = dn2cpp_newarr_ref_t({ordered.Count}, {PreciseArrayTypeInfoExpr(strElem)});");
                for (int i = 0; i < ordered.Count; i++)
                    Emit($"{arr}->data[{i}] = (Dn2CppObject*){_literals.GetOrAdd(ordered[i].name)};");
                Push(StackKind.Ref, "Dn2CppArrayRef*", arr);
                _stack[^1] = _stack[^1] with { StaticType = TypeDesc.MakeSZArray(strElem) };
                return;
            }
            // GetName<T>(T value) -> string?: the member name for the value, or null.
            case "GetName":
            {
                var v = Pop();
                string vt = NewTemp(cty);
                string r = NewTemp("Dn2CppString*");
                Emit($"{vt} = ({cty})({Cast(v, cty)});");
                Emit($"{r} = nullptr;");
                if (members.Count > 0)
                {
                    Emit($"switch ({vt}) {{");
                    foreach (var (value, memberName) in members)
                        Emit($"    case {EnumMemberLit(value, wide)}: {r} = {_literals.GetOrAdd(memberName)}; break;");
                    Emit("}");
                }
                Push(StackKind.Ref, "Dn2CppString*", r);
                return;
            }
            // IsDefined<T>(T value) -> bool: the value matches a declared member.
            case "IsDefined":
            {
                var v = Pop();
                string vt = NewTemp(cty);
                string r = NewTemp("int32_t");
                Emit($"{vt} = ({cty})({Cast(v, cty)});");
                string cond = members.Count == 0
                    ? "0"
                    : string.Join(" || ", members.Select(m => $"{vt} == {EnumMemberLit(m.value, wide)}"));
                Emit($"{r} = ({cond}) ? 1 : 0;");
                Push(StackKind.I4, "int32_t", r);
                return;
            }
            // Parse<T>(string) -> T: the value whose name matches, else a catchable
            // ArgumentException (matching.NET; the message is not modeled).
            case "Parse":
            {
                // Parse(string) or Parse(string, bool ignoreCase) — ignoreCase is on top.
                string? ic = PopIgnoreCase(baseParamCount: 1, extraParamCount: 2);
                var s = Pop();
                string st = NewTemp("Dn2CppString*");
                string r = NewTemp(cty);
                Emit($"{st} = {EnumParseSourceString(s, sig.ParameterTypes[0])};");
                EmitEnumParse(enumClass, members, wide, st, r, ic, onFail:
                    $"dn2cpp_throw_sr1(&dn2cpp_argument_exception_type, DN2CPP_SR_ENUM_VALUE_NOT_FOUND, {st});");
                Push(wide ? StackKind.I8 : StackKind.I4, cty, r);
                return;
            }
            // TryParse<T>(string value [, bool ignoreCase], out T result) -> bool.
            case "TryParse":
            {
                // The out arg is on top, above the optional ignoreCase bool.
                var outRef = Pop();
                string? ic = PopIgnoreCase(baseParamCount: 2, extraParamCount: 3);
                var s = Pop();
                string st = NewTemp("Dn2CppString*");
                string r = NewTemp(cty);            // the parsed value
                string ok = NewTemp("int32_t");     // the bool result
                Emit($"{st} = {EnumParseSourceString(s, sig.ParameterTypes[0])};");
                EmitEnumParse(enumClass, members, wide, st, r, ic, okVar: ok);
                // Store through the `out T` ref at the enum's REAL underlying width:
                // the ref can target a narrowed sub-word-underlying enum field (or a
                // packed array slot), where an int32 store clobbers the neighbors.
                string est = CppTypes.EnumStorage(enumClass.EnumUnderlying);
                Emit($"*({est}*)({Cast(outRef, est + "*")}) = ({est})({r});");
                // …but it can equally target an int32-PROMOTED local/arg slot, where that
                // same store leaves the upper bytes stale (`out SByteE` of -128 read back as
                // 128). Same fixup a byref ARGUMENT gets — this site pops its ref itself, so
                // PopArgs never saw it.
                NoteByRefSlotFixup(outRef, TypeDesc.MakeByRef(t));
                EmitByRefSlotFixups();
                Push(StackKind.I4, "int32_t", ok);
                return;
            }
        }
        throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: Enum.{name}<T> is not supported");
    }

    /// <summary>The packed E[] Enum.GetValues answers with — real .NET's shape:
    /// declared values in unsigned-underlying order, precisely typed
    /// (GetType() == typeof(E[])). Shared by the generic spelling and the
    /// non-generic one over a statically-known typeof(E)
    /// (MethodCompiler.EmitIntrinsic.EnumArray.cs). The array representation
    /// follows the enum's underlying width: 4-byte -> Dn2CppArrayI4, else packed
    /// Dn2CppArrayN.</summary>
    private void EmitPackedEnumValuesArray(TypeDesc t, ClassInfo enumClass)
    {
        bool wide = IsWideEnum(enumClass);
        var ordered = EnumMembersModel(enumClass).OrderBy(EnumSortKey(enumClass)).ToList();
        // The PRECISE per-element handle, not the untyped allocator's shared one. This
        // materialization has no newarr site token, so it taints a canonical trial and falls
        // back per instantiation. The enumerable map too: the non-generic caller's result is
        // statically System.Array, whose foreach dispatches GetEnumerator through the array's
        // own SZArray map.
        TaintIfCanonical(t, "newarr");
        _c.NoteArrayEnumerableElement(t);
        string vti = PreciseArrayTypeInfoExpr(t);
        if (RepOf(t) == ArrRep.I4)
        {
            string arr = NewTemp("Dn2CppArrayI4*");
            Emit($"{arr} = dn2cpp_newarr_i4_t({ordered.Count}, {vti});");
            for (int i = 0; i < ordered.Count; i++)
                Emit($"{arr}->data[{i}] = {EnumMemberLit(ordered[i].value, wide)};");
            Push(StackKind.Ref, "Dn2CppArrayI4*", arr);
        }
        else
        {
            string st = CppTypes.StorageOf(t);
            string arr = NewTemp("Dn2CppArrayN*");
            // An enum holds no managed pointer, so the unscanned allocator — the
            // same choice EmitNewarr makes for a reference-free element.
            Emit($"{arr} = dn2cpp_newarr_n_atomic_t({ordered.Count}, (int32_t)sizeof({st}), {vti});");
            for (int i = 0; i < ordered.Count; i++)
                Emit($"*({st}*)dn2cpp_elem_addr({arr}, {i}) = ({st}){EnumMemberLit(ordered[i].value, wide)};");
            Push(StackKind.Ref, "Dn2CppArrayN*", arr);
        }
        _stack[^1] = _stack[^1] with { StaticType = TypeDesc.MakeSZArray(t) };
    }

    /// <summary>Emit the (value, name) tables and a <c>dn2cpp_enum_parse</c> call for a
    /// generic <c>Enum.Parse</c>/<c>TryParse</c>. A token may be a member
    /// name or a signed decimal at the enum's model width; comma-separated tokens are OR'd.
    /// Case folds when <paramref name="ignoreCaseVar"/> is non-null (an int32 expr).
    /// <paramref name="valVar"/> is set to <c>default</c> (0) first; the success flag
    /// goes to <paramref name="okVar"/> when given (TryParse), otherwise
    /// <paramref name="onFail"/> runs on a miss (Parse). A 64-bit-underlying enum takes
    /// the <c>dn2cpp_enum_parse64</c> twin — its table does not fit int32.
    /// The numeric token's range is the UNDERLYING type's, not the model's, so the
    /// worker takes that width/signedness and reports an out-of-range decimal apart
    /// from a name miss: Parse renders it as OverflowException.</summary>
    private void EmitEnumParse(ClassInfo enumClass, List<(long value, string name)> members, bool wide, string strVar, string valVar, string? ignoreCaseVar, string? onFail = null, string? okVar = null)
    {
        string ic = ignoreCaseVar ?? "0";
        string cty = wide ? "int64_t" : "int32_t";
        string varr = "nullptr", narr = "nullptr";
        if (members.Count > 0)
        {
            varr = NewLocalArray(cty, members.Count);
            narr = NewLocalArray("Dn2CppString*", members.Count);
            for (int i = 0; i < members.Count; i++)
            {
                Emit($"{varr}[{i}] = {EnumMemberLit(members[i].value, wide)};");
                Emit($"{narr}[{i}] = {_literals.GetOrAdd(members[i].name)};");
            }
        }
        Emit($"{valVar} = 0;");
        string ok = okVar ?? NewTemp("int32_t");
        string fn = wide ? "dn2cpp_enum_parse64" : "dn2cpp_enum_parse";
        var (uw, uu) = EnumUnderlyingShape(enumClass);
        // TryParse wants no overflow/miss distinction, so it passes the null sink.
        string ovf = okVar is null ? NewTemp("int32_t") : "";
        string ovfArg = okVar is null ? $"&{ovf}" : "nullptr";
        Emit($"{ok} = {fn}({strVar}, {varr}, {narr}, {members.Count}, {ic}, {uw}, {uu}, &{valVar}, {ovfArg});");
        if (okVar is null)
            Emit($"if (!{ok}) {{ if ({ovf}) dn2cpp_throw_of(&dn2cpp_overflow_exception_type); {onFail} }}");
    }

    /// <summary>The enum's UNDERLYING width in bytes and whether it is unsigned — the
    /// range <c>Enum.Parse</c>'s numeric token must fit. Not the model width:
    /// 300 fits the int32 every sub-64-bit enum rides, and real .NET still raises
    /// OverflowException for a byte-underlying one.</summary>
    private static (int Width, int Unsigned) EnumUnderlyingShape(ClassInfo enumClass) =>
        enumClass.EnumUnderlying switch
        {
            PrimitiveTypeCode.SByte => (1, 0),
            PrimitiveTypeCode.Byte => (1, 1),
            PrimitiveTypeCode.Int16 => (2, 0),
            PrimitiveTypeCode.UInt16 => (2, 1),
            PrimitiveTypeCode.UInt32 => (4, 1),
            PrimitiveTypeCode.Int64 => (8, 0),
            PrimitiveTypeCode.UInt64 => (8, 1),
            _ => (4, 0),
        };

    /// <summary>The greedy flag matcher's sort key: the member's MODEL-width unsigned bit
    /// pattern, which is what <c>dn2cpp_enum_flags_to_string</c> masks against. Deliberately
    /// not <see cref="EnumSortKey"/> — that one reinterprets at the *underlying* width, and a
    /// sign-extended sub-word member's two keys disagree.</summary>
    private static ulong EnumFlagKey(long value, bool wide) => wide ? (ulong)value : (uint)(int)value;

    /// <summary>The Enum.GetValues/GetNames sort key: the constants' *unsigned* underlying
    /// magnitude, not declaration order — a sbyte -5 sorts as 251, a short -5 as 65531.
    /// One copy, because the generic lowering, the packed-array materialization and
    /// CppEmitter.SortedEnumMembers must agree on the order they hand back.</summary>
    internal static Func<(long value, string name), ulong> EnumSortKey(ClassInfo enumClass) =>
        enumClass.EnumUnderlying switch
        {
            PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte => m => (byte)m.value,
            PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => m => (ushort)m.value,
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 => m => (ulong)m.value,
            _ => m => (uint)m.value,
        };

    /// <summary>The body of a per-enum <c>Object.ToString</c> function wired into the enum's
    /// type-info <c>tostring</c> slot, so a boxed enum formats by member name (a plain enum
    /// switches on the value; a [Flags] enum decomposes a combined value into "A, B"; an
    /// undefined value falls back to the decimal). Returns null when the enum has no members
    /// — then the runtime's numeric enum fallback applies.</summary>
    internal static string? EnumToStringFn(ClassInfo enumClass, string fnName, LiteralPool literals)
    {
        // Model width throughout: a 64-bit-underlying enum boxes an 8-byte payload
        // (CppTypes.Of), and the int32 table both truncates its members and collides
        // distinct ones onto one case label.
        bool wide = IsWideEnum(enumClass);
        var members = EnumMembersModel(enumClass);
        if (members.Count == 0)
            return null;
        string cty = wide ? "int64_t" : "int32_t";
        string toStr = wide ? "dn2cpp_long_to_string" : "dn2cpp_int_to_string";
        var sb = new StringBuilder();
        sb.AppendLine($"static Dn2CppString* {fnName}(Dn2CppObject* o) {{");
        sb.AppendLine($"    {cty} v = *({cty}*)(o + 1);");
        if (IsFlagsEnum(enumClass))
        {
            // Sort by descending unsigned value for greedy flag matching (matches
            // the interpolation path and dn2cpp_enum_flags_to_string's contract).
            var ordered = new List<(long value, string name)>(members);
            ordered.Sort((a, b) => EnumFlagKey(b.value, wide).CompareTo(EnumFlagKey(a.value, wide)));
            string vals = string.Join(", ", ordered.Select(m => EnumMemberLit(m.value, wide)));
            string names = string.Join(", ", ordered.Select(m => literals.GetOrAdd(m.name)));
            string flagsFn = wide ? "dn2cpp_enum_flags_to_string64" : "dn2cpp_enum_flags_to_string";
            sb.AppendLine($"    static const {cty} fv[] = {{ {vals} }};");
            sb.AppendLine($"    static Dn2CppString* const fn[] = {{ {names} }};");
            sb.AppendLine($"    Dn2CppString* r = {flagsFn}(v, fv, fn, {ordered.Count});");
            sb.AppendLine($"    return r != nullptr ? r : {toStr}(v);");
        }
        else
        {
            sb.AppendLine("    switch (v) {");
            foreach (var (value, name) in members)
                sb.AppendLine($"        case {EnumMemberLit(value, wide)}: return {literals.GetOrAdd(name)};");
            sb.AppendLine("    }");
            sb.AppendLine($"    return {toStr}(v);");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>The (value, name) of an enum's declared members, in declaration order,
    /// deduped by value (first wins, so duplicate-value aliases don't emit duplicate C++
    /// case labels) — read as int32, which is the model width of every underlying
    /// narrower than 64 bits (a UInt32 constant reads as its int bit pattern, which is
    /// what the payload carries).
    ///
    /// <para><b>Call <see cref="EnumMembersModel"/>, not this.</b> On a 64-bit-underlying
    /// enum the truncation here is silent and the dedupe then collapses distinct members
    /// onto one value; the only caller that may take the narrow table directly is
    /// EnumMembersModel itself.</para></summary>
    internal static List<(int value, string name)> EnumMembers(ClassInfo enumClass)
    {
        var result = new List<(int, string)>();
        var seen = new HashSet<int>();
        var reader = enumClass.Module.Reader;
        var td = reader.GetTypeDefinition(enumClass.Handle);
        foreach (var fdh in td.GetFields())
        {
            var fd = reader.GetFieldDefinition(fdh);
            if ((fd.Attributes & System.Reflection.FieldAttributes.Literal) == 0)
                continue; // skip the instance value__ field
            var ch = fd.GetDefaultValue();
            if (ch.IsNil)
                continue;
            var constant = reader.GetConstant(ch);
            var blob = reader.GetBlobReader(constant.Value);
            int value = constant.TypeCode switch
            {
                ConstantTypeCode.SByte => blob.ReadSByte(),
                ConstantTypeCode.Byte => blob.ReadByte(),
                ConstantTypeCode.Int16 => blob.ReadInt16(),
                ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                ConstantTypeCode.Int32 => blob.ReadInt32(),
                ConstantTypeCode.UInt32 => unchecked((int)blob.ReadUInt32()),
                ConstantTypeCode.Int64 => unchecked((int)blob.ReadInt64()),
                ConstantTypeCode.UInt64 => unchecked((int)blob.ReadUInt64()),
                _ => 0,
            };
            if (seen.Add(value))
                result.Add((value, reader.GetString(fd.Name)));
        }
        return result;
    }

    /// <summary>True when an enum's values ride the 64-bit model slot — a long/ulong
    /// underlying (<see cref="CppTypes.Of"/>). Everything narrower rides int32.</summary>
    internal static bool IsWideEnum(ClassInfo enumClass) =>
        enumClass.EnumUnderlying is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64;

    /// <summary>The member table at the enum's MODEL width: <see cref="EnumMembers64"/>
    /// for a 64-bit underlying, the int32 <see cref="EnumMembers"/> widened otherwise.
    /// Every lowering that emits member values must read through this — the int32 table
    /// truncates a wide member AND dedupes on the truncation, so two distinct members
    /// collapse onto one C++ case label.</summary>
    internal static List<(long value, string name)> EnumMembersModel(ClassInfo enumClass) =>
        IsWideEnum(enumClass)
            ? EnumMembers64(enumClass)
            : EnumMembers(enumClass).Select(m => ((long)m.value, m.name)).ToList();

    /// <summary>A C++ literal for a model-width member constant. long.MinValue has no
    /// negative literal form in C++ (the token is an unsigned literal under a unary
    /// minus), so it is spelled as its predecessor minus one.</summary>
    internal static string EnumMemberLit(long value, bool wide) =>
        !wide ? ((int)value).ToString()
        : value == long.MinValue ? "(-INT64_C(9223372036854775807) - 1)"
        : $"INT64_C({value})";

    /// <summary>Like <see cref="EnumMembers"/> at full 64-bit width, for the
    /// 64-bit-underlying enums whose member values do not survive the int32
    /// truncation (a UInt64 constant reads as its long bit pattern).</summary>
    internal static List<(long value, string name)> EnumMembers64(ClassInfo enumClass)
    {
        var result = new List<(long, string)>();
        var seen = new HashSet<long>();
        var reader = enumClass.Module.Reader;
        var td = reader.GetTypeDefinition(enumClass.Handle);
        foreach (var fdh in td.GetFields())
        {
            var fd = reader.GetFieldDefinition(fdh);
            if ((fd.Attributes & System.Reflection.FieldAttributes.Literal) == 0)
                continue; // skip the instance value__ field
            var ch = fd.GetDefaultValue();
            if (ch.IsNil)
                continue;
            var constant = reader.GetConstant(ch);
            var blob = reader.GetBlobReader(constant.Value);
            long value = constant.TypeCode switch
            {
                ConstantTypeCode.SByte => blob.ReadSByte(),
                ConstantTypeCode.Byte => blob.ReadByte(),
                ConstantTypeCode.Int16 => blob.ReadInt16(),
                ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                ConstantTypeCode.Int32 => blob.ReadInt32(),
                ConstantTypeCode.UInt32 => blob.ReadUInt32(),
                ConstantTypeCode.Int64 => blob.ReadInt64(),
                ConstantTypeCode.UInt64 => unchecked((long)blob.ReadUInt64()),
                _ => 0,
            };
            if (seen.Add(value))
                result.Add((value, reader.GetString(fd.Name)));
        }
        return result;
    }

    /// <summary>The (byte width, signedness) of an enum's underlying type, read
    /// from the instance <c>value__</c> field's signature. Drives the enum hole's
    /// hex zero-pad width (.NET pads enum <c>:X</c> to 2 digits per underlying byte)
    /// and the decimal sign. A byte width of 0 marks a native-int-backed enum
    /// (see <see cref="ByteWidthExpr"/>). Defaults to int32 if not found.</summary>
    private static (int byteWidth, bool isSigned) EnumUnderlyingInfo(ClassInfo enumClass)
    {
        var reader = enumClass.Module.Reader;
        var td = reader.GetTypeDefinition(enumClass.Handle);
        foreach (var fdh in td.GetFields())
        {
            var fd = reader.GetFieldDefinition(fdh);
            if ((fd.Attributes & System.Reflection.FieldAttributes.Literal) != 0)
                continue; // the literal members; we want the instance value__ field
            var blob = reader.GetBlobReader(fd.Signature);
            blob.ReadSignatureHeader(); // FIELD header
            return blob.ReadSignatureTypeCode() switch
            {
                SignatureTypeCode.SByte => (1, true),
                SignatureTypeCode.Byte => (1, false),
                SignatureTypeCode.Int16 => (2, true),
                SignatureTypeCode.UInt16 => (2, false),
                SignatureTypeCode.Int32 => (4, true),
                SignatureTypeCode.UInt32 => (4, false),
                SignatureTypeCode.Int64 => (8, true),
                SignatureTypeCode.UInt64 => (8, false),
                SignatureTypeCode.IntPtr => (0, true),
                SignatureTypeCode.UIntPtr => (0, false),
                _ => (4, true),
            };
        }
        return (4, true);
    }

    /// <summary>True if the enum carries [Flags], so an interpolation hole
    /// combines its set bits into a name list.</summary>
    private static bool IsFlagsEnum(ClassInfo enumClass)
    {
        var reader = enumClass.Module.Reader;
        var td = reader.GetTypeDefinition(enumClass.Handle);
        foreach (var cah in td.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            string? attr = ca.Constructor.Kind switch
            {
                HandleKind.MethodDefinition => DeclTypeName(reader,
                    reader.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor).GetDeclaringType()),
                HandleKind.MemberReference =>
                    reader.GetMemberReference((MemberReferenceHandle)ca.Constructor).Parent is { Kind: HandleKind.TypeReference } p
                        ? RefTypeName(reader, (TypeReferenceHandle)p)
                        : null,
                _ => null,
            };
            if (attr == "System.FlagsAttribute")
                return true;
        }
        return false;
    }

    private static string DeclTypeName(MetadataReader reader, TypeDefinitionHandle h)
    {
        var td = reader.GetTypeDefinition(h);
        string ns = reader.GetString(td.Namespace);
        string nm = reader.GetString(td.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    private static string RefTypeName(MetadataReader reader, TypeReferenceHandle h)
    {
        var tr = reader.GetTypeReference(h);
        string ns = reader.GetString(tr.Namespace);
        string nm = reader.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>The raw signature-blob header of the method a MethodSpec
    /// instantiates (MemberRef across assemblies, MethodDef within one). Read
    /// straight off the blob — no type decoding, so generic var/mvar tokens in
    /// the signature need no context.</summary>
    private SignatureHeader MethodSpecSigHeader(MethodSpecificationHandle msh)
    {
        var mh = _reader.GetMethodSpecification(msh).Method;
        var blob = mh.Kind == HandleKind.MemberReference
            ? _reader.GetMemberReference((MemberReferenceHandle)mh).Signature
            : _reader.GetMethodDefinition((MethodDefinitionHandle)mh).Signature;
        return _reader.GetBlobReader(blob).ReadSignatureHeader();
    }

    /// <summary>AsyncTaskMethodBuilder&lt;T&gt; generic methods, reached as a MethodSpec over
    /// a TypeSpec-parented MemberRef. Start drives the state machine's MoveNext;
    /// AwaitUnsafeOnCompleted is the suspension path: it boxes the struct state machine to
    /// the heap (once) so it survives the returning frame, then registers the boxed MoveNext
    /// as a continuation on the awaited task.</summary>
    private void TranslateAsyncGenericIntrinsic(string name, MethodSpecificationHandle msh)
    {
        var methodArgs = _reader.GetMethodSpecification(msh)
            .DecodeSignature(_c.SigProvider, _method.Context).ToArray();
        switch (name)
        {
            // Start<TSM>(ref TSM sm): this = builder*, arg = sm*. AsyncIteratorMethodBuilder
            // names the very same synchronous driver MoveNext<TSM> — an async iterator's
            // MoveNextAsync calls it once per element instead of once per method.
            case "Start":
            case "MoveNext":
            {
                var sm = Pop();   // managed pointer to the state machine
                Pop();            // builder receiver — completed by ref via the sm
                var mn = _c.StateMachineMoveNext(methodArgs[0])
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: async {name}<{methodArgs[0]}> found no MoveNext");
                Emit($"{DirectCallSym(mn)}({ArgsWithRgctx(StateMachinePtr(methodArgs[0].Class!, sm), mn)});");
                return;
            }
            // AwaitUnsafeOnCompleted<TAwaiter,TSM> (Task/ValueTask awaiters, which
            // implement ICriticalNotifyCompletion) and AwaitOnCompleted<TAwaiter,TSM>
            // (the INotifyCompletion-only path a user awaitable takes) suspend the same
            // way here.
            case "AwaitOnCompleted":
            case "AwaitUnsafeOnCompleted": // AwaitUnsafeOnCompleted<TAwaiter,TSM>(ref awaiter, ref sm)
            {
                // Only the public instance 2-arg shape (ref awaiter, ref sm + receiver)
                // is modeled. The TPL's own builder IL also carries a STATIC box-based
                // overload (ref awaiter, IAsyncStateMachineBox); an unmapped builder
                // whose real IL reaches it must degrade to an ordinary gap row
                // (NotSupportedException), not corrupt/underflow the IL stack.
                if (!MethodSpecSigHeader(msh).IsInstance)
                    throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: the static box-based " +
                        $"{name} overload has no intrinsic mapping (only the public instance form is modeled)");
                var smRef = Pop();      // ref TStateMachine (the state machine being suspended)
                var awaiterRef = Pop(); // ref TAwaiter (saved in the SM's <>u__N field)
                Pop();                  // builder receiver — the SM's own builder field is authoritative
                var smArg = methodArgs[^1];
                if (smArg is not { Kind: TypeKind.Class, Class: { } sm })
                    throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: async state-machine arg {smArg} is not a class");
                var mn = _c.StateMachineMoveNext(smArg)
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: async AwaitUnsafeOnCompleted<{smArg}> found no MoveNext");
                // The suspension bakes the state machine's MoveNext address into
                // continuation state — instantiation-dependent for a canonical
                // state machine.
                if (SharedTrial && Compilation.IsCanonicalMethod(mn))
                    ThrowSharedTaint("async", sm.FullName);
                var builderField = sm.Fields.FirstOrDefault(f => f.Type.Class?.IntrinsicCppName == "Dn2CppAsyncBuilder")
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: state machine {sm.FullName} has no AsyncTaskMethodBuilder field");
                // The resumption differs by awaiter: a Task-backed await registers
                // MoveNext as a continuation on the awaited task; Task.Yield posts
                // it straight onto the run queue to resume on the next turn.
                // Every arm plants MoveNext's address as the continuation — the custom
                // one through CustomAwaiterResume, which notes it itself — so this
                // suspension needs MoveNext's body no less than a direct call would.
                _c.NoteNamedBodySymbol(_method, mn.Emittable);
                string resume = methodArgs[0].Class?.IntrinsicCppName switch
                {
                    "Dn2CppTaskAwaiter" =>
                        $"dn2cpp_task_on_completed(((Dn2CppTaskAwaiter*)({awaiterRef.Expr}))->task, " +
                        $"(void (*)(void*))&{mn.Emittable.CppName}, __sm);",
                    "Dn2CppYieldAwaiter" =>
                        $"dn2cpp_sched_post((void (*)(void*))&{mn.Emittable.CppName}, __sm);",
                    // A custom (non-Task/Yield) awaiter that genuinely suspends:
                    // synthesize a System.Action wrapping the boxed SM's MoveNext and
                    // hand it to the awaiter's INotifyCompletion.OnCompleted (or the
                    // ICriticalNotifyCompletion.UnsafeOnCompleted) so it fires the
                    // continuation when it completes. A *synchronous* user awaitable
                    // (IsCompleted always true) never reaches this branch.
                    _ => CustomAwaiterResume(methodArgs[0], awaiterRef, mn,
                             preferUnsafe: name == "AwaitUnsafeOnCompleted"),
                };
                string smCpp = sm.CppStructName;
                // Box the SM to the heap the first time it suspends (boxed doubles
                // as the flag), then queue MoveNext to resume. A REFERENCE-type state
                // machine already lives on the heap and is already shared by every
                // party that holds it (the enumerator handed to `await foreach` IS the
                // state machine), so copying it would fork the iteration: dereference
                // the byref to the object and suspend on that object itself.
                Emit(sm.IsValueType
                    ? $"{{ {smCpp}* __sm = ({smCpp}*)({smRef.Expr}); " +
                      $"if (__sm->{builderField.CppName}.boxed == nullptr) {{ " +
                      $"{smCpp}* __h = ({smCpp}*)dn2cpp_alloc(sizeof({smCpp})); *__h = *__sm; " +
                      $"__h->{builderField.CppName}.boxed = __h; __sm = __h; }} " +
                      $"{resume} }}"
                    : $"{{ {smCpp}* __sm = {StateMachinePtr(sm, smRef)}; {resume} }}");
                return;
            }
            default:
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: async generic intrinsic " +
                    $"AsyncTaskMethodBuilder::{name} has no intrinsic mapping yet");
        }
    }

    /// <summary>The <c>SM*</c> behind a builder's <c>ref TStateMachine</c> argument.
    /// A value-type state machine (what Release-built `async Task` / `async ValueTask`
    /// methods get) is passed by its own address, so the byref already IS the SM. A
    /// REFERENCE-type one (what Roslyn always emits for an `async IAsyncEnumerable&lt;T&gt;`
    /// iterator, and for a Debug-built async method) is passed as <c>ldloca</c> of a
    /// *reference* local — the address of the object pointer — so it needs one more
    /// dereference to reach the object.</summary>
    private static string StateMachinePtr(ClassInfo sm, StackEntry smRef) =>
        sm.IsValueType
            ? $"({sm.CppStructName}*)({smRef.Expr})"
            : $"*({sm.CppStructName}**)({smRef.Expr})";

    /// <summary>The resume statement for a custom suspending awaiter: allocate
    /// a System.Action whose target is the boxed state machine (<c>__sm</c>) and whose
    /// method is the SM's MoveNext, then call the awaiter's continuation-registering
    /// method (UnsafeOnCompleted/OnCompleted). The awaiter later invokes that Action,
    /// re-entering MoveNext. When the awaiter exposes no OnCompleted(Action), trap at
    /// runtime with a clear message (an awaiter that suspends must implement
    /// INotifyCompletion). Reachability of the call edge + the Action delegate type is
    /// in <c>Compilation.ReachCustomAwaiterContinuation</c>.</summary>
    private string CustomAwaiterResume(TypeDesc awaiterType, StackEntry awaiterRef, MethodInfo moveNext, bool preferUnsafe)
    {
        if (_c.CustomAwaiterOnCompleted(awaiterType, preferUnsafe) is not { } onCompleted
            || onCompleted.Signature.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { IsDelegate: true } action } actionParam]
            || awaiterType.Class is not { } awaiterCls)
            return "dn2cpp_fail(\"async: suspending on a custom non-Task awaitable whose awaiter has no "
                 + "INotifyCompletion.OnCompleted(Action) is not supported\");";
        // The continuation delegate is stamped with the Action type's own
        // type-info and wraps the state machine's MoveNext address.
        TaintIfCanonical(action, "async");
        if (SharedTrial && Compilation.IsCanonicalMethod(moveNext))
            ThrowSharedTaint("async", moveNext.DeclaringClass.FullName);
        _c.NoteNamedBodySymbol(_method, moveNext.Emittable);
        string act = action.CppStructName;
        string build = $"{act}* __act = ({act}*)dn2cpp_alloc(sizeof({act})); "
             + $"((Dn2CppObject*)__act)->type = &{action.CppTypeInfoName}; "
             + $"__act->f_target = (Dn2CppObject*)__sm; "
             + $"__act->f_method = (void*)&{moveNext.Emittable.CppName}; "
             + $"__act->f_prev = nullptr; ";
        // An interface-typed awaiter (a GetAwaiter whose declared return type is
        // an awaiter interface) only declares the registration method — on
        // itself or an inherited INotifyCompletion — so dispatch it through
        // the receiver's interface table.
        if (onCompleted.DeclaringClass.IsInterface)
        {
            var itf = onCompleted.DeclaringClass;
            _c.NoteReferencedType(itf);
            return build
                 + $"Dn2CppObject* __aw = *(Dn2CppObject**)({awaiterRef.Expr}); "
                 + $"const void** __awslots = dn2cpp_resolve_interface(__aw->type, &{itf.CppTypeInfoName}); "
                 + $"(({FnPtrType(onCompleted)})(__awslots[{onCompleted.VtableSlot}]))"
                 + $"(({itf.CppStructName}*)__aw, ({CppTypes.Of(actionParam)})__act);";
        }
        // ref TAwaiter is a pointer to the awaiter; a struct awaiter is its `this`
        // directly, a reference-type awaiter needs the stored reference dereferenced.
        string recv = awaiterCls.IsValueType
            ? $"({awaiterCls.CppStructName}*)({awaiterRef.Expr})"
            : $"*({awaiterCls.CppStructName}**)({awaiterRef.Expr})";
        return build
             + $"{DirectCallSym(onCompleted)}({ArgsWithRgctx($"{recv}, ({CppTypes.Of(actionParam)})__act", onCompleted)});";
    }

    /// <summary>The raw uint64 result slot expression for a Task&lt;T&gt; result
    /// value (int/long/float/double/reference results; floats are bit-cast into
    /// the slot). A struct result must go through <see cref="EmitTaskResultStore"/>
    /// instead (it heap-boxes, which needs an emit).</summary>
    private static string TaskResultStore(StackEntry v) => v.Kind switch
    {
        StackKind.I4 => $"(uint64_t)(uint32_t)({v.Expr})",
        StackKind.I8 => $"(uint64_t)({v.Expr})",
        StackKind.R8 => $"dn2cpp_r8_bits({v.Expr})", // float and double both ride the R8 stack slot
        StackKind.Ref or StackKind.Ptr => $"(uint64_t)(uintptr_t)({v.Expr})",
        _ => throw new NotSupportedException(
            $"async Task<T> result of kind {v.Kind} is not supported yet"),
    };

    /// <summary>The result-slot expression for a Task&lt;T&gt; result, heap-boxing a
    /// struct result (which does not fit the 8-byte slot) into a pointer; all other
    /// kinds delegate to <see cref="TaskResultStore"/>.</summary>
    private string EmitTaskResultStore(StackEntry v)
    {
        if (v.Kind != StackKind.Struct)
            return TaskResultStore(v);
        string tmp = NewTemp(v.CppType);
        Emit($"{tmp} = {v.Expr};");
        return $"(uint64_t)(uintptr_t)dn2cpp_struct_result_box(&{tmp}, (int32_t)sizeof({v.CppType}))";
    }

    /// <summary>A captureless C++ trampoline that runs a <c>Func&lt;TStruct&gt;</c> worker
    /// delegate's whole multicast chain prev-first, heap-boxes the LAST handler's by-value
    /// struct return (<c>dn2cpp_struct_result_box</c>), and packs the boxed pointer into the
    /// task's 8-byte result slot — the reader (<see cref="PushTaskResult"/>'s Struct arm,
    /// i.e. <c>Task&lt;T&gt;.Result</c> / await) copies it back out by the same T. Used by
    /// the worker-invoked task producers (<c>Task.Run</c>, <c>new Task&lt;T&gt;</c>,
    /// <c>StartNew</c>) where the struct kind has no 8-byte result thunk; the struct's exact
    /// C++ type is stamped here, so the runtime worker never needs a struct-return ABI. The
    /// walk mirrors the <c>dn2cpp_run_*</c> thunks — earlier handlers run for their side
    /// effects and their returns are dropped unboxed — and the local class is there because
    /// a captureless lambda cannot name itself to recurse.</summary>
    private static string TaskStructResultThunk(string cppStruct) =>
        $"+[](Dn2CppObject* __d) -> uint64_t {{ "
      + $"struct __chain {{ static {cppStruct} run(Dn2CppObject* __o) {{ "
      + $"auto* __dg = reinterpret_cast<Dn2CppDelegate*>(__o); "
      + $"if (__dg->prev != nullptr) run(__dg->prev); "
      + $"return reinterpret_cast<{cppStruct} (*)(Dn2CppObject*)>(__dg->method)(__dg->target); }} }}; "
      + $"{cppStruct} __r = __chain::run(__d); "
      + $"return (uint64_t)(uintptr_t)dn2cpp_struct_result_box(&__r, (int32_t)sizeof({cppStruct})); }}";

    /// <summary>The <c>Task.ContinueWith</c> mirror of <see cref="TaskStructResultThunk"/>:
    /// a two-argument trampoline over <c>(delegate, settled antecedent)</c> whose
    /// <c>Func&lt;Task, TStruct&gt;</c> struct result is boxed into the slot. Walks the
    /// multicast chain the same way, for the same reason.</summary>
    private static string TaskStructContWithThunk(string cppStruct) =>
        $"+[](Dn2CppObject* __d, Dn2CppObject* __ante) -> uint64_t {{ "
      + $"struct __chain {{ static {cppStruct} run(Dn2CppObject* __o, Dn2CppObject* __a) {{ "
      + $"auto* __dg = reinterpret_cast<Dn2CppDelegate*>(__o); "
      + $"if (__dg->prev != nullptr) run(__dg->prev, __a); "
      + $"return reinterpret_cast<{cppStruct} (*)(Dn2CppObject*, Dn2CppObject*)>(__dg->method)(__dg->target, __a); }} }}; "
      + $"{cppStruct} __r = __chain::run(__d, __ante); "
      + $"return (uint64_t)(uintptr_t)dn2cpp_struct_result_box(&__r, (int32_t)sizeof({cppStruct})); }}";

    /// <summary>Pushes a Task&lt;T&gt;'s result, reinterpreting the raw slot as
    /// the result type T.</summary>
    private void PushTaskResult(string taskExpr, TypeDesc t)
    {
        var k = CppTypes.KindOf(t);
        string expr = k switch
        {
            StackKind.I4 => $"(int32_t)({taskExpr}->result)",
            StackKind.I8 => $"(int64_t)({taskExpr}->result)",
            StackKind.R8 => $"dn2cpp_bits_r8({taskExpr}->result)", // narrows to float for Task<float>
            StackKind.Ref => $"({CppTypes.Of(t)})(uintptr_t)({taskExpr}->result)",
            // A struct result is heap-boxed in the slot (EmitTaskResultStore) — load
            // it back by dereferencing the boxed pointer as T.
            StackKind.Struct => $"(*({CppTypes.Of(t)}*)(uintptr_t)({taskExpr}->result))",
            _ => throw new NotSupportedException(
                $"async Task<{t}>.Result of kind {k} is not supported yet"),
        };
        Push(k, CppTypes.Of(t), expr);
    }
}
