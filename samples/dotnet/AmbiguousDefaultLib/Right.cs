namespace AmbiguousDefaultLib
{
    // The version an application compiles against: IRight and IBoxRight
    // override nothing, so the left interfaces hold every most specific body.
    public interface IRight : IBase
    {
    }

    public interface IBoxRight<T> : IBox<T>
    {
    }
}
