using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private readonly Dictionary<MethodInfo, string> _obfuscationBodyFiles = new();

    private void ValidateObfuscationTarget(MethodInfo method)
    {
        string? reason = method.HasMethodAttribute("System.Runtime.CompilerServices.AsyncStateMachineAttribute")
            || method.HasMethodAttribute("System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute")
            ? "async methods are not supported"
            : method.HasMethodAttribute("System.Runtime.CompilerServices.IteratorStateMachineAttribute")
            ? "iterator methods are not supported"
            : method.PInvoke is not null ? "P/Invoke has no managed implementation body"
            : method.IsUnmanagedCallersOnly ? "unmanaged entry points do not use the supported C++ symbol ABI"
            : method.IsAbstract ? "abstract methods have no implementation body"
            : method.IsSynthetic || HasReplacedObfuscationBody(method)
                || CoreIntrinsics.TryFindCutRow(method, out _)
                || _c.IntrinsicFtnTargets.Contains(method) || _c.InterceptFtnTargets.Contains(method)
                || _backend.ShouldSkipMethodBody(method.DeclaringClass, method)
                || CoreIntrinsics.IsIntrinsicType(method.DeclaringClass.FullName)
            ? "the implementation is replaced by an intrinsic or backend"
            : method.Rva == 0 ? "the method has no IL implementation body" : null;
        if (reason is not null)
            throw new NotSupportedException($"[Obfuscate] {method.DeclaringClass.FullName}::{method.Name}: {reason}.");
    }

    private bool HasReplacedObfuscationBody(MethodInfo method)
    {
        string owner = method.DeclaringClass.FullName;
        if (_c.IsBoundedMethod(owner, method.Name))
            return true;
        foreach (var row in CoreIntrinsics.BoundedIntercepts)
            if (row.Matches(owner, method.Name))
                return true;
        return false;
    }

    private void ValidateObfuscationTargets()
    {
        if (!_c.ObfuscationEnabled)
            return;
        foreach (var method in _c.ObfuscationMethods.Union(_c.Reachable).ToArray())
            if (method.HasObfuscateAttribute)
                ValidateObfuscationTarget(method);
    }

    private string BuildObfuscationTargets()
    {
        ValidateObfuscationTargets();
        var rows = new List<(string Managed, string Symbol, string File)>();
        foreach (var method in _c.ObfuscationMethods.Union(_c.Reachable).ToArray())
        {
            if (!method.HasObfuscateAttribute || Compilation.IsCanonicalMethod(method))
                continue;
            var implementation = method.SharedImpl ?? method;
            if (!_obfuscationBodyFiles.TryGetValue(implementation, out string? file))
                throw new NotSupportedException($"[Obfuscate] {method.DeclaringClass.FullName}::{method.Name}: no emitted implementation body.");
            if (!implementation.ObfuscationHasIlBody)
                throw new NotSupportedException($"[Obfuscate] {method.DeclaringClass.FullName}::{method.Name}: the emitted body replaces the managed IL implementation.");
            if (!implementation.IsObfuscationTarget)
                throw new InvalidOperationException("An obfuscation implementation was not protected from inlining.");
            string genericArguments = method.Context.MethodArgs.Length == 0 ? ""
                : "<" + string.Join(",", method.Context.MethodArgs.Select(t => t.ToString())) + ">";
            rows.Add((method.Module.AssemblyName + "!" + method.DeclaringClass.FullName + "::"
                + method.Name + genericArguments + method.SigShape,
                implementation.CppName, file));
        }
        if (rows.Count == 0)
            throw new NotSupportedException("--obfuscate requires at least one reachable [Dn2Cpp.Runtime.Obfuscate] implementation.");
        rows.Sort((a, b) =>
        {
            int order = string.CompareOrdinal(a.Symbol, b.Symbol);
            return order != 0 ? order : string.CompareOrdinal(a.Managed, b.Managed);
        });
        var json = new StringBuilder("{\n  \"version\": 1,\n  \"targets\": [\n");
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            string pattern = "^_Z" + row.Symbol.Length + row.Symbol + ".*$";
            json.Append("    {\"managedMethod\": ").Append(ObfuscationJsonString(row.Managed))
                .Append(", \"implementationSymbol\": ").Append(ObfuscationJsonString(row.Symbol))
                .Append(", \"cppFile\": ").Append(ObfuscationJsonString(row.File))
                .Append(", \"symbolPattern\": ").Append(ObfuscationJsonString(pattern))
                .Append(i + 1 == rows.Count ? "}\n" : "},\n");
        }
        return json.Append("  ]\n}\n").ToString();
    }

    private static string ObfuscationJsonString(string value)
    {
        var text = new StringBuilder("\"");
        foreach (char c in value)
        {
            if (c == '"' || c == '\\')
                text.Append('\\').Append(c);
            else if (c < ' ')
                text.Append("\\u").Append(((int)c).ToString("x4"));
            else
                text.Append(c);
        }
        return text.Append('"').ToString();
    }
}
