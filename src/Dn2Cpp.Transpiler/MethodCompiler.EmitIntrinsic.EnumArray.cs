using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitEnumArrayIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {
            // Non-generic Enum.* statics taking a runtime Type. The value forms
            // receive a boxed object; the enum payload is an int32 (the model width) at
            // box + sizeof(header).
            case ("System.Enum", "GetUnderlyingType") when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_enum_underlying({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // Nullable.GetUnderlyingType(Type): U for a closed Nullable<U>, else null.
            case ("System.Nullable", "GetUnderlyingType") when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_nullable_get_underlying({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // Enum.GetNames(Type) (non-generic) -> string[]. The runtime helper allocates
            // with the shared ref-array handle, so retag with the precise ti_arr_<string>
            // one: the layout is the same either way, only the IDENTITY differs, and
            // without the retag GetType() answers "System.Object[]" and
            // `== typeof(string[])` reads False. Type.GetEnumNames, the other spelling of
            // this call, retags through PushReflectionMemberArray.
            case ("System.Enum", "GetNames") when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                var strElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.String);
                Comp.NoteArrayElementType(strElem);
                string namesTmp = NewTemp("Dn2CppArrayRef*");
                Emit($"{namesTmp} = dn2cpp_enum_get_names({Cast(t, "Dn2CppType*")});");
                Emit($"((Dn2CppObject*){namesTmp})->type = {PreciseArrayTypeInfoExpr(strElem)};");
                Push(StackKind.Ref, "Dn2CppArrayRef*", namesTmp);
                return true;
            }
            // Enum.GetValues(Type). A STATICALLY-KNOWN typeof(E) answers exactly like real
            // .NET: the packed, precisely-typed E[] the generic spelling mints (one shared
            // emission, EmitPackedEnumValuesArray, whose enumerable-map note also serves
            // the System.Array.GetEnumerator foreach below).
            //
            // DECLARED DIVERGENCE, confined to a RUNTIME-chosen Type (a Type variable no
            // ldtoken reaches): object[] of boxed enum values, GetType() "System.Object[]"
            // where .NET says "E[]". The retag below must not be changed to name
            // ti_arr_<E>: the elements are boxes (dn2cpp_enum_get_values_boxed) while an
            // E[] is packed, so claiming the precise handle would make every ldelem read a
            // pointer as an integer — a wrong answer with no diagnostic.
            case ("System.Enum", "GetValues") when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                if (t.TypeToken is { Kind: TypeKind.Class, Class: { IsEnum: true } ec } et)
                {
                    EmitPackedEnumValuesArray(et, ec);
                    return true;
                }
                var objElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.Object);
                Comp.NoteArrayEnumerableElement(objElem);
                string arrTmp = NewTemp("Dn2CppArrayRef*");
                Emit($"{arrTmp} = dn2cpp_enum_get_values_boxed({Cast(t, "Dn2CppType*")});");
                Emit($"((Dn2CppObject*){arrTmp})->type = {PreciseArrayTypeInfoExpr(objElem)};");
                Push(StackKind.Ref, "Dn2CppArrayRef*", arrTmp);
                return true;
            }
            // Enum.GetValuesAsUnderlyingType(Type) (non-generic): an Array of the UNDERLYING
            // primitive (byte[]/int[]/long[]/ulong[]/...), typed as System.Array. The runtime
            // helper builds the properly-typed primitive array via dn2cpp_array_create_instance,
            // so its element type-info is the underlying's — GetType()/Length/GetValue all read
            // it — and no retag is needed (unlike GetValues over boxed enums). Cutting the real
            // Enum.GetValuesAsUnderlyingType body deletes the GetEnumInfo/GetEnumValuesAndNames
            // (InternalCall, switched over all eight storage widths) cascade.
            case ("System.Enum", "GetValuesAsUnderlyingType") when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                string rc = CppTypes.Of(sig.ReturnType); // System.Array
                Push(StackKind.Ref, rc,
                    $"({rc})dn2cpp_enum_get_values_underlying({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // System.Array.GetEnumerator (non-generic, → ): a foreach over a
            // bare System.Array (e.g. the non-generic Enum.GetValues(Type) result above)
            // lowers to this. Dispatch the non-generic IEnumerable.GetEnumerator on the
            // array's own SZArray map (its GetEnumerator thunk wraps the array in a fresh
            // SZArrayEnumerable<object>) — no compile-time wrap. Requires the array to carry
            // a per-element ti_arr_<T> handle with the map (the Enum.GetValues result is
            // retagged above); a bare runtime-internal array without one is out of scope.
            case ("System.Array", "GetEnumerator") when sig.ParameterTypes.Length == 0:
            {
                var arr = Pop();
                string retCpp = CppTypes.Of(sig.ReturnType); // System.Collections.IEnumerator*
                if (Comp.ReachNonGenericArrayEnumerator() is not ({ } itf, { } ge))
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: System.Array.GetEnumerator needs the non-generic IEnumerable");
                string fnPtr = $"{retCpp} (*)({itf.CppStructName}*)";
                Push(StackKind.Ref, retCpp,
                    $"(({fnPtr})(dn2cpp_resolve_interface(((Dn2CppObject*){arr.Expr})->type, &{itf.CppTypeInfoName})[{ge.VtableSlot}]))(({itf.CppStructName}*){arr.Expr})");
                return true;
            }
            case ("System.Enum", "GetName") when sig.ParameterTypes.Length == 2:
            {
                var v = Pop();
                var t = Pop();
                // The box goes over whole: its payload is 8 bytes for a long/ulong-underlying
                // enum and 4 for every narrower one, and the Type here is a run-time value,
                // so narrowing at the call site would truncate the wide members.
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_enum_get_name({Cast(t, "Dn2CppType*")}, {Cast(v, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Enum", "IsDefined") when sig.ParameterTypes.Length == 2:
            {
                var v = Pop();
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_enum_is_defined({Cast(t, "Dn2CppType*")}, {Cast(v, "Dn2CppObject*")})");
                return true;
            }
            // Enum.Parse(Type, string[, ignoreCase]) -> boxed enum, or ArgumentException.
            // Classify by TYPE, never arity: the ReadOnlySpan<char> overloads share the
            // arity (Parse(Type, ROS<char>) is also 2 args, Parse(Type, ROS<char>, bool)
            // also 3) but pass a span struct — casting one to Dn2CppString* would compile
            // and read garbage. Guarding on ParameterTypes[1].IsString leaves the span
            // forms unmatched, so they fall through to EmitIntrinsic's loud
            // NotSupportedException (naming ReadOnlySpan<char>) rather than a wrong shape.
            case ("System.Enum", "Parse") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1].IsString:
            {
                var s = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_enum_parse_type({Cast(t, "Dn2CppType*")}, {Cast(s, "Dn2CppString*")}, 0)");
                return true;
            }
            case ("System.Enum", "Parse") when sig.ParameterTypes.Length == 3
                && sig.ParameterTypes[1].IsString:
            {
                var ic = Pop();
                var s = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_enum_parse_type({Cast(t, "Dn2CppType*")}, {Cast(s, "Dn2CppString*")}, {Cast(ic, "int32_t")})");
                return true;
            }
            // Enum.Parse(Type, ReadOnlySpan<char>[, ignoreCase]) — the span complement of
            // the string forms above. Realize the span's {f__reference, f__length} as a
            // Dn2CppString and reuse the string parse path — the MrEnumStatics cut deleted
            // the real body, so cut ⇒ route needs this arm. Still classified by TYPE, not
            // arity: the string cases above and these are mutually exclusive on
            // ParameterTypes[1] (string vs span).
            case ("System.Enum", "Parse") when sig.ParameterTypes.Length is 2 or 3
                && IsSpanOf(sig.ParameterTypes[1], PrimitiveTypeCode.Char):
            {
                string icExpr = sig.ParameterTypes.Length == 3 ? Cast(Pop(), "int32_t") : "0";
                string sv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1]));
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_enum_parse_type({Cast(t, "Dn2CppType*")}, "
                    + $"dn2cpp_string_from_chars((const char16_t*){sv}.f__reference, {sv}.f__length), {icExpr})");
                return true;
            }
            // Enum.TryParse(Type, string[, ignoreCase], out object) -> bool + boxed enum.
            // The Type-driven complement of the generic TryParse<T> (EnumAsync.cs): the
            // enum type arrives at run time, so it reads the per-enum table off the runtime
            // type-info via dn2cpp_enum_try_parse_type (non-throwing on a value miss — the
            // TYPE is still validated). The out object arg is a byref object slot on top of
            // the stack; store the boxed result (or null on a miss) through it. Same
            // TYPE-not-arity discipline as Parse above: the ReadOnlySpan<char> overloads
            // (arity 3 and 4) are left unmatched to the loud NotSupportedException.
            case ("System.Enum", "TryParse") when sig.ParameterTypes.Length is 3 or 4
                && sig.ParameterTypes[1].IsString:
            {
                var outRef = Pop();
                string icExpr = sig.ParameterTypes.Length == 4 ? Cast(Pop(), "int32_t") : "0";
                var s = Pop();
                var t = Pop();
                string res = NewTemp("Dn2CppObject*");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_enum_try_parse_type({Cast(t, "Dn2CppType*")}, {Cast(s, "Dn2CppString*")}, {icExpr}, &{res});");
                Emit($"*({Cast(outRef, "Dn2CppObject**")}) = {res};");
                Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({outRef.Expr}));");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            // Enum.TryParse(Type, ReadOnlySpan<char>[, ignoreCase], out object) — the span
            // complement of the string TryParse above: realize the span as a Dn2CppString
            // and reuse dn2cpp_enum_try_parse_type. TYPE-gated on ParameterTypes[1] so it
            // never collides with the string forms.
            case ("System.Enum", "TryParse") when sig.ParameterTypes.Length is 3 or 4
                && IsSpanOf(sig.ParameterTypes[1], PrimitiveTypeCode.Char):
            {
                var outRef = Pop();
                string icExpr = sig.ParameterTypes.Length == 4 ? Cast(Pop(), "int32_t") : "0";
                string sv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1]));
                var t = Pop();
                string res = NewTemp("Dn2CppObject*");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_enum_try_parse_type({Cast(t, "Dn2CppType*")}, "
                    + $"dn2cpp_string_from_chars((const char16_t*){sv}.f__reference, {sv}.f__length), {icExpr}, &{res});");
                Emit($"*({Cast(outRef, "Dn2CppObject**")}) = {res};");
                Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({outRef.Expr}));");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            // Enum.Format(Type, object value, string format) — the non-generic static
            // formatter EnumConverter.ConvertTo drives (reached via the reflection path:
            // ConvertTo -> Enum.Format -> the GetEnumInfo/GetEnumValuesAndNames cascade).
            // G/g, D/d, X/x, F/f are handled in the runtime helper off the per-enum
            // (name, value) table plus the type's [Flags] bit; cutting this edge
            // (MrEnumStatics) deletes the whole reflection cascade behind Enum.Format.
            case ("System.Enum", "Format") when sig.ParameterTypes.Length == 3:
            {
                var fmt = Pop();
                var val = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_enum_format({Cast(t, "Dn2CppType*")}, {Cast(val, "Dn2CppObject*")}, {Cast(fmt, "Dn2CppString*")})");
                return true;
            }
            // Enum.ToObject(Type, <integral>) -> boxed enum. Same TYPE-not-arity discipline
            // as Parse/TryParse above, over .NET's whole overload set: the eight integral
            // forms here plus the object form below — together total, so an overload added
            // upstream falls to EmitIntrinsic's loud NotSupportedException rather than a
            // wrong pop. The value is funneled through its own declared C++ width first (an
            // unsigned sub-64 operand must ZERO-extend into the int64 helper argument —
            // `(int64_t)` straight off the int32 stack model would sign-extend uint
            // 0x80000000 into a 64-bit-underlying enum).
            case ("System.Enum", "ToObject") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive:
                    PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                    or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                    or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                    or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 }:
            {
                var v = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_enum_to_object({Cast(t, "Dn2CppType*")}, "
                    + $"(int64_t)({Cast(v, CppTypes.Of(sig.ParameterTypes[1]))}))");
                return true;
            }
            // Enum.ToObject(Type, object) — the boxed-value form; the runtime helper
            // reads the box at its own type-info (the eight integer widths plus
            // Char/Boolean and boxed enums; float/double/other throw ArgumentException,
            // matching .NET's Convert.GetTypeCode dispatch).
            case ("System.Enum", "ToObject") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1].IsObject:
            {
                var v = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_enum_to_object_boxed({Cast(t, "Dn2CppType*")}, {Cast(v, "Dn2CppObject*")})");
                return true;
            }

            // Array.Copy: the real body checks array covariance via MethodTable
            // internals we don't model. Element repr is tracked statically (an
            // element-sized array reuses the ref type-info at runtime), so emit a
            // typed memmove keyed on the source array's static C++ type.
            case ("System.Array", "Copy") when sig.ParameterTypes.Length == 3:
            {
                var len = Pop();
                var dst = Pop();
                var src = Pop();
                EmitArrayCopy(src, "0", dst, "0", len.Expr);
                return true;
            }
            case ("System.Array", "Copy") when sig.ParameterTypes.Length == 5:
            {
                var len = Pop();
                var dstIdx = Pop();
                var dst = Pop();
                var srcIdx = Pop();
                var src = Pop();
                EmitArrayCopy(src, srcIdx.Expr, dst, dstIdx.Expr, len.Expr);
                return true;
            }
            // HashHelpers (Dictionary<K,V> bucket sizing) -> runtime helpers.
            case ("System.Collections.HashHelpers", "GetPrime"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_hashhelpers_getprime((int32_t)({a.Expr}))");
                return true;
            }
            case ("System.Collections.HashHelpers", "ExpandPrime"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_hashhelpers_expandprime((int32_t)({a.Expr}))");
                return true;
            }
            case ("System.Collections.HashHelpers", "GetFastModMultiplier"):
            {
                var a = Pop();
                Push(StackKind.I8, "uint64_t", $"dn2cpp_hashhelpers_getfastmodmultiplier((uint32_t)({a.Expr}))");
                return true;
            }
            case ("System.Collections.HashHelpers", "FastMod"):
            {
                var mult = Pop();
                var divisor = Pop();
                var value = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(int32_t)dn2cpp_hashhelpers_fastmod((uint32_t)({value.Expr}), (uint32_t)({divisor.Expr}), (uint64_t)({mult.Expr}))");
                return true;
            }
            // HashHelpers.Primes: a static ReadOnlySpan<int> over the cached prime
            // ladder (FrozenHashTable.CalcNumBuckets reads it). The BCL builds it via
            // RuntimeHelpers.CreateSpan over an RVA blob we don't model; hand out a
            // {ptr,len} span over the runtime's file-scope copy of the same table.
            case ("System.Collections.HashHelpers", "get_Primes"):
            {
                string spanCt = CppTypes.Of(sig.ReturnType);
                string span = NewTemp(spanCt);
                Emit($"{span}.f__reference = (int32_t*)dn2cpp_hashhelpers_primes_data();");
                Emit($"{span}.f__length = dn2cpp_hashhelpers_primes_count();");
                Push(StackKind.Struct, spanCt, span);
                return true;
            }

            // RuntimeHelpers.InitializeArray(array, RuntimeFieldHandle): the
            // ldtoken pushed the field's RVA blob pointer; copy it into the array.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "InitializeArray"):
            {
                var blob = Pop();
                var arr = Pop();
                EmitInitializeArray(arr, blob.Expr);
                return true;
            }

            // RuntimeHelpers.EnsureSufficientExecutionStack: a stack-depth guard
            // that throws InsufficientExecutionStackException when the remaining
            // stack is low. The C# compiler emits it in a record's auto-generated
            // PrintMembers (a guard against deeply nested records). We have no
            // stack probe and rely on the native stack — treat it as a no-op
            // (static, parameterless, void) so records transpile.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "EnsureSufficientExecutionStack"):
                return true;
            // The Try sibling (Regex's recursion guard via Threading.StackHelper):
            // same no-probe posture — report the stack as always sufficient. Also
            // a const-fold oracle entry (CoreIntrinsics.ConstFoldedGetter), so
            // BranchLiveness prunes the guarded CallOnEmptyStack re-dispatch arm;
            // this case still serves the MemberRef call sites (an intrinsic-mapped
            // type's MemberRefs never reach the const-fold call-site path) with
            // the matching constant.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "TryEnsureSufficientExecutionStack"):
                Push(StackKind.I4, "int32_t", "1");
                return true;

            // RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle): force T's
            // static constructor (the pattern a generic type's cctor uses so that
            // T's registration side effects run before the generic code needs
            // them). The handle's static type token (carried through
            // ldtoken -> GetTypeFromHandle -> get_TypeHandle) lowers this to the
            // target's idempotent __ensure wrapper — the same first-use guard a
            // static-field access emits; the reach scanner's ldtoken window reaches
            // the cctor body (see Compilation.ScanBodyForGenerics). A type with no
            // cctor (primitives, most structs/enums) is a no-op, matching .NET.
            // A handle with no static token cannot be dispatched (type-infos carry
            // no cctor hook) — fail loudly rather than silently skip.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "RunClassConstructor")
                when sig.ParameterTypes.Length == 1:
            {
                var h = Pop();
                if (h.TypeToken is { Kind: TypeKind.Class, Class: { } rc })
                {
                    // Statics (and their initialization) stay per real
                    // instantiation — a shared body must not run the owner's.
                    TaintIfCanonical(rc, "statics");
                    var cc = rc.StaticCctor;
                    if (cc is not null)
                    {
                        Comp.NoteCctorEnsure(cc);
                        Emit($"{cc.CppName}__ensure();");
                    }
                }
                else if (h.TypeToken is null)
                {
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: RunClassConstructor on a non-static RuntimeTypeHandle is not supported");
                }
                return true;
            }

            // The base .ctor of a user `class MyCmp : EqualityComparer<T>`. The real ctor
            // body is empty (no instance state), so this only consumes the `this` receiver
            // the subclass ctor pushed.
            case ("System.Collections.Generic.EqualityComparer", ".ctor"):
                Pop(); // the receiver — the base ctor stores nothing
                return true;

            // EqualityComparer<T> is intrinsic. The JIT devirtualizes
            // Default.Equals/GetHashCode to type-specialized ops; do the same. The runtime
            // singleton carries a synthetic concrete subtype and both comparer interface
            // relations. Their relation-only rows deliberately have no slots because the
            // generic calls lower through TryEmitComparerDispatch, while non-generic calls
            // use the same default-object lowering as Tuple structural equality.
            case ("System.Collections.Generic.EqualityComparer", "get_Default"):
            {
                if (sig.ReturnType is not { Kind: TypeKind.Class, Class: { } comparerClass }
                    || comparerClass.Context.TypeArgs.Length != 1)
                    throw new InvalidOperationException(
                        "EqualityComparer<T>.Default has no closed element type");
                var elem = comparerClass.Context.TypeArgs[0];
                var comparerInterface = Comp.IEqualityComparerInterfaceFor(elem)
                    ?? throw new InvalidOperationException(
                        "EqualityComparer<T>.Default could not resolve its closed IEqualityComparer<T>");
                var nongenericInterface = Comp.NonGenericEqualityComparerInterface()
                    ?? throw new InvalidOperationException(
                        "EqualityComparer<T>.Default could not resolve System.Collections.IEqualityComparer");
                if (!Compilation.ContainsCanonPlaceholder(comparerInterface))
                    NoteReferencedType(comparerInterface);
                NoteReferencedType(nongenericInterface);
                string comparerType, interfaceType;
                if (SharedTrial && Compilation.ContainsCanonPlaceholder(elem))
                {
                    // Both identities are per instantiation, so a shared body reads them
                    // out of rgctx slots keyed on this get_Default call — the same trade
                    // Comparer<T>.Default makes. Naming the canonical handles instead
                    // would hand every group member the placeholder's identity: the
                    // singleton is cached under it, so ONE comparer would serve
                    // Dictionary<int,…> and Dictionary<Hue,…> alike.
                    if (!CallTokenIsOpaqueMemberRef("get_Default"))
                        ThrowSharedTaint("comparer-default", comparerClass.FullName);
                    comparerType = "(const Dn2CppTypeInfo*)" + RgctxSlotAccess(
                        RgctxSlotKind.EqualityComparerDefault, _callSiteToken,
                        "comparer-default", comparerClass.FullName);
                    interfaceType = "(const Dn2CppTypeInfo*)" + RgctxSlotAccess(
                        RgctxSlotKind.EqualityComparerInterface, _callSiteToken,
                        "comparer-default", comparerInterface.FullName);
                }
                else
                {
                    comparerType = TypeInfoExpr(sig.ReturnType)
                        ?? throw new InvalidOperationException(
                            "EqualityComparer<T>.Default has no comparer type-info");
                    interfaceType = TypeInfoExpr(TypeDesc.MakeClass(comparerInterface))
                        ?? throw new InvalidOperationException(
                            "EqualityComparer<T>.Default has no IEqualityComparer<T> type-info");
                }
                string nongenericInterfaceType = TypeInfoExpr(TypeDesc.MakeClass(nongenericInterface))
                    ?? throw new InvalidOperationException(
                        "EqualityComparer<T>.Default has no non-generic IEqualityComparer type-info");
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_default_equality_comparer({comparerType}, {interfaceType}, {nongenericInterfaceType})");
                Top = Top with { NonNull = true };
                return true;
            }
            // Equals/GetHashCode on an EqualityComparer<T>. Two receivers can reach here,
            // and they are answered differently:
            //
            //  * The Default singleton (get_Default above). Its IEqualityComparer<T> row
            //    is relation-only, so object dispatch falls back to T's default equality.
            //
            //  * A real `class MyCmp : EqualityComparer<T>` receiver (constructible — its
            //    base .ctor is mapped above). Discarding it and answering default equality
            //    would be SILENTLY WRONG, so dispatch through the object's
            //    IEqualityComparer<T> interface map — the same slot probe the
            //    Dictionary._comparer path uses — with default equality as the
            //    (unreachable-for-a-real-comparer) fallback. The interface + this receiver's
            //    overrides are reached in Compilation.ReachAllocatedType.
            //
            case ("System.Collections.Generic.EqualityComparer", "GetHashCode")
                when sig.ParameterTypes.Length == 1:
            {
                var key = Pop();
                var recv = Pop();
                recv = ComparerReceiverNullChecked(recv);
                if (Comp.UserComparerInterfaceMethod(sig.ParameterTypes[0], "GetHashCode") is not { } gm)
                    Push(StackKind.I4, "int32_t", EqualityHashExpr(sig.ParameterTypes[0], key));
                else
                    EmitComparerObjectDispatch(gm, sig.ParameterTypes[0], recv, key, null);
                return true;
            }
            case ("System.Collections.Generic.EqualityComparer", "Equals")
                when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                var recv = Pop();
                recv = ComparerReceiverNullChecked(recv);
                if (Comp.UserComparerInterfaceMethod(sig.ParameterTypes[0], "Equals") is not { } em)
                    Push(StackKind.I4, "int32_t", EqualityEqualsExpr(sig.ParameterTypes[0], a, b));
                else
                    EmitComparerObjectDispatch(em, sig.ParameterTypes[0], recv, a, b);
                return true;
            }

            // System.Collections.Comparer.Compare(object, object) — body-intercepted
            // (CoreIntrinsics.LoweredComparerCompare). Lowered to dn2cpp_object_compare, which orders a
            // boxed primitive/enum/string inline and a user reference type through non-generic
            // IComparable — the real body's `x as IComparable` cannot satisfy a boxed primitive, which
            // carries no interface map. This ONE case serves BOTH a direct call (the TranslateCall
            // route) and the synthesized IComparer.Compare interface-slot body (CppEmitter ->
            // CompileCoreIntrinsicWrapper), so ArrayList.Sort() / SortedList over boxed primitives sort
            // and search instead of throwing. The stateless receiver is discarded; culture-specific
            // Comparer maps to the same ordinal order, consistent with the global ordinal stance.
            case ("System.Collections.Comparer", "Compare") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Pop(); // the Comparer receiver — the default comparer holds no state we consult
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_object_compare({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")}, &{NonGenericIComparableTiName()})");
                return true;
            }

            // Array.MaxLength: the BCL's growable-collection guard ceiling
            // (0x7FFFFFC7), used by Stack/List/etc. Grow. A plain constant.
            case ("System.Array", "get_MaxLength"):
                Push(StackKind.I4, "int32_t", "0x7FFFFFC7");
                return true;
            // Multi-dimensional array (T[,]) shape queries. A T[,] is a method
            // receiver, not an ldlen target; its Dn2CppMDArray header stores
            // per-dimension lengths/lowerBounds + rank. A 1-D array receiver
            // (Dn2CppArray, no lengths[]) routes to the vector form — GetLength(0)
            // is its length, lower bound 0, rank 1.
            case ("System.Array", "GetLength") when sig.ParameterTypes.Length == 1:
            {
                var dim = Pop();
                var arr = Pop();
                // A receiver whose static type degraded to System.Array/object
                // (reflection-produced arrays) dispatches on the runtime
                // type-info — it may be an MD array under the hood.
                Push(StackKind.I4, "int32_t", arr.CppType == "Dn2CppMDArray*"
                    ? $"((Dn2CppMDArray*)({arr.Expr}))->lengths[{dim.Expr}]"
                    : ArrayRepOfCppTypeOrNull(arr.CppType) is not null
                        ? $"dn2cpp_array_length((Dn2CppArray*)({arr.Expr}))"
                        : $"dn2cpp_array_get_length_dyn({Cast(arr, "Dn2CppObject*")}, {Cast(dim, "int32_t")})");
                return true;
            }
            case ("System.Array", "GetLowerBound") when sig.ParameterTypes.Length == 1:
            {
                var dim = Pop();
                var arr = Pop();
                Push(StackKind.I4, "int32_t", arr.CppType == "Dn2CppMDArray*"
                    ? $"((Dn2CppMDArray*)({arr.Expr}))->lowerBounds[{dim.Expr}]"
                    : ArrayRepOfCppTypeOrNull(arr.CppType) is not null
                        ? "0"
                        : $"dn2cpp_array_get_lower_bound_dyn({Cast(arr, "Dn2CppObject*")}, {Cast(dim, "int32_t")})");
                return true;
            }
            case ("System.Array", "GetUpperBound") when sig.ParameterTypes.Length == 1:
            {
                var dim = Pop();
                var arr = Pop();
                string md = $"((Dn2CppMDArray*)({arr.Expr}))";
                Push(StackKind.I4, "int32_t", arr.CppType == "Dn2CppMDArray*"
                    ? $"({md}->lowerBounds[{dim.Expr}] + {md}->lengths[{dim.Expr}] - 1)"
                    : ArrayRepOfCppTypeOrNull(arr.CppType) is not null
                        ? $"(dn2cpp_array_length((Dn2CppArray*)({arr.Expr})) - 1)"
                        : $"dn2cpp_array_get_upper_bound_dyn({Cast(arr, "Dn2CppObject*")}, {Cast(dim, "int32_t")})");
                return true;
            }
            case ("System.Array", "get_Rank"):
            {
                var arr = Pop();
                Push(StackKind.I4, "int32_t", arr.CppType == "Dn2CppMDArray*"
                    ? $"((Dn2CppMDArray*)({arr.Expr}))->rank"
                    : ArrayRepOfCppTypeOrNull(arr.CppType) is not null
                        ? "1"
                        : $"dn2cpp_array_rank_dyn({Cast(arr, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Array", "get_Length"):
            {
                var arr = Pop();
                // The SZArray arm reads the length field, which is itself the
                // dereference — dn2cpp_array_length carries the null check an inline
                // read has no room for, like the ldlen lowering.
                Push(StackKind.I4, "int32_t", arr.CppType == "Dn2CppMDArray*"
                    ? $"dn2cpp_md_total_length((Dn2CppMDArray*)({arr.Expr}))"
                    : ArrayRepOfCppTypeOrNull(arr.CppType) is not null
                        ? $"dn2cpp_array_length((Dn2CppArray*)({arr.Expr}))"
                        : $"dn2cpp_array_length_dyn({Cast(arr, "Dn2CppObject*")})");
                return true;
            }
            // Non-generic Array.GetValue/SetValue — the reflection element accessors. The
            // runtime helpers dispatch on the receiver's type-info (any SZ rep or an MD
            // array) and follow real .NET's SetValue coercion rules (primitive widening,
            // enum/Nullable exactness, null -> default).
            case ("System.Array", "GetValue") when sig.ParameterTypes.Length == 1
                && sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 }:
            {
                var idx = Pop();
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_get_value({Cast(arr, "Dn2CppObject*")}, (int64_t)({idx.Expr}))");
                return true;
            }
            case ("System.Array", "GetValue") when sig.ParameterTypes.Length == 1
                && sig.ParameterTypes[0] is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 } gvEl }:
            {
                var indices = Pop();
                var arr = Pop();
                string isLong = gvEl.Primitive == PrimitiveTypeCode.Int64 ? "1" : "0";
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_get_value_indices({Cast(arr, "Dn2CppObject*")}, {Cast(indices, "Dn2CppObject*")}, {isLong})");
                return true;
            }
            case ("System.Array", "GetValue") when sig.ParameterTypes.Length is 2 or 3
                && sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }:
            {
                int rank = sig.ParameterTypes.Length;
                var idx = new string[rank];
                for (int i = rank - 1; i >= 0; i--)
                    idx[i] = Cast(Pop(), "int32_t");
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_get_value_fixed({Cast(arr, "Dn2CppObject*")}, dn2cpp_i32s({string.Join(", ", idx)}).v, {rank})");
                return true;
            }
            case ("System.Array", "SetValue") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 }:
            {
                var idx = Pop();
                var value = Pop();
                var arr = Pop();
                Emit($"dn2cpp_array_set_value({Cast(arr, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")}, (int64_t)({idx.Expr}));");
                return true;
            }
            case ("System.Array", "SetValue") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 } svEl }:
            {
                var indices = Pop();
                var value = Pop();
                var arr = Pop();
                string isLong = svEl.Primitive == PrimitiveTypeCode.Int64 ? "1" : "0";
                Emit($"dn2cpp_array_set_value_indices({Cast(arr, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")}, {Cast(indices, "Dn2CppObject*")}, {isLong});");
                return true;
            }
            case ("System.Array", "SetValue") when sig.ParameterTypes.Length is 3 or 4
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }:
            {
                int rank = sig.ParameterTypes.Length - 1;
                var idx = new string[rank];
                for (int i = rank - 1; i >= 0; i--)
                    idx[i] = Cast(Pop(), "int32_t");
                var value = Pop();
                var arr = Pop();
                Emit($"dn2cpp_array_set_value_fixed({Cast(arr, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")}, dn2cpp_i32s({string.Join(", ", idx)}).v, {rank});");
                return true;
            }
            // Array.CreateInstance(Type, lengths…): a runtime-typed array of the
            // element's storage rep, tagged with the precise (fabricated when
            // never statically instantiated) array identity. The lowerBounds
            // form admits all-zero bounds only (non-zero -> a catchable PNSE).
            case ("System.Array", "CreateInstance") when sig.ParameterTypes.Length is 2 or 3 or 4
                && sig.ParameterTypes.Skip(1).All(p => p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }):
            {
                int rank = sig.ParameterTypes.Length - 1;
                // A fixed-arity rank>1 CreateInstance mints an MD array with no
                // `new T[,]` site anywhere, so it keys the shared rank>=2 dispatch
                // map itself. The int[]-lengths overloads below stay
                // un-noted: their rank is a run-time value, so in a program whose
                // ONLY MD mint is such an overload the map is not installed and an
                // interface dispatch on the result keeps the loud abort (once any
                // other site notes, the one shared map serves those arrays too).
                if (rank > 1)
                    _c.NoteMdArrayUse();
                var lens = new string[rank];
                for (int i = rank - 1; i >= 0; i--)
                    lens[i] = Cast(Pop(), "int32_t");
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_create_instance({Cast(t, "Dn2CppType*")}, dn2cpp_i32s({string.Join(", ", lens)}).v, {rank})");
                return true;
            }
            // Array.CreateInstanceFromArrayType(Type arrayType, length(s)): the
            // element comes off the ARRAY type's identity.
            case ("System.Array", "CreateInstanceFromArrayType") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }:
            {
                var len = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_create_instance_from_arraytype({Cast(t, "Dn2CppType*")}, dn2cpp_i32s({Cast(len, "int32_t")}).v, 1)");
                return true;
            }
            case ("System.Array", "CreateInstance") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } }:
            {
                var lengths = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_create_instance_lengths({Cast(t, "Dn2CppType*")}, {Cast(lengths, "Dn2CppArrayI4*")}, nullptr)");
                return true;
            }
            case ("System.Array", "CreateInstance") when sig.ParameterTypes.Length == 3
                && sig.ParameterTypes[1] is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 } }
                && sig.ParameterTypes[2] is { Kind: TypeKind.SZArray }:
            {
                var bounds = Pop();
                var lengths = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_array_create_instance_lengths({Cast(t, "Dn2CppType*")}, {Cast(lengths, "Dn2CppArrayI4*")}, {Cast(bounds, "Dn2CppArrayI4*")})");
                return true;
            }
            // Non-generic Array.Reverse(Array[, index, length]): byte-wise
            // element swaps over the runtime element view.
            case ("System.Array", "Reverse") when sig.ParameterTypes.Length == 1
                && sig.ParameterTypes[0] is { Kind: TypeKind.Class or TypeKind.External }:
            {
                var arr = Pop();
                string tmpArr = NewTemp("Dn2CppObject*");
                Emit($"{tmpArr} = {Cast(arr, "Dn2CppObject*")};");
                Emit($"dn2cpp_array_reverse_dyn({tmpArr}, 0, dn2cpp_array_length_dyn({tmpArr}));");
                return true;
            }
            case ("System.Array", "Reverse") when sig.ParameterTypes.Length == 3
                && sig.ParameterTypes[0] is { Kind: TypeKind.Class or TypeKind.External }:
            {
                var len = Pop();
                var idx = Pop();
                var arr = Pop();
                Emit($"dn2cpp_array_reverse_dyn({Cast(arr, "Dn2CppObject*")}, {Cast(idx, "int32_t")}, {Cast(len, "int32_t")});");
                return true;
            }
            // Non-generic Array.IndexOf/LastIndexOf(Array, object [, int [, int]]) —
            // the untyped reflection form (an ArrayList/IList search, an
            // object[] scanned as System.Array). The sibling of the generic
            // Array.IndexOf<T> intrinsic, and the only one whose element type is
            // unknown until run time: box each element through the same accessor
            // Array.GetValue uses and compare with Object.Equals(object) — which is
            // what the real body's ObjectEqualityComparer does.
            case ("System.Array", "IndexOf") when sig.ParameterTypes.Length is 2 or 3 or 4
                && sig.ParameterTypes[1].IsObject:
            case ("System.Array", "LastIndexOf") when sig.ParameterTypes.Length is 2 or 3 or 4
                && sig.ParameterTypes[1].IsObject:
            {
                bool last = name == "LastIndexOf";
                var count = sig.ParameterTypes.Length >= 4 ? Pop() : null;
                var start = sig.ParameterTypes.Length >= 3 ? Pop() : null;
                var value = Pop();
                var array = Pop();
                string arr = NewTemp("Dn2CppObject*");
                Emit($"{arr} = {Cast(array, "Dn2CppObject*")};");
                string val = NewTemp("Dn2CppObject*");
                Emit($"{val} = {Cast(value, "Dn2CppObject*")};");
                string len = $"dn2cpp_array_length_dyn({arr})";
                string first = NewTemp("int32_t");
                Emit($"{first} = {(start is null ? (last ? $"{len} - 1" : "0") : Cast(start, "int32_t"))};");
                string cnt = NewTemp("int32_t");
                Emit($"{cnt} = {(count is not null ? Cast(count, "int32_t")
                    : last ? (start is null ? len : $"({len} == 0 ? 0 : {first} + 1)")
                    : $"{len} - {first}")};");
                string res = NewTemp("int32_t");
                string ix = NewTemp("int32_t");
                string end = NewTemp("int32_t");
                Emit($"{res} = -1;");
                Emit($"{end} = {(last ? $"{first} - {cnt}" : $"{first} + {cnt}")};");
                Emit(last
                    ? $"for ({ix} = {first}; {ix} > {end}; {ix}--) {{"
                    : $"for ({ix} = {first}; {ix} < {end}; {ix}++) {{");
                Emit($"    if (dn2cpp_object_equals(dn2cpp_array_get_value({arr}, (int64_t){ix}), {val})) {{ {res} = {ix}; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return true;
            }
            // Array.Clear(array, index, length): zero a run of elements.
            case ("System.Array", "Clear") when sig.ParameterTypes.Length == 3:
            {
                var len = Pop();
                var idx = Pop();
                var arr = Pop();
                EmitArrayClear(arr, idx.Expr, len.Expr);
                return true;
            }
            // Array.Clear(array): zero the whole array (Dictionary.Clear).
            case ("System.Array", "Clear") when sig.ParameterTypes.Length == 1:
            {
                var arr = Pop();
                // Reading the length as `((Dn2CppArray*)a)->length` beside the memset
                // dereferences a null array before anything can check it — a SIGSEGV
                // where .NET throws ArgumentNullException (Clear is STATIC; the array
                // is an argument). Check once into a temp and take the length off that:
                // the length expression and the memset's own operand guard would
                // otherwise be unsequenced arguments of one call, and two checks on
                // one operand must not be able to disagree about which fires.
                //
                // A receiver whose static rep is not concrete may be an MD array,
                // whose element count is the product of its lengths and whose
                // `length` field is its rank — so ask the shape question exactly
                // where the static type cannot answer it.
                if (ArrayRepOfCppTypeOrNull(arr.CppType) is null)
                {
                    string tl = NewTemp("int32_t");
                    Emit($"{tl} = dn2cpp_array_total_length(dn2cpp_array_require_arg((Dn2CppObject*)({arr.Expr})));");
                    EmitArrayClear(arr, "0", tl, ArrayOperandKind.Unchecked);
                    return true;
                }
                string al = NewTemp("Dn2CppArray*");
                Emit($"{al} = dn2cpp_array_require_arg((Dn2CppArray*)({arr.Expr}));");
                EmitArrayClear(arr, "0", $"{al}->length", ArrayOperandKind.Unchecked);
                return true;
            }
            // Array.Clone — a shallow copy returned as object. The
            // real body routes through Array.Copy / MethodTable internals; emit a
            // rep-keyed runtime clone (same precise type-info + length + element
            // block) instead. The clone keeps the source's type-info so its
            // GetType/element type match (verified vs real.NET).
            case ("System.Array", "Clone") when sig.ParameterTypes.Length == 0:
            {
                var arr = Pop();
                // A receiver whose static type degraded to a non-array (a `?.`
                // null-propagation merge unifies the byte[] arm with the null arm as
                // object) dispatches on the runtime type-info.
                string clone = ArrayRepOfCppTypeOrNull(arr.CppType) switch
                {
                    ArrRep.I4 => $"dn2cpp_array_clone_i4({Cast(arr, "Dn2CppArrayI4*")})",
                    ArrRep.Ref => $"dn2cpp_array_clone_ref({Cast(arr, "Dn2CppArrayRef*")})",
                    ArrRep.N => $"dn2cpp_array_clone_n({Cast(arr, "Dn2CppArrayN*")})",
                    _ => $"dn2cpp_array_clone_dyn({Cast(arr, "Dn2CppObject*")})",
                };
                Push(StackKind.Ref, "Dn2CppObject*", $"(Dn2CppObject*)({clone})");
                return true;
            }
            // Array.CopyTo(Array dest, int index) — copy this array's
            // elements into dest starting at index. The real body does a covariance
            // check via MethodTable internals; emit a typed memmove keyed on the
            // source's static C++ rep (source and dest share the element type), as
            // Array.Copy does. Only the int-index form is mapped.
            case ("System.Array", "CopyTo") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }:
            {
                var idx = Pop();   // destination start index
                var dst = Pop();   // destination Array
                var src = Pop();   // this array (the source)
                // The Array.Clear(array) shape with the operands' exception types
                // swapped round: the length comes off the RECEIVER, so a null one is
                // a NullReferenceException (checked once into a temp, as there too),
                // while the destination is an argument.
                string sl = NewTemp("Dn2CppArray*");
                Emit($"{sl} = dn2cpp_array_require_receiver((Dn2CppArray*)({src.Expr}));");
                // CopyTo screens the DESTINATION's rank itself rather than
                // leaving it to the Copy underneath, so the refusal is an
                // ArgumentException and it precedes Copy's own RankException on
                // the receiver. It is emitted here rather than passed down as an
                // operand guard because the _dyn fallback never sees those.
                string dl = NewTemp("Dn2CppObject*");
                Emit($"{dl} = dn2cpp_array_require_copyto_dest((Dn2CppObject*)({dst.Expr}));");
                // A non-concrete receiver may be an MD array, whose `length` field
                // is its rank.
                string srcLen = ArrayRepOfCppTypeOrNull(src.CppType) is null
                    ? $"dn2cpp_array_total_length((Dn2CppObject*){sl})"
                    : $"{sl}->length";
                EmitArrayCopy(src, "0", new StackEntry(dl, StackKind.Ref, "Dn2CppObject*"),
                    idx.Expr, srcLen, ArrayOperandKind.Unchecked, ArrayOperandKind.Unchecked);
                return true;
            }

            // Non-generic Array.Sort(Array[, index, length][, IComparer]) / Array.BinarySearch(
            // Array, object[, index, length][, IComparer]) — the boxed-element search/sort surface of
            // the old non-generic BCL collections (ArrayList, SortedList, PropertyDescriptorCollection,
            // …). The element type is unknown until run time, so the order is the shared boxed order
            // (dn2cpp_object_compare through non-generic System.IComparable, or an explicit
            // System.Collections.IComparer), consistent with TryCompareLValue and the generic
            // Array.Sort<T>/BinarySearch<T> path. Classified by parameter TYPE, never arity — the
            // pair-sort Sort(Array keys, Array items) collides on arity with Sort(Array, IComparer).
            case ("System.Array", "Sort"):
                return TryEmitNonGenericArraySort(sig);
            case ("System.Array", "BinarySearch"):
                return TryEmitNonGenericArrayBinarySearch(sig);

            case ("System.Object", ".ctor"):
            case ("System.Attribute", ".ctor"):
                Pop(); // this — base ctor is a no-op
                return true;

            // ---- System.Attribute: the opaque intrinsic base's instance surface ----
            // Attribute is an opaque intrinsic base type (s_intrinsicTypes): its real
            // GetHashCode/IsDefaultAttribute walk the instance's fields by reflection,
            // which dn2cpp does not transpile. A base-token call on an instance member the
            // subclass did not override lands here. (An override is bound under the
            // SUBCLASS's token and dispatches through its transpiled vtable.)
            //
            // GetHashCode: the Object.GetHashCode lowering — dispatch a wired override
            // via the type-info slot, else the identity hash. DECLARED DIVERGENCE: real
            // .NET hashes the first non-null field, so the VALUE differs; ours is stable
            // per instance, which is what an attribute used as a hash key needs. Gates
            // must never assert the raw value.
            case ("System.Attribute", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_gethashcode({Cast(o, "Dn2CppObject*")})");
                return true;
            }
            // IsDefaultAttribute: the base implementation returns false unconditionally;
            // matching it is exact for any subclass that does not override. A run-time
            // instance whose type DOES override (e.g. BrowsableAttribute) still answers
            // false through a base-token call — an approximation, the same posture as
            // the other opaque-base stubs (no per-type dispatch slot exists for it).
            case ("System.Attribute", "IsDefaultAttribute") when sig.ParameterTypes.Length == 0:
                Pop(); // this — the answer is a constant
                Push(StackKind.I4, "int32_t", "0");
                return true;
            // get_TypeId: the base implementation returns this.GetType(); mirror the
            // Object.GetType lowering — the runtime Type off the object header. Same
            // override caveat as IsDefaultAttribute, and the same base-exact answer.
            case ("System.Attribute", "get_TypeId") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppType*",
                    $"dn2cpp_get_type_from_handle(((Dn2CppObject*)({o.Expr}))->type)");
                return true;
            }
            // Match(object): the base implementation is `return Equals(obj)`, so mirror
            // the Object.Equals lowering — dispatch a wired Equals override via the
            // type-info slot, else reference equality (dn2cpp_object_equals). DECLARED
            // DIVERGENCE, the same opaque-base approximation as GetHashCode above: real
            // .NET's Attribute.Equals walks the instance's fields by reflection, so two
            // distinct-but-field-equal non-overriding attributes answer false here where
            // .NET says true.
            case ("System.Attribute", "Match") when sig.ParameterTypes.Length == 1:
            {
                var obj = Pop();
                var self = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_object_equals({Cast(self, "Dn2CppObject*")}, {Cast(obj, "Dn2CppObject*")})");
                return true;
            }

            default:
                return false;
        }
    }

    // ---- Non-generic, boxed-element Array.Sort / Array.BinarySearch ----
    //
    // The element type is unknown until run time, so the order is the shared boxed order —
    // dn2cpp_object_compare through the non-generic System.IComparable, or an explicit
    // System.Collections.IComparer — consistent with TryCompareLValue and the generic
    // Array.Sort<T>/BinarySearch<T> path. Overloads are classified by parameter TYPE, never arity:
    // the pair-sort Sort(Array keys, Array items) collides on arity with Sort(Array, IComparer), so
    // a count-based reading pops the items array as a comparer. Reachability marks the two interface
    // methods used and emits their type-infos (Compilation.ReachNonGenericArraySortSearch).

    private static bool IsNonGenericArrayReceiver(TypeDesc p) => p.Kind is TypeKind.Class or TypeKind.External;
    private static bool IsNonGenericComparerParam(TypeDesc p) =>
        p is { Kind: TypeKind.Class, Class.FullName: "System.Collections.IComparer" };
    private static bool IsInt32Param(TypeDesc p) =>
        p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 };

    /// <summary>The <c>ti_</c> of the non-generic <c>System.IComparable</c> — the default
    /// (comparerless / null-comparer) order's interface. Every body-emit route that names
    /// <c>&amp;ti_System_IComparable</c> (the Comparer.Compare intercept's direct call, the
    /// synthesized IComparer.Compare interface slot, an ldftn/ldvirtftn off Comparer.Compare,
    /// the default non-generic Array.Sort/BinarySearch order) funnels through this ONE helper,
    /// so noting the type referenced here lays its type-info down whichever route reached it,
    /// making the reference link structural rather than dependent on the emit-set interface
    /// closure happening to pull non-generic IComparable in. Idempotent with the Call/Callvirt
    /// reachability hook (Compilation.NoteNonGenericIComparableUsed): both add the same
    /// ClassInfo to the deduping ReferencedTypes set.</summary>
    private string NonGenericIComparableTiName()
    {
        var ic = Comp.FindClassByFullName("System.IComparable")
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: non-generic Array.Sort/BinarySearch "
                + "needs System.IComparable, which is not loaded");
        // Co-locate the emit guarantee with the site that NAMES the symbol (see summary).
        Comp.NoteReferencedType(ic);
        return ic.CppTypeInfoName;
    }

    /// <summary>The non-generic <c>System.Collections.IComparer</c>'s <c>ti_</c> and its
    /// <c>Compare(object, object)</c> interface slot, for the explicit-comparer arm's dispatch.</summary>
    private (string tiName, int slot) NonGenericIComparerDispatch()
    {
        var icmp = Comp.FindClassByFullName("System.Collections.IComparer")
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: the IComparer overload needs "
                + "System.Collections.IComparer, which is not loaded");
        Comp.EnsureCompleted(icmp);
        var compare = icmp.Methods.FirstOrDefault(m => m.Name == "Compare")
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: System.Collections.IComparer has no Compare");
        return (icmp.CppTypeInfoName, compare.VtableSlot);
    }

    /// <summary>A catchable PlatformNotSupportedException for an out-of-scope Array sort/search shape
    /// (the pair-sort, a multidimensional or otherwise exotic array). Loud, never a wrong answer — the
    /// EmitCustomComparerGuard posture for a shape declined by TYPE.</summary>
    private void EmitArraySortSearchUnsupported(string api, int arity) =>
        Emit($"dn2cpp_throw_platform_not_supported(\"{api}: this non-generic overload (arity {arity}) is "
            + "not supported - the pair-sort (Array keys, Array items), multidimensional, and other exotic "
            + "Array shapes are out of scope. Use the generic Array.Sort<T> / Array.BinarySearch<T>, or an IComparer.\");");

    private bool TryEmitNonGenericArraySort(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        bool hasComparer = ps.Length > 0 && IsNonGenericComparerParam(ps[^1]);
        int coreLen = ps.Length - (hasComparer ? 1 : 0);
        if (coreLen < 1 || !IsNonGenericArrayReceiver(ps[0]))
            return false;
        bool whole = coreLen == 1;
        bool range = coreLen == 3 && IsInt32Param(ps[1]) && IsInt32Param(ps[2]);
        if (!whole && !range)
        {
            // Pair-sort / exotic: decline by SHAPE. The operand count is known from the signature, so
            // pop exactly that many (never a guessed count) to balance the eval stack, then throw.
            for (int i = 0; i < ps.Length; i++)
                Pop();
            EmitArraySortSearchUnsupported("Array.Sort", ps.Length);
            return true;
        }
        // Commit. Pop in reverse IL order, then spill forward so operand side effects keep their order.
        var comparer = hasComparer ? Pop() : null;
        var countE = range ? Pop() : null;
        var indexE = range ? Pop() : null;
        var arrE = Pop();

        string arrT = NewTemp("Dn2CppObject*");
        Emit($"{arrT} = {Cast(arrE, "Dn2CppObject*")};");
        string indexT = "0", lengthT;
        if (range)
        {
            string it = NewTemp("int32_t");
            Emit($"{it} = {Cast(indexE!, "int32_t")};");
            string ct = NewTemp("int32_t");
            Emit($"{ct} = {Cast(countE!, "int32_t")};");
            indexT = it;
            lengthT = ct;
        }
        else
            lengthT = $"dn2cpp_array_length_dyn({arrT})";
        string cmpArgs;
        if (comparer is { } cmp)
        {
            string cmpT = NewTemp("Dn2CppObject*");
            Emit($"{cmpT} = {Cast(cmp, "Dn2CppObject*")};");
            var (icmpTi, slot) = NonGenericIComparerDispatch();
            cmpArgs = $"{cmpT}, &{icmpTi}, {slot}";
        }
        else
            cmpArgs = "nullptr, nullptr, 0";
        Emit($"dn2cpp_array_sort_object({arrT}, {indexT}, {lengthT}, &{NonGenericIComparableTiName()}, {cmpArgs});");
        return true;
    }

    private bool TryEmitNonGenericArrayBinarySearch(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        bool hasComparer = ps.Length > 0 && IsNonGenericComparerParam(ps[^1]);
        int coreLen = ps.Length - (hasComparer ? 1 : 0);
        if (coreLen < 2 || !IsNonGenericArrayReceiver(ps[0]))
            return false;
        bool whole = coreLen == 2 && ps[1].IsObject;
        bool range = coreLen == 4 && IsInt32Param(ps[1]) && IsInt32Param(ps[2]) && ps[3].IsObject;
        if (!whole && !range)
        {
            for (int i = 0; i < ps.Length; i++)
                Pop();
            EmitArraySortSearchUnsupported("Array.BinarySearch", ps.Length);
            Push(StackKind.I4, "int32_t", "0"); // returns int; the push is unreachable after the throw
            return true;
        }
        // Commit. Pop in reverse IL order, then spill forward.
        var comparer = hasComparer ? Pop() : null;
        var valueE = Pop();
        var countE = range ? Pop() : null;
        var indexE = range ? Pop() : null;
        var arrE = Pop();

        string arrT = NewTemp("Dn2CppObject*");
        Emit($"{arrT} = {Cast(arrE, "Dn2CppObject*")};");
        string lo = NewTemp("int32_t"), hi = NewTemp("int32_t");
        if (range)
        {
            string it = NewTemp("int32_t");
            Emit($"{it} = {Cast(indexE!, "int32_t")};");
            string ct = NewTemp("int32_t");
            Emit($"{ct} = {Cast(countE!, "int32_t")};");
            Emit($"{lo} = {it};");
            Emit($"{hi} = {it} + {ct} - 1;");
        }
        else
        {
            Emit($"{lo} = 0;");
            Emit($"{hi} = dn2cpp_array_length_dyn({arrT}) - 1;");
        }
        string valT = NewTemp("Dn2CppObject*");
        Emit($"{valT} = {Cast(valueE, "Dn2CppObject*")};");
        string cmpT = "nullptr", icmpTi = "";
        int slot = 0;
        if (comparer is { } cmp)
        {
            cmpT = NewTemp("Dn2CppObject*");
            Emit($"{cmpT} = {Cast(cmp, "Dn2CppObject*")};");
            (icmpTi, slot) = NonGenericIComparerDispatch();
        }
        // .NET's Array.BinarySearch loop, exactly: mid = lo + ((hi-lo)>>1); compare(element, value) in
        // that direction; return the found index or ~lo. Default compare = dn2cpp_object_compare
        // (non-generic IComparable); an explicit comparer (null at run time IS Comparer.Default) =
        // the IComparer.Compare dispatch, else the default.
        string icName = NonGenericIComparableTiName();
        string e = NewTemp("Dn2CppObject*");
        string mid = NewTemp("int32_t"), ord = NewTemp("int32_t"), found = NewTemp("int32_t");
        Emit($"{found} = -1;");
        Emit($"while ({lo} <= {hi}) {{");
        Emit($"    {mid} = {lo} + (({hi} - {lo}) >> 1);");
        Emit($"    {e} = dn2cpp_array_get_value({arrT}, (int64_t){mid});");
        string def = $"dn2cpp_object_compare({e}, {valT}, &{icName})";
        string cmpExpr = comparer is null
            ? def
            : $"({cmpT} != nullptr ? ((int32_t (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*))"
              + $"dn2cpp_resolve_interface({cmpT}->type, &{icmpTi})[{slot}])({cmpT}, {e}, {valT}) : {def})";
        Emit($"    {ord} = {cmpExpr};");
        Emit($"    if ({ord} == 0) {{ {found} = {mid}; break; }}");
        Emit($"    if ({ord} < 0) {lo} = {mid} + 1; else {hi} = {mid} - 1;");
        Emit("}");
        Push(StackKind.I4, "int32_t", $"({found} >= 0 ? {found} : ~{lo})");
        return true;
    }
}
