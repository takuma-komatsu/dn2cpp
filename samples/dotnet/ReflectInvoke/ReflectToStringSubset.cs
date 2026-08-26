#nullable disable
using System;
using System.Reflection;

namespace ReflectToStringSubset
{
    enum Tone { Quiet, Loud }

    [AttributeUsage(AttributeTargets.Class)]
    sealed class DisplayAttribute : Attribute
    {
        public Tone Tone;
        public string Name { get; set; }
        public DisplayAttribute(int order, string note, Type target, Tone tone, int[] values) { }
    }

    [Display(3, null, typeof(string), Tone.Loud, new[] { 4, 5 }, Tone = Tone.Quiet, Name = "named")]
    sealed class Subject
    {
        public int Count;
        public Subject(ref int seed) { }
        public ref string Read(ref int index, string[] values) => throw new InvalidOperationException();
        public static T Echo<T>(ref T value) => value;
        public string this[int index, long offset] => "";
    }

    static class Program
    {
        internal static void Run()
        {
            int reached = 7;
            Console.WriteLine("tostr-generic-reach=" + Subject.Echo<int>(ref reached));
            Type t = typeof(Subject);
            MethodInfo method = t.GetMethod("Read");
            ConstructorInfo ctor = t.GetConstructors()[0];
            FieldInfo field = t.GetField("Count");
            PropertyInfo property = t.GetProperty("Item");
            ParameterInfo parameter = method.GetParameters()[0];
            ParameterInfo result = method.ReturnParameter;
            CustomAttributeData attribute = null;
            foreach (CustomAttributeData candidate in t.CustomAttributes)
                if (candidate.AttributeType == typeof(DisplayAttribute))
                    attribute = candidate;
            MethodInfo generic = t.GetMethod("Echo");
            MethodInfo closedGeneric = generic.MakeGenericMethod(typeof(int));
            MethodInfo genericDefinition = closedGeneric.GetGenericMethodDefinition();
            ParameterInfo closedGenericParameter = closedGeneric.GetParameters()[0];
            ParameterInfo genericDefinitionParameter = genericDefinition.GetParameters()[0];

            Console.WriteLine("tostr-method-direct=" + method.ToString());
            MemberInfo member = method;
            Console.WriteLine("tostr-member=" + member.ToString());
            MethodBase methodBase = method;
            Console.WriteLine("tostr-methodbase=" + methodBase.ToString());
            Console.WriteLine("tostr-method-object=" + ((object)method).ToString());
            Console.WriteLine("tostr-ctor-direct=" + ctor.ToString());
            Console.WriteLine("tostr-ctor-object=" + ((object)ctor).ToString());
            Console.WriteLine("tostr-field-direct=" + field.ToString());
            Console.WriteLine("tostr-field-object=" + ((object)field).ToString());
            Console.WriteLine("tostr-property-direct=" + property.ToString());
            Console.WriteLine("tostr-property-object=" + ((object)property).ToString());
            Console.WriteLine("tostr-parameter-direct=" + parameter.ToString());
            Console.WriteLine("tostr-parameter-object=" + ((object)parameter).ToString());
            Console.WriteLine("tostr-return-direct=" + result.ToString());
            Console.WriteLine("tostr-return-object=" + ((object)result).ToString());
            Console.WriteLine("tostr-attribute-direct=" + attribute.ToString());
            Console.WriteLine("tostr-attribute-object=" + ((object)attribute).ToString());
            Console.WriteLine("tostr-generic-closed=" + closedGeneric.ToString());
            Console.WriteLine("tostr-generic-def=" + genericDefinition.ToString());
            Console.WriteLine("tostr-generic-param-closed=" + closedGenericParameter.ToString());
            Console.WriteLine("tostr-generic-param-def=" + genericDefinitionParameter.ToString());
        }
    }
}
