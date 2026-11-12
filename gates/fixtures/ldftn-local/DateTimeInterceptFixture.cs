using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeInterceptFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--datetime-intercepts" or "--datetime-direct-only" or "--datetime-public-only"))
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var dtfi = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
        var datetime = core.MainModule.GetType("System.DateTime");
        if (args[0] == "--datetime-public-only")
        {
            // External value-type locals need layout metadata; this probe tests
            // only the existing transient public-constructor fallback without it.
            var entry = app.EntryPoint;
            // Unreachable generic signatures still need CoreLib decoding. Keep
            // this no-reference input minimal rather than testing that boundary.
            foreach (var type in module.Types.ToArray())
                if (type.Name != "<Module>" && type != entry.DeclaringType)
                    module.Types.Remove(type);
            app.CustomAttributes.Clear();
            module.CustomAttributes.Clear();
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
            foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            {
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == "get_InvariantCulture")));
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == setter)));
            }
            var constructor = datetime.Methods.Single(method => method.IsConstructor && method.IsPublic && !method.IsStatic
                && method.Parameters.Count == 3 && method.Parameters.All(parameter => parameter.ParameterType.MetadataType == MetadataType.Int32));
            foreach (int value in new[] { 2024, 2, 29 }) il.Emit(OpCodes.Ldc_I4, value);
            il.Emit(OpCodes.Newobj, module.ImportReference(constructor));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            using var publicCheck = AssemblyDefinition.ReadAssembly(path);
            var body = publicCheck.EntryPoint.Body;
            var calls = body.Instructions.Where(item => item.Operand is MethodReference).ToArray();
            var target = (MethodReference)calls.Last().Operand;
            if (publicCheck.MainModule.Types.Count != 2 || body.Variables.Count != 0 || calls.Length != 5
                || calls.Take(4).Any(item => item.OpCode != OpCodes.Call
                    || ((MethodReference)item.Operand).DeclaringType.FullName != "System.Globalization.CultureInfo")
                || string.Join(",", calls.Take(4).Select(item => ((MethodReference)item.Operand).Name))
                    != "get_InvariantCulture,set_CurrentCulture,get_InvariantCulture,set_CurrentUICulture"
                || calls.Last().OpCode != OpCodes.Newobj || target.MetadataToken.TokenType != TokenType.MemberRef
                || target.DeclaringType.Scope.Name != "System.Private.CoreLib" || target.FullName != constructor.FullName
                || !target.HasThis || target.HasGenericParameters || target.ReturnType.MetadataType != MetadataType.Void
                || target.Parameters.Count != 3 || target.Parameters.Any(parameter => parameter.ParameterType.MetadataType != MetadataType.Int32))
                throw new InvalidOperationException("datetime public-only fixture lost its transient public constructor MemberRef");
            Console.WriteLine("datetime public-only root=" + target.FullName);
            return true;
        }
        if (args[0] == "--datetime-direct-only")
        {
            // No reflection, delegate, preservation or helper edge may keep these
            // bodies alive: their direct MemberRefs are the entire program.
            var entry = app.EntryPoint;
            entry.Body = new MethodBody(entry) { InitLocals = true };
            var date = new VariableDefinition(module.ImportReference(datetime));
            var onlyType = core.MainModule.GetType("System.DateOnly");
            var only = new VariableDefinition(module.ImportReference(onlyType));
            var part = new VariableDefinition(module.TypeSystem.Int32);
            entry.Body.Variables.Add(date);
            entry.Body.Variables.Add(only);
            entry.Body.Variables.Add(part);
            var il = entry.Body.GetILProcessor();
            var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
            foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            {
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == "get_InvariantCulture")));
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == setter)));
            }
            il.Emit(OpCodes.Ldloca, only);
            for (int i = 0; i < 3; i++) il.Emit(OpCodes.Ldloca, part);
            il.Emit(OpCodes.Call, module.ImportReference(onlyType.Methods.Single(method => method.Name == "Deconstruct")));
            il.Emit(OpCodes.Ldloca, date);
            il.Emit(OpCodes.Ldc_I8, 1L);
            il.Emit(OpCodes.Ldloca, date);
            il.Emit(OpCodes.Call, module.ImportReference(datetime.Methods.Single(method => method.Name == "TryAddTicks")));
            il.Emit(OpCodes.Pop);
            foreach (int value in new[] { 2024, 2, 29, 0, 0, 0, 0 }) il.Emit(OpCodes.Ldc_I4, value);
            il.Emit(OpCodes.Ldloca, date);
            il.Emit(OpCodes.Call, module.ImportReference(datetime.Methods.Single(method => method.Name == "TryCreate")));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newobj, module.ImportReference(datetime.Methods.Single(method => method.Name == ".ctor"
                && method.Parameters.Count == 3 && method.Parameters[0].ParameterType.MetadataType == MetadataType.Int64)));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            using var directCheck = AssemblyDefinition.ReadAssembly(path);
            var targets = directCheck.EntryPoint.Body.Instructions.Where(item => item.Operand is MethodReference)
                .Select(item => (MethodReference)item.Operand).ToArray();
            if (targets.Length != 8 || targets.Any(method => method.DeclaringType.Scope.Name != "System.Private.CoreLib")
                || string.Join(",", targets.Take(4).Select(method => method.Name)) != "get_InvariantCulture,set_CurrentCulture,get_InvariantCulture,set_CurrentUICulture"
                || string.Join(",", targets.Skip(4).Select(method => method.Name)) != "Deconstruct,TryAddTicks,TryCreate,.ctor")
                throw new InvalidOperationException("datetime direct-only fixture contains an unexpected root");
            Console.WriteLine("datetime direct-only roots=Deconstruct,TryAddTicks,TryCreate,.ctor");
            return true;
        }
        var records = new List<string>();

        void Rewrite(string owner, string name, MethodDefinition definition, bool address = false, bool construct = false, bool boundDelegate = false)
        {
            var stub = module.GetType(owner).Methods.Single(method => method.Name == name);
            MethodReference target = module.ImportReference(definition);
            if (stub.HasGenericParameters)
            {
                stub.GenericParameters[0].Attributes = definition.GenericParameters[0].Attributes;
                foreach (var constraint in definition.GenericParameters[0].Constraints)
                {
                    TypeReference type = constraint.ConstraintType;
                    if (type is GenericInstanceType generic)
                    {
                        var instance = new GenericInstanceType(module.ImportReference(generic.ElementType));
                        foreach (var argument in generic.GenericArguments)
                            instance.GenericArguments.Add(argument is GenericParameter parameter
                                ? stub.GenericParameters[parameter.Position] : module.ImportReference(argument));
                        type = instance;
                    }
                    else
                        type = module.ImportReference(type);
                    stub.GenericParameters[0].Constraints.Add(new GenericParameterConstraint(type));
                }
                var closed = new GenericInstanceMethod(target);
                closed.GenericArguments.Add(stub.GenericParameters[0]);
                target = closed;
            }
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            foreach (var parameter in stub.Parameters)
                il.Emit(OpCodes.Ldarg, parameter);
            if (boundDelegate)
            {
                // The CLR's hidden span return buffer follows the receiver on
                // Windows. A closed delegate preserves that instance-call ABI.
                if (!target.HasThis || target.Parameters.Count != 0 || stub.Parameters.Count != 1)
                    throw new InvalidOperationException("span delegate requires a receiver-only instance target");
                il.Emit(OpCodes.Ldftn, target);
                var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                il.Emit(OpCodes.Newobj, constructor);
            }
            else if (address)
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(stub.ReturnType);
                foreach (var parameter in stub.Parameters)
                    site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else
                il.Emit(construct ? OpCodes.Newobj : OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
            records.Add(name + "=" + (boundDelegate ? "ldftn+delegate" : address ? "ldftn+calli" : construct ? "newobj" : "call") + ":" + definition.FullName);
        }

        const string formatOwner = "DateTimeFormatInfoSubset.Program";
        Rewrite(formatOwner, "DayAddress", dtfi.Methods.Single(method => method.Name == "GetAbbreviatedDayName"), address: true);
        foreach (var pair in new[] { ("AmSpan", "AMDesignatorTChar"), ("PmSpan", "PMDesignatorTChar"),
            ("DateSpan", "DateSeparatorTChar"), ("TimeSpan", "TimeSeparatorTChar"), ("DecimalSpan", "DecimalSeparatorTChar") })
            Rewrite(formatOwner, pair.Item1, dtfi.Methods.Single(method => method.Name == pair.Item2));
        Rewrite(formatOwner, "DateSpanDelegate", dtfi.Methods.Single(method => method.Name == "DateSeparatorTChar"), boundDelegate: true);
        Rewrite(formatOwner, "InternalMonth", dtfi.Methods.Single(method => method.Name == "InternalGetMonthName"));
        Rewrite(formatOwner, "Adjust", dtfi.Methods.Single(method => method.Name == "YearMonthAdjustment"));
        const string dateOwner = "DateTimePrivateSubset.Program";
        Rewrite(dateOwner, "Add", datetime.Methods.Single(method => method.Name == "TryAddTicks"));
        Rewrite(dateOwner, "AddAddress", datetime.Methods.Single(method => method.Name == "TryAddTicks"), address: true);
        Rewrite(dateOwner, "Create", datetime.Methods.Single(method => method.Name == "TryCreate"));
        Rewrite(dateOwner, "Local", datetime.Methods.Single(method => method.Name == ".ctor" && method.Parameters.Count == 3
            && method.Parameters[0].ParameterType.MetadataType == MetadataType.Int64), construct: true);
        var validator = core.MainModule.GetType("System.Globalization.CultureInfo").Methods.Single(method => method.Name == "VerifyCultureName"
            && method.Parameters[0].ParameterType.MetadataType == MetadataType.String);
        Rewrite(dateOwner, "VerifyName", validator);
        Rewrite(dateOwner, "VerifyNameAddress", validator, address: true);

        var tokenize = module.GetType(formatOwner).Methods.Single(method => method.Name == "Tokenize");
        var tokenizeTarget = dtfi.Methods.Single(method => method.Name == "Tokenize");
        tokenize.Body = new MethodBody(tokenize) { InitLocals = true };
        var cursor = new VariableDefinition(module.ImportReference(((ByReferenceType)tokenizeTarget.Parameters[3].ParameterType).ElementType));
        tokenize.Body.Variables.Add(cursor);
        var tokenizeIl = tokenize.Body.GetILProcessor();
        tokenizeIl.Emit(OpCodes.Ldarg_0);
        tokenizeIl.Emit(OpCodes.Ldc_I4_0);
        tokenizeIl.Emit(OpCodes.Ldarg_1);
        tokenizeIl.Emit(OpCodes.Ldarg_2);
        tokenizeIl.Emit(OpCodes.Ldloca, cursor);
        tokenizeIl.Emit(OpCodes.Callvirt, module.ImportReference(tokenizeTarget));
        tokenizeIl.Emit(OpCodes.Ret);
        records.Add("Tokenize=callvirt:" + tokenizeTarget.FullName);

        // CoreCLR checks access for this hand-authored IL; native support is still
        // checked against the untouched CoreLib method declarations.
        var bypass = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Attribute")));
        module.Types.Add(bypass);
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        bypass.Methods.Add(ctor);
        var ctorIl = ctor.Body.GetILProcessor();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(method => method.IsConstructor)));
        ctorIl.Emit(OpCodes.Ret);
        var attribute = new CustomAttribute(ctor);
        attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "System.Private.CoreLib"));
        app.CustomAttributes.Add(attribute);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var check = AssemblyDefinition.ReadAssembly(path);
        foreach (string record in records)
        {
            string name = record[..record.IndexOf('=')];
            var stub = check.MainModule.GetType(name is "Add" or "AddAddress" or "Create" or "Local" or "VerifyName" or "VerifyNameAddress" ? dateOwner : formatOwner)
                .Methods.Single(method => method.Name == name);
            var body = stub.Body;
            string operation = record[(record.IndexOf('=') + 1)..record.IndexOf(':')];
            var opcode = operation switch { "ldftn+calli" or "ldftn+delegate" => OpCodes.Ldftn, "newobj" => OpCodes.Newobj,
                "callvirt" => OpCodes.Callvirt, _ => OpCodes.Call };
            var instruction = body.Instructions.Single(item => item.Operand is MethodReference && item.OpCode == opcode);
            var actual = (MethodReference)instruction.Operand;
            if (actual is GenericInstanceMethod specification)
                actual = specification.ElementMethod;
            if (body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Throw)
                || instruction.OpCode != opcode || actual.FullName.Replace("<!!0>", "<TChar>") != record[(record.IndexOf(':') + 1)..]
                || actual.DeclaringType.Scope.Name != "System.Private.CoreLib"
                || body.Instructions.Count(item => item.Operand is MethodReference) != (operation == "ldftn+delegate" ? 2 : 1)
                || operation == "ldftn+calli" && body.Instructions.Count(item => item.OpCode == OpCodes.Calli) != 1)
                throw new InvalidOperationException("datetime fixture did not write " + record + "; got " + instruction.OpCode + ":" + actual.FullName);
            if (operation == "ldftn+delegate")
            {
                var constructor = body.Instructions.Single(item => item.OpCode == OpCodes.Newobj).Operand as MethodReference;
                if (body.Instructions.Any(item => item.OpCode == OpCodes.Calli)
                    || constructor is null || constructor.Name != ".ctor" || !constructor.HasThis || constructor.ExplicitThis
                    || constructor.ReturnType.MetadataType != MetadataType.Void
                    || constructor.DeclaringType.FullName != stub.ReturnType.FullName
                    || constructor.DeclaringType is not GenericInstanceType { GenericArguments.Count: 1 } instance
                    || instance.GenericArguments[0] is not GenericParameter { Type: GenericParameterType.Method, Position: 0 }
                    || constructor.Parameters.Count != 2
                    || constructor.Parameters[0].ParameterType.MetadataType != MetadataType.Object
                    || constructor.Parameters[1].ParameterType.MetadataType != MetadataType.IntPtr)
                    throw new InvalidOperationException("datetime fixture lost its closed span delegate constructor");
            }
            Console.WriteLine(record);
        }
        return true;
    }
}
