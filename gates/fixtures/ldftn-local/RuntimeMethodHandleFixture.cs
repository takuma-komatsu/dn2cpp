using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeMethodHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-method-handle") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeMethodHandle");
        var shapes = new (string Name, string Return, string Parameters, bool Private)[]
        {
            ("GetAttributes", "System.Reflection.MethodAttributes", "System.RuntimeMethodHandleInternal", false),
            ("GetImplAttributes", "System.Reflection.MethodImplAttributes", "System.IRuntimeMethodInfo", false),
            ("GetMethodTable", "System.Runtime.CompilerServices.MethodTable*", "System.RuntimeMethodHandleInternal", true),
            ("GetSlot", "System.Int32", "System.RuntimeMethodHandleInternal", false),
            ("GetMethodDef", "System.Int32", "System.RuntimeMethodHandleInternal", false),
            ("GetUtf8NameInternal", "System.Void*", "System.RuntimeMethodHandleInternal", true),
            ("HasMethodInstantiation", "System.Boolean", "System.RuntimeMethodHandleInternal", false),
            ("GetStubIfNeededInternal", "System.RuntimeMethodHandleInternal", "System.RuntimeMethodHandleInternal,System.RuntimeType", true),
            ("GetMethodFromCanonical", "System.RuntimeMethodHandleInternal", "System.RuntimeMethodHandleInternal,System.RuntimeType", false),
            ("IsGenericMethodDefinition", "System.Boolean", "System.RuntimeMethodHandleInternal", false),
            ("IsTypicalMethodDefinition", "System.Boolean", "System.IRuntimeMethodInfo", true),
            ("GetGenericParameterCount", "System.Int32", "System.RuntimeMethodHandleInternal", true),
            ("IsDynamicMethod", "System.Boolean", "System.RuntimeMethodHandleInternal", false),
            ("GetResolver", "System.Resolver", "System.RuntimeMethodHandleInternal", false),
            ("IsConstructor", "System.Boolean", "System.RuntimeMethodHandleInternal", false),
            ("GetLoaderAllocatorInternal", "System.Reflection.LoaderAllocator", "System.RuntimeMethodHandleInternal", true),
        };
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeMethodHandleSubset.Program");
        foreach (var shape in shapes)
        {
            var method = owner.Methods.Single(m => m.Name == shape.Name && m.IsInternalCall);
            if (!method.IsStatic || method.HasGenericParameters || method.HasBody
                || (shape.Private ? !method.IsPrivate : !method.IsAssembly)
                || method.ReturnType.FullName != shape.Return
                || string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName)) != shape.Parameters)
                throw new InvalidOperationException("CoreCLR MethodDesc declaration changed: " + shape.Name);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == shape.Name + route);
                stub.Body = new MethodBody(stub) { InitLocals = true };
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                foreach (var parameter in target.Parameters)
                {
                    if (parameter.ParameterType.IsValueType)
                    {
                        var local = new VariableDefinition(parameter.ParameterType);
                        stub.Body.Variables.Add(local);
                        il.Emit(OpCodes.Ldloca, local);
                        il.Emit(OpCodes.Initobj, local.VariableType);
                        il.Emit(OpCodes.Ldloc, local);
                    }
                    else il.Emit(OpCodes.Ldnull);
                }
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        var managed = owner.Methods.Single(m => m.Name == "GetAttributes" && m.HasBody);
        if (managed.Parameters.Count != 1 || managed.Parameters[0].ParameterType.FullName != "System.IRuntimeMethodInfo")
            throw new InvalidOperationException("CoreCLR managed overload changed");
        var wrapper = program.Methods.Single(m => m.Name == "GetAttributesManaged");
        wrapper.Body = new MethodBody(wrapper);
        var wrapperIl = wrapper.Body.GetILProcessor();
        wrapperIl.Emit(OpCodes.Ldnull);
        wrapperIl.Emit(OpCodes.Call, module.ImportReference(managed));
        wrapperIl.Emit(OpCodes.Pop);
        wrapperIl.Emit(OpCodes.Ret);
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("method-handle-sites=16-direct,16-address,exact-CoreLib-signatures");
        return true;
    }
}
