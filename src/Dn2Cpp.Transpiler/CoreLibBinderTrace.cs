using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// The native runtime has no CoreCLR binder, EventPipe or ETW provider. Only
// the guarded trace arms can be elided; the resolver and probe code stays IL.
internal static class CoreLibBinderTrace
{
    private const string Context = "System.Runtime.Loader.AssemblyLoadContext";
    private const MethodAttributes StaticInternal = MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig;
    private const MethodAttributes InstancePrivate = MethodAttributes.Private | MethodAttributes.HideBySig;

    internal enum Kind { None, Resolving, AssemblyResolve, Satellite, LoadFrom }

    internal static Kind ClassifyGuardCaller(MethodInfo method, MethodBodyBlock body)
    {
        if (method.Name is not ("GetFirstResolvedAssemblyFromResolvingEvent" or "InvokeResolveEvent" or "ResolveSatelliteAssembly")
            || body.ExceptionRegions.Length != 0 || !IsContextBody(method))
            return Kind.None;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        if (definition.Attributes == InstancePrivate)
        {
            if (method.Name == "GetFirstResolvedAssemblyFromResolvingEvent"
                && HasSignature(reader, definition, 0x20, "System.Reflection.Assembly", "System.Reflection.AssemblyName"))
                return Kind.Resolving;
            if (method.Name == "ResolveSatelliteAssembly"
                && HasSignature(reader, definition, 0x20, "System.Reflection.RuntimeAssembly", "System.Reflection.AssemblyName"))
                return Kind.Satellite;
        }
        if (method.Name == "InvokeResolveEvent" && definition.Attributes == (InstancePrivate | MethodAttributes.Static)
            && HasSignature(reader, definition, 0, "System.Reflection.RuntimeAssembly",
                "System.ResolveEventHandler", "System.Reflection.RuntimeAssembly", "System.String"))
            return Kind.AssemblyResolve;
        return Kind.None;
    }

    internal static bool IsTelemetryWrapper(MethodInfo method)
    {
        if (method.Name is not ("TraceResolvingHandlerInvoked" or "TraceAssemblyResolveHandlerInvoked"
            or "TraceSatelliteSubdirectoryPathProbed" or "TraceAssemblyLoadFromResolveHandlerInvoked")
            || !IsContextBody(method))
            return false;
        var reader = method.Module.Reader;
        return WrapperKind(reader, reader.GetMethodDefinition(method.Handle)) != Kind.None;
    }

    internal static bool IsTelemetryWindow(MethodInfo caller, Kind kind, List<Instruction> il, int guard)
    {
        var reader = caller.Module.Reader;
        if (il[guard].OpCode != ILOpCode.Call || !TryMethodDefinition(reader, il[guard], out var condition)
            || condition.GetDeclaringType() != caller.DeclaringClass.Handle
            || !reader.StringComparer.Equals(condition.Name, "IsTracingEnabled")
            || condition.Attributes != StaticInternal || condition.ImplAttributes != MethodImplAttributes.InternalCall
            || condition.RelativeVirtualAddress != 0 || !HasSignature(reader, condition, 0, "System.Boolean"))
            return false;
        int first = guard + 2;
        int resume = (int)il[guard + 1].Operand;
        int end = first;
        while (end < il.Count && il[end].Offset < resume)
            end++;
        if (end < first + 2 || end == il.Count || il[end].Offset != resume
            || il[end - 1].OpCode != ILOpCode.Pop || il[end - 2].OpCode != ILOpCode.Call
            || !TryMethodDefinition(reader, il[end - 2], out var trace)
            || trace.GetDeclaringType() != caller.DeclaringClass.Handle || WrapperKind(reader, trace) != kind)
            return false;
        // The guard owns this arm. An independent entry is not an optional trace.
        bool EntersArm(int target) => target > il[guard].Offset && target < resume;
        for (int i = 0; i < il.Count; i++)
        {
            if (i >= guard && i < end)
                continue;
            var instruction = il[i];
            if (instruction.SwitchTargets is { } targets)
            {
                foreach (int target in targets)
                    if (EntersArm(target))
                        return false;
            }
            else if (ILDecoder.IsBranch(instruction.OpCode) && EntersArm((int)instruction.Operand))
                return false;
        }

        // The optional argument evaluation may branch forward or call only
        // the metadata getters used to describe this event. Any work, store,
        // call to another member, or additional transfer rejects the match.
        for (int i = first; i < end - 2; i++)
        {
            var instruction = il[i];
            switch (instruction.OpCode)
            {
                case ILOpCode.Ldarg_0 or ILOpCode.Ldarg_1 or ILOpCode.Ldarg_2
                    or ILOpCode.Ldloc_2 or ILOpCode.Ldloc_3 or ILOpCode.Ldloc_s
                    or ILOpCode.Ldnull or ILOpCode.Ldc_i4 or ILOpCode.Ldc_i4_0:
                    continue;
                case ILOpCode.Br_s or ILOpCode.Bne_un_s or ILOpCode.Brtrue_s or ILOpCode.Brfalse_s:
                    if (instruction.Operand > instruction.Offset && instruction.Operand <= il[end - 2].Offset)
                        continue;
                    break;
                case ILOpCode.Call or ILOpCode.Callvirt:
                    if (TryMethodDefinition(reader, instruction, out var argument) && IsTraceArgument(reader, argument))
                        continue;
                    break;
            }
            return false;
        }
        return true;
    }

