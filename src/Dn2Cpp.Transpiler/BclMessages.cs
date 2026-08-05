namespace Dn2Cpp;

/// <summary>The SR resource keys whose English text the emitter folds into every program as
/// <c>dn2cpp_bcl_messages</c>, so the C++ runtime's own throw sites raise the message real
/// .NET raises rather than <c>Exception.Message</c>'s type-name fallback.
///
/// <para>The text is resolved at transpile time out of the CoreLib the program was built
/// against (<see cref="Compilation.CoreLibSrText"/>) precisely so nothing looks a resource
/// up at run time: a runtime lookup would go through the table
/// <c>--no-manifest-resources System.Private.CoreLib</c> empties, so the first BCL fault of
/// such a build would die building its own diagnostic.</para>
///
/// <para>The runtime asks by KEY (the <c>DN2CPP_SR_*</c> constants in <c>dn2cpp_core.h</c>),
/// never by index, so drift between the two lists can only lose a message, never answer with
/// the wrong one. <c>gates/build-and-run-doc-claims.sh</c> diffs them.</para></summary>
internal static class BclMessages
{
    /// <summary>Resource keys, in emission order. An unresolvable key (a CoreLib carrying
    /// no embedded resources) is emitted with a null text and read as "no message".</summary>
    internal static readonly string[] Keys =
    [
        // Parameterless defaults — what `new X()` gives in real .NET, and what a runtime
        // trap of the same type must therefore give.
        "Arg_OverflowException",
        "Arg_IndexOutOfRangeException",
        "Arg_ArgumentException",
        "Arg_ArgumentOutOfRangeException",
        "ArgumentNull_Generic",
        "Arg_InvalidOperationException",
        "ObjectDisposed_Generic",
        "Arg_ArithmeticException",
        "Arg_OutOfMemoryException",
        "Arg_InvalidCastException",
        "Arg_TypeLoadException",
        "Arg_NotSupportedException",
        "Arg_PlatformNotSupported",
        "Arg_FormatException",
        "Arg_IOException",
        "IO_FileNotFound",
        "Arg_UnauthorizedAccessException",
        "Arg_KeyNotFound",
        "Arg_AmbiguousMatchException_NoMessage",
        "Arg_MissingMethodException",
        "Arg_NullReferenceException",
        "Arg_DivideByZero",
        "Arg_SynchronizationLockException",
        // Composite formats a runtime throw site fills from what it already holds.
        "Format_InvalidStringWithValue",
        "Format_BadDateTime",
        "Format_BadDateOnly",
        "Format_BadTimeOnly",
        "Format_BadTimeSpan",
        "Format_NeedSingleChar",
        "Arg_EnumValueNotFound",
        "IO_PathTooLong_Path",
        "Argument_AddingDuplicateWithKey",
        "Arg_KeyNotFoundWithKey",
        "ArgumentOutOfRange_Generic_MustBeNonNegative",
        "ArgumentOutOfRange_StartIndexLargerThanLength",
        "ArgumentOutOfRange_IndexLength",
        // The two suffixes ArgumentException.Message / ArgumentOutOfRangeException.Message
        // append; a runtime-raised one has no managed _paramName/_actualValue field to
        // dispatch through, so the site that knows them bakes them into the stored text.
        "Arg_ParamName_Name",
        "ArgumentOutOfRange_ActualValue",
    ];
}
