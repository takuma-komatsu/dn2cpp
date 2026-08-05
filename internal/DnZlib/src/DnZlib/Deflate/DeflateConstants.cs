namespace DnZlib.Deflate;

/// <summary>Return state of a block-compression step (mirrors zlib's <c>block_state</c>).</summary>
internal enum BlockState
{
    NeedMore = 0,      // block not completed, need more input or more output
    BlockDone = 1,     // block flush performed
    FinishStarted = 2, // finish started, need only more output at next deflate
    FinishDone = 3,    // finish done, accept no more input or output
}

/// <summary>Which block algorithm a compression level uses (replaces zlib's function pointer).</summary>
internal enum DeflateFunc
{
    Stored = 0,
    Fast = 1,
    Slow = 2,
    Quick = 3,
    Medium = 4,
}

/// <summary>Per-level match-finding configuration (mirrors zlib's <c>configuration_table</c>).</summary>
internal readonly struct DeflateConfig(ushort goodLength, ushort maxLazy, ushort niceLength, ushort maxChain, DeflateFunc func)
{
    public readonly ushort GoodLength = goodLength;
    public readonly ushort MaxLazy = maxLazy;
    public readonly ushort NiceLength = niceLength;
    public readonly ushort MaxChain = maxChain;
    public readonly DeflateFunc Func = func;
}

internal static class DeflateConstants
{
    public const int Literals = 256;
    public const int LengthCodes = 29;
    public const int LCodes = Literals + 1 + LengthCodes; // 286
    public const int DCodes = 30;
    public const int BlCodes = 19;
    public const int HeapSize = 2 * LCodes + 1; // 573
    public const int MaxBits = 15;
    public const int MaxBlBits = 7;
    public const int EndBlock = 256;
    public const int Rep3To6 = 16;
    public const int RepZ3To10 = 17;
    public const int RepZ11To138 = 18;

    public const int StoredBlock = 0;
    public const int StaticTrees = 1;
    public const int DynTrees = 2;

    public const int ZDeflated = 8;

    public const int MinMatch = 3;
    public const int MaxMatch = 258;
    public const int MinLookahead = MaxMatch + MinMatch + 1; // 262
    public const int WinInit = MaxMatch;
    public const int DistCodeLen = 512;
    public const int TooFar = 4096;
    public const int Nil = 0;
    public const int PresetDict = 0x20;
    public const byte OsCode = 3; // Unix

    public const int MaxStored = 65535;
    public const int MaxMemLevel = 9;
    public const int DefMemLevel = 8;

    // Status values (zlib deflate.h).
    public const int InitState = 42;
    public const int GzipState = 57;
    public const int ExtraState = 69;
    public const int NameState = 73;
    public const int CommentState = 91;
    public const int HcrcState = 103;
    public const int BusyState = 113;
    public const int FinishState = 666;

    /// <summary>Level 0..9 configuration table.</summary>
    public static readonly DeflateConfig[] Configuration =
    [
        new(0, 0, 0, 0, DeflateFunc.Stored),      // 0 store only
        new(4, 4, 8, 4, DeflateFunc.Quick),       // 1 max speed, no lazy matches
        new(4, 5, 16, 8, DeflateFunc.Fast),        // 2
        new(4, 6, 32, 32, DeflateFunc.Fast),       // 3
        new(4, 12, 32, 24, DeflateFunc.Medium),    // 4 lazy matches (zlib-ng medium tuning)
        new(8, 16, 32, 32, DeflateFunc.Medium),    // 5
        new(8, 16, 128, 128, DeflateFunc.Medium),  // 6
        new(8, 32, 128, 256, DeflateFunc.Medium),  // 7
        new(32, 128, 258, 1024, DeflateFunc.Medium), // 8
        new(32, 258, 258, 4096, DeflateFunc.Medium), // 9 max compression
    ];

    /// <summary>zlib's RANK macro used to order flush requests.</summary>
    public static int Rank(int f) => (f << 1) - (f > 4 ? 9 : 0);
}
