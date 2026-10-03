#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DynamicArrayNullEqualitySubset;

// Non-generic search must dispatch equality for the value type selected by a
// runtime Type, including one returned by reflection or boxed by framework IL.

internal struct DirectDynamicMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct DiamondMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ConditionalFirstMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ConditionalSecondMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct IndexedSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct IndexedUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct TypeOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct TypeOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct TypeSnapshotSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct TypeSnapshotUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct SignatureSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct SignatureUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct RuntimeSignatureSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct RuntimeSignatureUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct FieldMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct HiddenMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct FieldStoredMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct InstanceSnapshotLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct InstanceSnapshotDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct StaticSnapshotLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct StaticSnapshotDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct HelperSnapshotLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct HelperSnapshotDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct ArrayGetHolderSelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ArrayGetHolderUnsearchedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct ArraySetValueBoxMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct ArrayCloneMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ByRefArrayMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ArrayCopyBoxMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct ArrayCopyToBoxMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct RejectedConstrainedCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RejectedArrayCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RejectedArrayCopyToMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ConstrainedCopyBoxMatch
{
    internal static int Calls;
    internal readonly int Value;
    internal ConstrainedCopyBoxMatch(int value) => Value = value;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is ConstrainedCopyBoxMatch value && Value == value.Value;
    }
    public override int GetHashCode() => Value;
}

internal struct ZeroLengthArrayCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RankRejectedArrayCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RejectedValueArrayCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RejectedCreatedArrayCopyMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ByRefReplacedConstrainedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct BeforeByRefWriteMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct AfterByRefWriteMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct BeforeNonVoidByRefWriteMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct AfterNonVoidByRefWriteMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ByRefReturnMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ByRefReturnWrittenMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ByRefReturnBeforeMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct GenericRefBeforeSelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct GenericRefBeforeUnsearchedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct GenericRefAfterSelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct GenericRefAfterUnsearchedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct SignaturePairSelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct SignaturePairUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct HiddenMethodLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct HiddenMethodDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct IndexerSelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct IndexerUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct AritySelectedMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ArityUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct HiddenReflectedFieldLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct HiddenReflectedFieldDeadMatch
{
    public override bool Equals(object? other) => false;
    public override int GetHashCode() => 0;
}

internal struct HelperArrayCopyMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct HelperArrayCopyToMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return false;
    }
    public override int GetHashCode() => 0;
}

internal struct RefWriteMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct RefFieldWriteMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct RefStaticWriteMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ArrayGetTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct MakeArrayTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ObjectGetTypeBoxMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct NestedArrayRuntimeMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct RankedTypeArrayMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct HelperRankedTypeArrayMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct RepeatedRankedTypeArrayMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct RefStobjMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }
    public override int GetHashCode() => 0;
}

internal struct RefStobjBeforeMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct RefLdobjMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }
    public override int GetHashCode() => 0;
}

internal struct CopyShapeBoxMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct MdGetTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct TypeHandleMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }
    public override int GetHashCode() => 0;
}

internal struct ReflectedSnapshotLiveMatch
{
    internal static int Calls;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }
    public override int GetHashCode() => 0;
}

internal struct ReflectedSnapshotDeadMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct StaticErasedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StaticDeclaredMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StaticOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StaticOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct GenericStaticFirstMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct GenericStaticSecondMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct CctorStaticMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ConstructorMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ConstructorOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ConstructorOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct CctorOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct CctorOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct FieldAliasMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct UnselectedFieldStoredMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct OverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct OverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct FieldVirtualMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct StructFieldMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct BoxOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }

    public override int GetHashCode() => 0;
}

internal struct BoxOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct PropertyMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct SetterOnlyMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct MixedVisibilityMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct MethodMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct GenericArgumentMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct GenericSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct GenericUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct InterfaceMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct OverloadSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct OverloadUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct EmptySignatureSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct EmptySignatureUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct NonzeroOverloadMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct ZeroOverloadUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct LocalSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct HelperSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct SinkHelperMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct CrossHelperMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct DeepHelperMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => 0;
}

internal struct SharedGenericFirstMatch
{
    internal static int Calls;
    private int _value;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => _value;
}

internal struct SharedGenericSecondMatch
{
    internal static int Calls;
    private int _value;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is null;
    }

    public override int GetHashCode() => _value;
}

internal enum FirstSearchTag { Value }
internal enum SecondSearchTag { Value }

internal sealed class SharedArraySearcher<TTag> where TTag : struct
{
    internal int Search(Array values) => Array.IndexOf(values, null);
}

internal struct UnselectedLocalMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct UnselectedHelperMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct UnallocatedProviderMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct FrameworkBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal FrameworkBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is FrameworkBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct FrameworkListBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal FrameworkListBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is FrameworkListBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct FrameworkCopyBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal FrameworkCopyBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is FrameworkCopyBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct ReflectedFieldBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal ReflectedFieldBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is ReflectedFieldBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct ObjectFieldBoxMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }

    public override int GetHashCode() => 0;
}

internal struct ReflectedSetBoxMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }

    public override int GetHashCode() => 0;
}

internal struct ReflectedOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }

    public override int GetHashCode() => 0;
}

internal struct ReflectedOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ReflectedSetOverwriteSelectedMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is not null;
    }

    public override int GetHashCode() => 0;
}

internal struct ReflectedSetOverwrittenDeadMatch
{
    public override bool Equals(object? other) => other is not null;
    public override int GetHashCode() => 0;
}

internal struct ReflectedMethodBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal ReflectedMethodBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is ReflectedMethodBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct ReflectedPropertyBoxMatch
{
    internal static int Calls;
    private readonly int _id;

    internal ReflectedPropertyBoxMatch(int id) => _id = id;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is ReflectedPropertyBoxMatch match && match._id == _id;
    }

    public override int GetHashCode() => _id;
}

internal struct ArrayGetValueBoxMatch
{
    internal static int Calls;

    public override bool Equals(object? other)
    {
        Calls++;
        return other is ArrayGetValueBoxMatch;
    }

    public override int GetHashCode() => 0;
}

internal struct UnusedFrameworkBoxMatch
{
    private readonly int _id;

    internal UnusedFrameworkBoxMatch(int id) => _id = id;

    public override bool Equals(object? other) =>
        other is UnusedFrameworkBoxMatch match && match._id == _id;

    public override int GetHashCode() => _id;
}

internal struct UnusedNewarrMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct UnusedUserBoxMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal sealed class ReflectedValueHolder
{
    public ReflectedFieldBoxMatch Value = new(13);
}

internal sealed class ReflectedObjectHolder
{
    public object? Value;
}

internal sealed class ReflectedReturnHolder
{
    public ReflectedMethodBoxMatch MethodValue() => new(7);
    public ReflectedPropertyBoxMatch PropertyValue => new(9);
}

internal struct UnusedReflectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal struct PlainMatch
{
    internal int Value;

    internal PlainMatch(int value) => Value = value;
}

[StructLayout(LayoutKind.Sequential)]
internal struct UnsupportedStructuralMatch
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public int[]? Items;
}

internal sealed class ArrayHolder
{
    public FieldMatch[]? FieldValues;
    public Array? StoredValues;
    internal static Array? StoredStaticValues;
    internal static StaticDeclaredMatch[]? TypedStaticValues;
    private HiddenMatch[]? HiddenValues;
    public PropertyMatch[] PropertyValues => null!;
    public SetterOnlyMatch[] SetterValues { set { } }
    public MixedVisibilityMatch[] MixedVisibility { private get; set; } = null!;
    public MethodMatch[] MethodValues() => null!;
    public InterfaceMatch[] InterfaceValues() => null!;
    public OverloadSelectedMatch[] Overloaded() => null!;
    public OverloadUnselectedMatch[] Overloaded(int index) => null!;
    public EmptySignatureSelectedMatch[] EmptySignature() => null!;
    public EmptySignatureUnselectedMatch[] EmptySignature(int index) => null!;
    public ZeroOverloadUnselectedMatch[] NonzeroOverloaded() => null!;
    public NonzeroOverloadMatch[] NonzeroOverloaded(int index) => null!;
    public SignatureSelectedMatch[] SignatureOverloaded(int index) => null!;
    public SignatureUnselectedMatch[] SignatureOverloaded(string text) => null!;
    public RuntimeSignatureUnselectedMatch[] RuntimeSignatureOverloaded(int index) => null!;
    public RuntimeSignatureSelectedMatch[] RuntimeSignatureOverloaded(string text) => null!;
    public SignaturePairSelectedMatch[] PairOverloaded(int first, string second) => null!;
    public SignaturePairSelectedMatch[] PairOverloaded(string first, int second) => null!;
    public SignaturePairUnselectedMatch[] PairOverloaded(int first, int second) => null!;
    internal void TouchHidden() => HiddenValues = null;
}

