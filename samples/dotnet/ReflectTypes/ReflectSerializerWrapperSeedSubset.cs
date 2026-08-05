#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// The SERIALIZER-wrapper seed (Compilation.CollectSerializerWrapperSeeds): a
// reflection deserializer that must mutate a collection through a non-IList /
// non-IDictionary surface wraps it in the serializer's OWN adapter — Newtonsoft's
// internal Newtonsoft.Json.Utilities.CollectionWrapper<T> / DictionaryWrapper<K,V>,
// minted with typeof(CollectionWrapper<>).MakeGenericType(itemType)
// (JsonArrayContract.CreateWrapper / JsonDictionaryContract.CreateWrapper) and
// constructed through GetConstructor over the closed collection interface. Those
// are LIBRARY types, so the transpiler seeds them by (ns, name) lookup against the
// loaded modules — a no-op when nothing defines them. This section hosts stand-ins
// with the REAL adapters' (namespace, name) and ctor shapes in the gate's own
// assembly, which satisfies exactly the lookup Thrive's Newtonsoft.Json.dll does,
// and replays the two shapes that blocked Thrive's boot:
//
//  - an ICollection<string> creator argument (Tutorial.AlreadySeenTutorials'
//    SerializedData(ICollection<string>)): the contract default-creates List<string>
//    and wraps it in CollectionWrapper<string>;
//  - a List<T> creator argument (ThriveScriptsShared.VersionPatchNotes'
//    [JsonConstructor](string, List<string>, string)): observed wrapping on the
//    Thrive boot even though Newtonsoft's mirror says an IList-assignable target
//    never wraps — which is why the seed is deliberately GENEROUS for CREATOR
//    ARGUMENTS (an IEnumerable<T>-shaped ctor-parameter type seeds
//    CollectionWrapper<T> even when IList-assignable; the member surface keeps
//    the precise contract mirror).
//
// Both creator arguments are also the CREATOR-ARGUMENT surface itself: List<NoteLine>
// below is named ONLY as a ctor parameter (no field/property of it exists), so its
// resolution asserts that an openable class's instance-ctor parameter types join
// the reflection-ctor route's named surface.
//
// The positive paths match real .NET. The final section intentionally diverges:
// MakeGenericType over an element type no collection surface names stays the AOT
// boundary — real .NET constructs CollectionWrapper<Unlisted>, dn2cpp throws the
// (catchable) NotSupportedException naming the missing instantiation.

namespace Newtonsoft.Json.Utilities
{
    // Stand-in for Newtonsoft's internal CollectionWrapper<T>: same (namespace,
    // name), same two single-reference-argument public ctors, same delegating
    // behavior on the members the deserializer drives (IList.Add through the
    // wrapped ICollection<T>). Nothing constructs it statically — the seed is what
    // makes the closed instantiation exist and its ctors invokable.
    internal class CollectionWrapper<T> : IList
    {
        private readonly IList _list;
        private readonly ICollection<T> _genericCollection;

        public CollectionWrapper(IList list)
        {
            _list = list;
        }

        public CollectionWrapper(ICollection<T> list)
        {
            _genericCollection = list;
        }

        public int Add(object value)
        {
            if (_genericCollection != null)
            {
                _genericCollection.Add((T)value);
                return _genericCollection.Count - 1;
            }
            return _list.Add(value);
        }

        public void Clear()
        {
            if (_genericCollection != null)
                _genericCollection.Clear();
            else
                _list.Clear();
        }

        public bool Contains(object value)
            => _genericCollection != null ? _genericCollection.Contains((T)value) : _list.Contains(value);

        public int IndexOf(object value)
            => throw new InvalidOperationException("Wrapped ICollection<T> does not support IndexOf.");

        public void Insert(int index, object value)
            => throw new InvalidOperationException("Wrapped ICollection<T> does not support Insert.");

        public bool IsFixedSize => false;

        public bool IsReadOnly
            => _genericCollection != null ? _genericCollection.IsReadOnly : _list.IsReadOnly;

        public void Remove(object value)
        {
            if (_genericCollection != null)
                _genericCollection.Remove((T)value);
            else
                _list.Remove(value);
        }

        public void RemoveAt(int index)
            => throw new InvalidOperationException("Wrapped ICollection<T> does not support RemoveAt.");

        public object this[int index]
        {
            get => throw new InvalidOperationException("Wrapped ICollection<T> does not support indexed access.");
            set => throw new InvalidOperationException("Wrapped ICollection<T> does not support indexed access.");
        }

        public void CopyTo(Array array, int index)
            => throw new InvalidOperationException("Wrapped ICollection<T> does not support CopyTo.");

