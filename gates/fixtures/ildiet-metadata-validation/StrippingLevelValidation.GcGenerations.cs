using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class StrippingLevelValidation
{
    static void CreateGcGenerations(string original, string directory, string mode)
    {
        if (mode is not ("retained" or "called")) throw new ArgumentException("Unknown GC generations mode");
        CreateGcMemory(original, directory, "retained");
        var read = new ReaderParameters { InMemory = true };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        var gc = cm.GetType("System.GC");
        gc.Methods.Remove(gc.Methods.Single(m => m.Name == "GetTotalMemory"));
        MethodDefinition Leaf(string name, params TypeReference[] parameters)
        {
            var method = new MethodDefinition(name, MethodAttributes.Private | MethodAttributes.Static,
                am.TypeSystem.Int32) { ImplAttributes = MethodImplAttributes.InternalCall };
            foreach (var parameter in parameters) method.Parameters.Add(new ParameterDefinition(parameter));
            gc.Methods.Add(method);
            return method;
        }
        var generation = Leaf("GetGenerationInternal", am.TypeSystem.Object);
        var maximum = Leaf("GetMaxGeneration");
        var exercise = Method(gc, "Exercise", am.TypeSystem.Int32);
        exercise.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        exercise.Body.Instructions.Clear();
        var il = exercise.Body.GetILProcessor();
        // Private leaves belong to CoreLib. Call them there using an actual
        // allocated object, both directly and by managed function pointer.
        il.Emit(OpCodes.Ldc_I4_0);
        foreach (var method in new[] { generation, maximum })
        foreach (bool address in new[] { false, true })
        {
            if (method.Parameters.Count != 0)
            {
                il.Emit(OpCodes.Ldc_I4_8);
                il.Emit(OpCodes.Newarr, cm.GetType("System.Byte"));
            }
            if (address)
            {
                il.Emit(OpCodes.Ldftn, method);
                var signature = new CallSite(am.TypeSystem.Int32);
                foreach (var parameter in method.Parameters)
                    signature.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, signature);
            }
            else il.Emit(OpCodes.Call, method);
            // Zero is the runtime's explicit single-generation model, not CLR parity.
            il.Emit(OpCodes.Or);
        }
        il.Emit(OpCodes.Ret);
        if (mode == "retained") gc.Methods.Remove(exercise);
        else
        {
            var main = am.EntryPoint;
            main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
            main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
            il = main.Body.GetILProcessor();
            il.Emit(OpCodes.Call, am.ImportReference(exercise));
            il.Emit(OpCodes.Ret);
        }
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
    }
}
