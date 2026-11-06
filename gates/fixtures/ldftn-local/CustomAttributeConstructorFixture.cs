using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CustomAttributeConstructorFixture
{
    public static bool Run(string[] args)
    {
        if(args.Length is not(3 or 4)||!args[0].StartsWith("--cad-constructor",StringComparison.Ordinal))return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);var owner=core.MainModule.GetType("System.Reflection.CustomAttributeData");
        var definition=owner.Methods.Single(m=>m.Name=="get_Constructor");
        if(core.Name.Name!="System.Private.CoreLib"||definition.Attributes!=(MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.NewSlot|MethodAttributes.HideBySig|MethodAttributes.SpecialName)
            ||definition.ImplAttributes!=MethodImplAttributes.IL||!definition.HasBody||!definition.HasThis||definition.ExplicitThis||definition.HasGenericParameters
            ||definition.CallingConvention!=MethodCallingConvention.Default||definition.Parameters.Count!=0||definition.ReturnType.FullName!="System.Reflection.ConstructorInfo"
            ||definition.ReturnType.Resolve().Module!=core.MainModule||definition.Body.Instructions.Count!=2||definition.Body.Instructions[0].OpCode!=OpCodes.Ldnull||definition.Body.Instructions[1].OpCode!=OpCodes.Ret)
            throw new InvalidOperationException("CAD base constructor getter changed");
        using var app=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters{InMemory=true});var module=app.MainModule;var checks=module.GetType("CustomAttributeConstructorSubset.ConstructorChecks");
        bool native=!args[0].Contains("oracle",StringComparison.Ordinal),negative=args[0]=="--cad-constructor-negative";MethodReference target=module.ImportReference(definition);
        if(negative)
        {
            target=new MethodReference(target.Name,target.ReturnType,target.DeclaringType){HasThis=true};
            switch(args[3].Replace("address-","",StringComparison.Ordinal))
            {
                case "return-member":target.ReturnType=module.ImportReference(typeof(System.Reflection.MemberInfo));break;
                case "return-object":target.ReturnType=module.TypeSystem.Object;break;
                case "static":target.HasThis=false;break;
                case "argument":target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));break;
                case "generic":target.GenericParameters.Add(new GenericParameter("T",target));break;
                case "modifier":target.ReturnType=new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)),target.ReturnType);break;
                default:throw new InvalidOperationException("Unknown negative");
            }
        }
        foreach(string route in new[]{"BaseDirect","BasePointer","BaseAddress","Virtual","VirtualPointer","VirtualAddress"})
        {
            var method=checks.Methods.Single(m=>m.Name==route);method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();bool virt=route.StartsWith("Virtual",StringComparison.Ordinal);
            il.Emit(OpCodes.Ldarg_0);
            if(route.EndsWith("Address",StringComparison.Ordinal))
            {
                if(virt)il.Emit(OpCodes.Dup);il.Emit(virt?OpCodes.Ldvirtftn:OpCodes.Ldftn,target);
                var ctor=new MethodReference(".ctor",module.TypeSystem.Void,method.ReturnType){HasThis=true};ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));il.Emit(OpCodes.Newobj,ctor);
            }
            else if(route.EndsWith("Pointer",StringComparison.Ordinal))
            {
                if(virt)il.Emit(OpCodes.Dup);il.Emit(virt?OpCodes.Ldvirtftn:OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);site.Parameters.Add(new ParameterDefinition(target.DeclaringType));il.Emit(OpCodes.Calli,site);
            }
            else il.Emit(virt?OpCodes.Callvirt:OpCodes.Call,target);
            il.Emit(OpCodes.Ret);
        }
        // ldvirtftn selects once; calli may use another assignment-compatible receiver.
        var rebound=checks.Methods.Single(m=>m.Name=="ReboundPointer");rebound.Body=new MethodBody(rebound);
        var reboundIl=rebound.Body.GetILProcessor();reboundIl.Emit(OpCodes.Ldarg_1);reboundIl.Emit(OpCodes.Ldarg_0);reboundIl.Emit(OpCodes.Ldvirtftn,target);
        var reboundSite=new CallSite(target.ReturnType);reboundSite.Parameters.Add(new ParameterDefinition(target.DeclaringType));reboundIl.Emit(OpCodes.Calli,reboundSite);reboundIl.Emit(OpCodes.Ret);
        var function=checks.Methods.Single(m=>m.Name=="BaseFunction");function.Body=new MethodBody(function);
        var functionIl=function.Body.GetILProcessor();functionIl.Emit(OpCodes.Ldftn,target);functionIl.Emit(OpCodes.Conv_I);functionIl.Emit(OpCodes.Ret);
        if(native)
        {
            // Native opaque CAD has no public property table. Obtain the same exact
            // base MethodInfo from a real nonvirtual delegate, without invoking it.
            var getter=checks.Methods.Single(m=>m.Name=="Getter");getter.Body=new MethodBody(getter);
            var getterIl=getter.Body.GetILProcessor();
            getterIl.Emit(OpCodes.Newobj,checks.NestedTypes.Single(t=>t.Name=="BaseData").Methods.Single(m=>m.IsConstructor));
            getterIl.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name=="BaseAddress"));
            getterIl.Emit(OpCodes.Callvirt,module.ImportReference(typeof(Delegate).GetProperty("Method")!.GetMethod!));getterIl.Emit(OpCodes.Ret);
            var ctor=owner.Methods.Single(m=>m.IsConstructor&&!m.IsStatic&&m.Parameters.Count==0);var body=ctor.Body.Instructions;
            if(!ctor.IsFamily||body.Count!=3||body[0].OpCode!=OpCodes.Ldarg_0||body[1].OpCode!=OpCodes.Call||body[1].Operand is not MethodReference objectCtor
                ||objectCtor.DeclaringType.FullName!="System.Object"||objectCtor.Name!=".ctor"||objectCtor.Parameters.Count!=0||body[2].OpCode!=OpCodes.Ret)
                throw new InvalidOperationException("CAD protected constructor is not effect-free Object chain");
            var objectBody=objectCtor.Resolve().Body.Instructions;
            if(objectBody.Count!=1||objectBody[0].OpCode!=OpCodes.Ret)
                throw new InvalidOperationException("Object constructor is not ret-only");
            foreach(var type in checks.NestedTypes.Where(t=>t.Name is "BaseData" or "OverrideData"))
            foreach(var method in type.Methods.Where(m=>m.IsConstructor&&m.HasBody))
            foreach(var instruction in method.Body.Instructions)
                if(instruction.OpCode==OpCodes.Call&&instruction.Operand is MethodReference called&&called.DeclaringType.FullName==owner.FullName&&called.Name==".ctor")
                    instruction.Operand=module.ImportReference(objectCtor);
        }
        if(args[0]=="--cad-constructor-noalloc-base")foreach(string name in new[]{"BaseDirect","BaseFunction"})RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name==name));
        if(args[0]=="--cad-constructor-noalloc-virtual")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name=="Virtual"));
        if(negative)
        {
            var entry=new MethodDefinition("MalformedCadConstructor",MethodAttributes.Private|MethodAttributes.Static,module.TypeSystem.Void);checks.Methods.Add(entry);var il=entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name==(args[3].StartsWith("address-",StringComparison.Ordinal)?"VirtualPointer":"Virtual")));il.Emit(OpCodes.Pop);il.Emit(OpCodes.Ret);module.EntryPoint=entry;
        }
        else module.EntryPoint=checks.Methods.Single(m=>m.Name==(args[0]=="--cad-constructor-oracle"?"RunConstructorOracle":args[0]=="--cad-constructor-base-oracle"?"RunBaseOracle":args[0]=="--cad-constructor-public-oracle"?"Run":args[0]=="--cad-constructor-reflection"?"RunReflectionArtifact":"RunArtifact"));
        app.Write(args[1],new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("cad-constructor: "+args[0]+"; exact public base getter call kinds");return true;
    }
}
