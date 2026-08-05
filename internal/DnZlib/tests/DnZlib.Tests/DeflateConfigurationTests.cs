using DnZlib.Deflate;

namespace DnZlib.Tests;

/// <summary>
/// Locks in the level-to-strategy decisions in <see cref="DeflateConstants.Configuration"/> so an
/// accidental edit gets caught immediately rather than silently drifting compression ratio/speed.
/// See README "Deferred" notes for the levels 8-9 deflate_medium evaluation this records.
/// </summary>
public class DeflateConfigurationTests
{
    [Fact]
    public void Level8And9_UseMediumStrategy()
    {
        Assert.Equal(DeflateFunc.Medium, DeflateConstants.Configuration[8].Func);
        Assert.Equal(DeflateFunc.Medium, DeflateConstants.Configuration[9].Func);
    }
}
