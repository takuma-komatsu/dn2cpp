using System;

namespace Dn2Cpp.Runtime;

/// <summary>
/// Marks a static method as the managed implementation of a native P/Invoke entry
/// point. When the transpiler loads an assembly containing a method carrying this
/// attribute, every call to a bodyless <c>[DllImport(moduleName)]</c> method whose
/// entry point matches <paramref name="entryPoint"/> (or the method's own name when
/// unspecified) is lowered to a direct call to the attributed method instead of an
/// <c>extern "C"</c> native call — the entry point's native library is then never
/// referenced or linked. This lets a referenced assembly swap a BCL native backend
/// for a transpilable C# one (e.g. DnZlib replacing the vendored zlib behind
/// System.IO.Compression) just by being referenced.
///
/// The implementation method must be static, non-generic (on a non-generic type),
/// have an IL body, and its signature must lower to the same blittable native shape
/// as the P/Invoke it replaces (same parameter count; pointer-width and exact-width
/// integer/float types position-for-position — internal BCL pointer types are spelled
/// as <c>void*</c>/struct pointers on the implementation side and cast at the call site).
///
/// The transpiler matches this attribute by full name only
/// (<c>Dn2Cpp.Runtime.NativeImplementationAttribute</c>), so an assembly that must not
/// reference Dn2Cpp.Runtime can declare its own internal copy of this type instead
/// (the same convention as <c>System.Runtime.CompilerServices.IsExternalInit</c>).
/// Under a normal .NET runtime the attribute is inert.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class NativeImplementationAttribute : Attribute
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

    /// <summary>The <c>[DllImport]</c> module whose entry point this method implements
    /// (e.g. <c>"libSystem.IO.Compression.Native"</c>).</summary>
    public string ModuleName { get; }

    /// <summary>The entry-point symbol this method implements, or null to use the
    /// attributed method's own name.</summary>
    public string? EntryPoint { get; }
}
