using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Which instance bodies a CreateDelegate binding called with a null receiver — closed
// over null, or open over a non-virtual row — may enter: those .NET runs as well.
internal sealed partial class CppEmitter
{
    // DN2CPP_MTHA_NULLSAFE, DN2CPP_MTHA_NULLCONTEXT, DN2CPP_MTHA_NULLCTX_NULL and the shifts of
    // DN2CPP_MTHA_NULLLOOKUP_REF / DN2CPP_MTHA_NULLLOOKUP_VALUE.
    private const int NullSafeRow = 0x100;
    private const int NullContextRow = 0x200;
    private const int NullContextOnNullRow = 0x4000;
    private const int NullLookupRefShift = 16;
    private const int NullLookupValueShift = 24;

    // A body sees the actual null receiver, a type-only stand-in, or null with
    // its generic context supplied separately to the first prologue.
    private enum NullReceiverRun
    {
        Null,
        StandIn,
        ContextOnNull,
    }

    // What a body does with a null receiver, apart from what its callees do. Faults: it
    // dereferences the receiver where .NET's does not, or cannot be told. NullOnly: it
    // meets a null receiver as .NET's does but not a stand-in: a body the runtime
    // supplies, which takes or faults on a null receiver, or a synchronized one, whose
    // monitor enter rejects it. Forwards: it loads its receiver only as the receiver of
    // a Callees call. StraightForwarder: its code, nops aside, is a load of its receiver,
    // the call of its only callee and the return. The lookup masks name the class type
    // parameters for which .NET's code, shared over a reference type argument
    // (RefLookups) or a value type argument holding one (ValueLookups), looks its
    // generic dictionary up through the receiver.
    private sealed record NullReceiverScan(
        bool Faults,
        bool NullOnly,
        bool ReadsContext,
        bool Forwards,
        bool StraightForwarder,
        ulong RefLookups,
        ulong ValueLookups,
        List<MethodInfo>? Callees);

    private static readonly NullReceiverScan FaultingScan = new(true, false, false, false, false, 0, 0, null);
    private static readonly NullReceiverScan NullOnlyScan = new(false, true, false, false, false, 0, 0, null);

    // One body of a NullReceiverVerdict walk: its visit number, the lowest number it
    // reaches through bodies still open, its place on the open stack, and its own
    // verdict joined with those of the components it calls into. Callees is null for a
    // body that faults on its own.
    private sealed class NullReceiverVisit
    {
        public readonly MethodInfo Method;
        public readonly int Number;
        public readonly int Slot;
        public readonly List<MethodInfo>? Callees;
        public int Low;
        public int Next;
        public bool Runs;
        public ulong Ref;
        public ulong Value;

        public NullReceiverVisit(MethodInfo method, int number, int slot, List<MethodInfo>? callees,
            bool runs, ulong refs, ulong values)
        {
            Method = method;
            Number = number;
            Slot = slot;
            Callees = callees;
            Low = number;
            Runs = runs;
            Ref = refs;
            Value = values;
        }
    }

    private readonly Dictionary<MethodInfo, NullReceiverScan> _nullReceiverScans = new();
    private readonly Dictionary<(MethodInfo, NullReceiverRun), (bool Runs, ulong Ref, ulong Value)>
        _nullReceiverVerdicts = new();
    private readonly Dictionary<MethodInfo, (bool Runs, ulong Ref, ulong Value)> _nullReceiverContextVerdicts = new();

