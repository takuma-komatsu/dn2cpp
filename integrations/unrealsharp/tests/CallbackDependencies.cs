namespace UnrealSharp.Core
{
    internal static class LogUnrealSharpCore
    {
        public static string? LastError;
        public static void LogError(string message) => LastError = message;
    }

    internal static class UnrealSharpObject
    {
        public static nint Create(Type type, nint nativeObject) => throw new NotSupportedException();
    }
}

namespace UnrealSharp.Core.Marshallers
{
    internal static class StringMarshaller
    {
        public static void ToNative(nint buffer, int index, string value) => throw new NotSupportedException();
    }
}
