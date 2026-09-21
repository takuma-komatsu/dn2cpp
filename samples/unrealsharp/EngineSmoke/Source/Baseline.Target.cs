using UnrealBuildTool;

public class BaselineTarget : TargetRules
{
    public BaselineTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_8;
        if (Configuration == UnrealTargetConfiguration.Development)
        {
            BuildEnvironment = TargetBuildEnvironment.Unique;
            GlobalDefinitions.Add("LLM_ENABLED_IN_CONFIG=0");
        }
        ExtraModuleNames.Add("Baseline");
    }
}
