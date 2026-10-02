using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length != 1)
    throw new ArgumentException("expected KnownNullJoinsOnly.dll");
string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
var module = assembly.MainModule;
var owner = module.GetType("KnownNullJoinSubset.Program")
    ?? throw new InvalidOperationException("missing stack-join fixture owner");
var join = owner.Methods.Single(m => m.Name == "NullJoin");
var joinCall = (MethodReference)join.Body.Instructions.Single(i => i.OpCode == OpCodes.Call).Operand;
join.Body = new MethodBody(join) { MaxStackSize = 3 };
var joinIl = join.Body.GetILProcessor();
var secondNull = Instruction.Create(OpCodes.Ldnull);
var consumeNull = Instruction.Create(OpCodes.Call, joinCall);
joinIl.Emit(OpCodes.Ldstr, ",");
joinIl.Emit(OpCodes.Ldarg_0);
joinIl.Emit(OpCodes.Brtrue, secondNull);
joinIl.Emit(OpCodes.Ldnull);
joinIl.Emit(OpCodes.Br, consumeNull);
joinIl.Append(secondNull);
joinIl.Append(consumeNull);
joinIl.Emit(OpCodes.Ret);

var scan = owner.Methods.Single(m => m.Name == "BackedgeContains");
var scanInstructions = scan.Body.Instructions.ToArray();
int consumeIndex = Array.FindLastIndex(scanInstructions, i => i.OpCode == OpCodes.Call);
int comparerIndex = Array.FindIndex(scanInstructions, i => i.OpCode == OpCodes.Ldnull);
if (comparerIndex < 0 || comparerIndex >= consumeIndex
    || scanInstructions[consumeIndex].Operand is not MethodReference { Name: "Contains" } contains)
    throw new InvalidOperationException("unexpected comparer operand shape");
var prefix = scanInstructions.Take(comparerIndex).ToArray();
var oldLocals = scan.Body.Variables.ToArray();
scan.Body = new MethodBody(scan) { InitLocals = true, MaxStackSize = 6 };
foreach (var local in oldLocals)
    scan.Body.Variables.Add(local);
var scanIl = scan.Body.GetILProcessor();
foreach (var instruction in prefix)
    scanIl.Append(instruction);
var loop = Instruction.Create(OpCodes.Ldarg_0);
var consumeComparer = Instruction.Create(OpCodes.Call, contains);
var constructor = module.GetType("KnownNullJoinSubset.ModuloComparer").Methods.Single(m => m.IsConstructor);
scanIl.Emit(OpCodes.Ldnull);
scanIl.Append(loop);
scanIl.Emit(OpCodes.Brfalse, consumeComparer);
scanIl.Emit(OpCodes.Pop);
scanIl.Emit(OpCodes.Newobj, constructor);
scanIl.Emit(OpCodes.Ldc_I4_0);
scanIl.Emit(OpCodes.Starg_S, scan.Parameters[0]);
scanIl.Emit(OpCodes.Br, loop);
scanIl.Append(consumeComparer);
scanIl.Emit(OpCodes.Ret);

assembly.Write(path);
