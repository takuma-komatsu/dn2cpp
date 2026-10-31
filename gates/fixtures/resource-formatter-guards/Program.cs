using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ResourceFormatterGuards;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Zeroed permitDeserialization rejects before accessing the type table or stream.
        var reader = (ResourceReader)RuntimeHelpers.GetUninitializedObject(typeof(ResourceReader));
        var method = typeof(ResourceReader).GetMethod("DeserializeObject", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(int) }, null) ?? throw new InvalidOperationException("DeserializeObject(int) missing");
        foreach (int index in new[] { -1, 0, int.MaxValue })
        {
            Exception? failure = null;
            try { method.Invoke(reader, new object[] { index }); }
            catch (TargetInvocationException error) { failure = error.InnerException; }
            if (failure is null || failure.GetType() != typeof(NotSupportedException))
                throw new InvalidOperationException("Expected managed deserialization permit refusal", failure);
            if (failure.Message.Contains("primitive", StringComparison.Ordinal)
                || failure.Message.Contains("BinaryFormatter-serialized resource types", StringComparison.Ordinal))
                throw new InvalidOperationException("Formatter boundary reached instead of the managed permit guard", failure);
            Console.WriteLine("resource-formatter-permit:" + index + ":" + failure.GetType().FullName + ":" + failure.Message);
        }
        Console.WriteLine("resource-formatter-permit:end");
    }
}
