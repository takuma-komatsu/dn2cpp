using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

internal static class AppContextExitGuards
{
    private delegate bool RawPredicate(BlobReader blob);
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--appcontext-exit-guards") return false;
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
        var production=Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type Model(string name)=>production.GetType("Dn2Cpp."+name,true)!;
        object Make(string name)=>RuntimeHelpers.GetUninitializedObject(Model(name));
        void Set(object value,string name,object? fieldValue) { if(value.GetType().GetField(name,fields) is {} f) f.SetValue(value,fieldValue); else value.GetType().GetProperty(name,fields)!.SetValue(value,fieldValue); }
        object Get(object value,string name)=>value.GetType().GetField(name,fields) is {} f?f.GetValue(value)!:value.GetType().GetProperty(name,fields)!.GetValue(value)!;
        var predicate=Model("CoreIntrinsics").GetMethod("IsAppContextOnProcessExit",statics)!;
        var raw=Model("CoreIntrinsics").GetMethod("IsAppContextOnProcessExitSignature",statics)!.CreateDelegate<RawPredicate>();
        int count=0;
        void Check(bool success,string label) { if(!success)throw new InvalidOperationException("AppContext exit guard "+label);count++; }
        // Use the real loader and ClassInfo naming rules, not a modeled _fullName.
        string modelInput=Path.GetFullPath(args[3]);
        using(var empty=Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("AppContextExitGuardInput",new Version(1,0)),
            "AppContextExitGuardInput",Mono.Cecil.ModuleKind.Dll))
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
            return realReader.GetString(t.Namespace)=="System" && realReader.GetString(t.Name)=="AppContext" && t.GetDeclaringType().IsNil;
        });
        var actualHandle=realReader.GetTypeDefinition(ownerHandleReal).GetMethods().Single(h=>realReader.GetString(realReader.GetMethodDefinition(h).Name)=="OnProcessExit" && realReader.GetMethodDefinition(h).Attributes==(MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig));
        var actual=((IDictionary)Get(realModule,"MethodMap"))[actualHandle]!;
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-CoreLib-MethodDef-predicate");
        Console.WriteLine("appcontext-exit-actual-model="+MetadataTokens.GetToken(actualHandle).ToString("X8",CultureInfo.InvariantCulture));
        void Reject(object obj,string field,object replacement,string label)
        {
            object saved=Get(obj,field);Set(obj,field,replacement);
            try {Check(!(bool)predicate.Invoke(null,new[]{actual})!,label);}
            finally {Set(obj,field,saved);}
        }
        var owner=Get(actual,"DeclaringClass");
        Reject(realModule,"AssemblyName","Application.CoreLib","wrong-assembly");
        Reject(owner,"Handle",default(TypeDefinitionHandle),"nil-owner-handle");
        Reject(actual,"Attributes",MethodAttributes.Assembly|MethodAttributes.Static|MethodAttributes.HideBySig|MethodAttributes.PinvokeImpl,"pinvoke-flags");
        Reject(owner,"_fullName","Application.AppContext","wrong-owner");
        Reject(owner,"Module",Make("Module"),"wrong-owner-module");
        Reject(actual,"Name","OnProcessStart","wrong-name");
        Reject(actual,"Handle",default(MethodDefinitionHandle),"nil-handle");
        Reject(actual,"Rva",0,"missing-managed-body");
        Reject(actual,"Attributes",MethodAttributes.Private|MethodAttributes.Static|MethodAttributes.HideBySig,"wrong-access");
        Reject(actual,"Attributes",MethodAttributes.Assembly|MethodAttributes.HideBySig,"instance");
        foreach(var impl in new[]{MethodImplAttributes.InternalCall,MethodImplAttributes.PreserveSig,MethodImplAttributes.NoInlining,MethodImplAttributes.Synchronized})
            Reject(actual,"ImplAttributes",impl,"wrong-implementation-"+impl);
        var vectors=new(string Hex,bool Expected)[]{
            ("000001",true),("200001",false),("600001",false),("050001",false),
            ("10010001",false),("000002",false),("00010108",false),
            ("0000200501",false),("00001001",false),("00000100",false),
            ("0000",false),("",false)};
        foreach(var vector in vectors)
        {
            var md=new MetadataBuilder();md.AddModule(0,md.GetOrAddString("RawExit"),md.GetOrAddGuid(Guid.Empty),default,default);
            var blob=md.GetOrAddBlob(Convert.FromHexString(vector.Hex));var image=new BlobBuilder();new MetadataRootBuilder(md).Serialize(image,0,0);
            using var source=MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
            Check(raw(source.GetMetadataReader().GetBlobReader(blob))==vector.Expected,"raw-"+vector.Hex);
        }
        Check((bool)predicate.Invoke(null,new[]{actual})!,"actual-model-restored");
        Console.WriteLine("appcontext-exit-guards="+count);return true;
    }
}
