#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

// The canonical-wrapper seed (Compilation.ReachUserSurfaceNamedSpecializationCtors /
// CollectionInterfaceWrapperDefs): when the app's reflection-visible member surface
// declares an abstract BCL collection INTERFACE and the program uses late-bound
// construction (Activator.CreateInstance(Type) / ConstructorInfo.Invoke), the
// transpiler seeds the interface's canonical BCL materialization — List<T> for
// IEnumerable<T>/ICollection<T>/IList<T>, List<T> + ReadOnlyCollection<T> for
// IReadOnlyCollection<T>/IReadOnlyList<T>, HashSet<T> for ISet<T>, Dictionary<K,V>
// for IDictionary<K,V>, Dictionary<K,V> + ReadOnlyDictionary<K,V> for
// IReadOnlyDictionary<K,V> — so the typeof(Def<>).MakeGenericType(itemType) a
// reflection deserializer performs (Newtonsoft's JsonArrayContract /
// JsonDictionaryContract CreatedType computation) resolves, and the wrapper's
// ctor is invokable. This section replays the exact shape that blocked Thrive's
// AchievementsManager.PerformLoad: an IReadOnlyList<int> member driven through
// MakeGenericType(List<>) -> Activator temporary -> MakeGenericType(
// ReadOnlyCollection<>) -> ctor.Invoke(temporary).
//
// The positive paths match real .NET. The final section intentionally diverges:
// MakeGenericType over an instantiation nothing seeds or names statically stays
// the AOT boundary — real .NET constructs ReadOnlyCollection<double>, dn2cpp
// throws the (catchable) NotSupportedException whose message names the missing
// instantiation; the frozen snapshot asserts that message.

namespace ReflectMakeGenericSeedSubset
{
    class Creature
    {
        public override string ToString() => "creature";
    }

    // The reflection-visible surface: members declared at abstract collection
    // interfaces, exactly how a serialized game type declares them.
    class SaveData
    {
        private IReadOnlyList<int> unlockedIds;
        public IReadOnlyList<int> UnlockedIds => unlockedIds;
        public IList<Creature> Roster { get; set; }
        public IReadOnlyDictionary<string, int> Counts { get; set; }
        public ISet<string> Tags { get; set; }
        public SaveData(IReadOnlyList<int> ids) { unlockedIds = ids; }
    }

    class Program
    {
        internal static void Run()
        {
            // -- The Thrive path: IReadOnlyList<int> member, Newtonsoft-style. --
            // JsonArrayContract: itemType off the member type, the temporary
            // List<T> minted with MakeGenericType + Activator, CreatedType =
            // ReadOnlyCollection<T> minted with MakeGenericType, then its single
            // IList<T> ctor invoked with the temporary.
            Type member = typeof(SaveData).GetProperty("UnlockedIds").PropertyType;
            Type itemType = member.GetGenericArguments()[0];
            Console.WriteLine("member=" + member.Name + " item=" + itemType.Name);

            Type tempType = typeof(List<>).MakeGenericType(itemType);
            IList<int> temp = (IList<int>)Activator.CreateInstance(tempType);
            temp.Add(3);
            temp.Add(14);
            Console.WriteLine("temp=" + tempType.Name + " count=" + temp.Count);

            Type createdType = typeof(ReadOnlyCollection<>).MakeGenericType(itemType);
            Console.WriteLine("created=" + createdType.Name
                + " constructed=" + createdType.IsConstructedGenericType);
            ConstructorInfo[] rocCtors = createdType.GetConstructors();
            Console.WriteLine("roc ctors=" + rocCtors.Length
                + " params=" + rocCtors[0].GetParameters().Length);
            object ro = rocCtors[0].Invoke(new object[] { temp });
            IReadOnlyList<int> rl = (IReadOnlyList<int>)ro;
            Console.WriteLine("ro count=" + rl.Count + " [0]=" + rl[0] + " [1]=" + rl[1]);
            var sd = new SaveData(rl);
            Console.WriteLine("sd first=" + sd.UnlockedIds[0]);

            // -- IList<T> member over an app element type nothing lists statically:
            // CreatedType = List<T>, constructed parameterless via Activator. --
            Type rosterItem = typeof(SaveData).GetProperty("Roster").PropertyType
                .GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(rosterItem);
            IList<Creature> roster = (IList<Creature>)Activator.CreateInstance(listType);
            roster.Add(new Creature());
            Console.WriteLine("roster=" + listType.Name + " count=" + roster.Count
                + " [0]=" + roster[0]);

            // -- IReadOnlyDictionary<K,V> member: Dictionary<K,V> temporary +
            // ReadOnlyDictionary<K,V> wrapper, the JsonDictionaryContract analogue. --
            Type[] kv = typeof(SaveData).GetProperty("Counts").PropertyType
                .GetGenericArguments();
            Type dictType = typeof(Dictionary<,>).MakeGenericType(kv);
            IDictionary<string, int> d =
                (IDictionary<string, int>)Activator.CreateInstance(dictType);
            d["a"] = 1;
            d["b"] = 2;
            Type rodType = typeof(ReadOnlyDictionary<,>).MakeGenericType(kv);
            ConstructorInfo rodCtor = rodType.GetConstructors()[0];
            IReadOnlyDictionary<string, int> rd =
                (IReadOnlyDictionary<string, int>)rodCtor.Invoke(new object[] { d });
            Console.WriteLine("rod=" + rodType.Name + " count=" + rd.Count
                + " a=" + rd["a"] + " b=" + rd["b"]);

            // -- ISet<T> member: CreatedType = HashSet<T>. --
            Type setItem = typeof(SaveData).GetProperty("Tags").PropertyType
                .GetGenericArguments()[0];
            Type setType = typeof(HashSet<>).MakeGenericType(setItem);
            ISet<string> tags = (ISet<string>)Activator.CreateInstance(setType);
            tags.Add("alpha");
            tags.Add("alpha");
            Console.WriteLine("tags=" + setType.Name + " count=" + tags.Count);

            // -- The boundary that stays: an instantiation nothing seeds or names.
            // Real .NET constructs it; dn2cpp throws the catchable
            // NotSupportedException whose message names the missing instantiation. --
            try
            {
                Type bad = typeof(ReadOnlyCollection<>).MakeGenericType(typeof(double));
                Console.WriteLine("roc<double>: created " + bad.Name);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("roc<double>: NotSupportedException: " + e.Message);
            }
        }
    }
}
