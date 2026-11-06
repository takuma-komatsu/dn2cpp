using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class StrippingLevelValidation
{
    static void CheckGcMemoryClr()
    {
        var method = typeof(GC).GetMethod("GetTotalMemory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, System.Type.EmptyTypes, null) ?? throw new InvalidOperationException("Missing GC query");
        if (method.ReturnType != typeof(long) || !method.IsPrivate || !method.IsStatic
            || (method.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) == 0)
            throw new InvalidOperationException("The CoreLib GC query shape changed");
        var query = method.CreateDelegate<Func<long>>();
        if (query() < 0 || (long)method.Invoke(null, null)! < 0)
            throw new InvalidOperationException("Negative heap size");
        Console.WriteLine("gc-memory-clr=private-qcall,delegate,invoke,nonnegative");
    }

    static void CreateGcMemory(string original, string directory, string mode)
    {
        if (mode is not ("retained" or "called")) throw new ArgumentException("Unknown GC query mode");
        CreateRetainedIntercept(original, directory, "retained");
        var read = new ReaderParameters { InMemory = true };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        cm.Types.Add(new TypeDefinition("System", "Int64", TypeAttributes.Public | TypeAttributes.Sealed,
            cm.GetType("System.ValueType")));
        var gc = new TypeDefinition("System", "GC", TypeAttributes.Public, cm.GetType("System.Object"));
        cm.Types.Add(gc);
        var touch = Method(gc, "Touch", cm.TypeSystem.Void);
        touch.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        var query = new MethodDefinition("GetTotalMemory", MethodAttributes.Private | MethodAttributes.Static
            | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl, am.TypeSystem.Int64);
        var qcall = new ModuleReference("QCall");
        cm.ModuleReferences.Add(qcall);
        query.PInvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvWinapi, "GCInterface_GetTotalMemory", qcall);
        query.ImplAttributes = MethodImplAttributes.PreserveSig;
        gc.Methods.Add(query);
        // An in-CoreLib caller can name the private MethodDef, just as Medium's
        // retained reflection wrapper does. No invented managed body stands in for it.
        var exercise = Method(gc, "Exercise", cm.TypeSystem.Int32);
        exercise.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        exercise.Body.Instructions.Clear();
        var il = exercise.Body.GetILProcessor();
        il.Emit(OpCodes.Call, query);
        il.Emit(OpCodes.Ldc_I8, 0L);
        il.Emit(OpCodes.Clt);
        il.Emit(OpCodes.Ldftn, query);
        il.Emit(OpCodes.Calli, new CallSite(am.TypeSystem.Int64));
        il.Emit(OpCodes.Ldc_I8, 0L);
        il.Emit(OpCodes.Clt);
        il.Emit(OpCodes.Or);
        il.Emit(OpCodes.Ret);
        if (mode == "retained") gc.Methods.Remove(exercise);
        var main = am.EntryPoint;
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        il = main.Body.GetILProcessor();
        il.Emit(OpCodes.Call, am.ImportReference(touch));
        if (mode == "called") il.Emit(OpCodes.Call, am.ImportReference(exercise));
        else il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
    }
}
