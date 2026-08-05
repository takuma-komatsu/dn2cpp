using DnZlib;
using DnZlib.Internal;

namespace DnZlib.Tests;

/// <summary>Phase 0 sanity: the assembly loads, public enums resolve, and InternalsVisibleTo works.</summary>
public class SmokeTests
{
    [Fact]
    public void PublicEnumsHaveZlibValues()
    {
        Assert.Equal(0, (int)ZlibResult.Ok);
        Assert.Equal(1, (int)ZlibResult.StreamEnd);
        Assert.Equal(-3, (int)ZlibResult.DataError);
        Assert.Equal(4, (int)FlushMode.Finish);
        Assert.Equal(8, (int)CompressionMethod.Deflated);
    }

    [Fact]
    public void InternalsAreVisibleToTests()
    {
        // Proves the InternalsVisibleTo wiring resolves at compile + run time.
        Assert.Equal("1.3.2", ZlibConstants.Version);
        Assert.Equal(65521u, ZlibConstants.AdlerBase);
        Assert.Equal(258, ZlibConstants.MaxMatch);
    }
}
