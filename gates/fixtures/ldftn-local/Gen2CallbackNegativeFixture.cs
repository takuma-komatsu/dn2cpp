using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Gen2CallbackNegativeFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--gen2-callback-negative") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Gen2CallbackFixture.Run(new[] { "--gen2-callback-direct", args[1], args[2] });
        string suffix = args[3] switch
        {
            "ctor0" => "0", "ctor1" => "1", "final" => "Final",
            _ => throw new InvalidOperationException("unknown Gen2 negative target")
        };
        bool address = args[4].StartsWith("address-", StringComparison.Ordinal);
        string mode = address ? args[4].Substring(8) : args[4];
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("Gen2CallbackSubset.Program");
        var method = program.Methods.Single(m => m.Name == (address ? "Pointer" : "Direct") + suffix);
        var instruction = method.Body.Instructions.Single(i => i.OpCode == (address ? OpCodes.Ldftn : OpCodes.Call));
        var original = (MethodReference)instruction.Operand;
        var malformed = new MethodReference(original.Name, original.ReturnType, original.DeclaringType)
        {
            HasThis = original.HasThis,
            ExplicitThis = original.ExplicitThis,
            CallingConvention = original.CallingConvention
        };
        foreach (var parameter in original.Parameters)
            malformed.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        switch (mode)
        {
            case "return": malformed.ReturnType = module.TypeSystem.Object; break;
            case "return-modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, original.ReturnType); break;
            case "static": malformed.HasThis = false; break;
            case "argument" when suffix != "Final": malformed.Parameters[0].ParameterType = module.TypeSystem.Object; break;
            case "argument-byref" when suffix != "Final": malformed.Parameters[0].ParameterType = new ByReferenceType(malformed.Parameters[0].ParameterType); break;
            case "argument-modifier" when suffix != "Final": malformed.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, malformed.Parameters[0].ParameterType); break;
            default: throw new InvalidOperationException("unknown Gen2 negative form");
        }
        instruction.Operand = malformed;
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}
