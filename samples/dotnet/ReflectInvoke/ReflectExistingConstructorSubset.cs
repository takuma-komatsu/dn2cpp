using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectExistingConstructorSubset;

internal static class Program
{
    private sealed class Wrapper
    {
        public nint NativeObject;
        public nint Observed;
        private Wrapper(int value)
        {
            Observed = NativeObject + value;
            if (value < 0)
                throw new InvalidOperationException("constructor failure");
        }

        public static void Throw() => throw new InvalidOperationException("method failure");
    }

    internal static void Run()
    {
        Console.WriteLine("existing-constructor-begin");
        var receiver = (Wrapper)RuntimeHelpers.GetUninitializedObject(typeof(Wrapper));
        receiver.NativeObject = 100;
        MethodBase constructor = typeof(Wrapper).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(int) }, null)!;
        object? result = constructor.Invoke(receiver, new object[] { 23 });
        Console.WriteLine($"existing-constructor: {result is null} {receiver.NativeObject} {receiver.Observed}");
        try
        {
            constructor.Invoke(receiver, new object[] { -1 });
        }
        catch (TargetInvocationException exception)
        {
            Console.WriteLine($"existing-constructor-error: {exception.InnerException!.GetType().Name} {receiver.Observed}");
            Console.WriteLine($"existing-constructor-base: {(Exception)exception is ApplicationException} {exception.GetType().BaseType!.Name}");
        }
        Show("fresh-constructor", () => ((ConstructorInfo)constructor).Invoke(new object[] { -1 }));
        MethodInfo method = typeof(Wrapper).GetMethod(nameof(Wrapper.Throw))!;
        Show("method", () => method.Invoke(null, null));
        Show("method-unwrapped", () => method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, null, null));
        Show("constructor-unwrapped", () => ((ConstructorInfo)constructor).Invoke(BindingFlags.DoNotWrapExceptions,
            null, new object[] { -1 }, null));
        Show("receiver-unwrapped", () => constructor.Invoke(receiver, BindingFlags.DoNotWrapExceptions,
            null, new object[] { -1 }, null));
        Action action = (Action)method.CreateDelegate(typeof(Action));
        Show("delegate", action);
        Console.WriteLine("existing-constructor-end");
    }

    private static void Show(string name, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"existing-constructor-{name}: {exception.GetType().Name} {exception.InnerException?.GetType().Name} {exception.HResult:X8}");
        }
    }
}
