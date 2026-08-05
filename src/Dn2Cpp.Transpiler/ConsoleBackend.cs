using System.Text;

namespace Dn2Cpp;

/// <summary>Default backend: emits a native console <c>main</c> that runs the
/// assembly's managed entry point. Pure .NET — no target dependencies.</summary>
internal sealed class ConsoleBackend : IEmitBackend
{
    public string RuntimeHeader => "dn2cpp.h";

    public ICallIntrinsics? CallIntrinsics => null;

    public bool ShouldSkipMethodBody(ClassInfo cls, MethodInfo m) => false;

    public void EmitEpilogue(CppEmitter emitter, StringBuilder sb, IReadOnlyList<MethodInfo> cctors)
    {
        var ep = emitter.EntryPoint
            ?? throw new NotSupportedException("Input assembly has no managed entry point (must be an exe)");
        bool returnsInt = !ep.Signature.ReturnType.IsVoid;

        // A `static Main(string[] args)` entry point gets `int main(int argc, char** argv)`:
        // build the managed args array from argv[1..] (argv[0] is the program name, which
        // .NET's args excludes) tagged with the precise ti_arr_string handle (noted in
        // CppEmitter.Emit before EmitTypeInfos), and pass it to the managed entry. A
        // parameterless entry keeps the plain `int main()`; any other shape is rejected.
        var ps = ep.Signature.ParameterTypes;
        bool takesArgs = ps.Length == 1
            && ps[0] is { Kind: TypeKind.SZArray, Element: { } el } && el.IsString;
        if (ps.Length != 0 && !takesArgs)
            throw new NotSupportedException(
                "Only a parameterless entry point or 'static Main(string[] args)' is supported");

        string callArgs = takesArgs
            ? $"dn2cpp_argv_to_string_array(argc, argv, &ti_arr_{Compilation.ArrayElemMangle(ps[0].Element!)})"
            : "";

        // Every exit goes through dn2cpp_main_exit rather than a plain `return`: in an
        // executable that terminates the process without running static destructors,
        // which the never-stopping finalizer thread and pool workers would otherwise
        // lock after destruction. An unhandled exception leaves through
        // dn2cpp_main_abort, because real .NET dies of SIGABRT there rather than
        // exit(1). Both return only in a shared library, whose host owns the process;
        // the `return`s below are that path and are unreachable in an executable.
        // DN2CPP_RT_EXPORT: a DN2CPP_SHARED host dlsym's "main", and the library
        // compiles with -fvisibility=hidden, which would otherwise hide the symbol.
        sb.AppendLine(takesArgs
            ? "DN2CPP_RT_EXPORT int main(int argc, char** argv)"
            : "DN2CPP_RT_EXPORT int main()");
        sb.AppendLine("{");
        emitter.EmitInitCalls(sb, cctors);
        sb.AppendLine("    try {");
        sb.AppendLine(returnsInt
            ? $"        int32_t __rc = {ep.CppName}({callArgs});\n        dn2cpp_main_exit(__rc);\n        return __rc;"
            : $"        {ep.CppName}({callArgs});\n        dn2cpp_main_exit(0);\n        return 0;");
        sb.AppendLine("    } catch (Dn2CppException& __ex) {");
        sb.AppendLine("        dn2cpp_report_unhandled_exception(__ex.obj);");
        sb.AppendLine("        dn2cpp_main_abort();");
        sb.AppendLine("        return 1;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }
}
