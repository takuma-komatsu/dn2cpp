using System;
using System.Numerics;

namespace GenericMathClassOperators;

// Reference-type TSelf through static-abstract operator interfaces: a sealed
// class implementing IAdditionOperators / ISubtractionOperators /
// IEqualityOperators / IAdditiveIdentity, driven through constrained generic
// helpers. The transpiler resolves each constrained static-virtual call to the
// class's real static operator body and calls it directly — the same
// devirtualization as the struct path, but the impl lives on a heap class.
// A property-shaped static abstract (get_AdditiveIdentity) rides along.
// BankAccount pins the resolver's base-class walk: its impls (one implicit
// static property, one explicit operator implementation) live on AccountBase,
// while the constrained TSelf is the derived class. Money and Meters both flow
// through the same generic helpers and the generic Accumulator<T> class, so the
// canonical-shared machinery groups the reference instantiations under one
// canonical body — whose trial cannot resolve a single static impl (it differs
// per real class), so it must fall back to per-instantiation expansion and
// still produce exact output.
internal sealed class Money :
    IAdditionOperators<Money, Money, Money>,
    ISubtractionOperators<Money, Money, Money>,
    IEqualityOperators<Money, Money, bool>,
    IAdditiveIdentity<Money, Money>
{
    private readonly long _cents;

    public Money(long cents)
    {
        _cents = cents;
    }

    public long Cents => _cents;

    public static Money AdditiveIdentity => new Money(0);

    public static Money operator +(Money a, Money b) => new Money(a._cents + b._cents);

    public static Money operator -(Money a, Money b) => new Money(a._cents - b._cents);

    public static bool operator ==(Money a, Money b) => a._cents == b._cents;

    public static bool operator !=(Money a, Money b) => a._cents != b._cents;

    public override bool Equals(object obj) => obj is Money m && m._cents == _cents;

    public override int GetHashCode() => _cents.GetHashCode();
}

internal sealed class Meters :
    IAdditionOperators<Meters, Meters, Meters>,
    IAdditiveIdentity<Meters, Meters>
{
    private readonly double _length;

    public Meters(double length)
    {
        _length = length;
    }

    public double Length => _length;

    public static Meters AdditiveIdentity => new Meters(0.0);

    public static Meters operator +(Meters a, Meters b) => new Meters(a._length + b._length);
}

// The static impls live on the abstract base; the constrained TSelf is the
// derived class. AdditiveIdentity is an implicit static property impl; the
// operator is an explicit interface implementation (a class can't declare
// `operator +(BankAccount, BankAccount)` implicitly — neither parameter is the
// containing type), so the resolver's ExplicitInterfaceImpls probe and its
// SigKey scan both walk the base chain.
internal abstract class AccountBase :
    IAdditionOperators<BankAccount, BankAccount, BankAccount>,
    IAdditiveIdentity<BankAccount, BankAccount>
{
    public static BankAccount AdditiveIdentity => new BankAccount(0);

    static BankAccount IAdditionOperators<BankAccount, BankAccount, BankAccount>.operator +(BankAccount a, BankAccount b) =>
        new BankAccount(a.Balance + b.Balance);
}

internal sealed class BankAccount : AccountBase
{
    public long Balance { get; }

    public BankAccount(long balance)
    {
        Balance = balance;
    }
}

internal static class ClassOperators
{
    // T.AdditiveIdentity seed + op_Addition accumulate — the Enumerable.Sum
    // shape with a reference-type element.
    static T AddAll<T>(T[] xs) where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
    {
        T sum = T.AdditiveIdentity;
        foreach (var x in xs)
            sum += x;
        return sum;
    }

    static T Sub<T>(T a, T b) where T : ISubtractionOperators<T, T, T> => a - b;

    static bool Eq<T>(T a, T b) where T : IEqualityOperators<T, T, bool> => a == b;

    static T IdentityOf<T>() where T : IAdditiveIdentity<T, T> => T.AdditiveIdentity;

    // A generic class over the reference implementors: its instantiations
    // group under one canonical body, exercising the class-dimension shared
    // trial (the method-dimension twin is AddAll above).
    internal sealed class Accumulator<T> where T : IAdditionOperators<T, T, T>, IAdditiveIdentity<T, T>
    {
        private T _total;

        public Accumulator()
        {
            _total = T.AdditiveIdentity;
        }

        public void Add(T item)
        {
            _total += item;
        }

        public T Total => _total;
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== Class operators (reference-type TSelf) ==");
        var monies = new[] { new Money(150), new Money(275), new Money(-25) };
        Console.WriteLine($"Money sum      {AddAll(monies).Cents}");
        Console.WriteLine($"Money sub      {Sub(new Money(1000), new Money(333)).Cents}");
        Console.WriteLine($"Money eq       {Eq(new Money(42), new Money(42))} {Eq(new Money(42), new Money(43))}");
        Console.WriteLine($"Money identity {IdentityOf<Money>().Cents}");

        Console.WriteLine("== Base-class-provided impls (resolver base walk) ==");
        var accounts = new[] { new BankAccount(500), new BankAccount(-125), new BankAccount(225) };
        Console.WriteLine($"Account sum      {AddAll(accounts).Balance}");
        Console.WriteLine($"Account identity {IdentityOf<BankAccount>().Balance}");

        Console.WriteLine("== Canonical-shared implementors (per-instantiation expansion) ==");
        var macc = new Accumulator<Money>();
        macc.Add(new Money(1200));
        macc.Add(new Money(34));
        var lacc = new Accumulator<Meters>();
        lacc.Add(new Meters(1.5));
        lacc.Add(new Meters(2.25));
        lacc.Add(new Meters(4.0));
        Console.WriteLine($"Money acc  {macc.Total.Cents}");
        Console.WriteLine($"Meters acc {lacc.Total.Length}");
        Console.WriteLine($"Meters sum {AddAll(new[] { new Meters(10.0), new Meters(2.5) }).Length}");
    }
}
