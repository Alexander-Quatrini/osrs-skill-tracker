namespace OsrsSkillTracker.Helpers;

public static class OsrsXpTable
{
    private static readonly long[] _table = BuildTable();

    private static long[] BuildTable()
    {
        var table = new long[100]; // index 0 unused, 1–99
        table[1] = 0;
        for (int level = 2; level <= 99; level++)
        {
            double sum = 0;
            for (int i = 1; i < level; i++)
                sum += Math.Floor(i + 300.0 * Math.Pow(2.0, i / 7.0));
            table[level] = (long)Math.Floor(sum / 8.0);
        }
        return table;
    }

    public static long GetXpForLevel(int level)
    {
        if (level <= 1) return 0;
        if (level >= 99) return 13_034_431;
        return _table[level];
    }
}
