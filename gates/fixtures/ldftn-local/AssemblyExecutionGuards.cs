using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class AssemblyExecutionGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--assembly-execution-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsAppDomainExecuteAssembly",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsAppDomainExecuteAssemblySignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("ExecuteAssembly guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("AssemblyExecutionGuardInput", new Version(1, 0)),
            "AssemblyExecutionGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var publicOverload = core.MainModule.GetType("System.AppDomain").Methods.Single(m =>
                m.Name == "ExecuteAssembly" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 2
                && m.Parameters[0].ParameterType.FullName == "System.String"
                && m.Parameters[1].ParameterType.FullName == "System.String[]");
            var probe = new Mono.Cecil.MethodDefinition("PublicSameArity", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, empty.MainModule.TypeSystem.Int32);
            var host = new Mono.Cecil.TypeDefinition("Guard", "PublicOverload", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            host.Methods.Add(probe);
            var il = probe.Body.GetILProcessor();
            il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
            il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
            il.Emit(Mono.Cecil.Cil.OpCodes.Ldnull);
            il.Emit(Mono.Cecil.Cil.OpCodes.Callvirt, empty.MainModule.ImportReference(publicOverload));
            il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            empty.Write(modelInput, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
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
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="AppDomain" && t.GetDeclaringType().IsNil;
        });
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="ExecuteAssembly" && realReader.GetMethodDefinition(h).Attributes==(MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>()
            .Single(m => (string)Get(m, "AssemblyName") == "AssemblyExecutionGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var publicRef = appReader.MemberReferences.Single(h => appReader.GetString(appReader.GetMemberReference(h).Name) == "ExecuteAssembly");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", fields)!
            .Invoke(loaded, new object[] { appModule, publicRef, context })!;
        Check((MethodAttributes)Get(resolved, "Attributes") == (MethodAttributes.Public | MethodAttributes.HideBySig), "public-same-arity-MemberRef-resolved");
        Check(!(bool)predicate.Invoke(null, new[] { resolved })!, "public-same-arity-MemberRef-not-private-adapter");
        Console.WriteLine("assembly-execution-public-MemberRef:resolved-not-intercepted");

        // ResolveMemberRefMethod inspects metadata only. Stop-before-reachability remains
        // enabled; no Assembly.LoadFile / private execution / null receiver executes.
        var expectedPublic = realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h =>
            realReader.GetString(realReader.GetMethodDefinition(h).Name) == "ExecuteAssembly"
            && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)) == "2002080E1D0E");
        Check((MethodDefinitionHandle)Get(resolved, "Handle") == expectedPublic
            && ReferenceEquals(Get(resolved, "Module"), realModule), "public-same-arity-exact-CoreLib-MethodDef");
        foreach(var h in realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Where(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="ExecuteAssembly" && h!=actualHandle))
            Check(!(bool)predicate.Invoke(null,new[]{((IDictionary)Get(realModule,"MethodMap"))[h]!})!,"public-overload-excluded");
        Console.WriteLine("assembly-execution-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","00020812081D0E",true),("alias","00020812091D0E",true),
            ("instance","20020812081D0E",false),("generic","1001020812081D0E",false),
            ("bool-return","00020212081D0E",false),("argument-value","00020811081D0E",false),
            ("argument-byref","0002081012081D0E",false),("modifier","000208200C12081D0E",false),
            ("array-byref","0002081208101D0E",false),("array-element","00020812081D1C",false),
            ("mdarray","0002081208140E020000",false),("arity","0001081208",false),
            ("trailing","00020812081D0E00",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var runtimeHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System.Reflection"),md.GetOrAddString("Assembly"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Abstract,md.GetOrAddString("System"),md.GetOrAddString("Type"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("AppDomain"),default,f,m);
            var otherParent=md.AddTypeDefinition(TypeAttributes.NotPublic,md.GetOrAddString("System"),md.GetOrAddString("OtherHandle"),default,f,MetadataTokens.MethodDefinitionHandle(2));
            var ownerRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("AppDomain"));
            var runtimeRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System.Reflection"),md.GetOrAddString("Assembly"));
            var enumRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("Type"));
            string hex=vector.Hex;
            var handle=md.AddMethodDefinition(MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig,MethodImplAttributes.IL,md.GetOrAddString("ExecuteAssembly"),md.GetOrAddBlob(Convert.FromHexString(hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.Type");
            var runtimeClass=Class(runtimeHandle,"System.Reflection.Assembly");
            var owner=Class(ownerHandle,"System.AppDomain");Set(owner,"IsValueType",false);
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;
            modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            if(vector.Label.StartsWith("alias",StringComparison.Ordinal))
            {
                var result=((IEnumerable)Get(signature,"ParameterTypes")).Cast<object>().First();
                Check(ReferenceEquals(Get(result,"Class"),runtimeClass),"RuntimeType-alias-resolves-to-same-ClassInfo");
                Check(reader.TypeReferences.Count==3,"three-actual-TypeRef-rows");
            }
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","ExecuteAssembly");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(runtimeClass,"_fullName","System.Type");
                Reject(runtimeClass,"IsValueType",true);
                Reject(owner,"Module",Make("Module"));
                Reject(method,"Handle",default(MethodDefinitionHandle));
                Reject(method,"Attributes",MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig);
                Reject(method,"Attributes",MethodAttributes.Assembly|MethodAttributes.HideBySig);
                Reject(runtimeClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.AppDomain");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        foreach(string hex in new[]{"00020812001D0E","00020812011D0E","00020812021D0E","00020812031D0E"})
        {
            var metadata=new MetadataBuilder();metadata.AddModule(0,metadata.GetOrAddString("Raw"),metadata.GetOrAddGuid(Guid.Empty),default,default);
            var blob=metadata.GetOrAddBlob(Convert.FromHexString(hex));var image=new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(!raw(source.GetMetadataReader().GetBlobReader(blob)),"invalid-coded-token-"+hex);
        }
        Console.WriteLine("assembly-execution-raw-guards="+count);return true;
    }
}
