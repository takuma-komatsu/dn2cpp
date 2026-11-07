using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace InitializerTriggersOnly;

// Every initializer below but RelaxedUninitialized's is precise (an explicit static
// constructor, so the type is not beforefieldinit) and is triggered by one IL shape that
// names no static field of its type: a run-time root, a base-constructor call, a struct
// constructor or instance method called on a local or through a constrained call, a
// static abstract member's implementation called through a constrained call, and a
// method pointer. A witness is
// read only after its trigger, so it answers the same whether the initializer ran in the
// startup pass or at first use. The generic and Late* witnesses also record whether the
// initializer ran after Main set Registered, which only a first-use guard at the trigger
// satisfies; the Rare* types sit behind a branch that never runs, so they never initialize.
// The last block enters members where no IL call site runs the trigger: generic and
// reflective construction, reflective invocation, a property read, an allocation that
// runs no constructor, a boxed value type's slots and an attribute instance. The
// LoweredEntries block enters value-type members that a lowered call or a runtime slot
// runs: formatting, default equality, hashing and ordering, Array.Initialize, a boxed
// generic virtual method and a field-by-field comparison. The DelegateEntries block
// invokes a delegate bound to a value type's own instance method. The
// StateMachineEntries block drives a state machine through an async builder. The
// RelaxedEntries block allocates a beforefieldinit type without a constructor. The
// ContinuationEntries block enters a state machine's MoveNext through a continuation and
// an awaiter's registration method through a builder's await. The DefaultMethodEntries
// block calls interface default methods, one of them overridden. The StaticVirtualEntries
// block calls static abstract and static virtual members through constrained calls. The
// UninitializedChainEntries block allocates without a constructor types whose base
// chains mix precise-init and beforefieldinit levels.
internal static class Witness
{
    internal static bool Entry;
    internal static bool Base;
    internal static bool Struct;
    internal static bool Method;
    internal static bool Function;
    internal static bool StaticCall;
    internal static bool Untouched;
    internal static bool DefaultStruct;
    internal static bool Registered;
    internal static string? GenericBase;
    internal static string? SharedBase;
    internal static string? GenericStruct;
    internal static string? GenericMethod;
    internal static string? GenericFunction;
    internal static string? SharedFunction;
    internal static string? SharedStaticCall;
    internal static string? Constrained;
    internal static string? StaticVirtual;
    internal static string? ConstrainedText;
    internal static string? ConstrainedEquality;
    internal static string? ConstrainedHash;
    internal static string? LateStaticCall;
    internal static string? LateField;
    internal static string? LateObject;
    internal static string? LateBase;
    internal static string? LateStruct;
    internal static string? LateFunction;
#pragma warning disable CS0649 // never set: the branch it guards must stay untaken
    internal static bool TakeRareBranch;
#pragma warning restore CS0649
    internal static bool RareCall;
    internal static bool RareObject;
    internal static bool RareField;
    internal static string? ActivatedGeneric;
    internal static string? NewConstrained;
    internal static string? ActivatedStruct;
    internal static string? ConstructedByInfo;
    internal static string? ActivatedByType;
    internal static string? InvokedStatic;
    internal static string? ReflectedProperty;
    internal static string? Uninitialized;
    internal static string? BoxedReader;
    internal static string? BoxedText;
    internal static string? StagedAttribute;
    internal static string? BoundGeneric;
    internal static int BoundGenericRuns;
    internal static string? InvokedGeneric;
    internal static int InvokedGenericRuns;
    internal static string? InterpolatedText;
    internal static string? FormattedText;
    internal static string? BuilderText;
    internal static string? DefaultHashed;
    internal static string? DefaultEquated;
    internal static string? HashedKey;
    internal static string? SearchedItem;
    internal static string? ListedItem;
    internal static string? BinaryItem;
    internal static string? SortedItem;
    internal static string? NonGenericItem;
    internal static string? InitializedItem;
    internal static string? EmptyInitialized;
    internal static string? DynamicInitialized;
    internal static string? EmptyDynamic;
    internal static string? TypeOnlyInitialized;
    internal static string? GvmReader;
    internal static string? BoxedHash;
    internal static string? BoxedEquality;
    internal static string? WalkedField;
    internal static string? HashedField;
    internal static string? BoundStructMethod;
    internal static string? BoundGenericStructMethod;
    internal static string? BoundSharedStructMethod;
    internal static string? StartedMachine;
    internal static string? StartedMachineEntry;
    internal static string? IteratedMachine;
    internal static string? IteratedMachineEntry;
    internal static string? RelaxedUninitialized;
    internal static string? ContinuedMachine;
    internal static string? ContinuedMachineEntry;
    internal static string? RegisteredAwaiter;
    internal static string? RegisteredAwaiterEntry;
    internal static string? DefaultMethod;
    internal static string? DefaultMethodEntry;
    internal static string? SharedDefaultMethod;
    internal static string? SharedDefaultMethodEntry;
    internal static string? OverriddenDefault;
    internal static string? StaticAbstractImpl;
    internal static string? StaticAbstractInterface;
    internal static string? InheritedStaticVirtualImpl;
    internal static string? InheritedStaticVirtualInterface;
    internal static string? OverriddenStaticVirtualImpl;
    internal static string? OverriddenStaticVirtualInterface;

    internal static string Stage => Registered ? "registered" : "too-early";

    internal static int RecordRelaxed()
    {
        RelaxedUninitialized = Stage;
        return 32;
    }
}

// Non-generic precise-init types first triggered after Main set Registered. A startup
// run records "too-early"; a run at the trigger records "registered".
internal static class LateStaticCall
{
    static LateStaticCall() => Witness.LateStaticCall = Witness.Stage;

    internal static int Touch() => 11;
}

internal static class LateField
{
    internal static readonly int Value;

    static LateField()
    {
        Witness.LateField = Witness.Stage;
        Value = 12;
    }
}

internal sealed class LateObject
{
    static LateObject() => Witness.LateObject = Witness.Stage;
}

