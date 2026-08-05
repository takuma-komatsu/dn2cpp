using Godot;

// Concrete derivation of the abstract generic base, attached to a scene node.
// Its registration only works if [AssemblyHasScripts] survived transpilation
// with its full Type[], open generic base included — see GenericBase.cs.
public partial class GenericConcrete : GenericBase<int>
{
    protected override int Make() => 42;
}
