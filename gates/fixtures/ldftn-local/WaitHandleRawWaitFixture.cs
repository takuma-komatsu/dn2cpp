using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class WaitHandleRawWaitFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--wait-handle-raw-wait" or "--wait-handle-raw-wait-shim" or "--wait-handle-raw-wait-public"))
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var owner = core.MainModule.GetType("System.Threading.WaitHandle");
        bool Span(TypeReference type) => type is GenericInstanceType { IsValueType: true, GenericArguments.Count: 1 } span
            && span.ElementType.Resolve().Module == core.MainModule && span.ElementType.FullName == "System.ReadOnlySpan`1"
            && span.GenericArguments[0].MetadataType == MetadataType.IntPtr;
        MethodDefinition Wrapper(string name, int count)
        {
            var method = owner.Methods.Single(m => m.Name == name && m.Parameters.Count == count);
            bool assembly = name == "WaitMultipleIgnoringSyncContext" && count == 3;
            if (method.Attributes != ((assembly ? MethodAttributes.Assembly : MethodAttributes.Private) | MethodAttributes.Static | MethodAttributes.HideBySig)
                || method.ImplAttributes != MethodImplAttributes.IL || !method.HasBody || method.HasThis || method.ExplicitThis
                || method.CallingConvention != MethodCallingConvention.Default || method.HasGenericParameters || method.HasPInvokeInfo
                || method.ReturnType.MetadataType != MetadataType.Int32 || !Span(method.Parameters[0].ParameterType)
                || count == 4 && method.Parameters[1].ParameterType.MetadataType != MetadataType.Int32
                || method.Parameters[count - 2].ParameterType.MetadataType != MetadataType.Boolean
                || method.Parameters[count - 1].ParameterType.MetadataType != MetadataType.Int32
                || method.Body.Instructions.Any(i => i.OpCode.Code is Code.Ldfld or Code.Stfld or Code.Ldsfld or Code.Stsfld))
                throw new InvalidOperationException("Raw wait wrapper changed: " + name + "/" + count);
            return method;
        }
        var wrapper3 = Wrapper("WaitMultipleIgnoringSyncContext", 3);
        var core3 = Wrapper("WaitMultipleIgnoringSyncContextCore", 3);
        var wrapper4 = Wrapper("WaitMultipleIgnoringSyncContext", 4);
        var import = owner.Methods.Single(m => m.Name == "<WaitMultipleIgnoringSyncContext>g____PInvoke|2_0");
        if (import.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
            || import.ImplAttributes != MethodImplAttributes.PreserveSig || import.HasBody || import.HasThis || import.ExplicitThis
            || import.CallingConvention != MethodCallingConvention.Default || import.HasGenericParameters
            || import.PInvokeInfo is not { Module.Name: "QCall", EntryPoint: "WaitHandle_WaitMultipleIgnoringSyncContext" }
            || import.ReturnType.MetadataType != MetadataType.Int32 || import.Parameters.Count != 4
            || import.Parameters[0].ParameterType is not PointerType { ElementType.MetadataType: MetadataType.IntPtr }
            || import.Parameters.Skip(1).Any(p => p.ParameterType.MetadataType != MetadataType.Int32))
            throw new InvalidOperationException("Raw wait import changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("WaitHandleRawWaitRoutes.Program");
        if (args[0] != "--wait-handle-raw-wait-public")
        {
            foreach (var (name, definition, pointer) in new[] {
                ("Direct3", wrapper3, false), ("Core3", core3, false), ("Direct4", wrapper4, false),
                ("Pointer3", wrapper3, true), ("PointerCore3", core3, true), ("Pointer4", wrapper4, true),
                ("ImportDirect", import, false), ("ImportPointer", import, true) })
            {
                var method = program.Methods.Single(m => m.Name == name);
                var target = module.ImportReference(definition);
                if (method.ReturnType.FullName != target.ReturnType.FullName
                    || string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName))
                        != string.Join(",", target.Parameters.Select(p => p.ParameterType.FullName)))
                    throw new InvalidOperationException("Fixture signature mismatch: " + name);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                foreach (var parameter in method.Parameters) il.Emit(OpCodes.Ldarg, parameter);
                if (pointer)
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            if (args[0] == "--wait-handle-raw-wait-shim")
            {
                var attribute = module.GetType("Dn2Cpp.Runtime.NativeImplementationAttribute");
                var binding = new CustomAttribute(attribute.Methods.Single(m => m.IsConstructor));
                binding.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "QCall"));
                binding.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "WaitHandle_WaitMultipleIgnoringSyncContext"));
                program.Methods.Single(m => m.Name == "Managed").CustomAttributes.Add(binding);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (args[0] == "--wait-handle-raw-wait-public" ? "RunPublicChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("raw-wait-fixture=" + args[0] + ":three-real-wrappers,exact-QCall-binding,native-only-private-calls");
        return true;
    }
}
