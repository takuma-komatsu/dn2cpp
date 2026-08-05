namespace DnZlib;

/// <summary>Thrown by the high-level one-shot and stream APIs when the engine reports an error.</summary>
public sealed class ZlibException : Exception
{
    public ZlibException(ZlibResult result, string message)
        : base(message)
    {
        Result = result;
    }

    /// <summary>The low-level result code that produced this exception.</summary>
    public ZlibResult Result { get; }
}
