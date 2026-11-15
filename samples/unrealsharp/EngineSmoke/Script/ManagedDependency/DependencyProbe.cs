using System.Globalization;
using UnrealSharp.Attributes;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine.Core.Modules;
using UnrealSharp.Log;

namespace ManagedDependency;

[UModule]
public class DependencyModule : IModuleInterface
{
    public static int State { get; private set; }

    public static void WriteLifecycle(string marker)
    {
        UnrealLogger.Log("Dn2CppSmoke", marker);
        string? path = Environment.GetEnvironmentVariable("DN2CPP_SMOKE_LIFECYCLE_FILE");
        if (!string.IsNullOrEmpty(path))
        {
            if (!System.IO.Path.IsPathFullyQualified(path))
                throw new InvalidOperationException("Lifecycle artifact path must be absolute");
            System.IO.File.AppendAllText(path, marker + "\n");
        }
    }

    public void StartupModule()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (State != 0) throw new InvalidOperationException("Dependency module started twice");
        State = 17;
        WriteLifecycle("assembly-dependency-start=17");
    }

    public void ShutdownModule()
    {
        WriteLifecycle($"assembly-dependency-stop={State}");
        State = 0;
    }
}

[UClass]
public partial class UDependencyProbeObject : UObject
{
    [UProperty]
    public partial int Value { get; set; }
}
