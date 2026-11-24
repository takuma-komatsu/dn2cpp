using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Dn2Cpp;

/// <summary>Resolves metadata references before any transpiler model is constructed.</summary>
internal sealed class AssemblyLoadSet
{
    internal readonly List<string> Paths = new();
    internal string? CoreLibPath { get; private set; }
    internal HashSet<int> TargetFrameworkIndexes { get; private set; } = new();
    internal readonly Dictionary<string, Compilation.DefaultRefOutcome> DefaultRefStatus =
        new(StringComparer.OrdinalIgnoreCase);

    private sealed class AssemblyReferences
    {
        internal string Name = "";
        internal bool HasDefaultPrincipalAccessor;
        internal readonly List<string> References = new();
    }

    private readonly List<AssemblyReferences> _assemblies = new();

    private AssemblyReferences Add(string path)
    {
        using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
        var reader = pe.GetMetadataReader();
        var assembly = new AssemblyReferences
        {
            Name = reader.GetString(reader.GetAssemblyDefinition().Name),
            HasDefaultPrincipalAccessor = DefaultPrincipalContract.HasAccessor(reader),
        };
        foreach (var handle in reader.AssemblyReferences)
            assembly.References.Add(reader.GetString(reader.GetAssemblyReference(handle).Name));
        if (assembly.HasDefaultPrincipalAccessor)
            assembly.References.Add(DefaultPrincipalContract.TargetAssembly);
        Paths.Add(path);
        _assemblies.Add(assembly);
        if (assembly.Name == "System.Private.CoreLib")
            CoreLibPath ??= path;
        return assembly;
    }

    internal static AssemblyLoadSet Resolve(IReadOnlyList<string> paths, TranspileOptions options,
        bool allowHostCoreLib = false)
    {
        var result = new AssemblyLoadSet();
        foreach (string path in paths)
            result.Add(path);
        string? frameworkDirectory = null;
        if (options.AutoRef)
        {
            if (allowHostCoreLib && result.CoreLibPath is null)
            {
                // Assembly.Location is empty in the native CLI. Packaged
                // toolchains still supply their target CoreLib explicitly.
                string hostCoreLib = typeof(object).Assembly.Location;
                if (!string.IsNullOrEmpty(hostCoreLib) && File.Exists(hostCoreLib))
                    result.Add(hostCoreLib);
            }
            if (result.CoreLibPath is { } coreLib)
                frameworkDirectory = Path.GetDirectoryName(Path.GetFullPath(coreLib));
        }
        if (frameworkDirectory is not null)
            result.LoadReferenceClosure(frameworkDirectory);
        if (options.DefaultRefDir is { } dir)
        {
            bool injected = result.InjectDefaultRefs(dir, options.NoDefaultRefs ?? Array.Empty<string>());
            if (injected && frameworkDirectory is not null)
                result.LoadReferenceClosure(frameworkDirectory);
        }
        if (options.StrippingLevel.UsesTypeWideRetention())
            result.TargetFrameworkIndexes = TargetFrameworkAssets.Classify(result.Paths);
        return result;
    }

    /// <summary>Only implementation assemblies beside the original CoreLib can join
    /// the closure; explicit references keep their existing module positions.</summary>
    private void LoadReferenceClosure(string frameworkDirectory)
    {
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in _assemblies)
            loaded.Add(assembly.Name);
        var queue = new Queue<AssemblyReferences>(_assemblies);
        while (queue.Count != 0)
        {
            var assembly = queue.Dequeue();
            foreach (string name in assembly.References)
            {
                if (!loaded.Add(name))
                    continue;
                string path = Path.Combine(frameworkDirectory, name + ".dll");
                if (!File.Exists(path))
                    continue;
                if (name == DefaultPrincipalContract.TargetAssembly)
                {
                    using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
                    var reader = pe.GetMetadataReader();
                    if (!reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, name))
                        throw new NotSupportedException("default principal dependency has an unexpected assembly identity");
                }
                var added = Add(path);
                loaded.Add(added.Name);
                queue.Enqueue(added);
            }
        }
    }

    private bool InjectDefaultRefs(string directory, IReadOnlyList<string> suppressed)
    {
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in _assemblies)
            loaded.Add(assembly.Name);
        var off = new HashSet<string>(suppressed, StringComparer.OrdinalIgnoreCase);
        bool any = false;
        foreach (var (shim, trigger) in Compilation.DefaultReferences)
        {
            Compilation.DefaultRefOutcome outcome;
            if (!loaded.Contains(trigger))
                outcome = Compilation.DefaultRefOutcome.TriggerAbsent;
            else if (off.Contains(shim))
                outcome = Compilation.DefaultRefOutcome.Suppressed;
            else if (loaded.Contains(shim))
                outcome = Compilation.DefaultRefOutcome.AlreadyLoaded;
            else
            {
                string path = Path.Combine(directory, shim + ".dll");
                if (!File.Exists(path))
                    outcome = Compilation.DefaultRefOutcome.NotFound;
                else
                {
                    var assembly = Add(path);
                    if (!string.Equals(assembly.Name, shim, StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException(
                            $"default reference {path}: assembly name is '{assembly.Name}', expected '{shim}'");
                    loaded.Add(assembly.Name);
                    outcome = Compilation.DefaultRefOutcome.Injected;
                    any = true;
                }
            }
            DefaultRefStatus[shim] = outcome;
        }
        return any;
    }
}
