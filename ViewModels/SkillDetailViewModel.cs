using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;

namespace OsrsSkillTracker.ViewModels;

public partial class SkillDetailViewModel : BaseViewModel
{
    private readonly AppDbContext _db;

    public SkillDetailViewModel(AppDbContext db)
    {
        _db = db;
    }

    [ObservableProperty]
    SkillRowViewModel? skill;

    [ObservableProperty]
    ObservableCollection<XpHistoryPointViewModel> xpHistory = [];

    [ObservableProperty]
    ObservableCollection<GoalSummaryViewModel> relatedGoals = [];

    [RelayCommand]
    async Task LoadAsync(int skillId)
    {
        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player is null) return;

        var history = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id && s.SkillId == skillId)
            .OrderByDescending(s => s.RecordedAt)
            .Take(30)
            .ToListAsync();

        XpHistory = new ObservableCollection<XpHistoryPointViewModel>(
            history
                .OrderBy(s => s.RecordedAt)
                .Select(s => new XpHistoryPointViewModel
                {
                    RecordedAt = s.RecordedAt,
                    Xp = s.Xp,
                    Level = s.Level
                }));

        var goals = await _db.Goals
            .Include(g => g.Requirements)
            .Where(g => g.PlayerId == player.Id && !g.IsCompleted && g.Requirements.Any(r => r.SkillId == skillId))
            .ToListAsync();

        var latestSnapshots = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id)
            .GroupBy(s => s.SkillId)
            .Select(g => g.OrderByDescending(s => s.RecordedAt).First())
            .ToListAsync();

        var xpBySkillId = latestSnapshots.ToDictionary(s => s.SkillId, s => s.Xp);

        RelatedGoals = new ObservableCollection<GoalSummaryViewModel>(
            goals.Select(g =>
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
            }));
    }

    [RelayCommand]
    async Task NavigateToAddGoalAsync()
    {
        if (Skill is null) return;
        await Shell.Current.GoToAsync("goal", new Dictionary<string, object>
        {
            ["preselectedSkillId"] = Skill.SkillId
        });
    }
}
