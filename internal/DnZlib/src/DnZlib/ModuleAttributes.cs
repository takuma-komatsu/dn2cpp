using System.Runtime.CompilerServices;

// Skip zero-initialization of stackalloc/locals across the whole assembly. Hot paths explicitly
// initialize what they read; this removes redundant zeroing of large scratch buffers.
[module: SkipLocalsInit]
