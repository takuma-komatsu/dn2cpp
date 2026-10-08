namespace HotGvmCall.Library;

public delegate string? LateText();

public struct GvmCallCell<T>
{
    public ILateLayoutBin<T>? Item;
}

public interface ILateLayoutBin<T>
{
    [Dn2Cpp.Runtime.HotPath(NoAlloc = true)]
    int NoAllocConstant()
    {
#if LATE_NOALLOC_BAD
        return LateNoAllocHelper.Count();
#else
        return 1;
#endif
    }

    int Cold()
    {
        LateText text = this.ToString;
        return new T[1].Length + text()!.Length;
    }
}

public static class LateNoAllocHelper
{
    public static int Count() => Allocate() is null ? 0 : 1;

    public static object Allocate() => new object();
}
