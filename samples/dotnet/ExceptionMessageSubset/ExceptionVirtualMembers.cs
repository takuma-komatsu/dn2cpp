using System;
using System.Collections;
using System.Collections.Generic;

namespace ExceptionMessageSubset
{
    // The members an exception type declares ITSELF — ArgumentException.ParamName (a
    // virtual property), ArgumentOutOfRangeException.ActualValue, FileNotFoundException
    // .FileName — read off an exception the program constructed with `new`.
    //
    // Every one of them used to be a silent miscompile, and ParamName was a SIGSEGV:
    // the exception newobj was intercepted to the runtime's uniform allocator, which
    // stamped the type-info handle the RUNTIME owns for these BCL types. That handle
    // knew a name and a base and nothing else — no vtable (so `callvirt get_ParamName`
    // loaded slot 13 off a null table and jumped to 0x68), no instance size (so the
    // object had no storage for _paramName even if something had written it) and no
    // reflection tables — and the real ctor, the one that writes _paramName, was
    // skipped as having "nothing to run". The type-info now carries the emitted vtable,
    // size and tables (dn2cpp_type_binds, applied by dn2cpp_runtime_init) and the ctor
    // runs, so the object is complete whichever side allocated it — and it stays ONE
    // type-info, which is what keeps `catch`/`is`/`typeof` agreeing about it.
    //
    // .Message on the Argument*/FileNotFound family is exact now: the real ctor
    // chain runs so the base Exception::.ctor lands the BCL-computed message (a real
    // resource string recovered from the embedded .resources blob), and the used-virtual
    // reach pulls in each type's get_Message OVERRIDE — ArgumentException appending
    // "(Parameter 'x')", ArgumentOutOfRangeException adding "Actual value was N.",
    // FileNotFoundException building its text — dispatched even through a base-typed
    // (System.Exception) receiver. Those reads are exercised below.
    internal static class ExceptionVirtualMembers
    {
        private sealed class ChangingMessage : Exception
        {
            internal int Reads;
            internal readonly string Label;
            internal readonly bool Throws;
            internal ChangingMessage(string label, bool throws = false) : base("stored")
            {
                Label = label;
                Throws = throws;
            }
            public override string Message
            {
                get
                {
                    Reads++;
                    if (Throws)
                        throw new InvalidOperationException("message getter");
                    return Label + ":" + Reads;
                }
            }
            internal string StoredMessage() => base.Message;
        }

        private sealed class EmptyDerivedAggregate : AggregateException
        {
            internal EmptyDerivedAggregate() : base("derived base") { }
            public override string Message => "derived/" + base.Message;
            internal string BaseMessage() => base.Message;
        }

        private class DerivedAggregate : AggregateException
        {
            internal int Code = 37;
            internal string Tag = "ctor";
            internal DerivedAggregate() { }
            internal DerivedAggregate(string message) : base(message) { }
            internal DerivedAggregate(string message, Exception inner) : base(message, inner) { }
            internal DerivedAggregate(Exception[] inner) : base(inner) { }
            internal DerivedAggregate(IEnumerable<Exception> inner) : base(inner) { }
            internal DerivedAggregate(string message, Exception[] inner) : base(message, inner) { }
            internal DerivedAggregate(string message, IEnumerable<Exception> inner) : base(message, inner) { }
            internal string BaseMessage() => base.Message;
        }

        private sealed class FurtherDerivedAggregate : DerivedAggregate
        {
            internal readonly int Level;
            internal FurtherDerivedAggregate(string message, Exception[] inner) : base(message, inner)
            {
                Code = 73;
                Tag = "further";
                Level = 9;
            }
        }

        private sealed class OwnMessageAggregate : DerivedAggregate
        {
            internal int Reads;
            internal bool Throws;
            internal OwnMessageAggregate(Exception[] inner) : base("own base", inner) { }
            public override string Message
            {
                get
                {
                    Reads++;
                    if (Throws)
                        throw new InvalidOperationException("own getter");
                    return "own/" + base.Message;
                }
            }
        }

