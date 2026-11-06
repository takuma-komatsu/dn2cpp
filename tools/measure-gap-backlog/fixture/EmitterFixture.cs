using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length != 3) throw new ArgumentException("Transpiler assembly, EmitterInput assembly and fresh output directory are required");
string output = Path.GetFullPath(args[2]);
if (Directory.Exists(output)) throw new IOException("Output directory must be new");
Directory.CreateDirectory(output);
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
Type T(string name) => assembly.GetType("Dn2Cpp." + name, true)!;
object Get(object value, string name) => value.GetType().GetField(name, all)?.GetValue(value)
    ?? value.GetType().GetProperty(name, all)!.GetValue(value)!;
void Set(object value, string name, object? next)
{
    if (value.GetType().GetField(name, all) is { } field) field.SetValue(value, next);
    else value.GetType().GetProperty(name, all)!.SetValue(value, next);
}
object? Call(object value, string name, params object?[] arguments) => value.GetType().GetMethod(name, all)!.Invoke(value, arguments);
object Copy(object value) => typeof(object).GetMethod("MemberwiseClone", all)!.Invoke(value, null)!;
IList NewList(Type element) => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
int checks = 0;
void Assert(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
object Model(bool shared = false)
{
    var options = Activator.CreateInstance(T("TranspileOptions"))!;
    Set(options, "Measure", true); Set(options, "SharedGenerics", shared);
    Set(options, "MaxDegreeOfParallelism", 1); Set(options, "IsaSurfaceDump", "fixture");
    return T("Compilation").GetMethod("Create", all)!.Invoke(null, new object[] { new[] { Path.GetFullPath(args[1]) }, options })!;
}
object Method(object model, string owner, string name, int parameters = -1)
{
    var type = ((IEnumerable)Get(model, "Classes")).Cast<object>().Single(c => (string)Get(c, "FullName") == owner);
    return ((IEnumerable)Get(type, "Methods")).Cast<object>().Single(m => (string)Get(m, "Name") == name
        && (parameters < 0 || (int)Get(Get(Get(m, "Signature"), "ParameterTypes"), "Length") == parameters));
}
object Owner(object model, string name)
{
    var owner = Activator.CreateInstance(T("ClassInfo"))!;
    int dot = name.LastIndexOf('.');
    Set(owner, "Namespace", dot < 0 ? "" : name[..dot]); Set(owner, "Name", name[(dot + 1)..]);
    Set(owner, "Module", Get(model, "AppModule"));
    return owner;
}
object Clone(object seed, object owner, string name)
{
    var method = Copy(seed);
    _ = Get(method, "Signature");
    Set(method, "DeclaringClass", owner); Set(method, "Name", name); Set(method, "_cppName", null);
    return method;
}
object Emitter(object model) => Activator.CreateInstance(T("CppEmitter"), model, Activator.CreateInstance(T("ConsoleBackend")), false)!;
void Add(object model, object method) => Call(Get(model, "Reachable"), "Add", method);
object Failure(object model, string kind)
{
    var good = Method(model, "EmitterInput", "Good");
    if (kind == "http") return Clone(good, Owner(model, "System.Net.Http.HttpClientHandler"), "get_ClientCertificateOptions");
    if (kind == "enum") return Clone(Method(model, "EmitterInput", "Three"), Owner(model, "System.Enum"), "ToString");
    if (kind == "fallback") return Clone(Method(model, "EmitterInput", "Bad"), Owner(model, "System.String"), "UnmodeledBody");
    var owner = Owner(model, "Test.Structural"); Set(owner, "IsValueType", true);
    var field = Activator.CreateInstance(T("FieldInfo"))!;
    Set(field, "DeclaringClass", owner); Set(field, "Name", "Open");
    var variable = Activator.CreateInstance(T("TypeDesc"))!;
    Set(variable, "Kind", Enum.Parse(T("TypeKind"), "GenericVar")); Set(field, "Type", variable);
    ((IList)Get(owner, "Fields")).Add(field);
    var synthetic = Clone(good, owner, "Equals"); Set(synthetic, "IsSynthetic", true);
    ((IList)Get(model, "_synthesizedValueBodies")).Add(synthetic);
    return synthetic;
}
object Measure(object model, string name)
{
    var result = Call(Emitter(model), "MeasureGaps")!;
    string directory = Path.Combine(output, name); Directory.CreateDirectory(directory);
    T("MeasureReport").GetMethod("Write", all)!.Invoke(null, new[] { result, directory });
    return result;
}
var kinds = new[] { "http", "enum", "fallback", "structural" };
var measured = Model();
var failures = kinds.Select(kind => Failure(measured, kind)).ToArray();
foreach (var failure in failures) Add(measured, failure);
Add(measured, Method(measured, "EmitterInput", "Good"));
var result = Measure(measured, "wrappers");
var gaps = ((IEnumerable)Get(result, "Gaps")).Cast<object>().ToArray();
foreach (var failure in failures)
    Assert(gaps.Count(g => (string)Get(g, "Phase") == "compile"
        && (string)Get(g, "CppSymbol") == (string)Get(failure, "CppName")) == 1,
        "failed wrapper validated and recorded once: " + Get(failure, "Name"));
Assert((int)Get(result, "Compiled") == 1, "failed wrappers and trap fallback are not successful bodies; later scalar body still compiles");

// Normal emission still validates wrappers, and retains its established intrinsic
// trap fallback. A non-null sink is essential: normal planning skips these wrappers.
foreach (string kind in kinds)
{
    var model = Model(); var failure = Failure(model, kind); Add(model, failure);
    var emitter = Emitter(model); Call(model, "BeginMeasureSymbolAudit");
    var bodies = new List<string>();
    var methodParameter = Expression.Parameter(T("MethodInfo")); var textParameter = Expression.Parameter(typeof(string));
    var sink = Expression.Lambda(typeof(Action<,>).MakeGenericType(T("MethodInfo"), typeof(string)),
        Expression.Call(Expression.Constant(bodies), typeof(List<string>).GetMethod("Add")!, textParameter),
        methodParameter, textParameter).Compile();
    Exception? error = null;
    try { Call(emitter, "CompileReachableBodies", Activator.CreateInstance(T("LiteralPool")), sink, NewList(T("MethodInfo")), null); }
    catch (TargetInvocationException ex) { error = ex.InnerException; }
    if (kind == "fallback")
        Assert(error is null && bodies.Count == 1 && bodies[0].Contains("traps on call", StringComparison.Ordinal), "normal intrinsic fallback remains unchanged");
    else Assert(error is NotSupportedException && bodies.Count == 0, "normal wrapper failure remains fail-fast: " + kind);
    File.WriteAllText(Path.Combine(output, "normal-" + kind + ".txt"), error?.ToString() ?? string.Join("\n", bodies));
}

// Start with a queued, not-yet-scanned body. The real lifetime wrapper's lowering
// drains it after MeasureGaps has snapshotted the initial reach diagnostics.
var late = Model();
var stream = Owner(late, "System.Threading.WaitHandle");
var dispose = Clone(Method(late, "LifetimeInput", "Dispose", 0), stream, "Dispose");
var core = Clone(Method(late, "LifetimeInput", "Dispose", 1), stream, "Dispose");
((IList)Get(stream, "Methods")).Add(dispose); ((IList)Get(stream, "Methods")).Add(core);
((IList)Get(late, "Classes")).Add(stream);
Add(late, dispose);
var later = Method(late, "EmitterInput", "Later"); Add(late, later);
Call(Get(late, "_toScan"), "Enqueue", later);
Assert(((IList)Get(late, "ReachabilityDiagnostics")).Count == 0, "late case begins without reach gaps");
var lateResult = Measure(late, "late-discovery");
var lateGaps = ((IEnumerable)Get(lateResult, "Gaps")).Cast<object>().ToArray();
Assert(lateGaps.Any(g => (string)Get(g, "Phase") == "reach" && (string)Get(g, "Method") == "EmitterInput.Bad"), "late discovered scan failure survives final reporting");
Assert(lateGaps.Any(g => (string)Get(g, "Phase") == "compile" && (string)Get(g, "Method") == "EmitterInput.Bad"), "late discovered failing body is attempted");

// A non-NotSupportedException during a canonical trial is a compiler failure,
// not the expected placeholder fallback. Finalization removes its unshared body.
var planning = Model(shared: true);
var canonical = Owner(planning, "Test.Canonical");
var placeholder = Activator.CreateInstance(T("TypeDesc"))!;
Set(placeholder, "IsCanonPlaceholder", true);
var typeArguments = Array.CreateInstance(T("TypeDesc"), 1); typeArguments.SetValue(placeholder, 0);
Set(canonical, "Context", Activator.CreateInstance(T("GenericContext"), typeArguments, Array.CreateInstance(T("TypeDesc"), 0)));
var invalidBody = Clone(Method(planning, "EmitterInput", "Good"), canonical, "Malformed");
Set(invalidBody, "Rva", int.MaxValue); Add(planning, invalidBody);
var planningResult = Measure(planning, "planning-only");
var planningGaps = ((IEnumerable)Get(planningResult, "Gaps")).Cast<object>().ToArray();
Assert(planningGaps.Count(g => (string)Get(g, "Phase") == "compile" && (string)Get(g, "Pass") == "planning"
    && (string)Get(g, "CppSymbol") == (string)Get(invalidBody, "CppName")
    && (string)Get(g, "ExceptionType") == "BadImageFormatException") == 1, "unreplayed canonical planning failure remains visible");
Assert(!(bool)Call(Get(planning, "Reachable"), "Contains", invalidBody)!, "failed unshared canonical body was removed before emission");
Assert((int)Get(planningResult, "Attempted") == 0 && (int)Get(planningResult, "Compiled") == 0, "planning failure is not counted as an emission attempt or success");

// Retaining that diagnostic must not make its discarded symbol look defined to
// an emission caller. Keep the malformed callee in the real metadata token map.
var dangling = Model(shared: true);
var discardedOwner = Owner(dangling, "Test.Canonical");
Set(discardedOwner, "Context", Activator.CreateInstance(T("GenericContext"), typeArguments, Array.CreateInstance(T("TypeDesc"), 0)));
var discarded = Method(dangling, "EmitterInput", "Bad"); _ = Get(discarded, "Signature");
Set(discarded, "DeclaringClass", discardedOwner); Set(discarded, "_cppName", null);
Set(discarded, "Rva", int.MaxValue); Add(dangling, discarded);
Add(dangling, Method(dangling, "EmitterInput", "Later"));
var danglingResult = Measure(dangling, "planning-dangling");
var danglingGaps = ((IEnumerable)Get(danglingResult, "Gaps")).Cast<object>().ToArray();
Assert(danglingGaps.Any(g => (string)Get(g, "Phase") == "compile" && (string)Get(g, "Pass") == "planning"), "discarded callee has a planning diagnostic");
Assert(danglingGaps.Any(g => (string)Get(g, "Phase") == "dangling"
    && ((string)Get(g, "Message")).Contains((string)Get(discarded, "CppName"), StringComparison.Ordinal)),
    "planning-only diagnostic does not define a symbol named by emission");
Console.WriteLine($"Emitter fixture checks: {checks}");
