using System.Globalization;
using ReflectionDepthLibrary;

namespace ReflectionDepthSummary;

#if UNREAD_ATTRIBUTE_CTOR_DEPTH
[UnreadDepthConstructor]
#elif UNREAD_ATTRIBUTE_GETTER_DEPTH
[UnreadDepthGetter(Level = 1)]
#elif ATTRIBUTE_READ_DEPTH || ATTRIBUTE_GENERIC_READ_DEPTH
[ReadDepth(Level = 7)]
#endif
internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
#if UNREAD_ATTRIBUTE_CTOR_DEPTH
        Console.WriteLine("unread-attribute-ctor-ran");
#elif UNREAD_ATTRIBUTE_GETTER_DEPTH
        Console.WriteLine("unread-attribute-getter-ran");
#elif ATTRIBUTE_READ_DEPTH || ATTRIBUTE_GENERIC_READ_DEPTH
        AttributeDepthDriver.Read();
        Console.WriteLine("attribute-read-ran");
        if (args.Length > 0 && args[0] == "before-reflected-field-boxes")
            return;
        Console.WriteLine("== late reflected field boxes ==");
        Type fieldOwner = typeof(Holder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        object fieldValue = fieldOwner.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null)!;
        Console.WriteLine(fieldValue);
        Console.WriteLine("field equals=" + fieldValue.Equals("second"));
        Console.WriteLine("field hash=" + fieldValue.GetHashCode());
        Console.WriteLine("late reflected field boxes end");
#elif NONVIRTUAL_BASE_CALL_DEPTH
        new BaseCallDerived().Test();
        Console.WriteLine("base-call-ran");
#elif BASE_CONSTRUCTOR_DEPTH
        Console.WriteLine(new ObjectSlotDerived());
        Console.WriteLine("base-constructor-ran");
#elif INHERITED_OBJECT_SLOT_DEPTH
        object receiver = new ObjectSlotDerived();
        Console.WriteLine(receiver.ToString());
        Console.WriteLine("inherited-object-slot-ran");
#elif DEAD_PRIMITIVE_BRANCH_DEPTH
        if (typeof(int) == typeof(string)) DeadDescend<int>();
        Console.WriteLine("dead-primitive-branch-ran");
#elif DEAD_NOMINAL_BRANCH_DEPTH
        if (typeof(NominalEmptyA) == typeof(NominalEmptyB)) DeadNominalDescend<int>();
        if (typeof(NominalEmptyA[]) == typeof(NominalEmptyB[])) DeadNominalDescend<int>();
        if (typeof(Int32) == typeof(int)) DeadNominalDescend<int>();
        if (typeof(NominalGenericA<int>) == typeof(NominalGenericB<int>)) DeadNominalDescend<int>();
        if (typeof(NominalGenericA<int>) == typeof(NominalGenericA<long>)) DeadNominalDescend<int>();
        Console.WriteLine("dead-nominal-branch-ran");
#elif METHODIMPL_PRIORITY_DEPTH
        IPrioritySlot receiver = new PrioritySlotReceiver();
        receiver.Run();
        Console.WriteLine("methodimpl-priority-ran");
#elif INHERITED_INTERFACE_MAPPING_DEPTH
        IInheritedMapping receiver = new InheritedMappingDerived();
        receiver.Run();
        Console.WriteLine("inherited-interface-map-ran");
#elif PROTECTED_INTERFACE_MAPPING_DEPTH
        IProtectedMapping receiver = new ProtectedMappingDerived();
        receiver.Run();
        Console.WriteLine("protected-interface-map-ran");
#elif CLASS_NEWSLOT_MAPPING_DEPTH
        OriginalClassSlot receiver = new SeparateClassSlotDerived();
        receiver.Run();
        Console.WriteLine("class-newslot-map-ran");
#elif ISA_GETTER_BRANCH_DEPTH
        if (System.Runtime.Intrinsics.Arm.Sve.IsSupported) DeadIsaDescend<int>();
        if (System.Runtime.Intrinsics.Arm.Sve.Arm64.IsSupported) DeadIsaDescend<int>();
        if (System.Runtime.Intrinsics.Arm.Sve2.IsSupported) DeadIsaDescend<int>();
        if (System.Runtime.Intrinsics.Arm.Sve2.Arm64.IsSupported) DeadIsaDescend<int>();
        Console.WriteLine("isa-getter-branch-ran");
#elif CONSTRAINED_PRIMITIVE_DEPTH
        Console.WriteLine(new PrimitiveSlotReceiver() is not null);
        Console.WriteLine(PrimitiveCall<int>(1));
        Console.WriteLine("constrained-primitive-ran");
#elif CONST_GETTER_BRANCH_DEPTH
        if (System.Diagnostics.Debugger.IsLogging()) DeadGetterDescend<int>();
        Console.WriteLine("const-getter-branch-ran");
