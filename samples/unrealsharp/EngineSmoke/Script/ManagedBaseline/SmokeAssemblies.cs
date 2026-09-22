using ManagedDependency;
using UnrealSharp.Attributes;
using UnrealSharp.Engine;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Log;

namespace ManagedBaseline;

[UModule]
public class AssemblyOrderModule : IModuleInterface
{
    public void StartupModule()
    {
        if (DependencyModule.State != 17) throw new InvalidOperationException("Dependency module must start before its consumer");
        DependencyModule.WriteLifecycle("assembly-main-start=17");
        PendingShutdownProbe.Start();
    }

    public void ShutdownModule()
    {
        if (DependencyModule.State != 17) throw new InvalidOperationException("Dependency module must stop after its consumer");
        PendingShutdownProbe.Stop();
        DependencyModule.WriteLifecycle("assembly-main-stop=17");
    }
}

[UClass]
public partial class AAssemblyProbeActor : AActor
{
    [UProperty]
    public partial UDependencyProbeObject DependencyObject { get; set; }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ProbeAssemblies()
    {
        UDependencyProbeObject created = NewObject<UDependencyProbeObject>(this);
        created.Value = 25;
        DependencyObject = created;
        int result = DependencyModule.State == 17 && ReferenceEquals(DependencyObject, created) &&
            created.GetType().Assembly != GetType().Assembly
            ? DependencyObject.Value + DependencyModule.State : -1;
        UnrealLogger.Log("Dn2CppSmoke", $"assemblies={result}");
        return result;
    }
}
