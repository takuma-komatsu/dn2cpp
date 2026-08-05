using System.Threading;

namespace CustomAsyncTaskLib;

/// <summary>The F-bounded (CRTP) pool node: <typeparamref name="T"/> is a node OF ITSELF.
/// GDTask's <c>ITaskPoolNode&lt;T&gt;</c> to the letter — <c>Runner&lt;TStateMachine&gt; :
/// ITaskPoolNode&lt;Runner&lt;TStateMachine&gt;&gt;</c> ties the knot, and closing
/// <c>TaskPool&lt;Runner&lt;TSM&gt;&gt;</c> over it is what an unshielded transpile would
/// have to do once per compiler-generated state machine in the whole program.</summary>
public interface ITaskPoolNode<T> where T : class
{
    ref T NextNode { get; }
}

/// <summary>The lock-free free list every runner is rented from and returned to. Pooling is
/// the whole reason a library like this exists (an allocation-free await), and the reason its
/// promise type has to be generic over the state machine.</summary>
public static class TaskPool<T> where T : class, ITaskPoolNode<T>
{
    private const int MaxPoolSize = 64;

    private static int s_gate;
    private static int s_size;
    private static T s_root;

    public static int Size => s_size;

    public static bool TryPop(out T result)
    {
        if (Interlocked.CompareExchange(ref s_gate, 1, 0) == 0)
        {
            var v = s_root;
            if (v is not null)
            {
                ref var next = ref v.NextNode;
                s_root = next;
                next = null;
                s_size--;
                result = v;
                Volatile.Write(ref s_gate, 0);
                return true;
            }
            Volatile.Write(ref s_gate, 0);
        }
        result = null;
        return false;
    }

    public static bool TryPush(T item)
    {
        if (Interlocked.CompareExchange(ref s_gate, 1, 0) == 0)
        {
            if (s_size < MaxPoolSize)
            {
                item.NextNode = s_root;
                s_root = item;
                s_size++;
                Volatile.Write(ref s_gate, 0);
                return true;
            }
            Volatile.Write(ref s_gate, 0);
        }
        return false;
    }
}
