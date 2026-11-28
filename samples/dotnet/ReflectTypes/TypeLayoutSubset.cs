using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TypeLayoutSubset;

public static class Program
{
    private sealed class DefaultClass
    {
        public int Value = 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class SequentialClass
    {
        public int Value = 0;
    }

    private struct DefaultStruct
    {
        public int Value;

        public DefaultStruct(int value)
        {
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private struct AutoStruct
    {
        public int Value;

        public AutoStruct(int value)
        {
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct ExplicitStruct
    {
        [FieldOffset(0)]
        public int Low;

        [FieldOffset(0)]
        public byte First;

        public ExplicitStruct(int low, byte first)
        {
            Low = low;
            First = first;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SequentialGenericStruct<T>
    {
        public T Value;

        public SequentialGenericStruct(T value)
        {
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private struct AutoGenericStruct<T>
    {
        public T Value;

        public AutoGenericStruct(T value)
        {
            Value = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class SequentialGenericClass<T>
    {
        public T Value = default!;
    }

    private enum SampleEnum
    {
        Zero = 0,
        One = 1,
    }

    private sealed class ExplicitDelegator : TypeDelegator
    {
        public ExplicitDelegator(Type delegatingType) : base(delegatingType)
        {
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return TypeAttributes.ExplicitLayout;
        }
    }

    private sealed class ClearedLayoutDelegator : TypeDelegator
    {
        public ClearedLayoutDelegator(Type delegatingType) : base(delegatingType)
        {
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return base.GetAttributeFlagsImpl() & ~TypeAttributes.LayoutMask;
        }
    }

    private abstract class BaseCustomLayoutType : Type
    {
        public override Assembly Assembly => throw new NotSupportedException();
        public override string? AssemblyQualifiedName => null;
        public override Type? BaseType => null;
        public override string? FullName => Name;
        public override Guid GUID => Guid.Empty;
        public override Module Module => throw new NotSupportedException();
        public override string? Namespace => null;
        public override Type UnderlyingSystemType => this;
        public override string Name => nameof(BaseCustomLayoutType);

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override Type? GetElementType() => null;
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override Type? GetInterface(string name, bool ignoreCase) => throw new NotSupportedException();
        public override Type[] GetInterfaces() => throw new NotSupportedException();
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => throw new NotSupportedException();
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override Type? GetNestedType(string name, BindingFlags bindingAttr) => throw new NotSupportedException();
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => throw new NotSupportedException();
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => throw new NotSupportedException();
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) => throw new NotSupportedException();
        protected override bool HasElementTypeImpl() => false;
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters) => throw new NotSupportedException();
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsCOMObjectImpl() => false;
        protected override bool IsPointerImpl() => false;
        protected override bool IsPrimitiveImpl() => false;
        public override object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Array.Empty<object>();
        public override bool IsDefined(Type attributeType, bool inherit) => false;

        public Func<TypeAttributes> BindAttributeFlags()
        {
            return GetAttributeFlagsImpl;
        }
    }

    private sealed class DirectExplicitType : BaseCustomLayoutType
    {
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return TypeAttributes.ExplicitLayout;
        }
    }

    private static void Show(string label, Type type)
    {
        Console.WriteLine(label + "=" + type.IsAutoLayout + "/" + type.IsLayoutSequential + "/" + type.IsExplicitLayout);
    }

    private static bool GenericSequential<T>()
    {
        return typeof(T).IsLayoutSequential;
    }

    private static string ConstrainedLayout<T>(T type) where T : Type
    {
        return type.IsAutoLayout + "/" + type.IsLayoutSequential + "/" + type.IsExplicitLayout;
    }

    private static bool ThrowsNullReference(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (NullReferenceException)
        {
            return true;
        }
    }

    public static void Run()
    {
        Console.WriteLine("== type layout properties ==");
        Show("int", typeof(int));
        Show("bool", typeof(bool));
        Show("decimal", typeof(decimal));
        Show("void", typeof(void));
        Show("timespan", typeof(TimeSpan));
        Show("dateonly", typeof(DateOnly));
        Show("timeonly", typeof(TimeOnly));
        Show("yield-awaiter", typeof(YieldAwaitable.YieldAwaiter));
        Show("nint", typeof(nint));
        Show("parallel-loop-result", typeof(ParallelLoopResult));

        Show("datetime", typeof(DateTime));
        Show("datetimeoffset", typeof(DateTimeOffset));
        Show("string", typeof(string));
        Show("object", typeof(object));

        Show("task-awaiter", typeof(TaskAwaiter));
        Show("cancellation-token", typeof(CancellationToken));

        Show("default-class", typeof(DefaultClass));
        Show("sequential-class", typeof(SequentialClass));
        Show("default-struct", typeof(DefaultStruct));
        Show("auto-struct", typeof(AutoStruct));
        Show("explicit-struct", typeof(ExplicitStruct));
        Show("enum", typeof(SampleEnum));

        Show("nullable-def", typeof(Nullable<>));
        Show("nullable-int", typeof(Nullable<int>));
        Show("span-def", typeof(Span<>));
        Show("sequential-generic-struct-def", typeof(SequentialGenericStruct<>));
        Show("auto-generic-struct-def", typeof(AutoGenericStruct<>));
        Show("sequential-generic-class-def", typeof(SequentialGenericClass<>));
        Show("sequential-generic-class-int", typeof(SequentialGenericClass<int>));
        Show("int-array", typeof(int[]));
        Show("int-pointer", typeof(int*));
        Show("int-byref", typeof(int).MakeByRefType());
        Show("generic-param", typeof(Nullable<>).GetGenericArguments()[0]);

        var delegatorInt = new TypeDelegator(typeof(int));
        var explicitDelegator = new ExplicitDelegator(typeof(int));
        var clearedDelegator = new ClearedLayoutDelegator(typeof(int));
        var nestedDelegator = new TypeDelegator(explicitDelegator);
        Show("delegator-int", delegatorInt);
        Show("explicit-delegator", explicitDelegator);
        Show("cleared-delegator", clearedDelegator);
        Show("nested-delegator", nestedDelegator);

        Type? nullType = null;
        Console.WriteLine("null-receiver="
            + ThrowsNullReference(() => _ = nullType!.IsAutoLayout) + "/"
            + ThrowsNullReference(() => _ = nullType!.IsLayoutSequential) + "/"
            + ThrowsNullReference(() => _ = nullType!.IsExplicitLayout) + "/"
            + ThrowsNullReference(() => _ = nullType!.Attributes));

        Console.WriteLine("generic-seq="
            + GenericSequential<string>() + "/"
            + GenericSequential<SequentialClass>() + "/"
            + GenericSequential<DefaultStruct>() + "/"
            + GenericSequential<AutoStruct>() + "/"
            + GenericSequential<SampleEnum>() + "/"
            + GenericSequential<int>());

        Console.WriteLine("constrained-int=" + ConstrainedLayout(typeof(int)));
        Console.WriteLine("constrained-explicit=" + ConstrainedLayout(explicitDelegator));

        Console.WriteLine("intrinsic-meta-def="
            + typeof(TaskAwaiter[]).HasSameMetadataDefinitionAs(typeof(int[])) + "/"
            + typeof(TaskAwaiter[]).HasSameMetadataDefinitionAs(typeof(DefaultStruct[])));

        var directExplicit = new DirectExplicitType();
        Show("direct-custom-layout", directExplicit);
        var boundFlags = directExplicit.BindAttributeFlags();
        Console.WriteLine("delegate-flags=" + boundFlags() + "/" + boundFlags.Method.DeclaringType?.Name);
        Console.WriteLine("type layout properties end");
    }
}