        private class ExceptionSequence : IEnumerable<Exception>
        {
            internal readonly Exception[] Values;
            internal readonly string Failure;
            internal readonly Exception Error = new InvalidOperationException("sequence fault");
            internal readonly Exception DisposeError = new InvalidOperationException("dispose fault");
            internal string Trace = "";
            internal ExceptionSequence(Exception[] values, string failure = "")
            {
                Values = values;
                Failure = failure;
            }
            public IEnumerator<Exception> GetEnumerator()
            {
                Trace += "Get/";
                if (Failure == "Get") throw Error;
                return new Enumerator(this);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private sealed class Enumerator : IEnumerator<Exception>
            {
                private readonly ExceptionSequence _owner;
                private int _index = -1;
                internal Enumerator(ExceptionSequence owner) => _owner = owner;
                public bool MoveNext()
                {
                    _owner.Trace += "Move/";
                    if (_owner.Failure == "Move") throw _owner.Error;
                    return ++_index < _owner.Values.Length;
                }
                public Exception Current
                {
                    get
                    {
                        _owner.Trace += "Current/";
                        if (_owner.Failure == "Current" || _owner.Failure == "Current+Dispose") throw _owner.Error;
                        return _owner.Values[_index];
                    }
                }
                object IEnumerator.Current => Current;
                public void Dispose()
                {
                    _owner.Trace += "Dispose/";
                    if (_owner.Failure == "Dispose" || _owner.Failure == "Current+Dispose") throw _owner.DisposeError;
                }
                public void Reset() => throw new NotSupportedException();
            }
        }

        private sealed class ExceptionCollection : ExceptionSequence, ICollection<Exception>
        {
            internal ExceptionCollection(Exception[] values) : base(values, "Get") { }
            public int Count { get { Trace += "Count/"; return Values.Length; } }
            public bool IsReadOnly => true;
            public void CopyTo(Exception[] array, int index)
            {
                Trace += "Copy/";
                Array.Copy(Values, 0, array, index, Values.Length);
            }
            public void Add(Exception value) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(Exception value) => throw new NotSupportedException();
            public bool Remove(Exception value) => throw new NotSupportedException();
        }

        private static void SequenceFault(string mode)
        {
            var sequence = new ExceptionSequence(new Exception[] { new Exception("value") }, mode);
            try { _ = new AggregateException("custom", sequence); Console.WriteLine("sequence " + mode + " did not throw"); }
            catch (Exception error)
            {
                Console.WriteLine("sequence " + mode + " fault=" + error.Message
                    + " original=" + ReferenceEquals(error, sequence.Error)
                    + " dispose=" + ReferenceEquals(error, sequence.DisposeError));
            }
            Console.WriteLine("sequence " + mode + " trace=" + sequence.Trace);
        }

