#nullable enable
// A self-referential generic reference wrapper whose every member names the OPEN
// self-type (op_Implicit, a castclass, an isinst test), instantiated at several T
// from `static readonly` field initializers. A scan-side signature decode over
// these bodies must not mint the OPEN definition SettingValue<!0>: that reaches
// the struct emitter as a `!0`-typed field and aborts the transpile with no reach
// chain. Real System.Private.CoreLib (-r), diffed against .NET.
using System;
using System.Collections;
using System.Collections.Generic;
namespace SettingWrapperSubset;

interface IAssignableSetting
{
    void AssignFrom(object obj);
}

class SettingValue<TValueType> : IAssignableSetting
{
    private TValueType value;

    public SettingValue(TValueType value)
    {
        this.value = value;
    }

    public TValueType Value
    {
        get => value;
        set
        {
            if (EqualityComparer<TValueType>.Default.Equals(this.value, value))
                return;
            this.value = value;
        }
    }

    public static implicit operator TValueType(SettingValue<TValueType> v)
    {
        return v.value;
    }

    public void AssignFrom(object obj)
    {
        var other = (SettingValue<TValueType>)obj;
        Value = other.value;
    }

    public bool Equals(SettingValue<TValueType>? other)
    {
        if (other is null)
            return false;

        if (value is string)
            return EqualityComparer<TValueType>.Default.Equals(value, other.value);

        // A non-generic enumerator walk, not LINQ: this bucket references only
        // CoreLib, and the point is the `value is IEnumerable` isinst over the
        // open self-type.
        if (value is IEnumerable seq && other.value is IEnumerable otherSeq)
        {
            var e1 = seq.GetEnumerator();
            var e2 = otherSeq.GetEnumerator();
            while (true)
            {
                bool m1 = e1.MoveNext();
                bool m2 = e2.MoveNext();
                if (m1 != m2)
                    return false;
                if (!m1)
                    break;
                if (!Equals(e1.Current, e2.Current))
                    return false;
            }
            return true;
        }

        return EqualityComparer<TValueType>.Default.Equals(value, other.value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (obj is SettingValue<TValueType> other)
            return Equals(other);
        return false;
    }

    public override int GetHashCode()
    {
        return 17 ^ (value?.GetHashCode() ?? 0);
    }

    public override string ToString()
    {
        return value?.ToString() ?? "null";
    }
}

// `static readonly` initializers constructing the wrapper at several T. They all
// fire at assembly load, so a failure here blocks startup, not some later path.
static class Settings
{
    private static readonly SettingValue<int> maxFps = new SettingValue<int>(60);
    private static readonly SettingValue<float> masterVolume = new SettingValue<float>(0.8f);
    private static readonly SettingValue<bool> fullScreen = new SettingValue<bool>(true);
    private static readonly SettingValue<string> language = new SettingValue<string>("en");

    public static int MaxFps => maxFps.Value;
    public static float MasterVolume => masterVolume.Value;
    public static bool FullScreen => fullScreen.Value;
    public static string Language => language.Value;

    public static void Drive()
    {
        maxFps.Value = 120;
        masterVolume.Value = 0.5f;
        fullScreen.Value = false;
        language.Value = "fr";
    }
}

class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("sw fps=" + Settings.MaxFps);
        Console.WriteLine("sw vol=" + Settings.MasterVolume);
        Console.WriteLine("sw full=" + Settings.FullScreen);
        Console.WriteLine("sw lang=" + Settings.Language);

        Settings.Drive();
        Console.WriteLine("sw fps2=" + Settings.MaxFps);
        Console.WriteLine("sw lang2=" + Settings.Language);

        // op_Implicit unwrap and value-equality across independent instances.
        var a = new SettingValue<int>(1);
        var b = new SettingValue<int>(1);
        var c = new SettingValue<int>(2);
        int unwrapped = a;
        Console.WriteLine("sw unwrap=" + unwrapped);
        Console.WriteLine("sw eq=" + a.Equals(b));
        Console.WriteLine("sw neq=" + a.Equals(c));
        Console.WriteLine("sw hash=" + (a.GetHashCode() == b.GetHashCode()));

        // The self-type castclass path through the interface.
        IAssignableSetting s = new SettingValue<int>(0);
        s.AssignFrom(new SettingValue<int>(99));
        Console.WriteLine("sw assigned=" + ((SettingValue<int>)s).Value);

        // The string and IEnumerable arms of Equals.
        var w1 = new SettingValue<string>("hi");
        var w2 = new SettingValue<string>("hi");
        Console.WriteLine("sw str eq=" + w1.Equals(w2));

        var l1 = new SettingValue<List<object>>(new List<object> { 1, "two" });
        var l2 = new SettingValue<List<object>>(new List<object> { 1, "two" });
        Console.WriteLine("sw list eq=" + l1.Equals(l2));
    }
}
