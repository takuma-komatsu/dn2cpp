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
        Type("System", "IntPtr", cm.GetType("System.ValueType"));
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
        var binder = Type("System.Runtime.Loader", "AssemblyLoadContext", obj);
        var assemblyName = Type("System.Reflection", "AssemblyName", obj);
        var runtimeAssembly = Type("System.Reflection", "RuntimeAssembly", obj);
        var guard = new MethodDefinition("IsTracingEnabled",
            MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, cm.TypeSystem.Boolean)
        { ImplAttributes = MethodImplAttributes.InternalCall };
        binder.Methods.Add(guard);
        var trace = Cut(binder, "TraceSatelliteSubdirectoryPathProbed",
            cm.TypeSystem.Boolean, cm.TypeSystem.String, cm.TypeSystem.Int32);
        trace.Attributes |= MethodAttributes.HideBySig;
        trace.Attributes = (trace.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
        var flow = new FieldDefinition("Flow", FieldAttributes.Private | FieldAttributes.Static, cm.TypeSystem.Int32);
        binder.Fields.Add(flow);
        var probe = Method(binder, "ResolveSatelliteAssembly", runtimeAssembly);
        probe.Attributes = MethodAttributes.Private | MethodAttributes.HideBySig;
        probe.HasThis = true;
        probe.Parameters.Add(new ParameterDefinition(assemblyName));
        probe.Body.Instructions.Clear();
        var probed = probe.Body.GetILProcessor();
        var resume = Instruction.Create(OpCodes.Ldsfld, flow);
        probed.Emit(OpCodes.Ldc_I4_1);
        probed.Emit(OpCodes.Stsfld, flow);
        probed.Emit(OpCodes.Call, guard);
        probed.Emit(OpCodes.Brfalse_S, resume);
        probed.Emit(OpCodes.Ldnull);
        probed.Emit(OpCodes.Ldc_I4_0);
        probed.Emit(OpCodes.Call, trace);
        probed.Emit(OpCodes.Pop);
        probed.Append(resume);
        probed.Emit(OpCodes.Ldc_I4_S, (sbyte)10);
        probed.Emit(OpCodes.Mul);
        probed.Emit(OpCodes.Ldc_I4_2);
        probed.Emit(OpCodes.Add);
        probed.Emit(OpCodes.Stsfld, flow);
        probed.Emit(OpCodes.Ldnull);
        probed.Emit(OpCodes.Ret);
        var binderCtor = Method(binder, ".ctor", cm.TypeSystem.Void, constructor: true);
        var binderProbe = Method(binder, "ProbeOrder", cm.TypeSystem.Int32);
        binderProbe.Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        binderProbe.Body.Instructions.Clear();
        var drive = binderProbe.Body.GetILProcessor();
        drive.Emit(OpCodes.Newobj, binderCtor);
        drive.Emit(OpCodes.Ldnull);
        drive.Emit(OpCodes.Call, probe);
        drive.Emit(OpCodes.Pop);
        drive.Emit(OpCodes.Ldsfld, flow);
        drive.Emit(OpCodes.Ret);
        if (mode == "retained")
        {
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(binder));
            entry.Emit(OpCodes.Pop);
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
            const MethodAttributes rangeAttributes = MethodAttributes.Public | MethodAttributes.Virtual
                | MethodAttributes.HideBySig;
            MethodDefinition AddRange(TypeDefinition owner, MethodAttributes attributes)
            {
                var method = new MethodDefinition("GetString", attributes, am.TypeSystem.String);
                owner.Methods.Add(method);
                method.Parameters.Add(new ParameterDefinition(new ArrayType(am.TypeSystem.Byte)));
                method.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
                method.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
                return method;
            }
            AddRange(encoding, rangeAttributes | MethodAttributes.NewSlot | MethodAttributes.Abstract);
            void RetainEncoding(string name, bool isSealed)
            {
                var owner = Type("System.Text", name, encoding);
                if (isSealed) owner.Attributes |= TypeAttributes.Sealed;
                var range = AddRange(owner, rangeAttributes);
                var rangeBody = range.Body.GetILProcessor();
                rangeBody.Emit(OpCodes.Call, fail);
                rangeBody.Emit(OpCodes.Ldnull);
                rangeBody.Emit(OpCodes.Throw);

                var encodingNamesake = Method(owner, range.Name, am.TypeSystem.String);
                encodingNamesake.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
                encodingNamesake.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, name + " namesake"));
                entry.Emit(OpCodes.Ldtoken, am.ImportReference(owner));
                entry.Emit(OpCodes.Pop);
            }
            RetainEncoding("UTF32Encoding", isSealed: true);
            RetainEncoding("UTF7Encoding", isSealed: false);

            var cstr = Type("System.StubHelpers", "CSTRMarshaler", obj);
            cstr.Attributes = TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed;
            var convert = Method(cstr, "ConvertFixedToManaged", am.TypeSystem.String);
            convert.Attributes = MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig;
            convert.Parameters.Add(new ParameterDefinition(am.TypeSystem.IntPtr));
            convert.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            convert.Body.Instructions.Clear();
            var convertBody = convert.Body.GetILProcessor();
            convertBody.Emit(OpCodes.Call, fail);
            convertBody.Emit(OpCodes.Ldnull);
            convertBody.Emit(OpCodes.Throw);

            var convertNamesake = Method(cstr, convert.Name, am.TypeSystem.String);
            convertNamesake.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            convertNamesake.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            convertNamesake.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, "CSTR namesake"));
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(cstr));
            entry.Emit(OpCodes.Pop);

            var marshal = Type("System.Runtime.InteropServices", "Marshal", obj);
            marshal.Attributes |= TypeAttributes.Abstract | TypeAttributes.Sealed;
            var asAny = Type("System.StubHelpers", "AsAnyMarshaler", obj);
            var touchAsAny = Cut(asAny, "Touch", am.TypeSystem.Void);
            var callback = Type("System", "Func`3", obj);
            for (int i = 0; i < 3; i++) callback.GenericParameters.Add(new GenericParameter("T" + i, callback));
            var slow = Method(marshal, "ReadValueSlow", am.TypeSystem.Void);
            var t = new GenericParameter("T", slow);
            slow.GenericParameters.Add(t);
            slow.ReturnType = t;
            slow.Parameters.Add(new ParameterDefinition(am.TypeSystem.Object));
            slow.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            var signature = new GenericInstanceType(callback);
            signature.GenericArguments.Add(am.TypeSystem.IntPtr);
            signature.GenericArguments.Add(am.TypeSystem.Int32);
            signature.GenericArguments.Add(t);
            slow.Parameters.Add(new ParameterDefinition(signature));
            var result = new VariableDefinition(t);
            slow.Body.Variables.Add(result);
            slow.Body.Instructions.Clear();
            var slowBody = slow.Body.GetILProcessor();
            slowBody.Emit(OpCodes.Call, touchAsAny);
            slowBody.Emit(OpCodes.Ldloca, result);
            slowBody.Emit(OpCodes.Initobj, t);
            slowBody.Emit(OpCodes.Ldloc, result);
            slowBody.Emit(OpCodes.Ret);

            MethodDefinition ReadByte(TypeReference first)
            {
                var readByte = Method(marshal, "ReadByte", am.TypeSystem.Byte);
                readByte.Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
                readByte.Parameters.Add(new ParameterDefinition(first));
                readByte.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
                return readByte;
            }
            var legacy = ReadByte(am.TypeSystem.Object);
            var slowByte = new GenericInstanceMethod(slow);
            slowByte.GenericArguments.Add(am.TypeSystem.Byte);
            legacy.Body.Instructions.Clear();
            var legacyBody = legacy.Body.GetILProcessor();
            legacyBody.Emit(OpCodes.Ldarg_0);
            legacyBody.Emit(OpCodes.Ldarg_1);
            legacyBody.Emit(OpCodes.Ldnull);
            legacyBody.Emit(OpCodes.Call, slowByte);
            legacyBody.Emit(OpCodes.Ret);
            var typed = ReadByte(am.TypeSystem.IntPtr);
            typed.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_7));
            entry.Emit(OpCodes.Ldtoken, am.ImportReference(marshal));
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
            entry.Emit(OpCodes.Call, am.ImportReference(binderProbe));
            Check(12); // both sides of the disabled trace arm ran, in order
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
