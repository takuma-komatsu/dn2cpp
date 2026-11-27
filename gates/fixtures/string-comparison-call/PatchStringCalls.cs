using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StringComparisonCall;

internal static class PatchStringCalls
{
    internal static void Apply(string[] args)
    {
        string path = Path.GetFullPath(args.Single());
        using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = assembly.MainModule;
        var type = module.GetType("StringComparisonCall.Program");
        var equals = type.Methods.Single(m => m.Name == "DirectEquals");
        equals.Body.Instructions.Single(i => i.OpCode == OpCodes.Callvirt && i.Operand is MethodReference
            { Name: "Equals", DeclaringType.FullName: "System.String" }).OpCode = OpCodes.Call;

        // Roslyn binds overrides through Object and does not expose these three span
        // methods in the reference assembly. Use the exact definitions in the CoreLib
        // that will also execute the oracle.
        Rewrite("DirectObjectEquals", "Equals", typeof(object));
        Rewrite("DirectToString", "ToString");
        Rewrite("DirectTrim", "Trim", typeof(ReadOnlySpan<char>));
        Rewrite("DirectTrimStart", "TrimStart", typeof(ReadOnlySpan<char>));
        Rewrite("DirectTrimEnd", "TrimEnd", typeof(ReadOnlySpan<char>));

        // The staged oracle and native fixture only contain the test entry point.
        module.EntryPoint = type.Methods.Single(m => m.Name == "Run");
        type.Methods.Remove(type.Methods.Single(m => m.Name == "Main"));
        module.Types.Remove(module.GetType("StringComparisonCall.PatchStringCalls"));
        module.AssemblyReferences.Remove(module.AssemblyReferences.Single(r => r.Name == "Mono.Cecil"));

        string temporary = path + ".tmp";
        try
        {
            assembly.Write(temporary, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }

        void Rewrite(string fixture, string target, params Type[] parameters)
        {
            var source = type.Methods.Single(m => m.Name == fixture);
            var method = typeof(string).GetMethod(target, parameters)
                ?? throw new InvalidOperationException($"String.{target} is absent in the pinned CoreLib");
            source.Body = new MethodBody(source);
            var il = source.Body.GetILProcessor();
            foreach (var parameter in source.Parameters)
                il.Emit(OpCodes.Ldarg, parameter);
            il.Emit(OpCodes.Call, module.ImportReference(method));
            il.Emit(OpCodes.Ret);
        }
    }
}
