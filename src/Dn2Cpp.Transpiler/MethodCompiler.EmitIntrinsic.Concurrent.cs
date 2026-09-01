using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitConcurrentIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {
            // ---- System.Collections.Concurrent.BlockingCollection<T> ----
            // A producer/consumer blocking queue. Elements ride in a uniform boxed slot;
            // the call site boxes (Add) / unboxes (Take) by T's kind, recovered from the
            // Add parameter / Take return / TryTake `out` element type. The CancellationToken
            // and TimeSpan-timeout overloads are carve-outs (the int-ms forms cover the gate).
            case ("System.Collections.Concurrent.BlockingCollection", "Add"):
            {
                if (sig.ParameterTypes.Length != 1)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: only Add(T) is " +
                        "supported (the Add(T, CancellationToken) overload is a carve-out)");
                var elemT = sig.ParameterTypes[0];
                var val = Pop();  // item
                var coll = Pop(); // this
                string stored = BlockingCollBoxElement(elemT, val);
                Emit($"dn2cpp_blockingcoll_add((Dn2CppObject*)({coll.Expr}), {stored});");
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "TryAdd"):
            {
                string ms;
                if (sig.ParameterTypes.Length == 1)
                    ms = "0"; // single immediate attempt
                else if (sig.ParameterTypes.Length == 2
                    && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 })
                    ms = $"(int32_t)({Pop().Expr})";
                else
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: only TryAdd(T) and " +
                        "TryAdd(T, int millisecondsTimeout) are supported (the TimeSpan / " +
                        "CancellationToken overloads are carve-outs)");
                var elemT = sig.ParameterTypes[0];
                var val = Pop();  // item
                var coll = Pop(); // this
                string stored = BlockingCollBoxElement(elemT, val);
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_blockingcoll_tryadd((Dn2CppObject*)({coll.Expr}), {stored}, {ms});");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "Take"):
            {
                if (sig.ParameterTypes.Length != 0)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: only Take() is " +
                        "supported (the Take(CancellationToken) overload is a carve-out)");
                var elemT = sig.ReturnType;
                var coll = Pop(); // this
                string box = NewTemp("Dn2CppObject*");
                Emit($"{box} = dn2cpp_blockingcoll_take((Dn2CppObject*)({coll.Expr}));");
                var (kind, ct, expr) = BlockingCollUnboxElement(elemT, box);
                Push(kind, ct, expr);
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "TryTake"):
            {
                var elemT = sig.ParameterTypes[0].Element!; // out T -> T
                string ms;
                if (sig.ParameterTypes.Length == 1)
                    ms = "0"; // single immediate attempt
                else if (sig.ParameterTypes.Length == 2
                    && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 })
                    ms = $"(int32_t)({Pop().Expr})";
                else
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: only TryTake(out T) and " +
                        "TryTake(out T, int millisecondsTimeout) are supported (the TimeSpan / " +
                        "CancellationToken overloads are carve-outs)");
                var itemRef = Pop(); // out T (address)
                var coll = Pop();    // this
                string box = NewTemp("Dn2CppObject*");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_blockingcoll_trytake((Dn2CppObject*)({coll.Expr}), {ms}, &{box});");
                string ct = CppTypes.Of(elemT);
                if (CppTypes.KindOf(elemT) == StackKind.Ref)
                {
                    // On failure `box` is null, which is exactly default(T) for a reference.
                    Emit($"*(({ct}*)({itemRef.Expr})) = ({ct})({box});");
                }
                else if (CppTypes.KindOf(elemT) == StackKind.Struct)
                {
                    // A struct T needs a shape of its own for this `out` store: the scalar
                    // arm below cannot express it — `(T)0` is not a cast a struct has, and
                    // a struct's storage width IS its Of width, so the narrowing that arm
                    // guards against (a ref into a packed field/array slot) cannot arise
                    // for one. Value-initialize for default(T) on the !ok path, then copy
                    // the unboxed payload in on success.
                    string tiS = TypeInfoExpr(elemT)
                        ?? throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: BlockingCollection<{elemT}> element has no boxable type-info");
                    Emit($"*(({ct}*)({itemRef.Expr})) = {ct}{{}};"); // default(T) on the !ok path
                    Emit($"if ({ok}) *(({ct}*)({itemRef.Expr})) = *(({ct}*)dn2cpp_unbox({box}, {tiS}));");
                }
                else
                {
                    string ti = TypeInfoExpr(elemT)
                        ?? throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: BlockingCollection<{elemT}> element has no boxable type-info");
                    // Store through the `out T` ref at T's REAL storage width (the ref
                    // can target a narrowed struct field / packed array slot, where an
                    // Of-width store clobbers the neighbors); the box payload itself is
                    // Of-width (the box convention), so unbox reads stay at ct.
                    string st = CppTypes.StorageOf(elemT);
                    Emit($"*(({st}*)({itemRef.Expr})) = ({st})0;"); // default(T) on the !ok path
                    Emit($"if ({ok}) *(({st}*)({itemRef.Expr})) = ({st})(*(({ct}*)dn2cpp_unbox({box}, {ti})));");
                }
                if (elemT.ContainsGcReferences())
                    Emit($"if ({ok}) dn2cpp_gc_write_barrier_if_heap((void*)({itemRef.Expr}));");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "CompleteAdding"):
            {
                var coll = Pop(); // this
                Emit($"dn2cpp_blockingcoll_complete_adding((Dn2CppObject*)({coll.Expr}));");
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "get_Count"):
            {
                var coll = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_blockingcoll_count((Dn2CppObject*)({coll.Expr}))");
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "get_IsAddingCompleted"):
            {
                var coll = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_blockingcoll_is_adding_completed((Dn2CppObject*)({coll.Expr}))");
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "get_IsCompleted"):
            {
                var coll = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_blockingcoll_is_completed((Dn2CppObject*)({coll.Expr}))");
                return true;
            }
            case ("System.Collections.Concurrent.BlockingCollection", "get_BoundedCapacity"):
            {
                var coll = Pop(); // this
                Push(StackKind.I4, "int32_t", $"dn2cpp_blockingcoll_bounded_capacity((Dn2CppObject*)({coll.Expr}))");
                return true;
            }
            // Dispose is a no-op: the queue lives for the program (like the other threading
            // primitives' Dispose); there is no per-instance teardown.
            case ("System.Collections.Concurrent.BlockingCollection", "Dispose"):
            {
                Pop(); // this
                return true;
            }
            // ---- System.Threading.Tasks.Parallel (data-parallel loops) ----
            // For(from, to, [ParallelOptions,] Action<int|long>[, ParallelLoopState]): the
            // index-only forms (3 or 4 args). The body's generic arg count picks the plain
            // int/long thunk (1 arg) or the Break/Stop-aware "_state" thunk (2 args, second
            // arg ParallelLoopState); either way the runtime fans the range out across up to
            // MaxDegreeOfParallelism OS threads and join-barriers. The plain thunk always
            // returns the completed ParallelLoopResult; the "_state" thunk returns one
            // reflecting any Break()/Stop() calls. The thread-local-state overloads (a 3rd
            // generic arg) are a carve-out — an unrecognized arg shape lands on the loud
            // default NotSupported.
            case ("System.Threading.Tasks.Parallel", "For") when sig.ParameterTypes.Length == 3:
            {
                if (sig.ParameterTypes[2] is not { Kind: TypeKind.Class, Class: { } bodyDel })
                    throw new NotSupportedException(ParallelForBodyShapeError());
                var body = Pop();
                var to = Pop();
                var from = Pop();
                bool isLong = sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 };
                EmitParallelFor(bodyDel, isLong, from, to, body, "-1");
                return true;
            }
            case ("System.Threading.Tasks.Parallel", "For")
                when sig.ParameterTypes is [_, _, { } optsT, _] && IsParallelOptionsType(optsT):
            {
                if (sig.ParameterTypes[3] is not { Kind: TypeKind.Class, Class: { } bodyDel4 })
                    throw new NotSupportedException(ParallelForBodyShapeError());
                var body = Pop();
                var options = Pop();
                var to = Pop();
                var from = Pop();
                bool isLong = sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 };
                EmitParallelFor(bodyDel4, isLong, from, to, body, ParallelOptionsMaxDop(options));
                return true;
            }
            // Invoke([ParallelOptions,] params Action[]): run each Action in parallel,
            // join-barrier. The params array is a single Action[] operand (Roslyn
            // allocates it).
            case ("System.Threading.Tasks.Parallel", "Invoke")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray }]:
            {
                var actions = Pop();
                Emit($"dn2cpp_parallel_invoke({Cast(actions, "Dn2CppArrayRef*")}, -1);");
                return true;
            }
            case ("System.Threading.Tasks.Parallel", "Invoke")
                when sig.ParameterTypes is [{ } optsT2, { Kind: TypeKind.SZArray }]
                    && IsParallelOptionsType(optsT2):
            {
                var actions = Pop();
                var options = Pop();
                Emit($"dn2cpp_parallel_invoke({Cast(actions, "Dn2CppArrayRef*")}, " +
                     $"{ParallelOptionsMaxDop(options)});");
                return true;
            }
            // ParallelLoopResult.IsCompleted: false once any iteration called Break() or
            // Stop() (a thrown body exception never reaches here — Parallel.For/ForEach
            // itself throws AggregateException instead of returning a result, matching
            // real .NET). LowestBreakIteration (long?) mirrors the same struct, HasValue
            // only once Break() was ever called.
            case ("System.Threading.Tasks.ParallelLoopResult", "get_IsCompleted"):
                Push(StackKind.I4, "int32_t",
                    $"(int32_t)(({DerefReceiver("Dn2CppParallelLoopResult")}).isCompleted)");
                return true;
            case ("System.Threading.Tasks.ParallelLoopResult", "get_LowestBreakIteration"):
            {
                string r = NewTemp("Dn2CppParallelLoopResult");
                Emit($"{r} = {DerefReceiver("Dn2CppParallelLoopResult")};");
                PushNullableInt64(sig.ReturnType, $"{r}.hasLowestBreak", $"{r}.lowestBreakIteration");
                return true;
            }

            // ---- System.Threading.Tasks.ParallelLoopState ----
            // Passed by the runtime into the Action<T, ParallelLoopState> body overloads
            // (no public constructor, never newobj'd by user code). Break()/Stop() and the
            // read-only properties all dispatch through dn2cpp_parallel_loop_state_*
            // runtime helpers over the shared Break/Stop/lowestBreak state every
            // iteration's ParallelLoopState points at.
            case ("System.Threading.Tasks.ParallelLoopState", "Break"):
            {
                var recv = Pop(); // this
                Emit($"dn2cpp_parallel_loop_state_break((Dn2CppObject*)({recv.Expr}));");
                return true;
            }
            case ("System.Threading.Tasks.ParallelLoopState", "Stop"):
            {
                var recv = Pop(); // this
                Emit($"dn2cpp_parallel_loop_state_stop((Dn2CppObject*)({recv.Expr}));");
                return true;
            }
            case ("System.Threading.Tasks.ParallelLoopState", "get_ShouldExitCurrentIteration"):
            {
                var recv = Pop(); // this
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_parallel_loop_state_should_exit_current_iteration((Dn2CppObject*)({recv.Expr}))");
                return true;
            }
            case ("System.Threading.Tasks.ParallelLoopState", "get_IsStopped"):
            {
                var recv = Pop(); // this
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_parallel_loop_state_is_stopped((Dn2CppObject*)({recv.Expr}))");
                return true;
            }
            case ("System.Threading.Tasks.ParallelLoopState", "get_IsExceptional"):
            {
                var recv = Pop(); // this
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_parallel_loop_state_is_exceptional((Dn2CppObject*)({recv.Expr}))");
                return true;
            }
            case ("System.Threading.Tasks.ParallelLoopState", "get_LowestBreakIteration"):
            {
                var recv = Pop(); // this
                string castRecv = $"(Dn2CppObject*)({recv.Expr})";
                PushNullableInt64(sig.ReturnType,
                    $"dn2cpp_parallel_loop_state_lowest_break_has_value({castRecv})",
                    $"dn2cpp_parallel_loop_state_lowest_break_value({castRecv})");
                return true;
            }

            // ---- System.Threading.Tasks.ParallelOptions ----
            // Only MaxDegreeOfParallelism is modeled (a plain int field on the runtime
            // object, set up before the Parallel.* call). CancellationToken and
            // TaskScheduler are a carve-out, raised loudly rather than silently ignored —
            // a program relying on either would otherwise diverge from real .NET with no
            // diagnostic.
            case ("System.Threading.Tasks.ParallelOptions", "get_MaxDegreeOfParallelism"):
            {
                var recv = Pop(); // this
                Push(StackKind.I4, "int32_t", $"(((Dn2CppParallelOptions*)({recv.Expr}))->maxDop)");
                return true;
            }
            case ("System.Threading.Tasks.ParallelOptions", "set_MaxDegreeOfParallelism"):
            {
                var val = Pop();
                var recv = Pop(); // this
                Emit($"if (({val.Expr}) == 0 || ({val.Expr}) < -1) dn2cpp_throw_argument_out_of_range();");
                Emit($"((Dn2CppParallelOptions*)({recv.Expr}))->maxDop = ({val.Expr});");
                return true;
            }
            case ("System.Threading.Tasks.ParallelOptions",
                 "get_CancellationToken" or "set_CancellationToken"
                 or "get_TaskScheduler" or "set_TaskScheduler"):
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: ParallelOptions.{name[4..]} " +
                    "is a carve-out (only MaxDegreeOfParallelism is supported)");

            default:
                return false;
        }
    }

    /// <summary>Whether a decoded parameter/operand type is ParallelOptions. It lives in
    /// the System.Threading.Tasks.Parallel.dll facade (not CoreLib), so depending on which
    /// assemblies are referenced it resolves either as a loaded Class or — when only
    /// CoreLib is loaded, like the corelib_diff_gate gates — as an unresolved External
    /// TypeRef (same split as ParallelLoopResult in CppTypes.cs); accept either.</summary>
    private static bool IsParallelOptionsType(TypeDesc t) => t switch
    {
        { Kind: TypeKind.Class, Class.FullName: "System.Threading.Tasks.ParallelOptions" } => true,
        { Kind: TypeKind.External, ExternalName: "System.Threading.Tasks.ParallelOptions" } => true,
        _ => false,
    };

    /// <summary>The maxDop argument expression for a Parallel.* runtime call from a
    /// popped ParallelOptions stack entry (a null reference behaves like the default
    /// ParallelOptions — unlimited).</summary>
    private static string ParallelOptionsMaxDop(StackEntry options) =>
        $"(({options.Expr}) != nullptr ? ((Dn2CppParallelOptions*)({options.Expr}))->maxDop : -1)";

    /// <summary>Whether a decoded parameter/operand type is ParallelLoopState — same
    /// Class-vs-External split as ParallelOptions/ParallelLoopResult (the facade
    /// assembly is absent under a CoreLib-only reference set like corelib_diff_gate).</summary>
    private static bool IsParallelLoopStateType(TypeDesc t) => t switch
    {
        { Kind: TypeKind.Class, Class.FullName: "System.Threading.Tasks.ParallelLoopState" } => true,
        { Kind: TypeKind.External, ExternalName: "System.Threading.Tasks.ParallelLoopState" } => true,
        _ => false,
    };

    private static string ParallelForBodyShapeError() =>
        "only Parallel.For(from, to, [ParallelOptions, ] Action<int|long>) and " +
        "Parallel.For(from, to, [ParallelOptions, ] Action<int|long, ParallelLoopState>) are " +
        "supported (the thread-local-state overloads are a carve-out)";

    private static string ParallelForEachBodyShapeError() =>
        "only Parallel.ForEach<T>(T[] source, [ParallelOptions, ] Action<T>) and " +
        "Parallel.ForEach<T>(T[] source, [ParallelOptions, ] Action<T, ParallelLoopState>) are " +
        "supported (the thread-local-state overloads are a carve-out)";

    /// <summary>Emits one Parallel.For(from, to, ..., body) call site — the plain
    /// Action&lt;int|long&gt; body (1 generic arg) or the Break/Stop-aware
    /// Action&lt;int|long, ParallelLoopState&gt; body (2 generic args, the "_state"
    /// runtime thunk) — and pushes its ParallelLoopResult. Shared by the 3-arg and
    /// 4-arg (ParallelOptions) call sites, which differ only in the maxDop expression
    /// and which operands they already popped.</summary>
    private void EmitParallelFor(ClassInfo bodyDel, bool isLong, StackEntry from, StackEntry to, StackEntry body, string maxDop)
    {
        string width = isLong ? "8" : "4";
        switch (bodyDel.Context.TypeArgs.Length)
        {
            case 1:
                Emit($"dn2cpp_parallel_for_i{width}({from.Expr}, {to.Expr}, (Dn2CppObject*)({body.Expr}), {maxDop});");
                Push(StackKind.Struct, "Dn2CppParallelLoopResult", "Dn2CppParallelLoopResult{ 1, 0, -1 }");
                return;
            case 2 when IsParallelLoopStateType(bodyDel.Context.TypeArgs[1]):
                string res = NewTemp("Dn2CppParallelLoopResult");
                Emit($"{res} = dn2cpp_parallel_for_i{width}_state({from.Expr}, {to.Expr}, (Dn2CppObject*)({body.Expr}), {maxDop});");
                Push(StackKind.Struct, "Dn2CppParallelLoopResult", res);
                return;
            default:
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {ParallelForBodyShapeError()}");
        }
    }

    /// <summary>Builds a Nullable&lt;Int64&gt; (long?) value from a has-value + value C++
    /// expression pair and pushes it — used by ParallelLoopResult.LowestBreakIteration and
    /// ParallelLoopState.LowestBreakIteration, which surface the same optional-break-index
    /// shape from different backing storage. <paramref name="nullableType"/> is the call's
    /// decoded return type (a closed Nullable&lt;Int64&gt;), which gives the real
    /// hasValue/value field names of that instantiation.</summary>
    private void PushNullableInt64(TypeDesc nullableType, string hasValueExpr, string valueExpr)
    {
        if (NullableLayout(nullableType) is not (_, { } hvField, { } valField))
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: expected a Nullable<Int64> return type");
        string ct = CppTypes.Of(nullableType);
        string tmp = NewTemp(ct);
        Emit($"{tmp}.{hvField} = ({hasValueExpr});");
        Emit($"{tmp}.{valField} = ({valueExpr});");
        Push(StackKind.Struct, ct, tmp);
    }
}