internal class LateBase
{
    static LateBase() => Witness.LateBase = Witness.Stage;
}

internal sealed class LateDerived : LateBase
{
}

internal struct LateStruct
{
    static LateStruct() => Witness.LateStruct = Witness.Stage;

    internal int Read() => 13;
}

internal static class LateFunction
{
    static LateFunction() => Witness.LateFunction = Witness.Stage;

    internal static int Answer() => 14;
}

// Its initializer's output must land between the two lines around its trigger.
internal static class LateAnnouncer
{
    static LateAnnouncer() => Console.WriteLine("late announcer initializer");

    internal static int Touch() => 15;
}

// Reached only from a branch that never runs, so .NET never initializes them.
internal static class RareCall
{
    static RareCall() => Witness.RareCall = true;

    internal static void Touch() { }
}

internal sealed class RareObject
{
    static RareObject() => Witness.RareObject = true;
}

internal static class RareField
{
    internal static readonly int Value;

    static RareField()
    {
        Witness.RareField = true;
        Value = 1;
    }
}

// Entered only where no IL call site runs the initializer, each first after Main set
// Registered.
internal sealed class ActivatedGeneric
{
    static ActivatedGeneric() => Witness.ActivatedGeneric = Witness.Stage;
}

internal sealed class NewConstrained
{
    static NewConstrained() => Witness.NewConstrained = Witness.Stage;
}

internal struct ActivatedStruct
{
    internal int Value;

    static ActivatedStruct() => Witness.ActivatedStruct = Witness.Stage;

    public ActivatedStruct() => Value = 16;
}

internal sealed class ConstructedByInfo
{
    static ConstructedByInfo() => Witness.ConstructedByInfo = Witness.Stage;
}

internal sealed class ActivatedByType
{
    static ActivatedByType() => Witness.ActivatedByType = Witness.Stage;
}

// Whether the failure arrives inside a TargetInvocationException tells an initializer run
// inside the constructor call from one run before it.
internal sealed class ActivationFailure
{
    static ActivationFailure() => throw new InvalidOperationException("activation initializer failed");
}

internal static class InvokedStatic
{
    static InvokedStatic() => Witness.InvokedStatic = Witness.Stage;

    public static int Answer() => 17;
}

internal static class ReflectedProperty
{
    static ReflectedProperty() => Witness.ReflectedProperty = Witness.Stage;

    public static int Value => 18;
}

internal sealed class Uninitialized
{
    static Uninitialized() => Witness.Uninitialized = Witness.Stage;
}

internal struct BoxedReader : IReadable
{
    static BoxedReader() => Witness.BoxedReader = Witness.Stage;

    public int Read() => 19;
}

internal struct BoxedText
{
    static BoxedText() => Witness.BoxedText = Witness.Stage;

    public override string ToString() => "text";
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class StagedAttribute : Attribute
{
    static StagedAttribute() => Witness.StagedAttribute = Witness.Stage;
}

[Staged]
internal sealed class AttributeCarrier
{
}

// Closed generic types whose static method touches no static field, entered by
// reflection. Each counts its initializer runs, so the repeat entry proves it ran once.
internal static class BoundGeneric<T>
{
    static BoundGeneric()
    {
        Witness.BoundGenericRuns++;
        Witness.BoundGeneric = Witness.Stage;
    }

    public static int Answer() => 20;
}

internal static class InvokedGeneric<T>
{
    static InvokedGeneric()
    {
        Witness.InvokedGenericRuns++;
        Witness.InvokedGeneric = Witness.Stage;
    }

    public static int Answer() => 21;
}

// Value types whose members a lowered call or a runtime slot enters, each first after
// Main set Registered.
internal struct InterpolatedText
{
    static InterpolatedText() => Witness.InterpolatedText = Witness.Stage;

    public override string ToString() => "text";
}

internal struct FormattedText : IFormattable
{
    static FormattedText() => Witness.FormattedText = Witness.Stage;

    public string ToString(string? format) => "formatted:" + format;

    public string ToString(string? format, IFormatProvider? provider) => ToString(format);
}

internal struct BuilderText
{
    static BuilderText() => Witness.BuilderText = Witness.Stage;

    public override string ToString() => "builder";
}

internal struct DefaultHashed : IEquatable<DefaultHashed>
{
    static DefaultHashed() => Witness.DefaultHashed = Witness.Stage;

    public bool Equals(DefaultHashed other) => true;

    public override bool Equals(object? other) => other is DefaultHashed;

    public override int GetHashCode() => 26;
}

internal struct DefaultEquated : IEquatable<DefaultEquated>
{
    static DefaultEquated() => Witness.DefaultEquated = Witness.Stage;

    public bool Equals(DefaultEquated other) => true;

    public override bool Equals(object? other) => other is DefaultEquated;

    public override int GetHashCode() => 0;
}

internal struct HashedKey : IEquatable<HashedKey>
{
    static HashedKey() => Witness.HashedKey = Witness.Stage;

    public bool Equals(HashedKey other) => true;

    public override bool Equals(object? other) => other is HashedKey;

    public override int GetHashCode() => 1;
}

internal struct SearchedItem : IEquatable<SearchedItem>
{
    static SearchedItem() => Witness.SearchedItem = Witness.Stage;

    public bool Equals(SearchedItem other) => true;

    public override bool Equals(object? other) => other is SearchedItem;

    public override int GetHashCode() => 2;
}

internal struct ListedItem
{
    static ListedItem() => Witness.ListedItem = Witness.Stage;

    public override bool Equals(object? other) => other is ListedItem;

    public override int GetHashCode() => 3;
}

internal struct BinaryItem : IComparable<BinaryItem>
{
    static BinaryItem() => Witness.BinaryItem = Witness.Stage;

    public int CompareTo(BinaryItem other) => 0;
}

internal struct SortedItem : IComparable<SortedItem>
{
    static SortedItem() => Witness.SortedItem = Witness.Stage;

