using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal List<ClassInfo> InvariantCompareInfoTypes { get; } = new();
    private readonly HashSet<MethodInfo> _invariantCompareInfoDispatch = new();

    internal void NoteInvariantCompareInfoFactory(MethodInfo getter)
    {
        if (!CoreIntrinsics.IsInvariantCompareInfoGetter(getter))
            throw new NotSupportedException("Invariant CompareInfo requires its exact getter and field layout");
        var cls = getter.Signature.ReturnType.Class!;
        if (!CoreIntrinsics.TryGetInvariantCompareInfoFields(cls, out var name, out var sortName)
            || name.Type is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            || sortName.Type is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String })
            throw new NotSupportedException("Invariant CompareInfo requires its exact string field layout");
        if (InvariantCompareInfoTypes.Contains(cls)) return;
        InvariantCompareInfoTypes.Add(cls);
        ReachAllocatedType(cls);
    }
    internal void NoteInvariantCompareInfoDispatchDemand(MethodInfo getter)
    {
        if (getter.IsVirtual && CoreIntrinsics.IsInvariantCompareInfoGetter(getter))
        {
            _invariantCompareInfoDispatch.Add(getter);
            ReachUsedVirtual(getter);
        }
    }

    internal void NoteInvariantCompareInfoReflectionRow(MethodInfo getter)
    {
        if ((_reflectionInvokeUsed || _reflectionDelegateBindScanned || NeedsReflectionDelegateBind)
            && getter.IsVirtual && CoreIntrinsics.IsInvariantCompareInfoGetter(getter))
            _invariantCompareInfoDispatch.Add(getter);
    }

    // A branch-free immediate producer proves the receiver without a dataflow model.
    // Normal type initialization and virtual reachability remain unchanged.
    internal bool IsNativeInvariantCompareInfoReceiver(MethodInfo caller, int offset, MethodInfo getter)
    {
        if (caller.Rva == 0 || !getter.IsVirtual || !CoreIntrinsics.IsInvariantCompareInfoGetter(getter)) return false;
        var body = caller.Module.PE.GetMethodBody(caller.Rva);
        if (body.ExceptionRegions.Length != 0) return false;
        var instructions = ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
        if (instructions.Any(i => ILDecoder.IsBranch(i.OpCode) || i.OpCode == ILOpCode.Switch)) return false;
        for (int i = 1; i < instructions.Count; i++)
        {
            var call = instructions[i];
            var before = instructions[i - 1];
            if (call.Offset != offset || call.OpCode != ILOpCode.Callvirt || before.OpCode != ILOpCode.Call) continue;
            MethodInfo? Resolve(int token)
            {
                var handle = SRME.EntityHandle(token);
                return handle.Kind == HandleKind.MethodDefinition
                    ? caller.Module.MethodMap.GetValueOrDefault((MethodDefinitionHandle)handle)
                    : handle.Kind == HandleKind.MemberReference
                        ? ResolveMemberRefMethod(caller.Module, (MemberReferenceHandle)handle, caller.Context) : null;
            }
            var producer = Resolve(before.Token);
            if (Resolve(call.Token) != getter || producer is null || producer.DeclaringClass != getter.DeclaringClass
                || producer.Name != "get_InvariantCulture" || producer.Rva == 0
                || producer.ImplAttributes != MethodImplAttributes.IL || producer.PInvoke is not null
                || producer.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
                || producer.Signature.Header.RawValue != 0 || producer.Signature.GenericParameterCount != 0
                || producer.Signature.ParameterTypes.Length != 0
                || producer.Signature.ReturnType is not { Kind: TypeKind.Class, Class: { } result }
                || result != getter.DeclaringClass)
                return false;
            var blob = producer.Module.Reader.GetBlobReader(producer.Module.Reader.GetMethodDefinition(producer.Handle).Signature);
            return blob.RemainingBytes >= 4 && blob.ReadByte() == 0 && blob.ReadByte() == 0
                && blob.ReadByte() == 0x12 && blob.TryReadCompressedInteger(out int token)
                && token > 0 && (token >> 2) != 0 && (token & 3) != 3 && blob.RemainingBytes == 0;
        }
        return false;
    }

    internal void ValidateInvariantCompareInfoDispatch()
    {
        foreach (var getter in _invariantCompareInfoDispatch.ToList())
        {
            if (!getter.IsVirtual || !CoreIntrinsics.IsInvariantCompareInfoGetter(getter)) continue;
            foreach (var cls in DispatchReceivers(getter.DeclaringClass))
            {
                if (cls == getter.DeclaringClass || !IsAllocated(cls)) continue;
                bool descendant = false;
                for (var parent = cls.BaseClass; parent is not null; parent = parent.BaseClass)
                    if (parent == getter.DeclaringClass) { descendant = true; break; }
                if (!descendant) continue;
                EnsureCompleted(cls);
                int slot = getter.VtableSlot;
                if (slot < 0 || slot >= cls.Vtable.Count || cls.Vtable[slot] is not { } implementation
                    || implementation != getter)
                    throw new NotSupportedException(
                        $"CultureInfo.CompareInfo override dispatch is not supported for {cls.FullName}; "
                        + "use the invariant base getter without virtual, address or reflection dispatch to managed overrides");
            }
        }
    }

}
