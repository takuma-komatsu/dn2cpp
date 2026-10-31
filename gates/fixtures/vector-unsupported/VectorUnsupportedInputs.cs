using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using CMethod = Mono.Cecil.MethodDefinition;
using CType = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

internal static class VectorUnsupportedInputs
{
    internal static readonly string[] CountModes = { "valid", "methodspec", "methodspec-zero", "instance", "explicit-this", "vararg", "generic", "wrong-result", "return-modopt", "return-modreq", "return-pinned", "extra", "trailing", "owner-trailing", "owner-modopt", "owner-modreq", "owner-pinned" };
    internal static readonly string[] HelperModes = { "valid", "context-mvar", "foreign-owner", "instance", "explicit-this", "vararg", "wrong-result", "return-modopt", "return-modreq", "return-pinned", "extra", "trailing", "spec-modopt", "spec-modreq", "spec-pinned", "spec-byref", "spec-trailing", "spec-arity", "spec-zero", "owner-spec", "owner-class-spec", "owner-modopt-spec", "owner-modreq-spec" };
    public static void Generate(string mode, string corePath, string output)
    {
        bool helper = mode.StartsWith("helper-", StringComparison.Ordinal);
        string mutation = mode[(mode.IndexOf('-') + 1)..];
        if (!(helper ? HelperModes : CountModes).Contains(mutation)) throw new ArgumentException("Unknown mode " + mode);
        if (File.Exists(output)) throw new InvalidOperationException("Refusing overwrite " + output);
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorUnsupportedInput", new Version(1, 0)), "VectorUnsupportedInput." + mode, ModuleKind.Console);
        var m = app.MainModule;
        var host = new CType("Guard", "Entry", Mono.Cecil.TypeAttributes.Public, m.TypeSystem.Object); m.Types.Add(host);
        var main = new CMethod("Main", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, m.TypeSystem.Void); host.Methods.Add(main); m.EntryPoint = main;
        var element = m.ImportReference(core.MainModule.GetType("System.Decimal"));
        var declaration = helper
            ? core.MainModule.GetType("System.ThrowHelper").Methods.Single(x => x.Name == "ThrowForUnsupportedNumericsVectorBaseType")
            : core.MainModule.GetType("System.Numerics.Vector`1").Methods.Single(x => x.Name == "get_Count");
        var target = m.ImportReference(declaration);
        if (!helper)
        {
            var owner = new GenericInstanceType(m.ImportReference(declaration.DeclaringType)); owner.GenericArguments.Add(element);
            if (mutation == "owner-trailing") owner.GenericArguments.Add(m.TypeSystem.Int32);
            target.DeclaringType = mutation switch {
                "owner-modopt" => new OptionalModifierType(m.TypeSystem.Object, owner),
                "owner-modreq" => new RequiredModifierType(m.TypeSystem.Object, owner),
                "owner-pinned" => new PinnedType(owner), _ => owner };
        }
        else if (mutation == "foreign-owner")
        {
            var foreign = new CType("System", "ThrowHelper", declaration.DeclaringType.Attributes, m.TypeSystem.Object);
            var foreignMethod = new CMethod(declaration.Name, declaration.Attributes, m.TypeSystem.Void)
            { ImplAttributes = declaration.ImplAttributes };
            foreignMethod.GenericParameters.Add(new GenericParameter("T", foreignMethod));
            foreignMethod.Body.GetILProcessor().Emit(OpCodes.Ret);
            foreign.Methods.Add(foreignMethod);
            m.Types.Add(foreign); target.DeclaringType = foreign;
        }
        else if (mutation is "owner-modopt-spec" or "owner-modreq-spec")
        {
            target.DeclaringType = mutation == "owner-modopt-spec"
                ? new OptionalModifierType(m.TypeSystem.Object, target.DeclaringType)
                : new RequiredModifierType(m.TypeSystem.Object, target.DeclaringType);
        }
        else if (mutation is "owner-spec" or "owner-class-spec")
        {
            var owner = new GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(element); target.DeclaringType = owner;
        }
        switch (mutation)
        {
            case "instance": target.HasThis = true; break;
            case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
            case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
            case "generic": target.GenericParameters.Add(new GenericParameter("U", target)); break;
            case "wrong-result": target.ReturnType = m.TypeSystem.Int64; break;
            case "return-modopt": target.ReturnType = new OptionalModifierType(m.TypeSystem.Object, target.ReturnType); break;
            case "return-modreq": target.ReturnType = new RequiredModifierType(m.TypeSystem.Object, target.ReturnType); break;
            case "return-pinned": target.ReturnType = new PinnedType(target.ReturnType); break;
            case "extra": case "trailing": target.Parameters.Add(new ParameterDefinition(m.TypeSystem.Int32)); break;
        }
        MethodReference call = target;
        if (helper)
        {
            var spec = new GenericInstanceMethod(target);
            TypeReference argument = mutation switch {
                "spec-modopt" => new OptionalModifierType(m.TypeSystem.Object, element),
                "spec-modreq" => new RequiredModifierType(m.TypeSystem.Object, element),
                "spec-pinned" => new PinnedType(element),
                "spec-byref" => new ByReferenceType(element), _ => element };
            if (mutation == "context-mvar")
            {
                var caller = new MethodReference("Context", m.TypeSystem.Void, host);
                var parameter = new GenericParameter("U", caller); caller.GenericParameters.Add(parameter); argument = parameter;
            }
            if (mutation != "spec-zero") spec.GenericArguments.Add(argument);
            if (mutation is "spec-arity" or "spec-trailing") spec.GenericArguments.Add(m.TypeSystem.Int32);
            call = spec;
        }
        if (!helper && mutation is "methodspec" or "methodspec-zero")
        {
            var specification = new GenericInstanceMethod(target);
            if (mutation == "methodspec") specification.GenericArguments.Add(element);
            call = specification;
        }
        var il = main.Body.GetILProcessor();
        if (mutation == "context-mvar")
            foreach (string name in new[] { "Decimal", "Int128", "String" })
            { il.Emit(OpCodes.Ldtoken, m.ImportReference(core.MainModule.GetType("System." + name))); il.Emit(OpCodes.Pop); }
        il.Emit(OpCodes.Call, call); if (!helper) il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (mutation is "trailing" or "owner-trailing" or "spec-trailing") Patch(mode, output);
        if (mutation == "owner-class-spec") PatchPlainOwner(output);
        Verify(mode, output);
    }

