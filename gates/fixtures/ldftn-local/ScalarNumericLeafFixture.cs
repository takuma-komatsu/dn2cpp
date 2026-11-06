using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using EmitOp = System.Reflection.Emit.OpCodes;

internal static class ScalarNumericLeafFixture
{
    private static readonly string[] Routes = { "direct", "address", "calli" };
    private static readonly ulong[] SingleBits = { 0, 0x80000000, 1, 0x80000001, 0x007FFFFF, 0x807FFFFF,
        0x00800000, 0x80800000, 0x3F800000, 0x40000000, 0x7F7FFFFF, 0xFF7FFFFF,
        0x7F800000, 0xFF800000, 0x7FC00001, 0xFFC12345, 0x7F800001, 0xFF812345 };
    private static readonly ulong[] DoubleBits = { 0, 0x8000000000000000, 1, 0x8000000000000001,
        0x000FFFFFFFFFFFFF, 0x800FFFFFFFFFFFFF, 0x0010000000000000, 0x8010000000000000,
        0x3FF0000000000000, 0x4000000000000000, 0x7FEFFFFFFFFFFFFF, 0xFFEFFFFFFFFFFFFF,
        0x7FF0000000000000, 0xFFF0000000000000, 0x7FF8000000000001, 0xFFF8123456789ABC,
        0x7FF0000000000001, 0xFFF0123456789ABC };
    private static IEnumerable<(Type Owner, string Name)> Targets()
    {
        yield return (typeof(int), "Sign"); yield return (typeof(decimal), "get_Exponent");
        foreach (Type owner in new[] { typeof(float), typeof(double) })
            foreach (string name in ScalarNumericLeafGuards.FloatNames) yield return (owner, name);
    }
    private static IEnumerable<ulong> Inputs(Type owner)
    {
        if (owner == typeof(int)) return new ulong[] { 0x80000000, 0xFFFFFFFF, 0, 1, 0x7FFFFFFF };
        if (owner == typeof(decimal)) return Enumerable.Range(0, 29).SelectMany(s => new[] { (ulong)s, (ulong)(s + 128) });
        return owner == typeof(float) ? SingleBits : DoubleBits;
    }
    private static string Label(Type owner, string name, string route, ulong bits) => $"leaf-route:{owner.FullName}:{name}:{route}:{bits:X16}=";

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--scalar-numeric-leaf-oracle") { Oracle(); return true; }
        if (args.Length != 3 || args[0] != "--scalar-numeric-leaf-fixture") return false;
        Build(args[1], args[2]); return true;
    }

    private static void Oracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance;
        foreach (var row in Targets())
        foreach (string route in Routes)
        {
            var target = row.Owner.GetMethod(row.Name, flags)!;
            var dynamic = new System.Reflection.Emit.DynamicMethod("ScalarLeaf", typeof(ulong), new[] { typeof(ulong) }, typeof(ScalarNumericLeafFixture).Module, true);
            var il = dynamic.GetILGenerator();
            if (!target.IsStatic)
            {
                var local = il.DeclareLocal(row.Owner);
                if (row.Owner == typeof(decimal))
                {
                    il.Emit(EmitOp.Ldc_I4_M1); il.Emit(EmitOp.Ldc_I4_M1); il.Emit(EmitOp.Ldc_I4_M1);
                    il.Emit(EmitOp.Ldarg_0); il.Emit(EmitOp.Conv_I4); il.Emit(EmitOp.Ldc_I4, 128); il.Emit(EmitOp.And);
                    il.Emit(EmitOp.Ldarg_0); il.Emit(EmitOp.Conv_I4); il.Emit(EmitOp.Ldc_I4, 127); il.Emit(EmitOp.And); il.Emit(EmitOp.Conv_U1);
                    il.Emit(EmitOp.Newobj, typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!);
                    il.Emit(EmitOp.Stloc, local);
                }
                else
                {
                    // Initialize storage without any floating-point conversion.
                    il.Emit(EmitOp.Ldloca, local); il.Emit(EmitOp.Ldarg_0);
                    if (row.Owner == typeof(float)) { il.Emit(EmitOp.Conv_U4); il.Emit(EmitOp.Stind_I4); }
                    else il.Emit(EmitOp.Stind_I8);
                }
                il.Emit(EmitOp.Ldloca, local);
            }
            else
            {
                il.Emit(EmitOp.Ldarg_0);
                if (row.Owner != typeof(double)) il.Emit(EmitOp.Conv_I4);
            }
            if (route == "direct") il.Emit(EmitOp.Call, target);
            else
            {
                il.Emit(EmitOp.Ldftn, target);
                if (route == "address")
                {
                    var address = il.DeclareLocal(typeof(IntPtr)); il.Emit(EmitOp.Stloc, address); il.Emit(EmitOp.Ldloc, address);
                }
                Type[] parameters = target.IsStatic ? target.GetParameters().Select(p => p.ParameterType).ToArray() : new[] { row.Owner.MakeByRefType() };
                il.EmitCalli(EmitOp.Calli, System.Reflection.CallingConventions.Standard, target.ReturnType, parameters, null);
            }
            il.Emit(EmitOp.Conv_U8); il.Emit(EmitOp.Ret);
            var call = (Func<ulong, ulong>)dynamic.CreateDelegate(typeof(Func<ulong, ulong>));
            foreach (ulong bits in Inputs(row.Owner)) Console.WriteLine(Label(row.Owner, row.Name, route, bits) + call(bits).ToString("X16"));
        }
    }

    private static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ScalarNumericLeafRoutes", new Version(1, 0)), "ScalarNumericLeafRoutes", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(host);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(main); app.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        var result = new VariableDefinition(module.TypeSystem.UInt64); main.Body.Variables.Add(result);
        foreach (var row in Targets())
        foreach (string route in Routes)
        {
            var definition = core.MainModule.GetType(row.Owner.FullName).Methods.Single(m => m.Name == row.Name);
            var target = module.ImportReference(definition);
            var method = new MethodDefinition("Case" + host.Methods.Count, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.UInt64); host.Methods.Add(method);
            method.Parameters.Add(new ParameterDefinition(module.TypeSystem.UInt64));
            var il = method.Body.GetILProcessor();
            if (target.HasThis)
            {
                var local = new VariableDefinition(module.ImportReference(row.Owner)); method.Body.Variables.Add(local);
                if (row.Owner == typeof(decimal))
                {
                    il.Emit(OpCodes.Ldc_I4_M1); il.Emit(OpCodes.Ldc_I4_M1); il.Emit(OpCodes.Ldc_I4_M1);
                    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Conv_I4); il.Emit(OpCodes.Ldc_I4, 128); il.Emit(OpCodes.And);
                    il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Conv_I4); il.Emit(OpCodes.Ldc_I4, 127); il.Emit(OpCodes.And); il.Emit(OpCodes.Conv_U1);
                    il.Emit(OpCodes.Newobj, module.ImportReference(typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) })!));
                    il.Emit(OpCodes.Stloc, local);
                }
                else
                {
                    il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Ldarg_0);
                    if (row.Owner == typeof(float)) { il.Emit(OpCodes.Conv_U4); il.Emit(OpCodes.Stind_I4); }
                    else il.Emit(OpCodes.Stind_I8);
                }
                il.Emit(OpCodes.Ldloca, local);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); if (row.Owner != typeof(double)) il.Emit(OpCodes.Conv_I4);
            }
            if (route == "direct") il.Emit(OpCodes.Call, target);
            else
            {
                il.Emit(OpCodes.Ldftn, target);
                if (route == "address")
                {
                    var address = new VariableDefinition(module.TypeSystem.IntPtr); method.Body.Variables.Add(address); il.Emit(OpCodes.Stloc, address); il.Emit(OpCodes.Ldloc, address);
                }
                var site = new CallSite(target.ReturnType);
                site.Parameters.Add(new ParameterDefinition(target.HasThis ? new ByReferenceType(module.ImportReference(row.Owner)) : target.Parameters[0].ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            il.Emit(OpCodes.Conv_U8); il.Emit(OpCodes.Ret);
            foreach (ulong bits in Inputs(row.Owner))
            {
                entry.Emit(OpCodes.Ldstr, Label(row.Owner, row.Name, route, bits));
                entry.Emit(OpCodes.Ldc_I8, unchecked((long)bits)); entry.Emit(OpCodes.Call, method); entry.Emit(OpCodes.Stloc, result);
                entry.Emit(OpCodes.Ldloca, result); entry.Emit(OpCodes.Ldstr, "X16");
                entry.Emit(OpCodes.Call, module.ImportReference(typeof(ulong).GetMethod(nameof(ulong.ToString), new[] { typeof(string) })!));
                entry.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!));
                entry.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!));
            }
        }
        entry.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("scalar-leaf-routes-generated: CLR oracle uses visibility-skipping DynamicMethods; do not execute this external private-call fixture on CLR");
    }
}
