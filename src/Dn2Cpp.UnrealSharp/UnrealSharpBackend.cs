using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Dn2Cpp.UnrealSharp;

internal sealed class UnrealSharpBackend : IEmitBackend
{
    private const string Bootstrap = "UnrealSharp.Plugins.Dn2CppBootstrap";
    private readonly IReadOnlyList<string> _loadOrderFiles;
    private readonly List<string> _assemblies = new();
    private readonly List<string> _preparedAssemblies = new();
    private readonly List<MethodInfo?> _initializers = new();
    private MethodInfo _initialize = null!, _prepare = null!, _register = null!, _complete = null!, _tick = null!, _shutdown = null!, _release = null!;
    private ClassInfo _plugins = null!, _callbacks = null!;

    internal UnrealSharpBackend(IReadOnlyList<string> loadOrderFiles) => _loadOrderFiles = loadOrderFiles;
    public string RuntimeHeader => "dn2cpp_unrealsharp.h";
    public ICallIntrinsics? CallIntrinsics => null;
    public bool CatchUnmanagedCallbackExceptions => true;
    private static bool IsNativeBind(MethodInfo method)
        => (method.Module.AssemblyName is "UnrealSharp.Binds" or "UnrealSharp.Core" or "UnrealSharp" or "UnrealSharp.Log")
            && (method.DeclaringClass.FullName.StartsWith("UnrealSharp.Interop.Bind_", StringComparison.Ordinal)
                || method.DeclaringClass.FullName.StartsWith("UnrealSharp.Core.Interop.Bind_", StringComparison.Ordinal)
                || method.DeclaringClass.FullName.StartsWith("UnrealSharp.Log.Bind_", StringComparison.Ordinal));
    public (string Type, string Expression)? MarshalCalliArgument(MethodInfo enclosing, TypeDesc declared, string expression)
    {
        if (!IsNativeBind(enclosing))
            return null;
        if (declared.Class is { IsDelegate: true })
            throw new NotSupportedException("native bind callback requires an audited synchronous lifetime: " + enclosing.DeclaringClass.FullName);
        return declared.IsString ? ("const char*", $"dn2cpp_pinvoke_str_to_utf8({expression})")
            : declared.Kind == TypeKind.Primitive && declared.Primitive == PrimitiveTypeCode.Boolean
                ? ("bool", $"({expression} != 0)") : null;
    }
    public string? CalliAbiType(MethodInfo enclosing, TypeDesc declared)
        => IsNativeBind(enclosing) && declared.Kind == TypeKind.Primitive
            && declared.Primitive == PrimitiveTypeCode.Boolean ? "bool" : null;
    public bool ScopedCalliDelegateCallbacks(MethodInfo enclosing)
        => IsNativeBind(enclosing) && enclosing.DeclaringClass.FullName == "UnrealSharp.Interop.Bind_FScriptSet";
    public string? UnmanagedCallbackFailureValue(MethodInfo method)
        => method.Module.AssemblyName == "UnrealSharp.Core"
            && method.DeclaringClass.FullName == "UnrealSharp.Core.UnmanagedCallbacks"
            && method.Name == "InvokeManagedMethod"
            && method.Signature.ReturnType.Kind == TypeKind.Primitive
            && method.Signature.ReturnType.Primitive == PrimitiveTypeCode.Int32
            && method.Signature.ParameterTypes.Length == 5
            && method.Signature.ParameterTypes.All(t => t.Kind == TypeKind.Primitive
                && t.Primitive == PrimitiveTypeCode.IntPtr) ? "1" : null;
    public bool ShouldSkipMethodBody(ClassInfo cls, MethodInfo method) => false;

    public void ConfigureILDiet(ILDietRootPolicy policy, IReadOnlyList<string> paths, TranspileOptions options)
    {
        if (options.TrimReflection || options.HotupdateBase)
            throw new NotSupportedException("--unrealsharp requires complete reflection metadata and does not support hot-update images");
        // Unreal's generated registration and marshalling helpers have native-only callers.
        foreach (string path in paths)
        {
            using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
            var reader = pe.GetMetadataReader();
            string assembly = reader.GetString(reader.GetAssemblyDefinition().Name);
            if (!Compilation.IsFrameworkAssemblyName(assembly))
                foreach (var handle in reader.TypeDefinitions)
                    policy.FullTypeRoots.Add((assembly, TypeName(reader, handle)));
        }
    }

