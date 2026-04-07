namespace OsrsSkillTracker.ViewModels;

public class GoalSummaryViewModel
{
    public int GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public double OverallProgressPercent { get; set; }
    public int RequirementCount { get; set; }
    public int CompletedRequirementCount { get; set; }
}
