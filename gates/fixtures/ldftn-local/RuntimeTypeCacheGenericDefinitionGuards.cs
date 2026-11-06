using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeCacheGenericDefinitionGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--cache-generic-definition-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeCacheGenericDefinition",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeCacheGenericDefinitionSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("<GetGenericTypeDefinition>g__CacheGenericDefinition|50_0 guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
        using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("CacheDefinitionGuardInput",new Version(1,0)),
            "CacheDefinitionGuardInput",Mono.Cecil.ModuleKind.Dll))
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
        var runtimeType=realReader.TypeDefinitions.Single(h=> {
            var t=realReader.GetTypeDefinition(h);
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="RuntimeType" && t.GetDeclaringType().IsNil;
        });
        var cacheType=realReader.GetTypeDefinition(runtimeType).GetNestedTypes().Single(h=>realReader.GetString(realReader.GetTypeDefinition(h).Name)=="RuntimeTypeCache");
        var actualHandle=realReader.GetTypeDefinition(cacheType).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="<GetGenericTypeDefinition>g__CacheGenericDefinition|50_0");
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        var actualOwner=Get(actual,"DeclaringClass");
        Check((string)Get(actualOwner,"FullName")=="RuntimeTypeCache","actual-model-simple-nested-name");
        Check(realReader.GetTypeDefinition(cacheType).GetDeclaringType()==runtimeType,"actual-model-nested-parent");
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        var descriptor=Model("CoreIntrinsics").GetField("BrRuntimeTypeCacheGenericDefinition",statics)!.GetValue(null)!;
        Check((bool)descriptor.GetType().GetMethod("Matches",new[]{Model("MethodInfo")})!.Invoke(descriptor,new[]{actual})!,"actual-CoreLib-MethodDef-descriptor");
        Console.WriteLine("cache-generic-definition-actual-model="+Get(actualOwner,"FullName")+":"+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","20001208",true),("wrong-parent","20001208",false),("alias","20001205",true),("alias-second","20001209",true),("static","00001208",false),
            ("explicit","60001208",false),("vararg","25001208",false),("generic","3001001208",false),
            ("object","20001C",false),("value-return","20001108",false),("byref","2000101208",false),
            ("modified","200020081208",false),("argument","2001120808",false),("trailing","2000120808",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,default,md.GetOrAddString("RuntimeTypeCache"),default,f,m);
            var otherParent=md.AddTypeDefinition(TypeAttributes.NotPublic,md.GetOrAddString("System"),md.GetOrAddString("OtherRuntimeType"),default,f,MetadataTokens.MethodDefinitionHandle(2));
            md.AddNestedType(ownerHandle,vector.Label=="wrong-parent"?otherParent:enumHandle);
            // Two distinct metadata rows resolve through the ordinary module TypeIndex to one definition.
            var aliasOne=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"));
            var aliasTwo=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"));
            Check(aliasOne!=aliasTwo,"distinct-TypeRef-rows");
            var handle=md.AddMethodDefinition(MethodAttributes.Private|MethodAttributes.HideBySig,MethodImplAttributes.NoInlining,md.GetOrAddString("<GetGenericTypeDefinition>g__CacheGenericDefinition|50_0"),md.GetOrAddBlob(Convert.FromHexString(vector.Hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.RuntimeType");
            var owner=Class(ownerHandle,"RuntimeTypeCache");
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;
            modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            if(vector.Label.StartsWith("alias",StringComparison.Ordinal))
            {
                var result=Get(signature,"ReturnType");
                Check(ReferenceEquals(Get(result,"Class"),enumClass),"RuntimeType-alias-resolves-to-same-ClassInfo");
                Check(reader.TypeReferences.Count==2,"two-actual-TypeRef-rows");
            }
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","<GetGenericTypeDefinition>g__CacheGenericDefinition|50_0");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==(vector.Expected || vector.Label=="wrong-parent"),"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(enumClass,"_fullName","System.Type");
                Reject(enumClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.RuntimeTypeCache");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Public|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.IL,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        foreach(string hex in new[]{"20001200","20001201","20001202","20001203"})
        {
            var metadata=new MetadataBuilder();metadata.AddModule(0,metadata.GetOrAddString("Raw"),metadata.GetOrAddGuid(Guid.Empty),default,default);
            var blob=metadata.GetOrAddBlob(Convert.FromHexString(hex));var image=new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(!raw(source.GetMetadataReader().GetBlobReader(blob)),"invalid-coded-token-"+hex);
        }
        Console.WriteLine("cache-generic-definition-raw-guards="+count);return true;
    }
}
