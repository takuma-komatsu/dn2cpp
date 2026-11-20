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
        MethodDefinition Cut(TypeDefinition owner, string name, TypeReference result, params TypeReference[] arguments)
        {
            var method = Method(owner, name, result);
            method.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            foreach (var argument in arguments) method.Parameters.Add(new ParameterDefinition(argument));
            method.Body.Instructions.Clear();
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Call, fail);
            if (result.MetadataType != MetadataType.Void) il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);
            return method;
        }
        var copy = Cut(buffer, "BlockCopy", am.TypeSystem.Void, array, am.TypeSystem.Int32, array,
            am.TypeSystem.Int32, am.TypeSystem.Int32);
        var length = Cut(buffer, "ByteLength", am.TypeSystem.Int32, array);
        var get = Cut(buffer, "GetByte", am.TypeSystem.Byte, array, am.TypeSystem.Int32);
        var set = Cut(buffer, "SetByte", am.TypeSystem.Void, array, am.TypeSystem.Int32, am.TypeSystem.Byte);
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
        if (mode == "retained")
        {
            var eventSource = Type("System.Diagnostics.Tracing", "EventSource", obj);
            var provider = Type("System.Threading.Tasks", "TplEventSource", eventSource);
            var guid = Type("System", "Guid", cm.GetType("System.ValueType"));
            guid.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
            for (int i = 0; i != 4; i++)
                guid.Fields.Add(new FieldDefinition("_" + i, FieldAttributes.Private, am.TypeSystem.Int32));
            var helper = Method(provider, "CreateGuidForTaskID", guid);
            helper.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            helper.Body.Instructions.Clear();
            var helperBody = helper.Body.GetILProcessor();
            helperBody.Emit(OpCodes.Call, fail);
            helperBody.Emit(OpCodes.Ldnull);
            helperBody.Emit(OpCodes.Throw);
            // A safe non-void provider return still needs a callable wrapper.
            Cut(provider, "ScopeId", am.TypeSystem.Int32);
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(provider));
            entry.Emit(OpCodes.Pop);

            var textInfo = Type("System.Globalization", "TextInfo", obj);
            var valueType = cm.GetType("System.ValueType");
            Type("System", "UInt32", valueType);
            var rune = Type("System.Text", "Rune", valueType);
            rune.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
            rune.Fields.Add(new FieldDefinition("_value", FieldAttributes.Private, am.TypeSystem.UInt32));
            var nonInvariant = Method(rune, "ChangeCaseCultureAware", rune);
            nonInvariant.Parameters.Add(new ParameterDefinition(rune));
            nonInvariant.Parameters.Add(new ParameterDefinition(textInfo));
            nonInvariant.Parameters.Add(new ParameterDefinition(am.TypeSystem.Boolean));
            nonInvariant.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, fail));
            nonInvariant.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldarg_0));
            // A namesake with a different signature must remain ordinarily callable.
            var namesake = Method(rune, nonInvariant.Name, am.TypeSystem.Int32);
            namesake.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_7));
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(rune));
            entry.Emit(OpCodes.Pop);

            var encoding = Type("System.Text", "Encoding", obj);
            encoding.Attributes |= TypeAttributes.Abstract;
            var utf32 = Type("System.Text", "UTF32Encoding", encoding);
            utf32.Attributes |= TypeAttributes.Sealed;
            const MethodAttributes rangeAttributes = MethodAttributes.Public | MethodAttributes.Virtual
                | MethodAttributes.HideBySig;
            var baseRange = new MethodDefinition("GetString",
                rangeAttributes | MethodAttributes.NewSlot | MethodAttributes.Abstract, am.TypeSystem.String);
            encoding.Methods.Add(baseRange);
            var range = new MethodDefinition(baseRange.Name, rangeAttributes, am.TypeSystem.String);
            utf32.Methods.Add(range);
            foreach (var method in new[] { baseRange, range })
            {
                method.Parameters.Add(new ParameterDefinition(new ArrayType(am.TypeSystem.Byte)));
                method.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
                method.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            }
            var rangeBody = range.Body.GetILProcessor();
            rangeBody.Emit(OpCodes.Call, fail);
            rangeBody.Emit(OpCodes.Ldnull);
            rangeBody.Emit(OpCodes.Throw);

            var utf32Namesake = Method(utf32, range.Name, am.TypeSystem.String);
            utf32Namesake.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            utf32Namesake.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, "UTF32 namesake"));
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(utf32));
            entry.Emit(OpCodes.Pop);
        }
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
