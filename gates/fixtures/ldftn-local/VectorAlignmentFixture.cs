using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class VectorAlignmentFixture
{
    internal static readonly int[] Widths = { 0, 64, 128, 256, 512 };
    internal static readonly string[] Routes = { "direct", "address", "calli" };
    internal static readonly string[] Properties = { "Alignment", "ElementCount", "IsHardwareAccelerated" };
    internal static string Owner(int width) => width == 0 ? "System.Numerics.Vector" : "System.Runtime.Intrinsics.Vector" + width;
    internal static IEnumerable<(int Width, bool Decimal, string Property, string Route)> Cases()
    {
        foreach (int width in Widths)
        foreach (bool dec in new[] { false, true })
        foreach (string property in Properties)
        foreach (string route in Routes) yield return (width, dec, property, route);
    }
    static string Label(int width, bool dec, string property, string route) => $"vector-property:{width}:{(dec ? "decimal" : "int")}:{property}:{route}:pass";

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--vector-alignment-oracle") { Oracle(); return true; }
        if (args.Length != 3 || args[0] != "--vector-alignment-fixture") return false;
        Build(args[1], args[2]); return true;
    }

    static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var core = typeof(object).Assembly;
        Func<int> Call(System.Reflection.MethodInfo target, string route)
        {
            var dynamic = new System.Reflection.Emit.DynamicMethod("Property_" + route, typeof(int), Type.EmptyTypes, typeof(VectorAlignmentFixture).Module, true);
            var il = dynamic.GetILGenerator();
            if (route == "direct") il.Emit(System.Reflection.Emit.OpCodes.Call, target);
            else
            {
                il.Emit(System.Reflection.Emit.OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var local = il.DeclareLocal(typeof(IntPtr));
                    il.Emit(System.Reflection.Emit.OpCodes.Stloc, local); il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local);
                }
                il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Reflection.CallingConventions.Standard, target.ReturnType, Type.EmptyTypes, null);
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<int>)dynamic.CreateDelegate(typeof(Func<int>));
        }
        foreach (string route in Routes)
        {
            if (Call(typeof(System.Numerics.Vector).GetMethod("get_Alignment", flags)!, route)() != System.Numerics.Vector<byte>.Count)
                throw new InvalidOperationException("helper byte-count relation");
            Console.WriteLine("vector-alignment:helper:" + route + ":pass");
        }
        foreach (var item in Cases())
        {
            var vector = core.GetType(Owner(item.Width) + "`1", true)!.MakeGenericType(item.Decimal ? typeof(decimal) : typeof(int));
            var target = vector.GetMethods(flags).Single(m => m.Name.EndsWith(".get_" + item.Property, StringComparison.Ordinal));
            var call = Call(target, item.Route);
            if (item.Decimal && item.Property == "ElementCount")
            {
                try { call(); throw new InvalidOperationException("unsupported Count returned"); }
                catch (NotSupportedException error) when (error.Message == "Specified type is not supported") { }
            }
            else
            {
                int expected = item.Property switch
                {
                    "Alignment" => item.Width == 0 ? System.Numerics.Vector<byte>.Count : item.Width / 8,
                    "ElementCount" => (int)vector.GetProperty("Count", flags)!.GetValue(null)!,
                    _ => (bool)core.GetType(Owner(item.Width), true)!.GetProperty("IsHardwareAccelerated", flags)!.GetValue(null)! ? 1 : 0
                };
                if (call() != expected) throw new InvalidOperationException("property contract " + item);
            }
            Console.WriteLine(Label(item.Width, item.Decimal, item.Property, item.Route));
        }
    }

    internal static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorAlignment", new Version(1, 0)), "VectorAlignment", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var alignment = module.ImportReference(core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "get_Alignment"));
        void Add(string label, MethodReference target, string route, Action<ILProcessor> expected)
        {
            var method = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            host.Methods.Add(method); entry.Emit(OpCodes.Call, method);
            var il = method.Body.GetILProcessor();
            if (route == "direct") il.Emit(OpCodes.Call, target);
            else
            {
                il.Emit(OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var local = new VariableDefinition(module.TypeSystem.IntPtr); method.Body.Variables.Add(local);
                    il.Emit(OpCodes.Stloc, local); il.Emit(OpCodes.Ldloc, local);
                }
                il.Emit(OpCodes.Calli, new CallSite(target.ReturnType));
            }
            var pass = Instruction.Create(OpCodes.Ldstr, label);
            void Fail()
            {
                il.Emit(OpCodes.Ldstr, label);
                il.Emit(OpCodes.Newobj, module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!)); il.Emit(OpCodes.Throw);
            }
            expected(il); il.Emit(OpCodes.Beq, pass); Fail();
            il.Append(pass); il.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!)); il.Emit(OpCodes.Ret);
        }
        // The native software Numerics vector target is 16 bytes, independently of host CLR width.
        foreach (string route in Routes)
            Add("vector-alignment:helper:" + route + ":pass", alignment, route, il => il.Emit(OpCodes.Ldc_I4, 16));
        // Private ISimdVector bridges are CLR contract controls only. External
        // MemberRefs do not reproduce the Medium-retained ordinary-body route.
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }
}
