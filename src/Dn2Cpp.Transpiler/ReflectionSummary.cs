using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// A per-method summary of the values that reach reflection calls. The summary reads raw
// metadata and IL only (no model types, no Cecil) and resolves nothing: every value names
// tokens of the summarized method's own module, so the transpiler and ILDiet each resolve
// it with their own generic context. Generic parameters stay symbolic inside TypeSpec and
// MethodSpec tokens.
//
// A value is a set of terms plus an optional unknown reason; a site records the value of
// its receiver and of every parameter. A site whose operand is unknown says why, so a
// consumer can report it. Interpretation is deterministic: blocks are visited in
// ascending leader order and term ids are assigned in visitation order.

/// <summary>What a summary term denotes. Every token is a metadata token of the
/// summarized method's module.</summary>
internal enum TermKind : byte
{
    /// <summary>A type token: TypeDef, TypeRef or TypeSpec. With <see cref="RTerm.Number"/>
    /// <see cref="ReflectionSummary.InexactBox"/>, the runtime type of an
    /// <see cref="ExactObject"/> of that number.</summary>
    TypeToken,
    /// <summary>Type argument <see cref="RTerm.Number"/> of the MethodSpec <see cref="RTerm.Token"/>.</summary>
    MethodSpecArg,
    /// <summary>A type named by the string <see cref="RTerm.Text"/>, which
    /// <see cref="ReflectionTypeName.Parse"/> accepts. <see cref="RTerm.Number"/> bit 0 is
    /// ignoreCase; child 0 is the assembly searched, or absent for the calling module and
    /// CoreLib.</summary>
    TypeName,
    /// <summary>MakeGenericType: child 0 the definition, child 1 the arguments.</summary>
    MadeGeneric,
    /// <summary>An array type over child 0; <see cref="RTerm.Number"/> is
    /// <see cref="ReflectionTypeName.SZArray"/>, a rank, or 0 for an unknown rank.</summary>
    ArrayOf,
    ElementOf,
    GenericDefOf,
    /// <summary>Generic argument <see cref="RTerm.Number"/> (-1: any) of child 0.</summary>
    GenericArgOf,
    BaseOf,
    /// <summary>The FieldType, PropertyType, ReturnType or ParameterType
    /// (<see cref="RTerm.Number"/> is the <see cref="ReflectionOp"/>) of child 0.</summary>
    TypeOfMember,
    Str,
    /// <summary>An integer constant in <see cref="RTerm.Number"/>.</summary>
    Int,
    Null,
    /// <summary>An array of known contents: <see cref="RTerm.Number"/> is the length and
    /// child i the element at i, or -1 when the length is unknown and the one child holds
    /// every element.</summary>
    TypeArray,
    /// <summary>A union: the children are the alternatives, and <see cref="RTerm.Number"/>
    /// is an index into <see cref="ReflectionSummary.Unknowns"/> or -1.</summary>
    Set,
    /// <summary>A method or field token: MethodDef, MemberRef, MethodSpec or FieldDef. With
    /// <see cref="RTerm.Number"/> <see cref="ReflectionSummary.VirtualTarget"/>, a delegate
    /// target taken by <c>ldvirtftn</c>, which may run an override.</summary>
    MemberToken,
    /// <summary>A member lookup; <see cref="RTerm.Number"/> is the <see cref="ReflectionOp"/>
    /// and the children are <see cref="ReflectionSummary.LookupOwner"/> through
    /// <see cref="ReflectionSummary.LookupMemberTypes"/>, with an optional
    /// <see cref="ReflectionSummary.LookupDelegateType"/> for named delegate binding.</summary>
    Lookup,
    /// <summary>A member, parameter, type or attribute list; <see cref="RTerm.Number"/> is
    /// the <see cref="ReflectionOp"/>, child 0 the owner, child 1 the binding flags or the
    /// attribute type filter; child 2 is the optional member name or attribute inherit
    /// operand.</summary>
    Enumeration,
    /// <summary>Element <see cref="RTerm.Number"/> (-1: any) of the sequence child 0.</summary>
    Element,
    /// <summary>The <see cref="ReflectionAccessor"/> <see cref="RTerm.Number"/> of member
    /// child 0; child 1 the binding flags.</summary>
    Accessor,
    /// <summary>MakeGenericMethod: child 0 the method, child 1 the arguments.</summary>
    MadeGenericMethod,
    /// <summary>Parameter <see cref="RTerm.Number"/> of method child 0.</summary>
    Parameter,
    /// <summary>A delegate: child 0 its target methods, child 1 its type.</summary>
    Delegate,
    /// <summary>An object whose exact type is the type token <see cref="RTerm.Token"/>, or,
    /// with <see cref="RTerm.Number"/> <see cref="ReflectionSummary.InexactBox"/>, a box of a
    /// token that may resolve to a reference type: such a box is a no-op, so the object's
    /// type is the token's or any type derived from it.</summary>
    ExactObject,
    /// <summary>An assembly: the one declaring type child 0, the one named by
    /// <see cref="RTerm.Text"/>, or (<see cref="RTerm.Number"/> 1) the calling module's or
    /// (2) the entry assembly.</summary>
    Assembly,
    /// <summary>An instance of the compiler-generated display class TypeDef <see cref="RTerm.Token"/>.</summary>
    Closure,
    /// <summary>The array allocated at IL offset <see cref="RTerm.Number"/>. Interpretation
    /// state only: no site operand and no other term refers to one.</summary>
    Allocation,
    /// <summary>The arrays the field <see cref="RTerm.Token"/> holds while its table is built;
    /// <see cref="RTerm.Number"/> is the field's index in that table. Interpretation state
    /// only, like <see cref="Allocation"/>.</summary>
    FieldArray,
}

/// <summary>Why a value is not known.</summary>
internal enum UnknownReason : byte
{
    /// <summary>Declared parameter <see cref="RUnknown.Index"/> (0-based, <c>this</c> not counted)
    /// of the summarized method, or of the method <see cref="RUnknown.Token"/> when it is
    /// nonzero: a value a field read in this body holds.</summary>
    Parameter,
    InstanceField,
    StaticField,
    /// <summary>The result of the call <see cref="RUnknown.Token"/>.</summary>
    CallResult,
    /// <summary>The array allocated at IL offset <see cref="RUnknown.Index"/> reaches code
    /// that may write it.</summary>
    ArrayEscaped,
    /// <summary>The default element of the array element type <see cref="RUnknown.Token"/> is not modeled.</summary>
    ArrayDefault,
    /// <summary>Local (or, with token 1, argument) <see cref="RUnknown.Index"/> has its
    /// address taken.</summary>
    AddressTaken,
    /// <summary>Local (or, with token 1, argument) <see cref="RUnknown.Index"/> is stored
    /// inside a finally or fault handler.</summary>
    HandlerLocal,
    /// <summary>Local <see cref="RUnknown.Index"/> has an uninitialized or unmodeled entry value.</summary>
    InitialLocal,
    /// <summary>A string computed by the call <see cref="RUnknown.Token"/>.</summary>
    NonConstantString,
    ResolverDelegate,
    NonNullBinder,
    ThisType,
    /// <summary>A value computed by the opcode <see cref="RUnknown.Index"/>.</summary>
    UnsupportedOpcode,
    StackShape,
    Budget,
    CaughtException,
    /// <summary>The API is bound as a method group; its arguments arrive through the delegate.</summary>
    MethodGroup,
    /// <summary>The method a delegate over the virtual method <see cref="RUnknown.Token"/> runs:
    /// the override its target's runtime type selects.</summary>
    VirtualTarget,
    /// <summary>No path from the method entry reaches the site. Last: tallies size by it.</summary>
    Unreached,
}

/// <summary>A union of terms. <see cref="Unknown"/> is an index into
/// <see cref="ReflectionSummary.Unknowns"/> when the value may also be something the
/// summary does not know, or -1; a known value has at least one alternative. The default
/// value is empty.</summary>
internal readonly struct RValue
{
    /// <summary>Sorted, distinct term ids; null or empty when none.</summary>
    public readonly int[]? Alternatives;
    private readonly int _unknownPlusOne;

    public RValue(int[]? alternatives, int unknown)
    {
        Alternatives = alternatives;
        _unknownPlusOne = unknown < 0 ? 0 : unknown + 1;
    }

    public int Unknown => _unknownPlusOne - 1;
    public static RValue None => default;
    public bool IsEmpty => Count == 0 && Unknown < 0;
    public bool IsKnown => Unknown < 0 && Count > 0;
    public int Count => Alternatives is null ? 0 : Alternatives.Length;
}

/// <summary>One classified reflection call: the value of its receiver, of each parameter
/// by declared position, and of its first method type argument.</summary>
internal readonly struct RSite
{
    public readonly int Offset;
    /// <summary>The call, newobj, ldftn or ldvirtftn operand token.</summary>
    public readonly int Token;
    public readonly ReflectionApi Api;
    public readonly RValue Receiver;
    public readonly RValue[] Operands;
    public readonly RValue TypeArgument;

    public RSite(int offset, int token, ReflectionApi api, RValue receiver, RValue[] operands, RValue typeArgument)
    {
        Offset = offset;
        Token = token;
        Api = api;
        Receiver = receiver;
        Operands = operands;
        TypeArgument = typeArgument;
    }
}

internal sealed class RTerm
{
    public TermKind Kind;
    public int Token;
    public string? Text;
    public long Number;
    /// <summary>Term ids by position; -1 for an absent operand.</summary>
    public int[] Children = Array.Empty<int>();
}

internal readonly struct RUnknown
{
    public readonly UnknownReason Reason;
    public readonly int Token;
    public readonly int Index;

    public RUnknown(UnknownReason reason, int token, int index)
    {
        Reason = reason;
        Token = token;
        Index = index;
    }
}

internal sealed class ReflectionSummary
{
    internal const int LookupOwner = 0, LookupName = 1, LookupFlags = 2, LookupTypes = 3, LookupArity = 4;
    internal const int LookupReturnType = 5, LookupMemberTypes = 6, LookupDelegateType = 7;
    /// <summary>The <see cref="RTerm.Number"/> of an inexact <see cref="TermKind.ExactObject"/>
    /// and of the <see cref="TermKind.TypeToken"/> its GetType makes. A consumer treats the
    /// type as exact only where the token resolves to a value type or a sealed type.</summary>
    internal const long InexactBox = 1;
    /// <summary>The <see cref="RTerm.Number"/> of an <c>ldvirtftn</c> <see cref="TermKind.MemberToken"/>.</summary>
    internal const long VirtualTarget = 1;

    public RTerm[] Terms = Array.Empty<RTerm>();
    /// <summary>In ascending IL offset.</summary>
    public RSite[] Sites = Array.Empty<RSite>();
    /// <summary>Runtime array types at non-generic searches, in ascending IL offset.</summary>
    public (int Offset, RValue Types)[] ArraySearchSites = Array.Empty<(int, RValue)>();
    public RUnknown[] Unknowns = Array.Empty<RUnknown>();
    /// <summary>Whether the body held a site and was interpreted.</summary>
    public bool Interpreted;
    /// <summary>The visit or term budget ran out; every site operand is
    /// <see cref="UnknownReason.Budget"/>.</summary>
    public bool OverBudget;
    /// <summary>Two paths reached a block with different stack heights; every site operand
    /// is <see cref="UnknownReason.StackShape"/>.</summary>
    public bool StackShapeMismatch;
    public int BlockVisits;
    public int Reruns;

    /// <summary>The summary of a body that holds no reflection site.</summary>
    public static readonly ReflectionSummary Empty = new();

    /// <summary>The value a child slot denotes.</summary>
    public RValue ValueOf(int term)
    {
        if (term < 0)
            return RValue.None;
        var t = Terms[term];
        if (t.Kind == TermKind.Set)
            return new RValue(t.Children, (int)t.Number);
        return new RValue(new[] { term }, -1);
    }

    /// <summary>Renders the summary for the reflection-summary dump: one header line, then one
    /// line per site.</summary>
    public void Render(StringBuilder text, MetadataReader reader, string method)
    {
        text.Append("reflection-summary ").Append(method)
            .Append(" sites ").Append(Sites.Length)
            .Append(" terms ").Append(Terms.Length)
            .Append(" visits ").Append(BlockVisits);
        if (Reruns > 0)
            text.Append(" reruns ").Append(Reruns);
        if (OverBudget)
            text.Append(" over-budget");
        if (StackShapeMismatch)
            text.Append(" stack-shape");
        text.Append('\n');
        var renderer = new SummaryRenderer(this, reader);
        foreach (var site in Sites)
        {
            renderer.ResetBudget();
            text.Append("  IL_").Append(site.Offset.ToString("x4")).Append(' ')
                .Append(site.Api.Op.ToString()).Append('/').Append(site.Api.Kind.ToString());
            if (!site.Api.Recognized)
                text.Append(" unrecognized");
            text.Append(' ').Append(renderer.Member(site.Token));
            if (site.Receiver.Count > 0 || site.Receiver.Unknown >= 0)
            {
                text.Append(" this=");
                renderer.Value(text, site.Receiver, 0, false);
            }
            for (int i = 0; i < site.Operands.Length; i++)
            {
                text.Append(" p").Append(i).Append('=');
                renderer.Value(text, site.Operands[i], 0, i == site.Api.Flags);
            }
            if (site.TypeArgument.Count > 0)
            {
                text.Append(" T=");
                renderer.Value(text, site.TypeArgument, 0, false);
            }
            text.Append('\n');
        }
        foreach (var site in ArraySearchSites)
        {
            renderer.ResetBudget();
            text.Append("  IL_").Append(site.Offset.ToString("x4")).Append(" ArraySearch types=");
            renderer.Value(text, site.Types, 0, false);
            text.Append('\n');
        }
    }
}

// Renders terms readably; reads names from metadata and decodes nothing it does not print.
// Terms render as trees, so a site line renders at most MaxTerms of them: shared children
// would otherwise repeat once per path.
internal sealed class SummaryRenderer
{
    private const int MaxDepth = 8;
    private const int MaxTerms = 4096;
    private readonly ReflectionSummary _summary;
    private readonly MetadataReader _reader;
    private int _remaining = MaxTerms;

    public SummaryRenderer(ReflectionSummary summary, MetadataReader reader)
    {
        _summary = summary;
        _reader = reader;
    }

    public void ResetBudget() => _remaining = MaxTerms;

    public void Value(StringBuilder text, RValue value, int depth, bool flags)
    {
        int count = value.Count + (value.Unknown >= 0 ? 1 : 0);
        if (count == 0)
        {
            text.Append("{}");
            return;
        }
        if (count > 1)
            text.Append('{');
        for (int i = 0; i < value.Count; i++)
        {
            if (i > 0)
                text.Append(" | ");
            Term(text, value.Alternatives![i], depth, flags);
        }
        if (value.Unknown >= 0)
        {
            if (value.Count > 0)
                text.Append(" | ");
            Unknown(text, value.Unknown);
        }
        if (count > 1)
            text.Append('}');
    }

    private void Child(StringBuilder text, int term, int depth, bool flags = false)
    {
        if (term < 0)
            text.Append('-');
        else
            Value(text, _summary.ValueOf(term), depth, flags);
    }

    private void Term(StringBuilder text, int id, int depth, bool flags)
    {
        if (depth > MaxDepth || _remaining <= 0)
        {
            text.Append("...");
            return;
        }
        _remaining--;
        var t = _summary.Terms[id];
        var c = t.Children;
        int d = depth + 1;
        switch (t.Kind)
        {
            case TermKind.TypeToken:
                text.Append("typeof(").Append(TypeName(t.Token)).Append(')');
                if (t.Number == ReflectionSummary.InexactBox)
                    text.Append("+derived");
                break;
            case TermKind.MethodSpecArg:
                text.Append("targ(").Append(Member(t.Token)).Append(',').Append(t.Number).Append(')');
                break;
            case TermKind.TypeName:
                text.Append("name(\"").Append(Escape(t.Text ?? "")).Append('"');
                if ((t.Number & 1) != 0)
                    text.Append(" ignoreCase");
                if (c.Length > 0 && c[0] >= 0)
                {
                    text.Append(" in ");
                    Child(text, c[0], d);
                }
                text.Append(')');
                break;
            case TermKind.Str:
                text.Append('"').Append(Escape(t.Text ?? "")).Append('"');
                break;
            case TermKind.Int:
                text.Append(flags ? BindingFlagsText(t.Number) : t.Number.ToString());
                break;
            case TermKind.Null:
                text.Append("null");
                break;
            case TermKind.TypeArray:
                text.Append('[');
                if (t.Number < 0)
                    text.Append("*: ");
                for (int i = 0; i < c.Length; i++)
                {
                    if (i > 0)
                        text.Append(", ");
                    Child(text, c[i], d);
                }
                text.Append(']');
                break;
            case TermKind.Set:
                Value(text, _summary.ValueOf(id), depth, flags);
                break;
            case TermKind.MemberToken:
                text.Append("member(").Append(Member(t.Token)).Append(')');
                if (t.Number == ReflectionSummary.VirtualTarget)
                    text.Append("+virtual");
                break;
            case TermKind.Lookup:
                text.Append(((ReflectionOp)t.Number).ToString()).Append('(');
                Child(text, c[ReflectionSummary.LookupOwner], d);
                Labeled(text, " name=", c, ReflectionSummary.LookupName, d, false);
                Labeled(text, " flags=", c, ReflectionSummary.LookupFlags, d, true);
                Labeled(text, " types=", c, ReflectionSummary.LookupTypes, d, false);
                Labeled(text, " arity=", c, ReflectionSummary.LookupArity, d, false);
                Labeled(text, " returns=", c, ReflectionSummary.LookupReturnType, d, false);
                Labeled(text, " memberTypes=", c, ReflectionSummary.LookupMemberTypes, d, false);
                Labeled(text, " delegateType=", c, ReflectionSummary.LookupDelegateType, d, false);
                text.Append(')');
                break;
            case TermKind.Enumeration:
                text.Append(((ReflectionOp)t.Number).ToString()).Append('(');
                Child(text, c[0], d);
                Labeled(text, (ReflectionOp)t.Number == ReflectionOp.GetCustomAttributes ? " filter=" : " flags=",
                    c, 1, d, (ReflectionOp)t.Number != ReflectionOp.GetCustomAttributes);
                if ((ReflectionOp)t.Number != ReflectionOp.GetCustomAttributes)
                    Labeled(text, " name=", c, 2, d, false);
                text.Append(')');
                break;
            case TermKind.Accessor:
                text.Append(((ReflectionAccessor)t.Number).ToString()).Append('(');
                Child(text, c[0], d);
                Labeled(text, " flags=", c, 1, d, true);
                text.Append(')');
                break;
            case TermKind.Closure:
                text.Append("closure(").Append(TypeName(t.Token)).Append(')');
                break;
            case TermKind.ExactObject:
                text.Append(t.Number == ReflectionSummary.InexactBox ? "box(" : "new(").Append(TypeName(t.Token)).Append(')');
                break;
            case TermKind.Assembly:
                text.Append("assembly(");
                if (t.Text is not null)
                    text.Append('"').Append(Escape(t.Text)).Append('"');
                else if (c.Length > 0)
                    Child(text, c[0], d);
                else
                    text.Append(t.Number == 2 ? "entry" : "this");
                text.Append(')');
                break;
            case TermKind.Allocation:
                text.Append("alloc@IL_").Append(t.Number.ToString("x4"));
                break;
            default:
                text.Append(t.Kind.ToString());
                if (t.Kind is TermKind.ArrayOf or TermKind.GenericArgOf or TermKind.Element or TermKind.Parameter)
                    text.Append('#').Append(t.Number);
                else if (t.Kind == TermKind.TypeOfMember)
                    text.Append('#').Append(((ReflectionOp)t.Number).ToString());
                text.Append('(');
                for (int i = 0; i < c.Length; i++)
                {
                    if (i > 0)
                        text.Append(", ");
                    Child(text, c[i], d);
                }
                text.Append(')');
                break;
        }
    }

