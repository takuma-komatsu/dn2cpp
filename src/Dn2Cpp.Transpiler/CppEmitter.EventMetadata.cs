using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private sealed partial class TypeMetadataEmitter
    {
        private MetadataRow EventRow(Module module, EventDefinitionHandle handle, string declaring,
            string handler, string recipe, string display, string symbol,
            IReadOnlyDictionary<MethodDefinitionHandle, string> methods, bool attributes)
        {
            var reader = module.Reader;
            var definition = reader.GetEventDefinition(handle);
            var accessors = definition.GetAccessors();
            string Address(MethodDefinitionHandle method) => methods.TryGetValue(method, out var address) ? address : "nullptr";
            string adder = Address(accessors.Adder), remover = Address(accessors.Remover), raiser = Address(accessors.Raiser);
            var others = accessors.Others.Where(methods.ContainsKey).Select(Address).ToList();
            string otherVector = "nullptr";
            if (others.Count > 0)
            {
                otherVector = symbol + "_others";
                _sb.AppendLine($"static const Dn2CppMetadataHandle<Dn2CppMethodInfo> {otherVector}[] = {{ "
                    + string.Join(", ", others.Select(address => $"Dn2CppMetadataHandle<Dn2CppMethodInfo>::from_raw({address})")) + " };");
            }
            var all = new List<MethodDefinitionHandle>();
            if (!accessors.Adder.IsNil) all.Add(accessors.Adder);
            if (!accessors.Remover.IsNil) all.Add(accessors.Remover);
            if (!accessors.Raiser.IsNil) all.Add(accessors.Raiser);
            all.AddRange(accessors.Others);
            int flags = 0;
            bool publicAccessor = false, allPrivate = all.Count > 0;
            foreach (var method in all)
            {
                var attrs = reader.GetMethodDefinition(method).Attributes;
                if ((attrs & System.Reflection.MethodAttributes.Static) != 0) flags |= 1;
                publicAccessor |= (attrs & System.Reflection.MethodAttributes.MemberAccessMask) == System.Reflection.MethodAttributes.Public;
                allPrivate &= (attrs & System.Reflection.MethodAttributes.MemberAccessMask) == System.Reflection.MethodAttributes.Private;
            }
            flags |= publicAccessor ? 2 : allPrivate ? 4 : 0;
            var custom = attributes ? _e.BuildAttrTable(_sb, symbol, module, definition.GetCustomAttributes()) : (Expr: "nullptr", Count: 0);
            return new MetadataRow(new[] {
                MetadataValue.Text(reader.GetString(definition.Name)), MetadataValue.Ref(declaring), MetadataValue.Ref(handler),
                MetadataValue.Ref(adder), MetadataValue.Ref(remover), MetadataValue.Ref(raiser), MetadataValue.Ref(otherVector), MetadataValue.Signed(others.Count),
                MetadataValue.Signed(flags), MetadataValue.Signed((int)definition.Attributes), MetadataValue.Ref(custom.Expr), MetadataValue.Signed(custom.Count),
                MetadataValue.Signed(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle)), MetadataValue.Display(display), MetadataValue.Ref(recipe),
            });
        }

        private (string Expr, int Count)? BuildEventTable(ClassInfo cls, List<MethodInfo> methods)
        {
            var addresses = new Dictionary<MethodDefinitionHandle, string>();
            foreach (var method in methods)
                if (_memberAddr.TryGetValue(method, out var address))
                    addresses.TryAdd(method.Handle, address);
            var rows = new List<MetadataRow>();
            var reader = cls.Module.Reader;
            foreach (var handle in reader.GetTypeDefinition(cls.Handle).GetEvents())
            {
                var definition = reader.GetEventDefinition(handle);
                var handler = new ConstraintProvider(_c).Decode(cls.Module, definition.Type);
                string name = reader.GetString(definition.Name);
                string[] typeNames = cls.Context.TypeArgs.Select(type => _e.ReflectionSignatureType(type)).ToArray();
                rows.Add(EventRow(cls.Module, handle, _e.TypeInfoRef(cls, "event row's declaring type"),
                    "nullptr", RenderConstraintSignature(handler), SymbolicDisplay(handler, typeNames, Array.Empty<string>()) + " " + name,
                    cls.CppName + "_event_" + rows.Count, addresses, _c.IsUserModule(cls.Module)));
            }
            if (rows.Count == 0)
                return null;
            string table = "eventtab_" + cls.CppName;
            _e.EmitMetadataTable(_sb, "Dn2CppEventInfo", table, rows);
            return (table, rows.Count);
        }

        private void GenericDefinitionEvents(TypeMetadata data, Module module, TypeDefinition definition,
            string stem, string declaring, string[] typeNames,
            IReadOnlyDictionary<MethodDefinitionHandle, string> methods, bool attributes)
        {
            var rows = new List<MetadataRow>();
            var provider = new ConstraintProvider(_c);
            foreach (var handle in definition.GetEvents())
            {
                var item = module.Reader.GetEventDefinition(handle);
                var handler = provider.Decode(module, item.Type);
                string name = module.Reader.GetString(item.Name);
                rows.Add(EventRow(module, handle, declaring, "&dn2cpp_object_type", RenderConstraintSignature(handler),
                    SymbolicDisplay(handler, typeNames, Array.Empty<string>()) + " " + name,
                    "definition_" + stem + "_event_" + rows.Count, methods, attributes));
            }
            if (rows.Count > 0)
                _e.EmitMetadataTable(_sb, "Dn2CppEventInfo", data.Events = "definition_" + stem + "_events", rows);
            data.EventCount = rows.Count;
        }
    }
}
