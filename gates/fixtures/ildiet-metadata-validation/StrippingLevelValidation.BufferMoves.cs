using System.Globalization;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

static partial class StrippingLevelValidation
{
    static readonly (int Destination, int Source, int Count)[] BufferMoveCases =
    [
        (8, 0, 32768), (0, 8, 32768), (8, 8, 32768), (8, 0, 0), (8, 0, 16),
    ];
    const int BufferMoveExtent = 32784;

    delegate void ReferenceMove(ref byte destination, ref byte source, nuint count);
    static void CheckBufferMovesClr()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var methods = typeof(Buffer).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        int checkedCases = 0;
        foreach (string name in new[] { "BulkMoveWithWriteBarrierInternal", "BulkMoveWithWriteBarrierBatch" })
        foreach (var method in methods.Where(m => m.Name == name))
        {
            var move = method.CreateDelegate<ReferenceMove>();
            foreach (var test in BufferMoveCases)
            {
                // CoreCLR's batching helper is entered only above its 16 KiB threshold.
                if (name.EndsWith("Batch", StringComparison.Ordinal) && test.Count <= 16384)
                    continue;
                var bytes = Enumerable.Range(0, BufferMoveExtent).Select(i => (byte)(i % 251)).ToArray();
                move(ref bytes[test.Destination], ref bytes[test.Source], (nuint)test.Count);
                for (int i = 0; i < bytes.Length; i++)
                {
                    int sourceIndex = i >= test.Destination && i < test.Destination + test.Count
                        ? test.Source + i - test.Destination : i;
                    if (bytes[i] != sourceIndex % 251)
                        throw new InvalidOperationException($"{method}: copy mismatch at {i}");
                }
                checkedCases++;
            }
        }
        if (checkedCases != 8)
            throw new InvalidOperationException("The actual CoreLib move overload set changed");
        Console.WriteLine("buffer-moves-clr=barrier,batch,overlap,same,zero,large");
    }

    static void CreateBufferMoves(string original, string directory, string mode)
    {
        if (mode is not ("retained" or "called" or "bad-shape"))
            throw new ArgumentException("Unknown buffer-move mode", nameof(mode));
        CreateRetainedIntercept(original, directory, "retained");
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(directory);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        cm.Types.Add(new TypeDefinition("System", "UIntPtr", TypeAttributes.Public | TypeAttributes.Sealed,
            cm.GetType("System.ValueType")));
        var buffer = cm.GetType("System.Buffer");
        var failure = cm.GetType("MediumFixture.RawBodyOnly").Methods.Single();
        MethodDefinition Move(string name, bool body)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static,
                cm.TypeSystem.Void);
            TypeReference address = new ByReferenceType(cm.TypeSystem.Byte);
            method.Parameters.Add(new ParameterDefinition(address));
            method.Parameters.Add(new ParameterDefinition(address));
            method.Parameters.Add(new ParameterDefinition(mode == "bad-shape" ? am.TypeSystem.Int32 : am.TypeSystem.UIntPtr));
            if (body)
            {
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Call, failure);
                il.Emit(OpCodes.Ret);
            }
            else method.ImplAttributes = MethodImplAttributes.InternalCall;
            buffer.Methods.Add(method);
            return method;
        }
        var moves = new[]
        {
            Move("BulkMoveWithWriteBarrierInternal", false),
            Move("BulkMoveWithWriteBarrierBatch", true),
        };
        var main = am.EntryPoint;
        // Keep the existing culture pinning and type-touch prefix intact.
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        var entry = main.Body.GetILProcessor();
        var bytes = new VariableDefinition(new ArrayType(am.TypeSystem.Byte));
        var index = new VariableDefinition(am.TypeSystem.Int32);
        var failed = new VariableDefinition(am.TypeSystem.Int32);
        main.Body.Variables.Add(bytes);
        main.Body.Variables.Add(index);
        main.Body.Variables.Add(failed);
        void Constant(int value) => entry.Emit(OpCodes.Ldc_I4, value);
        void Loop(Action body)
        {
            Constant(0);
            entry.Emit(OpCodes.Stloc, index);
            var start = Instruction.Create(OpCodes.Nop);
            entry.Append(start);
            body();
            entry.Emit(OpCodes.Ldloc, index);
            Constant(1);
            entry.Emit(OpCodes.Add);
            entry.Emit(OpCodes.Stloc, index);
            entry.Emit(OpCodes.Ldloc, index);
            Constant(BufferMoveExtent);
            entry.Emit(OpCodes.Blt, start);
        }
        if (mode != "retained")
        foreach (var move in moves)
        foreach (bool address in new[] { false, true })
        foreach (var test in BufferMoveCases)
        {
            if (move.Name.EndsWith("Batch", StringComparison.Ordinal) && test.Count <= 16384)
                continue;
            Constant(BufferMoveExtent);
            entry.Emit(OpCodes.Newarr, am.TypeSystem.Byte);
            entry.Emit(OpCodes.Stloc, bytes);
            Loop(() =>
            {
                entry.Emit(OpCodes.Ldloc, bytes);
                entry.Emit(OpCodes.Ldloc, index);
                entry.Emit(OpCodes.Ldloc, index);
                Constant(251);
                entry.Emit(OpCodes.Rem);
                entry.Emit(OpCodes.Stelem_I1);
            });
            foreach (int offset in new[] { test.Destination, test.Source })
            {
                entry.Emit(OpCodes.Ldloc, bytes);
                Constant(offset);
                entry.Emit(OpCodes.Ldelema, am.TypeSystem.Byte);
            }
            Constant(test.Count);
            if (mode != "bad-shape") entry.Emit(OpCodes.Conv_U);
            if (address)
            {
                entry.Emit(OpCodes.Ldftn, am.ImportReference(move));
                var site = new CallSite(am.TypeSystem.Void);
                foreach (var parameter in move.Parameters)
                    site.Parameters.Add(new ParameterDefinition(am.ImportReference(parameter.ParameterType)));
                entry.Emit(OpCodes.Calli, site);
            }
            else entry.Emit(OpCodes.Call, am.ImportReference(move));
            Loop(() =>
            {
                entry.Emit(OpCodes.Ldloc, bytes);
                entry.Emit(OpCodes.Ldloc, index);
                entry.Emit(OpCodes.Ldelem_U1);
                var unchanged = Instruction.Create(OpCodes.Ldloc, index);
                var compare = Instruction.Create(OpCodes.Ldc_I4, 251);
                entry.Emit(OpCodes.Ldloc, index);
                Constant(test.Destination);
                entry.Emit(OpCodes.Blt, unchanged);
                entry.Emit(OpCodes.Ldloc, index);
                Constant(test.Destination + test.Count);
                entry.Emit(OpCodes.Bge, unchanged);
                entry.Emit(OpCodes.Ldloc, index);
                Constant(test.Source - test.Destination);
                entry.Emit(OpCodes.Add);
                entry.Emit(OpCodes.Br, compare);
                entry.Append(unchanged);
                entry.Append(compare);
                entry.Emit(OpCodes.Rem);
                entry.Emit(OpCodes.Ceq);
                Constant(0);
                entry.Emit(OpCodes.Ceq);
                entry.Emit(OpCodes.Ldloc, failed);
                entry.Emit(OpCodes.Or);
                entry.Emit(OpCodes.Stloc, failed);
            });
        }
        entry.Emit(OpCodes.Ldloc, failed);
        entry.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
    }
}
