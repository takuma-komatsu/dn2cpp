internal static class Entry
{
#if FULL_DAY
    private static void Main() => FullDaySubset.Program.Run();
#else
    private static void Main() => AbbreviatedDaySubset.Program.Run();
#endif
}
