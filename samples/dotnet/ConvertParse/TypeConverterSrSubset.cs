#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TypeConverterSrSubset
{
    // System.ComponentModel TypeConverter surface (transpiled from the real
    // System.ComponentModel.TypeConverter IL): ArrayConverter / CollectionConverter
    // ConvertTo(string) and the GetConvertToException path. Every one of these
    // methods opens with the BCL's `SR.UsingResourceKeys() ? key : text` guard —
    // a bool-returning System.SR member — so merely reaching them asserts the SR
    // intrinsic pushes a bool for it instead of dropping the return value (the
    // dropped push surfaced as "IL stack underflow" on the following branch).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            var ac = new ArrayConverter();
            int[] arr = { 1, 2, 3 };
            Console.WriteLine(ac.CanConvertTo(typeof(string)));   // True

            var cc = new CollectionConverter();
            var list = new List<int> { 4, 5 };
            Console.WriteLine(cc.ConvertTo(list, typeof(string))); // (Collection)

            // The string arm. ArrayConverter.ConvertTo(value, string) branches on
            // `value is Array` — an isinst to the abstract System.Array. Array type-infos
            // carry base=nullptr, so that isinst must be answered specially or the
            // converter falls through to GetConvertToException instead of formatting.
            Console.WriteLine(ac.ConvertTo(arr, typeof(string))); // Int32[] Array

            // Unsupported destination: TypeConverter.GetConvertToException builds
            // its message through SR.Format behind the same UsingResourceKeys guard.
            try
            {
                ac.ConvertTo(arr, typeof(int));
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        internal static void RunDefaultConverter()
        {
            var converter = TypeDescriptor.GetConverter(typeof(PlainSettings));
            Console.WriteLine("Default type converter: " + converter.GetType().FullName);
        }

        internal static void RunAnnotatedAttributes()
        {
            var converter = TypeDescriptor.GetConverter(typeof(AnnotatedSettings));
            Console.WriteLine("Annotated converter: " + converter.GetType().Name);
            var contract = typeof(AnnotatedSettings).GetCustomAttribute<ConstructorContractAttribute>();
            Console.WriteLine("Attribute fixed: " + Activator.CreateInstance(contract.Fixed).GetType().Name);
            Console.WriteLine("Attribute named: " + Activator.CreateInstance(contract.Named).GetType().Name);
        }

        [TypeConverter(typeof(SettingsConverter))]
        [ConstructorContract(typeof(FixedPayload), Named = typeof(NamedPayload))]
        private sealed class AnnotatedSettings { }

        private sealed class SettingsConverter : TypeConverter { public SettingsConverter() { } }
        private sealed class FixedPayload { public FixedPayload() { } }
        private sealed class NamedPayload { public NamedPayload() { } }

        private class ContractBaseAttribute : Attribute
        {
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            public Type Named { get; set; }
        }

        private sealed class ConstructorContractAttribute : ContractBaseAttribute
        {
            public Type Fixed { get; }
            public ConstructorContractAttribute(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type value)
                => Fixed = value;
        }

        private sealed class PlainSettings
        {
        }
    }
}
