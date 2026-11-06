using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class RuntimeFieldsGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--runtime-fields-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleFields",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleFieldsSignature",statics)!.CreateDelegate<RawPredicate>();
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("GetFields guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
    new Mono.Cecil.AssemblyNameDefinition("FieldsGuardInput",new Version(1,0)),
    "FieldsGuardInput",Mono.Cecil.ModuleKind.Dll))
using(var actualCore=Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2])) {
    var sourceMethod=actualCore.MainModule.GetType("System.RuntimeTypeHandle").Methods.Single(m=>m.Name=="GetFields" && m.Parameters.Count==3 && m.Parameters[0].ParameterType.FullName=="System.RuntimeType");
    var import=empty.MainModule.ImportReference(sourceMethod);
    var host=new Mono.Cecil.TypeDefinition("Guard","AliasHost",Mono.Cecil.TypeAttributes.Public|Mono.Cecil.TypeAttributes.Class,empty.MainModule.TypeSystem.Object);
    empty.MainModule.Types.Add(host);
    var probe=new Mono.Cecil.MethodDefinition("AliasProbe",Mono.Cecil.MethodAttributes.Public|Mono.Cecil.MethodAttributes.Static,import.ReturnType);
    foreach(var parameter in import.Parameters)probe.Parameters.Add(new Mono.Cecil.ParameterDefinition(parameter.ParameterType));
    host.Methods.Add(probe);var il=probe.Body.GetILProcessor();
    il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_2);il.Emit(Mono.Cecil.Cil.OpCodes.Call,import);il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
    empty.Write(modelInput,new Mono.Cecil.WriterParameters{Timestamp=0,DeterministicMvid=true});
}
        var options=Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options,"Input",modelInput);
        Set(options,"UseILDiet",false);Set(options,"SharedGenerics",false);
        Set(options,"AutoRef",false);Set(options,"IsaSurfaceDump","predicate-guard-only");
        object loaded;
        try { loaded=Model("Compilation").GetMethod("Create",statics)!.Invoke(null,new object[]{new[]{modelInput,Path.GetFullPath(args[2])},options})!; }
        catch(TargetInvocationException e) when(e.InnerException is not null) { throw e.InnerException; }
        var realModule=((IEnumerable)Get(loaded,"Modules")).Cast<object>().Single(m=>(string)Get(m,"AssemblyName")=="System.Private.CoreLib");
        var realReader=(MetadataReader)Get(realModule,"Reader");
        var ownerHandleReal=realReader.TypeDefinitions.Single(h=> {
            var t=realReader.GetTypeDefinition(h);
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="RuntimeTypeHandle" && t.GetDeclaringType().IsNil;
        });
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="GetFields" && realReader.GetMethodDefinition(h).Attributes==(MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        var actualParameters=((IEnumerable)Get(Get(actual,"Signature"),"ParameterTypes")).Cast<object>().ToArray();
        var actualSpan=Get(actualParameters[1],"Class");
        void RejectActual(object obj,string field,object replacement,string label) {
            object saved=Get(obj,field);Set(obj,field,replacement);
            try {Check(!(bool)predicate.Invoke(null,new[]{actual})!,label);}
            finally {Set(obj,field,saved);}
        }
        RejectActual(actualSpan,"Module",Make("Module"),"Span-same-name-wrong-module");
        RejectActual(actualSpan,"IsValueType",false,"Span-class-not-value");
        RejectActual(actualSpan,"IsByRefLike",false,"Span-not-byref-like");
        RejectActual(actualSpan,"Handle",ownerHandleReal,"Span-wrong-template-handle");
        RejectActual(actual,"ImplAttributes",MethodImplAttributes.NoInlining,"wrong-implementation-flags");
        RejectActual(actual,"Attributes",MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig,"wrong-visibility");
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-model-restored");
        var spanShape=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeFieldsSpan",statics)!;
        Check((bool)spanShape.Invoke(null,new[]{actualParameters[1],realModule})!,"actual-Span-IntPtr-shape");
        var typeArgs=(Array)Get(Get(actualSpan,"Context"),"TypeArgs");object savedArg=typeArgs.GetValue(0)!;
        typeArgs.SetValue(Get(actualParameters[2],"Element"),0);
        try {Check(!(bool)spanShape.Invoke(null,new[]{actualParameters[1],realModule})!,"Span-Int32-not-IntPtr");}
        finally {typeArgs.SetValue(savedArg,0);}
        var readOnlySpanHandle=realReader.TypeDefinitions.Single(h=>{var t=realReader.GetTypeDefinition(h);return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="ReadOnlySpan`1";});
        RejectActual(actualSpan,"Handle",readOnlySpanHandle,"ReadOnlySpan-not-Span");
        var byrefSpan=Make("TypeDesc");Set(byrefSpan,"Kind",Enum.Parse(Model("TypeKind"),"ByRef"));Set(byrefSpan,"Element",actualParameters[1]);
        Check(!(bool)spanShape.Invoke(null,new[]{byrefSpan,realModule})!,"byref-Span-not-byvalue");
        var appModule=((IEnumerable)Get(loaded,"Modules")).Cast<object>().Single(m=>(string)Get(m,"AssemblyName")=="FieldsGuardInput");
        var appReader=(MetadataReader)Get(appModule,"Reader");
        var aliasHandle=appReader.MemberReferences.Single(h=>appReader.GetString(appReader.GetMemberReference(h).Name)=="GetFields");
        var aliasDefinition=appReader.GetMemberReference(aliasHandle);
        var aliasProvider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{loaded},null)!;
        var aliasDecoder=typeof(MemberReference).GetMethods().Single(m=>m.Name=="DecodeMethodSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        var aliasSignature=aliasDecoder.Invoke(aliasDefinition,new object?[]{aliasProvider,null})!;
        var aliasParameters=((IEnumerable)Get(aliasSignature,"ParameterTypes")).Cast<object>().ToArray();
        Check(raw(appReader.GetBlobReader(aliasDefinition.Signature)),"actual-MemberRef-raw-alias");
        Check(ReferenceEquals(Get(aliasParameters[0],"Class"),Get(actualParameters[0],"Class")),"actual-RuntimeType-TypeRef-alias");
        Check((bool)spanShape.Invoke(null,new[]{aliasParameters[1],realModule})!,"actual-Span-TypeRef-alias-authority");
        var aliasSpan=Get(aliasParameters[1],"Class");
        Check(ReferenceEquals(Get(aliasSpan,"Module"),realModule) && Equals(Get(aliasSpan,"Handle"),Get(actualSpan,"Handle")),"actual-Span-TypeRef-same-CoreLib-template");
        foreach(var h in realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Where(h=> {
            string name=realReader.GetString(realReader.GetMethodDefinition(h).Name);
            return (name=="GetFields" || name=="<GetFields>g____PInvoke|65_0") && h!=actualHandle;
        })) Check(!(bool)predicate.Invoke(null,new[]{((IDictionary)Get(realModule,"MethodMap"))[h]!})!,"actual-marshaller-and-QCall-excluded");
        Console.WriteLine("runtime-fields-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        // Raw aliases are independent nonzero TypeDefOrRef encodings; actual model above
        // proves the Span template and IntPtr instantiation authority, not just shape.
        var vectors=new(string Hex,bool Expected)[]{
            ("000302120815110C01181008",true),("000302120915110D01181008",true),
            ("200302120815110C01181008",false),("10010302120815110C01181008",false),
            ("000308120815110C01181008",false),("000302110815110C01181008",false),
            ("000302120815120C01181008",false),("000302120815110C01081008",false),
            ("00030212081015110C01181008",false),("0003021208200815110C01181008",false),
            ("000302120815110C011808",false),("000302120815110C01181009",false),
            ("000302120815110C0118100800",false),("000302120015110C01181008",false),
            ("000302120815110001181008",false)};
        foreach(var vector in vectors) {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("RawFields"),md.GetOrAddGuid(Guid.Empty),default,default);
            var blob=md.GetOrAddBlob(Convert.FromHexString(vector.Hex));var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(raw(source.GetMetadataReader().GetBlobReader(blob))==vector.Expected,"raw-"+vector.Hex);
        }
        Console.WriteLine("runtime-fields-raw-guards="+count);return true;
    }
}
