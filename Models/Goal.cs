namespace OsrsSkillTracker.Models;

public class Goal
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsCompleted { get; set; }
    public Player Player { get; set; } = null!;
    public ICollection<GoalRequirement> Requirements { get; set; } = [];
}