        internal static void RunEnumerableAggregates()
        {
            Console.WriteLine("== aggregate enumerable constructors ==");
            var first = new ChangingMessage("first");
            var second = new ChangingMessage("second");
            GC.KeepAlive(new Func<string>(() => first.Message));
            GC.KeepAlive(new Func<string>(() => second.Message));
            var values = new Exception[] { first, second };
            IEnumerable<Exception> view = values;
            var arrayAggregate = new AggregateException(view);
            var covariantValues = new[] { first, second };
            var covariantAggregate = new AggregateException((IEnumerable<Exception>)covariantValues);
            var list = new List<Exception>(values);
            var listAggregate = new AggregateException("custom", (IEnumerable<Exception>)list);
            var sequence = new ExceptionSequence(values);
            var sequenceAggregate = new AggregateException((string)null, sequence);
            var collection = new ExceptionCollection(values);
            var collectionAggregate = new AggregateException("collection", collection);
            values[0] = new Exception("replacement");
            covariantValues[0] = new ChangingMessage("replacement");
            list.Clear();
            Console.WriteLine("enumerable constructed reads=" + first.Reads + "/" + second.Reads);
            foreach (var aggregate in new[] { arrayAggregate, covariantAggregate, listAggregate, sequenceAggregate, collectionAggregate })
                Console.WriteLine("enumerable snapshot count=" + aggregate.InnerExceptions.Count
                    + " first=" + ReferenceEquals(aggregate.InnerException, first)
                    + " order=" + (aggregate.InnerExceptions.Count == 2
                        && ReferenceEquals(aggregate.InnerExceptions[0], first) && ReferenceEquals(aggregate.InnerExceptions[1], second)));
            Console.WriteLine("custom sequence trace=" + sequence.Trace);
            Console.WriteLine("collection trace=" + collection.Trace);
            ReadAggregate("enumerable first", arrayAggregate, first, second);
            ReadAggregate("enumerable second", arrayAggregate, first, second);
            ReadAggregate("enumerable custom", listAggregate, first, second);
            AggregateCtor("empty enumerable", new AggregateException((IEnumerable<Exception>)Array.Empty<Exception>()));
            AggregateCtor("empty custom sequence", new AggregateException("", new ExceptionSequence(Array.Empty<Exception>())));
            InvalidAggregate("enumerable null", () => new AggregateException((IEnumerable<Exception>)null));
            InvalidAggregate("enumerable custom null", () => new AggregateException("custom", (IEnumerable<Exception>)null));
            var nullElement = new ExceptionSequence(new Exception[] { null, first });
            InvalidAggregate("enumerable null element", () => new AggregateException(nullElement));
            Console.WriteLine("enumerable null element trace=" + nullElement.Trace + " reads=" + first.Reads);
            foreach (string mode in new[] { "Get", "Move", "Current", "Dispose", "Current+Dispose" })
                SequenceFault(mode);
            Console.WriteLine("aggregate enumerable constructors end");
        }

        private static string Reads(ChangingMessage first, ChangingMessage second)
            => first.Reads + "/" + (second is null ? 0 : second.Reads);

        private static AggregateException ConstructAggregate(string label,
            Func<AggregateException> make, ChangingMessage first, ChangingMessage second = null)
        {
            GC.KeepAlive(new Func<string>(() => first.Message));
            if (second is not null)
                GC.KeepAlive(new Func<string>(() => second.Message));
            try
            {
                var aggregate = make();
                Console.WriteLine(label + " constructed reads=" + Reads(first, second)
                    + " first=" + ReferenceEquals(aggregate.InnerException, first)
                    + " order=" + (ReferenceEquals(aggregate.InnerExceptions[0], first)
                        && (second is null || ReferenceEquals(aggregate.InnerExceptions[1], second))));
                return aggregate;
            }
            catch (Exception error)
            {
                Console.WriteLine(label + " constructor threw=" + error.GetType().Name
                    + " reads=" + Reads(first, second));
                return null;
            }
        }

        private static void ReadAggregate(string label, Exception aggregate,
            ChangingMessage first, ChangingMessage second = null)
        {
            if (aggregate is null)
                return;
            try { Console.WriteLine(label + " message=" + aggregate.Message); }
            catch (Exception error) { Console.WriteLine(label + " getter threw=" + error.GetType().Name + ":" + error.Message); }
            Console.WriteLine(label + " reads=" + Reads(first, second));
        }

        private static void AggregateCtor(string label, AggregateException aggregate)
            => Console.WriteLine("ctor " + label + "=" + aggregate.Message
                + " count=" + aggregate.InnerExceptions.Count);

        private static void InvalidAggregate(string label, Func<AggregateException> make)
        {
            try { _ = make(); Console.WriteLine(label + " did not throw"); }
            catch (ArgumentException error) { Console.WriteLine(label + "=" + error.GetType().Name + " param=" + error.ParamName); }
        }