internal sealed class RefFieldHolder
{
    public Array Values = new object[0];
    public static Array StaticValues = new object[0];
}

internal class HiddenMethodBase
{
    public HiddenMethodDeadMatch[] Method() => null!;
}

internal sealed class HiddenMethodDerived : HiddenMethodBase
{
    public new HiddenMethodLiveMatch[] Method() => null!;
}

internal sealed class IndexedPropertyHolder
{
    public IndexerUnselectedMatch[] this[int index] => null!;
    public IndexerSelectedMatch[] this[string index] => null!;
}

internal sealed class GenericArityHolder
{
    public AritySelectedMatch[] Method() => null!;
    public ArityUnselectedMatch[] Method<T>() => null!;
}

internal class HiddenReflectedFieldBase
{
    public object Value = new HiddenReflectedFieldDeadMatch();
}

internal sealed class HiddenReflectedFieldDerived : HiddenReflectedFieldBase
{
    public new object Value = new HiddenReflectedFieldLiveMatch();
}

internal sealed class UnusedHolder
{
    public UnusedReflectedMatch[]? Values;
}

internal static class StaticInitHolder
{
    internal static Array Values = null!;

    static StaticInitHolder() => Init();

    private static void Init() => Values = new CctorStaticMatch[1];
}

internal sealed class ConstructorInitHolder
{
    internal Array Values;

    internal ConstructorInitHolder() => Values = new ConstructorMatch[1];
}

internal sealed class ConstructorOverwriteHolder
{
    internal Array Values;
    internal int FirstLength;

    internal ConstructorOverwriteHolder()
    {
        Values = new ConstructorOverwrittenDeadMatch[1];
        FirstLength = Values.Length;
        Values = new ConstructorOverwriteSelectedMatch[1];
    }
}

internal static class CctorOverwriteHolder
{
    internal static Array Values;
    internal static int FirstLength;

    static CctorOverwriteHolder()
    {
        Values = new CctorOverwrittenDeadMatch[1];
        FirstLength = Values.Length;
        Values = new CctorOverwriteSelectedMatch[1];
    }
}

internal static class StaticOverwriteHolder
{
    internal static Array Values = null!;
}

internal static class StaticSnapshotHolder
{
    internal static Array Values = null!;
}

internal static class GenericStaticHolder<T> where T : struct
{
    internal static Array Values = null!;
}

internal struct ValueArrayHolder
{
    internal Array Values;
}

internal abstract class ArrayTypeProvider
{
    internal abstract Type ArrayType();
}

internal sealed class PropertyArrayTypeProvider : ArrayTypeProvider
{
    internal override Type ArrayType() =>
        typeof(ArrayHolder).GetProperty(nameof(ArrayHolder.PropertyValues))!.PropertyType;
}

internal sealed class UnallocatedArrayTypeProvider : ArrayTypeProvider
{
    internal override Type ArrayType() => typeof(UnallocatedProviderMatch[]);
}

internal interface IArrayTypeProvider
{
    Type ArrayType();
}

internal sealed class InterfaceArrayTypeProvider : IArrayTypeProvider
{
    public Type ArrayType() =>
        typeof(ArrayHolder).GetMethod(nameof(ArrayHolder.InterfaceValues))!.ReturnType;
}

internal sealed class FieldVirtualArrayTypeProvider : IArrayTypeProvider
{
    public Type ArrayType() => typeof(FieldVirtualMatch[]);
}

internal sealed class FieldVirtualHolder
{
    internal IArrayTypeProvider Provider = null!;
}

internal static class Program
{
    private static Type Diamond0(bool choose) => choose ? Diamond1(true) : Diamond1(false);
    private static Type Diamond1(bool choose) => choose ? Diamond2(true) : Diamond2(false);
    private static Type Diamond2(bool choose) => choose ? Diamond3(true) : Diamond3(false);
    private static Type Diamond3(bool choose) => choose ? Diamond4(true) : Diamond4(false);
    private static Type Diamond4(bool choose) => choose ? Diamond5(true) : Diamond5(false);
    private static Type Diamond5(bool choose) => choose ? Diamond6(true) : Diamond6(false);
    private static Type Diamond6(bool choose) => choose ? Diamond7(true) : Diamond7(false);
    private static Type Diamond7(bool choose) => choose ? Diamond8(true) : Diamond8(false);
    private static Type Diamond8(bool choose) => choose ? Diamond9(true) : Diamond9(false);
    private static Type Diamond9(bool choose) => choose ? Diamond10(true) : Diamond10(false);
    private static Type Diamond10(bool choose) => choose ? Diamond11(true) : Diamond11(false);
    private static Type Diamond11(bool choose) => choose ? Diamond12(true) : Diamond12(false);
    private static Type Diamond12(bool choose) => typeof(DiamondMatch);

    internal static void RunDiamondOnly()
    {
        Console.WriteLine("== array search provenance diamond ==");
        Array values = Array.CreateInstance(Diamond0(true), 1);
        DiamondMatch.Calls = 0;
        Console.WriteLine("diamond=" + Array.IndexOf(values, null) + ":" + DiamondMatch.Calls);
        Console.WriteLine("array search provenance diamond end");
    }

#if ARRAY_UNSUPPORTED_EQUALITY_ONLY
    // Only the refusal build carries this body: a program that runs reflected
    // methods compiles every non-constructor method of its application types.
    internal static void RunUnsupportedEquality()
    {
        Array values = new UnsupportedStructuralMatch[1];
        Console.WriteLine(Array.IndexOf(values, null));
    }
#endif

    private static Type FieldArrayType(object owner) =>
        owner.GetType().GetField(nameof(ArrayHolder.FieldValues))!.FieldType;

    private static Type MethodArrayType() =>
        typeof(ArrayHolder).GetMethod(nameof(ArrayHolder.MethodValues))!.ReturnType;

    private static Array CreateFromElement(Type elementType) => Array.CreateInstance(elementType, 2);

    private static int SearchNull(Array values) => Array.IndexOf(values, null);

    private static int SearchGeneric<T>() where T : struct =>
        Array.IndexOf(Array.CreateInstance(typeof(T), 2), null);

    private static void Store(ArrayHolder owner, Array value) => owner.StoredValues = value;

    private static Array Load(ArrayHolder owner) => owner.StoredValues!;

    private static Type Deep0() => Deep1();
    private static Type Deep1() => Deep2();
    private static Type Deep2() => Deep3();
    private static Type Deep3() => Deep4();
    private static Type Deep4() => Deep5();
    private static Type Deep5() => Deep6();
    private static Type Deep6() => Deep7();
    private static Type Deep7() => Deep8();
    private static Type Deep8() => Deep9();
    private static Type Deep9() => Deep10();
    private static Type Deep10() => Deep11();
    private static Type Deep11() => Deep12();
    private static Type Deep12() => Deep13();
    private static Type Deep13() => typeof(DeepHelperMatch);

    private static string FieldStoredSearch()
    {
        ArrayHolder selected = new();
        selected.StoredValues = new FieldStoredMatch[1];
        ArrayHolder unsearched = new();
        unsearched.StoredValues = new UnselectedFieldStoredMatch[1];
        FieldStoredMatch.Calls = 0;
        return Array.IndexOf(selected.StoredValues!, null) + ":" + FieldStoredMatch.Calls
            + ":" + unsearched.StoredValues!.Length;
    }

    private static string InstanceFieldSnapshotSearch()
    {
        var owner = new ArrayHolder();
        owner.StoredValues = new InstanceSnapshotLiveMatch[1];
        Array saved = owner.StoredValues;
        owner.StoredValues = new InstanceSnapshotDeadMatch[1];
        InstanceSnapshotLiveMatch.Calls = 0;
        return Array.IndexOf(saved, null) + ":" + InstanceSnapshotLiveMatch.Calls
            + ":" + owner.StoredValues.Length;
    }

    private static string StaticFieldSnapshotSearch()
    {
        StaticSnapshotHolder.Values = new StaticSnapshotLiveMatch[1];
        Array saved = StaticSnapshotHolder.Values;
        StaticSnapshotHolder.Values = new StaticSnapshotDeadMatch[1];
        StaticSnapshotLiveMatch.Calls = 0;
        return Array.IndexOf(saved, null) + ":" + StaticSnapshotLiveMatch.Calls
            + ":" + StaticSnapshotHolder.Values.Length;
    }

