using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length != 2) throw new ArgumentException("Transpiler assembly and fresh output directory are required");
string output = Path.GetFullPath(args[1]);
if (Directory.Exists(output)) throw new IOException("Output directory must be new");
Directory.CreateDirectory(output);
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
var gapType = assembly.GetType("Dn2Cpp.MeasureGap", true)!;
var resultType = assembly.GetType("Dn2Cpp.MeasureResult", true)!;
var reportType = assembly.GetType("Dn2Cpp.MeasureReport", true)!;
var write = reportType.GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Static)!;
int checks = 0;
void Assert(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
object Gap(string phase, string name, string? symbol, string pass = "emission", string message = "gap") =>
    Activator.CreateInstance(gapType, phase, "Test", name, "NotSupportedException", message, symbol, pass)!;
Dictionary<string, string> Write(string name, object[] gaps, int successful, int? named)
{
    string directory = Path.Combine(output, name);
    Directory.CreateDirectory(directory);
    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(gapType))!;
    foreach (var gap in gaps) list.Add(gap);
    var result = Activator.CreateInstance(resultType, successful + gaps.Length, successful, list, named)!;
    write.Invoke(null, new object[] { result, directory });
    return File.ReadAllLines(Path.Combine(directory, "s0-measure.tsv"))
        .Select(line => line.Split('\t')).ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
}
var clean = Write("clean", Array.Empty<object>(), 5, 7);
Assert(clean["successfulBodyCount"] == "5", "successful count");
Assert(clean["failedBodyCount"] == "0", "zero failed bodies");
Assert(clean["failedEmissionBodyCount"] == "0", "zero failed emission bodies");
Assert(clean["unreplayedPlanningFailureBodyCount"] == "0", "zero unreplayed planning failures");
Assert(clean["symbolAuditStatus"] == "executed", "clean audit status");
Assert(clean["unknownGapCount"] == "null", "unknown remaining count");
Assert(clean["firstFailurePerBody"] == "true", "first failure qualification");
var mixedRows = new[] {
    Gap("compile", "SameDisplay", "body_a"), Gap("compile", "SameDisplay", "body_b"),
    Gap("compile", "SameDisplay", "body_a"), Gap("reach", "Late", "late", "reachability"),
    Gap("reach", "Late", "late", "reachability"), Gap("reach", "<specialization>", null, "reachability"),
    Gap("layout", "<specialization>", null, "planning"),
    Gap("compile-spec", "<specialization>", null),
};
var mixed = Write("mixed", mixedRows, 3, null);
Assert(mixed["failedBodyCount"] == "2", "symbols distinguish overloads and deduplicate repeated phases");
Assert(mixed["reachFailedMethodCount"] == "1", "late reach count deduplicated");
Assert(mixed["reachFailedSpecializationCount"] == "1", "specialization count");
Assert(mixed["infrastructureGapCount"] == "2", "planning and emission infrastructure");
Assert(mixed["recordedGapCount"] == "8", "raw rows preserved");
Assert(mixed["discoveryStatus"] == "partial", "reach gap makes discovery partial");
Assert(mixed["bodyPassStatus"] == "truncated", "layout/spec truncation");
Assert(mixed["symbolAuditStatus"] == "skipped", "no symbol audit after truncation");
Assert(mixed["namedSymbols"] == "null", "skipped named count");
var planningOnly = Write("planning-only", new[] { Gap("compile", "Canonical", "canon", "planning") }, 1, 0);
Assert(planningOnly["failedBodyCount"] == "1", "planning failure remains observed");
Assert(planningOnly["failedEmissionBodyCount"] == "0", "planning failure is not an emission attempt");
Assert(planningOnly["unreplayedPlanningFailureBodyCount"] == "1", "unreplayed planning count");
Write("reversed", mixedRows.Reverse().ToArray(), 3, null);
Assert(File.ReadAllText(Path.Combine(output, "mixed", "s0-gaps-detail.tsv")) ==
       File.ReadAllText(Path.Combine(output, "reversed", "s0-gaps-detail.tsv")), "deterministic detail row sorting");
