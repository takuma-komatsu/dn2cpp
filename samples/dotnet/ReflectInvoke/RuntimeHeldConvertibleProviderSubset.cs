#nullable enable
using System;
using System.Globalization;

namespace ReflectInvoke;

internal static class RuntimeHeldConvertibleProviderSubset
{
    private sealed class NumericProvider : IFormatProvider
    {
        private readonly NumberFormatInfo _numbers;
        public int Calls;
        public NumericProvider(NumberFormatInfo numbers) => _numbers = numbers;
        public object? GetFormat(Type? formatType)
        {
            Calls++;
            return formatType == typeof(NumberFormatInfo) ? _numbers : null;
        }
    }

    internal static void Run()
    {
        var numbers = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        numbers.NegativeSign = "minus";
        var provider = new NumericProvider(numbers);
        IConvertible converted = -12;
#if CONVERTIBLE_PROVIDER_TO_TYPE
        Console.WriteLine("provider convertible to-type=" + converted.ToType(typeof(string), provider) + "/" + provider.Calls);
#elif CONVERTIBLE_PROVIDER_GROUP
        Func<IFormatProvider?, string> formatter = converted.ToString;
        Console.WriteLine("provider convertible group=" + formatter(provider) + "/" + provider.Calls);
#elif CONVERTIBLE_PROVIDER_REFLECTED
        var method = typeof(IConvertible).GetMethod(nameof(IConvertible.ToString))!;
        Console.WriteLine("provider convertible reflected=" + method.Invoke(converted, new object?[] { provider }) + "/" + provider.Calls);
#elif CONVERTIBLE_PROVIDER_REFLECTED_DELEGATE
        var method = typeof(IConvertible).GetMethod(nameof(IConvertible.ToString))!;
        var formatter = method.CreateDelegate<Func<IFormatProvider?, string>>(converted);
        Console.WriteLine("provider convertible reflected-delegate=" + formatter(provider) + "/" + provider.Calls);
#else
        Console.WriteLine("provider convertible only=" + converted.ToString(provider) + "/" + provider.Calls);
#endif
    }
}
