using UnrealBuildTool;

public class Minimal : ModuleRules
{
    public Minimal(ReadOnlyTargetRules target) : base(target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new[] { "Core", "CoreUObject", "Engine", "UnrealSharpCore" });
    }
}
