using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

internal static class GCHandleEqualityGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--gchandle-equality-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, instance) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, instance)!.SetValue(value, next);
        }
        string input = Path.GetFullPath(args[3]);
        if (File.Exists(input)) throw new InvalidOperationException("model app must be new");
        using (var empty = Mono.Cecil.AssemblyDefinition.CreateAssembly(new Mono.Cecil.AssemblyNameDefinition("GCHandleGuardInput", new Version(1, 0)), "GCHandleGuardInput", Mono.Cecil.ModuleKind.Dll))
            empty.Write(input, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "predicate-guard-only");
        object loaded;
        try { loaded = Model("Compilation").GetMethod("Create", statics)!.Invoke(null, new object[] { new[] { input, Path.GetFullPath(args[2]) }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var module = ((IEnumerable)Get(loaded, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var ownerHandle = reader.TypeDefinitions.Single(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return reader.GetString(type.Name) == "GCHandle" && reader.GetString(type.Namespace) == "System.Runtime.InteropServices" && type.GetDeclaringType().IsNil;
        });
        var allocHandle = reader.GetTypeDefinition(ownerHandle).GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name) == "Alloc" && reader.GetMethodDefinition(h).GetParameters().Count == 1);
        var alloc = ((IDictionary)Get(module, "MethodMap"))[allocHandle]!;
        var descriptor = Get(Get(alloc, "Signature"), "ReturnType");
        var handleClass = Get(descriptor, "Class");
        var predicate = Model("MethodCompiler").GetMethod("IsGCHandleValue", statics)!;
        int checks = 0;
        void Check(bool result, string label)
        {
            if (!result) throw new InvalidOperationException("GCHandle equality guard: " + label);
            checks++;
        }
        Check((bool)predicate.Invoke(null, new[] { descriptor })!, "actual-CoreLib-GCHandle");
        Console.WriteLine("gchandle-equality-actual-model:" + MetadataTokens.GetToken(ownerHandle).ToString("X8", CultureInfo.InvariantCulture));
        void Reject(object value, string field, object next)
        {
            object saved = Get(value, field); Set(value, field, next);
            try { Check(!(bool)predicate.Invoke(null, new[] { descriptor })!, "wrong-" + field); }
            finally { Set(value, field, saved); }
        }
        Reject(handleClass, "_fullName", "Lookalike.GCHandle");
        Reject(handleClass, "IsValueType", false);
        Reject(handleClass, "IsEnum", true);
        Reject(handleClass, "GenericArity", 1);
        Reject(handleClass, "IntrinsicCppName", "Dn2CppOtherHandle");
        Reject(module, "AssemblyName", "LookalikeAssembly");
        Reject(descriptor, "Kind", Enum.Parse(Model("TypeKind"), "ByRef"));
        var typeArray = Array.CreateInstance(Model("TypeDesc"), 1); typeArray.SetValue(descriptor, 0);
        var emptyArray = Array.CreateInstance(Model("TypeDesc"), 0);
        var genericContext = Activator.CreateInstance(Model("GenericContext"), instance, null, new object[] { typeArray, emptyArray }, null)!;
        Reject(handleClass, "Context", genericContext);
        Check((bool)predicate.Invoke(null, new[] { descriptor })!, "restored-actual-positive");
        Console.WriteLine("gchandle-equality-guards:" + checks);
        return true;
    }
}
