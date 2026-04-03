namespace OsrsSkillTracker.Services.Models;

public class HiscoresResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SkillStat> Skills { get; set; } = new();
    public List<BossKillStat> BossKills { get; set; } = new();
}

public class SkillStat
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int Level { get; set; }
    public long Xp { get; set; }
}

public class BossKillStat
{
    public string BossKey { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int KillCount { get; set; }
}
