using Godot;

// Abstract generic base script. A partial Node-derived generic class lands in the
// source-generated [AssemblyHasScripts] Type[] as an OPEN generic definition, and
// an open definition is emitted only through its closed instantiations (no bare
// ti_) — so if the emitter cannot render it, the WHOLE attribute drops and
// GodotSharp registers ZERO scripts. This base carries no ScriptPath; it exists
// solely to put the open definition into that attribute.
public abstract partial class GenericBase<T> : Node
{
    protected abstract T Make();

    public override void _Ready()
    {
        T value = Make();
        GD.Print($"DN2CPP_DM_GENERIC value={value}");
    }
}