    public int CompareTo(SortedItem other) => 0;
}

internal struct NonGenericItem
{
    static NonGenericItem() => Witness.NonGenericItem = Witness.Stage;

    public override bool Equals(object? other) => false;

    public override int GetHashCode() => 4;
}

internal struct InitializedItem
{
    internal int Value;

    static InitializedItem() => Witness.InitializedItem = Witness.Stage;

    public InitializedItem() => Value = 22;
}

internal struct EmptyInitialized
{
    internal int Value;

    static EmptyInitialized() => Witness.EmptyInitialized = Witness.Stage;

    public EmptyInitialized() => Value = 1;
}

internal struct DynamicInitialized
{
    internal int Value;

    static DynamicInitialized() => Witness.DynamicInitialized = Witness.Stage;

    public DynamicInitialized() => Value = 23;
}

internal struct EmptyDynamic
{
    internal int Value;

    static EmptyDynamic() => Witness.EmptyDynamic = Witness.Stage;

    public EmptyDynamic() => Value = 1;
}

// Named only by typeof: its constructor row exists, but nothing constructs it.
internal struct TypeOnlyInitialized
{
    internal int Value;

    static TypeOnlyInitialized() => Witness.TypeOnlyInitialized = Witness.Stage;

    public TypeOnlyInitialized() => Value = 1;
}

internal interface IGenericReader
{
    int Read<T>();
}

internal struct GvmReader : IGenericReader
{
    static GvmReader() => Witness.GvmReader = Witness.Stage;

    public int Read<T>() => 24;
}

internal struct BoxedHash
{
    static BoxedHash() => Witness.BoxedHash = Witness.Stage;

    public override bool Equals(object? other) => false;

    public override int GetHashCode() => 25;
}

internal struct BoxedEquality
{
    static BoxedEquality() => Witness.BoxedEquality = Witness.Stage;

    public override bool Equals(object? other) => other is null;

    public override int GetHashCode() => 0;
}

// ValueType.Equals and GetHashCode of the holders run the field's own override.
internal struct WalkedField
{
    static WalkedField() => Witness.WalkedField = Witness.Stage;

    public override bool Equals(object? other) => other is WalkedField;

    public override int GetHashCode() => 0;
}

internal struct WalkedHolder
{
#pragma warning disable CS0649 // only ever default: the comparison reads it
    internal WalkedField Field;
#pragma warning restore CS0649
}

internal struct HashedField
{
    static HashedField() => Witness.HashedField = Witness.Stage;

    public override bool Equals(object? other) => other is HashedField;

    public override int GetHashCode() => 0;
}

internal struct HashedHolder
{
#pragma warning disable CS0649 // only ever default: the comparison reads it
    internal HashedField Field;
#pragma warning restore CS0649
}

internal static class LoweredEntries
{
    internal static void Run()
    {
        InterpolatedText text = default;
        string hole = $"{text}";
        Console.WriteLine($"interpolated hole={hole}/{Witness.InterpolatedText ?? "not-run"}");
        FormattedText formatted = default;
        string formattedHole = $"{formatted:X}";
        Console.WriteLine($"formatted hole={formattedHole}/{Witness.FormattedText ?? "not-run"}");
        BuilderText builderText = default;
        string builderHole = new StringBuilder().Append($"{builderText}").ToString();
        Console.WriteLine($"builder hole={builderHole}/{Witness.BuilderText ?? "not-run"}");
        int defaultHash = EqualityComparer<DefaultHashed>.Default.GetHashCode(default);
        Console.WriteLine($"default comparer GetHashCode={defaultHash}/{Witness.DefaultHashed ?? "not-run"}");
        bool defaultEqual = EqualityComparer<DefaultEquated>.Default.Equals(default, default);
        Console.WriteLine($"default comparer Equals={defaultEqual}/{Witness.DefaultEquated ?? "not-run"}");
        var keys = new HashSet<HashedKey>();
        bool added = keys.Add(default);
        Console.WriteLine($"hashed key={added}/{Witness.HashedKey ?? "not-run"}");
        int found = Array.IndexOf(new SearchedItem[1], default(SearchedItem));
        Console.WriteLine($"array search={found}/{Witness.SearchedItem ?? "not-run"}");
        var listed = new List<ListedItem>(1);
        listed.Add(default);
        Console.WriteLine($"list before search={Witness.ListedItem ?? "not-run"}");
        bool contained = listed.Contains(default);
        Console.WriteLine($"list search={contained}/{Witness.ListedItem ?? "not-run"}");
        int position = Array.BinarySearch(new BinaryItem[1], default(BinaryItem));
        Console.WriteLine($"binary search={position}/{Witness.BinaryItem ?? "not-run"}");
        var sorted = new SortedItem[2];
        Array.Sort(sorted);
        Console.WriteLine($"array sort={sorted.Length}/{Witness.SortedItem ?? "not-run"}");
        int untyped = Array.IndexOf((Array)new NonGenericItem[1], null);
        Console.WriteLine($"non-generic array search={untyped}/{Witness.NonGenericItem ?? "not-run"}");
        var items = new InitializedItem[2];
        Console.WriteLine($"typed array before initialize={Witness.InitializedItem ?? "not-run"}");
        items.Initialize();
        Console.WriteLine($"typed array initialize={items[1].Value}/{Witness.InitializedItem ?? "not-run"}");
        var empty = new EmptyInitialized[0];
        empty.Initialize();
        Console.WriteLine($"empty array initialize={empty.Length}/{Witness.EmptyInitialized ?? "not-run"}");
        var dynamic = new DynamicInitialized[2];
        InitializeAny(dynamic);
        Console.WriteLine($"run-time array initialize={dynamic[1].Value}/{Witness.DynamicInitialized ?? "not-run"}");
        var emptyDynamic = new EmptyDynamic[0];
        InitializeAny(emptyDynamic);
        Console.WriteLine($"empty run-time array initialize={emptyDynamic.Length}/{Witness.EmptyDynamic ?? "not-run"}");
        Console.WriteLine($"type-only constructor={typeof(TypeOnlyInitialized).Name}/{Witness.TypeOnlyInitialized ?? "not-run"}");
        IGenericReader reader = default(GvmReader);
        Console.WriteLine($"boxed generic virtual before call={Witness.GvmReader ?? "not-run"}");
        int read = reader.Read<int>();
        Console.WriteLine($"boxed generic virtual={read}/{Witness.GvmReader ?? "not-run"}");
        object boxedHash = default(BoxedHash);
        int hash = boxedHash.GetHashCode();
        Console.WriteLine($"boxed GetHashCode={hash}/{Witness.BoxedHash ?? "not-run"}");
        object boxedEquality = default(BoxedEquality);
        bool equal = boxedEquality.Equals(null);
        Console.WriteLine($"boxed Equals={equal}/{Witness.BoxedEquality ?? "not-run"}");
        object walked = default(WalkedHolder);
        bool walkedEqual = walked.Equals(default(WalkedHolder));
        Console.WriteLine($"field walk Equals={walkedEqual}/{Witness.WalkedField ?? "not-run"}");
        object hashedHolder = default(HashedHolder);
        _ = hashedHolder.GetHashCode();
        Console.WriteLine($"field walk GetHashCode={Witness.HashedField ?? "not-run"}");
        Console.WriteLine("lowered initializer entries end");
    }

