using Godot;

// Reads the sibling Player's exported properties through the engine's Variant
// path, proving that surface is reachable from OUTSIDE the owning instance.
//
// Also carries the --trim-godot-classes fallback probe. This C# must NEVER name
// Sprite2D: naming it would release its wrapper and the probe would stop probing
// the fallback. Under the trim the wrapper materializes as the nearest released
// ancestor (Node2D) while every operation still runs against the true native
// object. `managed=` is the discriminator — the MANAGED wrapper's
// GetType().Name, so the trimmed gate expects Node2D and the untrimmed one
// Sprite2D; everything before it must match across both.
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
    }
}
