using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeCacheGenericDefinitionFixture
{
    public static bool Run(string[] args)
    {
        if(args.Length is not(3 or 4)||!args[0].StartsWith("--cache-generic-definition",StringComparison.Ordinal))return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);var rt=core.MainModule.GetType("System.RuntimeType");var cacheType=rt.NestedTypes.Single(t=>t.Name=="RuntimeTypeCache");
        var definition=cacheType.Methods.Single(m=>m.Name=="<GetGenericTypeDefinition>g__CacheGenericDefinition|50_0");
        var ownerField=cacheType.Fields.Single(f=>f.Name=="m_runtimeType");var resultField=cacheType.Fields.Single(f=>f.Name=="_genericTypeDefinition");
        var outer=cacheType.Methods.Single(m=>m.Name=="GetGenericTypeDefinition");
        if(core.Name.Name!="System.Private.CoreLib"||definition.Attributes!=(MethodAttributes.Private|MethodAttributes.HideBySig)||definition.ImplAttributes!=MethodImplAttributes.NoInlining
            ||!definition.HasBody||!definition.HasThis||definition.ExplicitThis||definition.HasGenericParameters||definition.Parameters.Count!=0||definition.CallingConvention!=MethodCallingConvention.Default||definition.ReturnType.Resolve()!=rt
            ||ownerField.Attributes!=(FieldAttributes.Private|FieldAttributes.InitOnly)||resultField.Attributes!=FieldAttributes.Private||ownerField.FieldType.Resolve()!=rt||resultField.FieldType.Resolve()!=rt
            ||!outer.HasBody||!outer.Body.Instructions.Any(i=>i.OpCode==OpCodes.Call&&i.Operand is MethodReference called&&called.Name==definition.Name))
            throw new InvalidOperationException("Cache generic definition contract changed");
        bool fieldNegative=args[0]=="--cache-generic-definition-field-negative";
        if(fieldNegative)
        {
            switch(args[3])
            {
                case "unchanged":break;
                case "owner-writable":ownerField.Attributes=FieldAttributes.Private;break;
                case "result-initonly":resultField.Attributes=FieldAttributes.Private|FieldAttributes.InitOnly;break;
                case "owner-modifier":ownerField.FieldType=new OptionalModifierType(core.MainModule.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)),ownerField.FieldType);break;
                case "result-type":resultField.FieldType=core.MainModule.ImportReference(typeof(Type));break;
                default:throw new InvalidOperationException("Unknown field negative");
            }
        }
        using var app=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters{InMemory=true});var module=app.MainModule;var checks=module.GetType("RuntimeTypeCacheGenericDefinitionSubset.DefinitionChecks");
        bool oracle=args[0]=="--cache-generic-definition-oracle",negative=args[0]=="--cache-generic-definition-negative";MethodReference target=module.ImportReference(definition);
        if(negative)
        {
            target=new MethodReference(target.Name,target.ReturnType,target.DeclaringType){HasThis=true};
            switch(args[3].Replace("address-","",StringComparison.Ordinal))
            {
                case "return-type":target.ReturnType=module.ImportReference(typeof(Type));break;
                case "static":target.HasThis=false;break;
                case "argument":target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));break;
                case "generic":target.GenericParameters.Add(new GenericParameter("T",target));break;
                case "byref":target.ReturnType=new ByReferenceType(target.ReturnType);break;
                case "modifier":target.ReturnType=new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)),target.ReturnType);break;
                default:throw new InvalidOperationException("Unknown negative");
            }
        }
        if(!oracle)
        {
            var getter=checks.NestedTypes.Single(t=>t.Name=="Getter");foreach(var method in getter.Methods.Where(m=>m.Name is "Invoke" or "EndInvoke"))method.ReturnType=module.ImportReference(rt);
            foreach(string name in new[]{"Direct","Virtual","Pointer","Address","Outer","Stored"})
            {
                var method=checks.Methods.Single(m=>m.Name==name);method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,module.ImportReference(cacheType));
                if(name=="Stored")il.Emit(OpCodes.Ldfld,module.ImportReference(resultField));
                else if(name=="Outer")il.Emit(OpCodes.Callvirt,module.ImportReference(outer));
                else if(name=="Address")
                {
                    il.Emit(OpCodes.Ldftn,target);var ctor=new MethodReference(".ctor",module.TypeSystem.Void,method.ReturnType){HasThis=true};ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));il.Emit(OpCodes.Newobj,ctor);
                }
                else if(name=="Pointer")
                {
                    il.Emit(OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);site.Parameters.Add(new ParameterDefinition(target.DeclaringType));il.Emit(OpCodes.Calli,site);
                }
                else il.Emit(name=="Virtual"?OpCodes.Callvirt:OpCodes.Call,target);
                il.Emit(OpCodes.Ret);
            }
            var create=checks.Methods.Single(m=>m.Name=="NewCache");create.Body=new MethodBody(create){InitLocals=true};var cache=new VariableDefinition(module.ImportReference(cacheType));create.Body.Variables.Add(cache);var ci=create.Body.GetILProcessor();
            ci.Emit(OpCodes.Ldtoken,module.ImportReference(cacheType));ci.Emit(OpCodes.Call,module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle",new[]{typeof(RuntimeTypeHandle)})!));
            ci.Emit(OpCodes.Call,module.ImportReference(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("GetUninitializedObject",new[]{typeof(Type)})!));ci.Emit(OpCodes.Castclass,module.ImportReference(cacheType));ci.Emit(OpCodes.Stloc,cache);
            ci.Emit(OpCodes.Ldloc,cache);ci.Emit(OpCodes.Ldarg_0);ci.Emit(OpCodes.Castclass,module.ImportReference(rt));ci.Emit(OpCodes.Stfld,module.ImportReference(ownerField));
            ci.Emit(OpCodes.Ldloc,cache);ci.Emit(OpCodes.Ldarg_1);ci.Emit(OpCodes.Castclass,module.ImportReference(rt));ci.Emit(OpCodes.Stfld,module.ImportReference(resultField));ci.Emit(OpCodes.Ldloc,cache);ci.Emit(OpCodes.Ret);
        }
        if(args[0]=="--cache-generic-definition-noalloc")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name=="Direct"));
        if(negative)
        {
            var entry=new MethodDefinition("MalformedCacheDefinition",MethodAttributes.Private|MethodAttributes.Static,module.TypeSystem.Void);checks.Methods.Add(entry);var il=entry.Body.GetILProcessor();il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name==(args[3].StartsWith("address-",StringComparison.Ordinal)?"Pointer":"Direct")));il.Emit(OpCodes.Pop);il.Emit(OpCodes.Ret);module.EntryPoint=entry;
        }
        else module.EntryPoint=checks.Methods.Single(m=>m.Name==(oracle?"Run":args[0]=="--cache-generic-definition-reflection"?"RunReflectionArtifact":"RunArtifact"));
        if(fieldNegative)
        {
            // Rewrite the compile-only copy as IL, not the host's ReadyToRun image.
            core.MainModule.Attributes |= ModuleAttributes.ILOnly;
            core.Write(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1]))!,"System.Private.CoreLib.dll"),new WriterParameters{Timestamp=0,DeterministicMvid=true});
        }
        app.Write(args[1],new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("cache-generic-definition: "+args[0]+"; genuine managed storage, native-only private calls");return true;
    }
}