    private static void InitializeAny(Array array) => array.Initialize();
}

internal struct BoundStructMethod
{
    static BoundStructMethod() => Witness.BoundStructMethod = Witness.Stage;

    internal int Read() => 27;
}

internal struct BoundGenericStructMethod<T>
{
    static BoundGenericStructMethod() => Witness.BoundGenericStructMethod = Witness.Stage;

    internal int Read() => 28;
}

internal struct BoundSharedStructMethod<T>
{
    static BoundSharedStructMethod() => Witness.BoundSharedStructMethod = Witness.Stage;

    internal int Read() => 29;
}

// Binding boxes the receiver and takes the method's address; neither triggers, and the
// first invocation does.
internal static class DelegateEntries
{
    internal static void Run()
    {
        BoundStructMethod value = default;
        Func<int> read = value.Read;
        Console.WriteLine($"struct method delegate before call={Witness.BoundStructMethod ?? "not-run"}");
        Console.WriteLine($"struct method delegate={read()}/{Witness.BoundStructMethod ?? "not-run"}");
        BoundGenericStructMethod<int> generic = default;
        Func<int> readGeneric = generic.Read;
        Console.WriteLine($"generic struct method delegate before call={Witness.BoundGenericStructMethod ?? "not-run"}");
        Console.WriteLine($"generic struct method delegate={readGeneric()}/{Witness.BoundGenericStructMethod ?? "not-run"}");
        BoundSharedStructMethod<string> shared = default;
        Func<int> readShared = shared.Read;
        Console.WriteLine($"shared struct method delegate before call={Witness.BoundSharedStructMethod ?? "not-run"}");
        Console.WriteLine($"shared struct method delegate={readShared()}/{Witness.BoundSharedStructMethod ?? "not-run"}");
        Console.WriteLine("delegate initializer entries end");
    }
}

internal struct StartedMachine : IAsyncStateMachine
{
    internal AsyncTaskMethodBuilder Builder;

    static StartedMachine() => Witness.StartedMachine = Witness.Stage;

    internal static int Touch() => 30;

    public void MoveNext()
    {
        Witness.StartedMachineEntry = Witness.StartedMachine ?? "not-run";
        Builder.SetResult();
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }
}

internal struct IteratedMachine : IAsyncStateMachine
{
    static IteratedMachine() => Witness.IteratedMachine = Witness.Stage;

    internal static int Touch() => 31;

    public void MoveNext() => Witness.IteratedMachineEntry = Witness.IteratedMachine ?? "not-run";

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }
}

// The builder enters MoveNext with no IL call site naming it. Each machine's MoveNext
// records whether its initializer ran first; the static call after it is the only other
// trigger.
internal static class StateMachineEntries
{
    internal static void Run()
    {
        StartedMachine started = default;
        started.Builder = AsyncTaskMethodBuilder.Create();
        started.Builder.Start(ref started);
        Console.WriteLine($"async builder Start={Witness.StartedMachineEntry ?? "not-entered"}/{started.Builder.Task.IsCompleted}/{StartedMachine.Touch()}");
        AsyncIteratorMethodBuilder iterator = AsyncIteratorMethodBuilder.Create();
        IteratedMachine iterated = default;
        iterator.MoveNext(ref iterated);
        iterator.Complete();
        Console.WriteLine($"async iterator builder MoveNext={Witness.IteratedMachineEntry ?? "not-entered"}/{IteratedMachine.Touch()}");
        Console.WriteLine("state machine initializer entries end");
    }
}

// beforefieldinit: no explicit static constructor, only a field initializer.
internal sealed class RelaxedUninitialized
{
    internal static readonly int Value = Witness.RecordRelaxed();
}

// GetUninitializedObject runs no initializer of a beforefieldinit type; its first static
// field access does. The access sits in its own method so no compilation of Run can
// initialize the type ahead of it.
internal static class RelaxedEntries
{
    internal static void Run()
    {
        object instance = RuntimeHelpers.GetUninitializedObject(typeof(RelaxedUninitialized));
        Console.WriteLine($"beforefieldinit uninitialized object={instance is RelaxedUninitialized}/{Witness.RelaxedUninitialized ?? "not-run"}");
        Console.WriteLine($"beforefieldinit static field={ReadRelaxed()}/{Witness.RelaxedUninitialized ?? "not-run"}");
        Console.WriteLine("beforefieldinit entries end");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadRelaxed() => RelaxedUninitialized.Value;
}

// Completes inline: registering a continuation runs it at once.
internal struct ImmediateAwaiter : ICriticalNotifyCompletion
{
    public bool IsCompleted => false;

    public void GetResult()
    {
    }

    public void OnCompleted(Action continuation) => continuation();

