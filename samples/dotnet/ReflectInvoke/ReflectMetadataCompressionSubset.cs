using System;
using System.Reflection;
using Dn2Cpp.Runtime;

namespace ReflectMetadataCompressionSubset
{
    [AttributeUsage(AttributeTargets.All)]
    sealed class LabelAttribute : Attribute
    {
        public readonly string Text;
        public LabelAttribute(string text) => Text = text;
    }

    [NoCompressMetadata, Label("direct")]
    class Direct
    {
        [Label("field")]
        public int Field;
        [Label("property")]
        public int Value { get; set; }
        [Label("constructor")]
        public Direct(int value) => Field = value;
        [Label("method")]
        public string Read([Label("parameter")] string text) => text + Field;
    }

    class Middle : Direct { public Middle(int value) : base(value) { } }
    sealed class Descendant : Middle { public Descendant(int value) : base(value) { } }

    class ReflectionPolicyAttribute : NoCompressMetadataAttribute { }
    sealed class FastReflectionAttribute : ReflectionPolicyAttribute { }

    [FastReflection]
    sealed class Custom { public int Value = 7; }

    [NoCompressMetadata]
    struct ValueSubject { public int Value; }

    [NoCompressMetadata]
    enum EnumSubject { Value = 11 }

    [NoCompressMetadata]
    delegate int DelegateSubject(int value);

    [NoCompressMetadata]
    interface ISubject { int Read(int value); }

    sealed class Implementation : ISubject { public int Read(int value) => value + 1; }

    [NoCompressMetadata]
    class Generic<T>
    {
        public T Value;
        public Generic(T value) => Value = value;
        public T Read(T value) => value;
    }

    sealed class GenericDescendant : Generic<int>
    {
        public GenericDescendant(int value) : base(value) { }
    }

    sealed class Argument { public int Value = 13; }

    sealed class PlainGeneric<T>
    {
        public T Value;
        public PlainGeneric(T value) => Value = value;
    }

    [NoCompressMetadata]
    sealed class Outer
    {
        public int Value = 17;
        public sealed class Nested { public int Value = 19; }
    }

    sealed class Container
    {
        public int Value = 23;
        [NoCompressMetadata]
        public sealed class Nested { public int Value = 29; }
    }

    class CliBase { public int Value = 31; }
    sealed class CliDescendant : CliBase { }

    static class Program
    {
        static string Label(MemberInfo member) => ((LabelAttribute)member.GetCustomAttributes(false)[0]).Text;

        static void Field(string label, object subject)
        {
            Type type = subject.GetType();
            Console.WriteLine("metadata-compression-" + label + "=" + type.GetField("Value").GetValue(subject));
        }

        static void GenericMembers(string label, object subject, object value)
        {
            Type type = subject.GetType();
            MethodInfo method = type.GetMethod("Read");
            Console.WriteLine("metadata-compression-" + label + "=" + method.Invoke(subject, new[] { value })
                + "/" + method.GetParameters()[0].ParameterType.Name
                + "/" + type.GetGenericTypeDefinition().IsGenericTypeDefinition);
        }

        internal static void Run()
        {
            // Runtime type discovery keeps the attribute policy independent of ldtoken's native default.
            Console.WriteLine("metadata-compression-begin");
            object direct = new Direct(3);
            Type type = direct.GetType();
            MethodInfo method = type.GetMethod("Read");
            Console.WriteLine("metadata-compression-direct=" + method.Invoke(direct, new object[] { "v" })
                + "/" + method.GetParameters()[0].Name);
            Console.WriteLine("metadata-compression-labels=" + Label(type.GetField("Field"))
                + "/" + Label(type.GetProperty("Value")) + "/" + Label(type.GetConstructors()[0])
                + "/" + Label(method) + "/" + ((LabelAttribute)method.GetParameters()[0].GetCustomAttributes(false)[0]).Text);
            object descendant = new Descendant(5);
            Console.WriteLine("metadata-compression-inheritance="
                + descendant.GetType().GetMethod("Read").Invoke(descendant, new object[] { "v" })
                + "/" + descendant.GetType().BaseType.BaseType.Name);
            Field("custom", new Custom());
            Field("struct", new ValueSubject { Value = 9 });
            Console.WriteLine("metadata-compression-enum=" + (int)(EnumSubject)EnumSubject.Value.GetType().GetField("Value").GetValue(null));
            DelegateSubject function = value => value + 2;
            Console.WriteLine("metadata-compression-delegate=" + function.GetType().Name + "/" + function(10));
            object implementation = new Implementation();
            Type contract = implementation.GetType().GetInterfaces()[0];
            Console.WriteLine("metadata-compression-interface=" + contract.IsInterface
                + "/" + contract.GetMethod("Read").Invoke(implementation, new object[] { 12 }));
            GenericMembers("generic-value", new Generic<int>(0), 15);
            GenericMembers("generic-reference", new Generic<string>(""), "text");
            object argument = new Argument();
            object generic = new Generic<Argument>((Argument)argument);
            Console.WriteLine("metadata-compression-generic-argument="
                + ReferenceEquals(argument, generic.GetType().GetField("Value").GetValue(generic)));
            Field("generic-descendant", new GenericDescendant(16));
            Field("argument", argument);
            object plainGeneric = new PlainGeneric<Direct>((Direct)direct);
            Console.WriteLine("metadata-compression-plain-generic="
                + ReferenceEquals(direct, plainGeneric.GetType().GetField("Value").GetValue(plainGeneric))
                + "/" + plainGeneric.GetType().GetGenericTypeDefinition().IsGenericTypeDefinition);
            Field("outer", new Outer());
            Field("outer-nested", new Outer.Nested());
            Field("container", new Container());
            Field("container-nested", new Container.Nested());
            Field("cli-base", new CliBase());
            Field("cli-descendant", new CliDescendant());
            Field("namesake", new Other.Subject());
            Type array = new Direct[1].GetType();
            Console.WriteLine("metadata-compression-array=" + array.GetArrayRank() + "/" + array.GetElementType().Name);
            Console.WriteLine("metadata-compression-end");
        }
    }
}

namespace ReflectMetadataCompressionSubset.Other
{
    sealed class NoCompressMetadataAttribute : Attribute { }

    [NoCompressMetadata]
    sealed class Subject { public int Value = 37; }
}
