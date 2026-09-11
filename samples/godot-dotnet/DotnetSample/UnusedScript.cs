using Godot;

public partial class UnusedScript : Node
{
    private static readonly System.Action Callback = () => GD.Print("unused-helper");

    static UnusedScript()
    {
        GD.Print("DN2CPP_DM_UNUSED_SCRIPT_CCTOR");
    }

    public override void _Ready()
    {
        Callback();
    }
}
