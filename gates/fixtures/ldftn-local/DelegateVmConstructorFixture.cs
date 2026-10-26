using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DelegateVmConstructorFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--delegate-vm-constructors") return false;
        var counts = new Dictionary<string, int>
        {
            ["CtorClosed"] = 2,
            ["CtorClosedStatic"] = 2,
            ["CtorRTClosed"] = 2,
            ["CtorOpened"] = 3,
            ["CtorVirtualDispatch"] = 3,
            ["CtorCollectibleClosedStatic"] = 3,
            ["CtorCollectibleOpened"] = 4,
            ["CtorCollectibleVirtualDispatch"] = 4,
        };
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib")
            throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.MulticastDelegate");
        var definitions = counts.ToDictionary(pair => pair.Key, pair =>
        {
            var method = owner.Methods.Single(m => m.Name == pair.Key);
            if (!method.IsPrivate || method.IsStatic || method.HasGenericParameters
                || method.ReturnType.MetadataType != MetadataType.Void || method.Parameters.Count != pair.Value
                || method.Parameters[0].ParameterType.MetadataType != MetadataType.Object
                || method.Parameters.Skip(1).Any(p => p.ParameterType.MetadataType != MetadataType.IntPtr)
                || !method.HasBody || !method.Body.Instructions.Any(i => i.OpCode.Code == Code.Stfld
                    && i.Operand is FieldReference field && field.Name == "_methodPtr"
                    && field.DeclaringType.FullName == "System.Delegate"))
                throw new InvalidOperationException("The CoreCLR delegate initialization contract changed: " + pair.Key);
            return method;
        });
        if (!definitions["CtorRTClosed"].Body.Instructions.Any(i => i.Operand is MethodReference method
            && method.DeclaringType.FullName == "System.Delegate" && method.Name == "AdjustTarget"))
            throw new InvalidOperationException("Missing VM target adjustment");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var targets = definitions.ToDictionary(pair => "Stub" + pair.Key, pair => module.ImportReference(pair.Value));
        IEnumerable<TypeDefinition> Types(TypeDefinition type) => new[] { type }.Concat(type.NestedTypes.SelectMany(Types));
        var rewritten = counts.Keys.ToDictionary(name => name, _ => 0);
        int calls = 0, addresses = 0;
        foreach (var type in module.Types.SelectMany(Types))
        foreach (var method in type.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MethodReference reference
                || reference.DeclaringType.FullName != "MulticastVmConstructorSubset.Program"
                || !targets.TryGetValue(reference.Name, out var target)) continue;
            if (instruction.OpCode.Code == Code.Call) calls++;
            else if (instruction.OpCode.Code == Code.Ldftn) addresses++;
            else throw new InvalidOperationException("Unexpected VM constructor fixture site");
            instruction.Operand = target;
            rewritten[target.Name]++;
        }
        if (calls != 8 || addresses != 1 || rewritten.Any(pair => pair.Value != (pair.Key == "CtorRTClosed" ? 2 : 1)))
            throw new InvalidOperationException("Expected eight direct VM sites and one RTClosed address");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("vm-ctor-source=CoreLib,private,instance,nongeneric,void,Object+IntPtr,VM-fields");
        Console.WriteLine("vm-ctor-sites=8-direct,1-address");
        return true;
    }
}
