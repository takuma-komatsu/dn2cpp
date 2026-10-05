using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

string[] byRefModes = ["--byref-overwrite", "--byref-overwrite-int64", "--byref-copy"];
string[] originModes = ["--delegate-origin-argument", "--delegate-origin-field", "--delegate-origin-array",
    "--delegate-origin-checked-conv", "--delegate-origin-arithmetic", "--delegate-origin-box",
    "--delegate-origin-call", "--delegate-origin-local", "--delegate-origin-stack-join",
    "--delegate-origin-byref-argument"];
if (args.Length is < 1 or > 2 || (args.Length == 2
    && !byRefModes.Contains(args[1]) && !originModes.Contains(args[1])))
    throw new ArgumentException("expected ReflectInvoke.dll [" + string.Join("|", byRefModes.Concat(originModes)) + "]");

string? byRefMode = args.Length == 2 && byRefModes.Contains(args[1]) ? args[1] : null;
string? originMode = args.Length == 2 && originModes.Contains(args[1]) ? args[1] : null;

string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
var module = assembly.MainModule;
var owner = module.GetType("LdftnLocalSubset.Program")
    ?? throw new InvalidOperationException("missing IL fixture owner");

MethodDefinition Find(string name) => owner.Methods.Single(m => m.Name == name);
MethodDefinition FindOn(string type, string name) =>
    (module.GetType("LdftnLocalSubset." + type)
        ?? throw new InvalidOperationException("missing IL fixture type " + type))
    .Methods.Single(m => m.Name == name);
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
var deadOrigins = Find("DeadOrigins");
var virtualStored = Find("VirtualStored");
var instanceStored = Find("InstanceStored");
var int64Stored = Find("Int64Stored");
var originBoundary = Find("OriginBoundary");
var fromArgument = Find("FromArgument");
var fromField = Find("FromField");
var returnPointer = Find("ReturnPointer");
var originPointer = owner.Fields.Single(f => f.Name == "OriginPointer");
var sealedInterface = Find("SealedInterface");
var sealedGenericInterface = Find("SealedGenericInterface");
var invokeVirtualLoad = Find("InvokeVirtualLoad");
var valueTypeEquals = Find("ValueTypeEquals");
var valueTypeHash = Find("ValueTypeHash");
var valueTypeText = Find("ValueTypeText");
var typeDefStubs = new[] { Find("TypeDefInt"), Find("TypeDefString"), Find("TypeDefInstance"), Find("TypeDefGeneric") };
var memberRefTarget = module.GetType("LdftnLocalSubset.MemberRefTarget")
    ?? throw new InvalidOperationException("missing TypeDef MemberRef target");
var memberRefBox = module.GetType("LdftnLocalSubset.MemberRefBox`1")
    ?? throw new InvalidOperationException("missing generic TypeDef MemberRef target");
var scale = FindOn("VirtualBase", "Scale");
var offset = FindOn("InstanceHolder", "Offset");
var sealedScale = FindOn("ISealedScale", "Scale");
var sealedShift = new GenericInstanceMethod(FindOn("ISealedScale", "Shift"));
sealedShift.GenericArguments.Add(module.TypeSystem.Int32);

foreach (var (bodyName, slotName, returnType, takesOther) in new[]
    {
        ("Render", "ToString", module.TypeSystem.String, false),
        ("Same", "Equals", module.TypeSystem.Boolean, true),
        ("Hash", "GetHashCode", module.TypeSystem.Int32, false),
    })
{
    var body = FindOn("ObjectMethodImpl", bodyName);
    var slot = new MethodReference(slotName, returnType, module.TypeSystem.Object) { HasThis = true };
    if (takesOther)
        slot.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
    body.Overrides.Clear();
    body.Overrides.Add(slot);
}

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

{
    // The load after the branch never runs and names a member nothing resolves.
    var missing = new MethodReference("LdftnLocalMissing", module.TypeSystem.Int32, module.TypeSystem.Object);
    missing.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
    var il = Body(deadOrigins, pointerLocal: true).GetILProcessor();
    var join = Instruction.Create(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, subtract);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Br, join);
    il.Emit(OpCodes.Ldftn, missing);
    il.Emit(OpCodes.Pop);
    il.Append(join);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(deadOrigins));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(virtualStored, pointerLocal: true).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Dup);
    il.Emit(OpCodes.Ldvirtftn, scale);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(virtualStored));
    il.Emit(OpCodes.Ret);
}

