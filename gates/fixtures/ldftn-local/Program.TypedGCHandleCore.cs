using Mono.Cecil;
using Mono.Cecil.Cil;

internal partial class Program
{
    private static bool TryWriteTypedGCHandleCore(string[] args)
    {
        if (args.Length == 0 || args[0] != "--typed-gchandle-core")
            return false;
        if (args.Length != 2)
            throw new ArgumentException("expected --typed-gchandle-core output.dll");
        using var core = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.Private.CoreLib", new Version(10, 0)),
            "System.Private.CoreLib", ModuleKind.Dll);
        var module = core.MainModule;
        using var signatures = ModuleDefinition.CreateModule("TypedGCHandleSignatures", ModuleKind.Dll);
        var types = signatures.TypeSystem;
        if (types.CoreLibrary is AssemblyNameReference reference)
        {
            reference.Name = core.Name.Name;
            reference.Version = core.Name.Version;
            reference.PublicKeyToken = Array.Empty<byte>();
        }
        TypeDefinition Type(string ns, string name, TypeReference? parent = null,
            TypeAttributes attributes = TypeAttributes.Public)
        {
            var type = new TypeDefinition(ns, name, attributes, parent);
            module.Types.Add(type);
            return type;
        }
        var obj = Type("System", "Object");
        var value = Type("System", "ValueType", obj);
        foreach (string name in new[] { "Void", "Boolean", "Int32", "IntPtr" })
            Type("System", name, value, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
        Type("System", "String", obj, TypeAttributes.Public | TypeAttributes.Sealed);
        Type("System", "Array", obj);

        MethodDefinition Method(TypeDefinition owner, string name, TypeReference result,
            MethodAttributes attributes, params TypeReference[] parameters)
        {
            var method = new MethodDefinition(name, attributes | MethodAttributes.HideBySig, result);
            foreach (var parameter in parameters)
                method.Parameters.Add(new ParameterDefinition(parameter));
            owner.Methods.Add(method);
            if (!method.IsAbstract)
            {
                var il = method.Body.GetILProcessor();
                if (result.MetadataType is MetadataType.Boolean or MetadataType.Int32)
                    il.Emit(OpCodes.Ldc_I4_0);
                else if (result.MetadataType == MetadataType.IntPtr)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I);
                }
                else if (result.MetadataType != MetadataType.Void)
                {
                    var local = new VariableDefinition(result);
                    method.Body.Variables.Add(local);
                    method.Body.InitLocals = true;
                    il.Emit(OpCodes.Ldloc, local);
                }
                il.Emit(OpCodes.Ret);
            }
            return method;
        }
        var instance = MethodAttributes.Public;
        var statik = MethodAttributes.Public | MethodAttributes.Static;
        var slot = instance | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        Method(obj, ".ctor", types.Void, instance | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        Method(obj, "Equals", types.Boolean, slot, types.Object);
        Method(obj, "GetHashCode", types.Int32, slot);
        Method(obj, "ToString", types.String, slot);

        var disposable = Type("System", "IDisposable", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        Method(disposable, "Dispose", types.Void, slot | MethodAttributes.Abstract);
        var equatable = Type("System", "IEquatable`1", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
        var argument = new GenericParameter("T", equatable);
        equatable.GenericParameters.Add(argument);
        Method(equatable, "Equals", types.Boolean, slot | MethodAttributes.Abstract, argument);

        var handle = Type("System.Runtime.InteropServices", "GCHandle`1", value,
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
        var target = new GenericParameter("T", handle) { Attributes = GenericParameterAttributes.ReferenceTypeConstraint };
        handle.GenericParameters.Add(target);
        var self = new GenericInstanceType(handle) { IsValueType = true };
        self.GenericArguments.Add(target);
        var contract = new GenericInstanceType(equatable);
        contract.GenericArguments.Add(self);
        handle.Interfaces.Add(new InterfaceImplementation(disposable));
        handle.Interfaces.Add(new InterfaceImplementation(contract));
        handle.Fields.Add(new FieldDefinition("_handle", FieldAttributes.Private, types.IntPtr));
        Method(handle, ".ctor", types.Void, instance | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, target);
        var getter = Method(handle, "get_Target", target, instance | MethodAttributes.SpecialName);
        var setter = Method(handle, "set_Target", types.Void, instance | MethodAttributes.SpecialName, target);
        handle.Properties.Add(new PropertyDefinition("Target", PropertyAttributes.None, target) { GetMethod = getter, SetMethod = setter });
        var allocated = Method(handle, "get_IsAllocated", types.Boolean, instance | MethodAttributes.SpecialName);
        handle.Properties.Add(new PropertyDefinition("IsAllocated", PropertyAttributes.None, types.Boolean) { GetMethod = allocated });
        Method(handle, "Dispose", types.Void, slot | MethodAttributes.Final);
        Method(handle, "Equals", types.Boolean, slot | MethodAttributes.Final, self);
        Method(handle, "Equals", types.Boolean, instance | MethodAttributes.Virtual, types.Object);
        Method(handle, "GetHashCode", types.Int32, instance | MethodAttributes.Virtual);
        Method(handle, "FromIntPtr", self, statik, types.IntPtr);
        Method(handle, "ToIntPtr", types.IntPtr, statik, self);

        // Retention must stop at the modeled members, rather than emit these raw bodies.
        var forbidden = Type("TypedGCHandleFixture", "RawBodyOnly", obj);
        var fail = new MethodDefinition("Fail", statik, types.Void) { ImplAttributes = MethodImplAttributes.InternalCall };
        forbidden.Methods.Add(fail);
        foreach (var method in handle.Methods)
            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, fail));

        var intptr = module.GetType("System.IntPtr");
        intptr.Fields.Add(new FieldDefinition("Zero", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, types.IntPtr));
        Method(intptr, "op_Equality", types.Boolean, statik | MethodAttributes.SpecialName, types.IntPtr, types.IntPtr);
        Method(intptr, "op_Inequality", types.Boolean, statik | MethodAttributes.SpecialName, types.IntPtr, types.IntPtr);
        var culture = Type("System.Globalization", "CultureInfo", obj);
        Method(culture, "get_InvariantCulture", culture, statik | MethodAttributes.SpecialName);
        Method(culture, "set_CurrentCulture", types.Void, statik | MethodAttributes.SpecialName, culture);
        Method(culture, "set_CurrentUICulture", types.Void, statik | MethodAttributes.SpecialName, culture);
        var gc = Type("System", "GC", obj);
        Method(gc, "Collect", types.Void, statik);
        Method(gc, "WaitForPendingFinalizers", types.Void, statik);
        var console = Type("System", "Console", obj);
        Method(console, "WriteLine", types.Void, statik, types.String);

        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        core.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("typed-gchandle-core:written");
        return true;
    }
}
