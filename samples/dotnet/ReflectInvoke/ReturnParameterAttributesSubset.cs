#nullable disable
using System;
using System.Reflection;

namespace ReflectInvoke;

static class ReturnParameterAttributesSubset
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    sealed class ReturnDataAttribute : Attribute
    {
        public ReturnDataAttribute(string text) { }
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.ReturnValue)]
    sealed class ReturnNoteAttribute : Attribute
    {
        public readonly string Text;
        public string Name { get; set; }
        public int Number;
        public ReturnNoteAttribute(string text) => Text = text;
    }

    [AttributeUsage(AttributeTargets.ReturnValue)]
    sealed class ReturnKeptAttribute : Attribute
    {
        public readonly string Text;
        public ReturnKeptAttribute(string text) => Text = text;
    }

    static class NativeReturnRows
    {
        [return: ReturnData("data", Name = "metadata"), ReturnKept("retained")]
        public static int Data() => throw new InvalidOperationException("metadata-only body");

        [return: ReturnNote("native", Name = "named", Number = 7)]
        public static int Instance() => throw new InvalidOperationException("metadata-only body");

        public static int Argument([ReturnData("parameter", Name = "metadata")] int value)
            => throw new InvalidOperationException("metadata-only body");
    }

    sealed class PackedReturnRows<T>
    {
        [return: ReturnData("generic", Name = "definition")]
        public static T Data() => throw new InvalidOperationException("metadata-only body");

        [return: ReturnNote("packed", Name = "generic", Number = 9)]
        public static U Instance<U>() => throw new InvalidOperationException("metadata-only body");
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    sealed class LateArgumentAttribute : Attribute { public int Number { get; set; } }

    [AttributeUsage(AttributeTargets.ReturnValue)]
    sealed class LateReturnAttribute : Attribute { public int Number { get; set; } }

    static class LateExecutedRows
    {
        [return: LateReturn(Number = 3)]
        public static int Execute([LateArgument(Number = 4)] int value) => value;
    }

    internal static class Program
    {
        // The owner becomes executable after its parameter metadata was visited.
        static int Late1() => Late2();
        static int Late2() => LateExecutedRows.Execute(17);

        internal static void Run()
        {
            _ = new ReturnKeptAttribute("direct");
            Console.WriteLine("== return parameter attributes ==");
            ParameterInfo nativeData = typeof(NativeReturnRows).GetMethod("Data").ReturnParameter;
            Console.WriteLine("return native data=" + nativeData.IsDefined(typeof(ReturnDataAttribute), false)
                + "/" + nativeData.GetCustomAttributesData().Count);
            foreach (CustomAttributeData data in nativeData.GetCustomAttributesData())
                Console.WriteLine("return native row=" + data);

            ParameterInfo argument = typeof(NativeReturnRows).GetMethod("Argument").GetParameters()[0];
            Console.WriteLine("ordinary parameter data=" + argument.IsDefined(typeof(ReturnDataAttribute), false)
                + "/" + argument.GetCustomAttributesData().Count);

            Type packed = typeof(PackedReturnRows<>);
            ParameterInfo packedData = packed.GetMethod("Data").ReturnParameter;
            Console.WriteLine("return packed data=" + packedData.IsDefined(typeof(ReturnDataAttribute), false)
                + "/" + packedData.GetCustomAttributesData().Count + "/" + packedData.ParameterType.Name);
            foreach (CustomAttributeData data in packedData.GetCustomAttributesData())
                Console.WriteLine("return packed row=" + data);

            MethodInfo nativeMethod = typeof(NativeReturnRows).GetMethod("Instance");
            ParameterInfo nativeParameter = nativeMethod.ReturnParameter;
            ReturnNoteAttribute native = nativeParameter.GetCustomAttribute<ReturnNoteAttribute>();
            Console.WriteLine("return native instance=" + (native is null ? "missing"
                : native.Text + "/" + native.Name + "/" + native.Number)
                + "/" + nativeParameter.Position
                + "/" + ReferenceEquals(nativeParameter.Member, nativeMethod));

            ParameterInfo packedParameter = packed.GetMethod("Instance").ReturnParameter;
            ReturnNoteAttribute generic = packedParameter.GetCustomAttribute<ReturnNoteAttribute>();
            Console.WriteLine("return packed instance=" + (generic is null ? "missing"
                : generic.Text + "/" + generic.Name + "/" + generic.Number)
                + "/" + packedParameter.ParameterType.Name + "/" + packedParameter.ParameterType.IsGenericParameter);
            MethodInfo late = typeof(LateExecutedRows).GetMethod("Execute");
            Console.WriteLine("late parameter data=" + late.GetParameters()[0].GetCustomAttributesData().Count
                + "/" + late.ReturnParameter.GetCustomAttributesData().Count);
            Console.WriteLine("late parameter call=" + Late1());
            Console.WriteLine("return parameter attributes end");
        }
    }
}
