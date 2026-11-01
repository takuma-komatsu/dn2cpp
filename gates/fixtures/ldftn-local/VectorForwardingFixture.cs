internal static class VectorForwardingFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-forwarding-fixture") return false;
        var mode = args[3].Split(':');
        if (mode.Length != 2 || mode[0] is not ("explicit" or "alias"))
            throw new ArgumentException("expected explicit/alias:all-runtime/numeric-runtime/canonical-runtime");
        if (mode[1] == "numeric-runtime") VectorAsFixture.BuildNumeric(args[1], args[2], mode[0]);
        else if (mode[1] == "canonical-runtime") VectorAsFixture.BuildCanonical(args[1], args[2], mode[0]);
        else if (mode[1] == "all-runtime")
        {
            var cases = new List<(string Source, string Destination, string Route, string Mutation)>();
            foreach (string source in VectorAsFixture.Elements)
            foreach (string destination in VectorAsFixture.NumericElements)
            foreach (string route in new[] { "direct", "address", "calli" })
                cases.Add((source, destination, route, "valid-runtime"));
            foreach (string context in new[] { "mvar", "var" })
            foreach (string source in new[] { "decimal", "enum" })
            foreach (string route in new[] { "direct", "address", "calli" })
                cases.Add((source, "int", route, "valid-runtime-" + context));
            foreach (string route in new[] { "direct", "address", "calli" })
                cases.Add(("decimal", "int", route, "runtime-throw"));
            VectorAsFixture.Build(args[1], args[2], cases, mode[0]);
        }
        else throw new ArgumentException(mode[1]);
        using var serialized = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[1]);
        var calls = serialized.MainModule.Types.SelectMany(t => t.Methods).Where(m => m.HasBody)
            .SelectMany(m => m.Body.Instructions).Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>()
            .Where(m => m.DeclaringType.FullName.StartsWith("System.Numerics.Vector", StringComparison.Ordinal)).ToArray();
        if (calls.Any(m => m.Name == "As")) throw new InvalidOperationException("forwarding fixture retained a direct As call");
        var forwarding = calls.Where(m => m.Name == "op_Explicit" || m.Name.StartsWith("AsVector", StringComparison.Ordinal)).ToArray();
        int expected = mode[1] switch { "all-runtime" => 231, "numeric-runtime" => 48, _ => 4 };
        if (forwarding.Length != expected || forwarding.Any(m => mode[0] == "alias"
            ? m is not Mono.Cecil.GenericInstanceMethod { GenericArguments.Count: 1 } || !m.Name.StartsWith("AsVector", StringComparison.Ordinal)
            : m is Mono.Cecil.GenericInstanceMethod || m.Name != "op_Explicit"))
            throw new InvalidOperationException("forwarding fixture did not retain its exact family routes");
        if (forwarding.Select(m => m.ReturnType.FullName).Distinct().Count() != (mode[1] == "canonical-runtime" ? 1 : 12))
            throw new InvalidOperationException("forwarding fixture destination coverage changed");
        Console.WriteLine("vector-forwarding-fixture:" + args[3] + ":routes=" + expected);
        return true;
    }
}
