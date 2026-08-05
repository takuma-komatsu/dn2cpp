namespace GodotSample
{
    /// <summary>A Control exercising NON-lifecycle engine virtuals through the
    /// generalized reverse-dispatch bridge: the engine invokes them when the
    /// corresponding public method is called (get_minimum_size / get_tooltip), covering
    /// a Vector2 return and a Vector2 argument + String return.</summary>
    public class MyControl : Godot.Control
    {
        // A distinctive size proves the engine dispatched to this override rather than a
        // theme default, and that the Vector2 return is encoded.
        public override Godot.Vector2 _GetMinimumSize()
        {
            return new Godot.Vector2(123f, 45f);
        }

        // Echoing an argument component back proves both the Vector2 argument decode and
        // the String return encode.
        public override string _GetTooltip(Godot.Vector2 atPosition)
        {
            return string.Concat("tip@", ((int)atPosition.X).ToString());
        }
    }
}
