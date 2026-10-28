using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class CustomAttributeDataMemberGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if(args.Length!=3||args[0]!="--cad-member-guards")return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object obj,string name,object? value) {if(obj.GetType().GetField(name,fields) is {} f)f.SetValue(obj,value);else obj.GetType().GetProperty(name,fields)!.SetValue(obj,value);}
        object Get(object obj,string name)=>obj.GetType().GetField(name,fields) is {} f?f.GetValue(obj)!:obj.GetType().GetProperty(name,fields)!.GetValue(obj)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        Array ArrayOf(params object[] values) {var a=Array.CreateInstance(Model("TypeDesc"),values.Length);for(int i=0;i<values.Length;i++)a.SetValue(values[i],i);return a;}
        object ClassType(object cls) {var type=Make("TypeDesc");Set(type,"Kind",Enum.Parse(Model("TypeKind"),"Class"));Set(type,"Class",cls);return type;}
        object Context(params object[] types)=>Activator.CreateInstance(Model("GenericContext"),fields,null,new object[]{ArrayOf(types),ArrayOf()},null)!;
        var immutableRange=typeof(ImmutableArray).GetMethods().Single(m=>m.Name=="CreateRange"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1
            &&m.GetParameters()[0].ParameterType.IsGenericType&&m.GetParameters()[0].ParameterType.GetGenericTypeDefinition()==typeof(IEnumerable<>)).MakeGenericMethod(Model("TypeDesc"));
        object Signature(object result,object parameter,byte header=0,int generic=0)=>Activator.CreateInstance(typeof(MethodSignature<>).MakeGenericType(Model("TypeDesc")),new[]{(object)new SignatureHeader(header),result,1,generic,immutableRange.Invoke(null,new object[]{ArrayOf(parameter)})!})!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsCustomAttributeDataMember",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsCustomAttributeDataMemberSignature",statics)!.CreateDelegate<RawPredicate>();
        int checks=0;
        void Check(bool ok,string label) {if(!ok)throw new InvalidOperationException("CAD member guard "+label);checks++;}
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","000115120801120C1210",true),("aliases","0001151205011209120D",true),
            ("instance","200115120801120C1210",false),("generic","10010115120801120C1210",false),
            ("vararg","050115120801120C1210",false),("value-list","000115110801120C1210",false),
            ("value-element","000115120801110C1210",false),("value-member","000115120801120C1110",false),
            ("array-return","00011D120C1210",false),("extra-generic-arg","000115120802120C120C1210",false),
            ("modified-list","0001200815120801120C1210",false),("modified-element","000115120801200C120C1210",false),
            ("byref-member","000115120801120C101210",false),("trailing","000115120801120C121002",false),
            ("nil-list","000115120101120C1210",false),("nil-element","00011512080112021210",false),("nil-member","000115120801120C1203",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var listHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Interface|TypeAttributes.Abstract,md.GetOrAddString("System.Collections.Generic"),md.GetOrAddString("IList`1"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public,md.GetOrAddString("System.Reflection"),md.GetOrAddString("CustomAttributeData"),default,f,m);
            var memberHandle=md.AddTypeDefinition(TypeAttributes.Public,md.GetOrAddString("System.Reflection"),md.GetOrAddString("MemberInfo"),default,f,MetadataTokens.MethodDefinitionHandle(2));
            foreach(var pair in new[]{("System.Collections.Generic","IList`1"),("System.Reflection","CustomAttributeData"),("System.Reflection","MemberInfo")})
                md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString(pair.Item1),md.GetOrAddString(pair.Item2));
            var handle=md.AddMethodDefinition(MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig,MethodImplAttributes.IL,
                md.GetOrAddString("GetCustomAttributes"),md.GetOrAddBlob(Convert.FromHexString(vector.Hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var definition=reader.GetMethodDefinition(handle);Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) {var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);classes.Add(h,c);return c;}
            var list=Class(listHandle,"System.Collections.Generic.IList`1");var owner=Class(ownerHandle,"System.Reflection.CustomAttributeData");var member=Class(memberHandle,"System.Reflection.MemberInfo");
            object elementType=ClassType(owner),memberType=ClassType(member);
            if(vector.Label=="aliases")
            {
                var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
                var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
                var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
                var resolve=provider.GetType().GetMethod("GetTypeFromReference")!;
                object Resolve(int row)=>resolve.Invoke(provider,new object[]{reader,MetadataTokens.TypeReferenceHandle(row),(byte)0x12})!;
                Check(ReferenceEquals(Get(Resolve(1),"Class"),list),"list-alias-identity");
                elementType=Resolve(2);memberType=Resolve(3);
                Check(ReferenceEquals(Get(elementType,"Class"),owner)&&ReferenceEquals(Get(memberType,"Class"),member),"element-member-alias-identity");
            }
            // Generic construction is modeled; raw grammar and actual alias leaf resolution are tested independently.
            Set(list,"Context",Context(elementType));var signature=Signature(ClassType(list),memberType);
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","GetCustomAttributes");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Label=="exact")
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(list,"Module",Make("Module"));Reject(list,"Handle",ownerHandle);Reject(list,"Context",Context());Reject(list,"Context",Context(ClassType(member)));
                foreach(string sibling in new[]{"System.Reflection.Module","System.Reflection.Assembly","System.Reflection.ParameterInfo"})Reject(member,"_fullName",sibling);
                Reject(member,"Module",Make("Module"));Reject(owner,"_fullName","Application.CustomAttributeData");Reject(module,"AssemblyName","Application");
                Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining})Reject(method,"ImplAttributes",impl);
                Reject(method,"Signature",Signature(ClassType(list),memberType,0x20));Reject(method,"Signature",Signature(ClassType(list),memberType,0,1));
            }
        }
        // Read actual sibling MethodDefs; no public or private API is invoked here.
        using(var stream=File.OpenRead(args[2]))using(var pe=new PEReader(stream))
        {
            var reader=pe.GetMetadataReader();
            TypeDefinitionHandle Find(string ns,string name)=>reader.TypeDefinitions.Single(h=>reader.GetString(reader.GetTypeDefinition(h).Namespace)==ns&&reader.GetString(reader.GetTypeDefinition(h).Name)==name);
            var listHandle=Find("System.Collections.Generic","IList`1");var ownerHandle=Find("System.Reflection","CustomAttributeData");
            var actuals=typeof(System.Reflection.CustomAttributeData).GetMethods(BindingFlags.Public|BindingFlags.Static|BindingFlags.DeclaredOnly)
                .Where(m=>m.Name=="GetCustomAttributes"&&m.GetParameters().Length==1).ToArray();Check(actuals.Length==4,"actual-four-overloads");
            foreach(var actual in actuals)
            {
                var h=MetadataTokens.MethodDefinitionHandle(actual.MetadataToken&0xffffff);var d=reader.GetMethodDefinition(h);
                var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");
                object Class(TypeDefinitionHandle handle,string name) {var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",handle);Set(c,"_fullName",name);return c;}
                var owner=Class(ownerHandle,"System.Reflection.CustomAttributeData");var list=Class(listHandle,"System.Collections.Generic.IList`1");Set(list,"Context",Context(ClassType(owner)));
                string parameterName=actual.GetParameters()[0].ParameterType.FullName!;var parameter=Class(default,parameterName);
                var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",h);Set(method,"Name",reader.GetString(d.Name));Set(method,"Attributes",d.Attributes);Set(method,"ImplAttributes",d.ImplAttributes);Set(method,"Rva",d.RelativeVirtualAddress);
                Set(method,"Signature",Signature(ClassType(list),ClassType(parameter)));
                Check((bool)predicate.Invoke(null,new[]{method})! == (parameterName=="System.Reflection.MemberInfo"),"actual-overload-"+parameterName);
            }
        }
        Console.WriteLine("cad-member-raw-guards="+checks);return true;
    }
}
