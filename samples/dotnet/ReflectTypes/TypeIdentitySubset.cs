using System;
using System.Reflection;
using System.Text;

namespace TypeIdentitySubset
{
    // ONE CLR type must have exactly ONE type-info. A dn2cpp `Type` IS its Dn2CppTypeInfo
    // pointer — dn2cpp_get_type_from_handle interns one Type object per handle — so ==,
    // Equals, GetHashCode and ReferenceEquals all reduce to that pointer, and a second
    // handle for one type is not a tidiness question: the two are not assignable to each
    // other, compare unequal, and intern two rows in every table keyed by a declaring
    // type-info. VoidIdentitySubset's argument, generalized.
    //
    // The OBVIOUS probe passes either way — typeof(object) == new object().GetType() is
    // true even with a duplicate handle, since both routes name the runtime one. The
    // fragile routes are the ones nothing spells out: an emitted class's BASE pointer, and
    // a reflected member's FieldType/ParameterType. So every assertion below compares a
    // type reached THROUGH one of those against the typeof of the same type.
    //
    // Every line matches real .NET.
    internal static class Program
    {
        private enum Shade { Dim = 1, Bright = 2 }

        private class Root { }

        private class Bearer
        {
            public Type TypeField;
            public StringBuilder BuilderField;
            public MemberInfo MemberField;
            public MethodInfo MethodField;
            public string TextField;
            public Enum EnumField;
            public Exception ErrorField;
            public Array ArrayField;
            public ValueType BoxField;
            public Delegate CallField;

            public void Takes(Type t, StringBuilder sb, MethodInfo mi) { }
        }

        // Labeled, not a bare bool: a block of identical `True` lines cannot say WHICH
        // route came apart, and the frozen snapshot is the only thing a reader of a
        // failure has. The label names the route, since that is what a regression moves.
        private static void Same(string route, bool answer) => Console.WriteLine(route + " -> " + answer);

        internal static void Run()
        {
            Console.WriteLine("== one CLR type, one type-info ==");

            // The base pointer of an emitted class. System.Object's handle is the
            // runtime's (every allocation carries it), so a class whose base chain ends
            // anywhere else has a second System.Object.
            Same("typeof(Root).BaseType == typeof(object)", typeof(Root).BaseType == typeof(object));
            Same("typeof(ValueType).BaseType == typeof(object)", typeof(ValueType).BaseType == typeof(object));
            Same("typeof(Array).BaseType == typeof(object)", typeof(Array).BaseType == typeof(object));
            Same("typeof(Delegate).BaseType == typeof(object)", typeof(Delegate).BaseType == typeof(object));
            Same("typeof(Exception).BaseType == typeof(object)", typeof(Exception).BaseType == typeof(object));
            Same("typeof(string).BaseType == typeof(object)", typeof(string).BaseType == typeof(object));
            Same("typeof(Root).BaseType == new object().GetType()", typeof(Root).BaseType == new object().GetType());

            // The same question one level down, and running the OTHER way: an emitted
            // enum's base is the runtime's System.Enum handle, so typeof(Enum) has to be
            // that handle rather than an emitted one.
            Same("typeof(Shade).BaseType == typeof(Enum)", typeof(Shade).BaseType == typeof(Enum));
            Same("typeof(Enum).BaseType == typeof(ValueType)", typeof(Enum).BaseType == typeof(ValueType));
            Same("typeof(Enum).IsAssignableFrom(typeof(Shade))", typeof(Enum).IsAssignableFrom(typeof(Shade)));

            // A reflected member's type. These are the routes that spell a type-info
            // symbol without any `typeof` in sight, and each names a family whose
            // objects the RUNTIME allocates — a Dn2CppType, a Dn2CppMethodRef, a
            // Dn2CppStringBuilder — so the emitted shell can never be the identity.
            Type b = typeof(Bearer);
            Same("b.GetField(\"TypeField\").FieldType == typeof(Type)", b.GetField("TypeField").FieldType == typeof(Type));
            Same("b.GetField(\"BuilderField\").FieldType == typeof(StringBuilder)", b.GetField("BuilderField").FieldType == typeof(StringBuilder));
            Same("b.GetField(\"MemberField\").FieldType == typeof(MemberInfo)", b.GetField("MemberField").FieldType == typeof(MemberInfo));
            Same("b.GetField(\"MethodField\").FieldType == typeof(MethodInfo)", b.GetField("MethodField").FieldType == typeof(MethodInfo));
            Same("b.GetField(\"TextField\").FieldType == typeof(string)", b.GetField("TextField").FieldType == typeof(string));
            Same("b.GetField(\"EnumField\").FieldType == typeof(Enum)", b.GetField("EnumField").FieldType == typeof(Enum));
            Same("b.GetField(\"ErrorField\").FieldType == typeof(Exception)", b.GetField("ErrorField").FieldType == typeof(Exception));
            Same("b.GetField(\"ArrayField\").FieldType == typeof(Array)", b.GetField("ArrayField").FieldType == typeof(Array));
            Same("b.GetField(\"BoxField\").FieldType == typeof(ValueType)", b.GetField("BoxField").FieldType == typeof(ValueType));
            Same("b.GetField(\"CallField\").FieldType == typeof(Delegate)", b.GetField("CallField").FieldType == typeof(Delegate));

            ParameterInfo[] ps = b.GetMethod(nameof(Bearer.Takes)).GetParameters();
            Same("ps[0].ParameterType == typeof(Type)", ps[0].ParameterType == typeof(Type));
            Same("ps[1].ParameterType == typeof(StringBuilder)", ps[1].ParameterType == typeof(StringBuilder));
            Same("ps[2].ParameterType == typeof(MethodInfo)", ps[2].ParameterType == typeof(MethodInfo));

            // Identity is not just ==: a Type is a dictionary key and a hash bucket, and
            // two handles hash apart even when both spell the same name.
            Same("ReferenceEquals(typeof(Root).BaseType, typeof(object))", ReferenceEquals(typeof(Root).BaseType, typeof(object)));
            Same("typeof(Root).BaseType.Equals(typeof(object))", typeof(Root).BaseType.Equals(typeof(object)));
            Same("typeof(Root).BaseType.GetHashCode() == typeof(object).GetHashCode()", typeof(Root).BaseType.GetHashCode() == typeof(object).GetHashCode());
            Same("Type.GetType(\"System.Object\") == typeof(Root).BaseType", Type.GetType("System.Object") == typeof(Root).BaseType);

            // …and the runtime's own handles keep the properties only they carry: an
            // OWNED row must not be "fixed" into a bind, which would copy the emitted
            // metadata over them.
            Same("typeof(int).IsPrimitive", typeof(int).IsPrimitive);
            Same("typeof(string).IsSealed", typeof(string).IsSealed);
            Same("typeof(Enum).IsAbstract", typeof(Enum).IsAbstract);
            Same("typeof(Shade).IsEnum", typeof(Shade).IsEnum);

            // The declaring type of a member found by walking a base chain. The
            // g_meta_members intern is keyed on the declaring type-info POINTER, so two
            // System.Objects would intern this row twice.
            MethodInfo viaObject = typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo viaDerived = typeof(Bearer).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            Same("viaObject.DeclaringType == typeof(object)", viaObject.DeclaringType == typeof(object));
            Same("viaDerived.DeclaringType == typeof(object)", viaDerived.DeclaringType == typeof(object));
        }
    }
}
