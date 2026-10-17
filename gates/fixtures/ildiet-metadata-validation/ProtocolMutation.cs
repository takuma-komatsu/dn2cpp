using System.Diagnostics;
using System.Xml.Linq;

static class ProtocolMutation
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--request" || args[2] != "--result") return false;
        string companion = Environment.GetEnvironmentVariable("DN2CPP_TEST_ILDIET_COMPANION")
            ?? throw new InvalidOperationException("protocol proxy requires the real companion path");
        string mutation = Environment.GetEnvironmentVariable("DN2CPP_TEST_ILDIET_MUTATION")
            ?? throw new InvalidOperationException("protocol proxy requires a mutation");
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        start.ArgumentList.Add("exec");
        start.ArgumentList.Add(companion);
        foreach (string argument in args) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("real companion did not start");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("real companion failed: " + process.ExitCode);
        string output = XDocument.Load(args[1]).Root!.Attribute("output")!.Value;
        string pointer = Path.Combine(output, "ildiet-result.xml");
        if (File.ReadAllText(pointer) != File.ReadAllText(args[3]))
            throw new InvalidOperationException("real companion returned a mismatched publication pointer");
        var document = XDocument.Load(args[3]);
        var root = document.Root!;
        var assembly = root.Elements("assembly").First();
        switch (mutation)
        {
            case "version": root.SetAttributeValue("version", "1"); break;
            case "generation": root.SetAttributeValue("generation", "../outside"); break;
            case "hash": assembly.SetAttributeValue("hash", "0000000000000000"); break;
            case "contract": root.SetAttributeValue("contract", "0000000000000000"); break;
            case "mode": assembly.SetAttributeValue("mode", "unknown"); break;
            case "skipped-rewritten": assembly.SetAttributeValue("mode", "rewritten"); break;
            case "fallback": root.SetAttributeValue("fallback", "unknown"); break;
            case "copied-registry":
                assembly.SetAttributeValue("mode", "copied");
                assembly.SetAttributeValue("registryRewritten", "true");
                root.SetAttributeValue("constructorRegistriesRewritten", "true");
                break;
            case "duplicate-assembly": assembly.AddAfterSelf(new XElement(assembly)); break;
            case "counter-order":
                var counters = root.Element("counters")!;
                var attributes = counters.Attributes().Select(a => new XAttribute(a)).Reverse().ToArray();
                counters.RemoveAttributes();
                counters.Add(attributes);
                break;
            case "pointer-mismatch":
                var work = root.Element("counters")!.Attribute("work")!;
                work.Value = work.Value == "0" ? "1" : "0";
                break;
            case "file-bytes":
                using (var stream = File.Open(root.Attribute("input")!.Value, FileMode.Append)) stream.WriteByte(0);
                break;
            default: throw new InvalidOperationException("unknown protocol mutation: " + mutation);
        }
        string serialization = document.ToString(SaveOptions.DisableFormatting);
        File.WriteAllText(args[3], serialization);
        if (mutation != "pointer-mismatch") File.WriteAllText(pointer, serialization);
        Console.WriteLine("protocol-fixture-mutated=" + mutation);
        return true;
    }
}