#elif UNUSED_DEFAULT_INTERFACE_DEPTH
        Console.WriteLine(typeof(DefaultSlotPing).GetMethod("Read")!.Invoke(null, null));
        Console.WriteLine("unused-default-interface-ran");
#elif ABSTRACT_APPLICATION_DEPTH
        _ = Activator.CreateInstance(typeof(ConcreteConstructionTarget));
        Console.WriteLine("abstract-application-ran");
#elif ABSTRACT_LIBRARY_DEPTH
        _ = new AbstractLibraryHolder();
        _ = Activator.CreateInstance(typeof(ConcreteConstructionTarget));
        Console.WriteLine("abstract-library-ran");
#elif GENERIC_FACTORY_DIRECT_DEPTH
        _ = Activator.CreateInstance<FactoryData>();
        ShowSecond();
        Console.WriteLine("generic-factory-direct-ran");
#elif GENERIC_FACTORY_CLASS_DEPTH
        _ = Factory<FactoryData>.Make();
        ShowSecond();
        Console.WriteLine("generic-factory-class-ran");
#elif GENERIC_FACTORY_METHOD_DEPTH
        _ = Make<FactoryData>();
        ShowSecond();
        Console.WriteLine("generic-factory-method-ran");
#elif GENERIC_FACTORY_IDENTITY_DEPTH
        _ = Make<EmptyFactoryData>();
        _ = Make<FactoryData>();
        ShowSecond();
        Console.WriteLine("generic-factory-identity-ran");
#elif CONSTRUCTION_ACCESSOR_DEPTH
        Console.WriteLine(new ConstructionHolder() is not null);
#elif GENERIC_ACCESSOR_DEPTH
        Console.WriteLine(new GenericAccessorHolder() is not null);
#elif GENERIC_CTOR_DEPTH
        Console.WriteLine(new GenericConstructorHolder() is not null);
#elif EXECUTED_GENERIC_ARGUMENT_DEPTH
        Seed<AccessorData>();
        Console.WriteLine(new object() is not null);
#elif UNCALLED_EVENT_DEPTH
        Console.WriteLine(new EventHolder() is not null);
#elif LATE_CONSTRUCTION_ACCESSOR_DEPTH
        _ = new LateConstructionHolder();
        _ = Activator.CreateInstance(typeof(ConstructedData));
        ShowSecond();
#elif DIRECT_ACCESSOR_DEPTH
        Seed<DirectAccessorData>();
        _ = new DirectAccessorData().Value;
        ShowSecond();
#elif DIRECT_EVENT_ADD_DEPTH
        var holder = new DirectEventHolder();
        holder.E += Handler;
        ShowSecond();
#elif INVOKE_UNCALLED_VIRTUAL_DEPTH
        Console.WriteLine(typeof(Ping).GetMethod("Read")!.Invoke(null, null));
        Console.WriteLine(new UncalledNode<int>() is not null);
#elif UNCALLED_VIRTUAL_DEPTH
        Console.WriteLine(new UncalledNode<int>() is not null);
#elif UNCALLED_INTERFACE_DEPTH
        Console.WriteLine(new UncalledWorker() is not null);
#elif UNALLOCATED_RECEIVER_DEPTH
        _ = new ReceiverHolder();
        IColdReceiver receiver = new SafeReceiver();
        receiver.Run();
#elif UNUSED_COPIED_SHAPE_DEPTH || UNUSED_DEEP_COPIED_SHAPE_DEPTH
        Console.WriteLine(new CopiedShapeWorker() is not null);
#else
#if DEEP_DEPTH_SUMMARY
        Caller<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>.Run();
#else
        Caller<List<List<int>>>.Run();
#endif
        ShowSecond();
#if ORDINARY_CUT_DEPTH
        Console.WriteLine("== ordinary cut depth summary ==");
        Cut();
        Console.WriteLine("cut-complete");
#endif
#endif
    }

    internal static void Seed<T>() { }
#if DEAD_PRIMITIVE_BRANCH_DEPTH
    private static void DeadDescend<T>() => DeadDescend<List<T>>();
#endif
#if DEAD_NOMINAL_BRANCH_DEPTH
    private static void DeadNominalDescend<T>() => DeadNominalDescend<List<T>>();
#endif
#if CONST_GETTER_BRANCH_DEPTH
    private static void DeadGetterDescend<T>() => DeadGetterDescend<List<T>>();
#endif
#if ISA_GETTER_BRANCH_DEPTH
    private static void DeadIsaDescend<T>() => DeadIsaDescend<List<T>>();
#endif
#if CONSTRAINED_PRIMITIVE_DEPTH
    private static int PrimitiveCall<T>(T value) where T : IComparable => value.CompareTo(null);
