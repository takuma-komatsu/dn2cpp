// Fixture for SrmReadCore: nothing here is executed, only its compiled metadata
// is read. The varied shapes give the type/method/string/blob heaps stable content.
using System;

namespace SrmSample.Core
{
    public class Alpha
    {
        public const string Tag = "alpha-tag";

        public int Add(int a, int b)
        {
            return a + b;
        }

        public string Greet(string who)
        {
            return string.Concat("hi ", who);
        }
    }

    public struct Vector2
    {
        public int X;
        public int Y;

        public int Sum()
        {
            return X + Y;
        }
    }
}

namespace SrmSample.Util
{
    public enum Shade
    {
        Dark,
        Mid,
        Light,
    }

    public sealed class Beta
    {
        public static int Counter;

        public void Reset()
        {
            Counter = 0;
        }

        public double Scale(double v)
        {
            return v * 2.0;
        }
    }

    internal class Hidden
    {
        public void Secret()
        {
            Console.WriteLine("secret");
        }
    }
}
