#nullable disable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

// Private ResourceManager/ResourceReader entry points reached by name: the
// default reader type-name match, reader type validation, and the
// deserialization permit check, which refuses before reading the type table or
// stream. Each GetMethod sits beside its Invoke so the named body is retained.
namespace ResourceReaderGuardSubset
{
    internal static class Program
    {
        private const string Reader = "System.Resources.ResourceReader";

        private static string Outcome(Func<object> call)
        {
            try { return Convert.ToString(call(), CultureInfo.InvariantCulture); }
            catch (TargetInvocationException error) { return error.InnerException.GetType().FullName; }
        }

        private static object IsDefaultType(string typeName, string defaultName)
        {
            MethodInfo method = typeof(ResourceManager).GetMethod("IsDefaultType", BindingFlags.Static | BindingFlags.NonPublic);
            return method.Invoke(null, new object[] { typeName, defaultName });
        }

        private static object ValidateReaderType(string typeName)
        {
            MethodInfo method = typeof(ResourceReader).GetMethod("ValidateReaderType", BindingFlags.Static | BindingFlags.NonPublic);
            return method.Invoke(null, new object[] { typeName });
        }

        private static string DeserializeWithoutPermit(int index)
        {
            var reader = (ResourceReader)RuntimeHelpers.GetUninitializedObject(typeof(ResourceReader));
            MethodInfo method = typeof(ResourceReader).GetMethod("DeserializeObject", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(int) }, null);
            try
            {
                method.Invoke(reader, new object[] { index });
                return "none";
            }
            catch (TargetInvocationException error)
            {
                return error.InnerException.GetType().FullName + ": " + error.InnerException.Message;
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== resource reader type guards ==");
            string[] names = { null, "", Reader, "system.resources.ResourceReader", Reader + "Extra", " " + Reader,
                Reader + " ", Reader + ",mscorlib", Reader + ", MSCORLIB ", Reader + ",\tmscorlib\r\n",
                Reader + ", mscorlib ", Reader + ", mscorlib ,ignored",
                Reader + ",mscorlib, Version=0.0.0.0, Culture=bad, PublicKeyToken=bad",
                Reader + ",mscorlib,", Reader + ",mscorlib,,garbage", Reader + ",System.Private.CoreLib",
                Reader + ", mscorlibExtra", Reader + ",", Reader + ", ", Reader + ",,mscorlib",
                Reader + "\0,mscorlib", Reader + ",mscorlib\0", ",mscorlib", "X,Y,Z", "Å,mscorlib" };
            string[] defaults = { Reader, "", null, "X", "Å", Reader + ",mscorlib" };
            for (int i = 0; i < names.Length; i++)
            {
                string[] row = new string[defaults.Length];
                for (int j = 0; j < defaults.Length; j++)
                    row[j] = Outcome(() => IsDefaultType(names[i], defaults[j]));
                Console.WriteLine("default-type:" + i + ":" + string.Join(",", row));
            }
            string[] readers = { null, "", Reader, Reader + ", mscorlib", Reader + ", MSCORLIB,ignored", Reader + ", Wrong",
                Reader + "Extra", Reader + ",", Reader + " ,mscorlib" };
            for (int i = 0; i < readers.Length; i++)
                Console.WriteLine("validate-reader:" + i + ":" + Outcome(() => ValidateReaderType(readers[i])));
            foreach (int index in new[] { -1, 0, int.MaxValue })
                Console.WriteLine("deserialize-permit:" + index + ":" + DeserializeWithoutPermit(index));
            Console.WriteLine("resource reader type guards end");
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_READER_HEADERS") != "1") RunHeaders();
        }

        private static byte[] Header(string readerName, int magic)
        {
            using var names = new MemoryStream();
            using (var writer = new BinaryWriter(names, Encoding.UTF8, true))
            {
                writer.Write(readerName);
                writer.Write("System.Resources.RuntimeResourceSet");
            }
            byte[] nameBytes = names.ToArray();
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(magic);
                writer.Write(ResourceManager.HeaderVersionNumber);
                writer.Write(nameBytes.Length);
                writer.Write(nameBytes);
                writer.Write(2);
                writer.Write(0);
                writer.Write(0);
                while (stream.Position % 8 != 0) writer.Write((byte)0);
                writer.Write((int)stream.Position + 4);
            }
            return stream.ToArray();
        }

        private static void ReadHeader(string name, byte[] bytes)
        {
            try
            {
                using var reader = new ResourceReader(new MemoryStream(bytes));
                Console.WriteLine("reader-header:" + name + ":accepted");
            }
            catch (Exception exception)
            {
                Console.WriteLine("reader-header:" + name + ":" + exception.GetType().FullName);
            }
        }

        private static void RunHeaders()
        {
            // Formatting a readonly field directly takes its address. This section
            // exercises value loads; address refusal has its own metadata fixture.
            int magic = ResourceManager.MagicNumber;
            int version = ResourceManager.HeaderVersionNumber;
            Console.WriteLine("resource-header-fields:" + magic + ":" + version);
            Console.WriteLine("resource-header-reflection:"
                + (magic == (int)typeof(ResourceManager).GetField("MagicNumber").GetValue(null)) + ":"
                + (version == (int)typeof(ResourceManager).GetField("HeaderVersionNumber").GetValue(null)));
            ReadHeader("valid", Header(Reader + ", mscorlib", magic));
            ReadHeader("wrong-reader", Header("Wrong.Reader, mscorlib", magic));
            ReadHeader("bad-magic", Header(Reader + ", mscorlib", 0));
            ReadHeader("truncated", new byte[] { 0xce, 0xca, 0xef, 0xbe });
            Console.WriteLine("resource reader headers end");
        }

        // dn2cpp refuses BinaryFormatter construction, where .NET builds one, so the
        // gate pins the refusal text instead of diffing it.
        internal static void RunFormatterLimit()
        {
            MethodInfo method = typeof(ResourceReader).GetMethod("CreateBinaryFormatter", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                method.Invoke(null, null);
                Console.WriteLine("create-formatter -> none");
            }
            catch (TargetInvocationException error)
            {
                Console.WriteLine("create-formatter -> " + error.InnerException.GetType().Name + ": " + error.InnerException.Message);
            }
        }
    }
}
