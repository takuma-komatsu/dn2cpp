using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
using var module = ModuleDefinition.ReadModule(args[0]);
var owner = module.GetType("EnumOps.NarrowEnumStorage");
foreach (string suffix in new[] { "U8", "I8", "U16", "I16", "I32" })
{
    var echo = owner.Methods.Single(m => m.Name == "Echo" + suffix);
    foreach (string route in new[] { "Direct", "Pointer", "Generic", "Local", "Store" })
    {
        var method = owner.Methods.Single(m => m.Name == route + suffix);
        method.Body = new MethodBody(method);
        var il = method.Body.GetILProcessor();
        il.Emit(route == "Store" ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0);
        if (route == "Local")
        {
            method.Body.Variables.Add(new VariableDefinition(echo.Parameters[0].ParameterType));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
        }
        else if (route == "Store")
        {
            il.Emit(OpCodes.Starg_S, method.Parameters[0]);
            il.Emit(OpCodes.Ldarg_0);
        }
        else if (route == "Generic")
        {
            var identity = new GenericInstanceMethod(owner.Methods.Single(m => m.Name == "Identity"));
            identity.GenericArguments.Add(echo.Parameters[0].ParameterType);
            il.Emit(OpCodes.Call, identity);
        }
        else if (route == "Pointer")
        {
            il.Emit(OpCodes.Ldftn, echo);
            var site = new CallSite(echo.ReturnType);
            site.Parameters.Add(new ParameterDefinition(echo.Parameters[0].ParameterType));
            il.Emit(OpCodes.Calli, site);
        }
        else il.Emit(OpCodes.Call, echo);
        if (route is "Local" or "Store" or "Generic") il.Emit(OpCodes.Conv_I8);
        il.Emit(OpCodes.Ret);
    }
}
module.EntryPoint = owner.Methods.Single(m => m.Name == "Run");
// CLR probing must not resolve the sibling native EnumOps.exe as this assembly.
module.Assembly.Name.Name = Path.GetFileNameWithoutExtension(args[1]);
module.Name = Path.GetFileName(args[1]);
module.Write(args[1]);
Console.WriteLine("narrow enum fixture: direct, function pointer, generic, local, argument storage");