#endif
#if ORDINARY_CUT_DEPTH
    private static void Cut() => Descend<int>(3);
    private static void Descend<T>(int remaining)
    {
        if (remaining <= 0) return;
        Descend<List<T>>(remaining - 1);
    }
#endif
#if GENERIC_FACTORY_METHOD_DEPTH || GENERIC_FACTORY_IDENTITY_DEPTH
    private static T Make<T>() => Activator.CreateInstance<T>();
#endif

#if !UNUSED_COPIED_SHAPE_DEPTH && !UNUSED_DEEP_COPIED_SHAPE_DEPTH
    private static void ShowSecond()
    {
        Type owner = typeof(Holder).GetMethod("Uncalled")!.GetParameters()[0].ParameterType;
        Console.WriteLine(owner.GetField("Right")!.FieldType.GetField("Zero")!.GetValue(null));
    }
#endif

#if DIRECT_EVENT_ADD_DEPTH
    private static void Handler() { }
#endif
}

#if !UNUSED_COPIED_SHAPE_DEPTH && !UNUSED_DEEP_COPIED_SHAPE_DEPTH
internal static class Caller<T>
{
#if DEEP_DEPTH_SUMMARY
    internal static void Run() => Program.Seed<Layer<T>>();
#else
    internal static void Run() => Program.Seed<List<T>>();
#endif
}

internal sealed class Holder
{
    public static void Uncalled(Outer<Leaf> value) { }
}

#pragma warning disable CS0649, CS8618
internal sealed class Outer<T>
{
#if DEEP_DEPTH_SUMMARY
    public static Inner<First<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<T>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Left;
    public static Inner<Second<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<Layer<T>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> Right;
#elif LATE_CONSTRUCTION_ACCESSOR_DEPTH
    public static Inner<LateFirst> Left;
    public static Inner<LateSecond> Right;
#else
    public static Inner<First<List<List<T>>>> Left;
    public static Inner<Second<List<List<T>>>> Right;
#endif
}

internal sealed class Inner<T>
{
    public static T Zero;
}
#pragma warning restore CS0649, CS8618

internal struct Leaf { }
#if DEEP_DEPTH_SUMMARY
internal struct Layer<T> { }
#endif
internal struct First<T> { public override string ToString() => "first"; }
internal struct Second<T>
{
    public override string ToString() => "second";
#if ATTRIBUTE_READ_DEPTH || ATTRIBUTE_GENERIC_READ_DEPTH
    public override bool Equals(object? other) => other is string text && text == "second";
    public override int GetHashCode() => 29;
#endif
}
#endif

#if UNCALLED_VIRTUAL_DEPTH || INVOKE_UNCALLED_VIRTUAL_DEPTH
internal class UncalledNode<T>
{
    public virtual UncalledNode<UncalledNode<T>> Wrap() => new();
}
#elif UNCALLED_INTERFACE_DEPTH
internal interface IUncalledWorker
{
    void Run();
}

