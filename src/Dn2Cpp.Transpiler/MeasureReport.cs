using System.Globalization;

namespace Dn2Cpp;

/// <summary>Decode-free evidence for diagnostic consumers. No field claims that failed
/// body scans discovered their remaining callees or that emitted C++ would link.</summary>
internal static class MeasureReport
{
    internal static void Write(MeasureResult result, string outputDirectory)
    {
        var detailRows = result.Gaps.Select(g => string.Join('\t', new[]
        {
            Cell(g.Phase), Cell(g.Namespace), Cell(g.Method), Cell(g.ExceptionType),
            Cell(g.Message), Cell(g.CppSymbol ?? ""), Cell(g.Pass),
        })).OrderBy(line => line, StringComparer.Ordinal).ToArray();
        File.WriteAllText(Path.Combine(outputDirectory, "s0-gaps-detail.tsv"),
            string.Concat(detailRows.Select(line => line + "\n").ToArray()));

        bool partialDiscovery = result.Gaps.Any(g => g.Phase == "reach");
        bool truncated = result.Gaps.Any(g => g.Phase is "layout" or "compile-spec");
        int failedBodies = result.Gaps.Where(g => g.Phase == "compile")
            .Select(g => g.CppSymbol ?? g.Method).Distinct(StringComparer.Ordinal).Count();
        int reachMethods = result.Gaps.Where(g => g.Phase == "reach" && g.CppSymbol is not null)
            .Select(g => g.CppSymbol!).Distinct(StringComparer.Ordinal).Count();
        var values = new[]
        {
            "schemaVersion\t1",
            "runStatus\tcompleted",
            "successfulBodyCount\t" + Number(result.Compiled),
            "failedBodyCount\t" + Number(failedBodies),
            "failedEmissionBodyCount\t" + Number(result.Gaps.Where(g => g.Phase == "compile" && g.Pass != "planning")
                .Select(g => g.CppSymbol ?? g.Method).Distinct(StringComparer.Ordinal).Count()),
            "unreplayedPlanningFailureBodyCount\t" + Number(result.Gaps.Where(g => g.Phase == "compile" && g.Pass == "planning")
                .Select(g => g.CppSymbol ?? g.Method).Distinct(StringComparer.Ordinal).Count()),
            "infrastructureGapCount\t" + Number(result.Gaps.Count(g => g.Phase is "layout" or "compile-spec")),
            "reachFailedMethodCount\t" + Number(reachMethods),
            "reachFailedSpecializationCount\t" + Number(result.Gaps.Count(g => g.Phase == "reach" && g.CppSymbol is null)),
            "recordedGapCount\t" + Number(result.Gaps.Count),
            "discoveryStatus\t" + (partialDiscovery ? "partial" : "no-reported-failure"),
            "bodyPassStatus\t" + (truncated ? "truncated" : "finished"),
            "symbolAuditStatus\t" + (result.NamedSymbols is null ? "skipped" : partialDiscovery ? "partial" : "executed"),
            "namedSymbols\t" + (result.NamedSymbols is { } named ? Number(named) : "null"),
            "firstFailurePerBody\ttrue",
            "unknownGapCount\tnull",
        };
        File.WriteAllText(Path.Combine(outputDirectory, "s0-measure.tsv"),
            string.Concat(values.Select(line => line + "\n").ToArray()));
    }

    private static string Cell(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
