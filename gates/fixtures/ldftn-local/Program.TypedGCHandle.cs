using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal partial class Program
{
    private static bool TryWriteTypedGCHandleFixture(string[] args)
    {
        if (args.Length == 0 || args[0] != "--typed-gchandle-negative")
            return false;
        if (args.Length != 4)
            throw new ArgumentException("expected --typed-gchandle-negative input.dll output.dll case");
        using var assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(args[1]), new ReaderParameters { InMemory = true });
        var module = assembly.MainModule;
        string mode = args[3];
        var entry = assembly.EntryPoint ?? throw new InvalidOperationException("missing fixture entry point");
        foreach (var type in module.Types.Where(type => type != entry.DeclaringType && type.Name != "<Module>").ToArray())
            module.Types.Remove(type);
        entry.Body = new MethodBody(entry) { InitLocals = true };
        var il = entry.Body.GetILProcessor();
        if (mode.StartsWith("constrained-", StringComparison.Ordinal))
        {
            EmitInvalidTypedGCHandleContract(module, entry, mode);
        }
        else
        {
            var member = TypedGCHandleMember(module, mode);
            if (mode.StartsWith("ldftn-", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, member);
                il.Emit(OpCodes.Pop);
            }
            else if (mode.StartsWith("ldvirtftn-", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldvirtftn, member);
                il.Emit(OpCodes.Pop);
            }
            else
            {
                // A malformed MemberRef must fail before its claimed stack shape is consumed.
                il.Emit(mode.StartsWith("newobj-", StringComparison.Ordinal) ? OpCodes.Newobj : OpCodes.Call, member);
            }
        }
        il.Emit(OpCodes.Ret);
        string output = Path.GetFullPath(args[2]);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("typed-gchandle-negative:" + mode);
        return true;
    }

    private static void EmitInvalidTypedGCHandleContract(ModuleDefinition module, MethodDefinition entry, string mode)
    {
        bool equals = mode is "constrained-equals" or "constrained-disposable-equals" or "constrained-object-equals-handle";
        var handle = module.ImportReference(typeof(GCHandle<string>));
        TypeReference? owner = mode switch
        {
            "constrained-equatable-dispose" => module.ImportReference(typeof(IEquatable<GCHandle<string>>)),
            "constrained-disposable-equals" => module.ImportReference(typeof(IDisposable)),
            "constrained-object-equals-handle" => module.TypeSystem.Object,
            "constrained-dispose" or "constrained-equals" => null,
            _ => throw new ArgumentException("unknown typed handle contract " + mode)
        };
        MethodReference method;
        if (owner is null)
        {
            var unrelated = new TypeDefinition("TypedGCHandleNegative", equals ? "IUnrelatedEquals" : "IUnrelatedDispose",
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            module.Types.Add(unrelated);
            var declaration = new MethodDefinition(equals ? "Equals" : "Dispose", MethodAttributes.Public
                | MethodAttributes.Virtual | MethodAttributes.Abstract | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                equals ? module.TypeSystem.Boolean : module.TypeSystem.Void);
            unrelated.Methods.Add(declaration);
            method = declaration;
        }
        else
            method = new MethodReference(equals ? "Equals" : "Dispose",
                equals ? module.TypeSystem.Boolean : module.TypeSystem.Void, owner) { HasThis = true };
        if (equals)
            method.Parameters.Add(new ParameterDefinition(handle));
        var value = new VariableDefinition(handle);
        entry.Body.Variables.Add(value);
        var il = entry.Body.GetILProcessor();
        il.Emit(OpCodes.Ldloca, value);
        if (equals)
            il.Emit(OpCodes.Ldloc, value);
        il.Emit(OpCodes.Constrained, handle);
        il.Emit(OpCodes.Callvirt, method);
        if (equals)
            il.Emit(OpCodes.Pop);
        return;
    }

    private static MethodReference TypedGCHandleMember(ModuleDefinition module, string mode)
    {
        var definition = module.ImportReference(typeof(GCHandle<>));
        var owner = new GenericInstanceType(definition);
        owner.GenericArguments.Add(module.TypeSystem.String);
        var target = definition.GenericParameters[0];
        var self = new GenericInstanceType(definition);
        self.GenericArguments.Add(target);

        GenericInstanceType ClassEncodedSelf()
        {
            // Separate forwarded TypeRefs keep Cecil from normalizing a mixed class/value marker.
            var facade = module.AssemblyReferences.Single(reference => reference.Name == "System.Runtime");
            var classDefinition = new TypeReference(definition.Namespace, definition.Name, module, facade);
            classDefinition.GenericParameters.Add(new GenericParameter("T", classDefinition));
            var result = new GenericInstanceType(classDefinition);
            result.GenericArguments.Add(target);
            return result;
        }

        MethodReference Member(string name, TypeReference result, bool instance, params TypeReference[] parameters)
        {
            var method = new MethodReference(name, result, owner) { HasThis = instance };
            foreach (var parameter in parameters)
                method.Parameters.Add(new ParameterDefinition(parameter));
            return method;
        }

        string operation = mode.StartsWith("ldftn-", StringComparison.Ordinal) ? mode[6..]
            : mode.StartsWith("ldvirtftn-", StringComparison.Ordinal) ? mode[10..] : "dispose";
        var member = operation switch
        {
            "ctor" => Member(".ctor", module.TypeSystem.Void, true, target),
            "get-target" => Member("get_Target", target, true),
            "set-target" => Member("set_Target", module.TypeSystem.Void, true, target),
            "is-allocated" => Member("get_IsAllocated", module.TypeSystem.Boolean, true),
            "dispose" => Member("Dispose", module.TypeSystem.Void, true),
            "from-intptr" => Member("FromIntPtr", self, false, module.TypeSystem.IntPtr),
            "to-intptr" => Member("ToIntPtr", module.TypeSystem.IntPtr, false, self),
            "equals-self" => Member("Equals", module.TypeSystem.Boolean, true, self),
            "equals-object" => Member("Equals", module.TypeSystem.Boolean, true, module.TypeSystem.Object),
            "hash" => Member("GetHashCode", module.TypeSystem.Int32, true),
            _ => throw new ArgumentException("unknown typed handle operation " + operation)
        };
        if (mode.StartsWith("ldftn-", StringComparison.Ordinal) || mode.StartsWith("ldvirtftn-", StringComparison.Ordinal))
            return member;

        switch (mode)
        {
            case "call-get-target-return":
                return Member("get_Target", module.TypeSystem.Int32, true);
            case "call-set-target-parameter":
                return Member("set_Target", module.TypeSystem.Void, true, module.TypeSystem.Int32);
            case "call-ctor-parameter":
                return Member(".ctor", module.TypeSystem.Void, true, module.TypeSystem.Int32);
            case "call-dispose-return":
                member.ReturnType = module.TypeSystem.Int32;
                break;
            case "call-dispose-static":
                member.HasThis = false;
                break;
            case "call-dispose-explicit-this":
                member.ExplicitThis = true;
                break;
            case "call-dispose-vararg":
                member.CallingConvention = MethodCallingConvention.VarArg;
                break;
            case "call-dispose-generic":
                member.GenericParameters.Add(new GenericParameter("U", member));
                break;
            case "call-dispose-parameter":
                member.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
                break;
            case "call-owner-class":
                definition.IsValueType = false;
                owner.IsValueType = false;
                break;
            case "call-from-intptr-class":
                return Member("FromIntPtr", ClassEncodedSelf(), false, module.TypeSystem.IntPtr);
            case "call-to-intptr-class":
                return Member("ToIntPtr", module.TypeSystem.IntPtr, false, ClassEncodedSelf());
            case "call-dispose-modreq":
                member.ReturnType = new RequiredModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsVolatile)),
                    module.TypeSystem.Void);
                break;
            case "call-set-target-modopt":
                return Member("set_Target", module.TypeSystem.Void, true,
                    new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsVolatile)), target));
            case "call-owner-value-argument":
                owner.GenericArguments[0] = module.TypeSystem.Int32;
                break;
            case "call-owner-enum-argument":
                var enumeration = new TypeDefinition("TypedGCHandleNegative", "BadEnum",
                    TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(typeof(Enum)));
                enumeration.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public
                    | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
                module.Types.Add(enumeration);
                owner.GenericArguments[0] = enumeration;
                break;
            case "call-is-allocated-return":
                return Member("get_IsAllocated", module.TypeSystem.Int32, true);
            case "call-from-intptr-return":
                return Member("FromIntPtr", module.TypeSystem.Object, false, module.TypeSystem.IntPtr);
            case "call-from-intptr-parameter":
                return Member("FromIntPtr", self, false, module.TypeSystem.Int32);
            case "call-to-intptr-parameter":
                return Member("ToIntPtr", module.TypeSystem.IntPtr, false, module.TypeSystem.Object);
            case "call-equals-parameter":
                return Member("Equals", module.TypeSystem.Boolean, true, module.TypeSystem.Int32);
            case "call-equals-other-closed":
                var other = new GenericInstanceType(definition);
                other.GenericArguments.Add(module.TypeSystem.Object);
                return Member("Equals", module.TypeSystem.Boolean, true, other);
            case "call-hash-return":
                return Member("GetHashCode", module.TypeSystem.IntPtr, true);
            case "newobj-name":
                return Member("Dispose", module.TypeSystem.Void, true, target);
            case "newobj-return":
                return Member(".ctor", module.TypeSystem.Int32, true, target);
            case "newobj-static":
                return Member(".ctor", module.TypeSystem.Void, false, target);
            default:
                throw new ArgumentException("unknown typed handle fixture " + mode);
        }
        return member;
    }
}