    /// <summary>The DN2CPP_MTHA_* bits under which a CreateDelegate binding called with a
    /// null receiver (closed over null, or open over a non-virtual row) enters m's body,
    /// or 0 where .NET's call raises NullReferenceException first. NULLSAFE: the body and
    /// every callee it may pass the receiver to run with a null receiver as .NET's do.
    /// NULLCONTEXT: m's shared body, and each callee it passes the receiver on to, reads
    /// nothing off the receiver but the generic context, which .NET's code for the
    /// instantiation does not look up, so it runs on a stand-in receiver that carries only
    /// the declaring type. A runtime template level's row also names the lookups the
    /// clone's arguments decide (DN2CPP_MTHA_NULLLOOKUP_*). NULLCTX_NULL leaves the
    /// receiver null while supplying the context separately, so a body may test it.</summary>
    internal int NullReceiverAttrs(ClassInfo cls, MethodInfo m)
    {
        var onNull = NullReceiverVerdict(cls, m, NullReceiverRun.Null);
        if (onNull.Runs)
            return NullSafeRow | NullLookupAttrs(onNull.Ref, onNull.Value);
        if (cls.IsValueType || cls.IsInterface)
            return 0;
        var onStandIn = NullReceiverVerdict(cls, m, NullReceiverRun.StandIn);
        if (onStandIn.Runs)
            return NullContextRow | NullLookupAttrs(onStandIn.Ref, onStandIn.Value);
        var withContext = NullReceiverContextVerdict(cls, m);
        return withContext.Runs
            ? NullContextOnNullRow | NullLookupAttrs(withContext.Ref, withContext.Value)
            : 0;
    }

    // One byte per mask: bit n for class type parameter n, the last bit for every later one.
    private static int NullLookupAttrs(ulong refs, ulong values)
    {
        static int Byte(ulong mask) => (int)(mask & 0x7F) | ((mask >> 7) != 0 ? 0x80 : 0);
        return (Byte(refs) << NullLookupRefShift) | (Byte(values) << NullLookupValueShift);
    }

    // Whether m runs under a null receiver replaced as `run` says: its own body does, and
    // so does every body the receiver may be passed on to, however deep. A call cycle adds
    // no dereference of its own, so a strongly connected component of the receiver-passing
    // calls runs when each of its bodies and every component it calls into runs. Tarjan's
    // walk keeps only finished components' verdicts, so each is exact and independent of
    // which row asked first, and each body is visited once. A body known to fault visits no
    // more callees: its component and its callers fault whatever else it reaches. The masks
    // are a runtime template level's lookups over the bodies reached; template levels
    // project their type parameters onto their bases' in order.
    private (bool Runs, ulong Ref, ulong Value) NullReceiverVerdict(ClassInfo cls, MethodInfo m, NullReceiverRun run)
    {
        if (_nullReceiverVerdicts.TryGetValue((m, run), out var known))
            return known;
        var visits = new Dictionary<MethodInfo, NullReceiverVisit>();
        var open = new List<NullReceiverVisit>();
        var path = new List<NullReceiverVisit> { Enter(cls, m) };
        while (path.Count > 0)
        {
            var visit = path[^1];
            if (visit.Runs && visit.Callees is { } callees && visit.Next < callees.Count)
            {
                var callee = callees[visit.Next++];
                if (_nullReceiverVerdicts.TryGetValue((callee, run), out var finished))
                    Join(visit, finished);
                else if (visits.TryGetValue(callee, out var reached))
                    visit.Low = Math.Min(visit.Low, reached.Number);
                else
                    path.Add(Enter(callee.DeclaringClass, callee));
                continue;
            }
            path.RemoveAt(path.Count - 1);
            if (visit.Low == visit.Number)
            {
                bool runs = true;
                ulong refs = 0, values = 0;
                for (int i = visit.Slot; i < open.Count; i++)
                {
                    runs &= open[i].Runs;
                    refs |= open[i].Ref;
                    values |= open[i].Value;
                }
                var verdict = runs ? (true, refs, values) : (false, 0UL, 0UL);
                for (int i = visit.Slot; i < open.Count; i++)
                    _nullReceiverVerdicts[(open[i].Method, run)] = verdict;
                open.RemoveRange(visit.Slot, open.Count - visit.Slot);
                if (path.Count > 0)
                    Join(path[^1], verdict);
            }
            else
            {
                var caller = path[^1];
                caller.Low = Math.Min(caller.Low, visit.Low);
            }
        }
        return _nullReceiverVerdicts[(m, run)];

        NullReceiverVisit Enter(ClassInfo c, MethodInfo body)
        {
            var scan = ScanUnderNullReceiver(c, body);
            bool runs = OwnBodyRuns(c, scan, run, out ulong refs, out ulong values);
            var visit = new NullReceiverVisit(body, visits.Count, open.Count, runs ? scan.Callees : null,
                runs, refs, values);
            visits.Add(body, visit);
            open.Add(visit);
            return visit;
        }

        static void Join(NullReceiverVisit visit, (bool Runs, ulong Ref, ulong Value) verdict)
        {
            visit.Runs &= verdict.Runs;
            visit.Ref |= verdict.Ref;
            visit.Value |= verdict.Value;
        }
    }