    private static bool ValidAssemblyName(string name)
        => name.Length != 0 && name.All(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '_' || ch == '-');

    private static string TypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        string name = reader.GetString(type.Name);
        var parent = type.GetDeclaringType();
        if (!parent.IsNil)
            return TypeName(reader, parent) + "+" + name;
        string ns = reader.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static MethodInfo Resolve(Compilation c, string name, int parameters)
    {
        var cls = c.Classes.ToArray().FirstOrDefault(t => t.Module.AssemblyName == "UnrealSharp.Plugins" && t.FullName == Bootstrap)
            ?? throw new NotSupportedException("--unrealsharp requires the dn2cpp UnrealSharp fork bootstrap in UnrealSharp.Plugins.dll");
        var method = cls.Methods.FirstOrDefault(m => m.Name == name && m.IsStatic
            && m.Signature.ParameterTypes.Length == parameters && m.Signature.ReturnType.IsVoid)
            ?? throw new NotSupportedException("--unrealsharp: incompatible bootstrap method " + name);
        var signature = method.Signature.ParameterTypes;
        bool valid = name == "Initialize"
            ? signature.All(t => t.Kind == TypeKind.Primitive && t.Primitive == PrimitiveTypeCode.IntPtr)
            : name == "Tick"
                ? signature[0].Kind == TypeKind.Primitive && signature[0].Primitive == PrimitiveTypeCode.Single
                : (name == "Shutdown" || name == "ReleaseHandles") || (signature[0].Class?.FullName ?? signature[0].ExternalName) == "System.Reflection.Assembly";
        if (!valid)
            throw new NotSupportedException("--unrealsharp: incompatible bootstrap signature " + name);
        return method;
    }

