using System.Globalization;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class SearchValuesShuffleFixture
{
    static readonly byte[] Indices = { 0, 15, 16, 31, 65, 127, 128, 255 };
    static readonly string[] Routes = { "direct", "address", "calli" };
    static string Label(string route, byte index) => $"search-values-shuffle:{route}:{index}:pass";
    static byte Expected(byte index, bool ssse3) => ssse3
        ? (index < 128 ? (byte)((index & 15) + 1) : (byte)0)
        : (index < 16 ? (byte)(index + 1) : (byte)0);

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--search-values-shuffle-oracle") { Oracle(); return true; }
        if (args.Length != 3 || args[0] != "--search-values-shuffle-fixture") return false;
        Build(args[1], args[2]);
        return true;
    }

    static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var target = typeof(object).Assembly.GetType("System.Buffers.SearchValues", true)!.GetMethod("ShuffleNativeModified", flags)!;
        var vector = typeof(Vector128<byte>);
        var source = Vector128.Create((byte)1, (byte)2, (byte)3, (byte)4, (byte)5, (byte)6, (byte)7, (byte)8,
            (byte)9, (byte)10, (byte)11, (byte)12, (byte)13, (byte)14, (byte)15, (byte)16);
        Console.WriteLine("search-values-shuffle:ssse3=" + Ssse3.IsSupported);
        foreach (string route in Routes)
        {
            // Execute the actual private CoreLib body without changing its metadata.
            var method = new System.Reflection.Emit.DynamicMethod("Shuffle_" + route, vector,
                new[] { vector, vector }, typeof(SearchValuesShuffleFixture).Module, true);
            var il = method.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            if (route == "direct") il.Emit(System.Reflection.Emit.OpCodes.Call, target);
            else
            {
                il.Emit(System.Reflection.Emit.OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var pointer = il.DeclareLocal(typeof(IntPtr));
                    il.Emit(System.Reflection.Emit.OpCodes.Stloc, pointer); il.Emit(System.Reflection.Emit.OpCodes.Ldloc, pointer);
                }
                il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Reflection.CallingConventions.Standard,
                    vector, new[] { vector, vector }, null);
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            var invoke = (Func<Vector128<byte>, Vector128<byte>, Vector128<byte>>)method.CreateDelegate(typeof(Func<Vector128<byte>, Vector128<byte>, Vector128<byte>>));
            foreach (byte index in Indices)
            {
                var result = invoke(source, Vector128.Create(index));
                for (int lane = 0; lane < 16; lane++)
                    if (result.GetElement(lane) != Expected(index, Ssse3.IsSupported))
                        throw new InvalidOperationException(Label(route, index));
                Console.WriteLine(Label(route, index));
            }
        }
    }

    static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("SearchValuesShuffle", new Version(1, 0)), "SearchValuesShuffle", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        // The Diet lane must really rewrite the application, not copy it unchanged.
        var unused = new MethodDefinition("UnusedDietSentinel", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(unused); unused.Body.GetILProcessor().Emit(OpCodes.Ret);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var supported = module.ImportReference(typeof(Ssse3).GetProperty(nameof(Ssse3.IsSupported))!.GetMethod!);
        var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var fallback = Instruction.Create(OpCodes.Ldstr, "search-values-shuffle:ssse3=False");
        var report = Instruction.Create(OpCodes.Call, write);
        entry.Emit(OpCodes.Call, supported); entry.Emit(OpCodes.Brfalse, fallback);
        entry.Emit(OpCodes.Ldstr, "search-values-shuffle:ssse3=True"); entry.Emit(OpCodes.Br, report); entry.Append(fallback); entry.Append(report);
        var definition = core.MainModule.GetType("System.Buffers.SearchValues").Methods.Single(m => m.Name == "ShuffleNativeModified");
        var target = module.ImportReference(definition);
        var vectorMethods = core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods;
        MethodReference Create(int count) => module.ImportReference(vectorMethods.Single(m => m.Name == "Create" && !m.HasGenericParameters
            && m.Parameters.Count == count && m.Parameters.All(p => p.ParameterType.MetadataType == MetadataType.Byte)));
        var createSource = Create(16); var createIndices = Create(1);
        var element = new GenericInstanceMethod(module.ImportReference(vectorMethods.Single(m => m.Name == "GetElement" && m.GenericParameters.Count == 1)));
        element.GenericArguments.Add(module.TypeSystem.Byte);
        foreach (string route in Routes)
        foreach (byte index in Indices)
        {
            var method = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            host.Methods.Add(method); entry.Emit(OpCodes.Call, method);
            var il = method.Body.GetILProcessor();
            var result = new VariableDefinition(target.ReturnType); var expected = new VariableDefinition(module.TypeSystem.Byte);
            method.Body.Variables.Add(result); method.Body.Variables.Add(expected);
            for (int lane = 1; lane <= 16; lane++) il.Emit(OpCodes.Ldc_I4, lane);
            il.Emit(OpCodes.Call, createSource); il.Emit(OpCodes.Ldc_I4, (int)index); il.Emit(OpCodes.Call, createIndices);
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
            il.Emit(OpCodes.Stloc, result);
            var portable = Instruction.Create(OpCodes.Ldc_I4, (int)Expected(index, false));
            var store = Instruction.Create(OpCodes.Stloc, expected);
            il.Emit(OpCodes.Call, supported); il.Emit(OpCodes.Brfalse, portable);
            il.Emit(OpCodes.Ldc_I4, (int)Expected(index, true)); il.Emit(OpCodes.Br, store); il.Append(portable); il.Append(store);
            string label = Label(route, index);
            var fail = Instruction.Create(OpCodes.Ldstr, label);
            for (int lane = 0; lane < 16; lane++)
            {
                il.Emit(OpCodes.Ldloc, result); il.Emit(OpCodes.Ldc_I4, lane); il.Emit(OpCodes.Call, element);
                il.Emit(OpCodes.Ldloc, expected); il.Emit(OpCodes.Bne_Un, fail);
            }
            il.Emit(OpCodes.Ldstr, label); il.Emit(OpCodes.Call, write); il.Emit(OpCodes.Ret);
            il.Append(fail); il.Emit(OpCodes.Newobj, module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!)); il.Emit(OpCodes.Throw);
        }
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }
}
