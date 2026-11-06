using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeVirtualSlotCountFixture
{
    private interface IBase { void Base(); static abstract int BaseStatic(); }
    private interface IMixed : IBase
    {
        static abstract int StaticFirst();
        void InstanceMiddle();
        static virtual int StaticDefault() => 3;
        static int Helper() => 4;
        private void Hidden() { }
        void DefaultInstance() { }
        T GenericInstance<T>(T value);
    }
    private sealed class Concrete : IMixed
    {
        public void Base() { }
        public static int BaseStatic() => 1;
        public static int StaticFirst() => 2;
        public void InstanceMiddle() { }
        public T GenericInstance<T>(T value) => value;
    }

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--runtime-virtual-slot-order-oracle")
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            var map = typeof(Concrete).GetInterfaceMap(typeof(IMixed));
            string names = string.Join(",", map.InterfaceMethods.Select(m => m.Name));
            if (names != "InstanceMiddle,DefaultInstance,GenericInstance,StaticFirst,StaticDefault"
                || map.TargetMethods[1].DeclaringType != typeof(IMixed)
                || map.TargetMethods[4].DeclaringType != typeof(IMixed)
                || typeof(Concrete).GetInterfaceMap(typeof(IBase)).InterfaceMethods.Length != 2)
                throw new InvalidOperationException("CLR virtual-slot ordering changed");
            Console.WriteLine("virtual-slot-order-clr:" + names);
            Console.WriteLine("virtual-slot-order-clr:default-and-inherited:True");
            return true;
        }
        if (args.Length != 3 || args[0] is not ("--runtime-virtual-slot-count" or "--runtime-virtual-slot-count-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeVirtualSlotCountSubset.Program");
        bool reflectionOnly = args[0] == "--runtime-virtual-slot-count-reflection";
        foreach (string name in new[] { "GetNumVirtuals", "GetNumVirtualsAndStaticVirtuals" })
        {
            bool internalCall = name == "GetNumVirtuals";
            var method = owner.Methods.Single(m => m.Name == name && m.IsAssembly);
            if (!method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || method.HasBody == internalCall
                || !method.IsIL || !method.IsManaged || method.IsInternalCall != internalCall || method.IsPInvokeImpl
                || method.ReturnType.MetadataType != MetadataType.Int32 || method.Parameters.Count != 1
                || method.Parameters[0].ParameterType.FullName != "System.RuntimeType"
                || method.Parameters[0].ParameterType.Resolve().Module != core.MainModule)
                throw new InvalidOperationException("Runtime virtual-slot declaration changed: " + name);
            if (!internalCall)
            {
                var import = owner.Methods.Single(m => m.Name == name && m.IsPInvokeImpl);
                if (!import.IsPrivate || !import.IsStatic || import.HasBody || !import.IsPreserveSig
                    || import.Parameters.Count != 1 || import.Parameters[0].ParameterType.FullName != "System.Runtime.CompilerServices.QCallTypeHandle"
                    || import.ReturnType.MetadataType != MetadataType.Int32 || import.PInvokeInfo.Module.Name != "QCall"
                    || import.PInvokeInfo.EntryPoint != "RuntimeTypeHandle_GetNumVirtualsAndStaticVirtuals"
                    || !method.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == import))
                    throw new InvalidOperationException("Runtime virtual-slot QCall binding changed");
            }
            if (reflectionOnly) continue;
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == (internalCall ? "Count" : "Combined") + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("virtual-slot-count-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection") + ",exact-CoreLib-signatures,native-only");
        return true;
    }
}
