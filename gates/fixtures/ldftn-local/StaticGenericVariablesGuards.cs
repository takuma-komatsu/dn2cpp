using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class StaticGenericVariablesGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--static-generic-variables-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleContainsGenericVariablesStatic",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleContainsGenericVariablesStaticSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("ContainsGenericVariables guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
        using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("StaticGenericGuardInput",new Version(1,0)),
            "StaticGenericGuardInput",Mono.Cecil.ModuleKind.Dll))
            empty.Write(modelInput,new Mono.Cecil.WriterParameters{Timestamp=0,DeterministicMvid=true});
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
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="ContainsGenericVariables" && (realReader.GetMethodDefinition(h).Attributes & MethodAttributes.Static)!=0);
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        var rawSibling=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="ContainsGenericVariables" && (realReader.GetMethodDefinition(h).Attributes & MethodAttributes.Static)==0);
        Check(!(bool)predicate.Invoke(null,new[]{((IDictionary)Get(realModule,"MethodMap"))[rawSibling]!})!,"actual-instance-sibling-excluded");
        Console.WriteLine("static-generic-variables-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","0001021208",true),("alias","0001021209",true),
            ("instance","2001021208",false),("generic","100101021208",false),
            ("int-return","0001081208",false),("named-bool","0001110C1208",false),
            ("argument-value","0001021108",false),("argument-byref","000102101208",false),
            ("modifier","000102200C1208",false),("return-modifier","0001200C021208",false),
            ("noargument","000002",false),("trailing","000102120800",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var runtimeHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Abstract,md.GetOrAddString("System"),md.GetOrAddString("Type"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("RuntimeTypeHandle"),default,f,m);
            var otherParent=md.AddTypeDefinition(TypeAttributes.NotPublic,md.GetOrAddString("System"),md.GetOrAddString("OtherHandle"),default,f,MetadataTokens.MethodDefinitionHandle(2));
            var ownerRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("RuntimeTypeHandle"));
            var runtimeRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"));
            var enumRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("Type"));
            string hex=vector.Hex;
            var handle=md.AddMethodDefinition(MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig,MethodImplAttributes.InternalCall,md.GetOrAddString("ContainsGenericVariables"),md.GetOrAddBlob(Convert.FromHexString(hex)),0,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.Type");
            var runtimeClass=Class(runtimeHandle,"System.RuntimeType");
            var owner=Class(ownerHandle,"System.RuntimeTypeHandle");Set(owner,"IsValueType",true);
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;
            modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            if(vector.Label.StartsWith("alias",StringComparison.Ordinal))
            {
                var result=((IEnumerable)Get(signature,"ParameterTypes")).Cast<object>().Single();
                Check(ReferenceEquals(Get(result,"Class"),runtimeClass),"RuntimeType-alias-resolves-to-same-ClassInfo");
                Check(reader.TypeReferences.Count==3,"three-actual-TypeRef-rows");
            }
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","ContainsGenericVariables");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",0);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(runtimeClass,"_fullName","System.Type");
                Reject(runtimeClass,"IsValueType",true);
                Reject(owner,"Module",Make("Module"));
                Reject(method,"Handle",default(MethodDefinitionHandle));
                Reject(method,"Attributes",MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig);
                Reject(method,"Attributes",MethodAttributes.Assembly|MethodAttributes.HideBySig);
                Reject(runtimeClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.RuntimeTypeHandle");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",1);Reject(method,"Attributes",MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.IL,MethodImplAttributes.InternalCall|MethodImplAttributes.NoInlining,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        foreach(string hex in new[]{"0001021200","0001021201","0001021202","0001021203"})
        {
            var metadata=new MetadataBuilder();metadata.AddModule(0,metadata.GetOrAddString("Raw"),metadata.GetOrAddGuid(Guid.Empty),default,default);
            var blob=metadata.GetOrAddBlob(Convert.FromHexString(hex));var image=new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(!raw(source.GetMetadataReader().GetBlobReader(blob)),"invalid-coded-token-"+hex);
        }
        Console.WriteLine("static-generic-variables-raw-guards="+count);return true;
    }
}
