using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services.Messages;

public class StatsRefreshedMessage
{
    public HiscoresResult Result { get; init; } = new();
}
