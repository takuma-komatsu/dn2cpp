#nullable disable
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryManagerSubset
{
    // A custom MemoryManager<T>-backed Memory<T>: the subclass and the whole
    // MemoryManager/Memory plumbing transpile from the real CoreLib IL by normal
    // reachability once the subclass is instantiated. Pin hands out a raw pointer
    // with no GCHandle — the dn2cpp heap is non-moving, so pinning is identity —
    // and Unpin is a no-op.
    internal static class Program
    {
        private sealed unsafe class ArrayBackedManager : MemoryManager<byte>
        {
            private readonly byte[] _buffer;

            internal ArrayBackedManager(int length) => _buffer = new byte[length];

            internal byte[] Buffer => _buffer;

            internal bool Disposed { get; private set; }

            public override Span<byte> GetSpan() => _buffer;

            public override MemoryHandle Pin(int elementIndex = 0)
                => new MemoryHandle(Unsafe.AsPointer(ref _buffer[elementIndex]));

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing) => Disposed = true;
        }

        internal static unsafe void __GateEntry()
        {
            ArrayBackedManager manager = new ArrayBackedManager(8);

            // --- manager.Memory -> .Span: writes land in the backing array ---
            Memory<byte> mem = manager.Memory;
            Console.WriteLine(mem.Length);                       // 8
            Span<byte> span = mem.Span;
            span[0] = 11;
            span[7] = 77;
            Console.WriteLine($"{manager.Buffer[0]},{manager.Buffer[7]}"); // 11,77

            // --- Slice: still a view over the same manager's buffer ---
            Memory<byte> slice = mem.Slice(4, 3);
            Console.WriteLine(slice.Length);                     // 3
            slice.Span[0] = 44;
            Console.WriteLine(manager.Buffer[4]);                // 44

            // --- Memory<T>.Pin(): the manager branch (virtual Pin(elementIndex)) ---
            using (MemoryHandle handle = mem.Pin())
            {
                byte* p = (byte*)handle.Pointer;
                Console.WriteLine(p[0]);                         // 11
                p[1] = 22;
            }
            Console.WriteLine(manager.Buffer[1]);                // 22

            // a sliced memory pins at the slice offset
            using (MemoryHandle handle = slice.Pin())
            {
                Console.WriteLine(((byte*)handle.Pointer)[0]);   // 44
            }

            // --- TryGetMemoryManager: true + the same instance ---
            bool got = MemoryMarshal.TryGetMemoryManager(
                (ReadOnlyMemory<byte>)mem, out MemoryManager<byte> back);
            Console.WriteLine(got);                              // True
            Console.WriteLine(ReferenceEquals(back, manager));   // True

            // an array-backed memory has no manager
            Memory<byte> arrayMem = new byte[4];
            Console.WriteLine(MemoryMarshal.TryGetMemoryManager(
                (ReadOnlyMemory<byte>)arrayMem, out MemoryManager<byte> none)); // False
            Console.WriteLine(none is null);                     // True

            // --- Dispose through IDisposable (explicit interface impl) ---
            ((IDisposable)manager).Dispose();
            Console.WriteLine(manager.Disposed);                 // True
        }
    }
}
