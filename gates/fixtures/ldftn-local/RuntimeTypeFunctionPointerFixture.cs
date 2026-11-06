using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeFunctionPointerFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-function-pointer" or "--runtime-type-function-pointer-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        if (!runtimeType.IsSealed) throw new InvalidOperationException("RuntimeType must remain sealed");
        var function = owner.Methods.Single(m => m.Name == "IsFunctionPointer");
        var unmanaged = owner.Methods.Single(m => m.Name == "IsUnmanagedFunctionPointer");
        var functionGetter = runtimeType.Methods.Single(m => m.Name == "get_IsFunctionPointer");
        var unmanagedGetter = runtimeType.Methods.Single(m => m.Name == "get_IsUnmanagedFunctionPointer");
        Validate(function, functionGetter, true, runtimeType);
        Validate(unmanaged, unmanagedGetter, false, runtimeType);
        var body = function.Body.Instructions;
        var element = core.MainModule.GetType("System.Reflection.CorElementType");
        if (function.Body.HasExceptionHandlers || function.Body.HasVariables || body.Count != 5
            || body[0].OpCode != OpCodes.Ldarg_0 || body[1].OpCode != OpCodes.Callvirt
            || body[1].Operand is not MethodReference getElement || getElement.Resolve() is not { Name: "GetCorElementType" } getElementDefinition
            || getElementDefinition.DeclaringType != runtimeType || !getElementDefinition.HasThis
            || getElementDefinition.Parameters.Count != 0 || getElementDefinition.ReturnType.Resolve() != element
            || body[2].OpCode != OpCodes.Ldc_I4_S || (sbyte)body[2].Operand != 0x1b
            || body[3].OpCode != OpCodes.Ceq || body[4].OpCode != OpCodes.Ret
            || !element.IsEnum || element.Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Byte
            || Convert.ToInt32(element.Fields.Single(f => f.Name == "ELEMENT_TYPE_FNPTR").Constant) != 0x1b)
            throw new InvalidOperationException("Managed function-pointer predicate IL changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeFunctionPointerSubset.FunctionPointerPredicateChecks");
        bool reflectionOnly = args[0] == "--runtime-type-function-pointer-reflection";
        foreach (string prefix in new[] { "Function", "Unmanaged" })
        {
            var helper = prefix == "Function" ? function : unmanaged;
            var getter = prefix == "Function" ? functionGetter : unmanagedGetter;
            foreach (string route in reflectionOnly ? new[] { "Reflection", "ReflectionMethod" }
                : new[] { "Direct", "Pointer", "Getter", "VirtualGetter", "ConstrainedGetter", "Address", "GetterAddress", "FlowGetterAddress", "Reflection", "ReflectionMethod" })
            {
                var stub = checks.Methods.Single(m => m.Name == prefix + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(route is "Getter" or "VirtualGetter" or "ConstrainedGetter" or "GetterAddress" or "FlowGetterAddress" ? getter : helper);
                if (route is "Reflection" or "ReflectionMethod")
                {
                    il.Emit(OpCodes.Ldtoken, module.ImportReference(owner));
                    il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                    il.Emit(OpCodes.Ldstr, helper.Name);
                    il.Emit(OpCodes.Ldc_I4, (int)(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldtoken, module.ImportReference(runtimeType));
                    il.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle")!));
                    il.Emit(OpCodes.Stelem_Ref);
                    il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetMethod("GetMethod",
                        new[] { typeof(string), typeof(System.Reflection.BindingFlags), typeof(Type[]) })!));
                    if (route == "Reflection")
                    {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Newarr, module.TypeSystem.Object);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Stelem_Ref);
                        il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(System.Reflection.MethodBase)
                            .GetMethod("Invoke", new[] { typeof(object), typeof(object[]) })!));
                        il.Emit(OpCodes.Unbox_Any, module.TypeSystem.Boolean);
                    }
                }
                else if (route == "ConstrainedGetter")
                {
                    stub.Body.InitLocals = true;
                    var local = new VariableDefinition(module.ImportReference(runtimeType));
                    stub.Body.Variables.Add(local);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                    il.Emit(OpCodes.Stloc, local);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Constrained, module.ImportReference(runtimeType));
                    il.Emit(OpCodes.Callvirt, target);
                }
                else if (route == "FlowGetterAddress")
                {
                    stub.Body.InitLocals = true;
                    var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                    var address = new VariableDefinition(module.TypeSystem.IntPtr);
                    stub.Body.Variables.Add(receiver);
                    stub.Body.Variables.Add(address);
                    var direct = Instruction.Create(OpCodes.Ldftn, target);
                    var done = Instruction.Create(OpCodes.Ldloc, receiver);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                    il.Emit(OpCodes.Stloc, receiver);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Brfalse, direct);
                    il.Emit(OpCodes.Ldloc, receiver);
                    il.Emit(OpCodes.Ldvirtftn, target);
                    il.Emit(OpCodes.Stloc, address);
                    il.Emit(OpCodes.Br, done);
                    il.Append(direct);
                    il.Emit(OpCodes.Stloc, address);
                    il.Append(done);
                    il.Emit(OpCodes.Ldloc, address);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else if (route == "Address")
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldftn, target);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else if (route == "GetterAddress")
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldvirtftn, target);
                        EmitDelegate(il, module, stub.ReturnType);
                    }
                    else il.Emit(route == "VirtualGetter" ? OpCodes.Callvirt : OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-function-pointer-sites=" + (reflectionOnly ? "reflection-only" : "direct,calli,getter,callvirt,constrained,ldftn,ldvirtftn,flowing,reflection")
            + ",exact-CoreLib-declarations,retained-getter-IL,native-only");
        return true;
    }

    private static void Validate(MethodDefinition helper, MethodDefinition getter, bool managed, TypeDefinition runtimeType)
    {
        if (helper.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || helper.HasThis || helper.ExplicitThis || helper.HasGenericParameters
            || helper.CallingConvention != MethodCallingConvention.Default || helper.IsPInvokeImpl
            || helper.ReturnType.MetadataType != MetadataType.Boolean || helper.Parameters.Count != 1
            || helper.Parameters[0].ParameterType.Resolve() != runtimeType
            || helper.HasBody != managed || (helper.RVA != 0) != managed
            || helper.ImplAttributes != (managed ? MethodImplAttributes.IL : MethodImplAttributes.InternalCall)
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || !getter.HasThis || getter.ExplicitThis || getter.HasGenericParameters
            || getter.CallingConvention != MethodCallingConvention.Default || !getter.HasBody || getter.RVA == 0
            || getter.ImplAttributes != MethodImplAttributes.IL || getter.IsPInvokeImpl || getter.Parameters.Count != 0
            || getter.ReturnType.MetadataType != MetadataType.Boolean || getter.Body.HasExceptionHandlers || getter.Body.HasVariables
            || getter.Body.Instructions.Count != 3 || getter.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || getter.Body.Instructions[1].OpCode != OpCodes.Call
            || getter.Body.Instructions[1].Operand is not MethodReference target || target.Resolve() != helper
            || getter.Body.Instructions[2].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Runtime function-pointer declarations changed");
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}
