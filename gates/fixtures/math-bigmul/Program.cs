using System.Globalization;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args is ["--oracle"])
{
    var methods = typeof(Math).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
    var first = methods.Single(m => m.Name == "BigMul" && m.GetParameters() is var p && p.Length == 3
        && p[0].ParameterType == typeof(uint)).CreateDelegate<UInt32First>();
    var second = methods.Single(m => m.Name == "BigMul" && m.GetParameters() is var p && p.Length == 3
        && p[1].ParameterType == typeof(uint)).CreateDelegate<UInt64First>();
    Console.WriteLine("== mixed-width BigMul ==");
    foreach (string route in new[] { "direct", "address" })
    foreach (bool reverse in new[] { false, true })
    foreach (var test in new (string Name, uint Narrow, ulong Wide)[]
        { ("max", uint.MaxValue, ulong.MaxValue), ("high", 0x80000000, 0x8000000000000001),
          ("zero", 0, ulong.MaxValue), ("carry", 2, 0x8000000000000000) })
    {
        if (route == "address" && test.Name is "zero" or "carry") continue;
        ulong low;
        ulong high = reverse ? second(test.Wide, test.Narrow, out low) : first(test.Narrow, test.Wide, out low);
        UInt128 expected = (UInt128)test.Narrow * test.Wide;
        if (high != (ulong)(expected >> 64) || low != (ulong)expected)
            throw new InvalidOperationException("CoreLib mixed-width product differs from the full product");
        Console.WriteLine($"{route}{(reverse ? 64 : 32)}-{test.Name}={high}:{low}");
    }
    Console.WriteLine("mixed-width BigMul end");
    return;
}
if (args.Length != 4 || args[3] is not ("mixed" or "uint-first" or "ulong-first" or "bad-signed" or "bad-value"))
    throw new ArgumentException("Expected sample assembly, CoreLib, output assembly, and fixture mode");
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
using var app = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { InMemory = true, AssemblyResolver = resolver });
using var core = AssemblyDefinition.ReadAssembly(args[1]);
var module = app.MainModule;
var section = module.GetType("Dn2Cpp.MathMixedBigMulSubset");
var math = core.MainModule.GetType("System.Math");
int calls = 0, addresses = 0;
foreach (var method in section.Methods.Where(m => m.HasBody))
foreach (var instruction in method.Body.Instructions)
{
    if (instruction.Operand is not MethodReference old || old.DeclaringType.FullName != section.FullName
        || old.Name is not ("Multiply32x64" or "Multiply64x32")) continue;
    bool reverse = old.Name == "Multiply64x32";
    if (args[3] == "uint-first" && reverse || args[3] == "ulong-first" && !reverse) continue;
    if (args[3].StartsWith("bad-", StringComparison.Ordinal) && (!reverse || instruction.OpCode != OpCodes.Call || calls != 0)) continue;
    var target = math.Methods.Single(m => m.Name == "BigMul" && m.Parameters.Count == 3
        && m.Parameters[reverse ? 1 : 0].ParameterType.MetadataType == MetadataType.UInt32);
    var reference = module.ImportReference(target);
    if (args[3] == "bad-signed") reference.Parameters[2].ParameterType = new ByReferenceType(module.TypeSystem.Int64);
    if (args[3] == "bad-value")
    {
        reference.Parameters[2].ParameterType = module.TypeSystem.UInt64;
        if (instruction.Previous.OpCode != OpCodes.Ldloca && instruction.Previous.OpCode != OpCodes.Ldloca_S)
            throw new InvalidOperationException("The literal call no longer loads its out local immediately before the call");
        instruction.Previous.OpCode = OpCodes.Ldloc;
    }
    instruction.Operand = reference;
    if (instruction.OpCode == OpCodes.Ldftn) addresses++;
    else if (instruction.OpCode == OpCodes.Call) calls++;
    else throw new InvalidOperationException("Unexpected mixed-width call-site opcode");
}
if (calls == 0 || (!args[3].StartsWith("bad-", StringComparison.Ordinal) && addresses == 0))
    throw new InvalidOperationException("Mixed-width direct/address fixture calls were not rewritten");
module.EntryPoint = section.Methods.Single(m => m.Name == "Run");
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
app.Write(args[2], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
Console.WriteLine($"mixed-bigmul={args[3]}:calls={calls}:addresses={addresses}");

delegate ulong UInt32First(uint a, ulong b, out ulong low);
delegate ulong UInt64First(ulong a, uint b, out ulong low);
