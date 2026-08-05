namespace GodotSample
{
    /// <summary>
    /// A Godot.Resource subclass (RefCounted lifecycle, not Node). Exercises the
    /// ClassDB-registered construction path for RefCounted-derived classes.
    /// </summary>
    public class MyResource : Godot.Resource
    {
        [Godot.Export]
        public int Tag { get; set; } = 7;

        public int DoubledTag()
        {
            return Tag * 2;
        }
    }
}
