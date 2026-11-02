using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using OpCodes = Mono.Cecil.Cil.OpCodes;

internal static class UnsupportedAlignedDimFixture
{
    internal static readonly string[] Routes = { "direct", "address", "calli", "delegate" };
    internal static IEnumerable<(int Width, Type Element)> Pairs()
    {
        yield return (0, typeof(decimal));
        foreach (int width in new[] { 64, 128, 256, 512 })
        foreach (var element in new[] { typeof(char), typeof(Int128), typeof(UInt128) })
            yield return (width, element);
    }

    public static bool Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--unsupported-aligned-dim-inspect")
        {
            Inspect(args[1]);
            return true;
        }
        if (args.Length == 1 && args[0] == "--unsupported-aligned-dim-oracle")
        {
            Oracle();
            return true;
        }
        if (args.Length is 3 or 4 && args[0] == "--unsupported-aligned-dim-fixture")
        {
            Build(args[1], args[2], args.Length == 4 ? args[3] : "all-runtime", false);
            return true;
        }
        if (args.Length == 4 && args[0] == "--unsupported-aligned-dim-control")
        {
            Build(args[1], args[2], args[3], true);
            return true;
        }
        return false;
    }

    private static string Label(int width, Type element, string operation, string route) =>
        $"unsupported-aligned-dim:{width}:{element.Name}:{operation}:{route}";

    private static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("unsupported-aligned-dim:start");
        UnsupportedAlignedDimRuntime.Profile();
        var core = typeof(object).Assembly;
        foreach (var pair in Pairs())
        foreach (string operation in new[] { "LoadAligned", "StoreAligned" })
        {
            var vector = core.GetType(VectorAlignmentFixture.Owner(pair.Width) + "`1", true)!.MakeGenericType(pair.Element);
            var contract = core.GetType("System.Runtime.Intrinsics.ISimdVector`2", true)!.MakeGenericType(vector, pair.Element);
            var target = contract.GetMethod(operation)!;
            foreach (string route in new[] { "address", "delegate" })
            {
                var retained = Dynamic(target, vector, pair.Element, operation, route, true);
                UnsupportedAlignedDimRuntime.Retained(retained.CreateDelegate<Action<nint, int>>(), Label(pair.Width, pair.Element, operation, "retained-" + route));
            }
            foreach (string route in Routes)
            {
                var invoke = Dynamic(target, vector, pair.Element, operation, route, false);
                UnsupportedAlignedDimRuntime.Run(invoke.CreateDelegate<Action<nint, int>>(), operation == "StoreAligned", pair.Width == 0 ? System.Numerics.Vector<byte>.Count : pair.Width / 8, Label(pair.Width, pair.Element, operation, route));
            }
        }
        Console.WriteLine("unsupported-aligned-dim:passed");
    }

    private static DynamicMethod Dynamic(MethodInfo target, Type vector, Type element, string operation, string route, bool retain)
    {
        var dynamic = new DynamicMethod("UnsupportedAlignedDim_" + operation + "_" + route,
            typeof(void), new[] { typeof(nint), typeof(int) }, typeof(UnsupportedAlignedDimFixture).Module, true);
        var il = dynamic.GetILGenerator();
        bool store = operation == "StoreAligned";
        Type delegateType = (store ? typeof(UnsupportedAlignedStore<,>) : typeof(UnsupportedAlignedLoad<,>)).MakeGenericType(vector, element);
        LocalBuilder? local = null;
        if (route is "address" or "delegate")
        {
            local = il.DeclareLocal(route == "address" ? typeof(nint) : delegateType);
            if (route == "delegate") il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
            il.Emit(System.Reflection.Emit.OpCodes.Ldftn, target);
            if (route == "delegate") il.Emit(System.Reflection.Emit.OpCodes.Newobj, delegateType.GetConstructors().Single());
            il.Emit(System.Reflection.Emit.OpCodes.Stloc, local);
            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local);
            il.Emit(System.Reflection.Emit.OpCodes.Call, typeof(UnsupportedAlignedDimRuntime).GetMethod(route == "address" ? nameof(UnsupportedAlignedDimRuntime.Acquired) : nameof(UnsupportedAlignedDimRuntime.AcquiredDelegate))!);
        }
        if (!retain)
        {
            if (route == "delegate") il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local!);
            if (store)
            {
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
                il.Emit(System.Reflection.Emit.OpCodes.Call, typeof(UnsupportedAlignedDimRuntime).GetMethod(nameof(UnsupportedAlignedDimRuntime.Value))!.MakeGenericMethod(vector));
            }
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            il.Emit(System.Reflection.Emit.OpCodes.Call, typeof(UnsupportedAlignedDimRuntime).GetMethod(nameof(UnsupportedAlignedDimRuntime.Address))!);
            if (route == "direct") il.Emit(System.Reflection.Emit.OpCodes.Call, target);
            else if (route == "delegate") il.Emit(System.Reflection.Emit.OpCodes.Callvirt, delegateType.GetMethod("Invoke")!);
            else
            {
                if (route == "address") il.Emit(System.Reflection.Emit.OpCodes.Ldloc, local!);
                else il.Emit(System.Reflection.Emit.OpCodes.Ldftn, target);
                il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, CallingConventions.Standard, target.ReturnType,
                    target.GetParameters().Select(p => p.ParameterType).ToArray(), null);
            }
            if (!store) il.Emit(System.Reflection.Emit.OpCodes.Pop);
        }
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        return dynamic;
    }

    internal static void Build(string output, string corePath, string selection, bool control)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.ReadAssembly(typeof(UnsupportedAlignedDimFixture).Assembly.Location);
        string assemblyName = control ? "UnsupportedAlignedDimControl" : "UnsupportedAlignedDim";
        app.Name.Name = assemblyName;
        var module = app.MainModule; module.Name = assemblyName; module.Kind = ModuleKind.Console;
        var host = module.Types.Single(t => t.Name == nameof(UnsupportedAlignedDimRuntime));
        var loadDelegate = module.Types.Single(t => t.Name == "UnsupportedAlignedLoad`2");
        var storeDelegate = module.Types.Single(t => t.Name == "UnsupportedAlignedStore`2");
        foreach (var type in module.Types.ToArray())
            if (type.Name != "<Module>" && type != host && type != loadDelegate && type != storeDelegate) module.Types.Remove(type);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        void Print(string value) { entry.Emit(OpCodes.Ldstr, value); entry.Emit(OpCodes.Call, write); }
        Print(control ? "unsupported-aligned-control:start" : "unsupported-aligned-dim:start");
        if (!control) entry.Emit(OpCodes.Call, host.Methods.Single(m => m.Name == nameof(UnsupportedAlignedDimRuntime.Profile)));
        var selectionParts = selection.Split(':');
        bool all = selection is "all-runtime" or "retained-only" or "all-public";
        if (control && (selection is "all-runtime" or "retained-only") || !control && selection == "all-public")
            throw new ArgumentException("runtime and public control selections are distinct");
        if (!all && selectionParts.Length != (control ? 4 : 3)) throw new ArgumentException("invalid isolated DIM selection");
        int selected = 0;
        string[] kinds = !control ? new[] { "dim" } : selection == "all-public" ? new[] { "unsafe", "offset", "aligned", "nontemporal" } : new[] { selectionParts[3] };
        foreach (var pair in Pairs())
        foreach (string operation in new[] { "LoadAligned", "StoreAligned" })
        foreach (string kind in kinds)
        {
            string elementName = pair.Element == typeof(decimal) ? "decimal" : pair.Element == typeof(char) ? "char" : pair.Element == typeof(Int128) ? "int128" : "uint128";
            if (!all && (selectionParts[0] != pair.Width.ToString(CultureInfo.InvariantCulture) || selectionParts[1] != elementName || selectionParts[2] != operation)) continue;
            selected++;
            var element = module.ImportReference(pair.Element);
            var vector = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(VectorAlignmentFixture.Owner(pair.Width) + "`1")));
            vector.GenericArguments.Add(element);
            var declaration = module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2"));
            var contract = new GenericInstanceType(declaration);
            contract.GenericArguments.Add(vector); contract.GenericArguments.Add(element);
            bool store = operation == "StoreAligned";
            // The MemberRef carries the actual VAR declaration; its owner closes
            // TSelf and T. Concrete replacement signatures resolve differently.
            var dim = new MethodReference(operation, store ? module.TypeSystem.Void : declaration.GenericParameters[0], contract);
            if (store) dim.Parameters.Add(new ParameterDefinition(declaration.GenericParameters[0]));
            dim.Parameters.Add(new ParameterDefinition(new PointerType(declaration.GenericParameters[1])));
            MethodReference target = dim;
            if (control && kind != "constrained")
            {
                string name = kind switch { "unsafe" or "offset" => store ? "StoreUnsafe" : "LoadUnsafe",
                    "aligned" => operation, "nontemporal" => operation + "NonTemporal", _ => throw new ArgumentException("unknown public control") };
                var definition = core.MainModule.GetType(VectorAlignmentFixture.Owner(pair.Width)).Methods.Single(m =>
                    m.Name == name && m.HasGenericParameters && m.Parameters.Count == (store ? 2 : 1) + (kind == "offset" ? 1 : 0));
                var closed = new GenericInstanceMethod(module.ImportReference(definition)); closed.GenericArguments.Add(element); target = closed;
            }
            var delegateType = new GenericInstanceType(store ? storeDelegate : loadDelegate);
            delegateType.GenericArguments.Add(vector); delegateType.GenericArguments.Add(element);
            var delegateDefinition = store ? storeDelegate : loadDelegate;
            MethodReference DelegateMethod(string name)
            {
                var method = delegateDefinition.Methods.Single(m => m.Name == name);
                var reference = new MethodReference(name, method.ReturnType, delegateType) { HasThis = true };
                foreach (var parameter in method.Parameters) reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                return reference;
            }
            void Add(string route, bool retain)
            {
                var caller = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
                caller.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr)); caller.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
                host.Methods.Add(caller);
                var il = caller.Body.GetILProcessor();
                VariableDefinition? local = null;
                if (route is "address" or "delegate")
                {
                    local = new VariableDefinition(route == "address" ? module.TypeSystem.IntPtr : delegateType);
                    caller.Body.Variables.Add(local);
                    if (route == "delegate") il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldftn, target);
                    if (route == "delegate") il.Emit(OpCodes.Newobj, DelegateMethod(".ctor"));
                    il.Emit(OpCodes.Stloc, local); il.Emit(OpCodes.Ldloc, local);
                    il.Emit(OpCodes.Call, host.Methods.Single(m => m.Name == (route == "address" ? nameof(UnsupportedAlignedDimRuntime.Acquired) : nameof(UnsupportedAlignedDimRuntime.AcquiredDelegate))));
                }
                if (!retain)
                {
                    if (route == "delegate") il.Emit(OpCodes.Ldloc, local!);
                    if (store)
                    {
                        var value = new GenericInstanceMethod(host.Methods.Single(m => m.Name == nameof(UnsupportedAlignedDimRuntime.Value))); value.GenericArguments.Add(vector);
                        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, value);
                    }
                    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, host.Methods.Single(m => m.Name == nameof(UnsupportedAlignedDimRuntime.Address)));
                    if (kind == "offset") { il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Conv_U); }
                    if (kind == "constrained") il.Emit(OpCodes.Constrained, vector);
                    if (route == "direct") il.Emit(OpCodes.Call, target);
                    else if (route == "delegate") il.Emit(OpCodes.Callvirt, DelegateMethod("Invoke"));
                    else
                    {
                        if (route == "address") il.Emit(OpCodes.Ldloc, local!); else il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(store ? module.TypeSystem.Void : vector);
                        if (store) site.Parameters.Add(new ParameterDefinition(vector));
                        site.Parameters.Add(new ParameterDefinition(new PointerType(element)));
                        il.Emit(OpCodes.Calli, site);
                    }
                    if (!store) il.Emit(OpCodes.Pop);
                }
                il.Emit(OpCodes.Ret);
                entry.Emit(OpCodes.Ldnull); entry.Emit(OpCodes.Ldftn, caller);
                entry.Emit(OpCodes.Newobj, module.ImportReference(typeof(Action<nint, int>).GetConstructor(new[] { typeof(object), typeof(nint) })!));
                if (!retain) entry.Emit(store ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                if (!retain && !control)
                {
                    if (pair.Width == 0) entry.Emit(OpCodes.Call, host.Methods.Single(m => m.Name == nameof(UnsupportedAlignedDimRuntime.NumericsAlignment)));
                    else entry.Emit(OpCodes.Ldc_I4, pair.Width / 8);
                }
                entry.Emit(OpCodes.Ldstr, Label(pair.Width, pair.Element, operation, retain ? "retained-" + route : control ? "control-" + kind : route));
                entry.Emit(OpCodes.Call, host.Methods.Single(m => m.Name == (retain ? nameof(UnsupportedAlignedDimRuntime.Retained) : control ? nameof(UnsupportedAlignedDimRuntime.Control) : nameof(UnsupportedAlignedDimRuntime.Run))));
            }
            if (!control)
            {
                Add("address", true); Add("delegate", true);
                if (selection != "retained-only") foreach (string route in Routes) Add(route, false);
            }
            else Add("direct", false);
        }
        if (selected == 0) throw new ArgumentException("selection is outside the finite unsupported pair set");
        Print(control ? "unsupported-aligned-control:passed" : "unsupported-aligned-dim:passed");
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (!control) Inspect(output);
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }

    private static void Inspect(string path)
    {
        using var saved = AssemblyDefinition.ReadAssembly(path);
        using var stream = File.OpenRead(path);
        using var pe = new System.Reflection.PortableExecutable.PEReader(stream);
        var metadata = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var roots = new HashSet<string>(StringComparer.Ordinal);
        var references = new HashSet<int>();
        var allowed = Pairs().Select(p => VectorAlignmentFixture.Owner(p.Width) + "`1<" + p.Element.FullName + ">").ToHashSet(StringComparer.Ordinal);
        foreach (var method in saved.MainModule.Types.Single(t => t.Name == nameof(UnsupportedAlignedDimRuntime)).Methods.Where(m => m.Name.StartsWith("Case", StringComparison.Ordinal)))
        {
            var calls = method.Body.Instructions.Where(i => i.Operand is MethodReference m && m.DeclaringType is GenericInstanceType g
                && g.ElementType.FullName == "System.Runtime.Intrinsics.ISimdVector`2").ToArray();
            if (calls.Length != 1) throw new InvalidOperationException("each case must name exactly one real closed DIM");
            var instruction = calls[0];
            var target = (MethodReference)instruction.Operand;
            if (target.MetadataToken.TokenType != TokenType.MemberRef || target is GenericInstanceMethod || target.HasThis
                || target.ExplicitThis || target.CallingConvention != MethodCallingConvention.Default || target.HasGenericParameters)
                throw new InvalidOperationException("DIM must be a nongeneric static MemberRef");
            var owner = (GenericInstanceType)target.DeclaringType;
            if (owner.GenericArguments.Count != 2 || owner.GenericArguments[0] is not GenericInstanceType vector
                || vector.GenericArguments.Count != 1 || vector.GenericArguments[0].FullName != owner.GenericArguments[1].FullName
                || !allowed.Contains(vector.FullName) || owner.ElementType.Scope.Name != "System.Private.CoreLib"
                || vector.ElementType.Scope.Name != "System.Private.CoreLib") throw new InvalidOperationException("DIM owner closure");
            bool store = target.Name == "StoreAligned";
            if (!store && target.Name != "LoadAligned") throw new InvalidOperationException("unexpected DIM operation");
            var raw = metadata.GetMemberReference((System.Reflection.Metadata.MemberReferenceHandle)
                System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(target.MetadataToken.ToInt32()));
            string signature = Convert.ToHexString(metadata.GetBlobBytes(raw.Signature));
            if (signature != (store ? "00020113000F1301" : "000113000F1301")
                || raw.Parent.Kind != System.Reflection.Metadata.HandleKind.TypeSpecification)
                throw new InvalidOperationException("DIM raw VAR/pointer signature");
            bool address = method.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == nameof(UnsupportedAlignedDimRuntime.Address));
            bool calli = method.Body.Instructions.Any(i => i.OpCode.Code == Code.Calli);
            bool delegated = method.Body.Instructions.Any(i => i.OpCode.Code == Code.Newobj && i.Operand is MethodReference m
                && m.DeclaringType.Name.StartsWith("UnsupportedAligned", StringComparison.Ordinal));
            bool acquired = method.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == nameof(UnsupportedAlignedDimRuntime.Acquired));
            string route = !address ? delegated ? "retained-delegate" : "retained-address"
                : delegated ? "delegate" : calli ? acquired ? "address" : "calli" : "direct";
            if (!address && (calli || method.Body.Instructions.Any(i => i.OpCode.Code == Code.Callvirt || i.Operand is MethodReference m && m.Name == nameof(UnsupportedAlignedDimRuntime.Value))))
                throw new InvalidOperationException("retained-only case executes a DIM operand or invocation");
            if (route == "direct" ? instruction.OpCode.Code != Code.Call : instruction.OpCode.Code != Code.Ldftn)
                throw new InvalidOperationException("DIM route opcode");
            foreach (var site in method.Body.Instructions.Where(i => i.OpCode.Code == Code.Calli).Select(i => (CallSite)i.Operand))
                if (site.HasThis || site.CallingConvention != MethodCallingConvention.Default
                    || site.ReturnType.FullName != (store ? "System.Void" : vector.FullName)
                    || site.Parameters.Count != (store ? 2 : 1)
                    || site.Parameters[^1].ParameterType.FullName != owner.GenericArguments[1].FullName + "*"
                    || store && site.Parameters[0].ParameterType.FullName != vector.FullName)
                    throw new InvalidOperationException("closed calli signature");
            string root = vector.FullName + "::" + target.Name;
            if (!seen.Add(root + ":" + route)) throw new InvalidOperationException("duplicate DIM route");
            roots.Add(root);
            if (references.Add(target.MetadataToken.ToInt32()))
            {
                var specification = metadata.GetTypeSpecification((System.Reflection.Metadata.TypeSpecificationHandle)raw.Parent);
                Console.WriteLine("unsupported-aligned-dim-reference:" + root + ":" + signature + ":" + Convert.ToHexString(metadata.GetBlobBytes(specification.Signature)));
            }
        }
        if (roots.Count is not (1 or 26)) throw new InvalidOperationException("incomplete DIM root set");
        bool retainedOnly = seen.All(k => k.EndsWith(":retained-address", StringComparison.Ordinal) || k.EndsWith(":retained-delegate", StringComparison.Ordinal));
        foreach (string root in roots)
        foreach (string route in retainedOnly ? new[] { "retained-address", "retained-delegate" } : new[] { "retained-address", "retained-delegate", "direct", "address", "calli", "delegate" })
            if (!seen.Contains(root + ":" + route)) throw new InvalidOperationException("missing DIM route");
        Console.WriteLine("unsupported-aligned-dim-inspect:roots=" + roots.Count + ":routes=" + seen.Count + ":retained-only=" + retainedOnly
            + ":sha256=" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
    }

}

