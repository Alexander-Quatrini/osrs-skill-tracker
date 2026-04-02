namespace OsrsSkillTracker.Models;

public class BossKill
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string BossKey { get; set; } = string.Empty;
    public int KillCount { get; set; }
    public int Rank { get; set; }
    public DateTime RecordedAt { get; set; }
    public Player Player { get; set; } = null!;
}
