using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeDeclaringFixture
{
    public static bool Run(string[] args)
    {
        if(args.Length is not(3 or 4)||!args[0].StartsWith("--runtime-type-declaring",StringComparison.Ordinal))return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);var owner=core.MainModule.GetType("System.RuntimeTypeHandle");var rt=core.MainModule.GetType("System.RuntimeType");
        var definition=owner.Methods.Single(m=>m.Name=="GetDeclaringType");
        if(core.Name.Name!="System.Private.CoreLib"||definition.Attributes!=(MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig)
            ||definition.ImplAttributes!=MethodImplAttributes.IL||!definition.HasBody||definition.HasThis||definition.ExplicitThis||definition.HasGenericParameters
            ||definition.CallingConvention!=MethodCallingConvention.Default||definition.ReturnType.Resolve()!=rt||definition.Parameters.Count!=1||definition.Parameters[0].ParameterType.Resolve()!=rt)
            throw new InvalidOperationException("GetDeclaringType managed declaration changed");
        using var app=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters{InMemory=true});var module=app.MainModule;var checks=module.GetType("RuntimeTypeDeclaringSubset.DeclaringChecks");
        bool oracle=args[0].Contains("oracle",StringComparison.Ordinal),negative=args[0]=="--runtime-type-declaring-negative";
        MethodReference target=module.ImportReference(definition);
        if(negative)
        {
            target=new MethodReference(target.Name,target.ReturnType,target.DeclaringType);target.Parameters.Add(new ParameterDefinition(module.ImportReference(rt)));
            switch(args[3].Replace("address-","",StringComparison.Ordinal))
            {
                case "return-type":target.ReturnType=module.ImportReference(typeof(Type));break;
                case "parameter-type":target.Parameters[0].ParameterType=module.ImportReference(typeof(Type));break;
                case "instance":target.HasThis=true;break;
                case "generic":target.GenericParameters.Add(new GenericParameter("T",target));break;
                case "byref":target.Parameters[0].ParameterType=new ByReferenceType(target.Parameters[0].ParameterType);break;
                case "modifier":target.ReturnType=new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)),target.ReturnType);break;
                default:throw new InvalidOperationException("Unknown negative");
            }
        }
        if(!oracle)
        {
            var caller=checks.NestedTypes.Single(t=>t.Name=="Caller");
            foreach(var member in caller.Methods.Where(m=>m.Name is "Invoke" or "BeginInvoke")) member.Parameters[0].ParameterType=module.ImportReference(rt);
            caller.Methods.Single(m=>m.Name=="Invoke").ReturnType=module.ImportReference(rt);
            caller.Methods.Single(m=>m.Name=="EndInvoke").ReturnType=module.ImportReference(rt);
            foreach(string route in new[]{"Direct","Pointer","Address"})
            {
                var method=checks.Methods.Single(m=>m.Name==route);method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();
                if(route=="Address")
                {
                    il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Ldftn,target);var ctor=new MethodReference(".ctor",module.TypeSystem.Void,method.ReturnType){HasThis=true};
                    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));il.Emit(OpCodes.Newobj,ctor);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,module.ImportReference(rt));
                    if(route=="Pointer") {il.Emit(OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));il.Emit(OpCodes.Calli,site);}
                    else il.Emit(OpCodes.Call,target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        if(!oracle && !negative)
        {
            var cache=rt.NestedTypes.Single(t=>t.Name=="RuntimeTypeCache");
            var runtimeField=cache.Fields.Single(f=>f.Name=="m_runtimeType");var enclosingField=cache.Fields.Single(f=>f.Name=="m_enclosingType");
            var getRuntime=cache.Methods.Single(m=>m.Name=="GetRuntimeType");var enclosing=cache.Methods.Single(m=>m.Name=="GetEnclosingType");
            if(runtimeField.FieldType.Resolve()!=rt||enclosingField.FieldType.Resolve()!=rt||!getRuntime.HasBody||!enclosing.HasBody
                ||!getRuntime.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldfld&&i.Operand is FieldReference f&&f.Name==runtimeField.Name)
                ||!enclosing.Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.Name==definition.Name&&m.DeclaringType.FullName==owner.FullName))
                throw new InvalidOperationException("Isolated cache initialization contract changed");
            var method=checks.Methods.Single(m=>m.Name=="Cache");method.Body=new MethodBody(method){InitLocals=true};
            var local=new VariableDefinition(module.ImportReference(cache));method.Body.Variables.Add(local);var il=method.Body.GetILProcessor();
            var getType=module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle",new[]{typeof(RuntimeTypeHandle)})!);
            var allocate=module.ImportReference(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("GetUninitializedObject",new[]{typeof(Type)})!);
            il.Emit(OpCodes.Ldtoken,module.ImportReference(cache));il.Emit(OpCodes.Call,getType);il.Emit(OpCodes.Call,allocate);il.Emit(OpCodes.Castclass,module.ImportReference(cache));il.Emit(OpCodes.Stloc,local);
            il.Emit(OpCodes.Ldloc,local);il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,module.ImportReference(rt));il.Emit(OpCodes.Stfld,module.ImportReference(runtimeField));
            il.Emit(OpCodes.Ldloc,local);il.Emit(OpCodes.Callvirt,module.ImportReference(enclosing));il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ldloc,local);il.Emit(OpCodes.Callvirt,module.ImportReference(enclosing));il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Ceq);il.Emit(OpCodes.And);
            il.Emit(OpCodes.Ldloc,local);il.Emit(OpCodes.Ldfld,module.ImportReference(enclosingField));il.Emit(OpCodes.Ldtoken,module.TypeSystem.Void);il.Emit(OpCodes.Call,getType);il.Emit(OpCodes.Ceq);il.Emit(OpCodes.And);il.Emit(OpCodes.Ret);
        }
        if(args[0]=="--runtime-type-declaring-noalloc")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name=="Direct"));
        if(negative)
        {
            var entry=new MethodDefinition("MalformedDeclaring",MethodAttributes.Private|MethodAttributes.Static,module.TypeSystem.Void);checks.Methods.Add(entry);var il=entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name==(args[3].StartsWith("address-",StringComparison.Ordinal)?"Pointer":"Direct")));il.Emit(OpCodes.Pop);il.Emit(OpCodes.Ret);module.EntryPoint=entry;
        }
        else module.EntryPoint=checks.Methods.Single(m=>m.Name==(oracle?(args[0].EndsWith("nested-oracle",StringComparison.Ordinal)?"RunNestedOracle":"Run"):args[0].EndsWith("reflection",StringComparison.Ordinal)?"RunReflectionArtifact":"RunArtifact"));
        app.Write(args[1],new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("declaring-type: "+args[0]+"; private CLR is never executed");return true;
    }
}
