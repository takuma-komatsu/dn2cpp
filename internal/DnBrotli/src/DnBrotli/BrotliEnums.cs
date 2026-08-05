namespace DnBrotli;

/// <summary>Result of <c>BrotliDecoderDecompress</c> / <c>BrotliDecoderDecompressStream</c>.
/// Values match brotli's <c>BrotliDecoderResult</c> exactly.</summary>
public enum BrotliDecoderResult
{
    /// <summary>Decoding error, e.g. corrupted input or memory allocation problem.</summary>
    Error = 0,
    /// <summary>Decoding successfully completed.</summary>
    Success = 1,
    /// <summary>Partially done; should be called again with more input.</summary>
    NeedsMoreInput = 2,
    /// <summary>Partially done; should be called again with more output.</summary>
    NeedsMoreOutput = 3,
}

/// <summary>Detailed decoder error code. Values match brotli's
/// <c>BrotliDecoderErrorCode</c> (<c>BROTLI_DECODER_ERROR_CODES_LIST</c>) exactly.</summary>
public enum BrotliDecoderErrorCode
{
    NoError = 0,

    // Same as BrotliDecoderResult values.
    Success = 1,
    NeedsMoreInput = 2,
    NeedsMoreOutput = 3,

    // Errors caused by invalid input.
    ErrorFormatExuberantNibble = -1,
    ErrorFormatReserved = -2,
    ErrorFormatExuberantMetaNibble = -3,
    ErrorFormatSimpleHuffmanAlphabet = -4,
    ErrorFormatSimpleHuffmanSame = -5,
    ErrorFormatClSpace = -6,
    ErrorFormatHuffmanSpace = -7,
    ErrorFormatContextMapRepeat = -8,
    ErrorFormatBlockLength1 = -9,
    ErrorFormatBlockLength2 = -10,
    ErrorFormatTransform = -11,
    ErrorFormatDictionary = -12,
    ErrorFormatWindowBits = -13,
    ErrorFormatPadding1 = -14,
    ErrorFormatPadding2 = -15,
    ErrorFormatDistance = -16,

    // -17 code is reserved.

    ErrorCompoundDictionary = -18,
    ErrorDictionaryNotSet = -19,
    ErrorInvalidArguments = -20,

    // Memory allocation problems.
    ErrorAllocContextModes = -21,
    ErrorAllocTreeGroups = -22,
    // -23..-24 codes are reserved for distinct tree groups.
    ErrorAllocContextMap = -25,
    ErrorAllocRingBuffer1 = -26,
    ErrorAllocRingBuffer2 = -27,
    // -28..-29 codes are reserved for dynamic ring-buffer allocation.
    ErrorAllocBlockTypeTrees = -30,

    // "Impossible" states.
    ErrorUnreachable = -31,
}

/// <summary>Decoder parameters for <c>BrotliDecoderSetParameter</c>. Values match brotli's
/// <c>BrotliDecoderParameter</c> exactly.</summary>
public enum BrotliDecoderParameter
{
    /// <summary>Disable "canny" ring-buffer allocation strategy: ring buffer is allocated
    /// according to the window size, despite the real size of the content.</summary>
    DisableRingBufferReallocation = 0,
    /// <summary>Flag that determines if "Large Window Brotli" is used.</summary>
    LargeWindow = 1,
}

/// <summary>Operations for <c>BrotliEncoderCompressStream</c>. Values match brotli's
/// <c>BrotliEncoderOperation</c> exactly.</summary>
public enum BrotliEncoderOperation
{
    /// <summary>Process input; may postpone producing output.</summary>
    Process = 0,
    /// <summary>Produce output for all processed input; bytes are guaranteed decodable.</summary>
    Flush = 1,
    /// <summary>Finalize the stream; no more input may be pushed afterwards.</summary>
    Finish = 2,
    /// <summary>Emit a metadata block (skipped by decoders).</summary>
    EmitMetadata = 3,
}

/// <summary>Tuning modes for the encoder. Values match brotli's <c>BrotliEncoderMode</c>
/// exactly.</summary>
public enum BrotliEncoderMode
{
    /// <summary>No known attributes of the input; default.</summary>
    Generic = 0,
    /// <summary>UTF-8 formatted text input.</summary>
    Text = 1,
    /// <summary>WOFF 2.0 font input.</summary>
    Font = 2,
}

/// <summary>Encoder parameters for <c>BrotliEncoderSetParameter</c>. Values match brotli's
/// <c>BrotliEncoderParameter</c> exactly.</summary>
public enum BrotliEncoderParameter
{
    /// <summary><see cref="BrotliEncoderMode"/> tuning parameter.</summary>
    Mode = 0,
    /// <summary>Quality 0..11; higher is denser and slower.</summary>
    Quality = 1,
    /// <summary>Sliding-window size as log2, 10..24 (30 with large window).</summary>
    LgWin = 2,
    /// <summary>Input-block size as log2, 16..24.</summary>
    LgBlock = 3,
    /// <summary>Disable "literal context modeling" (faster/denser trade-off on some inputs).</summary>
    DisableLiteralContextModeling = 4,
    /// <summary>Estimated total input size; 0 (default) means unknown.</summary>
    SizeHint = 5,
    /// <summary>Flag that determines if "Large Window Brotli" is used.</summary>
    LargeWindow = 6,
    /// <summary>Recommended number of postfix bits (NPOSTFIX), 0..3.</summary>
    NPostfix = 7,
    /// <summary>Recommended number of direct distance codes (NDIRECT).</summary>
    NDirect = 8,
    /// <summary>Number of bytes already processed by a different instance (advanced).</summary>
    StreamOffset = 9,
}