        internal static void RunAggregateMessages()
        {
            Console.WriteLine("== lazy aggregate Message ==");
            var left = new ChangingMessage("left");
            var right = new ChangingMessage("right");
            var pair = ConstructAggregate("pair", () => new AggregateException(new Exception[] { left, right }), left, right);
            ReadAggregate("pair first", pair, left, right);
            ReadAggregate("pair second", pair, left, right);
            Console.WriteLine("ordinary stored=" + left.StoredMessage() + " reads=" + left.Reads);

            var throwing = new ChangingMessage("throw", true);
            var later = new ChangingMessage("later");
            var stopped = ConstructAggregate("throwing", () => new AggregateException(new Exception[] { throwing, later }), throwing, later);
            ReadAggregate("throwing first", stopped, throwing, later);
            ReadAggregate("throwing second", stopped, throwing, later);

            var nestedFault = new ChangingMessage("nested");
            GC.KeepAlive(new Func<string>(() => nestedFault.Message));
            var nested = new AggregateException(new Exception[] { nestedFault });
            var outer = new AggregateException("outer", new Exception[] { nested, new Exception("tail") });
            Console.WriteLine("nested constructed reads=" + nestedFault.Reads + " identity=" + ReferenceEquals(outer.InnerException, nested));
            ReadAggregate("nested first", outer, nestedFault);
            ReadAggregate("nested second", outer, nestedFault);

            AggregateCtor("default", new AggregateException());
            AggregateCtor("empty array", new AggregateException(Array.Empty<Exception>()));
            AggregateCtor("custom empty", new AggregateException("custom"));
            AggregateCtor("empty message", new AggregateException(""));
            AggregateCtor("null message", new AggregateException((string)null));
            AggregateCtor("custom single", new AggregateException("custom", new Exception("single")));
            AggregateCtor("custom array", new AggregateException("custom", new Exception[] { new Exception("one"), new Exception("two") }));
            AggregateCtor("null array message", new AggregateException((string)null, new Exception[] { new Exception("one") }));
            var original = new Exception("original");
            var arguments = new[] { original };
            var snapshot = new AggregateException(arguments);
            arguments[0] = new Exception("replacement");
            Console.WriteLine("ctor snapshot=" + snapshot.Message + " first=" + ReferenceEquals(snapshot.InnerException, original)
                + " element=" + ReferenceEquals(snapshot.InnerExceptions[0], original));
            InvalidAggregate("ctor null array", () => new AggregateException((Exception[])null));
            InvalidAggregate("ctor null element", () => new AggregateException(new Exception[] { null }));
            InvalidAggregate("ctor null single", () => new AggregateException("custom", (Exception)null));

            var codeUnits = new AggregateException("base\udfff", new Exception[] { new Exception("x\0\ud800y") }).Message;
            string units = "";
            foreach (char ch in codeUnits)
                units += ((int)ch).ToString("X4") + " ";
            Console.WriteLine("aggregate UTF16=" + units.TrimEnd());
            var derived = new EmptyDerivedAggregate();
            Exception asException = derived;
            Console.WriteLine("derived Message=" + asException.Message + " base=" + derived.BaseMessage());
            Console.WriteLine("derived ToString=" + asException.ToString());
            Console.WriteLine("empty ToString=" + new AggregateException("empty").ToString());
            Console.WriteLine("lazy aggregate Message end");
        }

        private static void DerivedState(string label, DerivedAggregate aggregate,
            Exception first = null, Exception second = null)
        {
            Console.WriteLine(label + " fields=" + aggregate.Code + "/" + aggregate.Tag);
            try
            {
                var inner = aggregate.InnerExceptions;
                int count = second is not null ? 2 : first is not null ? 1 : 0;
                Console.WriteLine(label + " count=" + inner.Count
                    + " first=" + ReferenceEquals(aggregate.InnerException, first)
                    + " order=" + (inner.Count == count && (count == 0 || ReferenceEquals(inner[0], first))
                        && (count < 2 || ReferenceEquals(inner[1], second)))
                    + " cached=" + ReferenceEquals(inner, aggregate.InnerExceptions));
            }
            catch (Exception error)
            {
                Console.WriteLine(label + " collection threw=" + error.GetType().Name);
            }
        }

        private static void DerivedCtor(string label, DerivedAggregate aggregate,
            Exception first = null, Exception second = null)
        {
            DerivedState("derived ctor " + label, aggregate, first, second);
            Console.WriteLine("derived ctor " + label + " message=" + aggregate.Message);
        }

        private static void ReadDerivedBase(string label, DerivedAggregate aggregate,
            ChangingMessage first, ChangingMessage second)
        {
            try { Console.WriteLine(label + " message=" + aggregate.BaseMessage()); }
            catch (Exception error) { Console.WriteLine(label + " getter threw=" + error.GetType().Name + ":" + error.Message); }
            Console.WriteLine(label + " reads=" + Reads(first, second));
        }

