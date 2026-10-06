using System;
using System.Globalization;
using System.Reflection;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== enumerated formal parameter ==");
        int count = 0;
        foreach (MethodInfo method in typeof(Subject).GetMethods())
        {
            if (method.Name != "Value")
                continue;
            count++;
            Type parameter = method.GetGenericArguments()[0];
            Console.WriteLine("nested=" + parameter.IsNested + " value=" + parameter.IsValueType
                + " class=" + parameter.IsClass + " base=" + parameter.BaseType?.FullName);
        }
        Console.WriteLine("count=" + count);
        Console.WriteLine("enumerated formal parameter end");
    }
}

internal static class Subject
{
    public static U Value<U>(U value) where U : struct => value;
}
