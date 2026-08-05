using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

/// <summary>Recovers the <c>ExceptionResource</c> / <c>ExceptionArgument</c> a
/// <c>System.ThrowHelper</c> sink raises, so the trap it lowers to can carry real .NET's
/// message. The resource member's NAME is the SR key; the argument member's name is the
/// paramName <c>ArgumentException.Message</c> appends.
///
/// <para>Half of the family states both at the CALL site
/// (<c>ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.X)</c>),
/// the other half bakes the resource into its own body
/// (<c>ThrowArgumentOutOfRange_IndexMustBeLessException()</c>). This reads the body either
/// way and reports, per position, whether the value is a constant there or comes from the
/// sink's own argument N — which the call site then supplies.</para>
///
/// <para><b>Metadata only.</b> No <see cref="ClassInfo"/>, no signature decode: the bodies
/// are read straight off the <see cref="Module"/>'s reader. A model read here would decode
/// members of a type reachability CUT, which <c>DN2CPP_STRICT_COMPLETION</c> forbids. For the
/// same reason the callee's positions are identified by PARAMETER NAME (<c>resource</c> /
/// <c>argument</c>) rather than by type — the two Get* helpers order them differently, so
/// position alone is not enough. Every failure path answers "unknown" and the caller keeps
/// the bare trap.</para>
///
/// <para><c>System.ThrowHelper</c> is a per-assembly polyfill, as are its two enums and its
/// SR, so every lookup is scoped to the CALLER's module — where an <c>internal</c> type
/// reference resolves.</para></summary>
internal static class ThrowHelperResources
{
    /// <summary>Where one of the two values comes from: a constant in the sink's body, or
    /// the sink's own parameter <see cref="ArgIndex"/>.</summary>
    internal readonly record struct Source(bool IsConst, int Value, int ArgIndex);

    /// <summary>The resource and argument sources of the sink <paramref name="name"/> with
    /// <paramref name="argCount"/> parameters, or (null, null) when the body is not one of
    /// the shapes above.</summary>
    internal static (Source? Res, Source? Arg) Sources(Module m, string name, int argCount)
    {
        var byName = m.ThrowHelperSinks ??= new Dictionary<(string, int), (Source?, Source?)>();
        if (byName.TryGetValue((name, argCount), out var cached))
            return cached;
        var result = Decode(m, name, argCount);
        byName[(name, argCount)] = result;
        return result;
    }

    /// <summary>The SR key an <c>ExceptionResource</c> value names, or null.</summary>
    internal static string? ResourceKey(Module m, int value) =>
        EnumMap(m, "ExceptionResource").TryGetValue(value, out var n) ? n : null;

    /// <summary>The parameter name an <c>ExceptionArgument</c> value names, or null.</summary>
    internal static string? ArgumentName(Module m, int value) =>
        EnumMap(m, "ExceptionArgument").TryGetValue(value, out var n) ? n : null;

    private static (Source?, Source?) Decode(Module m, string name, int argCount)
    {
        if (FindThrowHelper(m) is not { } thh)
            return (null, null);
        var reader = m.Reader;
        var td = reader.GetTypeDefinition(thh);
        foreach (var mh in td.GetMethods())
        {
            var md = reader.GetMethodDefinition(mh);
            if (reader.GetString(md.Name) != name || CountParams(reader, md) != argCount
                || md.RelativeVirtualAddress == 0)
                continue;
            return Walk(m, thh, md);
        }
        return (null, null);
    }

    /// <summary>Abstract-interprets the sink's body over the only shape these have —
    /// a run of <c>ldarg</c>/<c>ldc.i4</c> pushes feeding one <c>call</c>. Anything else
    /// (a branch, a field read, a string) ends the walk with what it has, which for an
    /// unmodeled sink is nothing.</summary>
    private static (Source?, Source?) Walk(Module m, TypeDefinitionHandle thh, MethodDefinition md)
    {
        var reader = m.Reader;
        List<Instruction> insns;
        try
        {
            insns = ILDecoder.Decode(
                m.PE.GetMethodBody(md.RelativeVirtualAddress).GetILBytes()!.ToImmutableArrayCompat());
        }
        catch (System.Exception)
        {
            return (null, null);
        }
        var pushes = new List<Source>();
        foreach (var insn in insns)
        {
            switch (insn.OpCode)
            {
                case ILOpCode.Ldarg_0: case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2: case ILOpCode.Ldarg_3:
                    pushes.Add(new Source(false, 0, (int)insn.OpCode - (int)ILOpCode.Ldarg_0));
                    continue;
                case ILOpCode.Ldarg_s: case ILOpCode.Ldarg:
                    pushes.Add(new Source(false, 0, (int)insn.Operand));
                    continue;
                case ILOpCode.Ldc_i4_m1: case ILOpCode.Ldc_i4_0: case ILOpCode.Ldc_i4_1:
                case ILOpCode.Ldc_i4_2: case ILOpCode.Ldc_i4_3: case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5: case ILOpCode.Ldc_i4_6: case ILOpCode.Ldc_i4_7:
                case ILOpCode.Ldc_i4_8:
                    pushes.Add(new Source(true, (int)insn.OpCode - (int)ILOpCode.Ldc_i4_0, 0));
                    continue;
                case ILOpCode.Ldc_i4_s: case ILOpCode.Ldc_i4:
                    pushes.Add(new Source(true, (int)insn.Operand, 0));
                    continue;
                case ILOpCode.Nop:
                    continue;
                case ILOpCode.Call:
                    return Match(m, thh, insn.Token, pushes);
                default:
                    return (null, null);
            }
        }
        return (null, null);
    }