    private static string HelperFieldSnapshotSearch()
    {
        var owner = new ArrayHolder();
        Store(owner, new HelperSnapshotLiveMatch[1]);
        Array saved = Load(owner);
        Store(owner, new HelperSnapshotDeadMatch[1]);
        HelperSnapshotLiveMatch.Calls = 0;
        return Array.IndexOf(saved, null) + ":" + HelperSnapshotLiveMatch.Calls
            + ":" + owner.StoredValues!.Length;
    }

    private static string ReflectedFieldSnapshotSearch()
    {
        var owner = new ReflectedObjectHolder();
        var field = typeof(ReflectedObjectHolder).GetField(nameof(ReflectedObjectHolder.Value))!;
        object sought = new();
        owner.Value = new ReflectedSnapshotLiveMatch();
        object saved = field.GetValue(owner)!;
        owner.Value = new ReflectedSnapshotDeadMatch();
        bool laterWrite = field.GetValue(owner) is ReflectedSnapshotDeadMatch;
        Array values = new object[] { saved };
        ReflectedSnapshotLiveMatch.Calls = 0;
        return Array.IndexOf(values, sought) + ":" + ReflectedSnapshotLiveMatch.Calls
            + ":" + laterWrite;
    }

    private static string ArrayGetHolderAliasSearch()
    {
        var owner = new ArrayHolder();
        owner.StoredValues = new ArrayGetHolderUnsearchedMatch[1];
        int firstLength = owner.StoredValues.Length;
        Array owners = new ArrayHolder[] { owner };
        var throughArray = (ArrayHolder)owners.GetValue(0)!;
        throughArray.StoredValues = new ArrayGetHolderSelectedMatch[1];
        ArrayGetHolderSelectedMatch.Calls = 0;
        return Array.IndexOf(owner.StoredValues, null) + ":" + ArrayGetHolderSelectedMatch.Calls
            + ":" + firstLength + ":" + ReferenceEquals(owner, throughArray);
    }

    private static string SharedDonorSearch()
    {
        SharedGenericFirstMatch.Calls = 0;
        SharedGenericSecondMatch.Calls = 0;
        int first = new SharedArraySearcher<FirstSearchTag>()
            .Search(new SharedGenericFirstMatch[1]);
        int second = new SharedArraySearcher<SecondSearchTag>()
            .Search(new SharedGenericSecondMatch[1]);
        return first + ":" + SharedGenericFirstMatch.Calls + ","
            + second + ":" + SharedGenericSecondMatch.Calls;
    }

    private static string ArraySetValueBoxSearch()
    {
        Array values = (Array)new object[1];
        values.SetValue(new ArraySetValueBoxMatch(), 0);
        ArraySetValueBoxMatch.Calls = 0;
        return Array.IndexOf(values, new object()) + ":" + ArraySetValueBoxMatch.Calls;
    }

    private static string ArrayCloneSearch()
    {
        Array source = new ArrayCloneMatch[1];
        Array values = (Array)source.Clone();
        ArrayCloneMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + ArrayCloneMatch.Calls;
    }

    private static int SearchByRef(ref Array values) => Array.IndexOf(values, null);

    private static string ByRefArraySearch()
    {
        Array values = new ByRefArrayMatch[1];
        ByRefArrayMatch.Calls = 0;
        return SearchByRef(ref values) + ":" + ByRefArrayMatch.Calls;
    }

    private static string ArrayCopyBoxSearch()
    {
        Array source = new ArrayCopyBoxMatch[1];
        object[] values = new object[1];
        Array.Copy(source, values, 1);
        ArrayCopyBoxMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":" + ArrayCopyBoxMatch.Calls;
    }

    private static string ArrayCopyToBoxSearch()
    {
        Array source = new ArrayCopyToBoxMatch[1];
        object[] values = new object[1];
        source.CopyTo(values, 0);
        ArrayCopyToBoxMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":" + ArrayCopyToBoxMatch.Calls;
    }

    private static void RejectConstrainedCopy(object[] values)
    {
        Array source = new RejectedConstrainedCopyMatch[1];
        Array.ConstrainedCopy(source, 0, values, 0, 1);
    }

    private static string RejectedConstrainedCopySearch()
    {
        object[] values = new object[1];
        string fault = "none";
        try { RejectConstrainedCopy(values); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, new object())
            + ":" + (values[0] is null);
    }

    private static void RejectArrayCopy(string[] values)
    {
        Array source = new RejectedArrayCopyMatch[1];
        Array.Copy(source, values, 1);
    }

    private static string RejectedArrayCopySearch()
    {
        string[] values = new string[1];
        string fault = "none";
        try { RejectArrayCopy(values); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, null)
            + ":" + (values[0] is null);
    }

    private static void RejectArrayCopyTo(string[] values)
    {
        Array source = new RejectedArrayCopyToMatch[1];
        source.CopyTo(values, 0);
    }

    private static string RejectedArrayCopyToSearch()
    {
        string[] values = new string[1];
        string fault = "none";
        try { RejectArrayCopyTo(values); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, null)
            + ":" + (values[0] is null);
    }

    private static string ConstrainedCopyBoxSearch()
    {
        ICollection source = new LinkedList<ConstrainedCopyBoxMatch>(
            new[] { new ConstrainedCopyBoxMatch(19) });
        object?[] first = new object?[1], second = new object?[1], values = new object?[1];
        source.CopyTo(first, 0);
        source.CopyTo(second, 0);
        Array.ConstrainedCopy(first, 0, values, 0, 1);
        ConstrainedCopyBoxMatch.Calls = 0;
        return Array.IndexOf((Array)values, second[0]) + ":"
            + ConstrainedCopyBoxMatch.Calls;
    }

    private static string ZeroLengthArrayCopySearch()
    {
        Array source = new ZeroLengthArrayCopyMatch[1];
        object?[] values = new object?[1];
        Array.Copy(source, values, 0);
        return Array.IndexOf((Array)values, new object()) + ":"
            + (values[0] is null);
    }

    private static string RankRejectedArrayCopySearch()
    {
        RankRejectedArrayCopyMatch[,] source = new RankRejectedArrayCopyMatch[1, 1];
        object?[] values = new object?[1];
        string fault = "none";
        try { Array.Copy(source, values, 1); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, new object())
            + ":" + (values[0] is null);
    }

    private static void RejectValueArrayCopy(int[] values)
    {
        Array source = new RejectedValueArrayCopyMatch[1];
        Array.Copy(source, values, 1);
    }

    private static string RejectedValueArrayCopySearch()
    {
        int[] values = new int[1];
        string fault = "none";
        try { RejectValueArrayCopy(values); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, null)
            + ":" + values[0];
    }

    private static void RejectCreatedArrayCopy(string[] values)
    {
        Array source = Array.CreateInstance(typeof(RejectedCreatedArrayCopyMatch), 1);
        Array.Copy(source, values, 1);
    }

    private static string RejectedCreatedArrayCopySearch()
    {
        string[] values = new string[1];
        string fault = "none";
        try { RejectCreatedArrayCopy(values); }
        catch (Exception ex) { fault = ex.GetType().Name; }
        return fault + ":" + Array.IndexOf((Array)values, null)
            + ":" + (values[0] is null);
    }

    private static void ReplaceCopySource(ref Array source) =>
        source = new object[] { new ByRefReplacedConstrainedMatch() };

    private static string ByRefReplacedConstrainedCopySearch()
    {
        Array source = new RejectedConstrainedCopyMatch[1];
        ReplaceCopySource(ref source);
        object[] values = new object[1];
        Array.ConstrainedCopy(source, 0, values, 0, 1);
        ByRefReplacedConstrainedMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":"
            + ByRefReplacedConstrainedMatch.Calls;
    }

    private static void ReplaceAfterSearch(ref Array source) =>
        source = new object[] { new AfterByRefWriteMatch() };

    private static string BeforeByRefWriteSearch()
    {
        Array source = new object[] { new BeforeByRefWriteMatch() };
        int found = Array.IndexOf(source, new object());
        ReplaceAfterSearch(ref source);
        return found + ":" + source.Length;
    }

    private static int ReplaceNonVoidSource(ref Array source)
    {
        source = new object[] { new AfterNonVoidByRefWriteMatch() };
        return 7;
    }

    private static string NonVoidByRefWriteSearch()
    {
        Array source = new BeforeNonVoidByRefWriteMatch[1];
        int result = ReplaceNonVoidSource(ref source);
        return result + ":" + Array.IndexOf(source, new object());
    }

    private static object[] ReplaceAndReturn(object[] returned, ref Array source)
    {
        source = new object[] { new ByRefReturnWrittenMatch() };
        return returned;
    }

    private static string NonVoidByRefReturnSearch()
    {
        Array source = new ByRefReturnBeforeMatch[1];
        object[] returned = new object[] { new ByRefReturnMatch() };
        object[] received = ReplaceAndReturn(returned, ref source);
        return Array.IndexOf((Array)received, new object()) + ":" + source.Length;
    }

