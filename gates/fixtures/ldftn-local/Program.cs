using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

string[] byRefModes = ["--byref-overwrite", "--byref-overwrite-int64", "--byref-copy"];
if (args.Length is < 1 or > 2 || (args.Length == 2 && !byRefModes.Contains(args[1])))
    throw new ArgumentException("expected ReflectInvoke.dll [" + string.Join("|", byRefModes) + "]");

string? byRefMode = args.Length == 2 ? args[1] : null;

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
    // Every byref mode overwrites the stored Add with Subtract through the
    // local's address; --byref-copy then builds the delegate from a copy.
    bool int64 = byRefMode == "--byref-overwrite-int64";
    var body = Body(stored, pointerLocal: !int64);
    if (int64)
        body.Variables.Add(new VariableDefinition(module.TypeSystem.Int64));
    if (byRefMode == "--byref-copy")
        body.Variables.Add(new VariableDefinition(module.TypeSystem.IntPtr));
    var il = body.GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    if (int64)
        il.Emit(OpCodes.Conv_U8);
    il.Emit(OpCodes.Stloc_0);
    if (byRefMode is not null)
    {
        il.Emit(OpCodes.Ldloca_S, body.Variables[0]);
        il.Emit(OpCodes.Ldftn, subtract);
        if (int64)
            il.Emit(OpCodes.Conv_U8);
        il.Emit(int64 ? OpCodes.Stind_I8 : OpCodes.Stind_I);
    }
    il.Emit(OpCodes.Ldloc_0);
    if (int64)
        il.Emit(OpCodes.Conv_U);
    if (byRefMode == "--byref-copy")
    {
        il.Emit(OpCodes.Stloc_1);
        il.Emit(OpCodes.Ldloc_1);
    }
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