    private void Labeled(StringBuilder text, string label, int[] children, int slot, int depth, bool flags)
    {
        if (slot >= children.Length || children[slot] < 0)
            return;
        text.Append(label);
        Child(text, children[slot], depth, flags);
    }

    private void Unknown(StringBuilder text, int index)
    {
        var u = _summary.Unknowns[index];
        text.Append('?').Append(u.Reason.ToString());
        switch (u.Reason)
        {
            case UnknownReason.Parameter:
                text.Append('(').Append(u.Index);
                if (u.Token != 0)
                    text.Append(" of ").Append(Member(u.Token));
                text.Append(')');
                break;
            case UnknownReason.InstanceField or UnknownReason.StaticField or UnknownReason.CallResult
                or UnknownReason.NonConstantString or UnknownReason.VirtualTarget:
                text.Append('(').Append(Member(u.Token)).Append(')');
                break;
            case UnknownReason.ArrayDefault:
                text.Append('(').Append(TypeName(u.Token)).Append(')');
                break;
            case UnknownReason.ArrayEscaped:
                text.Append("(IL_").Append(u.Index.ToString("x4")).Append(')');
                break;
            case UnknownReason.AddressTaken or UnknownReason.HandlerLocal or UnknownReason.InitialLocal:
                text.Append('(').Append(u.Token == 1 ? "A_" : "V_").Append(u.Index).Append(')');
                break;
            case UnknownReason.UnsupportedOpcode:
                text.Append('(').Append(((ILOpCode)u.Index).ToString().ToLowerInvariant()).Append(')');
                break;
        }
    }

    // BindingFlags values, which skip 0x80 and 0x80000 through 0x800000.
    private static readonly (long Value, string Name)[] BindingFlagNames =
    {
        (0x1, "IgnoreCase"), (0x2, "DeclaredOnly"), (0x4, "Instance"), (0x8, "Static"), (0x10, "Public"),
        (0x20, "NonPublic"), (0x40, "FlattenHierarchy"), (0x100, "InvokeMethod"), (0x200, "CreateInstance"),
        (0x400, "GetField"), (0x800, "SetField"), (0x1000, "GetProperty"), (0x2000, "SetProperty"),
        (0x4000, "PutDispProperty"), (0x8000, "PutRefDispProperty"), (0x10000, "ExactBinding"),
        (0x20000, "SuppressChangeType"), (0x40000, "OptionalParamBinding"), (0x1000000, "IgnoreReturn"),
        (0x2000000, "DoNotWrapExceptions"),
    };

    private static string BindingFlagsText(long value)
    {
        if (value == 0)
            return "Default";
        var text = new StringBuilder();
        long rest = value;
        foreach (var (mask, name) in BindingFlagNames)
        {
            if ((value & mask) == 0)
                continue;
            if (text.Length > 0)
                text.Append('|');
            text.Append(name);
            rest &= ~mask;
        }
        if (rest != 0)
        {
            if (text.Length > 0)
                text.Append('|');
            text.Append("0x").Append(rest.ToString("x"));
        }
        return text.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

    public string TypeName(int token)
    {
        if (token == 0)
            return "?";
        var handle = SRME.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return RawSignatureProvider.TypeDefinitionName(_reader, (TypeDefinitionHandle)handle);
            case HandleKind.TypeReference:
                return RawSignatureProvider.TypeReferenceName(_reader, (TypeReferenceHandle)handle);
            case HandleKind.TypeSpecification:
                return _reader.GetTypeSpecification((TypeSpecificationHandle)handle)
                    .DecodeSignature(RawSignatureProvider.Instance, null);
        }
        return "0x" + token.ToString("x8");
    }

    public string Member(int token)
    {
        if (token == 0)
            return "?";
        var handle = SRME.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var method = _reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                return TypeName(SRME.GetToken(method.GetDeclaringType())) + "::" + _reader.GetString(method.Name);
            }
            case HandleKind.FieldDefinition:
            {
                var field = _reader.GetFieldDefinition((FieldDefinitionHandle)handle);
                return TypeName(SRME.GetToken(field.GetDeclaringType())) + "::" + _reader.GetString(field.Name);
            }
            case HandleKind.MemberReference:
            {
                var member = _reader.GetMemberReference((MemberReferenceHandle)handle);
                string parent = member.Parent.Kind is HandleKind.TypeDefinition or HandleKind.TypeReference
                    or HandleKind.TypeSpecification
                    ? TypeName(SRME.GetToken(member.Parent)) : "";
                return parent + "::" + _reader.GetString(member.Name);
            }
            case HandleKind.MethodSpecification:
            {
                var spec = _reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                var args = spec.DecodeSignature(RawSignatureProvider.Instance, null);
                var text = new StringBuilder(Member(SRME.GetToken(spec.Method))).Append('<');
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0)
                        text.Append(',');
                    text.Append(args[i]);
                }
                return text.Append('>').ToString();
            }
        }
        return "0x" + token.ToString("x8");
    }
}

/// <summary>Summarizes the method bodies of one module. Holds the per-module caches: call
/// token classification, field shapes, and the field tables a compiler-field read consults.</summary>
internal sealed class ReflectionSummarizer
{
    internal const int MaxAlternatives = 16;
    internal const int MaxTerms = 4096;
    internal const int MaxReruns = 3;
    internal const int MaxNestDepth = 4;
    internal const int MaxTableRounds = 4;

    internal readonly MetadataReader Reader;
    private readonly PEReader _pe;
    private readonly Dictionary<int, CallInfo?> _calls = new();
    private readonly Dictionary<int, byte> _compilerClasses = new();
    private readonly Dictionary<int, FieldShape> _shapes = new();
    // Keyed by the root TypeDef of a nest.
    private readonly Dictionary<int, FieldTable> _tables = new();
    private int _tableDepth;

    public ReflectionSummarizer(MetadataReader reader, PEReader pe)
    {
        Reader = reader;
        _pe = pe;
    }

    /// <summary>Summarizes <paramref name="method"/>, whose decoded body is
    /// <paramref name="code"/>. Never throws on well-formed metadata.</summary>
    public ReflectionSummary Summarize(MethodDefinitionHandle method, MethodBodyBlock body, List<Instruction> code)
    {
        List<int>? sites = null;
        for (int i = 0; i < code.Count; i++)
        {
            var op = code[i].OpCode;
            if (op is not (ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Newobj or ILOpCode.Ldftn
                or ILOpCode.Ldvirtftn))
                continue;
            if (Call(code[i].Token) is { } call
                && (call.Api is { Kind: not ReflectionOpKind.Transfer } || call.ArraySearch))
                (sites ??= new List<int>()).Add(i);
        }
        if (sites is null)
            return ReflectionSummary.Empty;
        return new SummaryInterpreter(this, method, body, code, sites, null).Run();
    }

    internal sealed class CallInfo
    {
        public string Parent = "";
        public string Name = "";
        public string[] Parameters = Array.Empty<string>();
        public bool HasThis;
        public bool ReturnsVoid;
        public bool ReturnsString;
        /// <summary>The declaring type's token: TypeDef, TypeRef or TypeSpec; 0 when none.</summary>
        public int DeclaringType;
        /// <summary>The TypeDef of the declaring type or of its generic definition when it is
        /// in this module; 0 otherwise.</summary>
        public int DefinitionType;
        public string[]? TypeArguments;
        public ReflectionApi? Api;
        public bool ArraySearch;
    }

    internal CallInfo? Call(int token)
    {
        if (_calls.TryGetValue(token, out var cached))
            return cached;
        var info = DecodeCall(token);
        _calls.Add(token, info);
        return info;
    }