    private static string GenericRefBeforeSearch()
    {
        Array source = new GenericRefBeforeSelectedMatch[1];
        GenericRefBeforeSelectedMatch.Calls = 0;
        int found = Array.IndexOf(source, null);
        AssignThroughRef(ref source, (Array)new GenericRefAfterUnsearchedMatch[1]);
        return found + ":" + GenericRefBeforeSelectedMatch.Calls;
    }

    private static string GenericRefAfterSearch()
    {
        Array source = new GenericRefBeforeUnsearchedMatch[1];
        AssignThroughRef(ref source, (Array)new GenericRefAfterSelectedMatch[1]);
        GenericRefAfterSelectedMatch.Calls = 0;
        return Array.IndexOf(source, null) + ":" + GenericRefAfterSelectedMatch.Calls;
    }

    private static string EmptyTypeSignatureSearch()
    {
        var holder = new ArrayHolder();
        _ = holder.EmptySignature();
        _ = holder.EmptySignature(0);
        Type arrayType = typeof(ArrayHolder)
            .GetMethod(nameof(ArrayHolder.EmptySignature), new Type[0])!.ReturnType;
        Array selected = Array.CreateInstanceFromArrayType(arrayType, 1);
        EmptySignatureSelectedMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + EmptySignatureSelectedMatch.Calls;
    }

    private static string SignaturePairSearch(bool reverse)
    {
        var holder = new ArrayHolder();
        _ = holder.PairOverloaded(0, "");
        _ = holder.PairOverloaded("", 0);
        _ = holder.PairOverloaded(0, 0);
        var signature = new Type[2];
        if (reverse)
        {
            signature[0] = typeof(string);
            signature[1] = typeof(int);
        }
        else
        {
            signature[0] = typeof(int);
            signature[1] = typeof(string);
        }
        Type arrayType = typeof(ArrayHolder)
            .GetMethod(nameof(ArrayHolder.PairOverloaded), signature)!.ReturnType;
        Array values = Array.CreateInstanceFromArrayType(arrayType, 1);
        SignaturePairSelectedMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + SignaturePairSelectedMatch.Calls;
    }

    private static string HiddenMethodSearch()
    {
        var owner = new HiddenMethodDerived();
        _ = owner.Method();
        _ = ((HiddenMethodBase)owner).Method();
        Type arrayType = typeof(HiddenMethodDerived)
            .GetMethod(nameof(HiddenMethodDerived.Method), Type.EmptyTypes)!.ReturnType;
        Array values = Array.CreateInstanceFromArrayType(arrayType, 1);
        HiddenMethodLiveMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + HiddenMethodLiveMatch.Calls;
    }

    private static string IndexedPropertySearch()
    {
        var owner = new IndexedPropertyHolder();
        _ = owner[0];
        _ = owner[""];
        Type arrayType = typeof(IndexedPropertyHolder)
            .GetProperty("Item", new[] { typeof(string) })!.PropertyType;
        Array values = Array.CreateInstanceFromArrayType(arrayType, 1);
        IndexerSelectedMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + IndexerSelectedMatch.Calls;
    }

    private static string GenericAritySearch()
    {
        var owner = new GenericArityHolder();
        _ = owner.Method();
        _ = owner.Method<int>();
        Type arrayType = typeof(GenericArityHolder)
            .GetMethod(nameof(GenericArityHolder.Method), 0, Type.EmptyTypes)!.ReturnType;
        Array values = Array.CreateInstanceFromArrayType(arrayType, 1);
        AritySelectedMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + AritySelectedMatch.Calls;
    }

    private static string HiddenReflectedFieldSearch()
    {
        var owner = new HiddenReflectedFieldDerived();
        var field = typeof(HiddenReflectedFieldDerived).GetField("Value")!;
        object[] values = { field.GetValue(owner)! };
        HiddenReflectedFieldLiveMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":"
            + HiddenReflectedFieldLiveMatch.Calls + ":" + field.DeclaringType!.Name;
    }

    private static void CopyViaHelper(Array source, Array destination) =>
        Array.Copy(source, destination, 1);

    private static string HelperArrayCopySearch()
    {
        Array source = new HelperArrayCopyMatch[1];
        object[] values = new object[1];
        CopyViaHelper(source, values);
        HelperArrayCopyMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":" + HelperArrayCopyMatch.Calls;
    }

    private static void CopyToViaHelper(Array source, Array destination) =>
        source.CopyTo(destination, 0);

    private static string HelperArrayCopyToSearch()
    {
        Array source = new HelperArrayCopyToMatch[1];
        object[] values = new object[1];
        CopyToViaHelper(source, values);
        HelperArrayCopyToMatch.Calls = 0;
        return Array.IndexOf((Array)values, new object()) + ":" + HelperArrayCopyToMatch.Calls;
    }

    private static void ReplaceArray(ref Array value) => value = new RefWriteMatch[1];

    private static string RefWriteSearch()
    {
        Array values = new object[0];
        ReplaceArray(ref values);
        RefWriteMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + RefWriteMatch.Calls;
    }

    private static void ReplaceFieldArray(ref Array value) => value = new RefFieldWriteMatch[1];

    private static string RefFieldWriteSearch()
    {
        var holder = new RefFieldHolder();
        ReplaceFieldArray(ref holder.Values);
        RefFieldWriteMatch.Calls = 0;
        return Array.IndexOf(holder.Values, null) + ":" + RefFieldWriteMatch.Calls;
    }

    private static void ReplaceStaticArray(ref Array value) => value = new RefStaticWriteMatch[1];

    private static string RefStaticWriteSearch()
    {
        ReplaceStaticArray(ref RefFieldHolder.StaticValues);
        RefStaticWriteMatch.Calls = 0;
        return Array.IndexOf(RefFieldHolder.StaticValues, null) + ":" + RefStaticWriteMatch.Calls;
    }

    private static string ArrayGetTypeSearch()
    {
        Array source = Array.CreateInstance(typeof(ArrayGetTypeMatch), 1);
        Array selected = Array.CreateInstance(source.GetType().GetElementType()!, 1);
        ArrayGetTypeMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + ArrayGetTypeMatch.Calls;
    }

    private static string MakeArrayTypeSearch()
    {
        Type arrayType = typeof(MakeArrayTypeMatch).MakeArrayType();
        bool exact = arrayType == typeof(MakeArrayTypeMatch[]);
        Array selected = Array.CreateInstanceFromArrayType(arrayType, 1);
        MakeArrayTypeMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + MakeArrayTypeMatch.Calls + ":" + exact;
    }

    private static string ObjectArrayGetTypeSearch()
    {
        Array source = new object[] { new ObjectGetTypeBoxMatch() };
        Type element = source.GetType().GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        return Array.IndexOf(selected, null) + ":" + element.Name;
    }

    private static string NestedArrayTypeSearch(object[] container)
    {
        Type element = container[0].GetType().GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        NestedArrayRuntimeMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + NestedArrayRuntimeMatch.Calls;
    }

    private static string NestedArrayTypeSearch()
    {
        object[] container = new object[1];
        container[0] = new NestedArrayRuntimeMatch[1];
        return NestedArrayTypeSearch(container);
    }

    private static string RankedTypeArraySearch()
    {
        int[] lengths = new int[2];
        lengths[0] = 1;
        lengths[1] = 1;
        Array source = Array.CreateInstance(typeof(RankedTypeArrayMatch), lengths);
        Type element = source.GetType().MakeArrayType().GetElementType()!.GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        RankedTypeArrayMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + RankedTypeArrayMatch.Calls;
    }

    private static Array BuildRankedArray(Type type, int[] lengths) =>
        Array.CreateInstance(type, lengths);

    private static string HelperRankedTypeArraySearch()
    {
        Array source = BuildRankedArray(typeof(HelperRankedTypeArrayMatch), new[] { 1, 1 });
        Type element = source.GetType().MakeArrayType().GetElementType()!.GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        HelperRankedTypeArrayMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + HelperRankedTypeArrayMatch.Calls;
    }

    private static string RepeatedRankedTypeArraySearch()
    {
        Array source = BuildRankedArray(typeof(RepeatedRankedTypeArrayMatch), new[] { 1, 1 });
        Type element = source.GetType().MakeArrayType().MakeArrayType()
            .GetElementType()!.GetElementType()!.GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        RepeatedRankedTypeArrayMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + RepeatedRankedTypeArrayMatch.Calls;
    }

    private static void AssignThroughRef<T>(ref T slot, T value) => slot = value;

    private static T ReadThroughRef<T>(ref T slot) => slot;

