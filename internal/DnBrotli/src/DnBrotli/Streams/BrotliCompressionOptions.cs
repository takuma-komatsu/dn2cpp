namespace DnBrotli.Streams;

/// <summary>Whether a compression stream compresses or decompresses (mirrors the BCL enum values).</summary>
public enum CompressionMode
{
    Decompress = 0,
    Compress = 1,
}

/// <summary>Compression level preset (mirrors <c>System.IO.Compression.CompressionLevel</c>).</summary>
public enum CompressionLevel
{
    Optimal = 0,
    Fastest = 1,
    NoCompression = 2,
    SmallestSize = 3,
}

/// <summary>
/// Compression options for <see cref="BrotliStream"/>. <see cref="Quality"/> mirrors the BCL's
/// <c>BrotliCompressionOptions</c>; <see cref="Window"/> and <see cref="Mode"/> are the
/// advanced knobs the BCL hides (the DnZlib <c>ZlibCompressionOptions</c> pattern).
/// </summary>
public sealed class BrotliCompressionOptions
{
    private int _quality = BrotliEncoder.QualityDefault;
    private int _window = BrotliEncoder.WindowBitsDefault;
    private BrotliEncoderMode _mode = BrotliEncoderMode.Generic;

    /// <summary>Brotli quality, 0..11; higher is denser and slower. The default is 4.</summary>
    /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is less than 0 or greater than 11.</exception>
    public int Quality
    {
        get => _quality;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, BrotliEncoder.QualityMin);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BrotliEncoder.QualityMax);
            _quality = value;
        }
    }

    /// <summary>Sliding-window size as log2, 10..24. The default is 22.</summary>
    /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is less than 10 or greater than 24.</exception>
    public int Window
    {
        get => _window;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, BrotliEncoder.WindowBitsMin);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, BrotliEncoder.WindowBitsMax);
            _window = value;
        }
    }

    /// <summary>Encoder tuning mode. The default is <see cref="BrotliEncoderMode.Generic"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException" accessor="set">The value is not a defined <see cref="BrotliEncoderMode"/>.</exception>
    public BrotliEncoderMode Mode
    {
        get => _mode;
        set
        {
            if (value < BrotliEncoderMode.Generic || value > BrotliEncoderMode.Font)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _mode = value;
        }
    }
}
