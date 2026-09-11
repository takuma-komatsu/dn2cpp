using Godot;

public partial class HandwrittenContainer : Node
{
    public sealed class OrdinaryNested
    {
        private static readonly int Value;

        static OrdinaryNested()
        {
            Value = 73;
        }

        public static int ReadValue() => Value;
    }
}
