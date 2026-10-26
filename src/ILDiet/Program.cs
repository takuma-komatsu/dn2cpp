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
                Console.WriteLine("Usage: ildiet <assembly.dll> [-r <reference.dll>] --link-xml <file> [-o <directory>] [--project-root <directory>] [--link-feature <com|sre|remoting>] [--work-budget <n>] [--ildiet-skip-assembly <name>] [--stripping-level high|medium]");
                Console.WriteLine("       ildiet --request <request.xml> --result <result.xml>");
                return args.Length == 0 ? 1 : 0;
            }
            var request = new DietRequest();
            string? requestPath = null;
            string? resultPath = null;
            bool strippingLevelSpecified = false;
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
                    case "--ildiet-skip-assembly": request.SkipAssemblies.Add(Value()); break;
                    case "--work-budget": request.WorkBudget = DietRequest.ParseWorkBudget(Value()); break;
                    case "--stripping-level":
                        request.StrippingLevel = DietRequest.ParseStrippingLevel(Value());
                        strippingLevelSpecified = true;
                        break;
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
                    || request.ProjectRoots.Count != 0 || request.Features.Count != 0 || request.WorkBudget is not null
                    || request.SkipAssemblies.Count != 0 || request.Output != "ildiet-output" || strippingLevelSpecified)
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
    internal StrippingLevel StrippingLevel = StrippingLevel.High;
    internal long? WorkBudget;
    internal string Contract = "";
    internal readonly List<string> ReflectionRoots = new();
    internal readonly List<string> References = new();
    internal readonly List<string> LinkXml = new();
    internal readonly List<string> ProjectRoots = new();
    internal readonly List<string> Features = new();
    internal readonly List<(string Assembly, string Type)> Roots = new();
    internal readonly List<string> RewriteAssemblies = new();
    internal readonly List<string> SkipAssemblies = new();
    internal readonly List<(string Assembly, string Type)> TypeRoots = new();
    internal readonly List<(string Assembly, string Type, string Method)> MethodRoots = new();
    internal readonly List<(string Assembly, string BaseType)> ConditionalMembers = new();
    internal readonly List<(string Assembly, string Type)> SuppressedSeedTypes = new();
    internal readonly List<(string Assembly, string Type)> RegistrationAttributes = new();
    internal readonly List<(string Assembly, string Type, string Method)> ConstructorRegistries = new();
    internal readonly List<string> Cuts = new();

    internal static DietRequest Read(string path)
    {
        var root = Dn2Cpp.LinkXml.Parse(path);
        Check(root, "ildiet", "version", "input", "output", "copyAll", "workBudget", "strippingLevel");
        if (Required(root, "version") != "2")
            throw new NotSupportedException("unsupported ILDiet request version");
        var request = new DietRequest
        {
            Input = Required(root, "input"),
            Output = Required(root, "output"),
            WorkBudget = root.Attr("workBudget") is { } budget ? ParseWorkBudget(budget) : null,
            StrippingLevel = ParseStrippingLevel(root.Attr("strippingLevel") ?? "high"),
            Contract = ILDietProtocol.ContractDigest(root),
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
                    request.Roots.Add((Required(child, "assembly"), Required(child, "type")));
                    break;
                case "reflectionRoot":
                    Check(child, "reflectionRoot", "type");
                    request.ReflectionRoots.Add(Required(child, "type"));
                    break;
                case "skipAssembly":
                    Check(child, "skipAssembly", "name");
                    request.SkipAssemblies.Add(Required(child, "name"));
                    break;
                case "rewriteAssembly":
                    Check(child, "rewriteAssembly", "name");
                    request.RewriteAssemblies.Add(Required(child, "name"));
                    break;
                case "typeRoot":
                case "suppressDefaultSeeds":
                case "registrationAttribute":
                    Check(child, child.Name, "assembly", "type");
                    var selected = (Required(child, "assembly"), Required(child, "type"));
                    if (child.Name == "typeRoot") request.TypeRoots.Add(selected);
                    else if (child.Name == "suppressDefaultSeeds") request.SuppressedSeedTypes.Add(selected);
                    else request.RegistrationAttributes.Add(selected);
                    break;
                case "methodRoot":
                case "constructorRegistry":
                    Check(child, child.Name, "assembly", "type", "method");
                    var method = (Required(child, "assembly"), Required(child, "type"), Required(child, "method"));
                    if (child.Name == "methodRoot") request.MethodRoots.Add(method);
                    else request.ConstructorRegistries.Add(method);
                    break;
                case "conditionalMembers":
                    Check(child, "conditionalMembers", "assembly", "baseType");
                    request.ConditionalMembers.Add((Required(child, "assembly"), Required(child, "baseType")));
                    break;
                case "cut": Check(child, "cut", "method"); request.Cuts.Add(Required(child, "method")); break;
                default: throw new NotSupportedException("unknown request element: " + child.Name);
            }
            if (child.Children.Count != 0) throw new NotSupportedException("request entries cannot have children");
        }
        return request;
    }

    internal static StrippingLevel ParseStrippingLevel(string value) => value switch
    {
        "high" => StrippingLevel.High,
        "medium" => StrippingLevel.Medium,
        _ => throw new NotSupportedException("stripping level must be high or medium: " + value),
    };

    internal static long ParseWorkBudget(string value)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long budget) || budget <= 0)
            throw new NotSupportedException("work budget must be a positive integer");
        return budget;
    }

    private static string Required(LinkNode node, string name) => node.Attr(name) is { Length: > 0 } value
        ? value : throw new NotSupportedException("<" + node.Name + "> requires " + name);

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
