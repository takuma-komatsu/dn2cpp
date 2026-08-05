using System;
using System.IO;

namespace DirectoryDeleteSubset
{
    // Directory.Delete — real BCL IL (never intercepted): the recursive form
    // walks the tree through the readdir PAL and removes entries bottom-up
    // (SystemNative_Unlink + SystemNative_RmDir); the non-recursive form is a
    // bare RmDir. Also asserts the error path for a non-empty non-recursive
    // delete.
    internal static class Program
    {
        internal static void __GateEntry(string root)
        {
            Console.WriteLine("-- Directory.Delete --");
            string prune = Path.Combine(root, "prune");
            Directory.CreateDirectory(Path.Combine(prune, "inner"));
            File.WriteAllText(Path.Combine(prune, "a.txt"), "a");
            File.WriteAllText(Path.Combine(prune, "inner", "b.txt"), "b");

            bool nonEmptyCaught;
            try
            {
                Directory.Delete(prune); // non-recursive on a non-empty dir
                nonEmptyCaught = false;
            }
            catch (IOException)
            {
                nonEmptyCaught = true;
            }
            Console.WriteLine("nonEmptyCaught=" + nonEmptyCaught);

            Directory.Delete(prune, true);
            Console.WriteLine("pruneGone=" + !Directory.Exists(prune));

            string empty = Path.Combine(root, "empty-delete-me");
            Directory.CreateDirectory(empty);
            Directory.Delete(empty);
            Console.WriteLine("emptyGone=" + !Directory.Exists(empty));
        }
    }
}
