using System.Globalization;
using System.Reflection.Emit;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using EmitOpCodes = System.Reflection.Emit.OpCodes;

internal static class CancellationInvokeFixture
{
    static Action<Delegate?, object?, CancellationTokenSource?>? oracleDirect;
    static Action<Delegate?, object?, CancellationTokenSource?>? oracleAddress;
    static Action<Delegate?, object?, CancellationTokenSource?>? oracleCalli;
    static Func<CancellationTokenSource?, CancellationToken>? oracleSourceNew;
    static Func<CancellationTokenSource?, CancellationToken>? oracleSourceCall;
    static Func<bool, CancellationToken>? oracleBoolNew;
    static Func<bool, CancellationToken>? oracleBoolCall;
    static Action<Action<object?>, object?>? oracleNonVirtualAction;

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--cancellation-invoke-oracle")
        {
            Oracle();
            return true;
        }
        if (args.Length == 1 && args[0] == "--cancellation-cold-first-use-oracle")
        {
            Oracle(true);
            return true;
        }
        if (args.Length != 3 || args[0] != "--cancellation-invoke-fixture") return false;
        Build(args[1], args[2]);
        return true;
    }

    static Action<Delegate?, object?, CancellationTokenSource?> HelperOracle(string route)
    {
        var parameters = new[] { typeof(Delegate), typeof(object), typeof(CancellationTokenSource) };
        var target = typeof(CancellationTokenSource).GetMethod("Invoke",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, parameters, null)!;
        // The CLR oracle executes the untouched private CoreLib body.
        var dynamic = new DynamicMethod("CancellationInvoke_" + route, typeof(void), parameters,
            typeof(CancellationInvokeFixture).Module, true);
        var il = dynamic.GetILGenerator();
        il.Emit(EmitOpCodes.Ldarg_0); il.Emit(EmitOpCodes.Ldarg_1); il.Emit(EmitOpCodes.Ldarg_2);
        if (route == "direct") il.Emit(EmitOpCodes.Call, target);
        else
        {
            il.Emit(EmitOpCodes.Ldftn, target);
            if (route == "address")
            {
                var pointer = il.DeclareLocal(typeof(IntPtr));
                il.Emit(EmitOpCodes.Stloc, pointer); il.Emit(EmitOpCodes.Ldloc, pointer);
            }
            il.EmitCalli(EmitOpCodes.Calli, System.Reflection.CallingConventions.Standard,
                typeof(void), parameters, null);
        }
        il.Emit(EmitOpCodes.Ret);
        return (Action<Delegate?, object?, CancellationTokenSource?>)dynamic.CreateDelegate(
            typeof(Action<Delegate?, object?, CancellationTokenSource?>));
    }

    static Func<T, CancellationToken> ConstructorOracle<T>(bool address)
    {
        var target = typeof(CancellationToken).GetConstructor(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null, new[] { typeof(T) }, null)!;
        var dynamic = new DynamicMethod("CancellationToken_" + typeof(T).Name + (address ? "_call" : "_newobj"),
            typeof(CancellationToken), new[] { typeof(T) }, typeof(CancellationInvokeFixture).Module, true);
        var il = dynamic.GetILGenerator();
        if (address)
        {
            var token = il.DeclareLocal(typeof(CancellationToken));
            il.Emit(EmitOpCodes.Ldloca, token); il.Emit(EmitOpCodes.Ldarg_0);
            il.Emit(EmitOpCodes.Call, target); il.Emit(EmitOpCodes.Ldloc, token);
        }
        else
        {
            il.Emit(EmitOpCodes.Ldarg_0); il.Emit(EmitOpCodes.Newobj, target);
        }
        il.Emit(EmitOpCodes.Ret);
        return (Func<T, CancellationToken>)dynamic.CreateDelegate(typeof(Func<T, CancellationToken>));
    }

    static Action<Action<object?>, object?> NonVirtualActionOracle()
    {
        var target = typeof(Action<object>).GetMethod(nameof(Action<object>.Invoke))!;
        var dynamic = new DynamicMethod("ActionInvoke_nonvirtual", typeof(void),
            new[] { typeof(Action<object>), typeof(object) }, typeof(CancellationInvokeFixture).Module, true);
        var il = dynamic.GetILGenerator();
        il.Emit(EmitOpCodes.Ldarg_0); il.Emit(EmitOpCodes.Ldarg_1);
        il.Emit(EmitOpCodes.Call, target); il.Emit(EmitOpCodes.Ret);
        return (Action<Action<object?>, object?>)dynamic.CreateDelegate(typeof(Action<Action<object?>, object?>));
    }

    static void Oracle(bool coldFirstUse = false)
    {
        oracleDirect = HelperOracle("direct");
        oracleAddress = HelperOracle("address");
        oracleCalli = HelperOracle("calli");
        oracleSourceNew = ConstructorOracle<CancellationTokenSource?>(false);
        oracleSourceCall = ConstructorOracle<CancellationTokenSource?>(true);
        oracleBoolNew = ConstructorOracle<bool>(false);
        oracleBoolCall = ConstructorOracle<bool>(true);
        oracleNonVirtualAction = NonVirtualActionOracle();
        if (coldFirstUse) Witness.RunColdFirstUse();
        else Witness.RunCases();
    }

    // This ordinary IL is shared by the CLR oracle and the native fixture.
    // Only the eight target stubs are replaced when creating the app.
    static class Witness
    {
        static int calls;
        static int order;
        static object? seenState;
        static CancellationToken seenToken;
        static Exception? marker;
        static CancellationToken threadExpected;
        static int threadReady;
        static int threadStart;
        static int[]? threadFlags;
        static CancellationToken[]? coldTokens;
        static int[]? coldFlags;
        static int coldReady;
        static int coldStart;

        public static void RunEntry(string[] args)
        {
            if (args.Length == 0) RunCases();
            else if (args.Length == 1 && args[0] == "cold-first-use") RunColdFirstUse();
            else throw new InvalidOperationException("Unexpected cancellation witness mode");
        }

        public static void RunCases()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            ActionState();
            ActionNullState();
            TokenNullSource();
            TokenFresh();
            TokenCanceled();
            TokenAfterCancel();
            TokenDistinct();
            NullDelegate();
            WrongDelegate();
            ActionThrow();
            TokenThrow();
            ActionMulticast();
            TokenMulticastThrow();
            Constructors(false);
            Constructors(true);
            OrdinaryNullTokenArguments();
            OrdinaryNullActionThrowingArgument();
            NonVirtualAction();
            CanonicalAfterGc();
            CanonicalJoinedThreads();
        }

        static void InvokeDirect(Delegate? callback, object? state, CancellationTokenSource? source) => oracleDirect!(callback, state, source);
        static void InvokeAddress(Delegate? callback, object? state, CancellationTokenSource? source) => oracleAddress!(callback, state, source);
        static void InvokeCalli(Delegate? callback, object? state, CancellationTokenSource? source) => oracleCalli!(callback, state, source);
        static CancellationToken SourceNew(CancellationTokenSource? source) => oracleSourceNew!(source);
        static CancellationToken SourceCall(CancellationTokenSource? source) => oracleSourceCall!(source);
        static CancellationToken BoolNew(bool canceled) => oracleBoolNew!(canceled);
        static CancellationToken BoolCall(bool canceled) => oracleBoolCall!(canceled);
        static void InvokeNonVirtualAction(Action<object?> callback, object? state) => oracleNonVirtualAction!(callback, state);

        static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }

        static void Pass(string label) => Console.WriteLine("cancellation-invoke:" + label + ":pass");

        static void Reset()
        {
            calls = 0;
            order = 0;
            seenState = null;
            seenToken = default;
            marker = null;
        }

        static void RecordActionOne(object? state)
        {
            calls++;
            order = order * 10 + 1;
            seenState = state;
        }

        static void RecordActionTwo(object? state)
        {
            calls++;
            order = order * 10 + 2;
            seenState = state;
        }

        static void RecordTokenOne(object? state, CancellationToken token)
        {
            RecordActionOne(state);
            seenToken = token;
        }

        static void ThrowActionOne(object? state)
        {
            RecordActionOne(state);
            throw marker!;
        }

        static void ThrowTokenOne(object? state, CancellationToken token)
        {
            RecordTokenOne(state, token);
            throw marker!;
        }

        static void ThrowTokenTwo(object? state, CancellationToken token)
        {
            RecordActionTwo(state);
            seenToken = token;
            throw marker!;
        }

        static void RecordTokenThree(object? state, CancellationToken token)
        {
            calls++;
            order = order * 10 + 3;
            seenState = state;
            seenToken = token;
        }

        static void WrongShape()
        {
            calls++;
        }

        static void CheckEffects(object? state, int count, int sequence, string label)
        {
            Check(calls == count && order == sequence && ReferenceEquals(seenState, state), label);
        }

        static void CheckToken(CancellationToken token, bool canCancel, bool canceled, CancellationToken expected, string label)
        {
            Check(token.CanBeCanceled == canCancel && token.IsCancellationRequested == canceled && token.Equals(expected), label);
        }

        static void ActionState()
        {
            const string label = "action:direct:state";
            Reset();
            var state = new object();
            InvokeDirect(new Action<object?>(RecordActionOne), state, new CancellationTokenSource());
            CheckEffects(state, 1, 1, label);
            Pass(label);
        }

        static void ActionNullState()
        {
            const string label = "action:address:null-state";
            Reset();
            InvokeAddress(new Action<object?>(RecordActionOne), null, null);
            CheckEffects(null, 1, 1, label);
            Pass(label);
        }

        static void TokenNullSource()
        {
            const string label = "token:calli:null-source";
            Reset();
            InvokeCalli(new Action<object?, CancellationToken>(RecordTokenOne), null, null);
            CheckEffects(null, 1, 1, label);
            CheckToken(seenToken, false, false, default, label);
            Pass(label);
        }

        static void TokenFresh()
        {
            const string label = "token:direct:fresh-source";
            Reset();
            var state = new object();
            var source = new CancellationTokenSource();
            InvokeDirect(new Action<object?, CancellationToken>(RecordTokenOne), state, source);
            CheckEffects(state, 1, 1, label);
            CheckToken(seenToken, true, false, source.Token, label);
            Pass(label);
        }

        static void TokenCanceled()
        {
            const string label = "token:address:canceled-source";
            Reset();
            var source = new CancellationTokenSource();
            source.Cancel();
            InvokeAddress(new Action<object?, CancellationToken>(RecordTokenOne), null, source);
            CheckEffects(null, 1, 1, label);
            CheckToken(seenToken, true, true, source.Token, label);
            Check(!seenToken.Equals(BoolNew(true)), label);
            Pass(label);
        }

        static void TokenAfterCancel()
        {
            const string label = "token:calli:after-cancel";
            Reset();
            var source = new CancellationTokenSource();
            InvokeCalli(new Action<object?, CancellationToken>(RecordTokenOne), null, source);
            CheckToken(seenToken, true, false, source.Token, label);
            source.Cancel();
            CheckEffects(null, 1, 1, label);
            CheckToken(seenToken, true, true, source.Token, label);
            Pass(label);
        }

        static void TokenDistinct()
        {
            const string label = "token:direct:distinct-sources";
            Reset();
            var first = new CancellationTokenSource();
            var second = new CancellationTokenSource();
            InvokeDirect(new Action<object?, CancellationToken>(RecordTokenOne), first, first);
            var firstToken = seenToken;
            InvokeDirect(new Action<object?, CancellationToken>(RecordTokenOne), second, second);
            CheckEffects(second, 2, 11, label);
            CheckToken(firstToken, true, false, first.Token, label);
            CheckToken(seenToken, true, false, second.Token, label);
            Check(!firstToken.Equals(seenToken), label);
            first.Cancel();
            Check(firstToken.IsCancellationRequested && !seenToken.IsCancellationRequested, label);
            Pass(label);
        }

        static void NullDelegate()
        {
            const string label = "delegate:direct:null";
            Reset();
            bool caught = false;
            try { InvokeDirect(null, null, new CancellationTokenSource()); }
            catch (NullReferenceException) { caught = true; }
            Check(caught, label);
            CheckEffects(null, 0, 0, label);
            Pass(label);
        }

        static void WrongDelegate()
        {
            const string label = "delegate:calli:wrong-shape";
            Reset();
            bool caught = false;
            try { InvokeCalli(new Action(WrongShape), null, null); }
            catch (InvalidCastException) { caught = true; }
            Check(caught, label);
            CheckEffects(null, 0, 0, label);
            Pass(label);
        }

        static void ActionThrow()
        {
            const string label = "action:address:throw-identity";
            Reset();
            var state = new object();
            marker = new InvalidOperationException("action marker");
            Exception? caught = null;
            try { InvokeAddress(new Action<object?>(ThrowActionOne), state, null); }
            catch (Exception error) { caught = error; }
            Check(ReferenceEquals(caught, marker), label);
            CheckEffects(state, 1, 1, label);
            Pass(label);
        }

        static void TokenThrow()
        {
            const string label = "token:direct:throw-identity";
            Reset();
            var source = new CancellationTokenSource();
            var state = new object();
            marker = new InvalidOperationException("token marker");
            Exception? caught = null;
            try { InvokeDirect(new Action<object?, CancellationToken>(ThrowTokenOne), state, source); }
            catch (Exception error) { caught = error; }
            Check(ReferenceEquals(caught, marker), label);
            CheckEffects(state, 1, 1, label);
            CheckToken(seenToken, true, false, source.Token, label);
            Pass(label);
        }

        static void ActionMulticast()
        {
            const string label = "action:direct:multicast-order";
            Reset();
            var state = new object();
            var callbacks = Delegate.Combine(new Action<object?>(RecordActionOne), new Action<object?>(RecordActionTwo));
            InvokeDirect(callbacks, state, null);
            CheckEffects(state, 2, 12, label);
            Pass(label);
        }

        static void TokenMulticastThrow()
        {
            const string label = "token:address:multicast-stop";
            Reset();
            var source = new CancellationTokenSource();
            var state = new object();
            marker = new InvalidOperationException("multicast marker");
            var firstTwo = Delegate.Combine(new Action<object?, CancellationToken>(RecordTokenOne), new Action<object?, CancellationToken>(ThrowTokenTwo));
            var callbacks = Delegate.Combine(firstTwo, new Action<object?, CancellationToken>(RecordTokenThree));
            Exception? caught = null;
            try { InvokeAddress(callbacks, state, source); }
            catch (Exception error) { caught = error; }
            Check(ReferenceEquals(caught, marker), label);
            CheckEffects(state, 2, 12, label);
            CheckToken(seenToken, true, false, source.Token, label);
            Pass(label);
        }

        static CancellationToken ConstructSource(CancellationTokenSource? source, bool address) => address ? SourceCall(source) : SourceNew(source);
        static CancellationToken ConstructBool(bool canceled, bool address) => address ? BoolCall(canceled) : BoolNew(canceled);

        static void Constructors(bool address)
        {
            string prefix = "ctor:" + (address ? "address-call:" : "newobj:");
            string label = prefix + "source-null";
            CheckToken(ConstructSource(null, address), false, false, default, label);
            Pass(label);

            label = prefix + "source-fresh";
            var first = new CancellationTokenSource();
            var firstToken = ConstructSource(first, address);
            CheckToken(firstToken, true, false, first.Token, label);
            Pass(label);

            label = prefix + "source-distinct";
            var second = new CancellationTokenSource();
            var secondToken = ConstructSource(second, address);
            CheckToken(secondToken, true, false, second.Token, label);
            Check(!firstToken.Equals(secondToken), label);
            Pass(label);

            label = prefix + "source-after-cancel";
            first.Cancel();
            CheckToken(firstToken, true, true, first.Token, label);
            CheckToken(secondToken, true, false, second.Token, label);
            Pass(label);

            label = prefix + "source-canceled";
            var canceled = new CancellationTokenSource();
            canceled.Cancel();
            var canceledToken = ConstructSource(canceled, address);
            CheckToken(canceledToken, true, true, canceled.Token, label);
            Check(!canceledToken.Equals(ConstructBool(true, address)), label);
            Pass(label);

            label = prefix + "bool-false";
            CheckToken(ConstructBool(false, address), false, false, default, label);
            Pass(label);

            label = prefix + "bool-true";
            var trueToken = ConstructBool(true, address);
            Check(trueToken.CanBeCanceled && trueToken.IsCancellationRequested && !trueToken.Equals(default(CancellationToken)), label);
            Check(trueToken.Equals(ConstructBool(true, address)), label);
            Pass(label);
        }

        static object? StateArgument(object? state)
        {
            RecordActionOne(state);
            return state;
        }

        static CancellationToken TokenArgument(CancellationToken token)
        {
            calls++;
            order = order * 10 + 2;
            seenToken = token;
            return token;
        }

        static object? ThrowingArgument(object? state)
        {
            RecordActionOne(state);
            throw marker!;
        }

        static void OrdinaryNullTokenArguments()
        {
            const string label = "delegate:ordinary:null-arguments-order";
            Reset();
            var state = new object();
            var source = new CancellationTokenSource();
            Action<object?, CancellationToken>? callback = null;
            bool caught = false;
            try { callback!(StateArgument(state), TokenArgument(source.Token)); }
            catch (NullReferenceException) { caught = true; }
            Check(caught, label);
            CheckEffects(state, 2, 12, label);
            CheckToken(seenToken, true, false, source.Token, label);
            Pass(label);
        }

        static void OrdinaryNullActionThrowingArgument()
        {
            const string label = "delegate:ordinary:argument-throw-before-null";
            Reset();
            var state = new object();
            marker = new InvalidOperationException("argument marker");
            Action<object?>? callback = null;
            Exception? caught = null;
            try { callback!(ThrowingArgument(state)); }
            catch (Exception error) { caught = error; }
            Check(ReferenceEquals(caught, marker), label);
            CheckEffects(state, 1, 1, label);
            Pass(label);
        }

        static void NonVirtualAction()
        {
            const string label = "delegate:nonvirtual:nonnull-action";
            Reset();
            var state = new object();
            InvokeNonVirtualAction(new Action<object?>(RecordActionOne), state);
            CheckEffects(state, 1, 1, label);
            Pass(label);
        }

        static void ColdFirstUseWorker(object? argument)
        {
            int index = (int)argument!;
            int failed = 0;
            Interlocked.Increment(ref coldReady);
            while (Volatile.Read(ref coldStart) == 0) Thread.Yield();
            try
            {
                // Neither constructor has produced a true token before this barrier.
                var first = (index & 1) == 0 ? BoolNew(true) : BoolCall(true);
                coldTokens![index] = first;
                if (!first.CanBeCanceled || !first.IsCancellationRequested) failed = 1;
                for (int iteration = 0; iteration < 32; iteration++)
                {
                    var fromNew = BoolNew(true);
                    var fromCall = BoolCall(true);
                    if (!fromNew.CanBeCanceled || !fromNew.IsCancellationRequested || !fromNew.Equals(first)
                        || !fromCall.CanBeCanceled || !fromCall.IsCancellationRequested || !fromCall.Equals(first))
                        failed = 1;
                    Thread.Yield();
                }
            }
            catch (Exception)
            {
                failed = 1;
            }
            coldFlags![index] = failed;
        }

        public static void RunColdFirstUse()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            const string label = "ctor:bool-true:cold-first-use";
            coldReady = 0;
            coldStart = 0;
            coldTokens = new CancellationToken[4];
            coldFlags = new int[4];
            var workers = new Thread[4];
            for (int index = 0; index < 4; index++)
            {
                workers[index] = new Thread(new ParameterizedThreadStart(ColdFirstUseWorker));
                workers[index].Start(index);
            }
            while (Volatile.Read(ref coldReady) != 4) Thread.Yield();
            Volatile.Write(ref coldStart, 1);
            for (int index = 0; index < 4; index++) workers[index].Join();
            var expected = coldTokens[0];
            for (int index = 0; index < 4; index++)
            {
                Check(coldFlags[index] == 0, label);
                CheckToken(coldTokens[index], true, true, expected, label);
            }
            CheckToken(BoolNew(true), true, true, expected, label);
            CheckToken(BoolCall(true), true, true, expected, label);
            CheckToken(BoolNew(false), false, false, default, label);
            CheckToken(BoolCall(false), false, false, default, label);
            var fresh = new CancellationTokenSource();
            CheckToken(SourceNew(fresh), true, false, fresh.Token, label);
            CheckToken(SourceCall(fresh), true, false, fresh.Token, label);
            Check(!fresh.Token.Equals(expected) && !fresh.IsCancellationRequested, label);
            Pass(label);
        }

        static void CanonicalAfterGc()
        {
            const string label = "ctor:bool-true:canonical-after-gc";
            var expected = BoolNew(true);
            var fresh = new CancellationTokenSource();
            var freshToken = fresh.Token;
            CheckToken(expected, true, true, BoolCall(true), label);
            CheckToken(freshToken, true, false, fresh.Token, label);
            Check(!freshToken.Equals(expected), label);
            GC.Collect();
            CheckToken(BoolNew(true), true, true, expected, label);
            CheckToken(BoolCall(true), true, true, expected, label);
            CheckToken(freshToken, true, false, fresh.Token, label);
            Check(!freshToken.Equals(expected) && !fresh.IsCancellationRequested, label);
            Pass(label);
        }

        static void CanonicalWorker(object? argument)
        {
            int index = (int)argument!;
            int failed = 0;
            Interlocked.Increment(ref threadReady);
            while (Volatile.Read(ref threadStart) == 0) Thread.Yield();
            try
            {
                var fresh = new CancellationTokenSource();
                var freshToken = fresh.Token;
                for (int iteration = 0; iteration < 32; iteration++)
                {
                    var fromNew = BoolNew(true);
                    Thread.Yield();
                    var fromCall = BoolCall(true);
                    if (!fromNew.CanBeCanceled || !fromNew.IsCancellationRequested || !fromNew.Equals(threadExpected)
                        || !fromCall.CanBeCanceled || !fromCall.IsCancellationRequested || !fromCall.Equals(threadExpected)
                        || !threadExpected.IsCancellationRequested
                        || !freshToken.CanBeCanceled || freshToken.IsCancellationRequested || fresh.IsCancellationRequested
                        || !freshToken.Equals(fresh.Token) || freshToken.Equals(threadExpected))
                        failed = 1;
                    Thread.Yield();
                }
            }
            catch (Exception)
            {
                failed = 1;
            }
            threadFlags![index] = failed;
        }

        static void CanonicalJoinedThreads()
        {
            const string label = "ctor:bool-true:canonical-joined-threads";
            threadExpected = BoolNew(true);
            threadReady = 0;
            threadStart = 0;
            threadFlags = new int[4];
            var fresh = new CancellationTokenSource();
            var freshToken = fresh.Token;
            var workers = new Thread[4];
            for (int index = 0; index < 4; index++)
            {
                workers[index] = new Thread(new ParameterizedThreadStart(CanonicalWorker));
                workers[index].Start(index);
            }
            while (Volatile.Read(ref threadReady) != 4) Thread.Yield();
            Volatile.Write(ref threadStart, 1);
            for (int index = 0; index < 4; index++) workers[index].Join();
            Check(threadFlags[0] == 0 && threadFlags[1] == 0 && threadFlags[2] == 0 && threadFlags[3] == 0, label);
            CheckToken(BoolNew(true), true, true, threadExpected, label);
            CheckToken(BoolCall(true), true, true, threadExpected, label);
            CheckToken(freshToken, true, false, fresh.Token, label);
            Check(!freshToken.Equals(threadExpected) && !fresh.IsCancellationRequested, label);
            Pass(label);
        }
    }

    static void Build(string output, string corePath)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var source = AssemblyDefinition.ReadAssembly(typeof(CancellationInvokeFixture).Assembly.Location);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("CancellationInvoke", new Version(1, 0)), "CancellationInvoke", ModuleKind.Console);
        var module = app.MainModule;
        var original = source.MainModule.GetType(typeof(Witness).FullName!.Replace('+', '/'));
        var host = new TypeDefinition("Fixture", "CancellationInvokeProgram", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var fields = new Dictionary<string, FieldDefinition>();
        foreach (var field in original.Fields)
        {
            var copy = new FieldDefinition(field.Name, field.Attributes, module.ImportReference(field.FieldType));
            host.Fields.Add(copy); fields.Add(field.Name, copy);
        }
        var methods = new Dictionary<string, MethodDefinition>();
        foreach (var method in original.Methods)
        {
            var copy = new MethodDefinition(method.Name, method.Attributes, module.ImportReference(method.ReturnType));
            foreach (var parameter in method.Parameters)
                copy.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, module.ImportReference(parameter.ParameterType)));
            host.Methods.Add(copy); methods.Add(method.Name, copy);
        }
        var stubs = new HashSet<string> { "InvokeDirect", "InvokeAddress", "InvokeCalli", "SourceNew", "SourceCall", "BoolNew", "BoolCall", "InvokeNonVirtualAction" };
        foreach (var method in original.Methods)
        {
            if (stubs.Contains(method.Name)) continue;
            var copy = methods[method.Name];
            copy.Body.InitLocals = method.Body.InitLocals;
            copy.Body.MaxStackSize = method.Body.MaxStackSize;
            foreach (var local in method.Body.Variables)
                copy.Body.Variables.Add(new VariableDefinition(module.ImportReference(local.VariableType)));
            var instructions = new Dictionary<Instruction, Instruction>();
            foreach (var instruction in method.Body.Instructions)
            {
                var cloned = Instruction.Create(OpCodes.Nop);
                cloned.OpCode = instruction.OpCode;
                copy.Body.Instructions.Add(cloned); instructions.Add(instruction, cloned);
            }
            foreach (var instruction in method.Body.Instructions)
            {
                object? operand = instruction.Operand;
                instructions[instruction].Operand = operand switch
                {
                    Instruction branch => instructions[branch],
                    Instruction[] branches => branches.Select(branch => instructions[branch]).ToArray(),
                    VariableDefinition local => copy.Body.Variables[local.Index],
                    ParameterDefinition parameter => copy.Parameters[parameter.Index],
                    MethodReference target when target.DeclaringType.FullName == original.FullName => methods[target.Name],
                    MethodReference target => module.ImportReference(target),
                    FieldReference field when field.DeclaringType.FullName == original.FullName => fields[field.Name],
                    FieldReference field => module.ImportReference(field),
                    TypeReference type => module.ImportReference(type),
                    null => null,
                    _ when operand is string or sbyte or byte or int or long or float or double => operand,
                    _ => throw new InvalidOperationException("Unexpected witness operand " + operand.GetType().Name)
                };
            }
            foreach (var handler in method.Body.ExceptionHandlers)
            {
                copy.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType)
                {
                    TryStart = instructions[handler.TryStart],
                    TryEnd = handler.TryEnd is null ? null : instructions[handler.TryEnd],
                    HandlerStart = instructions[handler.HandlerStart],
                    HandlerEnd = handler.HandlerEnd is null ? null : instructions[handler.HandlerEnd],
                    CatchType = handler.CatchType is null ? null : module.ImportReference(handler.CatchType),
                    FilterStart = handler.FilterStart is null ? null : instructions[handler.FilterStart]
                });
            }
        }
        var sourceType = core.MainModule.GetType("System.Threading.CancellationTokenSource");
        var tokenType = core.MainModule.GetType("System.Threading.CancellationToken");
        var invoke = module.ImportReference(sourceType.Methods.Single(method => method.Name == "Invoke" && method.IsStatic
            && method.ReturnType.MetadataType == MetadataType.Void && method.Parameters.Count == 3
            && method.Parameters[0].ParameterType.FullName == "System.Delegate"
            && method.Parameters[1].ParameterType.MetadataType == MetadataType.Object
            && method.Parameters[2].ParameterType.FullName == sourceType.FullName));
        var sourceCtor = module.ImportReference(tokenType.Methods.Single(method => method.IsConstructor && !method.IsStatic
            && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == sourceType.FullName));
        var boolCtor = module.ImportReference(tokenType.Methods.Single(method => method.IsConstructor && !method.IsStatic
            && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.MetadataType == MetadataType.Boolean));
        foreach (string name in new[] { "InvokeDirect", "InvokeAddress", "InvokeCalli" })
        {
            var method = methods[name];
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2);
            if (name == "InvokeDirect") il.Emit(OpCodes.Call, invoke);
            else
            {
                il.Emit(OpCodes.Ldftn, invoke);
                if (name == "InvokeAddress")
                {
                    var pointer = new VariableDefinition(module.TypeSystem.IntPtr); method.Body.Variables.Add(pointer);
                    il.Emit(OpCodes.Stloc, pointer); il.Emit(OpCodes.Ldloc, pointer);
                }
                var signature = new CallSite(module.TypeSystem.Void);
                foreach (var parameter in invoke.Parameters) signature.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, signature);
            }
            il.Emit(OpCodes.Ret);
        }
        foreach (string name in new[] { "SourceNew", "SourceCall", "BoolNew", "BoolCall" })
        {
            var method = methods[name];
            var il = method.Body.GetILProcessor();
            var target = name.StartsWith("Source", StringComparison.Ordinal) ? sourceCtor : boolCtor;
            if (name.EndsWith("Call", StringComparison.Ordinal))
            {
                var token = new VariableDefinition(target.DeclaringType); method.Body.Variables.Add(token);
                il.Emit(OpCodes.Ldloca, token); il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Ldloc, token);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Newobj, target);
            }
            il.Emit(OpCodes.Ret);
        }
        var actionDefinition = core.MainModule.GetType("System.Action`1");
        var actionObject = new GenericInstanceType(module.ImportReference(actionDefinition));
        actionObject.GenericArguments.Add(module.TypeSystem.Object);
        var actionInvoke = module.ImportReference(actionDefinition.Methods.Single(method => method.Name == "Invoke"));
        actionInvoke.DeclaringType = actionObject;
        var nonVirtual = methods["InvokeNonVirtualAction"].Body.GetILProcessor();
        nonVirtual.Emit(OpCodes.Ldarg_0); nonVirtual.Emit(OpCodes.Ldarg_1);
        nonVirtual.Emit(OpCodes.Call, actionInvoke); nonVirtual.Emit(OpCodes.Ret);
        // This unused method makes application-level Diet rewriting observable.
        var sentinel = new MethodDefinition("UnusedDietSentinel", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(sentinel); sentinel.Body.GetILProcessor().Emit(OpCodes.Ret);
        app.EntryPoint = methods[nameof(Witness.RunEntry)];
        if (module.AssemblyReferences.Any(reference => reference.Name == source.Name.Name))
            throw new InvalidOperationException("The native witness must not reference its fixture generator");
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }
}