{
    var il = Body(instanceStored, pointerLocal: true).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldftn, offset);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Newobj, DelegateCtor(instanceStored));
    il.Emit(OpCodes.Ret);
}

{
    var body = Body(int64Stored, pointerLocal: false);
    body.Variables.Add(new VariableDefinition(module.TypeSystem.Int64));
    var il = body.GetILProcessor();
    il.Emit(OpCodes.Ldnull);
    il.Emit(OpCodes.Ldftn, add);
    il.Emit(OpCodes.Conv_U8);
    il.Emit(OpCodes.Stloc_0);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Conv_U);
    il.Emit(OpCodes.Newobj, DelegateCtor(int64Stored));
    il.Emit(OpCodes.Ret);
}

{
    var body = Body(originBoundary, pointerLocal: originMode == "--delegate-origin-local");
    var il = body.GetILProcessor();
    switch (originMode)
    {
        case "--delegate-origin-argument":
        case "--delegate-origin-byref-argument":
        {
            // The constructor body has no load origin of its own.
            var argumentBody = Body(fromArgument, pointerLocal: originMode == "--delegate-origin-byref-argument");
            var argumentIl = argumentBody.GetILProcessor();
            if (originMode == "--delegate-origin-byref-argument")
            {
                argumentIl.Emit(OpCodes.Ldarg_0);
                argumentIl.Emit(OpCodes.Stloc_0);
                argumentIl.Emit(OpCodes.Ldloca_S, argumentBody.Variables[0]);
                argumentIl.Emit(OpCodes.Pop);
            }
            argumentIl.Emit(OpCodes.Ldnull);
            argumentIl.Emit(originMode == "--delegate-origin-byref-argument" ? OpCodes.Ldloc_0 : OpCodes.Ldarg_0);
            argumentIl.Emit(OpCodes.Newobj, DelegateCtor(fromArgument));
            argumentIl.Emit(OpCodes.Ret);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Call, fromArgument);
            break;
        }
        case "--delegate-origin-field":
        {
            // Store in the caller; the constructor body only reads the field.
            var fieldIl = Body(fromField, pointerLocal: false).GetILProcessor();
            fieldIl.Emit(OpCodes.Ldnull);
            fieldIl.Emit(OpCodes.Ldsfld, originPointer);
            fieldIl.Emit(OpCodes.Newobj, DelegateCtor(fromField));
            fieldIl.Emit(OpCodes.Ret);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Stsfld, originPointer);
            il.Emit(OpCodes.Call, fromField);
            break;
        }
        case "--delegate-origin-array":
            body.Variables.Add(new VariableDefinition(new ArrayType(module.TypeSystem.IntPtr)));
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Newarr, module.TypeSystem.IntPtr);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Stelem_I);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldelem_I);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        case "--delegate-origin-checked-conv":
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Conv_Ovf_U_Un);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        case "--delegate-origin-arithmetic":
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        case "--delegate-origin-box":
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Box, module.TypeSystem.IntPtr);
            il.Emit(OpCodes.Unbox_Any, module.TypeSystem.IntPtr);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        case "--delegate-origin-call":
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Call, returnPointer);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        case "--delegate-origin-local":
        {
            var join = Instruction.Create(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Brtrue, join);
            il.Emit(OpCodes.Ldftn, subtract);
            il.Emit(OpCodes.Call, returnPointer);
            il.Emit(OpCodes.Stloc_0);
            il.Append(join);
            il.Emit(OpCodes.Newobj, DelegateCtor(originBoundary));
            break;
        }
        default:
        {
            var second = Instruction.Create(OpCodes.Ldftn, subtract);
            var join = Instruction.Create(OpCodes.Newobj, DelegateCtor(originBoundary));
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Brfalse, second);
            il.Emit(OpCodes.Ldftn, add);
            il.Emit(OpCodes.Br, join);
            il.Append(second);
            if (originMode == "--delegate-origin-stack-join")
                il.Emit(OpCodes.Call, returnPointer);
            il.Append(join);
            break;
        }
    }
    il.Emit(OpCodes.Ret);
}

