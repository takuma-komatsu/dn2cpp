#nullable enable
using System;
using System.Runtime.InteropServices;

// A delegate the native STORES and invokes on a LATER call, from a different stack frame —
// the collision-filter / error-callback idiom. A synchronous thread-local slot cannot serve
// that lifetime, since it is restored when the install call returns, so every delegate
// P/Invoke parameter goes through the persistent GC-rooted thunk pool instead. The managed
// side still roots the delegate itself, as any correct .NET program must.
namespace PInvokeStoredCallbackSubset;

internal static class Program
{
    private delegate int StoredCb(int x);

    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_store_cb(StoredCb fn);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_invoke_stored(int x);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_invoke_stored_thread(int x);

    private static StoredCb? s_root;

    private static int Triple(int x) => x * 3;

    // The collision-filter shape, bool(ref BlittableStruct), over a nested struct so the
    // byref arm exercises a struct-in-struct field graph.
    [StructLayout(LayoutKind.Sequential)]
    private struct Ent
    {
        public int Id;
        public int WorldId;
        public int Version;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FilterInput
    {
        public Ent Entity;
        public int A;
        public int B;
        public double Scale;
        public byte Flag;
    }

    private delegate bool FilterCb(ref FilterInput input);

    [DllImport("dn2cpptest")]
    private static extern void dn2cpptest_filter_install(FilterCb fn);
    [DllImport("dn2cpptest")]
    private static extern int dn2cpptest_filter_run(int a, int b);

    private static FilterCb? s_filter;

    private static bool Filter(ref FilterInput input)
    {
        input.Scale = input.Entity.Id + input.B;
        input.Flag = (byte)(input.Entity.Id % 2);
        return input.Entity.Id > input.B;
    }

    internal static void __GateEntry()
    {
        // Invoked only on the NEXT call, after the install has returned: a slot-based
        // marshal would dispatch through a restored or null slot here.
        s_root = Triple;
        dn2cpptest_store_cb(s_root);
        Console.WriteLine(dn2cpptest_invoke_stored(14));

        // A DISTINCT instance of the same delegate type takes a second pool slot, with the
        // first still parked.
        int bias = 5;
        s_root = x => x + bias;
        dn2cpptest_store_cb(s_root);
        Console.WriteLine(dn2cpptest_invoke_stored(37));

        // A NULL delegate marshals as native NULL — the unregister idiom — and never an
        // ArgumentNullException; the -1 sentinel proves the native received NULL. The pool
        // helper's own throw is for the explicit GetFunctionPointerForDelegate path.
        s_root = null;
        dn2cpptest_store_cb(s_root);
        Console.WriteLine(dn2cpptest_invoke_stored(99));    // -1

        // The verdict and the byref write-backs both fold into the native's returned
        // number, so one diff pins the return and the two-way byref.
        s_filter = Filter;
        dn2cpptest_filter_install(s_filter);
        Console.WriteLine(dn2cpptest_filter_run(7, 3));  // 1011
        Console.WriteLine(dn2cpptest_filter_run(2, 9));  // 2011

        // The native creates a thread the managed runtime has never seen and dispatches
        // through the same stored pointer there. Allocating enough garbage to collect from
        // inside the callback makes registration observable: without the thunk prologue,
        // Boehm aborts instead of merely returning a wrong value.
        s_root = x =>
        {
            int sum = 0;
            for (int i = 0; i < x; i++)
            {
                int[] a = new int[16];
                a[3] = i;
                sum += a[3] % 7;
            }
            return sum;
        };
        dn2cpptest_store_cb(s_root);
        Console.WriteLine(dn2cpptest_invoke_stored_thread(400000));

        // The worker above is short-lived. A second collection after its TLS destructor
        // ran proves the collector no longer retains the dead native thread registration.
        GC.Collect();
        Console.WriteLine("foreign callback done");
    }
}
