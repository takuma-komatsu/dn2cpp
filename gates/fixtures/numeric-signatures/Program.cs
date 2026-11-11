using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length == 3 && args[0] == "--check-bigmul-normalized")
{
    MetadataShape.CheckNormalizedBigMul(args[1], args[2]);
    return;
}
if (args.Length != 2) throw new ArgumentException("expected case output.dll");
string mode = args[0];
using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("NumericSignatures", new Version(1, 0)),
    "NumericSignatures", ModuleKind.Console);
var module = assembly.MainModule;
var program = new TypeDefinition("NumericSignatures", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
    module.TypeSystem.Object);
module.Types.Add(program);
var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
program.Methods.Add(entry);
assembly.EntryPoint = entry;
entry.Body.InitLocals = true;
var il = entry.Body.GetILProcessor();
foreach (string name in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
{
    il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
    il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(name)!.SetMethod!));
}
if (mode == "bigmul-control")
{
    foreach (Type type in new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(nint), typeof(nuint) })
        foreach (bool address in new[] { false, true }) EmitBigMul(type, address, "control");
}
else if (mode.StartsWith("bigmul-", StringComparison.Ordinal))
{
    string change = mode[7..];
    bool address = change.EndsWith("-address", StringComparison.Ordinal);
    change = change[..change.LastIndexOf('-')];
    EmitBigMul(change.StartsWith("native-", StringComparison.Ordinal) ? typeof(nint) : typeof(int), address, change);
}
else if (mode.StartsWith("int128-", StringComparison.Ordinal) || mode.StartsWith("uint128-", StringComparison.Ordinal))
{
    int separator = mode.IndexOf('-');
    Type target = mode[..separator] == "int128" ? typeof(Int128) : typeof(UInt128);
    string change = mode[(separator + 1)..];
    if (change == "control")
    {
        EmitChecked(target, "control");
        EmitChecked(target, "interface-control");
        EmitConstrainedTypeSpecControl(target);
    }
    else EmitChecked(target, change);
}
else throw new ArgumentException("unknown numeric signature fixture " + mode);
il.Emit(OpCodes.Ret);
string output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
if (mode.StartsWith("bigmul-int-owner-typespec-", StringComparison.Ordinal))
    MetadataShape.PlainBigMulOwner(output);
MetadataShape.Check(output, mode);
Console.WriteLine("numeric-signature-fixture:" + mode);

void EmitBigMul(Type type, bool address, string change)
{
    bool native = type == typeof(nint) || type == typeof(nuint);
    var operand = module.ImportReference(type);
    var member = module.ImportReference(type.GetMethods().Single(method => method.Name == "BigMul"));
    switch (change)
    {
        case "control": break;
        case "int-return": member.ReturnType = module.TypeSystem.UInt64; break;
        case "native-out": member.Parameters[2].ParameterType = new ByReferenceType(module.TypeSystem.Int64); break;
        case "int-modifier": member.Parameters[0].ParameterType = Modified(member.Parameters[0].ParameterType); break;
        case "int-owner-modifier":
        case "int-owner-typespec": member.DeclaringType = Modified(member.DeclaringType); break;
        default: throw new ArgumentException("unknown BigMul mutation " + change);
    }
    if (address) il.Emit(OpCodes.Ldftn, member);
    else
    {
        il.Emit(OpCodes.Ldc_I4_2);
        ConvertOperand();
        il.Emit(OpCodes.Ldc_I4_3);
        ConvertOperand();
        if (native)
        {
            var low = new VariableDefinition(change == "native-out" ? module.TypeSystem.Int64 : operand);
            entry.Body.Variables.Add(low);
            il.Emit(OpCodes.Ldloca, low);
        }
        il.Emit(OpCodes.Call, member);
    }
    il.Emit(OpCodes.Pop);

    void ConvertOperand()
    {
        if (native) il.Emit(OpCodes.Conv_I);
        else if (type == typeof(long) || type == typeof(ulong)) il.Emit(OpCodes.Conv_I8);
    }
}

