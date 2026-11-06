using System.Reflection;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class VectorUnsupportedDeclarations
{
    public static void Verify(string path)
    {
        using var pe = new PEReader(File.OpenRead(path)); var reader = pe.GetMetadataReader();
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("Installed declaration: " + label); }
        TypeDefinitionHandle Type(string ns, string name) => reader.TypeDefinitions.Single(h =>
        { var t = reader.GetTypeDefinition(h); return reader.StringComparer.Equals(t.Namespace, ns) && reader.StringComparer.Equals(t.Name, name); });
        void Parameter(GenericParameterHandleCollection handles, EntityHandle parent, string label, GenericParameterAttributes expectedAttributes = GenericParameterAttributes.None)
        {
            Check(handles.Count == 1, label + " arity");
            var parameter = reader.GetGenericParameter(handles.Single());
            Check(parameter.Parent == parent && parameter.Index == 0 && parameter.Attributes == expectedAttributes && parameter.GetConstraints().Count == 0, label + " unconstrained parameter");
            Console.WriteLine($"vector-unsupported-declaration {label} parent={MetadataTokens.GetToken(parent):X8} index=0 flags={(int)parameter.Attributes:X4} constraints=0");
        }
        var vector = Type(VectorUnsupportedFamily.Namespace, VectorUnsupportedFamily.TypeName);
        Parameter(reader.GetTypeDefinition(vector).GetGenericParameters(), vector, "Vector");
        var equatable = Type("System", "IEquatable`1");
        Parameter(reader.GetTypeDefinition(equatable).GetGenericParameters(), equatable, "IEquatable", (GenericParameterAttributes)0x20);
        var countHandle = reader.GetTypeDefinition(vector).GetMethods().Single(h => reader.StringComparer.Equals(reader.GetMethodDefinition(h).Name, "get_Count"));
        var count = reader.GetMethodDefinition(countHandle);
        Check(count.Attributes == (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            && count.ImplAttributes == MethodImplAttributes.IL && count.RelativeVirtualAddress != 0 && count.GetGenericParameters().Count == 0
            && Convert.ToHexString(reader.GetBlobBytes(count.Signature)) == "000008", "Count exact declaration");
        var helperType = Type("System", "ThrowHelper");
        var helperHandle = reader.GetTypeDefinition(helperType).GetMethods().Single(h => reader.StringComparer.Equals(reader.GetMethodDefinition(h).Name, VectorUnsupportedFamily.HelperName));
        var helper = reader.GetMethodDefinition(helperHandle);
        Check(helper.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            && helper.ImplAttributes == MethodImplAttributes.AggressiveInlining && helper.RelativeVirtualAddress != 0
            && Convert.ToHexString(reader.GetBlobBytes(helper.Signature)) == "10010001", "helper exact declaration");
        Parameter(helper.GetGenericParameters(), helperHandle, "ThrowHelper");
        var il = pe.GetMethodBody(count.RelativeVirtualAddress).GetILBytes()!;
        Check(il.Length >= 5 && il[0] == 0x28, "Count starts with call");
        var token = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 1)); Check(token.Kind == HandleKind.MethodSpecification, "Count exact generic call");
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)token);
        Check(spec.Method == helperHandle && Convert.ToHexString(reader.GetBlobBytes(spec.Signature)) == "0A011300", "Count guard uses original type T");
        Console.WriteLine($"vector-unsupported-declaration Count={MetadataTokens.GetToken(countHandle):X8} raw=000008 first-call={MetadataTokens.GetToken(token):X8} helper={MetadataTokens.GetToken(helperHandle):X8} raw=10010001 spec=0A011300");
    }

    // Each mutation writes a new private compiler-only copy. It is never a CLR input.
    public static void Copy(string mode, string source, string output)
    {
        if (File.Exists(output)) throw new InvalidOperationException("Refusing overwrite " + output);
        static string Digest(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        string sourceHash = Digest(source);
        CorFlags sourceFlags;
        Machine sourceMachine;
        PEMagic sourceMagic;
        int sourceNativeSize;
        string sourceName, sourcePublicKey;
        Version sourceVersion;
        using (var original = new PEReader(File.OpenRead(source)))
        {
            var metadata = original.GetMetadataReader();
            var assembly = metadata.GetAssemblyDefinition();
            sourceFlags = original.PEHeaders.CorHeader!.Flags;
            sourceNativeSize = original.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size;
            sourceMachine = original.PEHeaders.CoffHeader.Machine;
            sourceMagic = original.PEHeaders.PEHeader!.Magic;
            sourceName = metadata.GetString(assembly.Name);
            sourceVersion = assembly.Version;
            sourcePublicKey = Convert.ToHexString(metadata.GetBlobBytes(assembly.PublicKey));
        }
        using var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(source);
        // Re-emit IL into this private compiler-only copy; never rewrite the R2R input.
        core.MainModule.Attributes |= Mono.Cecil.ModuleAttributes.ILOnly;
        var vector = core.MainModule.GetType(VectorUnsupportedFamily.OwnerName);
        var itf = core.MainModule.GetType("System.IEquatable`1");
        var helper = core.MainModule.GetType("System.ThrowHelper").Methods.Single(x => x.Name == VectorUnsupportedFamily.HelperName);
        var count = vector.Methods.Single(x => x.Name == "get_Count");
        Mono.Cecil.GenericParameter? parameter = mode.StartsWith("owner-") ? vector.GenericParameters.Single()
            : mode.StartsWith("interface-") ? itf.GenericParameters.Single()
            : mode.StartsWith("helper-") ? helper.GenericParameters.Single() : null;
        switch (mode)
        {
            case "baseline": break;
            case "owner-flags": case "interface-flags": case "helper-flags": parameter!.Attributes = Mono.Cecil.GenericParameterAttributes.NotNullableValueTypeConstraint; break;
            case "owner-constraint": case "interface-constraint": case "helper-constraint": parameter!.Constraints.Add(new Mono.Cecil.GenericParameterConstraint(core.MainModule.GetType("System.ValueType"))); break;
            case "helper-public": helper.Attributes = (helper.Attributes & ~Mono.Cecil.MethodAttributes.MemberAccessMask) | Mono.Cecil.MethodAttributes.Public; break;
            case "helper-impl": helper.ImplAttributes = Mono.Cecil.MethodImplAttributes.IL; break;
            case "count-instance": count.IsStatic = false; count.HasThis = true; break;
            case "count-impl": count.ImplAttributes = Mono.Cecil.MethodImplAttributes.AggressiveInlining; break;
            default: throw new ArgumentException("Unknown declaration mutation " + mode);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        core.Write(output, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        using var pe = new PEReader(File.OpenRead(output)); var r = pe.GetMetadataReader();
        if (Digest(source) != sourceHash) throw new InvalidOperationException("Original CoreLib changed during private serialization");
        var copiedAssembly = r.GetAssemblyDefinition();
        var copyMagic = pe.PEHeaders.PEHeader!.Magic;
        // Cecil re-encodes this installed R2R header as PE32; the copy is metadata-only.
        bool normalizedReadyToRunHeader = sourceMachine == (Machine)0xfd1d
            && pe.PEHeaders.CoffHeader.Machine == (Machine)0xfd1d
            && sourceMagic == PEMagic.PE32Plus && copyMagic == PEMagic.PE32
            && (int)sourceFlags == 0x0c && (int)pe.PEHeaders.CorHeader!.Flags == 0x0d
            && sourceNativeSize == 220 && pe.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size == 0;
        if (pe.PEHeaders.CoffHeader.Machine != sourceMachine || (copyMagic != sourceMagic && !normalizedReadyToRunHeader)
            || pe.PEHeaders.CorHeader!.Flags != (sourceFlags | CorFlags.ILOnly)
            || pe.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size != 0
            || pe.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.RelativeVirtualAddress != 0
            || r.GetString(copiedAssembly.Name) != sourceName || copiedAssembly.Version != sourceVersion
            || Convert.ToHexString(r.GetBlobBytes(copiedAssembly.PublicKey)) != sourcePublicKey)
            throw new InvalidOperationException("Private IL-only copy architecture/identity/header mismatch");
        string Signature(string ns, string owner, string method)
        {
            var type = r.TypeDefinitions.Single(h => r.GetTypeDefinition(h).GetDeclaringType().IsNil
                && r.StringComparer.Equals(r.GetTypeDefinition(h).Namespace, ns)
                && r.StringComparer.Equals(r.GetTypeDefinition(h).Name, owner));
            var target = r.GetTypeDefinition(type).GetMethods().Single(h => r.StringComparer.Equals(r.GetMethodDefinition(h).Name, method));
            return Convert.ToHexString(r.GetBlobBytes(r.GetMethodDefinition(target).Signature));
        }
        if (Signature("System", "ThrowHelper", VectorUnsupportedFamily.HelperName) != "10010001"
            || Signature(VectorUnsupportedFamily.Namespace, VectorUnsupportedFamily.TypeName, "get_Count") != (mode == "count-instance" ? "200008" : "000008"))
            throw new InvalidOperationException("Private copy Count/helper raw signature mismatch");
        Console.WriteLine($"vector-unsupported-private-copy mode={mode} source-sha256={sourceHash} source-unchanged=true output-sha256={Digest(output)} source-flags={(int)sourceFlags:X8} copy-flags={(int)pe.PEHeaders.CorHeader.Flags:X8} source-native-header-size={sourceNativeSize} copy-native-header-size=0 source-machine={(int)sourceMachine:X4} copy-machine={(int)pe.PEHeaders.CoffHeader.Machine:X4} source-pe-magic={(int)sourceMagic:X4} copy-pe-magic={(int)copyMagic:X4} pinned-r2r-header-normalization={normalizedReadyToRunHeader} assembly-identity-preserved=true strong-name-verification-not-claimed=true nondeployable=true runtime-equivalence-not-claimed=true compiler-only=true");
        if (mode == "baseline")
        {
            Verify(output);
            return;
        }
        string typeName = mode.StartsWith("owner-") || mode.StartsWith("count-") ? VectorUnsupportedFamily.TypeName : mode.StartsWith("interface-") ? "IEquatable`1" : "ThrowHelper";
        string typeNamespace = mode.StartsWith("owner-") || mode.StartsWith("count-") ? VectorUnsupportedFamily.Namespace : "System";
        var typeHandle = r.TypeDefinitions.Single(h => r.GetTypeDefinition(h).GetDeclaringType().IsNil
            && r.StringComparer.Equals(r.GetTypeDefinition(h).Namespace, typeNamespace)
            && r.StringComparer.Equals(r.GetTypeDefinition(h).Name, typeName));
        var type = r.GetTypeDefinition(typeHandle);
        var methodHandle = type.GetMethods().FirstOrDefault(h => r.StringComparer.Equals(r.GetMethodDefinition(h).Name, mode.StartsWith("count-") ? "get_Count" : VectorUnsupportedFamily.HelperName));
        if (mode.EndsWith("flags") || mode.EndsWith("constraint"))
        {
            var parameters = mode.StartsWith("helper-") ? r.GetMethodDefinition(methodHandle).GetGenericParameters() : type.GetGenericParameters();
            var p = r.GetGenericParameter(parameters.Single());
            if (mode.EndsWith("flags") ? p.Attributes != GenericParameterAttributes.NotNullableValueTypeConstraint || p.GetConstraints().Count != 0 : p.Attributes != (mode.StartsWith("interface-") ? (GenericParameterAttributes)0x20 : GenericParameterAttributes.None) || p.GetConstraints().Count != 1)
                throw new InvalidOperationException("Declaration mutation normalized");
            Console.WriteLine($"vector-unsupported-private-declaration mode={mode} flags={(int)p.Attributes:X4} constraints={p.GetConstraints().Count} serialized=true compiler-only=true");
        }
        else
        {
            var m = r.GetMethodDefinition(methodHandle);
            bool valid = mode switch {
                "helper-public" => (m.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public,
                "helper-impl" => m.ImplAttributes == MethodImplAttributes.IL,
                "count-instance" => (m.Attributes & MethodAttributes.Static) == 0 && r.GetBlobBytes(m.Signature)[0] == 0x20,
                "count-impl" => m.ImplAttributes == MethodImplAttributes.AggressiveInlining, _ => false };
            if (!valid) throw new InvalidOperationException("Declaration mutation normalized");
            Console.WriteLine($"vector-unsupported-private-declaration mode={mode} attrs={(int)m.Attributes:X4} impl={(int)m.ImplAttributes:X4} raw={Convert.ToHexString(r.GetBlobBytes(m.Signature))} serialized=true compiler-only=true");
        }
    }
}
