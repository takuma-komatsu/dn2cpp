#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Array Type assignability — one rule with isinst (dn2cpp_typeinfo_assignable).
// Type.IsAssignableFrom / IsSubclassOf / BaseType used to run a private base-chain
// walk that knew nothing about arrays, so typeof(IEnumerable).IsAssignableFrom(
// stringArrayType) answered False while the (IEnumerable) cast succeeded — which is
// exactly how Newtonsoft's DefaultContractResolver.CreateContract mis-classified a
// Thrive string[] member as a *string* contract (organelles.json:
// "Cannot deserialize the current JSON array into type 'System.Object[]' because
// the type requires a JSON string value"). This section pins the unified rule on
// all three Type representations an array has in dn2cpp — which are now ONE
// identity per element type:
//   - typeof(T[]) and instance.GetType(): the precise per-element ti_arr_<T>
//     (elementType carried, array covariance answers);
//   - a reflected member type (FieldInfo.FieldType / PropertyInfo.PropertyType /
//     ParameterInfo.ParameterType): the SAME precise handle — value elements
//     (int[], enum[]) included — named by CppEmitter.FieldTypeInfoExpr's SZArray
//     arm and pre-noted by TypeMetadataEmitter.NoteReflectedMemberArrayElements.
//     So ft == typeof(string[]) holds by handle identity, GetElementType is real,
//     and the element-precise generic bits (IList<string> <- ft) answer like .NET.
// The member-type precision is load-bearing for JSON deserialization: Newtonsoft's
// DefaultContractResolver classifies a member by its reflected type, and an
// object-degraded array member fell into the untyped/Linq contract — a JArray
// stored through the unchecked setter thunk into a typed array field (the Thrive
// TrackList+Track silent corruption; the Track transcription below pins the fix).
//
// Every line below matches real .NET. The "GetInterfaces =" and "itfs=" counts read 11,
// and two mechanisms have to hold for that. GetInterfaces() skips the shared-generics
// canonical ALIAS rows the dispatch map interleaves with the real ones — types .NET has
// no equivalent of at all. And an SZArray dispatch map carries only the interfaces the
// SZArrayEnumerable<T> wrapper implements, so the three an array satisfies that it
// structurally cannot carry — System.ICloneable, System.Collections.IStructuralComparable
// and System.Collections.IStructuralEquatable — arrive as a shared RELATION-only row set
// (nullptr slots, so dispatch cannot see them), which BoundHandleSubset asserts in full
// for SZ arrays, MD arrays and System.Array itself. `is`/IsAssignableFrom answer True for
// those three off DN2CPP_TF_ARRAY_ITF, a fact about the TARGET that produces no row on
// the array's own table. If either count moves off 11, it is a regression.

namespace ArrayAssignabilitySubset
{
    class Holder
    {
        public string[] Names;
        public int[] Nums;
    }

    enum MusicContext
    {
        Menu,
        MicrobeStage,
        MulticellularStage,
    }

    // An element type this program names ONLY as an array member: no
    // new List<Biome>() exists anywhere in the gate program, so the
    // List<Biome> the CreateTemporaryCollection transcription below mints with
    // MakeGenericType is ctor-invokable purely through the transpiler's
    // SZArray temporary-list seed (Compilation.CollectArrayTemporaryListSeed).
    // That is the exact Thrive blind spot: the MusicContext flow further down
    // predates the seed and statically constructs its List ("eseed"), which is
    // why it kept passing while Thrive — which names no List<MusicContext> —
    // died on "Unable to find default constructor".
    enum Biome
    {
        Ocean,
        Tidepool,
        Vents,
    }

    // The Thrive Track shape: enum[]/string[] AUTO-PROPERTIES a JSON deserializer
    // reads through PropertyInfo.PropertyType (the proptab row's type-info).
    class Track
    {
        public string ResourcePath { get; set; }
        public MusicContext[] ExclusiveToContexts { get; set; }
        public string[] DisallowInContexts { get; set; }
        public Biome[] SpawnBiomes { get; set; }
    }

    static class Program
    {
        static void Bl(string label, bool v) => Console.WriteLine(label + " = " + v);

        // Newtonsoft DefaultContractResolver.CreateContract's collection decision
        // sequence (the part that mis-routed): dictionary before enumerable, string
        // conversion only after both.
        static string Classify(Type t)
        {
            if (typeof(IDictionary).IsAssignableFrom(t))
                return "dictionary";
            if (typeof(IEnumerable).IsAssignableFrom(t))
                return "array";
            return "object/string";
        }

