using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeReference = Mono.Cecil.TypeReference;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using CMethod = Mono.Cecil.MethodDefinition;
using CType = Mono.Cecil.TypeDefinition;

internal static class FixedVectorValueInputs
{
    internal static int Width { get; set; }
    internal static string TypeName => "Vector" + Width + "`1";
    internal static string OwnerName => "System.Runtime.Intrinsics." + TypeName;
    internal static string Mutation(string mode) => mode.StartsWith("constrained-equatable-", StringComparison.Ordinal) ? mode[22..]
        : mode.StartsWith("constrained-object-", StringComparison.Ordinal) ? mode[19..]
        : mode.StartsWith("boxed-", StringComparison.Ordinal) ? mode[6..]
        : mode.StartsWith("object-", StringComparison.Ordinal) ? mode[7..]
        : mode.StartsWith("hash-", StringComparison.Ordinal) ? mode[5..] : mode;

    // These assemblies are metadata/compiler inputs, never CLR execution inputs.
    public static void Generate(string mode, string corePath, string output)
    {
        string baseMode = Mutation(mode);
        string[] modes = { "all", "valid", "static", "explicit-this", "vararg", "generic", "byref-result", "return-modopt", "wrong-result", "byref-input", "modopt", "modreq", "wrong-closed", "wrong-width", "wrong-family", "wrong-input", "extra", "missing", "trailing", "owner-trailing", "methodspec", "bool", "char", "decimal", "int128", "uint128", "enum", "struct", "open", "string", "nullable", "generic-struct", "byref-like", "foreign-owner", "constrained-equatable", "constrained-object", "constrained-hash", "constrained-foreign-equatable", "constrained-wrong-equatable", "constrained-foreign-object", "constrained-receiver-modopt", "constrained-receiver-trailing", "methodspec-zero", "mvar-context", "context-spec-var", "context-spec-mvar" };
        if (!modes.Contains(baseMode)) throw new ArgumentException("Unknown vector value mode: " + mode);
        if (File.Exists(output)) throw new InvalidOperationException("Refusing to replace fixture input: " + output);
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("FixedVectorValueGuardInput", new Version(1, 0)), "FixedVectorValueGuardInput", ModuleKind.Console);
        var m = app.MainModule; m.Name = "FixedVectorValueGuardInput." + mode;
        var host = new CType("Guard", "Entry", Mono.Cecil.TypeAttributes.Public, m.TypeSystem.Object);
        m.Types.Add(host);
        var entry = new CMethod("Main", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, m.TypeSystem.Void);
        host.Methods.Add(entry); m.EntryPoint = entry;
        var vector = core.MainModule.GetType(FixedVectorValueInputs.OwnerName);
        GenericInstanceType Closed(TypeReference type, TypeReference element)
        { var result = new GenericInstanceType(type); result.GenericArguments.Add(element); return result; }
        var elements = new[] { "SByte", "Byte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "IntPtr", "UIntPtr", "Single", "Double" };
        int count = 0;
        foreach (string elementName in mode == "all" ? elements : new[] { "Double" })
        foreach (string route in mode == "all" ? new[] { "typed", "object", "hash", "constrained-equatable", "constrained-object", "constrained-hash" } : new[] { mode.StartsWith("boxed-") ? "boxed-equatable" : mode.StartsWith("constrained-equatable-") || mode.StartsWith("constrained-receiver-") ? "constrained-equatable" : mode.StartsWith("constrained-object-") ? "constrained-object" : mode.StartsWith("constrained-") ? mode : mode.StartsWith("object-") ? "object" : mode.StartsWith("hash-") ? "hash" : "typed" })
        {
            string mutation = Mutation(mode);
            TypeReference element = m.ImportReference(core.MainModule.GetType("System." + elementName));
            string? unsupported = mutation switch { "bool" => "Boolean", "char" => "Char", "decimal" => "Decimal", "int128" => "Int128", "uint128" => "UInt128", _ => null };
            if (unsupported is not null) element = m.ImportReference(core.MainModule.GetType("System." + unsupported));
            if (mutation is "enum" or "struct" or "byref-like")
            {
                var custom = new CType("Guard", "Element", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed,
                    m.ImportReference(core.MainModule.GetType(mutation == "enum" ? "System.Enum" : "System.ValueType")));
                custom.Fields.Add(new FieldDefinition(mutation == "enum" ? "value__" : "Value", Mono.Cecil.FieldAttributes.Public, m.TypeSystem.Int32));
                if (mutation == "byref-like")
                    custom.CustomAttributes.Add(new Mono.Cecil.CustomAttribute(m.ImportReference(core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute")
                        .Methods.Single(method => method.IsConstructor && method.Parameters.Count == 0))));
                m.Types.Add(custom); element = custom;
            }
            if (mutation == "string") element = m.TypeSystem.String;
            if (mutation == "nullable") element = Closed(m.ImportReference(core.MainModule.GetType("System.Nullable`1")), m.TypeSystem.Int32);
            if (mutation == "generic-struct")
            {
                var custom = new CType("Guard", "GenericElement`1", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed,
                    m.ImportReference(core.MainModule.GetType("System.ValueType")));
                custom.GenericParameters.Add(new GenericParameter("T", custom)); m.Types.Add(custom);
                element = Closed(custom, m.TypeSystem.Int32);
            }
            var ownerDefinition = m.ImportReference(vector);
            if (mutation == "open") element = ownerDefinition.GenericParameters[0];
            if (mutation == "foreign-owner")
            {
                var foreign = new CType("System.Runtime.Intrinsics", FixedVectorValueInputs.TypeName, Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed, m.ImportReference(core.MainModule.GetType("System.ValueType")));
                foreign.GenericParameters.Add(new GenericParameter("T", foreign)); m.Types.Add(foreign); ownerDefinition = foreign;
            }
            var owner = Closed(ownerDefinition, element);
            bool hash = route.Contains("hash"), obj = route.Contains("object");
            var declaration = vector.Methods.Single(x => x.Name == (hash ? "GetHashCode" : "Equals") && x.Parameters.Count == (hash ? 0 : 1)
                && (hash || (x.Parameters[0].ParameterType.FullName == "System.Object") == obj));
            var target = m.ImportReference(declaration); target.DeclaringType = owner;
            if (route.StartsWith("constrained-") || route == "boxed-equatable")
            {
                if (route.Contains("equatable"))
                {
                    var itf = core.MainModule.GetType("System.IEquatable`1");
                    TypeReference imported = m.ImportReference(itf);
                    if (route == "constrained-foreign-equatable")
                    {
                        var foreign = new CType("System", "IEquatable`1", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Interface | Mono.Cecil.TypeAttributes.Abstract);
                        foreign.GenericParameters.Add(new GenericParameter("T", foreign)); m.Types.Add(foreign); imported = foreign;
                    }
                    target = m.ImportReference(itf.Methods.Single(x => x.Name == "Equals")); target.DeclaringType = Closed(imported, route == "constrained-wrong-equatable" ? Closed(m.ImportReference(vector), m.TypeSystem.Int32) : owner);
                }
                else
                {
                    target = m.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(x => x.Name == (hash ? "GetHashCode" : "Equals") && !x.IsStatic));
                    if (route == "constrained-foreign-object")
                    {
                        var foreign = new CType("System", "Object", Mono.Cecil.TypeAttributes.Public, m.TypeSystem.Object);
                        m.Types.Add(foreign); target.DeclaringType = foreign;
                    }
                }
            }
            switch (mutation)
            {
                case "mvar-context":
                    var contextOwner = new MethodReference("Context", m.TypeSystem.Void, host);
                    var methodArg = new GenericParameter("U", contextOwner); contextOwner.GenericParameters.Add(methodArg);
                    target.Parameters[0].ParameterType = methodArg; break;
                case "static": target.HasThis = false; break;
                case "explicit-this": target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new GenericParameter("U", target)); break;
                case "byref-result": target.ReturnType = new ByReferenceType(target.ReturnType); break;
                case "return-modopt": target.ReturnType = new OptionalModifierType(m.TypeSystem.Object, target.ReturnType); break;
                case "wrong-result": target.ReturnType = m.TypeSystem.Int64; break;
                case "byref-input": target.Parameters[0].ParameterType = new ByReferenceType(target.Parameters[0].ParameterType); break;
                case "modopt": target.Parameters[0].ParameterType = new OptionalModifierType(m.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                case "modreq": target.Parameters[0].ParameterType = new RequiredModifierType(m.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                case "wrong-closed": target.Parameters[0].ParameterType = Closed(m.ImportReference(vector), m.TypeSystem.Int32); break;
                case "wrong-width": target.Parameters[0].ParameterType = Closed(m.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.Vector" + (Width == 64 ? 128 : 64) + "`1")), element); break;
                case "wrong-family": target.Parameters[0].ParameterType = Closed(m.ImportReference(core.MainModule.GetType("System.Numerics.Vector`1")), element); break;
                case "wrong-input": target.Parameters[0].ParameterType = m.TypeSystem.Int64; break;
                case "extra": case "trailing": target.Parameters.Add(new ParameterDefinition(m.TypeSystem.Int32)); break;
                case "missing": target.Parameters.Clear(); break;
                case "owner-trailing": ((GenericInstanceType)target.DeclaringType).GenericArguments.Add(m.TypeSystem.Int32); break;
            }
            var contextualOwner = target.DeclaringType;
            if (mutation is "context-spec-var" or "context-spec-mvar")
            {
                if (mutation == "context-spec-var") target.DeclaringType = ownerDefinition.GenericParameters[0];
                else
                {
                    var genericOwner = new MethodReference("Context", m.TypeSystem.Void, host);
                    var argument = new GenericParameter("U", genericOwner); genericOwner.GenericParameters.Add(argument);
                    target.DeclaringType = argument;
                }
            }
            MethodReference callTarget = target;
            if (mutation is "methodspec" or "methodspec-zero" or "context-spec-var" or "context-spec-mvar")
            { var spec = new GenericInstanceMethod(target); if (mutation == "methodspec") spec.GenericArguments.Add(m.TypeSystem.Int32); callTarget = spec; }
            var probe = new CMethod("Probe" + count++, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, m.TypeSystem.Void);
            host.Methods.Add(probe); probe.Body.InitLocals = true; var local = new VariableDefinition(owner); probe.Body.Variables.Add(local);
            var il = probe.Body.GetILProcessor();
            if (route == "boxed-equatable") { il.Emit(OpCodes.Ldloc, local); il.Emit(OpCodes.Box, owner); }
            else il.Emit(OpCodes.Ldloca, local);
            if (!hash) { if (obj) il.Emit(OpCodes.Ldnull); else il.Emit(OpCodes.Ldloc, local); }
            if (route.StartsWith("constrained-"))
            {
                TypeReference receiver = owner;
                if (mutation == "constrained-receiver-modopt") receiver = new OptionalModifierType(m.TypeSystem.Object, owner);
                if (mutation == "constrained-receiver-trailing")
                { var padded = Closed(ownerDefinition, element); padded.GenericArguments.Add(m.TypeSystem.Int32); receiver = padded; }
                il.Emit(OpCodes.Constrained, receiver);
            }
            il.Emit(route.StartsWith("constrained-") || route == "boxed-equatable" ? OpCodes.Callvirt : OpCodes.Call, callTarget); il.Emit(OpCodes.Pop);
            if (mutation is "context-spec-var" or "context-spec-mvar") { il.Emit(OpCodes.Ldtoken, route == "boxed-equatable" ? contextualOwner : owner); il.Emit(OpCodes.Pop); }
            il.Emit(OpCodes.Ret);
            entry.Body.GetILProcessor().Emit(OpCodes.Call, probe);
        }
        entry.Body.GetILProcessor().Emit(OpCodes.Ret);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (baseMode is "trailing" or "owner-trailing" or "constrained-receiver-trailing") PatchTrailing(output, baseMode != "trailing", baseMode == "constrained-receiver-trailing");
        FixedVectorValueShapeProof.Verify(mode, output);
        Console.WriteLine($"fixed-vector-value-input mode={mode} probes={count} compiler-only=true");
    }

    private static void PatchTrailing(string path, bool owner, bool receiver)
    {
        var bytes = File.ReadAllBytes(path);
        using var pe = new PEReader(new MemoryStream(bytes)); var reader = pe.GetMetadataReader();
        var reference = reader.MemberReferences.Select(reader.GetMemberReference).Single(x => reader.StringComparer.Equals(x.Name, "Equals"));
        var parent = reference.Parent;
        if (receiver)
        {
            parent = default;
            foreach (var methodHandle in reader.MethodDefinitions)
            {
                var definition = reader.GetMethodDefinition(methodHandle);
                if (definition.RelativeVirtualAddress == 0) continue;
                var il = pe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
                for (int i = 0; i + 5 < il.Length; i++) if (il[i] == 0xfe && il[i + 1] == 0x16)
                    parent = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i + 2));
            }
            if (parent.IsNil) throw new InvalidOperationException("Missing constrained receiver");
        }
        var handle = owner ? reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature : reference.Signature;
        var raw = reader.GetBlobBytes(handle);
        int index = 1;
        if (owner)
        { var blob = reader.GetBlobReader(handle); blob.ReadByte(); blob.ReadByte(); blob.ReadCompressedInteger(); index = blob.Offset; }
        if (raw[index] != (owner || reference.Name.IsNil || !reader.StringComparer.Equals(reference.Name, "GetHashCode") ? 2 : 1)) throw new InvalidOperationException("Expected padded count");
        raw[index]--;
        int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(handle);
        int prefix = bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
        raw.CopyTo(bytes, offset + prefix); File.WriteAllBytes(path, bytes);
        using var verify = new PEReader(File.OpenRead(path)); var checkedReader = verify.GetMetadataReader();
        if (!checkedReader.GetBlobBytes(handle).SequenceEqual(raw)) throw new InvalidOperationException("Raw fixture mutation was normalized");
        Console.WriteLine($"fixed-vector-value-raw {(owner ? "owner" : "method")}={Convert.ToHexString(raw)} trailing-byte-preserved=true");
    }
}