    // A null binding supplies one receiver-derived context. Before the body that
    // consumes it, only a single straight-line call forwarding this may run, so the
    // bodies before it form a chain, and one that leads back into itself never consumes
    // the context. Afterward, callees must run as they do under a null receiver.
    private (bool Runs, ulong Ref, ulong Value) NullReceiverContextVerdict(ClassInfo cls, MethodInfo m)
    {
        var chain = new List<(MethodInfo Method, ulong Ref, ulong Value)>();
        var onChain = new HashSet<MethodInfo>();
        (bool Runs, ulong Ref, ulong Value) verdict = (false, 0, 0);
        var (c, body) = (cls, m);
        while (!_nullReceiverContextVerdicts.TryGetValue(body, out verdict) && onChain.Add(body))
        {
            var scan = ScanUnderNullReceiver(c, body);
            bool runs = OwnBodyRuns(c, scan, NullReceiverRun.ContextOnNull, out ulong refs, out ulong values);
            chain.Add((body, refs, values));
            if (!runs)
                break;
            if (!scan.ReadsContext)
            {
                if (!scan.StraightForwarder)
                    break;
                body = scan.Callees![0];
                c = body.DeclaringClass;
                continue;
            }
            verdict = (true, 0, 0);
            if (scan.Callees is not null)
                foreach (var callee in scan.Callees)
                {
                    var next = NullReceiverVerdict(callee.DeclaringClass, callee, NullReceiverRun.Null);
                    if (!next.Runs)
                    {
                        verdict = (false, 0, 0);
                        break;
                    }
                    verdict.Ref |= next.Ref;
                    verdict.Value |= next.Value;
                }
            break;
        }
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            if (verdict.Runs)
                verdict = (true, verdict.Ref | chain[i].Ref, verdict.Value | chain[i].Value);
            _nullReceiverContextVerdicts[chain[i].Method] = verdict;
        }
        return verdict;
    }

    private static bool IsStraightReceiverForwarder(List<Instruction> code)
    {
        int step = 0;
        foreach (var insn in code)
        {
            if (insn.OpCode == ILOpCode.Nop)
                continue;
            bool expected = step switch
            {
                0 => insn.OpCode == ILOpCode.Ldarg_0
                    || ((insn.OpCode is ILOpCode.Ldarg_s or ILOpCode.Ldarg) && insn.Operand == 0),
                1 => insn.OpCode == ILOpCode.Call,
                2 => insn.OpCode == ILOpCode.Ret,
                _ => false,
            };
            if (!expected)
                return false;
            step++;
        }
        return step == 3;
    }

    // Whether the scanned body itself runs under the replaced receiver. A lookup of a
    // runtime template level waits for the clone's arguments; any other class's decides
    // here. A null receiver reaches a value type's body as a null `this` pointer, which it
    // may only pass on, and a reference type's, which faults wherever .NET's does unless
    // the body reads its context off it. A stand-in must only ever be passed on.
    private bool OwnBodyRuns(ClassInfo cls, NullReceiverScan scan, NullReceiverRun run,
        out ulong refs, out ulong values)
    {
        refs = values = 0;
        if (scan.Faults)
            return false;
        if (scan.NullOnly)
            return run != NullReceiverRun.StandIn;
        if (IsRuntimeTemplateLevel(cls))
        {
            refs = scan.RefLookups;
            values = scan.ValueLookups;
        }
        else if (LookupHit(cls, scan.RefLookups, scan.ValueLookups))
            return false;
        return run == NullReceiverRun.Null
            ? !scan.ReadsContext && (!cls.IsValueType || scan.Forwards)
            : run == NullReceiverRun.StandIn
                ? !cls.IsValueType && !cls.IsInterface && scan.Forwards
                : !cls.IsValueType && !cls.IsInterface;
    }

    // Whether .NET's code for cls reads its generic dictionary off the receiver for one of
    // the masks' lookups: a class type argument it shares code over, which the lookup needs.
    private static bool LookupHit(ClassInfo cls, ulong refs, ulong values)
    {
        if ((refs | values) == 0)
            return false;
        var args = cls.Context.TypeArgs;
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!SharesAsCanon(arg))
                continue;
            ulong lookups = arg.Kind switch
            {
                TypeKind.Class when arg.Class!.IsValueType || arg.Class.IsEnum => values,
                TypeKind.Primitive or TypeKind.Class or TypeKind.SZArray or TypeKind.MDArray => refs,
                _ => refs | values,
            };
            if ((lookups & (1UL << Math.Min(i, 63))) != 0)
                return true;
        }
        return false;
    }

    private NullReceiverScan ScanUnderNullReceiver(ClassInfo cls, MethodInfo m)
    {
        if (!_nullReceiverScans.TryGetValue(m, out var scan))
            _nullReceiverScans[m] = scan = ScanOwnBodyUnderNullReceiver(cls, m);
        return scan;
    }

    // What m's own emitted body does with a null receiver. A value type's body reads its
    // fields through an unchecked pointer, so it may load its receiver only to pass it on.
    // A reference type's field accesses and virtual calls fault on a null receiver as
    // .NET's do, and a non-virtual instance call runs its callee with the receiver the
    // caller passes, so Callees lists each callee that may be passed m's own: an instance
    // method of m's class or a base, since the receiver's static type must be that class,
    // called on a load of the receiver unless one may flow elsewhere (ReceivingCalls).
    // .NET's code for a reference type's instance method shared over a type argument looks
    // its generic dictionary up through the receiver, and dn2cpp's shared body reads its
    // generic context there. Replaced bodies never run; System.Enum's replacements fault
    // as .NET's bodies do, and a non-virtual call of Object's GetHashCode or Equals runs
    // the identity hash or reference equality, which take a null receiver as .NET's do.
    private NullReceiverScan ScanOwnBodyUnderNullReceiver(ClassInfo cls, MethodInfo m)
    {
        if (CoreIntrinsics.BrEnumInstanceFormat.Matches(cls.FullName, m.Name) || IsNullTolerantObjectMember(cls, m))
            return NullOnlyScan;
        if (HasReplacedBody(cls, m) || _backend.ShouldSkipMethodBody(cls, m))
            return FaultingScan;
        // A reference type's synchronized body first enters the monitor of its receiver,
        // which raises .NET's ArgumentNullException for a null one.
        if (m.IsSynchronized && !cls.IsValueType)
            return NullOnlyScan;
        var emitted = m.Emittable;
        bool readsContext = emitted.RgctxUses && !emitted.RgctxParam;
        bool classLookups = !cls.IsValueType && m.Context.MethodArgs.Length == 0;
        var module = m.Module;
        var reader = module.Reader;
        var code = ILDecoder.Decode(module.PE.GetMethodBody(m.Rva).GetILBytes()!.ToImmutableArrayCompat());
        List<MethodInfo>? callees = null;
        List<int>? loads = null;
        List<int>? instanceCalls = null;
        HashSet<int>? knownCalls = null;
        ulong refs = 0, values = 0;
        bool opaque = false, afterReadonly = false;
        for (int i = 0; i < code.Count; i++)
        {
            var insn = code[i];
            bool readonlyPrefix = afterReadonly;
            afterReadonly = insn.OpCode == ILOpCode.Readonly;
            switch (insn.OpCode)
            {
                case ILOpCode.Ldarg_0:
                    (loads ??= new()).Add(i);
                    continue;
                case ILOpCode.Ldarg_s or ILOpCode.Ldarg:
                    if (insn.Operand == 0)
                        (loads ??= new()).Add(i);
                    continue;
                case ILOpCode.Ldarga_s or ILOpCode.Ldarga or ILOpCode.Starg_s or ILOpCode.Starg:
                    if (insn.Operand == 0)
                        return FaultingScan;
                    continue;
                case ILOpCode.Calli:
                    opaque = true;
                    continue;
            }
            if (classLookups)
                NoteDictionaryLookups(module, code, i, readonlyPrefix, ref refs, ref values);
            if (insn.OpCode == ILOpCode.Call && CallsInstanceMethod(reader, insn.Token))
                (instanceCalls ??= new()).Add(i);
        }
        // A body that never loads its receiver passes it to no callee.
        if (loads is null)
            return new NullReceiverScan(false, false, readsContext, true, false, refs, values, null);
        if (instanceCalls is not null)
        {
            var receiving = ReceivingCalls(reader, code, loads, cls.IsValueType);
            foreach (int i in instanceCalls)
            {
                if (receiving is not null && !receiving.Contains(i))
                    continue;
                if (ReceiverCalleeOrNull(cls, m, code[i].Token, out bool known) is { } callee)
                {
                    (callees ??= new()).Add(callee);
                    (knownCalls ??= new()).Add(i);
                }
                opaque |= !known;
            }
        }
        if (opaque)
            return FaultingScan;
        bool forwards = true;
        foreach (int load in loads)
        {
            int consumer = ConsumerOf(reader, code, load, out int above);
            if (consumer < 0 || knownCalls?.Contains(consumer) != true
                || !CallShape(reader, code[consumer].Token, out int parameters, out _, out _)
                || parameters != above)
            {
                forwards = false;
                break;
            }
        }
        bool straight = callees is { Count: 1 } && IsStraightReceiverForwarder(code);
        return new NullReceiverScan(false, false, readsContext, forwards, straight, refs, values, callees);
    }

    private static bool IsNullTolerantObjectMember(ClassInfo cls, MethodInfo m) =>
        cls.FullName == "System.Object"
        && m.Name switch
        {
            "GetHashCode" => m.Signature.ParameterTypes.Length == 0,
            "Equals" => m.Signature.ParameterTypes is [{ IsObject: true }],
            _ => false,
        };

    // The callee of a non-virtual instance call that may be passed m's receiver, or null
    // when the callee is declared outside m's class and bases. `known` is false when the
    // call may receive it but its body cannot be told: a non-virtual interface call, a
    // callee without a compiled body, or a base chain not rooted at System.Object.
    private MethodInfo? ReceiverCalleeOrNull(ClassInfo cls, MethodInfo m, int token, out bool known)
    {
        known = true;
        var handle = SRME.EntityHandle(token);
        if (_c.DeclaringDefinitionOf(m.Module, handle) is not { } def)
            return null;
        if (IsInterfaceDefinition(def))
        {
            known = false;
            return null;
        }
        for (var c = cls; c is not null; c = c.BaseClass)
        {
            if (c.Module == def.Module && c.Handle == def.Handle)
            {
                MethodInfo? callee = null;
                try
                {
                    callee = _c.ResolveMethodHandle(m.Module, handle, m.Context, cls);
                }
                catch (NotSupportedException e) when (!Compilation.IsMustEscape(e))
                {
                }
                known = callee is not null
                    && (IsNullTolerantObjectMember(callee.DeclaringClass, callee) || _c.Reachable.Contains(callee));
                return known ? callee : null;
            }
            if (c.BaseClass is null)
                known = c.FullName == "System.Object";
        }
        return null;
    }

    // Whether .NET's code over the type argument t is shared: t's canonical form holds a
    // reference type, as a reference type's does and a generic value type's over one.
    private static bool SharesAsCanon(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object,
        TypeKind.Class => !(t.Class!.IsValueType || t.Class.IsEnum) || t.Class.Context.TypeArgs.Any(SharesAsCanon),
        TypeKind.ByRef or TypeKind.Pointer => false,
        _ => true,
    };

    // Adds the class type parameters for which .NET's code shared over them looks
    // code[i]'s operand up in the generic dictionary: an exact type, static member or
    // generic method instantiation mentioning one. Field accesses, element loads and
    // stores, and instance calls through a class slot need no lookup. A box and a
    // constrained call need one only for a value type, and an element address only to
    // check a reference element type. The JIT folds a box a branch tests directly or
    // through a type test naming no class type parameter.
    private void NoteDictionaryLookups(Module module, List<Instruction> code, int i, bool readonlyPrefix,
        ref ulong refs, ref ulong values)
    {
        var reader = module.Reader;
        var insn = code[i];
        switch (insn.OpCode)
        {
            case ILOpCode.Ldtoken or ILOpCode.Newarr or ILOpCode.Newobj or ILOpCode.Castclass
                or ILOpCode.Isinst or ILOpCode.Unbox or ILOpCode.Unbox_any or ILOpCode.Mkrefany
                or ILOpCode.Refanyval or ILOpCode.Ldsfld or ILOpCode.Ldsflda or ILOpCode.Stsfld:
                AddBoth(Mentions(reader, insn.Token), ref refs, ref values);
                return;
            case ILOpCode.Ldelema:
                if (!readonlyPrefix)
                    AddStored(reader, insn.Token, valueType: false, ref refs, ref values);
                return;
            case ILOpCode.Box:
                if (TestsOnlyByBranch(code, i + 1)
                    || (i + 1 < code.Count && code[i + 1].OpCode == ILOpCode.Isinst
                        && Mentions(reader, code[i + 1].Token) == 0 && TestsOnlyByBranch(code, i + 2)))
                    return;
                AddStored(reader, insn.Token, valueType: true, ref refs, ref values);
                return;
            case ILOpCode.Constrained:
                AddStored(reader, insn.Token, valueType: true, ref refs, ref values);
                return;
            case ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Ldftn or ILOpCode.Ldvirtftn:
            {
                ulong mask = Mentions(reader, insn.Token);
                if (mask == 0)
                    return;
                var handle = SRME.EntityHandle(insn.Token);
                // An interface slot resolves against the exact interface.
                if (handle.Kind == HandleKind.MethodSpecification || !CallsInstanceMethod(reader, insn.Token)
                    || (insn.OpCode is ILOpCode.Callvirt or ILOpCode.Ldvirtftn
                        && (_c.DeclaringDefinitionOf(module, handle) is not { } def || IsInterfaceDefinition(def))))
                    AddBoth(mask, ref refs, ref values);
                return;
            }
        }

        static ulong Mentions(MetadataReader reader, int token) =>
            ClassTypeParameters.Of(reader, SRME.EntityHandle(token));

        static bool TestsOnlyByBranch(List<Instruction> code, int at) =>
            at < code.Count
            && code[at].OpCode is ILOpCode.Brtrue or ILOpCode.Brtrue_s or ILOpCode.Brfalse or ILOpCode.Brfalse_s;

        static void AddBoth(ulong mask, ref ulong refs, ref ulong values)
        {
            refs |= mask;
            values |= mask;
        }

        // A lookup of a type only when it is stored as a value type (a box or a
        // constrained call) or only when it is stored as a reference (an element
        // address's check): a class type parameter decides by its argument, any other
        // type by its own storage, whatever arguments it mentions.
        static void AddStored(MetadataReader reader, int token, bool valueType, ref ulong refs, ref ulong values)
        {
            var type = SRME.EntityHandle(token);
            ulong mask = ClassTypeParameters.Of(reader, type);
            if (mask == 0)
                return;
            if (ClassTypeParameters.BareIndex(reader, type) is int index)
            {
                ulong bit = 1UL << Math.Min(index, 63);
                if (valueType)
                    values |= bit;
                else
                    refs |= bit;
            }
            else if (ClassTypeParameters.IsValueTypeInstance(reader, type) == valueType)
                AddBoth(mask, ref refs, ref values);
        }
    }

    // The instruction consuming the value code[load] pushes: the first instruction of the
    // straight-line code after it that pops it, a conditional branch included, with the
    // count of values above it there, or -1 past a branch or an instruction whose stack
    // effect is not tabled.
    private static int ConsumerOf(MetadataReader reader, List<Instruction> code, int load, out int above)
    {
        above = 0;
        for (int j = load + 1; j < code.Count; j++)
        {
            if (ConditionalBranchPops(code[j].OpCode) is int tested)
                return tested > above ? j : -1;
            if (!StackEffect(reader, code[j], out int pop, out int push))
                return -1;
            if (pop > above)
                return j;
            above += push - pop;
        }
        return -1;
    }

    // The calls given a receiver loaded at `loads`, when every load ends at its consumer:
    // a branch or comparison testing it, a pop, a non-virtual call it is the receiver of
    // returning void or a fixed-size primitive, which carries no reference, or for a
    // reference type a field access or callvirt through it, which faults on null. Null
    // when a load may flow on to other code.
    private static HashSet<int>? ReceivingCalls(MetadataReader reader, List<Instruction> code, List<int> loads,
        bool valueType)
    {
        var calls = new HashSet<int>();
        foreach (int load in loads)
        {
            int at = ConsumerOf(reader, code, load, out int above);
            if (at < 0)
                return null;
            var insn = code[at];
            switch (insn.OpCode)
            {
                case ILOpCode.Pop or ILOpCode.Ceq or ILOpCode.Cgt or ILOpCode.Cgt_un or ILOpCode.Clt
                    or ILOpCode.Clt_un:
                    continue;
                case ILOpCode.Ldfld or ILOpCode.Ldflda when !valueType:
                    continue;
                case ILOpCode.Stfld when !valueType && above == 1:
                    continue;
                case ILOpCode.Call or ILOpCode.Callvirt:
                    if (!CallShape(reader, insn.Token, out int parameters, out bool instance, out var returned)
                        || !instance || above != parameters)
                        return null;
                    if (insn.OpCode == ILOpCode.Callvirt)
                    {
                        if (valueType)
                            return null;
                        continue;
                    }
                    if (returned is not (SignatureTypeCode.Void
                        or >= SignatureTypeCode.Boolean and <= SignatureTypeCode.Double))
                        return null;
                    calls.Add(at);
                    continue;
                default:
                    if (ConditionalBranchPops(insn.OpCode) is null)
                        return null;
                    continue;
            }
        }
        return calls;
    }

    // The values a conditional branch tests, or null for any other instruction.
    private static int? ConditionalBranchPops(ILOpCode op) => op switch
    {
        ILOpCode.Brtrue or ILOpCode.Brtrue_s or ILOpCode.Brfalse or ILOpCode.Brfalse_s or ILOpCode.Switch => 1,
        >= ILOpCode.Beq_s and <= ILOpCode.Blt_un_s or >= ILOpCode.Beq and <= ILOpCode.Blt_un => 2,
        _ => null,
    };

    // The values an instruction pops and pushes, for the instructions straight-line code
    // computing call arguments uses; false for control flow and anything else.
    private static bool StackEffect(MetadataReader reader, in Instruction insn, out int pop, out int push)
    {
        pop = 0;
        push = 0;
        switch (insn.OpCode)
        {
            case ILOpCode.Nop or ILOpCode.Constrained or ILOpCode.Readonly or ILOpCode.Volatile
                or ILOpCode.Unaligned or ILOpCode.Tail:
                return true;
            case >= ILOpCode.Ldarg_0 and <= ILOpCode.Ldloc_3:
            case ILOpCode.Ldarg_s or ILOpCode.Ldarga_s or ILOpCode.Ldloc_s or ILOpCode.Ldloca_s
                or ILOpCode.Ldarg or ILOpCode.Ldarga or ILOpCode.Ldloc or ILOpCode.Ldloca:
            case >= ILOpCode.Ldnull and <= ILOpCode.Ldc_r8:
            case ILOpCode.Ldstr or ILOpCode.Ldsfld or ILOpCode.Ldsflda or ILOpCode.Ldtoken or ILOpCode.Ldftn
                or ILOpCode.Sizeof:
                push = 1;
                return true;
            case ILOpCode.Dup:
                pop = 1;
                push = 2;
                return true;
            case >= ILOpCode.Stloc_0 and <= ILOpCode.Stloc_3:
            case ILOpCode.Starg_s or ILOpCode.Stloc_s or ILOpCode.Starg or ILOpCode.Stloc or ILOpCode.Pop
                or ILOpCode.Stsfld or ILOpCode.Initobj:
                pop = 1;
                return true;
            case >= ILOpCode.Ldind_i1 and <= ILOpCode.Ldind_ref:
            case >= ILOpCode.Neg and <= ILOpCode.Conv_u8:
            case >= ILOpCode.Conv_ovf_i1_un and <= ILOpCode.Conv_ovf_u_un:
            case >= ILOpCode.Conv_ovf_i1 and <= ILOpCode.Conv_ovf_u8:
            case >= ILOpCode.Conv_u2 and <= ILOpCode.Conv_ovf_u:
            case ILOpCode.Conv_r_un or ILOpCode.Conv_u or ILOpCode.Ldfld or ILOpCode.Ldflda or ILOpCode.Isinst
                or ILOpCode.Castclass or ILOpCode.Box or ILOpCode.Unbox or ILOpCode.Unbox_any or ILOpCode.Ldlen
                or ILOpCode.Ldobj or ILOpCode.Ldvirtftn or ILOpCode.Ckfinite or ILOpCode.Newarr:
                pop = 1;
                push = 1;
                return true;
            case >= ILOpCode.Add and <= ILOpCode.Shr_un:
            case >= ILOpCode.Ldelema and <= ILOpCode.Ldelem_ref:
            case >= ILOpCode.Add_ovf and <= ILOpCode.Sub_ovf_un:
            case >= ILOpCode.Ceq and <= ILOpCode.Clt_un:
            case ILOpCode.Ldelem:
                pop = 2;
                push = 1;
                return true;
            case >= ILOpCode.Stind_ref and <= ILOpCode.Stind_r8:
            case ILOpCode.Stind_i or ILOpCode.Stfld or ILOpCode.Stobj or ILOpCode.Cpobj:
                pop = 2;
                return true;
            case >= ILOpCode.Stelem_i and <= ILOpCode.Stelem_ref:
            case ILOpCode.Stelem:
                pop = 3;
                return true;
            case ILOpCode.Call or ILOpCode.Callvirt:
                if (!CallShape(reader, insn.Token, out int parameters, out bool instance, out var returned))
                    return false;
                pop = parameters + (instance ? 1 : 0);
                push = returned != SignatureTypeCode.Void ? 1 : 0;
                return true;
            case ILOpCode.Newobj:
                if (!CallShape(reader, insn.Token, out parameters, out _, out _))
                    return false;
                pop = parameters;
                push = 1;
                return true;
            default:
                return false;
        }
    }

    private static bool IsInterfaceDefinition((Module Module, TypeDefinitionHandle Handle) def) =>
        (def.Module.Reader.GetTypeDefinition(def.Handle).Attributes & System.Reflection.TypeAttributes.Interface) != 0;

    // Whether a call token names an instance method: its signature's HASTHIS bit.
    private static bool CallsInstanceMethod(MetadataReader reader, int token) =>
        !CallShape(reader, token, out _, out bool instance, out _) || instance;

    // A call token's signature: its parameter count, HASTHIS bit and return type code.
    // False when the token names no method signature.
    private static bool CallShape(MetadataReader reader, int token, out int parameters, out bool instance,
        out SignatureTypeCode returned)
    {
        parameters = 0;
        instance = false;
        returned = SignatureTypeCode.Void;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.MethodSpecification)
            handle = reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
        BlobHandle signature = handle.Kind switch
        {
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)handle).Signature,
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)handle).Signature,
            _ => default,
        };
        if (signature.IsNil)
            return false;
        var blob = reader.GetBlobReader(signature);
        var header = blob.ReadSignatureHeader();
        if (header.Kind != SignatureKind.Method)
            return false;
        instance = header.IsInstance;
        if (header.IsGeneric)
            blob.ReadCompressedInteger();
        parameters = blob.ReadCompressedInteger();
        var ret = blob.ReadSignatureTypeCode();
        while (ret is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
        {
            blob.ReadTypeHandle();
            ret = blob.ReadSignatureTypeCode();
        }
        returned = ret;
        return true;
    }
}
