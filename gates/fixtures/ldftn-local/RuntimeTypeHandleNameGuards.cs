using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class RuntimeTypeHandleNameGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-type-handle-name-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleConstructName",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsRuntimeTypeHandleConstructNameSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("ConstructName guard "+label);count++; }
        var vectors=new(string Label,string Hex,bool Expected)[] {
            ("exact","20010E1108",true),("static","00010E1108",false),("explicit","60010E1108",false),
            ("vararg","25010E1108",false),("generic","3001010E1108",false),("wrong-return","20011C1108",false),
            ("class-enum","20010E1208",false),("primitive","20010E08",false),("byref","20010E101108",false),
            ("modified","20010E20081108",false),("return-modified","200120080E1108",false),
            ("extra","20020E110808",false),("trailing","20010E110808",false) };
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("TypeNameFormatFlags"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("RuntimeTypeHandle"),default,f,m);
            var handle=md.AddMethodDefinition(MethodAttributes.Assembly|MethodAttributes.HideBySig,MethodImplAttributes.IL,md.GetOrAddString("ConstructName"),md.GetOrAddBlob(Convert.FromHexString(vector.Hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.TypeNameFormatFlags");Set(enumClass,"IsEnum",true);Set(enumClass,"EnumUnderlying",PrimitiveTypeCode.Int32);
            var owner=Class(ownerHandle,"System.RuntimeTypeHandle");
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","ConstructName");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(enumClass,"_fullName","System.Reflection.BindingFlags");Reject(enumClass,"IsEnum",false);Reject(enumClass,"EnumUnderlying",PrimitiveTypeCode.Byte);
                Reject(enumClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.RuntimeTypeHandle");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Public|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        using(var stream=File.OpenRead(args[2]))using(var pe=new PEReader(stream))
        {
            var reader=pe.GetMetadataReader();var owner=reader.TypeDefinitions.Single(h=>reader.GetString(reader.GetTypeDefinition(h).Name)=="RuntimeTypeHandle"&&reader.GetString(reader.GetTypeDefinition(h).Namespace)=="System");
            var h=reader.GetTypeDefinition(owner).GetMethods().Single(h=>reader.GetString(reader.GetMethodDefinition(h).Name)=="ConstructName"&&(reader.GetMethodDefinition(h).Attributes&MethodAttributes.PinvokeImpl)!=0);
            var d=reader.GetMethodDefinition(h);Check(!raw(reader.GetBlobReader(d.Signature)),"raw-QCall-sibling");
            var module=Make("Module");Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Reader",reader);
            var cls=Make("ClassInfo");Set(cls,"Module",module);Set(cls,"_fullName","System.RuntimeTypeHandle");var method=Make("MethodInfo");
            Set(method,"Module",module);Set(method,"DeclaringClass",cls);Set(method,"Handle",h);Set(method,"Name","ConstructName");Set(method,"Attributes",d.Attributes);Set(method,"ImplAttributes",d.ImplAttributes);Set(method,"Rva",d.RelativeVirtualAddress);
            Check(!(bool)predicate.Invoke(null,new[]{method})!,"raw-QCall-definition");
        }
        Console.WriteLine("type-name-raw-guards="+count);return true;
    }
}