    public void UnsafeOnCompleted(Action continuation) => continuation();
}

internal struct ContinuedMachine : IAsyncStateMachine
{
    internal AsyncTaskMethodBuilder Builder;

    static ContinuedMachine() => Witness.ContinuedMachine = Witness.Stage;

    internal static int Touch() => 33;

    public void MoveNext() => Witness.ContinuedMachineEntry = Witness.ContinuedMachine ?? "not-run";

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }
}

internal struct RegisteredAwaiter : ICriticalNotifyCompletion
{
    static RegisteredAwaiter() => Witness.RegisteredAwaiter = Witness.Stage;

    internal static int Touch() => 34;

    public bool IsCompleted => false;

    public void GetResult()
    {
    }

    public void OnCompleted(Action continuation) => Witness.RegisteredAwaiterEntry = Witness.RegisteredAwaiter ?? "not-run";

    public void UnsafeOnCompleted(Action continuation) => Witness.RegisteredAwaiterEntry = Witness.RegisteredAwaiter ?? "not-run";
}

internal struct PlainMachine : IAsyncStateMachine
{
    internal AsyncTaskMethodBuilder Builder;

    public void MoveNext()
    {
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }
}

// A builder's await registers a continuation outside any MoveNext: an awaiter that runs
// it inline enters the state machine's MoveNext through the continuation, and an awaiter
// with an initializer is entered by the registration itself. Each records whether its
// initializer ran first; the static call after it is the only other trigger.
internal static class ContinuationEntries
{
    internal static void Run()
    {
        ContinuedMachine continued = default;
        continued.Builder = AsyncTaskMethodBuilder.Create();
        ImmediateAwaiter immediate = default;
        continued.Builder.AwaitUnsafeOnCompleted(ref immediate, ref continued);
        Console.WriteLine($"async continuation MoveNext={Witness.ContinuedMachineEntry ?? "not-entered"}/{ContinuedMachine.Touch()}");
        PlainMachine plain = default;
        plain.Builder = AsyncTaskMethodBuilder.Create();
        RegisteredAwaiter registered = default;
        plain.Builder.AwaitUnsafeOnCompleted(ref registered, ref plain);
        Console.WriteLine($"awaiter registration={Witness.RegisteredAwaiterEntry ?? "not-entered"}/{RegisteredAwaiter.Touch()}");
        Console.WriteLine("continuation initializer entries end");
    }
}

internal interface IDefaultReader
{
    static IDefaultReader() => Witness.DefaultMethod = Witness.Stage;

    internal static int Touch() => 35;

    int Read()
    {
        Witness.DefaultMethodEntry = Witness.DefaultMethod ?? "not-run";
        return 36;
    }
}

internal sealed class DefaultReader : IDefaultReader
{
}

internal interface ISharedDefaultReader<T>
{
    static ISharedDefaultReader() => Witness.SharedDefaultMethod = Witness.Stage;

    internal static int Touch() => 37;

    int Read()
    {
        Witness.SharedDefaultMethodEntry = Witness.SharedDefaultMethod ?? "not-run";
        return 38;
    }
}

internal sealed class SharedDefaultReader<T> : ISharedDefaultReader<T>
{
}

internal interface IOverriddenReader
{
    static IOverriddenReader() => Witness.OverriddenDefault = Witness.Stage;

    internal static int Touch() => 39;

    int Read() => 0;
}

internal sealed class OverridingReader : IOverriddenReader
{
    public int Read() => 40;
}

// Entering an interface's own default method body triggers its initializer, as a value
// type's instance method does; a class's override of the slot does not.
internal static class DefaultMethodEntries
{
    internal static void Run()
    {
        IDefaultReader reader = new DefaultReader();
        Console.WriteLine($"default interface method={reader.Read()}/{Witness.DefaultMethodEntry ?? "not-entered"}/{IDefaultReader.Touch()}");
        ISharedDefaultReader<string> shared = new SharedDefaultReader<string>();
        Console.WriteLine($"shared default interface method={shared.Read()}/{Witness.SharedDefaultMethodEntry ?? "not-entered"}/{ISharedDefaultReader<string>.Touch()}");
        IOverriddenReader overriding = new OverridingReader();
        Console.WriteLine($"overridden default interface method={overriding.Read()}/{Witness.OverriddenDefault ?? "not-run"}/{IOverriddenReader.Touch()}");
        Console.WriteLine("default method initializer entries end");
    }
}

internal interface IAbstractMaker
{
    static IAbstractMaker() => Witness.StaticAbstractInterface = Witness.Stage;

    static abstract int Make();

    internal static int Touch() => 43;
}

internal sealed class AbstractMaker : IAbstractMaker
{
    static AbstractMaker() => Witness.StaticAbstractImpl = Witness.Stage;

    public static int Make() => 41;
}

internal interface IInheritedMaker<TSelf> where TSelf : IInheritedMaker<TSelf>
{
    static IInheritedMaker() => Witness.InheritedStaticVirtualInterface = Witness.Stage;

    static virtual int Make() => 44;

    internal static int Touch() => 45;
}

internal sealed class InheritedMaker : IInheritedMaker<InheritedMaker>
{
    static InheritedMaker() => Witness.InheritedStaticVirtualImpl = Witness.Stage;
}

internal interface IOverriddenMaker<TSelf> where TSelf : IOverriddenMaker<TSelf>
{
    static IOverriddenMaker() => Witness.OverriddenStaticVirtualInterface = Witness.Stage;

    static virtual int Make() => 0;

    internal static int Touch() => 47;
}

internal struct OverridingMaker : IOverriddenMaker<OverridingMaker>
{
    static OverridingMaker() => Witness.OverriddenStaticVirtualImpl = Witness.Stage;

    public static int Make() => 46;
}

// A constrained call to a static virtual member triggers the type whose body runs: the
// implementation's, or the interface's when its default body runs. The interface's own
// static method then triggers it.
internal static class StaticVirtualEntries
{
    private static int Abstract<T>() where T : IAbstractMaker => T.Make();

