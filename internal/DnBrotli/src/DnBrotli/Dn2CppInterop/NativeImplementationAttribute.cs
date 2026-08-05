namespace Dn2Cpp.Runtime;

/// <summary>
/// Internal copy of the dn2cpp transpiler's <c>Dn2Cpp.Runtime.NativeImplementationAttribute</c>.
/// The transpiler matches the attribute by full name only (the same convention as
/// <c>System.Runtime.CompilerServices.IsExternalInit</c>), so declaring this copy lets DnBrotli
/// mark methods as managed implementations of native P/Invoke entry points (see
/// <see cref="DnBrotli.Dn2CppInterop.BrotliInterop"/>) without referencing dn2cpp.
/// Under a normal .NET runtime the attribute is inert.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class NativeImplementationAttribute : Attribute
{
    /// <summary>The implemented entry point's symbol is the attributed method's own name.</summary>
    public NativeImplementationAttribute(string moduleName)
    {
        ModuleName = moduleName;
    }

    public NativeImplementationAttribute(string moduleName, string entryPoint)
    {
        ModuleName = moduleName;
        EntryPoint = entryPoint;
    }

    /// <summary>The <c>[DllImport]</c> module whose entry point this method implements.</summary>
    public string ModuleName { get; }

    /// <summary>The entry-point symbol this method implements, or <see langword="null"/> to use
    /// the attributed method's own name.</summary>
    public string? EntryPoint { get; }
}
