using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class EnumStaticIntegerFormatFixture
{
    internal static bool Run(string[] args)
    {
        if(args.Length!=3 || args[0] is not ("--enum-static-integer" or "--enum-static-integer-public" or "--enum-static-integer-noalloc")) return false;
        using var core=AssemblyDefinition.ReadAssembly(args[2]);
        if(core.Name.Name!="System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var owner=core.MainModule.GetType("System.Enum");
        MethodDefinition Target(int count)
        {
            var m=owner.Methods.Single(m=>m.Name=="ToString" && m.IsStatic && m.Parameters.Count==count);
            if(m.Attributes!=(MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig)
                || m.ImplAttributes!=MethodImplAttributes.NoInlining || !m.HasBody || m.HasThis || m.ExplicitThis
                || m.GenericParameters.Count!=2 || m.CallingConvention!=MethodCallingConvention.Generic
                || m.ReturnType.MetadataType!=MetadataType.String
                || m.Parameters[0].ParameterType is not TypeDefinition {FullName:"System.RuntimeType"} runtime || runtime.Module!=core.MainModule
                || count==3 && m.Parameters[1].ParameterType.MetadataType!=MetadataType.Char
                || m.Parameters[^1].ParameterType is not ByReferenceType {ElementType.MetadataType:MetadataType.Byte})
                throw new InvalidOperationException("Unexpected Enum static declaration");
            return m;
        }
        var plain=Target(2);var formatted=Target(3);
        string path=Path.GetFullPath(args[1]);
        using var app=AssemblyDefinition.ReadAssembly(path,new ReaderParameters {InMemory=true});
        var module=app.MainModule;var program=module.GetType("EnumStaticIntegerFormatSubset.Program");
        if(args[0]!="--enum-static-integer-public")
        {
            foreach(var (name,definition,pointer) in new[]{("Direct",plain,false),("Pointer",plain,true),("Format",formatted,false),("FormatPointer",formatted,true)})
            {
                var method=program.Methods.Single(m=>m.Name==name);
                if(method.GenericParameters.Count!=2 || method.Parameters.Count!=definition.Parameters.Count)
                    throw new InvalidOperationException("Unexpected formatter fixture signature");
                var target=new GenericInstanceMethod(module.ImportReference(definition));
                foreach(var generic in method.GenericParameters) target.GenericArguments.Add(generic);
                method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,target.Parameters[0].ParameterType);
                foreach(var parameter in method.Parameters.Skip(1)) il.Emit(OpCodes.Ldarg,parameter);
                if(pointer)
                {
                    il.Emit(OpCodes.Ldftn,target);var call=new CallSite(target.ReturnType);
                    foreach(var parameter in target.Parameters) call.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli,call);
                }
                else il.Emit(OpCodes.Call,target);
                il.Emit(OpCodes.Ret);
            }
        }
        if(args[0]=="--enum-static-integer-noalloc")
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,program.Methods.Single(m=>m.Name=="Direct"));
        module.EntryPoint=program.Methods.Single(m=>m.Name=="RunProbe");
        app.Write(path,new WriterParameters {Timestamp=0,DeterministicMvid=true});
        Console.WriteLine("enum-static-integer-fixture="+args[0]+":owned-two-generic-definitions,private-native-only");
        return true;
    }
}
