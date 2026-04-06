using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Models;
using OsrsSkillTracker.Services.Messages;
using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

// NOTE: PollingService is a singleton but AppDbContext is scoped. This violates
// DI lifetime best practices and means the same DbContext instance is reused
// across all polls. This is per the Phase 2 spec and is safe on Windows desktop
// where there is no scope validation crash. A future refactor should inject
// IServiceProvider and create a scope per poll instead.
public class PollingService : IPollingService, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IHiscoresService _hiscores;
    private readonly AppDbContext _db;

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    public bool IsRunning => _loopTask is { IsCompleted: false };

    public PollingService(IHiscoresService hiscores, AppDbContext db)
    {
        _hiscores = hiscores;
        _db = db;
    }

    public void Start(string username)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(username, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunLoopAsync(string username, CancellationToken ct)
    {
        await PollOnceAsync(username, ct); // fire immediately on Start()

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await PollOnceAsync(username, ct);
        }
        catch (OperationCanceledException)
        {
            // Normal Stop() — not an error
        }
    }

    private async Task PollOnceAsync(string username, CancellationToken ct)
    {
        var result = await _hiscores.FetchPlayerStatsAsync(username);

        if (!result.Success)
        {
            WeakReferenceMessenger.Default.Send(new PollingErrorMessage { Error = result.ErrorMessage ?? "Unknown error" });
            return;
        }

        var player = await _db.Players
            .FirstOrDefaultAsync(p => p.Username == username, ct);

        if (player is null)
        {
            WeakReferenceMessenger.Default.Send(new PollingErrorMessage { Error = $"Player '{username}' not found in database." });
            return;
        }

        var now = DateTime.UtcNow;

        _db.XpSnapshots.AddRange(result.Skills.Select(s => new XpSnapshot
        {
            PlayerId   = player.Id,
            SkillId    = s.SkillId,
            Level      = s.Level,
            Xp         = s.Xp,
            Rank       = s.Rank,
            RecordedAt = now,
        }));

        _db.BossKills.AddRange(result.BossKills.Select(b => new BossKill
        {
            PlayerId   = player.Id,
            BossKey    = b.BossKey,
            KillCount  = b.KillCount,
            Rank       = b.Rank,
            RecordedAt = now,
        }));

        player.LastUpdated = now;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WeakReferenceMessenger.Default.Send(new PollingErrorMessage { Error = "Database write failed." });
            return;
        }

        // Sends on a ThreadPool thread — ViewModel recipients must marshal UI updates
        // via MainThread.BeginInvokeOnMainThread(...)
        WeakReferenceMessenger.Default.Send(new StatsRefreshedMessage { Result = result });
    }

    public void Dispose() => Stop();
}
