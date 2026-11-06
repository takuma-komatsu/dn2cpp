using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ConcreteVectorReductionFixture
{
    internal static readonly string[] Names = { "All", "Any", "None", "Count", "IndexOf", "LastIndexOf", "AllWhereAllBitsSet", "AnyWhereAllBitsSet", "NoneWhereAllBitsSet", "CountWhereAllBitsSet", "IndexOfWhereAllBitsSet", "LastIndexOfWhereAllBitsSet" };
    internal static readonly string[] Routes = { "direct", "address", "calli" };
    internal static bool Bits(string name) => name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal);
    internal static int Expected(string name, int width) => name switch
    {
        "All" or "Any" or "NoneWhereAllBitsSet" => 1,
        "Count" => width,
        "LastIndexOf" => width - 1,
        "IndexOfWhereAllBitsSet" or "LastIndexOfWhereAllBitsSet" => -1,
        _ => 0
    };
    internal static string Label(int width, string name, string route) => $"concrete-vector-reduction:{width}:{name}:{route}:pass";
    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--concrete-vector-reduction-oracle") { Oracle(); return true; }
        if (args.Length != 3 || args[0] != "--concrete-vector-reduction-fixture") return false;
        Build(args[1], args[2]); return true;
    }
    static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        foreach (int width in new[] { 2, 3 })
        foreach (string name in Names)
        foreach (string route in Routes)
        {
            var vector = width == 2 ? typeof(System.Numerics.Vector2) : typeof(System.Numerics.Vector3);
            var parameters = Bits(name) ? new[] { vector } : new[] { vector, typeof(float) };
            var target = typeof(System.Runtime.Intrinsics.Vector128).GetMethod(name, flags, null, parameters, null)!;
            var dynamic = new System.Reflection.Emit.DynamicMethod("Reduction_" + route, typeof(int), Type.EmptyTypes, typeof(ConcreteVectorReductionFixture).Module, true);
            var il = dynamic.GetILGenerator(); var local = il.DeclareLocal(vector);
            il.Emit(System.Reflection.Emit.OpCodes.Ldloca, local); il.Emit(System.Reflection.Emit.OpCodes.Initobj, vector);
            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local);
            if (!Bits(name)) il.Emit(System.Reflection.Emit.OpCodes.Ldc_R4, 0f);
            if (route == "direct") il.Emit(System.Reflection.Emit.OpCodes.Call, target);
            else
            {
                il.Emit(System.Reflection.Emit.OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var pointer = il.DeclareLocal(typeof(IntPtr));
                    il.Emit(System.Reflection.Emit.OpCodes.Stloc, pointer); il.Emit(System.Reflection.Emit.OpCodes.Ldloc, pointer);
                }
                il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Reflection.CallingConventions.Standard, target.ReturnType, parameters, null);
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            if (((Func<int>)dynamic.CreateDelegate(typeof(Func<int>)))() != Expected(name, width)) throw new InvalidOperationException(Label(width, name, route));
            Console.WriteLine(Label(width, name, route));
        }
    }
    internal static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ConcreteVectorReductions", new Version(1, 0)), "ConcreteVectorReductions", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(host);
        // A never-referenced method makes an application-level Diet reduction observable.
        var sentinel = new MethodDefinition("UnusedDietSentinel", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(sentinel); sentinel.Body.GetILProcessor().Emit(OpCodes.Ret);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        foreach (int width in new[] { 2, 3 })
        foreach (string name in Names)
        foreach (string route in Routes)
        {
            var definition = core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods.Single(m => m.Name == name && !m.HasGenericParameters && m.Parameters.Count == (Bits(name) ? 1 : 2) && m.Parameters[0].ParameterType.FullName == "System.Numerics.Vector" + width);
            var target = module.ImportReference(definition);
            var method = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(method); entry.Emit(OpCodes.Call, method);
            var il = method.Body.GetILProcessor(); var local = new VariableDefinition(target.Parameters[0].ParameterType); method.Body.Variables.Add(local);
            il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, local.VariableType); il.Emit(OpCodes.Ldloc, local);
            if (!Bits(name)) il.Emit(OpCodes.Ldc_R4, 0f);
            if (route == "direct") il.Emit(OpCodes.Call, target);
            else
            {
                il.Emit(OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var pointer = new VariableDefinition(module.TypeSystem.IntPtr); method.Body.Variables.Add(pointer);
                    il.Emit(OpCodes.Stloc, pointer); il.Emit(OpCodes.Ldloc, pointer);
                }
                var signature = new CallSite(target.ReturnType);
                foreach (var parameter in target.Parameters) signature.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, signature);
            }
            string label = Label(width, name, route); var pass = Instruction.Create(OpCodes.Ldstr, label);
            il.Emit(OpCodes.Ldc_I4, Expected(name, width)); il.Emit(OpCodes.Beq, pass);
            il.Emit(OpCodes.Ldstr, label); il.Emit(OpCodes.Newobj, module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!)); il.Emit(OpCodes.Throw);
            il.Append(pass); il.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!)); il.Emit(OpCodes.Ret);
        }
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }
}
