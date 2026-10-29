internal static class Entry
{
#if FULL_MONTH
    private static void Main() => FullMonthSubset.Program.Run();
#else
    private static void Main() => AbbreviatedMonthSubset.Program.Run();
#endif
}
