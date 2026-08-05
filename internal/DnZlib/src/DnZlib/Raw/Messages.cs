using System.Runtime.InteropServices;

namespace DnZlib.Raw;

/// <summary>
/// NUL-terminated UTF-8 error strings interned once into process-lifetime native memory, exactly
/// like zlib's own <c>static const char*</c> message literals — <c>z_stream.msg</c> always points
/// at one of these rather than an allocation made per failed call.
/// </summary>
internal static unsafe class Messages
{
    public static readonly byte* IncorrectHeaderCheck = Intern("incorrect header check\0"u8);
    public static readonly byte* UnknownCompressionMethod = Intern("unknown compression method\0"u8);
    public static readonly byte* InvalidWindowSize = Intern("invalid window size\0"u8);
    public static readonly byte* UnknownHeaderFlagsSet = Intern("unknown header flags set\0"u8);
    public static readonly byte* HeaderCrcMismatch = Intern("header crc mismatch\0"u8);
    public static readonly byte* InvalidBlockType = Intern("invalid block type\0"u8);
    public static readonly byte* InvalidStoredBlockLengths = Intern("invalid stored block lengths\0"u8);
    public static readonly byte* TooManyLengthOrDistanceSymbols = Intern("too many length or distance symbols\0"u8);
    public static readonly byte* InvalidCodeLengthsSet = Intern("invalid code lengths set\0"u8);
    public static readonly byte* InvalidBitLengthRepeat = Intern("invalid bit length repeat\0"u8);
    public static readonly byte* InvalidCodeMissingEndOfBlock = Intern("invalid code -- missing end-of-block\0"u8);
    public static readonly byte* InvalidLiteralLengthsSet = Intern("invalid literal/lengths set\0"u8);
    public static readonly byte* InvalidDistancesSet = Intern("invalid distances set\0"u8);
    public static readonly byte* InvalidLiteralLengthCode = Intern("invalid literal/length code\0"u8);
    public static readonly byte* InvalidDistanceCode = Intern("invalid distance code\0"u8);
    public static readonly byte* InvalidDistanceTooFarBack = Intern("invalid distance too far back\0"u8);
    public static readonly byte* IncorrectDataCheck = Intern("incorrect data check\0"u8);
    public static readonly byte* IncorrectLengthCheck = Intern("incorrect length check\0"u8);

    // zError() table (zlib's z_errmsg[]).
    public static readonly byte* NeedDictionary = Intern("need dictionary\0"u8);
    public static readonly byte* StreamEndMsg = Intern("stream end\0"u8);
    public static readonly byte* Empty = Intern("\0"u8);
    public static readonly byte* FileError = Intern("file error\0"u8);
    public static readonly byte* StreamErrorMsg = Intern("stream error\0"u8);
    public static readonly byte* DataErrorMsg = Intern("data error\0"u8);
    public static readonly byte* InsufficientMemory = Intern("insufficient memory\0"u8);
    public static readonly byte* BufferError = Intern("buffer error\0"u8);
    public static readonly byte* IncompatibleVersion = Intern("incompatible version\0"u8);

    private static byte* Intern(ReadOnlySpan<byte> nulTerminatedUtf8)
    {
        byte* p = (byte*)NativeMemory.Alloc((nuint)nulTerminatedUtf8.Length);
        nulTerminatedUtf8.CopyTo(new Span<byte>(p, nulTerminatedUtf8.Length));
        return p;
    }
}
