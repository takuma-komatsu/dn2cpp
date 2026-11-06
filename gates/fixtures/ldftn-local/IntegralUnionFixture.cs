using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class IntegralUnionFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--integral-union-negative") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var module = app.MainModule;
        var type = module.GetType("Bad");
        var field = type.Fields.Single(f => f.Name == "Field");
        switch (args[3])
        {
            case "unchanged": break;
            case "float": field.FieldType = module.TypeSystem.Single; break;
            case "boolean": field.FieldType = module.TypeSystem.Boolean; break;
            case "char": field.FieldType = module.TypeSystem.Char; break;
            case "native-int": field.FieldType = module.TypeSystem.IntPtr; break;
            case "pointer": field.FieldType = new PointerType(module.TypeSystem.Byte); break;
            case "byref": field.FieldType = new ByReferenceType(module.TypeSystem.Int32); break;
            case "fnptr": field.FieldType = new FunctionPointerType { ReturnType = module.TypeSystem.Int32 }; break;
            case "intrinsic": field.FieldType = module.ImportReference(core.MainModule.GetType("System.Runtime.InteropServices.GCHandle")); break;
            case "nested-union": field.FieldType = module.GetType("InnerUnion"); break;
            case "byval-array": field.FieldType = new ArrayType(module.TypeSystem.Int32); field.MarshalInfo = new FixedArrayMarshalInfo { Size = 2, ElementType = NativeType.I4 }; break;
            case "missing-offset": field.Offset = -1; break;
            case "empty": type.Fields.Clear(); break;
            case "misaligned": field.Offset = 1; break;
            case "undersized": type.ClassSize = 2; break;
            case "invalid-size": type.ClassSize = 9; break;
            case "layout-ref-overlap":
                field.FieldType = module.TypeSystem.Object;
                app.EntryPoint.Body = new MethodBody(app.EntryPoint);
                var instructions = app.EntryPoint.Body.Instructions;
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                instructions.Add(Instruction.Create(OpCodes.Newarr, type));
                instructions.Add(Instruction.Create(OpCodes.Pop));
                instructions.Add(Instruction.Create(OpCodes.Ret));
                break;
            case "ref-overlap": field.FieldType = module.TypeSystem.Object; break;
            case "byref-like":
                var ctor = core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute").Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0);
                type.CustomAttributes.Add(new CustomAttribute(module.ImportReference(ctor))); break;
            default: throw new ArgumentException("unknown integral union shape");
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("integral-union-metadata:" + args[3] + ";compile-only");
        return true;
    }
}