    private static string RefStobjSearch()
    {
        Array source = new RefStobjBeforeMatch[1];
        AssignThroughRef(ref source, (Array)new object[] { new RefStobjMatch() });
        RefStobjMatch.Calls = 0;
        return Array.IndexOf(source, new object()) + ":" + RefStobjMatch.Calls;
    }

    private static string RefLdobjSearch()
    {
        Array source = new object[] { new RefLdobjMatch() };
        Array selected = ReadThroughRef(ref source);
        RefLdobjMatch.Calls = 0;
        return Array.IndexOf(selected, new object()) + ":" + RefLdobjMatch.Calls;
    }

    private static string CopiedObjectArrayGetTypeSearch()
    {
        Array source = new CopyShapeBoxMatch[1];
        Array destination = new object[1];
        Array.Copy(source, destination, 1);
        Type element = destination.GetType().GetElementType()!;
        Array selected = Array.CreateInstance(element, 1);
        return Array.IndexOf(selected, null) + ":" + element.Name;
    }

    private static string MdArrayGetTypeSearch()
    {
        Array source = new MdGetTypeMatch[1, 1];
        Type sourceType = source.GetType();
        Array selected = Array.CreateInstance(sourceType.GetElementType()!, 1);
        MdGetTypeMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + MdGetTypeMatch.Calls
            + ":" + sourceType.GetArrayRank();
    }

    private static string TypeHandleSearch()
    {
        Type type = typeof(TypeHandleMatch);
        RuntimeTypeHandle handle = type.TypeHandle;
        Array selected = Array.CreateInstance(Type.GetTypeFromHandle(handle)!, 1);
        TypeHandleMatch.Calls = 0;
        return Array.IndexOf(selected, null) + ":" + TypeHandleMatch.Calls;
    }

    private static string FieldAliasSearch()
    {
        ArrayHolder owner = new();
        ArrayHolder alias = owner;
        alias.StoredValues = new FieldAliasMatch[1];
        FieldAliasMatch.Calls = 0;
        return Array.IndexOf(owner.StoredValues!, null) + ":" + FieldAliasMatch.Calls;
    }

    private static string FieldOverwriteSearch()
    {
        ArrayHolder owner = new();
        owner.StoredValues = new OverwrittenDeadMatch[1];
        int overwrittenLength = owner.StoredValues.Length;
        owner.StoredValues = new OverwriteSelectedMatch[1];
        OverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf(owner.StoredValues, null) + ":" + OverwriteSelectedMatch.Calls
            + ":" + overwrittenLength;
    }

    private static string FieldVirtualSearch()
    {
        FieldVirtualHolder owner = new();
        owner.Provider = new FieldVirtualArrayTypeProvider();
        Array array = Array.CreateInstanceFromArrayType(owner.Provider.ArrayType(), 1);
        FieldVirtualMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + FieldVirtualMatch.Calls;
    }

    private static string StaticErasedSearch()
    {
        ArrayHolder.StoredStaticValues = new StaticErasedMatch[1];
        StaticErasedMatch.Calls = 0;
        return Array.IndexOf(ArrayHolder.StoredStaticValues, null) + ":" + StaticErasedMatch.Calls;
    }

    private static string StaticDeclaredSearch()
    {
        ArrayHolder.TypedStaticValues = new StaticDeclaredMatch[1];
        StaticDeclaredMatch.Calls = 0;
        return Array.IndexOf((Array)ArrayHolder.TypedStaticValues, null)
            + ":" + StaticDeclaredMatch.Calls;
    }

    private static string CctorStaticSearch()
    {
        CctorStaticMatch.Calls = 0;
        return Array.IndexOf(StaticInitHolder.Values, null) + ":" + CctorStaticMatch.Calls;
    }

    private static string ConstructorFieldSearch()
    {
        ConstructorInitHolder owner = new();
        ConstructorMatch.Calls = 0;
        return Array.IndexOf(owner.Values, null) + ":" + ConstructorMatch.Calls;
    }

    private static string ConstructorOverwriteSearch()
    {
        ConstructorOverwriteHolder owner = new();
        ConstructorOverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf(owner.Values, null) + ":" + ConstructorOverwriteSelectedMatch.Calls
            + ":" + owner.FirstLength;
    }

    private static string CctorOverwriteSearch()
    {
        CctorOverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf(CctorOverwriteHolder.Values, null) + ":"
            + CctorOverwriteSelectedMatch.Calls + ":" + CctorOverwriteHolder.FirstLength;
    }

    private static string ConditionalTypeArraySearch(bool second)
    {
        Type[] elements = new Type[1];
        if (second)
            elements[0] = typeof(ConditionalSecondMatch);
        else
            elements[0] = typeof(ConditionalFirstMatch);
        Array array = Array.CreateInstance(elements[0], 1);
        ConditionalFirstMatch.Calls = 0;
        ConditionalSecondMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + ConditionalFirstMatch.Calls
            + ":" + ConditionalSecondMatch.Calls;
    }

    private static string IndexedTypeArraySearch()
    {
        Type[] elements = new Type[] { typeof(IndexedSelectedMatch), typeof(IndexedUnselectedMatch) };
        Array array = Array.CreateInstance(elements[0], 1);
        IndexedSelectedMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + IndexedSelectedMatch.Calls;
    }

    private static string TypeArrayOverwriteSearch()
    {
        Type dead = typeof(TypeOverwrittenDeadMatch);
        Type live = typeof(TypeOverwriteSelectedMatch);
        Type[] elements = new Type[1];
        elements[0] = dead;
        Type first = elements[0];
        elements[0] = live;
        Array array = Array.CreateInstance(elements[0], 1);
        TypeOverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + TypeOverwriteSelectedMatch.Calls
            + ":" + first.Name;
    }

    private static string TypeArraySnapshotSearch()
    {
        Type selected = typeof(TypeSnapshotSelectedMatch);
        Type unselected = typeof(TypeSnapshotUnselectedMatch);
        Type[] elements = new Type[1];
        elements[0] = selected;
        Type saved = elements[0];
        elements[0] = unselected;
        Array array = Array.CreateInstance(saved, 1);
        TypeSnapshotSelectedMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + TypeSnapshotSelectedMatch.Calls
            + ":" + elements[0].Name;
    }

    private static string SignatureArrayOverwriteSearch()
    {
        var holder = new ArrayHolder();
        _ = holder.SignatureOverloaded(0);
        _ = holder.SignatureOverloaded("");
        Type text = typeof(string);
        Type integer = typeof(int);
        Type[] parameters = new Type[1];
        parameters[0] = text;
        parameters[0] = integer;
        Type arrayType = typeof(ArrayHolder)
            .GetMethod(nameof(ArrayHolder.SignatureOverloaded), parameters)!.ReturnType;
        Array array = Array.CreateInstanceFromArrayType(arrayType, 1);
        SignatureSelectedMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + SignatureSelectedMatch.Calls;
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static Type ChooseSignatureType() => typeof(string);

    private static string RuntimeSignatureOverwriteSearch()
    {
        var holder = new ArrayHolder();
        _ = holder.RuntimeSignatureOverloaded(0);
        _ = holder.RuntimeSignatureOverloaded("");
        Type integer = typeof(int);
        Type[] parameters = new Type[1];
        parameters[0] = integer;
        parameters[0] = ChooseSignatureType();
        Type arrayType = typeof(ArrayHolder)
            .GetMethod(nameof(ArrayHolder.RuntimeSignatureOverloaded), parameters)!.ReturnType;
        Array array = Array.CreateInstanceFromArrayType(arrayType, 1);
        RuntimeSignatureSelectedMatch.Calls = 0;
        return Array.IndexOf(array, null) + ":" + RuntimeSignatureSelectedMatch.Calls;
    }

    private static string StaticOverwriteSearch()
    {
        StaticOverwriteHolder.Values = new StaticOverwrittenDeadMatch[1];
        int firstLength = StaticOverwriteHolder.Values.Length;
        StaticOverwriteHolder.Values = new StaticOverwriteSelectedMatch[1];
        StaticOverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf(StaticOverwriteHolder.Values, null) + ":"
            + StaticOverwriteSelectedMatch.Calls + ":" + firstLength;
    }

    private static void SetGenericStatic<T>() where T : struct
    {
        GenericStaticHolder<T>.Values = new T[1];
    }

    private static int SearchGenericStatic<T>() where T : struct =>
        Array.IndexOf(GenericStaticHolder<T>.Values, null);

    private static string GenericStaticSearchBoth()
    {
        GenericStaticFirstMatch.Calls = 0;
        GenericStaticSecondMatch.Calls = 0;
        SetGenericStatic<GenericStaticFirstMatch>();
        SetGenericStatic<GenericStaticSecondMatch>();
        int first = SearchGenericStatic<GenericStaticFirstMatch>();
        int second = SearchGenericStatic<GenericStaticSecondMatch>();
        return first + ":" + GenericStaticFirstMatch.Calls + ","
            + second + ":" + GenericStaticSecondMatch.Calls;
    }

    private static string StructFieldSearch()
    {
        ValueArrayHolder owner = new();
        owner.Values = new StructFieldMatch[1];
        StructFieldMatch.Calls = 0;
        return Array.IndexOf(owner.Values, null) + ":" + StructFieldMatch.Calls;
    }

    private static string BoxArrayOverwriteSearch()
    {
        object sought = new();
        object[] values = new object[1];
        values[0] = new BoxOverwrittenDeadMatch();
        bool firstPresent = values[0] is BoxOverwrittenDeadMatch;
        values[0] = new BoxOverwriteSelectedMatch();
        BoxOverwriteSelectedMatch.Calls = 0;
        return Array.IndexOf((Array)values, sought) + ":"
            + BoxOverwriteSelectedMatch.Calls + ":" + firstPresent;
    }

    private static void Probe(string label, Func<string> action)
    {
        try
        {
            Console.WriteLine(label + "=" + action());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name);
        }
    }

