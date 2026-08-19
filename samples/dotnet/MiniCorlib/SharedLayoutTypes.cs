namespace MiniBcl
{
    // LayoutMid is referenced only by a type token, so its grouped base's canonical
    // owner reaches struct ordering without field-type closure. Its full layout must
    // still name only declared field types.
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

    public struct LayoutAligned
    {
        public long Wide;
        public byte Tail;
    }

#pragma warning disable CS0169, CS0649 // instance fields exist for their LAYOUT, not to be read
    public class LayoutBase<T> where T : class
    {
        private T _ctx;
        private LayoutBeh<T> _beh;
        private readonly LayoutLeaf _leaf = new LayoutLeaf();
        private LayoutAligned _aligned;
        private bool _flag;
    }
#pragma warning restore CS0169, CS0649

    public abstract class LayoutMid : LayoutBase<LayoutCtx>
    {
    }
}
