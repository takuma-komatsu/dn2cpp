using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private sealed class TypeMetadata
    {
        internal string Name = "", Base = "nullptr", Vtable = "nullptr", Interfaces = "nullptr";
        internal string ToStringFn = "nullptr", HashFn = "nullptr", EqualsFn = "nullptr";
        internal string GenericDef = "nullptr", GenericArgs = "nullptr", EnumUnderlying = "nullptr", ElementType = "nullptr";
        internal string FinalizeFn = "nullptr", Rgctx = "nullptr", TypeObject = "nullptr", FormatSpec = "nullptr";
        internal string InstanceSize = "0", Flags = "0";
        internal int InterfaceCount, GenericArgCount, ArrayRank, VarianceMask;
        internal string Fields = "nullptr", Methods = "nullptr", Ctors = "nullptr", Props = "nullptr", CustomAttrs = "nullptr";
        internal int FieldCount, MethodCount, CtorCount, PropCount, CustomAttrCount;
        internal string EnumMembers = "nullptr", NestedTypes = "nullptr";
        internal string? AssemblyName;
        internal int EnumMemberCount, NestedCount, MetadataToken;
        internal uint IlAttrs;
        internal string? DefaultMemberName, EventSourceName, EventSourceGuid;
        internal string? GenericParamNames;
        internal ModeledSize? MarshalSize;
    }

    private void EmitTypeInfo(StringBuilder sb, string symbol, TypeMetadata data)
    {
        MetadataValue[] cold = {
            MetadataValue.Ref(data.Fields), MetadataValue.Signed(data.FieldCount), MetadataValue.Ref(data.Methods), MetadataValue.Signed(data.MethodCount),
            MetadataValue.Ref(data.Ctors), MetadataValue.Signed(data.CtorCount), MetadataValue.Ref(data.Props), MetadataValue.Signed(data.PropCount),
            MetadataValue.Ref(data.CustomAttrs), MetadataValue.Signed(data.CustomAttrCount), MetadataValue.Ref(data.EnumMembers), MetadataValue.Signed(data.EnumMemberCount),
            MetadataValue.Ref(data.NestedTypes), MetadataValue.Signed(data.NestedCount), MetadataValue.Text(data.AssemblyName),
            MetadataValue.Unsigned(data.IlAttrs), MetadataValue.Signed(data.MetadataToken), MetadataValue.Text(data.DefaultMemberName),
            MetadataValue.Signed(data.MarshalSize?.Size64 ?? 0), MetadataValue.Text(data.EventSourceName), MetadataValue.Text(data.EventSourceGuid),
            MetadataValue.Text(data.GenericParamNames),
        };
        bool hasCold = cold.Any(value => value.Pointer is not null || value.StringValue is not null || value.Encoded != 0);
        string coldSymbol = "refl_" + symbol;
        if (hasCold)
        {
            if (data.MarshalSize is { Size32: { } size32 } extent && size32 != extent.Size64)
            {
                sb.AppendLine("#if INTPTR_MAX == INT64_MAX");
                EmitMetadataTable(sb, "Dn2CppTypeReflection", coldSymbol, new[] { new MetadataRow(cold) });
                sb.AppendLine("#else");
                cold[18] = MetadataValue.Signed(size32);
                EmitMetadataTable(sb, "Dn2CppTypeReflection", coldSymbol, new[] { new MetadataRow(cold) });
                sb.AppendLine("#endif");
            }
            else
                EmitMetadataTable(sb, "Dn2CppTypeReflection", coldSymbol, new[] { new MetadataRow(cold) });
        }
        string[] hot = {
            InternMetadataName(data.Name), data.Base, data.Vtable, data.Interfaces, data.ToStringFn, data.HashFn, data.EqualsFn,
            data.GenericDef, data.GenericArgs, data.EnumUnderlying, data.ElementType, data.FinalizeFn, data.Rgctx, data.TypeObject, data.FormatSpec,
            data.InstanceSize, data.InterfaceCount.ToString(), data.Flags, data.GenericArgCount.ToString(), data.ArrayRank.ToString(), data.VarianceMask.ToString(),
            hasCold ? $"Dn2CppMetadataHandle<Dn2CppTypeReflection>::from_static(md_record_{coldSymbol})" : "nullptr",
        };
        sb.AppendLine($"const Dn2CppTypeInfo {symbol} = {{ {string.Join(", ", hot)} }};");
    }
}
