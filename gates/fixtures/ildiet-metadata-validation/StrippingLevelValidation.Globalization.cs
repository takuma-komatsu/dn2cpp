using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class StrippingLevelValidation
{
    static void CreateGlobalization(string original, string directory, string mode)
    {
        if (mode is not ("retained" or "called" or "managed" or "application" or "unrelated" or "alias"))
            throw new ArgumentException("Unknown globalization fixture mode", nameof(mode));
        Directory.CreateDirectory(directory);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(original);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(original, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(original, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        var obj = cm.GetType("System.Object");
        var value = cm.GetType("System.ValueType");
        var text = new TypeDefinition("System", "String", TypeAttributes.Public | TypeAttributes.Sealed, obj);
        cm.Types.Add(text);
        cm.Types.Add(new TypeDefinition("System", "Byte", TypeAttributes.Public | TypeAttributes.Sealed, value));
        cm.Types.Add(new TypeDefinition("System", "UInt16", TypeAttributes.Public | TypeAttributes.Sealed, value));
        var exception = new TypeDefinition("System", "Exception", TypeAttributes.Public, obj);
        cm.Types.Add(exception);
        var unsupported = new TypeDefinition("System", "PlatformNotSupportedException", TypeAttributes.Public, exception);
        cm.Types.Add(unsupported);
        var message = Method(exception, "get_Message", am.TypeSystem.String);
        message.Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
        message.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var contains = Method(text, "Contains", am.TypeSystem.Boolean);
        contains.Attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
        contains.Parameters.Add(new ParameterDefinition(am.TypeSystem.String));
        contains.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
        var culture = new TypeDefinition("System.Globalization", "CultureInfo", TypeAttributes.Public, obj);
        cm.Types.Add(culture);
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

        var module = mode == "application" ? am : cm;
        var interop = new TypeDefinition("", "Interop", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
            module.ImportReference(obj));
        var globalization = new TypeDefinition("", "Globalization", TypeAttributes.NestedPublic | TypeAttributes.Abstract
            | TypeAttributes.Sealed, module.ImportReference(obj));
        module.Types.Add(interop);
        interop.NestedTypes.Add(globalization);
        globalization.Fields.Add(new FieldDefinition("RetainedField", FieldAttributes.Private | FieldAttributes.Static,
            am.TypeSystem.Int32));
        var touch = Method(globalization, "Touch", am.TypeSystem.Void);
        touch.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        string nativeModule = mode == "alias" ? "System.Globalization.Native" : "libSystem.Globalization.Native";
        var native = new ModuleReference(nativeModule);
        module.ModuleReferences.Add(native);
        var pointer = new PointerType(am.TypeSystem.UInt16);
        MethodDefinition Import(string name, TypeReference buffer)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PInvokeImpl,
                am.TypeSystem.Int32)
            {
                ImplAttributes = MethodImplAttributes.PreserveSig,
                PInvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvCdecl | PInvokeAttributes.CharSetUnicode,
                    (mode == "unrelated" ? "Unrelated_" : "GlobalizationNative_") + name, native),
            };
            method.Parameters.Add(new ParameterDefinition(new PointerType(am.TypeSystem.UInt16)));
            method.Parameters.Add(new ParameterDefinition(buffer));
            method.Parameters.Add(new ParameterDefinition(am.TypeSystem.Int32));
            globalization.Methods.Add(method);
            return method;
        }
        var calendars = Import("GetCalendars", pointer);
        var addressOnly = Import("AddressOnly", pointer);
        var bytes = Import("GetSortKey", new ArrayType(am.TypeSystem.Byte));
        MethodDefinition Wrapper(string name, MethodDefinition target, bool address)
        {
            var method = Method(globalization, name, am.TypeSystem.Int32);
            method.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            var il = method.Body.GetILProcessor();
            method.Body.Instructions.Clear();
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_U);
            if (target.Parameters[1].ParameterType is PointerType) { il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Conv_U); }
            else il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldc_I4_0);
            if (address)
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(am.TypeSystem.Int32);
                foreach (var parameter in target.Parameters)
                    site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
            return method;
        }
        var call = Wrapper("CallCalendars", calendars, false);
        var address = Wrapper("AddressCalendars", addressOnly, true);
        var byteCall = Wrapper("CallSortKey", bytes, false);

        if (mode == "managed")
        {
            var attribute = new TypeDefinition("Dn2Cpp.Runtime", "NativeImplementationAttribute", TypeAttributes.Public,
                am.ImportReference(cm.GetType("System.Attribute")));
            am.Types.Add(attribute);
            var constructor = Method(attribute, ".ctor", am.TypeSystem.Void, constructor: true);
            constructor.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            constructor.Parameters.Add(new ParameterDefinition(am.TypeSystem.String));
            constructor.Parameters.Add(new ParameterDefinition(am.TypeSystem.String));
            foreach (var import in new[] { calendars, addressOnly })
            {
                var implementation = Method(am.EntryPoint.DeclaringType, "Managed" + import.Name, am.TypeSystem.Int32);
                foreach (var parameter in import.Parameters)
                    implementation.Parameters.Add(new ParameterDefinition(am.ImportReference(parameter.ParameterType)));
                implementation.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4, 57));
                var annotation = new CustomAttribute(constructor);
                annotation.ConstructorArguments.Add(new CustomAttributeArgument(am.TypeSystem.String, nativeModule));
                annotation.ConstructorArguments.Add(new CustomAttributeArgument(am.TypeSystem.String, import.PInvokeInfo.EntryPoint));
                implementation.CustomAttributes.Add(annotation);
            }
        }

        MethodDefinition Check(string name, MethodDefinition target, string entry)
        {
            var method = Method(am.EntryPoint.DeclaringType, name, am.TypeSystem.Int32);
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;
            method.Body.Variables.Add(new VariableDefinition(am.TypeSystem.Int32));
            method.Body.Variables.Add(new VariableDefinition(am.ImportReference(exception)));
            var il = method.Body.GetILProcessor();
            var start = Instruction.Create(OpCodes.Call, am.ImportReference(target));
            var handler = Instruction.Create(OpCodes.Stloc_1);
            var failure = Instruction.Create(OpCodes.Ldc_I4_2);
            var finish = Instruction.Create(OpCodes.Ldloc_0);
            il.Append(start);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Leave, finish);
            il.Append(handler);
            foreach (string part in new[] { nativeModule, entry })
            {
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Callvirt, am.ImportReference(message));
                il.Emit(OpCodes.Ldstr, part);
                il.Emit(OpCodes.Callvirt, am.ImportReference(contains));
                il.Emit(OpCodes.Brfalse, failure);
            }
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Leave, finish);
            il.Append(failure);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Leave, finish);
            il.Append(finish);
            il.Emit(OpCodes.Ret);
            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = am.ImportReference(unsupported), TryStart = start, TryEnd = handler,
                HandlerStart = handler, HandlerEnd = finish,
            });
            return method;
        }

        var main = am.EntryPoint;
        main.ReturnType = am.TypeSystem.Int32;
        main.Body = new MethodBody(main);
        var entryIl = main.Body.GetILProcessor();
        foreach (var setter in setters)
        {
            entryIl.Emit(OpCodes.Call, am.ImportReference(invariant));
            entryIl.Emit(OpCodes.Call, am.ImportReference(setter));
        }
        entryIl.Emit(OpCodes.Call, am.ImportReference(touch));
        if (mode is "called" or "alias")
        {
            var checks = new[] { Check("CheckCall", call, "GlobalizationNative_GetCalendars"),
                Check("CheckAddress", address, "GlobalizationNative_AddressOnly"),
                Check("CheckBytes", byteCall, "GlobalizationNative_GetSortKey") };
            entryIl.Emit(OpCodes.Ldc_I4_0);
            foreach (var check in checks) { entryIl.Emit(OpCodes.Call, check); entryIl.Emit(OpCodes.Add); }
        }
        else if (mode == "managed")
        {
            entryIl.Emit(OpCodes.Call, am.ImportReference(call));
            entryIl.Emit(OpCodes.Call, am.ImportReference(address));
            entryIl.Emit(OpCodes.Add);
            entryIl.Emit(OpCodes.Ldc_I4, 114);
            entryIl.Emit(OpCodes.Sub);
        }
        else entryIl.Emit(OpCodes.Ldc_I4_0);
        entryIl.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
        File.Copy(Path.Combine(original, "MediumLib.dll"), Path.Combine(directory, "MediumLib.dll"), true);
    }
}