    private static int Inherited<T>() where T : IInheritedMaker<T> => T.Make();

    private static int Overridden<T>() where T : IOverriddenMaker<T> => T.Make();

    internal static void Run()
    {
        Console.WriteLine($"static abstract implementation={Abstract<AbstractMaker>()}/{Witness.StaticAbstractImpl ?? "not-run"}/{Witness.StaticAbstractInterface ?? "not-run"}/{IAbstractMaker.Touch()}/{Witness.StaticAbstractInterface ?? "not-run"}");
        Console.WriteLine($"inherited static virtual={Inherited<InheritedMaker>()}/{Witness.InheritedStaticVirtualInterface ?? "not-run"}/{Witness.InheritedStaticVirtualImpl ?? "not-run"}/{IInheritedMaker<InheritedMaker>.Touch()}");
        Console.WriteLine($"overridden static virtual={Overridden<OverridingMaker>()}/{Witness.OverriddenStaticVirtualImpl ?? "not-run"}/{Witness.OverriddenStaticVirtualInterface ?? "not-run"}/{IOverriddenMaker<OverridingMaker>.Touch()}/{Witness.OverriddenStaticVirtualInterface ?? "not-run"}");
        Console.WriteLine("static virtual initializer entries end");
    }
}

// Records the initializers the chain types below run, in order.
internal static class ChainLog
{
    private static readonly List<string> Ran = new();

    internal static void Note(string name) => Ran.Add(name);

    internal static int NoteValue(string name)
    {
        Ran.Add(name);
        return 48;
    }

    internal static string Take()
    {
        string ran = Ran.Count == 0 ? "none" : string.Join(",", Ran);
        Ran.Clear();
        return ran;
    }
}

internal class ChainPreciseRoot
{
    static ChainPreciseRoot() => ChainLog.Note(nameof(ChainPreciseRoot));
}

// beforefieldinit: no explicit static constructor.
internal sealed class ChainRelaxedLeaf : ChainPreciseRoot
{
}

internal class ChainPreciseBase
{
    static ChainPreciseBase() => ChainLog.Note(nameof(ChainPreciseBase));
}

internal sealed class ChainPreciseLeaf : ChainPreciseBase
{
    static ChainPreciseLeaf() => ChainLog.Note(nameof(ChainPreciseLeaf));
}

// beforefieldinit: no explicit static constructor, only a field initializer.
internal class ChainRelaxedBase
{
    internal static readonly int Value = ChainLog.NoteValue(nameof(ChainRelaxedBase));
}

internal sealed class ChainOverRelaxed : ChainRelaxedBase
{
    static ChainOverRelaxed() => ChainLog.Note(nameof(ChainOverRelaxed));
}

internal class ChainFirst
{
    static ChainFirst() => ChainLog.Note(nameof(ChainFirst));
}

internal class ChainSecond : ChainFirst
{
    static ChainSecond() => ChainLog.Note(nameof(ChainSecond));
}

internal sealed class ChainThird : ChainSecond
{
    static ChainThird() => ChainLog.Note(nameof(ChainThird));
}

internal class ChainOuterRoot
{
    static ChainOuterRoot() => ChainLog.Note(nameof(ChainOuterRoot));
}

// beforefieldinit: no explicit static constructor, only a field initializer.
internal class ChainRelaxedMiddle : ChainOuterRoot
{
    internal static readonly int Value = ChainLog.NoteValue(nameof(ChainRelaxedMiddle));
}

internal sealed class ChainOuterLeaf : ChainRelaxedMiddle
{
    static ChainOuterLeaf() => ChainLog.Note(nameof(ChainOuterLeaf));
}

// GetUninitializedObject runs every precise-init initializer on the allocated type's base
// chain, the allocated type's first, and skips a beforefieldinit level. Each line names
// the initializers one allocation ran, in order. The beforefieldinit base's field access
// sits in its own method so no compilation of Run can initialize the type ahead of it.
internal static class UninitializedChainEntries
{
    internal static void Run()
    {
        Console.WriteLine($"uninitialized beforefieldinit leaf={RuntimeHelpers.GetUninitializedObject(typeof(ChainRelaxedLeaf)) is ChainRelaxedLeaf}/{ChainLog.Take()}");
        Console.WriteLine($"uninitialized beforefieldinit leaf again={RuntimeHelpers.GetUninitializedObject(typeof(ChainRelaxedLeaf)) is ChainRelaxedLeaf}/{ChainLog.Take()}");
        Console.WriteLine($"uninitialized precise leaf={RuntimeHelpers.GetUninitializedObject(typeof(ChainPreciseLeaf)) is ChainPreciseLeaf}/{ChainLog.Take()}");
        Console.WriteLine($"uninitialized over beforefieldinit base={RuntimeHelpers.GetUninitializedObject(typeof(ChainOverRelaxed)) is ChainOverRelaxed}/{ChainLog.Take()}");
        Console.WriteLine($"beforefieldinit base static field={ReadRelaxedBase()}/{ChainLog.Take()}");
        Console.WriteLine($"uninitialized three precise levels={RuntimeHelpers.GetUninitializedObject(typeof(ChainThird)) is ChainThird}/{ChainLog.Take()}");
        Console.WriteLine($"uninitialized over beforefieldinit middle={RuntimeHelpers.GetUninitializedObject(typeof(ChainOuterLeaf)) is ChainOuterLeaf}/{ChainLog.Take()}");
        Console.WriteLine("uninitialized chain entries end");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadRelaxedBase() => ChainRelaxedBase.Value;
}

internal static class Factory
{
    internal static T Make<T>() where T : new() => new T();
}

internal class Base
{
    static Base() => Witness.Base = true;
}

internal sealed class Derived : Base
{
}

internal struct Built
{
    internal int Value;

    static Built() => Witness.Struct = true;

    internal Built(int value) => Value = value;
}

internal struct MethodOnly
{
    static MethodOnly() => Witness.Method = true;

    internal int Read() => 1;
}

internal struct GenericMethodOnly<T>
{
    static GenericMethodOnly() => Witness.GenericMethod = Witness.Stage;

