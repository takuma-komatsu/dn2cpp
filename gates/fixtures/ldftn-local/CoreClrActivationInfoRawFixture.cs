using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CoreClrActivationInfoRawFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--coreclr-activation-info-raw" or "--coreclr-activation-info-raw-address")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.RuntimeTypeHandle").Methods.Single(m => m.Name == "GetActivationInfo" && m.IsPInvokeImpl);
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
            || definition.ImplAttributes != MethodImplAttributes.PreserveSig || definition.HasBody || definition.RVA != 0
            || definition.PInvokeInfo is not { Module.Name: "QCall", EntryPoint: "RuntimeTypeHandle_GetActivationInfo" }
            || definition.Parameters.Count != 6 || definition.Parameters[0].ParameterType.FullName != "System.Runtime.CompilerServices.ObjectHandleOnStack"
            || definition.Parameters.Skip(1).Any(p => p.ParameterType is not PointerType))
            throw new InvalidOperationException("Raw activation-info import metadata changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule; var target = module.ImportReference(definition);
        var checks = module.GetType("CoreClrActivationInfoSubset.ActivationChecks");
        var entry = new MethodDefinition("RawImportCompileOnly", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        checks.Methods.Add(entry); entry.Body.InitLocals = true;
        var local = new VariableDefinition(target.Parameters[0].ParameterType); entry.Body.Variables.Add(local);
        var il = entry.Body.GetILProcessor(); il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, local.VariableType); il.Emit(OpCodes.Ldloc, local);
        for (int index = 1; index < 6; index++) { il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Conv_U); }
        if (args[0].EndsWith("-address", StringComparison.Ordinal))
        {
            il.Emit(OpCodes.Ldftn, target); var site = new CallSite(module.TypeSystem.Void);
            foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            il.Emit(OpCodes.Calli, site);
        }
        else il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret); module.EntryPoint = entry;
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("activation-info-raw: compile-only, actual QCall declaration; DO NOT EXECUTE");
        return true;
    }
}
