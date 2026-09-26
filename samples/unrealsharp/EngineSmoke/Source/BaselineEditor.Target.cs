using UnrealBuildTool;

public class BaselineEditorTarget : TargetRules
{
    public BaselineEditorTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_8;
        ExtraModuleNames.Add("Baseline");
    }
}
