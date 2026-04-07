using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Services;
using OsrsSkillTracker.Services.Messages;

namespace OsrsSkillTracker.ViewModels;

public partial class SkillListViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly IPollingService _polling;

    public SkillListViewModel(AppDbContext db, IPollingService polling)
    {
        _db = db;
        _polling = polling;
        Title = "Skills";

        WeakReferenceMessenger.Default.Register<StatsRefreshedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(async () => await ((SkillListViewModel)recipient).LoadAsync()));
    }

    [ObservableProperty]
    ObservableCollection<SkillRowViewModel> skills = [];

    [ObservableProperty]
    SkillRowViewModel? selectedSkill;

    [ObservableProperty]
    string playerName = string.Empty;

    [ObservableProperty]
    string lastUpdated = string.Empty;

    [RelayCommand]
    async Task LoadAsync()
    {
        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player is null) return;

        PlayerName = player.DisplayName.Length > 0 ? player.DisplayName : player.Username;
        LastUpdated = player.LastUpdated.ToLocalTime().ToString("g");

        var latestSnapshots = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id)
            .GroupBy(s => s.SkillId)
            .Select(g => g.OrderByDescending(s => s.RecordedAt).First())
            .ToListAsync();

        var skillMeta = await _db.Skills.ToListAsync();
        var skillDict = skillMeta.ToDictionary(s => s.Id);

        // Previous snapshot XP for delta
        var distinctTimestamps = await _db.XpSnapshots
            .Where(s => s.PlayerId == player.Id)
            .Select(s => s.RecordedAt)
            .Distinct()
            .OrderByDescending(t => t)
            .Take(2)
            .ToListAsync();

        Dictionary<int, long> previousXpMap = [];
        if (distinctTimestamps.Count >= 2)
        {
            var prevSnapshots = await _db.XpSnapshots
                .Where(s => s.PlayerId == player.Id && s.RecordedAt == distinctTimestamps[1])
                .ToListAsync();
            previousXpMap = prevSnapshots.ToDictionary(s => s.SkillId, s => s.Xp);
        }

        var rows = latestSnapshots
            .Where(s => skillDict.ContainsKey(s.SkillId))
            .Select(s =>
            {
                var skill = skillDict[s.SkillId];
                previousXpMap.TryGetValue(s.SkillId, out long prevXp);
                long delta = s.Xp - prevXp;
                return new SkillRowViewModel
                {
                    SkillId = s.SkillId,
                    SkillName = skill.Name,
                    IconKey = skill.IconKey,
                    DisplayOrder = skill.DisplayOrder,
                    Level = s.Level,
                    Xp = s.Xp,
                    Rank = s.Rank,
                    XpGainedSinceLastSession = delta > 0 ? delta : 0
                };
            })
            .OrderBy(r => r.DisplayOrder)
            .ToList();

        Skills = new ObservableCollection<SkillRowViewModel>(rows);
    }

    [RelayCommand]
    void SelectSkill(SkillRowViewModel skill) => SelectedSkill = skill;

    [RelayCommand]
    void CloseDetail() => SelectedSkill = null;
}
