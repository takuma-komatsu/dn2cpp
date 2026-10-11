using System;
using System.Globalization;
using System.Reflection;

namespace HeaderlessReferenceIdentitySubset;

internal static class Program
{
    private sealed class ThrowProvider : IFormatProvider
    {
        public object GetFormat(Type formatType) => formatType == typeof(NumberFormatInfo)
            ? throw new InvalidOperationException("numeric provider must be ignored") : null;
    }
    private static ICloneable s_clone;
    private static IFormatProvider s_provider;

    private static object CloneObject(ICloneable value) => value;
    private static object AttributeObject(ICustomAttributeProvider value) => value;
    private static IFormatProvider Provider(IFormatProvider value) => value;

    internal static void RunProviders()
    {
        var numbers = NumberFormatInfo.CurrentInfo;
        IFormatProvider current = Provider(numbers);
        Console.WriteLine("current numbers identity=" + (numbers.GetType() == typeof(NumberFormatInfo)) + ":"
            + (current is NumberFormatInfo) + ":" + (current.GetType() == typeof(NumberFormatInfo)) + ":"
            + ReferenceEquals(current, numbers) + ":" + ReferenceEquals(current, CultureInfo.CurrentCulture.NumberFormat));
        var dates = DateTimeFormatInfo.InvariantInfo;
        s_provider = Provider(dates);
        object erased = s_provider;
        Console.WriteLine("date provider identity=" + (erased is DateTimeFormatInfo) + ":"
            + (erased.GetType() == typeof(DateTimeFormatInfo)) + ":" + ReferenceEquals(erased, dates) + ":"
            + (erased is IFormatProvider) + ":" + !(erased is NumberFormatInfo));
        Console.WriteLine("date provider relation=" + typeof(IFormatProvider).IsAssignableFrom(typeof(DateTimeFormatInfo)));
        Console.WriteLine("date numeric fallback=" + ReferenceEquals(NumberFormatInfo.GetInstance(s_provider), NumberFormatInfo.CurrentInfo));
        var culture = new CultureInfo("en-US");
        culture.NumberFormat.NumberDecimalSeparator = "~";
        IFormatProvider format = Provider(culture);
        Console.WriteLine("culture provider payload=" + 1.5.ToString("F1", format) + ":"
            + ReferenceEquals(format, culture));
        IFormatProvider numeric = Provider(culture.NumberFormat);
        Console.WriteLine("number provider payload=" + 1.5.ToString("F1", numeric) + ":"
            + ReferenceEquals(numeric, culture.NumberFormat) + ":" + (numeric is NumberFormatInfo));
    }

    private static T Identity<T>(T value) => value;
    private static CultureInfo CultureResult() => CultureInfo.InvariantCulture;
    private static DateTimeFormatInfo DateResult() => DateTimeFormatInfo.InvariantInfo;

    internal static void RunBoundaries()
    {
        IFormatProvider date = DateResult();
        Console.WriteLine("provider shared identity=" + ReferenceEquals(Identity(date), date));
        Func<CultureInfo> cultureFactory = CultureResult;
        Func<IFormatProvider> providerFactory = cultureFactory;
        Console.WriteLine("provider variant delegate=" + ReferenceEquals(providerFactory(), CultureInfo.InvariantCulture));
        var method = typeof(Program).GetMethod(nameof(Provider), BindingFlags.Static | BindingFlags.NonPublic);
        Console.WriteLine("provider Invoke date=" + ReferenceEquals(method.Invoke(null, new object[] { date }), date));
        var forward = (Func<IFormatProvider, IFormatProvider>)method.CreateDelegate(typeof(Func<IFormatProvider, IFormatProvider>));
        Console.WriteLine("provider delegate date=" + ReferenceEquals(forward(date), date));
        try
        {
            method.Invoke(null, new object[] { new object() });
            Console.WriteLine("provider Invoke invalid=accepted");
        }
        catch (ArgumentException) { Console.WriteLine("provider Invoke invalid=ArgumentException"); }
        Console.WriteLine("provider GetFormat numbers=" + ReferenceEquals(CultureInfo.InvariantCulture.GetFormat(typeof(NumberFormatInfo)), CultureInfo.InvariantCulture.NumberFormat));
        Console.WriteLine("provider GetFormat date=" + ReferenceEquals(date.GetFormat(typeof(DateTimeFormatInfo)), date));
        Console.WriteLine("provider GetFormat other=" + (date.GetFormat(typeof(NumberFormatInfo)) is null));
        Console.WriteLine("provider GetInstance direct=" + (NumberFormatInfo.GetInstance(date).GetType() == typeof(NumberFormatInfo)));
        IFormatProvider ignored = new ThrowProvider();
        Console.WriteLine("provider ignored text=" + ((IFormattable)'x').ToString(null, ignored));
        Console.WriteLine("provider ignored datetime=" + ((IFormattable)new DateTime(2020, 1, 2)).ToString("yyyy-MM-dd", ignored));
    }

    internal static void RunInterfaces()
    {
        var culture = CultureInfo.InvariantCulture;
        s_clone = culture;
        object cultureObject = CloneObject(s_clone);
        Console.WriteLine("culture cloneable identity=" + (cultureObject is CultureInfo) + ":"
            + (cultureObject.GetType() == typeof(CultureInfo)) + ":" + ReferenceEquals(cultureObject, culture));
        var numbers = NumberFormatInfo.InvariantInfo;
        object numberObject = CloneObject(numbers);
        Console.WriteLine("number cloneable identity=" + (numberObject is NumberFormatInfo) + ":"
            + (numberObject.GetType() == typeof(NumberFormatInfo)) + ":" + ReferenceEquals(numberObject, numbers));
        var text = culture.TextInfo;
        object textObject = CloneObject(text);
        Console.WriteLine("text cloneable identity=" + (textObject is TextInfo) + ":"
            + (textObject.GetType() == typeof(TextInfo)) + ":" + ReferenceEquals(textObject, text));
        var assembly = typeof(Program).Assembly;
        object assemblyObject = AttributeObject(assembly);
        Console.WriteLine("assembly attribute identity=" + (assemblyObject is Assembly) + ":"
            + (assemblyObject.GetType() == assembly.GetType()) + ":" + ReferenceEquals(assemblyObject, assembly));
        var module = typeof(Program).Module;
        object moduleObject = AttributeObject(module);
        Console.WriteLine("module attribute identity=" + (moduleObject is Module) + ":"
            + (moduleObject.GetType() == module.GetType()) + ":" + ReferenceEquals(moduleObject, module));
        Console.WriteLine("headerless null conversions=" + (CloneObject(null) is null) + ":"
            + (AttributeObject(null) is null) + ":" + (Provider(null) is null));
    }

    internal static void Run()
    {
        Console.WriteLine("== headerless managed reference identity ==");
        RunProviders();
        RunInterfaces();
        RunBoundaries();
        Console.WriteLine("headerless managed reference identity end");
    }
}