// C# loads a sealed interface member with ldftn; ldvirtftn of one binds its own
// body as well, whatever virtual of its signature the receiver's class declares.
foreach (var (stub, target) in new[] { (sealedInterface, (MethodReference)sealedScale),
    (sealedGenericInterface, sealedShift) })
{
    var il = Body(stub, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Dup);
    il.Emit(OpCodes.Ldvirtftn, target);
    il.Emit(OpCodes.Newobj, DelegateCtor(stub));
    il.Emit(OpCodes.Ret);
}

// A delegate type is sealed, so ldvirtftn of its Invoke binds what ldftn binds.
{
    var func = (GenericInstanceType)invokeVirtualLoad.ReturnType;
    var definition = func.Resolve();
    var invoke = new MethodReference("Invoke", definition.GenericParameters[1], func) { HasThis = true };
    invoke.Parameters.Add(new ParameterDefinition(definition.GenericParameters[0]));
    var il = Body(invokeVirtualLoad, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Dup);
    il.Emit(OpCodes.Ldvirtftn, module.ImportReference(invoke));
    il.Emit(OpCodes.Newobj, DelegateCtor(invokeVirtualLoad));
    il.Emit(OpCodes.Ret);
}

// Each ValueType stub callvirts System.ValueType's own override on its receiver.
var valueType = new TypeReference("System", "ValueType", module, module.TypeSystem.CoreLibrary);
var valueTypeOverrides = new (MethodDefinition Stub, string Name, TypeReference Return, bool TakesOther)[]
{
    (valueTypeEquals, "Equals", module.TypeSystem.Boolean, true),
    (valueTypeHash, "GetHashCode", module.TypeSystem.Int32, false),
    (valueTypeText, "ToString", module.TypeSystem.String, false),
};
foreach (var (stub, name, returnType, takesOther) in valueTypeOverrides)
{
    var target = new MethodReference(name, returnType, valueType) { HasThis = true };
    if (takesOther)
        target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
    var il = Body(stub, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    if (takesOther)
        il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Callvirt, target);
    il.Emit(OpCodes.Ret);
}

// A MethodImpl binds the renamed override to the slot its C# name overrode; a
// second pass over the rewritten image finds it renamed.
{
    var renamed = (module.GetType("LdftnLocalSubset.RenamedSlotOverride")
            ?? throw new InvalidOperationException("missing IL fixture type RenamedSlotOverride"))
        .Methods.Single(m => m.Name is "Scale" or "Rescale");
    renamed.Name = "Rescale";
    renamed.Overrides.Clear();
    renamed.Overrides.Add(FindOn("RenamedSlotBase", "Scale"));
}

// A MethodImpl moves each interface slot from its explicit stub to the named body
// and binds the renamed override Show to Object.ToString; a second pass finds all
// of them moved.
foreach (var (type, stubName, bodyName) in new[]
    {
        ("RenamedFillerImpl", "LdftnLocalSubset.IRenamedFiller.Measure", "Weigh"),
        ("RenamedFillerBox`1", "LdftnLocalSubset.IRenamedFiller.Measure", "Weigh"),
        ("RenamedSource", "LdftnLocalSubset.IRenamedSource<System.String>.Take", "Fetch"),
    })
{
    var body = FindOn(type, bodyName);
    var stub = FindOn(type, stubName);
    if (stub.Overrides.Count != 0)
    {
        body.Overrides.Add(stub.Overrides[0]);
        stub.Overrides.Clear();
    }
}
{
    var show = (module.GetType("LdftnLocalSubset.RenamedObjectReuse")
            ?? throw new InvalidOperationException("missing IL fixture type RenamedObjectReuse"))
        .Methods.Single(m => m.Name is "ToString" or "Show");
    show.Name = "Show";
    show.Overrides.Clear();
    show.Overrides.Add(new MethodReference("ToString", module.TypeSystem.String, module.TypeSystem.Object)
    {
        HasThis = true,
    });
}