var partial = Write("partial", new[] { Gap("reach", "Late", "late", "reachability") }, 1, 2);
Assert(partial["symbolAuditStatus"] == "partial", "named sweep is partial after reach failures");
Write("escaping", new[] { Gap("compile", "Test\tMethod", "test", message: "a\tb\r\nc") }, 0, 0);
string escaped = File.ReadAllText(Path.Combine(output, "escaping", "s0-gaps-detail.tsv"));
Assert(escaped.Split('\t').Length == 7, "TSV escaping preserves seven columns");
Assert(escaped.EndsWith("\n", StringComparison.Ordinal) && !escaped.Contains('\r'), "detail newline stable");

// Exercise the coordinator's failure policy without starting threads or loading a corpus.
// Only these initialized fields are read by the error arms under test.
const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
Type Model(string name) => assembly.GetType("Dn2Cpp." + name, true)!;
void Set(object instance, string field, object? value) => instance.GetType().GetField(field, all)!.SetValue(instance, value);
var compilationType = Model("Compilation");
var methodType = Model("MethodInfo");
var compilation = RuntimeHelpers.GetUninitializedObject(compilationType);
Set(compilation, "<SharedGenericsEnabled>k__BackingField", true);
var taints = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(methodType, typeof(string)))!;
Set(compilation, "SharedTaint", taints);
var owner = Activator.CreateInstance(Model("ClassInfo"))!;
Set(owner, "Namespace", "Test"); Set(owner, "Name", "Canonical");
var placeholder = Activator.CreateInstance(Model("TypeDesc"))!;
Set(placeholder, "IsCanonPlaceholder", true);
var placeholders = Array.CreateInstance(Model("TypeDesc"), 1); placeholders.SetValue(placeholder, 0);
var emptyTypes = Array.CreateInstance(Model("TypeDesc"), 0);
Set(owner, "Context", Activator.CreateInstance(Model("GenericContext"), placeholders, emptyTypes));
var method = Activator.CreateInstance(methodType)!;
Set(method, "DeclaringClass", owner); Set(method, "Name", "Trial"); Set(method, "_cppName", "canonical_trial");
var emitterType = Model("CppEmitter");
var emitter = Activator.CreateInstance(emitterType, compilation, Activator.CreateInstance(Model("ConsoleBackend")), false)!;
var parallelType = emitterType.GetNestedType("ParallelBodyResult", BindingFlags.NonPublic)!;
var commit = emitterType.GetMethod("CommitParallelBody", all)!;
IList ListOf(Type element) => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
Exception? Commit(Exception error, bool planning, IList? diagnostics)
{
    var slot = Activator.CreateInstance(parallelType, true)!;
    Set(slot, "Error", error);
    try { commit.Invoke(emitter, new object?[] { method, slot, planning, null, ListOf(methodType), diagnostics }); }
    catch (TargetInvocationException ex) { return ex.InnerException; }
    return null;
}
foreach (bool planning in new[] { true, false })
{
    var error = new InvalidOperationException("unexpected body failure");
    var diagnostics = ListOf(gapType);
    Assert(Commit(error, planning, diagnostics) is null, "measure continues after unexpected parallel failure");
    Assert(diagnostics.Count == 1, "parallel failure has exactly one diagnostic");
    Assert((string)gapType.GetProperty("CppSymbol")!.GetValue(diagnostics[0])! == "canonical_trial", "parallel diagnostic keeps body identity");
    Assert(ReferenceEquals(Commit(error, planning, null), error), "normal parallel failure propagates unchanged");
    foreach (string type in new[] { "InstantiationBoundException", "StrictCompletionException" })
    {
        var fatal = (Exception)Activator.CreateInstance(Model(type), "must escape")!;
        var fatalDiagnostics = ListOf(gapType);
        Assert(ReferenceEquals(Commit(fatal, planning, fatalDiagnostics), fatal), "must-escape parallel failure propagates unchanged");
        Assert(fatalDiagnostics.Count == 0, "must-escape error is never a gap");
    }
}
var expectedDiagnostics = ListOf(gapType);
Assert(Commit(new NotSupportedException("placeholder shape"), true, expectedDiagnostics) is null, "expected planning unsupported shape remains fallback");
Assert(expectedDiagnostics.Count == 0 && (string?)taints[method] == "unsupported", "expected planning fallback is not a compiler gap");
Console.WriteLine($"MeasureReport checks: {checks}");
