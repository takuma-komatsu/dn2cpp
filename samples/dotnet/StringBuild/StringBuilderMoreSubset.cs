#nullable disable
using System;
using System.Text;

namespace StringBuilderMoreSubset
{
    // The remaining StringBuilder surface: char[]/StringBuilder Appends, slice Appends,
    // repeated/char[] Inserts, sub-int and unsigned/decimal Append/Insert, range Replace,
    // the Length setter (grow zero-pads), the indexer (get throws IndexOutOfRange, set
    // ArgumentOutOfRange), grow-only exact EnsureCapacity, provider-aware AppendFormat.
    internal static class Program
    {
        private static void E(string label, Action a)
        {
            try
            {
                a();
                Console.WriteLine(label + ": ok");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name);
            }
        }

        internal static void Run()
        {
            // --- Append(char[]) / Append(char[], int, int) ---
            var sb = new StringBuilder();
            sb.Append((char[])null);                 // null appends nothing
            sb.Append(new[] { 'a', 'b', 'c' });
            sb.Append(new[] { 'd', 'e', 'f' }, 1, 2);
            Console.WriteLine(sb.ToString() + "|" + sb.Length); // abcef|5
            E("app-null-00", () => sb.Append((char[])null, 0, 0));
            E("app-null-01", () => sb.Append((char[])null, 0, 1));
            E("app-null-10", () => sb.Append((char[])null, 1, 0));
            E("app-oob", () => sb.Append(new[] { 'x' }, 1, 1));
            E("app-negcnt", () => sb.Append(new[] { 'x' }, 0, -1));

            // --- Append(StringBuilder [, int, int]) ---
            var asb = new StringBuilder("xy");
            asb.Append((StringBuilder)null);
            asb.Append(new StringBuilder("zw"));
            asb.Append(asb); // self-append doubles
            asb.Append(new StringBuilder("cdef"), 1, 2);
            Console.WriteLine(asb.ToString()); // xyzwxyzwde
            E("appsb-null-00", () => asb.Append((StringBuilder)null, 0, 0));
            E("appsb-null-01", () => asb.Append((StringBuilder)null, 0, 1));
            E("appsb-oob", () => asb.Append(new StringBuilder("x"), 1, 1));
            E("appsb-negs", () => asb.Append(new StringBuilder("x"), -1, 0));

            // --- sub-int integer Append / Insert ---
            var si = new StringBuilder();
            si.Append((byte)200).Append(' ').Append((sbyte)-5).Append(' ');
            si.Append((short)-300).Append(' ').Append((ushort)60000);
            si.Insert(0, (byte)7).Insert(0, (short)-2);
            Console.WriteLine(si.ToString()); // -27200 -5 -300 60000

            // --- unsigned + decimal Append / Insert (must not route through the
            // signed int/long formatters; decimal keeps its scale) ---
            var ud = new StringBuilder();
            ud.Append(uint.MaxValue).Append('|').Append(ulong.MaxValue).Append('|').Append(1.100m);
            ud.Insert(0, "|").Insert(0, 3000000000u).Insert(0, "|")
              .Insert(0, 18000000000000000000UL).Insert(0, "|").Insert(0, 2.50m);
            Console.WriteLine(ud.ToString());

            // --- Insert(int, char[]) / Insert(int, char[], int, int) ---
            var ic = new StringBuilder("0123");
            ic.Insert(2, new[] { 'a', 'b' });
            ic.Insert(0, (char[])null); // null inserts nothing
            ic.Insert(1, new[] { 'x', 'y', 'z' }, 1, 2);
            Console.WriteLine(ic.ToString()); // 0yz1ab23
            E("insarr-null-oob", () => ic.Insert(99, (char[])null));
            E("insarr2-null-01", () => ic.Insert(0, (char[])null, 0, 1));
            E("insarr2-oob", () => ic.Insert(0, new[] { 'x' }, 1, 1));

            // --- Insert(int, string, int count) ---
            var isc = new StringBuilder("--");
            isc.Insert(1, "ab", 3);
            isc.Insert(0, "zz", 0);         // count 0 inserts nothing
            isc.Insert(0, (string)null, 5); // null value inserts nothing
            Console.WriteLine(isc.ToString()); // -ababab-
            E("inscnt-neg", () => isc.Insert(0, "a", -1));
            E("inscnt-oobidx", () => isc.Insert(99, "a", 1));
            E("inscnt-nullnegidx", () => isc.Insert(-1, (string)null, 0));

            // --- Replace(string, string, int, int) / Replace(char, char, int, int) ---
            var rr = new StringBuilder("abcabcabc");
            rr.Replace("bc", "X", 0, 5); // the match at 4 crosses the range end
            Console.WriteLine(rr.ToString()); // aXabcabc
            var rr2 = new StringBuilder("abcabc");
            rr2.Replace("bc", "X", 0, 2); // no fully-contained match
            rr2.Replace("a", "X", 1, 0);  // empty range replaces nothing
            Console.WriteLine(rr2.ToString()); // abcabc
            var rr3 = new StringBuilder("aaaa");
            rr3.Replace('a', 'b', 1, 2);
            Console.WriteLine(rr3.ToString()); // abba
            var rr4 = new StringBuilder("abc");
            rr4.Replace("c", null, 0, 3); // null newValue removes
            Console.WriteLine(rr4.ToString()); // ab
            E("repl-oob", () => rr.Replace("a", "b", 0, 99));
            E("repl-negs", () => rr.Replace("a", "b", -1, 1));
            E("repl-null-old", () => rr.Replace((string)null, "b", 0, 1));
            E("repl-empty-old", () => rr.Replace("", "b", 0, 1));
            E("replc-oob", () => rr3.Replace('a', 'b', 2, 99));

            // --- set_Length: truncate / zero-pad grow ---
            var sl = new StringBuilder("abc");
            sl.Length = 1;
            sl.Length = 4; // regrow zero-pads (the old content does not resurface)
            Console.WriteLine(
                (int)sl[0] + "/" + (int)sl[1] + "/" + (int)sl[2] + "/" + (int)sl[3]
                + " cap=" + sl.Capacity); // 97/0/0/0 cap=16
            var sl2 = new StringBuilder("abc");
            sl2.Length = 20; // grow past the capacity doubles it
            Console.WriteLine("grow: len=" + sl2.Length + " cap=" + sl2.Capacity
                + " last=" + (int)sl2[19]); // len=20 cap=32 last=0
            E("len-neg", () => sl.Length = -1);

            // --- the indexer: IndexOutOfRange on get, ArgumentOutOfRange on set ---
            var ix = new StringBuilder("abc");
            ix[1] = 'X';
            Console.WriteLine(ix.ToString() + "|" + ix[2]); // aXc|c
            E("get-oob", () => { char c = ix[3]; });
            E("get-neg", () => { char c = ix[-1]; });
            E("set-oob", () => ix[3] = 'x');
            E("set-neg", () => ix[-1] = 'x');

            // --- EnsureCapacity: grow-only, to exactly the requested size ---
            var ec = new StringBuilder(16);
            Console.WriteLine("ec: " + ec.EnsureCapacity(17) + "/" + ec.EnsureCapacity(5)
                + "/" + ec.EnsureCapacity(40) + " cap=" + ec.Capacity); // 17/17/40 cap=40
            E("ec-neg", () => ec.EnsureCapacity(-1));

            // --- AppendFormat(IFormatProvider, ...) 1/2/3/params forms ---
            var af = new StringBuilder();
            af.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F2}", 3.14159);
            af.Append(' ');
            af.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}+{1}", 1, 2);
            af.Append(' ');
            af.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1}{2}", 'a', 'b', 'c');
            af.Append(' ');
            af.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{3}|{2}|{1}|{0}",
                new object[] { 10, 20, 30, 40 });
            Console.WriteLine(af.ToString()); // 3.14 1+2 abc 40|30|20|10

            // --- catchable edit-op range failures ---
            var ce = new StringBuilder("abcd");
            E("remove-oob", () => ce.Remove(2, 99));
            E("insert-oob", () => ce.Insert(99, 'x'));
            Console.WriteLine(ce.ToString()); // abcd

            // Insert(index, value, count) whose value.Length * count overflows raises
            // OutOfMemoryException, not the ArgumentOutOfRangeException the neighbouring
            // guards raise: Insert computes the product and calls the OOM throw helper.
            // It is a size computation, not a failed allocation — unlike the
            // allocation-failure aborts in dn2cpp_gc.cpp, which cannot mint an object.
            var oom = new StringBuilder("seed");
            E("insert-oom-max", () => oom.Insert(0, "ab", int.MaxValue));
            E("insert-oom-1g", () => oom.Insert(0, "ab", 1 << 30));
            E("insert-oom-1ch", () => oom.Insert(0, "a", int.MaxValue));
            // Still usable, and the rejected inserts left nothing behind.
            oom.Insert(0, "x", 3);
            Console.WriteLine("oom-survivor: " + oom.ToString()); // xxxseed
        }

        private static string _stateEvaluation = "";

        private static string StateUnits(string value)
        {
            if (value is null)
                return "null";
            string result = "";
            foreach (char ch in value)
                result += ((int)ch).ToString("X4") + " ";
            return result;
        }

        private static void StateFault(string label, Exception ex)
        {
            Console.WriteLine(label + " type=" + ex.GetType().Name);
            Console.WriteLine(label + " param=" + (ex is ArgumentException arg ? arg.ParamName : null));
            Console.WriteLine(label + " message=" + ex.Message.Replace("\r", "").Replace("\n", "|"));
            object actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
            Console.WriteLine(label + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
        }

        private static void StateResult(string label, StringBuilder sb, Action action)
        {
            try { action(); Console.WriteLine(label + " success"); }
            catch (Exception ex) { StateFault(label, ex); }
            Console.WriteLine(label + " content=" + StateUnits(sb is null ? null : sb.ToString()));
        }

        private static StringBuilder StateReceiver(StringBuilder value)
        {
            _stateEvaluation += "R";
            return value;
        }

        private static int StateNumber(string step, int value, bool fail)
        {
            _stateEvaluation += step;
            if (fail)
                throw new InvalidOperationException();
            return value;
        }

        internal static void RunFaults()
        {
            Console.WriteLine("== StringBuilder state faults ==");
            string[] sources = { "ab\0\ud800", "", null };
            int[] lengths = { int.MinValue, -1, 0, 1, 4, 7, 17 };
            int[] indices = { int.MinValue, -1, 0, 1, 3, 4, 7, int.MaxValue };
            for (int i = 0; i < sources.Length; i++)
            {
                foreach (int value in lengths)
                {
                    StringBuilder length = sources[i] is null ? null : new StringBuilder(sources[i]);
                    StateResult("length:" + i + ":" + value, length, () => length.Length = value);
                    StringBuilder capacity = sources[i] is null ? null : new StringBuilder(sources[i]);
                    StateResult("ensure:" + i + ":" + value, capacity,
                        () => Console.WriteLine("ensure value=" + capacity.EnsureCapacity(value)));
                }
                foreach (int index in indices)
                {
                    StringBuilder setter = sources[i] is null ? null : new StringBuilder(sources[i]);
                    StateResult("set index:" + i + ":" + index, setter, () => setter[index] = '\ud800');
                    StringBuilder getter = sources[i] is null ? null : new StringBuilder(sources[i]);
                    StateResult("get index:" + i + ":" + index, getter,
                        () => Console.WriteLine("get unit=" + ((int)getter[index]).ToString("X4")));
                }
            }
            _stateEvaluation = "";
            try { StateReceiver(null).Length = StateNumber("V", -1, false); }
            catch (Exception ex) { StateFault("length null evaluation", ex); }
            Console.WriteLine("length null evaluation=" + _stateEvaluation);
            _stateEvaluation = "";
            try { StateReceiver(null)[StateNumber("I", -1, false)] = (char)StateNumber("V", 1, true); }
            catch (Exception ex) { StateFault("index throwing evaluation", ex); }
            Console.WriteLine("index throwing evaluation=" + _stateEvaluation);
            ArgumentOutOfRangeException saved = null;
            try { new StringBuilder("abc").EnsureCapacity(-7); }
            catch (ArgumentOutOfRangeException ex) { saved = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            StateFault("state fault after GC", saved);
            Console.WriteLine("StringBuilder state faults end");
        }
        private static void InsertFaultResult(string label, StringBuilder sb, Func<StringBuilder> action)
        {
            try { Console.WriteLine(label + " identity=" + ReferenceEquals(sb, action())); }
            catch (Exception ex) { StateFault(label, ex); }
            Console.WriteLine(label + " content=" + StateUnits(sb is null ? null : sb.ToString()));
        }

        private static string InsertFaultValue(string value)
        {
            _stateEvaluation += "V";
            return value;
        }

        internal static void RunInsertFaults()
        {
            Console.WriteLine("== StringBuilder insert faults ==");
            string[] sources = { "ab\0\ud800", "", null };
            string[] values = { null, "", "X\0\ud800", "yz" };
            int[] indices = { int.MinValue, -1, 0, 1, 3, 4, 5, int.MaxValue };
            int[] counts = { int.MinValue, -1, 0, 1, 2, 3 };
            for (int i = 0; i < sources.Length; i++)
                foreach (int index in indices)
                {
                    StringBuilder chars = sources[i] is null ? null : new StringBuilder(sources[i]);
                    InsertFaultResult("insert char:" + i + ":" + index, chars, () => chars.Insert(index, '\ud800'));
                    for (int v = 0; v < values.Length; v++)
                    {
                        StringBuilder strings = sources[i] is null ? null : new StringBuilder(sources[i]);
                        InsertFaultResult("insert string:" + i + ":" + v + ":" + index, strings, () => strings.Insert(index, values[v]));
                        foreach (int count in counts)
                        {
                            StringBuilder repeats = sources[i] is null ? null : new StringBuilder(sources[i]);
                            InsertFaultResult("insert repeat:" + i + ":" + v + ":" + index + ":" + count, repeats,
                                () => repeats.Insert(index, values[v], count));
                        }
                    }
                }
            StringBuilder overflow = new StringBuilder("seed");
            InsertFaultResult("insert overflow", overflow, () => overflow.Insert(0, "ab", int.MaxValue));
            InsertFaultResult("insert null maximum count", overflow, () => overflow.Insert(0, (string)null, int.MaxValue));
            InsertFaultResult("insert empty maximum count", overflow, () => overflow.Insert(0, "", int.MaxValue));
            _stateEvaluation = "";
            try { StateReceiver(null).Insert(StateNumber("I", -1, false), InsertFaultValue(null), StateNumber("C", -1, false)); }
            catch (Exception ex) { StateFault("insert null evaluation", ex); }
            Console.WriteLine("insert null evaluation=" + _stateEvaluation);
            _stateEvaluation = "";
            try { StateReceiver(null).Insert(StateNumber("I", -1, false), InsertFaultValue(null), StateNumber("C", -1, true)); }
            catch (Exception ex) { StateFault("insert throwing evaluation", ex); }
            Console.WriteLine("insert throwing evaluation=" + _stateEvaluation);
            ArgumentOutOfRangeException saved = null;
            try { new StringBuilder("abc").Insert(-1, (string)null, -7); }
            catch (ArgumentOutOfRangeException ex) { saved = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            StateFault("insert fault after GC", saved);
            Console.WriteLine("StringBuilder insert faults end");
        }
        private static char[] CollectionArrayValue(char[] value)
        {
            _stateEvaluation += "A";
            return value;
        }

        private static StringBuilder CollectionBuilderValue(StringBuilder value)
        {
            _stateEvaluation += "B";
            return value;
        }

        internal static void RunCollectionFaults()
        {
            Console.WriteLine("== StringBuilder collection faults ==");
            string[] sources = { "ab\0\ud800", "", null };
            char[][] arrays = { new[] { 'X', '\0', '\ud800', 'Y' }, Array.Empty<char>(), new[] { 'z' }, null };
            int[] bounds = { int.MinValue, -1, 0, 1, 4, int.MaxValue };
            for (int i = 0; i < sources.Length; i++)
                for (int a = 0; a < arrays.Length; a++)
                    foreach (int start in bounds)
                        foreach (int count in bounds)
                        {
                            StringBuilder append = sources[i] is null ? null : new StringBuilder(sources[i]);
                            InsertFaultResult("array append:" + i + ":" + a + ":" + start + ":" + count, append,
                                () => append.Append(arrays[a], start, count));
                            foreach (int index in bounds)
                            {
                                StringBuilder insert = sources[i] is null ? null : new StringBuilder(sources[i]);
                                InsertFaultResult("array insert:" + i + ":" + a + ":" + index + ":" + start + ":" + count, insert,
                                    () => insert.Insert(index, arrays[a], start, count));
                            }
                        }
            _stateEvaluation = "";
            try { StateReceiver(null).Append(CollectionArrayValue(null), StateNumber("S", -1, false), StateNumber("C", -1, false)); }
            catch (Exception ex) { StateFault("array append null evaluation", ex); }
            Console.WriteLine("array append null evaluation=" + _stateEvaluation);
            _stateEvaluation = "";
            try { StateReceiver(null).Insert(StateNumber("I", -1, false), CollectionArrayValue(null), StateNumber("S", -1, false), StateNumber("C", -1, true)); }
            catch (Exception ex) { StateFault("array insert throwing evaluation", ex); }
            Console.WriteLine("array insert throwing evaluation=" + _stateEvaluation);
            StringBuilder[] builders = { new StringBuilder("XY\0\ud800"), new StringBuilder(), null };
            for (int i = 0; i < sources.Length; i++)
                for (int v = 0; v < builders.Length; v++)
                    foreach (int start in bounds)
                        foreach (int count in bounds)
                        {
                            StringBuilder append = sources[i] is null ? null : new StringBuilder(sources[i]);
                            InsertFaultResult("builder append:" + i + ":" + v + ":" + start + ":" + count, append,
                                () => append.Append(builders[v], start, count));
                        }
            StringBuilder self = new StringBuilder("ab\0\ud800");
            InsertFaultResult("builder self append", self, () => self.Append(self, 1, 3));
            InsertFaultResult("builder zero count large start", self, () => self.Append(self, int.MaxValue, 0));
            _stateEvaluation = "";
            try { StateReceiver(null).Append(CollectionBuilderValue(null), StateNumber("S", -1, false), StateNumber("C", -1, false)); }
            catch (Exception ex) { StateFault("builder append null evaluation", ex); }
            Console.WriteLine("builder append null evaluation=" + _stateEvaluation);
            ArgumentException builderSaved = null;
            try { new StringBuilder("abc").Append((StringBuilder)null, -7, -1); }
            catch (ArgumentException ex) { builderSaved = ex; }
            ArgumentException appendSaved = null;
            ArgumentException insertSaved = null;
            try { new StringBuilder("abc").Append((char[])null, -7, -1); }
            catch (ArgumentException ex) { appendSaved = ex; }
            try { new StringBuilder("abc").Insert(0, new char[1], 0, -7); }
            catch (ArgumentException ex) { insertSaved = ex; }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            StateFault("builder append fault after GC", builderSaved);
            StateFault("array append fault after GC", appendSaved);
            StateFault("array insert fault after GC", insertSaved);
            Console.WriteLine("StringBuilder collection faults end");
        }
    }
}