string temporary = path + ".ldftn-local.tmp";

MethodReference TypeDefReference(MethodDefinition target)
{
    var reference = new MethodReference(target.Name, target.ReturnType, target.DeclaringType)
    {
        HasThis = target.HasThis,
    };
    foreach (var parameter in target.Parameters)
        reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
    foreach (var parameter in target.GenericParameters)
        reference.GenericParameters.Add(new GenericParameter(parameter.Name, reference));
    return reference;
}

for (int i = 0; i < typeDefStubs.Length; i++)
{
    var target = memberRefTarget.Methods.Single(m => i switch
    {
        0 => m.Name == "Select" && m.Parameters[0].ParameterType.MetadataType == MetadataType.Int32,
        1 => m.Name == "Select" && m.Parameters[0].ParameterType.MetadataType == MetadataType.String,
        2 => m.Name == "Shift",
        _ => m.Name == "Echo",
    });
    MethodReference reference = TypeDefReference(target);
    if (i == 3)
    {
        var generic = new GenericInstanceMethod(reference);
        generic.GenericArguments.Add(module.TypeSystem.Int32);
        reference = generic;
    }
    var il = Body(typeDefStubs[i], pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    if (i == 2)
        il.Emit(OpCodes.Ldarg_1);
    il.Emit(i == 2 ? OpCodes.Callvirt : OpCodes.Call, reference);
    il.Emit(OpCodes.Ret);
}
{
    var stub = memberRefBox.Methods.Single(m => m.Name == "ThroughDefinition");
    var target = memberRefBox.Methods.Single(m => m.Name == "Read");
    var closedOwner = new GenericInstanceType(memberRefBox);
    foreach (var parameter in memberRefBox.GenericParameters)
        closedOwner.GenericArguments.Add(parameter);
    var reference = TypeDefReference(target);
    reference.DeclaringType = closedOwner;
    var il = Body(stub, pointerLocal: false).GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Call, reference);
    il.Emit(OpCodes.Ret);
}

try
{
    assembly.Write(temporary, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    File.Move(temporary, path, overwrite: true);
    using var written = AssemblyDefinition.ReadAssembly(path);
    foreach (var (type, name) in typeDefStubs.Select(m => (m.DeclaringType.FullName, m.Name))
                 .Append((memberRefBox.FullName, "ThroughDefinition")))
    {
        var stub = written.MainModule.GetType(type).Methods.Single(m => m.Name == name);
        var reference = (MethodReference)stub.Body.Instructions.Single(i => i.OpCode.Code is Code.Call or Code.Callvirt).Operand;
        if (reference is GenericInstanceMethod generic)
            reference = generic.ElementMethod;
        var expectedParent = name == "ThroughDefinition" ? TokenType.TypeSpec : TokenType.TypeDef;
        if (reference.MetadataToken.TokenType != TokenType.MemberRef
            || reference.DeclaringType.MetadataToken.TokenType != expectedParent)
            throw new InvalidOperationException("fixture lost its TypeDef-parent MemberRef: " + name);
    }
    Console.WriteLine("TypeDef-parent MemberRef fixtures verified");
    if (originMode is not null)
    {
        if (originMode is "--delegate-origin-argument" or "--delegate-origin-field" or "--delegate-origin-byref-argument")
        {
            string name = originMode == "--delegate-origin-field" ? "FromField" : "FromArgument";
            var constructorBody = written.MainModule.GetType(owner.FullName).Methods.Single(m => m.Name == name).Body;
            if (constructorBody.Instructions.Any(i => i.OpCode.Code is Code.Ldftn or Code.Ldvirtftn)
                || !constructorBody.Instructions.Any(i => i.OpCode.Code == Code.Newobj))
                throw new InvalidOperationException("fixture lost its delegate constructor without a load origin: " + name);
        }
        Console.WriteLine("delegate origin fixture verified: " + originMode);
    }
}
finally
{
    if (File.Exists(temporary))
        File.Delete(temporary);
}