    internal int Read() => 2;
}

internal static class FunctionOwner
{
    static FunctionOwner() => Witness.Function = true;

    internal static int Answer() => 42;
}

internal static class StaticCallOwner
{
    static StaticCallOwner() => Witness.StaticCall = true;

    internal static int Touch() => 1;
}

internal static class Untouched
{
    static Untouched() => Witness.Untouched = true;
}

internal struct DefaultOnly
{
#pragma warning disable CS0649 // only ever default: initobj and ldfld are not triggers
    internal int Value;
#pragma warning restore CS0649

    static DefaultOnly() => Witness.DefaultStruct = true;
}

internal class GenericBase<T>
{
    static GenericBase() => Witness.GenericBase = Witness.Stage;
}

internal sealed class FromGeneric : GenericBase<int>
{
}

internal class SharedBase<T>
{
    static SharedBase() => Witness.SharedBase = Witness.Stage;
}

internal sealed class SharedDerived<T> : SharedBase<T>
{
}

internal struct GenericBuilt<T>
{
    internal T Value;

    static GenericBuilt() => Witness.GenericStruct = Witness.Stage;

    internal GenericBuilt(T value) => Value = value;
}

internal static class GenericFunction<T>
{
    static GenericFunction() => Witness.GenericFunction = Witness.Stage;

    internal static int Answer() => 7;
}

internal static class SharedFunction<T>
{
    static SharedFunction() => Witness.SharedFunction = Witness.Stage;

    internal static int Answer() => 8;
}

internal static class SharedStaticCall<T>
{
    static SharedStaticCall() => Witness.SharedStaticCall = Witness.Stage;

    internal static int Touch() => 9;
}

internal interface IReadable
{
    int Read();
}

internal struct ConstrainedOnly<T> : IReadable
{
    static ConstrainedOnly() => Witness.Constrained = Witness.Stage;

    public int Read() => 3;
}

internal interface ICreatable
{
    static abstract int Create();
}

internal sealed class StaticVirtualOwner<T> : ICreatable
{
    static StaticVirtualOwner() => Witness.StaticVirtual = Witness.Stage;

    public static int Create() => 4;
}

// Each overrides one System.Object virtual that a constrained call through an
// unconstrained type parameter runs; the other overrides are never called.
internal struct ConstrainedText<T>
{
    static ConstrainedText() => Witness.ConstrainedText = Witness.Stage;

    public override string ToString() => "text";
}

internal struct ConstrainedEquality<T>
{
    static ConstrainedEquality() => Witness.ConstrainedEquality = Witness.Stage;

    public override bool Equals(object? other) => other is null;

    public override int GetHashCode() => 0;
}

internal struct ConstrainedHash<T>
{
    static ConstrainedHash() => Witness.ConstrainedHash = Witness.Stage;

    public override bool Equals(object? other) => false;

    public override int GetHashCode() => 6;
}

internal static class ConstrainedCalls
{
    internal static int Read<U>(U value) where U : IReadable => value.Read();

    internal static int Create<U>() where U : ICreatable => U.Create();

    internal static string? Text<U>(U value) => value!.ToString();

    internal static bool Same<U>(U value, object? other) => value!.Equals(other);

    internal static int Hash<U>(U value) => value!.GetHashCode();
}

internal static class SharedFunctionFactory
{
    internal static Func<int> Of<T>() => SharedFunction<T>.Answer;

    internal static int Call<T>() => SharedStaticCall<T>.Touch();
}

// Main is the only static member: nothing but the entry-point root triggers Program.
internal static class Program
{
    static Program() => Witness.Entry = true;

    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine($"entry initializer before Main={Witness.Entry}");

        _ = new Derived();
        Console.WriteLine($"base constructor call={Witness.Base}");

        Built built = new Built(5);
        Console.WriteLine($"struct constructor call={built.Value}/{Witness.Struct}");

        MethodOnly method = default;
        Console.WriteLine($"struct instance method={method.Read()}/{Witness.Method}");

        Func<int> function = FunctionOwner.Answer;
        Console.WriteLine($"method pointer={function()}/{Witness.Function}");

        Console.WriteLine($"static call={StaticCallOwner.Touch()}/{Witness.StaticCall}");

        Console.WriteLine($"typeof only={typeof(Untouched).Name}/{Witness.Untouched}");
        DefaultOnly empty = default;
        Console.WriteLine($"default struct={empty.Value}/{Witness.DefaultStruct}");

        Witness.Registered = true;
        _ = new FromGeneric();
        Console.WriteLine($"generic base constructor call={Witness.GenericBase ?? "not-run"}");
        _ = new SharedDerived<string>();
        Console.WriteLine($"shared base constructor call={Witness.SharedBase ?? "not-run"}");
        GenericBuilt<int> generic = new GenericBuilt<int>(3);
        Console.WriteLine($"generic struct constructor call={generic.Value}/{Witness.GenericStruct ?? "not-run"}");
        GenericMethodOnly<int> genericMethod = default;
        Console.WriteLine($"generic struct instance method={genericMethod.Read()}/{Witness.GenericMethod ?? "not-run"}");
        Func<int> genericFunction = GenericFunction<int>.Answer;
        Console.WriteLine($"generic method pointer={genericFunction()}/{Witness.GenericFunction ?? "not-run"}");
        Func<int> sharedFunction = SharedFunctionFactory.Of<string>();
        Console.WriteLine($"shared method pointer={sharedFunction()}/{Witness.SharedFunction ?? "not-run"}");
        Console.WriteLine($"shared static call={SharedFunctionFactory.Call<string>()}/{Witness.SharedStaticCall ?? "not-run"}");
        ConstrainedOnly<int> constrained = default;
        Console.WriteLine($"constrained struct method={ConstrainedCalls.Read(constrained)}/{Witness.Constrained ?? "not-run"}");
        Console.WriteLine($"constrained static method={ConstrainedCalls.Create<StaticVirtualOwner<int>>()}/{Witness.StaticVirtual ?? "not-run"}");
        Console.WriteLine($"constrained struct ToString={ConstrainedCalls.Text(new ConstrainedText<int>())}/{Witness.ConstrainedText ?? "not-run"}");
        Console.WriteLine($"constrained struct Equals={ConstrainedCalls.Same(new ConstrainedEquality<int>(), null)}/{Witness.ConstrainedEquality ?? "not-run"}");
        Console.WriteLine($"constrained struct GetHashCode={ConstrainedCalls.Hash(new ConstrainedHash<int>())}/{Witness.ConstrainedHash ?? "not-run"}");