    public IEnumerable<MethodInfo> AdditionalRootMethods(Compilation c)
    {
        _initialize = Resolve(c, "Initialize", 4);
        _prepare = Resolve(c, "PrepareAssembly", 1);
        _register = Resolve(c, "RegisterAssembly", 1);
        _complete = Resolve(c, "CompleteAssembly", 1);
        _tick = Resolve(c, "Tick", 1);
        _shutdown = Resolve(c, "Shutdown", 0);
        _release = Resolve(c, "ReleaseHandles", 0);
        foreach (var module in c.Modules.ToArray())
            if (c.IsUserModule(module))
            {
                if (!ValidAssemblyName(module.AssemblyName))
                    throw new NotSupportedException("--unrealsharp: invalid assembly simple name " + module.AssemblyName);
                _preparedAssemblies.Add(module.AssemblyName);
            }
        _plugins = c.Classes.ToArray().FirstOrDefault(t => t.Module.AssemblyName == "UnrealSharp.Plugins" && t.FullName == "UnrealSharp.Plugins.PluginsCallbacks")
            ?? throw new NotSupportedException("--unrealsharp: PluginsCallbacks missing");
        _callbacks = c.Classes.ToArray().FirstOrDefault(t => t.FullName == "UnrealSharp.Core.ManagedCallbacks" && t.Module.AssemblyName == "UnrealSharp.Core")
            ?? throw new NotSupportedException("--unrealsharp: ManagedCallbacks missing");
        c.ForceEmittedClasses.Add(_plugins);
        c.ForceEmittedClasses.Add(_callbacks);
        if (_loadOrderFiles.Count == 0)
            throw new NotSupportedException("--unrealsharp requires --unrealsharp-load-order");
        var orders = new List<(int Priority, string File, List<string> Names)>();
        foreach (string file in _loadOrderFiles)
        {
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(file));
                var root = json.RootElement;
                var names = new List<string>();
                foreach (var entry in root.GetProperty("LoadOrder").EnumerateArray())
                {
                    string name = entry.GetString() ?? "";
                    if (!ValidAssemblyName(name))
                        throw new NotSupportedException("--unrealsharp: invalid assembly simple name in " + file);
                    names.Add(name);
                }
                orders.Add((root.GetProperty("Priority").GetInt32(), Path.GetFileNameWithoutExtension(file), names));
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException
                || ex is UnauthorizedAccessException || ex is InvalidOperationException
                || ex is KeyNotFoundException || ex is FormatException || ex is OverflowException)
            {
                throw new NotSupportedException("--unrealsharp: invalid load-order file " + file + ": " + ex.Message, ex);
            }
        }
        foreach (var order in orders.OrderByDescending(o => o.Priority).ThenBy(o => o.File, StringComparer.Ordinal))
            foreach (string name in order.Names)
            {
                if (_assemblies.Contains(name))
                    throw new NotSupportedException("--unrealsharp: duplicate load-order assembly " + name);
                if (!c.Modules.Any(m => m.AssemblyName == name))
                    throw new NotSupportedException("--unrealsharp: load-order assembly is missing: " + name);
                _assemblies.Add(name);
                _initializers.Add(c.Classes.ToArray().FirstOrDefault(t => t.Module.AssemblyName == name && t.Name == "<Module>")
                    ?.Methods.FirstOrDefault(m => m.Name == ".cctor"));
            }
        if (!_assemblies.Contains(c.AppModule.AssemblyName))
            throw new NotSupportedException("--unrealsharp: input assembly is absent from the load order: " + c.AppModule.AssemblyName);
        foreach (var cls in c.Classes.ToArray())
            if (cls.Name == "<Module>" && c.IsUserModule(cls.Module) && cls.MembersReady
                && cls.Methods.Any(m => m.Name == ".cctor") && !_assemblies.Contains(cls.Module.AssemblyName))
                throw new NotSupportedException("--unrealsharp: module initializer assembly is absent from the load order: " + cls.Module.AssemblyName);
        var methods = new List<MethodInfo> { _initialize, _prepare, _register, _complete, _tick, _shutdown, _release };
        // Snapshot before signature decoding: resolving a generic signature can grow Classes.
        foreach (var cls in c.Classes.ToArray())
        {
            if (!c.IsUserModule(cls.Module) || cls.GenericArity != 0)
                continue;
            foreach (var method in cls.Methods.ToArray())
                if (method.Rva != 0 && method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters().Count == 0)
                    methods.Add(method);
        }
        return methods;
    }

    public IEnumerable<ClassInfo> ExternallyAllocatedClasses(Compilation c)
        => c.Classes.ToArray().Where(t => c.IsUserModule(t.Module) && t.GenericArity == 0 && !t.IsAbstract && !t.IsValueType);

    public void EmitEpilogue(CppEmitter emitter, StringBuilder sb, IReadOnlyList<MethodInfo> cctors)
    {
        sb.AppendLine("static std::recursive_mutex dn2cpp_us_mutex;");
        sb.AppendLine("static std::thread::id dn2cpp_us_thread;");
        sb.AppendLine("static std::atomic<int> dn2cpp_us_state{0}; // new, registering, ready, stopped, failed, active");
        sb.AppendLine("static size_t dn2cpp_us_next = 0;");
        sb.AppendLine("static bool dn2cpp_us_stopped = false;");
        sb.AppendLine("static void dn2cpp_us_boundary_sink(const char* where, Dn2CppObject* error) {");
        sb.AppendLine("    dn2cpp_us_state = 4;");
        sb.AppendLine("    std::fprintf(stderr, \"UnrealSharp callback failure: %s\\n\", where ? where : \"unknown\");");
        sb.AppendLine("    if (error) dn2cpp_report_unhandled_exception(error);");
        sb.AppendLine("}");
        sb.AppendLine("static int32_t dn2cpp_us_publish(int next, Dn2CppUnrealSharpResult* result) {");
        sb.AppendLine("    int active = 5;");
        sb.AppendLine("    if (!dn2cpp_us_state.compare_exchange_strong(active, next)) return dn2cpp_us_result(result, \"managed callback failed\");");
        sb.AppendLine("    return dn2cpp_us_result(result);");
        sb.AppendLine("}");
        sb.AppendLine("DN2CPP_RT_EXPORT int32_t dn2cpp_unrealsharp_initialize(const Dn2CppUnrealSharpHost* host, Dn2CppUnrealSharpResult* result) try {");
        sb.AppendLine("    std::lock_guard<std::recursive_mutex> lock(dn2cpp_us_mutex);");
        sb.AppendLine("    if (!result) return 0;");
        sb.AppendLine("    if (dn2cpp_us_state != 0) return dn2cpp_us_result(result, \"initialization is one-shot\");");
        sb.AppendLine("    if (!host || host->abi_version != DN2CPP_UNREALSHARP_ABI_VERSION || host->struct_size != sizeof(*host)) return dn2cpp_us_result(result, \"UnrealSharp host ABI mismatch\");");
        sb.AppendLine("    if (host->ue_major != 5 || host->ue_minor != 8 || host->ue_patch != 2 || !host->unrealsharp_revision || std::strcmp(host->unrealsharp_revision, DN2CPP_UNREALSHARP_REVISION)) return dn2cpp_us_result(result, \"UnrealSharp or UE compatibility mismatch\");");
        sb.AppendLine($"    if (host->plugin_callbacks_size != sizeof({_plugins.CppStructName}) || host->managed_callbacks_size != sizeof({_callbacks.CppStructName})) return dn2cpp_us_result(result, \"callback table size mismatch\");");
        sb.AppendLine("    if (host->reserved || !host->working_directory || !host->plugin_callbacks || !host->binds_callbacks || !host->managed_callbacks) return dn2cpp_us_result(result, \"missing host callbacks or invalid reserved field\");");
        sb.AppendLine("    dn2cpp_us_state = 5;");
        sb.AppendLine("    dn2cpp_us_thread = std::this_thread::get_id();");
        sb.AppendLine("    try {");
        sb.AppendLine("        dn2cpp_gc_set_manual_finalizer_drain(1);");
        sb.AppendLine("        dn2cpp_gc_set_self_roots_default(1);");
        sb.AppendLine("        dn2cpp_set_native_callback_gc_registration(1);");
        emitter.EmitInitCalls(sb, Array.Empty<MethodInfo>());
        sb.AppendLine("        dn2cpp_set_boundary_exception_sink(&dn2cpp_us_boundary_sink);");
        sb.AppendLine("        dn2cpp_gc_ensure_thread_registered();");
        sb.AppendLine($"        {_initialize.CppName}((intptr_t)host->working_directory, (intptr_t)host->plugin_callbacks, (intptr_t)host->binds_callbacks, (intptr_t)host->managed_callbacks);");
        sb.AppendLine("        if (dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"bootstrap callback failed\");");
        foreach (string assembly in _preparedAssemblies)
        {
            sb.AppendLine($"        {_prepare.CppName}(\"{assembly}\");");
            sb.AppendLine("        if (dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"assembly preparation callback failed\");");
        }
        sb.AppendLine($"        return dn2cpp_us_publish({(_assemblies.Count == 0 ? 2 : 1)}, result);");
        EmitCatch(sb, "initialize");
        sb.AppendLine("} catch (...) { return dn2cpp_us_result(result, \"native lifecycle lock failure\"); }");
        sb.AppendLine("DN2CPP_RT_EXPORT int32_t dn2cpp_unrealsharp_register_assembly(const char* name, Dn2CppUnrealSharpResult* result) try {");
        EmitEntry(sb, 1);
        sb.AppendLine("    if (!name) return dn2cpp_us_result(result, \"missing assembly name\");");
        sb.AppendLine("    try {");
        for (int i = 0; i < _assemblies.Count; i++)
        {
            string name = _assemblies[i];
            sb.AppendLine($"        if (dn2cpp_us_next == {i} && std::strcmp(name, \"{name}\") == 0) {{");
            sb.AppendLine("            int expected = 1;");
            sb.AppendLine("            if (!dn2cpp_us_state.compare_exchange_strong(expected, 5)) return dn2cpp_us_result(result, \"registration callback failed\");");
            sb.AppendLine($"            {_register.CppName}(\"{name}\");");
            if (_initializers[i] is { } initializer)
                sb.AppendLine($"            {initializer.CppName}__ensure();");
            sb.AppendLine($"            {_complete.CppName}(\"{name}\");");
            sb.AppendLine("            if (dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"registration callback failed\");");
            sb.AppendLine("            ++dn2cpp_us_next;");
            sb.AppendLine($"            return dn2cpp_us_publish(dn2cpp_us_next == {_assemblies.Count} ? 2 : 1, result);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("        return dn2cpp_us_result(result, \"unknown assembly or incorrect load order\");");
        EmitCatch(sb, "register assembly");
        sb.AppendLine("} catch (...) { return dn2cpp_us_result(result, \"native lifecycle lock failure\"); }");
        sb.AppendLine("DN2CPP_RT_EXPORT int32_t dn2cpp_unrealsharp_tick(float delta, Dn2CppUnrealSharpResult* result) try {");
        EmitEntry(sb, 2);
        sb.AppendLine("    int expected = 2;");
        sb.AppendLine("    if (!dn2cpp_us_state.compare_exchange_strong(expected, 5)) return dn2cpp_us_result(result, \"tick callback failed\");");
        sb.AppendLine("    try {");
        sb.AppendLine("        dn2cpp_sched_pump();");
        sb.AppendLine("        if (dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"scheduler callback failed\");");
        sb.AppendLine($"        {_tick.CppName}(delta);");
        sb.AppendLine("        dn2cpp_gc_drain_finalizers();");
        sb.AppendLine("        if (dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"tick callback failed\");");
        sb.AppendLine("        return dn2cpp_us_publish(2, result);");
        EmitCatch(sb, "tick");
        sb.AppendLine("} catch (...) { return dn2cpp_us_result(result, \"native lifecycle lock failure\"); }");
        sb.AppendLine("DN2CPP_RT_EXPORT int32_t dn2cpp_unrealsharp_shutdown(Dn2CppUnrealSharpResult* result) try {");
        sb.AppendLine("    std::lock_guard<std::recursive_mutex> lock(dn2cpp_us_mutex);");
        sb.AppendLine("    if (!result) return 0;");
        sb.AppendLine("    if (dn2cpp_us_state == 0 || dn2cpp_us_state == 5 || dn2cpp_us_stopped) return dn2cpp_us_result(result, \"runtime is not active\");");
        sb.AppendLine("    if (dn2cpp_us_thread != std::this_thread::get_id()) return dn2cpp_us_result(result, \"shutdown requires the initializing thread\");");
        sb.AppendLine("    dn2cpp_us_stopped = true;");
        sb.AppendLine("    dn2cpp_us_state = 3;");
        sb.AppendLine("    try {");
        sb.AppendLine("        bool module_failure = false;");
        sb.AppendLine("        try {");
        sb.AppendLine($"            {_shutdown.CppName}();");
        sb.AppendLine("            dn2cpp_gc_drain_finalizers();");
        sb.AppendLine("        } catch (Dn2CppException& ex) {");
        sb.AppendLine("            module_failure = true;");
        sb.AppendLine("            dn2cpp_report_boundary_exception(ex.obj, \"UnrealSharp shutdown\");");
        sb.AppendLine("        } catch (...) { module_failure = true; }");
        sb.AppendLine("        if (dn2cpp_runtime_quiesce(5000) < 0) return dn2cpp_us_result(result, \"shutdown timed out; handles and image retained\");");
        sb.AppendLine("        if (module_failure || dn2cpp_us_state == 4) return dn2cpp_us_result(result, \"shutdown failed; handles and image retained\");");
        sb.AppendLine($"        {_release.CppName}();");
        sb.AppendLine("        return dn2cpp_us_result(result);");
        EmitCatch(sb, "shutdown");
        sb.AppendLine("} catch (...) { return dn2cpp_us_result(result, \"native lifecycle lock failure\"); }");
    }

    private static void EmitEntry(StringBuilder sb, int state)
    {
        sb.AppendLine("    std::lock_guard<std::recursive_mutex> lock(dn2cpp_us_mutex);");
        sb.AppendLine("    if (!result) return 0;");
        sb.AppendLine($"    if (dn2cpp_us_state != {state}) return dn2cpp_us_result(result, \"invalid runtime state\");");
        sb.AppendLine("    if (dn2cpp_us_thread != std::this_thread::get_id()) return dn2cpp_us_result(result, \"operation requires the initializing thread\");");
    }

    private static void EmitCatch(StringBuilder sb, string boundary)
    {
        sb.AppendLine("    } catch (Dn2CppException& ex) {");
        sb.AppendLine("        dn2cpp_us_state = 4;");
        sb.AppendLine($"        dn2cpp_report_boundary_exception(ex.obj, \"UnrealSharp {boundary}\");");
        sb.AppendLine($"        return dn2cpp_us_result(result, \"managed exception during {boundary}\");");
        sb.AppendLine("    } catch (...) {");
        sb.AppendLine("        dn2cpp_us_state = 4;");
        sb.AppendLine($"        return dn2cpp_us_result(result, \"native exception during {boundary}\");");
        sb.AppendLine("    }");
    }
}
