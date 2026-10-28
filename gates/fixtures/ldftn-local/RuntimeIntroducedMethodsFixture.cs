using Mono.Cecil;
using Mono.Cecil.Cil;
internal static class RuntimeIntroducedMethodsFixture
{
    public static bool Run(string[] args)
    {
        bool negative=args.Length==4 && args[0]=="--introduced-methods-negative";
        if(!negative && (args.Length!=3 || !new[]{"--introduced-methods","--introduced-methods-reflection","--introduced-methods-noalloc","--introduced-methods-oracle"}.Contains(args[0])))return false;
        System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture=System.Globalization.CultureInfo.InvariantCulture;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);
        var owner=core.MainModule.GetType("System.RuntimeTypeHandle");
        var factory=owner.Methods.Single(m=>m.Name=="GetIntroducedMethods");
        var enumerator=owner.NestedTypes.Single(t=>t.Name=="IntroducedMethodEnumerator");
        var ctor=enumerator.Methods.Single(m=>m.IsConstructor && !m.IsStatic);
        if((int)factory.Attributes!=0x93 || (int)factory.ImplAttributes!=0 || factory.HasGenericParameters || factory.Parameters.Count!=1
            || factory.Parameters[0].ParameterType.FullName!="System.RuntimeType" || factory.ReturnType.Resolve()!=enumerator || !enumerator.IsValueType
            || factory.Body.Instructions.Count!=3 || factory.Body.Instructions[0].OpCode!=OpCodes.Ldarg_0
            || factory.Body.Instructions[1].OpCode!=OpCodes.Newobj || ((MethodReference)factory.Body.Instructions[1].Operand).Resolve()!=ctor
            || factory.Body.Instructions[2].OpCode!=OpCodes.Ret)throw new InvalidOperationException("Introduced factory metadata/IL changed");
        string path=Path.GetFullPath(args[1]);using var app=AssemblyDefinition.ReadAssembly(path,new ReaderParameters{InMemory=true});
        var mod=app.MainModule;var program=mod.GetType("RuntimeIntroducedMethodsSubset.Program");
        MethodDefinition Method(string name)=>program.Methods.Single(m=>m.Name==name);
        if(args[0]=="--introduced-methods-oracle")
        {
            var entry=Method("RunArtifactChecks");entry.Body=new MethodBody(entry);var il=entry.Body.GetILProcessor();il.Emit(OpCodes.Call,Method("Run"));il.Emit(OpCodes.Ret);mod.EntryPoint=entry;
        }
        else
        {
            var target=mod.ImportReference(factory);var value=mod.ImportReference(enumerator);var runtimeType=mod.ImportReference(factory.Parameters[0].ParameterType);
            var del=program.NestedTypes.Single(t=>t.Name=="Factory");var invoke=del.Methods.Single(m=>m.Name=="Invoke");
            invoke.ReturnType=value;invoke.Parameters[0].ParameterType=runtimeType;
            foreach(var member in del.Methods)
            {
                if(member.Name=="EndInvoke")member.ReturnType=value;
                if(member.Name=="BeginInvoke")member.Parameters[0].ParameterType=runtimeType;
            }
            // Exact typed delegate signature, with explicit boxing only in its object-returning caller.
            var delegateCtor=del.Methods.Single(m=>m.IsConstructor);
            var address=Method("Address");address.Body=new MethodBody(address);var ai=address.Body.GetILProcessor();ai.Emit(OpCodes.Ldnull);ai.Emit(OpCodes.Ldftn,target);ai.Emit(OpCodes.Newobj,delegateCtor);ai.Emit(OpCodes.Ret);
            foreach(string name in new[]{"Direct","Pointer","DelegateRoute","Discard"})
            {
                var stub=Method(name);stub.Body=new MethodBody(stub);var il=stub.Body.GetILProcessor();
                if(name=="DelegateRoute")il.Emit(OpCodes.Call,address);
                il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,runtimeType);
                if(name=="Pointer") {il.Emit(OpCodes.Ldftn,target);var site=new CallSite(value);site.Parameters.Add(new ParameterDefinition(runtimeType));il.Emit(OpCodes.Calli,site);}
                else if(name=="DelegateRoute")il.Emit(OpCodes.Callvirt,invoke);
                else il.Emit(OpCodes.Call,target);
                if(name=="Discard")il.Emit(OpCodes.Pop);else il.Emit(OpCodes.Box,value);
                il.Emit(OpCodes.Ret);
            }
            if(negative)
            {
                string mode=args[3];bool addressMode=mode.StartsWith("address-",StringComparison.Ordinal);
                if(addressMode)mode=mode.Substring(8);
                var malformed=new MethodReference(target.Name,target.ReturnType,target.DeclaringType){HasThis=false};
                malformed.Parameters.Add(new ParameterDefinition(runtimeType));
                switch(mode)
                {
                    case "class-return":
                        malformed.ReturnType=new TypeReference(value.Namespace,value.Name,mod,value.Scope,false){DeclaringType=value.DeclaringType};break;
                    case "object-return":malformed.ReturnType=mod.TypeSystem.Object;break;
                    case "byref-return":malformed.ReturnType=new ByReferenceType(value);break;
                    case "modifier":malformed.ReturnType=new OptionalModifierType(mod.TypeSystem.Object,value);break;
                    case "argument":malformed.Parameters[0].ParameterType=mod.TypeSystem.Object;break;
                    case "instance":malformed.HasThis=true;break;
                    case "generic":malformed.GenericParameters.Add(new GenericParameter("T",malformed));break;
                    default:throw new InvalidOperationException("unknown negative "+mode);
                }
                var body=Method(addressMode?"Address":"Direct").Body;
                var call=body.Instructions.Single(i=>i.OpCode==(addressMode?OpCodes.Ldftn:OpCodes.Call));
                call.Operand=malformed;
            }
            if(args[0]=="--introduced-methods-noalloc")
            {
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(mod,Method("Discard"));
            }
            mod.EntryPoint=Method(args[0]=="--introduced-methods-reflection"?"RunReflectionChecks":"RunArtifactChecks");
        }
        app.Write(path,new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("introduced-factory-fixture:exact-valuetype-return");return true;
    }
}