    private static void RunFrameworkBox()
    {
        var list = new LinkedList<FrameworkBoxMatch>();
        list.AddLast(new FrameworkBoxMatch(7));
        IEnumerator cursor = ((IEnumerable)list).GetEnumerator();
        cursor.MoveNext();
        object first = cursor.Current!;
        object second = cursor.Current!;
        Array values = new object[] { first };
        FrameworkBoxMatch.Calls = 0;
        Console.WriteLine("framework-box-forward=" + Array.IndexOf(values, second) + ":"
            + FrameworkBoxMatch.Calls + ":" + ReferenceEquals(first, second));
        FrameworkBoxMatch.Calls = 0;
        Console.WriteLine("framework-box-backward=" + Array.LastIndexOf(values, second) + ":"
            + FrameworkBoxMatch.Calls);
        FrameworkBoxMatch.Calls = 0;
        Console.WriteLine("framework-box-null=" + Array.IndexOf(values, null) + ":"
            + FrameworkBoxMatch.Calls);
    }

    private static void RunFrameworkListBox()
    {
        IList list = new List<FrameworkListBoxMatch> { new(9) };
        object first = list[0]!;
        object second = list[0]!;
        Array values = new object[] { first };
        FrameworkListBoxMatch.Calls = 0;
        Console.WriteLine("list-box-forward=" + Array.IndexOf(values, second) + ":"
            + FrameworkListBoxMatch.Calls + ":" + ReferenceEquals(first, second));
        FrameworkListBoxMatch.Calls = 0;
        Console.WriteLine("list-box-backward=" + Array.LastIndexOf(values, second) + ":"
            + FrameworkListBoxMatch.Calls);
    }

    private static void RunFrameworkCopyBox()
    {
        var list = new LinkedList<FrameworkCopyBoxMatch>();
        list.AddLast(new FrameworkCopyBoxMatch(11));
        ICollection source = list;
        object?[] first = new object?[1], second = new object?[1];
        source.CopyTo(first, 0);
        source.CopyTo(second, 0);
        FrameworkCopyBoxMatch.Calls = 0;
        Console.WriteLine("copy-box-forward=" + Array.IndexOf((Array)first, second[0]) + ":"
            + FrameworkCopyBoxMatch.Calls + ":" + ReferenceEquals(first[0], second[0]));
        FrameworkCopyBoxMatch.Calls = 0;
        Console.WriteLine("copy-box-backward=" + Array.LastIndexOf((Array)first, second[0]) + ":"
            + FrameworkCopyBoxMatch.Calls);
        FrameworkCopyBoxMatch.Calls = 0;
        Console.WriteLine("copy-box-other=" + Array.IndexOf((Array)first, new object()) + ":"
            + FrameworkCopyBoxMatch.Calls);
    }

    private static void RunReflectedFieldBox()
    {
        var holder = new ReflectedValueHolder();
        var field = typeof(ReflectedValueHolder).GetField(nameof(ReflectedValueHolder.Value))!;
        object first = field.GetValue(holder)!;
        object second = field.GetValue(holder)!;
        Array values = new object[] { first };
        ReflectedFieldBoxMatch.Calls = 0;
        Console.WriteLine("reflected-box-forward=" + Array.IndexOf(values, second) + ":"
            + ReflectedFieldBoxMatch.Calls + ":" + ReferenceEquals(first, second));
        ReflectedFieldBoxMatch.Calls = 0;
        Console.WriteLine("reflected-box-backward=" + Array.LastIndexOf(values, second) + ":"
            + ReflectedFieldBoxMatch.Calls);
    }

    private static void RunReflectedObjectFieldBox()
    {
        var holder = new ReflectedObjectHolder();
        holder.Value = new ObjectFieldBoxMatch();
        object boxed = typeof(ReflectedObjectHolder)
            .GetField(nameof(ReflectedObjectHolder.Value))!.GetValue(holder)!;
        object sought = new();
        Array values = new object[] { boxed };
        ObjectFieldBoxMatch.Calls = 0;
        Console.WriteLine("object-field-box=" + Array.IndexOf(values, sought) + ":"
            + ObjectFieldBoxMatch.Calls);
    }

    private static void RunReflectedSetObjectFieldBox()
    {
        var holder = new ReflectedObjectHolder();
        var field = typeof(ReflectedObjectHolder).GetField(nameof(ReflectedObjectHolder.Value))!;
        field.SetValue(holder, new ReflectedSetBoxMatch());
        object boxed = field.GetValue(holder)!;
        object sought = new();
        Array values = new object[] { boxed };
        ReflectedSetBoxMatch.Calls = 0;
        Console.WriteLine("reflected-set-box=" + Array.IndexOf(values, sought) + ":"
            + ReflectedSetBoxMatch.Calls);
    }

    private static void RunReflectedObjectFieldOverwrite()
    {
        var holder = new ReflectedObjectHolder();
        holder.Value = new ReflectedOverwrittenDeadMatch();
        bool firstPresent = holder.Value is ReflectedOverwrittenDeadMatch;
        holder.Value = new ReflectedOverwriteSelectedMatch();
        object boxed = typeof(ReflectedObjectHolder)
            .GetField(nameof(ReflectedObjectHolder.Value))!.GetValue(holder)!;
        object sought = new();
        Array values = new object[] { boxed };
        ReflectedOverwriteSelectedMatch.Calls = 0;
        Console.WriteLine("object-field-overwrite=" + Array.IndexOf(values, sought) + ":"
            + ReflectedOverwriteSelectedMatch.Calls + ":" + firstPresent);
    }

    private static void RunReflectedSetObjectFieldOverwrite()
    {
        var holder = new ReflectedObjectHolder();
        var field = typeof(ReflectedObjectHolder).GetField(nameof(ReflectedObjectHolder.Value))!;
        field.SetValue(holder, new ReflectedSetOverwrittenDeadMatch());
        bool firstPresent = field.GetValue(holder) is ReflectedSetOverwrittenDeadMatch;
        field.SetValue(holder, new ReflectedSetOverwriteSelectedMatch());
        object boxed = field.GetValue(holder)!;
        object sought = new();
        Array values = new object[] { boxed };
        ReflectedSetOverwriteSelectedMatch.Calls = 0;
        Console.WriteLine("reflected-set-overwrite=" + Array.IndexOf(values, sought) + ":"
            + ReflectedSetOverwriteSelectedMatch.Calls + ":" + firstPresent);
    }

    private static void RunReflectedMethodBox()
    {
        var holder = new ReflectedReturnHolder();
        _ = holder.MethodValue();
        var method = typeof(ReflectedReturnHolder).GetMethod(nameof(ReflectedReturnHolder.MethodValue))!;
        object first = method.Invoke(holder, null)!;
        object second = method.Invoke(holder, null)!;
        Array values = new object[] { first };
        ReflectedMethodBoxMatch.Calls = 0;
        Console.WriteLine("method-box=" + Array.IndexOf(values, second) + ":"
            + ReflectedMethodBoxMatch.Calls + ":" + ReferenceEquals(first, second));
    }

    private static void RunReflectedPropertyBox()
    {
        var holder = new ReflectedReturnHolder();
        _ = holder.PropertyValue;
        var property = typeof(ReflectedReturnHolder).GetProperty(nameof(ReflectedReturnHolder.PropertyValue))!;
        object first = property.GetValue(holder)!;
        object second = property.GetValue(holder)!;
        Array values = new object[] { first };
        ReflectedPropertyBoxMatch.Calls = 0;
        Console.WriteLine("property-box=" + Array.IndexOf(values, second) + ":"
            + ReflectedPropertyBoxMatch.Calls + ":" + ReferenceEquals(first, second));
    }

