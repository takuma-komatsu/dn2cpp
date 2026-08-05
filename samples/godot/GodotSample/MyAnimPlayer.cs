namespace GodotSample
{
    /// <summary>An AnimationPlayer exercising the one engine-virtual shape the other
    /// overrides do not: a Variant argument plus a Variant return through the
    /// reverse-dispatch trampoline. The engine passes each applied key's value and
    /// applies whatever the override returns.</summary>
    public class MyAnimPlayer : Godot.AnimationPlayer
    {
        private int _calls;
        private float _lastX;
        private float _lastY;

        public override Godot.Variant _PostProcessKeyValue(Godot.Animation animation, long track, Godot.Variant value, long objectId, long objectSubIdx)
        {
            _calls = _calls + 1;
            // Record what arrived so GDScript can assert the decode, and return a shifted value
            // so the engine observably applies the OVERRIDE's answer, not the raw key.
            Godot.Vector2 v = value.AsVector2();
            _lastX = v.X;
            _lastY = v.Y;
            return new Godot.Vector2(v.X + 1f, v.Y + 1f);
        }

        public int PostProcessCalls()
        {
            return _calls;
        }

        public Godot.Vector2 LastKeyValue()
        {
            return new Godot.Vector2(_lastX, _lastY);
        }
    }
}