        internal static void Run()
        {
            // -- typeof(T[]): the precise per-element handle. --
            Bl("IEnumerable <- string[]", typeof(IEnumerable).IsAssignableFrom(typeof(string[])));
            Bl("ICollection <- string[]", typeof(ICollection).IsAssignableFrom(typeof(string[])));
            Bl("IList <- string[]", typeof(IList).IsAssignableFrom(typeof(string[])));
            Bl("ICloneable <- string[]", typeof(ICloneable).IsAssignableFrom(typeof(string[])));
            Bl("Array <- string[]", typeof(Array).IsAssignableFrom(typeof(string[])));
            Bl("Array <- int[]", typeof(Array).IsAssignableFrom(typeof(int[])));
            Bl("IDictionary <- string[]", typeof(IDictionary).IsAssignableFrom(typeof(string[])));
            Bl("IList<string> <- string[]", typeof(IList<string>).IsAssignableFrom(typeof(string[])));
            Bl("IReadOnlyList<string> <- string[]", typeof(IReadOnlyList<string>).IsAssignableFrom(typeof(string[])));
            Bl("IEnumerable<object> <- string[]", typeof(IEnumerable<object>).IsAssignableFrom(typeof(string[])));
            Bl("IEnumerable<string> <- object[]", typeof(IEnumerable<string>).IsAssignableFrom(typeof(object[])));
            Bl("IEnumerable<object> <- int[]", typeof(IEnumerable<object>).IsAssignableFrom(typeof(int[])));
            Bl("IList<int> <- int[]", typeof(IList<int>).IsAssignableFrom(typeof(int[])));
            Bl("IEnumerable<uint> <- int[]", typeof(IEnumerable<uint>).IsAssignableFrom(typeof(int[])));
            Bl("object[] <- string[]", typeof(object[]).IsAssignableFrom(typeof(string[])));
            Bl("string[] <- object[]", typeof(string[]).IsAssignableFrom(typeof(object[])));
            Bl("int[] <- uint[]", typeof(int[]).IsAssignableFrom(typeof(uint[])));
            Bl("string[] subclassof Array", typeof(string[]).IsSubclassOf(typeof(Array)));
            Bl("string[] subclassof object", typeof(string[]).IsSubclassOf(typeof(object)));
            Bl("string[] subclassof object[]", typeof(string[]).IsSubclassOf(typeof(object[])));
            Console.WriteLine("string[].BaseType = " + typeof(string[]).BaseType.Name);
            Console.WriteLine("Array.BaseType = " + typeof(Array).BaseType.Name);
            Console.WriteLine("string[].GetInterfaces = " + typeof(string[]).GetInterfaces().Length);

            // -- instance.GetType(): the same precise handle typeof reports. --
            object inst = new string[] { "x", "y" };
            Type it = inst.GetType();
            Console.WriteLine("inst = " + it.FullName + " elem=" + it.GetElementType().Name
                + " ==typeof " + (it == typeof(string[])));
            Bl("IEnumerable <- inst", typeof(IEnumerable).IsAssignableFrom(it));
            Bl("IList<string> <- inst", typeof(IList<string>).IsAssignableFrom(it));

            // -- The reflected member type: a string[] field read through
            //    FieldInfo.FieldType — the same precise per-element handle. --
            FieldInfo f = typeof(Holder).GetField("Names");
            Type ft = f.FieldType;
            Console.WriteLine("ft = " + ft.FullName + " IsArray=" + ft.IsArray
                + " rank=" + ft.GetArrayRank()
                + " elem=" + ft.GetElementType().Name
                + " itfs=" + ft.GetInterfaces().Length
                + " ==typeof " + (ft == typeof(string[])));
            Bl("IEnumerable <- ft", typeof(IEnumerable).IsAssignableFrom(ft));
            Bl("IDictionary <- ft", typeof(IDictionary).IsAssignableFrom(ft));
            Bl("Array <- ft", typeof(Array).IsAssignableFrom(ft));
            Bl("IList<string> <- ft", typeof(IList<string>).IsAssignableFrom(ft));
            Console.WriteLine("ft.BaseType = " + ft.BaseType.Name);
            Type nt = typeof(Holder).GetField("Nums").FieldType;
            Console.WriteLine("nums ft = " + nt.FullName + " IsArray=" + nt.IsArray);
            Console.WriteLine("contract(ft) = " + Classify(ft));
            Console.WriteLine("contract(typeof(string[])) = " + Classify(typeof(string[])));

            // -- Newtonsoft's post-classification flow, transcribed
            //    (JsonArrayContract ctor + CreateNewList + the IsArray finalization):
            //    CollectionItemType = GetElementType(); a List<elem> temporary via
            //    MakeGenericType + Activator (the instantiation must be AOT-visible —
            //    seeded here the way a real program's own use would); populate;
            //    Array.CreateInstance(elem, n); IList.CopyTo; FieldInfo.SetValue.
            //    elem is now the REAL String, so the List<string> close is what
            //    MakeGenericType must find seeded. --
            var seeded = new List<string>();
            seeded.Add("seed");
            Console.WriteLine("seed = " + seeded.Count);
            Type elem = ft.GetElementType();
            Type tempT = typeof(List<>).MakeGenericType(elem);
            IList temp = (IList)Activator.CreateInstance(tempT);
            temp.Add("alpha");
            temp.Add("beta");
            Array arr = Array.CreateInstance(elem, temp.Count);
            temp.CopyTo(arr, 0);
            var h = new Holder();
            f.SetValue(h, arr);
            string[] names = h.Names;
            Console.WriteLine("names = " + string.Join(",", names) + " len=" + names.Length);
            Console.WriteLine("stored = " + h.Names.GetType().FullName);

            // -- The Thrive Track transcription: PropertyType of enum[]/string[]
            //    auto-properties is the precise array type (so the Newtonsoft-style
            //    classification lands on the array contract, never untyped/Linq), and
            //    the array-contract deserialization flow — down to the setter-thunk
            //    store — round-trips a VALUE-element (enum) array. --
            PropertyInfo pex = typeof(Track).GetProperty("ExclusiveToContexts");
            Type pt = pex.PropertyType;
            Console.WriteLine("pt = " + pt.FullName + " IsArray=" + pt.IsArray
                + " elem=" + pt.GetElementType().Name
                + " ==typeof " + (pt == typeof(MusicContext[])));
            Console.WriteLine("contract(pt) = " + Classify(pt));
            Type dt = typeof(Track).GetProperty("DisallowInContexts").PropertyType;
            Console.WriteLine("dt = " + dt.FullName
                + " ==typeof " + (dt == typeof(string[]))
                + " contract=" + Classify(dt));
            // The array-contract flow over the enum element (seeding List<MusicContext>
            // for the MakeGenericType close, like the string flow above).
            var eseed = new List<MusicContext>();
            eseed.Add(MusicContext.Menu);
            Console.WriteLine("eseed = " + eseed.Count);
            Type eelem = pt.GetElementType();
            IList etemp = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(eelem));
            etemp.Add(Enum.Parse(eelem, "MicrobeStage"));
            etemp.Add(Enum.Parse(eelem, "MulticellularStage"));
            Array earr = Array.CreateInstance(eelem, etemp.Count);
            etemp.CopyTo(earr, 0);
            var tr = new Track();
            pex.SetValue(tr, earr);
            MusicContext[] ex = tr.ExclusiveToContexts;
            Console.WriteLine("ex = " + ex[0] + "," + ex[1] + " len=" + ex.Length
                + " ok=" + (ex[0] == MusicContext.MicrobeStage));
            Console.WriteLine("exstored = " + tr.ExclusiveToContexts.GetType().FullName);

