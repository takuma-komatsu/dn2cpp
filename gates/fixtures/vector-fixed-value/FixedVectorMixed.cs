using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using CType = Mono.Cecil.TypeDefinition;
using CMethod = Mono.Cecil.MethodDefinition;

internal static class FixedVectorMixed
{
    public static void Generate(string corePath, string output)
    {
        if (File.Exists(output)) throw new InvalidOperationException("Refusing to replace fixture: " + output);
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("FixedVectorMixedInput", new Version(1, 0)), "FixedVectorMixedInput", ModuleKind.Console);
        var module = app.MainModule;
        var vector = module.ImportReference(core.MainModule.GetType(FixedVectorValueInputs.OwnerName));
        var valueType = module.ImportReference(core.MainModule.GetType("System.ValueType"));
        var enumType = new CType("Mixed", "EnumElement", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Enum")));
        enumType.Fields.Add(new Mono.Cecil.FieldDefinition("value__", Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.SpecialName | Mono.Cecil.FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
        module.Types.Add(enumType);
        var structType = new CType("Mixed", "StructElement", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout, valueType);
        structType.Fields.Add(new Mono.Cecil.FieldDefinition("Value", Mono.Cecil.FieldAttributes.Public, module.TypeSystem.Int32)); module.Types.Add(structType);
        GenericInstanceType Close(Mono.Cecil.TypeReference definition, Mono.Cecil.TypeReference element)
        { var result = new GenericInstanceType(definition); result.GenericArguments.Add(element); return result; }
        var wrapper = new CType("Mixed", "Wrapper`1", Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.SequentialLayout, valueType);
        var parameter = new Mono.Cecil.GenericParameter("T", wrapper); wrapper.GenericParameters.Add(parameter);
        wrapper.Fields.Add(new Mono.Cecil.FieldDefinition("Vector", Mono.Cecil.FieldAttributes.Public, Close(vector, parameter))); module.Types.Add(wrapper);
        var host = new CType("Mixed", "Entry", Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object); module.Types.Add(host);
        var entry = new CMethod("Main", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(entry); app.EntryPoint = entry;
        foreach (string route in new[] { "Direct", "Local", "Field" })
        {
            var method = new CMethod(route, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void);
            var t = new Mono.Cecil.GenericParameter("T", method); method.GenericParameters.Add(t); host.Methods.Add(method);
            var owner = Close(vector, t);
            if (route == "Direct") method.Parameters.Add(new ParameterDefinition(owner));
            method.Body.InitLocals = true; method.Body.Variables.Add(new VariableDefinition(route == "Field" ? Close(wrapper, t) : owner));
            var il = method.Body.GetILProcessor(); il.Emit(OpCodes.Ldtoken, owner); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            foreach (var element in new Mono.Cecil.TypeReference[] { module.TypeSystem.Int32, enumType, structType })
            {
                var closed = Close(vector, element); var wrapped = Close(wrapper, element);
                // Type tokens expose every actual vector/wrapper even when a local-only signature is not retained.
                var main = entry.Body.GetILProcessor(); main.Emit(OpCodes.Ldtoken, closed); main.Emit(OpCodes.Pop); main.Emit(OpCodes.Ldtoken, wrapped); main.Emit(OpCodes.Pop);
                if (route == "Direct") { entry.Body.InitLocals = true; var local = new VariableDefinition(closed); entry.Body.Variables.Add(local); main.Emit(OpCodes.Ldloc, local); }
                var call = new GenericInstanceMethod(method); call.GenericArguments.Add(element); main.Emit(OpCodes.Call, call);
            }
        }
        entry.Body.GetILProcessor().Emit(OpCodes.Ret);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        using var pe = new System.Reflection.PortableExecutable.PEReader(File.OpenRead(output)); var reader = pe.GetMetadataReader();
        if (reader.GetTableRowCount(TableIndex.MethodSpec) != 9) throw new InvalidOperationException("Mixed contextual method inventory changed");
        Console.WriteLine($"fixed-vector-mixed width={FixedVectorValueInputs.Width} routes=direct,local,field elements=int,enum,struct serialized-methodspecs=9");
    }

    public static void Inspect(string compiler, string core, string input, bool require)
    {
        var assembly = Assembly.LoadFrom(Path.GetFullPath(compiler));
        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Type Model(string name) => assembly.GetType("Dn2Cpp." + name, true)!;
        object? Get(object obj, string name) => obj.GetType().GetField(name, members)?.GetValue(obj) ?? obj.GetType().GetProperty(name, members)?.GetValue(obj);
        void Set(object obj, string name, object value) { if (obj.GetType().GetField(name, members) is { } f) f.SetValue(obj, value); else obj.GetType().GetProperty(name, members)!.SetValue(obj, value); }
        object? Call(object? obj, System.Reflection.MethodInfo method, params object[] args)
        { try { return method.Invoke(obj, args); } catch (TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; } }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", Path.GetFullPath(input)); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false); Set(options, "AutoRef", true); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        var compilation = Call(null, Model("Compilation").GetMethod("Create", members)!, new[] { Path.GetFullPath(input), Path.GetFullPath(core) }, options)!;
        var module = ((IEnumerable)Get(compilation, "Modules")!).Cast<object>().Single(m => (string)Get(m, "AssemblyName")! == "FixedVectorMixedInput");
        var reader = (MetadataReader)Get(module, "Reader")!;
        var context = Model("GenericContext").GetField("Empty", members)!.GetValue(null)!;
        var canonical = Activator.CreateInstance(Model("CanonicalGenerics"), compilation)!;
        var canonicalForm = Model("CanonicalGenerics").GetMethod("CanonicalForm", members)!;
        var shape = Model("Compilation").GetMethod("EnsureShape", members)!;
        var resolve = Model("Compilation").GetMethod("ResolveTypeTokenForScan", members)!;
        var numeric = Model("CoreIntrinsics").GetMethod("IsVectorValueClass", members)!;
        int examined = 0, leaks = 0;
        object? numericOwner = null, numericElement = null, enumOwner = null, enumElement = null;
        bool numericIdentity = false, numericWrapperStorage = false;
        var unsupportedOwners = new List<object>();
        for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
        {
            // Skip owner/method-context open tokens, which are proved separately by contextual reference fixtures.
            var bytes = reader.GetBlobBytes(reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(row)).Signature);
            if (bytes.Length < 5 || bytes[^2] is 0x13 or 0x1e) continue;
            var type = Call(compilation, resolve, module, (EntityHandle)MetadataTokens.TypeSpecificationHandle(row), context);
            if (type is null || Get(type, "Class") is not { } owner) continue;
            Call(compilation, shape, owner);
            string name = (string)Get(owner, "FullName")!;
            if (!name.StartsWith(FixedVectorValueInputs.OwnerName.Split('`')[0], StringComparison.Ordinal) && !name.StartsWith("Mixed.Wrapper", StringComparison.Ordinal)) continue;
            var normalized = Call(canonical, canonicalForm, type)!; var normalizedOwner = Get(normalized, "Class")!; Call(compilation, shape, normalizedOwner);
            bool storage = Storage(owner) == Storage(normalizedOwner);
            bool admitted = (bool)Call(null, numeric, owner)!, canonicalAdmitted = (bool)Call(null, numeric, normalizedOwner)!;
            bool safe = storage && (admitted || !canonicalAdmitted); examined++; if (!safe) leaks++;
            var element = ((Array)Get(Get(owner, "Context")!, "TypeArgs")!).GetValue(0)!;
            bool isPrimitiveInt = Get(element, "Kind")!.ToString() == "Primitive" && (PrimitiveTypeCode)Get(element, "Primitive")! == PrimitiveTypeCode.Int32;
            bool isWrapper = name.StartsWith("Mixed.Wrapper", StringComparison.Ordinal);
            if (isPrimitiveInt)
            {
                if (isWrapper) numericWrapperStorage = storage;
                else { numericOwner = owner; numericElement = element; numericIdentity = ReferenceEquals(type, normalized); }
            }
            else if (!isWrapper)
            {
                unsupportedOwners.Add(owner);
                if (Get(element, "Class") is { } elementOwner && (bool)Get(elementOwner, "IsEnum")!)
                { enumOwner = owner; enumElement = element; }
            }
            Console.WriteLine($"fixed-vector-mixed-case owner={name} actual-storage={Storage(owner)} canonical-storage={Storage(normalizedOwner)} actual-numeric={admitted} canonical-numeric={canonicalAdmitted} safe={safe}");
            string Storage(object value)
            {
                if (Get(value, "IntrinsicCppName") is string mapped) return mapped;
                var fields = ((IEnumerable)Get(value, "Fields")!).Cast<object>().ToArray();
                foreach (var field in fields) if ((string)Get(field, "Name")! == "Vector" && Get(Get(field, "Type")!, "Class") is { } fieldOwner)
                { Call(compilation, shape, fieldOwner); return Get(fieldOwner, "IntrinsicCppName") as string ?? "managed-vector-field"; }
                return "managed";
            }
        }
        if (examined < 6) throw new InvalidOperationException("Mixed closed vector/wrapper inventory incomplete");
        if (numericOwner is null || numericElement is null || enumOwner is null || enumElement is null || unsupportedOwners.Count != 2)
            throw new InvalidOperationException("Mixed numeric and unsupported control inventory incomplete");
        var callable = Model("CoreIntrinsics").GetMethod("IsVectorValueCallableClass", members)!;
        bool firstGood = (bool)Call(null, numeric, numericOwner)!;
        bool excluded = !(bool)Call(null, numeric, enumOwner)! && !(bool)Call(null, callable, enumOwner)!;
        foreach (var unsupported in unsupportedOwners)
            excluded &= !(bool)Call(null, numeric, unsupported)! && !(bool)Call(null, callable, unsupported)!;
        bool restoredGood = (bool)Call(null, numeric, numericOwner)!;
        var scalarInteger = Call(canonical, canonicalForm, numericElement)!;
        var scalarEnum = Call(canonical, canonicalForm, enumElement)!;
        bool scalarSharing = ReferenceEquals(scalarInteger, scalarEnum) && (bool)Get(scalarInteger, "IsCanonPlaceholder")!;
        bool controls = firstGood && excluded && restoredGood && numericIdentity && numericWrapperStorage && scalarSharing;
        Console.WriteLine($"fixed-vector-mixed-controls numeric-first={firstGood} unsupported-noncallable={excluded} numeric-restored={restoredGood} numeric-vector-identity={numericIdentity} numeric-wrapper-storage={numericWrapperStorage} scalar-enum-sharing={scalarSharing} passed={controls}");
        Console.WriteLine($"fixed-vector-mixed width={FixedVectorValueInputs.Width} examined={examined} storage-or-admission-leaks={leaks} mode={(require ? "require" : "observe")}");
        if (require && !controls) throw new InvalidOperationException("Mixed exact numeric/noncallable/sharing controls failed");
        if (require && leaks != 0) throw new InvalidOperationException("Unsupported fixed closure crosses numeric representation boundary");
    }
}
