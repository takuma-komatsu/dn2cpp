namespace MiniBcl
{
    // A canonical shared-generics owner reachable ONLY through a referenced-only
    // type's base chain: the app names LayoutMid as a type token and nothing else,
    // so LayoutMid and its base LayoutBase<LayoutCtx> are emitted opaquely, while
    // the group's canonical owner LayoutBase<__Canon> is pulled in by the struct
    // ordering alone. That owner gets a FULL field layout, so every field type it
    // spells must be declared too.
    public class LayoutLeaf
    {
        public int Value;
    }

    public class LayoutBeh<T> where T : class
    {
        public T Owner;
    }

    public class LayoutCtx
    {
        public int Id;
    }

#pragma warning disable CS0169, CS0649 // instance fields exist for their LAYOUT, not to be read
    public class LayoutBase<T> where T : class
    {
        private T _ctx;
        private LayoutBeh<T> _beh;
        private readonly LayoutLeaf _leaf = new LayoutLeaf();
        private bool _flag;
    }
#pragma warning restore CS0169, CS0649

    public abstract class LayoutMid : LayoutBase<LayoutCtx>
    {
    }
}