    private static void PatchPlainOwner(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var pe = new PEReader(new MemoryStream(bytes)); var reader = pe.GetMetadataReader();
        var member = reader.MemberReferences.Select(reader.GetMemberReference).Single(x => reader.StringComparer.Equals(x.Name, "ThrowForUnsupportedNumericsVectorBaseType"));
        var signature = reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature;
        var blob = reader.GetBlobReader(signature);
        if (blob.ReadByte() != 0x15 || blob.ReadByte() != 0x12) throw new InvalidOperationException("Expected padded helper owner");
        blob.ReadCompressedInteger();
        var original = reader.GetBlobBytes(signature);
        var plain = original[1..blob.Offset];
        int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
        if (original.Length >= 0x80 || bytes[offset] != original.Length) throw new InvalidOperationException("Expected short owner blob");
        bytes[offset] = (byte)plain.Length;
        Array.Clear(bytes, offset + 1, original.Length);
        plain.CopyTo(bytes, offset + 1);
        File.WriteAllBytes(path, bytes);
    }

    private static void Patch(string mode, string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var pe = new PEReader(new MemoryStream(bytes)); var reader = pe.GetMetadataReader();
        bool helper = mode.StartsWith("helper-", StringComparison.Ordinal);
        var member = reader.MemberReferences.Select(reader.GetMemberReference).Single(x => reader.StringComparer.Equals(x.Name, helper ? "ThrowForUnsupportedNumericsVectorBaseType" : "get_Count"));
        BlobHandle blob = mode.EndsWith("owner-trailing", StringComparison.Ordinal) ? reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature
            : mode.EndsWith("spec-trailing", StringComparison.Ordinal) ? reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(1)).Signature : member.Signature;
        var raw = reader.GetBlobBytes(blob);
        int countIndex = helper ? 2 : 1;
        if (mode.EndsWith("spec-trailing", StringComparison.Ordinal)) countIndex = 1;
        if (mode.EndsWith("owner-trailing", StringComparison.Ordinal))
        { var cursor = reader.GetBlobReader(blob); cursor.ReadByte(); cursor.ReadByte(); cursor.ReadCompressedInteger(); countIndex = cursor.Offset; }
        if (raw[countIndex] != (mode.EndsWith("owner-trailing", StringComparison.Ordinal) || mode.EndsWith("spec-trailing", StringComparison.Ordinal) ? 2 : 1)) throw new InvalidOperationException("Padding not preserved");
        raw[countIndex]--;
        int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
        int prefix = bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
        raw.CopyTo(bytes, offset + prefix); File.WriteAllBytes(path, bytes);
    }

    public static void Verify(string mode, string path)
    {
        using var pe = new PEReader(File.OpenRead(path)); var reader = pe.GetMetadataReader();
        bool helper = mode.StartsWith("helper-", StringComparison.Ordinal);
        string mutation = mode[(mode.IndexOf('-') + 1)..];
        void Require(bool ok, string detail) { if (!ok) throw new InvalidOperationException("Serialized " + mode + ": " + detail); }
        var memberHandle = reader.MemberReferences.Single(h => reader.StringComparer.Equals(reader.GetMemberReference(h).Name, helper ? "ThrowForUnsupportedNumericsVectorBaseType" : "get_Count"));
        var member = reader.GetMemberReference(memberHandle);
        var raw = reader.GetBlobReader(member.Signature);
        int header = raw.ReadByte();
        int generic = helper || mutation == "generic" ? 0x10 : 0;
        int expectedHeader = generic | (mutation switch { "instance" => 0x20, "explicit-this" => 0x60, "vararg" => 5, _ => 0 });
        Require(header == expectedHeader, "header");
        if ((header & 0x10) != 0) Require(raw.ReadCompressedInteger() == 1, "method arity");
        Require(raw.ReadCompressedInteger() == (mutation == "extra" ? 1 : 0), "parameter count");
        int returnCode = raw.ReadByte();
        int expectedReturn = mutation switch { "wrong-result" => 0x0a, "return-modopt" => 0x20, "return-modreq" => 0x1f, "return-pinned" => 0x45, _ => helper ? 1 : 8 };
        Require(returnCode == expectedReturn, "return shape");
        if (mutation is "return-modopt" or "return-modreq") { Require(raw.ReadCompressedInteger() > 0, "modifier token"); Require(raw.ReadByte() == (helper ? 1 : 8), "modified return"); }
        if (mutation == "return-pinned") Require(raw.ReadByte() == (helper ? 1 : 8), "pinned return");
        if (mutation is "extra" or "trailing") Require(raw.ReadByte() == 8, "extra/trailing Int32");
        Require(raw.RemainingBytes == 0, "method end");
        string ownerRaw = "";
        if (mutation is "owner-class-spec" or "owner-modopt-spec" or "owner-modreq-spec")
        {
            Require(helper && member.Parent.Kind == HandleKind.TypeSpecification, "nongeneric helper TypeSpec owner");
            var signature = reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature;
            ownerRaw = Convert.ToHexString(reader.GetBlobBytes(signature));
            var owner = reader.GetBlobReader(signature);
            if (mutation != "owner-class-spec")
            {
                Require(owner.ReadByte() == (mutation == "owner-modopt-spec" ? 0x20 : 0x1f), "owner exact modifier prefix");
                int modifierToken = owner.ReadCompressedInteger();
                Require((modifierToken & 3) == 1, "owner modifier TypeRef");
                var modifier = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(modifierToken >> 2));
                Require(reader.StringComparer.Equals(modifier.Namespace, "System") && reader.StringComparer.Equals(modifier.Name, "Object"), "owner exact modifier identity");
            }
            Require(owner.ReadByte() == 0x12, "owner plain CLASS not genericinst");
            int ownerToken = owner.ReadCompressedInteger(); Require((ownerToken & 3) == 1, "owner exact TypeRef");
            var type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(ownerToken >> 2));
            Require(reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "ThrowHelper"), "owner exact helper name");
            Require(type.ResolutionScope.Kind == HandleKind.AssemblyReference && reader.StringComparer.Equals(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name, "System.Private.CoreLib"), "owner exact CoreLib source");
            Require(owner.RemainingBytes == 0, "nongeneric helper owner exact end");
        }
        else if (member.Parent.Kind == HandleKind.TypeSpecification)
        {
            var owner = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature);
            ownerRaw = Convert.ToHexString(reader.GetBlobBytes(reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature));
            int first = owner.ReadByte();
            Require(first == (mutation switch { "owner-modopt" => 0x20, "owner-modreq" => 0x1f, "owner-pinned" => 0x45, _ => 0x15 }), "owner prefix");
            if (mutation is "owner-modopt" or "owner-modreq") { Require(owner.ReadCompressedInteger() > 0, "owner modifier"); Require(owner.ReadByte() == 0x15, "modified owner instance"); }
            if (mutation == "owner-pinned") Require(owner.ReadByte() == 0x15, "pinned owner instance");
            Require(owner.ReadByte() == (helper ? 0x12 : 0x11), "owner class/value kind");
            int coded = owner.ReadCompressedInteger(); Require((coded & 3) == 1, "owner typeref");
            var type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(coded >> 2));
            Require(reader.StringComparer.Equals(type.Name, helper ? "ThrowHelper" : "Vector`1"), "owner name");
            Require(type.ResolutionScope.Kind == HandleKind.AssemblyReference && reader.StringComparer.Equals(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name, "System.Private.CoreLib"), "owner CoreLib identity");
            Require(owner.ReadCompressedInteger() == 1, "owner arity"); Require(owner.ReadByte() == 0x11, "owner Decimal value kind");
            var arg = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(owner.ReadCompressedInteger() >> 2)); Require(reader.StringComparer.Equals(arg.Name, "Decimal"), "owner Decimal identity");
            if (mutation == "owner-trailing") Require(owner.ReadByte() == 8, "owner trailing byte");
            Require(owner.RemainingBytes == 0, "owner end");
        }
        else if (mutation == "foreign-owner")
        {
            Require(member.Parent.Kind == HandleKind.TypeDefinition, "foreign helper local owner");
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)member.Parent);
            Require(reader.StringComparer.Equals(type.Name, "ThrowHelper") && reader.StringComparer.Equals(type.Namespace, "System"), "foreign helper exact conflicting name");
            var localMethodHandle = type.GetMethods().Single(h => reader.StringComparer.Equals(reader.GetMethodDefinition(h).Name, "ThrowForUnsupportedNumericsVectorBaseType"));
            var localMethod = reader.GetMethodDefinition(localMethodHandle);
            Require((int)localMethod.Attributes == 0x0093 && (int)localMethod.ImplAttributes == 0x0100
                && localMethod.RelativeVirtualAddress != 0 && Convert.ToHexString(reader.GetBlobBytes(localMethod.Signature)) == "10010001", "foreign helper exact declaration");
            Require(localMethod.GetGenericParameters().Count == 1, "foreign helper method arity");
            var parameter = reader.GetGenericParameter(localMethod.GetGenericParameters().Single());
            Require(parameter.Parent == localMethodHandle && parameter.Index == 0 && (int)parameter.Attributes == 0
                && parameter.GetConstraints().Count == 0, "foreign helper unconstrained method parameter");
            Require(pe.GetMethodBody(localMethod.RelativeVirtualAddress).GetILBytes()!.SequenceEqual(new byte[] { 0x2a }), "foreign helper retained ret body");
            Console.WriteLine($"vector-unsupported-foreign-helper method={MetadataTokens.GetToken(localMethodHandle):X8} attrs=0093 impl=0100 raw=10010001 constraints=0 source=local-TypeDef serialized=true compiler-only=true");
            ownerRaw = "local-TypeDef:" + MetadataTokens.GetToken(member.Parent).ToString("X8");
        }
        else
        {
            Require(helper && member.Parent.Kind == HandleKind.TypeReference, "helper owner kind");
            var type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
            Require(reader.StringComparer.Equals(type.Name, "ThrowHelper") && reader.StringComparer.Equals(type.Namespace, "System"), "helper owner name");
            Require(type.ResolutionScope.Kind == HandleKind.AssemblyReference && reader.StringComparer.Equals(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name, "System.Private.CoreLib"), "helper owner source");
        }
        string specRaw = "";
        if (!helper && mutation is "methodspec" or "methodspec-zero")
        {
            Require(reader.GetTableRowCount(TableIndex.MethodSpec) == 1, "Count original MethodSpec row");
            var specification = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(1));
            Require(specification.Method == memberHandle, "Count original MemberRef MethodSpec target");
            specRaw = Convert.ToHexString(reader.GetBlobBytes(specification.Signature));
            var blob = reader.GetBlobReader(specification.Signature);
            Require(blob.ReadByte() == 0x0a && blob.ReadCompressedInteger() == (mutation == "methodspec" ? 1 : 0), "Count MethodSpec arity");
            if (mutation == "methodspec")
            {
                Require(blob.ReadByte() == 0x11, "Count MethodSpec value argument");
                int token = blob.ReadCompressedInteger(); Require((token & 3) == 1, "Count MethodSpec TypeRef");
                var type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(token >> 2));
                Require(reader.StringComparer.Equals(type.Name, "Decimal"), "Count MethodSpec Decimal argument");
            }
            Require(blob.RemainingBytes == 0, "Count MethodSpec end");
        }
        if (helper)
        {
            Require(reader.GetTableRowCount(TableIndex.MethodSpec) == 1, "one original spec");
            var spec = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(1)); Require(spec.Method == memberHandle, "original MemberRef target");
            specRaw = Convert.ToHexString(reader.GetBlobBytes(spec.Signature));
            var blob = reader.GetBlobReader(spec.Signature); Require(blob.ReadByte() == 0x0a, "spec header");
            int count = blob.ReadCompressedInteger(); Require(count == (mutation == "spec-arity" ? 2 : mutation == "spec-zero" ? 0 : 1), "spec arity");
            if (mutation == "context-mvar")
            {
                Require(blob.ReadByte() == 0x1e && blob.ReadCompressedInteger() == 0, "original scoped method variable");
                foreach (string name in new[] { "Decimal", "Int128", "String" })
                    Require(reader.TypeReferences.Any(h => reader.StringComparer.Equals(reader.GetTypeReference(h).Name, name)), "context witness " + name);
            }
            else if (count > 0)
            {
                int prefix = blob.ReadByte(); Require(prefix == (mutation switch { "spec-modopt" => 0x20, "spec-modreq" => 0x1f, "spec-pinned" => 0x45, "spec-byref" => 0x10, _ => 0x11 }), "spec argument prefix");
                if (mutation is "spec-modopt" or "spec-modreq") { Require(blob.ReadCompressedInteger() > 0, "spec modifier"); Require(blob.ReadByte() == 0x11, "modified value type"); }
                if (mutation is "spec-pinned" or "spec-byref") Require(blob.ReadByte() == 0x11, "qualified value type");
                int token = blob.ReadCompressedInteger(); Require((token & 3) == 1, "spec type reference");
                var type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(token >> 2)); Require(reader.StringComparer.Equals(type.Name, "Decimal"), "spec Decimal");
                if (mutation is "spec-arity" or "spec-trailing") Require(blob.ReadByte() == 8, "second/trailing spec type");
            }
            Require(blob.RemainingBytes == 0, "spec end");
        }
        Console.WriteLine($"vector-unsupported-shape mode={mode} member={Convert.ToHexString(reader.GetBlobBytes(member.Signature))} owner={ownerRaw} spec={specRaw} serialized=true compiler-only=true");
    }
}