    /// <summary>Aligns the pushes with the callee's parameters and reads off the two named
    /// positions. A callee outside this ThrowHelper (a real exception constructor) is not
    /// modeled — its message is built from arguments this lowering does not carry.</summary>
    private static (Source?, Source?) Match(Module m, TypeDefinitionHandle thh, int token,
        List<Source> pushes)
    {
        var reader = m.Reader;
        var h = SRME.EntityHandle(token);
        if (h.Kind != HandleKind.MethodDefinition)
            return (null, null);
        var callee = reader.GetMethodDefinition((MethodDefinitionHandle)h);
        if (callee.GetDeclaringType() != thh)
            return (null, null);
        var names = new List<string>();
        foreach (var ph in callee.GetParameters())
        {
            var p = reader.GetParameter(ph);
            if (p.SequenceNumber > 0)
                names.Add(reader.GetString(p.Name));
        }
        if (names.Count == 0 || pushes.Count < names.Count)
            return (null, null);
        int first = pushes.Count - names.Count;
        Source? res = null, arg = null;
        for (int i = 0; i < names.Count; i++)
        {
            if (names[i] == "resource")
                res = pushes[first + i];
            else if (names[i] is "argument" or "paramName")
                arg = pushes[first + i];
        }
        return (res, arg);
    }

    private static int CountParams(MetadataReader reader, MethodDefinition md)
    {
        int n = 0;
        foreach (var ph in md.GetParameters())
            if (reader.GetParameter(ph).SequenceNumber > 0)
                n++;
        return n;
    }

    private static TypeDefinitionHandle? FindThrowHelper(Module m)
    {
        if (m.ThrowHelperType is { } cached)
            return cached.IsNil ? null : cached;
        var reader = m.Reader;
        var found = default(TypeDefinitionHandle);
        foreach (var th in reader.TypeDefinitions)
        {
            var td = reader.GetTypeDefinition(th);
            if (reader.GetString(td.Name) == "ThrowHelper" && reader.GetString(td.Namespace) == "System")
            {
                found = th;
                break;
            }
        }
        m.ThrowHelperType = found;
        return found.IsNil ? null : found;
    }

    /// <summary>value -> member name of a <c>System.&lt;name&gt;</c> enum in
    /// <paramref name="m"/>, empty when the type is absent.</summary>
    private static Dictionary<int, string> EnumMap(Module m, string name)
    {
        var byType = m.ExceptionEnums ??= new Dictionary<string, Dictionary<int, string>>();
        if (byType.TryGetValue(name, out var cached))
            return cached;
        var map = new Dictionary<int, string>();
        var reader = m.Reader;
        foreach (var th in reader.TypeDefinitions)
        {
            var td = reader.GetTypeDefinition(th);
            if (reader.GetString(td.Name) != name || reader.GetString(td.Namespace) != "System")
                continue;
            foreach (var fh in td.GetFields())
            {
                var fd = reader.GetFieldDefinition(fh);
                if ((fd.Attributes & FieldAttributes.Literal) == 0)
                    continue;
                var ch = fd.GetDefaultValue();
                if (ch.IsNil)
                    continue;
                var c = reader.GetConstant(ch);
                var blob = reader.GetBlobReader(c.Value);
                int? v = c.TypeCode switch
                {
                    ConstantTypeCode.SByte => blob.ReadSByte(),
                    ConstantTypeCode.Byte => blob.ReadByte(),
                    ConstantTypeCode.Int16 => blob.ReadInt16(),
                    ConstantTypeCode.UInt16 => blob.ReadUInt16(),
                    ConstantTypeCode.Int32 => blob.ReadInt32(),
                    _ => null,
                };
                if (v is { } value)
                    map[value] = reader.GetString(fd.Name);
            }
            break;
        }
        byType[name] = map;
        return map;
    }
}
