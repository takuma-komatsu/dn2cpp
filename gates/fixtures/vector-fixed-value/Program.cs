using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length == 0 || !int.TryParse(args[0], out int width) || width is not (64 or 128 or 256 or 512))
    throw new ArgumentException("First argument must be fixed width 64, 128, 256, or 512");
FixedVectorValueInputs.Width = width;
args = args[1..];
if (args.Length == 6 && args[0] == "context-spec") args = [.. args, "direct"];
if (args is ["generate", var mode, var core, var output])
    FixedVectorValueInputs.Generate(mode, core, output);
else if (args is ["guard", var compiler, var guardCore, var input])
{
    FixedVectorValueInputs.Generate("all", guardCore, input);
    FixedVectorValueGuards.Run(compiler, guardCore, input, true, true);
}
else if (args is ["context-spec", var specCompiler, var specCore, var specInput, var variable, var kind, var specRoute]
    && specRoute is "direct" or "boxed" && variable is "var" or "mvar" && kind is "typed" or "hash")
{
    if (specRoute == "boxed" && kind == "hash") throw new ArgumentException("IEquatable has no hash method");
    FixedVectorValueInputs.Generate((specRoute == "boxed" ? "boxed-" : kind == "hash" ? "hash-" : "") + "context-spec-" + variable, specCore, specInput);
    FixedVectorValueGuards.Run(specCompiler, specCore, specInput, true, false, true);
}
else if (args is ["context", var contextCompiler, var contextCore, var contextInput, var route] && route is "direct" or "boxed")
{
    FixedVectorValueInputs.Generate(route == "boxed" ? "boxed-mvar-context" : "mvar-context", contextCore, contextInput);
    FixedVectorValueGuards.Run(contextCompiler, contextCore, contextInput, true, false, true);
}
else if (args is ["reference", var refCompiler, var refCore, var refInput, var expected] && expected is "accept" or "reject" or "managed")
    FixedVectorValueGuards.Run(refCompiler, refCore, refInput, expected == "accept", false, managedScope: expected == "managed");
else if (args is ["mixed-generate", var mixedCore, var mixedOutput])
    FixedVectorMixed.Generate(mixedCore, mixedOutput);
else if (args is ["mixed", var mixedCompiler, var mixedReadCore, var mixedInput, var mixedMode] && mixedMode is "observe" or "require")
    FixedVectorMixed.Inspect(mixedCompiler, mixedReadCore, mixedInput, mixedMode == "require");
else if (args is ["core-mutate", var coreMode, var originalCore, var changedCore])
    FixedVectorCoreMutations.Generate(coreMode, originalCore, changedCore);
else throw new ArgumentException("generate <mode> <corelib> <new-output> | guard <compiler> <corelib> <new-input> | reference <compiler> <corelib> <input> <accept|reject>");
