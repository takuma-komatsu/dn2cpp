using System.Globalization;
using System.Text;

namespace Dn2Cpp;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                Console.WriteLine("Usage: ildiet <assembly.dll> [-r <reference.dll>] --link-xml <file> [-o <directory>] [--project-root <directory>] [--link-feature <com|sre|remoting>]");
                Console.WriteLine("       ildiet --request <request.xml> --result <result.xml>");
                return args.Length == 0 ? 1 : 0;
            }
            var request = new DietRequest();
            string? requestPath = null;
            string? resultPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                string Value()
                {
                    if (++i == args.Length) throw new NotSupportedException("missing value for " + args[i - 1]);
                    return args[i];
                }
                switch (args[i])
                {
                    case "--request": requestPath = Value(); break;
                    case "--result": resultPath = Value(); break;
                    case "-o": request.Output = Value(); break;
                    case "-r": request.References.Add(Value()); break;
                    case "--link-xml": request.LinkXml.Add(Value()); break;
                    case "--project-root": request.ProjectRoots.Add(Value()); break;
                    case "--link-feature": request.Features.Add(Value()); break;
                    default:
                        if (args[i].StartsWith('-') || request.Input.Length != 0)
                            throw new NotSupportedException("unknown argument: " + args[i]);
                        request.Input = args[i];
                        break;
                }
            }
            if (requestPath is not null)
            {
                if (request.Input.Length != 0 || request.References.Count != 0 || request.LinkXml.Count != 0
                    || request.ProjectRoots.Count != 0 || request.Features.Count != 0 || request.Output != "ildiet-output")
                    throw new NotSupportedException("--request cannot be combined with direct input options");
                request = DietRequest.Read(requestPath);
                request.RequestFile = Path.GetFullPath(requestPath);
            }
            if (request.Input.Length == 0) throw new NotSupportedException("an input assembly is required");
            using var diet = new AssemblyDiet(request);
            diet.Run(resultPath);
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine("ILDiet: error: " + exception.Message);
            return 1;
        }
    }
}

internal sealed class DietRequest
{
    internal string? RequestFile;
    internal string Input = "";
    internal string Output = "ildiet-output";
    internal bool CopyAll;
    internal readonly List<string> References = new();
    internal readonly List<string> LinkXml = new();
    internal readonly List<string> ProjectRoots = new();
    internal readonly List<string> Features = new();
    internal readonly List<(string Assembly, string Type)> Roots = new();
    internal readonly List<string> Cuts = new();

    internal static DietRequest Read(string path)
    {
        var root = Dn2Cpp.LinkXml.Parse(path);
        Check(root, "ildiet", "input", "output", "copyAll");
        var request = new DietRequest
        {
            Input = Required(root, "input"),
            Output = Required(root, "output"),
            CopyAll = root.Attr("copyAll") switch
            {
                null or "false" => false,
                "true" => true,
                _ => throw new NotSupportedException("copyAll must be true or false"),
            },
        };
        foreach (var child in root.Children)
        {
            switch (child.Name)
            {
                case "reference": Check(child, "reference", "path"); request.References.Add(Required(child, "path")); break;
                case "linkXml": Check(child, "linkXml", "path"); request.LinkXml.Add(Required(child, "path")); break;
                case "projectRoot": Check(child, "projectRoot", "path"); request.ProjectRoots.Add(Required(child, "path")); break;
                case "feature": Check(child, "feature", "name"); request.Features.Add(Required(child, "name")); break;
                case "root":
                    Check(child, "root", "assembly", "type");
                    request.Roots.Add((child.Attr("assembly") ?? "", Required(child, "type")));
                    break;
                case "cut": Check(child, "cut", "method"); request.Cuts.Add(Required(child, "method")); break;
                default: throw new NotSupportedException("unknown request element: " + child.Name);
            }
            if (child.Children.Count != 0) throw new NotSupportedException("request entries cannot have children");
        }
        return request;
    }

    private static string Required(LinkNode node, string name) => node.Attr(name)
        ?? throw new NotSupportedException("<" + node.Name + "> requires " + name);

    private static void Check(LinkNode node, string name, params string[] attributes)
    {
        if (node.Name != name) throw new NotSupportedException("expected <" + name + ">");
        foreach (string attribute in node.Attributes.Keys)
            if (!attributes.Contains(attribute)) throw new NotSupportedException("unknown request attribute: " + attribute);
    }

    internal static string Escape(string value) => value.Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal).Replace("'", "&apos;", StringComparison.Ordinal);
}
