using OsrsSkillTracker.Helpers;

namespace OsrsSkillTracker.ViewModels;

public class SkillRowViewModel
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int Level { get; set; }
    public long Xp { get; set; }
    public int Rank { get; set; }
    public long XpGainedSinceLastSession { get; set; }

    public long XpToNextLevel =>
        Level >= 99 ? 0 : OsrsXpTable.GetXpForLevel(Level + 1) - Xp;

    public double PercentToNextLevel
    {
        get
        {
            if (Level >= 99) return 1.0;
            long xpAtLevel = OsrsXpTable.GetXpForLevel(Level);
            long xpAtNext = OsrsXpTable.GetXpForLevel(Level + 1);
            long range = xpAtNext - xpAtLevel;
            if (range <= 0) return 1.0;
            return (Xp - xpAtLevel) / (double)range;
        }
    }
}
