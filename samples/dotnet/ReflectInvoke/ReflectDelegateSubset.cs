#nullable enable
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ReflectDelegateSubset
{
    // SUBJECT: the reflection -> delegate bridge — MethodInfo.CreateDelegate and
    // the Delegate.CreateDelegate statics across the four binding modes
    // (open/closed x static/instance), variance, value-type receivers and
    // arguments, bind failures, identity and multicast. The created delegates
    // dispatch through a boxed-invoker trampoline, which is invisible here.
    class Greeter
    {
        private readonly string prefix;
        public Greeter(string prefix) => this.prefix = prefix;
        public string Greet(string name) => prefix + name;
        public int Mul(int a, int b) => a * b;
        public static int AddOne(int x) => x + 1;
        public static string Shout(string s, int n) => s + n;
        public void Note(object o) => Console.WriteLine($"note => {prefix}{o}");
        public object Fetch(string s) => prefix + s;
    }

    struct Counter
    {
        public int Value;
        public int Bump() => Value + 1;
    }

    class NameBase
    {
        public NameBase() { }
        private NameBase(int value) { }
        private string Secret(string value) => "private:" + value;
        private static int StaticSecret(int value) => value + 5;
        public virtual string Describe(string value) => "base:" + value;
        public string Overload(string value) => "base-overload:" + value;
        public virtual string SlotCase(string value) => "base-slot:" + value;
    }

    unsafe class NameTarget : NameBase
    {
        static NameTarget() { }
        public override string Describe(string value) => "derived:" + value;
        public string Overload(object value) => "object:" + value;
        public int Overload(int value) => value + 10;
        public string Écho(string value) => "unicode:" + value;
        public string Σend(string value) => "greek:" + value;
        public string Short(string value) => "long-s:" + value;
        public string Choice(string value) => "first:" + value;
        public string CHOICE(string value) => "last:" + value;
        public string SLOTCASE(string value) => "derived-nonvirtual:" + value;
        public override string SlotCase(string value) => "derived-slot:" + value;
        public static int Twice(int value) => value * 2;
        public static int TWICE(int value) => value * 3;
        public static object WiderReturn(string value) => value;
        public static string NarrowerReturn(string value) => value;
        public static void WiderArgument(object value) { }
        public static int RefValue(ref int value) => ++value;
        public static int OutValue(out int value) => value = 42;
        public static int Value(int value) => value + 1;
        public static T Generic<T>(T value) => value;
        public static int* Pointer(int* value) => value;
        public static uint* UnsignedPointer(int* value) => (uint*)value;
        public static ref int RefReturn(ref int value) => ref value;
        public static delegate*<int> FunctionReturn(int value) => (delegate*<int>)(nint)value;
        public static NameEnum EnumIdentity(NameEnum value) => value;
        public static int ObjectValue(object value) => 42;
    }

    class NameCovariantBase
    {
        public virtual object Kind() => "base";
    }

    class NameCovariantChild : NameCovariantBase
    {
        public virtual string KIND() => "case-before";
        public override string Kind() => "covariant";
    }

    class NameCovariantGrandchild : NameCovariantChild
    {
        public override string Kind() => "grandchild";
    }

    class NameVirtualChoices
    {
        public virtual string Pick() => "first";
        public virtual string PICK() => "last";
    }

    class NameGeneric<T>
    {
        public T Identity(T value) => value;
        private static T StaticIdentity(T value) => value;
    }

    class NameTemplate<T>
    {
        public Type Kind() => typeof(T);
        private static Type StaticKind() => typeof(T);
    }

    interface NameStaticVirtual
    {
        static virtual int Value() => 42;
        static abstract int AbstractValue();
    }

    delegate int NameRef(ref int value);
    delegate int NameOut(out int value);
    unsafe delegate int* NamePointer(int* value);
    delegate ref int NameRefReturn(ref int value);
    delegate T NameOpen<T>(T value);
    delegate int NameRefObject(ref object value);
    unsafe delegate delegate*<int> NameFunctionReturn(int value);
    unsafe delegate delegate*<long> NameOtherFunctionReturn(int value);
    unsafe delegate delegate*<int> NameWrongFunctionArgument(string value);
    enum NameEnum { Value }

    unsafe delegate uint* SignatureUnsignedPointer(uint* value);
    unsafe delegate void* SignatureVoidPointer(void* value);
    unsafe delegate int** SignatureDeepPointer(int** value);
    unsafe delegate uint** SignatureOtherDeepPointer(uint** value);
    unsafe delegate int***** SignatureFivePointer(int***** value);
    unsafe delegate int****** SignatureSixPointer(int****** value);
    unsafe delegate nint* SignatureNativePointer(nint* value);
    unsafe delegate nuint* SignatureUnsignedNativePointer(nuint* value);
    unsafe delegate ulong* SignatureLongPointer(ulong* value);
    unsafe delegate byte* SignatureBytePointer(byte* value);
    unsafe delegate ushort* SignatureShortPointer(ushort* value);
    unsafe delegate float* SignatureFloatPointer(float* value);
    unsafe delegate delegate*<int>* SignatureNestedFunction(int value);
    unsafe delegate int SignatureNestedFunctionArg(delegate*<int>* value);
    unsafe delegate delegate*<Guid> SignatureGuidFunction(int value);
    unsafe delegate delegate*<Guid>* SignatureGuidNestedFunction(int value);
    unsafe delegate int SignatureGuidNestedFunctionArg(delegate*<Guid>* value);
    unsafe delegate string SignatureGuidNestedFunctionText(delegate*<Guid>* value);
    unsafe delegate int SignatureGuidMixedFunctionArg(delegate*<Guid>* value, string extra);
    delegate int SignatureOpenInstance(SignatureTarget target, ref object value);
    unsafe delegate int SignatureStringPointerArg(string* value);
    unsafe delegate int SignatureObjectPointerArg(object* value);
    unsafe delegate int SignatureDeepStringPointerArg(string** value);
    unsafe delegate object* SignatureObjectPointerReturn();
    unsafe delegate string* SignatureStringPointerReturn();
    delegate ref Assembly SignatureRefAssemblyReturn();
    delegate Span<int> SignatureSpanReturn(ref Span<int> value);
    delegate ref Span<int> SignatureRefSpanReturn(ref Span<int> value);
    delegate ref Span<long> SignatureOtherRefSpanReturn(ref Span<int> value);
    delegate ref CancellationToken SignatureTokenReturn();
    delegate ref CancellationTokenRegistration SignatureRegistrationReturn();
    delegate void SignatureTokenArgument(ref CancellationToken value);
    delegate void SignatureRegistrationArgument(ref CancellationTokenRegistration value);
    delegate void SignatureGuidRef(ref Guid value);
    delegate void SignatureIntRef(ref int value);
    delegate void SignatureObjectRef(ref object value);
    delegate void SignatureStringRef(ref string value);
    delegate void SignatureMixedRef(ref Guid value, int extra);
    delegate ref Guid SignatureGuidRefReturn();
    delegate ref int SignatureIntRefReturn();
    unsafe delegate Guid* SignatureGuidPointer(Guid* value);
    unsafe delegate Guid* SignatureGuidPointerReturn();
    unsafe delegate int* SignatureIntPointerReturn();
    unsafe delegate delegate* unmanaged[Cdecl]<int> SignatureCdecl(int value);
    unsafe delegate delegate* unmanaged[Stdcall]<int> SignatureStdcall(int value);
    unsafe delegate delegate* unmanaged[Cdecl, SuppressGCTransition]<int> SignatureSuppress(int value);
    unsafe delegate delegate* unmanaged[SuppressGCTransition, Cdecl]<int> SignatureReordered(int value);
    unsafe delegate ref delegate*<int> SignatureRefFunction();
    unsafe delegate ref delegate*<long> SignatureOtherRefFunction();
    unsafe delegate int SignatureFunctionArg(delegate*<int> value);
    unsafe delegate int SignatureOtherFunctionArg(delegate*<long> value);
    unsafe delegate int SignatureCdeclArg(delegate* unmanaged[Cdecl]<int> value);
    delegate int SignatureObject(object value);
    delegate int SignatureOutObject(out object value);
    delegate int SignatureRefString(ref string value);
    delegate ref object SignatureRefObjectReturn();
    delegate ref string SignatureRefStringReturn();
    delegate int SignatureRefReceiver(ref SignatureTarget target, object value);

    unsafe class SignatureTarget
    {
        public static int NestedArgumentCalls;
        private static object objectSlot = "before";
        private static string stringSlot = "text";
        private static delegate*<int> functionSlot = (delegate*<int>)(nint)256;
        private static Assembly assemblySlot = typeof(SignatureTarget).Assembly;
        public int Instance(object value) => 42;
        public int InstanceReference(ref object value) { value = "instance"; return 44; }
        public static int Value(object value) => 42;
        public static int Reference(ref object value) { value = "after"; return 42; }
        public static int Output(out object value) { value = "out"; return 43; }
        public static int ClosedReference(ref object value, int number) => number;
        public static int ClosedValue(object value, int number) => number;
        public static int ClosedBinding(object marker, ref object value) { value = marker; return 45; }
        public static ref object ObjectReturn() => ref objectSlot;
        public static ref string StringReturn() => ref stringSlot;
        public static int* Pointer(int* value) => value;
        public static void* VoidPointer(void* value) => value;
        public static int** DeepPointer(int** value) => value;
        public static int***** FivePointer(int***** value) => value;
        public static long* LongPointer(long* value) => value;
        public static nint* NativePointer(nint* value) => value;
        public static bool* BoolPointer(bool* value) => value;
        public static char* CharPointer(char* value) => value;
        public static sbyte* BytePointer(sbyte* value) => value;
        public static int ObjectPointerArg(object* value) => 46;
        public static int StringPointerArg(string* value) => 47;
        public static int DeepObjectPointerArg(object** value) => 48;
        public static string* StringPointerReturn() => (string*)(nint)256;
        public static object* ObjectPointerReturn() => (object*)(nint)512;
        public static ref Assembly RefAssemblyReturn() => ref assemblySlot;
        public static Assembly AssemblyReturn() => assemblySlot;
        public static Span<int> SpanReturn(ref Span<int> value) => value;
        public static ref Span<int> RefSpanReturn(ref Span<int> value) => ref value;
        public static delegate* unmanaged[Cdecl]<int> Cdecl(int value) => (delegate* unmanaged[Cdecl]<int>)(nint)value;
        public static delegate* unmanaged[Cdecl, SuppressGCTransition]<int> Suppress(int value) => (delegate* unmanaged[Cdecl, SuppressGCTransition]<int>)(nint)value;
        public static ref delegate*<int> RefFunction() => ref functionSlot;
        public static int FunctionArg(delegate*<int> value) => (int)(nint)value;
        public static int CdeclArg(delegate* unmanaged[Cdecl]<int> value) => (int)(nint)value;
    }

    class SignatureOpaqueTarget
    {
        private static CancellationToken token;
        private static CancellationTokenRegistration registration;
        public static ref CancellationToken TokenReturn() => ref token;
        public static ref CancellationTokenRegistration RegistrationReturn() => ref registration;
        public static void TokenArgument(ref CancellationToken value) { }
        public static void RegistrationArgument(ref CancellationTokenRegistration value) { }
        public static bool TokenCanceled() => token.IsCancellationRequested;
    }

    unsafe class SignatureGeneric<T>
    {
        public Type Kind() => typeof(T);
        public static delegate*<T> Function(int value) => (delegate*<T>)(nint)value;
        public static delegate*<int> Fixed(int value) => (delegate*<int>)(nint)value;
        public static delegate*<T>* Nested(int value) => (delegate*<T>*)(nint)value;
        public static delegate*<int>* FixedNested(int value) => (delegate*<int>*)(nint)value;
        public static int NestedArgument(delegate*<T>* value)
        {
            SignatureTarget.NestedArgumentCalls++;
            return (int)(nint)value;
        }
        public static int MixedArgument(delegate*<T>* value, int extra) => extra;
        public static int FixedNestedArgument(delegate*<int>* value) => (int)(nint)value;
    }

    class SignatureRefGeneric<T>
    {
        public static void Ref(ref T value) { }
        public static void Mixed(ref T value, string extra) { }
        public static ref T Return() => throw new InvalidOperationException();
        public static int Fixed() => 7;
    }

    unsafe class SignaturePointerGeneric<T> where T : unmanaged
    {
        public static T* Pointer(T* value) => value;
        public static T* Return() => null;
        public static int Fixed() => 8;
    }

    class SignatureClassRefGeneric<T> where T : class
    {
        public static void Ref(ref T value) { }
        public static ref T Return() => throw new InvalidOperationException();
    }

    class SignatureAnchoredRef<T> where T : class
    {
        public static int Ref(ref T value) => 42;
    }

    unsafe delegate int SignatureArrayFunctionProbe(delegate*<int[], int> value);
    unsafe delegate int SignatureMatrixFunctionProbe(delegate*<int[,], int> value);
    unsafe delegate int SignatureTokenFunctionProbe(delegate*<CancellationToken, int> value);

    unsafe class SignatureArrayOverload<T>
    {
        public static int Ref(delegate*<int[], int> value) => 117;
        public static int Ref(delegate*<T[], int> value) => 118;
    }

    unsafe class SignatureMatrixOverload<T>
    {
        public static int Ref(delegate*<int[,], int> value) => 127;
        public static int Ref(delegate*<T[,], int> value) => 128;
    }

    unsafe class SignatureRankOverload<T>
    {
        public static int Ref(delegate*<int[,], int> value) => 137;
        public static int Ref(delegate*<T[,,], int> value) => 138;
    }

    unsafe class SignatureDirectArrayOverload<T>
    {
        public static int Ref(delegate*<int[], int> value) => 147;
        public static int Ref(delegate*<T, int> value) => 148;
    }

    unsafe class SignatureTokenOverload<T>
    {
        public static int Ref(delegate*<CancellationToken, int> value) => 157;
        public static int Ref(delegate*<T, int> value) => 158;
    }

    class SignatureRuntimeArgument<T> { }
    class SignatureRuntimeKnownBox<T> { }
    unsafe delegate int SignatureRuntimeIntBoxProbe(delegate*<SignatureRuntimeKnownBox<int>, int> value);
    unsafe delegate int SignatureRuntimeGuidBoxProbe(delegate*<SignatureRuntimeKnownBox<Guid>, int> value);
    unsafe class SignatureRuntimeBoxOverload<T>
    {
        public static int Ref(delegate*<SignatureRuntimeKnownBox<int>, int> value) => 177;
        public static int Ref(delegate*<T, int> value) => 178;
        public static int Matching(delegate*<SignatureRuntimeKnownBox<Guid>, int> value) => 187;
        public static int Matching(delegate*<T, int> value) => 188;
    }

    unsafe delegate int SignatureValueTaskFunctionProbe(delegate*<ValueTask<int>, int> value);
    unsafe class SignatureValueTaskOverload<T>
    {
        public static int Ref(delegate*<ValueTask<int>, int> value) => 167;
        public static int Ref(delegate*<ValueTask<T>, int> value) => 168;
    }

    unsafe class SignatureValueTaskTypeOverload<T>
    {
        public static int Ref(delegate*<ValueTask<int>, int> value) => 197;
        public static int Ref(delegate*<T, int> value) => 198;
    }

    unsafe delegate int SignatureFunctionProbe(delegate*<int, int> value);
    unsafe delegate int SignatureDeepProbe(int***** value);

    unsafe class SignatureFunctionOverload<T>
    {
        public static int Ref(delegate*<int, int> value) => 87;
        public static int Ref(delegate*<T, int> value) => 88;
    }

    unsafe class SignatureDeepOverload<T> where T : unmanaged
    {
        public static int Ref(int***** value) => 97;
        public static int Ref(T***** value) => 98;
        public static int Depth(int***** value) => 107;
        public static int Depth(T****** value) => 108;
    }

    class SignatureOverload<T>
    {
        public static int Ref(ref int value) { value++; return 7; }
        public static int Ref(ref T value) => 8;
    }

    class SignatureOverloadBase<T, U>
    {
        public static int Ref(ref int value) { value++; return 37; }
        public static int Ref(ref U value) => 38;
    }

    class SignatureOverloadDerived<T, U> : SignatureOverloadBase<T, U> { }
    class SignatureOverloadReordered<T, U> : SignatureOverloadBase<U, T> { }

    class SignatureBox<T> { }
    class SignatureOtherBox<T> { }
    class SignaturePairBox<T, U> { }
    delegate int SignatureBoxRef(ref SignatureBox<int>? value);
    delegate int SignaturePairBoxRef(ref SignaturePairBox<Guid, int>? value);

    class SignatureComposedOverload<T, U>
    {
        public static int Ref(ref SignatureBox<int>? value) { value = null; return 47; }
        public static int Ref(ref SignatureBox<T>? value) => 48;
        public static int Family(ref SignatureBox<int>? value) { value = null; return 57; }
        public static int Family(ref SignatureOtherBox<T>? value) => 58;
        public static int Constant(ref SignaturePairBox<Guid, int>? value) { value = null; return 67; }
        public static int Constant(ref SignaturePairBox<T, U>? value) => 68;
    }

    class SignatureReferenceOverload<T, U> where T : class where U : class
    {
        public static int Ref(ref string value) { value = "fixed"; return 27; }
        public static int Ref(ref U value) => 28;
    }

    unsafe delegate int SignaturePointerProbe(int* value);

    unsafe class SignaturePointerOverload<T> where T : unmanaged
    {
        public static int Pointer(int* value) { (*value)++; return 17; }
        public static int Pointer(T* value) => 18;
        public static int* Result() => (int*)(nint)256;
        public static T* result() => null;
    }

    class SignatureOverloadStorage { public static int Value; }

    class SignatureReturnOverload<T>
    {
        public static ref int Result() => ref SignatureOverloadStorage.Value;
        public static ref T result() => throw new InvalidOperationException();
    }

    static class Program
    {
        private static string SignatureOutcome(Func<Delegate?> bind)
        {
            try
            {
                return bind() is null ? "null" : "bound";
            }
            catch (ArgumentException exception)
            {
                return exception.GetType().Name + "/" + (exception.ParamName ?? "-");
            }
            catch (PlatformNotSupportedException)
            {
                return "unsupported";
            }
        }

        private static void SignaturePair(string label, Type delegateType, Type owner, string name)
        {
            MethodInfo method = owner.GetMethod(name)!;
            string methodResult = SignatureOutcome(() => Delegate.CreateDelegate(delegateType, method, false));
            string nameResult = SignatureOutcome(() => Delegate.CreateDelegate(delegateType, owner, name, false, false));
            Console.WriteLine($"signature {label} => {methodResult}/{nameResult}");
        }

        internal static unsafe void RunSignatureBindings()
        {
            Console.WriteLine("== delegate signature compatibility ==");
            Type owner = typeof(SignatureTarget);
            SignaturePair("ref object to value", typeof(NameRefObject), owner, "Value");
            SignaturePair("value to ref object", typeof(SignatureObject), owner, "Reference");
            SignaturePair("ref string to ref object", typeof(SignatureRefString), owner, "Reference");
            SignaturePair("out to ref", typeof(SignatureOutObject), owner, "Reference");
            SignaturePair("ref to out", typeof(NameRefObject), owner, "Output");
            SignaturePair("ref return variance", typeof(SignatureRefObjectReturn), owner, "StringReturn");
            SignaturePair("ref return opposite", typeof(SignatureRefStringReturn), owner, "ObjectReturn");
            SignaturePair("pointer signedness", typeof(SignatureUnsignedPointer), owner, "Pointer");
            SignaturePair("pointer to void", typeof(SignatureVoidPointer), owner, "Pointer");
            SignaturePair("void to pointer", typeof(NamePointer), owner, "VoidPointer");
            SignaturePair("deep pointer signedness", typeof(SignatureOtherDeepPointer), owner, "DeepPointer");
            SignaturePair("deep pointer exact", typeof(SignatureDeepPointer), owner, "DeepPointer");
            SignaturePair("five pointer exact", typeof(SignatureFivePointer), owner, "FivePointer");
            SignaturePair("pointer depth mismatch", typeof(SignatureSixPointer), owner, "FivePointer");
            SignaturePair("native vs fixed pointer", typeof(SignatureNativePointer), owner, "LongPointer");
            SignaturePair("unsigned native vs fixed pointer", typeof(SignatureUnsignedNativePointer), owner, "LongPointer");
            SignaturePair("native pointer signedness", typeof(SignatureUnsignedNativePointer), owner, "NativePointer");
            SignaturePair("long pointer signedness", typeof(SignatureLongPointer), owner, "LongPointer");
            SignaturePair("bool vs byte pointer", typeof(SignatureBytePointer), owner, "BoolPointer");
            SignaturePair("char vs ushort pointer", typeof(SignatureShortPointer), owner, "CharPointer");
            SignaturePair("byte pointer signedness", typeof(SignatureBytePointer), owner, "BytePointer");
            SignaturePair("float vs int pointer", typeof(SignatureFloatPointer), owner, "Pointer");
            SignaturePair("reference pointer argument", typeof(SignatureStringPointerArg), owner, "ObjectPointerArg");
            SignaturePair("reference pointer opposite", typeof(SignatureObjectPointerArg), owner, "StringPointerArg");
            SignaturePair("reference pointer return", typeof(SignatureObjectPointerReturn), owner, "StringPointerReturn");
            SignaturePair("reference pointer return opposite", typeof(SignatureStringPointerReturn), owner, "ObjectPointerReturn");
            SignaturePair("reference deep pointer", typeof(SignatureDeepStringPointerArg), owner, "DeepObjectPointerArg");
            SignaturePair("function leaf to object pointer", typeof(SignatureNestedFunctionArg), owner, "ObjectPointerArg");
            SignaturePair("headerless ref return exact", typeof(SignatureRefAssemblyReturn), owner, "RefAssemblyReturn");
            SignaturePair("headerless ref return to value", typeof(Func<Assembly>), owner, "RefAssemblyReturn");
            SignaturePair("headerless value return to ref", typeof(SignatureRefAssemblyReturn), owner, "AssemblyReturn");
            SignaturePair("byref-like ref to value", typeof(SignatureSpanReturn), owner, "RefSpanReturn");
            SignaturePair("byref-like value to ref", typeof(SignatureRefSpanReturn), owner, "SpanReturn");
            SignaturePair("byref-like ref exact", typeof(SignatureRefSpanReturn), owner, "RefSpanReturn");
            SignaturePair("byref-like ref referent mismatch", typeof(SignatureOtherRefSpanReturn), owner, "RefSpanReturn");
            SignaturePair("function return exact", typeof(NameFunctionReturn), typeof(NameTarget), "FunctionReturn");
            SignaturePair("function return mismatch", typeof(NameOtherFunctionReturn), typeof(NameTarget), "FunctionReturn");
            SignaturePair("Cdecl exact", typeof(SignatureCdecl), owner, "Cdecl");
            SignaturePair("Cdecl vs Stdcall", typeof(SignatureStdcall), owner, "Cdecl");
            SignaturePair("managed vs Cdecl", typeof(NameFunctionReturn), owner, "Cdecl");
            SignaturePair("suppressed exact", typeof(SignatureSuppress), owner, "Suppress");
            SignaturePair("suppressed vs Cdecl", typeof(SignatureCdecl), owner, "Suppress");
            SignaturePair("suppressed order", typeof(SignatureReordered), owner, "Suppress");
            SignaturePair("ref function exact", typeof(SignatureRefFunction), owner, "RefFunction");
            SignaturePair("ref function mismatch", typeof(SignatureOtherRefFunction), owner, "RefFunction");
            SignaturePair("function argument exact", typeof(SignatureFunctionArg), owner, "FunctionArg");
            SignaturePair("function argument mismatch", typeof(SignatureOtherFunctionArg), owner, "FunctionArg");
            SignaturePair("function argument convention", typeof(SignatureFunctionArg), owner, "CdeclArg");
            SignaturePair("generic function exact", typeof(NameFunctionReturn), typeof(SignatureGeneric<int>), "Function");
            SignaturePair("generic function mismatch", typeof(NameFunctionReturn), typeof(SignatureGeneric<long>), "Function");
            MethodInfo reference = owner.GetMethod("Reference")!;
            Console.WriteLine("signature hard siblings => "
                + SignatureOutcome(() => reference.CreateDelegate(typeof(SignatureObject))) + "/"
                + SignatureOutcome(() => reference.CreateDelegate<SignatureObject>()) + "/"
                + SignatureOutcome(() => reference.CreateDelegate(typeof(SignatureObject), null)) + "/"
                + SignatureOutcome(() => reference.CreateDelegate<SignatureObject>(null)) + "/"
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureObject), reference)) + "/"
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureObject), null, reference)) + "/"
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureObject), null, reference, false)));
            MethodInfo instance = owner.GetMethod("Instance")!;
            Console.WriteLine("signature receiver byref => "
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureRefReceiver), instance, false)));
            MethodInfo closedReference = owner.GetMethod("ClosedReference")!;
            Console.WriteLine("signature closed static byref => "
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(Func<int, int>), "bound", closedReference, false)) + "/"
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(Func<int, int>), null, closedReference, false)));
            foreach (Type shell in new[] { typeof(Delegate), typeof(MulticastDelegate) })
            {
                Console.WriteLine($"signature shell {shell.Name} => "
                    + SignatureOutcome(() => Delegate.CreateDelegate(shell, reference, false)) + "/"
                    + SignatureOutcome(() => reference.CreateDelegate(shell)));
            }
            object value = "initial";
            var refToOut = (NameRefObject)reference.CreateDelegate(typeof(NameRefObject));
            Console.WriteLine($"signature ref alias => {refToOut(ref value)}/{value}");
            SignatureTarget receiver = new SignatureTarget();
            MethodInfo instanceReference = owner.GetMethod("InstanceReference")!;
            var closedInstance = instanceReference.CreateDelegate<NameRefObject>(receiver);
            var openInstance = instanceReference.CreateDelegate<SignatureOpenInstance>();
            var closedStatic = owner.GetMethod("ClosedBinding")!.CreateDelegate<NameRefObject>("closed");
            int closedResult = closedInstance(ref value);
            int openResult = openInstance(receiver, ref value);
            int staticResult = closedStatic(ref value);
            Console.WriteLine($"signature byref modes => {closedResult}/{openResult}/{staticResult}/{value}");
            var alias = owner.GetMethod("ObjectReturn")!.CreateDelegate<SignatureRefObjectReturn>();
            ref object slot = ref alias();
            slot = "changed";
            Console.WriteLine($"signature ref return alias => {alias()}");
            var pointer = owner.GetMethod("Pointer")!.CreateDelegate<NamePointer>();
            int number = 42;
            Console.WriteLine($"signature pointer call => {*pointer(&number)}");
            var referenceArgument = owner.GetMethod("ObjectPointerArg")!.CreateDelegate<SignatureStringPointerArg>();
            var referenceReturn = owner.GetMethod("StringPointerReturn")!.CreateDelegate<SignatureObjectPointerReturn>();
            Console.WriteLine($"signature reference pointer calls => {referenceArgument(null)}/{(nint)referenceReturn()}");
            var assemblyAlias = owner.GetMethod("RefAssemblyReturn")!.CreateDelegate<SignatureRefAssemblyReturn>();
            ref Assembly assembly = ref assemblyAlias();
            assembly = typeof(object).Assembly;
            Console.WriteLine($"signature headerless ref alias => {assemblyAlias() == typeof(object).Assembly}");
            var signed = owner.GetMethod("Pointer")!.CreateDelegate<SignatureUnsignedPointer>();
            Console.WriteLine($"signature relaxed pointer call => {*signed((uint*)&number)}");
            var function = typeof(NameTarget).GetMethod("FunctionReturn")!.CreateDelegate<NameFunctionReturn>();
            var namedFunction = (NameFunctionReturn)Delegate.CreateDelegate(typeof(NameFunctionReturn), typeof(NameTarget), "FunctionReturn");
            Console.WriteLine($"signature function return calls => {(nint)function(256)}/{(nint)namedFunction(512)}");
            var convention = owner.GetMethod("Cdecl")!.CreateDelegate<SignatureStdcall>();
            Console.WriteLine($"signature unmanaged return call => {(nint)convention(768)}");
            var functionArgument = owner.GetMethod("FunctionArg")!.CreateDelegate<SignatureFunctionArg>();
            Console.WriteLine($"signature function argument call => {functionArgument((delegate*<int>)(nint)1024)}");
            var functionAlias = owner.GetMethod("RefFunction")!.CreateDelegate<SignatureRefFunction>();
            ref delegate*<int> functionSlot = ref functionAlias();
            functionSlot = (delegate*<int>)(nint)1280;
            Console.WriteLine($"signature ref function alias => {(nint)functionAlias()}");
            Type template = typeof(SignatureGeneric<>).MakeGenericType(typeof(Guid));
            SignaturePair("template known return mismatch", typeof(SignatureGuidNestedFunctionText), template, "NestedArgument");
            SignaturePair("template known argument mismatch", typeof(SignatureGuidMixedFunctionArg), template, "MixedArgument");
            var fixedFunction = template.GetMethod("Fixed")!.CreateDelegate<NameFunctionReturn>();
            var fixedNamed = (NameFunctionReturn)Delegate.CreateDelegate(typeof(NameFunctionReturn), template, "Fixed");
            var fixedNested = template.GetMethod("FixedNested")!.CreateDelegate<SignatureNestedFunction>();
            var fixedArgument = template.GetMethod("FixedNestedArgument")!.CreateDelegate<SignatureNestedFunctionArg>();
            Console.WriteLine($"signature template fixed => {(nint)fixedFunction(1536)}/{(nint)fixedNamed(1792)}/{(nint)fixedNested(2048)}/{fixedArgument((delegate*<int>*)(nint)2304)}/{fixedFunction.Method.DeclaringType == template}");
            Console.WriteLine("delegate signature compatibility end");
        }

        internal static void RunSignatureBoundaries(bool ordinary = true, bool overloads = true, bool shapes = true, bool leaves = true, bool identities = true, bool runtimeArguments = true)
        {
            Console.WriteLine("== runtime delegate signature boundaries ==");
            Type template = typeof(SignatureGeneric<>).MakeGenericType(typeof(Guid));
            SignaturePair("template dependent return", typeof(NameFunctionReturn), template, "Function");
            SignaturePair("template dependent nested return", typeof(SignatureNestedFunction), template, "Nested");
            SignaturePair("template dependent nested argument", typeof(SignatureNestedFunctionArg), template, "NestedArgument");
            SignaturePair("template matching return", typeof(SignatureGuidFunction), template, "Function");
            SignaturePair("template matching nested return", typeof(SignatureGuidNestedFunction), template, "Nested");
            SignaturePair("template matching nested argument", typeof(SignatureGuidNestedFunctionArg), template, "NestedArgument");
            Console.WriteLine("signature template missing => "
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureGuidFunction), template, "Missing", false, false)));
            Type opaque = typeof(SignatureOpaqueTarget);
            Console.WriteLine("signature opaque return invoke => " + OpaqueInvokeOutcome(opaque.GetMethod("TokenReturn")!, null));
            Console.WriteLine("signature opaque argument invoke => " + OpaqueInvokeOutcome(opaque.GetMethod("TokenArgument")!, new object[] { "wrong" }));
            Console.WriteLine("runtime delegate signature boundaries end");
            if (ordinary)
                RunOrdinaryTemplateBoundaries();
            if (overloads)
                RunOrdinaryOverloadBoundaries();
            if (overloads && shapes)
                RunShapeOverloadBoundaries();
            if (overloads && shapes && leaves)
                RunLeafOverloadBoundaries();
            if (overloads && shapes && leaves && identities)
                RunIdentityOverloadBoundaries();
            if (overloads && shapes && leaves && identities && runtimeArguments)
                RunRuntimeArgumentBoundaries();
        }

        private static void RunOrdinaryTemplateBoundaries()
        {
            Console.WriteLine("== ordinary runtime delegate signature boundaries ==");
            Type refs = typeof(SignatureRefGeneric<>).MakeGenericType(typeof(Guid));
            Type pointers = typeof(SignaturePointerGeneric<>).MakeGenericType(typeof(Guid));
            Type classes = typeof(SignatureClassRefGeneric<>).MakeGenericType(typeof(string));
            SignaturePair("ordinary ref matching", typeof(SignatureGuidRef), refs, "Ref");
            SignaturePair("ordinary ref mismatch", typeof(SignatureIntRef), refs, "Ref");
            SignaturePair("ordinary ref object", typeof(SignatureObjectRef), refs, "Ref");
            SignaturePair("ordinary pointer matching", typeof(SignatureGuidPointer), pointers, "Pointer");
            SignaturePair("ordinary pointer mismatch", typeof(NamePointer), pointers, "Pointer");
            SignaturePair("ordinary ref return matching", typeof(SignatureGuidRefReturn), refs, "Return");
            SignaturePair("ordinary ref return mismatch", typeof(SignatureIntRefReturn), refs, "Return");
            SignaturePair("ordinary pointer return matching", typeof(SignatureGuidPointerReturn), pointers, "Return");
            SignaturePair("ordinary pointer return mismatch", typeof(SignatureIntPointerReturn), pointers, "Return");
            SignaturePair("ordinary class ref matching", typeof(SignatureStringRef), classes, "Ref");
            SignaturePair("ordinary class ref mismatch", typeof(SignatureObjectRef), classes, "Ref");
            SignaturePair("ordinary class return matching", typeof(SignatureRefStringReturn), classes, "Return");
            SignaturePair("ordinary class return mismatch", typeof(SignatureRefObjectReturn), classes, "Return");
            Console.WriteLine("signature ordinary template missing => "
                + SignatureOutcome(() => Delegate.CreateDelegate(typeof(SignatureGuidRef), refs, "Missing", false, false)));
            Console.WriteLine("signature ordinary ref invoke => " + OpaqueInvokeOutcome(refs.GetMethod("Ref")!, new object[] { Guid.Empty }));
            Console.WriteLine("signature ordinary pointer invoke => " + OpaqueInvokeOutcome(pointers.GetMethod("Pointer")!, new object[] { (nint)0 }));
            Console.WriteLine("ordinary runtime delegate signature boundaries end");
        }

        internal static void RunOrdinaryTemplateSignatures()
        {
            Console.WriteLine("== ordinary runtime delegate signature controls ==");
            Type refs = typeof(SignatureRefGeneric<>).MakeGenericType(typeof(Guid));
            Type pointers = typeof(SignaturePointerGeneric<>).MakeGenericType(typeof(Guid));
            SignaturePair("ordinary known return mismatch", typeof(NameRef), refs, "Ref");
            SignaturePair("ordinary known argument mismatch", typeof(SignatureMixedRef), refs, "Mixed");
            SignaturePair("ordinary known shape mismatch", typeof(Action<Guid>), refs, "Ref");
            var refFixed = refs.GetMethod("Fixed")!.CreateDelegate<Func<int>>();
            var pointerFixed = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), pointers, "Fixed");
            Console.WriteLine($"signature ordinary template fixed => {refFixed()}/{pointerFixed()}/{refFixed.Method.DeclaringType == refs}");
            string value = "aot";
            int direct = SignatureAnchoredRef<string>.Ref(ref value);
            var anchored = typeof(SignatureAnchoredRef<string>).GetMethod("Ref")!.CreateDelegate<SignatureRefString>();
            var named = (SignatureRefString)Delegate.CreateDelegate(typeof(SignatureRefString), typeof(SignatureAnchoredRef<string>), "Ref");
            Console.WriteLine($"signature ordinary compiled ref => {direct}/{anchored(ref value)}/{named(ref value)}");
            Console.WriteLine("ordinary runtime delegate signature controls end");
        }

        private static string OverloadRefCall(Type owner, bool throwOnFailure)
        {
            try
            {
                var target = (NameRef?)Delegate.CreateDelegate(typeof(NameRef), owner, "Ref", false, throwOnFailure);
                if (target is null)
                    return "null";
                int value = 10;
                int returned = target(ref value);
                return $"bound/{returned}/{value}";
            }
            catch (PlatformNotSupportedException)
            {
                return "unsupported";
            }
        }

        private static string OverloadBoxCall(Type owner, bool throwOnFailure)
        {
            try
            {
                var target = (SignatureBoxRef?)Delegate.CreateDelegate(typeof(SignatureBoxRef), owner, "Ref", false, throwOnFailure);
                if (target is null)
                    return "null";
                SignatureBox<int>? value = new();
                int returned = target(ref value);
                return $"bound/{returned}/{value is null}";
            }
            catch (PlatformNotSupportedException)
            {
                return "unsupported";
            }
        }

        internal static unsafe void RunOrdinaryOverloads()
        {
            Console.WriteLine("== ordinary runtime overload selection ==");
            Type refs = typeof(SignatureOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload ref => {OverloadRefCall(refs, false)}/{OverloadRefCall(refs, true)}");
            Type derived = typeof(SignatureOverloadDerived<,>).MakeGenericType(typeof(int), typeof(Guid));
            Console.WriteLine($"signature overload declaring base => {OverloadRefCall(derived, false)}/{OverloadRefCall(derived, true)}");
            Type references = typeof(SignatureReferenceOverload<,>).MakeGenericType(typeof(string), typeof(object));
            var reference = (SignatureRefString)Delegate.CreateDelegate(typeof(SignatureRefString), references, "Ref");
            string text = "before";
            int returned = reference(ref text);
            Console.WriteLine($"signature overload reference arguments => {returned}/{text}");
            Type pointers = typeof(SignaturePointerOverload<>).MakeGenericType(typeof(Guid));
            var pointer = (SignaturePointerProbe)Delegate.CreateDelegate(typeof(SignaturePointerProbe), pointers, "Pointer");
            int value = 10;
            returned = pointer(&value);
            Console.WriteLine($"signature overload pointer => {returned}/{value}");
            var pointerReturn = (SignatureIntPointerReturn)Delegate.CreateDelegate(typeof(SignatureIntPointerReturn), pointers, "Result", true);
            Console.WriteLine($"signature overload pointer return => {(nint)pointerReturn()}");
            Type returns = typeof(SignatureReturnOverload<>).MakeGenericType(typeof(Guid));
            var alias = (SignatureIntRefReturn)Delegate.CreateDelegate(typeof(SignatureIntRefReturn), returns, "Result", true);
            alias() = 19;
            Console.WriteLine($"signature overload ref return => {alias()}/{SignatureOverloadStorage.Value}");
            Type box = typeof(SignatureBox<>).MakeGenericType(typeof(Guid));
            Type otherBox = typeof(SignatureOtherBox<>).MakeGenericType(typeof(Guid));
            Type pairBox = typeof(SignaturePairBox<,>).MakeGenericType(typeof(Guid), typeof(string));
            Console.WriteLine($"signature overload composed types => {box.GetGenericArguments()[0] == typeof(Guid)}/{otherBox.GetGenericArguments()[0] == typeof(Guid)}/{pairBox.GetGenericArguments()[1] == typeof(string)}");
            Type composed = typeof(SignatureComposedOverload<,>).MakeGenericType(typeof(Guid), typeof(string));
            Console.WriteLine($"signature overload composed ref => {OverloadBoxCall(composed, false)}/{OverloadBoxCall(composed, true)}");
            Type family = typeof(SignatureComposedOverload<,>).MakeGenericType(typeof(int), typeof(string));
            var familyTarget = (SignatureBoxRef)Delegate.CreateDelegate(typeof(SignatureBoxRef), family, "Family");
            SignatureBox<int>? boxed = new();
            returned = familyTarget(ref boxed);
            Console.WriteLine($"signature overload generic family => {returned}/{boxed is null}");
            var constant = (SignaturePairBoxRef)Delegate.CreateDelegate(typeof(SignaturePairBoxRef), composed, "Constant");
            SignaturePairBox<Guid, int>? pair = new();
            returned = constant(ref pair);
            Console.WriteLine($"signature overload constant argument => {returned}/{pair is null}");
            Console.WriteLine("ordinary runtime overload selection end");
        }

        private static void RunOrdinaryOverloadBoundaries()
        {
            Console.WriteLine("== unresolved ordinary overload selection ==");
            Type refs = typeof(SignatureOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine($"signature overload coincident ref => {OverloadRefCall(refs, false)}/{OverloadRefCall(refs, true)}");
            Type references = typeof(SignatureReferenceOverload<,>).MakeGenericType(typeof(object), typeof(string));
            Console.WriteLine("signature overload coincident reference => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureRefString), references, "Ref", false, false)));
            Type pointers = typeof(SignaturePointerOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident pointer => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignaturePointerProbe), pointers, "Pointer", false, false)));
            Console.WriteLine("signature overload coincident pointer return => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureIntPointerReturn), pointers, "Result", true, false)));
            Type returns = typeof(SignatureReturnOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident ref return => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureIntRefReturn), returns, "Result", true, false)));
            string reordered;
            try
            {
                Type owner = typeof(SignatureOverloadReordered<,>).MakeGenericType(typeof(Guid), typeof(int));
                reordered = SignatureOutcome(() => Delegate.CreateDelegate(typeof(NameRef), owner, "Ref", false, false));
            }
            catch (NotSupportedException)
            {
                reordered = "unsupported";
            }
            Console.WriteLine("signature overload reordered base => " + reordered);
            Type composed = typeof(SignatureComposedOverload<,>).MakeGenericType(typeof(int), typeof(string));
            Console.WriteLine($"signature overload coincident composed => {OverloadBoxCall(composed, false)}/{OverloadBoxCall(composed, true)}");
            Type constant = typeof(SignatureComposedOverload<,>).MakeGenericType(typeof(Guid), typeof(int));
            Console.WriteLine("signature overload coincident constant => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignaturePairBoxRef), constant, "Constant", false, false)));
            Console.WriteLine("unresolved ordinary overload selection end");
        }

        private static unsafe string FunctionOverloadCall(Type owner, bool throwOnFailure)
        {
            var target = (SignatureFunctionProbe)Delegate.CreateDelegate(typeof(SignatureFunctionProbe), owner, "Ref", false, throwOnFailure)!;
            return target((delegate*<int, int>)(nint)256).ToString();
        }

        private static unsafe string DeepOverloadCall(Type owner, string name, bool throwOnFailure)
        {
            var target = (SignatureDeepProbe)Delegate.CreateDelegate(typeof(SignatureDeepProbe), owner, name, false, throwOnFailure)!;
            return target((int*****)(nint)512).ToString();
        }

        internal static void RunShapeOverloads()
        {
            Console.WriteLine("== structured runtime overload selection ==");
            Type functions = typeof(SignatureFunctionOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload function => {FunctionOverloadCall(functions, false)}/{FunctionOverloadCall(functions, true)}");
            Type pointers = typeof(SignatureDeepOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload deep pointer => {DeepOverloadCall(pointers, "Ref", false)}/{DeepOverloadCall(pointers, "Ref", true)}");
            pointers = typeof(SignatureDeepOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine($"signature overload distinct deep levels => {DeepOverloadCall(pointers, "Depth", false)}/{DeepOverloadCall(pointers, "Depth", true)}");
            Console.WriteLine("structured runtime overload selection end");
        }

        private static void RunShapeOverloadBoundaries()
        {
            Console.WriteLine("== unresolved structured overload selection ==");
            Type functions = typeof(SignatureFunctionOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident function => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureFunctionProbe), functions, "Ref", false, false)));
            Type pointers = typeof(SignatureDeepOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident deep pointer => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureDeepProbe), pointers, "Ref", false, false)));
            Console.WriteLine("unresolved structured overload selection end");
        }

        private static unsafe int LeafOverloadCall(Type owner, int shape, bool throwOnFailure)
        {
            Type delegateType = shape == 0 ? typeof(SignatureArrayFunctionProbe)
                : shape == 1 ? typeof(SignatureMatrixFunctionProbe) : typeof(SignatureTokenFunctionProbe);
            var target = Delegate.CreateDelegate(delegateType, owner, "Ref", false, throwOnFailure)!;
            return shape == 0 ? ((SignatureArrayFunctionProbe)target)((delegate*<int[], int>)(nint)256)
                : shape == 1 ? ((SignatureMatrixFunctionProbe)target)((delegate*<int[,], int>)(nint)512)
                : ((SignatureTokenFunctionProbe)target)((delegate*<CancellationToken, int>)(nint)768);
        }

        internal static void RunLeafOverloads()
        {
            Console.WriteLine("== function leaf overload selection ==");
            Type arrays = typeof(SignatureArrayOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload array => {LeafOverloadCall(arrays, 0, false)}/{LeafOverloadCall(arrays, 0, true)}");
            Type matrices = typeof(SignatureMatrixOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload matrix => {LeafOverloadCall(matrices, 1, false)}/{LeafOverloadCall(matrices, 1, true)}");
            Type ranks = typeof(SignatureRankOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine($"signature overload array rank => {LeafOverloadCall(ranks, 1, false)}/{LeafOverloadCall(ranks, 1, true)}");
            Type tokenGuid = typeof(SignatureTokenOverload<>).MakeGenericType(typeof(Guid));
            Type tokenInt = typeof(SignatureTokenOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine($"signature overload opaque function leaf => {LeafOverloadCall(tokenGuid, 2, false)}/{LeafOverloadCall(tokenGuid, 2, true)}/{LeafOverloadCall(tokenInt, 2, false)}/{LeafOverloadCall(tokenInt, 2, true)}");
            Console.WriteLine("function leaf overload selection end");
        }

        private static void RunLeafOverloadBoundaries()
        {
            Console.WriteLine("== unresolved function leaf overload selection ==");
            Type arrays = typeof(SignatureArrayOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident array => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureArrayFunctionProbe), arrays, "Ref", false, false)));
            Type matrices = typeof(SignatureMatrixOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine("signature overload coincident matrix => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureMatrixFunctionProbe), matrices, "Ref", false, false)));
            Type direct = typeof(SignatureDirectArrayOverload<>).MakeGenericType(typeof(int[]));
            Console.WriteLine("signature overload direct array identity => " + SignatureOutcome(() =>
                Delegate.CreateDelegate(typeof(SignatureArrayFunctionProbe), direct, "Ref", false, false)));
            Console.WriteLine("unresolved function leaf overload selection end");
        }

        private static unsafe int ValueTaskOverloadCall(Type owner, bool throwOnFailure)
        {
            var target = (SignatureValueTaskFunctionProbe)Delegate.CreateDelegate(
                typeof(SignatureValueTaskFunctionProbe), owner, "Ref", false, throwOnFailure)!;
            return target((delegate*<ValueTask<int>, int>)(nint)1024);
        }

        internal static void RunIdentityOverloads()
        {
            Console.WriteLine("== omitted identity overload selection ==");
            Type values = typeof(SignatureValueTaskOverload<>).MakeGenericType(typeof(Guid));
            Console.WriteLine($"signature overload absent generic family => {ValueTaskOverloadCall(values, false)}/{ValueTaskOverloadCall(values, true)}");
            Console.WriteLine("omitted identity overload selection end");
        }

        private static void RunIdentityOverloadBoundaries()
        {
            Console.WriteLine("== unresolved omitted identity overload selection ==");
            Type values = typeof(SignatureValueTaskOverload<>).MakeGenericType(typeof(int));
            foreach (bool hard in new bool[] { false, true })
            {
                string mode = hard ? "hard" : "soft";
                Console.WriteLine("signature overload coincident generic family " + mode + " => " + SignatureOutcome(() =>
                    Delegate.CreateDelegate(typeof(SignatureValueTaskFunctionProbe), values, "Ref", false, hard)));
            }
            Console.WriteLine("unresolved omitted identity overload selection end");
        }

        private static unsafe int RuntimeBoxOverloadCall(Type owner, bool hard)
        {
            var target = (SignatureRuntimeIntBoxProbe)Delegate.CreateDelegate(
                typeof(SignatureRuntimeIntBoxProbe), owner, "Ref", false, hard)!;
            return target((delegate*<SignatureRuntimeKnownBox<int>, int>)(nint)1280);
        }

        internal static void RunRuntimeArgumentOverloads()
        {
            Console.WriteLine("== runtime type argument overload selection ==");
            Type argument = typeof(SignatureRuntimeArgument<>).MakeGenericType(typeof(Guid));
            Type owner = typeof(SignatureFunctionOverload<>).MakeGenericType(argument);
            Console.WriteLine($"signature overload synthesized argument => {FunctionOverloadCall(owner, false)}/{FunctionOverloadCall(owner, true)}");
            Type known = typeof(SignatureRuntimeKnownBox<>).MakeGenericType(typeof(Guid));
            Type boxes = typeof(SignatureRuntimeBoxOverload<>).MakeGenericType(known);
            Console.WriteLine($"signature overload generic argument difference => {RuntimeBoxOverloadCall(boxes, false)}/{RuntimeBoxOverloadCall(boxes, true)}");
            Console.WriteLine("runtime type argument overload selection end");
        }

        internal static void RunFamilyTypeOverloads()
        {
            Console.WriteLine("== generic family and runtime type overload selection ==");
            Type plain = typeof(SignatureValueTaskTypeOverload<>).MakeGenericType(typeof(int));
            Console.WriteLine($"signature overload nongeneric shape => {ValueTaskOverloadCall(plain, false)}/{ValueTaskOverloadCall(plain, true)}");
            Type guid = typeof(SignatureRuntimeArgument<>).MakeGenericType(typeof(Guid));
            Type child = typeof(SignatureValueTaskTypeOverload<>).MakeGenericType(guid);
            Console.WriteLine($"signature overload runtime child difference => {ValueTaskOverloadCall(child, false)}/{ValueTaskOverloadCall(child, true)}");
            Type integer = typeof(SignatureRuntimeArgument<>).MakeGenericType(typeof(int));
            Type family = typeof(SignatureValueTaskTypeOverload<>).MakeGenericType(integer);
            Console.WriteLine($"signature overload runtime family difference => {ValueTaskOverloadCall(family, false)}/{ValueTaskOverloadCall(family, true)}");
            Console.WriteLine("generic family and runtime type overload selection end");
        }

        private static void RunRuntimeArgumentBoundaries()
        {
            Console.WriteLine("== matching runtime generic identity boundaries ==");
            Type known = typeof(SignatureRuntimeKnownBox<>).MakeGenericType(typeof(Guid));
            Type owner = typeof(SignatureRuntimeBoxOverload<>).MakeGenericType(known);
            foreach (bool hard in new bool[] { false, true })
                Console.WriteLine("signature overload generic identity " + (hard ? "hard" : "soft") + " => " + SignatureOutcome(() =>
                    Delegate.CreateDelegate(typeof(SignatureRuntimeGuidBoxProbe), owner, "Matching", false, hard)));
            Console.WriteLine("matching runtime generic identity boundaries end");
        }

        private static string OpaqueInvokeOutcome(MethodInfo method, object[]? arguments)
        {
            try
            {
                return method.Invoke(null, arguments) is null ? "null" : "returned";
            }
            catch (Exception exception)
            {
                return exception.GetType().Name;
            }
        }

        private static void SignatureInvokeProbe(string label, MethodInfo method, object? argument)
        {
            SignatureTarget.NestedArgumentCalls = 0;
            string outcome;
            try
            {
                outcome = method.Invoke(null, new[] { argument })?.ToString() ?? "null";
            }
            catch (Exception exception)
            {
                outcome = exception.GetType().Name;
            }
            Console.WriteLine($"signature invoke {label} => {outcome}/{SignatureTarget.NestedArgumentCalls}");
        }

        internal static unsafe void RunSignatureInvokeDescriptors()
        {
            Console.WriteLine("== runtime function pointer invocation descriptors ==");
            Type template = typeof(SignatureGeneric<>).MakeGenericType(typeof(Guid));
            MethodInfo argument = template.GetMethod("NestedArgument")!;
            SignatureInvokeProbe("string", argument, "wrong");
            SignatureInvokeProbe("boxed integer", argument, 42);
            object pointer = typeof(ReflectInvokeValidationSubset.PointerTarget).GetMethod("Address")!.Invoke(null, null)!;
            SignatureInvokeProbe("int pointer box", argument, pointer);
            SignatureInvokeProbe("IntPtr", argument, (nint)256);
            object box = template.GetMethod("Nested")!.Invoke(null, new object[] { 512 })!;
            Console.WriteLine($"signature invoke nested box => {box is Pointer}/{(nint)Pointer.Unbox(box)}");
            SignatureInvokeProbe("nested box roundtrip", argument, box);
            Console.WriteLine("runtime function pointer invocation descriptors end");
        }

        internal static void RunOpaqueRefSignatures()
        {
            Console.WriteLine("== unsupported referent delegate signatures ==");
            Type owner = typeof(SignatureOpaqueTarget);
            SignaturePair("opaque return to object", typeof(SignatureRefObjectReturn), owner, "TokenReturn");
            SignaturePair("opaque return mismatch", typeof(SignatureRegistrationReturn), owner, "TokenReturn");
            SignaturePair("opaque return opposite", typeof(SignatureTokenReturn), owner, "RegistrationReturn");
            SignaturePair("opaque return exact", typeof(SignatureTokenReturn), owner, "TokenReturn");
            SignaturePair("opaque argument mismatch", typeof(SignatureRegistrationArgument), owner, "TokenArgument");
            SignaturePair("opaque argument opposite", typeof(SignatureTokenArgument), owner, "RegistrationArgument");
            SignaturePair("opaque argument exact", typeof(SignatureTokenArgument), owner, "TokenArgument");
            var alias = (SignatureTokenReturn)Delegate.CreateDelegate(typeof(SignatureTokenReturn), owner, "TokenReturn");
            ref CancellationToken slot = ref alias();
            bool before = slot.IsCancellationRequested;
            slot = new CancellationToken(true);
            Console.WriteLine($"signature opaque typed alias => {before}/{alias().IsCancellationRequested}/{SignatureOpaqueTarget.TokenCanceled()}");
            Console.WriteLine("unsupported referent delegate signatures end");
        }

        private static void NameOutcome(string label, Func<Delegate?> bind)
        {
            try
            {
                Console.WriteLine($"name {label} => {(bind() is null ? "null" : "bound")}");
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"name {label} => {exception.GetType().Name}/{exception.ParamName ?? "-"}");
            }
        }

        internal static unsafe void RunNamedBindings()
        {
            Console.WriteLine("== delegate method name bindings ==");
            NameTarget target = new NameTarget();
            Type unary = typeof(Func<string, string>);
            var instance = (Func<string, string>)Delegate.CreateDelegate(unary, target, "Describe");
            var instanceCase = (Func<string, string>)Delegate.CreateDelegate(unary, target, "describe", true);
            var instanceSoft = (Func<string, string>)Delegate.CreateDelegate(unary, target, "describe", true, false)!;
            Console.WriteLine($"name instance siblings => {instance("a")}|{instanceCase("b")}|{instanceSoft("c")}");
            var stat = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "Twice");
            var statCase = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "tWiCe", true);
            var statSoft = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "Twice", false, false)!;
            Console.WriteLine($"name static siblings => {stat(7)}|{statCase(7)}|{statSoft(7)}");
            Console.WriteLine($"name identity => {ReferenceEquals(instance.Target, target)}/{instance.Method.Name}/{instance == instanceCase}");
            Console.WriteLine($"name static identity => {stat.Target is null}/{stat.Method.Name}");
            var getType = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), target, "gEtTyPe", true);
            var toString = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), target, "ToString");
            Console.WriteLine($"name Object methods => {getType() == typeof(NameTarget)}/{toString()}");
            var referenceEquals = (Func<object, object, bool>)Delegate.CreateDelegate(typeof(Func<object, object, bool>), typeof(NameTarget), "ReferenceEquals");
            Console.WriteLine($"name Object inherited static => {referenceEquals(target, target)}/{referenceEquals(target, new NameTarget())}");
            Console.WriteLine($"name private base => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Secret"))("s")}");
            Console.WriteLine($"name private static base => {((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "StaticSecret"))(7)}");
            Console.WriteLine($"name inherited overload => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Overload"))("o")}");
            Console.WriteLine($"name value overload => {((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), target, "Overload"))(7)}");
            Console.WriteLine($"name case candidate => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "choice", true))("c")}");
            Console.WriteLine($"name nonvirtual before slot => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "slotcase", true))("v")}");
            Console.WriteLine($"name virtual case candidate => {((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new NameVirtualChoices(), "pick", true))()}");
            foreach (object receiver in new object[] { new NameCovariantChild(), new NameCovariantGrandchild() })
            {
                var objectReturn = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), receiver, "Kind");
                var stringReturn = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, "Kind");
                var caseReturn = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, "kind", true);
                Console.WriteLine($"name covariant slots => {objectReturn()}/{stringReturn()}/{caseReturn()}");
            }
            Console.WriteLine($"name unicode => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "éCHO", true))("u")}");
            Console.WriteLine($"name greek => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "σEND", true))("g")}");
            Console.WriteLine($"name ascii casing => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "SHORT", true))("a")}");
            NameOutcome("long s hard", () => Delegate.CreateDelegate(unary, target, "ſHORT", true));
            NameOutcome("long s soft", () => Delegate.CreateDelegate(unary, target, "ſHORT", true, false));
            Console.WriteLine($"name nul suffix => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Describe\0ignored"))("n")}");
            var generic = (Func<string, string>)Delegate.CreateDelegate(unary, new NameGeneric<string>(), "Identity");
            var genericStatic = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameGeneric<int>), "StaticIdentity");
            Console.WriteLine($"name closed generic owner => {generic("closed")}/{genericStatic(42)}");
            Type template = typeof(NameTemplate<>).MakeGenericType(typeof(Guid));
            var templateInstance = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), Activator.CreateInstance(template)!, "Kind");
            var templateStatic = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), template, "StaticKind");
            Console.WriteLine($"name template owner => {templateInstance().Name}/{templateStatic().Name}/{templateInstance.Method.DeclaringType == template}");
            object boxed = new Counter { Value = 41 };
            Console.WriteLine($"name boxed receiver => {((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), boxed, "Bump"))()}");
            int value = 6;
            var byRef = (NameRef)Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "RefValue");
            int result = byRef(ref value);
            Console.WriteLine($"name ref argument => {result}/{value}");
            var byOut = (NameOut)Delegate.CreateDelegate(typeof(NameOut), typeof(NameTarget), "OutValue");
            result = byOut(out value);
            Console.WriteLine($"name out argument => {result}/{value}");
            var refToOut = (NameRef)Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "OutValue");
            result = refToOut(ref value);
            Console.WriteLine($"name ref out signature => {result}/{value}");
            var pointer = (NamePointer)Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "Pointer");
            int pointed = 42;
            Console.WriteLine($"name pointer call => {*pointer(&pointed)}");
            var refReturn = (NameRefReturn)Delegate.CreateDelegate(typeof(NameRefReturn), typeof(NameTarget), "RefReturn");
            ref int alias = ref refReturn(ref value);
            alias = 43;
            Console.WriteLine($"name ref return alias => {value}");

            NameOutcome("missing hard", () => Delegate.CreateDelegate(unary, target, "Missing"));
            NameOutcome("missing soft instance", () => Delegate.CreateDelegate(unary, target, "Missing", false, false));
            NameOutcome("missing soft static", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Missing", false, false));
            NameOutcome("case sensitive instance", () => Delegate.CreateDelegate(unary, target, "describe", false, false));
            NameOutcome("case sensitive static", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "twice", false, false));
            NameOutcome("static via instance", () => Delegate.CreateDelegate(typeof(Func<int, int>), target, "Twice", false, false));
            NameOutcome("instance via type", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Describe", false, false));
            NameOutcome("open instance", () => Delegate.CreateDelegate(typeof(Func<NameTarget, string, string>), typeof(NameTarget), "Describe", false, false));
            NameOutcome("closed static", () => Delegate.CreateDelegate(typeof(Func<string>), target, "NarrowerReturn", false, false));
            NameOutcome("contravariance", () => Delegate.CreateDelegate(typeof(Action<string>), typeof(NameTarget), "WiderArgument", false, false));
            NameOutcome("covariance", () => Delegate.CreateDelegate(typeof(Func<string, object>), typeof(NameTarget), "NarrowerReturn", false, false));
            NameOutcome("return mismatch", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "WiderReturn", false, false));
            NameOutcome("ref value mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "Value", false, false));
            NameOutcome("value ref mismatch", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "RefValue", false, false));
            NameOutcome("ref object mismatch", () => Delegate.CreateDelegate(typeof(NameRefObject), typeof(NameTarget), "ObjectValue", false, false));
            NameOutcome("generic method", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Generic", false, false));
            NameOutcome("pointer signature", () => Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "Pointer", false, false));
            NameOutcome("pointer return mismatch", () => Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "UnsignedPointer", false, false));
            NameOutcome("byref return", () => Delegate.CreateDelegate(typeof(NameRefReturn), typeof(NameTarget), "RefReturn", false, false));
            NameOutcome("byref return mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "RefReturn", false, false));
            NameOutcome("static virtual", () => Delegate.CreateDelegate(typeof(Func<int>), typeof(NameStaticVirtual), "Value", false, false));
            NameOutcome("static abstract", () => Delegate.CreateDelegate(typeof(Func<int>), typeof(NameStaticVirtual), "AbstractValue", false, false));

            NameOutcome("null type first", () => Delegate.CreateDelegate(null!, (object)null!, null!, false, false));
            NameOutcome("null object target", () => Delegate.CreateDelegate(typeof(int), (object)null!, null!, false, false));
            NameOutcome("null type target", () => Delegate.CreateDelegate(typeof(int), (Type)null!, null!, false, false));
            NameOutcome("null instance method", () => Delegate.CreateDelegate(typeof(int), target, null!, false, false));
            NameOutcome("null static method", () => Delegate.CreateDelegate(typeof(int), typeof(NameTarget), null!, false, false));
            NameOutcome("non delegate instance", () => Delegate.CreateDelegate(typeof(int), target, "Missing", false, false));
            NameOutcome("non delegate static", () => Delegate.CreateDelegate(typeof(int), typeof(NameTarget), "Missing", false, false));
            NameOutcome("delegate shell", () => Delegate.CreateDelegate(typeof(Delegate), typeof(NameTarget), "Missing", false, false));
            NameOutcome("multicast shell instance", () => Delegate.CreateDelegate(typeof(MulticastDelegate), target, "Missing", false, false));
            NameOutcome("multicast shell static", () => Delegate.CreateDelegate(typeof(MulticastDelegate), typeof(NameTarget), "Missing", false, false));
            NameOutcome("multicast null target", () => Delegate.CreateDelegate(typeof(MulticastDelegate), (object)null!, null!, false, false));
            NameOutcome("open target first", () => Delegate.CreateDelegate(typeof(int), typeof(NameGeneric<>), "Missing", false, false));
            NameOutcome("open target null method", () => Delegate.CreateDelegate(typeof(int), typeof(NameGeneric<>), null!, false, false));
            NameOutcome("open delegate", () => Delegate.CreateDelegate(typeof(Func<>), typeof(NameTarget), "Missing", false, false));
            NameOutcome("open user delegate", () => Delegate.CreateDelegate(typeof(NameOpen<>), typeof(NameTarget), "Missing", false, false));
            NameOutcome("open delegate method info", () => Delegate.CreateDelegate(typeof(Func<>), typeof(NameTarget).GetMethod("Twice")!, false));
            Console.WriteLine("delegate method name bindings end");
        }

        private static void BoundaryOutcome(string label, Func<Delegate?> bind)
        {
            try
            {
                Console.WriteLine($"name boundary {label} => {(bind() is null ? "null" : "bound")}");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine($"name boundary {label} => unsupported");
            }
        }

        internal static void RunNamedBoundaries()
        {
            Console.WriteLine("== delegate name signature boundaries ==");
            BoundaryOutcome("function return", () => Delegate.CreateDelegate(typeof(NameFunctionReturn), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("function return mismatch", () => Delegate.CreateDelegate(typeof(NameOtherFunctionReturn), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("unrelated function name", () => Delegate.CreateDelegate(typeof(NameFunctionReturn), typeof(NameTarget), "Missing", false, false));
            BoundaryOutcome("function argument mismatch", () => Delegate.CreateDelegate(typeof(NameWrongFunctionArgument), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("MethodInfo function return mismatch", () => Delegate.CreateDelegate(typeof(NameOtherFunctionReturn), typeof(NameTarget).GetMethod("FunctionReturn")!, false));
            BoundaryOutcome("enum underlying", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "EnumIdentity", false, false));
            BoundaryOutcome("MethodInfo enum underlying", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget).GetMethod("EnumIdentity")!, false));
            BoundaryOutcome("MethodInfo ref value mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget).GetMethod("Value")!, false));
            BoundaryOutcome("MethodInfo ref object mismatch", () => Delegate.CreateDelegate(typeof(NameRefObject), typeof(NameTarget).GetMethod("ObjectValue")!, false));
            BoundaryOutcome("own constructor", () => Delegate.CreateDelegate(typeof(Action), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("base constructor", () => Delegate.CreateDelegate(typeof(Action<int>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("constructor query normalization", () => Delegate.CreateDelegate(typeof(Action), new NameTarget(), ".CTOR\0ignored", true, false));
            BoundaryOutcome("constructor return mismatch", () => Delegate.CreateDelegate(typeof(Func<int>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("constructor argument mismatch", () => Delegate.CreateDelegate(typeof(Action<string>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("static initializer", () => Delegate.CreateDelegate(typeof(Action), typeof(NameTarget), ".cctor", false, false));
            BoundaryOutcome("initializer query normalization", () => Delegate.CreateDelegate(typeof(Action), typeof(NameTarget), ".CCTOR\0ignored", true, false));
            BoundaryOutcome("initializer argument mismatch", () => Delegate.CreateDelegate(typeof(Action<string>), typeof(NameTarget), ".cctor", false, false));
            Console.WriteLine("delegate name signature boundaries end");
        }

        internal static void Run()
        {
            Greeter g = new Greeter("hi:");
            MethodInfo miGreet = typeof(Greeter).GetMethod("Greet")!;
            MethodInfo miMul = typeof(Greeter).GetMethod("Mul")!;
            MethodInfo miAdd = typeof(Greeter).GetMethod("AddOne")!;

            // Closed instance: CreateDelegate(Type, target).
            var closed = (Func<string, string>)miGreet.CreateDelegate(typeof(Func<string, string>), g);
            Console.WriteLine($"dg-closed => {closed("bob")}");

            // Generic closed instance: CreateDelegate<T>(target).
            var closedT = miGreet.CreateDelegate<Func<string, string>>(g);
            Console.WriteLine($"dg-closed-generic => {closedT("gen")}");

            // Open static: CreateDelegate(Type) / CreateDelegate<T>().
            var addOne = (Func<int, int>)miAdd.CreateDelegate(typeof(Func<int, int>));
            Console.WriteLine($"dg-open-static => {addOne(41)}");
            var addOneT = miAdd.CreateDelegate<Func<int, int>>();
            Console.WriteLine($"dg-open-static-generic => {addOneT(9)}");

            // Open instance: the delegate's first argument is the receiver
            // (Delegate.CreateDelegate with a null firstArgument).
            var open = (Func<Greeter, string, string>)Delegate.CreateDelegate(
                typeof(Func<Greeter, string, string>), null, miGreet)!;
            Console.WriteLine($"dg-open-instance => {open(g, "eve")}");

            // Closed static: the explicit firstArgument becomes the method's
            // first (reference-typed) parameter; a null firstArgument binds too.
            MethodInfo miShout = typeof(Greeter).GetMethod("Shout")!;
            var closedStatic = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), "yo", miShout)!;
            Console.WriteLine($"dg-closed-static => {closedStatic(3)}");
            var closedStaticNull = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), null, miShout)!;
            Console.WriteLine($"dg-closed-static-null => {closedStaticNull(4)}");
            // A value-typed first parameter cannot be first-arg-bound (.NET
            // stores the bound object unconverted).
            try
            {
                Delegate.CreateDelegate(typeof(Func<int>), 41, miAdd);
                Console.WriteLine("dg-closed-static-value => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-closed-static-value => ArgumentException");
            }

            // Delegate.CreateDelegate(Type, MethodInfo): the plain open form.
            var openStatic2 = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), miAdd);
            Console.WriteLine($"dg-static-form => {openStatic2(10)}");

            // Value-type arguments box per call through the trampoline.
            var mul = (Func<int, int, int>)miMul.CreateDelegate(typeof(Func<int, int, int>), g);
            Console.WriteLine($"dg-value-args => {mul(6, 7)}");

            // Value-type receiver: closed over a boxed struct.
            object boxed = new Counter { Value = 5 };
            var bump = (Func<int>)typeof(Counter).GetMethod("Bump")!.CreateDelegate(typeof(Func<int>), boxed);
            Console.WriteLine($"dg-struct-receiver => {bump()}");

            // Argument contravariance (delegate string -> method object) and
            // return covariance (method object -> delegate object over string).
            var contra = (Action<string>)typeof(Greeter).GetMethod("Note")!
                .CreateDelegate(typeof(Action<string>), g);
            contra("c");
            Console.WriteLine("dg-contravariant => ok");
            var co = (Func<string, object>)typeof(Greeter).GetMethod("Fetch")!
                .CreateDelegate(typeof(Func<string, object>), g);
            Console.WriteLine($"dg-covariant => {co("v")}");

            // Identity: Target unwraps to the bound receiver, Method reports the
            // bound MethodInfo, and two equal bindings compare equal.
            Console.WriteLine($"dg-target => {ReferenceEquals(closed.Target, g)}");
            Console.WriteLine($"dg-method => {closed.Method.Name}");
            var closed2 = (Func<string, string>)miGreet.CreateDelegate(typeof(Func<string, string>), g);
            Console.WriteLine($"dg-equal => {closed == closed2}");
            Console.WriteLine($"dg-not-equal => {closed == closedT && (Delegate)closed == (Delegate)addOne}");

            // Multicast: reflection-bound delegates chain like any other, and
            // HasSingleTarget sees the chain length.
            var multi = closed + closed2;
            Console.WriteLine($"dg-multicast => {multi("m")}");
            Console.WriteLine($"dg-single-target => {closed.HasSingleTarget}|{multi.HasSingleTarget}");

            // Bind failures: signature mismatch -> ArgumentException; the
            // no-target form over an instance method -> ArgumentException;
            // a static method with full arity + target -> ArgumentException;
            // throwOnBindFailure: false -> null.
            try
            {
                miGreet.CreateDelegate(typeof(Func<int, int>), g);
                Console.WriteLine("dg-mismatch => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-mismatch => ArgumentException");
            }
            try
            {
                miGreet.CreateDelegate(typeof(Func<string, string>));
                Console.WriteLine("dg-instance-open => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-instance-open => ArgumentException");
            }
            try
            {
                miAdd.CreateDelegate(typeof(Func<int, int>), g);
                Console.WriteLine("dg-static-target => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-static-target => ArgumentException");
            }
            Delegate? soft = Delegate.CreateDelegate(typeof(Func<int, int>), miGreet, false);
            Console.WriteLine($"dg-soft-fail => {(soft is null ? "null" : "bound")}");
        }
    }
}

namespace ReflectDelegateEnumSubset
{
    enum S8 : sbyte { }
    enum S8Other : sbyte { }
    enum U8 : byte { }
    enum U8Other : byte { }
    enum S16 : short { }
    enum S16Other : short { }
    enum U16 : ushort { }
    enum U16Other : ushort { }
    enum S32 : int { }
    enum S32Other : int { }
    enum U32 : uint { }
    enum U32Other : uint { }
    enum S64 : long { }
    enum S64Other : long { }
    enum U64 : ulong { }
    enum U64Other : ulong { }

    delegate void RefInt(ref int value);
    delegate void RefEnum(ref S32 value);
    delegate void OutInt(out int value);
    unsafe delegate int* PointerInt(int* value);
    unsafe delegate uint* PointerUInt(uint* value);
    delegate ref int RefIntReturn();
    unsafe delegate int FunctionInt(delegate*<int, int> value);

    unsafe class EnumTarget
    {
        internal static long Signed;
        internal static ulong Unsigned;
        internal static int Calls;
        private static S32 cell;

        public static S8 EnumS8(S8 value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static sbyte PrimitiveS8(sbyte value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static U8 EnumU8(U8 value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static byte PrimitiveU8(byte value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static S16 EnumS16(S16 value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static short PrimitiveS16(short value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static U16 EnumU16(U16 value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static ushort PrimitiveU16(ushort value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static S32 EnumS32(S32 value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static int PrimitiveS32(int value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static U32 EnumU32(U32 value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static uint PrimitiveU32(uint value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static S64 EnumS64(S64 value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static long PrimitiveS64(long value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static U64 EnumU64(U64 value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public static ulong PrimitiveU64(ulong value)
        {
            Unsigned = (ulong)value;
            Calls++;
            return value;
        }

        public S8 Instance(S8 value)
        {
            Signed = (long)value;
            Calls++;
            return value;
        }

        public static U16 Closed(string prefix, U16 value)
        {
            Unsigned = (ulong)value + (ulong)prefix.Length;
            Calls++;
            return value;
        }

        public static void RefEnum(ref S32 value) => value = (S32)53;
        public static void RefInt(ref int value) => value = 59;
        public static S32* Pointer(S32* value) => value;
        public static ref S32 RefReturn() => ref cell;
        public static int Function(delegate*<S32, int> value) => 19;
        public static S32 BoundValue(S32 value) => value;
    }

    class EnumRuntimeOwner<T>
    {
        public static U32 Enum(U32 value)
        {
            EnumTarget.Unsigned = (ulong)value;
            EnumTarget.Calls++;
            return value;
        }
    }

    static class Program
    {
        private static void Probe<T>(string label, string method, T value, bool unsigned, object? target = null)
        {
            foreach (bool named in new bool[] { true, false })
            {
                Type delegateType = typeof(Func<T, T>);
                MethodInfo mi = typeof(EnumTarget).GetMethod(method)!;
                Delegate? bound = named
                    ? target is null
                        ? Delegate.CreateDelegate(delegateType, typeof(EnumTarget), method, false, false)
                        : Delegate.CreateDelegate(delegateType, target, method, false, false)
                    : target is null
                        ? Delegate.CreateDelegate(delegateType, mi, false)
                        : Delegate.CreateDelegate(delegateType, target, mi, false);
                int count = EnumTarget.Calls;
                T result = ((Func<T, T>)bound!)(value);
                string observed = unsigned ? EnumTarget.Unsigned.ToString() : EnumTarget.Signed.ToString();
                Console.WriteLine($"enum {label} {(named ? "name" : "method")} => {result}/{observed}/{EnumTarget.Calls - count}/{System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, result)}");
            }
        }

        private static void Bind(string label, Type delegateType, string method)
        {
            foreach (bool named in new bool[] { true, false })
            {
                Delegate? bound = named
                    ? Delegate.CreateDelegate(delegateType, typeof(EnumTarget), method, false, false)
                    : Delegate.CreateDelegate(delegateType, typeof(EnumTarget).GetMethod(method)!, false);
                Console.WriteLine($"enum {label} {(named ? "name" : "method")} => {(bound is null ? "null" : "bound")}");
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== delegate enum signature compatibility ==");
            Probe("S8 underlying to enum", "EnumS8", (sbyte)(-113), false);
            Probe("S8 enum to underlying", "PrimitiveS8", (S8)(-113), false);
            Probe("S8 distinct enums", "EnumS8", (S8Other)(-113), false);
            Probe("U8 underlying to enum", "EnumU8", (byte)(227), true);
            Probe("U8 enum to underlying", "PrimitiveU8", (U8)(227), true);
            Probe("U8 distinct enums", "EnumU8", (U8Other)(227), true);
            Probe("S16 underlying to enum", "EnumS16", (short)(-30001), false);
            Probe("S16 enum to underlying", "PrimitiveS16", (S16)(-30001), false);
            Probe("S16 distinct enums", "EnumS16", (S16Other)(-30001), false);
            Probe("U16 underlying to enum", "EnumU16", (ushort)(60001), true);
            Probe("U16 enum to underlying", "PrimitiveU16", (U16)(60001), true);
            Probe("U16 distinct enums", "EnumU16", (U16Other)(60001), true);
            Probe("S32 underlying to enum", "EnumS32", (int)(-2000000001), false);
            Probe("S32 enum to underlying", "PrimitiveS32", (S32)(-2000000001), false);
            Probe("S32 distinct enums", "EnumS32", (S32Other)(-2000000001), false);
            Probe("U32 underlying to enum", "EnumU32", (uint)(4045620583U), true);
            Probe("U32 enum to underlying", "PrimitiveU32", (U32)(4045620583U), true);
            Probe("U32 distinct enums", "EnumU32", (U32Other)(4045620583U), true);
            Probe("S64 underlying to enum", "EnumS64", (long)(-8000000000000000001L), false);
            Probe("S64 enum to underlying", "PrimitiveS64", (S64)(-8000000000000000001L), false);
            Probe("S64 distinct enums", "EnumS64", (S64Other)(-8000000000000000001L), false);
            Probe("U64 underlying to enum", "EnumU64", (ulong)(17293822569102704641UL), true);
            Probe("U64 enum to underlying", "PrimitiveU64", (U64)(17293822569102704641UL), true);
            Probe("U64 distinct enums", "EnumU64", (U64Other)(17293822569102704641UL), true);
            Probe("closed instance", "Instance", (sbyte)-113, false, new EnumTarget());
            MethodInfo byteMethod = typeof(EnumTarget).GetMethod("EnumU8")!;
            var byteTarget = (Func<byte, byte>)byteMethod.CreateDelegate(typeof(Func<byte, byte>));
            EnumTarget.Calls = 0;
            byte byteValue = byteTarget(227);
            Console.WriteLine($"enum MethodInfo type => {byteValue}/{EnumTarget.Unsigned}/{EnumTarget.Calls}");
            var ushortTarget = typeof(EnumTarget).GetMethod("EnumU16")!.CreateDelegate<Func<ushort, ushort>>();
            EnumTarget.Calls = 0;
            ushort ushortValue = ushortTarget(60001);
            Console.WriteLine($"enum MethodInfo generic => {ushortValue}/{EnumTarget.Unsigned}/{EnumTarget.Calls}");
            Type clone = typeof(EnumRuntimeOwner<>).MakeGenericType(typeof(Guid));
            MethodInfo cloneMethod = clone.GetMethod("Enum")!;
            foreach (bool named in new bool[] { true, false })
            {
                var cloneTarget = (Func<uint, uint>)(named
                    ? Delegate.CreateDelegate(typeof(Func<uint, uint>), clone, "Enum", false, false)
                    : Delegate.CreateDelegate(typeof(Func<uint, uint>), cloneMethod, false))!;
                EnumTarget.Calls = 0;
                uint cloneValue = cloneTarget(4045620583U);
                Console.WriteLine($"enum runtime clone {(named ? "name" : "method")} => {cloneValue}/{EnumTarget.Unsigned}/{EnumTarget.Calls}");
            }
            Bind("signedness", typeof(Func<byte, byte>), "EnumS8");
            Bind("width", typeof(Func<short, short>), "EnumS8");
            Bind("bool", typeof(Func<bool, bool>), "EnumU8");
            Bind("char", typeof(Func<char, char>), "EnumU16");
            Bind("native int", typeof(Func<nint, nint>), "EnumS64");
            Bind("ordinary widening", typeof(Func<int, long>), "PrimitiveS64");
            Bind("ref enum to int", typeof(RefInt), "RefEnum");
            Bind("ref int to enum", typeof(RefEnum), "RefInt");
            Bind("out enum to int", typeof(OutInt), "RefEnum");
            Bind("pointer enum to int", typeof(PointerInt), "Pointer");
            Bind("pointer enum to uint", typeof(PointerUInt), "Pointer");
            Bind("ref return enum", typeof(RefIntReturn), "RefReturn");
            Bind("function identity", typeof(FunctionInt), "Function");
            MethodInfo closed = typeof(EnumTarget).GetMethod("Closed")!;
            var target = (Func<ushort, ushort>)Delegate.CreateDelegate(typeof(Func<ushort, ushort>), "abc", closed)!;
            EnumTarget.Calls = 0;
            ushort returned = target(60001);
            Console.WriteLine($"enum closed static => {returned}/{EnumTarget.Unsigned}/{EnumTarget.Calls}");
            Delegate? valueBound = Delegate.CreateDelegate(typeof(Func<S32>), (S32)41, typeof(EnumTarget).GetMethod("BoundValue")!, false);
            Console.WriteLine($"enum closed static value => {(valueBound is null ? "null" : "bound")}");
            Console.WriteLine("delegate enum signature compatibility end");
        }
    }
}

namespace ReflectIntrinsicPointerSubset
{
    public unsafe class PointerOverloadOwner<T> where T : unmanaged
    {
        public static CancellationToken* Pointer(CancellationToken* value)
        {
            Target.Calls += 7;
            Target.Seen = (nint)value;
            return value;
        }

        public static T* Pointer(T* value)
        {
            Target.Calls += 8;
            Target.Seen = (nint)value;
            return value;
        }
    }

    public unsafe class PointerOwner<T>
    {
        public static CancellationToken* Token(CancellationToken* value) => Target.Token(value);
    }

    public unsafe delegate CancellationToken* TokenPointer(CancellationToken* value);
    public unsafe delegate object* ObjectPointer(object* value);
    public unsafe delegate CancellationTokenRegistration* RegistrationPointer(CancellationTokenRegistration* value);
    public unsafe delegate int TokenArgument(CancellationToken* value);
    public unsafe delegate int ObjectArgument(object* value);
    public unsafe delegate int RegistrationArgument(CancellationTokenRegistration* value);
    public unsafe delegate CancellationToken* TokenResult();
    public unsafe delegate object* ObjectResult();
    public unsafe delegate CancellationTokenRegistration* RegistrationResult();
    public unsafe delegate CancellationToken***** DeepToken(CancellationToken***** value);
    public unsafe delegate CancellationTokenRegistration***** DeepRegistration(CancellationTokenRegistration***** value);
    public unsafe delegate CancellationToken****** DeeperToken(CancellationToken****** value);
    public unsafe delegate int* IntPointer(int* value);
    public unsafe delegate uint* UIntPointer(uint* value);
    public unsafe delegate long* LongPointer(long* value);
    public unsafe delegate int** DoubleIntPointer(int** value);
    public unsafe delegate object* StringArgument(string* value);
    public unsafe delegate object* ReferencePointer(object* value);

    public static unsafe class Target
    {
        public static int Calls;
        public static nint Seen;

        public static CancellationToken* Token(CancellationToken* value)
        {
            Calls++;
            Seen = (nint)value;
            return value;
        }

        public static int Parameter(CancellationToken* value)
        {
            Calls++;
            Seen = (nint)value;
            return 7;
        }

        public static CancellationToken* Result()
        {
            Calls++;
            return (CancellationToken*)0x1234;
        }

        public static CancellationToken***** Deep(CancellationToken***** value)
        {
            Calls++;
            Seen = (nint)value;
            return value;
        }

        public static int* Int32(int* value)
        {
            Calls++;
            return value;
        }

        public static object* Reference(object* value)
        {
            Calls++;
            return value;
        }
    }

    public static unsafe class Program
    {
        private static Delegate? Bind(Type delegateType, string name, bool named)
        {
            return named
                ? Delegate.CreateDelegate(delegateType, typeof(Target), name, false, false)
                : Delegate.CreateDelegate(delegateType, typeof(Target).GetMethod(name)!, false);
        }

        private static void Outcome(string label, Type delegateType, string method, bool named)
        {
            int count = Target.Calls;
            try
            {
                Delegate? result = Bind(delegateType, method, named);
                Console.WriteLine($"bind {label} {(named ? "name" : "method")} => {(result is null ? "null" : "bound")}/calls={Target.Calls - count}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"bind {label} {(named ? "name" : "method")} => {e.GetType().Name}/calls={Target.Calls - count}");
            }
        }

        private static void MatchingCalls(bool named)
        {
            string api = named ? "name" : "method";
            Target.Calls = 0;
            var token = (TokenPointer)Bind(typeof(TokenPointer), "Token", named)!;
            CancellationToken* zero = token(null);
            CancellationToken* value = token((CancellationToken*)0x1234);
            Console.WriteLine($"call Token {api} => {zero == null}/{(nint)value}/{Target.Seen}/{Target.Calls}");
            Target.Calls = 0;
            var parameter = (TokenArgument)Bind(typeof(TokenArgument), "Parameter", named)!;
            int result = parameter(null);
            Console.WriteLine($"call parameter {api} => {result}/{Target.Seen}/{Target.Calls}");
            Target.Calls = 0;
            var returned = (TokenResult)Bind(typeof(TokenResult), "Result", named)!;
            Console.WriteLine($"call return {api} => {(nint)returned()}/{Target.Calls}");
            Target.Calls = 0;
            var deep = (DeepToken)Bind(typeof(DeepToken), "Deep", named)!;
            Console.WriteLine($"call deep {api} => {(nint)deep((CancellationToken*****)0x1234)}/{Target.Seen}/{Target.Calls}");
            Target.Calls = 0;
            var ordinary = (IntPointer)Bind(typeof(IntPointer), "Int32", named)!;
            Console.WriteLine($"call ordinary {api} => {(nint)ordinary((int*)0x1234)}/{Target.Calls}");
        }

        public static void Run()
        {
            Console.WriteLine("== intrinsic pointer identity actual ==");
            foreach (bool named in new[] { true, false })
            {
                Outcome("Token matching", typeof(TokenPointer), "Token", named);
                Outcome("Token object", typeof(ObjectPointer), "Token", named);
                Outcome("Token registration", typeof(RegistrationPointer), "Token", named);
                Outcome("parameter matching", typeof(TokenArgument), "Parameter", named);
                Outcome("parameter object", typeof(ObjectArgument), "Parameter", named);
                Outcome("parameter registration", typeof(RegistrationArgument), "Parameter", named);
                Outcome("return matching", typeof(TokenResult), "Result", named);
                Outcome("return object", typeof(ObjectResult), "Result", named);
                Outcome("return registration", typeof(RegistrationResult), "Result", named);
                Outcome("deep matching", typeof(DeepToken), "Deep", named);
                Outcome("deep registration", typeof(DeepRegistration), "Deep", named);
                Outcome("deep depth", typeof(DeeperToken), "Deep", named);
                Outcome("ordinary matching", typeof(IntPointer), "Int32", named);
                Outcome("ordinary leaf", typeof(LongPointer), "Int32", named);
                Outcome("ordinary depth", typeof(DoubleIntPointer), "Int32", named);
                Outcome("primitive relaxation", typeof(UIntPointer), "Int32", named);
                Outcome("reference relaxation", typeof(StringArgument), "Reference", named);
                MatchingCalls(named);
            }
            Target.Calls = 0;
            object pointer = typeof(Target).GetMethod("Result")!.Invoke(null, null)!;
            Console.WriteLine($"Invoke return => {pointer is Pointer}/{(nint)Pointer.Unbox(pointer)}/{Target.Calls}");
            Target.Calls = 0;
            try
            {
                typeof(Target).GetMethod("Parameter")!.Invoke(null, new object?[] { "wrong" });
                Console.WriteLine($"Invoke wrong argument => ran/{Target.Calls}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Invoke wrong argument => {e.GetType().Name}/{Target.Calls}");
            }
            Type clone = typeof(PointerOwner<>).MakeGenericType(typeof(Guid));
            foreach (bool named in new[] { true, false })
            {
                Target.Calls = 0;
                Delegate? incompatible = named
                    ? Delegate.CreateDelegate(typeof(ObjectPointer), clone, "Token", false, false)
                    : Delegate.CreateDelegate(typeof(ObjectPointer), clone.GetMethod("Token")!, false);
                Console.WriteLine($"bind clone object {(named ? "name" : "method")} => {(incompatible is null ? "null" : "bound")}/calls={Target.Calls}");
                var matching = (TokenPointer)(named
                    ? Delegate.CreateDelegate(typeof(TokenPointer), clone, "Token", false, false)
                    : Delegate.CreateDelegate(typeof(TokenPointer), clone.GetMethod("Token")!, false))!;
                Console.WriteLine($"call clone {(named ? "name" : "method")} => {(nint)matching((CancellationToken*)0x1234)}/{Target.Seen}/{Target.Calls}");
            }
            Console.WriteLine("intrinsic pointer identity actual end");
        }

        public static void RunOverloads()
        {
            Console.WriteLine("== intrinsic pointer overload identity ==");
            Type owner = typeof(PointerOverloadOwner<>).MakeGenericType(typeof(Guid));
            foreach (bool throwing in new[] { false, true })
            {
                Target.Calls = 0;
                Target.Seen = 0;
                var matching = (TokenPointer)Delegate.CreateDelegate(
                    typeof(TokenPointer), owner, "Pointer", false, throwing)!;
                CancellationToken* zero = matching(null);
                CancellationToken* returned = matching((CancellationToken*)0x1234);
                Console.WriteLine($"call pointer overload {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{Target.Seen}/{Target.Calls}");
            }
            Console.WriteLine("intrinsic pointer overload identity end");
        }

        public static void RunFamilies()
        {
            Console.WriteLine("== intrinsic pointer family identity ==");
            Console.WriteLine($"pointer family => {typeof(ValueTask<>).Name}");
            Type owner = typeof(PointerFamilyOwner<>).MakeGenericType(typeof(Guid));
            foreach (bool throwing in new[] { false, true })
            {
                Target.Calls = 0;
                Target.Seen = 0;
                var matching = (TokenPointer)Delegate.CreateDelegate(
                    typeof(TokenPointer), owner, "Pointer", false, throwing)!;
                CancellationToken* zero = matching(null);
                CancellationToken* returned = matching((CancellationToken*)0x1234);
                Console.WriteLine($"call pointer family {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{Target.Seen}/{Target.Calls}");
            }
            Console.WriteLine("intrinsic pointer family identity end");
        }

        public static void RunGenericPointees()
        {
            Console.WriteLine("== intrinsic generic pointer identity ==");
            Type owner = typeof(GenericPointeeOwner<>).MakeGenericType(typeof(Guid));
            int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
            foreach (bool throwing in new[] { false, true })
            {
                OpaqueSignals.Fixed = 0;
                OpaqueSignals.Dependent = 0;
                OpaqueSignals.Seen = 0;
                var matching = (TaskPointer)Delegate.CreateDelegate(
                    typeof(TaskPointer), owner, "Pointer", false, throwing)!;
                if (matching.Method.MetadataToken != fixedToken)
                    throw new InvalidOperationException("Wrong generic pointee overload");
                ValueTask<int>* zero = matching(null);
                ValueTask<int>* returned = matching((ValueTask<int>*)0x1234);
                Console.WriteLine($"call pointer generic {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{OpaqueSignals.Seen}/{OpaqueSignals.Fixed}/{OpaqueSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
            }
            Console.WriteLine("intrinsic generic pointer identity end");
        }

        public static void RunArrayArguments()
        {
            Console.WriteLine("== intrinsic pointer array argument identity ==");
            Type array = Array.CreateInstance(typeof(ArrayArgumentElement), 0).GetType();
            Console.WriteLine($"pointer dynamic array => {array.GetArrayRank()}/{array.GetElementType() == typeof(ArrayArgumentElement)}");
            Type owner = typeof(ArrayArgumentOwner<>).MakeGenericType(array);
            int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
            foreach (bool throwing in new[] { false, true })
            {
                OpaqueSignals.Fixed = 0;
                OpaqueSignals.Dependent = 0;
                OpaqueSignals.Seen = 0;
                var matching = (TokenPointer)Delegate.CreateDelegate(
                    typeof(TokenPointer), owner, "Pointer", false, throwing)!;
                if (matching.Method.MetadataToken != fixedToken)
                    throw new InvalidOperationException("Wrong array argument overload");
                CancellationToken* zero = matching(null);
                CancellationToken* returned = matching((CancellationToken*)0x1234);
                Console.WriteLine($"call pointer array {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{OpaqueSignals.Seen}/{OpaqueSignals.Fixed}/{OpaqueSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
            }
            Console.WriteLine("intrinsic pointer array argument identity end");
        }

        public static void RunConstantArguments()
        {
            Console.WriteLine("== intrinsic pointer constant argument identity ==");
            Console.WriteLine($"pointer nested families => {typeof(ValueTask<>).Name}/{typeof(Tuple<,>).Name}");
            Type owner = typeof(ConstantArgumentOwner<>).MakeGenericType(typeof(Guid));
            int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
            foreach (bool throwing in new[] { false, true })
            {
                OpaqueSignals.Fixed = 0;
                OpaqueSignals.Dependent = 0;
                OpaqueSignals.Seen = 0;
                var matching = (NestedTaskPointer)Delegate.CreateDelegate(
                    typeof(NestedTaskPointer), owner, "Pointer", false, throwing)!;
                if (matching.Method.MetadataToken != fixedToken)
                    throw new InvalidOperationException("Wrong constant overload");
                ValueTask<Tuple<Guid, long>>* zero = matching(null);
                ValueTask<Tuple<Guid, long>>* returned = matching((ValueTask<Tuple<Guid, long>>*)0x1234);
                Console.WriteLine($"call pointer constant argument {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{OpaqueSignals.Seen}/{OpaqueSignals.Fixed}/{OpaqueSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
            }
            Console.WriteLine("intrinsic pointer constant argument identity end");
        }

        public static void RunArrayChildren()
        {
            Console.WriteLine("== intrinsic pointer array child identity ==");
            Type owner = typeof(ArrayChildOwner<>).MakeGenericType(typeof(Guid));
            int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
            foreach (bool throwing in new[] { false, true })
            {
                OpaqueSignals.Fixed = 0;
                OpaqueSignals.Dependent = 0;
                OpaqueSignals.Seen = 0;
                var matching = (ArrayTaskPointer)Delegate.CreateDelegate(
                    typeof(ArrayTaskPointer), owner, "Pointer", false, throwing)!;
                if (matching.Method.MetadataToken != fixedToken)
                    throw new InvalidOperationException("Wrong array child overload");
                ValueTask<int[]>* zero = matching(null);
                ValueTask<int[]>* returned = matching((ValueTask<int[]>*)0x1234);
                Console.WriteLine($"call pointer array child {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{OpaqueSignals.Seen}/{OpaqueSignals.Fixed}/{OpaqueSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
            }
            Console.WriteLine("intrinsic pointer array child identity end");
        }

        public static void RunMdArrayChildren()
        {
            Console.WriteLine("== intrinsic pointer MD array child identity ==");
            Type owner = typeof(MdArrayChildOwner<>).MakeGenericType(typeof(Guid));
            int fixedToken = owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)[0].MetadataToken;
            foreach (bool throwing in new[] { false, true })
            {
                OpaqueSignals.Fixed = 0;
                OpaqueSignals.Dependent = 0;
                OpaqueSignals.Seen = 0;
                var matching = (MdArrayTaskPointer)Delegate.CreateDelegate(
                    typeof(MdArrayTaskPointer), owner, "Pointer", false, throwing)!;
                if (matching.Method.MetadataToken != fixedToken)
                    throw new InvalidOperationException("Wrong MD array child overload");
                ValueTask<int[,]>* zero = matching(null);
                ValueTask<int[,]>* returned = matching((ValueTask<int[,]>*)0x1234);
                Console.WriteLine($"call pointer MD array child {(throwing ? "hard" : "soft")} => {zero == null}/{(nint)returned}/{OpaqueSignals.Seen}/{OpaqueSignals.Fixed}/{OpaqueSignals.Dependent}/{matching.Method.MetadataToken == fixedToken}");
            }
            Console.WriteLine("intrinsic pointer MD array child identity end");
        }
    }

    public unsafe class PointerFamilyOwner<T>
    {
        public static CancellationToken* Pointer(CancellationToken* value)
        {
            Target.Calls += 7;
            Target.Seen = (nint)value;
            return value;
        }

        public static ValueTask<T>* Pointer(ValueTask<T>* value)
        {
            Target.Calls += 8;
            Target.Seen = (nint)value;
            return value;
        }
    }

    public unsafe delegate ValueTask<int>* TaskPointer(ValueTask<int>* value);

    public static class OpaqueSignals
    {
        public static int Fixed;
        public static int Dependent;
        public static nint Seen;
    }

    public unsafe class GenericPointeeOwner<T> where T : unmanaged
    {
        public static ValueTask<int>* Pointer(ValueTask<int>* value)
        {
            OpaqueSignals.Fixed += 7;
            OpaqueSignals.Seen = (nint)value;
            return value;
        }

        public static T* Pointer(T* value)
        {
            OpaqueSignals.Dependent += 8;
            return value;
        }
    }

    public class ArrayArgumentElement
    {
    }

    public unsafe class ArrayArgumentOwner<T>
    {
        public static CancellationToken* Pointer(CancellationToken* value)
        {
            OpaqueSignals.Fixed += 7;
            OpaqueSignals.Seen = (nint)value;
            return value;
        }

        public static T* Pointer(T* value)
        {
            OpaqueSignals.Dependent += 8;
            return value;
        }
    }

    public unsafe delegate ValueTask<Tuple<Guid, long>>* NestedTaskPointer(ValueTask<Tuple<Guid, long>>* value);

    public unsafe class ConstantArgumentOwner<T>
    {
        public static ValueTask<Tuple<Guid, long>>* Pointer(ValueTask<Tuple<Guid, long>>* value)
        {
            OpaqueSignals.Fixed += 7;
            OpaqueSignals.Seen = (nint)value;
            return value;
        }

        public static ValueTask<Tuple<T, int>>* Pointer(ValueTask<Tuple<T, int>>* value)
        {
            OpaqueSignals.Dependent += 8;
            return value;
        }
    }

    public unsafe delegate ValueTask<int[]>* ArrayTaskPointer(ValueTask<int[]>* value);

    public unsafe class ArrayChildOwner<T>
    {
        public static ValueTask<int[]>* Pointer(ValueTask<int[]>* value)
        {
            OpaqueSignals.Fixed += 7;
            OpaqueSignals.Seen = (nint)value;
            return value;
        }

        public static ValueTask<T>* Pointer(ValueTask<T>* value)
        {
            OpaqueSignals.Dependent += 8;
            return value;
        }
    }

    public unsafe delegate ValueTask<int[,]>* MdArrayTaskPointer(ValueTask<int[,]>* value);

    public unsafe class MdArrayChildOwner<T>
    {
        public static ValueTask<int[,]>* Pointer(ValueTask<int[,]>* value)
        {
            OpaqueSignals.Fixed += 7;
            OpaqueSignals.Seen = (nint)value;
            return value;
        }

        public static ValueTask<T>* Pointer(ValueTask<T>* value)
        {
            OpaqueSignals.Dependent += 8;
            return value;
        }
    }
}
