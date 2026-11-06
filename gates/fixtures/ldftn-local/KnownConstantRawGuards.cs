using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

internal static class KnownConstantRawGuards
{
    internal static IEnumerable<(string Label, byte[] Signature, bool Expected)> Signatures()
    {
        yield return ("char", new byte[] { 0, 1, 2, 3 }, true);
        yield return ("string", new byte[] { 0, 1, 2, 14 }, true);
        yield return ("Type", new byte[] { 0, 1, 2, 18, 8 }, true);
        yield return ("generic", KnownConstantGuardImage.GenericSignature, true);
        foreach (byte header in new byte[] { 1, 2, 3, 4, 5, 0x20, 0x40, 0x60, 0x80 })
            yield return ("header-" + header, new byte[] { header, 1, 2, 3 }, false);
        foreach (byte header in new byte[] { 0x11, 0x15, 0x30, 0x50, 0x70, 0x90 })
            yield return ("generic-header-" + header, new byte[] { header, 1, 1, 2, 0x1e, 0 }, false);
        foreach (var item in new (string, byte[])[] {
            ("empty", Array.Empty<byte>()), ("truncated", new byte[] { 0, 1, 2 }),
            ("return-int", new byte[] { 0, 1, 8, 3 }), ("return-byref", new byte[] { 0, 1, 0x10, 2, 3 }),
            ("return-cmodreq", new byte[] { 0, 1, 0x1f, 8, 2, 3 }),
            ("return-cmodopt", new byte[] { 0, 1, 0x20, 8, 2, 3 }),
            ("parameter-byref", new byte[] { 0, 1, 2, 0x10, 3 }),
            ("parameter-cmodreq", new byte[] { 0, 1, 2, 0x1f, 8, 3 }),
            ("parameter-cmodopt", new byte[] { 0, 1, 2, 0x20, 8, 3 }),
            ("parameter-pointer", new byte[] { 0, 1, 2, 0x0f, 3 }),
            ("parameter-int", new byte[] { 0, 1, 2, 8 }),
            ("Type-valuetype", new byte[] { 0, 1, 2, 0x11, 8 }),
            ("Type-wrong-identity", new byte[] { 0, 1, 2, 0x12, 12 }),
            ("Type-nil", new byte[] { 0, 1, 2, 0x12, 0 }),
            ("Type-truncated", new byte[] { 0, 1, 2, 0x12 }),
            ("zero-params", new byte[] { 0, 0, 2 }),
            ("two-params", new byte[] { 0, 2, 2, 3, 3 }),
            ("trailing", new byte[] { 0, 1, 2, 3, 0 }),
            ("generic-arity-zero", new byte[] { 0x10, 0, 1, 2, 0x1e, 0 }),
            ("generic-arity-two", new byte[] { 0x10, 2, 1, 2, 0x1e, 0 }),
            ("generic-MVAR-one", new byte[] { 0x10, 1, 1, 2, 0x1e, 1 }),
            ("generic-VAR", new byte[] { 0x10, 1, 1, 2, 0x13, 0 }),
            ("generic-char", new byte[] { 0x10, 1, 1, 2, 3 }),
            ("generic-return", new byte[] { 0x10, 1, 1, 8, 0x1e, 0 }),
            ("generic-cmod", new byte[] { 0x10, 1, 1, 2, 0x20, 8, 0x1e, 0 }),
            ("generic-byref", new byte[] { 0x10, 1, 1, 2, 0x10, 0x1e, 0 }),
            ("generic-trailing", new byte[] { 0x10, 1, 1, 2, 0x1e, 0, 0 }) })
            yield return (item.Item1, item.Item2, false);
    }

    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--known-constant-raw-guards") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var model = new KnownConstantGuardModel(args[1]);
        var compilation = model.Make("Compilation");
        foreach (string pending in new[] { "_pendingFifo", "_pendingLifo" })
            model.Set(compilation, pending, Activator.CreateInstance(model.Type("Compilation").GetField(pending, KnownConstantGuardModel.Members)!.FieldType));
        var provider = Activator.CreateInstance(model.Type("SignatureProvider"), KnownConstantGuardModel.Members,
            null, new[] { compilation }, null)!;
        model.Set(compilation, "SigProvider", provider);
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("Known constant raw guard: " + label);
            checks++;
        }
        bool Validate(KnownConstantGuardImage image, bool spec)
        {
            var readers = (IDictionary)Activator.CreateInstance(model.Type("Compilation").GetField("_byReader", KnownConstantGuardModel.Members)!.FieldType)!;
            readers.Add(image.Reader, image.Module);
            model.Set(compilation, "_byReader", readers);
            var method = model.Type("Compilation").GetMethod(spec ? "ValidateKnownConstantSpec" : "ValidateKnownConstantReference",
                KnownConstantGuardModel.Members)!;
            try
            {
                method.Invoke(compilation, new[] { image.Module,
                    spec ? (object)image.Reader.GetMethodSpecification(image.Specification) : image.Reader.GetMemberReference(image.Reference),
                    image.Method, model.Context() });
                return true;
            }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException or BadImageFormatException)
            {
                return false;
            }
        }
        // Exercise the raw scanner independently: no generic model construction or
        // decoded TypeDesc can mask why a nested modifier was rejected.
        var scanner = model.Type("CoreIntrinsics").GetMethod("IsKnownConstantInstantiation", KnownConstantGuardModel.Members)!;
        byte[] StructArgument(params byte[] argument) => new byte[] { 0x0a, 1, 0x15, 0x11, 0x18, 1 }.Concat(argument).ToArray();
        foreach (var item in new (string Label, byte[] Signature, bool Expected)[] {
            ("primitive", new byte[] { 0x0a, 1, 8 }, true),
            ("struct-string", StructArgument(14), true),
            ("struct-object", StructArgument(0x1c), true),
            ("struct-class", StructArgument(0x12, 8), true),
            ("struct-vector", StructArgument(0x1d, 14), true),
            ("struct-array", StructArgument(0x14, 14, 2, 0, 0), true),
            ("struct-bounded-array", StructArgument(0x14, 8, 2, 1, 3, 1, 0x7f), true),
            ("struct-nested-struct", StructArgument(0x15, 0x11, 0x18, 1, 14), true),
            ("struct-nested-reference", StructArgument(0x15, 0x12, 8, 1, 14), true),
            ("struct-token-byte-20", StructArgument(0x11, 0x20), true),
            ("struct-two-args", new byte[] { 0x0a, 1, 0x15, 0x11, 0x18, 2, 14, 8 }, true),
            ("nested-modreq", StructArgument(0x1f, 8, 8), false),
            ("nested-modopt", StructArgument(0x20, 8, 8), false),
            ("nested-pinned", StructArgument(0x45, 8), false),
            ("nested-vector-modreq", StructArgument(0x1d, 0x1f, 8, 8), false),
            ("nested-array-modopt", StructArgument(0x14, 0x20, 8, 8, 1, 0, 0), false),
            ("deep-modopt", StructArgument(0x15, 0x11, 0x18, 1, 0x20, 8, 8), false),
            ("deep-pinned", StructArgument(0x15, 0x12, 8, 1, 0x45, 8), false),
            ("nested-pointer", StructArgument(0x0f, 8), false),
            ("nested-byref", StructArgument(0x10, 8), false),
            ("nested-void", StructArgument(1), false),
            ("outer-reference-instantiation", new byte[] { 0x0a, 1, 0x15, 0x12, 8, 1, 14 }, false),
            ("generic-zero-count", new byte[] { 0x0a, 1, 0x15, 0x11, 0x18, 0 }, false),
            ("generic-truncated-count", new byte[] { 0x0a, 1, 0x15, 0x11, 0x18, 2, 14 }, false),
            ("generic-trailing", new byte[] { 0x0a, 1, 0x15, 0x11, 0x18, 1, 14, 8 }, false),
            ("nil-type-token", StructArgument(0x11, 0), false),
            ("invalid-type-token-tag", StructArgument(0x11, 7), false),
            ("truncated-type-token", StructArgument(0x11, 0x80), false),
            ("array-zero-rank", StructArgument(0x14, 8, 0, 0, 0), false),
            ("array-too-many-sizes", StructArgument(0x14, 8, 1, 2, 3, 4, 0), false),
            ("array-too-many-bounds", StructArgument(0x14, 8, 1, 0, 2, 0, 0), false),
            ("array-truncated-bound", StructArgument(0x14, 8, 1, 0, 1, 0x80), false),
            ("depth-boundary", StructArgument(Enumerable.Repeat((byte)0x1d, 63).Append((byte)8).ToArray()), true),
            ("depth-exceeded", StructArgument(Enumerable.Repeat((byte)0x1d, 64).Append((byte)8).ToArray()), false) })
        {
            using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature, specification: item.Signature);
            bool accepted = (bool)scanner.Invoke(null, new object[] { image.Reader, image.Reader.GetMethodSpecification(image.Specification).Signature })!;
            Check(accepted == item.Expected, "raw scanner " + item.Label);
        }
        foreach (var item in Signatures())
        {
            byte[] target = item.Signature.Length > 0 && (item.Signature[0] & 0x10) != 0
                ? KnownConstantGuardImage.GenericSignature
                : item.Label == "string" ? new byte[] { 0, 1, 2, 14 }
                : item.Label.StartsWith("Type", StringComparison.Ordinal) ? new byte[] { 0, 1, 2, 18, 8 }
                : new byte[] { 0, 1, 2, 3 };
            using var image = new KnownConstantGuardImage(model, target, reference: item.Signature);
            Check(Validate(image, false) == item.Expected, "MemberRef " + item.Label);
        }
        for (int ownerMode = 0; ownerMode <= 8; ownerMode++)
        {
            using var image = new KnownConstantGuardImage(model, new byte[] { 0, 1, 2, 3 }, ownerMode: ownerMode);
            Check(Validate(image, false) == (ownerMode <= 1), "MemberRef owner identity " + ownerMode);
        }
        foreach (bool memberParent in new[] { false, true })
        {
            foreach (var item in new (string Label, byte[] Signature, bool Expected)[] {
                ("int", new byte[] { 0x0a, 1, 8 }, true),
                ("char", new byte[] { 0x0a, 1, 3 }, true),
                ("empty", Array.Empty<byte>(), false), ("header", new byte[] { 0, 1, 8 }, false),
                ("zero", new byte[] { 0x0a, 0 }, false), ("two", new byte[] { 0x0a, 2, 8, 8 }, false),
                ("truncated", new byte[] { 0x0a, 1 }, false),
                ("trailing", new byte[] { 0x0a, 1, 8, 0 }, false),
                ("cmodreq", new byte[] { 0x0a, 1, 0x1f, 8, 8 }, false),
                ("cmodopt", new byte[] { 0x0a, 1, 0x20, 8, 8 }, false),
                ("pinned", new byte[] { 0x0a, 1, 0x45, 8 }, false),
                ("byref", new byte[] { 0x0a, 1, 0x10, 8 }, false),
                ("pointer", new byte[] { 0x0a, 1, 0x0f, 8 }, false),
                ("string", new byte[] { 0x0a, 1, 14 }, false),
                ("object", new byte[] { 0x0a, 1, 0x1c }, false),
                ("array", new byte[] { 0x0a, 1, 0x1d, 8 }, false) })
            {
                using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature,
                    specification: item.Signature, memberParent: memberParent);
                var argument = model.Primitive(item.Label == "char" ? PrimitiveTypeCode.Char : PrimitiveTypeCode.Int32);
                model.Set(image.Method, "Context", model.Context(argument));
                Check(Validate(image, true) == item.Expected, "MethodSpec " + memberParent + " " + item.Label);
            }
            foreach (var item in Signatures().Where(item => !item.Expected))
            {
                using var image = new KnownConstantGuardImage(model, memberParent ? KnownConstantGuardImage.GenericSignature : item.Signature,
                    reference: item.Signature, memberParent: memberParent);
                model.Set(image.Method, "Context", model.Context(model.Primitive(PrimitiveTypeCode.Int32)));
                Check(!Validate(image, true), "MethodSpec parent " + memberParent + " " + item.Label);
            }
            foreach (int row in new[] { 6, 7 })
            {
                using var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature,
                    specification: new byte[] { 0x0a, 1, 0x11, (byte)(row << 2) }, memberParent: memberParent);
                var cls = model.Class(image.Module, System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle(row),
                    row == 6 ? "Fixture.ManagedStruct" : "Fixture.Enum");
                model.Set(cls, "IsValueType", row != 7);
                model.Set(cls, "IsEnum", row == 7);
                model.Set(image.Method, "Context", model.Context(model.ClassType(cls)));
                Check(Validate(image, true), "valid struct substitution " + memberParent + " " + row);
            }
            using (var image = new KnownConstantGuardImage(model, new byte[] { 0, 1, 2, 3 }, memberParent: memberParent))
            {
                model.Set(image.Method, "Context", model.Context(model.Primitive(PrimitiveTypeCode.Int32)));
                Check(!Validate(image, true), "MethodSpec cannot instantiate non-generic overload " + memberParent);
            }
            using (var image = new KnownConstantGuardImage(model, KnownConstantGuardImage.GenericSignature, memberParent: memberParent))
            {
                Check(!Validate(image, true), "open definition is not closed callable " + memberParent);
                foreach (string name in new[] { "System.Nullable`1", "Fixture.RefStruct", "Fixture.Reference" })
                {
                    int row = name == "System.Nullable`1" ? 5 : name == "Fixture.RefStruct" ? 8 : 9;
                    var cls = model.Class(image.Module, System.Reflection.Metadata.Ecma335.MetadataTokens.TypeDefinitionHandle(row), name);
                    model.Set(cls, "IsValueType", row != 9); model.Set(cls, "IsByRefLike", row == 8);
                    model.Set(image.Method, "Context", model.Context(model.ClassType(cls)));
                    Check(!Validate(image, true), "invalid closed substitution " + memberParent + " " + name);
                }
            }
        }
        Console.WriteLine("known-constant-raw-guards:True");
        Console.WriteLine("known-constant-raw-guard-checks:" + checks);
        return true;
    }
}
