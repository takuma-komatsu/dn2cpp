using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class CustomAttributeConstructorGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--cad-constructor-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsCustomAttributeDataConstructorBase",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsCustomAttributeDataConstructorSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("get_Constructor guard "+label);count++; }
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","20001208",true),("alias","20001205",true),("alias-second","20001209",true),("static","00001208",false),
            ("explicit","60001208",false),("vararg","25001208",false),("generic","3001001208",false),
            ("object","20001C",false),("value-return","20001108",false),("byref","2000101208",false),
            ("modified","200020081208",false),("argument","2001120808",false),("trailing","2000120808",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System.Reflection"),md.GetOrAddString("ConstructorInfo"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,md.GetOrAddString("System.Reflection"),md.GetOrAddString("CustomAttributeData"),default,f,m);
            // Two distinct metadata rows resolve through the ordinary module TypeIndex to one definition.
            var aliasOne=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System.Reflection"),md.GetOrAddString("ConstructorInfo"));
            var aliasTwo=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System.Reflection"),md.GetOrAddString("ConstructorInfo"));
            Check(aliasOne!=aliasTwo,"distinct-TypeRef-rows");
            var handle=md.AddMethodDefinition(MethodAttributes.Public|MethodAttributes.Virtual|MethodAttributes.NewSlot|MethodAttributes.HideBySig|MethodAttributes.SpecialName,MethodImplAttributes.IL,md.GetOrAddString("get_Constructor"),md.GetOrAddBlob(Convert.FromHexString(vector.Hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.Reflection.ConstructorInfo");
            var owner=Class(ownerHandle,"System.Reflection.CustomAttributeData");
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;
            modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            if(vector.Label.StartsWith("alias",StringComparison.Ordinal))
            {
                var result=Get(signature,"ReturnType");
                Check(ReferenceEquals(Get(result,"Class"),enumClass),"ConstructorInfo-alias-resolves-to-same-ClassInfo");
                Check(reader.TypeReferences.Count==2,"two-actual-TypeRef-rows");
            }
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","get_Constructor");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(enumClass,"_fullName","System.Reflection.MemberInfo");
                Reject(enumClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.CustomAttributeData");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Public|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        foreach(string hex in new[]{"20001200","20001201","20001202","20001203"})
        {
            var metadata=new MetadataBuilder();metadata.AddModule(0,metadata.GetOrAddString("Raw"),metadata.GetOrAddGuid(Guid.Empty),default,default);
            var blob=metadata.GetOrAddBlob(Convert.FromHexString(hex));var image=new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(!raw(source.GetMetadataReader().GetBlobReader(blob)),"invalid-coded-token-"+hex);
        }
        Console.WriteLine("cad-constructor-raw-guards="+count);return true;
    }
}
