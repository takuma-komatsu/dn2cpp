using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeTCharFixture
{
    public static bool Run(string[] args)
    {
        bool discard = args.Length == 4 && args[0] == "--datetime-tchar-discard";
        bool negative = args.Length == 4 && args[0] == "--datetime-tchar-negative";
        bool noalloc = args.Length == 4 && args[0] == "--datetime-tchar-noalloc";
        if (!discard && !negative && !noalloc && (args.Length != 3 || args[0] != "--datetime-tchar")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var target = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(method => method.Name == "DateSeparatorTChar");
        if ((int)target.Attributes != 0x83 || target.ImplAttributes != 0 || target.RVA == 0
            || target.GenericParameters.Count != 1 || target.Parameters.Count != 0
            || target.ReturnType is not GenericInstanceType result || result.ElementType.FullName != "System.ReadOnlySpan`1"
            || result.GenericArguments.Count != 1 || result.GenericArguments[0] is not GenericParameter { Position: 0 })
            throw new InvalidOperationException("DateSeparatorTChar installed signature changed");
        string path = Path.GetFullPath(args[1]);
        if (discard)
        {
            if (args[3] is not ("char" or "byte")) throw new InvalidOperationException("unknown discard width");
            using var tiny = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DateTimeTChar", new Version(1, 0)), "DateTimeTChar", ModuleKind.Console);
            var tm = tiny.MainModule;
            var type = new TypeDefinition("DateTimeTChar", "Discard", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                tm.ImportReference(core.MainModule.GetType("System.Object")));
            tm.Types.Add(type);
            var main = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Public, tm.TypeSystem.Void);
            type.Methods.Add(main);
            var il = main.Body.GetILProcessor();
            var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
            var invariant = tm.ImportReference(culture.Methods.Single(method => method.Name == "get_InvariantCulture"));
            foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            {
                il.Emit(OpCodes.Call, invariant);
                il.Emit(OpCodes.Call, tm.ImportReference(culture.Methods.Single(method => method.Name == setter)));
            }
            il.Emit(OpCodes.Call, tm.ImportReference(target.DeclaringType.Methods.Single(method => method.Name == "get_InvariantInfo")));
            var spec = new GenericInstanceMethod(tm.ImportReference(target));
            spec.GenericArguments.Add(args[3] == "char" ? tm.TypeSystem.Char : tm.TypeSystem.Byte);
            il.Emit(OpCodes.Call, spec);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldstr, "datetime-tchar-discard-" + args[3] + ":True");
            il.Emit(OpCodes.Call, tm.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!));
            il.Emit(OpCodes.Ret);
            tm.EntryPoint = main;
            tiny.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            return true;
        }
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DateTimeTChar.Program");
        foreach (string name in new[] { "CharDirect", "ByteDirect", "CharPointer", "BytePointer" })
        {
            var method = program.Methods.Single(value => value.Name == name);
            var instance = new GenericInstanceMethod(module.ImportReference(target));
            instance.GenericArguments.Add(name.StartsWith("Char", StringComparison.Ordinal) ? module.TypeSystem.Char : module.TypeSystem.Byte);
            method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            if (name.EndsWith("Pointer", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, instance);
                var site = new CallSite(method.ReturnType);
                site.Parameters.Add(new ParameterDefinition(module.ImportReference(target.DeclaringType)));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, instance);
            il.Emit(OpCodes.Ret);
        }
        if (noalloc)
        {
            if (args[3] is not ("char" or "byte")) throw new InvalidOperationException("unknown NoAlloc width");
            var selected = program.Methods.Single(method => method.Name == (args[3] == "char" ? "CharDirect" : "ByteDirect"));
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, selected);
            var main = new MethodDefinition("NoAllocProbe", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            program.Methods.Add(main);
            var il = main.Body.GetILProcessor();
            var culture = core.MainModule.GetType("System.Globalization.CultureInfo");
            foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            {
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == "get_InvariantCulture")));
                il.Emit(OpCodes.Call, module.ImportReference(culture.Methods.Single(method => method.Name == setter)));
            }
            il.Emit(OpCodes.Call, module.ImportReference(target.DeclaringType.Methods.Single(method => method.Name == "get_InvariantInfo")));
            il.Emit(OpCodes.Call, selected);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldstr, "datetime-tchar-noalloc-" + args[3] + ":True");
            il.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!));
            il.Emit(OpCodes.Ret);
            module.EntryPoint = main;
        }
        if (negative)
        {
            bool address = args[3].StartsWith("address-", StringComparison.Ordinal);
            string mode = address ? args[3].Substring(8) : args[3];
            var method = program.Methods.Single(value => value.Name == (address ? "CharPointer" : "CharDirect"));
            var bad = new MethodReference(target.Name, module.TypeSystem.Void, module.ImportReference(target.DeclaringType)) { HasThis = true };
            bad.GenericParameters.Add(new GenericParameter("TChar", bad));
            TypeReference template = module.ImportReference(((GenericInstanceType)target.ReturnType).ElementType);
            switch (mode)
            {
                case "span-class": template = new TypeReference("System", "ReadOnlySpan`1", module, template.Scope, false); break;
                case "return-owner": template = module.ImportReference(core.MainModule.GetType("System.Span`1")); break;
                case "static": bad.HasThis = false; break;
                case "generic-arity": bad.GenericParameters.Add(new GenericParameter("Extra", bad)); break;
                case "unsupported-int": break;
                default: throw new InvalidOperationException("unknown TChar negative");
            }
            var returnType = new GenericInstanceType(template);
            returnType.GenericArguments.Add(bad.GenericParameters[0]);
            bad.ReturnType = returnType;
            var specification = new GenericInstanceMethod(mode == "unsupported-int" ? module.ImportReference(target) : bad);
            specification.GenericArguments.Add(mode == "unsupported-int" ? module.TypeSystem.Int32 : module.TypeSystem.Char);
            method.Body.Instructions.Single(instruction => instruction.OpCode == (address ? OpCodes.Ldftn : OpCodes.Call)).Operand = specification;
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
