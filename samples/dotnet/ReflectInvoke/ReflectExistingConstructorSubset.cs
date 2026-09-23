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

    private sealed class Faulty
    {
        public Faulty() { }

        public Faulty(int value)
        {
            if (value < 0)
                throw new InvalidOperationException("activator failure");
        }

        public int Value
        {
            get => throw new InvalidOperationException("getter failure");
            set => throw new InvalidOperationException("setter failure");
        }

        public int Instance() => 1;
    }

    private sealed class ThrowingDefault
    {
        public ThrowingDefault() => throw new InvalidOperationException("default constructor failure");
    }

    private struct ThrowingValue
    {
        public ThrowingValue() => throw new InvalidOperationException("value constructor failure");
    }

    // Not readonly, so the compiler cannot fold the flags into a single constant.
    private static BindingFlags s_static = BindingFlags.Static;
    private static BindingFlags s_instance = BindingFlags.Instance;

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
        try
        {
            method.Invoke(null, null);
        }
        catch (TargetInvocationException exception)
        {
            Console.WriteLine($"existing-constructor-message: {exception.Message}");
        }
        Show("method-constant-flags", () => method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, null, null));
        Show("method-composed-flags", () => method.Invoke(null, s_static | BindingFlags.Public, null, null, null));
        Show("method-composed-unwrapped", () => method.Invoke(null, s_static | BindingFlags.DoNotWrapExceptions,
            null, null, null));
        Show("constructor-composed-flags", () => ((ConstructorInfo)constructor).Invoke(s_instance | BindingFlags.NonPublic,
            null, new object[] { -1 }, null));

        PropertyInfo property = typeof(Faulty).GetProperty(nameof(Faulty.Value))!;
        var faulty = new Faulty();
        Show("property-get", () => property.GetValue(faulty));
        Show("property-get-flags", () => property.GetValue(faulty, BindingFlags.Default, null, null, null));
        Show("property-get-unwrapped", () => property.GetValue(faulty, BindingFlags.DoNotWrapExceptions, null, null, null));
        Show("property-set", () => property.SetValue(faulty, 1));
        Show("property-set-flags", () => property.SetValue(faulty, 1, BindingFlags.Default, null, null, null));
        Show("property-set-unwrapped", () => property.SetValue(faulty, 1, BindingFlags.DoNotWrapExceptions,
            null, null, null));

        const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
        Show("activator-args", () => Activator.CreateInstance(typeof(Faulty), new object[] { -1 }));
        Show("activator-args-flags", () => Activator.CreateInstance(typeof(Faulty), publicInstance,
            null, new object[] { -1 }, null));
        Show("activator-args-unwrapped", () => Activator.CreateInstance(typeof(Faulty),
            publicInstance | BindingFlags.DoNotWrapExceptions, null, new object[] { -1 }, null));
        Show("activator-default", () => Activator.CreateInstance(typeof(ThrowingDefault)));
        Show("activator-default-flags", () => Activator.CreateInstance(typeof(ThrowingDefault), publicInstance,
            null, null, null));
        Show("activator-default-unwrapped", () => Activator.CreateInstance(typeof(ThrowingDefault),
            publicInstance | BindingFlags.DoNotWrapExceptions, null, null, null));

        MethodInfo instance = typeof(Faulty).GetMethod(nameof(Faulty.Instance))!;
        try
        {
            instance.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"existing-constructor-null-receiver: {exception is TargetInvocationException}");
        }

        Show("create-instance-generic", () => Activator.CreateInstance<ThrowingDefault>());
        Show("new-constraint", () => Construct<ThrowingDefault>());
        Show("lazy", () => _ = new Lazy<ThrowingDefault>().Value);
        Show("create-instance-value", () => Activator.CreateInstance<ThrowingValue>());
        Show("new-constraint-value", () => Construct<ThrowingValue>());
        Console.WriteLine("existing-constructor-end");
    }

    private static T Construct<T>() where T : new() => new T();

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
