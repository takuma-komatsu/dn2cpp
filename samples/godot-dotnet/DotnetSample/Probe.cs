using Godot;

// Reads the sibling Player's exported properties through the engine's Variant
// path, proving that surface is reachable from OUTSIDE the owning instance.
//
// This C# must never name
// Sprite2D: naming it would release its wrapper and the probe would stop probing
// the fallback. ILDiet materializes the nearest retained
// ancestor (Node2D) while every operation still runs against the true native
// object. `managed=` is the discriminator — the MANAGED wrapper's
// GetType().Name. Both ILDiet-enabled gates expect Node2D.
public partial class Probe : Node
{
    public override void _Ready()
    {
        var player = GetNode<Player>("../Player");
        int speed = (int)player.Get("Speed");
        string label = (string)player.Get("Label");
        GD.Print($"DN2CPP_DM_GET speed={speed} label={label}");

        var trimProbe = GetNode("../TrimProbe");
        string probeClass = trimProbe.GetClass();
        bool isNode2D = trimProbe is Node2D;
        var probe2D = (Node2D)trimProbe;
        probe2D.Position = new Vector2(7.5f, 8.5f);
        bool posOk = probe2D.Position == new Vector2(7.5f, 8.5f);
        GD.Print($"DN2CPP_DM_TRIMFALLBACK class={probeClass} isNode2D={isNode2D} name={trimProbe.Name} posOk={posOk} managed={trimProbe.GetType().Name}");

        using var resource = ResourceLoader.Load("res://scene_resource.tres");
        GD.Print($"DN2CPP_DM_SCENE_RESOURCE value={(int)resource.Get("Value")}");
    }
}
