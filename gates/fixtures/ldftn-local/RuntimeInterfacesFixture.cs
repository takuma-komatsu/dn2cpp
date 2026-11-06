using Mono.Cecil;
using Mono.Cecil.Cil;
internal static class RuntimeInterfacesFixture
{
 public static bool Run(string[] args)
 {
  bool negative=args.Length==4 && args[0]=="--runtime-interfaces-negative";
  if(!negative && (args.Length!=3 || !new[]{"--runtime-interfaces","--runtime-interfaces-reflection","--runtime-interfaces-noalloc","--runtime-interfaces-oracle","--runtime-interfaces-raw-import","--runtime-interfaces-raw-import-address"}.Contains(args[0])))return false;
  System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
  System.Globalization.CultureInfo.CurrentUICulture=System.Globalization.CultureInfo.InvariantCulture;
  using var core=AssemblyDefinition.ReadAssembly(args[2]);var owner=core.MainModule.GetType("System.RuntimeTypeHandle");
  var targetDef=owner.Methods.Single(m=>m.Name=="GetInterfaces" && m.Parameters.Count==1);
  if((int)targetDef.Attributes!=0x93 || (int)targetDef.ImplAttributes!=0 || targetDef.HasGenericParameters || targetDef.ReturnType.FullName!="System.Type[]" || targetDef.Parameters[0].ParameterType.FullName!="System.RuntimeType")throw new InvalidOperationException("GetInterfaces metadata changed");
  string path=Path.GetFullPath(args[1]);using var app=AssemblyDefinition.ReadAssembly(path,new ReaderParameters{InMemory=true});var mod=app.MainModule;var program=mod.GetType("RuntimeInterfacesSubset.Program");
  MethodDefinition Method(string name)=>program.Methods.Single(m=>m.Name==name);
  if(args[0].StartsWith("--runtime-interfaces-raw-import",StringComparison.Ordinal)) {
   var raw=owner.Methods.Single(m=>m.Name=="GetInterfaces" && m.IsPInvokeImpl);
   if((int)raw.Attributes!=0x2091 || (int)raw.ImplAttributes!=0x80 || raw.HasBody || raw.Parameters.Count!=2
    || raw.ReturnType.MetadataType!=MetadataType.Void || raw.Parameters[0].ParameterType.FullName!="System.Runtime.CompilerServices.MethodTable*"
    || raw.Parameters[1].ParameterType.FullName!="System.Runtime.CompilerServices.ObjectHandleOnStack"
    || raw.PInvokeInfo is not {Module.Name:"QCall",EntryPoint:"RuntimeTypeHandle_GetInterfaces"})throw new InvalidOperationException("raw interfaces import changed");
   var import=mod.ImportReference(raw);var entry=new MethodDefinition("RawInterfacesImportCompileOnly",MethodAttributes.Private|MethodAttributes.Static,mod.TypeSystem.Void);program.Methods.Add(entry);entry.Body.InitLocals=true;
   var result=new VariableDefinition(import.Parameters[1].ParameterType);entry.Body.Variables.Add(result);var il=entry.Body.GetILProcessor();
   il.Emit(OpCodes.Ldloca,result);il.Emit(OpCodes.Initobj,result.VariableType);il.Emit(OpCodes.Ldc_I4_0);il.Emit(OpCodes.Conv_U);il.Emit(OpCodes.Ldloc,result);
   if(args[0].EndsWith("-address",StringComparison.Ordinal)){il.Emit(OpCodes.Ldftn,import);var site=new CallSite(mod.TypeSystem.Void);foreach(var parameter in import.Parameters)site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));il.Emit(OpCodes.Calli,site);}
   else il.Emit(OpCodes.Call,import);il.Emit(OpCodes.Ret);mod.EntryPoint=entry;
   app.Write(path,new WriterParameters{Timestamp=0,DeterministicMvid=true});Console.WriteLine("runtime-interfaces-raw-import: compile only; DO NOT EXECUTE");return true;
  }

  if(args[0]=="--runtime-interfaces-oracle") mod.EntryPoint=Method("RunOracle");
  else {
   var target=mod.ImportReference(targetDef);var rt=mod.ImportReference(targetDef.Parameters[0].ParameterType);
   var del=program.NestedTypes.Single(t=>t.Name=="Factory");var invoke=del.Methods.Single(m=>m.Name=="Invoke");invoke.Parameters[0].ParameterType=rt;
   var begin=del.Methods.SingleOrDefault(m=>m.Name=="BeginInvoke");if(begin!=null)begin.Parameters[0].ParameterType=rt;
   var address=Method("Address");address.Body=new MethodBody(address);var ai=address.Body.GetILProcessor();ai.Emit(OpCodes.Ldnull);ai.Emit(OpCodes.Ldftn,target);ai.Emit(OpCodes.Newobj,del.Methods.Single(m=>m.IsConstructor));ai.Emit(OpCodes.Ret);
   foreach(string name in new[]{"Direct","Pointer","DelegateRoute"}) {
    var stub=Method(name);stub.Body=new MethodBody(stub);var il=stub.Body.GetILProcessor();if(name=="DelegateRoute")il.Emit(OpCodes.Call,address);
    il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Castclass,rt);
    if(name=="Pointer"){il.Emit(OpCodes.Ldftn,target);var site=new CallSite(target.ReturnType);site.Parameters.Add(new ParameterDefinition(rt));il.Emit(OpCodes.Calli,site);}
    else il.Emit(name=="DelegateRoute"?OpCodes.Callvirt:OpCodes.Call,name=="DelegateRoute"?invoke:target);
    il.Emit(OpCodes.Ret);
   }
   if(negative) {
    string mode=args[3];bool addressMode=mode.StartsWith("address-",StringComparison.Ordinal);if(addressMode)mode=mode.Substring(8);
    var malformed=new MethodReference(target.Name,target.ReturnType,target.DeclaringType){HasThis=false};malformed.Parameters.Add(new ParameterDefinition(rt));
    switch(mode) {
     case "return":malformed.ReturnType=mod.TypeSystem.Object;break;
     case "return-byref":malformed.ReturnType=new ByReferenceType(target.ReturnType);break;
     case "return-modifier":malformed.ReturnType=new OptionalModifierType(mod.TypeSystem.Object,target.ReturnType);break;
     case "argument":malformed.Parameters[0].ParameterType=mod.TypeSystem.Object;break;
     case "argument-byref":malformed.Parameters[0].ParameterType=new ByReferenceType(rt);break;
     case "argument-modifier":malformed.Parameters[0].ParameterType=new OptionalModifierType(mod.TypeSystem.Object,rt);break;
     case "instance":malformed.HasThis=true;break;
     case "generic":malformed.GenericParameters.Add(new GenericParameter("T",malformed));break;
     default:throw new InvalidOperationException("unknown negative "+mode);
    }
    Method(addressMode?"Address":"Direct").Body.Instructions.Single(i=>i.OpCode==(addressMode?OpCodes.Ldftn:OpCodes.Call)).Operand=malformed;
   }
   if(args[0]=="--runtime-interfaces-noalloc")RuntimeTypeDefaultCtorFixture.MarkNoAlloc(mod,Method("Direct"));
   mod.EntryPoint=Method(args[0]=="--runtime-interfaces-reflection"?"RunReflectionChecks":"RunArtifactChecks");
  }
  app.Write(path,new WriterParameters{Timestamp=0,DeterministicMvid=true});return true;
 }
}
