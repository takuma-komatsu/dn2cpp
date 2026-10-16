namespace AdmissionFilters;

public sealed class SelectionAnchor { }

public sealed class SelectionSzTarget
{
    public string Run() => "library sz";
}

public sealed class SelectionMdTarget
{
    public string Run() => "library md";
}

public sealed class AssemblyChoiceTarget
{
    public static string Run() => "library";
}

public sealed class AssemblyConstructedTarget
{
    public override string ToString() => "library constructed";
}

public sealed class LibraryAssemblyChoiceTarget
{
    public static string Run() => "library unique";
}

public sealed class LibraryAssemblyConstructedTarget
{
    public override string ToString() => "library unique constructed";
}
