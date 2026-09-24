using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length != 1)
    throw new ArgumentException("expected ReflectInvoke.dll");

string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
var module = assembly.MainModule;
var owner = module.GetType("LdftnLocalSubset.Program")
    ?? throw new InvalidOperationException("missing IL fixture owner");

MethodDefinition Find(string name) => owner.Methods.Single(m => m.Name == name);
var add = Find("Add");
var subtract = Find("Subtract");
var decorate = Find("Decorate");
var stored = Find("Stored");
var nopSeparated = Find("NopSeparated");
var nativeConvert = Find("NativeConvert");
var snapshotBeforeOverwrite = Find("SnapshotBeforeOverwrite");
var selected = Find("Selected");
var stackJoin = Find("StackJoin");
var closedStored = Find("ClosedStored");
var rawCalli = Find("RawCalli");

MethodReference DelegateCtor(MethodDefinition method)
{
    var ctor = new MethodReference(".ctor", module.TypeSystem.Void, method.ReturnType)
    {
        HasThis = true,
    };
    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
    return ctor;
}

MethodBody Body(MethodDefinition method, bool pointerLocal)
{
    var body = new MethodBody(method) { InitLocals = true, MaxStackSize = 3 };
    if (pointerLocal)
        body.Variables.Add(new VariableDefinition(module.TypeSystem.IntPtr));
    method.Body = body;
    return body;
}

{
    var il = Body(stored, pointerLocal: true).GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(stored));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(nopSeparated, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Nop);
    il.Emit(OpCodes.Newobj, DelegateCtor(nopSeparated));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(nativeConvert, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Conv_I);
    il.Emit(OpCodes.Newobj, DelegateCtor(nativeConvert));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(snapshotBeforeOverwrite, pointerLocal: true).GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Ldftn, subtract);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(snapshotBeforeOverwrite));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(selected, pointerLocal: true).GetILProcessor();
    var second = Instruction.Create(OpCodes.Ldftn, subtract);
    var join = Instruction.Create(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Brfalse, second);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Br, join);
    il.Append(second);
    il.Emit(OpCodes.Stloc_0);
    il.Append(join);
    il.Emit(OpCodes.Newobj, DelegateCtor(selected));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(stackJoin, pointerLocal: false).GetILProcessor();
    var second = Instruction.Create(OpCodes.Ldftn, subtract);
    var join = Instruction.Create(OpCodes.Newobj, DelegateCtor(stackJoin));
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Brfalse, second);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Br, join);
    il.Append(second);
    il.Append(join);
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(closedStored, pointerLocal: true).GetILProcessor();
    il.Emit(OpCodes.Ldstr, "C:");
    il.Emit(OpCodes.Ldftn, decorate);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(closedStored));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(rawCalli, pointerLocal: true).GetILProcessor();
    var callSite = new CallSite(module.TypeSystem.Int32);
    callSite.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldc_I4_7);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Calli, callSite);
    il.Emit(OpCodes.Ret);
}

string temporary = path + ".ldftn-local.tmp";
try
{
    assembly.Write(temporary, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    File.Move(temporary, path, overwrite: true);
}
finally
{
    if (File.Exists(temporary))
        File.Delete(temporary);
}
