using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

public interface IPollingService
{
    void Start(string username);
    void Stop();
    bool IsRunning { get; }
    event EventHandler<HiscoresResult>? StatsRefreshed;
    event EventHandler<string>? PollingError;
}
