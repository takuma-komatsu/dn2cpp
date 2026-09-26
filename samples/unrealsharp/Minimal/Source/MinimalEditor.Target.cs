using UnrealBuildTool;

public class MinimalEditorTarget : TargetRules
{
    public MinimalEditorTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.Latest;
        IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_8;
        ExtraModuleNames.Add("Minimal");
    }
}
