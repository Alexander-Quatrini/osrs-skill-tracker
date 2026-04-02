namespace OsrsSkillTracker.Models;

public class Goal
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int SkillId { get; set; }
    public int TargetLevel { get; set; }
    public long TargetXp { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCompleted { get; set; }
    public Player Player { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
