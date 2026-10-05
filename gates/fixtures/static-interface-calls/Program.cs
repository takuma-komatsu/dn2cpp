using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length != 1)
    throw new ArgumentException("expected AmbiguousDefault.dll");
string path = Path.GetFullPath(args[0]);
using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
using var library = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(path)!, "AmbiguousDefaultLib.dll"));
var module = app.MainModule;
var owner = module.GetType("AmbiguousDefault.Program");
var contract = library.MainModule.GetType("AmbiguousDefaultLib.IStaticBase");
foreach (bool valueReceiver in new[] { false, true })
foreach (string name in new[] { "Default", "DefaultGeneric", "Abstract", "AbstractGeneric" })
{
    var receiver = owner.NestedTypes.Single(t => t.Name == (valueReceiver ? "StaticBothValue" : "StaticBoth"));
    var body = owner.Methods.Single(m => m.Name == (valueReceiver ? "ConcreteValue" : "Concrete") + name);
    var text = body.Body.Instructions.SingleOrDefault(i => i.OpCode == OpCodes.Ldstr && (string)i.Operand == "rewritten");
    if (text is null)
        continue;
    MethodReference target = module.ImportReference(contract.Methods.Single(m => m.Name == name));
    if (name.EndsWith("Generic", StringComparison.Ordinal))
    {
        var closed = new GenericInstanceMethod(target);
        closed.GenericArguments.Add(module.TypeSystem.Int32);
        target = closed;
    }
    text.OpCode = OpCodes.Constrained;
    text.Operand = receiver;
    body.Body.GetILProcessor().InsertAfter(text, Instruction.Create(OpCodes.Call, target));
}
app.Write(path);
