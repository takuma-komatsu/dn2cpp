using System.Globalization;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length >= 2 && args[0] == "fixed")
{
    if (!int.TryParse(args[1], out int width) || width is not (64 or 128 or 256 or 512))
        throw new ArgumentException("Fixed width must be 64, 128, 256 or 512");
    VectorUnsupportedFamily.Width = width;
    args = args[2..];
    if (args is ["generate", var fixedMode, _, _] && !fixedMode.StartsWith("count-") && !fixedMode.StartsWith("helper-"))
        throw new ArgumentException("Fixed mode supports only Count and helper fixtures");
}
if (args is ["generate", var mode, var core, var output])
{
    if (mode.StartsWith("count-") || mode.StartsWith("helper-")) VectorUnsupportedInputs.Generate(mode, core, output);
    else VectorUnsupportedValueInputs.Generate(mode, core, output);
}
else if (args is ["reference", var compiler, var referenceCore, var input, var expected] && expected is "accept" or "reject" or "scope")
    VectorUnsupportedGuards.Run(compiler, referenceCore, input, expected);
else if (args is ["declaration-copy", var copyMode, var copyCore, var copyOutput]) VectorUnsupportedDeclarations.Copy(copyMode, copyCore, copyOutput);
else if (args is ["declarations", var declarationCore]) VectorUnsupportedDeclarations.Verify(declarationCore);
else throw new ArgumentException("generate <mode> <corelib> <new-output> | reference <compiler> <corelib> <input> <accept|reject|scope> | declarations <corelib> | declaration-copy <mode> <corelib> <new-corelib>");
