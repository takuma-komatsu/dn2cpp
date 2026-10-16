#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

// A recognized MakeGenericType site over arguments the program's own reflection
// computes: a member declared at an abstract BCL collection interface names its item
// types through PropertyType.GetGenericArguments(), and typeof(Def<>).MakeGenericType
// over them takes an instantiation the image already has, else the definition's
// runtime template, else a static instantiation when the known arguments meet the
// constraints, so the late-bound construction after it (Activator.CreateInstance(Type),
// ConstructorInfo.Invoke) runs. List<T>, ReadOnlyCollection<T>, HashSet<T>,
// Dictionary<K,V> and ReadOnlyDictionary<K,V> are built the way a reflection
// deserializer (Newtonsoft's JsonArrayContract / JsonDictionaryContract CreatedType
// computation) builds them, replaying the shape that blocked Thrive's
// AchievementsManager.PerformLoad: an IReadOnlyList<int> member driven through
// MakeGenericType(List<>) -> Activator temporary -> MakeGenericType(
// ReadOnlyCollection<>) -> ctor.Invoke(temporary).
//
// Every line matches real .NET, including ReadOnlyCollection<double>, which no
// member names and whose argument is a typeof constant at the site.

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

            // -- An instantiation nothing else names: its known argument instantiates
            // it statically. A miss would throw the catchable NotSupportedException. --
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
