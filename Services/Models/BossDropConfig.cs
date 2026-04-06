namespace OsrsSkillTracker.Services.Models;

public class BossDropConfig
{
    public string BossKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<UniqueDropConfig> Uniques { get; set; } = new();
}

public class UniqueDropConfig
{
    public string ItemName { get; set; } = string.Empty;
    public int DropRate { get; set; }
}
