using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EventSubset;

[AttributeUsage(AttributeTargets.Event)]
internal sealed class EventMarkAttribute : Attribute
{
    public string Text;
    public EventMarkAttribute(string text) => Text = text;
}

internal class EventBase
{
    [EventMark("base-event")]
    public event Action<int> BaseTick;
    protected event Action ProtectedTick;
    private event Action PrivateTick;
    public static event Action<int> Shared;
    public virtual event Action<int> Changed;
    public event Action Hidden;
    public event Action Shadow;
    public void Raise(int value)
    {
        BaseTick?.Invoke(value);
        Shared?.Invoke(value);
    }
}

internal sealed class EventDerived : EventBase
{
    public event Action DerivedTick;
    public override event Action<int> Changed;
    public new event Action<string> Hidden;
    private new event Action Shadow;
    private event Action Secret;
}

internal sealed class EventCase
{
    public event Action Tick;
    public event Action tick;
    public event Action Ωμέγα;
}

internal class GenericEventBase<T>
{
    public event Action<T> Value;
    public void Raise(T value) => Value?.Invoke(value);
}

internal sealed class GenericEventDerived<T> : GenericEventBase<T[]> { }
internal sealed class EventOverClosed<T> : EventBase { }
internal sealed class BareEventOwner<T>
{
    public event Action<T> Only;
}

[AttributeUsage(AttributeTargets.Event)]
internal sealed class PreserveAttribute : Attribute { }

internal sealed class AttributeEventOwner
{
    public int Count;
    [Preserve]
    private event Action<int> Exotic { add => Count++; remove => Count--; }
    private void RaiseExotic(int value) => Count += value;
    private void OtherPublic(int value) => Count += value * 2;
    private void OtherPrivate(int value) => Count += value * 3;
}

internal sealed class XmlEventOwner
{
    public int Count;
    private event Action<int> Exotic { add => Count++; remove => Count--; }
    private void RaiseExotic(int value) => Count += value;
    private void OtherPublic(int value) => Count += value * 2;
    private void OtherPrivate(int value) => Count += value * 3;
}

[DefaultMember("Exotic")]
internal class ExoticEventOwner
{
    public int Count;
    public event Action<int> Exotic
    {
        add => Count++;
        remove => Count--;
    }
    private void RaiseExotic(int value) => Count += value;
    public void OtherPublic(int value) => Count += value * 2;
    private void OtherPrivate(int value) => Count += value * 3;
}

internal sealed class InheritedExoticEventOwner : ExoticEventOwner { }

internal sealed class GenericOtherEventOwner
{
    public event Action Tick { add { } remove { } }
    public void Other<T>() { }
}

internal sealed class SynthEventOwner<T>
{
    public int Count;
    public event Action Ready
    {
        add => Count++;
        remove => Count--;
    }
    public T Echo(T value) => value;
}

internal static partial class Program
{
    private const BindingFlags EveryEvent = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
    private static string Fault(Action action)
    {
        try { action(); }
        catch (Exception exception) { return exception.GetType().Name; }
        return "none";
    }
    private static string Names(EventInfo[] events) => string.Join(",", events.Select(item => item.Name));
    private static string MethodNames(MethodInfo[] methods) => string.Join(",", methods.Select(item => item.Name));

    internal static void RunGenericOtherReflection()
    {
        new GenericOtherEventOwner().Other<int>();
        var other = typeof(GenericOtherEventOwner).GetEvent("Tick").GetOtherMethods(true)[0];
        Console.WriteLine("event generic other: " + other.IsGenericMethodDefinition + "/" + other.GetGenericArguments().Length + "/" + ReferenceEquals(other, typeof(GenericOtherEventOwner).GetMethod("Other")) + "/" + Fault(() => other.MakeGenericMethod(typeof(int))));
        var constructed = other.MakeGenericMethod(typeof(int));
        Console.WriteLine("event generic other context: " + ReferenceEquals(constructed, typeof(GenericOtherEventOwner).GetMethod("Other").MakeGenericMethod(typeof(int))) + "/" + ReferenceEquals(constructed.GetGenericMethodDefinition(), other));
    }

