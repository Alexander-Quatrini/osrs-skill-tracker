using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Helpers;
using OsrsSkillTracker.Models;
using OsrsSkillTracker.Services;
using OsrsSkillTracker.Services.Messages;

namespace OsrsSkillTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly IPollingService _polling;
    private readonly IHiscoresService _hiscores;

    public DashboardViewModel(AppDbContext db, IPollingService polling, IHiscoresService hiscores)
    {
        _db = db;
        _polling = polling;
        _hiscores = hiscores;
        Title = "Dashboard";

        WeakReferenceMessenger.Default.Register<StatsRefreshedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(async () => await ((DashboardViewModel)recipient).LoadAsync()));

        WeakReferenceMessenger.Default.Register<PollingErrorMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(() => ((DashboardViewModel)recipient).ErrorMessage = message.Error));
    }

    [ObservableProperty]
    string playerName = string.Empty;

    [ObservableProperty]
    string lastUpdated = string.Empty;

    [ObservableProperty]
    string errorMessage = string.Empty;

    [ObservableProperty]
    int totalLevel;

    [ObservableProperty]
    long totalXp;

    [ObservableProperty]
    int combatLevel;

    [ObservableProperty]
    ObservableCollection<SkillRowViewModel> closestToLevel = [];

    [ObservableProperty]
    ObservableCollection<SkillRowViewModel> recentGains = [];

    [ObservableProperty]
    ObservableCollection<GoalSummaryViewModel> activeGoals = [];

    [ObservableProperty]
    string wikiSearchQuery = string.Empty;

    [RelayCommand]
    async Task LoadAsync()
    {
        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        if (!_polling.IsRunning)
            _polling.Start(username);

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player is null) return;

        PlayerName = player.DisplayName.Length > 0 ? player.DisplayName : player.Username;
        LastUpdated = player.LastUpdated.ToLocalTime().ToString("g");

        // Latest snapshot per skill
        var latestSnapshots = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id)
            .GroupBy(s => s.SkillId)
            .Select(g => g.OrderByDescending(s => s.RecordedAt).First())
            .ToListAsync();

        var skills = await _db.Skills.ToListAsync();
        var skillDict = skills.ToDictionary(s => s.Id);

        // Build SkillRowViewModels for latest snapshots
        var rows = latestSnapshots
            .Where(s => skillDict.ContainsKey(s.SkillId))
            .Select(s =>
            {
                var skill = skillDict[s.SkillId];
                return new SkillRowViewModel
                {
                    SkillId = s.SkillId,
                    SkillName = skill.Name,
                    IconKey = skill.IconKey,
                    DisplayOrder = skill.DisplayOrder,
                    Level = s.Level,
                    Xp = s.Xp,
                    Rank = s.Rank
                };
            })
            .ToList();

        TotalLevel = rows.Sum(r => r.Level);
        TotalXp = rows.Sum(r => r.Xp);

        // Combat level using correct skill IDs
        int GetLevel(int skillId) => rows.FirstOrDefault(r => r.SkillId == skillId)?.Level ?? 1;
        CombatLevel = OsrsCombatCalculator.GetCombatLevel(
            attack: GetLevel(1),
            strength: GetLevel(4),
            defence: GetLevel(7),
            hitpoints: GetLevel(2),
            prayer: GetLevel(13),
            ranged: GetLevel(10),
            magic: GetLevel(16));

        // Closest to level: top 5 non-99 skills by PercentToNextLevel descending
        ClosestToLevel = new ObservableCollection<SkillRowViewModel>(
            rows.Where(r => r.Level < 99)
                .OrderByDescending(r => r.PercentToNextLevel)
                .Take(5));

        // Recent gains: compare two most recent distinct RecordedAt values
        var distinctTimestamps = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id)
            .Select(s => s.RecordedAt)
            .Distinct()
            .OrderByDescending(t => t)
            .Take(2)
            .ToListAsync();

        if (distinctTimestamps.Count < 2)
        {
            RecentGains = [];
        }
        else
        {
            var latestTime = distinctTimestamps[0];
            var previousTime = distinctTimestamps[1];

            var latestSnapshotMap = latestSnapshots.ToDictionary(s => s.SkillId);

            var previousSnapshots = await _db.XpSnapshots
                .Where(s => s.PlayerId == player.Id && s.RecordedAt == previousTime)
                .ToListAsync();

            var recentGainRows = new List<SkillRowViewModel>();
            foreach (var prev in previousSnapshots)
            {
                if (!latestSnapshotMap.TryGetValue(prev.SkillId, out var latest)) continue;
                long delta = latest.Xp - prev.Xp;
                if (delta <= 0) continue;
                var row = rows.FirstOrDefault(r => r.SkillId == prev.SkillId);
                if (row is null) continue;
                recentGainRows.Add(new SkillRowViewModel
                {
                    SkillId = row.SkillId,
                    SkillName = row.SkillName,
                    IconKey = row.IconKey,
                    DisplayOrder = row.DisplayOrder,
                    Level = row.Level,
                    Xp = row.Xp,
                    Rank = row.Rank,
                    XpGainedSinceLastSession = delta
                });
            }
            RecentGains = new ObservableCollection<SkillRowViewModel>(recentGainRows);
        }

        // Active goals
        var goals = await _db.Goals
            .Include(g => g.Requirements)
            .Where(g => g.PlayerId == player.Id && !g.IsCompleted)
            .ToListAsync();

        var xpBySkillId = latestSnapshots.ToDictionary(s => s.SkillId, s => s.Xp);
        var goalSummaries = goals.Select(g =>
        {
            var progresses = g.Requirements.Select(req =>
            {
                xpBySkillId.TryGetValue(req.SkillId, out long currentXp);
                return Math.Min(1.0, currentXp / (double)req.TargetXp);
            }).ToList();

            double overall = progresses.Count > 0 ? progresses.Average() * 100.0 : 0.0;
            int completed = g.Requirements.Count(req =>
            {
                xpBySkillId.TryGetValue(req.SkillId, out long currentXp);
                return currentXp >= req.TargetXp;
            });

            return new GoalSummaryViewModel
            {
                GoalId = g.Id,
                GoalName = g.Name,
                OverallProgressPercent = overall,
                RequirementCount = g.Requirements.Count,
                CompletedRequirementCount = completed
            };
        });

        ActiveGoals = new ObservableCollection<GoalSummaryViewModel>(goalSummaries);
    }

    [RelayCommand]
    async Task OpenWikiSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(WikiSearchQuery)) return;
        var uri = new Uri("https://oldschool.runescape.wiki/?search=" + Uri.EscapeDataString(WikiSearchQuery));
        await Launcher.OpenAsync(uri);
    }

    [RelayCommand]
    async Task RefreshAsync()
    {
        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        IsBusy = true;
        try
        {
            var result = await _hiscores.FetchPlayerStatsAsync(username);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Refresh failed.";
                return;
            }

            var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
            if (player is null) return;

            var now = DateTime.UtcNow;
            player.LastUpdated = now;

            foreach (var skill in result.Skills)
            {
                _db.XpSnapshots.Add(new XpSnapshot
                {
                    PlayerId = player.Id,
                    SkillId = skill.SkillId,
                    Level = skill.Level,
                    Xp = skill.Xp,
                    Rank = skill.Rank,
                    RecordedAt = now
                });
            }

            foreach (var boss in result.BossKills)
            {
                _db.BossKills.Add(new BossKill
                {
                    PlayerId = player.Id,
                    BossKey = boss.BossKey,
                    KillCount = boss.KillCount,
                    Rank = boss.Rank,
                    RecordedAt = now
                });
            }

            await _db.SaveChangesAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task ChangePlayerAsync()
    {
        Preferences.Remove("active_username");
        _polling.Stop();
        await Shell.Current.GoToAsync("//search");
    }
}