internal unsafe delegate TVector UnsupportedAlignedLoad<TVector, T>(T* source) where T : unmanaged;
internal unsafe delegate void UnsupportedAlignedStore<TVector, T>(TVector value, T* destination) where T : unmanaged;

internal static class UnsupportedAlignedDimRuntime
{
    private static string _effects = "";
    private static int _acquisitions;

    public static void Acquired(nint address)
    {
        if (address == 0 || _effects.Length != 0) throw new InvalidOperationException("function address acquisition");
        _acquisitions++;
    }

    public static void AcquiredDelegate(Delegate? value)
    {
        if (value is null || _effects.Length != 0) throw new InvalidOperationException("delegate acquisition");
        _acquisitions++;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static T Value<T>(int fault)
    {
        _effects += "V";
        if (fault == 1) throw new InvalidOperationException("value-producer");
        return default!;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static unsafe nint Address(nint pointer, int fault)
    {
        _effects += "A";
        if (fault == 2) throw new InvalidOperationException("address-producer");
        if (fault == 3)
        {
            var array = new byte[1];
            fixed (byte* address = &array[1]) return (nint)address;
        }
        if (fault == 4)
        {
            byte[] array = null!;
            fixed (byte* address = &array[0]) return (nint)address;
        }
        return pointer;
    }

    public static void Retained(Action<nint, int> acquire, string label)
    {
        _effects = ""; _acquisitions = 0;
        acquire(0, 0);
        if (_effects.Length != 0 || _acquisitions != 1) throw new InvalidOperationException(label + ":retention");
        Console.WriteLine(label + "|acquired|1|effects=|canary=unchanged");
    }

    public static int NumericsAlignment() => System.Numerics.Vector<byte>.Count;

    public static void Profile() => Console.WriteLine("unsupported-aligned-dim:profile:" + NumericsAlignment());

    public static unsafe void Run(Action<nint, int> invoke, bool store, int alignment, string label)
    {
        byte* buffer = stackalloc byte[192];
        byte* aligned = (byte*)(((nuint)buffer + 63) & ~(nuint)63);
        string[] modes = store ? new[] { "aligned", "null", "misaligned", "aligned16-not32", "value-throw", "address-throw", "array-bounds", "array-null" }
            : new[] { "aligned", "null", "misaligned", "aligned16-not32", "address-throw", "array-bounds", "array-null" };
        foreach (string mode in modes)
        {
            for (int index = 0; index < 192; index++) buffer[index] = (byte)(index * 3 + 17);
            _effects = ""; _acquisitions = 0;
            int fault = mode == "value-throw" ? 1 : mode == "address-throw" ? 2 : mode == "array-bounds" ? 3 : mode == "array-null" ? 4 : 0;
            nint pointer = mode == "null" ? 0 : (nint)(aligned + (mode == "misaligned" ? 1 : mode == "aligned16-not32" ? 16 : 0));
            Exception? caught = null;
            try { invoke(pointer, fault); }
            catch (Exception error) { caught = error; }
            if (caught is null) throw new InvalidOperationException(label + ":" + mode + ":unexpected return");
            string expectedEffects = store ? mode == "value-throw" ? "V" : "VA" : "A";
            int expectedAcquisitions = label.EndsWith(":address", StringComparison.Ordinal) || label.EndsWith(":delegate", StringComparison.Ordinal) ? 1 : 0;
            if (_effects != expectedEffects || _acquisitions != expectedAcquisitions) throw new InvalidOperationException(label + ":" + mode + ":effects/acquisition");
            for (int index = 0; index < 192; index++)
                if (buffer[index] != (byte)(index * 3 + 17)) throw new InvalidOperationException(label + ":" + mode + ":memory");
            bool expected = fault switch
            {
                1 => caught is InvalidOperationException && caught.Message == "value-producer",
                2 => caught is InvalidOperationException && caught.Message == "address-producer",
                3 => caught is IndexOutOfRangeException,
                4 => caught is NullReferenceException,
                _ => mode == "misaligned" || mode == "aligned16-not32" && alignment > 16 ? caught is AccessViolationException : caught is NotSupportedException
            };
            if (!expected) throw new InvalidOperationException(label + ":" + mode + ":exception " + caught);
            Console.WriteLine(label + ":" + mode + "|" + caught.GetType().FullName + "|" + caught.Message + "|" + caught.HResult + "|effects=" + _effects + "|acquisitions=" + _acquisitions + "|canary=unchanged");
        }
    }

    public static unsafe void Control(Action<nint, int> invoke, bool store, string label)
    {
        // Public/constrained controls observe the existing implementation. They
        // never pass an invalid address and do not assert unsupported parity.
        byte* buffer = stackalloc byte[192];
        byte* aligned = (byte*)(((nuint)buffer + 63) & ~(nuint)63);
        for (int mode = 0; mode < 2; mode++)
        {
            for (int index = 0; index < 80; index++) aligned[index] = (byte)(index * 3 + 17);
            _effects = ""; _acquisitions = 0;
            Exception? caught = null;
            try { invoke((nint)(aligned + mode), 0); }
            catch (Exception error) { caught = error; }
            uint checksum = 0;
            for (int index = 0; index < 80; index++) checksum = unchecked(checksum * 31 + aligned[index]);
            Console.WriteLine(label + ":" + mode + "|" + (caught is null ? "returned" : caught.GetType().FullName + "|" + caught.Message + "|" + caught.HResult) + "|effects=" + _effects + "|memory=" + checksum);
        }
    }
}
