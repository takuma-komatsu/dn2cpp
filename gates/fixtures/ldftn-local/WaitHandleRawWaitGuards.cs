using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Assembly = System.Reflection.Assembly;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

internal static class WaitHandleRawWaitGuards
{
    private delegate bool RawWrapper(string name, BlobReader blob, out EntityHandle span, out int count);
    private delegate bool RawImport(BlobReader blob);
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static unsafe bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--wait-handle-raw-wait-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var production = Assembly.LoadFrom(Path.GetFullPath(args[1]));
        Type Model(string name) => production.GetType("Dn2Cpp." + name, true)!;
        object Get(object value, string name) => value.GetType().GetField(name, Instance) is { } field
            ? field.GetValue(value)! : value.GetType().GetProperty(name, Instance)!.GetValue(value)!;
        void Set(object value, string name, object? next)
        {
            if (value.GetType().GetField(name, Instance) is { } field) field.SetValue(value, next);
            else value.GetType().GetProperty(name, Instance)!.SetValue(value, next);
        }
        int checks = 0;
        void Check(bool success, string label)
        {
            if (!success) throw new InvalidOperationException("raw-wait guard: " + label);
            checks++;
        }
        var wrapperPredicate = Model("CoreIntrinsics").GetMethod("IsWaitHandleRawWaitWrapper", Static)!;
        var importPredicate = Model("CoreIntrinsics").GetMethod("IsWaitHandleRawWaitImport", Static)!;
        var rawWrapper = Model("CoreIntrinsics").GetMethod("TryReadWaitHandleRawWaitWrapperSignature", Static)!.CreateDelegate<RawWrapper>();
        var rawImport = Model("CoreIntrinsics").GetMethod("IsWaitHandleRawWaitImportSignature", Static)!.CreateDelegate<RawImport>();
        void Raw(string name, string hex, bool expected, string label)
        {
            byte[] bytes = Convert.FromHexString(hex);
            fixed (byte* pointer = bytes)
                Check(rawWrapper(name, new BlobReader(pointer, bytes.Length), out _, out _) == expected, label);
        }
        const string three = "000308151185E801180208", four = "000408151185E80118080208";
        Raw("WaitMultipleIgnoringSyncContext", three, true, "raw-three");
        Raw("WaitMultipleIgnoringSyncContextCore", three, true, "raw-core-three");
        Raw("WaitMultipleIgnoringSyncContext", four, true, "raw-four");
        Raw("WaitMultipleIgnoringSyncContextCore", four, false, "no-core-four");
        Raw("WaitOne", three, false, "other-name");
        foreach (string hex in new[] {
            "200308151185E801180208", "250308151185E801180208", "10000308151185E801180208",
            "000309151185E801180208", "000308151285E801180208", "000308151185E801080208",
            "000308151185EA01180208", "00030815110001180208", "00030810151185E801180208",
            "0003081D151185E801180208", "000308151185E801180808", "000308151185E801180209",
            three + "00", "000408151185E80118020208" })
            Raw("WaitMultipleIgnoringSyncContext", hex, false, "raw-control-" + hex);
        for (int length = 0; length < three.Length; length += 2)
            Raw("WaitMultipleIgnoringSyncContext", three[..length], false, "raw-truncated-" + length);
        foreach (string hex in new[] { "0004080F18080808", "2004080F18080808", "0004080F1A080808", "0004080F18080208", "0004081018080808", "0004080F1808080800" })
        {
            byte[] bytes = Convert.FromHexString(hex);
            fixed (byte* pointer = bytes)
                Check(rawImport(new BlobReader(pointer, bytes.Length)) == (hex == "0004080F18080808"), "import-raw-" + hex);
        }
        var options = Activator.CreateInstance(Model("TranspileOptions"))!;
        string input = Path.GetFullPath(args[4]), corelib = Path.GetFullPath(args[2]);
        Set(options, "Input", input); Set(options, "UseILDiet", false); Set(options, "SharedGenerics", false);
        Set(options, "AutoRef", false); Set(options, "IsaSurfaceDump", "raw-wait-metadata-only");
        object compilation;
        try { compilation = Model("Compilation").GetMethod("Create", Static)!.Invoke(null, new object[] { new[] { input, corelib }, options })!; }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
        var module = ((IEnumerable)Get(compilation, "Modules")).Cast<object>().Single(m => (string)Get(m, "AssemblyName") == "System.Private.CoreLib");
        var reader = (MetadataReader)Get(module, "Reader");
        var owner = reader.TypeDefinitions.Single(h => reader.GetString(reader.GetTypeDefinition(h).Namespace) == "System.Threading"
            && reader.GetString(reader.GetTypeDefinition(h).Name) == "WaitHandle");
        int wrappers = 0, imports = 0;
        foreach (var handle in reader.GetTypeDefinition(owner).GetMethods())
        {
            var method = ((IDictionary)Get(module, "MethodMap"))[handle]!;
            string name = (string)Get(method, "Name");
            bool wrapper = name is "WaitMultipleIgnoringSyncContext" or "WaitMultipleIgnoringSyncContextCore";
            bool import = name == "<WaitMultipleIgnoringSyncContext>g____PInvoke|2_0";
            bool Accept(System.Reflection.MethodInfo predicate) => (bool)predicate.Invoke(null, new[] { method })!;
            bool ready = (bool)Get(method, "SignatureReady");
            Check(Accept(wrapperPredicate) == wrapper, "actual-wrapper-before:" + name);
            Check(Accept(importPredicate) == import, "actual-import-before:" + name);
            Check((bool)Get(method, "SignatureReady") == ready, "no-eager-decode:" + name);
            if (!wrapper && !import) continue;
            method.GetType().GetMethod("EnsureSignature", Instance)!.Invoke(method, null);
            Check(Accept(wrapperPredicate) == wrapper, "actual-wrapper-after:" + name);
            Check(Accept(importPredicate) == import, "actual-import-after:" + name);
            wrappers += wrapper ? 1 : 0; imports += import ? 1 : 0;
            var predicate = wrapper ? wrapperPredicate : importPredicate;
            void Reject(object target, string field, object replacement)
            {
                object saved = Get(target, field); Set(target, field, replacement);
                try { Check(!Accept(predicate), name + ":" + field); }
                finally { Set(target, field, saved); }
            }
            Reject(module, "AssemblyName", "Application");
            Reject(method, "Attributes", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig);
            Reject(method, "ImplAttributes", MethodImplAttributes.InternalCall);
            Reject(Get(method, "DeclaringClass"), "IsValueType", true);
            if (import)
            {
                var row = Model("CoreIntrinsics").GetField("PiAbsentCoreClrQCall", Static)!.GetValue(null)!;
                Check((bool)row.GetType().GetMethod("Matches")!.Invoke(row, new[] { method })!, "existing-QCall-descriptor");
                Check((bool)Model("Compilation").GetMethod("LowersToPInvoke", Instance)!.Invoke(compilation, new[] { method })!, "existing-PInvoke-route");
                var cutArgs = new object?[] { method, null };
                Check(!(bool)Model("CoreIntrinsics").GetMethod("TryFindCutRow", Static)!.Invoke(null, cutArgs)!, "intrinsic-owner-does-not-steal-import");
            }
        }
        Check(wrappers == 3 && imports == 1, "exact-owned-family");
        var diet = Assembly.LoadFrom(Path.GetFullPath(args[3]));
        var copied = diet.GetType("Dn2Cpp.AssemblyDiet", true)!.GetMethod("IsCopiedWaitHandleRawWaitWrapper", Static)!;
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        foreach (var method in core.MainModule.GetType("System.Threading.WaitHandle").Methods)
        {
            bool expected = method.Name is "WaitMultipleIgnoringSyncContext" or "WaitMultipleIgnoringSyncContextCore";
            bool Accept() => (bool)copied.Invoke(null, new object[] { method, "System.Threading.WaitHandle" })!;
            Check(Accept() == expected, "copied-actual:" + method.Name);
            if (!expected) continue;
            var attributes = method.Attributes; method.IsPublic = true;
            Check(!Accept(), "copied-visibility"); method.Attributes = attributes;
            var first = method.Parameters[0].ParameterType;
            method.Parameters[0].ParameterType = new ByReferenceType(first);
            Check(!Accept(), "copied-span-byref"); method.Parameters[0].ParameterType = first;
            var result = method.ReturnType; method.ReturnType = core.MainModule.TypeSystem.UInt32;
            Check(!Accept(), "copied-return"); method.ReturnType = result;
        }
        Console.WriteLine("raw-wait-guards=" + checks + ":three-wrappers,one-import,lazy-signatures,existing-QCall-route,copied-parity");
        return true;
    }
}
