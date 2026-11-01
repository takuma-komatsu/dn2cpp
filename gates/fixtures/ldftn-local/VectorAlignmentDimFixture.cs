using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class VectorAlignmentDimFixture
{
    internal static readonly Type[] Elements = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(nint), typeof(nuint), typeof(float), typeof(double) };

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--vector-alignment-dim-oracle") { Oracle(); return true; }
        if (args.Length == 3 && args[0] == "--vector-alignment-dim-fixture") { Build(args[1], args[2]); return true; }
        if (args.Length == 4 && args[0] == "--vector-alignment-dim-unsupported")
        {
            Build(args[1], args[2], new[] { args[3] switch { "char" => typeof(char), "bool" => typeof(bool),
                "decimal" => typeof(decimal), "int128" => typeof(Int128), "uint128" => typeof(UInt128),
                _ => throw new ArgumentException("unknown unsupported element") } });
            return true;
        }
        return false;
    }

    private static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var core = typeof(object).Assembly;
        foreach (int width in VectorAlignmentFixture.Widths)
        foreach (var element in Elements)
        foreach (string route in VectorAlignmentFixture.Routes)
        {
            var vector = core.GetType(VectorAlignmentFixture.Owner(width) + "`1", true)!.MakeGenericType(element);
            var contract = core.GetType("System.Runtime.Intrinsics.ISimdVector`2", true)!.MakeGenericType(vector, element);
            var copy = new System.Reflection.Emit.DynamicMethod("Copy", typeof(void), new[] { typeof(nint), typeof(nint) }, typeof(VectorAlignmentDimFixture).Module, true);
            var il = copy.GetILGenerator();
            var effect = typeof(VectorAlignmentDimRuntime).GetMethod(nameof(VectorAlignmentDimRuntime.Effect))!;
            void Call(string name)
            {
                var method = contract.GetMethod(name)!;
                if (route == "direct") il.Emit(System.Reflection.Emit.OpCodes.Call, method);
                else
                {
                    il.Emit(System.Reflection.Emit.OpCodes.Ldftn, method);
                    if (route == "address")
                    {
                        var local = il.DeclareLocal(typeof(nint));
                        il.Emit(System.Reflection.Emit.OpCodes.Stloc, local); il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local);
                    }
                    il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Reflection.CallingConventions.Standard,
                        method.ReturnType, method.GetParameters().Select(p => p.ParameterType).ToArray(), null);
                }
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); il.Emit(System.Reflection.Emit.OpCodes.Call, effect); Call("LoadAligned");
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1); il.Emit(System.Reflection.Emit.OpCodes.Call, effect); Call("StoreAligned");
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            VectorAlignmentDimRuntime.Run(width == 0 ? System.Numerics.Vector<byte>.Count : width / 8,
                copy.CreateDelegate<Action<nint, nint>>(), Label(width, element, route));
        }
    }

    private static string Label(int width, Type element, string route) => $"vector-alignment-dim:{width}:{element.Name}:{route}";

    internal static void Build(string output, string corePath, Type[]? elements = null)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.ReadAssembly(typeof(VectorAlignmentDimFixture).Assembly.Location);
        app.Name.Name = "VectorAlignmentDim";
        var module = app.MainModule; module.Name = "VectorAlignmentDim"; module.Kind = ModuleKind.Console;
        var host = module.Types.Single(t => t.Name == nameof(VectorAlignmentDimRuntime));
        foreach (var type in module.Types.ToArray()) if (type.Name != "<Module>" && type != host) module.Types.Remove(type);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var effect = host.Methods.Single(m => m.Name == nameof(VectorAlignmentDimRuntime.Effect));
        var run = host.Methods.Single(m => m.Name == nameof(VectorAlignmentDimRuntime.Run));
        var action = module.ImportReference(typeof(Action<nint, nint>).GetConstructor(new[] { typeof(object), typeof(nint) })!);
        foreach (int width in VectorAlignmentFixture.Widths)
        foreach (var element in elements ?? Elements)
        foreach (string route in VectorAlignmentFixture.Routes)
        {
            var vector = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(VectorAlignmentFixture.Owner(width) + "`1")));
            vector.GenericArguments.Add(module.ImportReference(element));
            var declaration = module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2"));
            var contract = new GenericInstanceType(declaration);
            contract.GenericArguments.Add(vector); contract.GenericArguments.Add(module.ImportReference(element));
            var copy = new MethodDefinition("Copy" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            copy.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr)); copy.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
            host.Methods.Add(copy);
            var il = copy.Body.GetILProcessor();
            MethodReference Target(string name)
            {
                // MemberRefs retain the declaration's VARs; the owner supplies
                // their closure. A concrete signature can resolve as a phantom.
                var target = new MethodReference(name, name == "LoadAligned" ? declaration.GenericParameters[0] : module.TypeSystem.Void, contract);
                if (name == "StoreAligned") target.Parameters.Add(new ParameterDefinition(declaration.GenericParameters[0]));
                target.Parameters.Add(new ParameterDefinition(new PointerType(declaration.GenericParameters[1])));
                return target;
            }
            void Call(string name)
            {
                var target = Target(name);
                if (route == "direct") il.Emit(OpCodes.Call, target);
                else
                {
                    il.Emit(OpCodes.Ldftn, target);
                    if (route == "address")
                    {
                        var local = new VariableDefinition(module.TypeSystem.IntPtr); copy.Body.Variables.Add(local);
                        il.Emit(OpCodes.Stloc, local); il.Emit(OpCodes.Ldloc, local);
                    }
                    var site = new CallSite(name == "LoadAligned" ? vector : module.TypeSystem.Void);
                    if (name == "StoreAligned") site.Parameters.Add(new ParameterDefinition(vector));
                    site.Parameters.Add(new ParameterDefinition(new PointerType(module.ImportReference(element))));
                    il.Emit(OpCodes.Calli, site);
                }
            }
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, effect); Call("LoadAligned");
            il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, effect); Call("StoreAligned"); il.Emit(OpCodes.Ret);
            entry.Emit(OpCodes.Ldc_I4, width == 0 ? 16 : width / 8);
            entry.Emit(OpCodes.Ldnull); entry.Emit(OpCodes.Ldftn, copy); entry.Emit(OpCodes.Newobj, action);
            entry.Emit(OpCodes.Ldstr, Label(width, element, route)); entry.Emit(OpCodes.Call, run);
        }
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }
}

