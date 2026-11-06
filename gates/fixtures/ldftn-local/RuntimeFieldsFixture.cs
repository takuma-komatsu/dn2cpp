using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;
internal static class RuntimeFieldsFixture
{
 public static bool Run(string[] args)
 {
  bool negative=args.Length==4 && args[0]=="--runtime-fields-negative";
  bool rawImport=args.Length==4 && args[0]=="--runtime-fields-raw-import";
  if(!negative && !rawImport && (args.Length!=3 || !new[]{"--runtime-fields","--runtime-fields-reflection","--runtime-fields-noalloc","--runtime-fields-oracle"}.Contains(args[0])))return false;
  CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;CultureInfo.CurrentUICulture=CultureInfo.InvariantCulture;
  using var core=AssemblyDefinition.ReadAssembly(args[2]);var owner=core.MainModule.GetType("System.RuntimeTypeHandle");
  var targetDef=owner.Methods.Single(m=>m.Name=="GetFields" && m.Parameters.Count==3 && m.Parameters[0].ParameterType.FullName=="System.RuntimeType");
  if((int)targetDef.Attributes!=0x93 || (int)targetDef.ImplAttributes!=0 || targetDef.HasGenericParameters || targetDef.ReturnType.MetadataType!=MetadataType.Boolean || targetDef.Parameters[1].ParameterType.FullName!="System.Span`1<System.IntPtr>" || targetDef.Parameters[2].ParameterType.FullName!="System.Int32&" || targetDef.Parameters[0].ParameterType.FullName!="System.RuntimeType")throw new InvalidOperationException("GetFields metadata changed");
  string path=Path.GetFullPath(args[1]);using var app=AssemblyDefinition.ReadAssembly(path,new ReaderParameters{InMemory=true});var mod=app.MainModule;var program=mod.GetType("RuntimeFieldsSubset.Program");
  MethodDefinition Method(string name)=>program.Methods.Single(m=>m.Name==name);
  if(rawImport) {
   bool marshaller=args[3].StartsWith("marshaller",StringComparison.Ordinal);bool addressMode=args[3].EndsWith("-address",StringComparison.Ordinal);
   var raw=owner.Methods.Single(m=>marshaller?m.Name=="GetFields" && m.Parameters[0].ParameterType is PointerType:m.Name=="<GetFields>g____PInvoke|65_0");
   if(raw.Parameters.Count!=3 || raw.Parameters[0].ParameterType.FullName!="System.Runtime.CompilerServices.MethodTable*" || raw.ReturnType.MetadataType!=MetadataType.ValueType)throw new InvalidOperationException("raw fields declaration changed");
   if(!marshaller && raw.PInvokeInfo is not {Module.Name:"QCall",EntryPoint:"RuntimeTypeHandle_GetFields"})throw new InvalidOperationException("raw fields import changed");
   var import=mod.ImportReference(raw);var entry=new MethodDefinition("RawFieldsCompileOnly",MethodAttributes.Private|MethodAttributes.Static,mod.TypeSystem.Void);program.Methods.Add(entry);entry.Body.InitLocals=true;var il=entry.Body.GetILProcessor();
   il.Emit(OpCodes.Ldc_I4_0);il.Emit(OpCodes.Conv_U);
   if(marshaller){var span=new VariableDefinition(import.Parameters[1].ParameterType);var count=new VariableDefinition(mod.TypeSystem.Int32);entry.Body.Variables.Add(span);entry.Body.Variables.Add(count);il.Emit(OpCodes.Ldloca,span);il.Emit(OpCodes.Initobj,span.VariableType);il.Emit(OpCodes.Ldloc,span);il.Emit(OpCodes.Ldloca,count);}
   else {il.Emit(OpCodes.Ldc_I4_0);il.Emit(OpCodes.Conv_U);il.Emit(OpCodes.Ldc_I4_0);il.Emit(OpCodes.Conv_U);}
   if(addressMode){il.Emit(OpCodes.Ldftn,import);var site=new CallSite(import.ReturnType);foreach(var parameter in import.Parameters)site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));il.Emit(OpCodes.Calli,site);}else il.Emit(OpCodes.Call,import);
   il.Emit(OpCodes.Pop);il.Emit(OpCodes.Ret);mod.EntryPoint=entry;app.Write(path,new WriterParameters{Timestamp=0,DeterministicMvid=true});return true;
  }

  if(args[0]=="--runtime-fields-oracle") {var entry=Method("RunArtifactChecks");entry.Body=new MethodBody(entry);var il=entry.Body.GetILProcessor();il.Emit(OpCodes.Call,Method("Run"));il.Emit(OpCodes.Ret);mod.EntryPoint=entry;}
  else {
   var target=mod.ImportReference(targetDef);var rt=mod.ImportReference(targetDef.Parameters[0].ParameterType);
   var del=program.NestedTypes.Single(t=>t.Name=="Query");var invoke=del.Methods.Single(m=>m.Name=="Invoke");invoke.Parameters[0].ParameterType=rt;
   var begin=del.Methods.SingleOrDefault(m=>m.Name=="BeginInvoke");if(begin!=null)begin.Parameters[0].ParameterType=rt;
   var address=Method("Address");address.Body=new MethodBody(address);var ai=address.Body.GetILProcessor();ai.Emit(OpCodes.Ldnull);ai.Emit(OpCodes.Ldftn,target);ai.Emit(OpCodes.Newobj,del.Methods.Single(m=>m.IsConstructor));ai.Emit(OpCodes.Ret);
   foreach(string name in new[]{"Direct","Pointer","DelegateRoute"}) {
    var stub=Method(name);stub.Body=new MethodBody(stub);var il=stub.Body.GetILProcessor();if(name=="DelegateRoute")il.Emit(OpCodes.Call,address);
    il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,rt);il.Emit(OpCodes.Ldarg_1);il.Emit(OpCodes.Ldarg_2);
    if(name=="Pointer"){il.Emit(OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);foreach(var p in target.Parameters)site.Parameters.Add(new ParameterDefinition(p.ParameterType));il.Emit(OpCodes.Calli,site);}
    else il.Emit(name=="DelegateRoute"?OpCodes.Callvirt:OpCodes.Call,name=="DelegateRoute"?invoke:target);
    il.Emit(OpCodes.Ret);
   }
   if(negative) {
    string mode=args[3];bool addressMode=mode.StartsWith("address-",StringComparison.Ordinal);if(addressMode)mode=mode.Substring(8);
    var malformed=new MethodReference(target.Name,target.ReturnType,target.DeclaringType){HasThis=false};foreach(var parameter in target.Parameters)malformed.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
    switch(mode){
     case "return":malformed.ReturnType=mod.TypeSystem.Int32;break;
     case "argument":malformed.Parameters[0].ParameterType=mod.TypeSystem.Object;break;
     case "span-byref":malformed.Parameters[1].ParameterType=new ByReferenceType(target.Parameters[1].ParameterType);break;
     case "span-modifier":malformed.Parameters[1].ParameterType=new OptionalModifierType(mod.TypeSystem.Object,target.Parameters[1].ParameterType);break;
     case "span-element":var span=new GenericInstanceType(((GenericInstanceType)target.Parameters[1].ParameterType).ElementType);span.GenericArguments.Add(mod.TypeSystem.Int32);malformed.Parameters[1].ParameterType=span;break;
     case "count-value":malformed.Parameters[2].ParameterType=mod.TypeSystem.Int32;break;
     case "count-modifier":malformed.Parameters[2].ParameterType=new OptionalModifierType(mod.TypeSystem.Object,target.Parameters[2].ParameterType);break;
     case "instance":malformed.HasThis=true;break;
     case "generic":malformed.GenericParameters.Add(new GenericParameter("T",malformed));break;
     default:throw new InvalidOperationException("unknown negative "+mode);
    }
    Method(addressMode?"Address":"Direct").Body.Instructions.Single(i=>i.OpCode==(addressMode?OpCodes.Ldftn:OpCodes.Call)).Operand=malformed;
   }
   if(args[0]=="--runtime-fields-noalloc")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(mod,Method("Direct"));
   mod.EntryPoint=Method(args[0]=="--runtime-fields-reflection"?"RunReflectionChecks":"RunArtifactChecks");
  }
  app.Write(path,new WriterParameters{Timestamp=0,DeterministicMvid=true});return true;
 }
}
