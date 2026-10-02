using System.Globalization;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Log;

namespace ManagedMinimal;

[UModule]
public class MinimalModule : IModuleInterface
{
    public void StartupModule()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        UnrealLogger.Log("Dn2CppAndroidMinimal", "managed-start=OK");
    }

    public void ShutdownModule() { }
}

[UClass]
public partial class AMinimalManagedActor : AActor
{
    [UFunction(FunctionFlags.BlueprintCallable)]
    public int Answer()
    {
        return 40 + 2;
    }
}
