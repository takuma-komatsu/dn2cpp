namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private bool _usesPlatformIsa;
    private readonly HashSet<string> _platformIsaHeaders = new(StringComparer.Ordinal);

    /// <summary>Records a capability token or helper named by the real emission pass.
    /// Planning and measure compile bodies too, but their discarded text must not widen
    /// the generated header.</summary>
    internal void NotePlatformIsaUse(string? helperHeader = null)
    {
        if (Phase != EmitPhase.Emission)
            return;
        _usesPlatformIsa = true;
        if (helperHeader is not null)
            _platformIsaHeaders.Add(helperHeader);
    }

    internal bool UsesPlatformIsa => _usesPlatformIsa;

    internal IEnumerable<string> PlatformIsaHeaders =>
        _platformIsaHeaders.OrderBy(h => h, StringComparer.Ordinal);
}
