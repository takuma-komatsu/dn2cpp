using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfITokenizeFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-tokenize-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-tokenize", "--dtfi-tokenize-reflection", "--dtfi-tokenize-minimal", "--dtfi-tokenize-noalloc", "--dtfi-tokenize-oracle" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(Path.GetFullPath(args[1]), new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfITokenizeSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] == "--dtfi-tokenize-oracle")
        {
            module.EntryPoint = Method("RunPublicOracle");
            app.Write(Path.GetFullPath(args[1]));
            return true;
        }
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "Tokenize");
        if (definition.MetadataToken.ToInt32() != 0x0600345f || (int)definition.Attributes != 0x83 || definition.ImplAttributes != 0
            || definition.RVA == 0 || definition.HasGenericParameters || definition.ReturnType.MetadataType != MetadataType.Boolean
            || definition.Parameters.Count != 4 || definition.Parameters[0].ParameterType.FullName != "System.TokenType"
            || definition.Parameters[1].ParameterType.FullName != "System.TokenType&"
            || definition.Parameters[2].ParameterType.FullName != "System.Int32&"
            || definition.Parameters[3].ParameterType.FullName != "System.__DTString&")
            throw new InvalidOperationException("installed Tokenize declaration changed");
        var token = module.ImportReference(core.MainModule.GetType("System.TokenType"));
        var cursorDefinition = core.MainModule.GetType("System.__DTString");
        var cursor = module.ImportReference(cursorDefinition);
        var placeholderToken = program.NestedTypes.Single(t => t.Name == "Token");
        var placeholderCursor = program.NestedTypes.Single(t => t.Name == "Cursor");
        TypeReference Map(TypeReference t)
        {
            if (t.FullName == placeholderToken.FullName) return token;
            if (t.FullName == placeholderCursor.FullName) return cursor;
            if (t is ByReferenceType b) return new ByReferenceType(Map(b.ElementType));
            if (t is PointerType p) return new PointerType(Map(p.ElementType));
            if (t is ArrayType a) return new ArrayType(Map(a.ElementType), a.Rank);
            if (t is GenericInstanceType g)
            {
                var result = new GenericInstanceType(Map(g.ElementType));
                foreach (var argument in g.GenericArguments) result.GenericArguments.Add(Map(argument));
                return result;
            }
            return t;
        }
        MethodReference MapMethod(MethodReference method)
        {
            if (method is GenericInstanceMethod generic)
            {
                var result = new GenericInstanceMethod(MapMethod(generic.ElementMethod));
                foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(Map(argument));
                return result;
            }
            if (method is MethodDefinition) return method;
            method.ReturnType = Map(method.ReturnType);
            foreach (var parameter in method.Parameters) parameter.ParameterType = Map(parameter.ParameterType);
            return method;
        }
        IEnumerable<TypeDefinition> Types(TypeDefinition t)
        {
            yield return t;
            foreach (var nested in t.NestedTypes)
                foreach (var descendant in Types(nested)) yield return descendant;
        }
        foreach (var type in Types(program).Where(t => t != placeholderToken && t != placeholderCursor))
        foreach (var method in type.Methods)
        {
            method.ReturnType = Map(method.ReturnType);
            foreach (var parameter in method.Parameters) parameter.ParameterType = Map(parameter.ParameterType);
            if (!method.HasBody) continue;
            foreach (var local in method.Body.Variables) local.VariableType = Map(local.VariableType);
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is TypeReference t) instruction.Operand = Map(t);
                else if (instruction.Operand is MethodReference m) instruction.Operand = MapMethod(m);
                else if (instruction.Operand is FieldReference f && f.DeclaringType.FullName == placeholderCursor.FullName)
                {
                    var actual = cursorDefinition.Fields.Single(field => field.Name == f.Name);
                    if (actual.FieldType.FullName != f.FieldType.FullName) throw new InvalidOperationException("cursor field shape changed");
                    instruction.Operand = module.ImportReference(actual);
                }
            }
        }
        var target = module.ImportReference(definition);
        foreach (string route in new[] { "Direct", "Virtual", "Pointer" })
        {
            var method = Method(route); method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            for (int i = 0; i < 5; i++) il.Emit(OpCodes.Ldarg, method.Parameters[i]);
            if (route == "Pointer")
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(target.ReturnType);
                site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }
        var address = Method("Address"); address.Body = new MethodBody(address);
        var ai = address.Body.GetILProcessor();
        ai.Emit(OpCodes.Ldarg_0); ai.Emit(OpCodes.Ldftn, target);
        ai.Emit(OpCodes.Newobj, program.NestedTypes.Single(t => t.Name == "ClosedQuery").Methods.Single(m => m.IsConstructor));
        ai.Emit(OpCodes.Ret);
        foreach (string producer in new[] { "Producer", "VirtualProducer" })
        {
            var call = Method(producer).Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "Direct");
            call.Operand = target; call.OpCode = producer == "Producer" ? OpCodes.Call : OpCodes.Callvirt;
        }
        RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
        if (negative)
        {
            string mode = args[3]; bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
            if (pointer) mode = mode.Substring(8);
            var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
            foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            switch (mode)
            {
                case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                case "mask-int": bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                case "token-value": bad.Parameters[1].ParameterType = token; break;
                case "value-long": bad.Parameters[2].ParameterType = new ByReferenceType(module.TypeSystem.Int64); break;
                case "parser-value": bad.Parameters[3].ParameterType = cursor; break;
                case "modifier": bad.Parameters[3].ParameterType = new OptionalModifierType(module.TypeSystem.Object, bad.Parameters[3].ParameterType); break;
                case "static": bad.HasThis = false; break;
                case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                case "arity": bad.Parameters.RemoveAt(3); break;
                default: throw new InvalidOperationException("unknown Tokenize negative");
            }
            Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
        }
        module.EntryPoint = Method(args[0] switch {
            "--dtfi-tokenize-reflection" => "RunReflectionChecks",
            "--dtfi-tokenize-minimal" or "--dtfi-tokenize-noalloc" => "RunMinimal",
            _ => "Run" });
        app.Write(Path.GetFullPath(args[1]), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
