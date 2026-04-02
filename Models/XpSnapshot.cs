namespace OsrsSkillTracker.Models;

public class XpSnapshot
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int SkillId { get; set; }
    public int Level { get; set; }
    public long Xp { get; set; }
    public int Rank { get; set; }
    public DateTime RecordedAt { get; set; }
    public Player Player { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