    private static void RunArrayGetValueBox()
    {
        Array array = Array.CreateInstance(typeof(ArrayGetValueBoxMatch), 1);
        object first = array.GetValue(0)!;
        object second = array.GetValue(0)!;
        Array values = new object[] { first };
        ArrayGetValueBoxMatch.Calls = 0;
        Console.WriteLine("array-get-box=" + Array.IndexOf(values, second) + ":"
            + ArrayGetValueBoxMatch.Calls + ":" + ReferenceEquals(first, second));
    }

    internal static void RunReflectionReturnBoxesOnly()
    {
        Console.WriteLine("== reflection and array return boxes ==");
        RunReflectedMethodBox();
        RunReflectedPropertyBox();
        RunArrayGetValueBox();
        Console.WriteLine("reflection and array return boxes end");
    }

    private static void RunUnusedFrameworkBox()
    {
        var list = new LinkedList<UnusedFrameworkBoxMatch>();
        list.AddLast(new UnusedFrameworkBoxMatch(17));
        object?[] objects = new object?[1];
        ((ICollection)list).CopyTo(objects, 0);
        Console.WriteLine("unused-box=" + (objects[0] is not null));
        UnusedNewarrMatch[] unsearched = new UnusedNewarrMatch[1];
        Console.WriteLine("unused-newarr=" + unsearched.Length);
        object userBox = new UnusedUserBoxMatch();
        Console.WriteLine("unused-user-box=" + userBox.GetType().Name);
    }

    internal static void RunFrameworkBoxesOnly()
    {
        Console.WriteLine("== framework box provenance ==");
        RunFrameworkBox();
        RunFrameworkListBox();
        RunFrameworkCopyBox();
        RunReflectedFieldBox();
        RunReflectedObjectFieldBox();
        RunReflectedSetObjectFieldBox();
        RunReflectedObjectFieldOverwrite();
        RunReflectedSetObjectFieldOverwrite();
        RunUnusedFrameworkBox();
        Console.WriteLine("field-stored=" + FieldStoredSearch());
        Console.WriteLine("instance-snapshot=" + InstanceFieldSnapshotSearch());
        Console.WriteLine("static-snapshot=" + StaticFieldSnapshotSearch());
        Console.WriteLine("helper-snapshot=" + HelperFieldSnapshotSearch());
        Console.WriteLine("reflected-snapshot=" + ReflectedFieldSnapshotSearch());
        Console.WriteLine("array-get-holder-alias=" + ArrayGetHolderAliasSearch());
        Console.WriteLine("shared-donor=" + SharedDonorSearch());
        Console.WriteLine("array-set-box=" + ArraySetValueBoxSearch());
        Console.WriteLine("array-clone=" + ArrayCloneSearch());
        Console.WriteLine("byref-array=" + ByRefArraySearch());
        Console.WriteLine("array-copy-box=" + ArrayCopyBoxSearch());
        Console.WriteLine("array-copyto-box=" + ArrayCopyToBoxSearch());
        Console.WriteLine("constrained-copy-rejected=" + RejectedConstrainedCopySearch());
        Console.WriteLine("signature-pair-forward=" + SignaturePairSearch(false));
        Console.WriteLine("signature-pair-reverse=" + SignaturePairSearch(true));
        Console.WriteLine("hidden-method=" + HiddenMethodSearch());
        Console.WriteLine("indexed-property=" + IndexedPropertySearch());
        Console.WriteLine("generic-arity=" + GenericAritySearch());
        Console.WriteLine("hidden-reflected-field=" + HiddenReflectedFieldSearch());
        Console.WriteLine("helper-copy-box=" + HelperArrayCopySearch());
        Console.WriteLine("helper-copyto-box=" + HelperArrayCopyToSearch());
        Console.WriteLine("ref-write=" + RefWriteSearch());
        Console.WriteLine("field-alias=" + FieldAliasSearch());
        Console.WriteLine("field-overwrite=" + FieldOverwriteSearch());
        Console.WriteLine("field-virtual=" + FieldVirtualSearch());
        Console.WriteLine("type-overwrite=" + TypeArrayOverwriteSearch());
        Console.WriteLine("type-snapshot=" + TypeArraySnapshotSearch());
        Console.WriteLine("signature-overwrite=" + SignatureArrayOverwriteSearch());
        Console.WriteLine("signature-runtime-overwrite=" + RuntimeSignatureOverwriteSearch());
        Console.WriteLine("static-overwrite=" + StaticOverwriteSearch());
        Console.WriteLine("generic-static=" + GenericStaticSearchBoth());
        Console.WriteLine("struct-field=" + StructFieldSearch());
        Console.WriteLine("box-overwrite=" + BoxArrayOverwriteSearch());
        Console.WriteLine("static-erased=" + StaticErasedSearch());
        Console.WriteLine("static-declared=" + StaticDeclaredSearch());
        Console.WriteLine("cctor-static=" + CctorStaticSearch());
        Console.WriteLine("constructor-field=" + ConstructorFieldSearch());
        Console.WriteLine("constructor-overwrite=" + ConstructorOverwriteSearch());
        Console.WriteLine("cctor-overwrite=" + CctorOverwriteSearch());
        Console.WriteLine("framework box provenance end");
    }

    internal static void RunFrameworkBoxAdditions()
    {
        Console.WriteLine("== array search copy and type provenance ==");
        Console.WriteLine("array-copy-rejected=" + RejectedArrayCopySearch());
        Console.WriteLine("array-copyto-rejected=" + RejectedArrayCopyToSearch());
        Console.WriteLine("constrained-copy-box=" + ConstrainedCopyBoxSearch());
        Console.WriteLine("array-copy-zero=" + ZeroLengthArrayCopySearch());
        Console.WriteLine("array-copy-rank=" + RankRejectedArrayCopySearch());
        Console.WriteLine("array-copy-value-rejected=" + RejectedValueArrayCopySearch());
        Console.WriteLine("array-copy-created-rejected=" + RejectedCreatedArrayCopySearch());
        Console.WriteLine("constrained-copy-byref-source=" + ByRefReplacedConstrainedCopySearch());
        Console.WriteLine("byref-before-write=" + BeforeByRefWriteSearch());
        Console.WriteLine("byref-nonvoid-write=" + NonVoidByRefWriteSearch());
        Console.WriteLine("byref-nonvoid-return=" + NonVoidByRefReturnSearch());
        Console.WriteLine("generic-ref-before=" + GenericRefBeforeSearch());
        Console.WriteLine("generic-ref-after=" + GenericRefAfterSearch());
        Console.WriteLine("empty-type-signature=" + EmptyTypeSignatureSearch());
        Console.WriteLine("ref-field-write=" + RefFieldWriteSearch());
        Console.WriteLine("ref-static-write=" + RefStaticWriteSearch());
        Console.WriteLine("array-get-type=" + ArrayGetTypeSearch());
        Console.WriteLine("make-array-type=" + MakeArrayTypeSearch());
        Console.WriteLine("object-get-type=" + ObjectArrayGetTypeSearch());
        Console.WriteLine("nested-array-get-type=" + NestedArrayTypeSearch());
        Console.WriteLine("ranked-array-type=" + RankedTypeArraySearch());
        Console.WriteLine("helper-ranked-array-type=" + HelperRankedTypeArraySearch());
        Console.WriteLine("repeated-ranked-array-type=" + RepeatedRankedTypeArraySearch());
        Console.WriteLine("ref-stobj=" + RefStobjSearch());
        Console.WriteLine("ref-ldobj=" + RefLdobjSearch());
        Console.WriteLine("copied-object-get-type=" + CopiedObjectArrayGetTypeSearch());
        Console.WriteLine("md-get-type=" + MdArrayGetTypeSearch());
        Console.WriteLine("type-handle=" + TypeHandleSearch());
        Console.WriteLine("array search copy and type provenance end");
    }

