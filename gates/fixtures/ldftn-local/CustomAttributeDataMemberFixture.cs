using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CustomAttributeDataMemberFixture
{
    public static bool Run(string[] args)
    {
        if(args.Length is not(3 or 4)||!args[0].StartsWith("--cad-member",StringComparison.Ordinal))return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);var owner=core.MainModule.GetType("System.Reflection.CustomAttributeData");
        var member=core.MainModule.GetType("System.Reflection.MemberInfo");
        var definition=owner.Methods.Single(m=>m.Name=="GetCustomAttributes"&&m.Parameters.Count==1&&m.Parameters[0].ParameterType.FullName==member.FullName);
        if(core.Name.Name!="System.Private.CoreLib"||definition.Attributes!=(MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig)
            ||definition.ImplAttributes!=MethodImplAttributes.IL||!definition.HasBody||definition.HasThis||definition.ExplicitThis||definition.HasGenericParameters
            ||definition.CallingConvention!=MethodCallingConvention.Default||definition.Parameters[0].ParameterType.Resolve()!=member
            ||definition.ReturnType is not GenericInstanceType result||result.ElementType.FullName!="System.Collections.Generic.IList`1"
            ||result.ElementType.Resolve().Module!=core.MainModule||result.GenericArguments.Count!=1||result.GenericArguments[0].Resolve()!=owner)
            throw new InvalidOperationException("CAD MemberInfo reader declaration changed");
        using var app=AssemblyDefinition.ReadAssembly(args[1],new ReaderParameters{InMemory=true});var module=app.MainModule;var checks=module.GetType("CustomAttributeDataMemberSubset.DataChecks");
        bool negative=args[0]=="--cad-member-negative",oracle=args[0].Contains("oracle",StringComparison.Ordinal);MethodReference target=module.ImportReference(definition);
        if(negative)
        {
            target=new MethodReference(target.Name,target.ReturnType,target.DeclaringType);target.Parameters.Add(new ParameterDefinition(module.ImportReference(member)));
            switch(args[3].Replace("address-","",StringComparison.Ordinal))
            {
                case "return-array":target.ReturnType=new ArrayType(module.ImportReference(owner));break;
                case "wrong-list": {var list=new GenericInstanceType(module.ImportReference(typeof(IReadOnlyList<>)));list.GenericArguments.Add(module.ImportReference(owner));target.ReturnType=list;break;}
                case "wrong-element": {var list=new GenericInstanceType(module.ImportReference(result.ElementType));list.GenericArguments.Add(module.TypeSystem.Object);target.ReturnType=list;break;}
                case "object":target.Parameters[0].ParameterType=module.TypeSystem.Object;break;
                case "byref":target.Parameters[0].ParameterType=new ByReferenceType(target.Parameters[0].ParameterType);break;
                case "instance":target.HasThis=true;break;
                case "generic":target.GenericParameters.Add(new GenericParameter("T",target));break;
                case "modifier":target.ReturnType=new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)),target.ReturnType);break;
                default:throw new InvalidOperationException("Unknown negative");
            }
        }
        if(!oracle)
        {
            // Native-only fixture: MemberInfo's empty base ctor has no retained route.
            // Keep the actual derived allocation/fields; public CLR oracle is unmodified.
            var baseBody = member.Methods.Single(m => m.IsConstructor && !m.IsStatic).Body.Instructions;
            var objectCtor = core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic);
            if (baseBody.Count != 3 || baseBody[0].OpCode != OpCodes.Ldarg_0
                || baseBody[1].OpCode != OpCodes.Call || baseBody[1].Operand is not MethodReference objectCall
                || objectCall.Resolve() != objectCtor || baseBody[2].OpCode != OpCodes.Ret
                || objectCtor.Body.Instructions.Count != 1 || objectCtor.Body.Instructions[0].OpCode != OpCodes.Ret)
                throw new InvalidOperationException("MemberInfo base constructor is not effect-free");
            var custom = checks.NestedTypes.Single(t => t.Name == "CustomMember");
            foreach (var instruction in custom.Methods.Single(m => m.IsConstructor).Body.Instructions)
                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference baseCtor
                    && baseCtor.Name == ".ctor" && baseCtor.DeclaringType.FullName == "System.Reflection.MemberInfo")
                { instruction.OpCode = OpCodes.Pop; instruction.Operand = null; }

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
                    il.Emit(OpCodes.Ldarg_0);
                    if(route=="Pointer") {il.Emit(OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));il.Emit(OpCodes.Calli,site);}
                    else il.Emit(OpCodes.Call,target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        if(args[0]=="--cad-member-noalloc")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name=="Direct"));
        if(negative)
        {
            var entry=new MethodDefinition("MalformedCadMember",MethodAttributes.Private|MethodAttributes.Static,module.TypeSystem.Void);checks.Methods.Add(entry);var il=entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull);il.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name==(args[3].StartsWith("address-",StringComparison.Ordinal)?"Pointer":"Direct")));il.Emit(OpCodes.Pop);il.Emit(OpCodes.Ret);module.EntryPoint=entry;
        }
        else module.EntryPoint=checks.Methods.Single(m=>m.Name==(oracle?(args[0]=="--cad-member-custom-oracle"?"RunCustomOracle":"Run"):args[0]=="--cad-member-reflection"?"RunReflectionArtifact":"RunArtifact"));
        app.Write(args[1],new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("cad-member: "+args[0]+"; exact public static reader");return true;
    }
}
