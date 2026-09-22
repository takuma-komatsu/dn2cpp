using System.Globalization;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Log;

namespace ManagedBaseline;

[UModule]
public class SmokeModule : IModuleInterface
{
    public void StartupModule()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        UnrealLogger.Log("Dn2CppSmoke", "module-start");
    }

    public void ShutdownModule() => UnrealLogger.Log("Dn2CppSmoke", "module-stop");
}

[UClass]
public partial class ASmokeActor : AActor
{
    [UProperty(PropertyFlags.EditAnywhere | PropertyFlags.BlueprintReadWrite)]
    public partial int Counter { get; set; }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int Add(int value)
    {
        Counter += value;
        UnrealLogger.Log("Dn2CppSmoke", $"add={Counter}");
        return Counter;
    }

    public override void BeginPlay()
    {
        base.BeginPlay();
        Counter = 40;
        ActorTickEnabled = false;
        UnrealLogger.Log("Dn2CppSmoke", $"begin-play={Add(2)} tick={IsActorTickEnabled()}");
    }
}
