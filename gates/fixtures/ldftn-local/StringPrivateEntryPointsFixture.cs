using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class StringPrivateEntryPointsFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--string-private-entrypoints" or "--string-private-entrypoints-public"
            or "--string-private-entrypoints-prefix" or "--string-private-entrypoints-noalloc"
            or "--string-private-bad-return" or "--string-private-bad-pointee" or "--string-private-bad-static")) return false;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        bool negative = args[0].StartsWith("--string-private-bad-", StringComparison.Ordinal);
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var owner = core.MainModule.GetType("System.String");
        var raw = owner.Methods.Single(m => m.Name == "GetRawStringDataAsUInt8");
        var length = owner.Methods.Single(m => m.Name == "wcslen");
        bool Common(MethodDefinition m, bool isStatic) => m.Attributes == (MethodAttributes.Assembly | MethodAttributes.HideBySig
                | (isStatic ? MethodAttributes.Static : 0)) && m.ImplAttributes == MethodImplAttributes.IL
            && m.HasBody && m.HasThis != isStatic && !m.ExplicitThis && !m.HasGenericParameters && !m.HasPInvokeInfo
            && m.CallingConvention == MethodCallingConvention.Default;
        if (!Common(raw, false) || raw.Parameters.Count != 0
            || raw.ReturnType is not ByReferenceType { ElementType.MetadataType: MetadataType.Byte }
            || !Common(length, true) || length.ReturnType.MetadataType != MetadataType.Int32 || length.Parameters.Count != 1
            || length.Parameters[0].ParameterType is not PointerType { ElementType.MetadataType: MetadataType.Char }
            || length.Body.Instructions.Count != 3 || length.Body.Instructions[0].OpCode != OpCodes.Ldarg_0
            || length.Body.Instructions[1].OpCode != OpCodes.Call || length.Body.Instructions[2].OpCode != OpCodes.Ret
            || length.Body.Instructions[1].Operand is not MethodDefinition { Name: "IndexOfNullCharacter", DeclaringType.FullName: "System.SpanHelpers" })
            throw new InvalidOperationException("String entry declaration or original forwarding body changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var probe = module.GetType("StringPrivateEntryPointsSubset.Probe");
        if (negative || args[0] is "--string-private-entrypoints" or "--string-private-entrypoints-noalloc")
        {
            foreach (var (name, definition, pointer, virtualCall) in new[] {
                ("RawDirect", raw, false, false), ("RawVirtual", raw, false, true), ("RawPointer", raw, true, false),
                ("LengthDirect", length, false, false), ("LengthPointer", length, true, false) })
            {
                var method = probe.Methods.Single(m => m.Name == name);
                var target = module.ImportReference(definition);
                if (method.ReturnType.FullName != target.ReturnType.FullName
                    || method.Parameters.Count != target.Parameters.Count + (target.HasThis ? 1 : 0))
                    throw new InvalidOperationException("String entry fixture signature changed: " + name);
                if (name == "RawDirect" && args[0] is "--string-private-bad-return" or "--string-private-bad-static")
                {
                    target = new MethodReference(target.Name, args[0] == "--string-private-bad-return"
                        ? new ByReferenceType(module.TypeSystem.Char) : target.ReturnType, target.DeclaringType)
                    { HasThis = args[0] != "--string-private-bad-static" };
                }
                if (name == "LengthDirect" && args[0] == "--string-private-bad-pointee")
                {
                    target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
                    target.Parameters.Add(new ParameterDefinition(new PointerType(module.TypeSystem.Byte)));
                }
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                foreach (var parameter in method.Parameters) il.Emit(OpCodes.Ldarg, parameter);
                if (pointer)
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    if (target.HasThis) site.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(virtualCall ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            foreach (var (name, definition) in new[] { ("TokenRaw", raw), ("TokenLength", length) })
            {
                var method = probe.Methods.Single(m => m.Name == name);
                var get = module.ImportReference(typeof(System.Reflection.MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle) })!);
                method.Body = new MethodBody(method); var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldtoken, module.ImportReference(definition));
                il.Emit(OpCodes.Call, get); il.Emit(OpCodes.Castclass, method.ReturnType); il.Emit(OpCodes.Ret);
            }
            if (!negative) RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, probe.Methods.Single(m => m.Name ==
                (args[0] == "--string-private-entrypoints-noalloc" ? "Allocating" : "RawDirect")));
        }
        module.EntryPoint = probe.Methods.Single(m => m.Name == (args[0] == "--string-private-entrypoints-prefix" ? "RunPublicPrefix"
            : args[0] == "--string-private-entrypoints-noalloc" ? "RunAllocationControl"
            : args[0] == "--string-private-bad-pointee" ? "RunLengthReferenceControl"
            : negative ? "RunRawReferenceControl"
            : args[0] == "--string-private-entrypoints" ? "RunNativeProbe" : "RunProbe"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("string-private-fixture=" + args[0] + ":two-owned-declarations,original-wcslen-body,private-calls-native-only");
        return true;
    }
}
