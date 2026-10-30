internal static class Entry
{
#if LONG_PATTERN
    private static void Main() => GeneralLongTimePatternSubset.Program.Run();
#else
    private static void Main() => GeneralShortTimePatternSubset.Program.Run();
#endif
}