        public int Count => _genericCollection != null ? _genericCollection.Count : _list.Count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public IEnumerator GetEnumerator()
            => _genericCollection != null ? _genericCollection.GetEnumerator() : _list.GetEnumerator();

        public object UnderlyingCollection => (object)_genericCollection ?? _list;
    }

    // Stand-in for Newtonsoft's internal DictionaryWrapper<TKey, TValue>: same
    // (namespace, name), same three single-reference-argument public ctors. The
    // deserializer drives it through the non-generic IDictionary indexer.
    internal class DictionaryWrapper<TKey, TValue> : IDictionary
    {
        private readonly IDictionary _dictionary;
        private readonly IDictionary<TKey, TValue> _genericDictionary;
        private readonly IReadOnlyDictionary<TKey, TValue> _readOnlyDictionary;

        public DictionaryWrapper(IDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        public DictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        {
            _genericDictionary = dictionary;
        }

        public DictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            _readOnlyDictionary = dictionary;
        }

        public object this[object key]
        {
            get
            {
                if (_genericDictionary != null)
                    return _genericDictionary[(TKey)key];
                if (_readOnlyDictionary != null)
                    return _readOnlyDictionary[(TKey)key];
                return _dictionary[key];
            }
            set
            {
                if (_genericDictionary != null)
                    _genericDictionary[(TKey)key] = (TValue)value;
                else if (_readOnlyDictionary != null)
                    throw new NotSupportedException();
                else
                    _dictionary[key] = value;
            }
        }

        public int Count
        {
            get
            {
                if (_genericDictionary != null)
                    return _genericDictionary.Count;
                if (_readOnlyDictionary != null)
                    return _readOnlyDictionary.Count;
                return _dictionary.Count;
            }
        }

        public bool Contains(object key)
        {
            if (_genericDictionary != null)
                return _genericDictionary.ContainsKey((TKey)key);
            if (_readOnlyDictionary != null)
                return _readOnlyDictionary.ContainsKey((TKey)key);
            return _dictionary.Contains(key);
        }

        public void Add(object key, object value)
        {
            if (_genericDictionary != null)
                _genericDictionary.Add((TKey)key, (TValue)value);
            else if (_readOnlyDictionary != null)
                throw new NotSupportedException();
            else
                _dictionary.Add(key, value);
        }

        public void Clear()
            => throw new InvalidOperationException("gate stand-in: Clear unused");

        public void Remove(object key)
            => throw new InvalidOperationException("gate stand-in: Remove unused");

        public ICollection Keys
            => throw new InvalidOperationException("gate stand-in: Keys unused");

        public ICollection Values
            => throw new InvalidOperationException("gate stand-in: Values unused");

        public bool IsFixedSize => false;

        public bool IsReadOnly => _readOnlyDictionary != null;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void CopyTo(Array array, int index)
            => throw new InvalidOperationException("gate stand-in: CopyTo unused");

        public IDictionaryEnumerator GetEnumerator()
            => throw new InvalidOperationException("gate stand-in: enumeration unused");

        IEnumerator IEnumerable.GetEnumerator()
            => throw new InvalidOperationException("gate stand-in: enumeration unused");
    }
}

namespace ReflectSerializerWrapperSeedSubset
{
    // The AlreadySeenTutorials shape: an app type whose creator argument is
    // declared at ICollection<string>.
    class TutorialData
    {
        public TutorialData(ICollection<string> seen)
        {
            Seen = seen;
        }

        public ICollection<string> Seen { get; set; }
    }

    // A section-local element type, so its wrapper's existence cannot leak in from
    // any other section's surface.
    class NoteLine
    {
        public override string ToString() => "note";
    }

    // The VersionPatchNotes shape: a [JsonConstructor]-style parameterized ctor
    // whose collection argument is a concrete List<T> — and List<NoteLine> is named
    // ONLY here, as a ctor parameter (the property deliberately stores a count, not
    // the list), so resolving it asserts the creator-argument surface itself.
    class PatchNotesData
    {
        public PatchNotesData(string intro, List<NoteLine> notes, string link)
        {
            Intro = intro;
            NoteCount = notes.Count;
            Link = link;
        }

        public string Intro { get; private set; }
        public int NoteCount { get; private set; }
        public string Link { get; private set; }
    }

    // A dictionary member declared at the interface: an existing-value populate
    // wraps through the DECLARED IDictionary<K,V> contract, so the interface seeds
    // DictionaryWrapper<K,V>.
    class CountsData
    {
        public IDictionary<string, int> Counts { get; set; }
    }

    // Never an element of any collection on any surface: CollectionWrapper<Unlisted>
    // stays unseeded — the AOT boundary the final section asserts.
    class Unlisted
    {
    }

