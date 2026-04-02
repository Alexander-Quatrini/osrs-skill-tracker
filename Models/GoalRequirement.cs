namespace OsrsSkillTracker.Models;

public class GoalRequirement
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public int SkillId { get; set; }
    public int TargetLevel { get; set; }
    public long TargetXp { get; set; }
    public Goal Goal { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
