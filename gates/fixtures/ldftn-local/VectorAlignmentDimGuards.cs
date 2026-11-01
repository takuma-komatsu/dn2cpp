using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;

internal static class VectorAlignmentDimGuards
{
    private static readonly string[] Mutations = { "valid", "instance", "explicit-this", "vararg", "generic", "return", "parameter",
        "byref", "modifier", "owner-modifier", "owner-element", "owner-self", "receiver-modifier", "methodspec" };

    public static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--vector-alignment-dim-helper-negative")
        {
            Build(args[1], args[2], "helper-" + args[3]);
            return true;
        }
        if (args.Length != 4 || args[0] != "--vector-alignment-dim-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Build(args[3], args[2]);
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(args[3])); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Model("Compilation").GetMethod("Create", statics)!.Invoke(null,
            new object[] { new[] { Path.GetFullPath(args[3]), Path.GetFullPath(args[2]) }, options })!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var core = modules.Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "AlignmentDimGuards");
        var reader = (MetadataReader)Get(core, "Reader");
        var originalPe = (PEReader)Get(core, "PE");
        var originalImage = File.ReadAllBytes(args[2]);
        var empty = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var primitive = Model("TypeDesc").GetMethod("MakePrimitive", statics)!;
        var makeClass = Model("TypeDesc").GetMethod("MakeClass", statics)!;
        var instantiate = Model("Compilation").GetMethod("Instantiate", instance)!;
        var complete = Model("Compilation").GetMethod("EnsureCompleted", instance)!;
        var referenceResolver = Model("Compilation").GetMethod("ResolveMemberRefMethod", instance)!;
        var resolve = Model("Compilation").GetMethod("TryResolveSimdAlignmentCall", instance)!;
        var validateBody = Model("Compilation").GetMethod("ValidateSimdAlignmentBody", instance)!;
        var interfaceHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == "ISimdVector`2");
        var loadHandle = reader.GetTypeDefinition(interfaceHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "LoadAligned");
        var loadBody = originalPe.GetMethodBody(reader.GetMethodDefinition(loadHandle).RelativeVirtualAddress).GetILBytes()!;
        var callToken = MetadataTokens.EntityHandle(BitConverter.ToInt32(loadBody, 8));
        var constrainedToken = MetadataTokens.EntityHandle(BitConverter.ToInt32(loadBody, 3));
        Array Arguments(params object[] values)
        {
            var result = Array.CreateInstance(Model("TypeDesc"), values.Length);
            for (int index = 0; index < values.Length; index++) result.SetValue(values[index], index);
            return result;
        }
        int checks = 0;
        void Check(bool value, string label) { if (!value) throw new InvalidOperationException("SIMD alignment guard: " + label); checks++; }
        object? Invoke(object module, EntityHandle call, object context, EntityHandle receiver, object self, object slot) =>
            resolve.Invoke(compilation, new[] { module, (object)call, context, receiver, self, slot });
        bool Refused(Func<object?> action)
        {
            try { return action() is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        var cases = new List<(object Vector, object Self, object Context, object Slot, object Bridge, int Width)>();
        foreach (int width in VectorAlignmentFixture.Widths)
        foreach (var elementType in VectorAlignmentDimFixture.Elements)
        {
            var code = elementType == typeof(nint) ? PrimitiveTypeCode.IntPtr : elementType == typeof(nuint) ? PrimitiveTypeCode.UIntPtr
                : Enum.Parse<PrimitiveTypeCode>(elementType.Name);
            var element = primitive.Invoke(null, new object[] { code })!;
            string ownerName = width == 0 ? "Vector`1" : "Vector" + width + "`1";
            var vectorHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Name) == ownerName
                && reader.GetString(reader.GetTypeDefinition(h).Namespace) == (width == 0 ? "System.Numerics" : "System.Runtime.Intrinsics"));
            var vector = instantiate.Invoke(compilation, new object[] { core, vectorHandle, Arguments(element) })!;
            complete.Invoke(compilation, new[] { vector });
            var self = makeClass.Invoke(null, new[] { vector })!;
            var contract = instantiate.Invoke(compilation, new object[] { core, interfaceHandle, Arguments(self, element) })!;
            complete.Invoke(compilation, new[] { contract });
            var context = Get(contract, "Context");
            var slot = referenceResolver.Invoke(compilation, new[] { core, (object)(MemberReferenceHandle)callToken, context })!;
            var bridge = Invoke(core, callToken, context, constrainedToken, self, slot);
            Check(bridge is not null, "positive-" + width + "-" + code);
            Check(ReferenceEquals(bridge, Invoke(core, callToken, context, constrainedToken, self, slot)), "cached-positive");
            Check(Get(bridge!, "DeclaringClass") == vector && (int)Get(bridge!, "Rva") != 0, "real-body-owner");
            cases.Add((vector, self, context, slot, bridge!, width));
        }
        var control = cases.Single(c => c.Width == 128 && ((PrimitiveTypeCode)Get(((Array)Get(c.Context, "TypeArgs")).GetValue(1)!, "Primitive")) == PrimitiveTypeCode.Int32);
        object? Good() => Invoke(core, callToken, control.Context, constrainedToken, control.Self, control.Slot);
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[3]);
        foreach (var method in serialized.MainModule.GetType("Fixture.Cases").Methods.Where(m => !m.Name.StartsWith("helper-", StringComparison.Ordinal)))
        {
            var receiver = MetadataTokens.EntityHandle(((Mono.Cecil.TypeReference)method.Body.Instructions[0].Operand).MetadataToken.ToInt32());
            var call = MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)method.Body.Instructions[1].Operand).MetadataToken.ToInt32());
            Check(ReferenceEquals(Good(), control.Bridge), "reference-good-before");
            Check(method.Name == "valid" ? ReferenceEquals(Invoke(app, call, empty, receiver, control.Self, control.Slot), control.Bridge)
                : Refused(() => Invoke(app, call, empty, receiver, control.Self, control.Slot)), "reference-" + method.Name);
            Check(ReferenceEquals(Good(), control.Bridge), "reference-good-after");
        }
        var helperReferences = serialized.MainModule.GetType("Fixture.Cases").Methods.Where(m => m.Name.StartsWith("helper-", StringComparison.Ordinal))
            .ToDictionary(m => m.Name, m => MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)m.Body.Instructions[0].Operand).MetadataToken.ToInt32()));
        var functionResolver = Model("Compilation").GetMethod("ResolveFunctionMemberRef", instance)!;
        var callResolver = Model("Compilation").GetMethod("ResolveCallTarget", instance)!;
        var specResolver = Model("Compilation").GetMethod("ResolveMethodSpec", instance)!;
        object? Helper(System.Reflection.MethodInfo resolver, EntityHandle token) => resolver.Invoke(compilation,
            new[] { app, resolver == callResolver ? (object)token : token.Kind == HandleKind.MethodSpecification ? (object)(MethodSpecificationHandle)token : (MemberReferenceHandle)token, empty });
        var validHelper = Helper(referenceResolver, helperReferences["helper-valid"]);
        Check(validHelper is not null, "helper-valid");
        foreach (var item in helperReferences)
        foreach (var resolver in item.Value.Kind == HandleKind.MethodSpecification ? new[] { specResolver, callResolver } : new[] { referenceResolver, functionResolver, callResolver })
        {
            Check(ReferenceEquals(Helper(referenceResolver, helperReferences["helper-valid"]), validHelper), "helper-good-before");
            Check(item.Key == "helper-valid" ? ReferenceEquals(Helper(resolver, item.Value), validHelper)
                : Refused(() => Helper(resolver, item.Value)), "helper-reference-" + item.Key + "-" + resolver.Name);
            Check(ReferenceEquals(Helper(referenceResolver, helperReferences["helper-valid"]), validHelper), "helper-good-after");
        }
        void Poison(object value, string field, object? replacement)
        {
            object saved = Get(value, field); Set(value, field, replacement);
            try { Check(Refused(Good), "model-" + field); }
            finally { Set(value, field, saved); }
            Check(ReferenceEquals(Good(), control.Bridge), "model-restored-" + field);
        }
        Poison(core, "AssemblyName", "Foreign.CoreLib"); Poison(control.Vector, "Module", app);
        Poison(control.Vector, "IsInterface", true); Poison(control.Vector, "IsValueType", false); Poison(control.Vector, "GenericArity", 2);
        Poison(control.Slot, "Module", app); Poison(control.Slot, "Rva", 1); Poison(control.Slot, "IsSynthetic", true);
        Poison(control.Slot, "Attributes", MethodAttributes.Public | MethodAttributes.Static);
        Poison(control.Slot, "ImplAttributes", MethodImplAttributes.InternalCall);
        Poison(control.Bridge, "Handle", default(MethodDefinitionHandle)); Poison(control.Bridge, "Rva", 0);
        Poison(control.Bridge, "IsSynthetic", true); Poison(control.Bridge, "Name", "get_Alignment");
        Poison(control.Bridge, "ImplAttributes", MethodImplAttributes.InternalCall);
        var slotResult = Get(Get(control.Slot, "Signature"), "ReturnType"); Poison(slotResult, "Primitive", PrimitiveTypeCode.UInt32);
        foreach (var code in new[] { PrimitiveTypeCode.Char, PrimitiveTypeCode.Boolean, PrimitiveTypeCode.String, PrimitiveTypeCode.Object })
        {
            var unsupported = primitive.Invoke(null, new object[] { code })!;
            var vectorHandle = (TypeDefinitionHandle)Get(control.Vector, "Handle");
            var vector = instantiate.Invoke(compilation, new object[] { core, vectorHandle, Arguments(unsupported) })!;
            complete.Invoke(compilation, new[] { vector }); var self = makeClass.Invoke(null, new[] { vector })!;
            var contract = instantiate.Invoke(compilation, new object[] { core, interfaceHandle, Arguments(self, unsupported) })!;
            complete.Invoke(compilation, new[] { contract }); var context = Get(contract, "Context");
            var slot = referenceResolver.Invoke(compilation, new[] { core, (object)(MemberReferenceHandle)callToken, context })!;
            Check(Refused(() => Invoke(core, callToken, context, constrainedToken, self, slot)), "unsupported-" + code);
        }
        foreach (string name in new[] { "Decimal", "Int128", "UInt128" })
        {
            var valueHandle = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
                && reader.GetString(reader.GetTypeDefinition(h).Name) == name);
            var value = ((IDictionary)Get(core, "ClassMap"))[valueHandle]!;
            var element = makeClass.Invoke(null, new[] { value })!;
            var vector = instantiate.Invoke(compilation, new object[] { core, (TypeDefinitionHandle)Get(control.Vector, "Handle"), Arguments(element) })!;
            complete.Invoke(compilation, new[] { vector }); var self = makeClass.Invoke(null, new[] { vector })!;
            var contract = instantiate.Invoke(compilation, new object[] { core, interfaceHandle, Arguments(self, element) })!;
            complete.Invoke(compilation, new[] { contract }); var context = Get(contract, "Context");
            var slot = referenceResolver.Invoke(compilation, new[] { core, (object)(MemberReferenceHandle)callToken, context })!;
            Check(Refused(() => Invoke(core, callToken, context, constrainedToken, self, slot)), "unsupported-" + name);
        }
        int BlobStart(BlobHandle blob) { int pos = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob); return pos + (originalImage[pos] < 128 ? 1 : originalImage[pos] < 192 ? 2 : 4); }
        int BodyStart(int rva) { var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData); int pos = section.PointerToRawData + rva - section.VirtualAddress; return pos + ((originalImage[pos] & 3) == 2 ? 1 : (originalImage[pos + 1] >> 4) * 4); }
        void Mutate(string label, Action<byte[]> change)
        {
            Check(ReferenceEquals(Good(), control.Bridge), "raw-good-before");
            var bytes = (byte[])originalImage.Clone(); change(bytes);
            using var pe = new PEReader(new MemoryStream(bytes, writable: false));
            Set(core, "Reader", pe.GetMetadataReader()); Set(core, "PE", pe);
            try { Check(Refused(Good), "raw-" + label); }
            finally { Set(core, "Reader", reader); Set(core, "PE", originalPe); }
            Check(ReferenceEquals(Good(), control.Bridge), "raw-good-after");
        }
        var slotHandle = (MethodDefinitionHandle)Get(control.Slot, "Handle");
        int signature = BlobStart(reader.GetMethodDefinition(slotHandle).Signature);
        foreach (var value in new byte[] { 0x20, 0x10, 0x05 }) Mutate("slot-header-" + value, bytes => bytes[signature] = value);
        Mutate("slot-result", bytes => bytes[signature + 2] = 9);
        var rawCall = reader.GetMemberReference((MemberReferenceHandle)callToken);
        int callParent = BlobStart(reader.GetTypeSpecification((TypeSpecificationHandle)rawCall.Parent).Signature);
        int callParentLength = reader.GetBlobBytes(reader.GetTypeSpecification((TypeSpecificationHandle)rawCall.Parent).Signature).Length;
        Mutate("slot-element", bytes => bytes[callParent + callParentLength - 1] = 0);
        int receiverBlob = BlobStart(reader.GetTypeSpecification((TypeSpecificationHandle)constrainedToken).Signature);
        Mutate("receiver-index", bytes => bytes[receiverBlob + 1] = 1);
        int bridgeStart = BodyStart((int)Get(control.Bridge, "Rva"));
        Mutate("bridge-value", bytes => bytes[bridgeStart + 1] = 32);
        Mutate("bridge-return", bytes => bytes[bridgeStart + 2] = 0);
        int methodIndexSize = Math.Max(reader.GetTableRowCount(TableIndex.MethodDef), reader.GetTableRowCount(TableIndex.MemberRef)) < 32768 ? 2 : 4;
        int typeIndexSize = reader.GetTableRowCount(TableIndex.TypeDef) < 65536 ? 2 : 4;
        int implementationSize = typeIndexSize + methodIndexSize * 2;
        int implementationTable = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodImpl);
        var rows = reader.GetTypeDefinition((TypeDefinitionHandle)Get(control.Vector, "Handle")).GetMethodImplementations().ToArray();
        var selected = rows.Single(h => reader.GetMethodImplementation(h).MethodBody == (MethodDefinitionHandle)Get(control.Bridge, "Handle"));
        int selectedOffset = implementationTable + (MetadataTokens.GetRowNumber(selected) - 1) * implementationSize;
        Mutate("methodimpl-body-kind", bytes => bytes[selectedOffset + typeIndexSize] ^= 1);
        var other = rows.First(h => h != selected);
        int otherOffset = implementationTable + (MetadataTokens.GetRowNumber(other) - 1) * implementationSize;
        Mutate("methodimpl-duplicate", bytes => Array.Copy(bytes, selectedOffset, bytes, otherOffset, implementationSize));
        var competingBody = reader.GetTypeDefinition((TypeDefinitionHandle)Get(control.Vector, "Handle")).GetMethods()
            .Single(h => reader.GetString(reader.GetMethodDefinition(h).Name).EndsWith(".get_ElementCount", StringComparison.Ordinal));
        Mutate("methodimpl-definition-duplicate", bytes =>
        {
            int body = MetadataTokens.GetRowNumber(competingBody) << 1;
            int declaration = MetadataTokens.GetRowNumber(slotHandle) << 1;
            for (int index = 0; index < methodIndexSize; index++)
            {
                bytes[otherOffset + typeIndexSize + index] = (byte)(body >> (index * 8));
                bytes[otherOffset + typeIndexSize + methodIndexSize + index] = (byte)(declaration >> (index * 8));
            }
        });
        int typeOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.TypeDef)
            + (MetadataTokens.GetRowNumber((TypeDefinitionHandle)Get(control.Vector, "Handle")) - 1) * reader.GetTableRowSize(TableIndex.TypeDef);
        Mutate("owner-flags", bytes => bytes[typeOffset] ^= 0x20);
        var throwType = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "ThrowHelper");
        var throwHandle = reader.GetTypeDefinition(throwType).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "ThrowAccessViolationException");
        int helperTypeOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.TypeDef)
            + (MetadataTokens.GetRowNumber(throwType) - 1) * reader.GetTableRowSize(TableIndex.TypeDef);
        Mutate("helper-owner-flags", bytes => bytes[helperTypeOffset] ^= 0x20);
        int helperDefinitionOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
            + (MetadataTokens.GetRowNumber(throwHandle) - 1) * reader.GetTableRowSize(TableIndex.MethodDef);
        Mutate("helper-method-flags", bytes => bytes[helperDefinitionOffset + 6] ^= 4);
        Mutate("helper-method-impl", bytes => bytes[helperDefinitionOffset + 4] ^= 3);
        int throwStart = BodyStart(reader.GetMethodDefinition(throwHandle).RelativeVirtualAddress);
        Mutate("throw-newobj", bytes => bytes[throwStart] = 0x28);
        Mutate("throw-target", bytes => bytes[throwStart + 1]++);
        Mutate("throw-opcode", bytes => bytes[throwStart + 5] = 0x2a);
        foreach (var item in cases.Where(c => c.Width == 0))
        {
            int start = BodyStart((int)Get(item.Bridge, "Rva"));
            var bytes = (byte[])originalImage.Clone(); bytes[start + 1]++;
            using var pe = new PEReader(new MemoryStream(bytes, writable: false)); Set(core, "PE", pe);
            try { Check(Refused(() => Invoke(core, callToken, item.Context, constrainedToken, item.Self, item.Slot)), "numerics-dependency"); }
            finally { Set(core, "PE", originalPe); }
        }
        var numerics = cases.First(c => c.Width == 0);
        var countHandle = reader.GetTypeDefinition((TypeDefinitionHandle)Get(numerics.Vector, "Handle")).GetMethods()
            .Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "get_Count");
        int countOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
            + (MetadataTokens.GetRowNumber(countHandle) - 1) * reader.GetTableRowSize(TableIndex.MethodDef);
        var changedCount = (byte[])originalImage.Clone(); changedCount[countOffset + 6] ^= 4;
        using (var pe = new PEReader(new MemoryStream(changedCount, writable: false)))
        {
            Check(ReferenceEquals(Invoke(core, callToken, numerics.Context, constrainedToken, numerics.Self, numerics.Slot), numerics.Bridge), "count-good-before");
            Set(core, "Reader", pe.GetMetadataReader());
            try { Check(Refused(() => Invoke(core, callToken, numerics.Context, constrainedToken, numerics.Self, numerics.Slot)), "count-target-flags"); }
            finally { Set(core, "Reader", reader); }
            Check(ReferenceEquals(Invoke(core, callToken, numerics.Context, constrainedToken, numerics.Self, numerics.Slot), numerics.Bridge), "count-good-after");
        }
        validateBody.Invoke(compilation, new[] { control.Bridge });
        Console.WriteLine("vector-alignment-dim-guards:checks=" + checks);
        return true;
    }

    private static void Build(string output, string corePath, string? entryPoint = null)
    {
        if (File.Exists(output)) throw new InvalidOperationException("guard output must be new");
        using var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath);
        using var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("AlignmentDimGuards", new Version(1, 0)), "AlignmentDimGuards", Mono.Cecil.ModuleKind.Dll);
        var module = app.MainModule;
        var host = new Mono.Cecil.TypeDefinition("Fixture", "Cases", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
        foreach (string mutation in Mutations)
        {
            var vector = new Mono.Cecil.GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.Vector128`1")));
            vector.GenericArguments.Add(mutation == "owner-self" ? module.TypeSystem.UInt32 : module.TypeSystem.Int32);
            var contract = new Mono.Cecil.GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2")));
            contract.GenericArguments.Add(vector); contract.GenericArguments.Add(mutation == "owner-element" ? module.TypeSystem.UInt32 : module.TypeSystem.Int32);
            Mono.Cecil.MethodReference target = new("get_Alignment", module.TypeSystem.Int32, contract);
            Mono.Cecil.TypeReference receiver = vector;
            switch (mutation)
            {
                case "instance": target.HasThis = true; break;
                case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                case "return": target.ReturnType = module.TypeSystem.UInt32; break;
                case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                case "byref": target.ReturnType = new Mono.Cecil.ByReferenceType(target.ReturnType); break;
                case "modifier": target.ReturnType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                case "owner-modifier": target.DeclaringType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, contract); break;
                case "receiver-modifier": receiver = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, receiver); break;
                case "methodspec": var spec = new Mono.Cecil.GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); target = spec; break;
            }
            var method = new Mono.Cecil.MethodDefinition(mutation, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method);
            var il = method.Body.GetILProcessor(); il.Emit(OpCodes.Constrained, receiver); il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        }
        foreach (string mutation in new[] { "valid", "instance", "explicit-this", "vararg", "generic", "return", "parameter", "byref", "modifier", "owner", "owner-modifier", "methodspec" })
        {
            Mono.Cecil.MethodReference target = new("ThrowAccessViolationException", module.TypeSystem.Void,
                module.ImportReference(core.MainModule.GetType("System.ThrowHelper")));
            switch (mutation)
            {
                case "instance": target.HasThis = true; break;
                case "explicit-this": target.HasThis = true; target.ExplicitThis = true; break;
                case "vararg": target.CallingConvention = Mono.Cecil.MethodCallingConvention.VarArg; break;
                case "generic": target.GenericParameters.Add(new Mono.Cecil.GenericParameter("T", target)); break;
                case "return": target.ReturnType = module.TypeSystem.Int32; break;
                case "parameter": target.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32)); break;
                case "byref": target.ReturnType = new Mono.Cecil.ByReferenceType(target.ReturnType); break;
                case "modifier": target.ReturnType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                case "owner": var owner = new Mono.Cecil.GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(module.TypeSystem.Int32); target.DeclaringType = owner; break;
                case "owner-modifier": target.DeclaringType = new Mono.Cecil.OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
                case "methodspec": var spec = new Mono.Cecil.GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); target = spec; break;
            }
            var method = new Mono.Cecil.MethodDefinition("helper-" + mutation, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method);
            var il = method.Body.GetILProcessor(); il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Ret);
        }
        if (entryPoint is not null)
        {
            module.Kind = Mono.Cecil.ModuleKind.Console;
            app.EntryPoint = host.Methods.Single(m => m.Name == entryPoint);
        }
        app.Write(output, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }
}