    internal static void Run()
    {
        Console.WriteLine("== dynamic array null equality ==");
        ArrayHolder holder = new();
        holder.TouchHidden();
        _ = holder.PropertyValues;
        holder.SetterValues = null!;
        holder.MixedVisibility = null!;
        _ = holder.MethodValues();
        _ = holder.InterfaceValues();
        _ = holder.Overloaded();
        _ = holder.Overloaded(0);
        _ = holder.NonzeroOverloaded();
        _ = holder.NonzeroOverloaded(0);

        Probe("direct", () =>
        {
            Array array = Array.CreateInstance(typeof(DirectDynamicMatch), 2);
            DirectDynamicMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + DirectDynamicMatch.Calls;
        });
        Probe("conditional-first", () => ConditionalTypeArraySearch(false));
        Probe("conditional-second", () => ConditionalTypeArraySearch(true));
        Probe("indexed-selected", IndexedTypeArraySearch);
        Probe("type-overwrite", TypeArrayOverwriteSearch);
        Probe("field-forward", () =>
        {
            Array array = CreateFromElement(FieldArrayType(holder).GetElementType()!);
            FieldMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + FieldMatch.Calls;
        });
        Probe("field-backward", () =>
        {
            Array array = CreateFromElement(FieldArrayType(holder).GetElementType()!);
            FieldMatch.Calls = 0;
            return Array.LastIndexOf(array, null) + ":" + FieldMatch.Calls;
        });
        Probe("hidden-field", () =>
        {
            Type arrayType = typeof(ArrayHolder).GetField("HiddenValues",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.FieldType;
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            HiddenMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + HiddenMatch.Calls;
        });
        Probe("field-stored", FieldStoredSearch);
        Probe("field-alias", FieldAliasSearch);
        Probe("field-overwrite", FieldOverwriteSearch);
        Probe("field-virtual", FieldVirtualSearch);
        Probe("static-overwrite", StaticOverwriteSearch);
        Probe("generic-static", GenericStaticSearchBoth);
        Probe("struct-field", StructFieldSearch);
        Probe("box-overwrite", BoxArrayOverwriteSearch);
        Probe("static-erased", StaticErasedSearch);
        Probe("static-declared", StaticDeclaredSearch);
        Probe("cctor-static", CctorStaticSearch);
        Probe("constructor-field", ConstructorFieldSearch);
        Probe("constructor-overwrite", ConstructorOverwriteSearch);
        Probe("cctor-overwrite", CctorOverwriteSearch);
        Probe("property", () =>
        {
            ArrayTypeProvider provider = new PropertyArrayTypeProvider();
            Array array = Array.CreateInstanceFromArrayType(provider.ArrayType(), 2);
            PropertyMatch.Calls = 0;
            return Array.LastIndexOf(array, null) + ":" + PropertyMatch.Calls;
        });
        Probe("setter-only", () =>
        {
            Type arrayType = typeof(ArrayHolder)
                .GetProperty(nameof(ArrayHolder.SetterValues))!.PropertyType;
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            SetterOnlyMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + SetterOnlyMatch.Calls;
        });
        Probe("mixed-visibility", () =>
        {
            Type arrayType = typeof(ArrayHolder)
                .GetProperty(nameof(ArrayHolder.MixedVisibility))!.PropertyType;
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            MixedVisibilityMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + MixedVisibilityMatch.Calls;
        });
        Probe("method", () =>
        {
            Array array = Array.CreateInstanceFromArrayType(MethodArrayType(), 2);
            MethodMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + MethodMatch.Calls;
        });
        Probe("generic-argument", () =>
        {
            Type arrayType = typeof(List<GenericArgumentMatch[]>).GenericTypeArguments[0];
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            GenericArgumentMatch.Calls = 0;
            return Array.LastIndexOf(array, null) + ":" + GenericArgumentMatch.Calls;
        });
        Probe("generic-selected", () =>
        {
            Type arrayType = typeof(Dictionary<GenericSelectedMatch[], GenericUnselectedMatch[]>)
                .GenericTypeArguments[0];
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            GenericSelectedMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + GenericSelectedMatch.Calls;
        });
        Probe("interface-provider", () =>
        {
            IArrayTypeProvider provider = new InterfaceArrayTypeProvider();
            Array array = Array.CreateInstanceFromArrayType(provider.ArrayType(), 2);
            InterfaceMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + InterfaceMatch.Calls;
        });
        Probe("overload", () =>
        {
            Type arrayType = typeof(ArrayHolder)
                .GetMethod(nameof(ArrayHolder.Overloaded), Type.EmptyTypes)!.ReturnType;
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            OverloadSelectedMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + OverloadSelectedMatch.Calls;
        });
        Probe("nonzero-overload", () =>
        {
            Type arrayType = typeof(ArrayHolder)
                .GetMethod(nameof(ArrayHolder.NonzeroOverloaded), new[] { typeof(int) })!.ReturnType;
            Array array = Array.CreateInstanceFromArrayType(arrayType, 2);
            NonzeroOverloadMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + NonzeroOverloadMatch.Calls;
        });
        Probe("local-after-create", () =>
        {
            Type elementType = typeof(LocalSelectedMatch);
            Array selected = Array.CreateInstance(elementType, 2);
            elementType = typeof(UnselectedLocalMatch);
            LocalSelectedMatch.Calls = 0;
            return Array.IndexOf(selected, null) + ":" + LocalSelectedMatch.Calls
                + ":" + elementType.Name;
        });
        Probe("helper-other-call", () =>
        {
            Array selected = CreateFromElement(typeof(HelperSelectedMatch));
            Array unsearched = CreateFromElement(typeof(UnselectedHelperMatch));
            HelperSelectedMatch.Calls = 0;
            return Array.IndexOf(selected, null) + ":" + HelperSelectedMatch.Calls
                + ":" + unsearched.Length;
        });
        Probe("helper-sink", () =>
        {
            Array selected = CreateFromElement(typeof(SinkHelperMatch));
            SinkHelperMatch.Calls = 0;
            return SearchNull(selected) + ":" + SinkHelperMatch.Calls;
        });
        Probe("field-helper", () =>
        {
            ArrayHolder owner = new();
            Store(owner, new CrossHelperMatch[1]);
            CrossHelperMatch.Calls = 0;
            return Array.IndexOf(Load(owner), null) + ":" + CrossHelperMatch.Calls;
        });
        Probe("deep-helper", () =>
        {
            Array array = Array.CreateInstance(Deep0(), 2);
            DeepHelperMatch.Calls = 0;
            return Array.IndexOf(array, null) + ":" + DeepHelperMatch.Calls;
        });
        Probe("shared-generic", () =>
        {
            SharedGenericFirstMatch.Calls = 0;
            SharedGenericSecondMatch.Calls = 0;
            int first = SearchGeneric<SharedGenericFirstMatch>();
            int second = SearchGeneric<SharedGenericSecondMatch>();
            return first + ":" + SharedGenericFirstMatch.Calls + ","
                + second + ":" + SharedGenericSecondMatch.Calls;
        });
        Probe("unallocated-provider", () => typeof(UnallocatedArrayTypeProvider).Name);
        Probe("unused-reflected", () =>
            typeof(UnusedHolder).GetField(nameof(UnusedHolder.Values))!.FieldType.Name);
        try { RunFrameworkBox(); }
        catch (Exception ex) { Console.WriteLine("framework-box-error=" + ex.GetType().Name); }
        try { RunFrameworkListBox(); }
        catch (Exception ex) { Console.WriteLine("list-box-error=" + ex.GetType().Name); }
        try { RunFrameworkCopyBox(); }
        catch (Exception ex) { Console.WriteLine("copy-box-error=" + ex.GetType().Name); }
        Probe("plain-forward", () => Array.IndexOf((Array)new PlainMatch[] { new(7) }, new PlainMatch(7)).ToString());
        Probe("plain-backward", () => Array.LastIndexOf((Array)new PlainMatch[] { new(7) }, new PlainMatch(7)).ToString());
        Probe("runtime-owned", () => Array.IndexOf((Array)new decimal[] { 1m, 2m }, 2m).ToString());
        Probe("runtime-no-slot", () =>
            Array.IndexOf((Array)new System.Threading.Tasks.ParallelLoopResult[1], null).ToString());
        Console.WriteLine("dynamic array null equality end");
    }

    internal static void RunArraySearchAdditions()
    {
        Console.WriteLine("== array search reference and type provenance ==");
        Probe("ref-field-write", RefFieldWriteSearch);
        Probe("ref-static-write", RefStaticWriteSearch);
        Probe("array-get-type", ArrayGetTypeSearch);
        Probe("make-array-type", MakeArrayTypeSearch);
        Probe("object-get-type", ObjectArrayGetTypeSearch);
        Probe("nested-array-get-type", NestedArrayTypeSearch);
        Probe("ranked-array-type", RankedTypeArraySearch);
        Probe("helper-ranked-array-type", HelperRankedTypeArraySearch);
        Probe("repeated-ranked-array-type", RepeatedRankedTypeArraySearch);
        Probe("ref-stobj", RefStobjSearch);
        Probe("ref-ldobj", RefLdobjSearch);
        Probe("copied-object-get-type", CopiedObjectArrayGetTypeSearch);
        Probe("md-get-type", MdArrayGetTypeSearch);
        Probe("type-handle", TypeHandleSearch);
        Probe("empty-type-signature", EmptyTypeSignatureSearch);
        Console.WriteLine("array search reference and type provenance end");
    }
}
