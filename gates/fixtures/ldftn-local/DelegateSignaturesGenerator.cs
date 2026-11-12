using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static partial class DelegateSignaturesFixture
{
    private static void Generate(string mode, string outputPath, string corePath)
    {
        using var coreImage = new PEReader(File.OpenRead(corePath));
        var core = coreImage.GetMetadataReader();
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString("DelegateSignatures.dll"),
            metadata.GetOrAddGuid(new Guid("13baf512-d975-438e-bf05-82ab60261729")), default, default);
        metadata.AddAssembly(metadata.GetOrAddString("DelegateSignatures." + mode), new Version(1, 0, 0, 0),
            default, default, 0, AssemblyHashAlgorithm.None);
        var coreAssembly = core.GetAssemblyDefinition();
        var coreRef = metadata.AddAssemblyReference(metadata.GetOrAddString("System.Private.CoreLib"),
            coreAssembly.Version, default, metadata.GetOrAddBlob(core.GetBlobBytes(coreAssembly.PublicKey)), AssemblyFlags.PublicKey, default);
        var imports = new Dictionary<string, TypeReferenceHandle>(StringComparer.Ordinal);
        TypeReferenceHandle Import(string space, string name)
        {
            string key = space + "." + name;
            if (!imports.TryGetValue(key, out var handle))
            {
                handle = metadata.AddTypeReference(coreRef, metadata.GetOrAddString(space), metadata.GetOrAddString(name));
                imports.Add(key, handle);
            }
            return handle;
        }
        byte[] Class(EntityHandle type)
        {
            var blob = new BlobBuilder();
            blob.WriteByte(0x12);
            blob.WriteCompressedInteger(CodedIndex.TypeDefOrRef(type));
            return blob.ToArray();
        }
        byte[] ImportSignature(MethodDefinition method)
        {
            var original = core.GetBlobReader(method.Signature);
            var blob = new BlobBuilder();
            blob.WriteByte(original.ReadByte());
            int count = original.ReadCompressedInteger();
            blob.WriteCompressedInteger(count);
            for (int i = 0; i <= count; i++) ImportType(ref original, blob);
            if (original.RemainingBytes != 0) throw new InvalidOperationException("control declaration has trailing bytes");
            return blob.ToArray();
        }
        void ImportType(ref BlobReader original, BlobBuilder blob)
        {
            byte code = original.ReadByte();
            blob.WriteByte(code);
            if (code == 0x12)
            {
                var handle = original.ReadTypeHandle();
                if (handle.Kind != HandleKind.TypeDefinition) throw new InvalidOperationException("control class is not a CoreLib definition");
                var type = core.GetTypeDefinition((TypeDefinitionHandle)handle);
                blob.WriteCompressedInteger(CodedIndex.TypeDefOrRef(Import(core.GetString(type.Namespace), core.GetString(type.Name))));
            }
            else if (code == 0x1d) ImportType(ref original, blob);
            else if (code is not (1 or 2 or 8 or 0x0e or 0x18 or 0x1c))
                throw new InvalidOperationException("unexpected control signature type " + code);
        }
        var bodies = new BlobBuilder();
        var bodyEncoder = new MethodBodyStreamEncoder(bodies);
        var code = new InstructionEncoder(new BlobBuilder());
        var culture = Import("System.Globalization", "CultureInfo");
        var invariant = metadata.AddMemberReference(culture, metadata.GetOrAddString("get_InvariantCulture"),
            metadata.GetOrAddBlob(new byte[] { 0, 0 }.Concat(Class(culture)).ToArray()));
        foreach (string name in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            var setter = metadata.AddMemberReference(culture, metadata.GetOrAddString(name),
                metadata.GetOrAddBlob(new byte[] { 0, 1, 1 }.Concat(Class(culture)).ToArray()));
            code.Call(invariant);
            code.Call(setter);
        }
        var candidates = DelegateSignaturesDeclarations.Candidates(core);
        if (mode == "control")
        {
            foreach (var target in candidates)
            {
                Emit(target, false);
                Emit(target, true);
            }
        }
        else
        {
            string owner = mode is "modifier-call" or "trailing-call" ? "Delegate" : "MulticastDelegate";
            string name = mode switch
            {
                "return-call" => "NewMulticastDelegate",
                "parameter-address" => "CtorOpened",
                "modifier-call" => "Equals",
                "header-address" or "trailing-call" or "fake-owner-address" or "owner-typespec-call" or "owner-modifier-address" => "GetHashCode",
                _ => throw new ArgumentException("unknown delegate signature case " + mode),
            };
            var target = candidates.Single(item => item.Owner == "System." + owner && item.Name == name
                && (name != "NewMulticastDelegate" || core.GetBlobBytes(core.GetMethodDefinition(item.Handle).Signature)[1] == 2));
            Emit(target, mode.EndsWith("-address", StringComparison.Ordinal));
        }
        code.OpCode(ILOpCode.Ret);
        var main = metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
            metadata.GetOrAddString("Main"), metadata.GetOrAddBlob(new byte[] { 0, 0, 1 }),
            bodyEncoder.AddMethodBody(code, maxStack: 8), MetadataTokens.ParameterHandle(1));
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
            default, MetadataTokens.FieldDefinitionHandle(1), main);
        metadata.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
            metadata.GetOrAddString("DelegateSignatures"), metadata.GetOrAddString("Program"), Import("System", "Object"),
            MetadataTokens.FieldDefinitionHandle(1), main);
        var pe = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage),
            new MetadataRootBuilder(metadata), bodies, entryPoint: main, flags: CorFlags.ILOnly);
        var image = new BlobBuilder();
        pe.Serialize(image);
        string output = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllBytes(output, image.ToArray());
        if (mode == "fake-owner-address") WriteFakeOwner(Path.Combine(Path.GetDirectoryName(output)!, "FakeDelegateOwner.dll"));

        void Emit(DelegateSignaturesDeclarations.Target target, bool address)
        {
            var definition = core.GetMethodDefinition(target.Handle);
            byte[] signature = ImportSignature(definition);
            EntityHandle owner = Import("System", target.Owner[7..]);
            if (mode != "control")
            {
                switch (mode)
                {
                    case "return-call":
                        signature = new byte[] { 0x20, 2 }.Concat(Class(Import("System", "Delegate"))).Concat(new byte[] { 0x1d, 0x1c, 8 }).ToArray();
                        break;
                    case "parameter-address": signature[^1] = 0x19; break;
                    case "modifier-call":
                        var modifier = new BlobBuilder();
                        modifier.WriteBytes(new byte[] { 0x20, 1, 2, 0x20 });
                        modifier.WriteCompressedInteger(CodedIndex.TypeDefOrRef(Import("System.Runtime.CompilerServices", "IsConst")));
                        modifier.WriteByte(0x1c);
                        signature = modifier.ToArray();
                        break;
                    case "header-address": signature[0] = 0x60; break;
                    case "trailing-call": signature = signature.Concat(new byte[] { 0 }).ToArray(); break;
                    case "fake-owner-address":
                        var fakeAssembly = metadata.AddAssemblyReference(metadata.GetOrAddString("FakeDelegateOwner"),
                            new Version(1, 0, 0, 0), default, default, 0, default);
                        owner = metadata.AddTypeReference(fakeAssembly, metadata.GetOrAddString("System"), metadata.GetOrAddString("MulticastDelegate"));
                        break;
                    case "owner-typespec-call": owner = metadata.AddTypeSpecification(metadata.GetOrAddBlob(Class(owner))); break;
                    case "owner-modifier-address":
                        var modified = new BlobBuilder();
                        modified.WriteByte(0x20);
                        modified.WriteCompressedInteger(CodedIndex.TypeDefOrRef(Import("System.Runtime.CompilerServices", "IsConst")));
                        modified.WriteBytes(Class(owner));
                        owner = metadata.AddTypeSpecification(metadata.GetOrAddBlob(modified));
                        break;
                }
            }
            var reference = metadata.AddMemberReference(owner, metadata.GetOrAddString(target.Name), metadata.GetOrAddBlob(signature));
            if (address)
            {
                code.OpCode(ILOpCode.Ldftn);
                code.Token(reference);
            }
            else
            {
                code.OpCode(ILOpCode.Ldnull);
                var original = core.GetBlobReader(definition.Signature);
                original.ReadByte();
                int count = original.ReadCompressedInteger();
                var ignored = new BlobBuilder();
                ImportType(ref original, ignored);
                for (int i = 0; i < count; i++)
                {
                    var type = new BlobBuilder();
                    ImportType(ref original, type);
                    byte element = type.ToArray()[0];
                    if (element is 2 or 8 or 0x18)
                    {
                        code.LoadConstantI4(0);
                        if (element == 0x18) code.OpCode(ILOpCode.Conv_i);
                    }
                    else code.OpCode(ILOpCode.Ldnull);
                }
                code.Call(reference);
            }
            if (address || signature[2] != 1) code.OpCode(ILOpCode.Pop);
        }

        void WriteFakeOwner(string path)
        {
            // A reference assembly bypasses the primary-input name collision guard and
            // exercises the delegate callable resolver's assembly identity check.
            var fake = new MetadataBuilder();
            fake.AddModule(0, fake.GetOrAddString("FakeDelegateOwner.dll"),
                fake.GetOrAddGuid(new Guid("172d3637-2de9-4670-b70e-f7f1a9b24045")), default, default);
            fake.AddAssembly(fake.GetOrAddString("FakeDelegateOwner"), new Version(1, 0, 0, 0), default, default, 0, AssemblyHashAlgorithm.None);
            var coreScope = fake.AddAssemblyReference(fake.GetOrAddString("System.Private.CoreLib"), coreAssembly.Version, default,
                fake.GetOrAddBlob(core.GetBlobBytes(coreAssembly.PublicKey)), AssemblyFlags.PublicKey, default);
            var parent = fake.AddTypeReference(coreScope, fake.GetOrAddString("System"), fake.GetOrAddString("Delegate"));
            var fakeCode = new InstructionEncoder(new BlobBuilder());
            fakeCode.LoadConstantI4(0);
            fakeCode.OpCode(ILOpCode.Ret);
            var fakeBodies = new BlobBuilder();
            var method = fake.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                MethodImplAttributes.IL, fake.GetOrAddString("GetHashCode"), fake.GetOrAddBlob(new byte[] { 0x20, 0, 8 }),
                new MethodBodyStreamEncoder(fakeBodies).AddMethodBody(fakeCode, maxStack: 1), MetadataTokens.ParameterHandle(1));
            fake.AddTypeDefinition(TypeAttributes.NotPublic, default, fake.GetOrAddString("<Module>"),
                default, MetadataTokens.FieldDefinitionHandle(1), method);
            fake.AddTypeDefinition(TypeAttributes.Public | TypeAttributes.Abstract, fake.GetOrAddString("System"),
                fake.GetOrAddString("MulticastDelegate"), parent, MetadataTokens.FieldDefinitionHandle(1), method);
            var fakePe = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
                new MetadataRootBuilder(fake), fakeBodies, flags: CorFlags.ILOnly);
            var fakeImage = new BlobBuilder();
            fakePe.Serialize(fakeImage);
            File.WriteAllBytes(path, fakeImage.ToArray());
        }
    }
}
