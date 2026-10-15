using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private sealed partial class TypeMetadataEmitter
    {
        private void NoteGenericDefinitionMemberTypes()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var pending = _constraintDefinitions.Where(p => p.Key.StartsWith("generic:", StringComparison.Ordinal)
                    && !seen.Contains(p.Key)).ToList();
                if (pending.Count == 0)
                    break;
                foreach (var pair in pending)
                {
                    seen.Add(pair.Key);
                    var owner = pair.Value;
                    var reader = owner.Module.Reader;
                    var definition = reader.GetTypeDefinition(owner.Handle);
                    var provider = new ConstraintProvider(_c);
                    foreach (var handle in definition.GetMethods())
                    {
                        var method = reader.GetMethodDefinition(handle);
                        var signature = method.DecodeSignature(provider, null);
                        NoteConstraintSignature(signature.ReturnType);
                        foreach (var parameter in signature.ParameterTypes)
                            NoteConstraintSignature(parameter);
                        foreach (var parameter in method.GetGenericParameters())
                            foreach (var constraint in ConstraintTypes(owner.Module, parameter))
                                NoteConstraintSignature(constraint);
                    }
                    foreach (var handle in definition.GetFields())
                        NoteConstraintSignature(reader.GetFieldDefinition(handle).DecodeSignature(provider, null));
                    foreach (var handle in definition.GetEvents())
                        NoteConstraintSignature(provider.Decode(owner.Module, reader.GetEventDefinition(handle).Type));
                }
            }
        }

        private string SymbolicDisplay(ConstraintSignature signature, string[] types, string[] methods, bool methodParameter = false)
        {
            if (signature.Kind == 5 || signature.Kind == 9)
                return (signature.Kind == 5 ? types : methods)[signature.Value];
            if (signature.Kind == 4)
                return signature.Name + "[" + string.Join(",", signature.Children.Select(c => SymbolicDisplay(c, types, methods))) + "]";
            if (signature.Children.Length == 1)
                return SymbolicDisplay(signature.Children[0], types, methods) + (signature.Kind switch {
                    1 => "*", 2 => methodParameter ? " ByRef" : "&", 6 => "[]",
                    7 => signature.Value == 1 ? "[*]" : "[" + new string(',', signature.Value - 1) + "]", _ => "",
                });
            return signature.Primitive is { } primitive ? _e.ReflectionSignatureType(primitive) : signature.Name ?? "<unavailable>";
        }

        private TypeMetadata GenericDefinitionMembers(TypeMetadata data)
        {
            if (!_constraintDefinitions.TryGetValue("generic:" + data.Name, out var owner))
                return data;
            var reader = owner.Module.Reader;
            var definition = reader.GetTypeDefinition(owner.Handle);
            string stem = CppNaming.GenericDefinitionStem(data.Name);
            string declaring = "&gendef_" + stem;
            string[] typeNames = definition.GetGenericParameters().Select(p => reader.GetString(reader.GetGenericParameter(p).Name)).ToArray();
            bool available = !_e._strippedGenericMetadata.Contains(data.Name);
            if (available)
                data.Flags = "(" + data.Flags + " | DN2CPP_TF_OBJECT_MEMBER_ROWS)";
            bool attributes = available && _c.IsUserModule(owner.Module);
            bool previousNative = _e._metadataNativeRows;
            _e._metadataNativeRows = data.Native;
            var methods = new List<MetadataRow>();
            var constructors = new List<MetadataRow>();
            var addresses = new Dictionary<MethodDefinitionHandle, string>();
            var provider = new ConstraintProvider(_c);
            foreach (var handle in definition.GetMethods())
            {
                var method = reader.GetMethodDefinition(handle);
                string name = reader.GetString(method.Name);
                bool constructor = name == ".ctor";
                if (name == ".cctor" || !available && !constructor)
                    continue;
                var signature = method.DecodeSignature(provider, null);
                string[] methodNames = method.GetGenericParameters().Select(p => reader.GetString(reader.GetGenericParameter(p).Name)).ToArray();
                string symbol = "definition_" + stem + "_" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle);
                var methodAttributes = attributes ? _e.BuildAttrTable(_sb, symbol, owner.Module, method.GetCustomAttributes()) : (Expr: "nullptr", Count: 0);
                var parameters = method.GetParameters().Where(p => reader.GetParameter(p).SequenceNumber > 0)
                    .ToDictionary(p => reader.GetParameter(p).SequenceNumber - 1);
                var parameterRows = new List<MetadataRow>();
                for (int i = 0; i < signature.ParameterTypes.Length; i++)
                {
                    bool hasParameter = parameters.TryGetValue(i, out var parameterHandle);
                    var parameter = hasParameter ? reader.GetParameter(parameterHandle) : default;
                    string? parameterName = !hasParameter || parameter.Name.IsNil ? null : reader.GetString(parameter.Name);
                    var parameterAttributes = attributes && hasParameter ? _e.BuildAttrTable(_sb, symbol + "_param_" + i,
                        owner.Module, parameter.GetCustomAttributes()) : (Expr: "nullptr", Count: 0);
                    string defaultValue = hasParameter ? RenderParameterDefault(owner.Module, parameterHandle,
                        signature.ParameterTypes[i].Primitive ?? TypeDesc.MakePrimitive(PrimitiveTypeCode.Object), symbol + "_default_" + i) : "nullptr";
                    parameterRows.Add(new MetadataRow(new[] {
                        MetadataValue.Ref("&dn2cpp_object_type"), MetadataValue.Text(parameterName), MetadataValue.Ref(parameterAttributes.Expr), MetadataValue.Signed(parameterAttributes.Count),
                        MetadataValue.Signed(hasParameter ? (int)parameter.Attributes : 0), MetadataValue.Ref("nullptr"), MetadataValue.Signed(0), MetadataValue.Ref("nullptr"), MetadataValue.Signed(0),
                        MetadataValue.Signed(0), MetadataValue.Display(SymbolicDisplay(signature.ParameterTypes[i], typeNames, methodNames) + (parameterName is null ? "" : " " + parameterName)),
                        MetadataValue.Display(null), MetadataValue.Text("symbolic"), MetadataValue.Signed(0), MetadataValue.Ref("nullptr"), MetadataValue.Ref(defaultValue),
                        MetadataValue.Ref("nullptr"), MetadataValue.Ref("nullptr"),
                    }));
                }
                string parameterTable = parameterRows.Count == 0 ? "nullptr" : symbol + "_params";
                if (parameterRows.Count > 0)
                    _e.EmitMetadataTable(_sb, "Dn2CppParamInfo", parameterTable, parameterRows);
                string returnType = RenderConstraintSignature(signature.ReturnType);
                string parameterTypes = RenderConstraintVector(signature.ParameterTypes.ToArray());
                _sb.AppendLine($"static const Dn2CppMethodDefinitionSignature {symbol}_signature = {{ {returnType}, {parameterTypes}, {signature.ParameterTypes.Length} }};");
                string returnDisplay = SymbolicDisplay(signature.ReturnType, typeNames, methodNames);
                string display = (constructor ? "Void" : returnDisplay) + " " + name
                    + (methodNames.Length == 0 ? "" : "[" + string.Join(",", methodNames) + "]")
                    + "(" + string.Join(", ", signature.ParameterTypes.Select(p => SymbolicDisplay(p, typeNames, methodNames, true))) + ")";
                var rows = constructor ? constructors : methods;
                if (!constructor)
                    addresses[handle] = rows.Count.ToString();
                int attrs = MetadataMemberAttrs((int)method.Attributes) | (methodNames.Length == 0 ? 0 : 0x40)
                    | ((method.Attributes & System.Reflection.MethodAttributes.SpecialName) != 0 ? 0x20 : 0);
                rows.Add(new MetadataRow(new[] {
                    MetadataValue.Text(name), MetadataValue.Ref(declaring), MetadataValue.Ref("&dn2cpp_object_type"), MetadataValue.Ref(parameterTable),
                    MetadataValue.Signed(signature.ParameterTypes.Length), MetadataValue.ExplicitSigned(attrs), MetadataValue.Signed(-1), MetadataValue.Ref("nullptr"), MetadataValue.Ref("nullptr"),
                    MetadataValue.Ref(methodAttributes.Expr), MetadataValue.Signed(methodAttributes.Count), MetadataValue.Text(null), MetadataValue.Signed((int)method.Attributes), MetadataValue.Signed((int)method.ImplAttributes),
                    MetadataValue.Signed(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle)), MetadataValue.Signed(methodNames.Length), MetadataValue.Ref("nullptr"), MetadataValue.Ref("nullptr"), MetadataValue.Signed(0),
                    MetadataValue.Signed(0), MetadataValue.Ref("nullptr"), MetadataValue.Signed(0), MetadataValue.Signed(0), MetadataValue.Display(display), MetadataValue.Display(display),
                    MetadataValue.Display(returnDisplay), MetadataValue.Display(returnDisplay), MetadataValue.Text("symbolic"), MetadataValue.Signed(0), MetadataValue.Signed(0), MetadataValue.Ref("nullptr"),
                    MetadataValue.Ref(RenderFormalParameters(owner.Module, method.GetGenericParameters())), MetadataValue.Ref("nullptr"), MetadataValue.Ref("nullptr"), MetadataValue.Ref("nullptr"), MetadataValue.Ref("&" + symbol + "_signature"),
                }));
            }
            if (methods.Count > 0)
                _e.EmitMetadataTable(_sb, "Dn2CppMethodInfo", data.Methods = "definition_" + stem + "_methods", methods);
            if (constructors.Count > 0)
                _e.EmitMetadataTable(_sb, "Dn2CppMethodInfo", data.Ctors = "definition_" + stem + "_ctors", constructors);
            foreach (var handle in addresses.Keys.ToList())
                addresses[handle] = _e.MetadataRowAddress(data.Methods, int.Parse(addresses[handle]));
            data.MethodCount = methods.Count;
            data.CtorCount = constructors.Count;
            var fields = new List<MetadataRow>();
            if (available)
                foreach (var handle in definition.GetFields())
                {
                    var field = reader.GetFieldDefinition(handle);
                    string name = reader.GetString(field.Name);
                    var fieldAttributes = attributes ? _e.BuildAttrTable(_sb, "definition_" + stem + "_field_" + fields.Count,
                        owner.Module, field.GetCustomAttributes()) : (Expr: "nullptr", Count: 0);
                    var signature = field.DecodeSignature(provider, null);
                    string type = signature.Primitive is { } leaf ? _e.FieldTypeInfoExpr(leaf, _emittedEnums) : "&dn2cpp_object_type";
                    var literal = (field.Attributes & System.Reflection.FieldAttributes.Literal) != 0
                        ? RenderLiteral(owner.Module, signature.Primitive ?? TypeDesc.MakePrimitive(PrimitiveTypeCode.Object),
                            "definition_" + stem + "_constant_" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle), handle, type)
                        : (Get: "nullptr", Bits: 0L);
                    fields.Add(new MetadataRow(new[] {
                        MetadataValue.Text(name), MetadataValue.Ref(declaring), MetadataValue.Ref(type), MetadataValue.ExplicitSigned(MetadataFieldAttrs((int)field.Attributes)),
                        MetadataValue.Ref(literal.Get), MetadataValue.Ref("nullptr"), MetadataValue.Ref(fieldAttributes.Expr), MetadataValue.Signed(fieldAttributes.Count), MetadataValue.Signed((int)field.Attributes),
                        MetadataValue.Signed(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle)), MetadataValue.Signed(literal.Bits), MetadataValue.Display(SymbolicDisplay(signature, typeNames, Array.Empty<string>()) + " " + name),
                        MetadataValue.Ref("nullptr"), MetadataValue.Ref(RenderConstraintSignature(signature)),
                    }));
                }
            if (fields.Count > 0)
                _e.EmitMetadataTable(_sb, "Dn2CppFieldInfo", data.Fields = "definition_" + stem + "_fields", fields);
            data.FieldCount = fields.Count;
            var properties = new List<MetadataRow>();
            if (available)
                foreach (var handle in definition.GetProperties())
                {
                    var property = reader.GetPropertyDefinition(handle);
                    var accessors = property.GetAccessors();
                    string getter = addresses.TryGetValue(accessors.Getter, out var g) ? g : "nullptr";
                    string setter = addresses.TryGetValue(accessors.Setter, out var s) ? s : "nullptr";
                    if (getter == "nullptr" && setter == "nullptr")
                        continue;
                    var accessor = reader.GetMethodDefinition(!accessors.Getter.IsNil ? accessors.Getter : accessors.Setter);
                    int attrs = MetadataMemberAttrs((int)accessor.Attributes);
                    if (!accessors.Setter.IsNil && (reader.GetMethodDefinition(accessors.Setter).Attributes & System.Reflection.MethodAttributes.MemberAccessMask) == System.Reflection.MethodAttributes.Public)
                        attrs = (attrs & ~4) | 2;
                    string name = reader.GetString(property.Name);
                    var propertyAttributes = attributes ? _e.BuildAttrTable(_sb, "definition_" + stem + "_property_" + properties.Count,
                        owner.Module, property.GetCustomAttributes()) : (Expr: "nullptr", Count: 0);
                    var signature = property.DecodeSignature(provider, null);
                    properties.Add(new MetadataRow(new[] {
                        MetadataValue.Text(name), MetadataValue.Ref(declaring), MetadataValue.Ref("&dn2cpp_object_type"), MetadataValue.Ref(getter), MetadataValue.Ref(setter),
                        MetadataValue.ExplicitSigned(attrs), MetadataValue.Ref(propertyAttributes.Expr), MetadataValue.Signed(propertyAttributes.Count), MetadataValue.Signed(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle)),
                        MetadataValue.Display(SymbolicDisplay(signature.ReturnType, typeNames, Array.Empty<string>()) + " " + name),
                    }));
                }
            if (properties.Count > 0)
                _e.EmitMetadataTable(_sb, "Dn2CppPropInfo", data.Props = "definition_" + stem + "_properties", properties);
            data.PropCount = properties.Count;
            if (available)
                GenericDefinitionEvents(data, owner.Module, definition, stem, declaring, typeNames, addresses, attributes);
            data.IlAttrs = (uint)definition.Attributes;
            data.MetadataToken = System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(owner.Handle);
            data.AssemblyName = owner.Module.AssemblyName;
            data.DefaultMemberName = DefaultMemberName(owner.Module, owner.Handle);
            if (attributes)
                (data.CustomAttrs, data.CustomAttrCount) = _e.BuildAttrTable(_sb, "definition_" + stem + "_type", owner.Module, definition.GetCustomAttributes());
            _e._metadataNativeRows = previousNative;
            return data;
        }
    }
}
