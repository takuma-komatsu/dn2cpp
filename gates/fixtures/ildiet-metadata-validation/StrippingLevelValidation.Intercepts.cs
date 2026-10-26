using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class StrippingLevelValidation
{
    static void CreateRetainedIntercept(string original, string directory, string mode)
    {
        if (mode is not ("retained" or "called"))
            throw new ArgumentException("Unknown retained-intercept mode", nameof(mode));
        Directory.CreateDirectory(directory);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(original);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(original, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(original, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        var obj = cm.GetType("System.Object");
        TypeDefinition Type(string ns, string name, TypeReference parent)
        {
            var type = new TypeDefinition(ns, name, TypeAttributes.Public, parent);
            cm.Types.Add(type);
            return type;
        }
        Type("System", "Byte", cm.GetType("System.ValueType"));
        var array = Type("System", "Array", obj);
        var culture = Type("System.Globalization", "CultureInfo", obj);
        var invariant = Method(culture, "get_InvariantCulture", culture);
        invariant.Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName;
        invariant.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var setters = new[] { "set_CurrentCulture", "set_CurrentUICulture" }.Select(name =>
        {
            var setter = Method(culture, name, am.TypeSystem.Void);
            setter.Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName;
            setter.Parameters.Add(new ParameterDefinition(culture));
            return setter;
        }).ToArray();
        var forbidden = Type("MediumFixture", "RawBodyOnly", obj);
        var fail = new MethodDefinition("Fail", MethodAttributes.Public | MethodAttributes.Static, am.TypeSystem.Void)
        {
            ImplAttributes = MethodImplAttributes.InternalCall,
        };
        forbidden.Methods.Add(fail);
        var buffer = Type("System", "Buffer", obj);
        var touch = Method(buffer, "Touch", am.TypeSystem.Void);
        touch.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        MethodDefinition Cut(string name, TypeReference result, params TypeReference[] arguments)
        {
            var method = Method(buffer, name, result);
            method.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            foreach (var argument in arguments) method.Parameters.Add(new ParameterDefinition(argument));
            method.Body.Instructions.Clear();
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Call, fail);
            if (result.MetadataType != MetadataType.Void) il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);
            return method;
        }
        var copy = Cut("BlockCopy", am.TypeSystem.Void, array, am.TypeSystem.Int32, array,
            am.TypeSystem.Int32, am.TypeSystem.Int32);
        var length = Cut("ByteLength", am.TypeSystem.Int32, array);
        var get = Cut("GetByte", am.TypeSystem.Byte, array, am.TypeSystem.Int32);
        var set = Cut("SetByte", am.TypeSystem.Void, array, am.TypeSystem.Int32, am.TypeSystem.Byte);
        var main = am.EntryPoint;
        main.ReturnType = am.TypeSystem.Int32;
        main.Body = new MethodBody(main) { InitLocals = true };
        var entry = main.Body.GetILProcessor();
        foreach (var setter in setters)
        {
            entry.Emit(OpCodes.Call, am.ImportReference(invariant));
            entry.Emit(OpCodes.Call, am.ImportReference(setter));
        }
        entry.Emit(OpCodes.Call, am.ImportReference(touch));
        if (mode == "called")
        {
            var bytes = new ArrayType(am.TypeSystem.Byte);
            main.Body.Variables.Add(new VariableDefinition(bytes));
            main.Body.Variables.Add(new VariableDefinition(bytes));
            var failed = new VariableDefinition(am.TypeSystem.Int32);
            main.Body.Variables.Add(failed);
            for (int i = 0; i < 2; i++)
            {
                entry.Emit(OpCodes.Ldc_I4_4);
                entry.Emit(OpCodes.Newarr, am.TypeSystem.Byte);
                entry.Emit(OpCodes.Stloc, main.Body.Variables[i]);
            }
            void Call(MethodDefinition method, bool address)
            {
                if (address)
                {
                    entry.Emit(OpCodes.Ldftn, am.ImportReference(method));
                    var signature = new CallSite(am.ImportReference(method.ReturnType));
                    foreach (var parameter in method.Parameters)
                        signature.Parameters.Add(new ParameterDefinition(am.ImportReference(parameter.ParameterType)));
                    entry.Emit(OpCodes.Calli, signature);
                }
                else entry.Emit(OpCodes.Call, am.ImportReference(method));
            }
            void Check(int expected)
            {
                entry.Emit(OpCodes.Ldc_I4, expected);
                entry.Emit(OpCodes.Ceq);
                entry.Emit(OpCodes.Ldc_I4_0);
                entry.Emit(OpCodes.Ceq);
                entry.Emit(OpCodes.Ldloc, failed);
                entry.Emit(OpCodes.Or);
                entry.Emit(OpCodes.Stloc, failed);
            }
            foreach (bool address in new[] { false, true })
            {
                int expected = address ? 83 : 41;
                entry.Emit(OpCodes.Ldloc_0);
                entry.Emit(OpCodes.Ldc_I4_1);
                entry.Emit(OpCodes.Ldc_I4, expected);
                Call(set, address);
                entry.Emit(OpCodes.Ldloc_0);
                entry.Emit(OpCodes.Ldc_I4_0);
                entry.Emit(OpCodes.Ldloc_1);
                entry.Emit(OpCodes.Ldc_I4_0);
                entry.Emit(OpCodes.Ldc_I4_4);
                Call(copy, address);
                entry.Emit(OpCodes.Ldloc_1);
                Call(length, address);
                Check(4);
                entry.Emit(OpCodes.Ldloc_1);
                entry.Emit(OpCodes.Ldc_I4_1);
                Call(get, address);
                Check(expected);
                entry.Emit(OpCodes.Ldloc_1);
                entry.Emit(OpCodes.Ldc_I4_0);
                Call(get, address);
                Check(0);
            }
            entry.Emit(OpCodes.Ldloc, failed);
        }
        else entry.Emit(OpCodes.Ldc_I4_0);
        entry.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
        File.Copy(Path.Combine(original, "MediumLib.dll"), Path.Combine(directory, "MediumLib.dll"), true);
    }
}
