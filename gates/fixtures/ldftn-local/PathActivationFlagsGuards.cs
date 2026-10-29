using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class PathActivationFlagsGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--path-flags-guards") return false;
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
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("Named activation flags guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        var neighborTokens = new Dictionary<int, bool>();
        string modelInput=Path.GetFullPath(args[3]);
        using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("PathFlagsGuardInput",new Version(1,0)),"PathFlagsGuardInput",Mono.Cecil.ModuleKind.Dll))
        using(var core=Mono.Cecil.AssemblyDefinition.ReadAssembly(args[2]))
        {
            var target=core.MainModule.GetType("System.Activator").Methods.Single(m=>m.Name=="CreateInstanceFrom" && m.IsStatic && m.Parameters.Count==8 && m.Parameters[0].ParameterType.FullName=="System.String");
            var imported=empty.MainModule.ImportReference(target);
            var host=new Mono.Cecil.TypeDefinition("Guard","AliasHost",Mono.Cecil.TypeAttributes.Public,empty.MainModule.TypeSystem.Object);
            empty.MainModule.Types.Add(host);
            var probe=new Mono.Cecil.MethodDefinition("AliasProbe",Mono.Cecil.MethodAttributes.Public|Mono.Cecil.MethodAttributes.Static,imported.ReturnType);
            foreach(var p in imported.Parameters)probe.Parameters.Add(new Mono.Cecil.ParameterDefinition(p.ParameterType));
            host.Methods.Add(probe);var il=probe.Body.GetILProcessor();
            foreach(var p in probe.Parameters)il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg,p);
            il.Emit(Mono.Cecil.Cil.OpCodes.Call,imported);il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            foreach (var neighbor in core.MainModule.GetType("System.Activator").Methods.Where(m => m.IsPublic && m.IsStatic
                && ((m.Name == "CreateInstanceFrom" && m.Parameters.Count is 2 or 3)
                    || (m.Name == "CreateInstance" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.String"))))
            {
                neighborTokens.Add(neighbor.MetadataToken.ToInt32(), neighbor.Name == "CreateInstanceFrom" && neighbor.Parameters.Count == 2);
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
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="CreateInstanceFrom" && Convert.ToHexString(realReader.GetBlobBytes(realReader.GetMethodDefinition(h).Signature)).StartsWith("000812",StringComparison.Ordinal));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Console.WriteLine("path-flags-actual-attrs="+Get(actual,"Attributes")+" impl="+Get(actual,"ImplAttributes")+" rva="+Get(actual,"Rva"));
        var diagnosticSignature=Get(actual,"Signature");
        Console.WriteLine("path-flags-actual-header="+Get(Get(diagnosticSignature,"Header"),"RawValue"));
        var diagnosticParameters=((IEnumerable)Get(diagnosticSignature,"ParameterTypes")).Cast<object>().ToArray();
        for(int index=0;index<diagnosticParameters.Length;index++)
        {
            var parameter=diagnosticParameters[index];var cls=Get(parameter,"Class");
            Console.WriteLine("path-flags-param-"+index+" kind="+Get(parameter,"Kind")
                +(cls==null?" primitive="+Get(parameter,"Primitive")
                    :" class="+Get(cls,"FullName")+" enum="+Get(cls,"IsEnum")+" value="+Get(cls,"IsValueType")
                        +" underlying="+Get(cls,"EnumUnderlying")+" sameModule="+ReferenceEquals(Get(cls,"Module"),realModule)+" handle="+Get(cls,"Handle")));
        }
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        Console.WriteLine("path-flags-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        var signature=Get(actual,"Signature");
        var parameters=((IEnumerable)Get(signature,"ParameterTypes")).Cast<object>().ToArray();
        var flags=Get(parameters[3],"Class");
        var decoded=Model("CoreIntrinsics").GetMethod("IsActivatorFromDecodedSignature",statics)!;
        Check((bool)decoded.Invoke(null,new[]{signature,realModule})!,"actual-decoded-eight");
        void Reject(object obj,string field,object replacement,string label)
        {
            object saved=Get(obj,field);Set(obj,field,replacement);
            try {Check(!(bool)predicate.Invoke(null,new[]{actual})!,label);}
            finally {Set(obj,field,saved);}
        }
        Reject(flags,"IsEnum",false,"BindingFlags-must-be-enum");
        Reject(flags,"EnumUnderlying",PrimitiveTypeCode.Int64,"BindingFlags-must-be-Int32");
        foreach(int index in new[]{3,4,6})Reject(Get(parameters[index],"Class"),"Module",Make("Module"),"parameter-module-"+index);
        Reject(Get(parameters[4],"Class"),"_fullName","System.Object","wrong-Binder");
        Reject(Get(parameters[6],"Class"),"_fullName","System.Object","wrong-CultureInfo");
        Reject(Get(Get(signature,"ReturnType"),"Class"),"IsValueType",true,"ObjectHandle-must-be-class");
        Reject(actual,"Attributes",(MethodAttributes)0x8096,"unexpected-RequireSecObject");
        Reject(actual,"ImplAttributes",MethodImplAttributes.InternalCall,"wrong-implementation");
        var appModule=((IEnumerable)Get(loaded,"Modules")).Cast<object>().Single(m=>(string)Get(m,"AssemblyName")=="PathFlagsGuardInput");
        var appReader=(MetadataReader)Get(appModule,"Reader");
        var aliasHandle=appReader.MemberReferences.Single(h=>appReader.GetString(appReader.GetMemberReference(h).Name)=="CreateInstanceFrom" && appReader.GetBlobBytes(appReader.GetMemberReference(h).Signature)[1]==8);
        var alias=appReader.GetMemberReference(aliasHandle);
        var provider=Activator.CreateInstance(Model("SignatureProvider"),fields,null,new[]{loaded},null)!;
        var decoder=typeof(MemberReference).GetMethods().Single(m=>m.Name=="DecodeMethodSignature").MakeGenericMethod(Model("TypeDesc"),typeof(object));
        var aliasSignature=decoder.Invoke(alias,new object?[]{provider,null})!;
        Check(raw(appReader.GetBlobReader(alias.Signature)),"actual-MemberRef-raw-alias");
        Check((bool)decoded.Invoke(null,new[]{aliasSignature,realModule})!,"actual-MemberRef-decoded-alias");
        var aliasParameters=((IEnumerable)Get(aliasSignature,"ParameterTypes")).Cast<object>().ToArray();
        foreach(int index in new[]{3,4,6})Check(ReferenceEquals(Get(parameters[index],"Class"),Get(aliasParameters[index],"Class")),"same-CoreLib-parameter-identity-"+index);
        var context = Model("GenericContext").GetField("Empty", statics)!.GetValue(null)!;
        var seen = new HashSet<int>();
        foreach (var h in appReader.MemberReferences.Where(h => appReader.GetString(appReader.GetMemberReference(h).Name).StartsWith("CreateInstance", StringComparison.Ordinal)))
        {
            var resolved = Model("Compilation").GetMethod("ResolveMemberRefMethod", fields)!.Invoke(loaded, new object[] { appModule, h, context })!;
            int token = MetadataTokens.GetToken((MethodDefinitionHandle)Get(resolved,"Handle"));
            Check(ReferenceEquals(Get(resolved,"Module"),realModule),"MemberRef-resolved-CoreLib");
            if (h == aliasHandle) Check(token == MetadataTokens.GetToken(actualHandle),"eight-argument-exact-target");
            else
            {
                Check(neighborTokens.TryGetValue(token,out bool admitted),"neighbor-exact-token");
                Check((bool)predicate.Invoke(null,new[]{resolved})! == admitted,"neighbor-admission-two-only");
                seen.Add(token);
            }
        }
        Check(seen.SetEquals(neighborTokens.Keys),"all-two-three-named-neighbors-resolved");
        var vectors=new(string Hex,bool Expected)[]{
            ("0008120C0E0E02111012141D1C12181D1C",true),
            ("0008120D0E0E02111112151D1C12191D1C",true),
            ("2008120C0E0E02111012141D1C12181D1C",false),
            ("100108120C0E0E02111012141D1C12181D1C",false),
            ("0008110C0E0E02111012141D1C12181D1C",false),
            ("0008120C0E0E02121012141D1C12181D1C",false),
            ("0008120C0E0E020812141D1C12181D1C",false),
            ("0008120C0E0E02111011141D1C12181D1C",false),
            ("0008120C0E0E0211101214101D1C12181D1C",false),
            ("0008120C0E0E02111012141D0E12181D1C",false),
            ("0008120C0E0E02111012141D1C11181D1C",false),
            ("0008120C0E0E02111012141D1C1218101D1C",false),
            ("0008120C0E0E02111012141D1C12181D1C00",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("RawFlags"),md.GetOrAddGuid(Guid.Empty),default,default);
            var blob=md.GetOrAddBlob(Convert.FromHexString(vector.Hex));var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob))==vector.Expected,"raw-"+vector.Hex);
        }
        Console.WriteLine("path-flags-guards="+count);return true;
    }
}
