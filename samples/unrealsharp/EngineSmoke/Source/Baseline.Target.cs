using UnrealBuildTool;

public class BaselineTarget : TargetRules
{
    public BaselineTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_8;
        ExtraModuleNames.Add("Baseline");
    }
}
