using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class NamedActivationGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--named-activation-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        object Map(string type,string field)=>Activator.CreateInstance(Model(type).GetField(field,fields)!.FieldType)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsActivatorNamed",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsActivatorNamedSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("CreateInstance guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
        // Replace the empty app creation block. This is valid unused public-call IL,
        // inspected by the model only; no activation overload is executed.
        var expectedOverloadTokens = new HashSet<int>();
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("NamedActivationGuardInput", new Version(1, 0)),
            "NamedActivationGuardInput", Mono.Cecil.ModuleKind.Dll))
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var methods = core.MainModule.GetType("System.Activator").Methods;
            var neighbors = new[]
            {
                methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 3
                    && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.String"
                    && m.ReturnType.FullName == "System.Runtime.Remoting.ObjectHandle"),
                methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 8
                    && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.String"
                    && m.ReturnType.FullName == "System.Runtime.Remoting.ObjectHandle"),
                methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 2
                    && m.Parameters[0].ParameterType.FullName == "System.Type" && m.Parameters[1].ParameterType.FullName == "System.Boolean"),
                methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 2
                    && m.Parameters[0].ParameterType.FullName == "System.Type" && m.Parameters[1].ParameterType.FullName == "System.Object[]"),
                methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType.FullName == "System.Type" && !m.HasGenericParameters)
            };
            var host = new Mono.Cecil.TypeDefinition("Guard", "NeighborOverloads", Mono.Cecil.TypeAttributes.Public, empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            for (int i = 0; i < neighbors.Length; i++)
            {
                var original = neighbors[i];
                expectedOverloadTokens.Add(original.MetadataToken.ToInt32());
                var import = empty.MainModule.ImportReference(original);
                var probe = new Mono.Cecil.MethodDefinition("Neighbor" + i, Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, import.ReturnType);
                foreach (var parameter in import.Parameters) probe.Parameters.Add(new Mono.Cecil.ParameterDefinition(parameter.ParameterType));
                host.Methods.Add(probe);
                var il = probe.Body.GetILProcessor();
                foreach (var parameter in probe.Parameters) il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, parameter);
                il.Emit(Mono.Cecil.Cil.OpCodes.Call, import);
                il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
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
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="Activator" && t.GetDeclaringType().IsNil;
        });
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="CreateInstance" && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)).StartsWith("000212",StringComparison.Ordinal) && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)).EndsWith("0E0E",StringComparison.Ordinal));
        var eightHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="CreateInstance"
            && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)).StartsWith("000812",StringComparison.Ordinal));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Console.WriteLine("named-activation-actual-attributes=" + ((int)Get(actual,"Attributes")).ToString("X4") + ";raw=" + ((int)realReader.GetMethodDefinition(actualHandle).Attributes).ToString("X4") + ";impl=" + ((int)Get(actual,"ImplAttributes")).ToString("X4"));
        var actualSignature = Get(actual,"Signature");
        var actualReturn = Get(actualSignature,"ReturnType");
        var actualClass = Get(actualReturn,"Class");
        Console.WriteLine("actual-owner="+Get(Get(actual,"DeclaringClass"),"FullName")+";RVA="+Get(actual,"Rva")+";raw="+Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(actualHandle).Signature))+";raw-ok="+raw(realReader.GetBlobReader(realReader.GetMethodDefinition(actualHandle).Signature)));
        Console.WriteLine("actual-return-kind="+Get(actualReturn,"Kind")+";name="+Get(actualClass,"FullName")+";value="+Get(actualClass,"IsValueType")+";same-module="+ReferenceEquals(Get(actualClass,"Module"),realModule));
        foreach(var parameter in ((IEnumerable)Get(actualSignature,"ParameterTypes")).Cast<object>()) Console.WriteLine("actual-parameter="+Get(parameter,"Kind")+";is-string="+Get(parameter,"IsString"));
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        // After loaded/realModule/predicate exist. Ordinary production resolver must
        // return the exact legitimate overload, not the new two-string adapter.
        var appModule = ((IEnumerable)Get(loaded, "Modules")).Cast<object>()
            .Single(m => (string)Get(m, "AssemblyName") == "NamedActivationGuardInput");
        var appReader = (MetadataReader)Get(appModule, "Reader");
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var seenOverloads = new HashSet<int>();
        foreach (var h in appReader.MemberReferences.Where(h => appReader.GetString(appReader.GetMemberReference(h).Name) == "CreateInstance"))
        {
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", fields)!
                .Invoke(loaded, new object[] { appModule, h, context })!;
            int token = MetadataTokens.GetToken((MethodDefinitionHandle)Get(resolved, "Handle"));
            Check(ReferenceEquals(Get(resolved, "Module"), realModule) && expectedOverloadTokens.Contains(token), "neighbor-MemberRef-exact-CoreLib-identity");
            Check((bool)predicate.Invoke(null, new[] { resolved })! == (token == MetadataTokens.GetToken(eightHandle)), "neighbor-MemberRef-exact-two-or-eight-adapter-policy");
            seenOverloads.Add(token);
            Console.WriteLine("named-activation-neighbor-MemberRef:" + token.ToString("X8", CultureInfo.InvariantCulture));
        }
        Check(seenOverloads.SetEquals(expectedOverloadTokens), "three-eight-and-Type-one-two-overloads-all-resolved");

        foreach(var h in realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Where(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="CreateInstance" && h!=actualHandle))
        {
            var other = ((IDictionary)Get(realModule,"MethodMap"))[h];
            if (other is not null)
                Check((bool)predicate.Invoke(null,new[]{other})! == (h == eightHandle),"other-overload-exact-eight-or-excluded");
            else
                Check(realReader.GetMethodDefinition(h).GetGenericParameters().Count != 0
                    && !raw(realReader.GetBlobReader(realReader.GetMethodDefinition(h).Signature)),"uninstantiated-generic-overload-excluded");
        }
        Console.WriteLine("named-activation-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var vectors=new(string Label,string Hex,bool Expected)[]{
            ("exact","0002120C0E0E",true),("alias","0002120D0E0E",true),
            ("instance","2002120C0E0E",false),("generic","100102120C0E0E",false),
            ("object","00021C0E0E",false),("value-return","0002110C0E0E",false),
            ("byref","000210120C0E0E",false),("modified","00022008120C0E0E",false),
            ("named-string","0002120C12080E",false),("argument-byref","0002120C100E0E",false),
            ("array","0002120C1D0E0E",false),("trailing","0002120C0E0E00",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("Guard"),md.GetOrAddGuid(Guid.Empty),default,default);
            md.AddAssembly(md.GetOrAddString("System.Private.CoreLib"),new Version(10,0,0,0),default,default,default,default);
            var f=MetadataTokens.FieldDefinitionHandle(1);var m=MetadataTokens.MethodDefinitionHandle(1);
            md.AddTypeDefinition(TypeAttributes.NotPublic,default,md.GetOrAddString("<Module>"),default,f,m);
            var runtimeHandle=md.AddTypeDefinition(TypeAttributes.NotPublic|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"),default,f,m);
            var enumHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Abstract,md.GetOrAddString("System.Runtime.Remoting"),md.GetOrAddString("ObjectHandle"),default,f,m);
            var ownerHandle=md.AddTypeDefinition(TypeAttributes.Public|TypeAttributes.Sealed,md.GetOrAddString("System"),md.GetOrAddString("Activator"),default,f,m);
            var otherParent=md.AddTypeDefinition(TypeAttributes.NotPublic,md.GetOrAddString("System"),md.GetOrAddString("OtherHandle"),default,f,MetadataTokens.MethodDefinitionHandle(2));
            var ownerRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("Activator"));
            var runtimeRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System"),md.GetOrAddString("RuntimeType"));
            var enumRef=md.AddTypeReference(MetadataTokens.EntityHandle(TableIndex.Module,1),md.GetOrAddString("System.Runtime.Remoting"),md.GetOrAddString("ObjectHandle"));
            string hex=vector.Hex;
            var handle=md.AddMethodDefinition((MethodAttributes)0x8096,MethodImplAttributes.IL,md.GetOrAddString("CreateInstance"),md.GetOrAddBlob(Convert.FromHexString(hex)),1,MetadataTokens.ParameterHandle(1));
            var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());var reader=source.GetMetadataReader();
            var compilation=Make("Compilation");var module=Make("Module");Set(module,"Reader",reader);Set(module,"AssemblyName","System.Private.CoreLib");Set(module,"Owner",compilation);
            var classes=(IDictionary)Map("Module","ClassMap");Set(module,"ClassMap",classes);
            object Class(TypeDefinitionHandle h,string name) { var c=Make("ClassInfo");Set(c,"Module",module);Set(c,"Handle",h);Set(c,"_fullName",name);Set(c,"IsSealed",true);classes.Add(h,c);return c; }
            var enumClass=Class(enumHandle,"System.Runtime.Remoting.ObjectHandle");
            var runtimeClass=Class(runtimeHandle,"System.RuntimeType");
            var owner=Class(ownerHandle,"System.Activator");Set(owner,"IsValueType",false);
            var readers=(IDictionary)Map("Compilation","_byReader");readers.Add(reader,module);Set(compilation,"_byReader",readers);
            var modules=(IList)Activator.CreateInstance(Model("Compilation").GetProperty("Modules",fields)!.PropertyType)!;
            modules.Add(module);Set(compilation,"<Modules>k__BackingField",modules);
            var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{compilation},null)!;
            var definition=reader.GetMethodDefinition(handle);var signature=decode.Invoke(definition,new object?[]{provider,null})!;
            if(vector.Label.StartsWith("alias",StringComparison.Ordinal))
            {
                var result=Get(signature,"ReturnType");
                Check(ReferenceEquals(Get(result,"Class"),enumClass),"ObjectHandle-alias-resolves-to-same-ClassInfo");
                Check(reader.TypeReferences.Count==3,"three-actual-TypeRef-rows");
            }
            var method=Make("MethodInfo");Set(method,"Module",module);Set(method,"DeclaringClass",owner);Set(method,"Handle",handle);Set(method,"Name","CreateInstance");
            Set(method,"Attributes",definition.Attributes);Set(method,"ImplAttributes",definition.ImplAttributes);Set(method,"Rva",1);Set(method,"Signature",signature);
            Check(raw(reader.GetBlobReader(definition.Signature))==vector.Expected,"raw-"+vector.Label);
            Check((bool)predicate.Invoke(null,new[]{method})! == vector.Expected,vector.Label);
            if(vector.Expected)
            {
                void Reject(object obj,string field,object value) {object old=Get(obj,field);Set(obj,field,value);Check(!(bool)predicate.Invoke(null,new[]{method})!,field);Set(obj,field,old);}
                Reject(enumClass,"_fullName","System.Object");
                Reject(enumClass,"IsValueType",true);
                Reject(owner,"Module",Make("Module"));
                Reject(method,"Handle",default(MethodDefinitionHandle));
                Reject(enumClass,"Module",Make("Module"));Reject(owner,"_fullName","Application.Activator");
                Reject(module,"AssemblyName","Application");Reject(method,"Rva",0);Reject(method,"Attributes",MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig);
                foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.AggressiveInlining,MethodImplAttributes.Synchronized})Reject(method,"ImplAttributes",impl);
            }
        }
        foreach(string hex in new[]{"000212000E0E","000212010E0E","000212020E0E","000212030E0E"})
        {
            var metadata=new MetadataBuilder();metadata.AddModule(0,metadata.GetOrAddString("Raw"),metadata.GetOrAddGuid(Guid.Empty),default,default);
            var blob=metadata.GetOrAddBlob(Convert.FromHexString(hex));var image=new BlobBuilder();new MetadataRootBuilder(metadata).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());Check(!raw(source.GetMetadataReader().GetBlobReader(blob)),"invalid-coded-token-"+hex);
        }
        Console.WriteLine("named-activation-raw-guards="+count);return true;
    }
}
