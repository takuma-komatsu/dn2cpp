using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class PathActivationAttributesGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--path-attrs-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsActivatorFrom",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsActivatorFromSignature",statics)!.CreateDelegate<RawPredicate>();
        var decode=typeof(MethodDefinition).GetMethods().Single(m=>m.Name=="DecodeSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("Named attributes guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        var expectedNeighborTokens = new HashSet<int>();
        string modelInput=Path.GetFullPath(args[3]);
        using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("PathAttributesGuardInput",new Version(1,0)),"PathAttributesGuardInput",Mono.Cecil.ModuleKind.Dll))
        using(var core=Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var target=core.MainModule.GetType("System.Activator").Methods.Single(m=>m.Name=="CreateInstanceFrom" && m.IsStatic && m.Parameters.Count==3 && m.Parameters[0].ParameterType.FullName=="System.String");
            var imported=empty.MainModule.ImportReference(target);
            var host=new Mono.Cecil.TypeDefinition("Guard","AliasHost",Mono.Cecil.TypeAttributes.Public,empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            var probe=new Mono.Cecil.MethodDefinition("AliasProbe",Mono.Cecil.MethodAttributes.Public|Mono.Cecil.MethodAttributes.Static,imported.ReturnType);
            foreach(var p in imported.Parameters)probe.Parameters.Add(new Mono.Cecil.ParameterDefinition(p.ParameterType));
            host.Methods.Add(probe);var il=probe.Body.GetILProcessor();
            foreach(var p in probe.Parameters)il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg,p);
            il.Emit(Mono.Cecil.Cil.OpCodes.Call,imported);il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            foreach (var neighbor in core.MainModule.GetType("System.Activator").Methods.Where(m => m.Name == "CreateInstance" && m.IsPublic && m.IsStatic && m.Parameters.Count == 3
                && (m.Parameters[0].ParameterType.FullName == "System.Type" || m.Parameters[0].ParameterType.FullName == "System.String")))
            {
                expectedNeighborTokens.Add(neighbor.MetadataToken.ToInt32());
                var reference = empty.MainModule.ImportReference(neighbor);
                var method = new Mono.Cecil.MethodDefinition("Neighbor" + neighbor.MetadataToken.ToInt32(), Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, reference.ReturnType);
                foreach (var parameter in reference.Parameters) method.Parameters.Add(new Mono.Cecil.ParameterDefinition(parameter.ParameterType));
                host.Methods.Add(method);
                var body = method.Body.GetILProcessor();
                foreach (var parameter in method.Parameters) body.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, parameter);
                body.Emit(Mono.Cecil.Cil.OpCodes.Call, reference); body.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            }
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
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="Activator" && t.GetDeclaringType().IsNil;
        });
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="CreateInstanceFrom" && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)).StartsWith("000312",StringComparison.Ordinal));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Console.WriteLine("path-attrs-actual-attrs="+Get(actual,"Attributes")+" impl="+Get(actual,"ImplAttributes")+" rva="+Get(actual,"Rva"));
        var diagnosticSignature=Get(actual,"Signature");
        Console.WriteLine("path-attrs-actual-header="+Get(Get(diagnosticSignature,"Header"),"RawValue"));
        var diagnosticParameters=((IEnumerable)Get(diagnosticSignature,"ParameterTypes")).Cast<object>().ToArray();
        for(int index=0;index<diagnosticParameters.Length;index++)
        {
            var parameter=diagnosticParameters[index];var cls=Get(parameter,"Class");
            Console.WriteLine("path-attrs-param-"+index+" kind="+Get(parameter,"Kind")
                +(cls==null?" primitive="+Get(parameter,"Primitive")
                    :" class="+Get(cls,"FullName")+" enum="+Get(cls,"IsEnum")+" value="+Get(cls,"IsValueType")
                        +" underlying="+Get(cls,"EnumUnderlying")+" sameModule="+ReferenceEquals(Get(cls,"Module"),realModule)+" handle="+Get(cls,"Handle")));
        }
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        Console.WriteLine("path-attrs-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var signature=Get(actual,"Signature");
        var parameters=((IEnumerable)Get(signature,"ParameterTypes")).Cast<object>().ToArray();
        var decoded=Model("CoreIntrinsics").GetMethod("IsActivatorFromDecodedSignature",statics)!;
        Check((bool)decoded.Invoke(null,new[]{signature,realModule})!,"actual-decoded-three");
        void Reject(object obj,string field,object replacement,string label)
        {
            object saved=Get(obj,field);Set(obj,field,replacement);
            try {Check(!(bool)predicate.Invoke(null,new[]{actual})!,label);}
            finally {Set(obj,field,saved);}
        }
        Reject(Get(Get(signature,"ReturnType"),"Class"),"IsValueType",true,"ObjectHandle-must-be-class");
        Reject(Get(Get(signature,"ReturnType"),"Class"),"Module",Make("Module"),"ObjectHandle-CoreLib-module");
        Reject(actual,"Attributes",(MethodAttributes)0x8096,"unexpected-RequireSecObject");
        Reject(actual,"ImplAttributes",MethodImplAttributes.InternalCall,"wrong-implementation");
        var appModule=((IEnumerable)Get(loaded,"Modules")).Cast<object>().Single(m=>(string)Get(m,"AssemblyName")=="PathAttributesGuardInput");
        var appReader=(MetadataReader)Get(appModule,"Reader");
        var aliasHandle=appReader.MemberReferences.Single(h=>appReader.GetString(appReader.GetMemberReference(h).Name)=="CreateInstanceFrom" && raw(appReader.GetBlobReader(appReader.GetMemberReference(h).Signature)));
        var alias=appReader.GetMemberReference(aliasHandle);
        var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{loaded},null)!;
        var decoder=typeof(MemberReference).GetMethods().Single(m=>m.Name=="DecodeMethodSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        var aliasSignature=decoder.Invoke(alias,new object?[]{provider,null})!;
        Check(raw(appReader.GetBlobReader(alias.Signature)),"actual-MemberRef-raw-alias");
        Check((bool)decoded.Invoke(null,new[]{aliasSignature,realModule})!,"actual-MemberRef-decoded-alias");
        var aliasParameters=((IEnumerable)Get(aliasSignature,"ParameterTypes")).Cast<object>().ToArray();
        Check(ReferenceEquals(Get(Get(aliasSignature,"ReturnType"),"Class"),Get(Get(signature,"ReturnType"),"Class")),"same-CoreLib-ObjectHandle-identity");
        var context=Model("GenericContext").GetField("Empty",statics)!.GetValue(null)!;
        var seenNeighbors = new HashSet<int>();
        foreach(var h in appReader.MemberReferences.Where(h=>appReader.GetString(appReader.GetMemberReference(h).Name).StartsWith("CreateInstance",StringComparison.Ordinal)))
        {
            var resolved=Model("Compilation").GetMethod("ResolveMemberRefMethod",fields)!.Invoke(loaded,new object[]{appModule,h,context})!;
            bool supported=h==aliasHandle;
            Check((bool)predicate.Invoke(null,new[]{resolved})! == supported,"actual-MemberRef-three-overload-policy");
            Check(ReferenceEquals(Get(resolved,"Module"),realModule),"actual-MemberRef-CoreLib-module");
            if(supported) Check((MethodDefinitionHandle)Get(resolved,"Handle")==actualHandle,"actual-MemberRef-target-identity");
            else
            {
                int token = MetadataTokens.GetToken((MethodDefinitionHandle)Get(resolved,"Handle"));
                Check(expectedNeighborTokens.Contains(token),"neighbor-exact-MethodDef-token");
                seenNeighbors.Add(token);
            }
        }
        Check(seenNeighbors.SetEquals(expectedNeighborTokens),"all-Type-and-named-neighbors-resolved");
        var vectors=new(string Hex,bool Expected)[]{
            ("0003120C0E0E1D1C",true), ("0003120D0E0E1D1C",true),
            ("2003120C0E0E1D1C",false), ("100103120C0E0E1D1C",false),
            ("0003110C0E0E1D1C",false), ("00031C0E0E1D1C",false),
            ("000310120C0E0E1D1C",false), ("00032008120C0E0E1D1C",false),
            ("0003120C12080E1D1C",false), ("0003120C100E0E1D1C",false),
            ("0003120C0E0E101D1C",false), ("0003120C0E0E1D0E",false),
            ("0003120C0E0E1C",false), ("0003120C0E0E141C020000",false), ("0003120C0E0E1D1210",false),
            ("0003120C0E0E1D1C00",false),
            ("000312000E0E1D1C",false), ("000312010E0E1D1C",false),
            ("000312020E0E1D1C",false), ("000312030E0E1D1C",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("RawFlags"),md.GetOrAddGuid(Guid.Empty),default,default);
            var blob=md.GetOrAddBlob(Convert.FromHexString(vector.Hex));var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob))==vector.Expected,"raw-"+vector.Hex);
        }
        Console.WriteLine("path-attrs-guards="+count);return true;
    }
}
