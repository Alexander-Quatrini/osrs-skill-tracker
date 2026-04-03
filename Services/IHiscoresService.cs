using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

public interface IHiscoresService
{
    Task<HiscoresResult> FetchPlayerStatsAsync(string username);
}
