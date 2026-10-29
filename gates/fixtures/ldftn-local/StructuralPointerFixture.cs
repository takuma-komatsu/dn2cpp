using System.Globalization;
using Mono.Cecil;

internal static class StructuralPointerFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--structural-pointer-negative") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var type = app.MainModule.GetType("Bad");
        var field = type.Fields.Single(f => f.Name == "Field");
        switch (args[3])
        {
            case "unchanged": break;
            case "fnptr":
                var pointer = new FunctionPointerType { ReturnType = app.MainModule.TypeSystem.Int32 };
                pointer.Parameters.Add(new ParameterDefinition(app.MainModule.TypeSystem.Int32));
                field.FieldType = pointer;
                break;
            case "byref": field.FieldType = new ByReferenceType(app.MainModule.TypeSystem.Int32); break;
            case "typed-reference": field.FieldType = app.MainModule.ImportReference(core.MainModule.GetType("System.TypedReference")); break;
            case "union":
                type.Attributes = (type.Attributes & ~TypeAttributes.LayoutMask) | TypeAttributes.ExplicitLayout;
                type.ClassSize = 16; type.PackingSize = 1;
                foreach (var member in type.Fields) member.Offset = 0;
                break;
            case "byref-like":
                var ctor = core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute").Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0);
                type.CustomAttributes.Add(new CustomAttribute(app.MainModule.ImportReference(ctor)));
                break;
            default: throw new ArgumentException("unknown structural pointer shape");
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("structural-pointer-negative:" + args[3] + ";compile-only");
        return true;
    }
}
