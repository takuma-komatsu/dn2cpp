using System.Globalization;
using System.Numerics;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ConcreteVectorReductionParity
{
    static readonly string[] Names = { "All", "Any", "None", "Count", "IndexOf", "LastIndexOf" };
    internal sealed record Case(int Width, string Name, uint[] Bits, uint Scalar, int Expected, string Label);
    static float Float(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
    static uint Bits(float value) => unchecked((uint)BitConverter.SingleToInt32Bits(value));

    static IEnumerable<Case> Cases()
    {
        foreach (int width in new[] { 2, 3, 4 })
        {
            int sequence = 0;
            var numeric = new (uint[], uint)[]
            {
                (new uint[] { 0, 0, 0, 0 }, 0),
                (new uint[] { 0x80000000, 0, 0x80000000, 0 }, 0),
                (new uint[] { 0, 0x80000000, 0, 0x80000000 }, 0x80000000),
                (new[] { Bits(1), Bits(2), Bits(3), Bits(4) }, 0),
                (new[] { Bits(1), Bits(2), Bits(3), Bits(4) }, Bits(3)),
                (new[] { Bits(-1), Bits(2), Bits(-1), Bits(4) }, Bits(-1)),
                (new[] { Bits(2), Bits(2), Bits(2), Bits(2) }, Bits(2)),
                (new[] { Bits(2), Bits(1), Bits(2), Bits(1) }, Bits(2)),
                (new uint[] { 0x7f800000, 0xff800000, 0x7f800000, 0xff800000 }, 0x7f800000),
                (new uint[] { 0x7f800000, 0xff800000, 0x7f800000, 0xff800000 }, 0xff800000),
                (new uint[] { 0xffffffff, 0x7fffffff, 0x7fc01234, 0x7f800001 }, 0xffffffff),
                (new uint[] { 0x7fc01234, 0x7f800001, 0xff800001, 0 }, 0x7fc01234),
                (new uint[] { 0x7f800001, 0x7f800001, 0x7f800001, 0x7f800001 }, 0x7f800001),
                (new uint[] { 0x7fc01234, 0, 0x80000000, 0x7fffffff }, 0),
            };
            var allBits = new uint[][]
            {
                new uint[] { 0, 0, 0, 0 },
                new uint[] { 0xffffffff, 0xffffffff, 0xffffffff, 0xffffffff },
                new uint[] { 0xffffffff, 0, 0, 0 },
                new uint[] { 0, 0xffffffff, 0, 0 },
                new uint[] { 0, 0, 0xffffffff, 0 },
                new uint[] { 0xffffffff, 0, 0xffffffff, 0xffffffff },
                new uint[] { 0x7fffffff, 0x7fc01234, 0x7f800001, 0xff800001 },
                new[] { Bits(-1), Bits(-1), Bits(-1), Bits(-1) },
            };
            foreach (var item in numeric)
            foreach (string name in Names)
            {
                if (width == 4 && name == "None") continue;
                var lanes = item.Item1.Take(width).ToArray();
                yield return Make(width, name, lanes, item.Item2, false, sequence++);
            }
            foreach (var lanes in allBits)
            foreach (string name in Names)
            {
                if (width == 4 && name == "None") continue;
                yield return Make(width, name, lanes.Take(width).ToArray(), 0, true, sequence++);
            }
        }
    }

    static Case Make(int width, string name, uint[] lanes, uint scalar, bool allBits, int index)
    {
        int count = 0, first = -1, last = -1;
        for (int lane = 0; lane < width; lane++)
        {
            // Numeric equality deliberately differs from NaN bit-pattern matching.
            bool equal = allBits ? lanes[lane] == uint.MaxValue : Float(lanes[lane]) == Float(scalar);
            if (!equal) continue;
            count++; if (first < 0) first = lane; last = lane;
        }
        int expected = name switch { "All" => count == width ? 1 : 0, "Any" => count != 0 ? 1 : 0,
            "None" => count == 0 ? 1 : 0, "Count" => count, "IndexOf" => first, _ => last };
        string method = name + (allBits ? "WhereAllBitsSet" : "");
        return new Case(width, method, lanes, scalar, expected, $"concrete-vector-reduction:{width}:{index}:{method}:pass");
    }

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--concrete-vector-reduction-parity-oracle") { Oracle(); return true; }
        if (args.Length != 3 || args[0] != "--concrete-vector-reduction-parity-fixture") return false;
        Build(args[1], args[2]); return true;
    }

    static IEnumerable<(int Width, string Name, int Mode, string Label)> Effects()
    {
        foreach (int width in new[] { 2, 3 })
        foreach (string name in Names)
        foreach (bool bits in new[] { false, true })
        for (int mode = 0; mode < (bits ? 2 : 3); mode++)
        {
            string method = name + (bits ? "WhereAllBitsSet" : "");
            yield return (width, method, mode, $"concrete-vector-reduction:effects:{width}:{method}:{mode}:pass");
        }
    }

    static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        foreach (var item in Cases())
        {
            object vector = item.Width == 2 ? (object)new Vector2(Float(item.Bits[0]), Float(item.Bits[1]))
                : item.Width == 3 ? new Vector3(Float(item.Bits[0]), Float(item.Bits[1]), Float(item.Bits[2]))
                : new Vector4(Float(item.Bits[0]), Float(item.Bits[1]), Float(item.Bits[2]), Float(item.Bits[3]));
            bool allBits = item.Name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal);
            var types = allBits ? new[] { vector.GetType() } : new[] { vector.GetType(), typeof(float) };
            var target = vector.GetType().GetMethod(item.Name, types)!;
            object result = target.Invoke(null, allBits ? new[] { vector } : new[] { vector, (object)Float(item.Scalar) })!;
            int actual = result is bool value ? value ? 1 : 0 : (int)result;
            if (actual != item.Expected) throw new InvalidOperationException(item.Label);
            Console.WriteLine(item.Label);
        }
        // These expected labels are checked against the generated program on CLR
        // before its native execution; the generated IL checks the producers.
        foreach (var item in Effects()) Console.WriteLine(item.Label);
    }

    static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ConcreteVectorReductionParity", new Version(1, 0)), "ConcreteVectorReductionParity", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        // A dead private body makes the ILDiet lane prove an actual rewrite.
        var sentinel = new MethodDefinition("UnusedDietSentinel", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(sentinel); sentinel.Body.GetILProcessor().Emit(OpCodes.Ret);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var bitcast = module.ImportReference(typeof(BitConverter).GetMethod(nameof(BitConverter.Int32BitsToSingle))!);
        var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var failure = module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
        foreach (var item in Cases())
        {
            var vector = core.MainModule.GetType("System.Numerics.Vector" + item.Width);
            bool allBits = item.Name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal);
            var target = module.ImportReference(vector.Methods.Single(m => m.Name == item.Name && m.Parameters.Count == (allBits ? 1 : 2)));
            var constructor = module.ImportReference(vector.Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == item.Width && m.Parameters.All(p => p.ParameterType.MetadataType == MetadataType.Single)));
            var method = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            host.Methods.Add(method); entry.Emit(OpCodes.Call, method);
            var il = method.Body.GetILProcessor();
            foreach (uint bits in item.Bits) { il.Emit(OpCodes.Ldc_I4, unchecked((int)bits)); il.Emit(OpCodes.Call, bitcast); }
            il.Emit(OpCodes.Newobj, constructor);
            if (!allBits) { il.Emit(OpCodes.Ldc_I4, unchecked((int)item.Scalar)); il.Emit(OpCodes.Call, bitcast); }
            il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ldc_I4, item.Expected);
            var pass = Instruction.Create(OpCodes.Ldstr, item.Label);
            il.Emit(OpCodes.Beq, pass);
            il.Emit(OpCodes.Ldstr, item.Label); il.Emit(OpCodes.Newobj, failure); il.Emit(OpCodes.Throw);
            il.Append(pass); il.Emit(OpCodes.Call, write); il.Emit(OpCodes.Ret);
        }
        AddEffects(core, module, host, entry, write, failure);
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }

    static void AddEffects(AssemblyDefinition core, ModuleDefinition module, TypeDefinition host,
        ILProcessor entry, MethodReference write, MethodReference failure)
    {
        var state = new FieldDefinition("ProducerState", FieldAttributes.Static | FieldAttributes.Private, module.TypeSystem.Int32);
        host.Fields.Add(state);
        var vectorException = module.ImportReference(typeof(ArgumentException).GetConstructor(Type.EmptyTypes)!);
        var scalarException = module.ImportReference(typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes)!);
        foreach (var item in Effects())
        {
            bool bits = item.Name.EndsWith("WhereAllBitsSet", StringComparison.Ordinal);
            var vector = core.MainModule.GetType("System.Numerics.Vector" + item.Width);
            var target = module.ImportReference(vector.Methods.Single(m => m.Name == item.Name && m.Parameters.Count == (bits ? 1 : 2)));
            var constructor = module.ImportReference(vector.Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == item.Width && m.Parameters.All(p => p.ParameterType.MetadataType == MetadataType.Single)));
            MethodDefinition Producer(string name, TypeReference result, int before, int after, bool throws, MethodReference error)
            {
                var producer = new MethodDefinition(name + host.Methods.Count, MethodAttributes.Static | MethodAttributes.Private, result);
                host.Methods.Add(producer);
                var il = producer.Body.GetILProcessor();
                var valid = Instruction.Create(OpCodes.Ldc_I4, after);
                il.Emit(OpCodes.Ldsfld, state); il.Emit(OpCodes.Ldc_I4, before); il.Emit(OpCodes.Beq, valid);
                il.Emit(OpCodes.Ldstr, item.Label + ":producer-order"); il.Emit(OpCodes.Newobj, failure); il.Emit(OpCodes.Throw);
                il.Append(valid); il.Emit(OpCodes.Stsfld, state);
                if (throws) { il.Emit(OpCodes.Newobj, error); il.Emit(OpCodes.Throw); }
                else if (before == 0)
                {
                    for (int lane = 0; lane < item.Width; lane++) il.Emit(OpCodes.Ldc_R4, 0f);
                    il.Emit(OpCodes.Newobj, constructor); il.Emit(OpCodes.Ret);
                }
                else { il.Emit(OpCodes.Ldc_R4, 0f); il.Emit(OpCodes.Ret); }
                return producer;
            }
            var vectorProducer = Producer("VectorProducer", module.ImportReference(vector), 0, 1, item.Mode == 1, vectorException);
            var scalarProducer = bits ? null : Producer("ScalarProducer", module.TypeSystem.Single, 1, 2, item.Mode == 2, scalarException);
            var test = new MethodDefinition("Effects" + host.Methods.Count, MethodAttributes.Static | MethodAttributes.Private, module.TypeSystem.Void);
            host.Methods.Add(test); entry.Emit(OpCodes.Call, test);
            var body = test.Body.GetILProcessor();
            body.Emit(OpCodes.Ldc_I4_0); body.Emit(OpCodes.Stsfld, state);
            var start = Instruction.Create(OpCodes.Call, vectorProducer);
            body.Append(start);
            if (scalarProducer is not null) body.Emit(OpCodes.Call, scalarProducer);
            body.Emit(OpCodes.Call, target);
            var verify = Instruction.Create(OpCodes.Ldsfld, state);
            if (item.Mode == 0)
            {
                string name = item.Name.Replace("WhereAllBitsSet", "", StringComparison.Ordinal);
                int expected = bits ? name == "None" ? 1 : name is "IndexOf" or "LastIndexOf" ? -1 : 0
                    : name is "All" or "Any" ? 1 : name == "None" ? 0 : name == "Count" ? item.Width : name == "IndexOf" ? 0 : item.Width - 1;
                body.Emit(OpCodes.Ldc_I4, expected); body.Emit(OpCodes.Beq, verify);
                body.Emit(OpCodes.Ldstr, item.Label + ":result"); body.Emit(OpCodes.Newobj, failure); body.Emit(OpCodes.Throw);
            }
            else
            {
                body.Emit(OpCodes.Pop);
                var missing = Instruction.Create(OpCodes.Ldstr, item.Label + ":missing-exception");
                body.Emit(OpCodes.Leave, missing);
                var handler = Instruction.Create(OpCodes.Pop);
                body.Append(handler); body.Emit(OpCodes.Leave, verify);
                body.Append(missing); body.Emit(OpCodes.Newobj, failure); body.Emit(OpCodes.Throw);
                test.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                {
                    TryStart = start, TryEnd = handler, HandlerStart = handler, HandlerEnd = missing,
                    CatchType = module.ImportReference(item.Mode == 1 ? typeof(ArgumentException) : typeof(InvalidOperationException)),
                });
            }
            body.Append(verify); body.Emit(OpCodes.Ldc_I4, item.Mode == 1 || bits ? 1 : 2);
            var pass = Instruction.Create(OpCodes.Ldstr, item.Label);
            body.Emit(OpCodes.Beq, pass);
            body.Emit(OpCodes.Ldstr, item.Label + ":producer-count"); body.Emit(OpCodes.Newobj, failure); body.Emit(OpCodes.Throw);
            body.Append(pass); body.Emit(OpCodes.Call, write); body.Emit(OpCodes.Ret);
        }
    }

}
