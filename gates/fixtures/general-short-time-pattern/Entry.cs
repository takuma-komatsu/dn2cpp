internal static class Entry
{
#if OFFSET_PATTERN
    private static void Main() => DateTimeOffsetPatternSubset.Program.Run();
#elif LONG_PATTERN
    private static void Main() => GeneralLongTimePatternSubset.Program.Run();
#else
    private static void Main() => GeneralShortTimePatternSubset.Program.Run();
#endif
}