            // -- Newtonsoft's CreateTemporaryCollection, transcribed exactly and
            //    with NO static List<Biome> anywhere in this program (the Thrive
            //    blind spot the flows above miss — see the Biome comment):
            //    array PropertyType -> GetElementType ->
            //    typeof(List<>).MakeGenericType(elem) ->
            //    GetConstructors() non-empty
            //    (LateBoundReflectionDelegateFactory.CreateDefaultConstructor;
            //    empty is the "Unable to find default constructor" throw) ->
            //    parameterless ConstructorInfo.Invoke -> populate through IList ->
            //    Array.CreateInstance + CopyTo (the ToArray finalization) ->
            //    PropertyInfo.SetValue. --
            PropertyInfo psb = typeof(Track).GetProperty("SpawnBiomes");
            Type bt = psb.PropertyType;
            Console.WriteLine("bt = " + bt.FullName + " contract=" + Classify(bt));
            Type belem = bt.GetElementType();
            Type btemp = typeof(List<>).MakeGenericType(belem);
            ConstructorInfo[] bctors = btemp.GetConstructors();
            ConstructorInfo bdef = null;
            foreach (ConstructorInfo ci in bctors)
                if (ci.GetParameters().Length == 0)
                    bdef = ci;
            Console.WriteLine("tempctors = " + (bctors.Length > 0)
                + " default=" + (bdef != null));
            IList btmp = (IList)bdef.Invoke(null);
            btmp.Add(Enum.Parse(belem, "Tidepool"));
            btmp.Add(Enum.Parse(belem, "Vents"));
            Array barr = Array.CreateInstance(belem, btmp.Count);
            btmp.CopyTo(barr, 0);
            psb.SetValue(tr, barr);
            Biome[] sb = tr.SpawnBiomes;
            Console.WriteLine("sb = " + sb[0] + "," + sb[1] + " len=" + sb.Length);
            Console.WriteLine("sbstored = " + tr.SpawnBiomes.GetType().FullName);
        }
    }
}