    private CallInfo? DecodeCall(int token)
    {
        if (token == 0 || (uint)token >> 24 is not (0x06 or 0x0A or 0x2B))
            return null;
        var handle = SRME.EntityHandle(token);
        string[]? typeArguments = null;
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            var spec = Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            typeArguments = ToArray(spec.DecodeSignature(RawSignatureProvider.Instance, null));
            handle = spec.Method;
        }
        var info = new CallInfo { TypeArguments = typeArguments };
        MethodSignature<string> signature;
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var method = Reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            var declaring = method.GetDeclaringType();
            info.Name = Reader.GetString(method.Name);
            info.Parent = RawSignatureProvider.TypeDefinitionName(Reader, declaring);
            info.DeclaringType = SRME.GetToken(declaring);
            info.DefinitionType = info.DeclaringType;
            signature = method.DecodeSignature(RawSignatureProvider.Instance, null);
        }
        else if (handle.Kind == HandleKind.MemberReference)
        {
            var member = Reader.GetMemberReference((MemberReferenceHandle)handle);
            if (member.GetKind() != MemberReferenceKind.Method)
                return null;
            info.Name = Reader.GetString(member.Name);
            var parent = member.Parent;
            if (parent.Kind == HandleKind.MethodDefinition)
                parent = Reader.GetMethodDefinition((MethodDefinitionHandle)parent).GetDeclaringType();
            if (parent.Kind is HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification)
            {
                info.DeclaringType = SRME.GetToken(parent);
                var definition = DefinitionOf((EntityHandle)parent);
                info.DefinitionType = definition.Kind == HandleKind.TypeDefinition ? SRME.GetToken(definition) : 0;
                info.Parent = definition.Kind switch
                {
                    HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(Reader, (TypeDefinitionHandle)definition),
                    HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(Reader, (TypeReferenceHandle)definition),
                    _ => "",
                };
            }
            signature = member.DecodeMethodSignature(RawSignatureProvider.Instance, null);
        }
        else
            return null;
        info.Parameters = ToArray(signature.ParameterTypes);
        info.HasThis = signature.Header.IsInstance;
        info.ReturnsVoid = signature.ReturnType == "System.Void";
        info.ReturnsString = signature.ReturnType == "System.String";
        info.ArraySearch = info.Parent == "System.Array" && info.Name is "IndexOf" or "LastIndexOf"
            && !info.HasThis && signature.GenericParameterCount == 0
            && info.Parameters.Length >= 2 && info.Parameters[0] == "System.Array";
        if (ReflectionApis.IsCandidateName(info.Name) || IsTransferName(info.Name))
            info.Api = ReflectionApis.Classify(info.Parent, info.Name, info.Parameters, info.HasThis,
                signature.GenericParameterCount);
        return info;
    }

    // The members the table classifies as Transfer, whose names IsCandidateName omits.
    private static bool IsTransferName(string name) => name is
        "GetTypeInfo" or "AsType" or "get_TypeHandle" or "GetEnumerator" or "get_Current" or "First" or "FirstOrDefault" or "Single"
        or "SingleOrDefault" or "Last" or "LastOrDefault" or "ElementAt" or "ElementAtOrDefault" or "Where"
        or "ToArray" or "ToList" or "OfType" or "Cast" or "AsEnumerable";

    /// <summary>The generic definition of a TypeSpec instantiation, or the handle itself.</summary>
    internal EntityHandle DefinitionOf(EntityHandle type)
    {
        if (type.Kind != HandleKind.TypeSpecification)
            return type;
        var blob = Reader.GetBlobReader(Reader.GetTypeSpecification((TypeSpecificationHandle)type).Signature);
        if (blob.RemainingBytes < 2 || blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
            return type;
        if (blob.ReadSignatureTypeCode() != SignatureTypeCode.TypeHandle)
            return type;
        return blob.ReadTypeHandle();
    }

    /// <summary>The TypeDef declaring a field token and the field's name; the TypeDef is 0
    /// when the field is not defined in this module.</summary>
    internal (int Type, string Name) FieldOwner(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.FieldDefinition)
        {
            var field = Reader.GetFieldDefinition((FieldDefinitionHandle)handle);
            return (SRME.GetToken(field.GetDeclaringType()), Reader.GetString(field.Name));
        }
        if (handle.Kind == HandleKind.MemberReference)
        {
            var member = Reader.GetMemberReference((MemberReferenceHandle)handle);
            string name = Reader.GetString(member.Name);
            if (member.Parent.Kind is HandleKind.TypeDefinition or HandleKind.TypeSpecification)
            {
                var definition = DefinitionOf(member.Parent);
                if (definition.Kind == HandleKind.TypeDefinition)
                    return (SRME.GetToken(definition), name);
            }
            return (0, name);
        }
        return (0, "");
    }

    /// <summary>The parent type name and member name of a field token.</summary>
    internal (string Parent, string Name) FieldName(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.FieldDefinition)
        {
            var field = Reader.GetFieldDefinition((FieldDefinitionHandle)handle);
            return (RawSignatureProvider.TypeDefinitionName(Reader, field.GetDeclaringType()), Reader.GetString(field.Name));
        }
        if (handle.Kind == HandleKind.MemberReference)
        {
            var member = Reader.GetMemberReference((MemberReferenceHandle)handle);
            string parent = member.Parent.Kind switch
            {
                HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(Reader, (TypeReferenceHandle)member.Parent),
                HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(Reader, (TypeDefinitionHandle)member.Parent),
                _ => "",
            };
            return (parent, Reader.GetString(member.Name));
        }
        return ("", "");
    }

    /// <summary>Whether a TypeDef is a C# lambda display class: a nested
    /// <c>&lt;&gt;c__DisplayClass</c> type marked [CompilerGenerated].</summary>
    internal bool IsDisplayClass(int typeDef) => CompilerClass(typeDef) == DisplayClassKind;

    private const byte DisplayClassKind = 1, DelegateCacheKind = 2;

    // A nested [CompilerGenerated] type whose fields only the compiler writes, and only from
    // the methods of its declaring type and of that type's nested types: a display class, or
    // the <>c / <>O class that caches lambda and method-group delegates.
    private byte CompilerClass(int typeDef)
    {
        if (typeDef == 0 || (uint)typeDef >> 24 != 0x02)
            return 0;
        if (_compilerClasses.TryGetValue(typeDef, out byte cached))
            return cached;
        var type = Reader.GetTypeDefinition((TypeDefinitionHandle)SRME.EntityHandle(typeDef));
        string name = Reader.GetString(type.Name);
        byte result = 0;
        if (!type.GetDeclaringType().IsNil && IsCompilerGenerated(type))
        {
            if (name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal))
                result = DisplayClassKind;
            else if (name is "<>c" or "<>O")
                result = DelegateCacheKind;
        }
        _compilerClasses.Add(typeDef, result);
        return result;
    }

    private bool IsCompilerGenerated(TypeDefinition type)
    {
        foreach (var handle in type.GetCustomAttributes())
        {
            var ctor = Reader.GetCustomAttribute(handle).Constructor;
            string name = "";
            if (ctor.Kind == HandleKind.MemberReference)
            {
                var parent = Reader.GetMemberReference((MemberReferenceHandle)ctor).Parent;
                if (parent.Kind == HandleKind.TypeReference)
                    name = RawSignatureProvider.TypeReferenceName(Reader, (TypeReferenceHandle)parent);
            }
            else if (ctor.Kind == HandleKind.MethodDefinition)
                name = RawSignatureProvider.TypeDefinitionName(Reader,
                    Reader.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType());
            if (name == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                return true;
        }
        return false;
    }

    internal enum FieldModel : byte
    {
        None,
        /// <summary>A field of a compiler class: every store anywhere in the nest of the
        /// class's declaring type contributes.</summary>
        Compiler,
    }

    /// <summary>How the summary models the field a token names.</summary>
    internal sealed class FieldShape
    {
        public FieldModel Model;
        /// <summary>The TypeDef whose nest table holds the field.</summary>
        public int Root;
        public string Key = "";
        public bool Static;
        public string Type = "";
        public SummaryInterpreter.CliDefault Default;
        /// <summary>The field is an SZ array only the root's nest can reach, so the elements
        /// stored through it are modeled.</summary>
        public bool Array;
    }

    private static readonly FieldShape Unmodeled = new();

    internal FieldShape ShapeOf(int token)
    {
        if (_shapes.TryGetValue(token, out var cached))
            return cached;
        var shape = DecodeShape(token);
        _shapes.Add(token, shape);
        return shape;
    }

    private FieldShape DecodeShape(int token)
    {
        var (owner, name) = FieldOwner(token);
        if (owner == 0)
            return Unmodeled;
        var ownerHandle = (TypeDefinitionHandle)SRME.EntityHandle(owner);
        var ownerType = Reader.GetTypeDefinition(ownerHandle);
        FieldDefinitionHandle handle = default;
        foreach (var candidate in ownerType.GetFields())
        {
            if (Reader.GetString(Reader.GetFieldDefinition(candidate).Name) != name)
                continue;
            // Two fields of one name differ only by type, which a name cannot tell apart.
            if (!handle.IsNil)
                return Unmodeled;
            handle = candidate;
        }
        if (handle.IsNil)
            return Unmodeled;
        if (CompilerClass(owner) == 0)
            return Unmodeled;
        var field = Reader.GetFieldDefinition(handle);
        return new FieldShape
        {
            Model = FieldModel.Compiler, Root = SRME.GetToken(ownerType.GetDeclaringType()),
            Key = ClosureKey(owner, name),
            Static = (field.Attributes & System.Reflection.FieldAttributes.Static) != 0,
            Array = IsSZArray(field),
            Type = field.DecodeSignature(RawSignatureProvider.Instance, null),
            Default = SummaryInterpreter.FieldDefault(field),
        };
    }

    private bool IsSZArray(FieldDefinition field)
    {
        var blob = Reader.GetBlobReader(field.Signature);
        if (blob.RemainingBytes < 2 || blob.ReadSignatureHeader().Kind != SignatureKind.Field)
            return false;
        var code = blob.ReadSignatureTypeCode();
        while (code is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
        {
            blob.ReadTypeHandle();
            code = blob.ReadSignatureTypeCode();
        }
        return code == SignatureTypeCode.SZArray;
    }

    internal sealed class FieldContribution
    {
        public required SummaryInterpreter Source;
        public required RValue Value;
        /// <summary>For an element store, the element index, or -1 for any.</summary>
        public long Index = -1;
        /// <summary>A fresh default contribution keeps its element token for typed array loads.</summary>
        public int DefaultElementToken;
    }

    /// <summary>What one round of a table knows about one field.</summary>
    internal sealed class FieldState
    {
        public readonly List<FieldContribution> Values = new();
        /// <summary>The elements stored into the arrays the field holds.</summary>
        public readonly List<FieldContribution> Elements = new();
        /// <summary>A store the table cannot see: an address taken, an undecodable or
        /// over-budget body, or a generic method whose method type parameters a display class
        /// renumbers.</summary>
        public bool Opaque;
        /// <summary>An array the field holds reaches code that may write it.</summary>
        public bool ArrayEscaped;
        public bool HoldsArray;
        /// <summary>A body stores an element through the field, into whichever array it
        /// holds then.</summary>
        public bool ElementWritten;
        /// <summary>The length every array allocated into the field shares; -1 when they
        /// differ or one is unknown, -2 before any.</summary>
        public long Length = -2;
    }

    /// <summary>The modeled fields of one root type's nest: the root and its nested types.
    /// A table is built in rounds; a body interpreted while its own table is built reads the
    /// previous complete round, or unknown before the first, so every round over-approximates
    /// the values the next one computes and any round is a sound answer.</summary>
    internal sealed class FieldTable
    {
        public required int Root;
        /// <summary>The last complete round; null before the first completes.</summary>
        public Dictionary<string, FieldState>? Done;
        public readonly List<string> Keys = new();
        public readonly Dictionary<string, int> KeyIndex = new();
        public readonly HashSet<string> Pinned = new();
        public bool Complete;
        public bool ReadWhileBuilding;
        public bool AllOpaque;

        public int IndexOf(string key)
        {
            if (KeyIndex.TryGetValue(key, out int index))
                return index;
            index = Keys.Count;
            Keys.Add(key);
            KeyIndex.Add(key, index);
            return index;
        }
    }

    internal static string ClosureKey(int type, string field) => type.ToString() + ":" + field;

    /// <summary>The table of <paramref name="root"/>'s nest: complete, under construction
    /// when a body it interprets reads it, or null past the nesting limit.</summary>
    internal FieldTable? TableOf(int root)
    {
        if (_tables.TryGetValue(root, out var table))
            return table;
        if (_tableDepth >= MaxNestDepth)
            return null;
        table = new FieldTable { Root = root };
        _tables.Add(root, table);
        _tableDepth++;
        BuildTable(table);
        _tableDepth--;
        table.Complete = true;
        return table;
    }

    private sealed class TableBody
    {
        public required MethodDefinitionHandle Handle;
        public required MethodBodyBlock Body;
        public required List<Instruction> Code;
        public required bool Generic;
        public readonly List<string> Touched = new();
    }

    private void BuildTable(FieldTable table)
    {
        var rootHandle = (TypeDefinitionHandle)SRME.EntityHandle(table.Root);
        var types = new List<TypeDefinitionHandle> { rootHandle };
        var depths = new List<int> { 0 };
        for (int i = 0; i < types.Count; i++)
        {
            var nestedTypes = Reader.GetTypeDefinition(types[i]).GetNestedTypes();
            if (depths[i] >= MaxNestDepth)
            {
                // A store in a type past the limit would go unseen.
                if (nestedTypes.Length > 0)
                    table.AllOpaque = true;
                continue;
            }
            foreach (var nested in nestedTypes)
            {
                types.Add(nested);
                depths.Add(depths[i] + 1);
            }
        }
        var bodies = new List<TableBody>();
        foreach (var type in types)
        {
            foreach (var methodHandle in Reader.GetTypeDefinition(type).GetMethods())
            {
                var method = Reader.GetMethodDefinition(methodHandle);
                if (method.RelativeVirtualAddress == 0)
                    continue;
                var body = _pe.GetMethodBody(method.RelativeVirtualAddress);
                var il = body.GetILBytes();
                var code = il is null ? null : ILDecoder.TryDecode(ImmutableArray.Create(il));
                if (code is null)
                {
                    table.AllOpaque = true;
                    continue;
                }
                TableBody? entry = null;
                foreach (var insn in code)
                {
                    if (insn.OpCode is not (ILOpCode.Stfld or ILOpCode.Stsfld or ILOpCode.Ldflda or ILOpCode.Ldsflda
                        or ILOpCode.Ldfld or ILOpCode.Ldsfld))
                        continue;
                    var shape = ShapeOf(insn.Token);
                    if (shape.Model == FieldModel.None || shape.Root != table.Root)
                        continue;
                    table.IndexOf(shape.Key);
                    bool interpret;
                    if (insn.OpCode is ILOpCode.Ldflda or ILOpCode.Ldsflda)
                    {
                        table.Pinned.Add(shape.Key);
                        interpret = false;
                    }
                    else if (insn.OpCode is ILOpCode.Stfld or ILOpCode.Stsfld)
                        interpret = true;
                    else
                        interpret = shape.Array;
                    if (!interpret)
                        continue;
                    entry ??= new TableBody
                    {
                        Handle = methodHandle, Body = body, Code = code, Generic = method.GetGenericParameters().Count > 0,
                    };
                    if (!entry.Touched.Contains(shape.Key))
                        entry.Touched.Add(shape.Key);
                }
                if (entry is not null)
                    bodies.Add(entry);
            }
        }
        if (table.AllOpaque)
            return;
        string? previous = null;
        for (int round = 0; ; round++)
        {
            table.ReadWhileBuilding = false;
            var states = new Dictionary<string, FieldState>();
            foreach (string key in table.Keys)
                states.Add(key, new FieldState { Opaque = table.Pinned.Contains(key) });
            foreach (var body in bodies)
            {
                var sink = new FieldSink(table);
                var interpreter = new SummaryInterpreter(this, body.Handle, body.Body, body.Code, new List<int>(), sink);
                var summary = interpreter.Run();
                if (summary.OverBudget || summary.StackShapeMismatch || body.Generic)
                {
                    foreach (string key in body.Touched)
                        states[key].Opaque = true;
                    continue;
                }
                interpreter.CommitSink(states);
            }
            string canonical = Canonical(table, states);
            bool settled = !table.ReadWhileBuilding || canonical == previous || round + 1 == MaxTableRounds;
            table.Done = states;
            if (settled)
                return;
            previous = canonical;
        }
    }

    // A rendering of a round that is equal for equal rounds, whatever term ids each body
    // assigned.
    private static string Canonical(FieldTable table, Dictionary<string, FieldState> states)
    {
        var text = new StringBuilder();
        foreach (string key in table.Keys)
        {
            var state = states[key];
            text.Append(key).Append(state.Opaque ? " o" : "").Append(state.ArrayEscaped ? " e" : "")
                .Append(state.HoldsArray ? " a" : "").Append(state.ElementWritten ? " w" : "")
                .Append(' ').Append(state.Length).Append(':');
            foreach (var c in state.Values)
                c.Source.Canonical(text, c.Value);
            text.Append('|');
            foreach (var c in state.Elements)
            {
                text.Append(c.Index).Append('=');
                c.Source.Canonical(text, c.Value);
            }
            text.Append('\n');
        }
        return text.ToString();
    }

    private static string[] ToArray(ImmutableArray<string> items)
    {
        var result = new string[items.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = items[i];
        return result;
    }
}

/// <summary>What one body stores into the fields of the table being built, recorded by the
/// pass that ends the body's interpretation.</summary>
internal sealed class FieldSink
{
    public readonly ReflectionSummarizer.FieldTable Table;
    /// <summary>The fields stored or written through, in first-store order.</summary>
    public readonly List<string> Keys = new();
    public readonly Dictionary<string, RValue> Values = new();
    /// <summary>The allocation sites stored into each array field.</summary>
    public readonly Dictionary<string, List<int>> Sites = new();
    /// <summary>The slots written on every path before an allocation is stored into a field.</summary>
    public readonly Dictionary<int, HashSet<long>> Written = new();
    public readonly Dictionary<string, List<(long Index, RValue Value)>> Elements = new();
    public readonly HashSet<string> Escaped = new();

    public FieldSink(ReflectionSummarizer.FieldTable table) => Table = table;

    public void Clear()
    {
        Keys.Clear();
        Values.Clear();
        Sites.Clear();
        Written.Clear();
        Elements.Clear();
        Escaped.Clear();
    }

    public void Touch(string key)
    {
        if (!Keys.Contains(key))
            Keys.Add(key);
    }

    public void Escape(string key)
    {
        Touch(key);
        Escaped.Add(key);
    }

    public void AddSite(string key, int site, HashSet<(int Site, long Slot)> written)
    {
        Touch(key);
        if (!Sites.TryGetValue(key, out var sites))
            Sites.Add(key, sites = new List<int>());
        if (!sites.Contains(site))
            sites.Add(site);
        var slots = new HashSet<long>();
        foreach (var entry in written)
            if (entry.Site == site)
                slots.Add(entry.Slot);
        if (Written.TryGetValue(site, out var previous))
            previous.IntersectWith(slots);
        else
            Written.Add(site, slots);
    }

    public void AddValue(string key, RValue value, SummaryInterpreter interpreter)
    {
        Touch(key);
        Values[key] = Values.TryGetValue(key, out var previous) ? interpreter.JoinValues(previous, value) : value;
    }

    public void AddElement(string key, long index, RValue value, SummaryInterpreter interpreter)
    {
        Touch(key);
        if (!Elements.TryGetValue(key, out var elements))
            Elements.Add(key, elements = new List<(long, RValue)>());
        for (int i = 0; i < elements.Count; i++)
            if (elements[i].Index == index)
            {
                elements[i] = (index, interpreter.JoinValues(elements[i].Value, value));
                return;
            }
        elements.Add((index, value));
    }
}

/// <summary>The abstract interpreter behind one summary: a forward dataflow over the
/// body's blocks whose values are term sets. Blocks run in ascending leader order; a join
/// unions alternatives. A value with more than <see cref="ReflectionSummarizer.MaxAlternatives"/>
/// alternatives, or one that mixes value categories, collapses to an unknown. Arrays are
/// modeled per allocation site and index, flow-insensitively; an array that reaches code
/// which may write it is escaped and the body is interpreted again.</summary>
internal sealed class SummaryInterpreter
{
    // Values of different categories never meet in a well-typed local or stack slot; a
    // join that mixes them marks a slot the summary cannot follow. Type, member, parameter
    // and assembly share one category because each is a custom-attribute provider.
    private enum Category : byte { None, Member, String, Int, Sequence, Delegate, Object, Closure }

    internal enum CliDefault : byte { Unknown, Zero, Null }

    // Raw signatures distinguish zero and null defaults without resolving assemblies.
    private sealed class CliDefaultProvider : ISignatureTypeProvider<CliDefault, object?>
    {
        public static readonly CliDefaultProvider Instance = new();

        public CliDefault GetPrimitiveType(PrimitiveTypeCode code) => code switch
        {
            PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
                => CliDefault.Zero,
            PrimitiveTypeCode.Object or PrimitiveTypeCode.String => CliDefault.Null,
            _ => CliDefault.Unknown,
        };

        public CliDefault GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            if ((SignatureTypeKind)rawTypeKind == SignatureTypeKind.Class)
                return CliDefault.Null;
            var baseType = reader.GetTypeDefinition(handle).BaseType;
            string? baseName = baseType.IsNil ? null : baseType.Kind switch
            {
                HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(reader, (TypeReferenceHandle)baseType),
                HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(reader, (TypeDefinitionHandle)baseType),
                _ => null,
            };
            return baseName == "System.Enum" || IsZeroDefault(RawSignatureProvider.TypeDefinitionName(reader, handle))
                ? CliDefault.Zero : CliDefault.Unknown;
        }

        public CliDefault GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            (SignatureTypeKind)rawTypeKind == SignatureTypeKind.Class ? CliDefault.Null
                : IsZeroDefault(RawSignatureProvider.TypeReferenceName(reader, handle)) ? CliDefault.Zero : CliDefault.Unknown;
        public CliDefault GetSZArrayType(CliDefault elementType) => CliDefault.Null;
        public CliDefault GetArrayType(CliDefault elementType, ArrayShape shape) => CliDefault.Null;
        public CliDefault GetByReferenceType(CliDefault elementType) => CliDefault.Unknown;
        public CliDefault GetPointerType(CliDefault elementType) => CliDefault.Unknown;
        public CliDefault GetFunctionPointerType(MethodSignature<CliDefault> signature) => CliDefault.Unknown;
        public CliDefault GetGenericInstantiation(CliDefault genericType, ImmutableArray<CliDefault> typeArguments) => genericType;
        public CliDefault GetGenericMethodParameter(object? genericContext, int index) => CliDefault.Unknown;
        public CliDefault GetGenericTypeParameter(object? genericContext, int index) => CliDefault.Unknown;
        public CliDefault GetModifiedType(CliDefault modifier, CliDefault unmodifiedType, bool isRequired) => unmodifiedType;
        public CliDefault GetPinnedType(CliDefault elementType) => elementType;
        public CliDefault GetTypeFromSpecification(MetadataReader reader, object? genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }

    internal static CliDefault FieldDefault(FieldDefinition field) =>
        field.DecodeSignature(CliDefaultProvider.Instance, null);

    private enum Flow : byte { Next, Stop, Branched }

    private const byte AddressPin = 1, HandlerPin = 2;

    private sealed class State
    {
        public required RValue[] Stack;
        public required RValue[] Locals;
        public required RValue[] Args;
        // The (allocation site, constant index) slots every path stored into since that
        // site's latest allocation: only those can no longer hold the default element.
        public required HashSet<(int Site, long Slot)> Written;
    }

    private readonly struct Region
    {
        public readonly int TryStart, TryEnd, Handler, Filter;
        public readonly bool Catches;

        public Region(int tryStart, int tryEnd, int handler, int filter, bool catches)
        {
            TryStart = tryStart;
            TryEnd = tryEnd;
            Handler = handler;
            Filter = filter;
            Catches = catches;
        }

        public bool Overlaps(Region other) => TryStart < other.TryEnd && other.TryStart < TryEnd;
    }

    private sealed class Allocation
    {
        public long Length = -2;
        public readonly Dictionary<long, RValue> Slots = new();
        // Read in this pass before a later store changed it: the pass must run again.
        public bool Read;
        // A fresh array a call returns holds the elements of Source, a sequence snapshot,
        // until the body stores into it.
        public bool Sourced;
        public RValue Source;
        // The newarr element type token fixes the array's type and default element.
        public int ElementToken;
    }

    private readonly ReflectionSummarizer _s;
    private readonly MetadataReader _r;
    private readonly List<Instruction> _code;
    private readonly List<int> _siteIndexes;
    private readonly FieldSink? _sink;
    private readonly int[] _offsets;
    private readonly int[] _blockStart;
    private readonly int[] _blockOf;
    private readonly Region[] _regions;
    private readonly bool[] _inTry;
    // The region whose filter block holds each instruction, or -1.
    private readonly int[] _filterOf;
    private readonly bool[] _receiverOnly;
    private readonly byte[] _localPin;
    private readonly ImmutableArray<CliDefault> _localDefaults;
    private readonly ImmutableArray<string> _localTypes;
    private readonly ImmutableArray<string> _parameterTypes;
    private readonly bool _initLocals;
    private readonly byte[] _argPin;
    private readonly int _method;
    private readonly bool _hasThis;
    private readonly int _thisClosure;
    private readonly bool _malformed;
    private readonly int _maxVisits;
    private readonly HashSet<int> _escaped = new();
    private readonly HashSet<int> _repeatedArrays = new();
    // Allocations stored into an array field of the table being built: other bodies write
    // them through the field, so a read through any other alias is unknown.
    private readonly HashSet<int> _shared = new();

    internal List<RTerm> Terms = new();
    private List<Category> _categories = new();
    private Dictionary<string, int> _termIds = new();
    private List<RUnknown> _unknowns = new();
    private Dictionary<(byte, int, int), int> _unknownIds = new();
    private State?[] _in = Array.Empty<State?>();
    private bool[] _pending = Array.Empty<bool>();
    private int _low;
    private Dictionary<int, Allocation> _allocations = new();
    private Dictionary<int, RSite> _recorded = new();
    private Dictionary<int, RValue> _arraySearchTypes = new();
    private Dictionary<string, RValue> _fieldReads = new();
    private bool _budget, _stackShape, _newEscape, _heapChanged;
    private int _visits, _reruns, _null = -1;

    private List<RValue> _stack = new();
    private RValue[] _locals = Array.Empty<RValue>();
    private RValue[] _args = Array.Empty<RValue>();
    private HashSet<(int Site, long Slot)> _written = new();

    internal SummaryInterpreter(ReflectionSummarizer summarizer, MethodDefinitionHandle method, MethodBodyBlock body,
        List<Instruction> code, List<int> siteIndexes, FieldSink? sink)
    {
        _s = summarizer;
        _r = summarizer.Reader;
        _code = code;
        _siteIndexes = siteIndexes;
        _sink = sink;
        int n = code.Count;
        bool hasNewArray = false;
        _offsets = new int[n];
        for (int i = 0; i < n; i++)
        {
            _offsets[i] = code[i].Offset;
            hasNewArray |= code[i].OpCode == ILOpCode.Newarr;
        }

        _method = SRME.GetToken(method);
        var definition = _r.GetMethodDefinition(method);
        var signature = definition.DecodeSignature(RawSignatureProvider.Instance, null);
        _hasThis = signature.Header.IsInstance;
        _parameterTypes = signature.ParameterTypes;
        int declaring = SRME.GetToken(definition.GetDeclaringType());
        _thisClosure = _hasThis && _s.IsDisplayClass(declaring) ? declaring : 0;
        int argCount = signature.ParameterTypes.Length + (_hasThis ? 1 : 0);
        _initLocals = body.LocalVariablesInitialized;
        _localDefaults = body.LocalSignature.IsNil ? ImmutableArray<CliDefault>.Empty
            : _r.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(CliDefaultProvider.Instance, null);
        _localTypes = body.LocalSignature.IsNil ? ImmutableArray<string>.Empty
            : _r.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(RawSignatureProvider.Instance, null);
        int localCount = _localDefaults.Length;
        foreach (var insn in code)
            if (LocalIndex(insn) is int local && local >= localCount)
                localCount = local + 1;
        _localPin = new byte[localCount];
        _argPin = new byte[argCount];
        _args = new RValue[argCount];
        _locals = new RValue[localCount];

        var leader = new bool[n + 1];
        bool malformed = false;
        if (n > 0)
            leader[0] = true;
        for (int i = 0; i < n; i++)
        {
            var insn = code[i];
            if (ILDecoder.IsBranch(insn.OpCode))
                malformed |= !MarkLeader(leader, (int)insn.Operand);
            else if (insn.OpCode == ILOpCode.Switch)
                foreach (int target in insn.SwitchTargets!)
                    malformed |= !MarkLeader(leader, target);
            if (ILDecoder.IsBranch(insn.OpCode) || insn.OpCode is ILOpCode.Switch or ILOpCode.Ret or ILOpCode.Throw
                or ILOpCode.Rethrow or ILOpCode.Endfinally or ILOpCode.Endfilter)
                leader[i + 1] = true;
        }
        var regions = body.ExceptionRegions;
        foreach (var region in regions)
        {
            malformed |= !MarkLeader(leader, region.TryOffset) || !MarkLeader(leader, region.HandlerOffset);
            MarkLeader(leader, region.TryOffset + region.TryLength);
            MarkLeader(leader, region.HandlerOffset + region.HandlerLength);
            if (region.Kind == ExceptionRegionKind.Filter)
                malformed |= !MarkLeader(leader, region.FilterOffset);
        }
        var starts = new List<int>();
        _blockOf = new int[n];
        for (int i = 0; i < n; i++)
        {
            if (leader[i])
                starts.Add(i);
            _blockOf[i] = starts.Count - 1;
        }
        _blockStart = starts.ToArray();
        _maxVisits = 8 * _blockStart.Length + 64;

        _inTry = new bool[n];
        _filterOf = new int[n];
        Array.Fill(_filterOf, -1);
        var built = new List<Region>();
        foreach (var region in regions)
        {
            int tryStart = IndexAtOrAfter(region.TryOffset), tryEnd = IndexAtOrAfter(region.TryOffset + region.TryLength);
            int handler = IndexOf(region.HandlerOffset);
            int filter = region.Kind == ExceptionRegionKind.Filter ? IndexOf(region.FilterOffset) : -1;
            if (handler < 0 || region.Kind == ExceptionRegionKind.Filter && filter < 0)
                continue;
            for (int i = tryStart; i < tryEnd; i++)
                _inTry[i] = true;
            bool catches = region.Kind is ExceptionRegionKind.Catch or ExceptionRegionKind.Filter;
            for (int i = filter < 0 ? handler : filter; i < handler; i++)
                _filterOf[i] = built.Count;
            built.Add(new Region(tryStart, tryEnd, _blockOf[handler], filter < 0 ? -1 : _blockOf[filter], catches));
            if (catches)
                continue;
            int handlerEnd = IndexAtOrAfter(region.HandlerOffset + region.HandlerLength);
            for (int i = handler; i < handlerEnd; i++)
            {
                if (LocalIndex(code[i]) is int local && IsStore(code[i].OpCode) && _localPin[local] == 0)
                    _localPin[local] = HandlerPin;
                if (ArgIndex(code[i]) is int arg && code[i].OpCode is ILOpCode.Starg or ILOpCode.Starg_s
                    && arg < argCount && _argPin[arg] == 0)
                    _argPin[arg] = HandlerPin;
            }
        }
        _regions = built.ToArray();
        if (hasNewArray)
            FindRepeatedArrays();

        _receiverOnly = new bool[n];
        for (int i = 0; i < n; i++)
        {
            var op = code[i].OpCode;
            if (op is not (ILOpCode.Ldloca or ILOpCode.Ldloca_s or ILOpCode.Ldarga or ILOpCode.Ldarga_s))
                continue;
            int j = i + 1;
            while (j < n && code[j].OpCode is ILOpCode.Constrained or ILOpCode.Readonly or ILOpCode.Volatile
                or ILOpCode.Unaligned or ILOpCode.Tail)
                j++;
            _receiverOnly[i] = j < n && code[j].OpCode is ILOpCode.Call or ILOpCode.Callvirt
                && _s.Call(code[j].Token) is { HasThis: true, Parameters.Length: 0 };
            if (_receiverOnly[i])
                continue;
            int slot = (int)code[i].Operand;
            if (op is ILOpCode.Ldloca or ILOpCode.Ldloca_s)
                _localPin[slot] = AddressPin;
            else if (slot < argCount)
                _argPin[slot] = AddressPin;
        }
        _malformed = malformed;
    }

    internal ReflectionSummary Run()
    {
        while (!_malformed)
        {
            ResetRun();
            // A pass that escapes an array runs to its end, so one rerun sees every escape
            // the pass could reach.
            bool clean = Drain();
            while (clean && _heapChanged && !_newEscape)
            {
                _heapChanged = false;
                foreach (var allocation in _allocations.Values)
                    allocation.Read = false;
                for (int b = 0; b < _in.Length; b++)
                    if (_in[b] is not null)
                        Enqueue(b);
                clean = Drain();
            }
            if (!_newEscape || _budget || _stackShape)
                break;
            if (_reruns == ReflectionSummarizer.MaxReruns)
            {
                _budget = true;
                break;
            }
            _reruns++;
        }
        if (_malformed)
            _stackShape = true;
        return Build();
    }

    // Sites spanned by backwards control flow may have older live arrays. A store
    // through their shared site identity cannot remove any instance's default.
    private void FindRepeatedArrays()
    {
        var ranges = new int[_code.Count + 1];
        for (int i = 0; i < _code.Count; i++)
        {
            var insn = _code[i];
            if (ILDecoder.IsBranch(insn.OpCode))
                AddRepeatedRange(ranges, i, IndexOf((int)insn.Operand));
            else if (insn.OpCode == ILOpCode.Switch)
                foreach (int target in insn.SwitchTargets!)
                    AddRepeatedRange(ranges, i, IndexOf(target));
        }
        for (int r = 0; r < _regions.Length; r++)
        {
            var region = _regions[r];
            int target = _blockStart[region.Filter >= 0 ? region.Filter : region.Handler];
            AddRepeatedRange(ranges, region.TryEnd - 1, target);
            if (region.Filter < 0)
                continue;
            // A declining or throwing filter also reaches every overlapping handler.
            for (int k = 0; k < _regions.Length; k++)
                if (k != r && _regions[k].Overlaps(region))
                {
                    var other = _regions[k];
                    AddRepeatedRange(ranges, _blockStart[region.Handler] - 1,
                        _blockStart[other.Filter >= 0 ? other.Filter : other.Handler]);
                }
        }
        int active = 0;
        for (int i = 0; i < _code.Count; i++)
        {
            active += ranges[i];
            if (active > 0 && _code[i].OpCode == ILOpCode.Newarr)
                _repeatedArrays.Add(_code[i].Offset);
        }
    }

    private static void AddRepeatedRange(int[] ranges, int source, int target)
    {
        if (target < 0 || target > source)
            return;
        ranges[target]++;
        ranges[source + 1]--;
    }

    private void ResetRun()
    {
        Terms = new List<RTerm>();
        _categories = new List<Category>();
        _termIds = new Dictionary<string, int>();
        _unknowns = new List<RUnknown>();
        _unknownIds = new Dictionary<(byte, int, int), int>();
        _in = new State?[_blockStart.Length];
        _pending = new bool[_blockStart.Length];
        _low = 0;
        _allocations = new Dictionary<int, Allocation>();
        _recorded = new Dictionary<int, RSite>();
        _arraySearchTypes = new Dictionary<int, RValue>();
        _fieldReads = new Dictionary<string, RValue>();
        _sink?.Clear();
        _newEscape = false;
        _heapChanged = false;
        _visits = 0;
        _null = -1;
        if (_blockStart.Length == 0)
            return;
        var args = new RValue[_args.Length];
        for (int i = 0; i < args.Length; i++)
            args[i] = _hasThis && i == 0
                ? _thisClosure != 0 ? Known(Term(TermKind.Closure, _thisClosure)) : Unknown(UnknownReason.ThisType)
                : Unknown(UnknownReason.Parameter, 0, i - (_hasThis ? 1 : 0));
        var locals = new RValue[_locals.Length];
        for (int i = 0; i < locals.Length; i++)
            locals[i] = (_initLocals && i < _localDefaults.Length ? _localDefaults[i] : CliDefault.Unknown) switch
            {
                CliDefault.Zero => IntValue(0),
                CliDefault.Null => Known(NullTerm()),
                _ => Unknown(UnknownReason.InitialLocal, 0, i),
            };
        _in[0] = new State { Stack = Array.Empty<RValue>(), Locals = locals, Args = args, Written = new() };
        Enqueue(0);
    }

    private bool Drain()
    {
        while (true)
        {
            int b = NextPending();
            if (b < 0)
                return true;
            if (++_visits > _maxVisits)
            {
                _budget = true;
                return false;
            }
            Visit(b);
            if (Terms.Count > ReflectionSummarizer.MaxTerms)
                _budget = true;
            if (_budget || _stackShape)
                return false;
        }
    }

    private int NextPending()
    {
        for (int b = _low; b < _pending.Length; b++)
        {
            if (!_pending[b])
                continue;
            _pending[b] = false;
            _low = b + 1;
            return b;
        }
        _low = _pending.Length;
        return -1;
    }

    private void Enqueue(int block)
    {
        _pending[block] = true;
        if (block < _low)
            _low = block;
    }

    private void Visit(int block)
    {
        var state = _in[block]!;
        _stack = new List<RValue>(state.Stack);
        _locals = (RValue[])state.Locals.Clone();
        _args = (RValue[])state.Args.Clone();
        _written = new HashSet<(int Site, long Slot)>(state.Written);
        int start = _blockStart[block];
        int end = block + 1 < _blockStart.Length ? _blockStart[block + 1] : _code.Count;
        if (_inTry[start])
            ToHandlers(start);
        if (_filterOf[start] >= 0)
            FromFilter(_filterOf[start], accepted: false);
        for (int i = start; i < end; i++)
        {
            var flow = Step(i, _code[i]);
            if (_stackShape || _budget || flow != Flow.Next)
                return;
        }
        if (end < _code.Count)
            FlowTo(end);
    }

    private void FlowTo(int index)
    {
        if (index < 0 || index >= _code.Count)
        {
            _stackShape = true;
            return;
        }
        Merge(_blockOf[index], _stack, _locals, _args, _written);
    }

    // A handler entry, reached with a null <paramref name="written"/>, may follow any
    // instruction of its range, so it knows no slot as written.
    private void Merge(int block, List<RValue> stack, RValue[] locals, RValue[] args,
        HashSet<(int Site, long Slot)>? written = null)
    {
        var state = _in[block];
        if (state is null)
        {
            _in[block] = new State
            {
                Stack = stack.ToArray(), Locals = (RValue[])locals.Clone(), Args = (RValue[])args.Clone(),
                Written = written is null ? new() : new HashSet<(int Site, long Slot)>(written),
            };
            Enqueue(block);
            return;
        }
        if (state.Stack.Length != stack.Count)
        {
            _stackShape = true;
            return;
        }
        int known = state.Written.Count;
        if (written is null)
            state.Written.Clear();
        else
            state.Written.IntersectWith(written);
        bool changed = state.Written.Count != known;
        for (int i = 0; i < stack.Count; i++)
            changed |= JoinSlot(state.Stack, i, stack[i]);
        for (int i = 0; i < locals.Length; i++)
            changed |= JoinSlot(state.Locals, i, locals[i]);
        for (int i = 0; i < args.Length; i++)
            changed |= JoinSlot(state.Args, i, args[i]);
        if (changed)
            Enqueue(block);
    }

    private bool JoinSlot(RValue[] slots, int i, RValue value)
    {
        var joined = Join(slots[i], value);
        if (Same(joined, slots[i]))
            return false;
        slots[i] = joined;
        return true;
    }

    // Every instruction of a protected range can transfer to its handlers, so the locals
    // after each store reach the handler entry. A filtered handler runs only after its
    // filter accepts, so its entry comes from the filter's exit.
    private void ToHandlers(int index)
    {
        foreach (var region in _regions)
            if (index >= region.TryStart && index < region.TryEnd)
                Raise(region);
    }

    private void Raise(Region region)
    {
        var stack = new List<RValue>();
        if (region.Catches)
            stack.Add(Unknown(UnknownReason.CaughtException));
        Merge(region.Filter >= 0 ? region.Filter : region.Handler, stack, _locals, _args);
    }

    // A filter that declines or throws lets the search, and the finally and fault handlers
    // that unwinding runs afterwards, see what the filter stored: every handler of a range
    // that overlaps the filtered one receives the filter's state.
    private void FromFilter(int filtered, bool accepted)
    {
        var region = _regions[filtered];
        if (accepted)
            Merge(region.Handler, new List<RValue> { Unknown(UnknownReason.CaughtException) }, _locals, _args);
        for (int k = 0; k < _regions.Length; k++)
            if (k != filtered && _regions[k].Overlaps(region))
                Raise(_regions[k]);
    }

    private Flow Step(int i, Instruction insn)
    {
        var op = insn.OpCode;
        switch (op)
        {
            case ILOpCode.Nop or ILOpCode.Volatile or ILOpCode.Readonly or ILOpCode.Tail or ILOpCode.Unaligned
                or ILOpCode.Constrained:
                break;
            case ILOpCode.Ldarg_0 or ILOpCode.Ldarg_1 or ILOpCode.Ldarg_2 or ILOpCode.Ldarg_3
                or ILOpCode.Ldarg_s or ILOpCode.Ldarg:
                Push(LoadArg(ArgIndex(insn)!.Value));
                break;
            case ILOpCode.Ldarga or ILOpCode.Ldarga_s:
            {
                int arg = (int)insn.Operand;
                Push(_receiverOnly[i] ? LoadArg(arg) : Unknown(UnknownReason.AddressTaken, 1, arg));
                break;
            }
            case ILOpCode.Starg or ILOpCode.Starg_s:
            {
                int arg = (int)insn.Operand;
                var value = Pop();
                if (arg < _args.Length && _argPin[arg] == 0)
                {
                    int parameter = arg - (_hasThis ? 1 : 0);
                    _args[arg] = parameter < 0 ? value : SlotStorageValue(value, _parameterTypes[parameter], op);
                }
                else
                    Escape(value);
                if (_inTry[i])
                    ToHandlers(i);
                if (_filterOf[i] >= 0)
                    FromFilter(_filterOf[i], accepted: false);
                break;
            }
            case ILOpCode.Ldloc_0 or ILOpCode.Ldloc_1 or ILOpCode.Ldloc_2 or ILOpCode.Ldloc_3
                or ILOpCode.Ldloc_s or ILOpCode.Ldloc:
                Push(LoadLocal(LocalIndex(insn)!.Value));
                break;
            case ILOpCode.Ldloca or ILOpCode.Ldloca_s:
            {
                int local = (int)insn.Operand;
                Push(_receiverOnly[i] ? LoadLocal(local) : Unknown(UnknownReason.AddressTaken, 0, local));
                break;
            }
            case ILOpCode.Stloc_0 or ILOpCode.Stloc_1 or ILOpCode.Stloc_2 or ILOpCode.Stloc_3
                or ILOpCode.Stloc_s or ILOpCode.Stloc:
            {
                int local = LocalIndex(insn)!.Value;
                var value = Pop();
                if (_localPin[local] == 0)
                    _locals[local] = local < _localTypes.Length ? SlotStorageValue(value, _localTypes[local], op) : value;
                else
                    Escape(value);
                if (_inTry[i])
                    ToHandlers(i);
                if (_filterOf[i] >= 0)
                    FromFilter(_filterOf[i], accepted: false);
                break;
            }
            case ILOpCode.Ldnull:
                Push(Known(NullTerm()));
                break;
            case ILOpCode.Ldc_i4_m1 or ILOpCode.Ldc_i4_0 or ILOpCode.Ldc_i4_1 or ILOpCode.Ldc_i4_2 or ILOpCode.Ldc_i4_3
                or ILOpCode.Ldc_i4_4 or ILOpCode.Ldc_i4_5 or ILOpCode.Ldc_i4_6 or ILOpCode.Ldc_i4_7 or ILOpCode.Ldc_i4_8:
                Push(IntValue((int)op - (int)ILOpCode.Ldc_i4_0));
                break;
            case ILOpCode.Ldc_i4_s or ILOpCode.Ldc_i4 or ILOpCode.Ldc_i8:
                Push(IntValue(insn.Operand));
                break;
            case ILOpCode.Ldc_r4 or ILOpCode.Ldc_r8 or ILOpCode.Sizeof:
                Push(Opcode(op));
                break;
            case ILOpCode.Ldstr:
                Push(Known(Term(TermKind.Str, 0,
                    _r.GetUserString(SRME.UserStringHandle(insn.Token & 0x00FFFFFF)))));
                break;
            case ILOpCode.Mkrefany or ILOpCode.Refanyval or ILOpCode.Refanytype:
                Escape(Pop());
                Push(Opcode(op));
                break;
            case ILOpCode.Dup:
            {
                var value = Pop();
                Push(value);
                Push(value);
                break;
            }
            case ILOpCode.Pop:
                Pop();
                break;
            case ILOpCode.Ret:
                if (_stack.Count > 0)
                    Escape(Pop());
                return Flow.Stop;
            case ILOpCode.Throw:
                Pop();
                return Flow.Stop;
            case ILOpCode.Endfilter:
                Pop();
                if (_filterOf[i] < 0)
                    _stackShape = true;
                else
                    FromFilter(_filterOf[i], accepted: true);
                return Flow.Stop;
            case ILOpCode.Rethrow or ILOpCode.Endfinally:
                return Flow.Stop;
            case ILOpCode.Or or ILOpCode.And or ILOpCode.Xor or ILOpCode.Add or ILOpCode.Sub:
            {
                var right = Pop();
                Push(IntOp(op, Pop(), right));
                break;
            }
            case ILOpCode.Mul or ILOpCode.Div or ILOpCode.Div_un or ILOpCode.Rem or ILOpCode.Rem_un
                or ILOpCode.Shl or ILOpCode.Shr or ILOpCode.Shr_un or ILOpCode.Add_ovf or ILOpCode.Add_ovf_un
                or ILOpCode.Sub_ovf or ILOpCode.Sub_ovf_un or ILOpCode.Mul_ovf or ILOpCode.Mul_ovf_un
                or ILOpCode.Ceq or ILOpCode.Cgt or ILOpCode.Cgt_un or ILOpCode.Clt or ILOpCode.Clt_un:
                Pop();
                Pop();
                Push(Opcode(op));
                break;
            case ILOpCode.Not or ILOpCode.Neg:
                Push(IntOp(op, Pop(), RValue.None));
                break;
            case ILOpCode.Conv_i1 or ILOpCode.Conv_u1 or ILOpCode.Conv_i2 or ILOpCode.Conv_u2 or ILOpCode.Conv_i4
                or ILOpCode.Conv_u4 or ILOpCode.Conv_i8 or ILOpCode.Conv_u8 or ILOpCode.Conv_r4 or ILOpCode.Conv_r8
                or ILOpCode.Conv_i or ILOpCode.Conv_u or ILOpCode.Conv_r_un or ILOpCode.Conv_ovf_i1
                or ILOpCode.Conv_ovf_u1 or ILOpCode.Conv_ovf_i2 or ILOpCode.Conv_ovf_u2 or ILOpCode.Conv_ovf_i4
                or ILOpCode.Conv_ovf_u4 or ILOpCode.Conv_ovf_i8 or ILOpCode.Conv_ovf_u8 or ILOpCode.Conv_ovf_i
                or ILOpCode.Conv_ovf_u or ILOpCode.Conv_ovf_i1_un or ILOpCode.Conv_ovf_u1_un
                or ILOpCode.Conv_ovf_i2_un or ILOpCode.Conv_ovf_u2_un or ILOpCode.Conv_ovf_i4_un
                or ILOpCode.Conv_ovf_u4_un or ILOpCode.Conv_ovf_i8_un or ILOpCode.Conv_ovf_u8_un
                or ILOpCode.Conv_ovf_i_un or ILOpCode.Conv_ovf_u_un:
                Push(Convert(op, Pop()));
                break;
            case ILOpCode.Isinst:
                Push(Join(Pop(), Known(NullTerm())));
                break;
            case ILOpCode.Ckfinite or ILOpCode.Castclass or ILOpCode.Unbox_any:
                Push(Pop());
                break;
            case ILOpCode.Ldlen:
                Push(Length(Pop()));
                break;
            case ILOpCode.Ldelem_i1 or ILOpCode.Ldelem_u1 or ILOpCode.Ldelem_i2 or ILOpCode.Ldelem_u2
                or ILOpCode.Ldelem_i4 or ILOpCode.Ldelem_u4 or ILOpCode.Ldelem_i8 or ILOpCode.Ldelem_i
                or ILOpCode.Ldelem_r4 or ILOpCode.Ldelem_r8 or ILOpCode.Ldelem_ref or ILOpCode.Ldelem:
            {
                var index = Pop();
                Push(LoadElement(Pop(), index, op, insn.Token));
                break;
            }
            case ILOpCode.Ldelema:
                Pop();
                Escape(Pop());
                Push(Opcode(op));
                break;
            case ILOpCode.Stelem_i1 or ILOpCode.Stelem_i2 or ILOpCode.Stelem_i4 or ILOpCode.Stelem_i8
                or ILOpCode.Stelem_i or ILOpCode.Stelem_r4 or ILOpCode.Stelem_r8 or ILOpCode.Stelem_ref
                or ILOpCode.Stelem:
            {
                var value = Pop();
                var index = Pop();
                StoreElement(Pop(), index, value, op, insn.Token);
                break;
            }
            case ILOpCode.Ldind_i1 or ILOpCode.Ldind_u1 or ILOpCode.Ldind_i2 or ILOpCode.Ldind_u2
                or ILOpCode.Ldind_i4 or ILOpCode.Ldind_u4 or ILOpCode.Ldind_i8 or ILOpCode.Ldind_i
                or ILOpCode.Ldind_r4 or ILOpCode.Ldind_r8 or ILOpCode.Ldind_ref or ILOpCode.Ldobj
                or ILOpCode.Localloc or ILOpCode.Unbox:
                Pop();
                Push(Opcode(op));
                break;
            case ILOpCode.Stind_i1 or ILOpCode.Stind_i2 or ILOpCode.Stind_i4 or ILOpCode.Stind_i8
                or ILOpCode.Stind_r4 or ILOpCode.Stind_r8 or ILOpCode.Stind_ref or ILOpCode.Stind_i
                or ILOpCode.Stobj:
            {
                var value = Pop();
                Pop();
                Escape(value);
                break;
            }
            case ILOpCode.Cpblk or ILOpCode.Initblk:
                Pop();
                Pop();
                Pop();
                break;
            case ILOpCode.Initobj:
                Pop();
                break;
            case ILOpCode.Br or ILOpCode.Br_s:
                FlowTo(IndexOf((int)insn.Operand));
                return Flow.Branched;
            case ILOpCode.Leave or ILOpCode.Leave_s:
                _stack.Clear();
                FlowTo(IndexOf((int)insn.Operand));
                return Flow.Branched;
            case ILOpCode.Brtrue or ILOpCode.Brtrue_s or ILOpCode.Brfalse or ILOpCode.Brfalse_s:
                Pop();
                FlowTo(IndexOf((int)insn.Operand));
                FlowTo(i + 1);
                return Flow.Branched;
            case ILOpCode.Beq or ILOpCode.Beq_s or ILOpCode.Bne_un or ILOpCode.Bne_un_s or ILOpCode.Bge
                or ILOpCode.Bge_s or ILOpCode.Bge_un or ILOpCode.Bge_un_s or ILOpCode.Bgt or ILOpCode.Bgt_s
                or ILOpCode.Bgt_un or ILOpCode.Bgt_un_s or ILOpCode.Ble or ILOpCode.Ble_s or ILOpCode.Ble_un
                or ILOpCode.Ble_un_s or ILOpCode.Blt or ILOpCode.Blt_s or ILOpCode.Blt_un or ILOpCode.Blt_un_s:
                Pop();
                Pop();
                FlowTo(IndexOf((int)insn.Operand));
                FlowTo(i + 1);
                return Flow.Branched;
            case ILOpCode.Switch:
                Pop();
                foreach (int target in insn.SwitchTargets!)
                    FlowTo(IndexOf(target));
                FlowTo(i + 1);
                return Flow.Branched;
            case ILOpCode.Call or ILOpCode.Callvirt:
                Call(i, insn);
                break;
            case ILOpCode.Newobj:
                NewObject(insn);
                break;
            case ILOpCode.Calli:
                Calli(insn);
                break;
            case ILOpCode.Ldfld:
                Pop();
                Push(LoadField(insn.Token, isStatic: false));
                break;
            case ILOpCode.Ldflda:
                Pop();
                Push(Unknown(UnknownReason.InstanceField, insn.Token));
                break;
            case ILOpCode.Stfld:
            {
                var value = Pop();
                Pop();
                StoreField(insn.Token, value);
                break;
            }
            case ILOpCode.Ldsfld:
                Push(LoadStatic(insn.Token));
                break;
            case ILOpCode.Ldsflda:
                Push(Unknown(UnknownReason.StaticField, insn.Token));
                break;
            case ILOpCode.Stsfld:
                StoreField(insn.Token, Pop());
                break;
            case ILOpCode.Newarr:
                Push(NewArray(insn.Offset, Pop(), insn.Token));
                break;
            case ILOpCode.Box:
                Pop();
                Push(Known(BoxedObject(insn.Token)));
                break;
            case ILOpCode.Ldftn:
                Push(Known(Term(TermKind.MemberToken, insn.Token)));
                break;
            case ILOpCode.Ldvirtftn:
                Pop();
                Push(Known(Term(TermKind.MemberToken, insn.Token, null, ReflectionSummary.VirtualTarget)));
                break;
            case ILOpCode.Ldtoken:
                Push(Known(Term((uint)insn.Token >> 24 is 0x01 or 0x02 or 0x1B ? TermKind.TypeToken : TermKind.MemberToken,
                    insn.Token)));
                break;
            default:
                _stackShape = true;
                return Flow.Stop;
        }
        return Flow.Next;
    }

    // The object a box makes. A box token that is a generic parameter or a class
    // instantiation may denote a reference type, whose box passes the object through
    // unchanged. Boxing a Nullable<X> makes an X or null: the object is the argument's
    // when it has a token, else an inexact box, whose consumer unwraps the Nullable.
    private int BoxedObject(int token)
    {
        var handle = SRME.EntityHandle(token);
        long number = 0;
        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var blob = _r.GetBlobReader(_r.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
            var code = blob.RemainingBytes < 2 ? SignatureTypeCode.Invalid : (SignatureTypeCode)blob.ReadByte();
            if (code is SignatureTypeCode.GenericTypeParameter or SignatureTypeCode.GenericMethodParameter)
                number = ReflectionSummary.InexactBox;
            else if (code == SignatureTypeCode.GenericTypeInstance)
            {
                if ((SignatureTypeKind)blob.ReadByte() == SignatureTypeKind.Class)
                    number = ReflectionSummary.InexactBox;
                else if (IsNullableDefinition(blob.ReadTypeHandle()))
                {
                    if (blob.ReadCompressedInteger() == 1 && blob.ReadSignatureTypeCode() == SignatureTypeCode.TypeHandle)
                        return Term(TermKind.ExactObject, SRME.GetToken(blob.ReadTypeHandle()));
                    number = ReflectionSummary.InexactBox;
                }
            }
        }
        return Term(TermKind.ExactObject, token, null, number);
    }

    private bool IsNullableDefinition(EntityHandle type) => type.Kind switch
    {
        HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(_r, (TypeReferenceHandle)type) == "System.Nullable`1",
        HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(_r, (TypeDefinitionHandle)type) == "System.Nullable`1",
        _ => false,
    };

    // Whether a delegate over the ldvirtftn token runs exactly that method: it is not
    // virtual, is final, or its type is sealed. Only a definition of this module tells.
    private bool DispatchesExactly(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodDefinition)
            return false;
        var method = _r.GetMethodDefinition((MethodDefinitionHandle)handle);
        if ((method.Attributes & System.Reflection.MethodAttributes.Virtual) == 0
            || (method.Attributes & System.Reflection.MethodAttributes.Final) != 0)
            return true;
        return (_r.GetTypeDefinition(method.GetDeclaringType()).Attributes & System.Reflection.TypeAttributes.Sealed) != 0;
    }

    private RValue LoadArg(int arg)
    {
        if (arg >= _args.Length)
            return Unknown(UnknownReason.Parameter, 0, arg - (_hasThis ? 1 : 0));
        if (_argPin[arg] != 0)
            return Unknown(_argPin[arg] == AddressPin ? UnknownReason.AddressTaken : UnknownReason.HandlerLocal, 1, arg);
        return _args[arg];
    }

    private RValue LoadLocal(int local)
    {
        if (_localPin[local] != 0)
            return Unknown(_localPin[local] == AddressPin ? UnknownReason.AddressTaken : UnknownReason.HandlerLocal, 0,
                local);
        return _locals[local];
    }

    private void Push(RValue value) => _stack.Add(value);

    private RValue Pop()
    {
        if (_stack.Count == 0)
        {
            _stackShape = true;
            return Unknown(UnknownReason.StackShape);
        }
        var value = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);
        return value;
    }

    private RValue[] PopArguments(int count)
    {
        var args = new RValue[count];
        for (int k = count - 1; k >= 0; k--)
            args[k] = Pop();
        return args;
    }

    private void Call(int i, Instruction insn)
    {
        var call = _s.Call(insn.Token);
        if (call is null)
        {
            _stackShape = true;
            return;
        }
        var args = PopArguments(call.Parameters.Length);
        var receiver = call.HasThis ? Pop() : RValue.None;
        if (call.ArraySearch)
            _arraySearchTypes[i] = TypeOfObject(args[0], insn.Token);
        var result = Invoke(i, insn, call, receiver, args);
        if (!call.ReturnsVoid)
            Push(result);
    }

    private void NewObject(Instruction insn)
    {
        var call = _s.Call(insn.Token);
        if (call is null)
        {
            _stackShape = true;
            return;
        }
        var args = PopArguments(call.Parameters.Length);
        if (call.Parameters.Length == 2 && call.Parameters[0] == "System.Object" && call.Parameters[1] == "System.IntPtr")
        {
            Escape(args[0]);
            Push(Known(Term(TermKind.Delegate, 0, null, 0,
                new[] { Child(args[1]), call.DeclaringType != 0 ? Term(TermKind.TypeToken, call.DeclaringType) : -1 })));
            return;
        }
        foreach (var arg in args)
            Escape(arg);
        if (_s.IsDisplayClass(call.DefinitionType))
            Push(Known(Term(TermKind.Closure, call.DefinitionType)));
        else if (call.DeclaringType != 0)
            Push(Known(Term(TermKind.ExactObject, call.DeclaringType)));
        else
            Push(Unknown(UnknownReason.CallResult, insn.Token));
    }

    private void Calli(Instruction insn)
    {
        var handle = SRME.EntityHandle(insn.Token);
        if (handle.Kind != HandleKind.StandaloneSignature)
        {
            _stackShape = true;
            return;
        }
        var signature = _r.GetStandaloneSignature((StandaloneSignatureHandle)handle)
            .DecodeMethodSignature(RawSignatureProvider.Instance, null);
        Pop();
        foreach (var arg in PopArguments(signature.ParameterTypes.Length))
            Escape(arg);
        if (signature.Header.IsInstance)
            Escape(Pop());
        if (signature.ReturnType != "System.Void")
            Push(Unknown(UnknownReason.CallResult, insn.Token));
    }

    private RValue Invoke(int i, Instruction insn, ReflectionSummarizer.CallInfo call, RValue receiver, RValue[] args)
    {
        if (call.Api is not { } api)
            return Unclassified(insn.Token, call, receiver, args);
        if (!api.Recognized)
        {
            Escape(receiver);
            foreach (var arg in args)
                Escape(arg);
            RecordSite(i, insn, call, api, receiver, args);
            return Unknown(UnknownReason.CallResult, insn.Token);
        }
        var result = Apply(api, call, insn.Token, insn.Offset, receiver, args);
        if (api.Kind != ReflectionOpKind.Transfer)
            RecordSite(i, insn, call, api, receiver, args);
        // The body a site runs, or a delegate it binds runs later, receives its instance and
        // stored value, and writes by-ref and out results back into its argument array.
        if (api.Kind is ReflectionOpKind.Execute or ReflectionOpKind.Construct or ReflectionOpKind.Bind)
        {
            Escape(Operand(api.Instance, call, insn.Token, receiver, args));
            Escape(Operand(api.Value, call, insn.Token, receiver, args));
            WriteBack(Operand(api.Args, call, insn.Token, receiver, args));
        }
        return result;
    }

    // Calls outside the table that produce a value a reflection call consumes.
    private RValue Unclassified(int token, ReflectionSummarizer.CallInfo call, RValue receiver, RValue[] args)
    {
        if (call.Parent == "System.Array" && call.Name == "Empty" && call.TypeArguments is { Length: 1 } empty
            && empty[0] == "System.Type")
            return Known(Term(TermKind.TypeArray, 0, null, 0, Array.Empty<int>()));
        if (call.Parent == "System.Type" && call.Name == "get_Assembly" && call.HasThis && args.Length == 0)
            return Known(Term(TermKind.Assembly, 0, null, 0, new[] { Child(receiver) }));
        if (call.Parent == "System.Reflection.Assembly" && !call.HasThis)
        {
            if (call.Name == "GetExecutingAssembly" && args.Length == 0)
                return Known(Term(TermKind.Assembly, 0, null, 1));
            if (call.Name == "GetEntryAssembly" && args.Length == 0)
                return Known(Term(TermKind.Assembly, 0, null, 2));
            if (call.Name == "Load" && call.Parameters.Length == 1 && call.Parameters[0] == "System.String")
                return MapStrings(args[0], token, text => Term(TermKind.Assembly, 0, text));
        }
        Escape(receiver);
        foreach (var arg in args)
            Escape(arg);
        return Unknown(call.ReturnsString ? UnknownReason.NonConstantString : UnknownReason.CallResult, token);
    }

    private RValue Operand(sbyte index, ReflectionSummarizer.CallInfo call, int token, RValue receiver, RValue[] args)
    {
        if (index >= 0)
            return index < args.Length ? args[index] : RValue.None;
        if (index == ReflectionApi.Receiver)
            return receiver;
        if (index == ReflectionApi.MethodTypeArgument)
            return TypeArgument(token);
        return RValue.None;
    }

    private RValue TypeArgument(int token) =>
        (uint)token >> 24 == 0x2B ? Known(Term(TermKind.MethodSpecArg, token)) : RValue.None;

    private RValue Apply(ReflectionApi api, ReflectionSummarizer.CallInfo call, int token, int offset, RValue receiver,
        RValue[] args)
    {
        RValue Op(sbyte index) => Operand(index, call, token, receiver, args);
        int Optional(sbyte index) => index == ReflectionApi.Absent ? -1 : Child(Op(index));
        var target = Op(api.Target);
        switch (api.Op)
        {
            case ReflectionOp.TypeGetType or ReflectionOp.AssemblyGetType:
            {
                if (api.AssemblyResolver >= 0 && !IsNullOnly(Op(api.AssemblyResolver))
                    || api.TypeResolver >= 0 && !IsNullOnly(Op(api.TypeResolver)))
                    return Unknown(UnknownReason.ResolverDelegate);
                int assembly = api.Op == ReflectionOp.AssemblyGetType ? Child(receiver) : -1;
                var cases = api.IgnoreCase >= 0 ? Op(api.IgnoreCase) : IntValue(0);
                var named = RValue.None;
                for (int k = 0; k < cases.Count; k++)
                {
                    if (Terms[cases.Alternatives![k]] is not { Kind: TermKind.Int } choice)
                    {
                        named = Join(named, Unknown(UnknownReason.CallResult, token));
                        continue;
                    }
                    long ignoreCase = choice.Number == 0 ? 0 : 1;
                    named = Join(named, MapStrings(Op(api.Name), token, text => ReflectionTypeName.Parse(text) is null
                        ? NullTerm()
                        : Term(TermKind.TypeName, 0, text, ignoreCase,
                            assembly < 0 ? Array.Empty<int>() : new[] { assembly })));
                }
                if (cases.Unknown >= 0)
                    named = Join(named, new RValue(null, cases.Unknown));
                return named;
            }
            case ReflectionOp.ObjectGetType:
                return TypeOfObject(target, token);
            case ReflectionOp.GetTypeFromHandle or ReflectionOp.MemberFromHandle or ReflectionOp.TypeInfoView
                or ReflectionOp.TypeHandle:
                return target;
            case ReflectionOp.SequenceElement:
                // A sequence view reads its source: an array it wraps is not escaped. A copy is
                // an array of its own the body may write. AsEnumerable and Cast may return the
                // source array itself.
                if (call.Name is "ToArray" or "ToList")
                    return FreshArray(offset, SequenceOf(Snapshot(target), token));
                if (call.Name is "AsEnumerable" or "Cast")
                    return target;
                if (call.Name is "GetEnumerator" or "Where" or "OfType")
                    return Snapshot(target);
                var element = Element(target, token);
                if (api.Value >= 0)
                    return Join(element, Op(api.Value));
                return call.Name is "FirstOrDefault" or "LastOrDefault" or "SingleOrDefault" or "ElementAtOrDefault"
                    ? Join(element, SequenceDefault(token)) : element;
            case ReflectionOp.GetMethod or ReflectionOp.GetConstructor or ReflectionOp.GetProperty
                or ReflectionOp.GetField or ReflectionOp.GetEvent or ReflectionOp.GetMember
                or ReflectionOp.GetNestedType or ReflectionOp.GetInterface or ReflectionOp.TypeInitializer:
            {
                if (api.Binder >= 0 && !IsNullOnly(Op(api.Binder)))
                    return Unknown(UnknownReason.NonNullBinder);
                var lookup = Known(Term(TermKind.Lookup, 0, null, (long)api.Op, new[]
                {
                    Child(target), Optional(api.Name), Child(Flags(api, Op)), Optional(api.Types),
                    Optional(api.Arity), Optional(api.ReturnType), Optional(api.MemberTypes),
                }));
                return api.Op == ReflectionOp.GetMember ? FreshArray(offset, lookup) : lookup;
            }
            case ReflectionOp.Accessor:
            {
                var accessor = Known(Term(TermKind.Accessor, 0, null, (long)api.Accessor,
                    new[] { Child(target), Child(Flags(api, Op)) }));
                // GetAccessors returns an array of its own the body may write.
                return api.Accessor == ReflectionAccessor.All ? FreshArray(offset, accessor) : accessor;
            }
            case ReflectionOp.DelegateMethod:
                return TargetsOf(target, token);
            case ReflectionOp.GetMethods or ReflectionOp.GetConstructors or ReflectionOp.GetProperties
                or ReflectionOp.GetFields or ReflectionOp.GetEvents or ReflectionOp.GetMembers
                or ReflectionOp.GetNestedTypes or ReflectionOp.GetInterfaces or ReflectionOp.GetDefaultMembers
                or ReflectionOp.AssemblyGetTypes or ReflectionOp.GetParameters or ReflectionOp.GetGenericArguments:
                return FreshArray(offset, Known(Term(TermKind.Enumeration, 0, null, (long)api.Op,
                    new[] { Child(target), api.Op is ReflectionOp.GetParameters or ReflectionOp.GetGenericArguments
                        ? -1 : Child(Flags(api, Op)), Optional(api.Name) })));
            case ReflectionOp.GetCustomAttributes:
            {
                int inherit = api.Inherit >= 0 ? Child(Op(api.Inherit)) : Term(TermKind.Int, 0, null, api.DefaultInherit ? 1 : 0);
                var list = Known(Term(TermKind.Enumeration, 0, null, (long)api.Op,
                    new[] { Child(target), Optional(api.AttributeType), inherit }));
                return call.Name == "GetCustomAttribute" ? Element(list, token) : FreshArray(offset, list);
            }
            case ReflectionOp.GetCustomAttributesData:
                return Known(Term(TermKind.Enumeration, 0, null, (long)api.Op, new[] { Child(target), -1 }));
            case ReflectionOp.GetElementType:
                return Known(Term(TermKind.ElementOf, 0, null, 0, new[] { Child(target) }));
            case ReflectionOp.GetGenericTypeDefinition:
                return Known(Term(TermKind.GenericDefOf, 0, null, 0, new[] { Child(target) }));
            case ReflectionOp.BaseType:
                return Known(Term(TermKind.BaseOf, 0, null, 0, new[] { Child(target) }));
            case ReflectionOp.MakeArrayType:
            {
                long rank = ReflectionTypeName.SZArray;
                if (api.Arity >= 0)
                    rank = SingleInt(Op(api.Arity)) is long known && known > 0 ? known : 0;
                return Known(Term(TermKind.ArrayOf, 0, null, rank, new[] { Child(target) }));
            }
            case ReflectionOp.FieldType or ReflectionOp.PropertyType or ReflectionOp.ReturnType
                or ReflectionOp.ParameterType:
                return Known(Term(TermKind.TypeOfMember, 0, null, (long)api.Op, new[] { Child(target) }));
            case ReflectionOp.MakeGenericType:
                return Known(Term(TermKind.MadeGeneric, 0, null, 0, new[] { Child(target), Child(Op(api.Types)) }));
            case ReflectionOp.MakeGenericMethod:
                return Known(Term(TermKind.MadeGenericMethod, 0, null, 0, new[] { Child(target), Child(Op(api.Types)) }));
            case ReflectionOp.CreateDelegate:
                return Known(Term(TermKind.Delegate, 0, null, 0, new[] { Child(target), Optional(api.DelegateType) }));
            case ReflectionOp.CreateDelegateNamed:
            {
                int lookup = Term(TermKind.Lookup, 0, null, (long)api.Op, new[]
                {
                    Child(target), Optional(api.Name), Child(Flags(api, Op)), -1, -1, -1, -1, Optional(api.DelegateType),
                });
                return Known(Term(TermKind.Delegate, 0, null, 0, new[] { lookup, Optional(api.DelegateType) }));
            }
        }
        return Unknown(UnknownReason.CallResult, token);
    }

    // The binding flags a lookup applies: the operand, or the family default with a
    // nonPublic and ignoreCase Booleans folded in under each possible choice.
    private RValue Flags(ReflectionApi api, Func<sbyte, RValue> operand)
    {
        var flags = api.Flags >= 0 ? operand(api.Flags) : IntValue(api.DefaultFlags);
        if (api.NonPublic >= 0)
        {
            var nonPublic = operand(api.NonPublic);
            flags = RValue.None;
            if (nonPublic.Unknown >= 0 || HasNonZero(nonPublic))
                flags = Join(flags, IntValue(api.DefaultFlags | ReflectionApis.NonPublic));
            if (nonPublic.Unknown >= 0 || HasZero(nonPublic))
                flags = Join(flags, IntValue(api.DefaultFlags));
        }
        if (api.IgnoreCase >= 0)
        {
            var ignoreCase = operand(api.IgnoreCase);
            var original = flags;
            flags = RValue.None;
            if (ignoreCase.Unknown >= 0 || HasNonZero(ignoreCase))
                flags = Join(flags, IntOp(ILOpCode.Or, original, IntValue(1)));
            if (ignoreCase.Unknown >= 0 || HasZero(ignoreCase))
                flags = Join(flags, original);
        }
        return flags;
    }

    private RValue TypeOfObject(RValue value, int token)
    {
        var result = RValue.None;
        for (int k = 0; k < value.Count; k++)
        {
            int id = value.Alternatives![k];
            var t = Terms[id];
            result = Join(result, t.Kind switch
            {
                TermKind.ExactObject or TermKind.Closure => Known(Term(TermKind.TypeToken, t.Token, null, t.Number)),
                TermKind.Str => Known(Term(TermKind.TypeName, 0, "System.String")),
                TermKind.Delegate when t.Children[1] >= 0 => ValueOf(t.Children[1]),
                TermKind.Allocation => ArrayObjectType((int)t.Number, token),
                TermKind.Null => RValue.None,
                _ => Unknown(UnknownReason.CallResult, token),
            });
        }
        if (value.Unknown >= 0)
            result = Join(result, new RValue(null, value.Unknown));
        return result.IsEmpty ? Unknown(UnknownReason.CallResult, token) : result;
    }

    private RValue ArrayObjectType(int site, int token)
    {
        if (!_allocations.TryGetValue(site, out var allocation) || allocation.ElementToken == 0)
            return Unknown(UnknownReason.CallResult, token);
        // Writes and escapes change the contents, never the allocation's element type.
        return Known(Term(TermKind.ArrayOf, 0, null, ReflectionTypeName.SZArray,
            new[] { Term(TermKind.TypeToken, allocation.ElementToken) }));
    }

    private RValue TargetsOf(RValue value, int token)
    {
        var result = RValue.None;
        for (int k = 0; k < value.Count; k++)
        {
            var t = Terms[value.Alternatives![k]];
            result = Join(result, t.Kind == TermKind.Delegate ? DelegateMethods(ValueOf(t.Children[0]))
                : Unknown(UnknownReason.CallResult, token));
        }
        if (value.Unknown >= 0)
            result = Join(result, new RValue(null, value.Unknown));
        return result.IsEmpty ? Unknown(UnknownReason.CallResult, token) : result;
    }

    // Delegate.Method over these targets: an ldvirtftn target that may be overridden
    // answers the override the delegate's target selects.
    private RValue DelegateMethods(RValue targets)
    {
        var result = new RValue(null, targets.Unknown);
        for (int k = 0; k < targets.Count; k++)
        {
            int id = targets.Alternatives![k];
            var t = Terms[id];
            result = Join(result, t.Kind != TermKind.MemberToken || t.Number != ReflectionSummary.VirtualTarget ? Known(id)
                : DispatchesExactly(t.Token) ? Known(Term(TermKind.MemberToken, t.Token))
                : Unknown(UnknownReason.VirtualTarget, t.Token));
        }
        return result;
    }

    // An element of a sequence value, read by the call <paramref name="token"/>. A value
    // that is no modeled sequence, such as a collection object, has unknown elements.
    private RValue Element(RValue sequence, int token)
    {
        var result = RValue.None;
        for (int k = 0; k < sequence.Count; k++)
        {
            int id = sequence.Alternatives![k];
            var t = Terms[id];
            result = Join(result, t.Kind switch
            {
                TermKind.Enumeration => Known(Term(TermKind.Element, 0, null, -1, new[] { id })),
                TermKind.Lookup when t.Number == (long)ReflectionOp.GetMember =>
                    Known(Term(TermKind.Element, 0, null, -1, new[] { id })),
                TermKind.Allocation => Contents((int)t.Number, RValue.None),
                TermKind.FieldArray => FieldArrayContents(t, RValue.None),
                TermKind.TypeArray => AllChildren(t),
                TermKind.Null => RValue.None,
                _ => Unknown(UnknownReason.CallResult, token),
            });
        }
        if (sequence.Unknown >= 0)
            result = Join(result, new RValue(null, sequence.Unknown));
        return result;
    }

    // An implicit OrDefault may return default(T) even when every source element is
    // non-default. Raw signatures distinguish reference null from unsupported value types.
    private RValue SequenceDefault(int token)
    {
        if (SRME.EntityHandle(token).Kind == HandleKind.MethodSpecification)
        {
            var defaults = _r.GetMethodSpecification((MethodSpecificationHandle)SRME.EntityHandle(token))
                .DecodeSignature(CliDefaultProvider.Instance, null);
            if (defaults.Length == 1)
                return defaults[0] switch
                {
                    CliDefault.Zero => IntValue(0),
                    CliDefault.Null => Known(NullTerm()),
                    _ => Unknown(UnknownReason.CallResult, token),
                };
        }
        return Unknown(UnknownReason.CallResult, token);
    }

    // The sequences a copy can hold: a value that is no modeled sequence copies unknown
    // elements.
    private RValue SequenceOf(RValue value, int token)
    {
        var result = new RValue(null, value.Unknown);
        for (int k = 0; k < value.Count; k++)
        {
            int id = value.Alternatives![k];
            var t = Terms[id];
            result = Join(result, t.Kind is TermKind.TypeArray or TermKind.Enumeration or TermKind.Null
                || t.Kind == TermKind.Lookup && t.Number == (long)ReflectionOp.GetMember
                ? Known(id) : Unknown(UnknownReason.CallResult, token));
        }
        return result;
    }

    private RValue MapStrings(RValue value, int token, Func<string, int> map)
    {
        var result = RValue.None;
        for (int k = 0; k < value.Count; k++)
        {
            var t = Terms[value.Alternatives![k]];
            result = Join(result, t.Kind switch
            {
                TermKind.Str => Known(map(t.Text!)),
                TermKind.Null => Known(NullTerm()),
                _ => Unknown(UnknownReason.NonConstantString, token),
            });
        }
        if (value.Unknown >= 0)
            result = Join(result, new RValue(null, value.Unknown));
        return result.IsEmpty ? Unknown(UnknownReason.NonConstantString, token) : result;
    }

    private void RecordSite(int i, Instruction insn, ReflectionSummarizer.CallInfo call, ReflectionApi api,
        RValue receiver, RValue[] args)
    {
        var operands = new RValue[args.Length];
        for (int k = 0; k < args.Length; k++)
            operands[k] = Snapshot(args[k]);
        _recorded[i] = new RSite(insn.Offset, insn.Token, api, call.HasThis ? Snapshot(receiver) : RValue.None,
            operands, TypeArgument(insn.Token));
    }

    private ReflectionSummary Build()
    {
        var sites = new List<RSite>();
        var arraySites = new List<(int Offset, RValue Types)>();
        for (int k = 0; k < _siteIndexes.Count; k++)
        {
            int i = _siteIndexes[k];
            var insn = _code[i];
            var call = _s.Call(insn.Token)!;
            if (call.ArraySearch)
            {
                RValue types = _budget ? Unknown(UnknownReason.Budget)
                    : _stackShape ? Unknown(UnknownReason.StackShape)
                    : insn.OpCode is ILOpCode.Ldftn or ILOpCode.Ldvirtftn ? Unknown(UnknownReason.MethodGroup)
                    : _arraySearchTypes.TryGetValue(i, out var known) ? known : Unknown(UnknownReason.Unreached);
                arraySites.Add((insn.Offset, types));
                continue;
            }
            var api = call.Api!.Value;
            if (_budget)
                sites.Add(Opaque(insn, call, api, UnknownReason.Budget));
            else if (_stackShape)
                sites.Add(Opaque(insn, call, api, UnknownReason.StackShape));
            else if (insn.OpCode is ILOpCode.Ldftn or ILOpCode.Ldvirtftn)
                sites.Add(Opaque(insn, call, api, UnknownReason.MethodGroup));
            else if (_recorded.TryGetValue(i, out var site))
                sites.Add(site);
            else
                sites.Add(Opaque(insn, call, api, UnknownReason.Unreached));
        }
        return new ReflectionSummary
        {
            Terms = Terms.ToArray(), Sites = sites.ToArray(), ArraySearchSites = arraySites.ToArray(),
            Unknowns = _unknowns.ToArray(), Interpreted = true,
            OverBudget = _budget, StackShapeMismatch = _stackShape, BlockVisits = _visits, Reruns = _reruns,
        };
    }

    private RSite Opaque(Instruction insn, ReflectionSummarizer.CallInfo call, ReflectionApi api, UnknownReason reason)
    {
        var unknown = Unknown(reason);
        var operands = new RValue[call.Parameters.Length];
        for (int k = 0; k < operands.Length; k++)
            operands[k] = unknown;
        return new RSite(insn.Offset, insn.Token, api, call.HasThis ? unknown : RValue.None, operands,
            TypeArgument(insn.Token));
    }

    // --- fields and closures ---

    private RValue LoadField(int token, bool isStatic)
    {
        var reason = isStatic ? UnknownReason.StaticField : UnknownReason.InstanceField;
        var shape = _s.ShapeOf(token);
        if (shape.Model == ReflectionSummarizer.FieldModel.None)
            return Unknown(reason, token);
        if (_fieldReads.TryGetValue(shape.Key, out var cached))
            return cached;
        // The table holds every store the field admits, so its union covers every read
        // whatever the receiver.
        var value = RValue.None;
        var table = _s.TableOf(shape.Root);
        if (table is not null && !table.AllOpaque)
        {
            if (!table.Complete)
                table.ReadWhileBuilding = true;
            bool building = shape.Array && _sink is not null && _sink.Table == table;
            if (building)
                value = Known(Term(TermKind.FieldArray, token, null, table.IndexOf(shape.Key)));
            if (table.Done is { } states && states.TryGetValue(shape.Key, out var state) && !state.Opaque)
            {
                foreach (var c in state.Values)
                    value = Join(value, Import(c.Source, c.Value, token, reason));
                if (!building && (state.HoldsArray || state.ArrayEscaped))
                    value = Join(value, ArrayOf(state, token, reason));
                // A store may run after this read or affect another instance.
                value = Join(value, shape.Default switch
                {
                    CliDefault.Zero => IntValue(0),
                    CliDefault.Null => Known(NullTerm()),
                    _ => Unknown(reason, token),
                });
            }
            else if (building)
                value = Join(value, Unknown(reason, token));
        }
        if (value.IsEmpty)
            value = Unknown(reason, token);
        _fieldReads[shape.Key] = value;
        return value;
    }

    private void StoreField(int token, RValue value)
    {
        var shape = _s.ShapeOf(token);
        if (_sink is null || shape.Model == ReflectionSummarizer.FieldModel.None || shape.Root != _sink.Table.Root)
        {
            Escape(value);
            return;
        }
        int self = shape.Array ? _sink.Table.IndexOf(shape.Key) : -1;
        var kept = new RValue(null, value.Unknown);
        for (int k = 0; k < value.Count; k++)
        {
            int id = value.Alternatives![k];
            var t = Terms[id];
            if (shape.Array && t.Kind == TermKind.Allocation)
            {
                _sink.AddSite(shape.Key, (int)t.Number, _written);
                if (_shared.Add((int)t.Number))
                    _newEscape = true;
                continue;
            }
            if (t.Kind == TermKind.FieldArray && t.Number == self)
                continue;
            kept = Join(kept, Known(id));
        }
        _sink.Touch(shape.Key);
        if (kept.IsEmpty)
            return;
        Escape(kept);
        _sink.AddValue(shape.Key, StorageValue(Snapshot(kept), shape.Type, ILOpCode.Stfld), this);
    }

    private RValue LoadStatic(int token)
    {
        var (parent, name) = _s.FieldName(token);
        if (parent == "System.Type" && name == "EmptyTypes")
            return Known(Term(TermKind.TypeArray, 0, null, 0, Array.Empty<int>()));
        return LoadField(token, isStatic: true);
    }

    // The array a field holds, as the complete table knows it. An element stored through
    // the field may land in an array the field took as a value rather than allocated, and
    // that array's other slots are not modeled index by index, so its contents are unknown.
    private RValue ArrayOf(ReflectionSummarizer.FieldState state, int token, UnknownReason reason)
    {
        if (state.ArrayEscaped || state.ElementWritten && HoldsValueArray(state))
            return Unknown(UnknownReason.ArrayEscaped);
        var any = RValue.None;
        var slots = new Dictionary<long, RValue>();
        var indexes = new List<long>();
        foreach (var e in state.Elements)
        {
            var element = Import(e.Source, e.Value, token, reason);
            if (e.Index < 0)
            {
                any = Join(any, element);
                continue;
            }
            if (slots.TryGetValue(e.Index, out var previous))
                slots[e.Index] = Join(previous, element);
            else
            {
                slots.Add(e.Index, element);
                indexes.Add(e.Index);
            }
        }
        long length = state.Length;
        if (length >= 0 && length <= ReflectionSummarizer.MaxAlternatives * 4)
        {
            var children = new int[length];
            for (int k = 0; k < length; k++)
            {
                slots.TryGetValue(k, out var slot);
                children[k] = Child(Join(slot, any));
            }
            return Known(Term(TermKind.TypeArray, 0, null, length, children));
        }
        indexes.Sort();
        var all = any;
        foreach (long index in indexes)
            all = Join(all, slots[index]);
        return Known(Term(TermKind.TypeArray, 0, null, -1, new[] { Child(all) }));
    }

    // Whether a store gave the array field a value other than null and an allocation.
    private static bool HoldsValueArray(ReflectionSummarizer.FieldState state)
    {
        foreach (var c in state.Values)
        {
            if (c.Value.Unknown >= 0)
                return true;
            for (int k = 0; k < c.Value.Count; k++)
                if (c.Source.Terms[c.Value.Alternatives![k]].Kind != TermKind.Null)
                    return true;
        }
        return false;
    }

    // The previous round's state of the field a FieldArray term stands for; null before the
    // first round completes or when the field is opaque.
    private ReflectionSummarizer.FieldState? FieldArrayState(RTerm t)
    {
        var table = _sink!.Table;
        if (table.Done is not { } states || !states.TryGetValue(table.Keys[(int)t.Number], out var state) || state.Opaque)
            return null;
        return state;
    }

    private UnknownReason FieldReason(int token) =>
        _s.ShapeOf(token).Static ? UnknownReason.StaticField : UnknownReason.InstanceField;

    private RValue FieldArrayContents(RTerm t, RValue index, ILOpCode load = ILOpCode.Nop)
    {
        var state = FieldArrayState(t);
        if (state is null)
            return Unknown(FieldReason(t.Token), t.Token);
        if (state.ArrayEscaped)
            return Unknown(UnknownReason.ArrayEscaped);
        long single = SingleInt(index) ?? -1;
        var result = RValue.None;
        foreach (var e in state.Elements)
            if (single < 0 || e.Index < 0 || e.Index == single)
                result = Join(result, e.DefaultElementToken != 0 ? DefaultElement(e.DefaultElementToken, load)
                    : Import(e.Source, e.Value, t.Token, FieldReason(t.Token)));
        return result;
    }

    private RValue FieldArrayValue(RTerm t)
    {
        var state = FieldArrayState(t);
        return state is null ? Unknown(FieldReason(t.Token), t.Token) : ArrayOf(state, t.Token, FieldReason(t.Token));
    }

    /// <summary>Records what this body stored into the fields of the table being built.</summary>
    internal void CommitSink(Dictionary<string, ReflectionSummarizer.FieldState> states)
    {
        var sink = _sink!;
        // An allocation stored into two fields is written through either.
        var owners = new Dictionary<int, int>();
        foreach (string key in sink.Keys)
            if (sink.Sites.TryGetValue(key, out var sites))
                foreach (int site in sites)
                    owners[site] = owners.TryGetValue(site, out int n) ? n + 1 : 1;
        foreach (string key in sink.Keys)
        {
            var state = states[key];
            if (sink.Values.TryGetValue(key, out var value))
                state.Values.Add(new ReflectionSummarizer.FieldContribution { Source = this, Value = value });
            if (sink.Sites.TryGetValue(key, out var sites))
                foreach (int site in sites)
                {
                    if (_escaped.Contains(site) || owners[site] > 1 || !_allocations.TryGetValue(site, out var allocation)
                        || allocation.Sourced && allocation.Slots.Count > 0)
                    {
                        state.ArrayEscaped = true;
                        continue;
                    }
                    if (allocation.Sourced)
                    {
                        state.Values.Add(new ReflectionSummarizer.FieldContribution { Source = this, Value = allocation.Source });
                        continue;
                    }
                    state.HoldsArray = true;
                    state.Length = state.Length == -2 || state.Length == allocation.Length ? allocation.Length : -1;
                    var indexes = new List<long>(allocation.Slots.Keys);
                    indexes.Sort();
                    foreach (long index in indexes)
                        state.Elements.Add(new ReflectionSummarizer.FieldContribution
                        {
                            Source = this, Value = allocation.Slots[index], Index = index,
                        });
                    // Stores through the field may run after a read. Only stores proven
                    // before every publication can remove an allocation's default slots.
                    var written = sink.Written[site];
                    var defaultElement = DefaultElement(allocation.ElementToken);
                    if (!defaultElement.IsEmpty)
                    {
                        if (allocation.Length >= 0 && allocation.Length <= ReflectionSummarizer.MaxAlternatives * 4)
                        {
                            for (long index = 0; index < allocation.Length; index++)
                                if (!written.Contains(index))
                                    state.Elements.Add(new ReflectionSummarizer.FieldContribution
                                    {
                                        Source = this, Value = defaultElement, Index = index,
                                        DefaultElementToken = allocation.ElementToken,
                                    });
                        }
                        else
                        {
                            bool complete = allocation.Length >= 0 && allocation.Length <= written.Count;
                            for (long index = 0; complete && index < allocation.Length; index++)
                                complete = written.Contains(index);
                            if (!complete)
                                state.Elements.Add(new ReflectionSummarizer.FieldContribution
                                {
                                    Source = this, Value = defaultElement,
                                    DefaultElementToken = allocation.ElementToken,
                                });
                        }
                    }
                }
            if (sink.Elements.TryGetValue(key, out var elements))
                foreach (var (index, element) in elements)
                {
                    state.HoldsArray = true;
                    state.ElementWritten = true;
                    state.Elements.Add(new ReflectionSummarizer.FieldContribution
                    {
                        Source = this, Value = element, Index = index,
                    });
                }
            if (sink.Escaped.Contains(key))
                state.ArrayEscaped = true;
        }
    }

    // A value interpreted in another body. An unknown that names a token of the module keeps
    // its reason, a parameter names that body's method, and one that names the other body's
    // locals or offsets becomes the field it was read through.
    private RValue Import(SummaryInterpreter source, RValue value, int field, UnknownReason reason)
    {
        var memo = new Dictionary<int, int>();
        var result = RValue.None;
        for (int k = 0; k < value.Count; k++)
            result = Join(result, ValueOf(ImportTerm(source, value.Alternatives![k], memo, field, reason)));
        if (value.Unknown >= 0)
            result = Join(result, new RValue(null, ImportUnknown(source, value.Unknown, field, reason)));
        return result;
    }

    private int ImportUnknown(SummaryInterpreter source, int unknown, int field, UnknownReason reason)
    {
        var u = source._unknowns[unknown];
        if (u.Reason == UnknownReason.Parameter)
            return UnknownIndex(u.Reason, u.Token != 0 ? u.Token : source._method, u.Index);
        return u.Reason is UnknownReason.CallResult or UnknownReason.NonConstantString or UnknownReason.StaticField
            or UnknownReason.InstanceField or UnknownReason.Budget or UnknownReason.ResolverDelegate
            or UnknownReason.NonNullBinder or UnknownReason.UnsupportedOpcode or UnknownReason.CaughtException
            or UnknownReason.VirtualTarget or UnknownReason.ArrayDefault
            ? UnknownIndex(u.Reason, u.Token, u.Index)
            : UnknownIndex(reason, field, 0);
    }

    private int ImportTerm(SummaryInterpreter source, int id, Dictionary<int, int> memo, int field, UnknownReason reason)
    {
        if (memo.TryGetValue(id, out int mapped))
            return mapped;
        var t = source.Terms[id];
        int result;
        if (t.Kind is TermKind.Allocation or TermKind.FieldArray)
            result = Term(TermKind.Set, 0, null, UnknownIndex(reason, field, 0), Array.Empty<int>());
        else
        {
            var children = new int[t.Children.Length];
            for (int k = 0; k < children.Length; k++)
                children[k] = t.Children[k] < 0 ? -1 : ImportTerm(source, t.Children[k], memo, field, reason);
            long number = t.Number;
            if (t.Kind == TermKind.Set)
            {
                if (number >= 0)
                    number = ImportUnknown(source, (int)number, field, reason);
                children = SortDistinct(children);
            }
            result = Term(t.Kind, t.Token, t.Text, number, children);
        }
        memo[id] = result;
        return result;
    }

    /// <summary>Renders a value structurally: equal values of two bodies render equal. A
    /// term reached again renders as a back-reference to its first rendering, so the text
    /// grows with the distinct terms, not with the paths to them.</summary>
    internal void Canonical(StringBuilder text, RValue value)
    {
        var rendered = new Dictionary<int, int>();
        text.Append('{');
        for (int k = 0; k < value.Count; k++)
        {
            CanonicalTerm(text, value.Alternatives![k], 0, rendered);
            text.Append(',');
        }
        if (value.Unknown >= 0)
            CanonicalUnknown(text, value.Unknown);
        text.Append('}');
    }

    private void CanonicalTerm(StringBuilder text, int id, int depth, Dictionary<int, int> rendered)
    {
        if (rendered.TryGetValue(id, out int first))
        {
            text.Append('^').Append(first);
            return;
        }
        rendered.Add(id, rendered.Count);
        var t = Terms[id];
        text.Append((int)t.Kind).Append(':').Append(t.Token).Append(':');
        if (t.Kind == TermKind.Set && t.Number >= 0)
            CanonicalUnknown(text, (int)t.Number);
        else
            text.Append(t.Number);
        if (t.Text is not null)
            text.Append('"').Append(t.Text.Length).Append(':').Append(t.Text);
        if (t.Children.Length == 0)
            return;
        if (depth > ReflectionSummarizer.MaxAlternatives)
        {
            text.Append("(#").Append(id).Append(')');
            return;
        }
        text.Append('(');
        foreach (int child in t.Children)
        {
            if (child < 0)
                text.Append('-');
            else
                CanonicalTerm(text, child, depth + 1, rendered);
            text.Append(',');
        }
        text.Append(')');
    }

    private void CanonicalUnknown(StringBuilder text, int unknown)
    {
        var u = _unknowns[unknown];
        text.Append('?').Append((int)u.Reason).Append(':').Append(u.Token).Append(':').Append(u.Index);
    }

    // --- arrays ---

    private RValue NewArray(int offset, RValue length, int elementToken)
    {
        if (!_allocations.TryGetValue(offset, out var allocation))
        {
            allocation = new Allocation { ElementToken = elementToken };
            _allocations.Add(offset, allocation);
        }
        _written.RemoveWhere(w => w.Site == offset);
        long known = SingleInt(length) is long n && n >= 0 ? n : -1;
        long widened = allocation.Length == -2 || allocation.Length == known ? known : -1;
        if (widened != allocation.Length && allocation.Read)
            _heapChanged = true;
        allocation.Length = widened;
        return Known(Term(TermKind.Allocation, 0, null, offset));
    }

    private RValue FreshArray(int offset, RValue source)
    {
        if (!_allocations.TryGetValue(offset, out var allocation))
        {
            allocation = new Allocation { Sourced = true, Length = -1 };
            _allocations.Add(offset, allocation);
        }
        var joined = Join(allocation.Source, source);
        if (!Same(joined, allocation.Source))
        {
            allocation.Source = joined;
            if (allocation.Read)
                _heapChanged = true;
        }
        return Known(Term(TermKind.Allocation, 0, null, offset));
    }

    private void StoreElement(RValue array, RValue index, RValue value, ILOpCode op, int token)
    {
        Escape(value);
        value = Snapshot(value);
        for (int k = 0; k < array.Count; k++)
        {
            var t = Terms[array.Alternatives![k]];
            if (t.Kind == TermKind.FieldArray)
            {
                _sink!.AddElement(_sink.Table.Keys[(int)t.Number], SingleInt(index) is long at && at >= 0 ? at : -1,
                    StoredElement(value, op, token, _s.ShapeOf(t.Token).Type), this);
                continue;
            }
            if (t.Kind != TermKind.Allocation || _escaped.Contains((int)t.Number)
                || !_allocations.TryGetValue((int)t.Number, out var allocation))
                continue;
            var stored = StoredElement(value, op, token, ElementName(allocation.ElementToken));
            // Only a store into the one array the value can be, at one index, overwrites
            // that slot's default.
            if (array.Count == 1 && array.Unknown < 0 && !_repeatedArrays.Contains((int)t.Number)
                && SingleInt(index) is long slot && slot >= 0)
                _written.Add(((int)t.Number, slot));
            if (AllInts(index))
                for (int j = 0; j < index.Count; j++)
                    StoreSlot(allocation, Terms[index.Alternatives![j]].Number, stored);
            else
                StoreSlot(allocation, -1, stored);
        }
    }

    private string ElementName(int token)
    {
        if (token == 0)
            return "";
        var handle = SRME.EntityHandle(token);
        return handle.Kind switch
        {
            HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(_r, (TypeReferenceHandle)handle),
            HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(_r, (TypeDefinitionHandle)handle),
            HandleKind.TypeSpecification => _r.GetTypeSpecification((TypeSpecificationHandle)handle)
                .DecodeSignature(RawSignatureProvider.Instance, null),
            _ => "",
        };
    }

    // Slots hold semantic element values so snapshots and sequence reads agree with
    // typed loads. An opcode fixes storage width, but not a small integer's signedness.
    private RValue StoredElement(RValue value, ILOpCode op, int token, string element)
    {
        if (element.EndsWith("[]", StringComparison.Ordinal))
            element = element.Substring(0, element.Length - 2);
        if (op == ILOpCode.Stelem_ref)
            return value;
        if (op == ILOpCode.Stelem)
            return StorageValue(value, ElementName(token), op);
        if (op is ILOpCode.Stelem_r4 or ILOpCode.Stelem_r8)
            return Opcode(op);
        var conversion = op switch
        {
            ILOpCode.Stelem_i1 => ILOpCode.Conv_u1,
            ILOpCode.Stelem_i2 => ILOpCode.Conv_u2,
            ILOpCode.Stelem_i4 => ILOpCode.Conv_i4,
            ILOpCode.Stelem_i8 => ILOpCode.Conv_i8,
            _ => ILOpCode.Conv_i,
        };
        var narrowed = Convert(conversion, value);
        if (op is not (ILOpCode.Stelem_i1 or ILOpCode.Stelem_i2))
            return narrowed;
        if (element is "System.Byte" or "System.SByte" or "System.UInt16" or "System.Int16" or "System.Char")
            return StorageValue(narrowed, element, op);
        // An unresolved enum or copied array may be signed or unsigned. Their values
        // agree only below the storage width's sign bit.
        var result = new RValue(null, narrowed.Unknown);
        long limit = op == ILOpCode.Stelem_i1 ? sbyte.MaxValue : short.MaxValue;
        for (int k = 0; k < narrowed.Count; k++)
        {
            int id = narrowed.Alternatives![k];
            result = Join(result, Terms[id] is { Kind: TermKind.Int } term && term.Number <= limit
                ? Known(id) : Opcode(op));
        }
        return result;
    }

    private RValue SlotStorageValue(RValue value, string type, ILOpCode op) => type is
        "System.Byte" or "System.SByte" or "System.UInt16" or "System.Int16" or "System.Char"
            ? StorageValue(value, type, op) : value;

    private RValue StorageValue(RValue value, string type, ILOpCode op)
    {
        var conversion = type switch
        {
            "System.SByte" => ILOpCode.Conv_i1,
            "System.Byte" => ILOpCode.Conv_u1,
            "System.Int16" => ILOpCode.Conv_i2,
            "System.UInt16" or "System.Char" => ILOpCode.Conv_u2,
            "System.Int32" or "System.UInt32" or "System.Reflection.BindingFlags" or "System.Reflection.MemberTypes" => ILOpCode.Conv_i4,
            "System.Int64" or "System.UInt64" => ILOpCode.Conv_i8,
            "System.IntPtr" or "System.UIntPtr" => ILOpCode.Conv_i,
            _ => ILOpCode.Nop,
        };
        if (conversion != ILOpCode.Nop)
            return Convert(conversion, value);
        var result = new RValue(null, value.Unknown);
        for (int k = 0; k < value.Count; k++)
        {
            int id = value.Alternatives![k];
            result = Join(result, Terms[id] is { Kind: TermKind.Int } term
                ? type == "System.Boolean" && term.Number is 0 or 1 ? Known(id) : Opcode(op)
                : Known(id));
        }
        return result;
    }

    private void StoreSlot(Allocation allocation, long index, RValue value)
    {
        if (allocation.Slots.TryGetValue(index, out var previous))
        {
            var joined = Join(previous, value);
            if (Same(joined, previous))
                return;
            value = joined;
        }
        allocation.Slots[index] = value;
        if (allocation.Read)
            _heapChanged = true;
    }

    private RValue LoadElement(RValue array, RValue index, ILOpCode load = ILOpCode.Nop, int token = 0)
    {
        var result = RValue.None;
        long single = SingleInt(index) ?? -1;
        for (int k = 0; k < array.Count; k++)
        {
            int id = array.Alternatives![k];
            var t = Terms[id];
            switch (t.Kind)
            {
                case TermKind.Allocation:
                    result = Join(result, Contents((int)t.Number, index, load));
                    break;
                case TermKind.FieldArray:
                    result = Join(result, FieldArrayContents(t, index, load));
                    break;
                case TermKind.TypeArray:
                    result = Join(result, single >= 0 && t.Number >= 0
                        ? single < t.Children.Length ? ValueOf(t.Children[single]) : RValue.None
                        : AllChildren(t));
                    break;
                case TermKind.Enumeration:
                {
                    var op = (ReflectionOp)t.Number;
                    if (single >= 0 && op == ReflectionOp.GetGenericArguments)
                        result = Join(result, Known(Term(TermKind.GenericArgOf, 0, null, single, new[] { t.Children[0] })));
                    else if (single >= 0 && op == ReflectionOp.GetParameters)
                        result = Join(result, Known(Term(TermKind.Parameter, 0, null, single, new[] { t.Children[0] })));
                    else
                        result = Join(result, Known(Term(TermKind.Element, 0, null, -1, new[] { id })));
                    break;
                }
                case TermKind.Null:
                    break;
                default:
                    // An array-returning lookup such as GetMember: some element of it.
                    result = Join(result, _categories[id] == Category.Member
                        ? Known(Term(TermKind.Element, 0, null, -1, new[] { id }))
                        : Opcode(ILOpCode.Ldelem_ref));
                    break;
            }
        }
        if (array.Unknown >= 0)
            result = Join(result, new RValue(null, array.Unknown));
        return load switch
        {
            ILOpCode.Ldelem_i1 => Convert(ILOpCode.Conv_i1, result),
            ILOpCode.Ldelem_u1 => Convert(ILOpCode.Conv_u1, result),
            ILOpCode.Ldelem_i2 => Convert(ILOpCode.Conv_i2, result),
            ILOpCode.Ldelem_u2 => Convert(ILOpCode.Conv_u2, result),
            ILOpCode.Ldelem_i4 or ILOpCode.Ldelem_u4 => Convert(ILOpCode.Conv_i4, result),
            ILOpCode.Ldelem_i8 => Convert(ILOpCode.Conv_i8, result),
            ILOpCode.Ldelem_i => Convert(ILOpCode.Conv_i, result),
            ILOpCode.Ldelem_r4 or ILOpCode.Ldelem_r8 => Opcode(load),
            ILOpCode.Ldelem => StorageValue(result, ElementName(token), load),
            _ => result,
        };
    }

    private RValue Contents(int site, RValue index, ILOpCode load = ILOpCode.Nop)
    {
        if (_escaped.Contains(site) || _shared.Contains(site))
            return Unknown(UnknownReason.ArrayEscaped, 0, site);
        if (!_allocations.TryGetValue(site, out var allocation))
            return RValue.None;
        allocation.Read = true;
        var result = allocation.Sourced ? LoadElement(allocation.Source, index, load) : RValue.None;
        // Only a constant index selects a slot: a set of indices is a loop counter on its
        // way to unknown, and reading it slot by slot would leave every partial union behind.
        if (SingleInt(index) is long single)
        {
            if (allocation.Slots.TryGetValue(single, out var slot))
                result = Join(result, slot);
            if (allocation.Slots.TryGetValue(-1, out var any))
                result = Join(result, any);
            return MayHoldDefault(site, allocation, single) ? Join(result, DefaultElement(allocation.ElementToken, load)) : result;
        }
        foreach (var slot in SlotValues(allocation))
            result = Join(result, slot);
        return MayHoldDefault(site, allocation, -1) ? Join(result, DefaultElement(allocation.ElementToken, load)) : result;
    }

    /// <summary>Whether element <paramref name="slot"/> (-1: any element) of a fresh array
    /// may still hold its default here: no store into it on every path since the array
    /// was allocated. A copy holds its source's elements instead.</summary>
    private bool MayHoldDefault(int site, Allocation allocation, long slot)
    {
        if (allocation.Sourced)
            return false;
        if (slot >= 0)
            return !_written.Contains((site, slot));
        if (allocation.Length < 0 || allocation.Length > _written.Count)
            return true;
        for (long k = 0; k < allocation.Length; k++)
            if (!_written.Contains((site, k)))
                return true;
        return false;
    }

    /// <summary>The element a fresh array holds before a store. A typed load proves
    /// integer zero or reference null; unproven element shapes remain unknown.</summary>
    private RValue DefaultElement(int elementToken, ILOpCode load = ILOpCode.Nop)
    {
        if (load is ILOpCode.Ldelem_i1 or ILOpCode.Ldelem_u1 or ILOpCode.Ldelem_i2 or ILOpCode.Ldelem_u2
            or ILOpCode.Ldelem_i4 or ILOpCode.Ldelem_u4 or ILOpCode.Ldelem_i8 or ILOpCode.Ldelem_i)
            return IntValue(0);
        if (load == ILOpCode.Ldelem_ref)
            return Known(NullTerm());
        var handle = SRME.EntityHandle(elementToken);
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
            {
                string name = RawSignatureProvider.TypeReferenceName(_r, (TypeReferenceHandle)handle);
                return IsZeroDefault(name) ? IntValue(0)
                    : name is "System.Object" or "System.String" or "System.Type" ? Known(NullTerm())
                    : Unknown(UnknownReason.ArrayDefault, elementToken);
            }
            case HandleKind.TypeDefinition:
            {
                var definition = (TypeDefinitionHandle)handle;
                string name = RawSignatureProvider.TypeDefinitionName(_r, definition);
                var baseType = _r.GetTypeDefinition(definition).BaseType;
                string? baseName = baseType.IsNil ? null : baseType.Kind switch
                {
                    HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(_r, (TypeReferenceHandle)baseType),
                    HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(_r, (TypeDefinitionHandle)baseType),
                    _ => null,
                };
                if (IsZeroDefault(name) || baseName == "System.Enum")
                    return IntValue(0);
                return baseName == "System.ValueType" || IsUnmodeledValueType(name)
                    ? Unknown(UnknownReason.ArrayDefault, elementToken) : Known(NullTerm());
            }
            case HandleKind.TypeSpecification:
            {
                var blob = _r.GetBlobReader(_r.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
                var code = blob.RemainingBytes < 2 ? SignatureTypeCode.Invalid : (SignatureTypeCode)blob.ReadByte();
                return code is SignatureTypeCode.SZArray or SignatureTypeCode.Array
                    || code == SignatureTypeCode.GenericTypeInstance && (SignatureTypeKind)blob.ReadByte() == SignatureTypeKind.Class
                    ? Known(NullTerm()) : Unknown(UnknownReason.ArrayDefault, elementToken);
            }
        }
        return Unknown(UnknownReason.ArrayDefault, elementToken);
    }

    // The integer elements the summary reads: integral primitives and the reflection flag enums.
    private static bool IsZeroDefault(string name) => name is "System.Boolean" or "System.Char" or "System.SByte"
        or "System.Byte" or "System.Int16" or "System.UInt16" or "System.Int32" or "System.UInt32" or "System.Int64"
        or "System.UInt64" or "System.IntPtr" or "System.UIntPtr" or "System.Reflection.BindingFlags"
        or "System.Reflection.MemberTypes";

    private static bool IsUnmodeledValueType(string name) => name is "System.Single" or "System.Double" or "System.Decimal";

    // Slot values in ascending index, so a join order never depends on dictionary order.
    private static List<RValue> SlotValues(Allocation allocation)
    {
        var keys = new List<long>(allocation.Slots.Keys);
        keys.Sort();
        var values = new List<RValue>(keys.Count);
        foreach (long key in keys)
            values.Add(allocation.Slots[key]);
        return values;
    }

    private RValue Length(RValue array)
    {
        var result = RValue.None;
        for (int k = 0; k < array.Count; k++)
        {
            var t = Terms[array.Alternatives![k]];
            if (t.Kind == TermKind.Allocation && !_escaped.Contains((int)t.Number) && !_shared.Contains((int)t.Number)
                && _allocations.TryGetValue((int)t.Number, out var read))
            {
                read.Read = true;
                if (read.Sourced)
                {
                    result = Join(result, Length(read.Source));
                    continue;
                }
            }
            long length = t.Kind switch
            {
                TermKind.Allocation when !_escaped.Contains((int)t.Number) && !_shared.Contains((int)t.Number)
                    && _allocations.TryGetValue((int)t.Number, out var allocation) => allocation.Length,
                TermKind.TypeArray => t.Number,
                TermKind.FieldArray => FieldArrayState(t) is { ArrayEscaped: false, Length: >= 0 } state ? state.Length : -1,
                _ => -1,
            };
            result = Join(result, length >= 0 ? IntValue(length) : Opcode(ILOpCode.Ldlen));
        }
        if (array.Unknown >= 0)
            result = Join(result, Opcode(ILOpCode.Ldlen));
        return result.IsEmpty ? Opcode(ILOpCode.Ldlen) : result;
    }

    // The value with every array allocation replaced by its contents, or by an escape
    // unknown, and an empty value by null: what a site operand or a stored value records.
    private RValue Snapshot(RValue value)
    {
        if (value.IsEmpty)
            return Known(NullTerm());
        bool any = false;
        for (int k = 0; k < value.Count; k++)
            any |= Terms[value.Alternatives![k]].Kind is TermKind.Allocation or TermKind.FieldArray;
        if (!any)
            return value;
        var result = new RValue(null, value.Unknown);
        for (int k = 0; k < value.Count; k++)
        {
            int id = value.Alternatives![k];
            var t = Terms[id];
            result = Join(result, t.Kind switch
            {
                TermKind.Allocation => AllocationValue((int)t.Number),
                TermKind.FieldArray => FieldArrayValue(t),
                _ => Known(id),
            });
        }
        return result;
    }

    private RValue AllocationValue(int site)
    {
        if (_escaped.Contains(site) || _shared.Contains(site) || !_allocations.TryGetValue(site, out var allocation))
            return Unknown(UnknownReason.ArrayEscaped, 0, site);
        allocation.Read = true;
        if (allocation.Sourced)
            return SourcedValue(allocation);
        long length = allocation.Length;
        if (length >= 0 && length <= ReflectionSummarizer.MaxAlternatives * 4)
        {
            var children = new int[length];
            allocation.Slots.TryGetValue(-1, out var any);
            for (int k = 0; k < length; k++)
            {
                allocation.Slots.TryGetValue(k, out var slot);
                var element = Join(slot, any);
                children[k] = Child(MayHoldDefault(site, allocation, k) ? Join(element, DefaultElement(allocation.ElementToken)) : element);
            }
            return Known(Term(TermKind.TypeArray, 0, null, length, children));
        }
        var all = MayHoldDefault(site, allocation, -1) ? DefaultElement(allocation.ElementToken) : RValue.None;
        foreach (var slot in SlotValues(allocation))
            all = Join(all, slot);
        return Known(Term(TermKind.TypeArray, 0, null, -1, new[] { Child(all) }));
    }

    // A copy the body wrote: its source's elements slot by slot when the source is one
    // array of known length, else any element of either at any index.
    private RValue SourcedValue(Allocation allocation)
    {
        if (allocation.Slots.Count == 0)
            return allocation.Source;
        allocation.Slots.TryGetValue(-1, out var any);
        var source = allocation.Source;
        if (source.Unknown < 0 && source.Count == 1 && Terms[source.Alternatives![0]] is { Kind: TermKind.TypeArray, Number: >= 0 } fixedLength)
        {
            var children = new int[fixedLength.Children.Length];
            for (int k = 0; k < children.Length; k++)
            {
                allocation.Slots.TryGetValue(k, out var slot);
                children[k] = Child(Join(Join(ValueOf(fixedLength.Children[k]), slot), any));
            }
            return Known(Term(TermKind.TypeArray, 0, null, children.Length, children));
        }
        var all = LoadElement(source, RValue.None);
        foreach (var slot in SlotValues(allocation))
            all = Join(all, slot);
        return Known(Term(TermKind.TypeArray, 0, null, -1, new[] { Child(all) }));
    }

    // Another body stores into any element of the array; its length stays known.
    private void WriteBack(RValue array)
    {
        for (int k = 0; k < array.Count; k++)
        {
            var t = Terms[array.Alternatives![k]];
            if (t.Kind == TermKind.FieldArray)
                _sink!.Escape(_sink.Table.Keys[(int)t.Number]);
            else if (t.Kind == TermKind.Allocation && !_escaped.Contains((int)t.Number)
                && _allocations.TryGetValue((int)t.Number, out var allocation))
                StoreSlot(allocation, -1, Unknown(UnknownReason.ArrayEscaped, 0, (int)t.Number));
        }
    }

    private void Escape(RValue value)
    {
        for (int k = 0; k < value.Count; k++)
        {
            var t = Terms[value.Alternatives![k]];
            if (t.Kind == TermKind.Allocation && _escaped.Add((int)t.Number))
                _newEscape = true;
            else if (t.Kind == TermKind.FieldArray)
                _sink!.Escape(_sink.Table.Keys[(int)t.Number]);
        }
    }

    // --- integers ---

    private RValue IntOp(ILOpCode op, RValue left, RValue right)
    {
        bool unary = op is ILOpCode.Not or ILOpCode.Neg;
        if (left.Unknown >= 0)
            return new RValue(null, left.Unknown);
        if (!unary && right.Unknown >= 0)
            return new RValue(null, right.Unknown);
        if (!AllInts(left) || !unary && !AllInts(right))
            return Opcode(op);
        // A loop counter would grow a set by one value per iteration, so addition and
        // subtraction fold constants only.
        if (op is ILOpCode.Add or ILOpCode.Sub && (left.Count > 1 || right.Count > 1))
            return Opcode(op);
        var result = RValue.None;
        for (int a = 0; a < left.Count; a++)
        {
            long x = Terms[left.Alternatives![a]].Number;
            if (unary)
            {
                result = Join(result, Folded(op, op == ILOpCode.Not ? ~x : unchecked(-x), x, x));
                continue;
            }
            for (int b = 0; b < right.Count; b++)
            {
                long y = Terms[right.Alternatives![b]].Number;
                result = Join(result, Folded(op, op switch
                {
                    ILOpCode.Or => x | y,
                    ILOpCode.And => x & y,
                    ILOpCode.Xor => x ^ y,
                    ILOpCode.Add => unchecked(x + y),
                    _ => unchecked(x - y),
                }, x, y));
            }
        }
        return result;
    }

    // Operands in the 32-bit range may be int32 or wider, whose results agree only while
    // the result stays in that range; a wider operand is int64, which wraps as long does.
    private RValue Folded(ILOpCode op, long result, long x, long y) =>
        IsInt32(x) && IsInt32(y) && !IsInt32(result) ? Opcode(op) : IntValue(result);

    private static bool IsInt32(long value) => value is >= int.MinValue and <= int.MaxValue;

    // An integer is tracked as its signed value whatever its stack width, so a conversion
    // whose result depends on that width (zero extension, native int) keeps only values on
    // which every width agrees; any other value is unknown.
    private RValue Convert(ILOpCode op, RValue value)
    {
        if (op is ILOpCode.Conv_r4 or ILOpCode.Conv_r8 or ILOpCode.Conv_r_un)
            return Opcode(op);
        var result = new RValue(null, value.Unknown);
        for (int k = 0; k < value.Count; k++)
        {
            var t = Terms[value.Alternatives![k]];
            long x = t.Number;
            long? converted = t.Kind != TermKind.Int ? null : op switch
            {
                ILOpCode.Conv_i1 => (sbyte)x,
                ILOpCode.Conv_u1 => (byte)x,
                ILOpCode.Conv_i2 => (short)x,
                ILOpCode.Conv_u2 => (ushort)x,
                ILOpCode.Conv_i4 or ILOpCode.Conv_u4 => (int)x,
                ILOpCode.Conv_i8 or ILOpCode.Conv_ovf_i8 => x,
                ILOpCode.Conv_u8 or ILOpCode.Conv_ovf_u8 or ILOpCode.Conv_ovf_i8_un or ILOpCode.Conv_ovf_u8_un =>
                    x >= 0 ? x : null,
                ILOpCode.Conv_i or ILOpCode.Conv_ovf_i or ILOpCode.Conv_ovf_i4 =>
                    x is >= int.MinValue and <= int.MaxValue ? x : null,
                ILOpCode.Conv_u or ILOpCode.Conv_ovf_u or ILOpCode.Conv_ovf_i_un or ILOpCode.Conv_ovf_u_un
                    or ILOpCode.Conv_ovf_i4_un or ILOpCode.Conv_ovf_u4 or ILOpCode.Conv_ovf_u4_un =>
                    x is >= 0 and <= int.MaxValue ? x : null,
                ILOpCode.Conv_ovf_i1 => x is >= sbyte.MinValue and <= sbyte.MaxValue ? x : null,
                ILOpCode.Conv_ovf_i1_un => x is >= 0 and <= sbyte.MaxValue ? x : null,
                ILOpCode.Conv_ovf_u1 or ILOpCode.Conv_ovf_u1_un => x is >= 0 and <= byte.MaxValue ? x : null,
                ILOpCode.Conv_ovf_i2 => x is >= short.MinValue and <= short.MaxValue ? x : null,
                ILOpCode.Conv_ovf_i2_un => x is >= 0 and <= short.MaxValue ? x : null,
                ILOpCode.Conv_ovf_u2 or ILOpCode.Conv_ovf_u2_un => x is >= 0 and <= ushort.MaxValue ? x : null,
                _ => null,
            };
            result = Join(result, converted is long v ? IntValue(v) : Opcode(op));
        }
        return result;
    }

    private bool AllInts(RValue value)
    {
        if (!value.IsKnown)
            return false;
        for (int k = 0; k < value.Count; k++)
            if (Terms[value.Alternatives![k]].Kind != TermKind.Int)
                return false;
        return true;
    }

    private long? SingleInt(RValue value) =>
        AllInts(value) && value.Count == 1 ? Terms[value.Alternatives![0]].Number : null;

    private bool HasZero(RValue value)
    {
        for (int k = 0; k < value.Count; k++)
            if (Terms[value.Alternatives![k]] is { Kind: TermKind.Int, Number: 0 })
                return true;
        return false;
    }

    private bool HasNonZero(RValue value)
    {
        for (int k = 0; k < value.Count; k++)
            if (Terms[value.Alternatives![k]] is not { Kind: TermKind.Int, Number: 0 })
                return true;
        return false;
    }

    private bool MaybeTrue(RValue value) => value.Unknown >= 0 || HasNonZero(value);

    private bool IsNullOnly(RValue value)
    {
        if (!value.IsKnown)
            return false;
        for (int k = 0; k < value.Count; k++)
            if (Terms[value.Alternatives![k]].Kind != TermKind.Null)
                return false;
        return true;
    }

    // --- terms and values ---

    private RValue Known(int term) => new(new[] { term }, -1);

    private RValue Unknown(UnknownReason reason, int token = 0, int index = 0) =>
        new(null, UnknownIndex(reason, token, index));

    private RValue Opcode(ILOpCode op) => Unknown(UnknownReason.UnsupportedOpcode, 0, (int)op);

    private RValue IntValue(long value) => Known(Term(TermKind.Int, 0, null, value));

    private int NullTerm()
    {
        if (_null < 0)
            _null = Term(TermKind.Null, 0);
        return _null;
    }

    private int UnknownIndex(UnknownReason reason, int token, int index)
    {
        var key = ((byte)reason, token, index);
        if (_unknownIds.TryGetValue(key, out int id))
            return id;
        id = _unknowns.Count;
        _unknowns.Add(new RUnknown(reason, token, index));
        _unknownIds.Add(key, id);
        return id;
    }

    private int Term(TermKind kind, int token, string? text = null, long number = 0, int[]? children = null)
    {
        children ??= Array.Empty<int>();
        var key = new StringBuilder();
        key.Append((int)kind).Append(',').Append(token).Append(',').Append(number);
        foreach (int child in children)
            key.Append(',').Append(child);
        if (text is not null)
            key.Append('"').Append(text);
        string k = key.ToString();
        if (_termIds.TryGetValue(k, out int id))
            return id;
        id = Terms.Count;
        Terms.Add(new RTerm { Kind = kind, Token = token, Text = text, Number = number, Children = children });
        _categories.Add(CategoryOf(kind, number, children));
        _termIds.Add(k, id);
        return id;
    }

    private Category CategoryOf(TermKind kind, long number, int[] children)
    {
        switch (kind)
        {
            case TermKind.Str:
                return Category.String;
            case TermKind.Int:
                return Category.Int;
            case TermKind.Null or TermKind.Set:
                return Category.None;
            case TermKind.TypeArray or TermKind.Allocation or TermKind.FieldArray or TermKind.Enumeration:
                return Category.Sequence;
            case TermKind.Delegate:
                return Category.Delegate;
            case TermKind.ExactObject:
                return Category.Object;
            case TermKind.Closure:
                return Category.Closure;
            case TermKind.Element:
                return children.Length > 0 && children[0] >= 0 && Terms[children[0]] is { Kind: TermKind.Enumeration } list
                    && (ReflectionOp)list.Number == ReflectionOp.GetCustomAttributes ? Category.Object : Category.Member;
        }
        return Category.Member;
    }

    /// <summary>The value a child slot denotes.</summary>
    private RValue ValueOf(int term)
    {
        if (term < 0)
            return RValue.None;
        var t = Terms[term];
        if (t.Kind == TermKind.Set)
            return new RValue(t.Children, (int)t.Number);
        return Known(term);
    }

    private RValue AllChildren(RTerm term)
    {
        var result = RValue.None;
        foreach (int child in term.Children)
            result = Join(result, ValueOf(child));
        return result;
    }

    /// <summary>The term id a composite term holds for a value: the one term itself, or a
    /// <see cref="TermKind.Set"/>.</summary>
    private int Child(RValue value)
    {
        value = Snapshot(value);
        if (value.Count == 1 && value.Unknown < 0)
            return value.Alternatives![0];
        return Term(TermKind.Set, 0, null, value.Unknown, value.Alternatives ?? Array.Empty<int>());
    }

    internal RValue JoinValues(RValue a, RValue b) => Join(a, b);

    private RValue Join(RValue a, RValue b)
    {
        if (a.IsEmpty)
            return b;
        if (b.IsEmpty || Same(a, b))
            return a;
        if (IsBudget(a))
            return a;
        if (IsBudget(b))
            return b;
        var alternatives = MergeSorted(a.Alternatives, b.Alternatives);
        int unknown = a.Unknown >= 0 ? a.Unknown : b.Unknown;
        if (alternatives.Length > ReflectionSummarizer.MaxAlternatives || Mixes(alternatives))
            return Unknown(UnknownReason.Budget);
        return new RValue(alternatives, unknown);
    }

    private bool IsBudget(RValue value) =>
        value.Count == 0 && value.Unknown >= 0 && _unknowns[value.Unknown].Reason == UnknownReason.Budget;

    private bool Mixes(int[] alternatives)
    {
        var seen = Category.None;
        foreach (int id in alternatives)
        {
            var category = _categories[id];
            if (category == Category.None)
                continue;
            if (seen == Category.None)
                seen = category;
            else if (seen != category)
                return true;
        }
        return false;
    }

    private static bool Same(RValue a, RValue b)
    {
        if (a.Unknown != b.Unknown || a.Count != b.Count)
            return false;
        for (int k = 0; k < a.Count; k++)
            if (a.Alternatives![k] != b.Alternatives![k])
                return false;
        return true;
    }

    private static int[] MergeSorted(int[]? a, int[]? b)
    {
        a ??= Array.Empty<int>();
        b ??= Array.Empty<int>();
        var merged = new List<int>(a.Length + b.Length);
        int i = 0, j = 0;
        while (i < a.Length || j < b.Length)
        {
            if (j >= b.Length || i < a.Length && a[i] < b[j])
                merged.Add(a[i++]);
            else if (i >= a.Length || b[j] < a[i])
                merged.Add(b[j++]);
            else
            {
                merged.Add(a[i++]);
                j++;
            }
        }
        return merged.ToArray();
    }

    private static int[] SortDistinct(int[] items)
    {
        var sorted = new List<int>(items);
        sorted.Sort();
        var result = new List<int>(sorted.Count);
        foreach (int item in sorted)
            if (result.Count == 0 || result[result.Count - 1] != item)
                result.Add(item);
        return result.ToArray();
    }

    // --- instruction shapes ---

    private int IndexOf(int offset)
    {
        int lo = 0, hi = _offsets.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (_offsets[mid] == offset)
                return mid;
            if (_offsets[mid] < offset)
                lo = mid + 1;
            else
                hi = mid - 1;
        }
        return -1;
    }

    private int IndexAtOrAfter(int offset)
    {
        int lo = 0, hi = _offsets.Length;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_offsets[mid] < offset)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

    private bool MarkLeader(bool[] leader, int offset)
    {
        if (offset == (_code.Count > 0 ? _code[_code.Count - 1].NextOffset : 0))
        {
            leader[_code.Count] = true;
            return true;
        }
        int index = IndexOf(offset);
        if (index < 0)
            return false;
        leader[index] = true;
        return true;
    }

    private static bool IsStore(ILOpCode op) => op is ILOpCode.Stloc_0 or ILOpCode.Stloc_1 or ILOpCode.Stloc_2
        or ILOpCode.Stloc_3 or ILOpCode.Stloc_s or ILOpCode.Stloc;

    private static int? LocalIndex(Instruction insn) => insn.OpCode switch
    {
        ILOpCode.Ldloc_0 or ILOpCode.Stloc_0 => 0,
        ILOpCode.Ldloc_1 or ILOpCode.Stloc_1 => 1,
        ILOpCode.Ldloc_2 or ILOpCode.Stloc_2 => 2,
        ILOpCode.Ldloc_3 or ILOpCode.Stloc_3 => 3,
        ILOpCode.Ldloc_s or ILOpCode.Ldloc or ILOpCode.Stloc_s or ILOpCode.Stloc or ILOpCode.Ldloca_s
            or ILOpCode.Ldloca => (int)insn.Operand,
        _ => null,
    };

    private static int? ArgIndex(Instruction insn) => insn.OpCode switch
    {
        ILOpCode.Ldarg_0 => 0,
        ILOpCode.Ldarg_1 => 1,
        ILOpCode.Ldarg_2 => 2,
        ILOpCode.Ldarg_3 => 3,
        ILOpCode.Ldarg_s or ILOpCode.Ldarg or ILOpCode.Starg_s or ILOpCode.Starg => (int)insn.Operand,
        _ => null,
    };
}