        Console.WriteLine("initializer triggers end");

        Console.WriteLine($"late initializer before its trigger={Witness.LateStaticCall ?? "not-run"}");
        Console.WriteLine($"late static call={LateStaticCall.Touch()}/{Witness.LateStaticCall ?? "not-run"}");
        Console.WriteLine($"late static field={LateField.Value}/{Witness.LateField ?? "not-run"}");
        _ = new LateObject();
        Console.WriteLine($"late constructor call={Witness.LateObject ?? "not-run"}");
        _ = new LateDerived();
        Console.WriteLine($"late base constructor call={Witness.LateBase ?? "not-run"}");
        LateStruct late = default;
        Console.WriteLine($"late struct instance method={late.Read()}/{Witness.LateStruct ?? "not-run"}");
        Func<int> lateFunction = LateFunction.Answer;
        Console.WriteLine($"late method pointer={lateFunction()}/{Witness.LateFunction ?? "not-run"}");
        Console.WriteLine("late announcer trigger next");
        Console.WriteLine($"late announcer touch={LateAnnouncer.Touch()}");

        if (Witness.TakeRareBranch)
        {
            RareCall.Touch();
            _ = new RareObject();
            _ = RareField.Value;
        }
        Console.WriteLine($"untaken branch initializers={Witness.RareCall}/{Witness.RareObject}/{Witness.RareField}");
        Console.WriteLine("late initializers end");

        Console.WriteLine($"generic activation={Activator.CreateInstance<ActivatedGeneric>() is not null}/{Witness.ActivatedGeneric ?? "not-run"}");
        Console.WriteLine($"new constraint={Factory.Make<NewConstrained>() is not null}/{Witness.NewConstrained ?? "not-run"}");
        Console.WriteLine($"generic struct activation={Activator.CreateInstance<ActivatedStruct>().Value}/{Witness.ActivatedStruct ?? "not-run"}");
        ConstructorInfo constructor = typeof(ConstructedByInfo).GetConstructor(Type.EmptyTypes)!;
        Console.WriteLine($"constructor invoke={constructor.Invoke(null) is ConstructedByInfo}/{Witness.ConstructedByInfo ?? "not-run"}");
        Console.WriteLine($"type activation={Activator.CreateInstance(typeof(ActivatedByType)) is ActivatedByType}/{Witness.ActivatedByType ?? "not-run"}");
        for (int i = 0; i < 2; i++)
        {
            try
            {
                Console.WriteLine($"type activation failure{i}=returned:{Activator.CreateInstance(typeof(ActivationFailure)) is not null}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"type activation failure{i}=wrapped:{ex is TargetInvocationException}/{Root(ex)}");
            }
        }
        Console.WriteLine($"method invoke={typeof(InvokedStatic).GetMethod("Answer")!.Invoke(null, null)}/{Witness.InvokedStatic ?? "not-run"}");
        Console.WriteLine($"static property={typeof(ReflectedProperty).GetProperty("Value")!.GetValue(null)}/{Witness.ReflectedProperty ?? "not-run"}");
        Console.WriteLine($"uninitialized object={RuntimeHelpers.GetUninitializedObject(typeof(Uninitialized)) is Uninitialized}/{Witness.Uninitialized ?? "not-run"}");
        object boxedReader = default(BoxedReader);
        Console.WriteLine($"boxed before call={Witness.BoxedReader ?? "not-run"}");
        Console.WriteLine($"boxed interface call={((IReadable)boxedReader).Read()}/{Witness.BoxedReader ?? "not-run"}");
        object boxedText = default(BoxedText);
        Console.WriteLine($"boxed ToString={boxedText.ToString()}/{Witness.BoxedText ?? "not-run"}");
        Console.WriteLine($"attribute instance={typeof(AttributeCarrier).GetCustomAttributes(false).Length}/{Witness.StagedAttribute ?? "not-run"}");
        var bound = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), typeof(BoundGeneric<int>), "Answer");
        int firstBound = bound();
        int repeatBound = bound();
        Console.WriteLine($"generic delegate by name={firstBound}/{repeatBound}/{Witness.BoundGeneric ?? "not-run"}/runs={Witness.BoundGenericRuns}");
        MethodInfo invoked = typeof(InvokedGeneric<int>).GetMethod("Answer")!;
        object? firstInvoke = invoked.Invoke(null, null);
        object? repeatInvoke = invoked.Invoke(null, null);
        Console.WriteLine($"generic method invoke={firstInvoke}/{repeatInvoke}/{Witness.InvokedGeneric ?? "not-run"}/runs={Witness.InvokedGenericRuns}");
        Console.WriteLine("reflective initializers end");
        LoweredEntries.Run();
        DelegateEntries.Run();
        StateMachineEntries.Run();
        RelaxedEntries.Run();
        ContinuationEntries.Run();
        DefaultMethodEntries.Run();
        StaticVirtualEntries.Run();
        UninitializedChainEntries.Run();
        return 0;
    }

    // .NET wraps a failed initializer in a TypeInitializationException, which dn2cpp does
    // not model; only the wrapping the reflective call itself adds is compared.
    private static string Root(Exception ex)
    {
        if (ex is TargetInvocationException { InnerException: { } inner })
            ex = inner;
        while (ex is TypeInitializationException { InnerException: { } cause })
            ex = cause;
        return ex.GetType().Name + ":" + ex.Message;
    }
}