    private static bool IsContextBody(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || method.Module != owner.Module
            || owner.FullName != Context || owner.Handle.IsNil || owner.IsValueType || owner.IsInterface
            || method.IsSynthetic || method.Handle.IsNil || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var type = reader.GetTypeDefinition(owner.Handle);
        var d = reader.GetMethodDefinition(method.Handle);
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && RawSignatureProvider.TypeDefinitionName(reader, owner.Handle) == Context
            && d.GetDeclaringType() == owner.Handle && d.ImplAttributes == MethodImplAttributes.IL;
    }

    private static Kind WrapperKind(MetadataReader r, MethodDefinition d)
    {
        if (d.Attributes != StaticInternal || d.ImplAttributes != MethodImplAttributes.IL || d.RelativeVirtualAddress == 0)
            return Kind.None;
        const string b = "System.Boolean", s = "System.String";
        return r.GetString(d.Name) switch
        {
            "TraceResolvingHandlerInvoked" when HasSignature(r, d, 0, b, s, s, s, s, s) => Kind.Resolving,
            "TraceAssemblyResolveHandlerInvoked" when HasSignature(r, d, 0, b, s, s, s, s) => Kind.AssemblyResolve,
            "TraceSatelliteSubdirectoryPathProbed" when HasSignature(r, d, 0, b, s, "System.Int32") => Kind.Satellite,
            "TraceAssemblyLoadFromResolveHandlerInvoked" when HasSignature(r, d, 0, b, s, b, s, s) => Kind.LoadFrom,
            _ => Kind.None,
        };
    }

    private static bool IsTraceArgument(MetadataReader r, MethodDefinition d)
    {
        string owner = RawSignatureProvider.TypeDefinitionName(r, d.GetDeclaringType());
        const string asm = "System.Reflection.Assembly", s = "System.String", b = "System.Boolean";
        return (owner, r.GetString(d.Name)) switch
        {
            ("System.Reflection.AssemblyName", "get_FullName") or ("System.Reflection.MemberInfo", "get_Name")
                or ("System.Object", "ToString") or (Context, "get_Name")
                or (asm, "get_FullName") or (asm, "get_Location") => HasSignature(r, d, 0x20, s),
            ("System.Delegate", "get_Method") => HasSignature(r, d, 0x20, "System.Reflection.MethodInfo"),
            (Context, "get_Default") => HasSignature(r, d, 0, Context),
            (asm, "op_Inequality") => HasSignature(r, d, 0, b, asm, asm),
            (asm, "get_IsDynamic") => HasSignature(r, d, 0x20, b),
            _ => false,
        };
    }

    private static bool HasSignature(MetadataReader r, MethodDefinition d, byte header, string result,
        params string[] parameters)
    {
        var sig = r.GetBlobReader(d.Signature);
        if (d.GetGenericParameters().Count != 0 || sig.RemainingBytes < parameters.Length + 3
            || sig.ReadByte() != header || sig.ReadByte() != parameters.Length || !ReadType(r, ref sig, result))
            return false;
        foreach (string parameter in parameters)
            if (!ReadType(r, ref sig, parameter))
                return false;
        return sig.RemainingBytes == 0;

        static bool ReadType(MetadataReader reader, ref BlobReader signature, string expected)
        {
            if (signature.RemainingBytes == 0)
                return false;
            var code = signature.ReadByte();
            switch (expected)
            {
                case "System.Boolean": return code == (byte)SignatureTypeCode.Boolean;
                case "System.String": return code == (byte)SignatureTypeCode.String;
                case "System.Int32": return code == (byte)SignatureTypeCode.Int32;
            }
            if (code != (byte)SignatureTypeKind.Class || !signature.TryReadCompressedInteger(out int handle)
                || (handle & 3) != 0 || (handle >> 2) == 0 || (handle >> 2) > reader.TypeDefinitions.Count)
                return false;
            var type = SRME.TypeDefinitionHandle(handle >> 2);
            return RawSignatureProvider.TypeDefinitionName(reader, type) == expected;
        }
    }

    private static bool TryMethodDefinition(MetadataReader r, Instruction instruction, out MethodDefinition method)
    {
        int token = instruction.Token, row = token & 0x00ff_ffff;
        if ((uint)token >> 24 != 0x06 || row == 0 || row > r.MethodDefinitions.Count)
        {
            method = default;
            return false;
        }
        method = r.GetMethodDefinition(SRME.MethodDefinitionHandle(row));
        return true;
    }
}
