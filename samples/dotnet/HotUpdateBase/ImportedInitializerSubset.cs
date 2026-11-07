namespace HotUpdateBase;

// Precise-init AOT types (an explicit static constructor) that the patch enters first,
// through a static-method, a constructor and a static-field import. Nothing in the base
// touches them, so each initializer runs at the patch's import.
public static class InitializerStage
{
    public static string? Log;

    public static void Note(string entry) => Log = Log is null ? entry : Log + "," + entry;

    public static string Report(int value) => value + "/" + (Log ?? "none");
}

public static class ImportedStaticInitializer
{
    static ImportedStaticInitializer() => InitializerStage.Note("static");

    public static int Touch() => 61;
}

public sealed class ImportedConstructedInitializer
{
    static ImportedConstructedInitializer() => InitializerStage.Note("constructed");

    public int Value() => 62;
}

public static class ImportedFieldInitializer
{
    public static int Value;

    static ImportedFieldInitializer()
    {
        InitializerStage.Note("field");
        Value = 63;
    }
}