    internal static void RunReflection(bool collect = true)
    {
        Console.WriteLine("== reflected events ==");
        var type = typeof(EventDerived);
        var item = type.GetEvent("BaseTick");
        var add = item.GetAddMethod();
        var remove = item.GetRemoveMethod();
        Console.WriteLine("event basic: " + item.Name + "/" + add.Name + "/" + remove.Name + "/" + (item.GetRaiseMethod() is null) + "/" + item.GetOtherMethods(true).Length);
        Console.WriteLine("event shape: " + (item.EventHandlerType == typeof(Action<int>)) + "/" + item.IsMulticast + "/" + item.IsSpecialName + "/" + (item.Attributes == EventAttributes.None));
        Console.WriteLine("event context: " + (item.DeclaringType == typeof(EventBase)) + "/" + (item.ReflectedType == type) + "/" + ReferenceEquals(add, type.GetMethod("add_BaseTick")) + "/" + ReferenceEquals(remove, type.GetMethod("remove_BaseTick")));
        Console.WriteLine("event identity: " + ReferenceEquals(item, type.GetEvent("BaseTick")) + "/" + (item == type.GetEvent("BaseTick")) + "/" + item.Equals(type.GetEvent("BaseTick")) + "/" + !ReferenceEquals(item, typeof(EventBase).GetEvent("BaseTick")));
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 8; i++)
            _ = type.GetEvent("BaseTick");
        Console.WriteLine("event intern allocation: " + (GC.GetAllocatedBytesForCurrentThread() == before));
        Console.WriteLine("event public order: " + Names(type.GetEvents()));
        Console.WriteLine("event all order: " + Names(type.GetEvents(EveryEvent)));
        Console.WriteLine("event declared order: " + Names(type.GetEvents(EveryEvent | BindingFlags.DeclaredOnly)));
        var secret = type.GetEvent("Secret", EveryEvent);
        Console.WriteLine("event private: " + (secret.GetAddMethod() is null) + "/" + (secret.GetRemoveMethod() is null) + "/" + ReferenceEquals(secret.AddMethod, secret.GetAddMethod(true)) + "/" + ReferenceEquals(secret.RemoveMethod, secret.GetRemoveMethod(true)));
        Console.WriteLine("event hiding: " + (type.GetEvent("Hidden").DeclaringType == type) + "/" + (type.GetEvent("Shadow") is null) + "/" + (type.GetEvent("PrivateTick", EveryEvent) is null));
        Console.WriteLine("event ignore case: " + ReferenceEquals(item, type.GetEvent("basetick", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)) + "/" + type.GetMember("baset*", MemberTypes.Event, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).Length + "/" + Fault(() => typeof(EventCase).GetEvent("TICK", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)));
        Console.WriteLine("event unicode: " + (typeof(EventCase).GetEvent("ωμέγα", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).Name == "Ωμέγα") + "/" + typeof(EventCase).GetMember("ωμ*", MemberTypes.Event, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase).Length);
        Console.WriteLine("event member: " + (item.MemberType == MemberTypes.Event) + "/" + (item.MetadataToken >> 24 == 20) + "/" + (item.Module == typeof(EventBase).Module) + "/" + ReferenceEquals(item, type.GetMember("BaseTick", MemberTypes.Event, EveryEvent)[0]) + "/" + ReferenceEquals(item, type.GetMemberWithSameMetadataDefinitionAs(typeof(EventBase).GetEvent("BaseTick"))));
        Console.WriteLine("event mixed kind: " + string.Join(",", type.GetMembers(EveryEvent | BindingFlags.DeclaredOnly).Select(member => member.MemberType).Distinct()));
        var marks = item.GetCustomAttributes(typeof(EventMarkAttribute), false);
        Console.WriteLine("event attributes: " + ((EventMarkAttribute)marks[0]).Text + "/" + item.IsDefined(typeof(EventMarkAttribute), false) + "/" + (item.GetCustomAttributesData()[0].AttributeType == typeof(EventMarkAttribute)));
        Console.WriteLine("event display: " + item);
        int sum = 0;
        Action<int> handler = value => sum += value;
        var target = new EventDerived();
        item.AddEventHandler(target, handler);
        target.Raise(3);
        item.RemoveEventHandler(target, handler);
        target.Raise(5);
        var shared = typeof(EventBase).GetEvent("Shared");
        shared.AddEventHandler(null, handler);
        target.Raise(7);
        shared.RemoveEventHandler(null, handler);
        Console.WriteLine("event invocation: " + sum);
        Console.WriteLine("event invocation guards: " + Fault(() => item.AddEventHandler(null, handler)) + "/" + Fault(() => item.RemoveEventHandler(new object(), handler)) + "/" + Fault(() => item.AddEventHandler(target, (Action)(() => { }))) + "/" + Fault(() => secret.AddEventHandler(target, handler)));
        var exotic = typeof(ExoticEventOwner).GetEvent("Exotic");
        var allOthers = exotic.GetOtherMethods(true);
        var publicOthers = exotic.GetOtherMethods();
        Console.WriteLine("event exotic: " + exotic.IsSpecialName + "/" + (exotic.GetRaiseMethod() is null) + "/" + ReferenceEquals(exotic.RaiseMethod, exotic.GetRaiseMethod(true)) + "/" + MethodNames(publicOthers) + "/" + MethodNames(allOthers));
        Console.WriteLine("event exotic identity: " + ReferenceEquals(exotic.RaiseMethod, typeof(ExoticEventOwner).GetMethod("RaiseExotic", EveryEvent)) + "/" + ReferenceEquals(allOthers[0], typeof(ExoticEventOwner).GetMethod("OtherPublic")) + "/" + ReferenceEquals(allOthers[1], typeof(ExoticEventOwner).GetMethod("OtherPrivate", EveryEvent)));
        var inheritedExotic = typeof(InheritedExoticEventOwner).GetEvent("Exotic");
        Console.WriteLine("event inherited accessors: " + (inheritedExotic.GetRaiseMethod(true) is null) + "/" + MethodNames(inheritedExotic.GetOtherMethods(true)) + "/" + ReferenceEquals(inheritedExotic.AddMethod, typeof(InheritedExoticEventOwner).GetMethod("add_Exotic")));
        var exoticTarget = new ExoticEventOwner();
        exotic.AddEventHandler(exoticTarget, handler);
        exotic.GetRaiseMethod(true).Invoke(exoticTarget, new object[] { 2 });
        allOthers[0].Invoke(exoticTarget, new object[] { 3 });
        allOthers[1].Invoke(exoticTarget, new object[] { 4 });
        exotic.RemoveEventHandler(exoticTarget, handler);
        Console.WriteLine("event exotic invocation: " + exoticTarget.Count);
        allOthers[0] = null;
        Console.WriteLine("event fresh arrays: " + (exotic.GetOtherMethods(true)[0] is not null) + "/" + !ReferenceEquals(publicOthers, exotic.GetOtherMethods()) + "/" + type.GetEvents().Any(e => ReferenceEquals(e, item)));
        var definition = typeof(GenericEventDerived<>);
        var formal = definition.GetGenericArguments()[0];
        var open = definition.GetEvent("Value");
        var handlerType = typeof(Action<>).MakeGenericType(formal.MakeArrayType());
        Console.WriteLine("event generic shape: " + ReferenceEquals(open.EventHandlerType, handlerType) + "/" + open.EventHandlerType.ContainsGenericParameters + "/" + (open.DeclaringType == definition.BaseType) + "/" + (open.ReflectedType == definition));
        Console.WriteLine("event generic multicast: " + open.IsMulticast);
        Console.WriteLine("event generic accessor: " + ReferenceEquals(open.AddMethod, definition.GetMethod("add_Value")) + "/" + (open.AddMethod.GetParameters()[0].ParameterType == handlerType) + "/" + (open.RemoveMethod.GetParameters()[0].ParameterType == handlerType));
        Console.WriteLine("event generic guards: " + Fault(() => open.AddEventHandler(null, handler)) + "/" + Fault(() => open.RemoveEventHandler(null, handler)) + "/" + Fault(() => open.AddMethod.Invoke(null, new object[] { handler })));
        var bare = typeof(BareEventOwner<>).GetEvent("Only");
        Console.WriteLine("event bare definition: " + (bare.EventHandlerType.GetGenericArguments()[0] == typeof(BareEventOwner<>).GetGenericArguments()[0]) + "/" + ReferenceEquals(bare.AddMethod, typeof(BareEventOwner<>).GetMethod("add_Only")));
        var closed = typeof(GenericEventBase<int>).GetEvent("Value");
        var closedTarget = new GenericEventBase<int>();
        closed.AddEventHandler(closedTarget, handler);
        closedTarget.Raise(11);
        closed.RemoveEventHandler(closedTarget, handler);
        var closedBase = typeof(EventOverClosed<>).GetEvent("BaseTick");
        closedBase.AddEventHandler(target, handler);
        target.Raise(13);
        closedBase.RemoveEventHandler(target, handler);
        Console.WriteLine("event closed operations: " + sum + "/" + (closed.EventHandlerType == typeof(Action<int>)) + "/" + (closedBase.DeclaringType == typeof(EventBase)));
        var cross = (EventInfo)typeof(GenericEventBase<string>).GetMemberWithSameMetadataDefinitionAs(closed);
        Console.WriteLine("event cross owner: " + (cross.EventHandlerType == typeof(Action<string>)) + "/" + ReferenceEquals(cross, typeof(GenericEventBase<string>).GetEvent("Value")));
        _ = new SynthEventOwner<int>();
#if REFLECTED_EVENTS_UNSHARED
        GC.KeepAlive(typeof(SynthEventOwner<Guid>));
#endif
        var synthesized = typeof(SynthEventOwner<>).MakeGenericType(typeof(Guid));
        var synthesizedEvent = synthesized.GetEvent("Ready");
        var synthesizedTarget = Activator.CreateInstance(synthesized);
        synthesizedEvent.AddEventHandler(synthesizedTarget, (Action)(() => { }));
        int added = (int)synthesized.GetField("Count").GetValue(synthesizedTarget);
        synthesizedEvent.RemoveEventHandler(synthesizedTarget, (Action)(() => { }));
        Console.WriteLine("event runtime owner: " + ReferenceEquals(synthesizedEvent.AddMethod, synthesized.GetMethod("add_Ready")) + "/" + (synthesizedEvent.DeclaringType == synthesized) + "/" + (synthesizedEvent.ReflectedType == synthesized) + "/" + added + "/" + synthesized.GetField("Count").GetValue(synthesizedTarget));
        Console.WriteLine("event default member: " + ReferenceEquals(exotic, typeof(ExoticEventOwner).GetDefaultMembers()[0]));
        Console.WriteLine("event null guards: " + Fault(() => type.GetEvent(null)) + "/" + Fault(() => ((EventInfo)null).GetAddMethod()) + "/" + Fault(() => ((EventInfo)null).GetOtherMethods(true)));
        bool parallel = true;
        Parallel.For(0, 12, _ => { if (!ReferenceEquals(item, type.GetEvent("BaseTick")) || !ReferenceEquals(add, item.AddMethod)) parallel = false; });
        if (collect)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Console.WriteLine("event repeated: " + parallel + "/" + ReferenceEquals(item, type.GetEvent("BaseTick")) + "/" + ReferenceEquals(open.EventHandlerType, handlerType));
        RunGenericOtherReflection();
        Console.WriteLine("reflected events end");
    }
}
