namespace OsrsSkillTracker.ViewModels;

public class BossRowViewModel
{
    public string BossKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int KillCount { get; set; }
    public int Rank { get; set; }
    public bool HasDropConfig { get; set; }
    public List<ExpectedDropViewModel> ExpectedDrops { get; set; } = [];
}
