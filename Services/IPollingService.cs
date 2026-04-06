namespace OsrsSkillTracker.Services;

public interface IPollingService
{
    void Start(string username);
    void Stop();
    bool IsRunning { get; }
}
