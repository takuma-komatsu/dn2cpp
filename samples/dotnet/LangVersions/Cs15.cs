// C# 15: collection expression arguments (`with(...)`), union types, `closed`
// classes and records, extension indexers, labeled `break` / `continue`, and
// non-virtual static interface members.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cs15;

internal sealed record class Cat(string Name);

internal sealed record class Dog(string Name, int Tricks);

// C# 15 union: a value of one of the case types; a default Pet holds none.
internal union Pet(Cat, Dog);

// A union with a value-type case.
internal union Score(int, string);

// C# 15 closed hierarchy: only this assembly may derive, so a switch over the
// direct descendants is exhaustive without a default arm.
internal closed record class GateState;

internal sealed record class Shut : GateState;

internal sealed record class Open(int Percent) : GateState;

internal closed class Shape
{
}

internal sealed class Circle : Shape
{
    public int R;
}

internal sealed class Square : Shape
{
    public int Side;
}

internal sealed class Grid
{
    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new int[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public int[] Cells { get; }
}

// C# 15 extension indexers.
internal static class GridExtensions
{
    extension(Grid grid)
    {
        public int this[int x, int y]
        {
            get => grid.Cells[y * grid.Width + x];
            set => grid.Cells[y * grid.Width + x] = value;
        }
    }

    extension(IEnumerable<int> sequence)
    {
        public int this[int index] => sequence.ElementAt(index);
    }
}

// C# 15 non-virtual static interface members (no runtime DIM support needed).
internal interface IMeasurement
{
    static readonly double CentimetersPerMeter = 100;

    static double ToMeters(double centimeters) => centimeters / CentimetersPerMeter;
}

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 15.0 ==");

        CollectionExpressionArguments();
        Unions();
        ClosedHierarchies();
        ExtensionIndexers();
        LabeledBreakContinue();
        StaticInterfaceMembers();
    }

    private static void CollectionExpressionArguments()
    {
        string[] values = ["one", "two", "three"];
        List<string> names = [with(capacity: values.Length * 2), .. values];
        Console.WriteLine($"with(capacity): Count={names.Count} Capacity={names.Capacity} [{string.Join(",", names)}]");

        HashSet<string> set = [with(StringComparer.OrdinalIgnoreCase), "Hello", "HELLO", "hello", "World"];
        Console.WriteLine($"with(comparer) HashSet: Count={set.Count} Contains(WORLD)={set.Contains("WORLD")}");

        List<int> reserved = [with(capacity: 16)];
        Console.WriteLine($"with(capacity) only: Count={reserved.Count} Capacity={reserved.Capacity}");

        IList<int> list = [with(capacity: 4), 1, 2];
        Console.WriteLine($"with(capacity) IList<int>: Count={list.Count} [{string.Join(",", list)}]");
    }

    private static string Describe(Pet pet) => pet switch
    {
        Dog d => $"dog {d.Name} ({d.Tricks} tricks)",
        Cat c => "cat " + c.Name,
    };

    private static Score Grade(int points)
    {
        if (points >= 50)
        {
            return points;
        }

        return "fail";
    }

    private static string Show(Score score) => score switch
    {
        int n => "points " + n,
        string message => "message " + message,
    };

    private static void Unions()
    {
        Pet[] pets = [new Cat("Tom"), new Dog("Rex", 3)];
        foreach (Pet pet in pets)
        {
            Console.WriteLine("union switch: " + Describe(pet));
        }

        Pet clever = new Dog("Fido", 5);
        if (clever is Dog { Tricks: > 2 } d)
        {
            Console.WriteLine("union property pattern: " + d.Name);
        }

        Console.WriteLine("union value-type case: " + Show(Grade(70)));
        Console.WriteLine("union reference case: " + Show(Grade(10)));
    }

    private static string Describe(GateState state) => state switch
    {
        Shut => "shut",
        Open(var percent) => percent + "% open",
    };

    private static int Area(Shape shape) => shape switch
    {
        Circle c => 3 * c.R * c.R,
        Square s => s.Side * s.Side,
    };

    private static void ClosedHierarchies()
    {
        GateState[] states = [new Shut(), new Open(40)];
        foreach (GateState state in states)
        {
            Console.WriteLine("closed record switch: " + Describe(state));
        }

        Console.WriteLine("closed class switch: " + Area(new Circle { R = 2 }) + " " + Area(new Square { Side = 5 }));
    }

    private static void ExtensionIndexers()
    {
        Grid grid = new Grid(3, 2);
        grid[1, 0] = 4;
        grid[2, 1] = 9;
        grid[2, 1] += 1;
        Console.WriteLine($"extension indexer (get/set): {grid[1, 0]} {grid[2, 1]} cells=[{string.Join(",", grid.Cells)}]");

        IEnumerable<int> numbers = Enumerable.Range(1, 10);
        Console.WriteLine("extension indexer (IEnumerable<int>): " + numbers[2]);
    }

    private static void LabeledBreakContinue()
    {
        // 1 = blocked row (skip the rest of it), 3 = goal (stop searching).
        int[,] map =
        {
            { 0, 0, 1, 0 },
            { 0, 2, 0, 0 },
            { 0, 0, 0, 3 },
            { 0, 0, 0, 0 },
        };
        int visited = 0;
        string goal = "none";
        outer: for (int row = 0; row < map.GetLength(0); row++)
        {
            for (int col = 0; col < map.GetLength(1); col++)
            {
                if (map[row, col] == 1)
                {
                    continue outer;
                }

                visited++;
                if (map[row, col] == 3)
                {
                    goal = $"({row},{col})";
                    break outer;
                }
            }
        }

        Console.WriteLine($"labeled continue/break (for): visited={visited} goal={goal}");

        // The List<T> enumerators are disposed in finally blocks the jumps leave.
        List<List<int>> waves = [[1, 2, -1, 4], [5, 6], [7, 99, 8], [1000]];
        int sum = 0;
        waveLoop: foreach (List<int> wave in waves)
        {
            foreach (int enemy in wave)
            {
                if (enemy < 0)
                {
                    continue waveLoop;
                }

                if (enemy == 99)
                {
                    break waveLoop;
                }

                sum += enemy;
            }
        }

        Console.WriteLine("labeled continue/break (foreach): sum=" + sum);

        int steps = 0;
        loop: while (true)
        {
            steps++;
            switch (steps)
            {
                case 1:
                case 2:
                    continue loop;
                case 3:
                    break loop;
            }
        }

        Console.WriteLine("labeled break out of switch in loop: steps=" + steps);
    }

    private static void StaticInterfaceMembers()
    {
        Console.WriteLine("static interface method: " + IMeasurement.ToMeters(250));
        Console.WriteLine("static interface field: " + IMeasurement.CentimetersPerMeter);
    }
}
