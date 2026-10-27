using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class NarrowEnumArgumentFixture
{
    internal static void Apply(ModuleDefinition module)
    {
        var owner = module.GetType("NarrowEnumArgumentSubset.EnumArguments451");
        foreach (string suffix in new[] { "U8", "I8", "U16", "I16", "I32", "U32", "I64" })
        {
            var echo = owner.Methods.Single(m => m.Name == "Echo" + suffix);
            foreach (string route in new[] { "Direct", "Pointer", "DirectAddress" })
            {
                if (suffix == "I64" && route == "DirectAddress") continue;
                var target = route == "DirectAddress" ? owner.Methods.Single(m => m.Name == "Address" + suffix) : echo;
                var stub = owner.Methods.Single(m => m.Name == route + suffix);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
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
            if (suffix == "I64") continue;
            var store = owner.Methods.Single(m => m.Name == "Store" + suffix);
            store.Body = new MethodBody(store);
            var storeIl = store.Body.GetILProcessor();
            storeIl.Emit(OpCodes.Ldarg_1);
            storeIl.Emit(OpCodes.Starg_S, store.Parameters[0]);
            storeIl.Emit(OpCodes.Ldarg_0);
            storeIl.Emit(suffix == "U32" ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
            storeIl.Emit(OpCodes.Ret);
            var local = owner.Methods.Single(m => m.Name == "Local" + suffix);
            local.Body = new MethodBody(local);
            local.Body.Variables.Add(new VariableDefinition(echo.Parameters[0].ParameterType));
            var localIl = local.Body.GetILProcessor();
            localIl.Emit(OpCodes.Ldarg_0);
            localIl.Emit(OpCodes.Stloc_0);
            localIl.Emit(OpCodes.Ldloc_0);
            localIl.Emit(suffix == "U32" ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
            localIl.Emit(OpCodes.Ret);
        }
        var identity = owner.Methods.Single(m => m.Name == "Identity");
        foreach (string suffix in new[] { "U8", "I8", "U16", "I16" })
            foreach (string route in new[] { "Generic", "GenericOther", "GenericPointer" })
            {
                var element = owner.NestedTypes.Single(t => t.Name == suffix + (route == "GenericOther" ? "Other" : ""));
                var target = new GenericInstanceMethod(identity);
                target.GenericArguments.Add(element);
                var stub = owner.Methods.Single(m => m.Name == route + suffix);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (route == "GenericPointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(element);
                    site.Parameters.Add(new ParameterDefinition(element));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Conv_I8);
                il.Emit(OpCodes.Ret);
            }
        var instance = owner.Methods.Single(m => m.Name == "DirectInstance");
        var instanceTarget = owner.NestedTypes.Single(t => t.Name == "U8Owner").Methods.Single(m => m.Name == "Echo");
        instance.Body = new MethodBody(instance);
        var instanceIl = instance.Body.GetILProcessor();
        instanceIl.Emit(OpCodes.Ldarg_0);
        instanceIl.Emit(OpCodes.Ldarg_1);
        instanceIl.Emit(OpCodes.Callvirt, instanceTarget);
        instanceIl.Emit(OpCodes.Ret);
    }
}
