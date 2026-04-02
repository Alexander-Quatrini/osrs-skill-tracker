namespace OsrsSkillTracker.Models;

public class Player
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public ICollection<Goal> Goals { get; set; } = [];
    public ICollection<XpSnapshot> XpSnapshots { get; set; } = [];
    public ICollection<BossKill> BossKills { get; set; } = [];
}
