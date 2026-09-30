namespace System;

// The guard's blank-string rejection puts the supplied name in Message and fixed
// text in ParamName. This body is the .NET oracle for the name-based lowering.
internal static class ThrowHelper
{
    internal static string IfNullOrWhitespace(string argument, string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
            throw new ArgumentException(paramName, "Argument is whitespace");
        }
        return argument;
    }
}