        private static void DerivedSequenceFault(string mode)
        {
            var sequence = new ExceptionSequence(new Exception[] { new Exception("value") }, mode);
            try { _ = new DerivedAggregate("custom", sequence); Console.WriteLine("derived sequence " + mode + " did not throw"); }
            catch (Exception error)
            {
                Console.WriteLine("derived sequence " + mode + " fault=" + error.Message
                    + " original=" + ReferenceEquals(error, sequence.Error)
                    + " dispose=" + ReferenceEquals(error, sequence.DisposeError));
            }
            Console.WriteLine("derived sequence " + mode + " trace=" + sequence.Trace);
        }

        internal static void RunDerivedAggregates()
        {
            Console.WriteLine("== derived aggregate constructors and getters ==");
            var first = new Exception("first");
            var second = new Exception("second");
            var pair = new[] { first, second };
            DerivedCtor("default", new DerivedAggregate());
            DerivedCtor("message", new DerivedAggregate("custom"));
            DerivedCtor("single", new DerivedAggregate("custom", first), first);
            DerivedCtor("array", new DerivedAggregate(pair), first, second);
            DerivedCtor("enumerable", new DerivedAggregate((IEnumerable<Exception>)pair), first, second);
            DerivedCtor("message array", new DerivedAggregate("custom", pair), first, second);
            DerivedCtor("message enumerable", new DerivedAggregate("custom", (IEnumerable<Exception>)pair), first, second);
            DerivedCtor("null message", new DerivedAggregate((string)null));
            DerivedCtor("null single message", new DerivedAggregate(null, first), first);
            DerivedCtor("null array message", new DerivedAggregate(null, pair), first, second);
            DerivedCtor("null enumerable message", new DerivedAggregate(null, (IEnumerable<Exception>)pair), first, second);
            DerivedCtor("empty message", new DerivedAggregate(""));
            DerivedCtor("empty array", new DerivedAggregate(Array.Empty<Exception>()));
            DerivedCtor("empty enumerable", new DerivedAggregate((IEnumerable<Exception>)Array.Empty<Exception>()));

            var array = new[] { first, second };
            var snapshot = new DerivedAggregate("snapshot", array);
            array[0] = new Exception("replacement");
            var list = new List<Exception>(pair);
            var fromList = new DerivedAggregate("list", (IEnumerable<Exception>)list);
            list.Clear();
            var sequence = new ExceptionSequence(pair);
            var fromSequence = new DerivedAggregate(sequence);
            var collection = new ExceptionCollection(pair);
            var fromCollection = new DerivedAggregate("collection", collection);
            DerivedCtor("snapshot", snapshot, first, second);
            DerivedCtor("list snapshot", fromList, first, second);
            DerivedCtor("custom sequence", fromSequence, first, second);
            DerivedCtor("custom collection", fromCollection, first, second);
            Console.WriteLine("derived custom sequence trace=" + sequence.Trace);
            Console.WriteLine("derived custom collection trace=" + collection.Trace);
            snapshot.Code = 41;
            snapshot.Tag = "after";
            DerivedState("derived rewritten", snapshot, first, second);
            try
            {
                IList<Exception> readOnly = snapshot.InnerExceptions;
                Console.WriteLine("derived read-only=" + readOnly.IsReadOnly);
                try { readOnly.Add(first); Console.WriteLine("derived read-only add succeeded"); }
                catch (NotSupportedException) { Console.WriteLine("derived read-only add=NotSupportedException"); }
                Console.WriteLine("derived read-only count=" + readOnly.Count);
            }
            catch (Exception error) { Console.WriteLine("derived read-only threw=" + error.GetType().Name); }

            InvalidAggregate("derived null array", () => new DerivedAggregate((Exception[])null));
            InvalidAggregate("derived message null array", () => new DerivedAggregate("custom", (Exception[])null));
            InvalidAggregate("derived null enumerable", () => new DerivedAggregate((IEnumerable<Exception>)null));
            InvalidAggregate("derived message null enumerable", () => new DerivedAggregate("custom", (IEnumerable<Exception>)null));
            InvalidAggregate("derived null single", () => new DerivedAggregate("custom", (Exception)null));
            InvalidAggregate("derived null element", () => new DerivedAggregate(new Exception[] { first, null }));
            InvalidAggregate("derived enumerable null element", () => new DerivedAggregate(new ExceptionSequence(new Exception[] { null, first })));
            foreach (string mode in new[] { "Get", "Move", "Current", "Dispose", "Current+Dispose" })
                DerivedSequenceFault(mode);

            var left = new ChangingMessage("derived-left");
            var right = new ChangingMessage("derived-right");
            var lazy = new DerivedAggregate("lazy", new Exception[] { left, right });
            DerivedState("derived lazy", lazy, left, right);
            Console.WriteLine("derived constructed reads=" + Reads(left, right));
            ReadAggregate("derived first", lazy, left, right);
            ReadAggregate("derived second", lazy, left, right);
            ReadDerivedBase("derived nonvirtual base", lazy, left, right);

            var ownLeft = new ChangingMessage("own-left");
            var ownRight = new ChangingMessage("own-right");
            var own = new OwnMessageAggregate(new Exception[] { ownLeft, ownRight });
            DerivedState("derived own", own, ownLeft, ownRight);
            Console.WriteLine("derived own constructed reads=" + own.Reads + "/" + Reads(ownLeft, ownRight));
            ReadAggregate("derived own virtual", own, ownLeft, ownRight);
            ReadDerivedBase("derived own nonvirtual base", own, ownLeft, ownRight);
            Console.WriteLine("derived own getter reads=" + own.Reads);
            own.Throws = true;
            ReadAggregate("derived own fault", own, ownLeft, ownRight);
            Console.WriteLine("derived own fault getter reads=" + own.Reads);

            var throwing = new ChangingMessage("throw", true);
            var later = new ChangingMessage("later");
            var faults = new FurtherDerivedAggregate("faults", new Exception[] { throwing, later });
            DerivedState("derived further", faults, throwing, later);
            Console.WriteLine("derived further level=" + faults.Level + " constructed reads=" + Reads(throwing, later));
            ReadAggregate("derived inner fault", faults, throwing, later);
            ReadDerivedBase("derived base inner fault", faults, throwing, later);
            try { throw snapshot; }
            catch (AggregateException caught)
            {
                Console.WriteLine("derived caught identity=" + ReferenceEquals(caught, snapshot)
                    + " first=" + ReferenceEquals(caught.InnerException, first));
            }
            Console.WriteLine("derived aggregate constructors and getters end");
        }

