#nullable disable
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// Reflection flags/properties long-tail: the raw-ECMA-word surface
// (Type/MethodBase/FieldInfo/ParameterInfo.Attributes and the Is* predicates
// derived from them), MemberInfo.MemberType/MetadataToken/Module,
// Type.Equals/UnderlyingSystemType/HasElementType/MakeArrayType/
// GetEnumValuesAsUnderlyingType/FindInterfaces/IsVisible family,
// MemberInfo.CustomAttributes (CustomAttributeData, AttributeType only), and
// RuntimeHelpers.AllocateTypeAssociatedMemory. Everything here matches real
// .NET except the marked AOT-boundary divergences: MakeArrayType over an
// element type never instantiated as an array throws NotSupportedException,
// CustomAttributeData.ConstructorArguments throws the catchable
// PlatformNotSupportedException (dn2cpp records AttributeType only),
// ParameterInfo.DefaultValue throws it too (the Constant-table blob is not
// carried into the image; HasDefaultValue answers exactly), and
// Type.GetGenericParameterConstraints throws it where real .NET throws
// InvalidOperationException (no generic-parameter Type ever materializes).

namespace ReflectLongtailSubset
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    sealed class MarkAttribute : Attribute
    {
        public string Tag;
        public MarkAttribute(string tag) { Tag = tag; }
    }

    interface IAlpha { int A(); }
    interface IBeta : IAlpha { int B(); }

    [Mark("type")]
    class Gadget : IBeta, IDisposable
    {
        public class NestedPub { }
        internal class NestedInt { }

        [Mark("field")] public int Counter;
        private int secret;

        [Mark("prop")] public int Level { get; set; }
        public int InitValue { get; init; }

        [Mark("method")]
        public virtual int Spin(int turns) => turns + Counter + secret;

        public static int Opt(int a, int b = 5) => a + b;

        public static bool TryGet(out int v)
        {
            v = 3;
            return true;
        }

        private static int readonlyRefValue;
        public static ref readonly int ReadOnlyRef() => ref readonlyRefValue;

        public T Echo<T>(T v) => v;

        public int A() => 1;
        public int B() => 2;
        public void Dispose() { }

        public Gadget(int c) { Counter = c; secret = c; }
    }

    sealed class SealedGadget : Gadget
    {
        public SealedGadget() : base(0) { }
        public sealed override int Spin(int turns) => turns;
    }

    abstract class AbsGadget
    {
        public abstract int Grow();
    }

    internal class PlainInternal { public int P; }

    // Never used as an array element anywhere: MakeArrayType over it hits the
    // AOT boundary (intentional divergence, see the header comment).
    class NeverArrayed { public int N; }

    static class Program
    {
        static void Show(string label, object v) => Console.WriteLine(label + "=" + v);

        internal static void Run()
        {
            Console.WriteLine("== reflect longtail ==");
            // Keep instances/members reachable.
            var g = new Gadget(1);
            Show("use", g.Spin(1) + new SealedGadget().Spin(2) + Gadget.Opt(1) + g.Echo(3)
                + new PlainInternal().P + new NeverArrayed().N + g.Level);

            Type tg = typeof(Gadget);

            // -- Type.Equals / UnderlyingSystemType / identity --
            Show("t-eq", tg.Equals(typeof(Gadget)));
            Show("t-eq-obj", tg.Equals((object)"x"));
            Show("t-eq-null", tg.Equals((Type)null));
            Show("underlying", tg.UnderlyingSystemType == tg);

            // -- Type attributes word + visibility family (exact raw words) --
            Show("attrs-gadget", (int)tg.Attributes);
            Show("attrs-sealed", (int)typeof(SealedGadget).Attributes);
            Show("attrs-abs", (int)typeof(AbsGadget).Attributes);
            Show("attrs-itf", (int)typeof(IBeta).Attributes);
            Show("ispub-gadget", tg.IsPublic);
            Show("isnotpub-gadget", tg.IsNotPublic);
            Show("isvis-gadget", tg.IsVisible);
            Show("ispub-int", typeof(int).IsPublic);
            Show("isnotpub-int", typeof(int).IsNotPublic);
            Show("isvis-int", typeof(int).IsVisible);
            Show("iscom", tg.IsCOMObject);
            Show("isfnptr", tg.IsFunctionPointer);

            // -- HasElementType / element chain --
            Show("haselem-arr", typeof(int[]).HasElementType);
            Show("haselem-int", typeof(int).HasElementType);
            Show("haselem-rt", new Gadget[0].GetType().HasElementType);

            // -- MakeArrayType: in-image resolution + the AOT boundary --
            Show("makearr-int", typeof(int).MakeArrayType() == typeof(int[]));
            Show("makearr-ref", tg.MakeArrayType() == typeof(Gadget[]));
            try
            {
                typeof(NeverArrayed).MakeArrayType();
                Console.WriteLine("makearr-missing=ok");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("makearr-missing=NotSupportedException");
            }
            // The rank overload denotes a MULTIDIMENSIONAL array type even for
            // rank 1 (T[*] != T[]) — dn2cpp models no MD array types, so it
            // throws NotSupportedException (intentional divergence, see the
            // header comment).
            try
            {
                typeof(int).MakeArrayType(2);
                Console.WriteLine("makearr-rank=ok");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("makearr-rank=NotSupportedException");
            }

            // -- assignability direction / generic-parameter / nested visibility --
            Show("assignto-itf", tg.IsAssignableTo(typeof(IBeta)));
            Show("assignto-rev", typeof(IBeta).IsAssignableTo(tg));
            Show("assignto-null", tg.IsAssignableTo(null));
            Show("genparam", tg.IsGenericParameter);
            Show("nestedpub-pub", typeof(Gadget.NestedPub).IsNestedPublic);
            Show("nestedpub-int", typeof(Gadget.NestedInt).IsNestedPublic);
            Show("nestedpub-top", tg.IsNestedPublic);

            // -- MemberType / MetadataToken / Module --
            MethodInfo spin = tg.GetMethod("Spin");
            FieldInfo counter = tg.GetField("Counter");
            PropertyInfo level = tg.GetProperty("Level");
            ConstructorInfo ctor = tg.GetConstructors()[0];
            Show("mt-method", (int)spin.MemberType);
            Show("mt-ctor", (int)ctor.MemberType);
            Show("mt-field", (int)counter.MemberType);
            Show("mt-prop", (int)level.MemberType);
            Show("mt-type", (int)((MemberInfo)tg).MemberType);
            Show("tok-nonzero", spin.MetadataToken != 0 && counter.MetadataToken != 0
                && level.MetadataToken != 0 && ctor.MetadataToken != 0 && tg.MetadataToken != 0);
            Show("tok-stable", spin.MetadataToken == tg.GetMethod("Spin").MetadataToken);
            Show("tok-distinct", spin.MetadataToken != counter.MetadataToken);
            Show("mod-eq", tg.Module == typeof(Program).Module);
            Show("mod-ne-corelib", tg.Module == typeof(int).Module);
            Show("mod-member", ((MemberInfo)spin).Module == tg.Module);

            // -- MethodBase flags (exact raw words from the same metadata) --
            Show("m-attrs", (int)spin.Attributes);
            Show("m-impl", (int)spin.MethodImplementationFlags);
            Show("m-virt", spin.IsVirtual);
            Show("m-virt-opt", tg.GetMethod("Opt").IsVirtual);
            Show("m-final", typeof(SealedGadget).GetMethod("Spin").IsFinal);
            Show("m-notfinal", spin.IsFinal);
            Show("m-abs", typeof(AbsGadget).GetMethod("Grow").IsAbstract);
            Show("m-notabs", spin.IsAbstract);
            Show("m-isctor", ctor.IsConstructor);
            Show("m-notctor", spin.IsConstructor);
            Show("m-generic", tg.GetMethod("Echo").IsGenericMethod);
            Show("m-notgeneric", spin.IsGenericMethod);
            Show("c-attrs", (int)ctor.Attributes);

            // -- FieldInfo raw word + predicates --
            Show("f-attrs", (int)counter.Attributes);
            Show("f-special", counter.IsSpecialName);
            Show("f-priv-pub", counter.IsPrivate);
            FieldInfo secret = tg.GetField("secret", BindingFlags.NonPublic | BindingFlags.Instance);
            Show("f-priv-priv", secret.IsPrivate);
            Show("f-priv-attrs", (int)secret.Attributes);

            // -- ParameterInfo: Member / Attributes / IsOptional --
            ParameterInfo[] optPs = tg.GetMethod("Opt").GetParameters();
            Show("p-opt0", optPs[0].IsOptional);
            Show("p-opt1", optPs[1].IsOptional);
            Show("p-attrs0", (int)optPs[0].Attributes);
            Show("p-attrs1", (int)optPs[1].Attributes);
            Show("p-member", optPs[1].Member.Name);
            Show("p-ctor-member", (int)ctor.GetParameters()[0].Member.MemberType);
            MethodInfo initSetter = tg.GetProperty("InitValue").SetMethod;
            ParameterInfo initP = initSetter.ReturnParameter;
            Type[] initReq = initP.GetRequiredCustomModifiers();
            Show("p-init-modreq-count", initReq.Length);
            if (initReq.Length > 0)
                Show("p-init-modreq-type", initReq[0].FullName);
            Show("p-init-modopt", initP.GetOptionalCustomModifiers().Length);
            Show("p-value-modreq", initSetter.GetParameters()[0].GetRequiredCustomModifiers().Length);
            Show("p-plain-modreq", optPs[0].GetRequiredCustomModifiers().Length);
            Show("p-return-modreq", tg.GetMethod("Opt").ReturnParameter.GetRequiredCustomModifiers().Length);
            ParameterInfo roRet = tg.GetMethod("ReadOnlyRef").ReturnParameter;
            Type[] roReq = roRet.GetRequiredCustomModifiers();
            Show("p-byref-ret-modreq", roReq.Length + ":" + roReq[0].FullName);
            // IsOut / HasDefaultValue read the recorded ParameterAttributes word
            // exactly; DefaultValue is the marked divergence (the Constant-table
            // blob is not carried into the image -> catchable
            // PlatformNotSupportedException where real .NET reports 5).
            ParameterInfo outP = tg.GetMethod("TryGet").GetParameters()[0];
            Show("p-isout", outP.IsOut + "|" + optPs[1].IsOut);
            Show("p-hasdefault", optPs[1].HasDefaultValue + "|" + optPs[0].HasDefaultValue);
            try
            {
                Console.WriteLine("p-default=" + optPs[1].DefaultValue);
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("p-default=PlatformNotSupportedException");
            }
            // GetGenericParameterConstraints: no generic-parameter Type ever
            // materializes, so the call throws the catchable
            // PlatformNotSupportedException (real .NET: InvalidOperationException
            // on a non-parameter receiver — also a throw, different type).
            try
            {
                tg.GetGenericParameterConstraints();
                Console.WriteLine("genparam-constraints=none");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("genparam-constraints=PlatformNotSupportedException");
            }
            catch (InvalidOperationException)
            {
                // Real .NET's answer on a non-generic-parameter receiver; caught
                // so the capture-time dotnet oracle run completes.
                Console.WriteLine("genparam-constraints=InvalidOperationException");
            }

            // -- FindInterfaces: managed predicate over the interface set --
            Type[] found = tg.FindInterfaces((t, criteria) => t.Name != (string)criteria, "IDisposable");
            string[] names = new string[found.Length];
            for (int i = 0; i < found.Length; i++) names[i] = found[i].Name;
            Array.Sort(names, StringComparer.Ordinal);
            Show("finditf", names.Length + ":" + string.Join(",", names));

            // -- GetEnumValuesAsUnderlyingType: exact element identity + values --
            short[] forceShortArray = new short[1]; // keep short[] in the AOT image
            Show("force-short", forceShortArray.Length);
            Array sv = typeof(ShortEnum).GetEnumValuesAsUnderlyingType();
            short[] svt = (short[])sv;
            string ss = "";
            for (int i = 0; i < svt.Length; i++) ss += (i > 0 ? "," : "") + svt[i];
            Show("enum-short", svt.Length + ":" + ss);
            Array iv = typeof(IntEnum).GetEnumValuesAsUnderlyingType();
            int[] ivt = (int[])iv;
            string si = "";
            for (int i = 0; i < ivt.Length; i++) si += (i > 0 ? "," : "") + ivt[i];
            Show("enum-int", ivt.Length + ":" + si);

            // -- CustomAttributes (CustomAttributeData; AttributeType only) --
            Show("cad-type", DescribeCad(tg.CustomAttributes.ToArray()));
            Show("cad-method", DescribeCad(spin.CustomAttributes.ToArray()));
            Show("cad-field", DescribeCad(counter.CustomAttributes.ToArray()));
            Show("cad-prop", DescribeCad(level.CustomAttributes.ToArray()));
            Show("cad-any", tg.CustomAttributes.Any(a => a.AttributeType == typeof(MarkAttribute)));
            Show("cad-asm", Assembly.GetEntryAssembly().CustomAttributes
                .Any(a => a.AttributeType.Name == "AsmMetaAttribute"));
            try
            {
                var args = tg.CustomAttributes.First().ConstructorArguments;
                Console.WriteLine("cad-args=" + args.Count);
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("cad-args=PlatformNotSupportedException");
            }

            // -- RuntimeHelpers.AllocateTypeAssociatedMemory --
            IntPtr mem = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(Gadget), 64);
            Show("alloc", mem != IntPtr.Zero);
        }

        enum ShortEnum : short { Lo = 1, Hi = 2, Top = 300 }
        enum IntEnum { Zero = 0, Neg = -7, Big = 100000 }

        static string DescribeCad(System.Reflection.CustomAttributeData[] cads)
        {
            string[] names = new string[cads.Length];
            for (int i = 0; i < cads.Length; i++) names[i] = cads[i].AttributeType.Name;
            Array.Sort(names, StringComparer.Ordinal);
            return names.Length + ":" + string.Join(",", names);
        }
    }
}