internal static class VectorAlignmentDimRuntime
{
    private static int _effects;
    public static nint Effect(nint pointer) { _effects++; return pointer; }

    public static unsafe void Run(int width, Action<nint, nint> copy, string label)
    {
        byte* sourceBuffer = stackalloc byte[128];
        byte* destinationBuffer = stackalloc byte[128];
        byte* source = (byte*)(((nuint)sourceBuffer + 63) & ~(nuint)63);
        byte* destination = (byte*)(((nuint)destinationBuffer + 63) & ~(nuint)63);
        for (int mode = 0; mode < 3; mode++)
        {
            for (int index = 0; index < 64; index++) { source[index] = (byte)(index * 3 + 1); destination[index] = 231; }
            _effects = 0;
            bool failed = false;
            try { copy((nint)(source + (mode == 1 ? 1 : 0)), (nint)(destination + (mode == 2 ? 1 : 0))); }
            catch (AccessViolationException) { failed = true; }
            if (failed != (mode != 0) || _effects != (mode == 1 ? 1 : 2))
                throw new InvalidOperationException(label + ":exceptions/effects");
            for (int index = 0; index < 64; index++)
                if (destination[index] != (mode == 0 && index < width ? source[index] : 231))
                    throw new InvalidOperationException(label + ":memory");
            Console.WriteLine(label + ":" + mode + ":pass");
        }
    }
}
