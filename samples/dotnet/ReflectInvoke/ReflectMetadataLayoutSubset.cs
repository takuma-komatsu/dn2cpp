using System;
using System.Reflection;
using System.Threading;

namespace ReflectMetadataLayoutSubset;

[AttributeUsage(AttributeTargets.Class)]
sealed class LabelAttribute : Attribute
{
    public readonly string Text;
    public LabelAttribute(string text) => Text = text;
}

[Label("native")]
class NativeBase
{
    public int Field;
    public int Value { get; set; }
    public NativeBase(int value) => Field = value;
    public string Read(string text) => text + Field;
}

sealed class PackedDerived : NativeBase
{
    public PackedDerived(int value) : base(value) { }
}

[Label("packed")]
class PackedBase
{
    public int Field;
    public int Value { get; set; }
    public PackedBase(int value) => Field = value;
    public string Read(string text) => text + Field;
}

sealed class NativeDerived : PackedBase
{
    public NativeDerived(int value) : base(value) { }
}

sealed class Generic<T>
{
    public T Value;
    public Generic(T value) => Value = value;
    public T Read(T value) => value;
}

sealed class NativeArrayElement { public int Value; }
sealed class PackedArrayElement { public int Value; }
sealed class GenericToken { public int Value; }

interface IRead
{
    int Read(int value);
}

sealed class AddReader : IRead
{
    int IRead.Read(int value) => value + 1;
}

sealed class MultiplyReader : IRead
{
    int IRead.Read(int value) => value * 3;
}

sealed class InvokeFamily<T>
{
    public int A() => 1;
    public int B() => 2;
    public int C() => 3;
    public int D() => 4;
    public int E() => 5;
    public int F() => 6;
    public int G() => 7;
    public int H() => 8;
}

sealed class InvocationWorker
{
    readonly MethodInfo[] methods;
    readonly object[] receivers;
    public int Total;

    public InvocationWorker(MethodInfo[] methods, object[] receivers)
    {
        this.methods = methods;
        this.receivers = receivers;
    }

    public void Run()
    {
        for (int pass = 0; pass < 4; pass++)
        {
            for (int index = 0; index < methods.Length; index++)
            {
                int row = (pass & 1) == 0 ? index : methods.Length - index - 1;
                Total += (int)methods[row].Invoke(receivers[row], null);
            }
        }
    }
}

static class Program
{
    static Type TypeOf<T>() => typeof(T);

    static void Members(string label, Type type, object instance)
    {
        FieldInfo field = type.GetField("Field");
        PropertyInfo property = type.GetProperty("Value");
        MethodInfo method = type.GetMethod("Read");
        field.SetValue(instance, 11);
        property.SetValue(instance, 13);
        Console.WriteLine(label + "-members=" + field.GetValue(instance) + "/"
            + property.GetValue(instance) + "/" + method.Invoke(instance, new object[] { "v" }));
        Console.WriteLine(label + "-owner=" + method.DeclaringType.Name + "/" + method.ReflectedType.Name);
        Console.WriteLine(label + "-identity=" + ReferenceEquals(method, type.GetMethod("Read"))
            + "/" + ReferenceEquals(property.GetGetMethod(), type.GetMethod("get_Value")));
        Console.WriteLine(label + "-parameter=" + method.GetParameters()[0]);
        Console.WriteLine(label + "-attribute=" + ((LabelAttribute)method.DeclaringType.GetCustomAttributes(typeof(LabelAttribute), false)[0]).Text);
        ConstructorInfo constructor = type.GetConstructor(new[] { typeof(int) });
        Console.WriteLine(label + "-constructor=" + field.GetValue(constructor.Invoke(new object[] { 17 })));
    }

    static void GenericMembers(string label, Type type, object instance, object value)
    {
        FieldInfo field = type.GetField("Value");
        MethodInfo method = type.GetMethod("Read");
        field.SetValue(instance, value);
        Console.WriteLine(label + "-generic=" + field.FieldType.Name + "/" + field.GetValue(instance)
            + "/" + method.Invoke(instance, new[] { value }));
        Console.WriteLine(label + "-generic-parameter=" + (method.GetParameters()[0].ParameterType == field.FieldType));
    }

    static void CacheCapacity()
    {
        object[] subjects = { new InvokeFamily<bool>(), new InvokeFamily<byte>(), new InvokeFamily<sbyte>(),
            new InvokeFamily<short>(), new InvokeFamily<ushort>(), new InvokeFamily<int>(),
            new InvokeFamily<uint>(), new InvokeFamily<long>(), new InvokeFamily<ulong>() };
        MethodInfo[] methods = new MethodInfo[72];
        object[] receivers = new object[methods.Length];
        int count = 0;
        foreach (object subject in subjects)
        {
            foreach (MethodInfo method in subject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                methods[count] = method;
                receivers[count++] = subject;
            }
        }
        int sum = 0;
        for (int pass = 0; pass < 4; pass++)
        {
            for (int index = 0; index < count; index++)
            {
                int row = (pass & 1) == 0 ? index : count - index - 1;
                sum += (int)methods[row].Invoke(receivers[row], null);
            }
        }
        Console.WriteLine("metadata-layout-cache-capacity=" + count + "/" + sum);
        InvocationWorker first = new InvocationWorker(methods, receivers);
        InvocationWorker second = new InvocationWorker(methods, receivers);
        Thread firstThread = new Thread(first.Run);
        Thread secondThread = new Thread(second.Run);
        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();
        Console.WriteLine("metadata-layout-cache-threads=" + first.Total + "/" + second.Total);
    }

    internal static void Run()
    {
        Console.WriteLine("metadata-layout-begin");
        Members("native", typeof(NativeBase), new NativeBase(1));
        Members("packed", new PackedBase(2).GetType(), new PackedBase(2));
        Members("packed-derived", new PackedDerived(3).GetType(), new PackedDerived(3));
        Members("native-derived", typeof(NativeDerived), new NativeDerived(4));
        Console.WriteLine("metadata-layout-name=" + (Type.GetType("ReflectMetadataLayoutSubset.PackedBase") == new PackedBase(0).GetType()));

        GenericMembers("native-value", typeof(Generic<int>), new Generic<int>(0), 19);
        GenericMembers("native-reference", typeof(Generic<string>), new Generic<string>(""), "text");
        GenericMembers("packed-reference", new Generic<object>(null).GetType(), new Generic<object>(null), "object");
        Console.WriteLine("metadata-layout-generic-token=" + TypeOf<GenericToken>().GetField("Value").Name);
        Console.WriteLine("metadata-layout-open=" + typeof(Generic<>).IsGenericTypeDefinition);

        Type nativeArray = typeof(NativeArrayElement[]);
        Type packedArray = new PackedArrayElement[1].GetType();
        Console.WriteLine("metadata-layout-arrays=" + nativeArray.GetArrayRank() + "/" + packedArray.GetArrayRank()
            + "/" + nativeArray.GetElementType().GetField("Value").Name
            + "/" + packedArray.GetElementType().GetField("Value").Name);

        IRead add = new AddReader();
        IRead multiply = new MultiplyReader();
        MethodInfo read = add.GetType().GetInterfaces()[0].GetMethod("Read");
        object[] arguments = { 5 };
        int sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            sum += (int)read.Invoke(add, arguments);
            sum += (int)read.Invoke(multiply, arguments);
        }
        Console.WriteLine("metadata-layout-interface-receivers=" + sum);
        CacheCapacity();
        Console.WriteLine("metadata-layout-end");
    }
}
