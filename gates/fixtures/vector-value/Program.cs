using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args is ["generate", var mode, var core, var output])
    VectorValueInputs.Generate(mode, core, output);
else if (args is ["guard", var compiler, var guardCore, var input])
{
    VectorValueInputs.Generate("all", guardCore, input);
    VectorValueGuards.Run(compiler, guardCore, input, true, true);
}
else if (args is ["context-spec", var specCompiler, var specCore, var specInput, var variable, var kind]
    && variable is "var" or "mvar" && kind is "typed" or "hash")
{
    VectorValueInputs.Generate((kind == "hash" ? "hash-" : "") + "context-spec-" + variable, specCore, specInput);
    VectorValueGuards.Run(specCompiler, specCore, specInput, true, false, true);
}
else if (args is ["context", var contextCompiler, var contextCore, var contextInput])
{
    VectorValueInputs.Generate("mvar-context", contextCore, contextInput);
    VectorValueGuards.Run(contextCompiler, contextCore, contextInput, true, false, true);
}
else if (args is ["reference", var refCompiler, var refCore, var refInput, var expected] && expected is "accept" or "reject")
    VectorValueGuards.Run(refCompiler, refCore, refInput, expected == "accept", false);
else throw new ArgumentException("generate <mode> <corelib> <new-output> | guard <compiler> <corelib> <new-input> | reference <compiler> <corelib> <input> <accept|reject>");