internal sealed class UncalledWorker : IUncalledWorker
{
    public void Run() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if UNALLOCATED_RECEIVER_DEPTH
internal interface IColdReceiver { void Run(); }
internal sealed class SafeReceiver : IColdReceiver
{
    public void Run() => Console.WriteLine("Safe");
}
internal class ReceiverHolder
{
    public virtual IColdReceiver Uncalled() => new ColdReceiver<int>();
}
internal sealed class ColdReceiver<T> : IColdReceiver
{
    public void Run() => Descend<T>();
    private static void Descend<U>() => Descend<List<U>>();
}
#endif

#if UNUSED_COPIED_SHAPE_DEPTH || UNUSED_DEEP_COPIED_SHAPE_DEPTH
internal class CopiedShapeWorker
{
    public virtual void Uncalled()
    {
#if UNUSED_DEEP_COPIED_SHAPE_DEPTH
        Program.Seed<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<List<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>();
#else
        Program.Seed<List<List<List<int>>>>();
#endif
    }
}
#endif

#if CONSTRUCTION_ACCESSOR_DEPTH
#pragma warning disable CS0649, CS8618
internal class ConstructionHolder
{
    public AccessorData Data;
    public virtual object Uncalled() => Activator.CreateInstance(typeof(AccessorData))!;
}
#pragma warning restore CS0649, CS8618
#elif GENERIC_ACCESSOR_DEPTH
internal class GenericAccessorHolder
{
    public virtual void Uncalled() => Program.Seed<AccessorData>();
}
#elif GENERIC_CTOR_DEPTH
internal class GenericConstructorHolder
{
    public virtual void Uncalled() => Program.Seed<ConstructorData>();
}
#elif UNCALLED_EVENT_DEPTH
internal class EventHolder
{
    public virtual event Action E
    {
        add { Descend<int>(); }
        remove { }
    }
    private static void Descend<T>() => Descend<List<T>>();
}
#elif LATE_CONSTRUCTION_ACCESSOR_DEPTH
#pragma warning disable CS0649, CS8618
internal class LateConstructionHolder
{
    public ConstructedData Data;
    public virtual object Uncalled() => Activator.CreateInstance(typeof(ConstructedData))!;
}
#pragma warning restore CS0649, CS8618
#elif DIRECT_EVENT_ADD_DEPTH
internal class DirectEventHolder
{
    public virtual event Action E
    {
        add { Program.Seed<List<List<List<int>>>>(); }
        remove { Descend<int>(); }
    }
    private static void Descend<T>() => Descend<List<T>>();
}
#elif INVOKE_UNCALLED_VIRTUAL_DEPTH
internal static class Ping
{
    public static int Read() => 7;
}
#endif

#if LATE_CONSTRUCTION_ACCESSOR_DEPTH
internal struct LateFirst { public override string ToString() => "first"; }
internal struct LateSecond { public override string ToString() => "second"; }
#endif

#if GENERIC_FACTORY_CLASS_DEPTH
internal static class Factory<T>
{
    internal static T Make() => Activator.CreateInstance<T>();
}
#endif

#if NONVIRTUAL_BASE_CALL_DEPTH
internal class BaseCallParent { public virtual void Run() { } }
internal sealed class BaseCallDerived : BaseCallParent
{
    public override void Run() => Descend<int>();
    public void Test() => base.Run();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if BASE_CONSTRUCTOR_DEPTH || INHERITED_OBJECT_SLOT_DEPTH
internal class ObjectSlotParent
{
    public override string ToString() { Descend<int>(); return "base"; }
    private static void Descend<T>() => Descend<List<T>>();
}
internal sealed class ObjectSlotDerived : ObjectSlotParent
{
    public override string ToString() => "derived";
}
#endif

#if ABSTRACT_APPLICATION_DEPTH || ABSTRACT_LIBRARY_DEPTH
internal sealed class ConcreteConstructionTarget { public ConcreteConstructionTarget() { } }
#endif

#if ABSTRACT_APPLICATION_DEPTH
internal abstract class AbstractApplicationTarget
{
    public override string ToString() { Descend<int>(); return "abstract"; }
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if ABSTRACT_LIBRARY_DEPTH
#pragma warning disable CS0649
internal sealed class AbstractLibraryHolder { public AbstractConstructionData? Data; }
#pragma warning restore CS0649
#endif

#if DEAD_NOMINAL_BRANCH_DEPTH
internal sealed class NominalEmptyA { }
internal sealed class NominalEmptyB { }
internal sealed class Int32 { }
internal sealed class NominalGenericA<T> { }
internal sealed class NominalGenericB<T> { }
#endif

#if METHODIMPL_PRIORITY_DEPTH
internal interface IPrioritySlot { void Run(); }
internal class PrioritySlotReceiver : IPrioritySlot
{
    public virtual void Run() => Descend<int>();
    void IPrioritySlot.Run() => Console.WriteLine("explicit-slot");
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if CONSTRAINED_PRIMITIVE_DEPTH
internal class PrimitiveSlotReceiver : IComparable
{
    public int CompareTo(object? value) { Descend<int>(); return 0; }
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if UNUSED_DEFAULT_INTERFACE_DEPTH
internal static class DefaultSlotPing { public static int Read() => 7; }
internal interface IUnusedDefaultSlot
{
    void Run() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if INHERITED_INTERFACE_MAPPING_DEPTH
internal interface IInheritedMapping { void Run(); }
internal class InheritedMappingBase : IInheritedMapping
{
    void IInheritedMapping.Run() => Console.WriteLine("inherited-interface-ok");
}
internal class InheritedMappingDerived : InheritedMappingBase
{
    public virtual void Run() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if PROTECTED_INTERFACE_MAPPING_DEPTH
internal interface IProtectedMapping { void Run(); }
internal class ProtectedMappingBase : IProtectedMapping
{
    void IProtectedMapping.Run() => Console.WriteLine("protected-interface-ok");
}
internal class ProtectedMappingDerived : ProtectedMappingBase, IProtectedMapping
{
    protected virtual void Run() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif

#if CLASS_NEWSLOT_MAPPING_DEPTH
internal class OriginalClassSlot
{
    public virtual void Run() => Console.WriteLine("original-class-slot-ok");
}
internal class SeparateClassSlot : OriginalClassSlot
{
    public new virtual void Run() { }
}
internal class SeparateClassSlotDerived : SeparateClassSlot
{
    public override void Run() => Descend<int>();
    private static void Descend<T>() => Descend<List<T>>();
}
#endif