        // A user exception deriving from a BCL exception that the runtime also raises:
        // its own type-info chains to the runtime's handle for the base, so `catch
        // (ArgumentException)` still sees it — and its OVERRIDE of a base virtual is the
        // one that must dispatch.
        internal sealed class TaggedArgumentException : ArgumentException
        {
            private readonly string _tag;
            public TaggedArgumentException(string message, string paramName, string tag)
                : base(message, paramName) => _tag = tag;
            public override string ParamName => base.ParamName + "/" + _tag;
        }

        private static void P(string label, object v) => Console.WriteLine("vm " + label + ": " + (v ?? "<null>"));

        internal static void Run()
        {
            // ArgumentNullException(string paramName) — the crash case. ParamName is a
            // virtual property DECLARED ON THE BASE (ArgumentException), so the read is a
            // callvirt through a vtable slot the receiver's type-info has to carry.
            var ane = new ArgumentNullException("p");
            P("ane.ParamName", ane.ParamName);
            // .Message: base "Value cannot be null." + the get_Message override's
            // "(Parameter 'p')". The (c) route too — a base-typed receiver whose callee
            // resolves to System.Exception::get_Message still dispatches the override.
            P("ane.Message", ane.Message);
            P("ane via (Exception).Message", ((Exception)ane).Message);
            P("ane.InnerException", ane.InnerException);
            P("ane.GetType().Name", ane.GetType().Name);
            P("ane.GetType().FullName", ane.GetType().FullName);
            // One type-info per type: the handle typeof() names is the handle the object
            // carries. Two (an emitted ti_ beside the runtime's) and this is false.
            P("ane.GetType() == typeof", ane.GetType() == typeof(ArgumentNullException));
            P("ane is ArgumentException", ane is ArgumentException);
            P("ane is SystemException", ane is SystemException);
            P("ane is Exception", ane is Exception);

            // ArgumentException's own ctor shapes: (message) leaves ParamName null,
            // (message, paramName) sets it. Both messages are exact.
            var ae1 = new ArgumentException("just a message");
            P("ae1.ParamName", ae1.ParamName);
            P("ae1.Message", ae1.Message);
            var ae2 = new ArgumentException("m", "pn");
            P("ae2.ParamName", ae2.ParamName);
            // (message, paramName) shape: Message is "m (Parameter 'pn')".
            P("ae2.Message", ae2.Message);

            // ArgumentOutOfRangeException.ActualValue: an object-typed field the ctor
            // boxes into — a second declared field, past _paramName, on an allocation
            // that used to be the bare message/inner prefix.
            var aoore = new ArgumentOutOfRangeException("idx", 42, "out of range");
            P("aoore.ParamName", aoore.ParamName);
            // Message chains the base ArgumentException override ("out of range (Parameter
            // 'idx')") and appends its own "Actual value was 42." on a second line.
            P("aoore.Message", aoore.Message);
            P("aoore.ActualValue", aoore.ActualValue);
            P("aoore.ActualValue.GetType().Name", aoore.ActualValue.GetType().Name);
            P("aoore is ArgumentException", aoore is ArgumentException);
            var aooreNoValue = new ArgumentOutOfRangeException("idx2");
            P("aooreNoValue.ParamName", aooreNoValue.ParamName);
            P("aooreNoValue.ActualValue", aooreNoValue.ActualValue);

            // FileNotFoundException.FileName: same shape, a different BCL family.
            var fnf = new System.IO.FileNotFoundException("not found", "f.txt");
            P("fnf.FileName", fnf.FileName);
            // FileNotFoundException.get_Message dispatches its own override (which reads
            // the base _message field): with a message set, it returns it verbatim.
            P("fnf.Message", fnf.Message);
            P("fnf is IOException", fnf is System.IO.IOException);

            // InnerException chains through the same objects.
            var outer = new InvalidOperationException("outer", new FormatException("inner"));
            P("outer.Message", outer.Message);
            P("outer.InnerException.Message", outer.InnerException.Message);
            P("outer.InnerException.GetType().Name", outer.InnerException.GetType().Name);
            P("outer.InnerException.InnerException", outer.InnerException.InnerException);

            // Thrown and caught: the same object, reached through each of its base types.
            try
            {
                throw new ArgumentNullException("thrown");
            }
            catch (ArgumentException e)
            {
                P("caught as ArgumentException, ParamName", e.ParamName);
                P("caught, GetType().Name", e.GetType().Name);
            }
            try
            {
                throw new ArgumentOutOfRangeException("ti", 7, "why");
            }
            catch (Exception e)
            {
                P("caught as Exception, GetType().Name", e.GetType().Name);
                P("caught as Exception, downcast ActualValue", ((ArgumentOutOfRangeException)e).ActualValue);
                // The (c) route: `e` is statically System.Exception, so `e.Message`
                // resolves to the intrinsic base getter, which must still dispatch the
                // ArgumentOutOfRangeException override through the vtable.
                P("caught as Exception, Message", e.Message);
            }
            try
            {
                throw new ArgumentException("sys", "sp");
            }
            catch (SystemException e)
            {
                P("caught as SystemException, ParamName", ((ArgumentException)e).ParamName);
            }

            // A user override of a BCL exception's virtual, dispatched through the base
            // declaration's slot — and through a base-typed receiver.
            var tagged = new TaggedArgumentException("m", "up", "tag");
            P("tagged.ParamName", tagged.ParamName);
            ArgumentException asBase = tagged;
            P("tagged via base.ParamName", asBase.ParamName);
            try
            {
                throw tagged;
            }
            catch (ArgumentException e)
            {
                P("tagged caught as ArgumentException, ParamName", e.ParamName);
            }
        }
    }
}
