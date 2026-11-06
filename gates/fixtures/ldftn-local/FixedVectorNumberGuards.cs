using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;
using CecilMethod = Mono.Cecil.MethodDefinition;
using CecilType = Mono.Cecil.TypeDefinition;

internal static class FixedVectorNumberGuards
{
    internal static bool BuildDependency(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-number-dependency-fixture") return false;
        var mode = args[3].Split(':');
        if (mode.Length != 4 || mode[0] is not ("Max" or "Min" or "GreaterThan" or "LessThan")
            || mode[2] is not ("decimal" or "enum" or "struct" or "refs" or "int128" or "uint128")
            || mode[3] is not ("direct" or "address" or "calli"))
            throw new ArgumentException("expected dependency:width:managed-element:direct/address/calli");
        int width = int.Parse(mode[1], CultureInfo.InvariantCulture);
        if (width is not (64 or 128 or 256 or 512)) throw new ArgumentException("invalid fixed width");
        VectorMagnitudeFixture.Build(args[1], args[2], new[] { (mode[2], mode[3], "valid") }, width, mode[0]);
        Console.WriteLine("vector-number-dependency-fixture:" + args[3]);
        return true;
    }

    internal static void CheckShared(object compilation, object core, string corePath, string operation,
        object[] controls, Action<bool, string> check, bool audit = false)
    {
        var failures = new HashSet<string>(StringComparer.Ordinal);
        if (audit)
        {
            var originalCheck = check;
            check = (result, label) => { if (result) originalCheck(true, label); else failures.Add(label); };
        }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        void Set(object value, string name, object replacement)
        {
            if (value.GetType().GetField(name, flags) is { } field) field.SetValue(value, replacement);
            else value.GetType().GetProperty(name, flags)!.SetValue(value, replacement);
        }
        object Invoke(string name, params object?[] arguments) => compilation.GetType().GetMethod(name, flags)!.Invoke(compilation, arguments)!;
        bool Refused(Func<object> action)
        {
            try { return action() is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
            catch (TargetInvocationException error) when (audit && error.InnerException is not null)
            { failures.Add("non-refusal-exception:" + error.InnerException.GetType().FullName); return false; }
        }
        var empty = compilation.GetType().Assembly.GetType("Dn2Cpp.GenericContext", true)!.GetField("Empty", statics)!.GetValue(null)!;
        var app = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "VectorMagnitude");
        var pe = (PEReader)Get(core, "PE");
        var reader = (MetadataReader)Get(core, "Reader");
        var byReader = (IDictionary)Get(compilation, "_byReader");
        byte[] original = File.ReadAllBytes(corePath);
        using var cecil = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath);
        var facade = cecil.MainModule.GetType("System.Numerics.Vector");
        var vectorDefinition = cecil.MainModule.GetType("System.Numerics.Vector`1");
        var math = cecil.MainModule.GetType("System.Runtime.Intrinsics.VectorMath");
        var contract = cecil.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2");
        string selected = operation == "MaxNumber" ? "Max" : "Min";
        string comparison = operation == "MaxNumber" ? "GreaterThan" : "LessThan";
        CecilMethod Definition(object method) => (CecilMethod)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(method, "Handle")));
        int BlobOffset(BlobHandle blob)
        {
            int at = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return at + (original[at] < 0x80 ? 1 : original[at] < 0xc0 ? 2 : 4);
        }
        int Row(TableIndex table, int row) => pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(table) + (row - 1) * reader.GetTableRowSize(table);
        int stringSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        int blobSize = reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2;
        int methodSize = Math.Max(reader.GetTableRowCount(TableIndex.MethodDef), reader.GetTableRowCount(TableIndex.MemberRef)) < 32768 ? 2 : 4;
        int typeSize = reader.GetTableRowCount(TableIndex.TypeDef) < 65536 ? 2 : 4;
        byte[] Index(int value, int size) => BitConverter.GetBytes(value).Take(size).ToArray();
        var mutations = new List<Mutation>();
        void Parent(MemberReferenceHandle member, string label)
        {
            var reference = reader.GetMemberReference(member);
            byte[] signature = reader.GetBlobBytes(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            var variants = new List<(string Name, byte[] Bytes)>();
            var value = (byte[])signature.Clone(); value[1] = 0x11; variants.Add(("value-contract", value));
            var variable = (byte[])signature.Clone(); variable[^2] = variable[^2] == 0x13 ? (byte)0x1e : (byte)0x13; variants.Add(("wrong-variable", variable));
            variants.Add(("trailing", signature.Concat(new byte[] { 0 }).ToArray()));
            variants.Add(("modopt", new byte[] { 0x20, 4 }.Concat(signature).ToArray()));
            variants.Add(("modreq", new byte[] { 0x1f, 4 }.Concat(signature).ToArray()));
            foreach (var variant in variants)
            {
                var spare = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.TypeSpec)).Select(MetadataTokens.TypeSpecificationHandle)
                    .First(h => { int n = reader.GetBlobBytes(reader.GetTypeSpecification(h).Signature).Length; return n >= variant.Bytes.Length + 8 && n < 128; });
                int at = BlobOffset(reader.GetTypeSpecification(spare).Signature);
                int memberSize = reader.GetTableRowSize(TableIndex.MemberRef) - stringSize - blobSize;
                mutations.Add(new Mutation(label + ":isolated-parent:" + variant.Name, new[]
                {
                    (Row(TableIndex.MemberRef, MetadataTokens.GetRowNumber(member)), Index((MetadataTokens.GetRowNumber(spare) << 3) | 4, memberSize)),
                    (at - 1, new[] { (byte)variant.Bytes.Length }), (at, variant.Bytes),
                }));
            }
        }
        var vectorHandle = (TypeDefinitionHandle)MetadataTokens.EntityHandle(vectorDefinition.MetadataToken.ToInt32());
        foreach (string name in new[] { selected, comparison })
        {
            var bridge = vectorDefinition.Methods.Single(m => m.Name.EndsWith("." + name, StringComparison.Ordinal));
            var implementationHandle = reader.GetTypeDefinition(vectorHandle).GetMethodImplementations().Single(h => reader.GetMethodImplementation(h).MethodBody == MetadataTokens.EntityHandle(bridge.MetadataToken.ToInt32()));
            var implementation = reader.GetMethodImplementation(implementationHandle);
            var other = reader.GetTypeDefinition(vectorHandle).GetMethodImplementations().First(h => h != implementationHandle);
            int row = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(other)) + typeSize + methodSize;
            var slot = contract.Methods.Single(m => m.Name == name);
            var facadeMethod = facade.Methods.Single(m => m.Name == name && m.HasGenericParameters);
            mutations.Add(new Mutation("methodimpl:duplicate-open:" + name, new[] { (row, Index((int)slot.MetadataToken.RID << 1, methodSize)) }));
            mutations.Add(new Mutation("methodimpl:conflicting-name:" + name, new[] { (row, Index((int)facadeMethod.MetadataToken.RID << 1, methodSize)) }));
            mutations.Add(new Mutation("methodimpl:nil-declaration:" + name, new[] { (row, new byte[methodSize]) }));
            Parent((MemberReferenceHandle)implementation.MethodDeclaration, "methodimpl:" + name);
            var mathMethod = math.Methods.Single(m => m.Name == (name == selected ? operation : selected));
            var nonfloating = (Instruction)mathMethod.Body.Instructions[11].Operand;
            var rawCall = (Mono.Cecil.MethodReference)nonfloating.Next!.Next!.Next!.Operand;
            Parent((MemberReferenceHandle)MetadataTokens.EntityHandle(rawCall.MetadataToken.ToInt32()), "math:" + name);
        }
        int rawCases = 0, warmCases = 0;
        foreach (var root in controls)
        {
            var vector = Get(Get(Get(root, "Signature"), "ReturnType"), "Class");
            var context = Get(vector, "Context");
            var privateBridge = vectorDefinition.Methods.Single(m => m.Name.EndsWith("." + operation, StringComparison.Ordinal));
            var rootSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)privateBridge.Body.Instructions[2].Operand).MetadataToken.ToInt32());
            var count = Invoke("ResolveVectorMagnitudeManagedGuard", root);
            var deps = new HashSet<object>();
            object Handle(Mono.Cecil.MethodReference reference, object ctx) => Invoke("ResolveMethodHandle", core, MetadataTokens.EntityHandle(reference.MetadataToken.ToInt32()), ctx, null);
            foreach (var instruction in Definition(root).Body.Instructions.Where(i => i.Operand is Mono.Cecil.MethodReference call && call.Name is "get_IsHardwareAccelerated" or "SkipInit"))
                deps.Add(Handle((Mono.Cecil.MethodReference)instruction.Operand, Get(root, "Context")));
            var helper = Handle((Mono.Cecil.MethodReference)Definition(count).Body.Instructions[0].Operand, Get(count, "Context"));
            var supported = Handle((Mono.Cecil.MethodReference)Definition(helper).Body.Instructions[0].Operand, Get(helper, "Context"));
            deps.Add(supported); deps.Add(count); deps.Add(helper);
            foreach (var instruction in Definition(supported).Body.Instructions.Take(5).Where(i => i.OpCode == OpCodes.Call)) deps.Add(Handle((Mono.Cecil.MethodReference)instruction.Operand, empty));
            var sink = Handle((Mono.Cecil.MethodReference)Definition(helper).Body.Instructions[3].Operand, empty); deps.Add(sink);
            foreach (var instruction in Definition(sink).Body.Instructions.Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Newobj)) deps.Add(Handle((Mono.Cecil.MethodReference)instruction.Operand, empty));
            var publicCurrent = root;
            foreach (string name in new[] { operation, selected })
            {
                var mathCall = (Mono.Cecil.MethodReference)Definition(publicCurrent).Body.Instructions[4].Operand;
                var mathCurrent = Invoke("ResolveMagnitudeMathCall", publicCurrent, mathCall.MetadataToken.ToInt32(), name); deps.Add(mathCurrent);
                var nonfloating = (Instruction)Definition(mathCurrent).Body.Instructions[11].Operand;
                var dispatch = (Mono.Cecil.MethodReference)nonfloating.Next!.Next!.Next!.Operand;
                string next = name == operation ? selected : comparison;
                deps.Add(Handle(dispatch, Get(mathCurrent, "Context")));
                var bridge = Invoke("ResolveNumberBinaryBridge", mathCurrent, dispatch.MetadataToken.ToInt32(), vector, next); deps.Add(bridge);
                publicCurrent = Handle((Mono.Cecil.MethodReference)Definition(bridge).Body.Instructions[2].Operand, Get(bridge, "Context")); deps.Add(publicCurrent);
            }
            foreach (var dependency in deps)
            {
                string label = Definition(dependency).FullName;
                void Poison(object value, string field, object replacement, string probe = "")
                {
                    string detail = field + (probe.Length == 0 ? "" : ":" + probe);
                    var saved = Get(value, field); Set(value, field, replacement);
                    try
                    {
                        check(Refused(() => Invoke("ResolveVectorMagnitudeManagedGuard", root)), "number-shared-warm-guard:" + label + ":" + detail);
                        check(Refused(() => Invoke("TryResolveVectorMagnitudeSpec", core, rootSpec, context)), "number-shared-warm-precache:" + label + ":" + detail);
                        warmCases++;
                    }
                    finally { Set(value, field, saved); }
                    check(ReferenceEquals(Invoke("ResolveVectorMagnitudeManagedGuard", root), count), "number-shared-warm-restored-count:" + label + ":" + detail);
                    check(ReferenceEquals(Invoke("TryResolveVectorMagnitudeSpec", core, rootSpec, context), root), "number-shared-warm-restored-target:" + label + ":" + detail);
                }
                Poison(dependency, "Name", "InvalidNumberSharedDependency");
                Poison(dependency, "Module", app); Poison(dependency, "Handle", default(MethodDefinitionHandle));
                Poison(dependency, "Rva", 1); Poison(dependency, "Attributes", MethodAttributes.Private);
                Poison(dependency, "ImplAttributes", MethodImplAttributes.InternalCall); Poison(dependency, "IsSynthetic", true);
                Poison(dependency, "Signature", Get(ReferenceEquals(dependency, count) ? root : count, "Signature"));
                var ctx = Get(dependency, "Context");
                bool none = ((Array)Get(ctx, "TypeArgs")).Length == 0 && ((Array)Get(ctx, "MethodArgs")).Length == 0;
                Poison(dependency, "Context", none ? Get(root, "Context") : empty);
                if (none) Poison(dependency, "Context", Get(vector, "Context"), "typeargs-only");
                Poison(Get(dependency, "DeclaringClass"), "Module", app);
                Poison(Get(dependency, "DeclaringClass"), "Handle", default(TypeDefinitionHandle));
                foreach (var changed in SignatureMutations(Get(dependency, "Signature")))
                    Poison(dependency, "Signature", changed.Signature, changed.Name);
                foreach (var changed in ContextMutations(compilation, Get(dependency, "Context"), check))
                    Poison(dependency, "Context", changed.Context, changed.Name);
                var owner = Get(dependency, "DeclaringClass");
                if (!ReferenceEquals(owner, vector))
                    foreach (var changed in ContextMutations(compilation, Get(owner, "Context"), check))
                        Poison(owner, "Context", changed.Context, "owner:" + changed.Name);
            }
            foreach (var mutation in mutations)
            {
                var changed = (byte[])original.Clone(); foreach (var patch in mutation.Patches) patch.Bytes.CopyTo(changed, patch.Offset);
                using var modified = new PEReader(new MemoryStream(changed, writable: false));
                var changedReader = modified.GetMetadataReader(); byReader.Add(changedReader, core);
                Set(core, "PE", modified); Set(core, "Reader", changedReader);
                try
                {
                    check(Refused(() => Invoke("ResolveVectorMagnitudeManagedGuard", root)), "number-shared-raw-guard:" + mutation.Name);
                    check(Refused(() => Invoke("TryResolveVectorMagnitudeSpec", core, rootSpec, context)), "number-shared-raw-precache:" + mutation.Name);
                    rawCases++;
                }
                finally { Set(core, "PE", pe); Set(core, "Reader", reader); byReader.Remove(changedReader); }
                check(ReferenceEquals(Invoke("ResolveVectorMagnitudeManagedGuard", root), count), "number-shared-raw-restored-count:" + mutation.Name);
                check(ReferenceEquals(Invoke("TryResolveVectorMagnitudeSpec", core, rootSpec, context), root), "number-shared-raw-restored-target:" + mutation.Name);
            }
        }
        if (failures.Count != 0)
        {
            foreach (string failure in failures.OrderBy(s => s, StringComparer.Ordinal)) Console.Error.WriteLine("number-shared-audit-failure:" + failure);
            throw new InvalidOperationException("Number shared authority audit found " + failures.Count + " distinct failures");
        }
        Console.WriteLine("vector-number-shared-authority:" + operation + ":elements=" + controls.Length + ":raw=" + rawCases + ":warm=" + warmCases);
    }

    private static IEnumerable<(string Name, object Signature)> SignatureMutations(object signature)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        var parameters = ((IEnumerable)Get(signature, "ParameterTypes")).Cast<object>().ToArray();
        var result = Get(signature, "ReturnType");
        var elementType = result.GetType();
        var createArray = typeof(System.Collections.Immutable.ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == "Create" && m.IsGenericMethodDefinition && m.GetParameters() is [var p] && p.ParameterType.IsArray)
            .MakeGenericMethod(elementType);
        var constructor = signature.GetType().GetConstructors().Single();
        for (int position = -1; position < parameters.Length; position++)
        foreach (string change in new[] { "wrong-kind", "canonical" })
        {
            var original = position < 0 ? result : parameters[position];
            var modified = typeof(object).GetMethod("MemberwiseClone", flags)!.Invoke(original, null)!;
            if (change == "canonical") elementType.GetField("IsCanonPlaceholder", flags)!.SetValue(modified, true);
            else
            {
                var kind = elementType.GetField("Kind", flags)!;
                kind.SetValue(modified, Enum.Parse(kind.FieldType, kind.GetValue(original)!.ToString() == "Primitive" ? "Class" : "Primitive"));
            }
            var values = Array.CreateInstance(elementType, parameters.Length);
            for (int i = 0; i < parameters.Length; i++) values.SetValue(i == position ? modified : parameters[i], i);
            var immutable = createArray.Invoke(null, new object[] { values })!;
            var arguments = constructor.GetParameters().Select(p => p.Name switch
            {
                "header" => Get(signature, "Header"), "returnType" => position < 0 ? modified : result,
                "requiredParameterCount" => Get(signature, "RequiredParameterCount"), "genericParameterCount" => Get(signature, "GenericParameterCount"),
                "parameterTypes" => immutable, _ => throw new InvalidOperationException("unknown MethodSignature constructor parameter: " + p.Name),
            }).ToArray();
            yield return ((position < 0 ? "return" : "parameter[" + position + "]") + ":" + change, constructor.Invoke(arguments));
        }
    }

    private static IEnumerable<(string Name, object Context)> ContextMutations(object compilation, object context, Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        object Clone(object value) => typeof(object).GetMethod("MemberwiseClone", flags)!.Invoke(value, null)!;
        string Mangle(object value) => (string)compilation.GetType().GetMethod("MangleArg", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new[] { value })!;
        foreach (string group in new[] { "TypeArgs", "MethodArgs" })
        {
            var original = (Array)Get(context, group);
            for (int i = 0; i < original.Length; i++)
            {
                var argument = original.GetValue(i)!;
                if (Get(argument, "Kind").ToString() != "Class") continue;
                var identity = Get(argument, "Class");
                foreach (string change in new[] { "same-mangle-class", "wrong-kind", "canonical" })
                {
                    var modified = Clone(argument);
                    if (change == "same-mangle-class")
                    {
                        var twin = Clone(identity);
                        check(!ReferenceEquals(twin, identity) && (string)Get(twin, "CppName") == (string)Get(identity, "CppName"), "number-context-distinct-class-same-cpp-name");
                        modified.GetType().GetField("Class", flags)!.SetValue(modified, twin);
                        check(Mangle(argument) == Mangle(modified), "number-context-distinct-class-same-mangle");
                    }
                    else if (change == "wrong-kind")
                        modified.GetType().GetField("Kind", flags)!.SetValue(modified, Enum.Parse(Get(argument, "Kind").GetType(), "Primitive"));
                    else modified.GetType().GetField("IsCanonPlaceholder", flags)!.SetValue(modified, true);
                    var types = (Array)((Array)Get(context, "TypeArgs")).Clone();
                    var methods = (Array)((Array)Get(context, "MethodArgs")).Clone();
                    (group == "TypeArgs" ? types : methods).SetValue(modified, i);
                    var changed = Activator.CreateInstance(context.GetType(), new object[] { types, methods })!;
                    check(ReferenceEquals(Get(argument, "Class"), identity) && Get(argument, "Kind").ToString() == "Class" && !(bool)Get(argument, "IsCanonPlaceholder"), "number-context-original-descriptor-unchanged");
                    yield return (group + "[" + i + "]:" + change, changed);
                }
            }
        }
    }

    private sealed record Mutation(string Name, (int Offset, byte[] Bytes)[] Patches);

    internal static void Check(object compilation, object core, string corePath, string operation,
        int width, object[] controls, Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field
            ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        void Set(object value, string name, object replacement)
        {
            if (value.GetType().GetField(name, flags) is { } field) field.SetValue(value, replacement);
            else value.GetType().GetProperty(name, flags)!.SetValue(value, replacement);
        }
        object Invoke(string name, params object?[] arguments) => compilation.GetType().GetMethod(name, flags)!.Invoke(compilation, arguments)!;
        bool Refused(Func<object> action)
        {
            try { return action() is null; }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
        }
        var assembly = compilation.GetType().Assembly;
        var intrinsics = assembly.GetType("Dn2Cpp.CoreIntrinsics", true)!;
        bool Admitted(object method, string predicate) => (bool)intrinsics.GetMethod(predicate, statics)!.Invoke(null, new[] { method })!;
        var empty = assembly.GetType("Dn2Cpp.GenericContext", true)!.GetField("Empty", statics)!.GetValue(null)!;
        var modules = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().ToArray();
        var app = modules.Single(m => (string)Get(m, "AssemblyName") == "VectorMagnitude");
        var originalPe = (PEReader)Get(core, "PE");
        var reader = (MetadataReader)Get(core, "Reader");
        var byReader = (IDictionary)Get(compilation, "_byReader");
        byte[] original = File.ReadAllBytes(corePath);
        using var cecil = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath);
        var mathOwner = cecil.MainModule.GetType("System.Runtime.Intrinsics.VectorMath");
        var contract = cecil.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2");
        string selection = operation == "MaxNumber" ? "Max" : "Min";
        string comparison = operation == "MaxNumber" ? "GreaterThan" : "LessThan";
        string Opposite(string name) => name switch
        {
            "MaxNumber" => "MinNumber", "MinNumber" => "MaxNumber", "Max" => "Min", "Min" => "Max",
            "GreaterThan" => "LessThan", "LessThan" => "GreaterThan", _ => throw new ArgumentException(name),
        };
        CecilType Facade(int bits) => cecil.MainModule.GetType("System.Runtime.Intrinsics.Vector" + bits);
        CecilType Vector(int bits) => cecil.MainModule.GetType("System.Runtime.Intrinsics.Vector" + bits + "`1");
        CecilMethod Public(int bits, string name) => Facade(bits).Methods.Single(m => m.Name == name && m.HasGenericParameters);
        CecilMethod Definition(object method) => (CecilMethod)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(method, "Handle")));
        object VectorOf(object method) => Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
        int RvaOffset(int rva)
        {
            var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            return section.PointerToRawData + rva - section.VirtualAddress;
        }
        int BodyOffset(CecilMethod method)
        {
            int offset = RvaOffset(method.RVA);
            return offset + ((original[offset] & 3) == 2 ? 1 : (original[offset + 1] >> 4) * 4);
        }
        int BlobOffset(BlobHandle blob)
        {
            int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return offset + (original[offset] < 0x80 ? 1 : original[offset] < 0xc0 ? 2 : 4);
        }
        int Row(TableIndex table, int row) => originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(table)
            + (row - 1) * reader.GetTableRowSize(table);
        int stringSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        int blobSize = reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2;
        int typeSize = reader.GetTableRowCount(TableIndex.TypeDef) < 65536 ? 2 : 4;
        int methodSize = Math.Max(reader.GetTableRowCount(TableIndex.MethodDef), reader.GetTableRowCount(TableIndex.MemberRef)) < 32768 ? 2 : 4;
        byte[] Index(int value, int size) => BitConverter.GetBytes(value).Take(size).ToArray();
        var mutations = new List<Mutation>();
        var unique = new HashSet<string>(StringComparer.Ordinal);
        void Patch(string name, params (int Offset, byte[] Bytes)[] patches)
        {
            check(patches.Any(p => !original.AsSpan(p.Offset, p.Bytes.Length).SequenceEqual(p.Bytes)), "fixed-number-mutation-changes:" + name);
            string key = string.Join(";", patches.Select(p => p.Offset + ":" + Convert.ToHexString(p.Bytes)));
            if (unique.Add(key)) mutations.Add(new Mutation(name, patches));
        }
        void Byte(string name, int offset, int value) => Patch(name, (offset, new[] { (byte)value }));
        void Token(string name, CecilMethod method, Instruction instruction, int token) =>
            Patch(name, (BodyOffset(method) + instruction.Offset + instruction.OpCode.Size, BitConverter.GetBytes(token)));
        void Opcode(string name, CecilMethod method, Instruction instruction, int code) => Byte(name, BodyOffset(method) + instruction.Offset, code);
        void Branch(string name, CecilMethod method, Instruction branch)
        {
            Opcode(name + ":direction", method, branch, branch.OpCode == OpCodes.Brfalse_S ? 0x2d : 0x2c);
            Byte(name + ":target", BodyOffset(method) + branch.Offset + 1, 0);
        }
        void Spec(string name, Mono.Cecil.MethodReference call, int variable)
        {
            var handle = (MethodSpecificationHandle)MetadataTokens.EntityHandle(call.MetadataToken.ToInt32());
            var spec = reader.GetMethodSpecification(handle);
            var bytes = reader.GetBlobBytes(spec.Signature);
            check(bytes.SequenceEqual(new byte[] { 0x0a, 1, (byte)variable, 0 }), "fixed-number-exact-spec:" + name);
            Byte(name + ":raw-variable", BlobOffset(spec.Signature) + 2, variable == 0x13 ? 0x1e : 0x13);
            Byte(name + ":raw-index", BlobOffset(spec.Signature) + 3, 1);
        }
        void SpecTarget(string name, Mono.Cecil.MethodReference call, CecilMethod replacement)
        {
            int row = (int)call.MetadataToken.RID;
            Patch(name, (Row(TableIndex.MethodSpec, row), Index((int)replacement.MetadataToken.RID << 1,
                reader.GetTableRowSize(TableIndex.MethodSpec) - blobSize)));
        }
        void ParentVariants(MemberReferenceHandle member, string label)
        {
            var reference = reader.GetMemberReference(member);
            byte[] signature = reader.GetBlobBytes(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            var variants = new List<(string Name, byte[] Bytes)>();
            var value = (byte[])signature.Clone(); value[1] = 0x11; variants.Add(("value-contract", value));
            variants.Add(("trailing", signature.Concat(new byte[] { 0 }).ToArray()));
            variants.Add(("modopt", new byte[] { 0x20, 4 }.Concat(signature).ToArray()));
            variants.Add(("modreq", new byte[] { 0x1f, 4 }.Concat(signature).ToArray()));
            foreach (var variant in variants)
            {
                // Redirect only this MemberRef. The installed interface and generic constraints retain their original blob.
                var spare = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.TypeSpec)).Select(MetadataTokens.TypeSpecificationHandle)
                    .First(h => { int n = reader.GetBlobBytes(reader.GetTypeSpecification(h).Signature).Length; return n >= variant.Bytes.Length + 8 && n < 128; });
                var blob = reader.GetTypeSpecification(spare).Signature;
                int offset = BlobOffset(blob);
                check(original[offset - 1] < 128, "fixed-number-isolated-parent-single-byte-length");
                int memberSize = reader.GetTableRowSize(TableIndex.MemberRef) - stringSize - blobSize;
                Patch(label + ":isolated-parent:" + variant.Name,
                    (Row(TableIndex.MemberRef, MetadataTokens.GetRowNumber(member)), Index((MetadataTokens.GetRowNumber(spare) << 3) | 4, memberSize)),
                    (offset - 1, new[] { (byte)variant.Bytes.Length }), (offset, variant.Bytes));
            }
        }
        var rawMethods = new HashSet<CecilMethod>();
        void MethodAuthority(CecilMethod method, string label)
        {
            if (!rawMethods.Add(method)) return;
            int row = Row(TableIndex.MethodDef, (int)method.MetadataToken.RID);
            Byte(label + ":raw-static", row + 6, original[row + 6] ^ 0x10);
            Byte(label + ":raw-impl", row + 4, original[row + 4] ^ 0x20);
            Patch(label + ":raw-rva", (row, BitConverter.GetBytes(method.RVA == 0 ? 1 : 0)));
            var definition = reader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.EntityHandle(method.MetadataToken.ToInt32()));
            Byte(label + ":raw-signature", BlobOffset(definition.Signature), original[BlobOffset(definition.Signature)] ^ 0x20);
            var otherName = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == "ToString").Name;
            Patch(label + ":raw-name", (row + 8, Index(MetadataTokens.GetHeapOffset(otherName), stringSize)));
            foreach (var generic in definition.GetGenericParameters())
            {
                int at = Row(TableIndex.GenericParam, MetadataTokens.GetRowNumber(generic));
                Byte(label + ":generic-flags:" + MetadataTokens.GetRowNumber(generic), at + 2, original[at + 2] ^ 1);
            }
        }
        void NoInitializer(CecilType owner)
        {
            var unrelated = owner.Methods.First(m => !rawMethods.Contains(m) && m.Name != ".cctor");
            var name = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == ".cctor").Name;
            Patch("cctor:" + owner.FullName, (Row(TableIndex.MethodDef, (int)unrelated.MetadataToken.RID) + 8, Index(MetadataTokens.GetHeapOffset(name), stringSize)));
        }
        void ExceptionRegion(CecilMethod method)
        {
            int header = RvaOffset(method.RVA);
            check((original[header] & 3) == 3 && !method.Body.HasExceptionHandlers, "fixed-number-fat-no-eh:" + method.FullName);
            int end = (BodyOffset(method) + method.Body.CodeSize + 3) & ~3;
            // A real small catch clause is attached without changing the guarded IL prefix.
            byte[] section = new byte[16]; section[0] = 1; section[1] = 16; section[8] = 1; section[9] = 1; section[11] = 1;
            BitConverter.GetBytes(cecil.MainModule.GetType("System.Exception").MetadataToken.ToInt32()).CopyTo(section, 12);
            Patch("eh:" + method.FullName, (header, new[] { (byte)(original[header] | 8) }), (end, section));
        }

        var roots = new List<(object Method, object Count, MethodSpecificationHandle Spec, object Context, object[] Dependencies)>();
        foreach (var method in controls)
        {
            var vector = VectorOf(method);
            var context = Get(vector, "Context");
            var bridge = Vector(width).Methods.Single(m => m.Name.EndsWith("." + operation, StringComparison.Ordinal));
            byte[] body = originalPe.GetMethodBody(bridge.RVA).GetILBytes()!;
            check(body.Length == 8 && body[0] == 2 && body[1] == 3 && body[2] == 0x28 && body[7] == 0x2a,
                "fixed-number-original-private-forwarder");
            var spec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(body, 3));
            var specification = reader.GetMethodSpecification(spec);
            check(specification.Method == (EntityHandle)(MethodDefinitionHandle)Get(method, "Handle")
                && Convert.ToHexString(reader.GetBlobBytes(specification.Signature)) == "0A011300", "fixed-number-private-owner-var");
            check(ReferenceEquals(Invoke("TryResolveVectorMagnitudeSpec", core, spec, context), method), "fixed-number-private-target");
            var count = Invoke("ResolveVectorMagnitudeManagedGuard", method);
            var leaf = Get(count, "DeclaringClass");
            var leafHandle = (TypeDefinitionHandle)Get(leaf, "Handle");
            check(reader.GetString(reader.GetTypeDefinition(leafHandle).Name) == "Vector64`1"
                && ReferenceEquals(Get(leaf, "Module"), core), "fixed-number-real-vector64-count");
            check(ReferenceEquals(((Array)Get(Get(leaf, "Context"), "TypeArgs")).GetValue(0), ((Array)Get(context, "TypeArgs")).GetValue(0)), "fixed-number-count-same-element");
            check((width == 64) == ReferenceEquals(leaf, vector), "fixed-number-no-outer-count-substitution");
            var dependencies = new HashSet<object>();
            void Visit(object current)
            {
                if (!dependencies.Add(current)) return;
                var definition = Definition(current);
                if (definition.Name is "Max" or "Min" or "GreaterThan" or "LessThan")
                {
                    check(!Admitted(current, "IsVectorMagnitude") && !Admitted(current, "IsVectorMagnitudeManagedGuard"), "fixed-number-proof-only-not-callable:" + definition.Name);
                    var privateBridge = definition.DeclaringType.Module.GetType(definition.DeclaringType.FullName + "`1").Methods.Single(m => m.Name.EndsWith("." + definition.Name, StringComparison.Ordinal));
                    var privateCall = (Mono.Cecil.MethodReference)privateBridge.Body.Instructions[2].Operand;
                    check(Invoke("TryResolveVectorMagnitudeSpec", core, (MethodSpecificationHandle)MetadataTokens.EntityHandle(privateCall.MetadataToken.ToInt32()), Get(VectorOf(current), "Context")) is null,
                        "fixed-number-proof-only-raw-no-selection-admission:" + definition.Name);
                }
                int bits = int.Parse(definition.DeclaringType.Name.Substring(6), CultureInfo.InvariantCulture);
                var il = definition.Body.Instructions;
                bool compare = definition.Name is "GreaterThan" or "LessThan";
                if (bits == 64)
                {
                    var skip = il.Single(i => i.Operand is Mono.Cecil.MethodReference call && call.Name == "SkipInit");
                    dependencies.Add(Invoke("ResolveMethodSpec", core, (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)skip.Operand).MetadataToken.ToInt32()), Get(current, "Context")));
                    return;
                }
                if (!compare)
                {
                    dependencies.Add(Invoke("ResolveMethodHandle", core, MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)il[0].Operand).MetadataToken.ToInt32()), empty, null));
                    var math = Invoke("ResolveMagnitudeMathCall", current, ((Mono.Cecil.MethodReference)il[4].Operand).MetadataToken.ToInt32(), definition.Name);
                    dependencies.Add(math);
                    var mathDefinition = Definition(math);
                    var nonfloating = (Instruction)mathDefinition.Body.Instructions[11].Operand;
                    var call = (Mono.Cecil.MethodReference)nonfloating.Next!.Next!.Next!.Operand;
                    string next = definition.Name == operation ? selection : comparison;
                    var value = VectorOf(current);
                    dependencies.Add(Invoke("ResolveMemberRefMethod", core, (MemberReferenceHandle)MetadataTokens.EntityHandle(call.MetadataToken.ToInt32()), Get(math, "Context")));
                    var resolvedBridge = Invoke("ResolveNumberBinaryBridge", math, call.MetadataToken.ToInt32(), value, next);
                    dependencies.Add(resolvedBridge);
                    var bridgeCall = (Mono.Cecil.MethodReference)Definition(resolvedBridge).Body.Instructions[2].Operand;
                    Visit(Invoke("ResolveNumberBinaryCall", resolvedBridge, bridgeCall.MetadataToken.ToInt32(), value, next, 0x13));
                }
                var start = compare ? il[0] : (Instruction)il[1].Operand;
                var half = (Mono.Cecil.MethodReference)start.Next!.Next!.Next!.Next!.Operand;
                Visit(Invoke("ResolveMethodSpec", core, (MethodSpecificationHandle)MetadataTokens.EntityHandle(half.MetadataToken.ToInt32()), Get(current, "Context")));
            }
            Visit(method);
            dependencies.Add(count);
            var countBody = Definition(count).Body.Instructions;
            var helper = Invoke("TryResolveVectorManagedThrowSpec", core, (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)countBody[0].Operand).MetadataToken.ToInt32()), Get(count, "Context"));
            dependencies.Add(helper);
            check((string)Get(helper, "Name") == "ThrowForUnsupportedIntrinsicsVector64BaseType", "fixed-number-original-helper");
            var helperBody = Definition(helper).Body.Instructions;
            check(helperBody[2].OpCode == OpCodes.Ldc_I4_S && (sbyte)helperBody[2].Operand == 0x43, "fixed-number-original-resource");
            dependencies.Add(Invoke("ResolveMemberRefMethod", core, (MemberReferenceHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)helperBody[0].Operand).MetadataToken.ToInt32()), Get(helper, "Context")));
            var sink = Invoke("ResolveMethodHandle", core, MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)helperBody[3].Operand).MetadataToken.ToInt32()), empty, null);
            dependencies.Add(sink);
            foreach (var instruction in Definition(sink).Body.Instructions.Where(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Newobj))
                dependencies.Add(Invoke("ResolveMethodHandle", core, MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)instruction.Operand).MetadataToken.ToInt32()), empty, null));
            var supported = Vector(64).Methods.Single(m => m.Name == "get_IsSupported");
            foreach (var instruction in supported.Body.Instructions.Take(5).Where(i => i.OpCode == OpCodes.Call))
                dependencies.Add(Invoke("ResolveMethodHandle", core, MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)instruction.Operand).MetadataToken.ToInt32()), empty, null));
            roots.Add((method, count, spec, context, dependencies.ToArray()));
        }
        check(roots.Count == 6, "fixed-number-six-managed-elements");

        for (int bits = width; bits >= 64; bits /= 2)
        {
            foreach (string name in width == 64 ? new[] { operation } : new[] { operation, selection, comparison })
            {
                var method = Public(bits, name);
                string label = bits + ":" + name;
                MethodAuthority(method, label);
                var il = method.Body.Instructions;
                bool compare = name == comparison;
                if (bits == 64)
                {
                    int start = compare ? 0 : 2;
                    if (!compare)
                    {
                        Opcode(label + ":constant-false", method, il[0], 0x17);
                        Opcode(label + ":constant-branch", method, il[1], 0x2d);
                        Byte(label + ":constant-target", BodyOffset(method) + il[1].Offset + 1, 1);
                    }
                    Opcode(label + ":result-address", method, il[start], 0x0e);
                    Opcode(label + ":skip-callvirt", method, il[start + 1], 0x6f);
                    Opcode(label + ":index-zero", method, il[start + 2], 0x17);
                    Opcode(label + ":index-local", method, il[start + 3], 0x0a);
                    Branch(label + ":loop-entry", method, il[start + 4]);
                    var condition = (Instruction)il[start + 4].Operand;
                    Opcode(label + ":condition-index", method, condition, 0x06);
                    Opcode(label + ":count-callvirt", method, condition.Next!, 0x6f);
                    var lane = il.First(i => i.Operand is Mono.Cecil.MethodReference call && call.Name == "GetElementUnsafe");
                    Token(label + ":lane-before-count", method, condition.Next!, ((Mono.Cecil.MethodReference)lane.Operand).MetadataToken.ToInt32());
                    ExceptionRegion(method);
                    continue;
                }
                if (!compare)
                {
                    var getter = Facade(bits).Methods.Single(m => m.Name == "get_IsHardwareAccelerated");
                    MethodAuthority(getter, label + ":hardware");
                    Opcode(label + ":hardware-query", getter, getter.Body.Instructions[0], 0x6f);
                    Opcode(label + ":hardware-ret", getter, getter.Body.Instructions[1], 0);
                    Opcode(label + ":query-callvirt", method, il[0], 0x6f);
                    Branch(label + ":hardware-branch", method, il[1]);
                    Opcode(label + ":hardware-left", method, il[2], 3);
                    Opcode(label + ":hardware-right", method, il[3], 2);
                    Opcode(label + ":hardware-callvirt", method, il[4], 0x6f);
                    var hardwareSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)il[4].Operand).MetadataToken.ToInt32()));
                    Byte(label + ":hardware-element-index", BlobOffset(hardwareSpec.Signature) + reader.GetBlobBytes(hardwareSpec.Signature).Length - 1, 1);
                }
                var startHalf = compare ? il[0] : (Instruction)il[1].Operand;
                var left = startHalf.Next!; var right = left.Next!.Next!; var callHalf = right.Next!;
                check(startHalf.OpCode == OpCodes.Ldarg_0 && left.OpCode == OpCodes.Ldfld
                    && left.Next!.OpCode == OpCodes.Ldarg_1 && right.OpCode == OpCodes.Ldfld && callHalf.OpCode == OpCodes.Call,
                    "fixed-number-lower-prefix:" + label);
                var field = (Mono.Cecil.FieldReference)left.Operand;
                check(field.MetadataToken == ((Mono.Cecil.FieldReference)right.Operand).MetadataToken && field.Name == "_lower", "fixed-number-same-lower:" + label);
                Opcode(label + ":lower-left", method, startHalf, 3);
                Opcode(label + ":lower-right", method, left.Next!, 2);
                Opcode(label + ":lower-callvirt", method, callHalf, 0x6f);
                var upper = (Mono.Cecil.FieldReference)il.First(i => i.Operand is Mono.Cecil.FieldReference f && f.Name == "_upper").Operand;
                Token(label + ":left-upper", method, left, upper.MetadataToken.ToInt32());
                Token(label + ":right-upper", method, right, upper.MetadataToken.ToInt32());
                Patch(label + ":both-upper", (BodyOffset(method) + left.Offset + 1, BitConverter.GetBytes(upper.MetadataToken.ToInt32())),
                    (BodyOffset(method) + right.Offset + 1, BitConverter.GetBytes(upper.MetadataToken.ToInt32())));
                var wrongField = (Mono.Cecil.FieldReference)Public(bits == 128 ? 256 : 128, name).Body.Instructions.First(i => i.Operand is Mono.Cecil.FieldReference f && f.Name == "_lower").Operand;
                Token(label + ":wrong-width-field", method, left, wrongField.MetadataToken.ToInt32());
                Token(label + ":foreign-field", method, left, cecil.MainModule.GetType("System.Decimal").Fields[0].MetadataToken.ToInt32());
                var half = (Mono.Cecil.MethodReference)callHalf.Operand;
                Spec(label + ":half", half, 0x1e);
                SpecTarget(label + ":same-width", half, method);
                if (bits != 512) SpecTarget(label + ":up-width", half, Public(bits * 2, name));
                if (bits != 128) SpecTarget(label + ":skip-width", half, Public(bits / 4, name));
                SpecTarget(label + ":wrong-half-operation", half, Public(bits / 2, Opposite(name)));
                var reference = reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(field.MetadataToken.ToInt32()));
                var fieldParent = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature;
                Byte(label + ":field-parent-var", BlobOffset(fieldParent) + reader.GetBlobBytes(fieldParent).Length - 2, 0x13);
                Byte(label + ":field-value-mvar", BlobOffset(reference.Signature) + reader.GetBlobBytes(reference.Signature).Length - 2, 0x1e);
                var fieldDefinition = Vector(bits).Fields.Single(f => f.Name == "_lower");
                int fieldRow = Row(TableIndex.Field, (int)fieldDefinition.MetadataToken.RID);
                Byte(label + ":field-static", fieldRow, original[fieldRow] ^ 0x10);
            }
        }

        if (width > 64)
        foreach (string name in new[] { operation, selection })
        {
            var method = mathOwner.Methods.Single(m => m.Name == name);
            MethodAuthority(method, "math:" + name);
            var il = method.Body.Instructions;
            for (int i = 0; i < 12; i++)
            {
                var instruction = il[i];
                if (instruction.OpCode == OpCodes.Call) Opcode("math:" + name + ":type-call:" + i, method, instruction, 0x6f);
                else if (instruction.OpCode == OpCodes.Ldtoken)
                    Token("math:" + name + ":type-token:" + i, method, instruction, cecil.MainModule.GetType("System.Int32").MetadataToken.ToInt32());
                else Branch("math:" + name + ":type-branch:" + i, method, instruction);
            }
            var nonfloating = (Instruction)il[11].Operand;
            Opcode("math:" + name + ":left", method, nonfloating, 3);
            Opcode("math:" + name + ":right", method, nonfloating.Next!, 2);
            Token("math:" + name + ":constrained-element", method, nonfloating.Next!.Next!, BitConverter.ToInt32(original, BodyOffset(method) + il[0].Offset + 1));
            var instructionCall = nonfloating.Next!.Next!.Next!;
            var call = (Mono.Cecil.MethodReference)instructionCall.Operand;
            Opcode("math:" + name + ":callvirt", method, instructionCall, 0x6f);
            string next = name == operation ? selection : comparison;
            var opposite = mathOwner.Methods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>()
                .First(m => m.Name == Opposite(next) && m.MetadataToken.TokenType == Mono.Cecil.TokenType.MemberRef);
            Token("math:" + name + ":wrong-dispatch", method, instructionCall, opposite.MetadataToken.ToInt32());
            var conditional = mathOwner.Methods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>()
                .First(m => m.Name == "ConditionalSelect");
            Token("math:" + name + ":select-before-guard", method, instructionCall, conditional.MetadataToken.ToInt32());
            var reference = reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(call.MetadataToken.ToInt32()));
            ParentVariants((MemberReferenceHandle)MetadataTokens.EntityHandle(call.MetadataToken.ToInt32()), "math:" + name);
            var parentBlob = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature;
            int end = BlobOffset(parentBlob) + reader.GetBlobBytes(parentBlob).Length;
            Byte("math:" + name + ":raw-parent-type-self", end - 4, 0x13);
            Byte("math:" + name + ":raw-parent-wrong-self", end - 3, 1);
            Byte("math:" + name + ":raw-parent-type-element", end - 2, 0x13);
            Byte("math:" + name + ":raw-parent-wrong-element", end - 1, 0);
            Byte("math:" + name + ":raw-call-signature", BlobOffset(reference.Signature), 0x20);
            ExceptionRegion(method);
        }
        if (width > 64)
        for (int bits = width; bits > 64; bits /= 2)
        foreach (string name in new[] { selection, comparison })
        {
            var vector = Vector(bits);
            var bridge = vector.Methods.Single(m => m.Name.EndsWith("." + name, StringComparison.Ordinal));
            string label = bits + ":bridge:" + name;
            MethodAuthority(bridge, label);
            check(bridge.Body.Instructions.Count == 4 && originalPe.GetMethodBody(bridge.RVA).GetILBytes()!.Length == 8, "fixed-number-exact-dependency-bridge");
            Opcode(label + ":left", bridge, bridge.Body.Instructions[0], 3);
            Opcode(label + ":right", bridge, bridge.Body.Instructions[1], 2);
            Opcode(label + ":callvirt", bridge, bridge.Body.Instructions[2], 0x6f);
            Opcode(label + ":return", bridge, bridge.Body.Instructions[3], 0);
            var call = (Mono.Cecil.MethodReference)bridge.Body.Instructions[2].Operand;
            Spec(label + ":public-call", call, 0x13);
            SpecTarget(label + ":wrong-operation", call, Public(bits, Opposite(name)));
            SpecTarget(label + ":wrong-owner", call, Public(bits == 128 ? 256 : 128, name));
            var vectorDef = reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(vector.MetadataToken.ToInt32()));
            var implementationHandle = vectorDef.GetMethodImplementations().Single(h => reader.GetMethodImplementation(h).MethodBody == MetadataTokens.EntityHandle(bridge.MetadataToken.ToInt32()));
            var implementation = reader.GetMethodImplementation(implementationHandle);
            int row = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(implementationHandle)) + typeSize;
            var otherHandle = vectorDef.GetMethodImplementations().First(h => h != implementationHandle);
            int otherRow = Row(TableIndex.MethodImpl, MetadataTokens.GetRowNumber(otherHandle)) + typeSize;
            Patch(label + ":nil-body", (row, new byte[methodSize]));
            Patch(label + ":nil-declaration", (row + methodSize, new byte[methodSize]));
            Patch(label + ":duplicate", (otherRow, original.AsSpan(row, methodSize * 2).ToArray()));
            var slot = contract.Methods.Single(m => m.Name == name);
            Patch(label + ":open-methoddef", (row + methodSize, Index((int)slot.MetadataToken.RID << 1, methodSize)));
            Patch(label + ":duplicate-open-methoddef", (otherRow + methodSize, Index((int)slot.MetadataToken.RID << 1, methodSize)));
            Patch(label + ":conflicting-methoddef-name", (otherRow + methodSize, Index((int)Public(bits, name).MetadataToken.RID << 1, methodSize)));
            ParentVariants((MemberReferenceHandle)implementation.MethodDeclaration, label);
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            Byte(label + ":declaration-signature", BlobOffset(declaration.Signature), 0x20);
            var parent = reader.GetTypeSpecification((TypeSpecificationHandle)declaration.Parent).Signature;
            int parentEnd = BlobOffset(parent) + reader.GetBlobBytes(parent).Length;
            Byte(label + ":declaration-vector-mvar", parentEnd - 4, 0x1e);
            Byte(label + ":declaration-element-mvar", parentEnd - 2, 0x1e);
            Byte(label + ":declaration-element-index", parentEnd - 1, 1);
            MethodAuthority(slot, "contract:" + name);
        }

        var root = roots[0];
        var countDefinition = Definition(root.Count);
        var countInstruction = countDefinition.Body.Instructions[0];
        Opcode("count:callvirt", countDefinition, countInstruction, 0x6f);
        var helperSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)countInstruction.Operand).MetadataToken.ToInt32()));
        var helperDefinition = (CecilMethod)cecil.MainModule.LookupToken(MetadataTokens.GetToken(helperSpec.Method));
        var helperInstructions = helperDefinition.Body.Instructions;
        MethodAuthority(countDefinition, "count"); MethodAuthority(helperDefinition, "unsupported-helper");
        Branch("unsupported-helper:branch", helperDefinition, helperInstructions[1]);
        Byte("unsupported-helper:resource", BodyOffset(helperDefinition) + helperInstructions[2].Offset + 1, 0x42);
        Opcode("unsupported-helper:return", helperDefinition, helperInstructions[^1], 0);
        Opcode("unsupported-helper:sink-callvirt", helperDefinition, helperInstructions[3], 0x6f);
        var unsupported = Vector(64).Methods.Single(m => m.Name == "get_IsSupported");
        MethodAuthority(unsupported, "unsupported-getter");
        foreach (var instruction in unsupported.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is Mono.Cecil.TypeDefinition).Take(2))
            Token("unsupported-getter:whitelist:" + instruction.Offset, unsupported, instruction, cecil.MainModule.GetType("System.Decimal").MetadataToken.ToInt32());
        var skipDefinition = root.Dependencies.Select(Definition).Single(m => m.Name == "SkipInit");
        MethodAuthority(skipDefinition, "skip-init");
        Opcode("skip-init:constructor", skipDefinition, skipDefinition.Body.Instructions[0], 0x28);
        Opcode("skip-init:throw", skipDefinition, skipDefinition.Body.Instructions[1], 0x2a);
        Spec("count:helper", (Mono.Cecil.MethodReference)countInstruction.Operand, 0x13);
        var skipCall = (Mono.Cecil.MethodReference)Public(64, operation).Body.Instructions.Single(i => i.Operand is Mono.Cecil.MethodReference call && call.Name == "SkipInit").Operand;
        var skipSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(skipCall.MetadataToken.ToInt32()));
        Byte("skip-init:raw-owner-var", BlobOffset(skipSpec.Signature) + reader.GetBlobBytes(skipSpec.Signature).Length - 2, 0x13);
        var supportedReference = reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)helperInstructions[0].Operand).MetadataToken.ToInt32()));
        var supportedParent = reader.GetTypeSpecification((TypeSpecificationHandle)supportedReference.Parent).Signature;
        Byte("unsupported-getter:raw-owner-var", BlobOffset(supportedParent) + reader.GetBlobBytes(supportedParent).Length - 2, 0x13);
        foreach (var terminal in root.Dependencies.Select(Definition).Where(m => m.DeclaringType.FullName is "System.Type" or "System.NotSupportedException"
            || m.Name is "ThrowNotSupportedException" or "GetResourceString")) MethodAuthority(terminal, "terminal:" + terminal.FullName);
        foreach (int bits in new[] { 64, 128, 256, 512 }.Where(bits => bits <= width)) { NoInitializer(Facade(bits)); NoInitializer(Vector(bits)); }
        NoInitializer(helperDefinition.DeclaringType);
        NoInitializer(skipDefinition.DeclaringType);
        if (width > 64) NoInitializer(mathOwner);

        int rawCases = 0;
        foreach (var mutation in mutations)
        {
            var changed = (byte[])original.Clone();
            foreach (var patch in mutation.Patches) patch.Bytes.CopyTo(changed, patch.Offset);
            using var modified = new PEReader(new MemoryStream(changed, writable: false));
            var changedReader = modified.GetMetadataReader(); byReader.Add(changedReader, core);
            Set(core, "PE", modified); Set(core, "Reader", changedReader);
            try
            {
                foreach (var item in roots)
                {
                    check(Refused(() => Invoke("ResolveVectorMagnitudeManagedGuard", item.Method)), "fixed-number-raw-guard:" + width + ":" + operation + ":" + mutation.Name);
                    check(Refused(() => Invoke("TryResolveVectorMagnitudeSpec", core, item.Spec, item.Context)), "fixed-number-raw-precache:" + width + ":" + operation + ":" + mutation.Name);
                    rawCases++;
                }
            }
            finally { Set(core, "PE", originalPe); Set(core, "Reader", reader); byReader.Remove(changedReader); }
            foreach (var item in roots)
            {
                check(ReferenceEquals(Invoke("TryResolveVectorMagnitudeSpec", core, item.Spec, item.Context), item.Method), "fixed-number-raw-restored-target:" + mutation.Name);
                check(ReferenceEquals(Invoke("ResolveVectorMagnitudeManagedGuard", item.Method), item.Count), "fixed-number-raw-restored-count:" + mutation.Name);
            }
        }
        int warmCases = 0;
        foreach (var item in roots)
        foreach (var dependency in item.Dependencies)
        {
            string label = Definition(dependency).FullName;
            void Poison(object value, string field, object replacement, string probe = "")
            {
                string detail = field + (probe.Length == 0 ? "" : ":" + probe);
                    var saved = Get(value, field); Set(value, field, replacement);
                try
                {
                    check(Refused(() => Invoke("ResolveVectorMagnitudeManagedGuard", item.Method)), "fixed-number-warm-guard:" + label + ":" + detail);
                    check(Refused(() => Invoke("TryResolveVectorMagnitudeSpec", core, item.Spec, item.Context)), "fixed-number-warm-precache:" + label + ":" + detail);
                    warmCases++;
                }
                finally { Set(value, field, saved); }
                check(ReferenceEquals(Invoke("TryResolveVectorMagnitudeSpec", core, item.Spec, item.Context), item.Method), "fixed-number-warm-restored-target:" + label + ":" + detail);
                check(ReferenceEquals(Invoke("ResolveVectorMagnitudeManagedGuard", item.Method), item.Count), "fixed-number-warm-restored-count:" + label + ":" + detail);
            }
            Poison(dependency, "Name", "InvalidNumberDependency");
            Poison(dependency, "Module", app);
            Poison(dependency, "Handle", default(MethodDefinitionHandle));
            Poison(dependency, "Rva", 1);
            Poison(dependency, "Attributes", MethodAttributes.Private);
            Poison(dependency, "ImplAttributes", MethodImplAttributes.InternalCall);
            Poison(dependency, "IsSynthetic", true);
            Poison(dependency, "DeclaringClass", ReferenceEquals(Get(dependency, "DeclaringClass"), Get(item.Count, "DeclaringClass")) ? Get(item.Method, "DeclaringClass") : Get(item.Count, "DeclaringClass"));
            Poison(dependency, "Signature", Get(ReferenceEquals(dependency, item.Count) ? item.Method : item.Count, "Signature"));
            var context = Get(dependency, "Context");
            bool noContext = ((Array)Get(context, "TypeArgs")).Length == 0 && ((Array)Get(context, "MethodArgs")).Length == 0;
            Poison(dependency, "Context", noContext ? Get(item.Method, "Context") : empty);
            if (noContext) Poison(dependency, "Context", item.Context, "typeargs-only");
            Poison(Get(dependency, "DeclaringClass"), "Module", app);
            Poison(Get(dependency, "DeclaringClass"), "Handle", default(TypeDefinitionHandle));
            foreach (var changed in SignatureMutations(Get(dependency, "Signature")))
                Poison(dependency, "Signature", changed.Signature, changed.Name);
            if (!ReferenceEquals(dependency, item.Method))
            {
                foreach (var changed in ContextMutations(compilation, Get(dependency, "Context"), check))
                    Poison(dependency, "Context", changed.Context, changed.Name);
                var owner = Get(dependency, "DeclaringClass");
                if (!ReferenceEquals(owner, VectorOf(item.Method)))
                    foreach (var changed in ContextMutations(compilation, Get(owner, "Context"), check))
                        Poison(owner, "Context", changed.Context, "owner:" + changed.Name);
            }
        }
        Console.WriteLine("vector-number-fixed-authority:" + operation + ":width=" + width + ":elements=" + roots.Count
            + ":raw=" + rawCases + ":warm=" + warmCases);
    }
}