    class Program
    {
        internal static void Run()
        {
            // -- Site 2's shape: ICollection<string> creator argument. The contract
            // default-creates the materialization (List<string>, the existing BCL
            // seed) and wraps it: MakeGenericType(CollectionWrapper<>) -> GetConstructor
            // over the closed ICollection<string> (obtained the way CreateWrapper
            // obtains it, MakeGenericType over the open interface) -> ctor.Invoke. --
            ConstructorInfo tutorialCtor = typeof(TutorialData).GetConstructors()[0];
            Type seenType = tutorialCtor.GetParameters()[0].ParameterType;
            Type seenItem = seenType.GetGenericArguments()[0];
            Console.WriteLine("seen=" + seenType.Name + " item=" + seenItem.Name);

            object seenList = Activator.CreateInstance(typeof(List<>).MakeGenericType(seenItem));
            Type seenWrapType = typeof(Newtonsoft.Json.Utilities.CollectionWrapper<>)
                .MakeGenericType(seenItem);
            Type seenCtorArg = typeof(ICollection<>).MakeGenericType(seenItem);
            ConstructorInfo seenWrapCtor = seenWrapType.GetConstructor(new[] { seenCtorArg });
            Console.WriteLine("wrap=" + seenWrapType.Name
                + " ctor(" + seenWrapCtor.GetParameters()[0].ParameterType.Name + ")");
            IList seenWrap = (IList)seenWrapCtor.Invoke(new object[] { seenList });
            seenWrap.Add("tutorial-a");
            seenWrap.Add("tutorial-b");
            object tutorial = tutorialCtor.Invoke(new object[] { seenList });
            ICollection<string> seen = ((TutorialData)tutorial).Seen;
            Console.WriteLine("wrapped count=" + seenWrap.Count
                + " seen count=" + seen.Count
                + " contains-a=" + seen.Contains("tutorial-a"));

            // -- Site 1's shape: a List<T> creator argument, elem named only as a
            // ctor parameter. The creator-argument arm's generosity seeds
            // CollectionWrapper<NoteLine> even though List<T> is IList-assignable
            // (a member-surface List<T> would not seed). --
            ConstructorInfo notesCtor = typeof(PatchNotesData).GetConstructors()[0];
            Type notesType = notesCtor.GetParameters()[1].ParameterType;
            Type noteItem = notesType.GetGenericArguments()[0];
            object notes = Activator.CreateInstance(notesType);
            Type notesWrapType = typeof(Newtonsoft.Json.Utilities.CollectionWrapper<>)
                .MakeGenericType(noteItem);
            IList notesWrap = (IList)notesWrapType
                .GetConstructor(new[] { typeof(ICollection<>).MakeGenericType(noteItem) })
                .Invoke(new object[] { notes });
            notesWrap.Add(new NoteLine());
            notesWrap.Add(new NoteLine());
            notesWrap.Add(new NoteLine());
            object patchNotes = notesCtor.Invoke(new object[] { "v1", notes, "link" });
            Console.WriteLine("notes=" + notesType.Name + " item=" + noteItem.Name
                + " wrapped=" + notesWrap.Count
                + " bound count=" + ((PatchNotesData)patchNotes).NoteCount);

            // -- Dictionary side: a member declared at IDictionary<K,V> seeds
            // DictionaryWrapper<K,V>; the deserializer populates through the
            // non-generic IDictionary indexer. --
            Type countsType = typeof(CountsData).GetProperty("Counts").PropertyType;
            Type[] kv = countsType.GetGenericArguments();
            object dict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(kv));
            Type dictWrapType = typeof(Newtonsoft.Json.Utilities.DictionaryWrapper<,>)
                .MakeGenericType(kv);
            ConstructorInfo dictWrapCtor = dictWrapType.GetConstructor(new[] { countsType });
            IDictionary dictWrap = (IDictionary)dictWrapCtor.Invoke(new object[] { dict });
            dictWrap["a"] = 1;
            dictWrap["b"] = 2;
            Console.WriteLine("dictwrap=" + dictWrapType.Name
                + " ctor(" + dictWrapCtor.GetParameters()[0].ParameterType.Name + ")"
                + " count=" + dictWrap.Count
                + " a=" + dictWrap["a"] + " contains-b=" + dictWrap.Contains("b"));

            // -- The boundary that stays: an element type no collection surface
            // names. Real .NET constructs it; dn2cpp throws the catchable
            // NotSupportedException naming the missing instantiation. --
            try
            {
                Type bad = typeof(Newtonsoft.Json.Utilities.CollectionWrapper<>)
                    .MakeGenericType(typeof(Unlisted));
                Console.WriteLine("wrap<Unlisted>: created " + bad.Name);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("wrap<Unlisted>: NotSupportedException: " + e.Message);
            }
        }
    }
}
