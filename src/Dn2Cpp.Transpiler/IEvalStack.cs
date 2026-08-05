using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>
/// The evaluation-stack and emission surface of <see cref="MethodCompiler"/>: every
/// piece of compiler state the intrinsic-table partials
/// (<c>MethodCompiler.EmitIntrinsic.*</c>, <c>GenericIntrinsic</c>, <c>Vector</c>,
/// <c>HttpShim</c>, <c>ValueEquality</c>) are allowed to reach. Those tables hold no
/// state of their own — they read a call site, push and pop operands, and append C++
/// text — so naming that surface keeps them clear of the block/branch/EH machinery
/// they must not disturb: the entry stacks, the region reconstruction, the liveness
/// map, the temp counter's declaration list.
///
/// The boundary is drawn at *state*, not at helpfulness. A member belongs here when it
/// is the intrinsic tables' only route to a <c>MethodCompiler</c> field; the emission
/// helpers they also call stay class-private peers, since each is itself written over
/// this surface and adding them would turn an interface into a mirror of the class.
///
/// <para><b>Reading the top of the stack is separate from consuming it, and that is the
/// point.</b> <see cref="Top"/>, <see cref="Peek"/> and <see cref="StackDepth"/> neither
/// pop nor emit, so a <c>when</c> guard may ask what the pending operand is before its
/// arm has committed to handling the call: a guard may ask the question, only the taken
/// arm may answer it. An arm that inspected the operand by popping it would have to push
/// it back on every path it declines, and one that inspected it by *building* the answer
/// would emit a spill above a loop that does not exist yet, and record shared-body call
/// edges for code nothing emits.</para>
///
/// <para><see cref="Push"/> spills its expression to a fresh temp;
/// <see cref="PushEntry"/> does not, and exists because an arm that has already
/// materialized its value — or that must carry metadata <see cref="Push"/> cannot
/// express (<c>StaticType</c>, <c>NonNull</c>, <c>KnownNull</c>, <c>StrLiteral</c>) —
/// would otherwise reach past this surface into the stack list itself.</para>
/// </summary>
internal interface IEvalStack
{
    // ---- the call site being translated ----

    Compilation Comp { get; }
    MethodInfo Method { get; }
    Module Module { get; }
    MetadataReader Reader { get; }
    LiteralPool Literals { get; }

    /// <summary>Set when the call being translated is a <c>callvirt</c>. An arm whose
    /// lowering differs between the virtual and non-virtual forms of one BCL method
    /// reads it rather than re-deriving the opcode.</summary>
    bool CallIsVirtual { get; }

    /// <summary>Armed while compiling a canonical-owner-world body under
    /// <c>--shared-generics</c>. An arm that would bake an answer computed about
    /// <c>__Canon</c> into a body shared by every real instantiation tests it, or taints
    /// through the class's <c>TaintIfCanonical</c> peers, before it decides.</summary>
    bool SharedTrial { get; }

    // ---- the evaluation stack ----

    int StackDepth { get; }

    /// <summary>The pending operand, readable and replaceable without popping — the
    /// replacement is how an arm re-tags what it just pushed (<c>Top = Top with
    /// { StaticType = … }</c>) when the metadata only becomes known after the push.</summary>
    StackEntry Top { get; set; }

    /// <summary>The operand <paramref name="fromTop"/> places down, 1 being
    /// <see cref="Top"/>.</summary>
    StackEntry Peek(int fromTop);

    void Push(StackKind kind, string cppType, string expr, TypeDesc? typeToken = null);

    void PushEntry(StackEntry entry);

    StackEntry Pop();

    // ---- the C++ sink ----

    void Emit(string line);

    string NewTemp(string cppType);

    string NewLocalArray(string elemCppType, int count);
}