void EmitChecked(Type target, string change)
{
    bool interfaceForm = change.StartsWith("interface-", StringComparison.Ordinal) || change.StartsWith("constrained-", StringComparison.Ordinal);
    var self = module.ImportReference(target);
    TypeReference owner = self;
    TypeReference result = self;
    if (interfaceForm)
    {
        var definition = module.ImportReference(typeof(INumberBase<>));
        var closed = new GenericInstanceType(definition);
        closed.GenericArguments.Add(self);
        owner = closed;
        result = definition.GenericParameters[0];
    }
    var member = new MethodReference("CreateChecked", result, owner);
    var source = new GenericParameter("TOther", member);
    member.GenericParameters.Add(source);
    member.Parameters.Add(new ParameterDefinition(source));
    TypeReference argument = module.TypeSystem.Int32;
    TypeReference constrained = self;
    switch (change)
    {
        case "control":
        case "interface-control": break;
        case "member-return": member.ReturnType = Modified(result); break;
        case "member-return-type": member.ReturnType = module.ImportReference(target == typeof(Int128) ? typeof(UInt128) : typeof(Int128)); break;
        case "member-instance": member.HasThis = true; break;
        case "member-vararg": member.CallingConvention = MethodCallingConvention.VarArg; break;
        case "member-generic-arity": member.GenericParameters.Add(new GenericParameter("TExtra", member)); break;
        case "methodspec-arity": break;
        case "interface-owner-arity": ((GenericInstanceType)owner).GenericArguments.Add(self); break;
        case "constrained-wrong-target": constrained = Modified(module.ImportReference(target == typeof(Int128) ? typeof(UInt128) : typeof(Int128))); break;
        case "member-parameter": member.Parameters[0].ParameterType = Modified(source); break;
        case "methodspec-modifier": argument = Modified(argument); break;
        case "methodspec-pinned": argument = new PinnedType(argument); break;
        case "owner-modifier":
        case "interface-owner-modifier": member.DeclaringType = Modified(owner); break;
        case "constrained-modifier": constrained = Modified(self); break;
        case "constrained-pinned": constrained = new PinnedType(self); break;
        default: throw new ArgumentException("unknown CreateChecked mutation " + change);
    }
    var call = new GenericInstanceMethod(member);
    call.GenericArguments.Add(argument);
    if (change == "methodspec-arity") call.GenericArguments.Add(argument);
    il.Emit(OpCodes.Ldc_I4, 7);
    if (interfaceForm) il.Emit(OpCodes.Constrained, constrained);
    il.Emit(OpCodes.Call, call);
    il.Emit(OpCodes.Pop);
}

void EmitConstrainedTypeSpecControl(Type target)
{
    var body = new MethodDefinition("ConstrainedControl", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
    program.Methods.Add(body);
    var self = new GenericParameter("TSelf", body);
    body.GenericParameters.Add(self);
    var definition = module.ImportReference(typeof(INumberBase<>));
    var owner = new GenericInstanceType(definition);
    owner.GenericArguments.Add(self);
    self.Constraints.Add(new GenericParameterConstraint(owner));
    var member = new MethodReference("CreateChecked", definition.GenericParameters[0], owner);
    var source = new GenericParameter("TOther", member);
    member.GenericParameters.Add(source);
    member.Parameters.Add(new ParameterDefinition(source));
    var create = new GenericInstanceMethod(member);
    create.GenericArguments.Add(module.TypeSystem.Int32);
    var bodyIl = body.Body.GetILProcessor();
    bodyIl.Emit(OpCodes.Ldc_I4, 7);
    bodyIl.Emit(OpCodes.Constrained, self);
    bodyIl.Emit(OpCodes.Call, create);
    bodyIl.Emit(OpCodes.Pop);
    bodyIl.Emit(OpCodes.Ret);
    var probe = new GenericInstanceMethod(body);
    probe.GenericArguments.Add(module.ImportReference(target));
    il.Emit(OpCodes.Call, probe);
}

TypeReference Modified(TypeReference type) => new OptionalModifierType(module.ImportReference(typeof(IsConst)), type);
