using System;

namespace Sample
{
    internal abstract class Animal
    {
        internal static int Created;

        protected Animal()
        {
            Created = Created + 1;
        }

        public abstract string Speak();

        public virtual string Describe()
        {
            return string.Concat("An animal says: ", Speak());
        }
    }

    internal sealed class Dog : Animal
    {
        public override string Speak()
        {
            return "Woof!";
        }
    }

    internal sealed class Cat : Animal
    {
        public override string Speak()
        {
            return "Meow!";
        }

        public override string Describe()
        {
            return string.Concat("A cat says: ", Speak(), " (haughtily)");
        }
    }

    internal enum Color
    {
        Red,
        Green,
        Blue,
    }

    internal struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vec2 Add(Vec2 other)
        {
            return new Vec2(X + other.X, Y + other.Y);
        }

        public float LengthSquared()
        {
            return X * X + Y * Y;
        }
    }

    internal interface IShape
    {
        float Area();
        string Label();
    }

    internal interface IExplicitTest
    {
        string GetMessage();
    }

    internal sealed class ExplicitTestImpl : IExplicitTest
    {
        string IExplicitTest.GetMessage()
        {
            return "explicit interface call worked";
        }
    }

    internal sealed class Circle : IShape
    {
        private readonly float _radius;

        public Circle(float radius)
        {
            _radius = radius;
        }

        public float Area()
        {
            return 3.14159f * _radius * _radius;
        }

        public string Label()
        {
            return "circle";
        }
    }

    internal sealed class Square : IShape
    {
        private readonly float _side;

        public Square(float side)
        {
            _side = side;
        }

        public float Area()
        {
            return _side * _side;
        }

        public string Label()
        {
            return "square";
        }
    }

    internal delegate int IntOp(int x);

    internal delegate void Notify(int x);

    internal delegate string Describer();

    internal sealed class GameError : Exception
    {
        public readonly string Reason;

        public GameError(string reason)
        {
            Reason = reason;
        }
    }

    internal sealed class OtherError : Exception
    {
    }

    internal static class Config
    {
        public static int Value;

        static Config()
        {
            Value = 123;
        }
    }

    internal interface IValued
    {
        int Value();
    }

    internal struct Coin : IValued
    {
        private readonly int _cents;

        public Coin(int cents)
        {
            _cents = cents;
        }

        public int Value()
        {
            return _cents;
        }
    }

    internal sealed class Gem : IValued
    {
        public int Value()
        {
            return 1000;
        }
    }

    /// <summary>
    /// A growable generic list (List&lt;T&gt;-style), implemented purely in C#
    /// over a backing array. Proves the transpiler can compile real
    /// collection code: generic class + generic arrays + resize + exceptions.
    /// </summary>
    internal sealed class MyList<T>
    {
        private T[] _items;
        private int _count;

        public MyList()
        {
            _items = new T[4];
            _count = 0;
        }

        public int Count
        {
            get { return _count; }
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                T[] bigger = new T[_items.Length * 2];
                int i = 0;
                while (i < _count)
                {
                    bigger[i] = _items[i];
                    i = i + 1;
                }
                _items = bigger;
            }
            _items[_count] = item;
            _count = _count + 1;
        }

        public T Get(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new GameError("index out of range");
            }
            return _items[index];
        }
    }

    // Each closed generic type gets its own static storage: Counter<int> and
    // Counter<string> must hold independent counts.
    internal static class Counter<T>
    {
        public static int Count;

        public static void Inc()
        {
            Count = Count + 1;
        }
    }

    internal sealed class Box<T>
    {
        private readonly T _value;

        public Box(T value)
        {
            _value = value;
        }

        public T Get()
        {
            return _value;
        }
    }

    internal struct Pair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    internal static class Program
    {
        private static int Fib(int n)
        {
            if (n < 2)
            {
                return n;
            }
            return Fib(n - 1) + Fib(n - 2);
        }

        private static string SwitchName(int n)
        {
            switch (n)
            {
                case 0:
                    return "zero";
                case 1:
                    return "one";
                case 2:
                    return "two";
                case 3:
                    return "three";
                default:
                    return "many";
            }
        }

        private static int Triple(int x)
        {
            return x * 3;
        }

        // Never called. Uses a BCL API the transpiler does not map; tree-shaking
        // must drop it so the build still succeeds (the property that lets us
        // pull in large library assemblies without compiling unused code).
        private static string NeverCalled()
        {
            return System.IO.Path.GetExtension("unused.txt").ToUpperInvariant();
        }

        private static string CheckedAdd(int a, int b)
        {
            try
            {
                int sum = checked(a + b);
                return string.Concat("sum=", Identity<string>(IntToStr(sum)));
            }
            catch (OverflowException)
            {
                return "overflow!";
            }
        }

        private static string IntToStr(int n)
        {
            if (n == 2147483646)
            {
                return "2147483646";
            }
            return "big";
        }

        private static T Identity<T>(T value)
        {
            return value;
        }

        private static T[] MakePair<T>(T a, T b)
        {
            T[] arr = new T[2];
            arr[0] = a;
            arr[1] = b;
            return arr;
        }

        private static void Swap(ref int a, ref int b)
        {
            int t = a;
            a = b;
            b = t;
        }

        private static bool TryHalf(int n, out int half)
        {
            if (n % 2 == 0)
            {
                half = n / 2;
                return true;
            }
            half = 0;
            return false;
        }

        private static void Bump(ref Vec2 v)
        {
            v.X = v.X + 1.0f;
        }

        private static int SumValues<T>(T a, T b) where T : IValued
        {
            return a.Value() + b.Value();
        }

        private static int _acc;

        private static void AddSmall(int x)
        {
            _acc = _acc + x;
        }

        private static void AddBig(int x)
        {
            _acc = _acc + x * 100;
        }

        private static int Risky(int n)
        {
            if (n < 0)
            {
                throw new GameError("negative input");
            }
            return n * 2;
        }

        private static int WithCleanup(int n)
        {
            try
            {
                if (n < 0)
                {
                    throw new GameError("bad input");
                }
                Console.WriteLine("in try");
                return n + 1;
            }
            finally
            {
                Console.WriteLine("cleanup ran");
            }
        }

        private static string Classify(int n)
        {
            try
            {
                if (n == 0)
                {
                    throw new GameError("zero");
                }
                if (n < 0)
                {
                    throw new OtherError();
                }
                return "positive";
            }
            catch (GameError e)
            {
                return string.Concat("game error: ", e.Reason);
            }
            catch (OtherError)
            {
                return "other error";
            }
            finally
            {
                Console.WriteLine("classify finally");
            }
        }

        private static int Main()
        {
            Console.WriteLine("Hello from godot-il2cpp!");

            Animal a = new Dog();
            Animal b = new Cat();
            Console.WriteLine(a.Describe());
            Console.WriteLine(b.Describe());
            Console.WriteLine(Animal.Created);

            int[] fib = new int[10];
            int i = 0;
            while (i < fib.Length)
            {
                fib[i] = Fib(i);
                i = i + 1;
            }

            int sum = 0;
            i = 0;
            while (i < fib.Length)
            {
                sum = sum + fib[i];
                i = i + 1;
            }
            Console.WriteLine(fib[9]);
            Console.WriteLine(sum);

            bool ok = sum == 88;
            Console.WriteLine(ok);

            Vec2 v = new Vec2(3.0f, 4.0f);
            Vec2 w = v.Add(new Vec2(1.0f, 2.0f));
            Console.WriteLine((int)w.X);
            Console.WriteLine((int)w.Y);
            Console.WriteLine((int)v.LengthSquared());

            Color color = Color.Blue;
            Console.WriteLine((int)color);

            Console.WriteLine(SwitchName(2));
            Console.WriteLine(SwitchName(7));

            object boxed = 42;
            int unboxed = (int)boxed;
            Console.WriteLine(unboxed);

            object boxedVec = new Vec2(5.0f, 6.0f);
            Vec2 unboxedVec = (Vec2)boxedVec;
            Console.WriteLine((int)unboxedVec.Y);

            Animal mystery = new Dog();
            Console.WriteLine(mystery is Dog);
            Console.WriteLine(mystery is Cat);
            Animal? maybe = mystery as Cat;
            Console.WriteLine(maybe == null);
            Dog definitely = (Dog)mystery;
            Console.WriteLine(definitely.Speak());

            Animal[] zoo = new Animal[2];
            zoo[0] = new Dog();
            zoo[1] = new Cat();
            int z = 0;
            while (z < zoo.Length)
            {
                Console.WriteLine(zoo[z].Speak());
                z = z + 1;
            }

            Console.WriteLine(Config.Value);

            IShape[] shapes = new IShape[2];
            shapes[0] = new Circle(1.0f);
            shapes[1] = new Square(3.0f);
            int si = 0;
            while (si < shapes.Length)
            {
                Console.WriteLine(shapes[si].Label());
                Console.WriteLine((int)shapes[si].Area());
                si = si + 1;
            }
            object first = shapes[0];
            Console.WriteLine(first is IShape);

            try
            {
                Console.WriteLine(Risky(21));
                Console.WriteLine(Risky(-1));
                Console.WriteLine("unreachable");
            }
            catch (GameError error)
            {
                Console.WriteLine(string.Concat("caught: ", error.Reason));
            }
            Console.WriteLine("after catch");

            IntOp triple = Triple;
            Console.WriteLine(triple(14));

            int factor = 5;
            IntOp times = x => x * factor;
            Console.WriteLine(times(8));

            IntOp neg = x => -x;
            Console.WriteLine(neg(7));

            Animal pet = new Cat();
            Describer speak = pet.Speak;
            Console.WriteLine(speak());

            Console.WriteLine(WithCleanup(9));
            try
            {
                WithCleanup(-1);
            }
            catch (GameError)
            {
                Console.WriteLine("outer caught");
            }

            Notify notify = AddSmall;
            notify += AddBig;
            notify(10);
            Console.WriteLine(_acc);
            notify -= AddSmall;
            notify(1);
            Console.WriteLine(_acc);

            Box<int> intBox = new Box<int>(99);
            Console.WriteLine(intBox.Get());
            Box<string> strBox = new Box<string>("boxed string");
            Console.WriteLine(strBox.Get());

            Pair<int, string> entry = new Pair<int, string>(7, "seven");
            Console.WriteLine(entry.Key);
            Console.WriteLine(entry.Value);

            Console.WriteLine(Identity<int>(314));
            Console.WriteLine(Identity<string>("generic method"));

            Counter<int>.Inc();
            Counter<int>.Inc();
            Counter<string>.Inc();
            Console.WriteLine(Counter<int>.Count);
            Console.WriteLine(Counter<string>.Count);

            int p = 3;
            int q = 8;
            Swap(ref p, ref q);
            Console.WriteLine(p);
            Console.WriteLine(q);

            int h;
            bool even = TryHalf(20, out h);
            Console.WriteLine(even);
            Console.WriteLine(h);

            Vec2 bumped = new Vec2(10.0f, 0.0f);
            Bump(ref bumped);
            Console.WriteLine((int)bumped.X);

            Console.WriteLine(SumValues<Coin>(new Coin(25), new Coin(10)));
            Console.WriteLine(SumValues<Gem>(new Gem(), new Gem()));

            Console.WriteLine(Classify(5));
            Console.WriteLine(Classify(0));
            Console.WriteLine(Classify(-3));

            Console.WriteLine(CheckedAdd(2147483645, 1));
            Console.WriteLine(CheckedAdd(2147483647, 1));

            MyList<int> numbers = new MyList<int>();
            int n2 = 0;
            while (n2 < 10)
            {
                numbers.Add(n2 * n2);
                n2 = n2 + 1;
            }
            Console.WriteLine(numbers.Count);
            Console.WriteLine(numbers.Get(9));

            MyList<string> words = new MyList<string>();
            words.Add("hello");
            words.Add("collection");
            Console.WriteLine(string.Concat(words.Get(0), " ", words.Get(1)));

            Console.WriteLine((int)Math.Sqrt(144.0));
            Console.WriteLine(Math.Abs(-5));
            Console.WriteLine((int)Math.Max(3.5, 7.5));
            Console.WriteLine(Math.Min(10, 4));
            Console.WriteLine((int)Math.Floor(3.9));

            int score = 42;
            Console.WriteLine(string.Concat("score: ", score.ToString()));
            bool flag = true;
            Console.WriteLine(string.Concat("flag=", flag.ToString()));

            // Test typeof / ldtoken / System.Type
            Type programType = typeof(Program);
            Console.WriteLine(programType != null);
            Console.WriteLine(programType.Name);
            Console.WriteLine(typeof(int).Name);
            Console.WriteLine(typeof(string).Name);

            int[] ints = MakePair<int>(11, 22);
            Console.WriteLine(ints[0] + ints[1]);
            string[] strs = MakePair<string>("a", "b");
            Console.WriteLine(string.Concat(strs[0], strs[1]));
            double[] ds = new double[3];
            ds[0] = 1.5;
            ds[2] = 2.5;
            Console.WriteLine((int)(ds[0] + ds[2]));

            IExplicitTest explicitObj = new ExplicitTestImpl();
            Console.WriteLine(explicitObj.GetMessage());

            int[,] grid = new int[2, 3];
            grid[0, 0] = 10;
            grid[0, 1] = 20;
            grid[1, 2] = 30;
            Console.WriteLine(grid[0, 0] + grid[0, 1] + grid[1, 2]);

            string[,] strGrid = new string[2, 2];
            strGrid[0, 0] = "hello";
            strGrid[1, 1] = "world";
            Console.WriteLine(string.Concat(strGrid[0, 0], " ", strGrid[1, 1]));

            double[,] dblGrid = new double[2, 2];
            dblGrid[0, 0] = 1.5;
            dblGrid[1, 1] = 2.5;
            Console.WriteLine((int)(dblGrid[0, 0] + dblGrid[1, 1]));

            // UTF-16 string semantics: Length counts code units (not UTF-8
            // bytes) and the indexer returns full .NET chars. 'é' = U+00E9 (233),
            // '✓' = U+2713 (10003).
            string uni = "Aé✓";
            Console.WriteLine(uni);
            Console.WriteLine(uni.Length);
            Console.WriteLine((int)uni[1]);
            Console.WriteLine((int)uni[2]);

            // A supplementary-plane char (U+1F600 😀) is two UTF-16 code units:
            // the high/low surrogates 0xD83D (55357) / 0xDE00 (56832). Printing
            // it round-trips the surrogate pair back through UTF-8 intact.
            string emoji = "😀";
            Console.WriteLine(emoji);
            Console.WriteLine(emoji.Length);
            Console.WriteLine((int)emoji[0]);
            Console.WriteLine((int)emoji[1]);

            return 0;
        }
    }
}
