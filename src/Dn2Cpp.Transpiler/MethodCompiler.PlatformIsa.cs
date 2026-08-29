using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>The <see cref="InterceptEmitArm.PlatformIsa"/> mouth: lowers a call to a
    /// static member of a CoreLib platform-ISA facade (<see cref="ClassInfo.PlatformIsa"/>
    /// stamped) and never falls through to <see cref="EmitManagedCall"/>. Total over the
    /// <see cref="CoreIntrinsics.MdPlatformIsa"/> predicate:
    /// <list type="bullet">
    /// <item><c>IsSupported</c> pushes the runtime capability token of a lowered family, or
    /// constant 0 — the verdict <see cref="BranchLiveness"/> already pruned the guarded arm
    /// by, so the two can never disagree.</item>
    /// <item>An instruction of a lowered family calls its <c>dn2cpp_isa_*</c> helper:
    /// vectors by value (the helper takes <c>const Dn2CppVectorN&amp;</c>), scalars and
    /// pointers by value, a ValueTuple operand as its items in order (consecutive helper
    /// parameters), a ValueTuple result through trailing out-pointers in item order with
    /// a void helper.</item>
    /// <item>An instruction of an unlowered family throws PlatformNotSupportedException
    /// after consuming its operands — what real .NET does when <c>IsSupported</c> is
    /// false — and pushes a typed placeholder so the stack shape stays well-typed for
    /// the unreachable continuation.</item>
    /// </list>
    /// A by-value struct the call names (a ValueTuple operand or result) is only ever
    /// named HERE — the facade body that would have named it is cut — so it is force-
    /// emitted with its real fields; the unlowered path's placeholder needs the struct
    /// definition exactly as the lowered path's temp does.</summary>
    private void EmitPlatformIsaCall(MethodInfo callee)
    {
        var family = callee.DeclaringClass.PlatformIsa
            ?? throw new InvalidOperationException(
                $"{callee.DeclaringClass.FullName}.{callee.Name}: PlatformIsa arm on a non-ISA type");
        string member = $"{CoreIntrinsics.IsaContractName(family)}.{callee.Name}";
        if (!callee.IsStatic)
            throw new NotSupportedException($"{member}: a platform-ISA facade has no instance members");
        var sig = callee.Signature;
        var ps = sig.ParameterTypes;
        bool lowered = CoreIntrinsics.IsaLowered(family);
        if (callee.Name == "get_IsSupported" && ps.Length == 0)
        {
            Push(StackKind.I4, "int32_t", lowered ? CoreIntrinsics.IsaSupportedToken(family) : "0");
            return;
        }

        var ret = sig.ReturnType;
        RequireIsaStruct(ret);
        foreach (var p in ps)
            RequireIsaStruct(p);

        // Operands, right-to-left. A vector operand may sit on the stack as an address
        // (an `in` parameter or a ldloca'd local): dereference it into the by-value type.
        // A ValueTuple operand is staged in a temp and handed over as its items.
        var operands = new List<string>[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
        {
            var e = Pop();
            string ct = CppTypes.Of(ps[i]);
            string val = CppTypes.KindOf(ps[i]) == StackKind.Struct && e.Kind == StackKind.Ptr
                ? $"(*({ct}*)({e.Expr}))"
                : Cast(e, ct);
            operands[i] = new List<string>();
            if (ps[i] is { Kind: TypeKind.Class, Class: { } tup } && CoreIntrinsics.IsIsaValueTuple(tup))
            {
                string tt = NewTemp(ct);
                Emit($"{tt} = {val};");
                for (int j = 0; j < tup.Context.TypeArgs.Length; j++)
                    operands[i].Add($"{tt}.{TupleItemCppName(tup, j)}");
            }
            else
            {
                operands[i].Add(val);
            }
        }
        var args = new List<string>();
        foreach (var o in operands)
            args.AddRange(o);
        bool isVoid = ret is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void };

        if (!lowered)
        {
            Emit($"dn2cpp_throw_platform_not_supported(\"{family.QualifiedName}.{callee.Name}: "
                + "instruction set not lowered\");");
            if (!isVoid)
            {
                // Unreachable; stack typing only.
                string ct = CppTypes.Of(ret);
                var kind = CppTypes.KindOf(ret);
                Push(kind, ct, kind switch
                {
                    StackKind.Struct => CppTypes.ZeroInitExpr(ct),
                    StackKind.Ref or StackKind.Ptr => $"(({ct})nullptr)",
                    _ => $"(({ct})0)",
                });
            }
            return;
        }

        string helper = CoreIntrinsics.IsaHelperName(family, callee.Name, sig);
        if (ret is { Kind: TypeKind.Class, Class: { } tupleCls } && CoreIntrinsics.IsIsaValueTuple(tupleCls))
        {
            string tupleCpp = CppTypes.Of(ret);
            string tv = NewTemp(tupleCpp);
            Emit($"{tv} = {tupleCpp}{{}};");
            for (int i = 0; i < tupleCls.Context.TypeArgs.Length; i++)
                args.Add($"&{tv}.{TupleItemCppName(tupleCls, i)}");
            Emit($"{helper}({string.Join(", ", args)});");
            Push(StackKind.Struct, tupleCpp, tv);
            return;
        }

        string call = $"{helper}({string.Join(", ", args)})";
        if (isVoid)
        {
            Emit(call + ";");
            return;
        }
        Push(CppTypes.KindOf(ret), CppTypes.Of(ret), call);
    }

    /// <summary>A non-intrinsic by-value struct an ISA call names is emitted with its real
    /// fields (<see cref="Compilation.NoteForceEmit"/>): nothing else in the tree names
    /// it once the facade body is cut, and an opaque shell would leave the temp or
    /// placeholder naming an undefined type.</summary>
    private void RequireIsaStruct(TypeDesc t)
    {
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false, IntrinsicCppName: null } c })
        {
            Comp.EnsureCompleted(c);
            _c.NoteForceEmit(c);
        }
    }

    /// <summary>The C++ field name of <c>Item{i+1}</c> on a completed ValueTuple class.</summary>
    private static string TupleItemCppName(ClassInfo tupleCls, int i)
    {
        string item = $"Item{i + 1}";
        return tupleCls.Fields.First(fl => fl.Name == item).CppName;
    }
}
