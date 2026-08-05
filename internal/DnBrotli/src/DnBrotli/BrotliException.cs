namespace DnBrotli;

/// <summary>Thrown by the high-level one-shot and stream APIs when the engine reports an error.</summary>
public sealed class BrotliException : Exception
{
    public BrotliException(string message)
        : base(message)
    {
        ErrorCode = BrotliDecoderErrorCode.NoError;
    }

    public BrotliException(BrotliDecoderErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>The detailed decoder error code that produced this exception, or
    /// <see cref="BrotliDecoderErrorCode.NoError"/> for encoder-side failures.</summary>
    public BrotliDecoderErrorCode ErrorCode { get; }
}
