using Mono.Cecil;

internal static class DecimalDivModFixture
{
    public static bool Run(string[] args)
    {
        if ((args.Length != 3 && args.Length != 4) || args[0] != "--decimal-divmod") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var target = core.MainModule.Types.Single(t => t.FullName == "System.Decimal").Methods.Single(m => m.Name == "DecDivMod1E9");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var imported = app.MainModule.ImportReference(target);
        string mode = args.Length == 4 ? args[3] : "valid";
        switch (mode)
        {
            case "instance": imported.HasThis = true; break;
            case "vararg": imported.CallingConvention = MethodCallingConvention.VarArg; break;
            case "return": imported.ReturnType = app.MainModule.TypeSystem.Int32; break;
            case "byvalue": imported.Parameters[0].ParameterType = ((ByReferenceType)imported.Parameters[0].ParameterType).ElementType; break;
            case "pointer": imported.Parameters[0].ParameterType = new PointerType(((ByReferenceType)imported.Parameters[0].ParameterType).ElementType); break;
            case "generic": imported.GenericParameters.Add(new GenericParameter("T", imported)); break;
            case "foreignref":
                var foreign = new TypeDefinition("System", "Decimal", TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed,
                    new TypeReference("System", "ValueType", app.MainModule, app.MainModule.TypeSystem.CoreLibrary));
                app.MainModule.Types.Add(foreign);
                imported.Parameters[0].ParameterType = new ByReferenceType(foreign);
                break;
            case "modopt": imported.Parameters[0].ParameterType = new OptionalModifierType(app.MainModule.TypeSystem.Object, imported.Parameters[0].ParameterType); break;
            case "valid": case "retained": break;
            default: throw new ArgumentException("Unknown mode " + mode);
        }
        int calls = 0, addresses = 0;
        foreach (var type in app.MainModule.Types)
        foreach (var method in type.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
            if (instruction.Operand is MethodReference reference && reference.DeclaringType.FullName == "Dn2Cpp.DecimalDivModSubset" && reference.Name == "Divide")
            {
                instruction.Operand = imported;
                if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldftn) addresses++;
                else calls++;
            }
        if (calls != 3 || addresses != 1) throw new Exception($"Unexpected divmod routes {calls}/{addresses}");
        if (mode == "retained")
        {
            foreach (var method in app.MainModule.Types.SelectMany(t => t.Methods).Where(m => m.HasBody))
            foreach (var instruction in method.Body.Instructions)
                if (instruction.Operand is MethodReference reference && reference.DeclaringType.FullName == "Dn2Cpp.DecimalDivModSubset" && reference.Name == "Run")
                { instruction.OpCode = Mono.Cecil.Cil.OpCodes.Nop; instruction.Operand = null; }
        }
        app.Write(args[1]);
        Console.WriteLine($"Decimal divmod direct={calls} address={addresses}; CLR execution forbidden for rewritten input");
        return true;
    }
}
